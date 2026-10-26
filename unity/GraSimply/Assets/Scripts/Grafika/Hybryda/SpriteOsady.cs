using System;
using System.Collections.Generic;
using GraSimply.Grafika.Mroczny;
using GraSimply.Logika;

namespace GraSimply.Grafika.Hybryda
{
    /// <summary>
    /// Sprite'y sceny osady w stylu hybrydowym: pixelart rysowany jak w klasycznym
    /// (domki z faktury deski i cegły, sylwetki z linijek tekstu), ale w barwach
    /// materiałów stylu Mroczny, żeby po nałożeniu światła cała klatka weszła
    /// do jednej wspólnej palety.
    ///
    /// Każdy sprite powstaje w rozdzielczości pikselartu, jest powiększany ×2
    /// i dopiero wtedy dostaje kontur — kontur ma więc jeden piksel sceny,
    /// a wnętrze „grube” ziarno, co odróżnia obiekty od drobnego gruntu.
    /// </summary>
    public sealed class SpriteOsady
    {
        /// <summary>Krotność powiększenia sprite'ów względem pikselartu.</summary>
        public const int Skala = 2;

        private readonly Dictionary<string, Plotno> _pamiec = new Dictionary<string, Plotno>();

        private struct Wyglad
        {
            public int A, H, DachH;
            public Barwa[] Sciana, Dach;
            public string Faktura;
            public Barwa? Okno;
        }

        // Pół szerokości, wysokość ściany, ściana, dach, faktura, światło w oknie, wysokość dachu.
        private static readonly Dictionary<string, Wyglad> Wygladu = new Dictionary<string, Wyglad>
        {
            { "dom", Opis(8, 9, Materialy.Deski, Materialy.Dachowka, "deski", Materialy.Okno[2], 6) },
            { "kuznia", Opis(7, 7, Materialy.KamienMuru, Materialy.Lupek, "cegla", Materialy.Okno[1], 5) },
            { "huta", Opis(7, 8, Materialy.KamienMuru, Materialy.SkalaCzerwona, "cegla", Materialy.Okno[1], 5) },
            { "spichlerz", Opis(7, 10, Materialy.Deski, Materialy.Mosiadz, "deski", null, 6) },
            { "karawanseraj", Opis(8, 7, Materialy.Tynk, Materialy.Strzecha, "cegla", Materialy.Okno[2], 5) },
            { "tartak", Opis(7, 6, Materialy.Deski, Materialy.Strzecha, "deski", null, 4) },
            { "sklep", Opis(6, 7, Materialy.Tynk, Materialy.Dachowka, "cegla", Materialy.Okno[2], 5) },
            { "warsztat", Opis(6, 7, Materialy.Deski, Materialy.Lupek, "deski", Materialy.Okno[2], 5) },
            { "stajnie", Opis(8, 6, Materialy.Deski, Materialy.Strzecha, "deski", null, 4) },
            { "laboratorium", Opis(6, 8, Materialy.KamienMuru, Materialy.Lupek, "cegla", Materialy.Magia[2], 5) },
            { "tawerna", Opis(7, 8, Materialy.Deski, Materialy.Dachowka, "deski", Materialy.Okno[2], 5) },
            { "lecznica", Opis(6, 7, Materialy.Tynk, Materialy.Tynk, "cegla", Materialy.Okno[2], 5) },
            { "targ", Opis(8, 4, Materialy.Deski, Materialy.Mosiadz, "deski", null, 4) },
            { "wieza", Opis(4, 16, Materialy.KamienMuru, Materialy.Lupek, "cegla", null, 5) },
        };

        private static Wyglad Opis(int a, int h, Barwa[] sciana, Barwa[] dach, string faktura,
                                   Barwa? okno, int dachH)
        {
            return new Wyglad { A = a, H = h, Sciana = sciana, Dach = dach, Faktura = faktura, Okno = okno, DachH = dachH };
        }

