using System;

namespace GraSimply.Logika
{
    /// <summary>
    /// Gładki szum wartości na współrzędnych świata — odpowiednik <c>game/szum.py</c>.
    /// Podstawa ciągłych biomów (wzorzec z Project Zomboid): cechy terenu pochodzą z szumu
    /// zasianego jednym ziarnem, więc las przechodzi przez granicę regionu bez szwu.
    /// Mieszanie liczb całkowitych jest 32-bitowe z zawijaniem, a reszta to arytmetyka IEEE,
    /// więc wyniki zgadzają się z Pythonem co do bitu.
    /// </summary>
    public static class Szum
    {
        private static double Skrot(int ix, int iy, long ziarno)
        {
            uint h = unchecked((uint)ix * 374761393u + (uint)iy * 668265263u + (uint)ziarno * 2246822519u);
            h = unchecked((h ^ (h >> 13)) * 1274126177u);
            h ^= h >> 16;
            return h / 4294967296.0;
        }

        private static double Wygladz(double t)
        {
            return t * t * (3.0 - 2.0 * t);
        }

        /// <summary>Szum wartości 0–1: węzły co 1.0, wygładzona interpolacja dwuliniowa.</summary>
        public static double Szum2(double x, double y, long ziarno)
        {
            double fx0 = Math.Floor(x);
            double fy0 = Math.Floor(y);
            int x0 = (int)fx0;
            int y0 = (int)fy0;
            double u = Wygladz(x - fx0);
            double v = Wygladz(y - fy0);
            double a = Skrot(x0, y0, ziarno);
            double b = Skrot(x0 + 1, y0, ziarno);
            double c = Skrot(x0, y0 + 1, ziarno);
            double d = Skrot(x0 + 1, y0 + 1, ziarno);
            double g = a + (b - a) * u;
            double dd = c + (d - c) * u;
            return g + (dd - g) * v;
        }

        /// <summary>Kilka warstw szumu o malejącej amplitudzie — duże plamy z drobnym zróżnicowaniem.</summary>
        public static double Fbm(double x, double y, long ziarno, int oktawy = 3)
        {
            double suma = 0.0, waga = 1.0, razem = 0.0, czestosc = 1.0;
            for (int o = 0; o < oktawy; o++)
            {
                suma += waga * Szum2(x * czestosc, y * czestosc, (ziarno + 7919L * o) & 0x7FFFFFFFL);
                razem += waga;
                waga *= 0.5;
                czestosc *= 2.0;
            }
            return suma / razem;
        }
    }
}
