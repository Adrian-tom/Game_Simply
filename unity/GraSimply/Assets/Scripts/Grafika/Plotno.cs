using System;
using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>Kolor RGBA, bajt na składową — jak piksel w pygame.</summary>
    public struct Barwa : IEquatable<Barwa>
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Barwa(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Barwa(int r, int g, int b, int a = 255)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
            A = (byte)a;
        }

        public static readonly Barwa Pusta = new Barwa(0, 0, 0, 0);

        public bool Equals(Barwa inna)
        {
            return R == inna.R && G == inna.G && B == inna.B && A == inna.A;
        }

        public override bool Equals(object obj)
        {
            return obj is Barwa inna && Equals(inna);
        }

        public override int GetHashCode()
        {
            return (R << 24) | (G << 16) | (B << 8) | A;
        }

        public override string ToString()
        {
            return $"({R}, {G}, {B}, {A})";
        }
    }

    /// <summary>
    /// Płótno pikseli — odpowiednik <c>pygame.Surface</c> w czystym C#.
    ///
    /// Trzyma bajty w układzie RGBA32, czyli dokładnie tym, którego oczekuje
    /// <c>Texture2D.LoadRawTextureData</c>, więc przekazanie klatki do Unity
    /// nie wymaga przepakowywania. Dzięki temu, że nie ma tu nic z UnityEngine,
    /// cały pixelart da się narysować i porównać z wersją pythonową bez edytora.
    /// </summary>
    public sealed class Plotno
    {
        public readonly int Szerokosc;
        public readonly int Wysokosc;

        /// <summary>Surowe bajty RGBA, wiersz po wierszu od góry.</summary>
        public readonly byte[] Dane;

        public Plotno(int szerokosc, int wysokosc, bool przezroczyste = true)
        {
            Szerokosc = Math.Max(1, szerokosc);
            Wysokosc = Math.Max(1, wysokosc);
            Dane = new byte[Szerokosc * Wysokosc * 4];
            if (!przezroczyste)
            {
                // pygame.Surface bez SRCALPHA startuje jako czarne i nieprzejrzyste.
                for (int i = 3; i < Dane.Length; i += 4)
                {
                    Dane[i] = 255;
                }
            }
        }

        public Plotno Kopia()
        {
            var nowe = new Plotno(Szerokosc, Wysokosc);
            Buffer.BlockCopy(Dane, 0, nowe.Dane, 0, Dane.Length);
            return nowe;
        }

        public bool WSrodku(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Szerokosc && y < Wysokosc;
        }

        private int Indeks(int x, int y)
        {
            return (y * Szerokosc + x) * 4;
        }

        /// <summary>Ustawia piksel wprost, bez mieszania — jak <c>Surface.set_at</c>.</summary>
        public void Ustaw(int x, int y, Barwa kolor)
        {
            if (!WSrodku(x, y))
            {
                return;
            }
            int i = Indeks(x, y);
            Dane[i] = kolor.R;
            Dane[i + 1] = kolor.G;
            Dane[i + 2] = kolor.B;
            Dane[i + 3] = kolor.A;
        }

        public Barwa Wez(int x, int y)
        {
            if (!WSrodku(x, y))
            {
                return Barwa.Pusta;
            }
            int i = Indeks(x, y);
            return new Barwa(Dane[i], Dane[i + 1], Dane[i + 2], Dane[i + 3]);
        }

        public byte Alfa(int x, int y)
        {
            return WSrodku(x, y) ? Dane[Indeks(x, y) + 3] : (byte)0;
        }

        /// <summary>Wypełnia cały obszar jednym kolorem.</summary>
        public void Wypelnij(Barwa kolor)
        {
            for (int i = 0; i < Dane.Length; i += 4)
            {
                Dane[i] = kolor.R;
                Dane[i + 1] = kolor.G;
                Dane[i + 2] = kolor.B;
                Dane[i + 3] = kolor.A;
            }
        }

        /// <summary>Wypełnia prostokąt — jak <c>Surface.fill(kolor, (x, y, w, h))</c>.</summary>
        public void Wypelnij(Barwa kolor, int x, int y, int szer, int wys)
        {
            for (int py = y; py < y + wys; py++)
            {
                for (int px = x; px < x + szer; px++)
                {
                    Ustaw(px, py, kolor);
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Przenoszenie i mieszanie                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Nakłada płótno w punkcie (x, y) z mieszaniem „source over”.
        ///
        /// Sprite'y gry mają piksele albo w pełni kryjące, albo w pełni
        /// przejrzyste, więc dla nich to zwykłe kopiowanie — tak jak w pygame.
        /// Mieszanie liczy się tylko dla cienia pod postacią i plakietek z imionami.
        /// </summary>
        public void Nalóż(Plotno zrodlo, int x, int y)
        {
            for (int zy = 0; zy < zrodlo.Wysokosc; zy++)
            {
                int cy = y + zy;
                if (cy < 0 || cy >= Wysokosc)
                {
                    continue;
                }
                for (int zx = 0; zx < zrodlo.Szerokosc; zx++)
                {
                    int cx = x + zx;
                    if (cx < 0 || cx >= Szerokosc)
                    {
                        continue;
                    }
                    int zi = zrodlo.Indeks(zx, zy);
                    byte a = zrodlo.Dane[zi + 3];
                    if (a == 0)
                    {
                        continue;
                    }
                    int ci = Indeks(cx, cy);
                    if (a == 255)
                    {
                        Dane[ci] = zrodlo.Dane[zi];
                        Dane[ci + 1] = zrodlo.Dane[zi + 1];
                        Dane[ci + 2] = zrodlo.Dane[zi + 2];
                        Dane[ci + 3] = 255;
                        continue;
                    }
                    Dane[ci] = (byte)((zrodlo.Dane[zi] * a + Dane[ci] * (255 - a)) / 255);
                    Dane[ci + 1] = (byte)((zrodlo.Dane[zi + 1] * a + Dane[ci + 1] * (255 - a)) / 255);
                    Dane[ci + 2] = (byte)((zrodlo.Dane[zi + 2] * a + Dane[ci + 2] * (255 - a)) / 255);
                    Dane[ci + 3] = Math.Max(Dane[ci + 3], a);
                }
            }
        }

        /// <summary>Dodawanie składowych — jak <c>BLEND_RGB_ADD</c>.</summary>
        public void DodajNa(Plotno zrodlo, int x, int y)
        {
            Mieszaj(zrodlo, x, y, (a, b) => Math.Min(255, a + b));
        }

        /// <summary>Mnożenie składowych — jak <c>BLEND_RGB_MULT</c>.</summary>
        public void PomnozNa(Plotno zrodlo, int x, int y)
        {
            Mieszaj(zrodlo, x, y, Pomnoz);
        }

        /// <summary>
        /// Mnożenie dwóch składowych tak, jak robi to pygame.
        ///
        /// Nie jest to <c>a * b / 255</c>, choć tak by się wydawało: pygame
        /// liczy <c>(a * b + 255) >> 8</c>, co dla części wartości daje wynik
        /// o jeden większy. Przy mgle wojny różnica jest widoczna na co drugim
        /// pikselu, więc trzeba powtórzyć dokładnie tę formułę — została
        /// ustalona przez porównanie z wyjściem pygame piksel po pikselu.
        /// </summary>
        private static int Pomnoz(int a, int b)
        {
            return (a * b + 255) >> 8;
        }

        private void Mieszaj(Plotno zrodlo, int x, int y, Func<int, int, int> dzialanie)
        {
            for (int zy = 0; zy < zrodlo.Wysokosc; zy++)
            {
                int cy = y + zy;
                if (cy < 0 || cy >= Wysokosc)
                {
                    continue;
                }
                for (int zx = 0; zx < zrodlo.Szerokosc; zx++)
                {
                    int cx = x + zx;
                    if (cx < 0 || cx >= Szerokosc)
                    {
                        continue;
                    }
                    int zi = zrodlo.Indeks(zx, zy);
                    int ci = Indeks(cx, cy);
                    Dane[ci] = (byte)dzialanie(Dane[ci], zrodlo.Dane[zi]);
                    Dane[ci + 1] = (byte)dzialanie(Dane[ci + 1], zrodlo.Dane[zi + 1]);
                    Dane[ci + 2] = (byte)dzialanie(Dane[ci + 2], zrodlo.Dane[zi + 2]);
                }
            }
        }

        /// <summary>Mnoży każdą składową RGBA przez kolor — jak <c>BLEND_RGBA_MULT</c>.</summary>
        public void PomnozPrzez(Barwa kolor)
        {
            for (int i = 0; i < Dane.Length; i += 4)
            {
                Dane[i] = (byte)Pomnoz(Dane[i], kolor.R);
                Dane[i + 1] = (byte)Pomnoz(Dane[i + 1], kolor.G);
                Dane[i + 2] = (byte)Pomnoz(Dane[i + 2], kolor.B);
                Dane[i + 3] = (byte)Pomnoz(Dane[i + 3], kolor.A);
            }
        }

        // ------------------------------------------------------------------ //
        //  Kształty                                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Wypełniony wielokąt metodą skanowania wierszy.
        ///
        /// Służy jako maska dla faktur (deski, cegła, dachówka), więc liczy się
        /// tylko to, które piksele są w środku. Reguła „środek piksela”
        /// z półotwartym przedziałem daje brzegi bez dziur i bez podwójnych
        /// pikseli na stykach ścian.
        /// </summary>
        public static bool[] MaskaWielokata(int szerokosc, int wysokosc,
                                            IList<(int X, int Y)> punkty)
        {
            var maska = new bool[szerokosc * wysokosc];
            int ile = punkty.Count;
            if (ile < 3)
            {
                return maska;
            }
            var przeciecia = new List<double>();
            for (int y = 0; y < wysokosc; y++)
            {
                double sy = y + 0.5;
                przeciecia.Clear();
                for (int i = 0; i < ile; i++)
                {
                    (int x1, int y1) = punkty[i];
                    (int x2, int y2) = punkty[(i + 1) % ile];
                    if (y1 == y2)
                    {
                        continue;
                    }
                    double dolna = Math.Min(y1, y2);
                    double gorna = Math.Max(y1, y2);
                    if (sy < dolna || sy >= gorna)
                    {
                        continue;
                    }
                    double t = (sy - y1) / (double)(y2 - y1);
                    przeciecia.Add(x1 + t * (x2 - x1));
                }
                if (przeciecia.Count < 2)
                {
                    continue;
                }
                przeciecia.Sort();
                for (int i = 0; i + 1 < przeciecia.Count; i += 2)
                {
                    int od = (int)Math.Ceiling(przeciecia[i] - 0.5);
                    int doX = (int)Math.Floor(przeciecia[i + 1] - 0.5);
                    for (int x = Math.Max(0, od); x <= Math.Min(szerokosc - 1, doX); x++)
                    {
                        maska[y * szerokosc + x] = true;
                    }
                }
            }
            return maska;
        }

        /// <summary>Wypełniona elipsa wpisana w prostokąt — jak <c>draw.ellipse</c>.</summary>
        public void Elipsa(Barwa kolor, int x, int y, int szer, int wys, int grubosc = 0)
        {
            double rx = szer / 2.0;
            double ry = wys / 2.0;
            double cx = x + rx;
            double cy = y + ry;
            for (int py = y; py < y + wys; py++)
            {
                for (int px = x; px < x + szer; px++)
                {
                    double nx = (px + 0.5 - cx) / rx;
                    double ny = (py + 0.5 - cy) / ry;
                    double d = nx * nx + ny * ny;
                    if (d > 1.0)
                    {
                        continue;
                    }
                    if (grubosc > 0)
                    {
                        double wx = (px + 0.5 - cx) / Math.Max(0.5, rx - grubosc);
                        double wy = (py + 0.5 - cy) / Math.Max(0.5, ry - grubosc);
                        if (wx * wx + wy * wy <= 1.0)
                        {
                            continue; // środek zostaje pusty — rysujemy tylko obwód
                        }
                    }
                    Ustaw(px, py, kolor);
                }
            }
        }

        /// <summary>Odcinek metodą Bresenhama — jak <c>draw.line</c> o grubości 1.</summary>
        public void Odcinek(Barwa kolor, int x1, int y1, int x2, int y2)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int blad = dx - dy;
            while (true)
            {
                Ustaw(x1, y1, kolor);
                if (x1 == x2 && y1 == y2)
                {
                    return;
                }
                int e2 = 2 * blad;
                if (e2 > -dy)
                {
                    blad -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    blad += dx;
                    y1 += sy;
                }
            }
        }

        /// <summary>Obwód prostokąta o zadanej grubości — jak <c>draw.rect</c> z obramowaniem.</summary>
        public void Obramowanie(Barwa kolor, int x, int y, int szer, int wys, int grubosc)
        {
            for (int i = 0; i < grubosc; i++)
            {
                Wypelnij(kolor, x + i, y + i, szer - 2 * i, 1);
                Wypelnij(kolor, x + i, y + wys - 1 - i, szer - 2 * i, 1);
                Wypelnij(kolor, x + i, y + i, 1, wys - 2 * i);
                Wypelnij(kolor, x + szer - 1 - i, y + i, 1, wys - 2 * i);
            }
        }

        /// <summary>
        /// Powiększenie całkowitą krotnością metodą najbliższego sąsiada —
        /// pixelart nie znosi wygładzania.
        /// </summary>
        public Plotno Skaluj(int krotnosc)
        {
            var nowe = new Plotno(Szerokosc * krotnosc, Wysokosc * krotnosc);
            for (int y = 0; y < nowe.Wysokosc; y++)
            {
                for (int x = 0; x < nowe.Szerokosc; x++)
                {
                    nowe.Ustaw(x, y, Wez(x / krotnosc, y / krotnosc));
                }
            }
            return nowe;
        }

        /// <summary>
        /// Przeskalowanie ułamkowe (najbliższy sąsiad) — potrzebne dla
        /// pulsującego blasku ognia, który w wersji pythonowej robi
        /// <c>transform.scale_by</c>.
        /// </summary>
        public Plotno SkalujO(double krotnosc)
        {
            int szer = Math.Max(1, (int)(Szerokosc * krotnosc));
            int wys = Math.Max(1, (int)(Wysokosc * krotnosc));
            var nowe = new Plotno(szer, wys);
            for (int y = 0; y < wys; y++)
            {
                int zy = Math.Min(Wysokosc - 1, y * Wysokosc / wys);
                for (int x = 0; x < szer; x++)
                {
                    int zx = Math.Min(Szerokosc - 1, x * Szerokosc / szer);
                    nowe.Ustaw(x, y, Wez(zx, zy));
                }
            }
            return nowe;
        }
    }
}