        // ------------------------------------------------------------------ //
        //  Osadnik                                                            //
        // ------------------------------------------------------------------ //

        private static readonly string[] Osadnik =
        {
            "..ooo..",
            ".ohhho.",
            ".ohsho.",
            "..sss..",
            ".occco.",
            "occccco",
            "occccco",
            ".occco.",
            "..c.c..",
            "..b.b..",
            "..o.o..",
        };

        // Druga klatka marszu: nogi rozstawione. Razem z podskokiem daje kroki
        // bez osobnych rysunków na każdy kierunek — ludzik jest symetryczny.
        private static readonly string[] OsadnikKrok =
        {
            "..ooo..",
            ".ohhho.",
            ".ohsho.",
            "..sss..",
            ".occco.",
            "occccco",
            "occccco",
            ".occco.",
            ".c...c.",
            ".b...b.",
            ".o...o.",
        };

        /// <summary>Koszula po zajęciu — zawód widać z daleka, bez czytania podpisu.</summary>
        private static Barwa Koszula(string zajecie)
        {
            switch (zajecie)
            {
                case "drwal": return Materialy.SukmanaZielona[4];
                case "kamieniarz": return Materialy.Kamien[6];
                case "zielarz": return Materialy.Listowie[6];
                case "mysliwy": return Materialy.SkoraWyprawiona[5];
                case "rolnik": return Materialy.Strzecha[7];
                case "gornik": return Materialy.SukmanaFioletowa[4];
                case "tracz": return Materialy.Deski[6];
                case "hutnik": return Materialy.SukmanaCzerwona[3];
                case "handlarz": return Materialy.Mosiadz[6];
                case "rzemieslnik": return Materialy.SukmanaModra[5];
                case "straznik": return Materialy.SukmanaCzerwona[6];
                case "uzdrowiciel": return Materialy.Kosc[7];
                default: return Materialy.SukmanaSzara[4];
            }
        }

        /// <summary>
        /// Sylwetka osadnika. <paramref name="krok"/> 1 = nogi rozstawione,
        /// <paramref name="lezy"/> = śpi (obrócony o 90°).
        /// </summary>
        public Plotno Osadnika(string zajecie, bool chory, int krok, bool lezy)
        {
            string klucz = $"os|{zajecie}|{chory}|{krok}|{lezy}";
            if (_pamiec.TryGetValue(klucz, out Plotno gotowy))
            {
                return gotowy;
            }
            var paleta = new Dictionary<char, Barwa>
            {
                { 'o', Piksele.Kontur },
                { 'h', Materialy.Kora[3] },
                { 's', chory ? Materialy.Listowie[4] : Materialy.Skora[5] },
                { 'c', Koszula(zajecie ?? "") },
                { 'b', Materialy.Kora[2] },
            };
            Plotno p = Piksele.SpriteZTekstu(krok == 1 ? OsadnikKrok : Osadnik, paleta).Skaluj(Skala);
            if (lezy)
            {
                p = ObrocWLewo(p);
            }
            _pamiec[klucz] = p;
            return p;
        }

        /// <summary>Obrót o 90° przeciwnie do ruchu wskazówek — jak <c>pygame.transform.rotate(spr, 90)</c>.</summary>
        public static Plotno ObrocWLewo(Plotno z)
        {
            var wynik = new Plotno(z.Wysokosc, z.Szerokosc);
            for (int y = 0; y < z.Wysokosc; y++)
            {
                for (int x = 0; x < z.Szerokosc; x++)
                {
                    wynik.Ustaw(y, z.Szerokosc - 1 - x, z.Wez(x, y));
                }
            }
            return wynik;
        }

        /// <summary>Bohater: sylwetka klasy z klasycznego pixelartu, powiększona do skali sceny.</summary>
        public Plotno Bohater(string klasa)
        {
            string klucz = "bohater|" + (klasa ?? "");
            if (!_pamiec.TryGetValue(klucz, out Plotno p))
            {
                p = Piksele.Obrys(Teren.SpriteGracza(klasa).Skaluj(Skala));
                _pamiec[klucz] = p;
            }
            return p;
        }

