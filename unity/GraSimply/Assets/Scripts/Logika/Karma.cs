using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>
    /// Karma — reputacja bohatera i jej realne skutki w świecie: ceny u kupców,
    /// opór przy rekrutacji i reakcja świątyń.
    ///
    /// Skala jest symetryczna wokół zera. Dodatnia karma = ludzie ci ufają,
    /// ujemna = boją się ciebie i liczą sobie drożej, ale strach też bywa
    /// argumentem.
    /// </summary>
    public static class Karma
    {
        /// <summary>Próg dolny, klucz, nazwa, ikona — pierwszy pasujący od góry wygrywa.</summary>
        private static readonly (int Prog, string Klucz, string Nazwa, string Ikona)[] Progi =
        {
            (12, "swiety", "Święty", "😇"),
            (5, "prawy", "Prawy", "🕊"),
            (-4, "neutralny", "Neutralny", "⚖"),
            (-11, "podejrzany", "Podejrzany", "🌑"),
            (-1000000000, "okrutny", "Okrutny", "💀"),
        };

        /// <summary>Maksymalna zmiana ceny w obie strony (±15%).</summary>
        private const double MaxModyfikatorCen = 0.15;

        /// <summary>Karma, przy której modyfikator cen osiąga maksimum.</summary>
        private const int KarmaNasycenia = 15;

        private static readonly Dictionary<string, int> BonusySwiatyni =
            new Dictionary<string, int>
            {
                { "swiety", 25 }, { "prawy", 10 }, { "neutralny", 0 },
                { "podejrzany", -10 }, { "okrutny", -20 },
            };

        public static (string Klucz, string Nazwa, string Ikona) Poziom(Gracz gracz)
        {
            int k = gracz.Karma;
            foreach ((int prog, string klucz, string nazwa, string ikona) in Progi)
            {
                if (k >= prog)
                {
                    return (klucz, nazwa, ikona);
                }
            }
            return ("okrutny", "Okrutny", "💀");
        }

        /// <summary>Krótki opis reputacji do nagłówków menu.</summary>
        public static string Etykieta(Gracz gracz)
        {
            (string _, string nazwa, string ikona) = Poziom(gracz);
            int k = gracz.Karma;
            return $"{ikona} {nazwa} ({(k >= 0 ? "+" : "")}{k})";
        }

        private static double Znormalizowana(Gracz gracz)
        {
            double k = gracz.Karma / (double)KarmaNasycenia;
            return Math.Max(-1.0, Math.Min(1.0, k));
        }

        /// <summary>Mnożnik cen u kupców z zakresu [0.85, 1.15].</summary>
        public static double MnoznikCen(Gracz gracz)
        {
            return 1.0 - MaxModyfikatorCen * Znormalizowana(gracz);
        }

        public static string OpisCen(Gracz gracz)
        {
            double mnoznik = MnoznikCen(gracz);
            int procent = (int)Math.Round(Math.Abs(1.0 - mnoznik) * 100, MidpointRounding.AwayFromZero);
            if (procent < 1)
            {
                return "";
            }
            return mnoznik < 1.0
                ? $"  🕊  Twoja sława otwiera sakiewki kupców: ceny niższe o {procent}%."
                : $"  🌑  Kupcy patrzą na ciebie krzywo: ceny wyższe o {procent}%.";
        }

        /// <summary>
        /// Zmiana ST próby przekonania NPC do dołączenia. Dobra sława ułatwia,
        /// zła utrudnia — porządni ludzie nie garną się do kogoś, kto zostawia
        /// za sobą trupy.
        /// </summary>
        public static int ModyfikatorRekrutacji(Gracz gracz)
        {
            int k = gracz.Karma;
            if (k >= 12)
            {
                return -3;
            }
            if (k >= 5)
            {
                return -1;
            }
            if (k <= -12)
            {
                return 3;
            }
            if (k <= -5)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>Lustrzane odbicie rekrutacji: kogo nie da się przekonać, tego można złamać.</summary>
        public static int ModyfikatorZastraszania(Gracz gracz)
        {
            return -ModyfikatorRekrutacji(gracz);
        }

        public static int BonusSwiatyni(Gracz gracz)
        {
            return BonusySwiatyni[Poziom(gracz).Klucz];
        }
    }
}
