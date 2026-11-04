using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Polecenia gracza na jedną klatkę: kierunek (osie siatki), działanie i budowanie.</summary>
    public struct Wejscie
    {
        /// <summary>Kierunek ruchu w osiach siatki; długość nie ma znaczenia (logika normalizuje).</summary>
        public double Dx;
        public double Dy;

        /// <summary>Zbieraj / użyj tego, co jest przed bohaterem.</summary>
        public bool Dzialaj;

        /// <summary>Co postawić przed sobą: 0 nic, 1 śpiwór, 2 ognisko, 3 namiot, 4 chata.</summary>
        public int Buduj;
        public bool Jedz;
        public bool Spij;

        /// <summary>Odłóż plecak do magazynu osady.</summary>
        public bool Zloz;

        /// <summary>Otwórz albo zamknij panel osady.</summary>
        public bool Panel;

        /// <summary>W panelu: −1/+1 wybór osadnika.</summary>
        public int Nawig;

        /// <summary>W panelu: −1/+1 zmiana zajęcia wybranego osadnika.</summary>
        public int Zmien;
    }

    /// <summary>Opis zbieractwa: ile razy można użyć obiektu i co daje jedno użycie.</summary>
    public sealed class OpisZbioru
    {
        public int Uzyc;
        public string Czynnosc;
        public KeyValuePair<string, int>[] Plon;

        /// <summary>Po ilu godzinach gry obiekt odrasta (0 = nigdy).</summary>
        public int OdrostGodzin;
    }

    /// <summary>
    /// „Zomboid fantasy”: świat w czasie rzeczywistym. Zaczynasz bez niczego, ze śpiworem; zbierasz
    /// drewno, kamień, jagody i włókno, stawiasz ognisko i namiot, a potem — osadę.
    ///
    /// Czysta logika bez Unity: <see cref="Aktualizuj"/> dostaje czas klatki i polecenia, więc
    /// da się ją sprawdzić testami bez okna. Świat leży w <see cref="GeneratorSwiata"/>
    /// (zero zapisanych kafli); tu jest tylko to, co gracz zmienił, jego potrzeby i plecak.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const double Predkosc = 3.4;            // kafli na sekundę
        public const double Promien = 0.28;
        public const double Zasieg = 1.6;
        public const double CzasZbioru = 0.9;          // sekundy jednego zbioru
        public const double DlugoscDobyS = 480.0;      // rzeczywistych sekund na dobę gry
        public const double SzybkoscSnu = 40.0;        // ile razy szybciej płynie czas podczas snu

        public static readonly Dictionary<Obiekt, OpisZbioru> Zbiory = new Dictionary<Obiekt, OpisZbioru>
        {
            { Obiekt.Drzewo, new OpisZbioru { Uzyc = 3, Czynnosc = "ścinasz drzewo",
                Plon = new[] { new KeyValuePair<string, int>("drewno", 2) } } },
            { Obiekt.Skala, new OpisZbioru { Uzyc = 3, Czynnosc = "łupiesz skałę",
                Plon = new[] { new KeyValuePair<string, int>("kamien", 2) } } },
            { Obiekt.Krzak, new OpisZbioru { Uzyc = 2, Czynnosc = "zrywasz owoce", OdrostGodzin = 48,
                Plon = new[] { new KeyValuePair<string, int>("jagody", 2), new KeyValuePair<string, int>("wlokno", 1) } } },
            { Obiekt.Ziolo, new OpisZbioru { Uzyc = 1, Czynnosc = "zbierasz zioła", OdrostGodzin = 72,
                Plon = new[] { new KeyValuePair<string, int>("ziola", 2), new KeyValuePair<string, int>("wlokno", 1) } } },
        };

        /// <summary>Koszt budowli: klucz → składniki. Śpiwór nie kosztuje nic, ale trzeba go mieć w plecaku.</summary>
        public static readonly Dictionary<string, KeyValuePair<string, int>[]> Koszty =
            new Dictionary<string, KeyValuePair<string, int>[]>
            {
                { "spiwor", new[] { new KeyValuePair<string, int>("spiwor", 1) } },
                { "ognisko", new[] { new KeyValuePair<string, int>("drewno", 5), new KeyValuePair<string, int>("kamien", 3) } },
                { "namiot", new[] { new KeyValuePair<string, int>("drewno", 8), new KeyValuePair<string, int>("wlokno", 6) } },
                { "chata", new[] { new KeyValuePair<string, int>("drewno", 12), new KeyValuePair<string, int>("kamien", 6) } },
            };

        public static readonly string[] KluczeBudowli = { "", "spiwor", "ognisko", "namiot", "chata" };

        public readonly GeneratorSwiata Swiat;
        public readonly long Seed;

        public double X;
        public double Y;

        /// <summary>Kierunek, w który patrzy bohater (wektor jednostkowy w osiach siatki).</summary>
        public (double X, double Y) Kierunek = (0.7071067811865476, 0.7071067811865476);

        public double Godzina = 7.0;
        public int Dzien = 1;
        public double Glod = 100.0;
        public double Energia = 100.0;
        public double Hp = 100.0;
        public bool Spi;
        public bool Martwy;
        public bool ObozZalozony;

        public readonly Dictionary<string, int> Plecak = new Dictionary<string, int> { { "spiwor", 1 } };
        public readonly Dictionary<(int X, int Y), string> Budowle = new Dictionary<(int X, int Y), string>();
        public readonly List<string> Dziennik = new List<string>();

        private sealed class Zuzycie
        {
            public int Uzyte;
            public double OdrostGodzina = -1;     // bezwzględna godzina gry, gdy obiekt odrośnie
        }

        private readonly Dictionary<(int X, int Y), Zuzycie> _zuzyte = new Dictionary<(int X, int Y), Zuzycie>();
        private double _czasGry;                          // godziny od początku gry
        private (int X, int Y, double Postep)? _zbior;
        private bool _snNocny;

        /// <summary>Bohater zaczyna na środku polany startowej.</summary>
        public SwiatWolny(long seed)
        {
            Seed = seed;
            Swiat = new GeneratorSwiata(seed);
            ZalozStanOsady(seed);
            X = 0.5;
            Y = 0.5;
            Pisz("Budzisz się na ziemi. Masz tylko śpiwór. Zbierz drewno, włókno i kamienie — przed nocą postaw obóz.");
        }

        public double GodzinGry => _czasGry;

        public (int X, int Y, double Postep)? Zbieranie => _zbior;

        public string Czas => $"Dzień {Dzien}, {(int)Godzina:00}:{(int)((Godzina - (int)Godzina) * 60):00}";

        /// <summary>Jasność doby 0 (noc) – 1 (dzień) z płynnym świtem i zmierzchem.</summary>
        public double Jasnosc
        {
            get
            {
                double g = Godzina;
                if (g >= 6 && g < 18) return 1.0;
                if (g >= 18 && g < 21) return 1.0 - 0.88 * (g - 18) / 3.0;
                if (g >= 4 && g < 6) return 0.12 + 0.88 * (g - 4) / 2.0;
                return 0.12;
            }
        }

        public int Ile(string przedmiot)
        {
            return Plecak.TryGetValue(przedmiot, out int n) ? n : 0;
        }

        private void Dodaj(string przedmiot, int ile)
        {
            Plecak[przedmiot] = Ile(przedmiot) + ile;
        }

        private void Pisz(string tekst)
        {
            Dziennik.Add(tekst);
            if (Dziennik.Count > 40)
            {
                Dziennik.RemoveAt(0);
            }
        }

        /// <summary>Obiekt na kaflu z uwzględnieniem tego, co gracz ściął albo zebrał.</summary>
        public Obiekt ObiektNa(int x, int y)
        {
            Obiekt bazowy = Swiat.ObiektNa(x, y);
            if (!Zbiory.TryGetValue(bazowy, out OpisZbioru opis))
            {
                return bazowy;
            }
            if (_zuzyte.TryGetValue((x, y), out Zuzycie z))
            {
                if (z.OdrostGodzina >= 0 && _czasGry >= z.OdrostGodzina)
                {
                    _zuzyte.Remove((x, y));
                    return bazowy;
                }
                if (z.Uzyte >= opis.Uzyc)
                {
                    return Obiekt.Brak;
                }
            }
            return bazowy;
        }

        /// <summary>Ile użyć zostało obiektowi (do rysowania: napoczęte drzewo wygląda inaczej).</summary>
        public int Pozostalo(int x, int y)
        {
            Obiekt o = ObiektNa(x, y);
            if (!Zbiory.TryGetValue(o, out OpisZbioru opis))
            {
                return 0;
            }
            return opis.Uzyc - (_zuzyte.TryGetValue((x, y), out Zuzycie z) ? z.Uzyte : 0);
        }

        /// <summary>Czy kafel jest zajęty: przeszkoda w świecie albo postawiona budowla.</summary>
        public bool Zajety(int x, int y)
        {
            if (Budowle.TryGetValue((x, y), out string b) && b != "spiwor")
            {
                return true;
            }
            Obiekt o = ObiektNa(x, y);
            return o == Obiekt.Drzewo || o == Obiekt.Skala || o == Obiekt.Woda || o == Obiekt.Sciana;
        }

        /// <summary>Czy okrąg bohatera w punkcie (px, py) nie wchodzi w żaden zajęty kafel.</summary>
        private bool Wolne(double px, double py)
        {
            int x0 = (int)Math.Floor(px - Promien), x1 = (int)Math.Floor(px + Promien);
            int y0 = (int)Math.Floor(py - Promien), y1 = (int)Math.Floor(py + Promien);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    if (!Zajety(x, y))
                    {
                        continue;
                    }
                    // Przeszkoda zajmuje środek kafla (0,12–0,88), więc między drzewami da się przejść bokiem.
                    if (px + Promien > x + 0.12 && px - Promien < x + 0.88
                        && py + Promien > y + 0.12 && py - Promien < y + 0.88)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
