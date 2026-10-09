using System.Collections.Generic;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>
    /// Materiały stylu „mroczne fantasy w duchu Fallouta”: rampy barw od cienia
    /// do światła, zbudowane z reguł, a nie wzięte z gotowej palety.
    ///
    /// Reguły z rozpoznania oryginału: grunt siedzi w wąskim, ciepłym pasie
    /// (odcień ok. 30°, nasycenie do ~0,46, jasność 40–140/255), zieleń jest
    /// zawsze oliwkowa (odcień 80–100°), jasne tony są przygaszone, a pełne
    /// nasycenie pojawia się tylko w prawie-czerni. Świat zostaje fantasy —
    /// las jest lasem — ale pokazanym przez ten sam kurz i ochrę.
    ///
    /// Barwy samoświecące (ogień, okna, magia, runy) są osobno: omija je
    /// przyciemnianie nocą, więc muszą być rozpoznawalne po samej wartości.
    /// Dlatego nie leżą na żadnej innej rampie.
    /// </summary>
    public static class Materialy
    {
        // ---- grunt ------------------------------------------------------- //

        /// <summary>Ziemia — dominujący ton całego świata.</summary>
        public static readonly Barwa[] Ziemia = Paleta.Rampa(32, 0.24, 0.46, 0.10, 0.54, 10, -8);

        /// <summary>Ściółka leśna: brąz z domieszką oliwki.</summary>
        public static readonly Barwa[] Sciolka = Paleta.Rampa(40, 0.26, 0.48, 0.08, 0.46, 10, -10);

        /// <summary>Sucha trawa i poszycie — oliwka przechodząca w khaki.</summary>
        public static readonly Barwa[] Trawa = Paleta.Rampa(72, 0.30, 0.52, 0.10, 0.52, 10, 14);

        /// <summary>Żywa zieleń: liście, mech. Najjaśniejszy stopień to maksimum, jakie dopuszcza ten styl.</summary>
        public static readonly Barwa[] Listowie = Paleta.Rampa(82, 0.34, 0.60, 0.07, 0.58, 10, 14);

        /// <summary>Igliwie — chłodniejsza, ciemniejsza zieleń.</summary>
        public static readonly Barwa[] Igliwie = Paleta.Rampa(100, 0.30, 0.62, 0.05, 0.42, 9, 10);

        /// <summary>Muł bagienny.</summary>
        public static readonly Barwa[] Mul = Paleta.Rampa(48, 0.22, 0.42, 0.07, 0.38, 9, -6);

        /// <summary>Woda bagienna — mętna, zielonobrązowa, nie błękitna.</summary>
        public static readonly Barwa[] WodaBagienna = Paleta.Rampa(96, 0.20, 0.45, 0.07, 0.40, 8, 20);

        /// <summary>Czerwona ochra kanionu; ciemniejąc idzie ku cegle.</summary>
        public static readonly Barwa[] Ochra = Paleta.Rampa(20, 0.34, 0.66, 0.10, 0.60, 10, -10);

        /// <summary>Pył kamienny ruin i wzgórz — prawie achromatyczny, ciepły.</summary>
        public static readonly Barwa[] Pyl = Paleta.Rampa(38, 0.08, 0.20, 0.10, 0.56, 10, -4);

        // ---- kamień, drewno, budulec ------------------------------------- //

        public static readonly Barwa[] Kamien = Paleta.Rampa(40, 0.05, 0.14, 0.08, 0.60, 10, -6);

        /// <summary>Kamień zimniejszy — na mury i kolumny, żeby odcinały się od skał.</summary>
        public static readonly Barwa[] KamienMuru = Paleta.Rampa(60, 0.07, 0.12, 0.09, 0.62, 10, 20);

        public static readonly Barwa[] SkalaCzerwona = Paleta.Rampa(16, 0.30, 0.62, 0.09, 0.58, 10, -8);

        /// <summary>Kora i martwe drewno.</summary>
        public static readonly Barwa[] Kora = Paleta.Rampa(28, 0.30, 0.62, 0.06, 0.50, 9, -8);

        /// <summary>Deski, belki, obrobione drewno — jaśniejsze i cieplejsze od kory.</summary>
        public static readonly Barwa[] Deski = Paleta.Rampa(32, 0.32, 0.58, 0.10, 0.58, 9, -6);

        public static readonly Barwa[] Tynk = Paleta.Rampa(40, 0.14, 0.34, 0.20, 0.70, 9, -6);

        /// <summary>Strzecha i słoma.</summary>
        public static readonly Barwa[] Strzecha = Paleta.Rampa(42, 0.34, 0.56, 0.12, 0.62, 9, -10);

        public static readonly Barwa[] Dachowka = Paleta.Rampa(14, 0.34, 0.66, 0.10, 0.52, 9, -6);

        public static readonly Barwa[] Lupek = Paleta.Rampa(205, 0.08, 0.20, 0.08, 0.46, 8, 10);

        public static readonly Barwa[] Zelazo = Paleta.Rampa(130, 0.04, 0.14, 0.06, 0.62, 9, 10);

        public static readonly Barwa[] Mosiadz = Paleta.Rampa(40, 0.42, 0.72, 0.12, 0.66, 9, -8);

        public static readonly Barwa[] Kosc = Paleta.Rampa(44, 0.10, 0.24, 0.30, 0.78, 8, -6);

        // ---- postaci ----------------------------------------------------- //

        public static readonly Barwa[] Skora = Paleta.Rampa(24, 0.28, 0.52, 0.18, 0.72, 9, -6);

        /// <summary>Skóra wyprawiona: pasy, buty, karwasze.</summary>
        public static readonly Barwa[] SkoraWyprawiona = Paleta.Rampa(28, 0.34, 0.64, 0.07, 0.46, 9, -8);

        public static readonly Barwa[] SukmanaCzerwona = Paleta.Rampa(4, 0.34, 0.68, 0.08, 0.50, 9, -6);

        /// <summary>Jedyny chłodny akcent — dopuszczalny w drobnej ilości, na szacie maga.</summary>
        public static readonly Barwa[] SukmanaModra = Paleta.Rampa(222, 0.24, 0.50, 0.08, 0.46, 9, 6);

        public static readonly Barwa[] SukmanaZielona = Paleta.Rampa(88, 0.26, 0.52, 0.07, 0.44, 9, 12);

        public static readonly Barwa[] SukmanaSzara = Paleta.Rampa(48, 0.08, 0.20, 0.08, 0.48, 9, -4);

        public static readonly Barwa[] SukmanaFioletowa = Paleta.Rampa(286, 0.18, 0.42, 0.06, 0.38, 9, 8);

        // ---- barwy samoświecące ------------------------------------------ //

        /// <summary>
        /// Ogień od żaru do jądra płomienia. Te barwy nie leżą na żadnej rampie
        /// materiału, więc oświetlenie rozpoznaje je po wartości i nie przyciemnia
        /// — ognisko świeci nocą pełną jasnością, jak w oryginale.
        /// </summary>
        public static readonly Barwa[] Ogien =
        {
            S6(0x93, 0x2B, 0x0C), S6(0xD4, 0x00, 0x00), S6(0xFC, 0x38, 0x00),
            S6(0xFC, 0x74, 0x00), S6(0xFC, 0xB0, 0x30), S6(0xFC, 0xE0, 0x80),
        };

        /// <summary>Ciepłe światło w oknach i paleniskach.</summary>
        public static readonly Barwa[] Okno =
        {
            S6(0xC8, 0x6C, 0x14), S6(0xF0, 0x98, 0x2C), S6(0xFC, 0xC4, 0x5C),
        };

        /// <summary>Poświata magii: portal, kostur maga.</summary>
        public static readonly Barwa[] Magia =
        {
            S6(0x58, 0x20, 0xA8), S6(0x8C, 0x48, 0xF0), S6(0xC4, 0x94, 0xFC),
        };

        /// <summary>Czerwone runy legowiska bossa.</summary>
        public static readonly Barwa[] Runy =
        {
            S6(0x8C, 0x00, 0x08), S6(0xE4, 0x14, 0x14), S6(0xFC, 0x70, 0x50),
        };

        private static Barwa S6(int r, int g, int b)
        {
            return Paleta.Siatka6(r, g, b);
        }

        /// <summary>Barwy, których nocą nie przyciemniamy.</summary>
        public static readonly HashSet<int> Samoswiecace = Zbierz(Ogien, Okno, Magia, Runy);

        public static bool CzySamoswiecaca(byte r, byte g, byte b)
        {
            return Samoswiecace.Contains((r << 16) | (g << 8) | b);
        }

        private static HashSet<int> Zbierz(params Barwa[][] rampy)
        {
            var zbior = new HashSet<int>();
            foreach (Barwa[] rampa in rampy)
            {
                foreach (Barwa b in rampa)
                {
                    zbior.Add((b.R << 16) | (b.G << 8) | b.B);
                }
            }
            return zbior;
        }

        /// <summary>Wszystkie rampy materiałów — z nich (plus chrom) powstaje paleta główna.</summary>
        public static IEnumerable<Barwa[]> WszystkieRampy()
        {
            return new[]
            {
                Ziemia, Sciolka, Trawa, Listowie, Igliwie, Mul, WodaBagienna, Ochra, Pyl,
                Kamien, KamienMuru, SkalaCzerwona, Kora, Deski, Tynk, Strzecha, Dachowka, Lupek,
                Zelazo, Mosiadz, Kosc, Skora, SkoraWyprawiona, SukmanaCzerwona, SukmanaModra,
                SukmanaZielona, SukmanaSzara, SukmanaFioletowa, Ogien, Okno, Magia, Runy,
            };
        }
    }
}
