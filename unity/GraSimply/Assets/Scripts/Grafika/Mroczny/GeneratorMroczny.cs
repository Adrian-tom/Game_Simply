using System;
using System.Collections.Generic;
using System.Text;
using GraSimply.Logika;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>
    /// Styl „Mroczny”: mroczne fantasy w języku obrazu pierwszego Fallouta.
    ///
    /// Zamiast kafli z blokami terenu — jeden ciągły grunt w rzucie
    /// trimetrycznym, który za krawędzią regionu gaśnie w czerń. Obiekty to
    /// modele 3D „sfotografowane” raz w ustalonym świetle (<see cref="Fotograf"/>),
    /// z cieniem rzucanym na grunt. Noc przyciemnia obraz mnożeniem, a światła
    /// tylko podnoszą je z powrotem; barwy samoświecące (ogień, okna, magia)
    /// zostają nietknięte. Na końcu cała klatka wpada do jednej palety.
    ///
    /// Region składa się raz — grunt, cienie i obiekty trafiają do bufora
    /// kluczowanego tym, co na mapie widać. Co klatkę dochodzą tylko rzeczy
    /// żywe: płomienie, postać, światło i paleta.
    /// </summary>
    public sealed class GeneratorMroczny : IRysownikRegionu
    {
        public int SzerSceny => Rzut.SzerSceny;
        public int WysSceny => Rzut.WysSceny;
        public int SkalaSceny => 1;

        /// <summary>Ile klatek ma woda bagien — tyle wersji gruntu trzymamy.</summary>
        private const int KlatkiWody = 3;

        /// <summary>Światło nocy: przyciemnienie bez źródeł, lekko w stronę błękitu.</summary>
        private static readonly (double R, double G, double B) Noc = (0.36, 0.37, 0.47);

        private sealed class Swiatlo
        {
            public double X, Y, Promien;
            public (double R, double G, double B) Kolor;
            public bool Migocze;
        }

        /// <summary>Region złożony do bufora: obraz bez rzeczy żywych i to, co trzeba do nich dorysować.</summary>
        private sealed class Sklad
        {
            public Plotno Obraz;
            public readonly List<Swiatlo> Swiatla = new List<Swiatlo>();
            public readonly List<(int X, int Y)> Ogniska = new List<(int, int)>();
        }

        private readonly Dictionary<string, SpriteBryly> _sprite = new Dictionary<string, SpriteBryly>();
        private readonly Dictionary<string, Sklad> _sklady = new Dictionary<string, Sklad>();
        private readonly Dictionary<string, SpriteBryly> _bohaterowie = new Dictionary<string, SpriteBryly>();
        private Kwantyzator _kwantyzator;
        private bool[] _komorkiSwiecace;

        // ------------------------------------------------------------------ //
        //  Klatka                                                             //
        // ------------------------------------------------------------------ //

        public void Rysuj(Plotno cel, PoleDoRysunku[][] pola, double t, (int X, int Y) region,
                          (int X, int Y)? graczXy, string klasa, bool wszystkoOdkryte,
                          (int Chaty, int Palisada, int Wieza)? osada, bool zmierzch)
        {
            int klatka = Grunt.MaWode(pola, wszystkoOdkryte) ? (int)(t * 3) % KlatkiWody : 0;
            Sklad sklad = Zloz(pola, region, wszystkoOdkryte, osada, klatka);

            // Gdzie stoi postać: na polu z budowlą — z przodu, żeby budowla jej nie zasłaniała.
            (int X, int Y)? postac = null;
            if (graczXy.HasValue)
            {
                (int gx, int gy) = graczXy.Value;
                bool naPunkcie = !string.IsNullOrEmpty(pola[gy][gx].Punkt);
                postac = Ekran(gx + (naPunkcie ? 0.86 : 0.5), gy + (naPunkcie ? 0.92 : 0.55));
            }
            (int kx, int ky) = Kamera(postac, pola.Length, cel.Szerokosc, cel.Wysokosc);
            Wytnij(sklad.Obraz, cel, kx, ky);

            foreach ((int x, int y) in sklad.Ogniska)
            {
                Plomien(cel, x - kx, y - ky, t);
            }

            var swiatla = new List<Swiatlo>();
            foreach (Swiatlo s in sklad.Swiatla)
            {
                swiatla.Add(new Swiatlo
                {
                    X = s.X - kx, Y = s.Y - ky, Promien = s.Promien, Kolor = s.Kolor, Migocze = s.Migocze,
                });
            }
            if (postac.HasValue)
            {
                ZaznaczPole(cel, graczXy.Value.X, graczXy.Value.Y, kx, ky, t);
                int px = postac.Value.X - kx, py = postac.Value.Y - ky;
                RysujBohatera(cel, px, py, klasa, t);
                swiatla.Add(new Swiatlo
                {
                    X = px - 0.15 * Rzut.AX, Y = py - 0.5 * Rzut.Wysokosc, Promien = 3.2 * Rzut.AX,
                    Kolor = (1.0, 0.86, 0.62), Migocze = true,
                });
            }

            if (zmierzch)
            {
                Oswietl(cel, swiatla, t);
            }
            Kwantyzator().Kwantyzuj(cel);
        }

        /// <summary>
        /// Lewy górny róg kadru na płótnie regionu. Postać w środku kadru (trochę
        /// niżej, bo nad nią są drzewa i dachy), ale kadr nie wyjeżdża poza płótno
        /// — na skraju regionu postać schodzi ze środka, jak w Falloucie przy
        /// krawędzi mapy.
        /// </summary>
        private static (int X, int Y) Kamera((int X, int Y)? postac, int n, int szer, int wys)
        {
            (int cx, int cy) = postac ?? Ekran(n / 2.0, n / 2.0);
            int kx = cx - szer / 2;
            int ky = cy - wys / 2 - (postac.HasValue ? (int)(0.3 * Rzut.Wysokosc) : 0);
            // Kadr większy niż płótno regionu: wyśrodkowany, z czernią po bokach.
            kx = Rzut.SzerRegionu <= szer ? (Rzut.SzerRegionu - szer) / 2 : Math.Max(0, Math.Min(Rzut.SzerRegionu - szer, kx));
            ky = Rzut.WysRegionu <= wys ? (Rzut.WysRegionu - wys) / 2 : Math.Max(0, Math.Min(Rzut.WysRegionu - wys, ky));
            return (kx, ky);
        }

        /// <summary>Kopiuje wycinek płótna regionu do kadru; to, co poza płótnem, zostaje czarne.</summary>
        private static void Wytnij(Plotno zrodlo, Plotno cel, int kx, int ky)
        {
            int x0 = Math.Max(0, -kx);
            int x1 = Math.Min(cel.Szerokosc, zrodlo.Szerokosc - kx);
            for (int y = 0; y < cel.Wysokosc; y++)
            {
                int zy = ky + y;
                int wiersz = y * cel.Szerokosc * 4;
                Array.Clear(cel.Dane, wiersz, cel.Szerokosc * 4);
                for (int i = wiersz + 3; i < wiersz + cel.Szerokosc * 4; i += 4)
                {
                    cel.Dane[i] = 255;
                }
                if (zy < 0 || zy >= zrodlo.Wysokosc || x1 <= x0)
                {
                    continue;
                }
                Buffer.BlockCopy(zrodlo.Dane, (zy * zrodlo.Szerokosc + kx + x0) * 4,
                                 cel.Dane, wiersz + x0 * 4, (x1 - x0) * 4);
            }
        }

        private static (int X, int Y) Ekran(double wx, double wy, double wz = 0)
        {
            (double sx, double sy) = Rzut.NaEkran(wx, wy, wz);
            return ((int)Math.Round(sx), (int)Math.Round(sy));
        }

        // ------------------------------------------------------------------ //
        //  Skład regionu                                                      //
        // ------------------------------------------------------------------ //

        private Sklad Zloz(PoleDoRysunku[][] pola, (int X, int Y) region, bool wszystkoOdkryte,
                           (int Chaty, int Palisada, int Wieza)? osada, int klatka)
        {
            var kl = new StringBuilder();
            kl.Append(region.X).Append(',').Append(region.Y).Append('|').Append(wszystkoOdkryte ? 1 : 0)
              .Append('|').Append(klatka).Append('|').Append(osada.HasValue ? osada.Value.ToString() : "-").Append('|');
            foreach (PoleDoRysunku[] wiersz in pola)
            {
                foreach (PoleDoRysunku p in wiersz)
                {
                    kl.Append(p.Odkryte ? '1' : '0').Append(p.Biom).Append(':').Append(p.Punkt).Append(';');
                }
            }
            string klucz = kl.ToString();
            if (_sklady.TryGetValue(klucz, out Sklad gotowy))
            {
                return gotowy;
            }
            if (_sklady.Count >= 2 * KlatkiWody)
            {
                _sklady.Clear(); // trzymamy tylko bieżący region — po zmianie stary jest zbędny
            }

            var sklad = new Sklad { Obraz = Grunt.Rysuj(pola, region, wszystkoOdkryte, klatka) };
            var obiekty = new List<(double Glebia, int X, int Y, SpriteBryly S)>();
            int n = pola.Length;
            (int X, int Y)? karczowisko = null;
            bool rosnacaOsada = osada.HasValue
                && (osada.Value.Chaty != 0 || osada.Value.Palisada != 0 || osada.Value.Wieza != 0);
            if (osada.HasValue && osada.Value.Chaty >= 3)
            {
                karczowisko = ZnajdzOboz(pola);
            }

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    PoleDoRysunku pole = pola[y][x];
                    if (!wszystkoOdkryte && !pole.Odkryte)
                    {
                        continue; // nieznane: tylko przygaszony grunt, jak niezbadane w Falloucie
                    }
                    string biom = string.IsNullOrEmpty(pole.Biom) ? "równiny" : pole.Biom;
                    int s = Ziarno(region, x, y);
                    if (!string.IsNullOrEmpty(pole.Punkt))
                    {
                        Punkt(pole.Punkt, x, y, s, rosnacaOsada ? osada : null, obiekty, sklad);
                        continue;
                    }
                    if (karczowisko.HasValue
                        && Math.Max(Math.Abs(x - karczowisko.Value.X), Math.Abs(y - karczowisko.Value.Y)) <= 1)
                    {
                        continue; // osada wykarczowała okolicę obozu
                    }
                    Roslinnosc(biom, x, y, s, obiekty);
                }
            }

            // Najpierw wszystkie cienie jedną maską — nakładające się cienie
            // nie ciemnieją podwójnie — potem obiekty od tyłu do przodu.
            var maska = new byte[Rzut.SzerRegionu * Rzut.WysRegionu];
            foreach ((double _, int ox, int oy, SpriteBryly spr) in obiekty)
            {
                if (spr.Cien != null)
                {
                    DodajCien(maska, spr.Cien, ox + spr.CienOx, oy + spr.CienOy);
                }
            }
            NalozCienie(sklad.Obraz, maska);
            obiekty.Sort((a, b) => a.Glebia.CompareTo(b.Glebia));
            foreach ((double _, int ox, int oy, SpriteBryly spr) in obiekty)
            {
                sklad.Obraz.Nalóż(spr.Obraz, ox + spr.Ox, oy + spr.Oy);
            }
            _sklady[klucz] = sklad;
            return sklad;
        }

        private static int Ziarno((int X, int Y) region, int x, int y)
        {
            return (x * 13 + y * 7 + region.X * 101 + region.Y * 211 + 1) & 0xFFFF;
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

        private void Dodaj(List<(double, int, int, SpriteBryly)> obiekty, double wx, double wy, SpriteBryly s)
        {
            (int sx, int sy) = Ekran(wx, wy);
            // Głębia = ekranowa wysokość podstawy; remis rozstrzyga x, żeby kolejność była stała.
            obiekty.Add((Rzut.AY * wx + Rzut.BY * wy + wx * 1e-4, sx, sy, s));
        }

        /// <summary>Sprite z bufora — wariantów jest kilka na rodzaj, więc las składa się z niewielu modeli.</summary>
        private SpriteBryly Sprite(string rodzaj, int wariant, Func<int, Szkic> szkic)
        {
            string klucz = rodzaj + "#" + wariant;
            if (!_sprite.TryGetValue(klucz, out SpriteBryly s))
            {
                int ziarno = 3 + wariant * 17;
                s = szkic(ziarno).Zrob(ziarno);
                _sprite[klucz] = s;
            }
            return s;
        }

        // ---- roślinność i skały ------------------------------------------ //

        /// <summary>Miejsca na obiekty w obrębie pola, od tyłu do przodu.</summary>
        private static readonly (double X, double Y)[] Miejsca =
        {
            (0.28, 0.26), (0.72, 0.30), (0.48, 0.52), (0.26, 0.74), (0.74, 0.72), (0.52, 0.86), (0.86, 0.50),
        };

        private static readonly Dictionary<string, int> Gestosc = new Dictionary<string, int>
        {
            { "las", 4 }, { "równiny", 2 }, { "bagna", 3 }, { "wzgórza", 2 }, { "kanion", 2 }, { "ruiny", 3 },
        };

        private void Roslinnosc(string biom, int x, int y, int s, List<(double, int, int, SpriteBryly)> obiekty)
        {
            int ile = Gestosc.TryGetValue(biom, out int g) ? g : 1;
            int start = (int)(Piksele.Szum(x, y, s + 9) * Miejsca.Length);
            for (int i = 0; i < ile; i++)
            {
                double los = Piksele.Szum(i, s, 5);
                if (biom != "las" && los < 0.4)
                {
                    continue;
                }
                (double mx, double my) = Miejsca[(start + i * 3) % Miejsca.Length];
                double wx = x + mx + (Piksele.Szum(i, s, 6) - 0.5) * 0.12;
                double wy = y + my + (Piksele.Szum(i, s, 7) - 0.5) * 0.12;
                int w = (int)(Piksele.Szum(i, s, 8) * 3);
                double rodzaj = Piksele.Szum(i, s, 2);
                SpriteBryly spr;
                switch (biom)
                {
                    case "las":
                        spr = rodzaj < 0.62 ? Sprite("sosna", w, Szkice.Sosna)
                            : rodzaj < 0.88 ? Sprite("dab", w, Szkice.Dab)
                            : Sprite("krzak", w, Szkice.Krzak);
                        break;
                    case "bagna":
                        spr = rodzaj < 0.7 ? Sprite("trzcina", w, Szkice.Trzcina)
                            : Sprite("martwe", w % 2, Szkice.MartweDrzewo);
                        break;
                    case "wzgórza":
                        spr = rodzaj < 0.6 ? Sprite("glaz", w, z => Szkice.Glaz(z, 1.5))
                            : Sprite("krzak", w, Szkice.Krzak);
                        break;
                    case "kanion":
                        spr = rodzaj < 0.55
                            ? Sprite("glaz_czerwony", w, z => Szkice.Glaz(z, 1.4, Materialy.SkalaCzerwona))
                            : Sprite("iglica", w % 2, Szkice.Iglica);
                        break;
                    case "ruiny":
                        spr = rodzaj < 0.4 ? Sprite("kolumna", w, Szkice.Kolumna)
                            : rodzaj < 0.75 ? Sprite("mur", w % 2, Szkice.Mur)
                            : Sprite("glaz", w, z => Szkice.Glaz(z));
                        break;
                    default: // równiny
                        spr = rodzaj < 0.45 ? Sprite("dab", w, Szkice.Dab)
                            : rodzaj < 0.8 ? Sprite("krzak", w, Szkice.Krzak)
                            : Sprite("glaz", w, z => Szkice.Glaz(z));
                        break;
                }
                Dodaj(obiekty, wx, wy, spr);
            }
        }

        // ---- punkty orientacyjne ----------------------------------------- //

        private void Punkt(string punkt, int x, int y, int s, (int Chaty, int Palisada, int Wieza)? osada,
                           List<(double, int, int, SpriteBryly)> obiekty, Sklad sklad)
        {
            double cx = x + 0.5, cy = y + 0.5;
            const double K = Budowle.Powiekszenie;
            SpriteBryly spr;
            switch (punkt)
            {
                case "obóz":
                    Oboz(cx, cy, osada, obiekty, sklad);
                    return;
                case "karczma":
                    spr = Sprite("karczma", 0, Budowle.Karczma);
                    DodajSwiatlo(sklad, cx - 0.05 * K, cy + 0.18 * K, 0.15 * K, 95, (1.0, 0.80, 0.52));
                    break;
                case "kuźnia":
                    spr = Sprite("kuznia", 0, Budowle.Kuznia);
                    DodajSwiatlo(sklad, cx - 0.04 * K, cy + 0.16 * K, 0.05 * K, 90, (1.0, 0.62, 0.36), true);
                    break;
                case "świątynia":
                    spr = Sprite("swiatynia", 0, Budowle.Swiatynia);
                    break;
                case "miasto":
                    spr = Sprite("miasto", 0, Budowle.Miasto);
                    DodajSwiatlo(sklad, cx, cy + 0.1 * K, 0.12 * K, 110, (1.0, 0.82, 0.56));
                    break;
                case "jaskinia":
                    spr = Sprite("jaskinia", 0, Budowle.Jaskinia);
                    break;
                case "boss":
                    spr = Sprite("obelisk", 0, Budowle.Obelisk);
                    DodajSwiatlo(sklad, cx, cy, 0.3 * K, 85, (1.0, 0.36, 0.32), true);
                    break;
                case "portal":
                    spr = Sprite("portal", 0, Budowle.Portal);
                    DodajSwiatlo(sklad, cx, cy, 0.17 * K, 100, (0.78, 0.56, 1.0), true);
                    break;
                case "leze_smoka":
                    spr = Sprite("leze", 0, Budowle.LezeSmoka);
                    DodajSwiatlo(sklad, cx, cy + 0.26 * K, 0.08 * K, 80, (1.0, 0.55, 0.34), true);
                    break;
                case "latajaca_wyspa":
                    spr = Sprite("wyspa", 0, Budowle.LatajacaWyspa);
                    break;
                default:
                    spr = Sprite("obelisk", 0, Budowle.Obelisk);
                    break;
            }
            Dodaj(obiekty, cx, cy, spr);
        }

        /// <summary>Miejsca chat wokół namiotu, w polach; mnożone przez rozmach osady.</summary>
        private static readonly (double X, double Y)[] MiejscaChat =
        {
            (-0.50, -0.14), (0.26, -0.50), (-0.52, 0.34), (0.50, 0.08),
            (-0.10, -0.62), (0.46, 0.50), (-0.14, 0.60), (-0.62, -0.52),
        };

        /// <summary>
        /// Obóz gracza: namiot z paleniskiem, a z czasem chaty, wieża i palisada.
        /// Od trzech chat osada zajmuje też sąsiednie pola (są wykarczowane),
        /// więc rozstawia się szerzej.
        /// </summary>
        private void Oboz(double cx, double cy, (int Chaty, int Palisada, int Wieza)? osada,
                          List<(double, int, int, SpriteBryly)> obiekty, Sklad sklad)
        {
            const double K = Budowle.Powiekszenie;
            Dodaj(obiekty, cx, cy, Sprite("oboz", 0, Budowle.Oboz));
            Ognisko(sklad, cx + 0.12 * K, cy + 0.12 * K);
            if (!osada.HasValue)
            {
                return;
            }
            (int chaty, int palisada, int wieza) = osada.Value;
            double rozmach = chaty >= 3 ? 1.55 : 1.0;
            for (int i = 0; i < Math.Min(chaty, MiejscaChat.Length); i++)
            {
                (double mx, double my) = MiejscaChat[i];
                // Co trzecia chata kryta gontem — osada rośnie latami, nie z jednej dostawy.
                int w = i;
                Dodaj(obiekty, cx + mx * rozmach, cy + my * rozmach,
                      Sprite("chata_osady" + (i % 3 == 2 ? "_gont" : ""), w % 4,
                             z => Budowle.ChataSzkic(z, i % 3 == 2, 1.25)));
            }
            if (wieza > 0)
            {
                Dodaj(obiekty, cx - 0.66 * rozmach, cy + 0.02 * rozmach, Sprite("wieza", wieza, _ => Budowle.Wieza(wieza)));
            }
            if (palisada > 0)
            {
                double r = 0.86 * rozmach;
                int pali = (int)(46 * rozmach);
                // Brama od strony obserwatora.
                double brama = Math.Atan2(Rzut.DoObserwatora.Y, Rzut.DoObserwatora.X);
                for (int i = 0; i < pali; i++)
                {
                    double kat = i / (double)pali * Math.PI * 2;
                    double roznica = Math.Abs(Math.IEEERemainder(kat - brama, Math.PI * 2));
                    if (roznica < 0.2)
                    {
                        continue;
                    }
                    Dodaj(obiekty, cx + Math.Cos(kat) * r, cy + Math.Sin(kat) * r,
                          Sprite("pal" + palisada, i % 4, z => Budowle.Pal(z, palisada)));
                }
            }
        }

        private static void DodajSwiatlo(Sklad sklad, double wx, double wy, double wz, double promien,
                                    (double, double, double) kolor, bool migocze = false)
        {
            (int sx, int sy) = Ekran(wx, wy, wz);
            // Promienie podane w pikselach rzutu bez powiększenia — rosną razem z nim.
            sklad.Swiatla.Add(new Swiatlo
            {
                X = sx, Y = sy, Promien = promien * 1.3 * Rzut.Z, Kolor = kolor, Migocze = migocze,
            });
        }

        private static void Ognisko(Sklad sklad, double wx, double wy)
        {
            (int sx, int sy) = Ekran(wx, wy);
            sklad.Ogniska.Add((sx, sy));
            sklad.Swiatla.Add(new Swiatlo
            {
                X = sx, Y = sy - 6 * Rzut.Z, Promien = 170 * Rzut.Z, Kolor = (1.0, 0.74, 0.46), Migocze = true,
            });
        }

        // ------------------------------------------------------------------ //
        //  Cienie                                                             //
        // ------------------------------------------------------------------ //

        private static void DodajCien(byte[] maska, Plotno cien, int x0, int y0)
        {
            for (int y = 0; y < cien.Wysokosc; y++)
            {
                int cy = y0 + y;
                if (cy < 0 || cy >= Rzut.WysRegionu)
                {
                    continue;
                }
                for (int x = 0; x < cien.Szerokosc; x++)
                {
                    int cx = x0 + x;
                    if (cx < 0 || cx >= Rzut.SzerRegionu)
                    {
                        continue;
                    }
                    byte a = cien.Dane[(y * cien.Szerokosc + x) * 4 + 3];
                    int i = cy * Rzut.SzerRegionu + cx;
                    if (a > maska[i])
                    {
                        maska[i] = a;
                    }
                }
            }
        }

        private static void NalozCienie(Plotno obraz, byte[] maska)
        {
            byte[] d = obraz.Dane;
            for (int i = 0; i < maska.Length; i++)
            {
                int a = maska[i];
                if (a == 0)
                {
                    continue;
                }
                int j = i * 4;
                // Cień to ciepła ciemność, nie szarość — gaśnie ku brązowi, jak ziemia nocą.
                d[j] = (byte)((d[j] * (255 - a) + 16 * a) / 255);
                d[j + 1] = (byte)((d[j + 1] * (255 - a) + 10 * a) / 255);
                d[j + 2] = (byte)((d[j + 2] * (255 - a) + 6 * a) / 255);
            }
        }

        // ------------------------------------------------------------------ //
        //  Rzeczy żywe                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Płomień ogniska: kilka języków z barw ognia, które co klatkę zmieniają
        /// wysokość i kołyszą się. Same barwy samoświecące — noc ich nie gasi.
        /// </summary>
        public static void Plomien(Plotno cel, int x, int y, double t)
        {
            Barwa[] ogien = Materialy.Ogien;
            double z = Rzut.Z;
            int faza = (int)(t * 10);
            for (int j = 0; j < 3; j++)
            {
                double h = (9 + 4 * Piksele.Szum(faza, j, 71) - Math.Abs(j - 1) * 3) * z;
                double kolys = Math.Sin(t * 7 + j * 2.1) * 1.2 * z;
                double bx = x + (j - 1) * 2 * z;
                for (int r = 0; r < (int)h; r++)
                {
                    double u = r / h;
                    double pol = (1.0 - u) * 1.8 * z * (j == 1 ? 1.2 : 0.8);
                    int px0 = (int)Math.Round(bx + kolys * u - pol);
                    int px1 = (int)Math.Round(bx + kolys * u + pol);
                    for (int px = px0; px <= px1; px++)
                    {
                        double odSrodka = Math.Abs(px - (bx + kolys * u)) / Math.Max(0.6, pol);
                        double ciep = 1.0 - u * 0.75 - odSrodka * 0.45 + (Piksele.Szum(px, r + faza, 73) - 0.5) * 0.3;
                        cel.Ustaw(px, y - r, Paleta.Z(ogien, ciep));
                    }
                }
            }
            // Iskra unosząca się nad ogniem.
            double iskra = (t * 1.7) % 1.0;
            cel.Ustaw(x + (int)(Math.Sin(t * 5) * 3 * z), y - (int)((12 + iskra * 10) * z), ogien[iskra < 0.6 ? 4 : 2]);
        }

        /// <summary>Obrys pola gracza: przerywana linia w kolorze bursztynu, jak heks pod kursorem.</summary>
        private static void ZaznaczPole(Plotno cel, int x, int y, int kx, int ky, double t)
        {
            var naroza = new[] { Ekran(x, y), Ekran(x + 1, y), Ekran(x + 1, y + 1), Ekran(x, y + 1) };
            for (int k = 0; k < 4; k++)
            {
                naroza[k] = (naroza[k].X - kx, naroza[k].Y - ky);
            }
            double puls = 0.75 + 0.25 * Math.Sin(t * 3);
            var kolor = new Barwa((int)(0xFC * puls), (int)(0x9C * puls), (int)(0x48 * puls));
            for (int k = 0; k < 4; k++)
            {
                (int x1, int y1) = naroza[k];
                (int x2, int y2) = naroza[(k + 1) % 4];
                int krokow = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
                for (int i = 0; i <= krokow; i++)
                {
                    if ((i / 3) % 2 == 1)
                    {
                        continue;
                    }
                    int px = x1 + (x2 - x1) * i / Math.Max(1, krokow);
                    int py = y1 + (y2 - y1) * i / Math.Max(1, krokow);
                    cel.Ustaw(px, py, kolor);
                }
            }
        }

        private void RysujBohatera(Plotno cel, int px, int py, string klasa, double t)
        {
            string k = klasa ?? "";
            if (!_bohaterowie.TryGetValue(k, out SpriteBryly s))
            {
                s = Postacie.Bohater(k).Zrob(11);
                _bohaterowie[k] = s;
            }
            if (s.Cien != null)
            {
                cel.Nalóż(s.Cien, px + s.CienOx, py + s.CienOy);
            }
            // Oddech zamiast podskoku: postać unosi się o piksel co dwie sekundy.
            int oddech = Math.Sin(t * Math.PI) > 0.6 ? 1 : 0;
            cel.Nalóż(s.Obraz, px + s.Ox, py + s.Oy - oddech);
        }

        // ------------------------------------------------------------------ //
        //  Światło i paleta                                                   //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Noc: każdy piksel mnożony przez natężenie — otoczenie albo najjaśniejsze
        /// z pobliskich świateł (max, nie suma: dwa ogniska obok siebie nie
        /// prześwietlają ziemi). Barwy samoświecące przepuszczamy bez zmian.
        /// </summary>
        private void Oswietl(Plotno cel, List<Swiatlo> swiatla, double t)
        {
            int szer = cel.Szerokosc, wys = cel.Wysokosc;
            var natR = new float[szer * wys];
            var natG = new float[szer * wys];
            var natB = new float[szer * wys];
            for (int i = 0; i < natR.Length; i++)
            {
                natR[i] = (float)Noc.R;
                natG[i] = (float)Noc.G;
                natB[i] = (float)Noc.B;
            }
            foreach (Swiatlo s in swiatla)
            {
                double r = s.Promien;
                if (s.Migocze)
                {
                    r *= 1 + Math.Sin(t * 9 + s.X) * 0.04 + Math.Sin(t * 13.7 + s.Y) * 0.03;
                }
                int x0 = Math.Max(0, (int)(s.X - r)), x1 = Math.Min(szer - 1, (int)(s.X + r));
                // Na gruncie światło rozlewa się w spłaszczoną elipsę, jak sam rzut.
                int y0 = Math.Max(0, (int)(s.Y - r * 0.62)), y1 = Math.Min(wys - 1, (int)(s.Y + r * 0.62));
                for (int y = y0; y <= y1; y++)
                {
                    double dy = (y - s.Y) / 0.62;
                    for (int x = x0; x <= x1; x++)
                    {
                        double dx = x - s.X;
                        double d = Math.Sqrt(dx * dx + dy * dy) / r;
                        if (d >= 1)
                        {
                            continue;
                        }
                        double f = 1 - d;
                        f = f * f * (3 - 2 * f); // gładkie wygaszenie, bez twardego kręgu
                        int i = y * szer + x;
                        natR[i] = Math.Max(natR[i], (float)(s.Kolor.R * f));
                        natG[i] = Math.Max(natG[i], (float)(s.Kolor.G * f));
                        natB[i] = Math.Max(natB[i], (float)(s.Kolor.B * f));
                    }
                }
            }
            bool[] swiecace = KomorkiSwiecace();
            byte[] dane = cel.Dane;
            for (int i = 0; i < natR.Length; i++)
            {
                int j = i * 4;
                byte r = dane[j], g = dane[j + 1], b = dane[j + 2];
                if (swiecace[((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3)] && Materialy.CzySamoswiecaca(r, g, b))
                {
                    continue;
                }
                dane[j] = (byte)Math.Min(255, r * natR[i]);
                dane[j + 1] = (byte)Math.Min(255, g * natG[i]);
                dane[j + 2] = (byte)Math.Min(255, b * natB[i]);
            }
        }

        /// <summary>Komórki RGB555, w które wpada jakaś barwa samoświecąca — szybkie sito przed zbiorem.</summary>
        private bool[] KomorkiSwiecace()
        {
            if (_komorkiSwiecace == null)
            {
                var k = new bool[32 * 32 * 32];
                foreach (int c in Materialy.Samoswiecace)
                {
                    int r = (c >> 16) & 255, g = (c >> 8) & 255, b = c & 255;
                    k[((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3)] = true;
                }
                _komorkiSwiecace = k;
            }
            return _komorkiSwiecace;
        }

        /// <summary>Paleta główna: wszystkie rampy materiałów plus czerń. Ogień i światła nietykalne.</summary>
        private Kwantyzator Kwantyzator()
        {
            if (_kwantyzator == null)
            {
                var barwy = new List<Barwa> { new Barwa(0, 0, 0) };
                foreach (Barwa[] rampa in Materialy.WszystkieRampy())
                {
                    barwy.AddRange(rampa);
                }
                // Przyciemnione wersje ramp — noc i cienie potrzebują stopni poniżej
                // najciemniejszego stopnia materiału, inaczej wszystko zlewa się w jedną barwę.
                foreach (Barwa[] rampa in Materialy.WszystkieRampy())
                {
                    foreach (Barwa b in rampa)
                    {
                        if (!Materialy.CzySamoswiecaca(b.R, b.G, b.B))
                        {
                            barwy.Add(Paleta.Siatka6(b.R * 0.5, b.G * 0.5, b.B * 0.55));
                            barwy.Add(Paleta.Siatka6(b.R * 0.28, b.G * 0.28, b.B * 0.34));
                        }
                    }
                }
                _kwantyzator = new Kwantyzator(barwy, Materialy.Samoswiecace);
            }
            return _kwantyzator;
        }
    }
}
