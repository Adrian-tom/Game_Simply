using System;
using GraSimply.Logika;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>
    /// Grunt regionu rysowany naraz, jako jedna ciągła powierzchnia.
    ///
    /// Styl klasyczny układał osobne kafle z blokami ziemi; tu kafli nie ma.
    /// Każdy piksel ekranu jest rzutowany z powrotem na grunt, a faktura liczy
    /// się we współrzędnych świata — więc przechodzi płynnie przez granice pól
    /// i regionów. Biom bierzemy z pola, ale granica pól jest poszarpana szumem,
    /// dzięki czemu las przechodzi w równinę nieregularną linią, a nie prostą.
    ///
    /// Faktura odwraca proporcje stylu klasycznego, zgodnie z pomiarami
    /// oryginału: drobne ziarno 1-pikselowe o dużej sile i ledwie widoczne
    /// duże plamy. Siatki Bayera nie ma — ziarno jest nieregularne.
    /// </summary>
    public static class Grunt
    {
        /// <summary>Szerokość przejścia w czerń na krawędzi regionu, w polach.</summary>
        private const double Zanik = 0.35;

        /// <summary>Jak mocno granica biomów jest poszarpana, w polach.</summary>
        private const double Poszarpanie = 0.34;

        /// <summary>Stały zasiew stylu — faktura ma się zgadzać na styku regionów.</summary>
        private const int Zasiew = 7331;

        public static Plotno Rysuj(PoleDoRysunku[][] pola, (int X, int Y) region, bool wszystkoOdkryte,
                                   int klatka)
        {
            var p = new Plotno(Rzut.SzerRegionu, Rzut.WysRegionu, przezroczyste: false);
            int n = pola.Length;
            byte[] d = p.Dane;
            for (int sy = 0; sy < Rzut.WysRegionu; sy++)
            {
                for (int sx = 0; sx < Rzut.SzerRegionu; sx++)
                {
                    (double wx, double wy) = Rzut.NaGrunt(sx + 0.5, sy + 0.5);
                    double brzeg = Math.Min(Math.Min(wx, wy), Math.Min(n - wx, n - wy));
                    if (brzeg <= 0)
                    {
                        continue; // poza regionem — czerń, jak poza mapą w oryginale
                    }

                    // Współrzędne globalne: faktura nie ma szwu między regionami.
                    double gx = region.X * n + wx;
                    double gy = region.Y * n + wy;

                    double jx = wx + (Gladki(gx, gy, 0.55, Zasiew + 11) - 0.5) * 2 * Poszarpanie;
                    double jy = wy + (Gladki(gx, gy, 0.55, Zasiew + 23) - 0.5) * 2 * Poszarpanie;
                    int fx = Math.Max(0, Math.Min(n - 1, (int)Math.Floor(jx)));
                    int fy = Math.Max(0, Math.Min(n - 1, (int)Math.Floor(jy)));
                    PoleDoRysunku pole = pola[fy][fx];

                    Barwa kolor = Faktura(pole.Biom, gx, gy, sx, sy, region, klatka);

                    double mnoznik = Wygladz(brzeg / Zanik);
                    if (!wszystkoOdkryte && !pole.Odkryte)
                    {
                        kolor = Zamglij(kolor, gx, gy);
                    }
                    int i = (sy * Rzut.SzerRegionu + sx) * 4;
                    d[i] = (byte)(kolor.R * mnoznik);
                    d[i + 1] = (byte)(kolor.G * mnoznik);
                    d[i + 2] = (byte)(kolor.B * mnoznik);
                }
            }
            return p;
        }

        /// <summary>Czy w regionie jest widoczna woda — wtedy grunt ma klatki animacji.</summary>
        public static bool MaWode(PoleDoRysunku[][] pola, bool wszystkoOdkryte)
        {
            foreach (PoleDoRysunku[] wiersz in pola)
            {
                foreach (PoleDoRysunku pole in wiersz)
                {
                    if (pole.Biom == "bagna" && (wszystkoOdkryte || pole.Odkryte))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ //
        //  Faktury biomów                                                     //
        // ------------------------------------------------------------------ //

        private static Barwa Faktura(string biom, double gx, double gy, int sx, int sy,
                                     (int X, int Y) region, int klatka)
        {
            int s = (region.X * 7919 + region.Y * 104729) & 0xFFFF;
            // Ziarno 1-px: dwa niezależne szumy, żeby nie układało się w żaden wzór.
            double ziarno = 0.65 * Piksele.Szum(sx, sy, s + 1) + 0.35 * Piksele.Szum(sx + 3, sy + 7, s + 2);
            double drobne = Piksele.Szum(sx, sy, s + 3);
            double plama = Gladki(gx, gy, 1.4, Zasiew + 1);
            double srednie = Gladki(gx, gy, 0.22, Zasiew + 2);

            // Wspólna baza jasności: duże plamy ledwie widoczne, ziarno mocne.
            double v = 0.52 + (ziarno - 0.5) * 0.46 + (plama - 0.5) * 0.18 + (srednie - 0.5) * 0.16;

            switch (biom)
            {
                case "las":
                    return Las(gx, gy, v, drobne);
                case "bagna":
                    return Bagna(gx, gy, v, drobne, sx, sy, s, klatka);
                case "wzgórza":
                    return Wzgorza(gx, gy, v, sx, sy, s);
                case "kanion":
                    return Kanion(gx, gy, v, drobne);
                case "ruiny":
                    return Ruiny(gx, gy, v, drobne);
                default:
                    return Rowniny(gx, gy, v, drobne);
            }
        }

        /// <summary>Wypłowiała trawa na przemian z gołą ziemią.</summary>
        private static Barwa Rowniny(double gx, double gy, double v, double drobne)
        {
            double trawa = Gladki(gx, gy, 0.7, Zasiew + 5) * 0.7 + Gladki(gx, gy, 0.18, Zasiew + 6) * 0.3;
            if (trawa > 0.47)
            {
                // Pojedyncze jaśniejsze źdźbła — tylko w trawie, nie na ziemi.
                double w = v + (drobne > 0.92 ? 0.22 : 0.0) + (trawa - 0.47) * 0.4;
                return Paleta.Z(Materialy.Trawa, w);
            }
            return Paleta.Z(Materialy.Ziemia, v - 0.04);
        }

        /// <summary>Ściółka pod koronami: ciemniej, z plamami mchu i rdzą opadłych liści.</summary>
        private static Barwa Las(double gx, double gy, double v, double drobne)
        {
            double mech = Gladki(gx, gy, 0.45, Zasiew + 7);
            if (drobne > 0.975)
            {
                return Paleta.Z(Materialy.Ochra, v * 0.8); // opadły liść
            }
            if (mech > 0.6)
            {
                return Paleta.Z(Materialy.Listowie, v * 0.62 + (mech - 0.6) * 0.5);
            }
            return Paleta.Z(Materialy.Sciolka, v - 0.08);
        }

        /// <summary>Muł z rozlewiskami mętnej wody; woda migocze w trzech klatkach.</summary>
        private static Barwa Bagna(double gx, double gy, double v, double drobne, int sx, int sy, int s, int klatka)
        {
            double woda = Gladki(gx, gy, 0.75, Zasiew + 9) * 0.7 + Gladki(gx, gy, 0.24, Zasiew + 10) * 0.3;
            if (woda > 0.56)
            {
                double fala = Piksele.Szum(sx + klatka * 5, sy, s + 40);
                double w = 0.42 + (Gladki(gx, gy, 0.3, Zasiew + 12) - 0.5) * 0.3;
                if (fala > 0.94)
                {
                    w += 0.32; // odblask
                }
                return Paleta.Z(Materialy.WodaBagienna, w);
            }
            if (woda > 0.5)
            {
                return Paleta.Z(Materialy.Mul, v - 0.25); // mokry brzeg
            }
            if (drobne > 0.9)
            {
                return Paleta.Z(Materialy.Trawa, v * 0.75); // kępki turzycy
            }
            return Paleta.Z(Materialy.Mul, v);
        }

        /// <summary>Kamienisty pył z drobnymi otoczakami, które rzucają cień w prawo.</summary>
        private static Barwa Wzgorza(double gx, double gy, double v, int sx, int sy, int s)
        {
            if (Piksele.Szum(sx, sy, s + 50) > 0.955)
            {
                return Paleta.Z(Materialy.Kamien, 0.72);
            }
            if (Piksele.Szum(sx - 1, sy, s + 50) > 0.955)
            {
                return Paleta.Z(Materialy.Kamien, 0.15); // cień kamyka
            }
            double darn = Gladki(gx, gy, 0.6, Zasiew + 14);
            if (darn > 0.62)
            {
                return Paleta.Z(Materialy.Trawa, v * 0.85);
            }
            return Paleta.Z(Materialy.Pyl, v);
        }

        /// <summary>Spękana czerwona ochra: sieć szczelin z grzbietów szumu.</summary>
        private static Barwa Kanion(double gx, double gy, double v, double drobne)
        {
            double g = Gladki(gx, gy, 0.32, Zasiew + 15);
            if (Math.Abs(g - 0.5) < 0.022)
            {
                return Paleta.Z(Materialy.SkalaCzerwona, 0.08); // szczelina
            }
            if (Math.Abs(g - 0.5) < 0.04)
            {
                return Paleta.Z(Materialy.Ochra, v - 0.2); // ciemny brzeg szczeliny
            }
            return Paleta.Z(Materialy.Ochra, v + (drobne > 0.96 ? 0.15 : 0.0));
        }

        /// <summary>
        /// Rozbite płyty dawnej posadzki: siatka kamieni w układzie świata, część
        /// płyt wyrwana i zasypana pyłem, w szparach ziemia.
        /// </summary>
        private static Barwa Ruiny(double gx, double gy, double v, double drobne)
        {
            const double Plyt = 3.0;
            double u = gx * Plyt;
            double w = gy * Plyt;
            int pu = (int)Math.Floor(u);
            int pw = (int)Math.Floor(w);
            double fu = u - pu;
            double fw = w - pw;
            double ton = Piksele.Szum(pu, pw, Zasiew + 30);
            bool wyrwana = Piksele.Szum(pu, pw, Zasiew + 31) < 0.28;
            if (wyrwana)
            {
                return Paleta.Z(Materialy.Pyl, v - 0.06);
            }
            if (fu < 0.07 || fw < 0.07)
            {
                return Paleta.Z(Materialy.Ziemia, v * 0.45); // fuga
            }
            if (fu > 0.93 || fw > 0.93)
            {
                return Paleta.Z(Materialy.KamienMuru, v + 0.12); // krawędź płyty w świetle
            }
            double rysa = Gladki(gx, gy, 0.12, Zasiew + 33);
            if (Math.Abs(rysa - 0.5) < 0.012)
            {
                return Paleta.Z(Materialy.KamienMuru, 0.12);
            }
            return Paleta.Z(Materialy.KamienMuru, v * 0.85 + (ton - 0.5) * 0.18);
        }

        /// <summary>
        /// Nieodkryte pole: przyciemnione do jednej trzeciej i odbarwione, z ledwie
        /// widoczną mgiełką — widać kształt świata, ale nie szczegóły.
        /// </summary>
        private static Barwa Zamglij(Barwa k, double gx, double gy)
        {
            double sr = (k.R + k.G + k.B) / 3.0;
            double mgla = 0.26 + (Gladki(gx, gy, 0.5, Zasiew + 60) - 0.5) * 0.12;
            return new Barwa((int)((k.R * 0.45 + sr * 0.55) * mgla),
                             (int)((k.G * 0.45 + sr * 0.55) * mgla),
                             (int)((k.B * 0.45 + sr * 0.55) * mgla + 3));
        }

        // ------------------------------------------------------------------ //

        /// <summary>Szum wartości o okresie podanym w polach świata.</summary>
        public static double Gladki(double gx, double gy, double okres, int s)
        {
            return Piksele.SzumGladki(gx / okres, gy / okres, 1.0, s);
        }

        private static double Wygladz(double t)
        {
            t = Math.Max(0.0, Math.Min(1.0, t));
            return t * t * (3 - 2 * t);
        }
    }
}
