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
    ///
    /// Scena zna kolejność rysowania i światło; jak wyglądają kafle, obiekty
    /// i postać, mówi jej generator aktywnego stylu (<see cref="IGeneratorTerenu"/>).
    /// Generator jest brany przy tworzeniu sceny — po zmianie stylu trzeba
    /// zbudować nową scenę, bo bufory kafli należą do starego.
    /// </summary>
    public sealed class ScenaMapy
    {
        /// <summary>Szerokość sceny w aktywnym stylu — do przygotowania płótna.</summary>
        public static int Szer => Styl.Aktywny.Generator.SzerSceny;

        /// <summary>Wysokość sceny w aktywnym stylu.</summary>
        public static int Wys => Styl.Aktywny.Generator.WysSceny;

        public bool Zmierzch;

        private readonly IGeneratorTerenu _gen;
        private readonly IRysownikRegionu _rysownik;
        private readonly Styl _styl;
        private readonly int _x0;

        private readonly Dictionary<string, (int H, Plotno[] Warianty)> _kafle =
            new Dictionary<string, (int, Plotno[])>();
        private readonly Dictionary<string, Plotno> _mgla = new Dictionary<string, Plotno>();
        private readonly Dictionary<string, List<Dekoracja>> _dekor =
            new Dictionary<string, List<Dekoracja>>();
        private readonly Dictionary<string, Plotno> _osady = new Dictionary<string, Plotno>();
        private readonly Plotno _obwodka;
        private readonly Dictionary<bool, Plotno> _tlo;
        private readonly Plotno _winieta;
        private readonly Dictionary<string, Plotno> _blask;

        public ScenaMapy()
        {
            _styl = Styl.Aktywny;
            _rysownik = _styl.Generator as IRysownikRegionu;
            if (_rysownik != null)
            {
                return; // styl składa region sam — bufory kafli są mu zbędne
            }
            _gen = (IGeneratorTerenu)_styl.Generator;
            _x0 = _gen.SzerSceny / 2;
            _obwodka = _gen.Obwodka();
            _tlo = new Dictionary<bool, Plotno>
            {
                { false, _gen.Tlo(false) },
                { true, _gen.Tlo(true) },
            };
            _winieta = _gen.Winieta();
            _blask = new Dictionary<string, Plotno>();
            foreach (KeyValuePair<string, (int Promien, Barwa Kolor)> b in _styl.Blaski)
            {
                _blask[b.Key] = Piksele.Poswiata(b.Value.Promien, b.Value.Kolor);
            }
        }

        /// <summary>Styl, którym ta scena rysuje.</summary>
        public Styl Styl => _styl;

        public int SzerokoscSceny => _styl.Generator.SzerSceny;

        public int WysokoscSceny => _styl.Generator.WysSceny;

        public int SkalaSceny => _styl.Generator.SkalaSceny;

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
                int h = _gen.WysokoscPola(biom, x, y, s);
                int klatki = Math.Max(1, _gen.Klatki(biom));
                var warianty = new Plotno[klatki];
                for (int k = 0; k < klatki; k++)
                {
                    warianty[k] = _gen.Kafel(biom, h, s, k);
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
                p = _gen.Zamglij(kafel, x, y, Zmierzch);
                _mgla[klucz] = p;
            }
            return p;
        }

        private List<Dekoracja> Dekoracje((int X, int Y) region, int x, int y, string biom, string punkt)
        {
            string klucz = $"{region.X},{region.Y}|{x}|{y}|{biom}|{punkt}";
            if (!_dekor.TryGetValue(klucz, out List<Dekoracja> lista))
            {
                lista = _gen.Dekoracje(biom, punkt, Ziarno(region, x, y));
                _dekor[klucz] = lista;
            }
            return lista;
        }

        private Plotno Osada(int chaty, int palisada, int wieza)
        {
            string klucz = $"{chaty}|{palisada}|{wieza}";
            if (!_osady.TryGetValue(klucz, out Plotno p))
            {
                p = _gen.OsadaObozu(chaty, palisada, wieza);
                _osady[klucz] = p;
            }
            return p;
        }

        // ------------------------------------------------------------------ //
        //  Rysowanie                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Rysuje region na płótnie o rozmiarze sceny tego stylu.</summary>
        /// <param name="cel">Płótno sceny.</param>
        /// <param name="pola">Siatka pól regionu.</param>
        /// <param name="t">Czas w sekundach — animuje wodę, ogień i chód postaci.</param>
        /// <param name="region">Współrzędne regionu — wchodzą w ziarno kafli.</param>
        /// <param name="graczXy">Pole gracza albo null (ekran tytułowy).</param>
        /// <param name="klasa">Klasa postaci — decyduje o wyglądzie sprite'a.</param>
        /// <param name="wszystkoOdkryte">Pomija mgłę wojny (ekran tytułowy).</param>
        /// <param name="osada">Chaty, palisada i wieża — obóz rośnie razem z osadą.</param>
        public void Rysuj(Plotno cel, PoleDoRysunku[][] pola, double t,
                          (int X, int Y) region = default,
                          (int X, int Y)? graczXy = null, string klasa = null,
                          bool wszystkoOdkryte = false,
                          (int Chaty, int Palisada, int Wieza)? osada = null)
        {
            if (_rysownik != null)
            {
                _rysownik.Rysuj(cel, pola, t, region, graczXy, klasa, wszystkoOdkryte, osada, Zmierzch);
                return;
            }
            int tw2 = _gen.TW / 2;
            int th2 = _gen.TH / 2;
            cel.Nalóż(_tlo[Zmierzch], 0, 0);
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
                    int sx = _x0 + (x - y) * tw2;
                    int sy = _gen.Y0 + (x + y) * th2;
                    (int h, Plotno kafel) = Kafel(region, x, y, biom, klatka);
                    (int X, int Y) srodek = (sx, sy - h + th2);

                    if (!wszystkoOdkryte && !pole.Odkryte)
                    {
                        cel.Nalóż(Zamglony(region, x, y, biom, kafel), sx - tw2, sy - h);
                        continue;
                    }
                    cel.Nalóż(kafel, sx - tw2, sy - h);

                    bool tuGracz = graczXy.HasValue && graczXy.Value.X == x && graczXy.Value.Y == y;
                    if (tuGracz)
                    {
                        cel.Nalóż(_obwodka, sx - tw2, sy - h);
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
                        cel.Nalóż(widok, srodek.X + _gen.KotwicaOsady.Dx, srodek.Y + _gen.KotwicaOsady.Dy);
                        dekoracje = dekoracje.FindAll(d => d.Sprite == null);
                    }

                    foreach (Dekoracja d in dekoracje)
                    {
                        if (d.Sprite == null)
                        {
                            _gen.Ognisko(cel, srodek.X + d.Dx, srodek.Y + d.Dy, t);
                            swiatla.Add(("ogien", srodek.X + d.Dx, srodek.Y + d.Dy + _gen.SwiatloOgniskaDy));
                        }
                        else
                        {
                            cel.Nalóż(d.Sprite, srodek.X + d.Dx, srodek.Y + d.Dy);
                        }
                    }

                    if (!string.IsNullOrEmpty(punkt)
                        && _gen.SwiatlaPunktow.TryGetValue(punkt,
                            out (string Rodzaj, int Dx, int Dy) sw))
                    {
                        swiatla.Add((sw.Rodzaj, srodek.X + sw.Dx, srodek.Y + sw.Dy));
                    }

                    if (tuGracz)
                    {
                        pozycjaGracza = _gen.PozycjaGracza(srodek, !string.IsNullOrEmpty(punkt));
                        swiatla.Add(("latarnia", pozycjaGracza.Value.X + _gen.SwiatloGracza.Dx,
                                     pozycjaGracza.Value.Y + _gen.SwiatloGracza.Dy));
                    }
                }
            }

            // Gracz na końcu, nad drzewami z pól przed nim: czytelność ważniejsza
            // niż ścisła głębia — w lesie inaczej ginie całkiem.
            if (pozycjaGracza.HasValue)
            {
                _gen.RysujGracza(cel, pozycjaGracza.Value.X, pozycjaGracza.Value.Y, klasa, t);
            }

            if (Zmierzch)
            {
                Oswietl(cel, swiatla, t);
            }
            cel.PomnozNa(_winieta, 0, 0);
            _gen.Obrobka(cel, t);
        }

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
            mapa.Wypelnij(_styl.SwiatloOtoczenia);
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
