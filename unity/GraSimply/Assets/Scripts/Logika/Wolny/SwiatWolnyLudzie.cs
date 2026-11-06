using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Co osadnik robi w danej części doby.</summary>
    public enum BlokPlanu { Praca, Odpoczynek, Wolne, Straz }

    /// <summary>Wędrowiec czekający przy ognisku na decyzję gracza. Sekretu gracz nie widzi, tylko niepewną wskazówkę.</summary>
    public sealed class Przybysz
    {
        public Osadnik O;
        public double X, Y;
        public string Surowiec;
        public int Ile;
        public string Wskazowka;
        public string Sekret;
    }

    /// <summary>
    /// Ludzie w trybie przetrwania. Każdy ma plan doby z czterech części, który decyduje, ile pracuje, kiedy śpi
    /// i czy czuwa nocą. Nowi nie przychodzą sami: przybysz czeka przy ognisku na decyzję, a pod spodem może
    /// kryć się choroba, zdrada albo sabotaż, o których mówią tylko niepewne wskazówki.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const string SekretChoroba = "choroba";
        public const string SekretZdrada = "zdrada";
        public const string SekretSabotaz = "sabotaz";

        public static readonly string[] NazwyBlokow = { "Rano", "Dzień", "Wieczór", "Noc" };
        public static readonly string[] GodzinyBlokow = { "5–11", "11–17", "17–21", "21–5" };

        private static readonly Dictionary<string, string> Wskazowki = new Dictionary<string, string>
        {
            { SekretChoroba, "Kaszle i jest blady." },
            { SekretZdrada, "Dopytuje, gdzie trzymacie zapasy." },
            { SekretSabotaz, "Ma poparzone ręce i unika wzroku." },
        };

        public Przybysz Gosc;
        public bool GoscOtwarty;

        private int _dobreDecyzje;

        public static BlokPlanu[] DomyslnyPlan(string zajecie)
        {
            return zajecie == "straznik"
                ? new[] { BlokPlanu.Odpoczynek, BlokPlanu.Praca, BlokPlanu.Straz, BlokPlanu.Straz }
                : new[] { BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Wolne, BlokPlanu.Odpoczynek };
        }

        public static int NumerBloku(double godzina)
        {
            return godzina >= 5 && godzina < 11 ? 0 : godzina >= 11 && godzina < 17 ? 1 : godzina >= 17 && godzina < 21 ? 2 : 3;
        }

        public BlokPlanu BlokTeraz(PostacOsadnika p)
        {
            return p.Plan[NumerBloku(Godzina)];
        }

        /// <summary>Renoma osady 0–10: zadowoleni ludzie, ich liczba, przeżyte dni i trafne decyzje.</summary>
        public double Renoma
        {
            get
            {
                List<Osadnik> l = Obozowisko.Osadnicy;
                double morale = l.Count == 0 ? 50.0 : l.Average(o => o.Morale);
                double r = morale / 25.0 + l.Count * 0.5 + Dzien * 0.15 + _dobreDecyzje * 0.5;
                return Math.Max(0.0, Math.Min(10.0, r));
            }
        }

        // ------------------------------------------------------------------ //
        //  Kafelki panelu                                                     //
        // ------------------------------------------------------------------ //

        private void KliknijLudzie(string id)
        {
            List<Osadnik> ludzie = Obozowisko.Osadnicy;
            PostacOsadnika wybrany = Wybrany >= 0 && Wybrany < ludzie.Count ? Postacie.FirstOrDefault(p => p.O == ludzie[Wybrany]) : null;
            if (id == "g:otworz")
            {
                if (Gosc != null)
                {
                    GoscOtwarty = !GoscOtwarty;
                    PanelOtwarty = GoscOtwarty;
                    TrybBudowy = false;
                    WybranaBudowla = null;
                }
            }
            else if (id == "g:tak")
            {
                Przyjmij();
            }
            else if (id == "g:nie")
            {
                Odpraw();
            }
            else if (id == "x:wygnaj")
            {
                Wygnaj(wybrany);
            }
            else if (id == "h:wszyscy" && wybrany != null)
            {
                foreach (PostacOsadnika p in Postacie)
                {
                    p.Plan = (BlokPlanu[])wybrany.Plan.Clone();
                }
                Pisz("Wszyscy dostają ten sam plan doby.");
            }
            else if (id.StartsWith("h:") && wybrany != null && int.TryParse(id.Substring(2), out int blok) && blok >= 0 && blok < 4)
            {
                wybrany.Plan[blok] = (BlokPlanu)(((int)wybrany.Plan[blok] + 1) % 4);
            }
        }

        // ------------------------------------------------------------------ //
        //  Przybysze                                                          //
        // ------------------------------------------------------------------ //

        private void NowyPrzybysz()
        {
            if (Gosc != null || !SrodekObozu().HasValue)
            {
                return;
            }
            bool pierwszy = !_bylPrzybysz;
            if (!pierwszy && _rng.Losowa() >= 0.35 + Renoma * 0.03 + Szczebel * 0.1)
            {
                return;
            }
            if (Osada.WolneChaty(Obozowisko) <= 0)
            {
                Pisz("Przy ognisku stanął wędrowiec, ale nie masz dla niego miejsca do spania i poszedł dalej.");
                return;
            }
            _bylPrzybysz = true;
            Gosc = WylosujPrzybysza();
            (int X, int Y) c = SrodekObozu().Value;
            (Gosc.X, Gosc.Y) = WolnyPunktObok(c.X, c.Y);
            Pisz($"Przy ognisku czeka {Gosc.O.Imie}. Zdecyduj przed zmierzchem: kafelek Przybysz albo G.");
        }

        /// <summary>
        /// Lepsza renoma to więcej chętnych i wybór lepszego. Im lepszy kandydat, tym większa szansa na sekret,
        /// a wskazówki mylą w obie strony: uczciwy też bywa blady, a zdrajca wygląda zwyczajnie.
        /// </summary>
        private Przybysz WylosujPrzybysza()
        {
            int chetnych = 1 + (int)(Renoma / 3.0);
            var zajete = new HashSet<string>(Obozowisko.Osadnicy.Select(o => o.Imie));
            Osadnik najlepszy = null;
            double wynik = -1.0;
            for (int i = 0; i < chetnych; i++)
            {
                Osadnik o = Osada.NowyKandydat(_rng, zajete);
                double suma = (o.Kompetencje ?? new Dictionary<string, Umiejetnosc>()).Values.Sum(u => u.P + u.Pasja * 2.0);
                if (suma > wynik)
                {
                    wynik = suma;
                    najlepszy = o;
                }
            }
            double szansa = Math.Max(0.1, 0.24 + (chetnych - 1) * 0.04 - _dobreDecyzje * 0.01);
            string sekret = null;
            if (_rng.Losowa() < szansa)
            {
                double los = _rng.Losowa();
                sekret = los < 0.4 ? SekretChoroba : los < 0.7 ? SekretSabotaz : SekretZdrada;
            }
            string wskazowka = "Wygląda zwyczajnie.";
            if (sekret != null && _rng.Losowa() < 0.65)
            {
                wskazowka = Wskazowki[sekret];
            }
            else if (sekret == null && _rng.Losowa() < 0.2)
            {
                wskazowka = Wskazowki.Values.ElementAt(_rng.Zakres(0, Wskazowki.Count));
            }
            bool jedzenie = _rng.Losowa() < 0.6;
            return new Przybysz
            {
                O = najlepszy,
                Surowiec = jedzenie ? "jagody" : "drewno",
                Ile = _rng.Calkowita(jedzenie ? 2 : 3, jedzenie ? 6 : 8),
                Wskazowka = wskazowka,
                Sekret = sekret,
            };
        }

        private void Przyjmij()
        {
            if (Gosc == null)
            {
                return;
            }
            if (Osada.WolneChaty(Obozowisko) <= 0)
            {
                Pisz("Nie ma gdzie go położyć: postaw namiot albo chatę.");
                return;
            }
            if (Razem(Gosc.Surowiec) < Gosc.Ile)
            {
                Pisz($"Nie stać cię: prosi o {Gosc.Ile} {Nazwa(Gosc.Surowiec)}.");
                return;
            }
            Zabierz(Gosc.Surowiec, Gosc.Ile);
            Obozowisko.Osadnicy.Add(Gosc.O);
            var p = new PostacOsadnika { O = Gosc.O, X = Gosc.X, Y = Gosc.Y, CelX = Gosc.X, CelY = Gosc.Y, Sekret = Gosc.Sekret };
            Postacie.Add(p);
            Wybrany = Obozowisko.Osadnicy.Count - 1;
            Pisz($"{Gosc.O.Imie} zostaje w osadzie.");
            SprawdzSzczebel();
            Gosc = null;
            GoscOtwarty = false;
            PanelOtwarty = true;
        }

        private void Odpraw()
        {
            if (Gosc == null)
            {
                return;
            }
            Pisz($"{Gosc.O.Imie} rusza dalej.");
            if (Gosc.Sekret != null)
            {
                _dobreDecyzje++;
            }
            Gosc = null;
            GoscOtwarty = false;
            PanelOtwarty = false;
        }

        /// <summary>Wypędzenie niczego nie zdradza: gra nie mówi, czy miałeś rację. Niewinny kosztuje zaufanie.</summary>
        private void Wygnaj(PostacOsadnika p)
        {
            if (p == null)
            {
                return;
            }
            Obozowisko.Osadnicy.Remove(p.O);
            Pisz($"Wypędzasz: {p.O.Imie}. Ludzie patrzą w milczeniu.");
            foreach (Osadnik o in Obozowisko.Osadnicy)
            {
                o.Morale = Math.Max(0.0, o.Morale - (p.Sekret == null ? 7.0 : 2.0));
            }
            if (p.Sekret == null)
            {
                _dobreDecyzje = Math.Max(0, _dobreDecyzje - 1);
            }
            SynchronizujPostacie();
        }

        // ------------------------------------------------------------------ //
        //  Doba: praca, zmęczenie i sekrety                                   //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// <see cref="Osada.DzienOsady"/> liczy pełny dzień każdego. Plan rozstrzyga, kto dziś faktycznie stanął
        /// do pracy: dwie części doby to komplet, mniej to szansa, a na czas liczenia reszta jest „bez zajęcia”.
        /// </summary>
        private List<(Osadnik O, string Zajecie)> WylaczNieobecnych()
        {
            var zdjeci = new List<(Osadnik O, string Zajecie)>();
            foreach (PostacOsadnika p in Postacie)
            {
                double udzial = p.Plan.Count(b => b == BlokPlanu.Praca) / 2.0;
                if (p.Zmeczenie >= 80.0)
                {
                    udzial *= 0.5;
                }
                // Rzemieślnik pracuje w zakładzie na mapie: dniówka z gry fabularnej go nie dotyczy.
                if (p.O.Zajecie == "rzemieslnik" || (p.O.Zajecie != "bezczynny" && p.O.Zajecie != "straznik" && _rng.Losowa() >= udzial))
                {
                    zdjeci.Add((p.O, p.O.Zajecie));
                    p.O.Zajecie = "bezczynny";
                }
            }
            return zdjeci;
        }

        /// <summary>Dodatkowa część doby przy pracy daje ekstra plon, ale płaci się za nią zmęczeniem.</summary>
        private void DobaLudzi()
        {
            foreach (PostacOsadnika p in Postacie.ToList())
            {
                int praca = p.Plan.Count(b => b == BlokPlanu.Praca);
                int straz = p.Plan.Count(b => b == BlokPlanu.Straz);
                int odpoczynek = p.Plan.Count(b => b == BlokPlanu.Odpoczynek);
                int wolne = p.Plan.Count(b => b == BlokPlanu.Wolne);
                if (praca >= 3 && ObiektPracy(p.O.Zajecie) != Obiekt.Brak)
                {
                    string plon = p.O.Zajecie == "drwal" ? "drewno" : p.O.Zajecie == "kamieniarz" ? "kamien"
                                : p.O.Zajecie == "zielarz" ? "ziola" : p.O.Zajecie == "mysliwy" ? "zywnosc" : "ruda";
                    Obozowisko.Surowce[plon] = Obozowisko.Surowce.Wez(plon) + praca - 2;
                }
                p.Zmeczenie = Math.Max(0.0, Math.Min(100.0, p.Zmeczenie + praca * 15.0 + straz * 20.0 - odpoczynek * 34.0 - wolne * 8.0));
                p.O.Morale = Math.Max(0.0, Math.Min(100.0, p.O.Morale + wolne * 1.5 - (p.Zmeczenie >= 70.0 ? 5.0 : 0.0)));
                if (p.Zmeczenie >= 90.0 && p.O.Chory <= 0 && _rng.Losowa() < 0.3)
                {
                    p.O.Chory = 2;
                    Pisz($"{p.O.Imie} pada z przepracowania.");
                }
                p.DniWOsadzie++;
                KrokSekretu(p);
            }
        }

        private void KrokSekretu(PostacOsadnika p)
        {
            if (p.Sekret == SekretChoroba && p.DniWOsadzie >= 2 && _rng.Losowa() < 0.45)
            {
                p.O.Chory = 3;
                Pisz($"{p.O.Imie} przyniósł chorobę: ma gorączkę.");
                foreach (Osadnik inny in Obozowisko.Osadnicy.Where(o => o != p.O && o.Chory <= 0))
                {
                    if (_rng.Losowa() < 0.3)
                    {
                        inny.Chory = 2;
                        Pisz($"{inny.Imie} też się zaraził.");
                    }
                }
                p.Sekret = null;
            }
            else if (p.Sekret == SekretSabotaz && p.DniWOsadzie >= 3 && _rng.Losowa() < 0.4)
            {
                Sabotaz(p);
            }
            else if (p.Sekret == SekretZdrada && p.DniWOsadzie >= 5 && _rng.Losowa() < 0.3)
            {
                Zdrada(p);
            }
        }

        /// <summary>Sabotaż nie ma podpisu: ślad wskazuje sprawcę, ale czasem też kogoś niewinnego.</summary>
        private void Sabotaz(PostacOsadnika p)
        {
            List<(int X, int Y)> mury = Budowle.Where(b => b.Value == "palisada" || b.Value == "brama").Select(b => b.Key).ToList();
            if (mury.Count > 0 && _rng.Losowa() < 0.5)
            {
                Budowle.Remove(mury[_rng.Zakres(0, mury.Count)]);
                Pisz("Rano ktoś znalazł wyłamany fragment muru. To nie były szkielety.");
            }
            else
            {
                string[] zapasy = Obozowisko.Surowce.Where(r => r.Value > 0).Select(r => r.Key).ToArray();
                if (zapasy.Length == 0)
                {
                    return;
                }
                string co = zapasy[_rng.Zakres(0, zapasy.Length)];
                Obozowisko.Surowce[co] = (int)(Obozowisko.Surowce[co] * 0.6);
                Pisz("W nocy spłonęła część zapasów. Ktoś tu pomógł ogniu.");
            }
            if (_rng.Losowa() < 0.6)
            {
                p.Podejrzany = true;
            }
            List<PostacOsadnika> inni = Postacie.Where(q => q != p).ToList();
            if (inni.Count > 0 && _rng.Losowa() < 0.35)
            {
                inni[_rng.Zakres(0, inni.Count)].Podejrzany = true;
            }
        }

        /// <summary>Zdrajca znika z łupem i zostawia dziurę w murze tuż przed nocą.</summary>
        private void Zdrada(PostacOsadnika p)
        {
            foreach (string s in new[] { "drewno", "kamien", "zywnosc" })
            {
                Obozowisko.Surowce[s] = (int)(Obozowisko.Surowce.Wez(s) * 0.65);
            }
            List<(int X, int Y)> mury = Budowle.Where(b => b.Value == "brama" || b.Value == "palisada").Select(b => b.Key).ToList();
            if (mury.Count > 0)
            {
                Budowle.Remove(mury[_rng.Zakres(0, mury.Count)]);
            }
            Obozowisko.Osadnicy.Remove(p.O);
            foreach (Osadnik o in Obozowisko.Osadnicy)
            {
                o.Morale = Math.Max(0.0, o.Morale - 8.0);
            }
            Pisz($"Zdrada! {p.O.Imie} uciekł z zapasami i zostawił dziurę w obronie.");
            _dobreDecyzje = Math.Max(0, _dobreDecyzje - 1);
            SynchronizujPostacie();
        }
    }
}
