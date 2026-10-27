using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Co bohater może zrobić z tym, co stoi obok: opis dla gracza i odpowiedź menu obozu.</summary>
    public sealed class Interakcja
    {
        public string Opis;

        /// <summary>
        /// Odpowiedź, którą logika obozu przyjmie jak wpisaną przez gracza: numer opcji menu
        /// („23” = badania) albo „info:klucz” (obejrzenie budynku bez własnego menu).
        /// </summary>
        public string Odpowiedz;
    }

    /// <summary>
    /// Chodzenie bohatera po obozie: strzałki lub WASD przesuwają go po polach placu,
    /// a spacja (E, Enter) robi to, co podpowiada napis „▶” — wejście do skryptorium,
    /// odpoczynek w domu, wyprawa przez bramę. Menu z numerami zostaje jako skrót.
    ///
    /// Strzałki są ustawione względem ekranu, nie siatki: plac jest rysowany w rzucie
    /// skośnym, więc „w górę” to krok po przekątnej siatki. Gdy przekątna jest zajęta,
    /// bohater ślizga się wzdłuż przeszkody (jeden z dwóch kroków składowych), dzięki
    /// czemu nie klinuje się na rogach budynków.
    /// </summary>
    public static class ChodzenieObozu
    {
        public enum WynikRuchu
        {
            Ruszyl,
            Zablokowane,
        }

        /// <summary>Kierunki ekranowe → krok po przekątnej siatki.</summary>
        private static readonly Dictionary<string, (int Dx, int Dy)> Kroki = new Dictionary<string, (int, int)>
        {
            { "gora", (-1, -1) }, { "dol", (1, 1) }, { "lewo", (-1, 1) }, { "prawo", (1, -1) },
        };

        /// <summary>Klawisze okna → odpowiedzi menu obozu (poprzedzone „@”, żeby nie pomylić z numerem).</summary>
        public static readonly Dictionary<string, string> Skroty = new Dictionary<string, string>
        {
            { "gora", "@gora" }, { "dol", "@dol" }, { "lewo", "@lewo" }, { "prawo", "@prawo" },
            { "w", "@gora" }, { "s", "@dol" }, { "a", "@lewo" }, { "d", "@prawo" },
            { "spacja", "@akcja" }, { "e", "@akcja" },
        };

        /// <summary>Gdzie staje bohater po powrocie z wyprawy: tuż za bramą.</summary>
        public static readonly (int X, int Y) PoPowrocie = (UkladPlacu.Brama.X, UkladPlacu.Brama.Y - 1);

        public static bool CzyKierunek(string odpowiedz)
        {
            return odpowiedz != null && odpowiedz.StartsWith("@") && Kroki.ContainsKey(odpowiedz.Substring(1));
        }

        /// <summary>Czy bohater może stanąć na polu: wnętrze placu bez budynków i ogniska.</summary>
        public static bool Wolne(Dictionary<(int X, int Y), ElementPlacu> plan, (int X, int Y) p)
        {
            if (p.X < 1 || p.Y < 1 || p.X > UkladPlacu.Siatka - 2 || p.Y > UkladPlacu.Siatka - 2)
            {
                return false;
            }
            if (p == UkladPlacu.Ognisko)
            {
                return false;
            }
            return !plan.TryGetValue(p, out ElementPlacu el) || el.Rodzaj == "farma";
        }

        /// <summary>Jeden krok w kierunku „gora/dol/lewo/prawo”. Zapisuje nową pozycję w <paramref name="gracz"/>.</summary>
        public static WynikRuchu Ruch(Gracz gracz, StanOsady osada, string kierunek)
        {
            if (kierunek.StartsWith("@"))
            {
                kierunek = kierunek.Substring(1);
            }
            if (!Kroki.TryGetValue(kierunek, out (int Dx, int Dy) k))
            {
                return WynikRuchu.Zablokowane;
            }
            Dictionary<(int X, int Y), ElementPlacu> plan = UkladPlacu.Rozklad(osada);
            (int X, int Y) poz = gracz.PozycjaObozu;
            // Przekątna, a gdy zajęta — dwa kroki składowe (ślizg wzdłuż przeszkody).
            var kandydaci = new[] { (poz.X + k.Dx, poz.Y + k.Dy), (poz.X + k.Dx, poz.Y), (poz.X, poz.Y + k.Dy) };
            foreach ((int X, int Y) cel in kandydaci)
            {
                if (Wolne(plan, cel))
                {
                    gracz.PozycjaObozu = cel;
                    return WynikRuchu.Ruszyl;
                }
            }
            return WynikRuchu.Zablokowane;
        }

        private static readonly Dictionary<string, string> NazwyInfo = new Dictionary<string, string>
        {
            { "sklep", "sklep" }, { "kuznia", "kuźnię" }, { "huta", "hutę" }, { "tartak", "tartak" },
            { "warsztat", "warsztat" }, { "laboratorium", "laboratorium" }, { "lecznica", "lecznicę" },
            { "targ", "targ" }, { "karawanseraj", "karawanseraj" }, { "tawerna", "tawernę" },
            { "wieza", "wieżę strażniczą" }, { "stajnie", "stajnie" }, { "spichlerz", "spichlerz" },
        };

        /// <summary>
        /// Co jest do zrobienia na polu bohatera i w jego sąsiedztwie (osiem pól) albo null.
        /// Pierwszeństwo: brama, potem najbliższy obiekt (pola stykające się bokiem przed przekątnymi),
        /// a przy remisie — skryptorium, dom, budowa, ognisko, reszta.
        /// </summary>
        public static Interakcja Cel(StanOsady osada, (int X, int Y) poz)
        {
            Dictionary<(int X, int Y), ElementPlacu> plan = UkladPlacu.Rozklad(osada);
            var kandydaci = new List<(int Odleglosc, int Waga, Interakcja Akcja)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    (int X, int Y) p = (poz.X + dx, poz.Y + dy);
                    int odl = Math.Abs(dx) + Math.Abs(dy);
                    if (p == UkladPlacu.Brama)
                    {
                        kandydaci.Add((-1, 0, new Interakcja { Opis = "wyrusz na wyprawę", Odpowiedz = "1" }));
                    }
                    else if (p == UkladPlacu.Ognisko)
                    {
                        kandydaci.Add((odl, 4, new Interakcja { Opis = "usiądź przy ognisku (osada)", Odpowiedz = "16" }));
                    }
                    else if (plan.TryGetValue(p, out ElementPlacu el))
                    {
                        Interakcja a = DlaElementu(el);
                        if (a != null)
                        {
                            kandydaci.Add((odl, Waga(el), a));
                        }
                    }
                }
            }
            return kandydaci.Count == 0 ? null
                : kandydaci.OrderBy(k => k.Odleglosc).ThenBy(k => k.Waga).First().Akcja;
        }

        private static int Waga(ElementPlacu el)
        {
            return el.Klucz == "skryptorium" ? 1 : el.Klucz == "dom" || el.Klucz == "namiot" ? 2
                 : el.Rodzaj == "budowa" ? 3 : 5;
        }

        private static Interakcja DlaElementu(ElementPlacu el)
        {
            if (el.Rodzaj == "budowa")
            {
                string nazwa = el.Klucz == "chata" ? "chatę" : Oboz.OpisBudynkuKlucza(el.Klucz).Nazwa.ToLowerInvariant();
                return new Interakcja { Opis = $"pracuj przy budowie: {nazwa}", Odpowiedz = "11" };
            }
            if (el.Klucz == "skryptorium")
            {
                return new Interakcja { Opis = "wejdź do skryptorium (badania)", Odpowiedz = "23" };
            }
            if (el.Klucz == "dom" || el.Klucz == "namiot")
            {
                string gdzie = el.Klucz == "dom" ? "w domu" : "w namiocie";
                return new Interakcja { Opis = $"odpocznij {gdzie} (1 dzień)", Odpowiedz = "3" };
            }
            if (NazwyInfo.TryGetValue(el.Klucz ?? "", out string info))
            {
                return new Interakcja { Opis = $"obejrzyj {info}", Odpowiedz = "info:" + el.Klucz };
            }
            return null;
        }

        /// <summary>Tekst podpowiedzi pod planszą; zawsze zaczyna się od „▶” (rysuje go osobna plakietka).</summary>
        public static string Podpowiedz(StanOsady osada, (int X, int Y) poz)
        {
            Interakcja a = Cel(osada, poz);
            return a != null ? $"▶ Spacja: {a.Opis}" : "▶ Strzałki lub WASD: chodź po obozie";
        }
    }
}
