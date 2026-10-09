using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Styl graficzny gry: wszystkie barwy i stałe wyglądu w jednym miejscu.
    ///
    /// Renderer jest proceduralny, więc „zmiana grafiki” to zmiana liczb, nie
    /// podmiana plików. Trzymanie ich tutaj daje dwie rzeczy naraz: da się
    /// przełączać wygląd gry jednym przypisaniem, a testy porównujące piksele
    /// z wersją pythonową mogą dalej działać na stylu
    /// <see cref="Klasyczny"/> — bo ten jest co do bajtu tym, co rysował
    /// Python, i taki ma zostać.
    ///
    /// Rysujące klasy (<see cref="Teren"/>, <see cref="ScenaMapy"/>,
    /// <see cref="Pulpit"/>) czytają <see cref="Aktywny"/>. To stan globalny,
    /// ale styl jest globalny z natury — alternatywą byłoby przepychanie go
    /// przez kilkadziesiąt funkcji rysujących, co nic nie daje.
    /// </summary>
    public sealed class Styl
    {
        public string Nazwa;

        /// <summary>Generator kafli, obiektów i postaci — „pędzel” tego stylu.</summary>
        public IGeneratorTerenu Generator;

        // ---- teren ------------------------------------------------------- //

        /// <summary>Rampa czterech barw na biom, od cienia do światła.</summary>
        public Dictionary<string, Barwa[]> Rampy;

        public Barwa[] Ziemia;
        public Barwa[] Kamien;
        public Barwa[] Czerwien;
        public Barwa[] Woda;

        /// <summary>Wysokość bloku terenu w pikselach, per biom.</summary>
        public Dictionary<string, int> WysokosciBiomow;

        // ---- budynki ----------------------------------------------------- //

        public Barwa[] Tynk;
        public Barwa[] Drewno;
        public Barwa[] Dachowka;
        public Barwa[] Lupek;
        public Barwa[] Zloto;

        // ---- scena ------------------------------------------------------- //

        public Barwa NieboGora;
        public Barwa NieboDol;
        public Barwa NieboGoraZmierzch;
        public Barwa NieboDolZmierzch;

        /// <summary>Jasność otoczenia o zmierzchu — im ciemniejsza, tym mocniej liczą się światła.</summary>
        public Barwa SwiatloOtoczenia;

        /// <summary>Rodzaj poświaty → promień i barwa.</summary>
        public Dictionary<string, (int Promien, Barwa Kolor)> Blaski;

        /// <summary>Barwa konturu wokół brył; w ciemnym stylu bywa inna niż czerń.</summary>
        public Barwa Kontur;

        // ---- chrom interfejsu -------------------------------------------- //

        public Barwa Tlo;
        public Barwa TloPanelu;
        public Barwa RamkaJasna;
        public Barwa RamkaCiemna;
        public Barwa Cwiek;
        public Barwa Wyrozniony;
        public Barwa Tekst;
        public Barwa Przygaszony;
        public Barwa PasekHp;
        public Barwa PasekMany;

        // ---- obróbka końcowa --------------------------------------------- //

        /// <summary>
        /// Paleta, do której sprowadzana jest gotowa klatka, albo null.
        ///
        /// Jeden wspólny zestaw barw dla całego obrazu scala wygląd mocniej
        /// niż cokolwiek innego — tak robiły gry z lat 90., bo musiały.
        /// </summary>
        public Barwa[] PaletaGlowna;

        /// <summary>Siła ziarna dosypywanego do gotowej klatki (0 = brak).</summary>
        public double Ziarno;

        private static Styl _aktywny;

        /// <summary>
        /// Styl, którym rysuje gra. Podmiana działa od następnej klatki.
        ///
        /// Domyślna wartość jest brana leniwie, a nie w inicjalizatorze pola:
        /// pola statyczne powstają w kolejności deklaracji, więc przypisanie
        /// <c>= Klasyczny</c> tutaj wzięłoby jeszcze niezainicjowane null
        /// i cała grafika sypałaby się na pierwszym rysowaniu.
        /// </summary>
        public static Styl Aktywny
        {
            get { return _aktywny ?? (_aktywny = Klasyczny); }
            set { _aktywny = value; }
        }

        // ------------------------------------------------------------------ //
        //  Klasyczny — wygląd przeniesiony z wersji pythonowej                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Wygląd jeden do jednego z <c>grafika/</c> w Pythonie.
        ///
        /// Tych liczb nie wolno ruszać: na nich stoją testy porównujące kafle
        /// i sprite'y piksel w piksel z oryginałem. Nowy wygląd dodaje się
        /// jako osobny styl, nie przez podmianę tych wartości.
        /// </summary>
        public static readonly Styl Klasyczny = new Styl
        {
            Nazwa = "klasyczny",
            Generator = GeneratorKlasyczny.Instancja,

            Rampy = new Dictionary<string, Barwa[]>
            {
                { "równiny", new[] { new Barwa(46, 78, 42), new Barwa(70, 112, 50), new Barwa(104, 148, 60), new Barwa(146, 180, 78) } },
                { "las", new[] { new Barwa(26, 50, 36), new Barwa(38, 72, 44), new Barwa(56, 98, 52), new Barwa(82, 128, 62) } },
                { "bagna", new[] { new Barwa(30, 48, 50), new Barwa(42, 66, 62), new Barwa(58, 88, 74), new Barwa(84, 114, 86) } },
                { "wzgórza", new[] { new Barwa(66, 76, 60), new Barwa(94, 106, 76), new Barwa(124, 136, 92), new Barwa(160, 166, 116) } },
                { "kanion", new[] { new Barwa(112, 58, 40), new Barwa(152, 86, 50), new Barwa(190, 120, 70), new Barwa(222, 164, 102) } },
                { "ruiny", new[] { new Barwa(66, 64, 58), new Barwa(92, 88, 78), new Barwa(120, 114, 100), new Barwa(152, 146, 126) } },
            },

            Ziemia = new[] { new Barwa(48, 34, 28), new Barwa(74, 52, 36), new Barwa(100, 72, 46), new Barwa(124, 92, 58) },
            Kamien = new[] { new Barwa(52, 54, 60), new Barwa(80, 82, 88), new Barwa(110, 112, 116), new Barwa(146, 146, 146) },
            Czerwien = new[] { new Barwa(90, 44, 34), new Barwa(128, 64, 42), new Barwa(164, 92, 56), new Barwa(196, 128, 80) },
            Woda = new[] { new Barwa(22, 44, 58), new Barwa(32, 66, 82), new Barwa(52, 96, 110), new Barwa(120, 170, 176) },

            WysokosciBiomow = new Dictionary<string, int>
            {
                { "równiny", 4 }, { "las", 5 }, { "bagna", 1 },
                { "wzgórza", 11 }, { "kanion", 8 }, { "ruiny", 4 },
            },

            Tynk = new[] { new Barwa(120, 104, 86), new Barwa(164, 146, 120), new Barwa(206, 190, 160), new Barwa(232, 222, 196) },
            Drewno = new[] { new Barwa(70, 46, 32), new Barwa(104, 70, 44), new Barwa(140, 98, 60), new Barwa(170, 126, 80) },
            Dachowka = new[] { new Barwa(80, 34, 30), new Barwa(120, 52, 38), new Barwa(160, 74, 48), new Barwa(196, 104, 66) },
            Lupek = new[] { new Barwa(40, 46, 60), new Barwa(58, 66, 84), new Barwa(80, 90, 110), new Barwa(110, 120, 138) },
            Zloto = new[] { new Barwa(150, 120, 60), new Barwa(196, 160, 80), new Barwa(228, 196, 110), new Barwa(250, 230, 160) },

            NieboGora = new Barwa(120, 170, 210),
            NieboDol = new Barwa(196, 214, 222),
            NieboGoraZmierzch = new Barwa(30, 28, 58),
            NieboDolZmierzch = new Barwa(86, 58, 84),
            SwiatloOtoczenia = new Barwa(112, 104, 168),

            Blaski = new Dictionary<string, (int, Barwa)>
            {
                { "ogien", (44, new Barwa(255, 150, 70)) },
                { "okno", (20, new Barwa(255, 190, 110)) },
                { "kuznia", (26, new Barwa(255, 120, 50)) },
                { "magia", (30, new Barwa(170, 90, 255)) },
                { "krew", (26, new Barwa(255, 60, 50)) },
                { "latarnia", (30, new Barwa(220, 190, 140)) },
            },

            Kontur = new Barwa(18, 16, 24),

            Tlo = new Barwa(14, 12, 20),
            TloPanelu = new Barwa(20, 17, 28),
            RamkaJasna = new Barwa(150, 124, 86),
            RamkaCiemna = new Barwa(70, 56, 44),
            Cwiek = new Barwa(226, 196, 120),
            Wyrozniony = new Barwa(232, 200, 130),
            Tekst = new Barwa(224, 216, 200),
            Przygaszony = new Barwa(150, 142, 132),
            PasekHp = new Barwa(196, 52, 52),
            PasekMany = new Barwa(70, 110, 220),

            PaletaGlowna = null,
            Ziarno = 0.0,
        };
    }
}
