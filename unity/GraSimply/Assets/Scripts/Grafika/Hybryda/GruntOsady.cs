using GraSimply.Logika;
using System;
using GraSimply.Grafika.Mroczny;

namespace GraSimply.Grafika.Hybryda
{
    /// <summary>
    /// Grunt placu: ciągła powierzchnia jak w stylu Mroczny (faktura liczona
    /// we współrzędnych świata, drobne ziarno, ledwie widoczne plamy), z utrąconą
    /// ziemią tam, gdzie ktoś mieszka, i czernią tuż za obrzeżem — osada leży
    /// w ciemności jak oaza w świetle ogniska.
    ///
    /// Zbudowany raz na kombinację pory roku i kształtu placu, potem tylko
    /// kopiowany do klatki.
    /// </summary>
    public static class GruntOsady
    {
        /// <summary>Jak daleko za obrzeżem placu grunt gaśnie w czerń, w polach.</summary>
        private const double Zanik = 2.4;

        /// <summary>Jak mocno granica ubitej ziemi jest poszarpana, w polach.</summary>
        private const double Poszarpanie = 0.3;

        private const int Zasiew = 5151;

        /// <param name="plac">Ubita ziemia: lewy, górny, prawy i dolny brzeg we współrzędnych świata.</param>
        public static Plotno Zbuduj(int szer, int wys, string pora, (double X0, double Y0, double X1, double Y1) plac)
        {
            var p = new Plotno(szer, wys, przezroczyste: false);
            byte[] d = p.Dane;
            bool zima = pora == "zima";
            Barwa[] trawa = RampaTrawy(pora);
            for (int sy = 0; sy < wys; sy++)
            {
                for (int sx = 0; sx < szer; sx++)
                {
                    (double wx, double wy) = RzutOsady.NaGrunt(sx + 0.5, sy + 0.5);
                    double zew = Math.Max(Math.Max(-wx, -wy), Math.Max(wx - UkladPlacu.Siatka, wy - UkladPlacu.Siatka));
                    if (zew >= Zanik)
                    {
                        continue; // poza kręgiem — czerń
                    }
                    double mnoznik = zew <= 0 ? 1.0 : Wygladz(1 - zew / Zanik);

                    double jx = wx + (Grunt.Gladki(wx, wy, 0.55, Zasiew + 11) - 0.5) * 2 * Poszarpanie;
                    double jy = wy + (Grunt.Gladki(wx, wy, 0.55, Zasiew + 23) - 0.5) * 2 * Poszarpanie;
                    bool ubita = jx >= plac.X0 && jx <= plac.X1 && jy >= plac.Y0 && jy <= plac.Y1;

                    // Ziarno 1-px z dwóch niezależnych szumów — żadnego wzoru siatki.
                    double ziarno = 0.65 * Piksele.Szum(sx, sy, Zasiew + 1) + 0.35 * Piksele.Szum(sx + 3, sy + 7, Zasiew + 2);
                    double drobne = Piksele.Szum(sx, sy, Zasiew + 3);
                    double plama = Grunt.Gladki(wx, wy, 1.4, Zasiew + 1);
                    double srednie = Grunt.Gladki(wx, wy, 0.22, Zasiew + 2);
                    double v = 0.52 + (ziarno - 0.5) * 0.46 + (plama - 0.5) * 0.18 + (srednie - 0.5) * 0.16;

                    Barwa kolor;
                    if (ubita)
                    {
                        kolor = Paleta.Z(zima ? Materialy.Pyl : Materialy.Ziemia, v - 0.04);
                    }
                    else if (zima)
                    {
                        // Śnieg: jasna, ledwie pofalowana powierzchnia z ciemniejszymi prześwitami.
                        double sniegowa = Grunt.Gladki(wx, wy, 0.7, Zasiew + 5) * 0.7 + Grunt.Gladki(wx, wy, 0.18, Zasiew + 6) * 0.3;
                        kolor = sniegowa > 0.72
                            ? Paleta.Z(Materialy.Pyl, v)
                            : Paleta.Z(Materialy.Kosc, 0.55 + (v - 0.5) * 0.7);
                    }
                    else
                    {
                        double kepa = Grunt.Gladki(wx, wy, 0.7, Zasiew + 5) * 0.7 + Grunt.Gladki(wx, wy, 0.18, Zasiew + 6) * 0.3;
                        kolor = kepa > 0.47
                            ? Paleta.Z(trawa, v + (drobne > 0.92 ? 0.22 : 0.0) + (kepa - 0.47) * 0.4)
                            : Paleta.Z(Materialy.Ziemia, v - 0.04);
                    }

                    int i = (sy * szer + sx) * 4;
                    d[i] = (byte)(kolor.R * mnoznik);
                    d[i + 1] = (byte)(kolor.G * mnoznik);
                    d[i + 2] = (byte)(kolor.B * mnoznik);
                }
            }
            return p;
        }

        /// <summary>Trawa zależna od pory roku: wiosną soczysta, latem wypłowiała, jesienią złota.</summary>
        private static Barwa[] RampaTrawy(string pora)
        {
            switch (pora)
            {
                case "wiosna": return Materialy.Listowie;
                case "jesien": return Materialy.Strzecha;
                default: return Materialy.Trawa;
            }
        }

        private static double Wygladz(double t)
        {
            t = Math.Max(0.0, Math.Min(1.0, t));
            return t * t * (3 - 2 * t);
        }
    }
}
