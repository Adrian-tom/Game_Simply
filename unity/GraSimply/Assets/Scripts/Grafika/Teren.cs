using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Grafika
{
    /// <summary>Jedna dekoracja na polu: sprite (null = ognisko rysowane na żywo) i przesunięcie.</summary>
    public struct Dekoracja
    {
        public Plotno Sprite;
        public int Dx;
        public int Dy;

        public Dekoracja(Plotno sprite, int dx, int dy)
        {
            Sprite = sprite;
            Dx = dx;
            Dy = dy;
        }
    }

    /// <summary>Kafle izometryczne, dekoracje biomów i budynki punktów na mapie.</summary>
    public static class Teren
    {
        /// <summary>Romb kafla.</summary>
        public const int TW = 32;
        public const int TH = 16;

        public static readonly Dictionary<string, Barwa[]> Rampy = new Dictionary<string, Barwa[]>
        {
            { "równiny", new[] { new Barwa(46, 78, 42), new Barwa(70, 112, 50), new Barwa(104, 148, 60), new Barwa(146, 180, 78) } },
            { "las", new[] { new Barwa(26, 50, 36), new Barwa(38, 72, 44), new Barwa(56, 98, 52), new Barwa(82, 128, 62) } },
            { "bagna", new[] { new Barwa(30, 48, 50), new Barwa(42, 66, 62), new Barwa(58, 88, 74), new Barwa(84, 114, 86) } },
            { "wzgórza", new[] { new Barwa(66, 76, 60), new Barwa(94, 106, 76), new Barwa(124, 136, 92), new Barwa(160, 166, 116) } },
            { "kanion", new[] { new Barwa(112, 58, 40), new Barwa(152, 86, 50), new Barwa(190, 120, 70), new Barwa(222, 164, 102) } },
            { "ruiny", new[] { new Barwa(66, 64, 58), new Barwa(92, 88, 78), new Barwa(120, 114, 100), new Barwa(152, 146, 126) } },
        };

        private static readonly Dictionary<string, int> WysokosciBiomow = new Dictionary<string, int>
        {
            { "równiny", 4 }, { "las", 5 }, { "bagna", 1 },
            { "wzgórza", 11 }, { "kanion", 8 }, { "ruiny", 4 },
        };

        public static readonly Barwa[] Ziemia =
        {
            new Barwa(48, 34, 28), new Barwa(74, 52, 36), new Barwa(100, 72, 46), new Barwa(124, 92, 58),
        };

        public static readonly Barwa[] Kamien =
        {
            new Barwa(52, 54, 60), new Barwa(80, 82, 88), new Barwa(110, 112, 116), new Barwa(146, 146, 146),
        };

        public static readonly Barwa[] Czerwien =
        {
            new Barwa(90, 44, 34), new Barwa(128, 64, 42), new Barwa(164, 92, 56), new Barwa(196, 128, 80),
        };

        public static readonly Barwa[] Woda =
        {
            new Barwa(22, 44, 58), new Barwa(32, 66, 82), new Barwa(52, 96, 110), new Barwa(120, 170, 176),
        };

        public static Barwa[] RampaBiomu(string biom)
        {
            return Rampy.TryGetValue(biom ?? "", out Barwa[] r) ? r : Rampy["równiny"];
        }

        public static int WysokoscPola(string biom, int x, int y, int s)
        {
            int baza = WysokosciBiomow.TryGetValue(biom ?? "", out int h) ? h : 4;
            return baza + (int)(Piksele.Szum(x, y, s + 99) * 3);
        }

        public static bool WRombie(int px, int py)
        {
            return Math.Abs(px + 0.5 - TW / 2.0) / (TW / 2.0)
                   + Math.Abs(py + 0.5 - TH / 2.0) / (TH / 2.0) <= 1.0;
        }

        /// <summary>Blok terenu: wierzch z fakturą biomu + boki w warstwach skały.</summary>
        public static Plotno Kafel(string biom, int h, int s, int klatka = 0)
        {
            int grubosc = h + 6;
            var p = new Plotno(TW, TH + grubosc);
            Barwa[] rampa = RampaBiomu(biom);
            Barwa[] boki = biom == "wzgórza" || biom == "ruiny" ? Kamien
                : biom == "kanion" ? Czerwien
                : Ziemia;

            for (int px = 0; px < TW; px++)
            {
                int gora = px < 16 ? 8 + px / 2 : 16 - (px - 16) / 2;
                for (int d = 0; d < grubosc; d++)
                {
                    int y = gora + d;
                    if (y >= p.Wysokosc)
                    {
                        break;
                    }
                    // lewy bok w świetle, prawy w cieniu
                    double baza = px < 16 ? 0.62 : 0.3;
                    double warstwa = Piksele.SzumGladki(px * 0.2, y, 3.0, s + 7);
                    double v = baza + (warstwa - 0.5) * 0.35 + (Piksele.Szum(px, y, s) - 0.5) * 0.15;
                    if ((y + px / 7) % 5 == 0)
                    {
                        v -= 0.2;
                    }
                    Barwa kolor = Piksele.ZRampy(boki, v, px, y);
                    if (d < 2 && (biom == "równiny" || biom == "las" || biom == "wzgórza"))
                    {
                        // darń zwisająca nad krawędzią
                        kolor = rampa[px < 16 ? 1 : 0];
                    }
                    p.Ustaw(px, y, kolor);
                }
            }

            for (int py = 0; py < TH; py++)
            {
                for (int px = 0; px < TW; px++)
                {
                    if (!WRombie(px, py))
                    {
                        continue;
                    }
                    int gx = px + s * 31;
                    int gy = py * 2 + s * 17;
                    double v = 0.5 + (Piksele.SzumGladki(gx, gy, 4.0, s) - 0.5) * 0.7
                               + (Piksele.Szum(px, py, s) - 0.5) * 0.2;
                    Barwa kolor = Piksele.ZRampy(rampa, v, px, py);
                    if (biom == "bagna" && Piksele.SzumGladki(gx, gy, 5.0, s + 3) < 0.55)
                    {
                        bool fala = (px + py * 2 + klatka * 3) % 11 == 0
                                    && Piksele.Szum(px, py, s + klatka) > 0.6;
                        kolor = fala
                            ? Woda[3]
                            : Piksele.ZRampy(Woda, 0.35 + (Piksele.Szum(px, py, s + 9) - 0.5) * 0.3, px, py);
                    }
                    else if (biom == "równiny" && Piksele.Szum(px, py, s + 5) > 0.975)
                    {
                        kolor = Piksele.Szum(px, py, s + 6) > 0.5
                            ? new Barwa(226, 208, 96)
                            : new Barwa(214, 120, 150);
                    }
                    if (!WRombie(px, py - 1) || (px < 16 && !WRombie(px - 1, py)))
                    {
                        kolor = Piksele.Jasniej(kolor);
                    }
                    else if (!WRombie(px, py + 1))
                    {
                        kolor = Piksele.Ciemniej(kolor, 0.8);
                    }
                    p.Ustaw(px, py, kolor);
                }
            }
            return p;
        }

        /// <summary>Nieodkryte pole: przyciemniony blok z rzadką mgiełką na wierzchu.</summary>
        public static Plotno Zamglij(Plotno kafel, int x, int y, bool noc)
        {
            Plotno m = kafel.Kopia();
            m.PomnozPrzez(noc ? new Barwa(70, 66, 96, 255) : new Barwa(128, 136, 158, 255));
            Barwa kol = noc ? new Barwa(52, 50, 74) : new Barwa(150, 158, 180);
            for (int py = 0; py < TH; py++)
            {
                for (int px = 0; px < TW; px++)
                {
                    if (WRombie(px, py)
                        && Piksele.SzumGladki(px + x * 32, py * 2 + y * 16, 6, 5) > 0.6
                        && Piksele.Bayer[py % 4][px % 4] < 3)
                    {
                        m.Ustaw(px, py, kol);
                    }
                }
            }
            return m;
        }

        /// <summary>Romb podświetlający pole gracza.</summary>
        public static Plotno Obwodka(Barwa? kolor = null)
        {
            Barwa k = kolor ?? new Barwa(236, 200, 110);
            var p = new Plotno(TW, TH);
            for (int py = 0; py < TH; py++)
            {
                for (int px = 0; px < TW; px++)
                {
                    bool wnetrze = WRombie(px - 1, py) && WRombie(px + 1, py)
                                   && WRombie(px, py - 1) && WRombie(px, py + 1);
                    if (WRombie(px, py) && !wnetrze && (px + py) % 2 == 0)
                    {
                        p.Ustaw(px, py, k);
                    }
                }
            }
            return p;
        }

        // ------------------------------------------------------------------ //
        //  Roślinność i skały                                                 //
        // ------------------------------------------------------------------ //

        public static Plotno Sosna(int wys, int s)
        {
            int szer = wys / 2 + 3;
            var p = new Plotno(szer * 2 + 1, wys + 3);
            int cx = szer;
            p.Wypelnij(new Barwa(70, 46, 32), cx - 1, wys - 3, 2, 5);
            p.Ustaw(cx, wys - 2, new Barwa(48, 30, 24));
            Barwa[] rampa = Rampy["las"];
            for (int i = 0; i < 3; i++)
            {
                int top = (int)(i * wys * 0.26);
                int dol = top + (int)(wys * 0.46);
                for (int y = top; y < Math.Min(dol, wys - 1); y++)
                {
                    double t = (y - top) / (double)Math.Max(1, dol - top);
                    int pol = (int)(1 + t * (szer - 1) * (0.7 + 0.3 * (i + 1) / 3.0));
                    for (int x = cx - pol; x <= cx + pol; x++)
                    {
                        double nx = (x - cx) / (pol + 0.5);
                        double v = 0.62 - 0.55 * nx - t * 0.25 + (Piksele.Szum(x, y, s) - 0.5) * 0.35;
                        p.Ustaw(x, y, Piksele.ZRampy(rampa, v, x, y));
                    }
                }
            }
            return Piksele.Obrys(p);
        }

        public static Plotno DrzewoLisciaste(int s)
        {
            var p = new Plotno(17, 20);
            p.Wypelnij(new Barwa(84, 58, 38), 7, 11, 2, 9);
            p.Wypelnij(new Barwa(60, 40, 28), 8, 11, 1, 9);
            var rampa = new[]
            {
                new Barwa(38, 70, 40), new Barwa(62, 104, 48),
                new Barwa(98, 142, 56), new Barwa(150, 184, 80),
            };
            p.Nalóż(Piksele.Kula(7, 6, rampa, s, 0.3), 1, 0);
            return Piksele.Obrys(p);
        }

        public static Plotno Skala(int rx, int ry, int s, Barwa[] rampa = null)
        {
            return Piksele.Obrys(Piksele.Kula(rx, ry, rampa ?? Kamien, s, 0.25));
        }

        public static Plotno Trzcina(int s)
        {
            var p = new Plotno(7, 8);
            for (int i = 0; i < 4; i++)
            {
                int x = (int)(Piksele.Szum(i, 0, s) * 6);
                int h = 3 + (int)(Piksele.Szum(i, 1, s) * 5);
                for (int y = 8 - h; y < 8; y++)
                {
                    p.Ustaw(x, y, y > 8 - h + 1 ? new Barwa(96, 120, 58) : new Barwa(132, 96, 52));
                }
            }
            return p;
        }

        public static Plotno Kolumna(int s, int wys)
        {
            var p = new Plotno(7, wys + 2);
            for (int y = 1; y < wys + 2; y++)
            {
                for (int x = 1; x < 6; x++)
                {
                    double v = 0.9 - (x - 1) * 0.2 + (Piksele.Szum(x, y, s) - 0.5) * 0.3;
                    p.Ustaw(x, y, Piksele.ZRampy(Kamien, v, x, y));
                }
            }
            p.Wypelnij(Kamien[3], 0, 1, 7, 1);
            for (int x = 1; x < 6; x++)
            {
                // złamany szczyt
                if (Piksele.Szum(x, 0, s) > 0.5)
                {
                    p.Ustaw(x, 0, Kamien[2]);
                }
            }
            return Piksele.Obrys(p);
        }

        // ------------------------------------------------------------------ //
        //  Budynki i punkty                                                   //
        // ------------------------------------------------------------------ //

        public static readonly Barwa[] Tynk =
        {
            new Barwa(120, 104, 86), new Barwa(164, 146, 120),
            new Barwa(206, 190, 160), new Barwa(232, 222, 196),
        };

        public static readonly Barwa[] Drewno =
        {
            new Barwa(70, 46, 32), new Barwa(104, 70, 44),
            new Barwa(140, 98, 60), new Barwa(170, 126, 80),
        };

        public static readonly Barwa[] Dachowka =
        {
            new Barwa(80, 34, 30), new Barwa(120, 52, 38),
            new Barwa(160, 74, 48), new Barwa(196, 104, 66),
        };

        public static readonly Barwa[] Lupek =
        {
            new Barwa(40, 46, 60), new Barwa(58, 66, 84),
            new Barwa(80, 90, 110), new Barwa(110, 120, 138),
        };

        public static readonly Barwa[] Zloto =
        {
            new Barwa(150, 120, 60), new Barwa(196, 160, 80),
            new Barwa(228, 196, 110), new Barwa(250, 230, 160),
        };

        public static Plotno Namiot()
        {
            var p = new Plotno(30, 24);
            var plotno = new[]
            {
                new Barwa(96, 70, 46), new Barwa(140, 106, 66),
                new Barwa(184, 148, 96), new Barwa(216, 190, 136),
            };
            Piksele.Wypelnij(p, new[] { (2, 18), (13, 23), (13, 4) }, plotno, 0.75, 3, "deski", 2, 18);
            Piksele.Wypelnij(p, new[] { (13, 23), (26, 16), (15, 2), (13, 4) }, plotno, 0.3, 4, "deski", 13, 23);
            p.Wypelnij(new Barwa(34, 24, 20), 9, 15, 3, 6);
            p.Odcinek(new Barwa(80, 56, 40), 13, 4, 15, 2);
            return Piksele.Obrys(p);
        }

        public static Plotno Budynek(string rodzaj, int s)
        {
            var p = new Plotno(34, 38);
            if (rodzaj == "karczma")
            {
                Piksele.Domek(p, 16, 30, 11, 9, Drewno, Dachowka, s, "deski",
                              okno: new Barwa(255, 206, 110));
                p.Wypelnij(new Barwa(210, 170, 80), 27, 19, 3, 3); // szyld
            }
            else if (rodzaj == "kuźnia")
            {
                Piksele.Domek(p, 16, 30, 10, 8, Kamien, Lupek, s, "cegla",
                              okno: new Barwa(255, 140, 50), dachH: 6);
                Piksele.Wypelnij(p, new[] { (21, 8), (24, 9), (24, 20), (21, 19) },
                                 Kamien, 0.5, s + 9, "cegla", 21, 8);
            }
            else if (rodzaj == "świątynia")
            {
                Piksele.Domek(p, 16, 31, 10, 11, Tynk, Zloto, s, "cegla", dachH: 4);
                p.Nalóż(Piksele.Kula(5, 5, Tynk, s, 0.05), 11, 6);
                p.Wypelnij(new Barwa(240, 210, 110), 16, 3, 1, 4);
                p.Wypelnij(new Barwa(240, 210, 110), 15, 4, 3, 1);
            }
            else if (rodzaj == "miasto")
            {
                Piksele.Domek(p, 10, 32, 7, 7, Tynk, Dachowka, s, "deski",
                              okno: new Barwa(255, 206, 110));
                Piksele.Domek(p, 23, 33, 7, 5, Drewno, Dachowka, s + 5, "deski");
                Piksele.Domek(p, 17, 26, 5, 14, Kamien, Lupek, s + 9, "cegla", dachH: 8);
            }
            return Piksele.Obrys(p);
        }

        /// <summary>
        /// Obóz, który rośnie razem z osadą: namiot, chaty, palisada, wieża.
        /// Punkt zaczepienia = środek pola to (26, 30) na zwróconym płótnie.
        /// </summary>
        public static Plotno OsadaObozu(int chaty, int palisada, int wieza)
        {
            var p = new Plotno(52, 44);
            const int cx = 26;
            const int cy = 30;
            var miejsca = new[]
            {
                (-13, -3), (13, -3), (-19, 2), (19, 2), (-8, 5), (9, 6), (-2, -7), (4, -8),
            };
            var elementy = new List<(int Dy, string Rodzaj, int Dx, int I)>();
            for (int i = 0; i < Math.Min(chaty, miejsca.Length); i++)
            {
                (int dx, int dy) = miejsca[i];
                elementy.Add((dy, "chata", dx, i));
            }
            elementy.Add((0, "namiot", 0, 0));
            if (wieza > 0)
            {
                elementy.Add((-6, "wieza", -20, 0));
            }
            // Python sortuje krotki, więc porządek jest po dy, potem po nazwie rodzaju.
            foreach ((int dy, string rodzaj, int dx, int i) in
                     elementy.OrderBy(e => e.Dy).ThenBy(e => e.Rodzaj, StringComparer.Ordinal)
                             .ThenBy(e => e.Dx).ThenBy(e => e.I))
            {
                if (rodzaj == "chata")
                {
                    var chatka = new Plotno(14, 16);
                    Piksele.Domek(chatka, 7, 13, 5, 4, Drewno,
                                  i % 3 != 0 ? Dachowka : Zloto, 40 + i, "deski", dachH: 4);
                    p.Nalóż(Piksele.Obrys(chatka), cx + dx - 7, cy + dy - 13);
                }
                else if (rodzaj == "namiot")
                {
                    Plotno n = Namiot();
                    p.Nalóż(n, cx - n.Szerokosc / 2 - 2, cy + 4 - n.Wysokosc);
                }
                else
                {
                    var wiez = new Plotno(12, 26);
                    Piksele.Domek(wiez, 6, 23, 4, 14 + 2 * wieza, Kamien, Lupek, 77, "cegla", dachH: 5);
                    p.Nalóż(Piksele.Obrys(wiez), cx + dx - 6, cy + dy - 23);
                }
            }
            if (palisada > 0)
            {
                for (int i = 0; i < 22; i++)
                {
                    double kat = i / 22.0 * (2 * Math.PI);
                    int px = cx + (int)(Math.Cos(kat) * 24);
                    int py = cy + (int)(Math.Sin(kat) * 11);
                    int wys = 4 + palisada;
                    if (0.15 * Math.PI < kat && kat < 0.85 * Math.PI && i % 5 == 0)
                    {
                        continue; // brama od frontu
                    }
                    p.Wypelnij(new Barwa(58, 38, 26), px - 1, py - wys, 2, wys);
                    p.Ustaw(px, py - wys, new Barwa(120, 86, 52));
                }
            }
            return p;
        }

        public static Plotno Jaskinia(int s)
        {
            var p = new Plotno(30, 20);
            p.Nalóż(Piksele.Kula(14, 9, Kamien, s, 0.3), 0, 1);
            p.Elipsa(new Barwa(14, 12, 18), 9, 10, 8, 10);
            p.Elipsa(new Barwa(30, 26, 34), 10, 11, 6, 8, grubosc: 1);
            return Piksele.Obrys(p);
        }

        public static Plotno Obelisk(Barwa kolor)
        {
            var p = new Plotno(12, 24);
            var rampa = new[]
            {
                new Barwa(26, 22, 34), new Barwa(44, 38, 56),
                new Barwa(66, 58, 82), new Barwa(96, 86, 114),
            };
            Piksele.Wypelnij(p, new[] { (3, 22), (6, 23), (6, 2), (5, 1) }, rampa, 0.7, 2);
            Piksele.Wypelnij(p, new[] { (6, 23), (9, 22), (7, 2), (6, 2) }, rampa, 0.25, 3);
            p.Wypelnij(kolor, 5, 9, 2, 3);
            return Piksele.Obrys(p);
        }

        public static Plotno KragKamieni()
        {
            var p = new Plotno(26, 16);
            for (int i = 0; i < 8; i++)
            {
                double a = i / 8.0 * (2 * Math.PI);
                double x = 13 + Math.Cos(a) * 10;
                double y = 8 + Math.Sin(a) * 5;
                p.Nalóż(Skala(2, 2, i), (int)x - 2, (int)y - 3);
            }
            return p;
        }

        public static Plotno LezeSmoka(int s)
        {
            var p = new Plotno(32, 22);
            p.Nalóż(Piksele.Kula(15, 10, Czerwien, s, 0.3), 0, 1);
            for (int i = 0; i < 5; i++)
            {
                // kości i złoto u wejścia
                p.Ustaw(8 + i * 4, 18 + i % 2,
                        i % 2 != 0 ? new Barwa(230, 222, 200) : new Barwa(240, 200, 90));
            }
            p.Elipsa(new Barwa(20, 10, 10), 11, 9, 10, 11);
            return Piksele.Obrys(p);
        }

        public static Plotno LatajacaWyspa(int s)
        {
            var p = new Plotno(26, 30);
            Piksele.Wypelnij(p, new[] { (1, 12), (25, 12), (14, 28), (11, 28) }, Kamien, 0.45, s);
            p.Nalóż(Piksele.Kula(12, 4, Rampy["równiny"], s, 0.2), 1, 8);
            p.Nalóż(DrzewoLisciaste(s), 5, -6);
            return Piksele.Obrys(p);
        }

        public static Plotno SpritePunktu(string punkt, int s)
        {
            switch (punkt)
            {
                case "obóz":
                    return Namiot();
                case "karczma":
                case "kuźnia":
                case "świątynia":
                case "miasto":
                    return Budynek(punkt, s);
                case "jaskinia":
                    return Jaskinia(s);
                case "boss":
                    return Obelisk(new Barwa(230, 50, 50));
                case "portal":
                    return KragKamieni();
                case "leze_smoka":
                    return LezeSmoka(s);
                case "latajaca_wyspa":
                    return LatajacaWyspa(s);
                default:
                    return Obelisk(new Barwa(120, 200, 255));
            }
        }

        /// <summary>Światła punktów: rodzaj poświaty i przesunięcie od środka pola.</summary>
        public static readonly Dictionary<string, (string Rodzaj, int Dx, int Dy)> SwiatlaPunktow =
            new Dictionary<string, (string, int, int)>
            {
                { "karczma", ("okno", -6, -10) },
                { "kuźnia", ("kuznia", -5, -8) },
                { "miasto", ("okno", -6, -8) },
                { "portal", ("magia", 0, -2) },
                { "boss", ("krew", 0, -12) },
                { "leze_smoka", ("kuznia", 0, -4) },
            };

        private static readonly (int X, int Y)[] Miejsca =
        {
            (-8, -1), (6, -2), (-2, 3), (9, 3), (-10, 3), (2, -4), (0, 0),
        };

        private static readonly Dictionary<string, int> IleDekoracji = new Dictionary<string, int>
        {
            { "las", 4 }, { "równiny", 1 }, { "bagna", 3 },
            { "wzgórza", 2 }, { "kanion", 2 }, { "ruiny", 3 },
        };

        /// <summary>
        /// Lista dekoracji względem środka wierzchu pola.
        /// Sprite null = ognisko rysowane na żywo (migocze, więc nie da się go zbuforować).
        /// </summary>
        public static List<Dekoracja> Dekoracje(string biom, string punkt, int s)
        {
            var wynik = new List<Dekoracja>();
            if (!string.IsNullOrEmpty(punkt))
            {
                Plotno spr = SpritePunktu(punkt, s);
                int dx = punkt == "obóz" ? -2 : 0;
                if (punkt == "latajaca_wyspa")
                {
                    wynik.Add(new Dekoracja(spr, -spr.Szerokosc / 2, -spr.Wysokosc - 4));
                }
                else
                {
                    wynik.Add(new Dekoracja(spr, dx - spr.Szerokosc / 2, 4 - spr.Wysokosc));
                }
                if (punkt == "obóz")
                {
                    wynik.Add(new Dekoracja(null, 8, 2));
                }
                return wynik;
            }

            int ile = IleDekoracji.TryGetValue(biom ?? "", out int n) ? n : 1;
            for (int i = 0; i < ile; i++)
            {
                if (biom != "las" && Piksele.Szum(i, s, 5) < 0.35)
                {
                    continue;
                }
                (int mx, int my) = Miejsca[Modulo(i + s, Miejsca.Length)];
                Plotno spr;
                if (biom == "las")
                {
                    spr = Sosna(18 + (int)(Piksele.Szum(i, s, 1) * 8), s + i);
                }
                else if (biom == "równiny")
                {
                    spr = Piksele.Szum(i, s, 2) > 0.5
                        ? DrzewoLisciaste(s + i)
                        : Skala(3, 2, s + i, Rampy["równiny"]);
                }
                else if (biom == "bagna")
                {
                    spr = Trzcina(s + i);
                }
                else if (biom == "wzgórza")
                {
                    spr = Skala(4 + i, 3 + i / 2, s + i);
                }
                else if (biom == "kanion")
                {
                    spr = Skala(4, 5, s + i, Czerwien);
                }
                else
                {
                    spr = Kolumna(s + i, 6 + (int)(Piksele.Szum(i, s, 3) * 8));
                }
                wynik.Add(new Dekoracja(spr, mx - spr.Szerokosc / 2, my - spr.Wysokosc + 1));
            }
            // Dalsze krzaki zasłaniane bliższymi — sortowanie po dolnej krawędzi.
            return wynik.OrderBy(d => d.Dy + d.Sprite.Wysokosc).ToList();
        }

        private static int Modulo(int a, int b)
        {
            int r = a % b;
            return r < 0 ? r + b : r;
        }

        // ------------------------------------------------------------------ //
        //  Postać gracza                                                      //
        // ------------------------------------------------------------------ //

        private static readonly string[] Sylwetka =
        {
            "...ooo...",
            "..ohhHo..",
            "..ohhHo..",
            "..osssoo.",
            ".ocaagCow",
            "ocaaaaCow",
            "ocagaaCow",
            "ocaaaaCo.",
            ".ocaaCCo.",
            "..oCCCo..",
            "..obobo..",
            "..ob.bo..",
            "..oo.oo..",
        };

        /// <summary>Kolor płaszcza i hełmu po klasie — sylwetka ta sama, klasę widać po barwach.</summary>
        private static readonly Dictionary<string, (Barwa Plaszcz, Barwa PlaszczC, Barwa Helm)> KoloryKlas =
            new Dictionary<string, (Barwa, Barwa, Barwa)>
            {
                { "wojownik", (new Barwa(164, 46, 48), new Barwa(106, 28, 38), new Barwa(182, 188, 202)) },
                { "mag", (new Barwa(64, 82, 170), new Barwa(40, 50, 110), new Barwa(90, 110, 200)) },
                { "łotrzyk", (new Barwa(70, 74, 62), new Barwa(44, 48, 40), new Barwa(60, 56, 50)) },
                { "lotrzyk", (new Barwa(70, 74, 62), new Barwa(44, 48, 40), new Barwa(60, 56, 50)) },
                { "druid", (new Barwa(76, 124, 58), new Barwa(48, 82, 40), new Barwa(120, 96, 60)) },
                { "nekromanta", (new Barwa(70, 40, 86), new Barwa(40, 22, 52), new Barwa(200, 196, 180)) },
            };

        public static Plotno SpriteGracza(string klasa)
        {
            string klucz = (klasa ?? "").ToLowerInvariant();
            if (!KoloryKlas.TryGetValue(klucz, out (Barwa Plaszcz, Barwa PlaszczC, Barwa Helm) kolory))
            {
                kolory = KoloryKlas["wojownik"];
            }
            var paleta = new Dictionary<char, Barwa>
            {
                { 'o', new Barwa(22, 18, 28) },
                { 'h', kolory.Helm },
                { 'H', Piksele.Ciemniej(kolory.Helm, 0.65) },
                { 's', new Barwa(228, 176, 134) },
                { 'c', kolory.Plaszcz },
                { 'C', kolory.PlaszczC },
                { 'a', new Barwa(146, 152, 166) },
                { 'g', new Barwa(232, 192, 84) },
                { 'b', new Barwa(74, 52, 40) },
                { 'w', new Barwa(216, 222, 232) },
            };
            return Piksele.SpriteZTekstu(Sylwetka, paleta);
        }

        /// <summary>Ognisko: żar, płomień i iskry zależne od czasu — rysowane wprost na scenie.</summary>
        public static void Ognisko(Plotno cel, int x, int y, double t)
        {
            var zar = new[] { (-2, 0), (2, 0), (0, 1), (-1, -1), (1, -1) };
            for (int i = 0; i < zar.Length; i++)
            {
                (int dx, int dy) = zar[i];
                cel.Ustaw(x + dx, y + dy,
                          i % 2 != 0 ? new Barwa(90, 60, 40) : new Barwa(66, 44, 30));
            }
            for (int i = 0; i < 10; i++)
            {
                double faza = Reszta(t * 2.3 + i * 0.37, 1.0);
                int fx = x + (int)(Math.Sin(i * 2.1 + t * 7) * (1.5 - faza));
                int fy = y - (int)(faza * 7);
                Barwa kolor = faza < 0.25 ? new Barwa(255, 240, 170)
                    : faza < 0.6 ? new Barwa(255, 170, 60)
                    : new Barwa(210, 70, 40);
                cel.Ustaw(fx, fy, kolor);
            }
            for (int i = 0; i < 4; i++)
            {
                // iskry
                double faza = Reszta(t * 0.6 + i * 0.25, 1.0);
                cel.Ustaw(x + (int)(Math.Sin(i * 5 + t * 2) * 4), y - 8 - (int)(faza * 18),
                          faza < 0.7 ? new Barwa(255, 190, 90) : new Barwa(140, 80, 50));
            }
        }

        /// <summary>Reszta o znaku dzielnika — jak <c>%</c> na liczbach zmiennoprzecinkowych w Pythonie.</summary>
        private static double Reszta(double a, double b)
        {
            double r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
