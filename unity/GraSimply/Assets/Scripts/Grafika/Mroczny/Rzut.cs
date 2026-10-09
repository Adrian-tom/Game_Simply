using System;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>
    /// Rzut stylu mrocznego: skośny, jak w Falloucie, a nie romb 2:1.
    ///
    /// Fallout rozpina kafel podłogi na wektorach (+48, −12) i (+32, +24),
    /// więc jedna krawędź opada 1:4, druga 3:4 — to rzut trimetryczny,
    /// i właśnie on odróżnia ten świat od izometrii w stylu Diablo.
    /// Tu ten sam kształt: pole mapy to (+40, −10)·Z wzdłuż osi x świata
    /// i (+26⅔, +20)·Z wzdłuż osi y, a jednostka wysokości to 52·Z pikseli.
    ///
    /// Współrzędne świata: (0, 0, 0) to róg regionu, jedno pole ma bok 1,
    /// z rośnie w górę. Cały region rysuje się na płótnie
    /// <see cref="SzerRegionu"/>×<see cref="WysRegionu"/>, a scena 640×480
    /// jest jego wycinkiem wokół postaci.
    /// </summary>
    public static class Rzut
    {
        /// <summary>Kadr sceny — tyle widać naraz; kamera chodzi za postacią.</summary>
        public const int SzerSceny = 640;
        public const int WysSceny = 480;

        /// <summary>
        /// Powiększenie rzutu. Przy 1 cały region mieści się w kadrze, ale
        /// postać ma wtedy 27 px i ginie; Fallout pokazywał wycinek świata
        /// z ludźmi wysokimi na ~45 px i przewijał go za graczem. Tu tak samo.
        /// </summary>
        public const double Z = 1.6;

        public const double AX = 40 * Z, AY = -10 * Z;
        public const double BX = 80.0 / 3.0 * Z, BY = 20 * Z;
        public const double Wysokosc = 52 * Z;

        /// <summary>Margines nad tylnym narożnikiem regionu — na drzewa i wieże z ostatniego rzędu.</summary>
        private const double Nad = 120 * Z;

        /// <summary>Ekranowa pozycja punktu (0, 0, 0) na płótnie całego regionu.</summary>
        public const double OX = 24;

        public const double OY = Nad - AY * 9;

        /// <summary>Płótno całego regionu 9×9 — kadr wycina z niego prostokąt.</summary>
        public const int SzerRegionu = (int)(OX * 2 + (AX + BX) * 9);

        public const int WysRegionu = (int)(OY + BY * 9 + 24);

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

        /// <summary>
        /// Kierunek do obserwatora w układzie świata (znormalizowany).
        ///
        /// To ten wektor, wzdłuż którego punkt świata nie zmienia pozycji na
        /// ekranie: rozwiązanie AX·dx + BX·dy = 0 i AY·dx + BY·dy − H·dz = 0,
        /// zwrócone ku górze, bo patrzymy z góry.
        /// </summary>
        public static readonly (double X, double Y, double Z) DoObserwatora =
            Normuj(-BX, AX, (AY * -BX + BY * AX) / Wysokosc);

        /// <summary>
        /// Kierunek ku światłu: z lewej i wysoko, prostopadle do linii wzroku,
        /// więc lewe ściany są jasne, prawe w cieniu. Na ekranie światło pada
        /// z lewego górnego rogu, a cienie kładą się w prawo i lekko w dół,
        /// jak w oryginale — obiekty są „namalowane w południe”.
        /// </summary>
        public static readonly (double X, double Y, double Z) DoSwiatla = Normuj(-0.9, -0.6, 1.35);

        public static (double X, double Y, double Z) Normuj(double x, double y, double z)
        {
            double d = Math.Sqrt(x * x + y * y + z * z);
            return (x / d, y / d, z / d);
        }
    }
}
