using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika.Mroczny;
using GraSimply.Logika;

namespace GraSimply.Grafika.Hybryda
{
    /// <summary>
    /// Scena osady z bliska w stylu hybrydowym: pixelartowe chaty i ludziki
    /// na ciągłym gruncie z rzutem i światłem stylu Mroczny.
    ///
    /// Osadnicy naprawdę chodzą (<see cref="SymulacjaRuchu"/>): rano idą do
    /// stanowisk, wieczorem siadają przy ogniu albo idą do tawerny, w nocy
    /// śpią przy chatach. Doba sceny trwa <see cref="RytmDnia.DlugoscDnia"/>
    /// sekund i jest osobna od dnia świata.
    ///
    /// Światło dnia nakładamy na całą scenę, a to, co świeci samo (ognisko,
    /// okna, kuźnia), jest rozjaśniane po tym — dlatego noc jest ciemna,
    /// a ogień jedyny jasny, choć wszystko narysowano jako pixelart.
    /// Na końcu klatka wpada do wspólnej palety stylu.
    /// </summary>
    public sealed class ScenaOsady
    {
        public const int Szer = 640;
        public const int Wys = 360;

        /// <summary>Scena zaczyna rano, nie w ciemności.</summary>
        public const double PrzesuniecieDnia = 0.2 * RytmDnia.DlugoscDnia;

        /// <summary>Tyle sekund ruchu nadrabiamy po przerwie w oglądaniu sceny.</summary>
        public const double Nadrobienie = 12.0;

        private readonly SpriteOsady _sprite = new SpriteOsady();
        private readonly SymulacjaRuchu _ruch = new SymulacjaRuchu(UkladPlacu.SiatkaRuchu);
        private readonly Dictionary<string, Plotno> _grunty = new Dictionary<string, Plotno>();
        private readonly List<PodpisOsadnika> _podpisy = new List<PodpisOsadnika>();
        private double? _ostatniT;

        public IReadOnlyList<PodpisOsadnika> Podpisy => _podpisy;

        /// <summary>Symulacja ruchu — do testów i podglądu.</summary>
        public SymulacjaRuchu Ruch => _ruch;

        /// <summary>Jasność sceny z ostatniej klatki: 0 = noc, 1 = południe.</summary>
        public double Jasnosc { get; private set; } = 1.0;

        // ------------------------------------------------------------------ //
        //  Klatka                                                             //
        // ------------------------------------------------------------------ //

        public void Rysuj(Plotno cel, StanOsady osada, double t, bool zmierzch)
        {
            Dictionary<(int X, int Y), ElementPlacu> plan = UkladPlacu.Rozklad(osada);
            int palisada = osada.Budynki.TryGetValue("palisada", out int pp) ? pp : 0;
            double czasDnia = t + PrzesuniecieDnia;

            Ruszaj(osada, plan, t, czasDnia);

            Plotno grunt = Grunt(osada.Pora, plan, palisada);
            if (grunt.Dane.Length == cel.Dane.Length)
            {
                Buffer.BlockCopy(grunt.Dane, 0, cel.Dane, 0, grunt.Dane.Length);
            }
            else
            {
                cel.Nalóż(grunt, 0, 0);
            }

            _podpisy.Clear();
            var swiatla = new List<Swiatlo>();
            var rzeczy = new List<(double Glebokosc, Action Rysuj)>();

            foreach (KeyValuePair<(int X, int Y), ElementPlacu> wpis in plan)
            {
                (int x, int y) = wpis.Key;
                ElementPlacu el = wpis.Value;
                if (el.Rodzaj == "farma")
                {
                    rzeczy.Add((-1.0, () => PoleUprawne(cel, x, y, el.Poziom)));
                    continue;
                }
                rzeczy.Add((x + y + 1.0, () => RysujElement(cel, x, y, el, swiatla)));
            }
            foreach ((int x, int y) in UkladPlacu.PolaPalisady(palisada))
            {
                int px = x, py = y;
                rzeczy.Add((px + py + 1.0 - 0.1, () => Slup(cel, px, py, palisada)));
            }
            foreach (OsadnikDoRysunku o in osada.Osadnicy)
            {
                if (!_ruch.Postacie.ContainsKey(o.Imie))
                {
                    continue;
                }
                (double x, double y) = _ruch.Pozycja(o.Imie);
                OsadnikDoRysunku osadnik = o;
                rzeczy.Add((x + y + 1.0 + 0.01, () => RysujOsadnika(cel, osadnik, t)));
            }
            rzeczy.Add((3.5 + 4.3 + 0.01, () => RysujBohatera(cel, osada.Klasa, swiatla)));
            rzeczy.Add((UkladPlacu.Ognisko.X + UkladPlacu.Ognisko.Y + 1.0, () => RysujOgnisko(cel, t, swiatla)));

            foreach ((double _, Action rysuj) in rzeczy.OrderBy(r => r.Glebokosc))
            {
                rysuj();
            }

            // Światło dnia, potem to, co świeci samo — i wspólna paleta.
            double jasnosc = zmierzch ? 0.12 : RytmDnia.Swiatlo(czasDnia);
            Jasnosc = jasnosc;
            (double R, double G, double B) otoczenie = Otoczenie(jasnosc);
            if (otoczenie.R < 0.999 || otoczenie.G < 0.999 || otoczenie.B < 0.999)
            {
                Oswietlenie.Zastosuj(cel, swiatla, t, otoczenie);
            }
            if (osada.Pora == "zima")
            {
                Snieg(cel, t);
            }
            Oswietlenie.Paleta().Kwantyzuj(cel);
        }

