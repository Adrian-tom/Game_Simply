using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Dom w trybie miasta: mieszkańcy są liczbą, a nie osobnymi postaciami.</summary>
    public sealed class DomMiasta
    {
        public double Mieszkancy;
    }

    /// <summary>
    /// Tryb miasta: po awansie na szczebel Miasto gra przechodzi w city buildera na tej samej mapie.
    /// Kamera odrywa się od bohatera, a stawianie nie wymaga jego obecności. Dom działa tylko przy drodze,
    /// która prowadzi do składu w jego zasięgu. Nocne fale ustają: najazdy miejskie dojdą w późniejszym etapie.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const int PojemnoscDomu = 8;
        public const int ZasiegSkladu = 24;              // tyle kafli drogi od składu
        public const double WprowadzaniePerS = 0.25;     // tylu mieszkańców na sekundę wprowadza się do domu
        public const double WyprowadzaniePerS = 0.05;
        public const double PredkoscKamery = 11.0;
        public const int MieszkancowNaCel = 16;

        public static readonly string[] BudowleMiejskie = { "droga", "dom", "sklad", "wyburz" };

        private static readonly (string Id, string Nazwa, string[] Budowle)[] KategorieMiejskie =
        {
            ("miasto", "Miasto", BudowleMiejskie),
            ("obrona", "Obrona", new[] { "palisada", "brama", "wieza" }),
            ("zaklady", "Zakłady", Stacje.Concat(BudynkiMiejskie).ToArray()),
            ("rzemioslo", "Rzemiosło", new string[0]),
        };

        /// <summary>Gracz wrócił na chwilę do bohatera (klawisz H); domyślnie steruje kamerą.</summary>
        public bool SterujeBohaterem;

        public double KameraX, KameraY;

        /// <summary>Ile sekund jeszcze pokazywać komunikat o założeniu miasta.</summary>
        public double ZaZalozeniem;

        public readonly HashSet<(int X, int Y)> Drogi = new HashSet<(int X, int Y)>();
        public readonly Dictionary<(int X, int Y), DomMiasta> Domy = new Dictionary<(int X, int Y), DomMiasta>();

        /// <summary>Rośnie przy każdej zmianie sieci dróg: warstwa rysująca wie wtedy, że ma przebudować siatkę.</summary>
        public int WersjaDrog;

        private readonly Dictionary<(int X, int Y), (int X, int Y)> _stopy = new Dictionary<(int X, int Y), (int X, int Y)>();
        private readonly HashSet<(int X, int Y)> _wZasiegu = new HashSet<(int X, int Y)>();

        public bool TrybMiasta => Szczebel >= Szczeble.Length - 1;

        public bool KameraMiejska => TrybMiasta && !SterujeBohaterem;

        public int Mieszkancy => (int)Domy.Values.Sum(d => d.Mieszkancy);

        public (string Id, string Nazwa, string[] Budowle)[] KategorieTeraz => TrybMiasta ? KategorieMiejskie : Kategorie;

        /// <summary>Zakładka z wczytanego zapisu mogła zniknąć wraz ze zmianą trybu: wtedy otwiera się pierwsza.</summary>
        private void PoprawKategorie()
        {
            if (KategorieTeraz.All(k => k.Id != Kategoria))
            {
                Kategoria = KategorieTeraz[0].Id;
            }
        }

        /// <summary>Budynek zajmuje kwadrat o tym boku, licząc od kafla zaczepienia.</summary>
        public static int Stopa(string klucz)
        {
            return klucz == "dom" ? 2 : klucz == "sklad" ? 3 : 1;
        }

        /// <summary>Czy droga z tego kafla dochodzi do składu w jego zasięgu.</summary>
        public bool DrogaWZasiegu(int x, int y)
        {
            return _wZasiegu.Contains((x, y));
        }

        public bool DomPodlaczony((int X, int Y) dom)
        {
            return Obrzeze(dom, Stopa("dom")).Any(_wZasiegu.Contains);
        }

        /// <summary>Automatyczne przejście po awansie: ta sama mapa, kamera idzie w górę.</summary>
        private void ZalozMiasto()
        {
            SterujeBohaterem = false;
            Spi = false;
            KameraX = X;
            KameraY = Y;
            Kategoria = "miasto";
            TrybBudowy = false;
            WybranaBudowla = null;
            PanelOtwarty = false;
            GoscOtwarty = false;
            ZaZalozeniem = 10.0;
            foreach (Wrog g in Wrogowie.Where(g => g.Stan != StanWroga.Martwy))
            {
                g.Stan = StanWroga.Martwy;
                g.Timer = 2.2;
            }
            Pociski.Clear();
            Pisz("Założono miasto! Od teraz budujesz z góry, a bohater czeka na wezwanie.");
        }

        /// <summary>Krok kamery i życia domów. Zwraca true, gdy klawisze kierunku poszły do kamery.</summary>
        private bool KrokMiasta(double dt, Wejscie w)
        {
            ZaZalozeniem = Math.Max(0.0, ZaZalozeniem - dt);
            foreach (KeyValuePair<(int X, int Y), DomMiasta> d in Domy)
            {
                d.Value.Mieszkancy = DomPodlaczony(d.Key)
                    ? Math.Min(PojemnoscDomu, d.Value.Mieszkancy + WprowadzaniePerS * dt)
                    : Math.Max(0.0, d.Value.Mieszkancy - WyprowadzaniePerS * dt);
            }
            if (!KameraMiejska)
            {
                return false;
            }
            double dl = Math.Sqrt(w.Dx * w.Dx + w.Dy * w.Dy);
            if (dl > 1e-9)
            {
                KameraX = Math.Max(-GeneratorSwiata.Granica, Math.Min(GeneratorSwiata.Granica, KameraX + w.Dx / dl * PredkoscKamery * dt));
                KameraY = Math.Max(-GeneratorSwiata.Granica, Math.Min(GeneratorSwiata.Granica, KameraY + w.Dy / dl * PredkoscKamery * dt));
            }
            return true;
        }

        private void PrzelaczBohatera()
        {
            if (!TrybMiasta)
            {
                return;
            }
            SterujeBohaterem = !SterujeBohaterem;
            KameraX = X;
            KameraY = Y;
            TrybBudowy = false;
            WybranaBudowla = null;
            PanelOtwarty = false;
            Spi = false;
        }

        // ------------------------------------------------------------------ //
        //  Stawianie                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Powód odmowy dla budowli zajmującej kilka kafli; w mieście las i skały można wyciąć.</summary>
        private string PowodStopy(string klucz, int x, int y)
        {
            if (klucz == "wyburz")
            {
                return CoWyburzyc(x, y) == null ? "Tu nie ma czego wyburzać." : null;
            }
            int bok = Stopa(klucz);
            for (int a = x; a < x + bok; a++)
            {
                for (int b = y; b < y + bok; b++)
                {
                    if (!GeneratorSwiata.WGranicach(a, b) || Budowle.ContainsKey((a, b)) || _stopy.ContainsKey((a, b)) || Drogi.Contains((a, b)))
                    {
                        return "Tu nie da się nic postawić.";
                    }
                    Obiekt o = ObiektNa(a, b);
                    if (o == Obiekt.Woda || o == Obiekt.Sciana || (o != Obiekt.Brak && !TrybMiasta))
                    {
                        return "Tu nie da się nic postawić.";
                    }
                }
            }
            if (klucz != "spiwor" && klucz != "droga" && X + Promien > x + 0.12 && X - Promien < x + bok - 0.12
                && Y + Promien > y + 0.12 && Y - Promien < y + bok - 0.12)
            {
                return KameraMiejska ? "Stoi tu bohater." : "Stoisz w tym miejscu — odsuń się.";
            }
            return null;
        }

        private (int X, int Y)? CoWyburzyc(int x, int y)
        {
            if (Drogi.Contains((x, y)) || Budowle.ContainsKey((x, y)))
            {
                return (x, y);
            }
            return _stopy.TryGetValue((x, y), out (int X, int Y) kotwica) ? kotwica : ((int, int)?)null;
        }

        private void PostawMiejskie(string klucz, int x, int y)
        {
            if (klucz == "wyburz")
            {
                Wyburz(CoWyburzyc(x, y).Value);
                return;
            }
            int bok = Stopa(klucz);
            for (int a = x; a < x + bok; a++)
            {
                for (int b = y; b < y + bok; b++)
                {
                    Wytnij(a, b);
                }
            }
            if (klucz == "droga")
            {
                Drogi.Add((x, y));
            }
            else
            {
                Budowle[(x, y)] = klucz;
                if (klucz == "dom")
                {
                    Domy[(x, y)] = new DomMiasta();
                }
            }
            PrzeliczMiejsca();
        }

        /// <summary>
        /// Droga ciągnięta myszą: kursor potrafi przeskoczyć kilka kafli, więc wypełniamy łamaną między nimi,
        /// najpierw wzdłuż X, potem wzdłuż Y. Kafle stykające się tylko rogiem nie tworzyłyby połączenia.
        /// </summary>
        public void PolozOdcinekDrogi((int X, int Y) z, (int X, int Y) @do)
        {
            int x = z.X, y = z.Y;
            while (x != @do.X || y != @do.Y)
            {
                if (x != @do.X)
                {
                    x += Math.Sign(@do.X - x);
                }
                else
                {
                    y += Math.Sign(@do.Y - y);
                }
                if (PowodBudowy("droga", x, y) == null)
                {
                    PostawNa("droga", x, y);
                }
            }
        }

        /// <summary>Wycinka pod budowę: obiekt znika na stałe, bez plonu.</summary>
        private void Wytnij(int x, int y)
        {
            if (Zbiory.ContainsKey(ObiektNa(x, y)))
            {
                _zuzyte[(x, y)] = new Zuzycie { Uzyte = int.MaxValue / 2 };
            }
        }

        private void Wyburz((int X, int Y) k)
        {
            Drogi.Remove(k);
            Budowle.Remove(k);
            Place.Remove(k);
            Domy.Remove(k);
            ZycieBudowli.Remove(k);
            PrzeliczMiejsca();
        }

        // ------------------------------------------------------------------ //
        //  Sieć dróg                                                          //
        // ------------------------------------------------------------------ //

        private static IEnumerable<(int X, int Y)> Obrzeze((int X, int Y) k, int bok)
        {
            for (int i = 0; i < bok; i++)
            {
                yield return (k.X + i, k.Y - 1);
                yield return (k.X + i, k.Y + bok);
                yield return (k.X - 1, k.Y + i);
                yield return (k.X + bok, k.Y + i);
            }
        }

        /// <summary>Kafle zajęte przez większe budynki i to, które drogi prowadzą do składu.</summary>
        private void PrzeliczSiec()
        {
            _stopy.Clear();
            foreach (KeyValuePair<(int X, int Y), string> b in Budowle)
            {
                int bok = Stopa(b.Value);
                if (bok <= 1)
                {
                    continue;
                }
                for (int a = b.Key.X; a < b.Key.X + bok; a++)
                {
                    for (int c = b.Key.Y; c < b.Key.Y + bok; c++)
                    {
                        if ((a, c) != b.Key)
                        {
                            _stopy[(a, c)] = b.Key;
                        }
                    }
                }
            }

            _wZasiegu.Clear();
            var kolejka = new Queue<((int X, int Y) K, int D)>();
            foreach (KeyValuePair<(int X, int Y), string> b in Budowle.Where(b => b.Value == "sklad"))
            {
                foreach ((int X, int Y) k in Obrzeze(b.Key, Stopa("sklad")))
                {
                    if (Drogi.Contains(k) && _wZasiegu.Add(k))
                    {
                        kolejka.Enqueue((k, 0));
                    }
                }
            }
            var kroki = new (int Dx, int Dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            while (kolejka.Count > 0)
            {
                ((int X, int Y) k, int d) = kolejka.Dequeue();
                if (d >= ZasiegSkladu)
                {
                    continue;
                }
                foreach ((int dx, int dy) in kroki)
                {
                    (int X, int Y) n = (k.X + dx, k.Y + dy);
                    if (Drogi.Contains(n) && _wZasiegu.Add(n))
                    {
                        kolejka.Enqueue((n, d + 1));
                    }
                }
            }
            WersjaDrog++;
        }
    }
}