        // ------------------------------------------------------------------ //
        //  Budynki                                                            //
        // ------------------------------------------------------------------ //

        /// <summary>Budynek o danym poziomie — rozbudowa rośnie w górę. Zwraca też środek podstawy.</summary>
        public Plotno Budynek(string klucz, int poziom, out (int X, int Y) podstawa)
        {
            if (!Wygladu.TryGetValue(klucz, out Wyglad w))
            {
                w = Wygladu["dom"];
            }
            int h = w.H + 2 * (poziom - 1);
            int szer = 2 * w.A + 6, wys = h + w.DachH + 8;
            int cx = szer / 2, cy = wys - 2;
            podstawa = (cx * Skala, cy * Skala);
            string k = $"bud|{klucz}|{poziom}";
            if (_pamiec.TryGetValue(k, out Plotno gotowy))
            {
                return gotowy;
            }
            var p = new Plotno(szer, wys);
            Piksele.Domek(p, cx, cy, w.A, h, w.Sciana, w.Dach, (int)(RytmDnia.LosImienia(klucz) % 97),
                          w.Faktura, okno: w.Okno, dachH: w.DachH);
            Plotno wynik = Piksele.Obrys(p.Skaluj(Skala));
            _pamiec[k] = wynik;
            return wynik;
        }

        /// <summary>Chata — trzy warianty dachu na przemian. Środek podstawy w (10, 23) pikselartu.</summary>
        public Plotno Chata(int i, out (int X, int Y) podstawa)
        {
            podstawa = (10 * Skala, 23 * Skala);
            string k = $"chata|{i % 3}";
            if (_pamiec.TryGetValue(k, out Plotno gotowy))
            {
                return gotowy;
            }
            Barwa[][] dachy = { Materialy.Dachowka, Materialy.Mosiadz, Materialy.Lupek };
            var p = new Plotno(20, 26);
            Piksele.Domek(p, 10, 23, 6, 6, Materialy.Deski, dachy[i % 3], 40 + i, "deski",
                          okno: Materialy.Okno[2], dachH: 5);
            Plotno wynik = Piksele.Obrys(p.Skaluj(Skala));
            _pamiec[k] = wynik;
            return wynik;
        }

        /// <summary>Namiot — zanim stanie dom, w obozie śpi się pod płótnem. Środek podstawy w (13, 21).</summary>
        public Plotno Namiot(out (int X, int Y) podstawa)
        {
            podstawa = (13 * Skala, 21 * Skala);
            const string k = "namiot";
            if (_pamiec.TryGetValue(k, out Plotno gotowy))
            {
                return gotowy;
            }
            Plotno wynik = Piksele.Obrys(Teren.Namiot().Skaluj(Skala));
            _pamiec[k] = wynik;
            return wynik;
        }

        // ------------------------------------------------------------------ //
        //  Cień                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Półprzezroczysta elipsa pod obiektem, wysunięta w prawo: światło pada
        /// z lewego górnego rogu, więc cień kładzie się w prawo i lekko w dół.
        /// </summary>
        public Plotno Cien(int szer, int wys, int alfa = 96)
        {
            string k = $"cien|{szer}|{wys}|{alfa}";
            if (_pamiec.TryGetValue(k, out Plotno gotowy))
            {
                return gotowy;
            }
            var p = new Plotno(szer, wys);
            var kolor = new Barwa(0, 0, 0, alfa);
            double rx = szer / 2.0, ry = wys / 2.0;
            for (int y = 0; y < wys; y++)
            {
                for (int x = 0; x < szer; x++)
                {
                    double dx = (x + 0.5 - rx) / rx, dy = (y + 0.5 - ry) / ry;
                    if (dx * dx + dy * dy <= 1.0)
                    {
                        p.Ustaw(x, y, kolor);
                    }
                }
            }
            _pamiec[k] = p;
            return p;
        }
    }
}
