using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    public enum TypWroga { Slugus, Lotr, Wojownik, Mag }

    public enum StanWroga { Stoi, Idzie, Zamach, Odpoczynek, Ogluszony, Martwy }

    public enum AkcjaBohatera { Brak, Cios, Ciezki, Unik, Zachwianie, Wir, Szarza, Okrzyk }

    /// <summary>Szkielet w świecie. Zamach trwa widocznie długo: to czas gracza na unik albo parowanie.</summary>
    public sealed class Wrog
    {
        public TypWroga Typ;
        public double X, Y, Hp, MaxHp;
        public StanWroga Stan = StanWroga.Idzie;
        public double Timer;
        public double KierX = 1, KierY;
        public bool Nocny;

        /// <summary>Rośnie z każdym nowym zamachem: po tym warstwa rysująca poznaje, że ma zacząć animację od nowa.</summary>
        public int Zamachy;

        public double CelX, CelY;
        public (int X, int Y)? CelBudowla;
        public PostacOsadnika CelOsadnik;

        /// <summary>Mur, który zastąpił drogę: szkielet burzy go, zanim pójdzie dalej.</summary>
        public (int X, int Y)? Mur;
        public double DoCelu;
    }

    public sealed class Pocisk
    {
        public double X, Y, VX, VY, Zycie, Obrazenia;
    }

    /// <summary>Plecak zostawiony po śmierci; leży na ziemi, aż gracz po niego wróci.</summary>
    public sealed class PorzuconyPlecak
    {
        public double X, Y;
        public Dictionary<string, int> Zawartosc;
    }

    public struct ZdarzenieWalki
    {
        public double X, Y;
        public string Rodzaj;
    }

    /// <summary>
    /// Walka w czasie rzeczywistym. W dzień jak w Souls: wytrzymałość na każdy ruch, unik z chwilą nietykalności,
    /// blok, parowanie w ostatniej chwili i riposta; wrogów jest mało, ale każdy groźny. W nocy idą fale na obóz
    /// i wtedy budzą się ciosy specjalne na czas odnowienia. Śmierć: pobudka w obozie, plecak zostaje na ziemi.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const double MaxWytrzymalosc = 100.0;
        public const double MinWytrzymalosc = 6.0;      // poniżej tego bohater musi złapać oddech
        public const double OknoParowania = 0.2;
        public const double CzasRiposty = 1.3;
        public const double NocOd = 21.0, NocDo = 5.0;
        public const int LimitWrogow = 14;
        public const int MaxAtakujacych = 2;

        /// <summary>Co tyle godzin gry przychodzi nocą kolejna fala.</summary>
        public const double OdstepFal = 3.0;

        /// <summary>Z tylu kafli nocny szkielet woli bohatera od budowli: kto stoi przy obozie, ten go broni.</summary>
        public const double ZasiegUwagi = 16.0;

        public bool WalkaWlaczona;
        public double Wytrzymalosc = MaxWytrzymalosc;
        public AkcjaBohatera Akcja;
        public int LicznikAkcji;
        public bool Blokuje;
        public double Riposta;
        public int Zabici;

        /// <summary>Ile razy bohater padł w tej sesji: statystyka do symulacji balansu, nie trafia do zapisu.</summary>
        public int Zgony;

        public readonly List<Wrog> Wrogowie = new List<Wrog>();
        public readonly List<Pocisk> Pociski = new List<Pocisk>();
        public readonly List<PorzuconyPlecak> Porzucone = new List<PorzuconyPlecak>();
        public readonly List<ZdarzenieWalki> Zdarzenia = new List<ZdarzenieWalki>();
        public readonly Dictionary<string, double> Odnowienie = new Dictionary<string, double>();

        /// <summary>Ile życia ma jeszcze budowla, którą wrogowie zaczęli niszczyć.</summary>
        public readonly Dictionary<(int X, int Y), double> ZycieBudowli = new Dictionary<(int X, int Y), double>();

        private double _czasAkcji, _dlugoscAkcji;
        private bool _ciosWykonany;
        private double _zaWytchnienie, _czasBloku = 99.0, _nietykalny;
        private double _unikX, _unikY;
        private double _nastepnaFala = -1.0, _nastepnyWloczega = 4.0;
        private bool _bylaNoc;
        private Losowanie _rngWalki;
        private readonly HashSet<Wrog> _trafieni = new HashSet<Wrog>();

        public static readonly (string Id, string Nazwa, string Klawisz, double Czas)[] Specjalne =
        {
            ("wir", "Wir", "Q", 8.0), ("szarza", "Szarża", "C", 10.0), ("okrzyk", "Okrzyk", "V", 16.0),
        };

        public bool CzyNoc => Godzina >= NocOd || Godzina < NocDo;

        public double PostepAkcji => _dlugoscAkcji <= 0 ? 0 : 1.0 - _czasAkcji / _dlugoscAkcji;

        public double DlugoscAkcji => _dlugoscAkcji;

        public bool Nietykalny => _nietykalny > 0;

        public bool WrogBlisko(double zasieg)
        {
            return Wrogowie.Any(w => w.Stan != StanWroga.Martwy && Dystans(w.X, w.Y, X, Y) <= zasieg);
        }

        private static double Dystans(double ax, double ay, double bx, double by)
        {
            return Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        }

        public static (double Hp, double Obrazenia, double Predkosc, double Zamach, double Odpoczynek, double Zasieg, int Koszt) Cechy(TypWroga t)
        {
            switch (t)
            {
                case TypWroga.Lotr: return (42, 13, 3.0, 0.5, 0.9, 1.15, 2);
                case TypWroga.Wojownik: return (120, 28, 1.9, 1.05, 1.3, 1.45, 4);
                case TypWroga.Mag: return (36, 14, 2.1, 0.95, 1.8, 7.0, 2);
                default: return (28, 8, 2.3, 0.75, 1.1, 1.1, 1);
            }
        }

        // ------------------------------------------------------------------ //
        //  Krok walki                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Jedna klatka walki. Zwraca mnożnik zwykłego ruchu bohatera; 0 = ruchem rządzi akcja.</summary>
        private double KrokWalki(double dt, Wejscie w)
        {
            _rngWalki = _rngWalki ?? new Losowanie((Seed ^ 0x2f6e2b1L) & 0x7FFFFFFFL);
            Zdarzenia.Clear();
            _nietykalny = Math.Max(0, _nietykalny - dt);
            Riposta = Math.Max(0, Riposta - dt);
            foreach (string k in Odnowienie.Keys.ToList())
            {
                Odnowienie[k] = Math.Max(0, Odnowienie[k] - dt);
            }

            Zagrozenia();
            double mnoznik = Spi ? 0.0 : RuchyBohatera(dt, w);
            KrokObrony(dt);
            KrokWrogow(dt);
            KrokPociskow(dt);
            Porzucone.RemoveAll(p =>
            {
                if (!w.Dzialaj || Dystans(p.X, p.Y, X, Y) > Zasieg)
                {
                    return false;
                }
                foreach (KeyValuePair<string, int> z in p.Zawartosc)
                {
                    Dodaj(z.Key, z.Value);
                }
                Pisz("Odzyskujesz swój plecak.");
                return true;
            });
            return mnoznik;
        }

        private double RuchyBohatera(double dt, Wejscie w)
        {
            _zaWytchnienie = Math.Max(0, _zaWytchnienie - dt);
            Blokuje = false;
            _czasBloku = w.Blok ? _czasBloku + dt : 0.0;

            if (Akcja != AkcjaBohatera.Brak)
            {
                return TrwaAkcja(dt);
            }

            if (w.CelWalki.HasValue && (w.Cios || w.Ciezki || w.Blok))
            {
                double kx = w.CelWalki.Value.X - X, ky = w.CelWalki.Value.Y - Y, d = Math.Sqrt(kx * kx + ky * ky);
                if (d > 0.05)
                {
                    Kierunek = (kx / d, ky / d);
                }
            }
            double rusza = Math.Sqrt(w.Dx * w.Dx + w.Dy * w.Dy);
            if (w.Unik && Wytrzymalosc >= MinWytrzymalosc)
            {
                (_unikX, _unikY) = rusza > 1e-9 ? (w.Dx / rusza, w.Dy / rusza) : (Kierunek.X, Kierunek.Y);
                Kierunek = (_unikX, _unikY);
                Zuzyj(22);
                Zacznij(AkcjaBohatera.Unik, 0.38);
                _nietykalny = Math.Max(_nietykalny, 0.28);
                return 0.0;
            }
            if (w.Ciezki && Wytrzymalosc >= MinWytrzymalosc)
            {
                Zuzyj(32);
                Zacznij(AkcjaBohatera.Ciezki, 0.9);
                return 0.15;
            }
            if (w.Cios && Wytrzymalosc >= MinWytrzymalosc)
            {
                Zuzyj(14);
                Zacznij(AkcjaBohatera.Cios, 0.46);
                return 0.3;
            }
            if (w.Blok && Wytrzymalosc > 0)
            {
                Blokuje = true;
                Wytrzymalosc = Math.Min(MaxWytrzymalosc, Wytrzymalosc + dt * 6.0);
                return 0.45;
            }
            if (_zaWytchnienie <= 0)
            {
                Wytrzymalosc = Math.Min(MaxWytrzymalosc, Wytrzymalosc + dt * 34.0);
            }
            return 1.0;
        }

        private void Zuzyj(double ile)
        {
            Wytrzymalosc = Math.Max(0, Wytrzymalosc - ile);
            _zaWytchnienie = 0.55;
            _zbior = null;
        }

        private void Zacznij(AkcjaBohatera a, double czas)
        {
            Akcja = a;
            _czasAkcji = _dlugoscAkcji = czas;
            _ciosWykonany = false;
            _trafieni.Clear();
            LicznikAkcji++;
        }

        private double TrwaAkcja(double dt)
        {
            _czasAkcji -= dt;
            double p = PostepAkcji;
            double mnoznik = 0.0;
            switch (Akcja)
            {
                case AkcjaBohatera.Unik:
                    Ruch(_unikX * 8.0 * dt, _unikY * 8.0 * dt);
                    break;
                case AkcjaBohatera.Cios:
                    mnoznik = 0.3;
                    Trafienie(p, 0.38, 17, 1.6, 0.5, false);
                    break;
                case AkcjaBohatera.Ciezki:
                    mnoznik = 0.15;
                    Trafienie(p, 0.56, 42, 1.85, 0.35, true);
                    break;
                case AkcjaBohatera.Wir:
                    Trafienie(p, 0.4, 32, 2.5, -1.1, true);
                    break;
                case AkcjaBohatera.Okrzyk:
                    if (p >= 0.35 && !_ciosWykonany)
                    {
                        _ciosWykonany = true;
                        foreach (Wrog g in Wrogowie.Where(g => g.Stan != StanWroga.Martwy && Dystans(g.X, g.Y, X, Y) <= 4.2).ToList())
                        {
                            ZadajWrogowi(g, 6, false);
                            Oglusz(g, 2.2);
                        }
                        Zdarzenia.Add(new ZdarzenieWalki { X = X, Y = Y, Rodzaj = "okrzyk" });
                    }
                    break;
                case AkcjaBohatera.Szarza:
                    Ruch(Kierunek.X * 16.0 * dt, Kierunek.Y * 16.0 * dt);
                    foreach (Wrog g in Wrogowie.Where(g => g.Stan != StanWroga.Martwy && Dystans(g.X, g.Y, X, Y) <= 1.2).ToList())
                    {
                        if (_trafieni.Add(g))
                        {
                            ZadajWrogowi(g, 36, true);
                        }
                    }
                    break;
            }
            if (_czasAkcji <= 0)
            {
                Akcja = AkcjaBohatera.Brak;
            }
            return mnoznik;
        }

        /// <summary>Cios wychodzi raz, w tej chwili zamachu. Półcień łuku: 0,5 = ±60°, wartość ujemna = dookoła.</summary>
        private void Trafienie(double postep, double chwila, double obrazenia, double zasieg, double luk, bool ciezki)
        {
            if (_ciosWykonany || postep < chwila)
            {
                return;
            }
            _ciosWykonany = true;
            foreach (Wrog g in Wrogowie.Where(g => g.Stan != StanWroga.Martwy).ToList())
            {
                double dx = g.X - X, dy = g.Y - Y, d = Math.Sqrt(dx * dx + dy * dy);
                if (d > zasieg)
                {
                    continue;
                }
                double kier = d < 1e-6 ? 1.0 : (dx * Kierunek.X + dy * Kierunek.Y) / d;
                if (luk >= -1.0 && kier < luk)
                {
                    continue;
                }
                bool riposta = Riposta > 0 || g.Stan == StanWroga.Ogluszony;
                double sila = Narzedzia.Contains("miecz") ? obrazenia * 1.35 : obrazenia;
                ZadajWrogowi(g, riposta ? sila * 2.6 : sila, ciezki || riposta);
                if (riposta)
                {
                    Riposta = 0;
                    Zdarzenia.Add(new ZdarzenieWalki { X = g.X, Y = g.Y, Rodzaj = "riposta" });
                }
            }
        }

        private void ZadajWrogowi(Wrog g, double obrazenia, bool ciezki)
        {
            g.Hp -= obrazenia;
            Zdarzenia.Add(new ZdarzenieWalki { X = g.X, Y = g.Y, Rodzaj = "trafienie" });
            if (g.Hp <= 0)
            {
                g.Stan = StanWroga.Martwy;
                g.Timer = 2.2;
                Zabici++;
                return;
            }
            // Wojownika przerywa dopiero mocny cios; słabszych każdy.
            if (ciezki || g.Typ != TypWroga.Wojownik)
            {
                Oglusz(g, ciezki ? 0.7 : 0.3);
            }
            if (g.Stan == StanWroga.Stoi)
            {
                g.Stan = StanWroga.Idzie;
            }
        }

        private static void Oglusz(Wrog g, double czas)
        {
            if (g.Stan != StanWroga.Martwy)
            {
                g.Stan = StanWroga.Ogluszony;
                g.Timer = Math.Max(g.Stan == StanWroga.Ogluszony ? g.Timer : 0, czas);
            }
        }

        /// <summary>Cios specjalny z kafelka: tylko nocą, na czas odnowienia, bez kosztu wytrzymałości.</summary>
        private void UzyjSpecjala(string id)
        {
            if (!WalkaWlaczona)
            {
                return;
            }
            if (!CzyNoc)
            {
                Pisz("Ciosy specjalne budzą się dopiero nocą.");
                return;
            }
            if (Akcja != AkcjaBohatera.Brak)
            {
                return;
            }
            foreach ((string i, string _, string _, double czas) in Specjalne)
            {
                if (i != id || (Odnowienie.TryGetValue(id, out double zostalo) && zostalo > 0))
                {
                    continue;
                }
                Odnowienie[id] = czas;
                _zbior = null;
                if (id == "wir")
                {
                    Zacznij(AkcjaBohatera.Wir, 0.65);
                }
                else if (id == "szarza")
                {
                    Zacznij(AkcjaBohatera.Szarza, 0.3);
                    _nietykalny = Math.Max(_nietykalny, 0.35);
                }
                else
                {
                    Zacznij(AkcjaBohatera.Okrzyk, 0.6);
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Bohater obrywa                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>Atak na bohatera z punktu (zx, zy). Zwraca true, gdy parował: to karze napastnika.</summary>
        private bool ZadajBohaterowi(double obrazenia, double zx, double zy)
        {
            if (_nietykalny > 0)
            {
                return false;
            }
            if (Narzedzia.Contains("zbroja"))
            {
                obrazenia *= 0.75;
            }
            if (Spi)
            {
                Spi = false;
                Pisz("Budzi cię cios!");
            }
            double dx = zx - X, dy = zy - Y, d = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy));
            bool zPrzodu = (dx * Kierunek.X + dy * Kierunek.Y) / d > 0.25;
            if (Blokuje && zPrzodu)
            {
                if (_czasBloku <= OknoParowania)
                {
                    Riposta = CzasRiposty;
                    Zdarzenia.Add(new ZdarzenieWalki { X = X, Y = Y, Rodzaj = "parowanie" });
                    return true;
                }
                Wytrzymalosc -= obrazenia * 1.6;
                _zaWytchnienie = 0.7;
                Zdarzenia.Add(new ZdarzenieWalki { X = X, Y = Y, Rodzaj = "blok" });
                if (Wytrzymalosc > 0)
                {
                    Hp -= obrazenia * 0.15;
                    SprawdzSmierc();
                    return false;
                }
                Wytrzymalosc = 0;
                Zacznij(AkcjaBohatera.Zachwianie, 0.9);
                Pisz("Gardę przebito!");
                obrazenia *= 0.5;
            }
            else if (obrazenia >= 12 && Akcja != AkcjaBohatera.Zachwianie)
            {
                Zacznij(AkcjaBohatera.Zachwianie, 0.35);
            }
            Hp -= obrazenia;
            Zdarzenia.Add(new ZdarzenieWalki { X = X, Y = Y, Rodzaj = "rana" });
            SprawdzSmierc();
            return false;
        }

        private void SprawdzSmierc()
        {
            if (Hp <= 0 && !Martwy)
            {
                Smierc();
            }
        }

        /// <summary>Pobudka w obozie (albo przy śpiworze, albo na polanie). Plecak zostaje na miejscu śmierci.</summary>
        private void Smierc()
        {
            Zgony++;
            if (Plecak.Any(p => p.Value > 0))
            {
                Porzucone.Add(new PorzuconyPlecak { X = X, Y = Y, Zawartosc = Plecak.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value) });
                Plecak.Clear();
            }
            (int X, int Y)? dom = SrodekObozu();
            if (dom == null && Budowle.ContainsValue("spiwor"))
            {
                dom = Budowle.First(b => b.Value == "spiwor").Key;
            }
            (X, Y) = dom.HasValue ? WolnyPunktObok(dom.Value.X, dom.Value.Y) : (0.5, 0.5);
            Hp = 70.0;
            Glod = Math.Max(Glod, 45.0);
            Energia = Math.Max(Energia, 45.0);
            Wytrzymalosc = MaxWytrzymalosc;
            Spi = false;
            Akcja = AkcjaBohatera.Brak;
            _nietykalny = 2.5;
            // Bez tego wrogowie stojący przy miejscu pobudki zabijaliby bohatera w kółko.
            foreach (Wrog g in Wrogowie.Where(g => Dystans(g.X, g.Y, X, Y) <= 7.0))
            {
                Oglusz(g, 3.5);
            }
            _zbior = null;
            Pisz(Porzucone.Count > 0 ? "Padasz. Budzisz się obolały, a plecak został tam, gdzie zginąłeś." : "Padasz. Budzisz się obolały.");
        }

        // ------------------------------------------------------------------ //
        //  Skąd biorą się wrogowie                                            //
        // ------------------------------------------------------------------ //

        private void Zagrozenia()
        {
            // W mieście nie ma nocnych fal: najazdy zapowiadane dojdą w etapie obrony miasta.
            bool noc = CzyNoc && !TrybMiasta;
            if (noc && !_bylaNoc)
            {
                _nastepnaFala = _czasGry;
            }
            if (!noc && _bylaNoc)
            {
                Swit();
            }
            _bylaNoc = noc;

            if (noc && _czasGry >= _nastepnaFala)
            {
                _nastepnaFala = _czasGry + OdstepFal;
                Fala();
            }
            if (!noc && !TrybMiasta && Godzina >= 9 && _czasGry >= _nastepnyWloczega)
            {
                _nastepnyWloczega = _czasGry + 5.0;
                Wloczega();
            }
            Wrogowie.RemoveAll(g => !g.Nocny && Dystans(g.X, g.Y, X, Y) > 40.0);
            if (Gosc != null && Godzina >= 20.0)
            {
                Pisz($"{Gosc.O.Imie} nie doczekał się decyzji i odchodzi.");
                Gosc = null;
                if (GoscOtwarty)
                {
                    GoscOtwarty = false;
                    PanelOtwarty = false;
                }
            }
        }

        /// <summary>
        /// Zagrożenie zależy od wielkości osady, a nie od kalendarza: mała osada ma mało wrogów, a rozrost
        /// kosztuje większe fale. Upływ dni decyduje tylko o tym, jakie typy szkieletów już chodzą.
        /// </summary>
        public int BudzetOsady => Obozowisko.Osadnicy.Count + Budowle.Count / 3 + Szczebel * 2;

        public static List<TypWroga> SkladFali(int dzien, Losowanie rng, int premia = 0)
        {
            var sklad = new List<TypWroga>();
            int budzet = 3 + premia;
            while (budzet > 0)
            {
                double los = rng.Losowa();
                TypWroga t = dzien >= 4 && los < 0.14 ? TypWroga.Wojownik
                           : dzien >= 2 && los < 0.32 ? TypWroga.Mag
                           : dzien >= 2 && los < 0.55 ? TypWroga.Lotr
                           : TypWroga.Slugus;
                sklad.Add(t);
                budzet -= Cechy(t).Koszt;
            }
            return sklad;
        }

        private void Fala()
        {
            int zywi = Wrogowie.Count(g => g.Stan != StanWroga.Martwy);
            (int X, int Y)? oboz = SrodekObozu();
            double cx = oboz.HasValue ? oboz.Value.X + 0.5 : X, cy = oboz.HasValue ? oboz.Value.Y + 0.5 : Y;
            int nowych = 0;
            foreach (TypWroga t in SkladFali(Dzien, _rngWalki, BudzetOsady))
            {
                if (zywi + nowych >= LimitWrogow)
                {
                    break;
                }
                if (Dodaj(t, cx, cy, 15.0, true))
                {
                    nowych++;
                }
            }
            if (nowych > 0)
            {
                Pisz($"Z mroku wychodzą szkielety ({nowych})!");
                if (Spi)
                {
                    Spi = false;
                    Pisz("Budzi cię hałas.");
                }
            }
        }

        /// <summary>W dzień mało wrogów, ale groźnych: pojedyncze szkielety krążą z dala od bohatera.</summary>
        private void Wloczega()
        {
            if (Wrogowie.Count(g => !g.Nocny && g.Stan != StanWroga.Martwy) >= 2)
            {
                return;
            }
            TypWroga t = Dzien <= 1 ? TypWroga.Lotr : _rngWalki.Losowa() < 0.6 ? TypWroga.Wojownik : TypWroga.Lotr;
            Dodaj(t, X, Y, 17.0, false);
        }

        private bool Dodaj(TypWroga t, double cx, double cy, double promien, bool nocny)
        {
            for (int proba = 0; proba < 16; proba++)
            {
                double kat = _rngWalki.Losowa() * Math.PI * 2;
                double r = promien + _rngWalki.Losowa() * 4.0;
                double x = cx + Math.Cos(kat) * r, y = cy + Math.Sin(kat) * r;
                if (!GeneratorSwiata.WGranicach((int)Math.Floor(x), (int)Math.Floor(y)) || !Wolne(x, y))
                {
                    continue;
                }
                double hp = Cechy(t).Hp;
                Wrogowie.Add(new Wrog { Typ = t, X = x, Y = y, Hp = hp, MaxHp = hp, Nocny = nocny, Stan = nocny ? StanWroga.Idzie : StanWroga.Stoi });
                return true;
            }
            return false;
        }

        /// <summary>O świcie nocne szkielety się rozsypują, a obóz dostaje czas na łatanie.</summary>
        private void Swit()
        {
            int ile = 0;
            foreach (Wrog g in Wrogowie.Where(g => g.Nocny && g.Stan != StanWroga.Martwy))
            {
                g.Stan = StanWroga.Martwy;
                g.Timer = 2.2;
                ile++;
            }
            Pociski.Clear();
            ZycieBudowli.Clear();
            PotrzebnyZapis = true;
            NowyPrzybysz();
            foreach (PostacOsadnika p in Postacie)
            {
                p.Zycie = p.Uzbrojony ? ZycieStraznika : ZycieOsadnika;
            }
            Pisz(ile > 0 ? $"Świt. Ostatnie szkielety ({ile}) rozsypują się w proch." : "Świt. Przetrwałeś noc.");
        }

        // ------------------------------------------------------------------ //
        //  Zachowanie wrogów                                                  //
        // ------------------------------------------------------------------ //

        private void KrokWrogow(double dt)
        {
            for (int i = Wrogowie.Count - 1; i >= 0; i--)
            {
                Wrog g = Wrogowie[i];
                if (g.Stan == StanWroga.Martwy)
                {
                    g.Timer -= dt;
                    if (g.Timer <= 0)
                    {
                        Wrogowie.RemoveAt(i);
                    }
                    continue;
                }
                KrokWroga(g, dt);
            }
            for (int a = 0; a < Wrogowie.Count; a++)
            {
                for (int b = a + 1; b < Wrogowie.Count; b++)
                {
                    Rozsun(Wrogowie[a], Wrogowie[b]);
                }
            }
        }

        private void Rozsun(Wrog a, Wrog b)
        {
            if (a.Stan == StanWroga.Martwy || b.Stan == StanWroga.Martwy)
            {
                return;
            }
            double dx = b.X - a.X, dy = b.Y - a.Y, d = Math.Sqrt(dx * dx + dy * dy);
            if (d >= 0.7 || d < 1e-6)
            {
                return;
            }
            double p = (0.7 - d) * 0.5;
            Przesun(a, -dx / d * p, -dy / d * p);
            Przesun(b, dx / d * p, dy / d * p);
        }

        private void Przesun(Wrog g, double dx, double dy)
        {
            if (WolneDlaWroga(g.X + dx, g.Y))
            {
                g.X += dx;
            }
            if (WolneDlaWroga(g.X, g.Y + dy))
            {
                g.Y += dy;
            }
        }

        private void WybierzCelWroga(Wrog g)
        {
            g.CelBudowla = null;
            g.CelOsadnik = null;
            if (g.Mur.HasValue)
            {
                if (Budowle.ContainsKey(g.Mur.Value))
                {
                    g.CelBudowla = g.Mur;
                    g.CelX = g.Mur.Value.X + 0.5;
                    g.CelY = g.Mur.Value.Y + 0.5;
                    return;
                }
                g.Mur = null;
            }
            double doBohatera = Dystans(g.X, g.Y, X, Y);
            g.CelX = X;
            g.CelY = Y;
            // Bohater blisko albo brak obozu: idzie na bohatera. Inaczej na najbliższego śpiącego lub budowlę.
            if (doBohatera < ZasiegUwagi || !g.Nocny || !SrodekObozu().HasValue)
            {
                return;
            }
            double naj = double.MaxValue;
            foreach (PostacOsadnika p in Postacie)
            {
                double d = Dystans(g.X, g.Y, p.X, p.Y);
                if (d < naj)
                {
                    naj = d;
                    g.CelOsadnik = p;
                    g.CelX = p.X;
                    g.CelY = p.Y;
                }
            }
            foreach (KeyValuePair<(int X, int Y), string> b in Budowle)
            {
                double d = Dystans(g.X, g.Y, b.Key.X + 0.5, b.Key.Y + 0.5);
                if (d < naj)
                {
                    naj = d;
                    g.CelOsadnik = null;
                    g.CelBudowla = b.Key;
                    g.CelX = b.Key.X + 0.5;
                    g.CelY = b.Key.Y + 0.5;
                }
            }
        }

        private void KrokWroga(Wrog g, double dt)
        {
            var c = Cechy(g.Typ);
            switch (g.Stan)
            {
                case StanWroga.Stoi:
                    if (Dystans(g.X, g.Y, X, Y) < 9.0)
                    {
                        g.Stan = StanWroga.Idzie;
                    }
                    return;
                case StanWroga.Ogluszony:
                case StanWroga.Odpoczynek:
                    g.Timer -= dt;
                    if (g.Timer <= 0)
                    {
                        g.Stan = StanWroga.Idzie;
                    }
                    return;
                case StanWroga.Zamach:
                    g.Timer -= dt;
                    if (g.Timer <= 0)
                    {
                        Uderz(g, c.Obrazenia, c.Zasieg);
                        if (g.Stan == StanWroga.Zamach)
                        {
                            g.Stan = StanWroga.Odpoczynek;
                            g.Timer = c.Odpoczynek;
                        }
                    }
                    return;
            }

            g.DoCelu -= dt;
            if (g.DoCelu <= 0)
            {
                g.DoCelu = 0.5;
                WybierzCelWroga(g);
            }
            if (g.CelOsadnik != null)
            {
                g.CelX = g.CelOsadnik.X;
                g.CelY = g.CelOsadnik.Y;
            }
            else if (g.CelBudowla == null)
            {
                g.CelX = X;
                g.CelY = Y;
            }
            double dx = g.CelX - g.X, dy = g.CelY - g.Y, d = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy));
            g.KierX = dx / d;
            g.KierY = dy / d;
            if (d <= c.Zasieg)
            {
                // Najwyżej dwóch naraz: tłum ma osaczać, ale każdy cios da się jeszcze zobaczyć i odpowiedzieć.
                bool naBohatera = g.CelBudowla == null && g.CelOsadnik == null;
                if (naBohatera && Wrogowie.Count(o => o != g && o.Stan == StanWroga.Zamach && o.CelBudowla == null && o.CelOsadnik == null) >= MaxAtakujacych)
                {
                    g.Stan = StanWroga.Odpoczynek;
                    g.Timer = 0.35;
                    return;
                }
                g.Stan = StanWroga.Zamach;
                g.Timer = c.Zamach;
                g.Zamachy++;
                return;
            }
            double krok = c.Predkosc * dt;
            double x0 = g.X, y0 = g.Y;
            Przesun(g, g.KierX * krok, g.KierY * krok);
            if (Math.Abs(g.X - x0) + Math.Abs(g.Y - y0) < krok * 0.25)
            {
                // Zablokowany przez mur: burzy go. Przez cokolwiek innego: obchodzi bokiem.
                (int X, int Y) przed = ((int)Math.Floor(g.X + g.KierX * 0.75), (int)Math.Floor(g.Y + g.KierY * 0.75));
                if (Budowle.TryGetValue(przed, out string stoi) && Mur(stoi))
                {
                    g.Mur = przed;
                    g.DoCelu = 0;
                    return;
                }
                Przesun(g, -g.KierY * krok, g.KierX * krok);
            }
        }

        private void Uderz(Wrog g, double obrazenia, double zasieg)
        {
            if (g.CelBudowla.HasValue)
            {
                UderzBudowle(g.CelBudowla.Value, obrazenia);
                return;
            }
            if (g.Typ == TypWroga.Mag)
            {
                double dx = X - g.X, dy = Y - g.Y, d = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy));
                if (d <= zasieg + 2.0)
                {
                    Pociski.Add(new Pocisk { X = g.X, Y = g.Y, VX = dx / d * 6.0, VY = dy / d * 6.0, Zycie = 2.0, Obrazenia = obrazenia });
                }
                return;
            }
            if (g.CelOsadnik != null)
            {
                if (Dystans(g.X, g.Y, g.CelOsadnik.X, g.CelOsadnik.Y) <= zasieg + 0.4)
                {
                    UderzOsadnika(g.CelOsadnik, obrazenia);
                }
                return;
            }
            // Bohater może odskoczyć w czasie zamachu: cios sięga tylko tam, gdzie jeszcze stoi.
            if (Dystans(g.X, g.Y, X, Y) <= zasieg + 0.25 && ZadajBohaterowi(obrazenia, g.X, g.Y))
            {
                Oglusz(g, 1.6);
            }
        }

        private void UderzBudowle((int X, int Y) k, double obrazenia)
        {
            if (!Budowle.TryGetValue(k, out string klucz))
            {
                return;
            }
            double zycie = ZycieBudowli.TryGetValue(k, out double z) ? z : ZyciePoczatkowe(klucz);
            zycie -= obrazenia;
            Zdarzenia.Add(new ZdarzenieWalki { X = k.X + 0.5, Y = k.Y + 0.5, Rodzaj = "budowla" });
            if (zycie > 0)
            {
                ZycieBudowli[k] = zycie;
                return;
            }
            Budowle.Remove(k);
            ZycieBudowli.Remove(k);
            if (Place.Remove(k))
            {
                PrzeliczMiejsca();
                Pisz($"Szkielety zniszczyły plac budowy: {NazwaBudowli(klucz.Substring(PrzedrostekPlacu.Length))}. Materiały przepadły.");
                return;
            }
            PrzeliczMiejsca();
            Pisz(klucz == "chata" ? "Szkielety zburzyły chatę!" : klucz == "namiot" ? "Szkielety podarły namiot!"
               : klucz == "ognisko" ? "Szkielety rozrzuciły ognisko!" : klucz == "palisada" ? "Szkielety przebiły palisadę!"
               : klucz == "brama" ? "Szkielety wyłamały bramę!" : klucz == "wieza" ? "Szkielety zwaliły wieżę!"
               : $"Szkielety zniszczyły: {NazwaBudowli(klucz)}!");
        }

        public static double ZyciePoczatkowe(string klucz)
        {
            switch (klucz)
            {
                case "wieza": return 160;
                case "warsztat":
                case "tartak":
                case "kuznia":
                case "spichlerz":
                case "tawerna": return 130;
                case "lecznica": return 110;
                case "targ": return 90;
                case "farma": return 60;
                case "chata": return 130;
                case "palisada": return 90;
                case "brama": return 70;
                case "namiot": return 60;
                case "ognisko": return 40;
                default: return klucz.StartsWith(PrzedrostekPlacu) ? 50 : 15;
            }
        }

        private void UderzOsadnika(PostacOsadnika p, double obrazenia)
        {
            p.Zycie -= obrazenia;
            Zdarzenia.Add(new ZdarzenieWalki { X = p.X, Y = p.Y, Rodzaj = "trafienie" });
            if (p.Zycie > 0)
            {
                return;
            }
            Obozowisko.Osadnicy.Remove(p.O);
            Pisz($"{p.O.Imie} ginie z rąk szkieletów.");
            foreach (Wrog g in Wrogowie.Where(g => g.CelOsadnik == p))
            {
                g.CelOsadnik = null;
                g.DoCelu = 0;
            }
            SynchronizujPostacie();
        }

        private void KrokPociskow(double dt)
        {
            for (int i = Pociski.Count - 1; i >= 0; i--)
            {
                Pocisk p = Pociski[i];
                p.X += p.VX * dt;
                p.Y += p.VY * dt;
                p.Zycie -= dt;
                bool trafil = Dystans(p.X, p.Y, X, Y) < 0.5 && !Nietykalny;
                if (trafil)
                {
                    ZadajBohaterowi(p.Obrazenia, p.X - p.VX * 0.1, p.Y - p.VY * 0.1);
                }
                if (trafil || p.Zycie <= 0 || Zajety((int)Math.Floor(p.X), (int)Math.Floor(p.Y)))
                {
                    Pociski.RemoveAt(i);
                }
            }
        }
    }
}
