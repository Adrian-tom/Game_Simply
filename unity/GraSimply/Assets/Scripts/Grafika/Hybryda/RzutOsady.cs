using System;

namespace GraSimply.Grafika.Hybryda
{
    /// <summary>
    /// Rzut placu osady: ten sam skośny, trimetryczny kształt co mapa regionu
    /// w stylu Mroczny (<see cref="Mroczny.Rzut"/>), tylko w mniejszej skali,
    /// żeby cały plac 7×7 mieścił się w kadrze 640×360.
    ///
    /// Współrzędne świata: (0, 0, 0) to róg placu, jedno pole ma bok 1,
    /// z rośnie w górę. Środek pola (i, j) to (i + 0,5; j + 0,5).
    /// </summary>
    public sealed class RzutOsady
    {
        /// <summary>Powiększenie względem rzutu regionu (tam 1,6): plac musi się zmieścić.</summary>
        public const double Z = 1.15;

        public const double AX = 40 * Z, AY = -10 * Z;
        public const double BX = 80.0 / 3.0 * Z, BY = 20 * Z;
        public const double Wysokosc = 52 * Z;

        /// <summary>Ekranowa pozycja punktu (0, 0, 0) — plac leży na środku kadru.</summary>
        public const double OX = 52, OY = 150;

        private const double Wyznacznik = AX * BY - AY * BX;

        public static (double X, double Y) NaEkran(double wx, double wy, double wz = 0)
        {
            return (OX + AX * wx + BX * wy, OY + AY * wx + BY * wy - Wysokosc * wz);
        }

        /// <summary>Punkt na gruncie (z = 0) pod danym pikselem ekranu.</summary>
        public static (double WX, double WY) NaGrunt(double sx, double sy)
        {
            double dx = sx - OX;
            double dy = sy - OY;
            return ((BY * dx - BX * dy) / Wyznacznik, (-AY * dx + AX * dy) / Wyznacznik);
        }

        /// <summary>Piksel środka pola (i, j) na gruncie.</summary>
        public static (int X, int Y) SrodekPola(double i, double j)
        {
            (double x, double y) = NaEkran(i + 0.5, j + 0.5);
            return ((int)Math.Round(x), (int)Math.Round(y));
        }
    }
}