        /// <summary>
        /// Natężenie światła tam, gdzie nic nie świeci: od chłodnej nocy stylu
        /// Mroczny po biały dzień, a w przejściu odrobinę cieplejsze.
        /// </summary>
        public static (double R, double G, double B) Otoczenie(double jasnosc)
        {
            (double nr, double ng, double nb) = Oswietlenie.Noc;
            double r = nr + (1.0 - nr) * jasnosc;
            double g = ng + (1.0 - ng) * jasnosc;
            double b = nb + (1.0 - nb) * jasnosc;
            double zmierzch = 4.0 * jasnosc * (1.0 - jasnosc);   // najmocniej w połowie przejścia
            return (r, g * (1.0 - 0.14 * zmierzch), b * (1.0 - 0.28 * zmierzch));
        }

        // ------------------------------------------------------------------ //
        //  Ruch                                                               //
        // ------------------------------------------------------------------ //

        private void Ruszaj(StanOsady osada, Dictionary<(int X, int Y), ElementPlacu> plan,
                            double t, double czasDnia)
        {
            _ruch.Przebuduj(UkladPlacu.Zablokowane(plan));
            bool pierwsza = !_ostatniT.HasValue;

            var imiona = new HashSet<string>(osada.Osadnicy.Select(o => o.Imie));
            foreach (string imie in _ruch.Imiona.ToList())
            {
                if (!imiona.Contains(imie))
                {
                    _ruch.Usun(imie);
                }
            }

            string pora = RytmDnia.Faza(czasDnia);
            List<(int X, int Y)> domy = UkladPlacu.Domy(plan);
            (int X, int Y)? tawerna = null;
            foreach (KeyValuePair<(int X, int Y), ElementPlacu> wpis in plan)
            {
                if (wpis.Value.Klucz == "tawerna")
                {
                    tawerna = wpis.Key;
                    break;
                }
            }
            for (int i = 0; i < osada.Osadnicy.Count; i++)
            {
                OsadnikDoRysunku o = osada.Osadnicy[i];
                var dane = new Osadnik { Imie = o.Imie, Zajecie = o.Zajecie, Chory = o.Chory ? 1 : 0 };
                (( int X, int Y) cel, string czynnosc) = RytmDnia.Zadanie(
                    dane, UkladPlacu.Stanowisko(o.Zajecie, plan), domy[i % domy.Count],
                    UkladPlacu.Ognisko, tawerna, pora);
                if (!_ruch.Postacie.ContainsKey(o.Imie))
                {
                    // Przy pierwszym obrazie osada już żyje; później przybysz wchodzi bramą.
                    _ruch.Dodaj(o.Imie, pierwsza ? cel : UkladPlacu.Brama);
                }
                _ruch.UstawCel(o.Imie, cel, czynnosc);
            }

            // Scena stoi, gdy gracz siedzi w menu innego widoku. Po powrocie nie
            // zaczynamy od zera: nadrabiamy kilka sekund, żeby ludzie byli w drodze
            // albo na miejscu, a nie rozsypani po starych pozycjach.
            double uplynelo;
            if (pierwsza)
            {
                uplynelo = Nadrobienie;
            }
            else
            {
                uplynelo = Math.Max(0.0, t - _ostatniT.Value);
                // Zwykła klatka to ułamek sekundy (chwilowe zacięcie ucinamy do 0,1 s);
                // dłuższa przerwa znaczy „sceny nie było widać” — wtedy nadrabiamy.
                uplynelo = Math.Min(uplynelo, uplynelo > 0.5 ? Nadrobienie : 0.1);
            }
            _ostatniT = t;
            while (uplynelo > 1e-9)
            {
                double krok = Math.Min(0.1, uplynelo);
                _ruch.Aktualizuj(krok);
                uplynelo -= krok;
            }
        }

