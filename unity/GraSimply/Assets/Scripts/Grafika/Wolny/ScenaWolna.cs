using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika.Hybryda;
using GraSimply.Grafika.Mroczny;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Grafika.Wolny
{
    /// <summary>
    /// Scena świata w czasie rzeczywistym („Zomboid fantasy”): izometryczne kafle 32×16 wokół bohatera,
    /// drzewa, skały, krzaki, woda, namiot i ognisko. Noc przyciemnia całość, a światło ma ognisko
    /// i latarnia bohatera — tak jak w scenie osady (styl hybrydowy).
    ///
    /// Scena nie trzyma stanu świata: rysuje wprost to, co mówi <see cref="SwiatWolny"/>, kafel po
    /// kaflu w kolejności głębokości (x + y), więc drzewo z przodu zasłania bohatera, a bohater
    /// — drzewo z tyłu.
    /// </summary>
    public sealed class ScenaWolna
    {
        public const int Szer = 640;
        public const int Wys = 360;
        public const int PolowaKafla = 16;       // szerokość kafla to 2 × 16 px, wysokość 2 × 8 px
        public const int CentrumX = 320;
        public const int CentrumY = 196;

        private readonly SpriteOsady _sprite = new SpriteOsady();
        private readonly List<PodpisOsadnika> _podpisy = new List<PodpisOsadnika>();
        private Plotno _duch;

        /// <summary>Imiona osadników z ostatniej klatki — tekst rysuje warstwa interfejsu.</summary>
        public IReadOnlyList<PodpisOsadnika> Podpisy => _podpisy;

        /// <summary>Piksel środka kafla (tx, ty) przy bohaterze w (hx, hy).</summary>
        public static (int X, int Y) NaEkran(double tx, double ty, double hx, double hy)
        {
            double dx = tx - hx, dy = ty - hy;
            return ((int)Math.Round(CentrumX + (dx - dy) * PolowaKafla), (int)Math.Round(CentrumY + (dx + dy) * (PolowaKafla / 2.0)));
        }

        /// <summary>Kafel pod pikselem ekranu — przyda się do klikania w świat.</summary>
        public static (double X, double Y) NaSwiat(double sx, double sy, double hx, double hy)
        {
            double a = (sx - CentrumX) / PolowaKafla;          // dx - dy
            double b = (sy - CentrumY) / (PolowaKafla / 2.0);  // dx + dy
            return (hx + (a + b) / 2.0, hy + (b - a) / 2.0);
        }

        private static void Romb(Plotno cel, Barwa kolor, int cx, int cy)
        {
            for (int r = 0; r < 16; r++)
            {
                int pol = r < 8 ? 2 * r + 2 : 2 * (15 - r) + 2;
                cel.Wypelnij(kolor, cx - pol, cy - 8 + r, 2 * pol, 1);
            }
        }

        private static Barwa Grunt(string biom, double odcien)
        {
            int d = odcien < 0.12 ? -1 : odcien > 0.88 ? 1 : 0;
            switch (biom)
            {
                case "las": return Materialy.Listowie[Math.Max(0, 3 + d)];
                case "bagna": return Materialy.Mul[Math.Max(0, 3 + d)];
                case "wzgórza": return Materialy.Sciolka[Math.Max(0, 4 + d)];
                case "kanion": return Materialy.SkalaCzerwona[Math.Max(0, 3 + d)];
                case "ruiny": return Materialy.Pyl[Math.Max(0, 4 + d)];
                default: return Materialy.Trawa[Math.Max(0, 5 + d)];
            }
        }

        public void Rysuj(Plotno cel, SwiatWolny s, double t, string klasa = "Wojownik")
        {
            cel.Wypelnij(Piksele.Kontur);
            int ix0 = (int)Math.Floor(s.X), iy0 = (int)Math.Floor(s.Y);
            const int zasieg = 30;
            var obiekty = new List<(double Glebokosc, Action Rysuj)>();
            var swiatla = new List<Swiatlo>();

            for (int ty = iy0 - zasieg; ty <= iy0 + zasieg; ty++)
            {
                for (int tx = ix0 - zasieg; tx <= ix0 + zasieg; tx++)
                {
                    (int sx, int sy) = NaEkran(tx + 0.5, ty + 0.5, s.X, s.Y);
                    if (sx < -40 || sx > Szer + 40 || sy < -50 || sy > Wys + 50)
                    {
                        continue;
                    }
                    int x = tx, y = ty, px = sx, py = sy;
                    if (!GeneratorSwiata.WGranicach(x, y))
                    {
                        Romb(cel, Materialy.Pyl[0], px, py);
                        continue;
                    }
                    Obiekt o = s.ObiektNa(x, y);
                    if (o == Obiekt.Woda)
                    {
                        int fala = (int)(t * 1.5 + x * 3 + y * 5) % 4;
                        Romb(cel, Materialy.WodaBagienna[3 + (fala == 0 ? 1 : 0)], px, py);
                    }
                    else
                    {
                        Romb(cel, Grunt(s.Swiat.Biom(x, y), s.Swiat.Odcien(x, y)), px, py);
                    }
                    double glebokosc = (x + 0.5 - s.X) + (y + 0.5 - s.Y);
                    if (o != Obiekt.Brak && o != Obiekt.Woda)
                    {
                        int pozostalo = s.Pozostalo(x, y);
                        obiekty.Add((glebokosc, () => RysujObiekt(cel, o, px, py, pozostalo, x, y)));
                    }
                    if (s.Budowle.TryGetValue((x, y), out string budowla))
                    {
                        obiekty.Add((glebokosc + 0.01, () => RysujBudowle(cel, budowla, px, py, t, swiatla)));
                    }
                }
            }

            _podpisy.Clear();
            foreach (PostacOsadnika p in s.Postacie)
            {
                PostacOsadnika postac = p;
                (int ox, int oy) = NaEkran(p.X, p.Y, s.X, s.Y);
                if (ox > -30 && ox < Szer + 30 && oy > -30 && oy < Wys + 50)
                {
                    obiekty.Add(((p.X - s.X) + (p.Y - s.Y), () => RysujOsadnika(cel, postac, ox, oy, t)));
                }
            }
            double gb = 0.0;
            obiekty.Add((gb + 0.2, () => RysujBohatera(cel, s, klasa, swiatla)));
            foreach ((double _, Action rysuj) in obiekty.OrderBy(o => o.Glebokosc))
            {
                rysuj();
            }

            if (s.Zbieranie.HasValue)
            {
                (int bx, int by) = NaEkran(s.Zbieranie.Value.X + 0.5, s.Zbieranie.Value.Y + 0.5, s.X, s.Y);
                PasekPostepu(cel, bx, by - 40, s.Zbieranie.Value.Postep);
            }

            double jasnosc = s.Spi ? Math.Min(s.Jasnosc, 0.35) : s.Jasnosc;
            (double R, double G, double B) otoczenie = ScenaOsady.Otoczenie(jasnosc);
            if (otoczenie.R < 0.999 || otoczenie.G < 0.999 || otoczenie.B < 0.999)
            {
                Oswietlenie.Zastosuj(cel, swiatla, t, otoczenie);
            }
            Oswietlenie.Paleta().Kwantyzuj(cel);
            // Duch na końcu, po świetle i palecie: ma być czytelny także nocą.
            if (s.PodgladBudowy() is (int gx, int gy, string klucz, bool mozna))
            {
                (int dx, int dy) = NaEkran(gx + 0.5, gy + 0.5, s.X, s.Y);
                RysujDucha(cel, klucz, dx, dy, mozna, t);
            }
        }

        /// <summary>Półprzezroczysta budowla w kolorze zgody (zielony) albo odmowy (czerwony) z obrysem kafla.</summary>
        private void RysujDucha(Plotno cel, string klucz, int cx, int cy, bool mozna, double t)
        {
            Barwa kolor = mozna ? new Barwa(110, 230, 120) : new Barwa(240, 80, 70);
            const int bok = 90, podstawa = 70;
            if (_duch == null)
            {
                _duch = new Plotno(bok, bok);
            }
            _duch.Wypelnij(Barwa.Pusta);
            RysujBudowle(_duch, klucz, bok / 2, podstawa, t, new List<Swiatlo>());
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    int ex = cx - bok / 2 + x, ey = cy - podstawa + y;
                    if (_duch.Alfa(x, y) > 120 && cel.WSrodku(ex, ey))
                    {
                        Barwa b = _duch.Wez(x, y), tlo = cel.Wez(ex, ey);
                        cel.Ustaw(ex, ey, new Barwa((b.R + kolor.R + tlo.R) / 3, (b.G + kolor.G + tlo.G) / 3,
                                                    (b.B + kolor.B + tlo.B) / 3));
                    }
                }
            }
            for (int r = 0; r < 16; r++)
            {
                int pol = r < 8 ? 2 * r + 2 : 2 * (15 - r) + 2;
                cel.Wypelnij(kolor, cx - pol, cy - 8 + r, 2, 1);
                cel.Wypelnij(kolor, cx + pol - 2, cy - 8 + r, 2, 1);
            }
        }

        private static void PasekPostepu(Plotno cel, int cx, int y, double postep)
        {
            cel.Wypelnij(Piksele.Kontur, cx - 17, y - 1, 34, 6);
            cel.Wypelnij(Materialy.Pyl[1], cx - 16, y, 32, 4);
            cel.Wypelnij(Materialy.Listowie[6], cx - 16, y, (int)(32 * Math.Max(0.0, Math.Min(1.0, postep))), 4);
        }

        private void Cien(Plotno cel, int cx, int cy, int szer)
        {
            Plotno c = _sprite.Cien(szer, Math.Max(4, szer / 3), 90);
            cel.Nalóż(c, cx - c.Szerokosc / 2, cy - c.Wysokosc / 2 - 1);
        }

        private void RysujObiekt(Plotno cel, Obiekt o, int cx, int cy, int pozostalo, int x, int y)
        {
            switch (o)
            {
                case Obiekt.Drzewo:
                {
                    bool igly = Szum.Losowa(x, y, 5) < 0.45;
                    Barwa[] liscie = igly ? Materialy.Igliwie : Materialy.Listowie;
                    double skala = 0.55 + 0.15 * pozostalo;     // napoczęte drzewo ma rzadszą koronę
                    Cien(cel, cx, cy + 2, 16);
                    cel.Wypelnij(Materialy.Kora[2], cx - 2, cy - 14, 4, 15);
                    cel.Wypelnij(Materialy.Kora[4], cx, cy - 14, 2, 15);
                    int szerDol = (int)(24 * skala), szerSrodek = (int)(18 * skala), szerGora = (int)(11 * skala);
                    cel.Wypelnij(liscie[2], cx - szerDol / 2, cy - 24, szerDol, 9);
                    cel.Wypelnij(liscie[3], cx - szerSrodek / 2, cy - 31, szerSrodek, 8);
                    cel.Wypelnij(liscie[5], cx - szerGora / 2, cy - 37, szerGora, 7);
                    cel.Wypelnij(liscie[6], cx - szerGora / 2 + 1, cy - 36, Math.Max(1, szerGora / 2), 2);
                    break;
                }
                case Obiekt.Skala:
                {
                    int w = 8 + 2 * pozostalo;
                    Cien(cel, cx, cy + 3, w + 4);
                    cel.Wypelnij(Materialy.Kamien[3], cx - w, cy - 6, 2 * w, 8);
                    cel.Wypelnij(Materialy.Kamien[5], cx - w + 3, cy - 11, 2 * w - 7, 6);
                    cel.Wypelnij(Materialy.Kamien[7], cx - w + 5, cy - 11, 5, 2);
                    break;
                }
                case Obiekt.Krzak:
                {
                    Cien(cel, cx, cy + 3, 18);
                    cel.Wypelnij(Materialy.Listowie[3], cx - 8, cy - 7, 16, 8);
                    cel.Wypelnij(Materialy.Listowie[5], cx - 6, cy - 12, 12, 6);
                    if (pozostalo > 0)
                    {
                        Barwa jagoda = Materialy.SukmanaCzerwona[6];
                        cel.Wypelnij(jagoda, cx - 5, cy - 8, 2, 2);
                        cel.Wypelnij(jagoda, cx + 2, cy - 10, 2, 2);
                        cel.Wypelnij(jagoda, cx + 4, cy - 5, 2, 2);
                    }
                    break;
                }
                case Obiekt.Ziolo:
                {
                    cel.Wypelnij(Materialy.Listowie[6], cx - 3, cy - 7, 1, 8);
                    cel.Wypelnij(Materialy.Listowie[7], cx, cy - 9, 1, 10);
                    cel.Wypelnij(Materialy.Listowie[6], cx + 3, cy - 6, 1, 7);
                    cel.Wypelnij(Materialy.Strzecha[8], cx - 1, cy - 11, 3, 2);
                    break;
                }
                case Obiekt.Sciana:
                {
                    Cien(cel, cx, cy + 2, 26);
                    cel.Wypelnij(Materialy.KamienMuru[3], cx - 13, cy - 16, 26, 17);
                    cel.Wypelnij(Materialy.KamienMuru[5], cx - 13, cy - 18, 26, 3);
                    for (int r = 0; r < 3; r++)
                    {
                        cel.Wypelnij(Materialy.KamienMuru[1], cx - 13, cy - 13 + r * 5, 26, 1);
                    }
                    break;
                }
            }
        }

        private void RysujBudowle(Plotno cel, string klucz, int cx, int cy, double t, List<Swiatlo> swiatla)
        {
            switch (klucz)
            {
                case "spiwor":
                    cel.Wypelnij(Materialy.SukmanaCzerwona[3], cx - 9, cy - 4, 18, 7);
                    cel.Wypelnij(Materialy.SukmanaCzerwona[5], cx - 8, cy - 4, 16, 2);
                    cel.Wypelnij(Materialy.Kosc[6], cx - 9, cy - 4, 5, 6);
                    break;
                case "ognisko":
                    for (int k = 0; k < 8; k++)
                    {
                        double kat = k / 8.0 * Math.PI * 2;
                        cel.Wypelnij(k % 2 == 0 ? Materialy.Kamien[2] : Materialy.Kamien[4],
                                     cx + (int)(Math.Cos(kat) * 9) - 2, cy + (int)(Math.Sin(kat) * 4) - 1, 4, 3);
                    }
                    cel.Wypelnij(Materialy.Kora[1], cx - 6, cy - 2, 12, 3);
                    GeneratorMroczny.Plomien(cel, cx, cy, t);
                    swiatla.Add(new Swiatlo { X = cx, Y = cy - 8, Promien = 150, Kolor = (1.0, 0.64, 0.36), Migocze = true });
                    break;
                case "chata":
                {
                    Plotno spr = _sprite.Chata(1, out (int X, int Y) podstawa);
                    Cien(cel, cx, cy + 3, 36);
                    cel.Nalóż(spr, cx - podstawa.X, cy - podstawa.Y);
                    swiatla.Add(new Swiatlo { X = cx, Y = cy - 8, Promien = 46, Kolor = (0.95, 0.74, 0.46) });
                    break;
                }
                case "namiot":
                    Cien(cel, cx, cy + 3, 34);
                    for (int r = 0; r < 22; r++)
                    {
                        int pol = 15 - (int)(r * 0.62);
                        cel.Wypelnij(Materialy.Strzecha[5], cx - pol, cy - r, pol, 1);
                        cel.Wypelnij(Materialy.Strzecha[3], cx, cy - r, pol, 1);
                    }
                    cel.Wypelnij(Materialy.Pyl[0], cx - 3, cy - 10, 6, 11);
                    cel.Wypelnij(Materialy.Deski[6], cx - 1, cy - 25, 2, 4);
                    break;
            }
        }

        private void RysujOsadnika(Plotno cel, PostacOsadnika p, int sx, int sy, double t)
        {
            int krok = p.Idzie && (int)(p.Dystans * 2.4) % 2 == 1 ? 1 : 0;
            Plotno spr = _sprite.Osadnika(p.O.Zajecie, p.O.Chory > 0, krok, p.Spi);
            int odbicie = !p.Idzie && !p.Spi && (int)(t * 2.6 + p.O.Imie.Length) % 2 == 1 ? 1 : 0;
            Plotno cien = _sprite.Cien(spr.Szerokosc + 4, 6, 110);
            cel.Nalóż(cien, sx - cien.Szerokosc / 2 + 2, sy - 3);
            cel.Nalóż(spr, sx - spr.Szerokosc / 2, sy - spr.Wysokosc - odbicie + 2);
            Barwa kolor = p.O.Morale < 35 ? Materialy.SukmanaCzerwona[6] : Styl.Aktywny.Tekst;
            _podpisy.Add(new PodpisOsadnika(sx, sy - spr.Wysokosc - 4, p.O.Imie, kolor));
        }

        private void RysujBohatera(Plotno cel, SwiatWolny s, string klasa, List<Swiatlo> swiatla)
        {
            Plotno spr = _sprite.Bohater(klasa);
            int sx = CentrumX, sy = CentrumY;
            Plotno cien = _sprite.Cien(spr.Szerokosc + 6, 7, 120);
            cel.Nalóż(cien, sx - cien.Szerokosc / 2 + 2, sy - 3);
            if (s.Spi)
            {
                cel.Wypelnij(Materialy.SukmanaCzerwona[3], sx - 14, sy - 6, 28, 9);
                cel.Wypelnij(Materialy.Kosc[7], sx - 14, sy - 6, 7, 7);
                return;
            }
            cel.Nalóż(spr, sx - spr.Szerokosc / 2, sy - spr.Wysokosc + 2);
            // Latarnia bohatera: po zmroku jedyne światło poza ogniskiem.
            swiatla.Add(new Swiatlo { X = sx, Y = sy - spr.Wysokosc / 2, Promien = 78, Kolor = (1.0, 0.86, 0.62), Migocze = true });
        }
    }
}
