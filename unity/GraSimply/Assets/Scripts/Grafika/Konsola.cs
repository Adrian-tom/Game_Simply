using System;
using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>Jeden wiersz konsoli gotowy do narysowania.</summary>
    public struct WierszKonsoli
    {
        public string Tekst;

        /// <summary>Klawisz opcji (np. „3”) albo null — wiersz z opcją jest klikalny.</summary>
        public string Opcja;

        /// <summary>Wiersz złożony wyłącznie ze znaków ramki — rysowany barwą ramki.</summary>
        public bool Ozdobnik;
    }

    /// <summary>
    /// Układanie tekstu konsoli: zawijanie wierszy, rozpoznawanie klikalnych
    /// opcji i wycinanie okna widocznych wierszy.
    ///
    /// Krój konsoli jest o stałej szerokości znaku, więc zawijamy po liczbie
    /// znaków, a nie po pikselach — wynik jest ten sam co w wersji pythonowej,
    /// a warstwa Unity nie musi mierzyć tekstu przy każdym wierszu.
    /// </summary>
    public static class Konsola
    {
        /// <summary>Znaki, z których składają się poziome linie w menu gry.</summary>
        private const string ZnakiOzdobne = "═─━-=·";

        /// <summary>
        /// Klawisz opcji z początku linii, np. „  [3]  wschód” → „3”.
        /// Odpowiednik wyrażenia <c>^\s*\[([0-9A-Za-z]{1,3})\]</c> z Pythona.
        /// </summary>
        public static string KlawiszOpcji(string linia)
        {
            if (linia == null)
            {
                return null;
            }
            int i = 0;
            while (i < linia.Length && char.IsWhiteSpace(linia[i]))
            {
                i++;
            }
            if (i >= linia.Length || linia[i] != '[')
            {
                return null;
            }
            int koniec = linia.IndexOf(']', i + 1);
            if (koniec < 0)
            {
                return null;
            }
            string klucz = linia.Substring(i + 1, koniec - i - 1);
            if (klucz.Length < 1 || klucz.Length > 3)
            {
                return null;
            }
            foreach (char c in klucz)
            {
                if (!char.IsLetterOrDigit(c) || c > 'z')
                {
                    return null;
                }
            }
            return klucz;
        }

        /// <summary>
        /// Usuwa emoji z tekstu, zostawiając znaki ramek i bloków.
        ///
        /// Wyjście awaryjne na wypadek, gdyby krój systemowy nie miał ikon —
        /// wtedy zamiast nich pokazują się puste prostokąty i tekst staje się
        /// nieczytelny. Reguła jest ta sama co w oknie pygame: znaki od U+2300
        /// w górę to ikony, poza blokiem rysowania ramek (U+2500–U+25FF),
        /// z którego zbudowane są linie menu.
        /// </summary>
        public static string BezEmoji(string tekst)
        {
            if (string.IsNullOrEmpty(tekst))
            {
                return tekst;
            }
            var wynik = new System.Text.StringBuilder(tekst.Length);
            bool wIkonie = false;
            bool zjedzSpacje = false;
            foreach (char c in tekst)
            {
                bool ikona = char.IsSurrogate(c)
                             || (c >= 0x2300 && !(c >= 0x2500 && c <= 0x25FF))
                             || c == 0xFE0F || c == 0x200D;
                if (ikona)
                {
                    // Razem z ikoną zabieramy jedną spację po niej — ikona zajmuje
                    // w tekście mniej więcej tyle co znak plus odstęp. Spacji
                    // przed ikoną nie ruszamy, bo z nich zrobione są wcięcia
                    // i kolumny menu (nazwa.PadRight), a te mają zostać równe.
                    wIkonie = true;
                    zjedzSpacje = true;
                    continue;
                }
                if (zjedzSpacje && c == ' ')
                {
                    zjedzSpacje = false;
                    wIkonie = false;
                    continue;
                }
                zjedzSpacje = false;
                wIkonie = false;
                wynik.Append(c);
            }
            return wynik.ToString();
        }

        public static bool CzyOzdobnik(string linia)
        {
            string pas = (linia ?? "").Trim();
            if (pas.Length == 0)
            {
                return false;
            }
            foreach (char c in pas)
            {
                if (ZnakiOzdobne.IndexOf(c) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Zawija linię do podanej liczby znaków, zachowując wcięcie.
        /// Kolejne wiersze dostają wcięcie +2, żeby było widać, że to ciąg dalszy.
        /// </summary>
        public static List<string> Zawin(string linia, int znakow)
        {
            var wynik = new List<string>();
            if (znakow < 8)
            {
                znakow = 8;
            }
            if (linia == null)
            {
                wynik.Add("");
                return wynik;
            }
            if (linia.Length <= znakow)
            {
                wynik.Add(linia);
                return wynik;
            }
            int wciecie = 0;
            while (wciecie < linia.Length && linia[wciecie] == ' ')
            {
                wciecie++;
            }
            string[] slowa = linia.Substring(wciecie).Split(' ');
            string biezacy = "";
            for (int i = 0; i < slowa.Length; i++)
            {
                if (i == 0)
                {
                    biezacy = new string(' ', wciecie) + slowa[i];
                    continue;
                }
                string proba = biezacy + " " + slowa[i];
                if (biezacy.Length > 0 && proba.Length > znakow)
                {
                    wynik.Add(biezacy);
                    biezacy = new string(' ', wciecie + 2) + slowa[i];
                }
                else
                {
                    biezacy = proba;
                }
            }
            wynik.Add(biezacy);
            return wynik;
        }

        /// <summary>
        /// Układa wiersze do pokazania.
        /// </summary>
        /// <param name="linie">Linie z konsoli (ostatnia jest tą w budowie).</param>
        /// <param name="znakow">Ile znaków mieści się w wierszu.</param>
        /// <param name="mieszczSie">Ile wierszy mieści panel.</param>
        /// <param name="przewiniecie">O ile wierszy cofnięty jest widok (0 = koniec).</param>
        /// <param name="wpisywane">Tekst wpisywany przez gracza albo null, gdy gra nie pyta.</param>
        /// <param name="kursor">Czy pokazać migający kursor.</param>
        /// <param name="ileWyzej">Ile wierszy zostało powyżej okna (0 = nic).</param>
        public static List<WierszKonsoli> Widok(IList<string> linie, int znakow, int mieszczSie,
                                                int przewiniecie, string wpisywane, bool kursor,
                                                out int ileWyzej)
        {
            var wszystkie = new List<WierszKonsoli>();
            for (int i = 0; i < linie.Count; i++)
            {
                string linia = linie[i];
                if (i == linie.Count - 1 && wpisywane != null)
                {
                    linia = linia + wpisywane + (kursor ? "▌" : " ");
                }
                string opcja = KlawiszOpcji(linia);
                List<string> zawinięte = Zawin(linia, znakow);
                for (int j = 0; j < zawinięte.Count; j++)
                {
                    wszystkie.Add(new WierszKonsoli
                    {
                        Tekst = zawinięte[j],
                        // Klikalny jest tylko pierwszy wiersz opcji i tylko wtedy,
                        // gdy gra faktycznie czeka na odpowiedź.
                        Opcja = j == 0 && wpisywane != null ? opcja : null,
                        Ozdobnik = CzyOzdobnik(zawinięte[j]),
                    });
                }
            }

            if (mieszczSie < 1)
            {
                mieszczSie = 1;
            }
            przewiniecie = Math.Max(0, Math.Min(przewiniecie, wszystkie.Count - mieszczSie));
            int koniec = wszystkie.Count - przewiniecie;
            int poczatek = Math.Max(0, koniec - mieszczSie);
            if (poczatek > 0)
            {
                // Pierwszy wiersz ustępuje miejsca znacznikowi „wyżej jest więcej”.
                poczatek += 1;
            }
            ileWyzej = poczatek;
            var widoczne = new List<WierszKonsoli>();
            for (int i = poczatek; i < koniec; i++)
            {
                widoczne.Add(wszystkie[i]);
            }
            return widoczne;
        }
    }
}
