using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Jedno pole regionu.</summary>
    public sealed class Pole
    {
        public string Biom;
        public bool Odkryte;
        public bool Odwiedzone;
        public int Zbierania;

        /// <summary>Klucz punktu orientacyjnego albo null, gdy pole jest puste.</summary>
        public string Punkt;

        public Pole(string biom, string punkt = null)
        {
            Biom = biom;
            Punkt = punkt;
        }
    }

    /// <summary>
    /// Trwały świat: siatka regionów, biomy, punkty orientacyjne, mgła wojny.
    ///
    /// Region jest identyfikowany parą współrzędnych (RegionX, RegionY).
    /// Raz wygenerowany region zostaje w <c>gracz.Regiony</c> — można do niego
    /// wrócić i zastać te same pola, odkrycia i zużyte miejsca zbierania.
    /// Trudność (MapaGen) wynika z odległości od regionu startowego (0, 0).
    ///
    /// Generator jest przepisany z wersji pythonowej razem z kolejnością
    /// losowań, więc ten sam seed daje ten sam świat w obu wersjach gry.
    /// </summary>
    public static class Mapa
    {
        public const int Rozmiar = 9;
        public const int Srodek = Rozmiar / 2;

        /// <summary>Szansa, że region poza startowym ma legowisko bossa (ok. co trzeci region).</summary>
        private const double SzansaBossa = 0.34;

        public static readonly string[] BiomyNazwy =
        {
            "równiny", "ruiny", "las", "bagna", "wzgórza", "kanion",
        };

        /// <summary>Klawisz menu → (nazwa kierunku, dx, dy).</summary>
        public static readonly List<(string Klawisz, string Nazwa, int Dx, int Dy)> Kierunki =
            new List<(string, string, int, int)>
            {
                ("1", "północ", 0, -1),
                ("2", "zachód", -1, 0),
                ("3", "wschód", 1, 0),
                ("4", "południe", 0, 1),
            };

        private static readonly string[] PunktyLosowe =
        {
            "karczma", "kuźnia", "świątynia", "jaskinia",
        };

        public static readonly string[] PunktyMityczne =
        {
            "portal", "leze_smoka", "latajaca_wyspa",
        };

        public static int LiczbaPol()
        {
            return Rozmiar * Rozmiar;
        }

        /// <summary>Trudność regionu = 1 + odległość (Chebyshev) od regionu startowego.</summary>
        public static int PoziomRegionu(int rx, int ry)
        {
            return 1 + Math.Max(Math.Abs(rx), Math.Abs(ry));
        }

        /// <summary>Powtarzalne ziarno regionu — ten sam świat po powrocie i po wczytaniu.</summary>
        public static long ZiarnoRegionu(long seed, int rx, int ry)
        {
            // Python liczy to na nieograniczonych liczbach całkowitych i dopiero
            // na końcu maskuje, więc rachunek musi iść po 64 bitach ze znakiem,
            // a maska zdejmuje resztę.
            long n = seed * 1_000_003L + rx * 73_856_093L + ry * 19_349_663L;
            return n & 0x7FFF_FFFFL;
        }

        /// <summary>Klastry biomów (Voronoi) — region wygląda jak mapa, nie jak szum.</summary>
        private static string[][] SiatkaBiomow(Losowanie rng)
        {
            const int n = Rozmiar;
            int ile = rng.Calkowita(4, 6);
            List<string> wybrane = rng.Probka(BiomyNazwy, ile);
            var ziarna = new List<(int X, int Y, string Biom)>();
            foreach (string biom in wybrane)
            {
                int zx = rng.Calkowita(0, n - 1);
                int zy = rng.Calkowita(0, n - 1);
                ziarna.Add((zx, zy, biom));
            }

            var siatka = new string[n][];
            for (int y = 0; y < n; y++)
            {
                siatka[y] = new string[n];
                for (int x = 0; x < n; x++)
                {
                    // Python wybiera min() — przy remisie zostaje pierwszy
                    // z listy, więc tu też porównujemy ostro większym.
                    int najlepszy = int.MaxValue;
                    string biom = ziarna[0].Biom;
                    foreach ((int zx, int zy, string b) in ziarna)
                    {
                        int d = (zx - x) * (zx - x) + (zy - y) * (zy - y);
                        if (d < najlepszy)
                        {
                            najlepszy = d;
                            biom = b;
                        }
                    }
                    siatka[y][x] = biom;
                }
            }
            return siatka;
        }

        /// <summary>
        /// Tworzy region Rozmiar×Rozmiar. Układ zależy od seeda postaci
        /// i współrzędnych regionu, więc jest powtarzalny przy powrocie,
        /// ale inny w każdej nowej rozgrywce.
        /// </summary>
        public static Pole[][] GenerujMape(int mapaGen = 1, long seed = 0, int rx = 0, int ry = 0)
        {
            var rng = new Losowanie(ZiarnoRegionu(seed, rx, ry));
            string[][] biomy = SiatkaBiomow(rng);
            bool startowy = rx == 0 && ry == 0;

            var pola = new Pole[Rozmiar][];
            for (int y = 0; y < Rozmiar; y++)
            {
                pola[y] = new Pole[Rozmiar];
                for (int x = 0; x < Rozmiar; x++)
                {
                    string punkt = null;
                    if (startowy && x == Srodek && y == Srodek)
                    {
                        punkt = "obóz";
                    }
                    else if (rng.Losowa() < 0.13)
                    {
                        punkt = rng.Wybierz(PunktyLosowe);
                    }
                    pola[y][x] = new Pole(biomy[y][x], punkt);
                }
            }

            if (!startowy && rng.Losowa() < SzansaBossa)
            {
                int bx = rng.Calkowita(0, Rozmiar - 1);
                int by = rng.Calkowita(0, Rozmiar - 1);
                pola[by][bx].Punkt = "boss";
            }

            if (mapaGen >= 2)
            {
                double szansa = mapaGen < 5 ? 0.42 : 0.58;
                if (rng.Losowa() < szansa)
                {
                    var wolne = new List<(int X, int Y)>();
                    for (int y = 0; y < Rozmiar; y++)
                    {
                        for (int x = 0; x < Rozmiar; x++)
                        {
                            string p = pola[y][x].Punkt;
                            if (p != "obóz" && p != "boss")
                            {
                                wolne.Add((x, y));
                            }
                        }
                    }
                    if (wolne.Count > 0)
                    {
                        (int mx, int my) = rng.Wybierz(wolne);
                        pola[my][mx].Punkt = rng.Wybierz(PunktyMityczne);
                    }
                }
            }

            if (mapaGen >= 2)
            {
                double szansaMiasta = mapaGen < 4 ? 0.62 : 0.82;
                if (rng.Losowa() < szansaMiasta)
                {
                    var wolne = new List<(int X, int Y)>();
                    for (int y = 0; y < Rozmiar; y++)
                    {
                        for (int x = 0; x < Rozmiar; x++)
                        {
                            string p = pola[y][x].Punkt;
                            if (p != "obóz" && p != "boss" && !PunktyMityczne.Contains(p))
                            {
                                wolne.Add((x, y));
                            }
                        }
                    }
                    if (wolne.Count > 0)
                    {
                        (int cx, int cy) = rng.Wybierz(wolne);
                        pola[cy][cx].Punkt = "miasto";
                    }
                }
            }

            return pola;
        }

        // ------------------------------------------------------------------ //
        //  Trwały świat — słownik regionów                                    //
        // ------------------------------------------------------------------ //

        public static string KluczRegionu(int rx, int ry)
        {
            return $"{rx},{ry}";
        }

        /// <summary>Zwraca siatkę regionu — generuje ją tylko przy pierwszej wizycie.</summary>
        public static Pole[][] RegionPola(Gracz gracz, int rx, int ry)
        {
            string klucz = KluczRegionu(rx, ry);
            if (!gracz.Regiony.TryGetValue(klucz, out Pole[][] pola) || !SiatkaPoprawna(pola))
            {
                pola = GenerujMape(PoziomRegionu(rx, ry), gracz.Seed, rx, ry);
                gracz.Regiony[klucz] = pola;
            }
            return pola;
        }

        /// <summary>Ile regionów gracz odwiedził (rozmiar trwałego świata).</summary>
        public static int LiczbaRegionow(Gracz gracz)
        {
            return gracz.Regiony.Count;
        }

        private static bool SiatkaPoprawna(Pole[][] pola)
        {
            return pola != null && pola.Length == Rozmiar
                   && pola[0] != null && pola[0].Length == Rozmiar;
        }

        /// <summary>Gwarantuje spójny stan świata: współrzędne regionu i bieżącą siatkę.</summary>
        public static void ZapewnijMape(Gracz gracz)
        {
            if (gracz.Seed == 0)
            {
                gracz.Seed = new Losowanie().Zakres(1, int.MaxValue);
            }
            gracz.MapaGen = PoziomRegionu(gracz.RegionX, gracz.RegionY);
            gracz.MapaPola = RegionPola(gracz, gracz.RegionX, gracz.RegionY);
            PrzytnijPozycje(gracz);
            OdkryjPole(gracz);
        }

        private static void PrzytnijPozycje(Gracz gracz)
        {
            gracz.MapaX = Math.Max(0, Math.Min(Rozmiar - 1, gracz.MapaX));
            gracz.MapaY = Math.Max(0, Math.Min(Rozmiar - 1, gracz.MapaY));
        }

        public static Pole PoleNa(Gracz gracz, int x, int y)
        {
            return gracz.MapaPola[y][x];
        }

        public static Pole PoleGracza(Gracz gracz)
        {
            ZapewnijMape(gracz);
            return PoleNa(gracz, gracz.MapaX, gracz.MapaY);
        }

        /// <summary>Oznacza aktualne pole jako odkryte. Mapa musi już istnieć.</summary>
        public static void OdkryjPole(Gracz gracz)
        {
            Pole pole = PoleNa(gracz, gracz.MapaX, gracz.MapaY);
            pole.Odkryte = true;
            gracz.AktualnyBiom = pole.Biom;
        }

        public static int LiczbaOdkrytych(Gracz gracz)
        {
            if (gracz.MapaPola == null)
            {
                return 0;
            }
            int ile = 0;
            foreach (Pole[] wiersz in gracz.MapaPola)
            {
                foreach (Pole p in wiersz)
                {
                    if (p.Odkryte)
                    {
                        ile++;
                    }
                }
            }
            return ile;
        }

        /// <summary>Biomy i lokacje z odkrytych pól — w kolejności katalogu ikon.</summary>
        public static (List<string> Biomy, List<string> Punkty) SymboleOdkryte(Gracz gracz)
        {
            var biomy = new HashSet<string>();
            var punkty = new HashSet<string>();
            if (gracz.MapaPola != null)
            {
                foreach (Pole[] wiersz in gracz.MapaPola)
                {
                    foreach (Pole pole in wiersz)
                    {
                        if (!pole.Odkryte)
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(pole.Biom))
                        {
                            biomy.Add(pole.Biom);
                        }
                        if (!string.IsNullOrEmpty(pole.Punkt))
                        {
                            punkty.Add(pole.Punkt);
                        }
                    }
                }
            }
            var listaBiomow = Ikony.Biomy.Where(p => biomy.Contains(p.Key))
                                   .Select(p => p.Key).ToList();
            var listaPunktow = Ikony.Punkty.Where(p => punkty.Contains(p.Key))
                                    .Select(p => p.Key).ToList();
            return (listaBiomow, listaPunktow);
        }

        public static string GlifPola(Gracz gracz, int x, int y)
        {
            Pole pole = PoleNa(gracz, x, y);
            return Ikony.GlifPola(pole.Biom, pole.Punkt,
                                  x == gracz.MapaX && y == gracz.MapaY, pole.Odkryte);
        }

        public static string OpisPunktu(string punkt)
        {
            if (string.IsNullOrEmpty(punkt))
            {
                return "";
            }
            var nazwy = new Dictionary<string, string>
            {
                { "obóz", "obóz" }, { "karczma", "karczma" }, { "kuźnia", "kuźnia" },
                { "świątynia", "świątynia" }, { "jaskinia", "jaskinia" },
                { "boss", "legowisko bossa" }, { "portal", "portal do innego wymiaru" },
                { "leze_smoka", "leże smoka" }, { "latajaca_wyspa", "latająca wyspa" },
                { "miasto", "miasto za murami" },
            };
            return Ikony.EtykietaPunktu(punkt, nazwy.TryGetValue(punkt, out string n) ? n : punkt);
        }

        /// <summary>Co widać w danym kierunku (biom, jeśli pole odkryte).</summary>
        public static string EtykietaKierunku(Gracz gracz, int dx, int dy)
        {
            int nx = gracz.MapaX + dx;
            int ny = gracz.MapaY + dy;
            if (nx < 0 || ny < 0 || nx >= Rozmiar || ny >= Rozmiar)
            {
                int rx = gracz.RegionX + Atrybuty.PodzielWDol(nx, Rozmiar);
                int ry = gracz.RegionY + Atrybuty.PodzielWDol(ny, Rozmiar);
                return CzyRegionZnany(gracz, rx, ry)
                    ? $"🧭 znany region [{rx}, {ry}]"
                    : $"🌄 nowy region [{rx}, {ry}]";
            }
            Pole pole = PoleNa(gracz, nx, ny);
            if (!pole.Odkryte)
            {
                return $"{Ikony.Mgla} ???";
            }
            if (pole.Punkt == "obóz")
            {
                return Ikony.EtykietaPunktu("obóz", "obóz");
            }
            string txt = Ikony.EtykietaBiomu(pole.Biom);
            if (!string.IsNullOrEmpty(pole.Punkt))
            {
                txt += $", {OpisPunktu(pole.Punkt)}";
            }
            return txt;
        }

        /// <summary>
        /// Przesuwa gracza. Zwraca true, gdy przekroczono krawędź i zmieniono region.
        ///
        /// Region po drugiej stronie krawędzi jest trwały: wyjście na wschód
        /// i powrót na zachód wraca dokładnie tam, skąd się wyszło. Dzielenie
        /// i reszta muszą iść w dół jak w Pythonie, inaczej przejście przez
        /// krawędź zachodnią trafiłoby w zły region.
        /// </summary>
        public static bool PrzesunGracza(Gracz gracz, int dx, int dy)
        {
            ZapewnijMape(gracz);
            int nx = gracz.MapaX + dx;
            int ny = gracz.MapaY + dy;
            bool nowa = false;

            if (nx < 0 || ny < 0 || nx >= Rozmiar || ny >= Rozmiar)
            {
                gracz.RegionX += Atrybuty.PodzielWDol(nx, Rozmiar);
                gracz.RegionY += Atrybuty.PodzielWDol(ny, Rozmiar);
                gracz.MapaX = ResztaWDol(nx, Rozmiar);
                gracz.MapaY = ResztaWDol(ny, Rozmiar);
                gracz.MapaGen = PoziomRegionu(gracz.RegionX, gracz.RegionY);
                gracz.MapaPola = RegionPola(gracz, gracz.RegionX, gracz.RegionY);
                nowa = true;
            }
            else
            {
                gracz.MapaX = nx;
                gracz.MapaY = ny;
            }

            OdkryjPole(gracz);
            return nowa;
        }

        /// <summary>Reszta z dzielenia o znaku dzielnika — jak <c>%</c> w Pythonie.</summary>
        public static int ResztaWDol(int a, int b)
        {
            int r = a % b;
            if (r != 0 && (r < 0) != (b < 0))
            {
                r += b;
            }
            return r;
        }

        public static bool CzyRegionZnany(Gracz gracz, int rx, int ry)
        {
            return gracz.Regiony.ContainsKey(KluczRegionu(rx, ry));
        }

        /// <summary>Krótki opis położenia regionu względem obozu — dla nagłówka mapy.</summary>
        public static string OpisRegionu(Gracz gracz)
        {
            int rx = gracz.RegionX;
            int ry = gracz.RegionY;
            if (rx == 0 && ry == 0)
            {
                return "region startowy (obóz)";
            }
            var czesci = new List<string>();
            if (ry < 0)
            {
                czesci.Add($"{Math.Abs(ry)}× na północ");
            }
            else if (ry > 0)
            {
                czesci.Add($"{ry}× na południe");
            }
            if (rx < 0)
            {
                czesci.Add($"{Math.Abs(rx)}× na zachód");
            }
            else if (rx > 0)
            {
                czesci.Add($"{rx}× na wschód");
            }
            return string.Join(" i ", czesci) + " od obozu";
        }
    }
}