        // ------------------------------------------------------------------ //
        //  Grunt                                                              //
        // ------------------------------------------------------------------ //

        private Plotno Grunt(string pora, Dictionary<(int X, int Y), ElementPlacu> plan, int palisada)
        {
            // Ubita ziemia sięga tam, gdzie ktoś mieszka: w nowym obozie to placyk
            // przy ognisku, a pełny kwadrat po obwód palisady — gdy osada wyrosła.
            int x0 = UkladPlacu.Ognisko.X, x1 = x0, y0 = UkladPlacu.Ognisko.Y, y1 = y0;
            foreach ((int X, int Y) p in plan.Keys)
            {
                x0 = Math.Min(x0, p.X);
                x1 = Math.Max(x1, p.X);
                y0 = Math.Min(y0, p.Y);
                y1 = Math.Max(y1, p.Y);
            }
            (double X0, double Y0, double X1, double Y1) plac = palisada > 0
                ? (0.0, 0.0, (double)UkladPlacu.Siatka, (double)UkladPlacu.Siatka)
                : (x0 - 0.2, y0 - 0.2, x1 + 1.2, y1 + 1.2);
            string klucz = $"{pora}|{plac.X0}|{plac.Y0}|{plac.X1}|{plac.Y1}";
            if (!_grunty.TryGetValue(klucz, out Plotno g))
            {
                if (_grunty.Count > 12)
                {
                    _grunty.Clear();
                }
                g = GruntOsady.Zbuduj(Szer, Wys, pora, plac);
                _grunty[klucz] = g;
            }
            return g;
        }

        // ------------------------------------------------------------------ //
        //  Rysowanie rzeczy                                                   //
        // ------------------------------------------------------------------ //

        private void RysujElement(Plotno cel, int x, int y, ElementPlacu el, List<Swiatlo> swiatla)
        {
            (int sx, int sy) = RzutOsady.SrodekPola(x, y);
            Plotno spr;
            (int X, int Y) podstawa;
            if (el.Rodzaj == "namiot")
            {
                spr = _sprite.Namiot(out podstawa);
            }
            else if (el.Rodzaj == "chata")
            {
                spr = _sprite.Chata(el.Poziom, out podstawa);
            }
            else
            {
                spr = _sprite.Budynek(el.Klucz, el.Poziom, out podstawa);
            }

            Plotno cien = _sprite.Cien((int)(spr.Szerokosc * 1.05), (int)(spr.Szerokosc * 0.36));
            cel.Nalóż(cien, sx - cien.Szerokosc / 2 + (int)(spr.Wysokosc * 0.14), sy - cien.Wysokosc / 2 + 2);
            cel.Nalóż(spr, sx - podstawa.X, sy - podstawa.Y);

            string blask = null;
            if (el.Rodzaj == "chata")
            {
                blask = "chata";
            }
            else if (UkladPlacu.Blask.TryGetValue(el.Klucz, out string b))
            {
                blask = b;
            }
            if (blask != null)
            {
                DodajBlask(swiatla, blask, sx, sy - spr.Wysokosc / 2);
            }
        }

