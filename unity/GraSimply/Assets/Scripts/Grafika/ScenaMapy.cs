using System;
using System.Collections.Generic;
using GraSimply.Logika;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Scena mapy: region 9×9 w rzucie izometrycznym, z mgłą wojny i światłem.
    ///
    /// Rysuje prosto z danych gry — nie trzyma własnej kopii świata. Zbuforowane
    /// są tylko obrazki kafli i dekoracji, kluczowane tym, co na polu widać,
    /// więc zmiana w grze (np. pokonany boss) odświeża się sama.
    /// </summary>
    public sealed class ScenaMapy
    {
        /// <summary>Rozdzielczość sceny w pikselach gry.</summary>
        public const int Szer = 320;
        public const int Wys = 240;

        /// <summary>Ekranowa pozycja górnego wierzchołka pola (0, 0).</summary>
        private const int X0 = Szer / 2;
        private const int Y0 = 46;

        public bool Zmierzch;

        private readonly Dictionary<string, (int H, Plotno[] Warianty)> _kafle =
            new Dictionary<string, (int, Plotno[])>();
        private readonly Dictionary<string, Plotno> _mgla = new Dictionary<string, Plotno>();
        private readonly Dictionary<string, List<Dekoracja>> _dekor =
            new Dictionary<string, List<Dekoracja>>();
        private readonly Dictionary<string, Plotno> _osady = new Dictionary<string, Plotno>();
        private readonly Dictionary<string, Plotno> _spriteGracza = new Dictionary<string, Plotno>();
        private readonly Plotno _obwodka;
        private readonly Plotno _cien;
        private readonly Dictionary<bool, Plotno> _niebo;
        private readonly Plotno _winieta;
        private readonly Dictionary<string, Plotno> _blask;

        public ScenaMapy()
        {
            _obwodka = Teren.Obwodka();
            _cien = new Plotno(12, 5);
            _cien.Elipsa(new Barwa(0, 0, 0, 90), 0, 0, 12, 5);
            _niebo = new Dictionary<bool, Plotno>
            {
                { false, Piksele.GradientNieba(Szer, Wys, new Barwa(120, 170, 210), new Barwa(196, 214, 222)) },
                { true, Piksele.GradientNieba(Szer, Wys, new Barwa(30, 28, 58), new Barwa(86, 58, 84)) },
            };
            _winieta = Piksele.Winieta(Szer, Wys);
            _blask = new Dictionary<string, Plotno>
            {
                { "ogien", Piksele.Poswiata(44, new Barwa(255, 150, 70)) },
                { "okno", Piksele.Poswiata(20, new Barwa(255, 190, 110)) },
                { "kuznia", Piksele.Poswiata(26, new Barwa(255, 120, 50)) },
                { "magia", Piksele.Poswiata(30, new Barwa(170, 90, 255)) },
                { "krew", Piksele.Poswiata(26, new Barwa(255, 60, 50)) },
                { "latarnia", Piksele.Poswiata(30, new Barwa(220, 190, 140)) },
            };
        }

        // ------------------------------------------------------------------ //
        //  Bufory                                                             //
        // ------------------------------------------------------------------ //

        private static int Ziarno((int X, int Y) region, int x, int y)
        {
            return (x * 13 + y * 7 + region.X * 101 + region.Y * 211 + 1) & 0xFFFF;
        }

        private (int H, Plotno Kafel) Kafel((int X, int Y) region, int x, int y, string biom, int klatka)
        {
            string klucz = $"{region.X},{region.Y}|{x}|{y}|{biom}";
            if (!_kafle.TryGetValue(klucz, out (int H, Plotno[] Warianty) wpis))
            {
                int s = Ziarno(region, x, y);
                int h = Teren.WysokoscPola(biom, x, y, s);
                int klatki = biom == "bagna" ? 3 : 1;
                var warianty = new Plotno[klatki];
                for (int k = 0; k < klatki; k++)
                {
                    warianty[k] = Teren.Kafel(biom, h, s, k);
                }
                wpis = (h, warianty);
                _kafle[klucz] = wpis;
            }
            return (wpis.H, wpis.Warianty[klatka % wpis.Warianty.Length]);
        }

        private Plotno Zamglony((int X, int Y) region, int x, int y, string biom, Plotno kafel)
        {
            string klucz = $"{region.X},{region.Y}|{x}|{y}|{biom}|{Zmierzch}";
            if (!_mgla.TryGetValue(klucz, out Plotno p))
            {
                p = Teren.Zamglij(kafel, x, y, Zmierzch);
                _mgla[klucz] = p;
            }
            return p;
        }

        private List<Dekoracja> Dekoracje((int X, int Y) region, int x, int y, string biom, string punkt)
        {
            string klucz = $"{region.X},{region.Y}|{x}|{y}|{biom}|{punkt}";
            if (!_dekor.TryGetValue(klucz, out List<Dekoracja> lista))
            {
                lista = Teren.Dekoracje(biom, punkt, Ziarno(region, x, y));
                _dekor[klucz] = lista;
            }
            return lista;
        }

        private Plotno Osada(int chaty, int palisada, int wieza)
        {
            string klucz = $"{chaty}|{palisada}|{wieza}";
            if (!_osady.TryGetValue(klucz, out Plotno p))
            {
                p = Teren.OsadaObozu(chaty, palisada, wieza);
                _osady[klucz] = p;
            }
            return p;
        }

        private Plotno SpriteGracza(string klasa)
        {
            string klucz = klasa ?? "";
            if (!_spriteGracza.TryGetValue(klucz, out Plotno p))
            {
                p = Teren.SpriteGracza(klasa);
                _spriteGracza[klucz] = p;
            }
            return p;
        }

        // ------------------------------------------------------------------ //
        //  Rysowanie                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Rysuje region na płótnie Szer×Wys.
        /// </summary>
        /// <param name="cel">Płótno sceny (320×240).</param>
        /// <param name="pola">Siatka pól regionu.</param>
        /// <param name="t">Czas w sekundach — animuje wodę, ogień i chód postaci.</param>
        /// <param name="region">Współrzędne regionu — wchodzą w ziarno kafli.</param>
        /// <param name="graczXy">Pole gracza albo null (ekran tytułowy).</param>
        /// <param name="klasa">Klasa postaci — decyduje o barwach sprite'a.</param>
        /// <param name="wszystkoOdkryte">Pomija mgłę wojny (ekran tytułowy).</param>
        /// <param name="osada">Chaty, palisada i wieża — obóz rośnie razem z osadą.</param>
        public void Rysuj(Plotno cel, PoleDoRysunku[][] pola, double t,
                          (int X, int Y) region = default,
                          (int X, int Y)? graczXy = null, string klasa = null,
                          bool wszystkoOdkryte = false,
                          (int Chaty, int Palisada, int Wieza)? osada = null)
        {
            cel.Nalóż(_niebo[Zmierzch], 0, 0);
            var swiatla = new List<(string Rodzaj, int X, int Y)>();
            (int X, int Y)? pozycjaGracza = null;
            (int X, int Y)? karczowisko = null;

            if (osada.HasValue && osada.Value.Chaty >= 3)
            {
                karczowisko = ZnajdzOboz(pola);
            }
            int klatka = (int)(t * 3) % 3;
            int n = pola.Length;

            // Od tyłu do przodu — bliższe pola zasłaniają dalsze.
            for (int suma = 0; suma < 2 * n - 1; suma++)
            {
                for (int x = 0; x < n; x++)
                {
                    int y = suma - x;
                    if (y < 0 || y >= n)
                    {
                        continue;
                    }
                    PoleDoRysunku pole = pola[y][x];
                    string biom = string.IsNullOrEmpty(pole.Biom) ? "równiny" : pole.Biom;
                    int sx = X0 + (x - y) * (TW2);
                    int sy = Y0 + (x + y) * (TH2);
                    (int h, Plotno kafel) = Kafel(region, x, y, biom, klatka);
                    (int X, int Y) srodek = (sx, sy - h + TH2);

                    if (!wszystkoOdkryte && !pole.Odkryte)
                    {
                        cel.Nalóż(Zamglony(region, x, y, biom, kafel), sx - TW2, sy - h);
                        continue;
                    }
                    cel.Nalóż(kafel, sx - TW2, sy - h);

                    bool tuGracz = graczXy.HasValue && graczXy.Value.X == x && graczXy.Value.Y == y;
                    if (tuGracz)
                    {
                        cel.Nalóż(_obwodka, sx - TW2, sy - h);
                    }

                    string punkt = pole.Punkt;
                    List<Dekoracja> dekoracje = Dekoracje(region, x, y, biom, punkt);
                    if (karczowisko.HasValue && string.IsNullOrEmpty(punkt)
                        && Math.Max(Math.Abs(x - karczowisko.Value.X),
                                    Math.Abs(y - karczowisko.Value.Y)) <= 1)
                    {
                        dekoracje = new List<Dekoracja>(); // osada wykarczowała okolicę obozu
                    }
                    if (punkt == "obóz" && osada.HasValue
                        && (osada.Value.Chaty != 0 || osada.Value.Palisada != 0 || osada.Value.Wieza != 0))
                    {
                        Plotno widok = Osada(osada.Value.Chaty, osada.Value.Palisada, osada.Value.Wieza);
                        cel.Nalóż(widok, srodek.X - 26, srodek.Y - 30);
                        dekoracje = dekoracje.FindAll(d => d.Sprite == null);
                    }

                    foreach (Dekoracja d in dekoracje)
                    {
                        if (d.Sprite == null)
                        {
                            Teren.Ognisko(cel, srodek.X + d.Dx, srodek.Y + d.Dy, t);
                            swiatla.Add(("ogien", srodek.X + d.Dx, srodek.Y + d.Dy - 2));
                        }
                        else
                        {
                            cel.Nalóż(d.Sprite, srodek.X + d.Dx, srodek.Y + d.Dy);
                        }
                    }

                    if (!string.IsNullOrEmpty(punkt)
                        && Teren.SwiatlaPunktow.TryGetValue(punkt,
                            out (string Rodzaj, int Dx, int Dy) sw))
                    {
                        swiatla.Add((sw.Rodzaj, srodek.X + sw.Dx, srodek.Y + sw.Dy));
                    }

                    if (tuGracz)
                    {
                        bool naPunkcie = !string.IsNullOrEmpty(punkt);
                        pozycjaGracza = (srodek.X - 4 + (naPunkcie ? 7 : 0),
                                         srodek.Y - 11 + (naPunkcie ? 3 : 0));
                        swiatla.Add(("latarnia", pozycjaGracza.Value.X + 4, pozycjaGracza.Value.Y + 6));
                    }
                }
            }

            // Gracz na końcu, nad drzewami z pól przed nim: czytelność ważniejsza
            // niż ścisła głębia — w lesie inaczej ginie całkiem.
            if (pozycjaGracza.HasValue)
            {
                int px = pozycjaGracza.Value.X;
                int py = pozycjaGracza.Value.Y;
                int bob = (int)(Math.Abs(Math.Sin(t * 3)) * 1.5);
                cel.Nalóż(_cien, px - 1, py + 10);
                cel.Nalóż(SpriteGracza(klasa), px, py - bob);
                int my = py - 7 - (int)(Math.Abs(Math.Sin(t * 2.2)) * 2);
                var szerokosci = new[] { 5, 3, 1 };
                for (int i = 0; i < szerokosci.Length; i++)
                {
                    // złoty grot nad głową
                    int szer = szerokosci[i];
                    cel.Wypelnij(new Barwa(236, 200, 110), px + 4 - szer / 2, my + i, szer, 1);
                }
                cel.Wypelnij(new Barwa(120, 90, 40), px + 2, my - 1, 5, 1);
            }

            if (Zmierzch)
            {
                Oswietl(cel, swiatla, t);
            }
            cel.PomnozNa(_winieta, 0, 0);
        }

        private const int TW2 = Teren.TW / 2;
        private const int TH2 = Teren.TH / 2;

        private static (int X, int Y)? ZnajdzOboz(PoleDoRysunku[][] pola)
        {
            for (int y = 0; y < pola.Length; y++)
            {
                for (int x = 0; x < pola[y].Length; x++)
                {
                    if (pola[y][x].Punkt == "obóz")
                    {
                        return (x, y);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Mapa światła: ciemne otoczenie + addytywne poświaty, potem mnożenie
        /// przez scenę i delikatny bloom nad źródłami.
        /// </summary>
        private void Oswietl(Plotno cel, List<(string Rodzaj, int X, int Y)> swiatla, double t)
        {
            var mapa = new Plotno(cel.Szerokosc, cel.Wysokosc, przezroczyste: false);
            mapa.Wypelnij(new Barwa(112, 104, 168));
            foreach ((string rodzaj, int x, int y) in swiatla)
            {
                Plotno b = _blask[rodzaj];
                if (rodzaj == "ogien")
                {
                    b = b.SkalujO(1 + Math.Sin(t * 9) * 0.04 + Math.Sin(t * 13.7) * 0.03);
                }
                mapa.DodajNa(b, x - b.Szerokosc / 2, y - b.Wysokosc / 2);
            }
            cel.PomnozNa(mapa, 0, 0);
            foreach ((string rodzaj, int x, int y) in swiatla)
            {
                Plotno maly = _blask[rodzaj].SkalujO(0.35);
                maly.PomnozPrzez(new Barwa(70, 70, 70, 255));
                cel.DodajNa(maly, x - maly.Szerokosc / 2, y - maly.Wysokosc / 2);
            }
        }
    }
}
