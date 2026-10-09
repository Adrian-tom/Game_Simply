using System;
using System.Collections.Generic;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>Model obiektu przed „sfotografowaniem”: bryła, materiały i obrys.</summary>
    public sealed class Szkic
    {
        public Bryla Bryla;
        public Material[] Materialy;
        public W3 Min;
        public W3 Max;

        public Szkic(Bryla bryla, Material[] materialy, W3 min, W3 max)
        {
            Bryla = bryla;
            Materialy = materialy;
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Ten sam model w innej skali. Faktury liczone są we współrzędnych
        /// świata, więc większy budynek dostaje więcej desek i dachówek,
        /// a nie grubsze — gęstość detalu na piksel zostaje ta sama.
        /// </summary>
        public Szkic Skaluj(double k)
        {
            return new Szkic(Bryla.Skaluj(k), Materialy, Min * k, Max * k);
        }

        public SpriteBryly Zrob(int ziarno)
        {
            return Fotograf.Zrob(Bryla, Materialy, Min, Max, ziarno);
        }
    }

    /// <summary>
    /// Modele roślinności i skał. Jednostką jest bok pola; podstawa obiektu leży
    /// w (0, 0, 0). Każdy model ma wariant — ziarno zmienia proporcje, pochylenie
    /// i układ gałęzi, więc las nie składa się z identycznych drzew.
    /// </summary>
    public static class Szkice
    {
        // Wspólne materiały roślin i skał.
        private static Material Kora => new Material(Materialy.Kora, 0.16, 34);
        private static Material Listowie => new Material(Materialy.Listowie, 0.22, 26);
        private static Material Igliwie => new Material(Materialy.Igliwie, 0.20, 30);
        private static Material KamienM => new Material(Materialy.Kamien, 0.14, 11);

        private static double Los(int ziarno, int i) => Piksele.Szum(ziarno, i, 977);

        // ------------------------------------------------------------------ //
        //  Drzewa                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>Sosna: zwężający się pień i piętra opadających gałęzi z igliwiem.</summary>
        public static Szkic Sosna(int ziarno)
        {
            double h = 0.78 + Los(ziarno, 1) * 0.36;
            double r0 = 0.17 + Los(ziarno, 2) * 0.06;
            var czesci = new List<Bryla>
            {
                Bryla.Stozkowa(new W3(0, 0, 0), new W3(0, 0, h * 0.92), 0.032, 0.010, 0),
            };
            int pietra = 5;
            for (int i = 0; i < pietra; i++)
            {
                double t = i / (double)(pietra - 1);
                double z = h * (0.24 + t * 0.62);
                double r = r0 * (1.0 - t * 0.78);
                double wys = h * (0.22 - t * 0.06);
                // Lekkie przesunięcie pięter — pień nie jest idealną osią.
                double dx = (Los(ziarno, 10 + i) - 0.5) * 0.025;
                double dy = (Los(ziarno, 20 + i) - 0.5) * 0.025;
                czesci.Add(Bryla.Stozek(dx, dy, z - wys * 0.35, r, wys, 1));
            }
            Bryla korona = Bryla.Zlep(0.025, czesci.GetRange(1, czesci.Count - 1).ToArray())
                                .Chropowata(0.035, 28, ziarno);
            Bryla calosc = Bryla.Suma(czesci[0], korona);
            return new Szkic(calosc, new[] { Kora, Igliwie },
                             new W3(-r0 - 0.06, -r0 - 0.06, 0), new W3(r0 + 0.06, r0 + 0.06, h + 0.04));
        }

        /// <summary>Drzewo liściaste: pień rozwidlony w konary, korona z kęp.</summary>
        public static Szkic Dab(int ziarno)
        {
            double h = 0.62 + Los(ziarno, 1) * 0.22;
            double pien = h * 0.48;
            var drewno = new List<Bryla>
            {
                Bryla.Stozkowa(new W3(0, 0, -0.02), new W3(0, 0, pien), 0.055, 0.034, 0),
            };
            var kepy = new List<Bryla>();
            int konary = 3 + (int)(Los(ziarno, 2) * 2);
            for (int i = 0; i < konary; i++)
            {
                double kat = i / (double)konary * Math.PI * 2 + Los(ziarno, 30 + i);
                double dl = 0.13 + Los(ziarno, 40 + i) * 0.07;
                var koniec = new W3(Math.Cos(kat) * dl, Math.Sin(kat) * dl, pien + 0.10 + Los(ziarno, 50 + i) * 0.1);
                drewno.Add(Bryla.Stozkowa(new W3(0, 0, pien - 0.04), koniec, 0.030, 0.012, 0));
                kepy.Add(Bryla.Kula(koniec.X * 1.15, koniec.Y * 1.15, koniec.Z + 0.06,
                                    0.13 + Los(ziarno, 60 + i) * 0.05, 1));
            }
            kepy.Add(Bryla.Kula(0, 0, h - 0.12, 0.17, 1));
            kepy.Add(Bryla.Kula(0.04, -0.03, pien + 0.12, 0.15, 1));
            Bryla korona = Bryla.Zlep(0.07, kepy.ToArray()).Chropowata(0.05, 20, ziarno);
            Bryla calosc = Bryla.Suma(Bryla.Zlep(0.02, drewno.ToArray()), korona);
            double zasieg = 0.36;
            return new Szkic(calosc, new[] { Kora, Listowie },
                             new W3(-zasieg, -zasieg, -0.02), new W3(zasieg, zasieg, h + 0.08));
        }

        /// <summary>
        /// Martwe drzewo: poskręcany pień i nagie gałęzie rozwidlone dwa razy.
        /// Sygnatura pustkowi — w fantasy to drzewo z przeklętej ziemi.
        /// </summary>
        public static Szkic MartweDrzewo(int ziarno)
        {
            double h = 0.55 + Los(ziarno, 1) * 0.25;
            var czesci = new List<Bryla>();
            var pochyl = new W3((Los(ziarno, 2) - 0.5) * 0.12, (Los(ziarno, 3) - 0.5) * 0.12, h * 0.55);
            czesci.Add(Bryla.Stozkowa(new W3(0, 0, -0.02), pochyl, 0.045, 0.026, 0));
            int galezie = 3 + (int)(Los(ziarno, 4) * 2);
            for (int i = 0; i < galezie; i++)
            {
                double kat = i / (double)galezie * Math.PI * 2 + Los(ziarno, 10 + i) * 1.2;
                double dl = 0.14 + Los(ziarno, 20 + i) * 0.12;
                var a = pochyl * (0.7 + Los(ziarno, 30 + i) * 0.3);
                var b = a + new W3(Math.Cos(kat) * dl, Math.Sin(kat) * dl, 0.10 + Los(ziarno, 40 + i) * 0.16);
                czesci.Add(Bryla.Stozkowa(a, b, 0.020, 0.008, 0));
                for (int j = 0; j < 2; j++)
                {
                    double k2 = kat + (j == 0 ? -0.7 : 0.7) + (Los(ziarno, 50 + i * 2 + j) - 0.5) * 0.6;
                    double d2 = dl * 0.55;
                    var c = b + new W3(Math.Cos(k2) * d2, Math.Sin(k2) * d2, 0.06 + Los(ziarno, 60 + i) * 0.08);
                    czesci.Add(Bryla.Stozkowa(b, c, 0.008, 0.003, 0));
                }
            }
            Bryla calosc = Bryla.Zlep(0.012, czesci.ToArray()).Chropowata(0.006, 60, ziarno);
            return new Szkic(calosc, new[] { new Material(Materialy.Kora, 0.10, 50, 0.08) },
                             new W3(-0.42, -0.42, -0.02), new W3(0.42, 0.42, h + 0.32));
        }

        /// <summary>Krzak: kilka spłaszczonych kęp przy ziemi.</summary>
        public static Szkic Krzak(int ziarno)
        {
            var kepy = new List<Bryla>();
            int n = 3 + (int)(Los(ziarno, 1) * 3);
            for (int i = 0; i < n; i++)
            {
                double kat = i * 2.4 + Los(ziarno, 10 + i);
                double r = 0.05 + Los(ziarno, 20 + i) * 0.06;
                kepy.Add(Bryla.Elipsoida(Math.Cos(kat) * r, Math.Sin(kat) * r, 0.04,
                                         0.08, 0.08, 0.06, 0));
            }
            Bryla b = Bryla.Zlep(0.04, kepy.ToArray()).Chropowata(0.03, 30, ziarno);
            return new Szkic(b, new[] { new Material(Materialy.Listowie, 0.24, 30, -0.08) },
                             new W3(-0.2, -0.2, -0.02), new W3(0.2, 0.2, 0.16));
        }

        /// <summary>Kępa trzciny: smukłe, pochylone łodygi z kolbami.</summary>
        public static Szkic Trzcina(int ziarno)
        {
            var czesci = new List<Bryla>();
            int n = 6 + (int)(Los(ziarno, 1) * 4);
            for (int i = 0; i < n; i++)
            {
                double x = (Los(ziarno, 10 + i) - 0.5) * 0.14;
                double y = (Los(ziarno, 20 + i) - 0.5) * 0.14;
                double h = 0.16 + Los(ziarno, 30 + i) * 0.14;
                var gora = new W3(x + (Los(ziarno, 40 + i) - 0.5) * 0.06, y, h);
                czesci.Add(Bryla.Stozkowa(new W3(x, y, 0), gora, 0.006, 0.003, 0));
                if (i % 2 == 0)
                {
                    czesci.Add(Bryla.Elipsoida(gora.X, gora.Y, gora.Z - 0.02, 0.008, 0.008, 0.022, 1));
                }
            }
            return new Szkic(Bryla.Suma(czesci.ToArray()),
                             new[] { new Material(Materialy.Trawa, 0.1, 40), new Material(Materialy.Kora, 0.1, 40) },
                             new W3(-0.14, -0.14, 0), new W3(0.14, 0.14, 0.34));
        }

        // ------------------------------------------------------------------ //
        //  Skały                                                              //
        // ------------------------------------------------------------------ //

        /// <summary>Głaz: zgnieciona elipsoida, poszarpana w dwóch skalach, z mchem na wierzchu.</summary>
        public static Szkic Glaz(int ziarno, double skala = 1.0, Barwa[] rampa = null)
        {
            double rx = (0.12 + Los(ziarno, 1) * 0.07) * skala;
            double ry = (0.10 + Los(ziarno, 2) * 0.06) * skala;
            double rz = (0.07 + Los(ziarno, 3) * 0.06) * skala;
            Bryla b = Bryla.Elipsoida(0, 0, rz * 0.55, rx, ry, rz, 0)
                           .Chropowata(0.06 * skala, 7 / skala, ziarno)
                           .Chropowata(0.015 * skala, 26 / skala, ziarno + 3);
            var kamien = new Material(rampa ?? Materialy.Kamien, 0.16, 14);
            return new Szkic(b, new[] { kamien },
                             new W3(-rx - 0.05 * skala, -ry - 0.05 * skala, -0.01),
                             new W3(rx + 0.05 * skala, ry + 0.05 * skala, rz * 1.6 + 0.04));
        }

        /// <summary>Iglica skalna kanionu: wysoki, warstwowany słup ochry.</summary>
        public static Szkic Iglica(int ziarno)
        {
            double h = 0.32 + Los(ziarno, 1) * 0.26;
            var czesci = new List<Bryla>();
            int warstwy = 4;
            for (int i = 0; i < warstwy; i++)
            {
                double z = h * i / warstwy;
                double r = 0.13 - i * 0.022 + Los(ziarno, 10 + i) * 0.02;
                czesci.Add(Bryla.Elipsoida((Los(ziarno, 20 + i) - 0.5) * 0.03, 0, z + h / warstwy * 0.5,
                                           r, r * 0.85, h / warstwy * 0.62, 0));
            }
            Bryla b = Bryla.Zlep(0.02, czesci.ToArray()).Chropowata(0.03, 12, ziarno);
            return new Szkic(b, new[] { new Material(Materialy.SkalaCzerwona, 0.16, 16) },
                             new W3(-0.2, -0.2, -0.01), new W3(0.2, 0.2, h + 0.08));
        }

        /// <summary>Złamana kolumna dawnej świątyni, z bazą i odłamkiem u stóp.</summary>
        public static Szkic Kolumna(int ziarno)
        {
            double h = 0.22 + Los(ziarno, 1) * 0.3;
            Bryla trzon = Bryla.Stozkowa(new W3(0, 0, 0.04), new W3(0, 0, h), 0.055, 0.050, 0);
            // Złamanie: odcięcie skośną płaszczyzną (przybliżone obróconym prostopadłościanem).
            Bryla ciecie = Bryla.Prostopadloscian(0, 0, h + 0.05, 0.2, 0.2, 0.06, 0).Obroc(Los(ziarno, 2) * 3);
            Bryla kolumna = trzon.Minus(ciecie.Przesun(0, 0, -0.02)).Chropowata(0.006, 40, ziarno);
            Bryla baza = Bryla.Prostopadloscian(0, 0, 0.022, 0.08, 0.08, 0.022, 0, 0.006);
            Bryla odlamek = Bryla.Stozkowa(new W3(0.10, 0.06, 0.03), new W3(0.20, 0.10, 0.035), 0.03, 0.028, 0)
                                 .Chropowata(0.006, 40, ziarno + 1);
            return new Szkic(Bryla.Suma(kolumna, baza, odlamek),
                             new[] { new Material(Materialy.KamienMuru, 0.10, 24) },
                             new W3(-0.12, -0.12, -0.01), new W3(0.26, 0.18, h + 0.04));
        }

        /// <summary>Fragment muru z wyszczerbionym szczytem.</summary>
        public static Szkic Mur(int ziarno)
        {
            double dl = 0.2 + Los(ziarno, 1) * 0.14;
            double h = 0.14 + Los(ziarno, 2) * 0.16;
            Bryla sciana = Bryla.Prostopadloscian(0, 0, h / 2, dl, 0.035, h / 2, 0, 0.004);
            Bryla szczerba = Bryla.Elipsoida(dl * 0.3, 0, h, dl * 0.5, 0.1, h * 0.45, 0)
                                  .Chropowata(0.03, 14, ziarno);
            Bryla mur = sciana.Minus(szczerba).Chropowata(0.006, 34, ziarno + 2).Obroc(Los(ziarno, 3) * Math.PI);
            return new Szkic(mur, new[] { new Material(Materialy.KamienMuru, 0.14, 30) },
                             new W3(-dl - 0.05, -dl - 0.05, -0.01), new W3(dl + 0.05, dl + 0.05, h + 0.03));
        }
    }
}
