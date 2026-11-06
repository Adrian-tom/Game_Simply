using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Osadnik chodzący po świecie: dane z <see cref="Osada"/> i pozycja w kaflach.</summary>
    public sealed class PostacOsadnika
    {
        public Osadnik O;
        public double X;
        public double Y;
        public double CelX;
        public double CelY;
        public double Postoj;
        public bool Idzie;
        public bool Spi;
        public double Dystans;

        /// <summary>Tyle ciosów szkieletów wytrzyma osadnik, zanim zginie.</summary>
        public double Zycie = 45.0;

        /// <summary>Plan doby: rano, dzień, wieczór, noc.</summary>
        public BlokPlanu[] Plan = SwiatWolny.DomyslnyPlan("");

        public double Zmeczenie;
        public int DniWOsadzie;

        /// <summary>Ukryta wada przybysza; gracz jej nie widzi.</summary>
        public string Sekret;

        /// <summary>Ślad po sabotażu wskazał tę osobę, co nie znaczy, że słusznie.</summary>
        public bool Podejrzany;

        /// <summary>Ostatnia widziana część doby: jej zmiana wysyła osadnika gdzie indziej.</summary>
        public int NumerBloku = -1;

        /// <summary>Strażnik: ma zbroję i właśnie idzie na wroga albo z nim walczy.</summary>
        public bool Uzbrojony;
        public bool WWalce;
        public double AtakCd;

        /// <summary>Rośnie z każdym ciosem strażnika: warstwa rysująca zaczyna wtedy animację.</summary>
        public int Ataki;

        /// <summary>Środki kafli do przejścia po kolei; pusta = jest na miejscu.</summary>
        public readonly Queue<(double X, double Y)> Trasa = new Queue<(double X, double Y)>();
    }

    /// <summary>
    /// Faza 2: osada w świecie czasu rzeczywistego. Gdy stoi obóz (namiot i ognisko), przychodzą ludzie.
    /// Gospodarkę liczy ten sam kod co w grze fabularnej — <see cref="Osada.DzienOsady"/> na ukrytym
    /// <see cref="Gracz"/> (zawody, umiejętności, pasje, morale, jedzenie, opał), a tu jest to, co dochodzi
    /// w tym trybie: magazyn zasilany z plecaka, miejsca do spania z postawionych budowli,
    /// przybysze, chodzenie po świecie i panel zarządzania.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const int MiejscaNamiotu = 2;
        public const int MiejscaChaty = 3;
        public const double ZasiegObozu = 6.0;         // tyle kafli od obozu działa magazyn
        public const double SzansaPrzybysza = 0.4;     // dziennie, gdy jest wolne miejsce (pierwszy przychodzi od razu)
        public const double PredkoscOsadnika = 1.5;

        /// <summary>Zawody dostępne w panelu: te, które nie wymagają jeszcze budynków.</summary>
        public static readonly string[] ZajeciaPanelu =
        {
            "bezczynny", "drwal", "kamieniarz", "zielarz", "mysliwy", "gornik", "straznik", "budowniczy", "rzemieslnik",
        };

        /// <summary>Przedmiot z plecaka → surowiec w magazynie osady (włókna magazyn nie przyjmuje).</summary>
        private static readonly Dictionary<string, string> DoMagazynu = new Dictionary<string, string>
        {
            { "drewno", "drewno" }, { "kamien", "kamien" }, { "ziola", "ziola" }, { "jagody", "zywnosc" }, { "ruda", "ruda" },
        };

        public Gracz Obozowisko { get; private set; }
        public readonly List<PostacOsadnika> Postacie = new List<PostacOsadnika>();
        public bool PanelOtwarty;
        public int Wybrany;

        private Losowanie _rng;
        private bool _bylPrzybysz;

        private void ZalozStanOsady(long seed)
        {
            _rng = new Losowanie((seed ^ 0x5bd1e995L) & 0x7FFFFFFFL);
            Obozowisko = new Gracz("Wędrowiec", "Wojownik", new Losowanie(seed & 0x7FFFFFFFL));
            Obozowisko.Osadnicy.Clear();
            Obozowisko.Wiezi.Clear();
            Obozowisko.Chaty = 0;
            Obozowisko.Zloto = 0;
            Obozowisko.WObozie = true;
            Obozowisko.Surowce = Logika.Oboz.Surowce.ToDictionary(s => s.Key, s => 0);
        }

        /// <summary>Ile mam przedmiotu do budowania: plecak razem z magazynem osady.</summary>
        public int Razem(string przedmiot)
        {
            int zMagazynu = DoMagazynu.TryGetValue(przedmiot, out string s) ? Obozowisko.Surowce.Wez(s) : 0;
            return Ile(przedmiot) + zMagazynu + Wyroby.Wez(przedmiot);
        }

        private void Zabierz(string przedmiot, int ile)
        {
            int zPlecaka = Math.Min(Ile(przedmiot), ile);
            Plecak[przedmiot] = Ile(przedmiot) - zPlecaka;
            int reszta = ile - zPlecaka;
            if (reszta > 0 && DoMagazynu.TryGetValue(przedmiot, out string s))
            {
                int zMagazynu = Math.Min(Obozowisko.Surowce.Wez(s), reszta);
                Obozowisko.Surowce[s] = Obozowisko.Surowce.Wez(s) - zMagazynu;
                reszta -= zMagazynu;
            }
            if (reszta > 0)
            {
                Wyroby[przedmiot] = Wyroby.Wez(przedmiot) - reszta;
            }
        }

        /// <summary>Środek obozu: ognisko, a bez niego namiot. Null, gdy nic jeszcze nie stoi.</summary>
        public (int X, int Y)? SrodekObozu()
        {
            foreach (string klucz in new[] { "ognisko", "namiot", "chata" })
            {
                foreach (KeyValuePair<(int X, int Y), string> b in Budowle)
                {
                    if (b.Value == klucz)
                    {
                        return b.Key;
                    }
                }
            }
            return null;
        }

        public bool PrzyObozie()
        {
            (int X, int Y)? c = SrodekObozu();
            if (c == null)
            {
                return false;
            }
            double dx = c.Value.X + 0.5 - X, dy = c.Value.Y + 0.5 - Y;
            return dx * dx + dy * dy <= ZasiegObozu * ZasiegObozu;
        }

        /// <summary>Odkłada z plecaka do magazynu wszystko, co magazyn przyjmuje. Działa tylko przy obozie.</summary>
        public bool Zloz()
        {
            if (!PrzyObozie())
            {
                Pisz("Magazyn jest przy obozie — wróć do ogniska albo namiotu.");
                return false;
            }
            var czesci = new List<string>();
            foreach (KeyValuePair<string, string> m in DoMagazynu)
            {
                int ile = Ile(m.Key);
                if (ile > 0)
                {
                    Plecak[m.Key] = 0;
                    Obozowisko.Surowce[m.Value] = Obozowisko.Surowce.Wez(m.Value) + ile;
                    czesci.Add($"{ile} {Nazwa(m.Key)}");
                }
            }
            Pisz(czesci.Count > 0 ? $"Do magazynu: {string.Join(", ", czesci)}." : "Nie masz nic do odłożenia.");
            return czesci.Count > 0;
        }

        /// <summary>Miejsca do spania osady wynikają z tego, co stoi na mapie.</summary>
        private void PrzeliczMiejsca()
        {
            Obozowisko.Chaty = MiejscaNamiotu * Budowle.Values.Count(b => b == "namiot")
                         + MiejscaChaty * Budowle.Values.Count(b => b == "chata");
        }

        /// <summary>Koniec doby: dzień pracy osady, a potem być może nowy człowiek przy ognisku.</summary>
        private void DzienObozu()
        {
            if (!ObozZalozony)
            {
                return;
            }
            Obozowisko.Czas = Dzien - 1;
            List<(Osadnik O, string Zajecie)> zdjeci = WalkaWlaczona ? WylaczNieobecnych() : null;
            (List<string> kronika, List<string> pilne) = Osada.DzienOsady(Obozowisko, _rng);
            if (zdjeci != null)
            {
                foreach ((Osadnik o, string zajecie) in zdjeci)
                {
                    o.Zajecie = zajecie;
                }
            }
            foreach (string w in pilne.Concat(kronika))
            {
                Pisz(w);
            }
            if (WalkaWlaczona)
            {
                DobaLudzi();
            }
            else if (Osada.WolneChaty(Obozowisko) > 0 && (!_bylPrzybysz || _rng.Losowa() < SzansaPrzybysza))
            {
                _bylPrzybysz = true;
                Osada.ZatrudnijOsadnika(Obozowisko, _rng, "bezczynny", true);
                Osadnik nowy = Obozowisko.Osadnicy[Obozowisko.Osadnicy.Count - 1];
                Pisz($"Przychodzi {nowy.Imie} ({nowy.Cecha}). Umie: {Kompetencje.OpisKrotki(nowy, 2).Replace("🔥", "*")}. Daj zajęcie: Tab.");
            }
            SynchronizujPostacie();
        }

        /// <summary>Postacie na mapie odpowiadają liście osadników: zmarli i ci, co odeszli, znikają.</summary>
        private void SynchronizujPostacie()
        {
            Postacie.RemoveAll(p => !Obozowisko.Osadnicy.Contains(p.O));
            (int X, int Y) c = SrodekObozu() ?? ((int)Math.Floor(X), (int)Math.Floor(Y));
            foreach (Osadnik o in Obozowisko.Osadnicy)
            {
                if (Postacie.All(p => p.O != o))
                {
                    (double px, double py) = WolnyPunktObok(c.X, c.Y);
                    Postacie.Add(new PostacOsadnika { O = o, X = px, Y = py, CelX = px, CelY = py });
                }
            }
            Wybrany = Math.Max(0, Math.Min(Wybrany, Obozowisko.Osadnicy.Count - 1));
        }

        private (double X, double Y) WolnyPunktObok(int x, int y)
        {
            for (int r = 1; r <= 8; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Wolne(x + dx + 0.5, y + dy + 0.5))
                        {
                            return (x + dx + 0.5, y + dy + 0.5);
                        }
                    }
                }
            }
            return (x + 0.5, y + 0.5);
        }

        // ------------------------------------------------------------------ //
        //  Osadnicy na mapie                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>Obiekt, przy którym pracuje dany zawód; Brak = kręci się po obozie.</summary>
        private static Obiekt ObiektPracy(string zajecie)
        {
            switch (zajecie)
            {
                case "drwal": return Obiekt.Drzewo;
                case "kamieniarz":
                case "gornik": return Obiekt.Skala;
                case "zielarz": return Obiekt.Ziolo;
                case "mysliwy": return Obiekt.Krzak;
                default: return Obiekt.Brak;
            }
        }

        /// <summary>Wybiera cel i wyznacza do niego trasę po kaflach, omijając przeszkody.</summary>
        private void NowyCel(PostacOsadnika p, (int X, int Y) c, bool noc)
        {
            WybierzCel(p, c, noc);
            p.Trasa.Clear();
            var start = ((int)Math.Floor(p.X), (int)Math.Floor(p.Y));
            var meta = ((int)Math.Floor(p.CelX), (int)Math.Floor(p.CelY));
            foreach ((int X, int Y) k in SciezkaKafli(start, meta))
            {
                p.Trasa.Enqueue((k.X + 0.5, k.Y + 0.5));
            }
        }

        /// <summary>
        /// Najkrótsza droga po wolnych kaflach (przeszukiwanie wszerz, cztery kierunki, okno ±30 kafli).
        /// Pusta, gdy celu nie da się osiągnąć — osadnik zostaje i wybierze inny cel później.
        /// </summary>
        private List<(int X, int Y)> SciezkaKafli((int X, int Y) start, (int X, int Y) meta)
        {
            var poprzedni = new Dictionary<(int X, int Y), (int X, int Y)> { { start, start } };
            var kolejka = new Queue<(int X, int Y)>();
            kolejka.Enqueue(start);
            var kroki = new (int Dx, int Dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            while (kolejka.Count > 0 && !poprzedni.ContainsKey(meta))
            {
                (int X, int Y) k = kolejka.Dequeue();
                foreach ((int dx, int dy) in kroki)
                {
                    (int X, int Y) n = (k.X + dx, k.Y + dy);
                    if (Math.Abs(n.X - start.X) > 30 || Math.Abs(n.Y - start.Y) > 30
                        || poprzedni.ContainsKey(n) || Zajety(n.X, n.Y))
                    {
                        continue;
                    }
                    poprzedni[n] = k;
                    kolejka.Enqueue(n);
                }
            }
            var trasa = new List<(int X, int Y)>();
            if (!poprzedni.ContainsKey(meta) || meta == start)
            {
                return trasa;
            }
            for ((int X, int Y) k = meta; k != start; k = poprzedni[k])
            {
                trasa.Add(k);
            }
            trasa.Reverse();
            return trasa;
        }

        private void WybierzCel(PostacOsadnika p, (int X, int Y) c, bool noc)
        {
            p.Postoj = 0.0;
            if (noc)
            {
                // Noc: pod dach. Każdy ma swoje stałe miejsce przy jednej z budowli.
                List<(int X, int Y)> dachy = Budowle.Where(b => b.Value == "namiot" || b.Value == "chata")
                                                    .Select(b => b.Key).OrderBy(k => k.X).ThenBy(k => k.Y).ToList();
                (int X, int Y) dom = dachy.Count > 0 ? dachy[Postacie.IndexOf(p) % dachy.Count] : c;
                (p.CelX, p.CelY) = WolnyPunktObok(dom.X, dom.Y);
                return;
            }
            bool przyPracy = !WalkaWlaczona || BlokTeraz(p) == BlokPlanu.Praca;
            if (BudowaZEtapami && przyPracy && p.O.Chory <= 0 && MiejscePracy(p, out (int X, int Y) stanowisko))
            {
                (p.CelX, p.CelY) = WolnyPunktObok(stanowisko.X, stanowisko.Y);
                return;
            }
            Obiekt praca = p.O.Chory > 0 || !przyPracy ? Obiekt.Brak : ObiektPracy(p.O.Zajecie);
            if (praca != Obiekt.Brak)
            {
                // Kilka prób w okolicy obozu: osadnik staje obok drzewa albo skały właściwej dla zawodu.
                for (int proba = 0; proba < 24; proba++)
                {
                    int x = c.X + _rng.Calkowita(-12, 12), y = c.Y + _rng.Calkowita(-12, 12);
                    if (ObiektNa(x, y) == praca)
                    {
                        (p.CelX, p.CelY) = WolnyPunktObok(x, y);
                        return;
                    }
                }
            }
            (p.CelX, p.CelY) = WolnyPunktObok(c.X + _rng.Calkowita(-4, 4), c.Y + _rng.Calkowita(-4, 4));
        }

        /// <summary>Krok chodzenia osadników: do celu, chwila postoju (praca), nowy cel. Nocą śpią przy budowlach.</summary>
        private void RuszPostacie(double dt)
        {
            (int X, int Y)? srodek = SrodekObozu();
            if (srodek == null)
            {
                return;
            }
            bool noc = Godzina >= 21.0 || Godzina < 5.0;
            bool zmiana = noc != _nocPoprzednio;
            _nocPoprzednio = noc;
            foreach (PostacOsadnika p in Postacie)
            {
                if (p.WWalce)
                {
                    continue;
                }
                // W pełnych zasadach sen wyznacza plan osadnika, a nie sama pora doby.
                bool spi = WalkaWlaczona ? BlokTeraz(p) == BlokPlanu.Odpoczynek : noc;
                int blok = NumerBloku(Godzina);
                bool nowyBlok = WalkaWlaczona && p.NumerBloku != blok;
                p.NumerBloku = blok;
                if (WalkaWlaczona ? nowyBlok : zmiana)
                {
                    NowyCel(p, srodek.Value, spi);
                }
                if (p.Trasa.Count == 0)
                {
                    p.Idzie = false;
                    p.Spi = spi;
                    p.Postoj += dt;
                    if (!spi && p.Postoj > 6.0)
                    {
                        NowyCel(p, srodek.Value, false);
                    }
                    continue;
                }
                (double tx, double ty) = p.Trasa.Peek();
                double dx = tx - p.X, dy = ty - p.Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                double krok = Math.Min(d, PredkoscOsadnika * dt);
                p.Spi = false;
                p.Idzie = true;
                if (d > 1e-9)
                {
                    p.X += dx / d * krok;
                    p.Y += dy / d * krok;
                    p.Dystans += krok;
                }
                if (d - krok < 0.02)
                {
                    p.Trasa.Dequeue();
                }
                else if (Zajety((int)Math.Floor(tx), (int)Math.Floor(ty)))
                {
                    NowyCel(p, srodek.Value, spi);     // gracz postawił tu budowlę: nowa trasa
                }
            }
        }

        private bool _nocPoprzednio;

        // ------------------------------------------------------------------ //
        //  Panel zarządzania                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>Panel: góra/dół wybiera osadnika, lewo/prawo zmienia jego zajęcie.</summary>
        private void ObsluzPanel(Wejscie w)
        {
            List<Osadnik> ludzie = Obozowisko.Osadnicy;
            if (ludzie.Count == 0)
            {
                return;
            }
            Wybrany = (Wybrany + w.Nawig + ludzie.Count) % ludzie.Count;
            if (w.Zmien != 0)
            {
                int i = Array.IndexOf(ZajeciaPanelu, ludzie[Wybrany].Zajecie);
                int nowy = ((i < 0 ? 0 : i) + w.Zmien + ZajeciaPanelu.Length) % ZajeciaPanelu.Length;
                UstawZajecieWybranego(ZajeciaPanelu[nowy]);
            }
        }
    }
}
