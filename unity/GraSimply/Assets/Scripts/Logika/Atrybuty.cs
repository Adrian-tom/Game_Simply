using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    public sealed class OpisAtrybutu
    {
        public string Nazwa;
        public string Skrot;
        public string Ikona;
        public string Opis;
    }

    public sealed class OpisSkilla
    {
        public string Nazwa;
        public string Atrybut;
        public string Ikona;
        public string Opis;
    }

    public sealed class WynikTestu
    {
        public string Skill;
        public int St;
        public int Rzut;
        public int Premia;
        public int Suma;
        public bool Sukces;
        public bool Krytyczny;
        public bool Wpadka;
    }

    /// <summary>Atrybuty i testy umiejętności w stylu Baldur's Gate 3 (k20 + premia).</summary>
    public static class Atrybuty
    {
        public const int MaxAtrybut = 20;
        public const int MinAtrybut = 8;

        public static readonly string[] Kolejnosc =
        {
            "sila", "zrecznosc", "kondycja", "inteligencja", "madrosc", "charyzma",
        };

        public static readonly Dictionary<string, OpisAtrybutu> Opisy =
            new Dictionary<string, OpisAtrybutu>
            {
                { "sila", new OpisAtrybutu { Nazwa = "Siła", Skrot = "SIL", Ikona = "💪", Opis = "Wspinaczka, pchanie, siła ciosu" } },
                { "zrecznosc", new OpisAtrybutu { Nazwa = "Zręczność", Skrot = "ZRĘ", Ikona = "🏃", Opis = "Uniki, zamki, trafienia krytyczne" } },
                { "kondycja", new OpisAtrybutu { Nazwa = "Kondycja", Skrot = "KON", Ikona = "❤️", Opis = "Wytrzymałość i zapas HP" } },
                { "inteligencja", new OpisAtrybutu { Nazwa = "Inteligencja", Skrot = "INT", Ikona = "🧠", Opis = "Wiedza, magia, rozwiązywanie zagadek" } },
                { "madrosc", new OpisAtrybutu { Nazwa = "Mądrość", Skrot = "MDR", Ikona = "🦉", Opis = "Spostrzegawczość i przetrwanie" } },
                { "charyzma", new OpisAtrybutu { Nazwa = "Charyzma", Skrot = "CHA", Ikona = "✨", Opis = "Perswazja, zastraszanie, oszustwo" } },
            };

        public static readonly Dictionary<string, OpisSkilla> Skille =
            new Dictionary<string, OpisSkilla>
            {
                { "atletyka", new OpisSkilla { Nazwa = "Atletyka", Atrybut = "sila", Ikona = "🧗", Opis = "Wspinaczka, skoki, siłowe próby" } },
                { "akrobatyka", new OpisSkilla { Nazwa = "Akrobatyka", Atrybut = "zrecznosc", Ikona = "🤸", Opis = "Uniki, równowaga, zejście z ciosu" } },
                { "zwinne_palce", new OpisSkilla { Nazwa = "Zwinne palce", Atrybut = "zrecznosc", Ikona = "🔑", Opis = "Zamki, kradzież, rozbrajanie pułapek" } },
                { "spostrzegawczosc", new OpisSkilla { Nazwa = "Spostrzegawczość", Atrybut = "madrosc", Ikona = "👁", Opis = "Ukryte przejścia, pułapki, kłamstwa" } },
                { "przetrwanie", new OpisSkilla { Nazwa = "Przetrwanie", Atrybut = "madrosc", Ikona = "🐾", Opis = "Dzicz, tropy, żywioły" } },
                { "perswazja", new OpisSkilla { Nazwa = "Perswazja", Atrybut = "charyzma", Ikona = "💬", Opis = "Negocjacje i przekonywanie" } },
                { "zastraszanie", new OpisSkilla { Nazwa = "Zastraszanie", Atrybut = "charyzma", Ikona = "😠", Opis = "Groźby i dominacja" } },
                { "oszustwo", new OpisSkilla { Nazwa = "Oszustwo", Atrybut = "charyzma", Ikona = "🃏", Opis = "Blef, kłamstwo, zgrywanie roli" } },
            };

        /// <summary>Start jak w BG3: 8–16, klasa ma jedną „główną” statystykę.</summary>
        private static readonly Dictionary<string, int[]> StartAtrybutow =
            new Dictionary<string, int[]>
            {
                // kolejność jak w Kolejnosc: SIL, ZRĘ, KON, INT, MDR, CHA
                { "Wojownik", new[] { 16, 12, 15, 8, 10, 10 } },
                { "Mag", new[] { 8, 12, 12, 16, 13, 10 } },
                { "Lotrzyk", new[] { 8, 16, 12, 12, 10, 14 } },
                { "Druid", new[] { 10, 12, 13, 10, 16, 10 } },
                { "Nekromanta", new[] { 8, 12, 12, 16, 10, 13 } },
            };

        private static readonly Dictionary<string, string[]> BieglosciKlas =
            new Dictionary<string, string[]>
            {
                { "Wojownik", new[] { "atletyka", "zastraszanie" } },
                { "Mag", new[] { "spostrzegawczosc", "perswazja" } },
                { "Lotrzyk", new[] { "zwinne_palce", "akrobatyka", "oszustwo" } },
                { "Druid", new[] { "przetrwanie", "spostrzegawczosc" } },
                { "Nekromanta", new[] { "zastraszanie", "oszustwo" } },
            };

        public static Dictionary<string, int> StartoweAtrybuty(string klasa)
        {
            if (!StartAtrybutow.TryGetValue(klasa, out int[] baza))
            {
                baza = StartAtrybutow["Wojownik"];
            }
            var wynik = new Dictionary<string, int>();
            for (int i = 0; i < Kolejnosc.Length; i++)
            {
                wynik[Kolejnosc[i]] = baza[i];
            }
            return wynik;
        }

        public static List<string> BiegleSkilleKlasy(string klasa)
        {
            return BieglosciKlas.TryGetValue(klasa, out string[] lista)
                ? new List<string>(lista)
                : new List<string>();
        }

        /// <summary>Uzupełnia brakujące pola (nowa postać albo stary zapis).</summary>
        public static void Zapewnij(Gracz gracz)
        {
            if (gracz.Atrybuty == null || gracz.Atrybuty.Count == 0)
            {
                gracz.Atrybuty = StartoweAtrybuty(gracz.Klasa);
            }
            else
            {
                Dictionary<string, int> start = StartoweAtrybuty(gracz.Klasa);
                foreach (KeyValuePair<string, int> para in start)
                {
                    if (!gracz.Atrybuty.ContainsKey(para.Key))
                    {
                        gracz.Atrybuty[para.Key] = para.Value;
                    }
                }
                foreach (string klucz in gracz.Atrybuty.Keys.ToList())
                {
                    gracz.Atrybuty[klucz] = Math.Max(1, Math.Min(MaxAtrybut, gracz.Atrybuty[klucz]));
                }
            }
            if (gracz.BiegleSkille == null || gracz.BiegleSkille.Count == 0)
            {
                gracz.BiegleSkille = BiegleSkilleKlasy(gracz.Klasa);
            }
        }

        public static int Wartosc(Gracz gracz, string atrybut)
        {
            Zapewnij(gracz);
            return gracz.Atrybuty.TryGetValue(atrybut, out int v) ? v : 10;
        }

        /// <summary>Premia D&amp;D: (wartość − 10) // 2, z dzieleniem w dół jak w Pythonie.</summary>
        public static int Modyfikator(Gracz gracz, string atrybut)
        {
            return PodzielWDol(Wartosc(gracz, atrybut) - 10, 2);
        }

        /// <summary>
        /// Dzielenie całkowite w dół — Python dzieli <c>//</c> w stronę minus
        /// nieskończoności, C# obcina do zera. Przy atrybucie 8 daje to −1
        /// kontra 0, więc różnica jest widoczna w rozgrywce.
        /// </summary>
        public static int PodzielWDol(int a, int b)
        {
            int iloraz = a / b;
            if ((a % b != 0) && ((a < 0) != (b < 0)))
            {
                iloraz--;
            }
            return iloraz;
        }

        public static string TekstModyfikatora(int mod)
        {
            return mod >= 0 ? $"+{mod}" : mod.ToString();
        }

        /// <summary>Premia z biegłości jak w 5e / BG3.</summary>
        public static int Bieglosc(Gracz gracz)
        {
            int poziom = Math.Max(1, gracz.Poziom);
            return 2 + (poziom - 1) / 4;
        }

        public static bool CzyBiegly(Gracz gracz, string skill)
        {
            Zapewnij(gracz);
            return gracz.BiegleSkille != null && gracz.BiegleSkille.Contains(skill);
        }

        /// <summary>
        /// Premia do testu danego skilla.
        ///
        /// Wersja pythonowa dolicza tu jeszcze cechy pochodzenia, myśli
        /// z gabinetu i talent „czytanie ludzi”. Te systemy nie są jeszcze
        /// przeniesione (etap 2), więc na razie zostaje atrybut, biegłość
        /// i kara z ran — reszta dojdzie razem ze swoimi modułami.
        /// </summary>
        public static int PremiaSkilla(Gracz gracz, string skill)
        {
            OpisSkilla info = Skille[skill];
            int premia = Modyfikator(gracz, info.Atrybut);
            if (CzyBiegly(gracz, skill))
            {
                premia += Bieglosc(gracz);
            }
            premia += Przetrwanie.KaraTestu(gracz, info.Atrybut);
            return premia;
        }

        /// <summary>ST rośnie lekko z numerem regionu.</summary>
        public static int Trudnosc(Gracz gracz, int baza)
        {
            int extra = Math.Max(0, gracz.MapaGen - 1) / 2;
            return Math.Min(20, Math.Max(8, baza + extra));
        }

        /// <summary>Cichy rzut k20. Nat 20 = sukces, nat 1 = porażka.</summary>
        public static WynikTestu RzucTest(Gracz gracz, string skill, int st, Losowanie rng)
        {
            int rzut = rng.Calkowita(1, 20);
            int premia = PremiaSkilla(gracz, skill);
            int suma = rzut + premia;
            bool krytyczny = rzut == 20;
            bool wpadka = rzut == 1;
            bool sukces = krytyczny || (!wpadka && suma >= st);
            return new WynikTestu
            {
                Skill = skill, St = st, Rzut = rzut, Premia = premia, Suma = suma,
                Sukces = sukces, Krytyczny = krytyczny, Wpadka = wpadka,
            };
        }

        public static string OpisTestu(WynikTestu wynik)
        {
            OpisSkilla info = Skille[wynik.Skill];
            string atr = Opisy[info.Atrybut].Nazwa;
            string status = wynik.Krytyczny ? "KRYTYCZNY SUKCES!"
                : wynik.Wpadka ? "KRYTYCZNA WPADKA"
                : wynik.Sukces ? "SUKCES" : "porażka";
            string znak = TekstModyfikatora(wynik.Premia);
            return $"  {info.Ikona}  {info.Nazwa} ({atr})  ST {wynik.St}\n" +
                   $"      k20: {wynik.Rzut} {znak} = {wynik.Suma}  →  {status}";
        }

        public static string LiniaAtrybutow(Gracz gracz)
        {
            Zapewnij(gracz);
            var czesci = new List<string>();
            foreach (string k in Kolejnosc)
            {
                OpisAtrybutu info = Opisy[k];
                czesci.Add($"{info.Ikona}{info.Skrot} {Wartosc(gracz, k)}");
            }
            return "  Atrybuty: " + string.Join("  ", czesci);
        }
    }
}
