using System;
using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>Opcja menu wyłuskana z tekstu konsoli.</summary>
    public sealed class OpcjaEkranu
    {
        /// <summary>Co wpisać, żeby ją wybrać („3”, „0”).</summary>
        public string Klawisz;

        /// <summary>Nazwa bez emoji i bez klawisza, np. „Zbierz surowce (zostało 2)”.</summary>
        public string Tekst;

        /// <summary>Kierunek ruchu (dx, dy) albo null — opcje ruchu idą na mapę jako strzałki.</summary>
        public (int Dx, int Dy)? Kierunek;

        /// <summary>Dla ruchu: dokąd prowadzi („???”, „kanion”).</summary>
        public string Cel;
    }

    /// <summary>
    /// Ekran gry rozłożony na części: tytuł, treść, opcje i to, na co gra czeka.
    ///
    /// Logika gry pisze tekst do konsoli tak samo jak w wersji terminalowej —
    /// ramki z „═══”, nagłówek, linie z „[3] …” i zachętę „Twój wybór:”.
    /// Interfejs okienkowy może to pokazać jako konsolę, a interfejs fantasy
    /// rozkłada na elementy: opcje ruchu stają się strzałkami na mapie,
    /// pozostałe — przyciskami, narracja — dziennikiem. Logika o tym nie wie
    /// i nie musi.
    /// </summary>
    public sealed class EkranGry
    {
        public string Tytul = "";
        public readonly List<string> Tresc = new List<string>();
        public readonly List<OpcjaEkranu> Opcje = new List<OpcjaEkranu>();

        /// <summary>Gra czeka na Enter („[Naciśnij Enter, aby kontynuować...]”).</summary>
        public bool CzekaNaEnter;

        /// <summary>Gra czeka na wpisanie tekstu (np. imienia), a nie na wybór opcji.</summary>
        public bool CzekaNaTekst;

        /// <summary>Zachęta pytania bez emoji, np. „Imię bohatera:”.</summary>
        public string Zacheta = "";

        /// <summary>Na ekranie było logo z bloków — ekran tytułowy.</summary>
        public bool MaLogo;

        private static readonly Dictionary<string, (int, int)> Kierunki = new Dictionary<string, (int, int)>
        {
            { "północ", (0, -1) }, { "południe", (0, 1) }, { "zachód", (-1, 0) }, { "wschód", (1, 0) },
        };

        public bool MaKierunki
        {
            get
            {
                foreach (OpcjaEkranu o in Opcje)
                {
                    if (o.Kierunek.HasValue)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Rozkłada linie konsoli. Ostatnia linia to zachęta w budowie — gra
        /// dopisuje do niej odpowiedź gracza.
        /// </summary>
        /// <param name="linie">Linie z konsoli od ostatniego czyszczenia.</param>
        /// <param name="pyta">Czy gra czeka teraz na odpowiedź.</param>
        /// <param name="czekaNaEnter">Czy to pytanie jest tylko „naciśnij Enter”.</param>
        public static EkranGry Z(IList<string> linie, bool pyta, bool czekaNaEnter)
        {
            var e = new EkranGry { CzekaNaEnter = pyta && czekaNaEnter };
            if (linie == null || linie.Count == 0)
            {
                return e;
            }
            int ostatnia = linie.Count - 1;
            if (pyta)
            {
                e.Zacheta = Czysty(linie[ostatnia]);
            }

            // Tytuł: pierwsza treść między pierwszymi dwiema liniami ozdobnymi.
            int ozdobnych = 0;
            bool tytulZnaleziony = false;
            for (int i = 0; i < linie.Count; i++)
            {
                if (pyta && i == ostatnia)
                {
                    break; // zachęta — nie treść
                }
                string surowa = linie[i] ?? "";
                if (Konsola.CzyOzdobnik(surowa))
                {
                    ozdobnych++;
                    continue;
                }
                if (CzyLogo(surowa))
                {
                    e.MaLogo = true;
                    continue;
                }
                string linia = Czysty(surowa);
                if (linia.Length == 0)
                {
                    continue;
                }
                string klawisz = Konsola.KlawiszOpcji(surowa);
                if (klawisz != null)
                {
                    e.Opcje.Add(Opcja(klawisz, linia));
                    continue;
                }
                if (linia.StartsWith("[Naciśnij", StringComparison.Ordinal))
                {
                    continue;
                }
                if (!tytulZnaleziony && ozdobnych == 1 && !e.MaLogo)
                {
                    e.Tytul = linia;
                    tytulZnaleziony = true;
                    continue;
                }
                // Linia surowców to same liczby po wycięciu emoji — HUD pokazuje je ikonami.
                if (linia.StartsWith("Surowce:", StringComparison.Ordinal))
                {
                    continue;
                }
                e.Tresc.Add(linia);
            }
            e.CzekaNaTekst = pyta && !e.CzekaNaEnter && e.Opcje.Count == 0;
            return e;
        }

        /// <summary>Linia bez emoji, bez skrajnych spacji i bez podwójnych odstępów po ikonach.</summary>
        public static string Czysty(string linia)
        {
            string t = Konsola.BezEmoji(linia ?? "").Trim();
            while (t.Contains("   "))
            {
                t = t.Replace("   ", "  ");
            }
            return t;
        }

        /// <summary>Linia logo z bloków — po narożnikach ramek; sam pasek z „█” (np. HP) logiem nie jest.</summary>
        private static bool CzyLogo(string linia)
        {
            return linia.IndexOfAny(new[] { '╗', '╝', '╔', '╚' }) >= 0;
        }

        private static OpcjaEkranu Opcja(string klawisz, string linia)
        {
            // „[3]  wschód  →  ??? ” → nazwa „wschód”, cel „???”
            int nawias = linia.IndexOf(']');
            string tekst = (nawias >= 0 ? linia.Substring(nawias + 1) : linia).Trim();
            var o = new OpcjaEkranu { Klawisz = klawisz, Tekst = tekst };
            int strzalka = tekst.IndexOf('→');
            string nazwa = (strzalka >= 0 ? tekst.Substring(0, strzalka) : tekst).Trim();
            if (Kierunki.TryGetValue(nazwa.ToLowerInvariant(), out (int, int) k))
            {
                o.Kierunek = k;
                o.Tekst = char.ToUpperInvariant(nazwa[0]) + nazwa.Substring(1);
                o.Cel = strzalka >= 0 ? tekst.Substring(strzalka + 1).Trim() : "";
            }
            else
            {
                // „Wojownik    Dużo HP…” — kolumna nazwy i opisu: rozdzielamy myślnikiem.
                int przerwa = o.Tekst.IndexOf("  ", StringComparison.Ordinal);
                if (przerwa > 0)
                {
                    string reszta = o.Tekst.Substring(przerwa).Trim();
                    o.Tekst = o.Tekst.Substring(0, przerwa) + (reszta.StartsWith("(") ? " " : " — ") + reszta;
                }
                while (o.Tekst.Contains("  "))
                {
                    o.Tekst = o.Tekst.Replace("  ", " ");
                }
            }
            return o;
        }
    }
}
