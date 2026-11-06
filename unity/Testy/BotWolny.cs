using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Jak dobrze bot gra. To model, a nie człowiek: liczby porównują wersje balansu między sobą.</summary>
    public sealed class ProfilBota
    {
        public string Nazwa;

        /// <summary>Jak często reaguje na widoczny zamach wroga.</summary>
        public double Reakcja;

        /// <summary>Czy reaguje parowaniem, czy tylko unikiem.</summary>
        public bool Parowanie;

        /// <summary>Czy używa ciosów specjalnych.</summary>
        public bool Specjalne;

        /// <summary>Ile sekund traci na namysł po każdej zmianie celu.</summary>
        public double Zwloka;

        public static readonly ProfilBota Sprawny = new ProfilBota { Nazwa = "sprawny", Reakcja = 0.75, Parowanie = true, Specjalne = true, Zwloka = 0.5 };
        public static readonly ProfilBota Przecietny = new ProfilBota { Nazwa = "przeciętny", Reakcja = 0.4, Specjalne = true, Zwloka = 4.0 };
        public static readonly ProfilBota Poczatkujacy = new ProfilBota { Nazwa = "początkujący", Reakcja = 0.1, Zwloka = 12.0 };
        public static readonly ProfilBota[] Wszystkie = { Sprawny, Przecietny, Poczatkujacy };
    }

    /// <summary>
    /// Bot gra wyłącznie przez <see cref="Wejscie"/>, tak jak gracz, i słucha <see cref="CeleWolne"/>:
    /// jeśli podpowiedzi prowadzą w ślepą uliczkę, bot też w niej utknie i symulacja to pokaże.
    /// </summary>
    public sealed class BotWolny
    {
        private readonly SwiatWolny _s;
        private readonly ProfilBota _p;
        private readonly Random _los;
        private readonly Queue<Wejscie> _kolejka = new Queue<Wejscie>();
        private readonly Dictionary<Wrog, (int Zamach, bool Reaguje)> _reakcje = new Dictionary<Wrog, (int, bool)>();
        private readonly Dictionary<(int X, int Y), double> _zakazane = new Dictionary<(int X, int Y), double>();

        private double _czas, _zwloka;
        private string _ostatniCel;
        private (int X, int Y)? _cel;
        private List<(int X, int Y)> _droga = new List<(int X, int Y)>();
        private double _zaPlan, _kontrola, _kontrolaX, _kontrolaY, _kontrolaZbioru;
        private int _kontrolaPlecaka;

        public BotWolny(SwiatWolny swiat, ProfilBota profil, int seed)
        {
            _s = swiat;
            _p = profil;
            _los = new Random(seed);
        }

        /// <summary>Polecenia na jedną klatkę.</summary>
        public Wejscie Krok(double dt)
        {
            _czas += dt;
            _zaPlan -= dt;
            Wejscie? walka = Walka();
            if (walka.HasValue)
            {
                _kolejka.Clear();
                return walka.Value;
            }
            if (_kolejka.Count > 0)
            {
                return _kolejka.Dequeue();
            }
            if (_s.Spi)
            {
                return new Wejscie();
            }
            if (_s.PanelOtwarty)
            {
                return ObsluzPanel();
            }
            if (_s.TrybBudowy)
            {
                return new Wejscie { Klik = "a:buduj" };
            }

            CelWolny cel = CeleWolne.Aktualny(_s);
            string id = cel?.Id ?? "koniec";
            if (id != _ostatniCel)
            {
                _ostatniCel = id;
                _zwloka = _p.Zwloka;
                _cel = null;
            }
            if (_zwloka > 0)
            {
                _zwloka -= dt;
                return new Wejscie();
            }

            // Jak człowiek: nie czeka z jedzeniem i snem, aż gra krzyknie.
            if (_s.Glod < 50.0 && _s.Razem("jagody") > 0)
            {
                return new Wejscie { Klik = "a:jedz" };
            }
            if (_s.Energia < 35.0 && id != "spij" && !_s.WrogBlisko(12.0) && _s.Budowle.ContainsValue("namiot"))
            {
                return Spij();
            }
            return Dzialaj(id);
        }

        // ------------------------------------------------------------------ //
        //  Cele                                                               //
        // ------------------------------------------------------------------ //

        private Wejscie Dzialaj(string id)
        {
            switch (id)
            {
                case "plecak":
                    PorzuconyPlecak p = _s.Porzucone[0];
                    return Odleglosc(p.X, p.Y) <= 1.0 ? new Wejscie { Dzialaj = true } : IdzDo((int)Math.Floor(p.X), (int)Math.Floor(p.Y), 0.8);
                case "jedz":
                    return new Wejscie { Klik = "a:jedz" };
                case "jagody":
                    return Zbieraj("jagody");
                case "spij":
                    return Spij();
                case "zbierz-ognisko":
                    return Zbieraj(_s.Razem("drewno") < CeleWolne.NaOgnisko ? "drewno" : "kamien");
                case "ognisko":
                    return Postaw("ognisko");
                case "zbierz-namiot":
                    return Zbieraj("drewno");
                case "plac-namiot":
                    return Postaw("namiot");
                case "szkielet":
                case "plotno":
                    return PracujNaPlacu(_s.Place.Values.First(q => q.Klucz == "namiot"));
                case "wlokno":
                    return Zbieraj("wlokno");
                case "noc":
                    return Noc();
                case "przybysz":
                    return Przybysz();
                case "zajecie":
                    return new Wejscie { Klik = "a:osada" };
                case "palisada":
                    return Buduj("palisada");
                case "tartak":
                    return Buduj("tartak");
                default:
                    return _s.Razem("drewno") < 120 ? Zbieraj("drewno") : DoObozu();
            }
        }

        private Wejscie Noc()
        {
            if (!_s.CzyNoc && _s.Godzina < 20.0)
            {
                return Zbieraj("drewno");
            }
            if (!_s.CzyNoc || !_s.PrzyObozie())
            {
                return DoObozu();
            }
            return _s.Energia < 60.0 ? Spij() : new Wejscie();
        }

        private Wejscie Przybysz()
        {
            Przybysz g = _s.Gosc;
            if (_s.Razem(g.Surowiec) < g.Ile)
            {
                return Zbieraj(g.Surowiec == "drewno" || g.Surowiec == "kamien" || g.Surowiec == "jagody" || g.Surowiec == "wlokno" ? g.Surowiec : "drewno");
            }
            _kolejka.Enqueue(new Wejscie { Klik = "g:tak" });
            return new Wejscie { Klik = "g:otworz" };
        }

        private Wejscie ObsluzPanel()
        {
            if (_s.GoscOtwarty && _s.Gosc != null)
            {
                return new Wejscie { Klik = "g:tak" };
            }
            for (int i = 0; i < _s.Obozowisko.Osadnicy.Count; i++)
            {
                if (_s.Obozowisko.Osadnicy[i].Zajecie == "bezczynny")
                {
                    _kolejka.Enqueue(new Wejscie { Klik = "z:drwal" });
                    return new Wejscie { Klik = "o:" + i };
                }
            }
            return new Wejscie { Klik = "a:osada" };
        }

        private Wejscie Buduj(string klucz)
        {
            PlacBudowy plac = _s.Place.Values.FirstOrDefault(q => q.Klucz == klucz);
            return plac != null ? PracujNaPlacu(plac) : Postaw(klucz);
        }

        private Wejscie PracujNaPlacu(PlacBudowy plac)
        {
            EtapBudowy etap = SwiatWolny.Etapy[plac.Klucz][plac.Etap];
            if (!plac.Zaplacony)
            {
                foreach (KeyValuePair<string, int> m in etap.Materialy)
                {
                    if (_s.Razem(m.Key) < m.Value)
                    {
                        return Zbieraj(m.Key);
                    }
                }
            }
            return Odleglosc(plac.X + 0.5, plac.Y + 0.5) <= 1.9 ? new Wejscie { Dzialaj = true } : IdzDo(plac.X, plac.Y, 1.6);
        }

        private Wejscie Postaw(string klucz)
        {
            (int X, int Y)? oboz = _s.SrodekObozu();
            if (oboz.HasValue && Odleglosc(oboz.Value.X + 0.5, oboz.Value.Y + 0.5) > 4.0)
            {
                return IdzDo(oboz.Value.X, oboz.Value.Y, 2.5);
            }
            (int X, int Y)? miejsce = MiejsceBudowy(klucz);
            if (miejsce == null)
            {
                return IdzDo((int)Math.Floor(_s.X) + _los.Next(-4, 5), (int)Math.Floor(_s.Y) + _los.Next(-4, 5), 1.0);
            }
            _kolejka.Enqueue(new Wejscie { Klik = "b:" + klucz });
            _kolejka.Enqueue(new Wejscie { Poloz = true, Kursor = miejsce.Value });
            _kolejka.Enqueue(new Wejscie { Klik = "a:buduj" });
            return new Wejscie { Klik = "a:buduj" };
        }

        /// <summary>Stawia co drugi kafel, żeby bot nie zamurował się we własnym obozie.</summary>
        private (int X, int Y)? MiejsceBudowy(string klucz)
        {
            int cx = (int)Math.Floor(_s.X), cy = (int)Math.Floor(_s.Y);
            for (int r = 2; r <= 5; r++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    for (int y = cy - r; y <= cy + r; y++)
                    {
                        if (Math.Max(Math.Abs(x - cx), Math.Abs(y - cy)) == r && x % 2 == 0 && y % 2 == 0
                            && _s.PowodBudowy(klucz, x, y) == null)
                        {
                            return (x, y);
                        }
                    }
                }
            }
            return null;
        }

        private Wejscie DoObozu()
        {
            (int X, int Y)? oboz = _s.SrodekObozu();
            if (oboz == null || Odleglosc(oboz.Value.X + 0.5, oboz.Value.Y + 0.5) <= 3.0)
            {
                return new Wejscie();
            }
            return IdzDo(oboz.Value.X, oboz.Value.Y, 2.5);
        }

        private Wejscie Spij()
        {
            if (_s.MiejsceSnu() != null)
            {
                return _s.Energia < 90.0 && !_s.WrogBlisko(12.0) ? new Wejscie { Klik = "a:spij" } : new Wejscie();
            }
            foreach (KeyValuePair<(int X, int Y), string> b in _s.Budowle)
            {
                if (b.Value == "namiot" || b.Value == "spiwor")
                {
                    return IdzDo(b.Key.X, b.Key.Y, 2.0);
                }
            }
            if (_s.Ile("spiwor") > 0)
            {
                return Postaw("spiwor");
            }
            return new Wejscie();
        }

        // ------------------------------------------------------------------ //
        //  Zbieranie i chodzenie                                              //
        // ------------------------------------------------------------------ //

        private static bool Pasuje(string material, Obiekt o)
        {
            switch (material)
            {
                case "drewno": return o == Obiekt.Drzewo;
                case "kamien": return o == Obiekt.Skala;
                case "jagody": return o == Obiekt.Krzak;
                case "wlokno": return o == Obiekt.Krzak || o == Obiekt.Ziolo;
                default: return false;
            }
        }

        private Wejscie Zbieraj(string material)
        {
            if (_cel.HasValue && (!Pasuje(material, _s.ObiektNa(_cel.Value.X, _cel.Value.Y)) || Zakazany(_cel.Value)))
            {
                _cel = null;
            }
            if (_cel == null)
            {
                _cel = NajblizszyZasob(material);
                _kontrolaZbioru = _czas;
                _kontrolaPlecaka = SumaPlecaka();
                if (_cel == null)
                {
                    return new Wejscie();
                }
            }
            (int x, int y) = _cel.Value;
            bool przeszkoda = _s.Zajety(x, y);
            double dx = x + 0.5 - _s.X, dy = y + 0.5 - _s.Y, d = Math.Sqrt(dx * dx + dy * dy);
            if (d <= (przeszkoda ? 1.3 : 0.35))
            {
                if (SumaPlecaka() != _kontrolaPlecaka)
                {
                    _kontrolaPlecaka = SumaPlecaka();
                    _kontrolaZbioru = _czas;
                }
                else if (_czas - _kontrolaZbioru > 6.0)
                {
                    Zakaz(_cel.Value);
                    _cel = null;
                    return new Wejscie();
                }
                if (przeszkoda && (dx * _s.Kierunek.X + dy * _s.Kierunek.Y) / d < 0.7)
                {
                    return new Wejscie { Dx = dx, Dy = dy };
                }
                return new Wejscie { Dzialaj = true };
            }
            _kontrolaZbioru = _czas;
            return IdzDo(x, y, przeszkoda ? 1.1 : 0.0);
        }

        private int SumaPlecaka()
        {
            return _s.Plecak.Values.Sum();
        }

        private bool Zakazany((int X, int Y) k)
        {
            return _zakazane.TryGetValue(k, out double do_) && do_ > _czas;
        }

        private void Zakaz((int X, int Y) k)
        {
            _zakazane[k] = _czas + 90.0;
        }

        private double Odleglosc(double x, double y)
        {
            return Math.Sqrt((x - _s.X) * (x - _s.X) + (y - _s.Y) * (y - _s.Y));
        }

        /// <summary>Najbliższy po drodze obiekt danego rodzaju, do którego da się podejść.</summary>
        private (int X, int Y)? NajblizszyZasob(string material)
        {
            (int X, int Y) start = ((int)Math.Floor(_s.X), (int)Math.Floor(_s.Y));
            var odwiedzone = new HashSet<(int, int)> { start };
            var kolejka = new Queue<(int X, int Y)>();
            kolejka.Enqueue(start);
            while (kolejka.Count > 0)
            {
                (int X, int Y) k = kolejka.Dequeue();
                foreach ((int nx, int ny) in Sasiedzi(k))
                {
                    if (!GeneratorSwiata.WGranicach(nx, ny) || Math.Abs(nx - start.X) > 60 || Math.Abs(ny - start.Y) > 60)
                    {
                        continue;
                    }
                    if (Pasuje(material, _s.ObiektNa(nx, ny)) && !Zakazany((nx, ny)))
                    {
                        return (nx, ny);
                    }
                    if (!_s.Zajety(nx, ny) && odwiedzone.Add((nx, ny)))
                    {
                        kolejka.Enqueue((nx, ny));
                    }
                }
            }
            return null;
        }

        private static IEnumerable<(int, int)> Sasiedzi((int X, int Y) k)
        {
            yield return (k.X + 1, k.Y);
            yield return (k.X - 1, k.Y);
            yield return (k.X, k.Y + 1);
            yield return (k.X, k.Y - 1);
        }

        /// <summary>Krok w stronę kafla: zatrzymuje się w odległości <paramref name="promien"/> od jego środka.</summary>
        private Wejscie IdzDo(int x, int y, double promien)
        {
            if (_zaPlan <= 0 || _droga.Count == 0 || _droga[_droga.Count - 1] != (x, y))
            {
                _droga = Droga(x, y);
                _droga.Add((x, y));
                _zaPlan = 1.5;
            }
            if (_czas - _kontrola > 1.5)
            {
                if (Odleglosc(_kontrolaX, _kontrolaY) < 0.3 && _cel.HasValue)
                {
                    Zakaz(_cel.Value);
                    _cel = null;
                }
                _kontrola = _czas;
                _kontrolaX = _s.X;
                _kontrolaY = _s.Y;
            }
            while (_droga.Count > 1 && Odleglosc(_droga[0].X + 0.5, _droga[0].Y + 0.5) < 0.25)
            {
                _droga.RemoveAt(0);
            }
            if (Odleglosc(x + 0.5, y + 0.5) <= promien)
            {
                return new Wejscie();
            }
            return new Wejscie { Dx = _droga[0].X + 0.5 - _s.X, Dy = _droga[0].Y + 0.5 - _s.Y };
        }

        private List<(int X, int Y)> Droga(int cx, int cy)
        {
            (int X, int Y) start = ((int)Math.Floor(_s.X), (int)Math.Floor(_s.Y));
            var skad = new Dictionary<(int X, int Y), (int X, int Y)> { [start] = start };
            var kolejka = new Queue<(int X, int Y)>();
            kolejka.Enqueue(start);
            (int X, int Y)? koniec = null;
            double naj = double.MaxValue;
            while (kolejka.Count > 0 && skad.Count < 9000)
            {
                (int X, int Y) k = kolejka.Dequeue();
                double d = Math.Abs(k.X - cx) + Math.Abs(k.Y - cy);
                if (d < naj)
                {
                    naj = d;
                    koniec = k;
                    if (d == 0)
                    {
                        break;
                    }
                }
                foreach ((int nx, int ny) in Sasiedzi(k))
                {
                    if (GeneratorSwiata.WGranicach(nx, ny) && !skad.ContainsKey((nx, ny)) && !_s.Zajety(nx, ny))
                    {
                        skad[(nx, ny)] = k;
                        kolejka.Enqueue((nx, ny));
                    }
                }
            }
            var droga = new List<(int X, int Y)>();
            (int X, int Y) t = koniec ?? start;
            while (t != start)
            {
                droga.Add(t);
                t = skad[t];
            }
            droga.Reverse();
            return droga;
        }

        // ------------------------------------------------------------------ //
        //  Walka                                                              //
        // ------------------------------------------------------------------ //

        private Wejscie? Walka()
        {
            double zasieg = _s.CzyNoc && _s.SrodekObozu().HasValue ? 14.0 : 7.0;
            Wrog naj = null;
            double dNaj = double.MaxValue;
            int blisko = 0, wokol = 0;
            foreach (Wrog g in _s.Wrogowie)
            {
                if (g.Stan == StanWroga.Martwy)
                {
                    continue;
                }
                double d = Odleglosc(g.X, g.Y);
                blisko += d <= 2.5 ? 1 : 0;
                wokol += d <= 4.2 ? 1 : 0;
                if (d < dNaj)
                {
                    dNaj = d;
                    naj = g;
                }
            }
            if (naj == null || dNaj > zasieg)
            {
                return null;
            }
            if (_s.Akcja != AkcjaBohatera.Brak)
            {
                return new Wejscie();
            }

            foreach (Wrog g in _s.Wrogowie)
            {
                if (g.Stan != StanWroga.Zamach || g.Typ == TypWroga.Mag || g.CelBudowla != null || g.CelOsadnik != null)
                {
                    continue;
                }
                var c = SwiatWolny.Cechy(g.Typ);
                double d = Odleglosc(g.X, g.Y);
                if (d > c.Zasieg + 0.5)
                {
                    continue;
                }
                if (!_reakcje.TryGetValue(g, out (int Zamach, bool Reaguje) r) || r.Zamach != g.Zamachy)
                {
                    r = (g.Zamachy, _los.NextDouble() < _p.Reakcja);
                    _reakcje[g] = r;
                }
                if (!r.Reaguje)
                {
                    continue;
                }
                if (g.Timer > 0.14)
                {
                    // Widzi zamach: nie zaczyna ciosu, którego nie zdąży skończyć, bo wtedy nie uchyli się na czas.
                    if (g.Timer < 0.6)
                    {
                        return new Wejscie();
                    }
                    continue;
                }
                if (_p.Parowanie)
                {
                    return new Wejscie { Blok = true, CelWalki = (g.X, g.Y) };
                }
                if (_s.Wytrzymalosc >= 22.0)
                {
                    return new Wejscie { Unik = true, Dx = _s.X - g.X, Dy = _s.Y - g.Y };
                }
            }

            if (_p.Specjalne && _s.CzyNoc)
            {
                if (wokol >= 3 && Gotowy("okrzyk"))
                {
                    return new Wejscie { Klik = "s:okrzyk" };
                }
                if (blisko >= 2 && Gotowy("wir"))
                {
                    return new Wejscie { Klik = "s:wir" };
                }
            }
            if (dNaj <= 1.5)
            {
                return _s.Wytrzymalosc >= 14.0
                    ? new Wejscie { Cios = true, CelWalki = (naj.X, naj.Y) }
                    : new Wejscie { Dx = _s.X - naj.X, Dy = _s.Y - naj.Y };
            }
            return new Wejscie { Dx = naj.X - _s.X, Dy = naj.Y - _s.Y };
        }

        private bool Gotowy(string id)
        {
            return !_s.Odnowienie.TryGetValue(id, out double zostalo) || zostalo <= 0;
        }
    }

    /// <summary>Co się wydarzyło w jednej rozgrywce bota.</summary>
    public sealed class WynikRozgrywki
    {
        public readonly Dictionary<string, (double Sekunda, double Godzina)> Cele = new Dictionary<string, (double, double)>();
        public readonly Dictionary<int, int> ZgonyNocy = new Dictionary<int, int>();
        public readonly Dictionary<int, bool> ObozOSwicie = new Dictionary<int, bool>();
        public readonly Dictionary<int, double> ZycieOSwicie = new Dictionary<int, double>();

        /// <summary>Kiedy (godzina gry) po raz pierwszy padła budowla danego rodzaju.</summary>
        public readonly Dictionary<string, double> PierwszaStrata = new Dictionary<string, double>();
        public double? NamiotGodzina, NamiotSekunda;
        public int ZgonyDnia, Zabici, Osadnicy;
        public string Koniec;
    }

    /// <summary>Puszcza boty na wielu światach i liczy to, czego nie widać z jednej rozgrywki.</summary>
    public static class SymulacjaWolna
    {
        public const double Krok = 0.05;

        public static WynikRozgrywki Rozegraj(long seed, ProfilBota profil, int dni, Action<SwiatWolny> przygotuj = null)
        {
            var s = new SwiatWolny(seed) { WalkaWlaczona = true, BudowaZEtapami = true };
            przygotuj?.Invoke(s);
            var bot = new BotWolny(s, profil, (int)(seed & 0x7FFFFFFF));
            var w = new WynikRozgrywki();
            int zgony = 0;
            bool bylaNoc = false;
            double t = 0;
            bool mialOgnisko = false, mialNamiot = false;
            while (s.Dzien <= dni)
            {
                s.Aktualizuj(Krok, bot.Krok(Krok));
                t += Krok;
                CelWolny cel = CeleWolne.Aktualny(s);
                if (cel != null && !w.Cele.ContainsKey(cel.Id))
                {
                    w.Cele[cel.Id] = (t, s.GodzinGry + 7.0);
                }
                if (w.NamiotSekunda == null && s.Budowle.ContainsValue("namiot"))
                {
                    w.NamiotSekunda = t;
                    w.NamiotGodzina = s.GodzinGry + 7.0;
                }
                bool ognisko = s.Budowle.ContainsValue("ognisko"), namiot = s.Budowle.ContainsValue("namiot");
                if (mialOgnisko && !ognisko && !w.PierwszaStrata.ContainsKey("ognisko"))
                {
                    w.PierwszaStrata["ognisko"] = s.GodzinGry + 7.0;
                }
                if (mialNamiot && !namiot && !w.PierwszaStrata.ContainsKey("namiot"))
                {
                    w.PierwszaStrata["namiot"] = s.GodzinGry + 7.0;
                }
                mialOgnisko = ognisko;
                mialNamiot = namiot;
                bool noc = s.CzyNoc;
                int ktora = s.Godzina >= SwiatWolny.NocOd ? s.Dzien : s.Dzien - 1;
                if (s.Zgony != zgony)
                {
                    zgony = s.Zgony;
                    if (noc)
                    {
                        w.ZgonyNocy[ktora] = w.ZgonyNocy.TryGetValue(ktora, out int z) ? z + 1 : 1;
                    }
                    else
                    {
                        w.ZgonyDnia++;
                    }
                }
                if (bylaNoc && !noc)
                {
                    w.ObozOSwicie[s.Dzien - 1] = s.Budowle.ContainsValue("namiot") && s.Budowle.ContainsValue("ognisko");
                    w.ZycieOSwicie[s.Dzien - 1] = s.Hp;
                }
                bylaNoc = noc;
            }
            w.Zabici = s.Zabici;
            w.Osadnicy = s.Obozowisko.Osadnicy.Count;
            w.Koniec = CeleWolne.Aktualny(s)?.Id ?? "koniec";
            return w;
        }

        private static string Zegar(double godzinGry)
        {
            int dzien = (int)(godzinGry / 24.0) + 1;
            double g = godzinGry % 24.0;
            return $"dzień {dzien}, {(int)g:00}:{(int)((g - (int)g) * 60):00}";
        }

        private static double Mediana(IEnumerable<double> liczby)
        {
            List<double> l = liczby.OrderBy(x => x).ToList();
            return l.Count == 0 ? double.NaN : l[l.Count / 2];
        }

        /// <summary>Sama walka na polanie, bez obozu: czy umiejętności bota robią różnicę i ile kosztuje dany wróg.</summary>
        public static (double ZyciePo, double Zgony, double Sekundy) Arena(ProfilBota profil, TypWroga typ, int ile, int prob)
        {
            double zycie = 0, zgony = 0, sekundy = 0;
            for (int p = 0; p < prob; p++)
            {
                var s = new SwiatWolny(5000 + p * 31L) { WalkaWlaczona = true, BudowaZEtapami = true, Godzina = 8.0 };
                var bot = new BotWolny(s, profil, p);
                for (int i = 0; i < ile; i++)
                {
                    double kat = i * 2 * Math.PI / ile;
                    double hp = SwiatWolny.Cechy(typ).Hp;
                    s.Wrogowie.Add(new Wrog { Typ = typ, X = s.X + Math.Cos(kat) * 5, Y = s.Y + Math.Sin(kat) * 5, Hp = hp, MaxHp = hp });
                }
                double t = 0;
                while (t < 120.0 && s.Zgony == 0 && s.Wrogowie.Any(g => g.Stan != StanWroga.Martwy))
                {
                    s.Aktualizuj(Krok, bot.Krok(Krok));
                    t += Krok;
                }
                zycie += s.Zgony > 0 ? 0 : s.Hp;
                zgony += s.Zgony > 0 ? 1 : 0;
                sekundy += t;
            }
            return (zycie / prob, zgony / prob, sekundy / prob);
        }

        /// <summary>Raport na konsolę: dotnet run -- --symulacja [światów] [dni].</summary>
        public static int Raport(int swiatow, int dni)
        {
            Console.WriteLine($"Symulacja: {swiatow} światów, {dni} dni. To boty, a nie ludzie: liczby służą do porównań.");
            foreach (ProfilBota profil in ProfilBota.Wszystkie)
            {
                var wyniki = new WynikRozgrywki[swiatow];
                System.Threading.Tasks.Parallel.For(0, swiatow, i => wyniki[i] = Rozegraj(1000 + i * 7919L, profil, dni));
                Console.WriteLine();
                Console.WriteLine($"=== Bot {profil.Nazwa} ===");
                foreach ((TypWroga typ, int ile) in new[] { (TypWroga.Slugus, 1), (TypWroga.Slugus, 5), (TypWroga.Lotr, 1), (TypWroga.Lotr, 3), (TypWroga.Wojownik, 1), (TypWroga.Mag, 2) })
                {
                    var a = Arena(profil, typ, ile, 40);
                    Console.WriteLine($"Arena {ile} × {typ}: życie po walce {a.ZyciePo:0}, porażki {a.Zgony * 100:0}%, {a.Sekundy:0} s");
                }
                List<WynikRozgrywki> zNamiotem = wyniki.Where(w => w.NamiotGodzina.HasValue).ToList();
                int przedNoca = zNamiotem.Count(w => w.NamiotGodzina < 21.0);
                Console.WriteLine($"Namiot przed pierwszą nocą: {przedNoca}/{swiatow}; w ogóle: {zNamiotem.Count}/{swiatow}");
                if (zNamiotem.Count > 0)
                {
                    Console.WriteLine($"Namiot, mediana: {Mediana(zNamiotem.Select(w => w.NamiotSekunda.Value)):0} s gry, {Zegar(Mediana(zNamiotem.Select(w => w.NamiotGodzina.Value)))}");
                }
                for (int noc = 1; noc < dni; noc++)
                {
                    int bezSmierci = wyniki.Count(w => !w.ZgonyNocy.ContainsKey(noc));
                    double zgony = wyniki.Average(w => w.ZgonyNocy.TryGetValue(noc, out int z) ? z : 0);
                    int oboz = wyniki.Count(w => w.ObozOSwicie.TryGetValue(noc, out bool b) && b);
                    double zycie = wyniki.Average(w => w.ZycieOSwicie.TryGetValue(noc, out double h) ? h : 0);
                    Console.WriteLine($"Noc {noc}: bez śmierci {bezSmierci}/{swiatow}, zgonów śr. {zgony:0.0}, obóz stoi o świcie {oboz}/{swiatow}, życie o świcie śr. {zycie:0}");
                }
                foreach (string b in new[] { "ognisko", "namiot" })
                {
                    List<WynikRozgrywki> stracili = wyniki.Where(w => w.PierwszaStrata.ContainsKey(b)).ToList();
                    if (stracili.Count > 0)
                    {
                        Console.WriteLine($"Pierwsza strata: {b} w {stracili.Count}/{swiatow}, mediana {Zegar(Mediana(stracili.Select(w => w.PierwszaStrata[b])))}");
                    }
                }
                Console.WriteLine($"Zgony za dnia, śr.: {wyniki.Average(w => w.ZgonyDnia):0.0}; zabitych śr.: {wyniki.Average(w => w.Zabici):0}; osadników na końcu śr.: {wyniki.Average(w => w.Osadnicy):0.0}");
                Console.WriteLine("Pierwsze pojawienie się celu (mediana):");
                foreach (string id in new[] { "zbierz-ognisko", "ognisko", "zbierz-namiot", "plac-namiot", "szkielet", "wlokno", "plotno", "noc", "przybysz", "zajecie", "palisada", "tartak", "plecak", "jagody", "jedz", "spij" })
                {
                    List<WynikRozgrywki> doszli = wyniki.Where(w => w.Cele.ContainsKey(id)).ToList();
                    if (doszli.Count > 0)
                    {
                        Console.WriteLine($"  {id,-15} {doszli.Count,3}/{swiatow}  {Mediana(doszli.Select(w => w.Cele[id].Sekunda)),5:0} s  {Zegar(Mediana(doszli.Select(w => w.Cele[id].Godzina)))}");
                    }
                }
                Console.WriteLine("Cel na koniec: " + string.Join(", ", wyniki.GroupBy(w => w.Koniec).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}")));
            }
            return 0;
        }
    }
}
