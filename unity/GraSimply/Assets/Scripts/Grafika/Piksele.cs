using System;
using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Prymitywy pixelartu liczone w kodzie — bez plików PNG.
    ///
    /// Wrażenie bryły daje kilka reguł naraz: jedno źródło światła (z lewej
    /// góry), cieniowanie po normalnej, rampy 4 kolorów z ditheringiem Bayera
    /// zamiast gładkich gradientów i ciemny kontur 1 px wokół każdej bryły.
    ///
    /// Wszystkie rachunki są na <c>double</c> i w kolejności jak w wersji
    /// pythonowej, bo port ma dawać te same piksele — testy porównują je
    /// co do bajtu z wzorcami z Pythona.
    /// </summary>
    public static class Piksele
    {
        public static readonly (double X, double Y, double Z) Swiatlo = (-0.55, -0.65, 0.52);

        public static readonly int[][] Bayer =
        {
            new[] { 0, 8, 2, 10 },
            new[] { 12, 4, 14, 6 },
            new[] { 3, 11, 1, 9 },
            new[] { 15, 7, 13, 5 },
        };

        public static readonly Barwa Kontur = new Barwa(18, 16, 24);

        /// <summary>
        /// Powtarzalny szum 0…1 — ten sam piksel zawsze dostaje tę samą wartość.
        ///
        /// Python liczy to na nieograniczonych liczbach i maskuje do 32 bitów,
        /// więc rachunek musi iść po <c>ulong</c>: przy <c>int</c> mnożenia
        /// by się przekręciły i szum wyszedłby inny.
        /// </summary>
        public static double Szum(int x, int y, int s = 0)
        {
            ulong n = (ulong)((long)x * 374761393L + (long)y * 668265263L + (long)s * 982451653L)
                      & 0xFFFFFFFFUL;
            n = ((n ^ (n >> 13)) * 1274126177UL) & 0xFFFFFFFFUL;
            return ((n ^ (n >> 16)) & 0xFFFFUL) / 65535.0;
        }

        /// <summary>Szum wartości z interpolacją — plamy zamiast pojedynczych pikseli.</summary>
        public static double SzumGladki(double x, double y, double skala, int s = 0)
        {
            double gx = x / skala;
            double gy = y / skala;
            int x0 = (int)Math.Floor(gx);
            int y0 = (int)Math.Floor(gy);
            double fx = gx - x0;
            double fy = gy - y0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            double a = Szum(x0, y0, s);
            double b = Szum(x0 + 1, y0, s);
            double c = Szum(x0, y0 + 1, s);
            double d = Szum(x0 + 1, y0 + 1, s);
            return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy;
        }

        /// <summary>Wartość 0…1 → kolor z rampy, z ditheringiem między stopniami.</summary>
        public static Barwa ZRampy(Barwa[] rampa, double v, int x, int y)
        {
            v = Math.Min(Math.Max(v, 0.0), 1.0) * (rampa.Length - 1);
            int i = (int)v;
            if (v - i > Bayer[Modulo(y, 4)][Modulo(x, 4)] / 16.0 && i < rampa.Length - 1)
            {
                i += 1;
            }
            return rampa[i];
        }

        /// <summary>Reszta nieujemna — indeksy Bayera muszą działać też dla ujemnych pozycji.</summary>
        private static int Modulo(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }

        public static Barwa Ciemniej(Barwa c, double f = 0.6)
        {
            return new Barwa((int)(c.R * f), (int)(c.G * f), (int)(c.B * f));
        }

        public static Barwa Jasniej(Barwa c, double f = 1.18, int plus = 8)
        {
            return new Barwa(Math.Min(255, (int)(c.R * f) + plus),
                             Math.Min(255, (int)(c.G * f) + plus),
                             Math.Min(255, (int)(c.B * f) + plus));
        }

        /// <summary>
        /// Elipsa cieniowana jak bryła (normalna · światło).
        /// Korony drzew, skały, krzaki.
        /// </summary>
        public static Plotno Kula(int rx, int ry, Barwa[] rampa, int s = 0, double grudki = 0.18)
        {
            var p = new Plotno(2 * rx + 1, 2 * ry + 1);
            (double lx, double ly, double lz) = Swiatlo;
            for (int y = 0; y < 2 * ry + 1; y++)
            {
                for (int x = 0; x < 2 * rx + 1; x++)
                {
                    double nx = (x - rx) / (rx + 0.5);
                    double ny = (y - ry) / (ry + 0.5);
                    double r2 = nx * nx + ny * ny;
                    if (r2 > 1)
                    {
                        continue;
                    }
                    double nz = Math.Sqrt(1 - r2);
                    double d = Math.Max(0.0, nx * lx + ny * ly + nz * lz);
                    d += (SzumGladki(x, y, 2.2, s) - 0.5) * grudki * 2;
                    Barwa kolor = ZRampy(rampa, d * 1.05, x, y);
                    if (r2 > 0.78 && nx + ny > 0.2)
                    {
                        // cień własny po stronie odwróconej od światła
                        kolor = Ciemniej(rampa[0], 0.7);
                    }
                    p.Ustaw(x, y, kolor);
                }
            }
            return p;
        }

        /// <summary>Wielokąt z fakturą (deski, cegła, dachówka) liczoną per piksel.</summary>
        public static void Wypelnij(Plotno p, IList<(int X, int Y)> punkty, Barwa[] rampa,
                                    double baza, int s, string wzor = "", int x0 = 0, int y0 = 0)
        {
            bool[] maska = Plotno.MaskaWielokata(p.Szerokosc, p.Wysokosc, punkty);
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach ((int px, int py) in punkty)
            {
                minX = Math.Min(minX, px);
                maxX = Math.Max(maxX, px);
                minY = Math.Min(minY, py);
                maxY = Math.Max(maxY, py);
            }
            for (int y = Math.Max(0, minY); y < Math.Min(p.Wysokosc, maxY + 1); y++)
            {
                for (int x = Math.Max(0, minX); x < Math.Min(p.Szerokosc, maxX + 1); x++)
                {
                    if (!maska[y * p.Szerokosc + x])
                    {
                        continue;
                    }
                    double v = baza + (Szum(x, y, s) - 0.5) * 0.25;
                    int lx = x - x0;
                    int ly = y - y0;
                    if (wzor == "deski" && Modulo(lx, 3) == 0)
                    {
                        v -= 0.3;
                    }
                    else if (wzor == "cegla"
                             && (Modulo(ly, 3) == 0
                                 || Modulo(lx + PodzielWDol(ly, 3) * 2, 5) == 0))
                    {
                        v -= 0.3;
                    }
                    else if (wzor == "dach" && Modulo(ly, 2) == 0)
                    {
                        v -= Modulo(lx + ly, 4) != 0 ? 0.25 : 0.45;
                    }
                    p.Ustaw(x, y, ZRampy(rampa, v, x, y));
                }
            }
        }

        /// <summary>Dzielenie w dół jak <c>//</c> w Pythonie (ważne dla ujemnych pozycji faktury).</summary>
        private static int PodzielWDol(int a, int b)
        {
            int iloraz = a / b;
            if (a % b != 0 && (a < 0) != (b < 0))
            {
                iloraz--;
            }
            return iloraz;
        }

        /// <summary>
        /// Prostopadłościan izometryczny z dachem dwuspadowym.
        /// (cx, cy) = środek podstawy.
        /// </summary>
        public static void Domek(Plotno p, int cx, int cy, int a, int h, Barwa[] sciana,
                                 Barwa[] dach, int s, string wzor = "deski",
                                 Barwa? okno = null, int? dachH = null)
        {
            int b = a / 2;
            (int X, int Y) L = (cx - a, cy);
            (int X, int Y) F = (cx, cy + b);
            (int X, int Y) R = (cx + a, cy);
            (int X, int Y) B = (cx, cy - b);

            (int X, int Y) Up((int X, int Y) punkt, int d)
            {
                return (punkt.X, punkt.Y - d);
            }

            Wypelnij(p, new[] { L, F, Up(F, h), Up(L, h) }, sciana, 0.62, s, wzor, cx - a, cy);
            Wypelnij(p, new[] { F, R, Up(R, h), Up(F, h) }, sciana, 0.28, s + 1, wzor, cx, cy);
            if (okno.HasValue)
            {
                p.Wypelnij(okno.Value, cx - a / 2 - 1, cy - h / 2 + b / 2 - 1, 2, 3);
            }
            int dh = dachH ?? a;
            (int X, int Y) p1 = ((L.X + B.X) / 2, (L.Y + B.Y) / 2 - h - dh);
            (int X, int Y) p2 = ((F.X + R.X) / 2, (F.Y + R.Y) / 2 - h - dh);
            Wypelnij(p, new[] { Up(B, h), Up(R, h), p2, p1 }, dach, 0.2, s + 2, "dach", cx, cy);
            Wypelnij(p, new[] { Up(F, h), Up(R, h), p2 }, sciana, 0.2, s + 3, wzor, cx, cy);
            Wypelnij(p, new[] { Up(L, h), Up(F, h), p2, p1 }, dach, 0.72, s + 4, "dach", cx, cy);
            p.Odcinek(Ciemniej(dach[0], 0.6), p1.X, p1.Y, p2.X, p2.Y);
        }

        /// <summary>Ciemny kontur 1 px — oddziela bryłę od tła.</summary>
        public static Plotno Obrys(Plotno zrodlo, Barwa? kolor = null)
        {
            Barwa k = kolor ?? Kontur;
            var wynik = new Plotno(zrodlo.Szerokosc, zrodlo.Wysokosc);
            int w = zrodlo.Szerokosc;
            int h = zrodlo.Wysokosc;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (zrodlo.Alfa(x, y) != 0)
                    {
                        continue;
                    }
                    // Alfa() poza płótnem zwraca 0, więc brzegi same się pilnują.
                    if (zrodlo.Alfa(x + 1, y) != 0 || zrodlo.Alfa(x - 1, y) != 0
                        || zrodlo.Alfa(x, y + 1) != 0 || zrodlo.Alfa(x, y - 1) != 0)
                    {
                        wynik.Ustaw(x, y, k);
                    }
                }
            }
            wynik.Nalóż(zrodlo, 0, 0);
            return wynik;
        }

        /// <summary>Światło punktowe w pasmach z ditheringiem — pikselowe, nie gładkie.</summary>
        public static Plotno Poswiata(int r, Barwa kolor, int stopnie = 5)
        {
            var p = new Plotno(2 * r, 2 * r, przezroczyste: false);
            for (int y = 0; y < 2 * r; y++)
            {
                for (int x = 0; x < 2 * r; x++)
                {
                    double d = Hypot(x - r, (y - r) * 1.6) / r;
                    double v = Math.Pow(Math.Max(0.0, 1 - d), 1.4);
                    double q = v * stopnie;
                    int i = (int)q + (q - (int)q > Bayer[Modulo(y, 4)][Modulo(x, 4)] / 16.0 ? 1 : 0);
                    double f = Math.Min(i, stopnie) / (double)stopnie;
                    p.Ustaw(x, y, new Barwa((int)(kolor.R * f), (int)(kolor.G * f), (int)(kolor.B * f)));
                }
            }
            return p;
        }

        private static double Hypot(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }

        /// <summary>
        /// Sprite zapisany jako linijki znaków: każdy znak = kolor z palety,
        /// reszta przezroczysta.
        /// </summary>
        public static Plotno SpriteZTekstu(string[] wiersze, Dictionary<char, Barwa> paleta)
        {
            var p = new Plotno(wiersze[0].Length, wiersze.Length);
            for (int y = 0; y < wiersze.Length; y++)
            {
                string w = wiersze[y];
                for (int x = 0; x < w.Length; x++)
                {
                    if (paleta.TryGetValue(w[x], out Barwa kolor))
                    {
                        p.Ustaw(x, y, kolor);
                    }
                }
            }
            return p;
        }

        public static Plotno GradientNieba(int w, int h, Barwa gora, Barwa dol, int pasma = 12)
        {
            var p = new Plotno(w, h, przezroczyste: false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double f = y / (double)h + (Bayer[Modulo(y, 4)][Modulo(x, 4)] / 16.0 - 0.5) * 0.08;
                    f = Math.Min(Math.Max(Zaokraglij(f * pasma) / pasma, 0.0), 1.0);
                    p.Ustaw(x, y, new Barwa((int)(gora.R + (dol.R - gora.R) * f),
                                            (int)(gora.G + (dol.G - gora.G) * f),
                                            (int)(gora.B + (dol.B - gora.B) * f)));
                }
            }
            return p;
        }

        /// <summary>Maska do mnożenia: ciemniejsze rogi, drobne pasma z ditheringiem.</summary>
        public static Plotno Winieta(int w, int h)
        {
            var p = new Plotno(w, h, przezroczyste: false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double d = Hypot((x - w / 2.0) / (w / 2.0), (y - h / 2.0) / (h / 2.0));
                    double v = 1 - Math.Max(0.0, d - 0.7) * 0.8;
                    v = Zaokraglij((v + (Bayer[Modulo(y, 4)][Modulo(x, 4)] / 16.0 - 0.5) / 24) * 24) / 24;
                    var k = (byte)(int)(255 * Math.Min(v, 1.0));
                    p.Ustaw(x, y, new Barwa(k, k, k));
                }
            }
            return p;
        }

        /// <summary>
        /// Zaokrąglanie jak <c>round()</c> w Pythonie — do najbliższej parzystej
        /// przy połowie. C# domyślnie w <c>Math.Round(double)</c> robi to samo,
        /// ale nazwa własna trzyma intencję w jednym miejscu.
        /// </summary>
        private static double Zaokraglij(double v)
        {
            return Math.Round(v, MidpointRounding.ToEven);
        }
    }
}