        /// <summary>Poświata budynku: kolor i zasięg zależą od tego, co w nim się dzieje.</summary>
        private static void DodajBlask(List<Swiatlo> swiatla, string rodzaj, double x, double y)
        {
            switch (rodzaj)
            {
                case "kuznia":
                    swiatla.Add(new Swiatlo { X = x, Y = y, Promien = 96, Kolor = (1.0, 0.55, 0.28), Migocze = true });
                    break;
                case "magia":
                    swiatla.Add(new Swiatlo { X = x, Y = y, Promien = 84, Kolor = (0.78, 0.56, 1.0), Migocze = true });
                    break;
                case "latarnia":
                    swiatla.Add(new Swiatlo { X = x, Y = y, Promien = 72, Kolor = (1.0, 0.86, 0.60) });
                    break;
                case "chata":
                    swiatla.Add(new Swiatlo { X = x, Y = y + 4, Promien = 46, Kolor = (0.95, 0.74, 0.46) });
                    break;
                default: // okno
                    swiatla.Add(new Swiatlo { X = x, Y = y, Promien = 70, Kolor = (1.0, 0.80, 0.52) });
                    break;
            }
        }

        /// <summary>Zagony zamiast budynku — pola leżą na ziemi, nie stoją na niej.</summary>
        private static void PoleUprawne(Plotno cel, int x, int y, int poziom)
        {
            // Zagony: ciemna bruzda i jaśniejszy grzbiet na przemian, grubo (2 px), żeby
            // pole odcinało się od ubitej ziemi. Rośliny rosną z poziomem farmy.
            Barwa bruzda = Materialy.Ziemia[0], grzbiet = Materialy.Ziemia[6];
            for (int k = 0; k < 5; k++)
            {
                double u = 0.12 + k * 0.19;
                (double ax, double ay) = RzutOsady.NaEkran(x + 0.06, y + u);
                (double bx, double by) = RzutOsady.NaEkran(x + 0.94, y + u);
                for (int g = 0; g < 3; g++)
                {
                    cel.Odcinek(g == 1 ? grzbiet : bruzda, (int)ax, (int)ay + g, (int)bx, (int)by + g);
                }
                int ile = poziom >= 3 ? 9 : poziom == 2 ? 7 : 5;
                for (int s = 0; s < ile; s++)
                {
                    double w = 0.1 + s * (0.8 / (ile - 1));
                    (double px, double py) = RzutOsady.NaEkran(x + w, y + u);
                    bool zloty = poziom >= 3 && s % 2 == 0;
                    Barwa kolor = zloty ? Materialy.Strzecha[8] : Materialy.Listowie[poziom >= 2 ? 6 : 4];
                    int wys = poziom >= 3 ? 5 : poziom == 2 ? 4 : 2;
                    cel.Wypelnij(kolor, (int)px - 1, (int)py - wys, 2, wys);
                }
            }
        }

        /// <summary>Odcinek palisady na jednym polu obwodu: żerdź i poprzeczki do sąsiadów.</summary>
        private static void Slup(Plotno cel, int x, int y, int poziom)
        {
            (int sx, int sy) = RzutOsady.SrodekPola(x, y);
            int wys = 12 + 5 * poziom;
            cel.Wypelnij(Piksele.Kontur, sx - 2, sy - wys - 1, 5, wys + 2);
            cel.Wypelnij(Materialy.Deski[3], sx - 1, sy - wys, 2, wys);
            cel.Wypelnij(Materialy.Deski[1], sx + 1, sy - wys, 1, wys);
            cel.Wypelnij(Materialy.Deski[6], sx - 1, sy - wys - 1, 2, 1);

            foreach ((int dx, int dy) in new[] { (1, 0), (0, 1) })
            {
                int nx = x + dx, ny = y + dy;
                bool obwod = nx < UkladPlacu.Siatka && ny < UkladPlacu.Siatka
                             && (nx == 0 || ny == 0 || nx == UkladPlacu.Siatka - 1 || ny == UkladPlacu.Siatka - 1);
                if (!obwod || (nx, ny) == UkladPlacu.Brama)
                {
                    continue;
                }
                (int tx, int ty) = RzutOsady.SrodekPola(nx, ny);
                // Poprzeczki grube na dwa piksele, w jaśniejszym drewnie niż żerdzie.
                foreach (int wyz in new[] { wys - 5, wys / 2 })
                {
                    cel.Odcinek(Materialy.Deski[4], sx, sy - wyz, tx, ty - wyz);
                    cel.Odcinek(Materialy.Deski[2], sx, sy - wyz + 1, tx, ty - wyz + 1);
                }
            }
        }

