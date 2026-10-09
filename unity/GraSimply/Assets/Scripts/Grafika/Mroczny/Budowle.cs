using System;
using System.Collections.Generic;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>
    /// Budowle i punkty orientacyjne: chaty, karczma, kuźnia, świątynia, jaskinia,
    /// miasto, legowisko bossa, portal, leże smoka, latająca wyspa i obóz.
    ///
    /// Budynki są zwrócone frontem ku obserwatorowi: ściana od strony +y jest
    /// widoczna i leży w półcieniu, ściana od −x jest widoczna i oświetlona —
    /// jedna strona jasna, druga ciemna, jak na sprite'ach Fallouta. Okna,
    /// paleniska i runy są z barw samoświecących, więc nocą świecą.
    /// </summary>
    public static class Budowle
    {
        private static double Los(int ziarno, int i) => Piksele.Szum(ziarno, i, 1931);

        /// <summary>
        /// Budowle modelowane są w skali pola, ale fotografowane powiększone —
        /// chata musi stać wyżej niż drzewo obok, inaczej mapa czyta się jak
        /// makieta z zabawkami.
        /// </summary>
        public const double Powiekszenie = 1.7;

        private static Szkic Wielki(Bryla b, Material[] mat, W3 min, W3 max) =>
            new Szkic(b, mat, min, max).Skaluj(Powiekszenie);

        // ---- materiały --------------------------------------------------- //

        private static Material Bale => new Material(Materialy.Deski, 0.12, 30) { Pasy = 0.034 };
        private static Material Strzecha => new Material(Materialy.Strzecha, 0.22, 40) { Pasy = 0.03 };
        private static Material Dachowka => new Material(Materialy.Dachowka, 0.14, 36) { Pasy = 0.026 };
        private static Material Lupek => new Material(Materialy.Lupek, 0.12, 36) { Pasy = 0.024 };
        private static Material Tynk => new Material(Materialy.Tynk, 0.10, 24);
        private static Material Belka => new Material(Materialy.Kora, 0.08, 30, -0.14);
        private static Material Mur => new Material(Materialy.KamienMuru, 0.16, 30) { Pasy = 0.04 };
        private static Material Skala => new Material(Materialy.Kamien, 0.16, 12) { Nalot = Materialy.Listowie };
        private static Material Drzwi => new Material(Materialy.Deski, 0.08, 40, -0.22);
        private static Material Okno => new Material(Materialy.Okno, 0.4, 40, 0.0, swieci: true);
        private static Material Ogien => new Material(Materialy.Ogien, 0.6, 30, 0.0, swieci: true);
        private static Material Magia => new Material(Materialy.Magia, 0.6, 22, 0.0, swieci: true);
        private static Material Runy => new Material(Materialy.Runy, 0.5, 30, 0.0, swieci: true);
        private static Material Mosiadz => new Material(Materialy.Mosiadz, 0.10, 30);
        private static Material Kosc => new Material(Materialy.Kosc, 0.08, 40);
        private static Material Ciemnosc => new Material(Materialy.Kamien, 0.05, 20, -0.45);
        private static Material Plotno => new Material(Materialy.SukmanaSzara, 0.10, 20, 0.18);
        private static Material Zelazo => new Material(Materialy.Zelazo, 0.10, 40);

        // ---- chata ------------------------------------------------------- //

        /// <summary>
        /// Chata z bali: ściany w poziome pasy, strzecha albo gont, drzwi, okno
        /// ze światłem i kamienny komin. Elementy budowane w układzie chaty,
        /// a całość obracana — osada nie wygląda jak rząd klonów.
        /// </summary>
        public static Bryla Chata(int ziarno, double skala, out Material[] mat, bool gont = false)
        {
            mat = new[] { Bale, gont ? Dachowka : Strzecha, Drzwi, Okno, Mur };
            double dx = 0.15 * skala, dy = 0.11 * skala, hw = 0.13 * skala;
            var czesci = new List<Bryla>
            {
                Bryla.Prostopadloscian(0, 0, hw / 2, dx, dy, hw / 2, 0, 0.004),
                Bryla.Dach(0, 0, hw - 0.004, dx + 0.03 * skala, dy + 0.035 * skala, 0.12 * skala, 1)
                     .Chropowata(0.004, 50, ziarno),
                Bryla.Prostopadloscian(0.05 * skala, dy + 0.002, 0.045 * skala, 0.026 * skala, 0.006,
                                       0.045 * skala, 2),
                Bryla.Prostopadloscian(-0.06 * skala, dy + 0.003, 0.075 * skala, 0.022 * skala, 0.004,
                                       0.018 * skala, 3),
                Bryla.Prostopadloscian(-dx - 0.003, -0.01, 0.075 * skala, 0.004, 0.022 * skala,
                                       0.018 * skala, 3),
                Bryla.Prostopadloscian(0.08 * skala, -0.04 * skala, hw + 0.08 * skala, 0.022 * skala,
                                       0.022 * skala, 0.08 * skala, 4, 0.003),
            };
            return Bryla.Suma(czesci.ToArray()).Obroc((Los(ziarno, 1) - 0.5) * 0.5);
        }

        public static Szkic ChataSzkic(int ziarno, bool gont = false, double skala = Powiekszenie)
        {
            Bryla b = Chata(ziarno, 1.0, out Material[] mat, gont);
            return new Szkic(b, mat, new W3(-0.24, -0.24, -0.01), new W3(0.24, 0.24, 0.40)).Skaluj(skala);
        }

        // ---- karczma ----------------------------------------------------- //

        /// <summary>
        /// Karczma: kamienna podmurówka, bielone ściany z ciemnym ryglem, dach
        /// z dachówki, dwa świecące okna, szyld na wysięgniku i beczka u wejścia.
        /// </summary>
        public static Szkic Karczma(int ziarno)
        {
            var mat = new[] { Mur, Tynk, Belka, Dachowka, Okno, Drzwi, Mosiadz, Bale };
            const double dx = 0.22, dy = 0.15, pod = 0.05, hw = 0.20;
            var czesci = new List<Bryla>
            {
                Bryla.Prostopadloscian(0, 0, pod / 2, dx + 0.01, dy + 0.01, pod / 2, 0, 0.004)
                     .Chropowata(0.004, 40, ziarno),
                Bryla.Prostopadloscian(0, 0, pod + hw / 2, dx, dy, hw / 2, 1, 0.003),
                Bryla.Dach(0, 0, pod + hw - 0.004, dx + 0.035, dy + 0.045, 0.16, 3),
            };
            // Rygiel: pionowe i ukośne belki na widocznych ścianach.
            for (int i = -2; i <= 2; i++)
            {
                czesci.Add(Bryla.Prostopadloscian(i * dx / 2.2, dy + 0.003, pod + hw / 2, 0.008, 0.004, hw / 2, 2));
                czesci.Add(Bryla.Prostopadloscian(-dx - 0.003, i * dy / 2.2, pod + hw / 2, 0.004, 0.008, hw / 2, 2));
            }
            czesci.Add(Bryla.Prostopadloscian(0, dy + 0.003, pod + hw * 0.55, dx, 0.004, 0.007, 2));
            czesci.Add(Bryla.Prostopadloscian(-dx - 0.003, 0, pod + hw * 0.55, 0.004, dy, 0.007, 2));
            // Okna i drzwi.
            czesci.Add(Bryla.Prostopadloscian(-0.12, dy + 0.004, pod + 0.13, 0.026, 0.004, 0.026, 4));
            czesci.Add(Bryla.Prostopadloscian(0.12, dy + 0.004, pod + 0.13, 0.026, 0.004, 0.026, 4));
            czesci.Add(Bryla.Prostopadloscian(-dx - 0.004, 0.05, pod + 0.13, 0.004, 0.026, 0.026, 4));
            czesci.Add(Bryla.Prostopadloscian(0.0, dy + 0.004, pod + 0.06, 0.032, 0.005, 0.06, 5));
            // Szyld na wysięgniku i beczka.
            czesci.Add(Bryla.Kapsula(new W3(-dx, dy + 0.01, pod + 0.17), new W3(-dx - 0.07, dy + 0.06, pod + 0.17),
                                     0.006, 2));
            czesci.Add(Bryla.Prostopadloscian(-dx - 0.06, dy + 0.055, pod + 0.13, 0.025, 0.004, 0.022, 6));
            czesci.Add(Bryla.Stozkowa(new W3(0.08, dy + 0.06, 0), new W3(0.08, dy + 0.06, 0.07), 0.032, 0.032, 7));
            Bryla b = Bryla.Suma(czesci.ToArray());
            return Wielki(b, mat, new W3(-0.34, -0.26, -0.01), new W3(0.30, 0.30, 0.48));
        }

        // ---- kuźnia ------------------------------------------------------ //

        /// <summary>Kamienna kuźnia z rozżarzonym paleniskiem w otwartej ścianie, kominem i kowadłem.</summary>
        public static Szkic Kuznia(int ziarno)
        {
            var mat = new[] { Mur, Lupek, Ogien, Zelazo, Bale };
            const double dx = 0.18, dy = 0.14, hw = 0.17;
            Bryla sciany = Bryla.Prostopadloscian(0, 0, hw / 2, dx, dy, hw / 2, 0, 0.004)
                                .Chropowata(0.005, 36, ziarno);
            // Otwarta wnęka paleniska w ścianie frontowej.
            Bryla wneka = Bryla.Prostopadloscian(-0.04, dy, 0.07, 0.06, 0.05, 0.07, 0);
            var czesci = new List<Bryla>
            {
                sciany.Minus(wneka),
                Bryla.Prostopadloscian(-0.04, dy - 0.04, 0.035, 0.05, 0.012, 0.035, 2).Chropowata(0.01, 30, ziarno),
                Bryla.Dach(0, 0, hw - 0.004, dx + 0.03, dy + 0.035, 0.10, 1),
                Bryla.Prostopadloscian(0.1, -0.05, hw + 0.11, 0.035, 0.035, 0.13, 0, 0.004).Chropowata(0.004, 36, ziarno + 1),
                // Kowadło na pniaku przed kuźnią.
                Bryla.Stozkowa(new W3(0.10, dy + 0.09, 0), new W3(0.10, dy + 0.09, 0.05), 0.028, 0.024, 4),
                Bryla.Prostopadloscian(0.10, dy + 0.09, 0.062, 0.035, 0.014, 0.012, 3),
                Bryla.Kapsula(new W3(0.065, dy + 0.09, 0.066), new W3(0.045, dy + 0.09, 0.066), 0.008, 3),
            };
            return Wielki(Bryla.Suma(czesci.ToArray()), mat,
                             new W3(-0.26, -0.24, -0.01), new W3(0.26, 0.30, 0.46));
        }

        // ---- świątynia --------------------------------------------------- //

        /// <summary>Kapliczka: stopnie, cztery kolumny, kopuła i złota iglica.</summary>
        public static Szkic Swiatynia(int ziarno)
        {
            var mat = new[] { Mur, Mosiadz, new Material(Materialy.Tynk, 0.06, 20, 0.05) };
            var czesci = new List<Bryla>
            {
                Bryla.Prostopadloscian(0, 0, 0.02, 0.19, 0.19, 0.02, 0, 0.004),
                Bryla.Prostopadloscian(0, 0, 0.05, 0.16, 0.16, 0.012, 0, 0.004),
            };
            for (int i = 0; i < 4; i++)
            {
                double x = i % 2 == 0 ? -0.11 : 0.11;
                double y = i < 2 ? -0.11 : 0.11;
                czesci.Add(Bryla.Stozkowa(new W3(x, y, 0.06), new W3(x, y, 0.22), 0.022, 0.019, 0));
            }
            czesci.Add(Bryla.Prostopadloscian(0, 0, 0.235, 0.15, 0.15, 0.016, 0, 0.004));
            Bryla kopula = Bryla.Kula(0, 0, 0.25, 0.13, 2)
                                .Przetnij(Bryla.Prostopadloscian(0, 0, 0.35, 0.2, 0.2, 0.1, 2));
            czesci.Add(kopula);
            czesci.Add(Bryla.Kula(0, 0, 0.39, 0.018, 1));
            czesci.Add(Bryla.Stozkowa(new W3(0, 0, 0.39), new W3(0, 0, 0.46), 0.008, 0.002, 1));
            return Wielki(Bryla.Suma(czesci.ToArray()).Chropowata(0.003, 50, ziarno), mat,
                             new W3(-0.22, -0.22, -0.01), new W3(0.22, 0.22, 0.48));
        }

        // ---- jaskinia, leże ---------------------------------------------- //

        /// <summary>Skalny pagór z czarnym wejściem; na wierzchu mech.</summary>
        public static Szkic Jaskinia(int ziarno)
        {
            var mat = new[] { Skala, Ciemnosc };
            Bryla pagor = Bryla.Elipsoida(0, 0, 0.06, 0.26, 0.22, 0.20, 0)
                               .Chropowata(0.06, 6, ziarno).Chropowata(0.015, 22, ziarno + 1);
            Bryla wejscie = Bryla.Elipsoida(-0.02, 0.2, 0.06, 0.09, 0.12, 0.10, 1);
            Bryla wnetrze = Bryla.Elipsoida(-0.02, 0.12, 0.06, 0.085, 0.10, 0.095, 1);
            Bryla b = Bryla.Suma(pagor.Minus(wejscie), wnetrze.Przetnij(wejscie));
            return Wielki(b, mat, new W3(-0.34, -0.30, -0.01), new W3(0.34, 0.32, 0.32));
        }

        /// <summary>Leże smoka: wielka czerwona skała z grotą, kości i złoto u wejścia.</summary>
        public static Szkic LezeSmoka(int ziarno)
        {
            var mat = new[] { new Material(Materialy.SkalaCzerwona, 0.18, 8), Ciemnosc, Kosc, Mosiadz };
            Bryla skala = Bryla.Elipsoida(0, 0, 0.08, 0.34, 0.28, 0.28, 0)
                               .Chropowata(0.08, 5, ziarno).Chropowata(0.02, 18, ziarno + 1);
            Bryla wejscie = Bryla.Elipsoida(0, 0.26, 0.08, 0.13, 0.14, 0.14, 1);
            var czesci = new List<Bryla>
            {
                skala.Minus(wejscie),
                Bryla.Elipsoida(0, 0.16, 0.08, 0.12, 0.12, 0.13, 1).Przetnij(wejscie),
            };
            for (int i = 0; i < 5; i++)
            {
                double x = -0.15 + i * 0.07 + (Los(ziarno, i) - 0.5) * 0.04;
                double y = 0.34 + (Los(ziarno, 10 + i) - 0.5) * 0.06;
                if (i % 2 == 0)
                {
                    czesci.Add(Bryla.Kapsula(new W3(x - 0.03, y, 0.01), new W3(x + 0.03, y + 0.01, 0.012), 0.008, 2));
                }
                else
                {
                    czesci.Add(Bryla.Kula(x, y, 0.012, 0.022, 3).Chropowata(0.01, 60, ziarno + i));
                }
            }
            return Wielki(Bryla.Suma(czesci.ToArray()), mat,
                             new W3(-0.44, -0.38, -0.01), new W3(0.44, 0.44, 0.44));
        }

        // ---- boss, portal, wyspa ----------------------------------------- //

        /// <summary>Czarny obelisk z czerwonymi runami i czaszkami u podstawy.</summary>
        public static Szkic Obelisk(int ziarno)
        {
            var mat = new[] { new Material(Materialy.Kamien, 0.08, 20, -0.32), Runy, Kosc };
            var czesci = new List<Bryla>
            {
                Bryla.Prostopadloscian(0, 0, 0.025, 0.09, 0.09, 0.025, 0, 0.004),
                Bryla.Stozkowa(new W3(0, 0, 0.05), new W3(0, 0, 0.46), 0.05, 0.022, 0),
                Bryla.Stozek(0, 0, 0.45, 0.026, 0.06, 0),
            };
            for (int i = 0; i < 5; i++)
            {
                double z = 0.12 + i * 0.065;
                double r = 0.05 - (z - 0.05) / 0.41 * 0.028 + 0.002;
                czesci.Add(Bryla.Prostopadloscian(-r * 0.7, r * 0.7, z, 0.010, 0.010, 0.016, 1));
            }
            for (int i = 0; i < 3; i++)
            {
                double kat = i * 2.1 + 0.4;
                czesci.Add(Bryla.Kula(Math.Cos(kat) * 0.13, Math.Sin(kat) * 0.13, 0.02, 0.022, 2));
            }
            return Wielki(Bryla.Suma(czesci.ToArray()), mat,
                             new W3(-0.18, -0.18, -0.01), new W3(0.18, 0.18, 0.53));
        }

        /// <summary>Portal: kamienny łuk z błoną magii w środku.</summary>
        public static Szkic Portal(int ziarno)
        {
            var mat = new[] { new Material(Materialy.KamienMuru, 0.16, 24) { Nalot = Materialy.Listowie }, Magia };
            var czesci = new List<Bryla>
            {
                Bryla.Prostopadloscian(-0.13, 0, 0.17, 0.035, 0.04, 0.17, 0, 0.006).Chropowata(0.008, 30, ziarno),
                Bryla.Prostopadloscian(0.13, 0, 0.17, 0.035, 0.04, 0.17, 0, 0.006).Chropowata(0.008, 30, ziarno + 1),
                Bryla.Prostopadloscian(0, 0, 0.36, 0.19, 0.045, 0.03, 0, 0.006).Chropowata(0.008, 30, ziarno + 2),
                Bryla.Prostopadloscian(0, 0, 0.17, 0.095, 0.006, 0.165, 1).Chropowata(0.01, 16, ziarno + 3),
            };
            for (int i = 0; i < 4; i++)
            {
                double kat = i * 1.57 + 0.3;
                czesci.Add(Bryla.Elipsoida(Math.Cos(kat) * 0.24, Math.Sin(kat) * 0.2, 0.03, 0.03, 0.03, 0.05, 0)
                                .Chropowata(0.01, 30, ziarno + 10 + i));
            }
            return Wielki(Bryla.Suma(czesci.ToArray()).Obroc(0.6), mat,
                             new W3(-0.3, -0.3, -0.01), new W3(0.3, 0.3, 0.42));
        }

        /// <summary>Latająca wyspa: odwrócony stożek skały unoszący się nad ziemią, z drzewem na szczycie.</summary>
        public static Szkic LatajacaWyspa(int ziarno)
        {
            var mat = new[] { Skala, new Material(Materialy.Trawa, 0.2, 30), new Material(Materialy.Kora, 0.12, 30),
                              new Material(Materialy.Listowie, 0.22, 24) };
            const double z0 = 0.30;
            var czesci = new List<Bryla>
            {
                Bryla.Stozkowa(new W3(0, 0, z0 + 0.12), new W3(0.02, -0.01, z0 - 0.16), 0.20, 0.02, 0)
                     .Chropowata(0.04, 8, ziarno),
                Bryla.Elipsoida(0, 0, z0 + 0.12, 0.21, 0.19, 0.03, 1).Chropowata(0.01, 26, ziarno + 1),
                Bryla.Stozkowa(new W3(0.03, 0.02, z0 + 0.13), new W3(0.03, 0.02, z0 + 0.30), 0.02, 0.012, 2),
                Bryla.Zlep(0.04, Bryla.Kula(0.03, 0.02, z0 + 0.32, 0.08, 3), Bryla.Kula(-0.03, 0.03, z0 + 0.27, 0.06, 3),
                           Bryla.Kula(0.08, 0.0, z0 + 0.27, 0.055, 3)).Chropowata(0.025, 22, ziarno + 2),
            };
            return Wielki(Bryla.Suma(czesci.ToArray()), mat,
                             new W3(-0.26, -0.26, z0 - 0.2), new W3(0.26, 0.26, z0 + 0.43));
        }

        // ---- miasto ------------------------------------------------------ //

        /// <summary>Miasto za murami: blankowany mur z bramą, za nim dachy i wieża.</summary>
        public static Szkic Miasto(int ziarno)
        {
            var czesci = new List<Bryla>();
            // Domy za murem.
            czesci.Add(Chata(ziarno + 1, 0.9, out _, true).Przesun(-0.12, -0.10, 0));
            czesci.Add(Chata(ziarno + 2, 0.8, out _, true).Przesun(0.14, -0.14, 0));
            czesci.Add(Bryla.Prostopadloscian(0.02, -0.02, 0.17, 0.05, 0.05, 0.17, 0, 0.004)
                            .Chropowata(0.004, 30, ziarno));
            czesci.Add(Bryla.Stozek(0.02, -0.02, 0.34, 0.075, 0.12, 1));
            // Mur z blankami i brama.
            Bryla mur = Bryla.Prostopadloscian(0, 0.14, 0.08, 0.28, 0.03, 0.08, 0, 0.003);
            for (int i = -6; i <= 6; i += 2)
            {
                mur = mur.Minus(Bryla.Prostopadloscian(i * 0.022, 0.14, 0.165, 0.011, 0.05, 0.02, 0));
            }
            Bryla brama = Bryla.Prostopadloscian(0, 0.14, 0.04, 0.04, 0.05, 0.05, 5);
            czesci.Add(mur.Minus(brama));
            czesci.Add(Bryla.Prostopadloscian(0, 0.125, 0.04, 0.04, 0.012, 0.045, 5));
            Bryla b = Bryla.Suma(czesci.ToArray());
            // Domy dzielą materiały z murem: 0 = mur, 1 = dachówka, 2 = bale, 3 = łupek.
            return Wielki(b, new[] { Mur, Dachowka, Drzwi, Okno, Mur, Ciemnosc },
                             new W3(-0.36, -0.36, -0.01), new W3(0.36, 0.36, 0.50));
        }

        // ---- obóz -------------------------------------------------------- //

        /// <summary>Namiot z płótna na dwóch żerdziach, z ciemnym wejściem.</summary>
        public static Bryla Namiot(int ziarno, double skala)
        {
            var czesci = new List<Bryla>
            {
                Bryla.Dach(0, 0, 0, 0.12 * skala, 0.11 * skala, 0.15 * skala, 0).Chropowata(0.004, 30, ziarno),
                Bryla.Prostopadloscian(-0.12 * skala, 0.0, 0.05 * skala, 0.004, 0.035 * skala, 0.05 * skala, 1),
                Bryla.Kapsula(new W3(-0.13 * skala, 0, 0), new W3(-0.13 * skala, 0, 0.17 * skala), 0.006, 2),
                Bryla.Kapsula(new W3(0.13 * skala, 0, 0), new W3(0.13 * skala, 0, 0.17 * skala), 0.006, 2),
            };
            return Bryla.Suma(czesci.ToArray()).Obroc(1.2 + (Los(ziarno, 1) - 0.5) * 0.4);
        }

        /// <summary>Kamienny krąg ogniska z polanami — płomień dorysowuje się na żywo.</summary>
        public static Bryla Palenisko(double x, double y)
        {
            var czesci = new List<Bryla>();
            for (int i = 0; i < 7; i++)
            {
                double kat = i / 7.0 * Math.PI * 2;
                czesci.Add(Bryla.Elipsoida(x + Math.Cos(kat) * 0.05, y + Math.Sin(kat) * 0.05, 0.008,
                                           0.016, 0.016, 0.012, 3));
            }
            czesci.Add(Bryla.Kapsula(new W3(x - 0.03, y - 0.01, 0.01), new W3(x + 0.03, y + 0.01, 0.014), 0.008, 2));
            czesci.Add(Bryla.Kapsula(new W3(x - 0.01, y + 0.03, 0.012), new W3(x + 0.01, y - 0.03, 0.01), 0.008, 2));
            return Bryla.Suma(czesci.ToArray());
        }

        public static Szkic Oboz(int ziarno)
        {
            var mat = new[] { Plotno, new Material(Materialy.SukmanaSzara, 0.05, 20, -0.4), Belka,
                              new Material(Materialy.Kamien, 0.14, 30) };
            Bryla b = Bryla.Suma(Namiot(ziarno, 1.0).Przesun(-0.04, -0.06, 0), Palenisko(0.12, 0.12));
            return Wielki(b, mat, new W3(-0.24, -0.24, -0.01), new W3(0.24, 0.24, 0.22));
        }

        // ---- osada -------------------------------------------------------- //
        //
        // Osada nie jest jedną bryłą: chaty, pale palisady i wieża to osobne
        // obiekty ustawiane przez generator. Każdy ma własną głębię, więc
        // postać może stanąć między chatami, a gotowe sprite'y chat i pali
        // powtarzają się, zamiast liczyć całą osadę od nowa przy każdej zmianie.

        /// <summary>Zaostrzony pal palisady; wyższy z każdym poziomem palisady.</summary>
        public static Szkic Pal(int ziarno, int poziom)
        {
            double h = 0.10 + poziom * 0.035 + (Los(ziarno, 3) - 0.5) * 0.03;
            var b = Bryla.Suma(
                Bryla.Kapsula(new W3(0, 0, 0), new W3(0, 0, h), 0.013, 0).Chropowata(0.002, 60, ziarno),
                Bryla.Stozek(0, 0, h, 0.013, 0.03, 0));
            return new Szkic(b, new[] { Belka }, new W3(-0.03, -0.03, -0.01), new W3(0.03, 0.03, h + 0.05))
                .Skaluj(Powiekszenie);
        }

        /// <summary>Wieża strażnicza na czterech słupach, z pomostem i stożkowym daszkiem.</summary>
        public static Szkic Wieza(int poziom)
        {
            var mat = new[] { Belka, Strzecha, Bale };
            double hw = 0.42 + 0.08 * poziom;
            var czesci = new List<Bryla>();
            for (int i = 0; i < 4; i++)
            {
                double px = i % 2 == 0 ? -0.045 : 0.045;
                double py = i < 2 ? -0.045 : 0.045;
                czesci.Add(Bryla.Kapsula(new W3(px, py, 0), new W3(px * 0.9, py * 0.9, hw), 0.008, 0));
            }
            // Zastrzały między słupami — bez nich wieża wygląda jak stół.
            czesci.Add(Bryla.Kapsula(new W3(-0.045, 0.045, 0.05), new W3(0.042, 0.042, hw * 0.6), 0.005, 0));
            czesci.Add(Bryla.Kapsula(new W3(-0.045, -0.045, 0.05), new W3(-0.042, 0.042, hw * 0.6), 0.005, 0));
            czesci.Add(Bryla.Prostopadloscian(0, 0, hw, 0.065, 0.065, 0.012, 2, 0.002));
            czesci.Add(Bryla.Prostopadloscian(0, 0.062, hw + 0.03, 0.065, 0.004, 0.02, 2));
            czesci.Add(Bryla.Prostopadloscian(-0.062, 0, hw + 0.03, 0.004, 0.065, 0.02, 2));
            czesci.Add(Bryla.Stozek(0, 0, hw + 0.07, 0.085, 0.11, 1).Chropowata(0.004, 40, poziom));
            return new Szkic(Bryla.Suma(czesci.ToArray()), mat,
                             new W3(-0.1, -0.1, -0.01), new W3(0.1, 0.1, hw + 0.2)).Skaluj(Powiekszenie);
        }
    }
}
