namespace GraSimply.Logika
{
    /// <summary>Jedna pora roku i jej wpływ na osadę.</summary>
    public sealed class Pora
    {
        public string Klucz;
        public string Nazwa;
        public string Ikona;
        public double Zbiory;
        public double Rolnictwo;
        public bool Opal;
        public double Zagrozenie;
        public double CenyZywnosci;
        public string Opis;
    }

    /// <summary>
    /// Kalendarz świata: rok = cztery pory po 30 dni.
    ///
    /// Pora roku zmienia zbiory, rolnictwo, potrzebę opału, ceny żywności
    /// i to, jak często bandy ruszają na osady. Zima jest sprawdzianem zapasów.
    /// </summary>
    public static class Kalendarz
    {
        public const int DniPory = 30;
        public const int DniRoku = 4 * DniPory;

        public static readonly Pora[] Pory =
        {
            new Pora
            {
                Klucz = "wiosna", Nazwa = "Wiosna", Ikona = "🌱",
                Zbiory = 1.0, Rolnictwo = 1.0, Opal = false,
                Zagrozenie = 1.0, CenyZywnosci = 1.0,
                Opis = "Ziemia odmarza. Czas siać i odbudowywać zapasy.",
            },
            new Pora
            {
                Klucz = "lato", Nazwa = "Lato", Ikona = "☀",
                Zbiory = 1.2, Rolnictwo = 1.2, Opal = false,
                Zagrozenie = 1.1, CenyZywnosci = 0.9,
                Opis = "Długie dni. Drogi suche, bandy ruchliwe.",
            },
            new Pora
            {
                Klucz = "jesien", Nazwa = "Jesień", Ikona = "🍂",
                Zbiory = 1.1, Rolnictwo = 1.6, Opal = false,
                Zagrozenie = 1.0, CenyZywnosci = 0.75,
                Opis = "Żniwa. Co zbierzesz teraz, zjesz zimą.",
            },
            new Pora
            {
                Klucz = "zima", Nazwa = "Zima", Ikona = "❄",
                Zbiory = 0.4, Rolnictwo = 0.0, Opal = true,
                Zagrozenie = 1.3, CenyZywnosci = 1.5,
                Opis = "Mróz. Pola śpią, opał znika, głodne bandy schodzą z gór.",
            },
        };

        public static Pora PoraDnia(int dzien)
        {
            return Pory[(dzien % DniRoku) / DniPory];
        }

        public static Pora PoraGracza(Gracz gracz)
        {
            return PoraDnia(gracz.Czas);
        }

        public static int Rok(int dzien)
        {
            return dzien / DniRoku + 1;
        }

        public static int DzienPory(int dzien)
        {
            return dzien % DniPory + 1;
        }

        /// <summary>Ile dni do zimy; 0 = już zima.</summary>
        public static int DniDoZimy(int dzien)
        {
            int d = dzien % DniRoku;
            int startZimy = 3 * DniPory;
            return d >= startZimy ? 0 : startZimy - d;
        }

        public static string OpisDaty(int dzien)
        {
            Pora p = PoraDnia(dzien);
            return $"{p.Ikona} {p.Nazwa}, dzień {DzienPory(dzien)}/{DniPory} · rok {Rok(dzien)}";
        }
    }
}