        private void RysujOsadnika(Plotno cel, OsadnikDoRysunku o, double t)
        {
            PostacRuchu post = _ruch.Postacie[o.Imie];
            bool idzie = post.Idzie;
            bool sen = !idzie && post.Czynnosc == "sen" && _ruch.Dotarl(o.Imie);
            int krok = idzie && (int)(post.Dystans * 2.4) % 2 == 1 ? 1 : 0;
            Plotno spr = _sprite.Osadnika(o.Zajecie, o.Chory, krok, sen);

            int odbicie = 0;
            if (idzie && (int)(post.Dystans * 4.8) % 2 == 1)
            {
                odbicie = 2;                  // podskok na każdym kroku (dwa piksele sceny = jeden pixelart)
            }
            else if (!idzie && post.Czynnosc == "praca")
            {
                // Pracujący kołysze się jak przy młocie; faza z imienia, żeby nie pracowali chórem.
                long faza = RytmDnia.LosImienia(o.Imie) % 7;
                odbicie = (int)((t + faza) * 2.6) % 2 == 1 ? 2 : 0;
            }

            (double x, double y) = _ruch.Pozycja(o.Imie);
            (double ex, double ey) = RzutOsady.NaEkran(x + 0.5, y + 0.5);
            int sx = (int)Math.Round(ex), sy = (int)Math.Round(ey);
            Plotno cien = _sprite.Cien(spr.Szerokosc + 4, 6, 110);
            cel.Nalóż(cien, sx - cien.Szerokosc / 2 + 3, sy - 3);
            cel.Nalóż(spr, sx - spr.Szerokosc / 2, sy - spr.Wysokosc - odbicie + 2);

            Barwa kolor = o.Morale < 35 ? Materialy.SukmanaCzerwona[6] : Styl.Aktywny.Tekst;
            _podpisy.Add(new PodpisOsadnika(sx, sy - spr.Wysokosc - 4, o.Imie, kolor));
        }

        private void RysujBohatera(Plotno cel, string klasa, List<Swiatlo> swiatla)
        {
            Plotno spr = _sprite.Bohater(klasa);
            (double ex, double ey) = RzutOsady.NaEkran(3.5, 4.3);
            int sx = (int)Math.Round(ex), sy = (int)Math.Round(ey);
            Plotno cien = _sprite.Cien(spr.Szerokosc + 6, 7, 120);
            cel.Nalóż(cien, sx - cien.Szerokosc / 2 + 3, sy - 3);
            cel.Nalóż(spr, sx - spr.Szerokosc / 2, sy - spr.Wysokosc + 2);
            // Bohater nosi latarnię — jak na mapie regionu.
            swiatla.Add(new Swiatlo
            {
                X = sx - 4, Y = sy - spr.Wysokosc / 2, Promien = 64, Kolor = (1.0, 0.86, 0.62), Migocze = true,
            });
        }

        private static void RysujOgnisko(Plotno cel, double t, List<Swiatlo> swiatla)
        {
            (int sx, int sy) = RzutOsady.SrodekPola(UkladPlacu.Ognisko.X, UkladPlacu.Ognisko.Y);
            // Obmurowanie: dziesięć kamieni na elipsie.
            for (int k = 0; k < 10; k++)
            {
                double kat = k / 10.0 * Math.PI * 2;
                int kx = sx + (int)(Math.Cos(kat) * 14);
                int ky = sy + (int)(Math.Sin(kat) * 7);
                cel.Wypelnij(k % 2 == 0 ? Materialy.Kamien[2] : Materialy.Kamien[4], kx - 2, ky - 1, 4, 3);
            }
            GeneratorMroczny.Plomien(cel, sx, sy, t);
            swiatla.Add(new Swiatlo
            {
                X = sx, Y = sy - 8, Promien = 190, Kolor = (1.0, 0.64, 0.36), Migocze = true,
            });
        }

        /// <summary>Płatki: stała garść pikseli sunąca po przekątnej, jak w scenie pythonowej.</summary>
        private static void Snieg(Plotno cel, double t)
        {
            Barwa platek = Materialy.Kosc[7];
            for (int i = 0; i < 140; i++)
            {
                double ax = Piksele.Szum(i, 1, 901), ay = Piksele.Szum(i, 2, 902);
                int x = (int)((ax * Szer + t * (10 + Piksele.Szum(i, 3, 903) * 14)) % Szer);
                int y = (int)((ay * Wys + t * (24 + Piksele.Szum(i, 4, 904) * 30)) % Wys);
                cel.Ustaw(x, y, platek);
            }
        }
    }
}
