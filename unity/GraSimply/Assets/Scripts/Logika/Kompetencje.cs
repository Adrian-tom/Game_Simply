using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>Jedna umiejętność osadnika: poziom 0–20, postęp do następnego i pasja (0, 1 albo 2).</summary>
    public sealed class Umiejetnosc
    {
        public int P;
        public double Xp;
        public int Pasja;
    }

    /// <summary>Nazwa i ikona umiejętności.</summary>
    public sealed class OpisUmiejetnosci
    {
        public string Nazwa;
        public string Ikona;
    }

    /// <summary>
    /// Kompetencje osadników: umiejętności 0–20, pasje i awans przez pracę.
    /// Odpowiednik <c>game/kompetencje.py</c> — te same liczby, ten sam sposób
    /// losowania startu (z imienia), ten sam tekst komunikatów.
    ///
    /// Wcześniej osadnik zbierał „doświadczenie w zawodzie” w trzech progach
    /// (★ ★★ ★★★), z czego każdy dawał +10% pracy. Teraz każdy ma dziesięć
    /// umiejętności, a niektóre z nich kocha: <b>pasja</b> przyspiesza naukę
    /// i poprawia humor, gdy osadnik robi to, co lubi.
    /// </summary>
    public static class Kompetencje
    {
        public const int MaxPoziom = 20;
        public const double XpNaDzien = 60.0;

        /// <summary>Mnożnik nauki: brak pasji ×0,35, mała ×1, duża ×1,5.</summary>
        public static readonly double[] MnoznikPasji = { 0.35, 1.0, 1.5 };

        /// <summary>Premia do celu morale, gdy osadnik robi to, co lubi.</summary>
        public static readonly double[] PremiaMoralePasji = { 0.0, 4.0, 8.0 };

        /// <summary>Kolejność jak w Pythonie — od niej zależy losowanie startu.</summary>
        public static readonly List<KeyValuePair<string, OpisUmiejetnosci>> Umiejetnosci =
            new List<KeyValuePair<string, OpisUmiejetnosci>>
            {
                Opis("budowa", "Budowa", "🏗"),
                Opis("gornictwo", "Górnictwo", "⛏"),
                Opis("uprawa", "Uprawa", "🌾"),
                Opis("zbieractwo", "Zbieractwo", "🌿"),
                Opis("lowiectwo", "Łowiectwo", "🏹"),
                Opis("rzemioslo", "Rzemiosło", "🔧"),
                Opis("handel", "Handel", "💰"),
                Opis("medycyna", "Medycyna", "⚕"),
                Opis("walka", "Walka", "🛡"),
                Opis("wiedza", "Wiedza", "📜"),
            };

        /// <summary>Która umiejętność rośnie od danego zajęcia. Bezczynny nie ćwiczy niczego.</summary>
        public static readonly Dictionary<string, string> UmiejetnoscZajecia = new Dictionary<string, string>
        {
            { "drwal", "zbieractwo" }, { "zielarz", "zbieractwo" }, { "kamieniarz", "gornictwo" },
            { "gornik", "gornictwo" }, { "mysliwy", "lowiectwo" }, { "rolnik", "uprawa" },
            { "tracz", "rzemioslo" }, { "hutnik", "rzemioslo" }, { "rzemieslnik", "rzemioslo" },
            { "handlarz", "handel" }, { "straznik", "walka" }, { "uzdrowiciel", "medycyna" },
            { "budowniczy", "budowa" }, { "badacz", "wiedza" },
        };

        private static readonly (int Prog, string Nazwa)[] Tytuly =
        {
            (16, "arcymistrz"), (12, "mistrz"), (8, "czeladnik"), (4, "uczeń"), (0, "nowicjusz"),
        };

        private static KeyValuePair<string, OpisUmiejetnosci> Opis(string klucz, string nazwa, string ikona)
        {
            return new KeyValuePair<string, OpisUmiejetnosci>(
                klucz, new OpisUmiejetnosci { Nazwa = nazwa, Ikona = ikona });
        }

        public static OpisUmiejetnosci OpisKlucza(string klucz)
        {
            foreach (KeyValuePair<string, OpisUmiejetnosci> para in Umiejetnosci)
            {
                if (para.Key == klucz)
                {
                    return para.Value;
                }
            }
            return null;
        }

        /// <summary>XP potrzebne, by z <paramref name="poziom"/> wejść na kolejny (krzywa kawałkami liniowa).</summary>
        public static double XpDoNastepnego(int poziom)
        {
            int p = Math.Max(0, Math.Min(MaxPoziom - 1, poziom));
            if (p <= 9)
            {
                return 40.0 + (400.0 - 40.0) * p / 9;
            }
            return 400.0 + (1200.0 - 400.0) * (p - 9) / 10;
        }

        public static string Tytul(int poziom)
        {
            foreach ((int prog, string nazwa) in Tytuly)
            {
                if (poziom >= prog)
                {
                    return nazwa;
                }
            }
            return Tytuly[Tytuly.Length - 1].Nazwa;
        }

        public static string OpisPasji(int pasja)
        {
            int ile = Math.Max(0, Math.Min(2, pasja));
            return ile == 0 ? "" : ile == 1 ? "🔥" : "🔥🔥";
        }

        // ------------------------------------------------------------------ //
        //  Tworzenie                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Umiejętności startowe osadnika o danym imieniu. Losowanie jest zasiane imieniem,
        /// więc ta sama osoba ma te same predyspozycje w każdej grze i w obu wersjach.
        /// </summary>
        public static Dictionary<string, Umiejetnosc> Generuj(string imie)
        {
            Losowanie rng = Losowanie.ZNapisu("kompetencje|" + imie);
            var klucze = new List<string>();
            foreach (KeyValuePair<string, OpisUmiejetnosci> para in Umiejetnosci)
            {
                klucze.Add(para.Key);
            }
            var wynik = new Dictionary<string, Umiejetnosc>();
            foreach (string k in klucze)
            {
                wynik[k] = new Umiejetnosc { P = Math.Min(6, (int)rng.Trojkatny(0, 6, 1.5)), Xp = 0.0, Pasja = 0 };
            }
            foreach (string k in rng.Probka(klucze, 2))            // dwa talenty: ktoś jest w czymś dobry
            {
                wynik[k].P = Math.Min(12, wynik[k].P + rng.Calkowita(2, 5));
            }
            List<string> dwie = rng.Probka(klucze, 2);
            wynik[dwie[0]].Pasja = rng.Losowa() < 0.3 ? 2 : 1;
            if (rng.Losowa() < 0.55)
            {
                wynik[dwie[1]].Pasja = 1;
            }
            return wynik;
        }

        /// <summary>
        /// Stary zapis albo świeży osadnik: dopisz umiejętności. <paramref name="dosw"/> to dawne
        /// doświadczenie w zawodach (suma dni pracy) — ★★★ przechodzi w poziom 3 × liczba gwiazdek.
        /// </summary>
        public static Dictionary<string, Umiejetnosc> Zapewnij(Osadnik o, Dictionary<string, int> dosw = null)
        {
            if (o.Kompetencje == null || o.Kompetencje.Count == 0)
            {
                o.Kompetencje = Generuj(o.Imie ?? "");
                if (dosw != null)
                {
                    foreach (KeyValuePair<string, int> wpis in dosw)
                    {
                        if (!UmiejetnoscZajecia.TryGetValue(wpis.Key, out string u))
                        {
                            continue;
                        }
                        int gwiazdki = (wpis.Value >= 10 ? 1 : 0) + (wpis.Value >= 30 ? 1 : 0) + (wpis.Value >= 60 ? 1 : 0);
                        if (gwiazdki > 0)
                        {
                            o.Kompetencje[u].P = Math.Max(o.Kompetencje[u].P, 3 * gwiazdki);
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, OpisUmiejetnosci> para in Umiejetnosci)
            {
                if (!o.Kompetencje.ContainsKey(para.Key))        // nowa umiejętność w starym zapisie
                {
                    o.Kompetencje[para.Key] = new Umiejetnosc();
                }
            }
            return o.Kompetencje;
        }

        // ------------------------------------------------------------------ //
        //  Odczyt                                                             //
        // ------------------------------------------------------------------ //

        public static int Poziom(Osadnik o, string umiejetnosc)
        {
            return Zapewnij(o)[umiejetnosc].P;
        }

        public static int Pasja(Osadnik o, string umiejetnosc)
        {
            return Zapewnij(o)[umiejetnosc].Pasja;
        }

        /// <summary>Umiejętność ćwiczona przez zajęcie albo null (bezczynny, nieznane).</summary>
        public static string ZUmiejetnosciZajecia(string zajecie)
        {
            return UmiejetnoscZajecia.TryGetValue(zajecie ?? "", out string u) ? u : null;
        }

        /// <summary>Mnożnik wydajności od umiejętności w zajęciu: 0,8 na poziomie 0, 1,0 na 4, 1,8 na 20.</summary>
        public static double Czynnik(Osadnik o, string zajecie = null)
        {
            string u = ZUmiejetnosciZajecia(string.IsNullOrEmpty(zajecie) ? o.Zajecie : zajecie);
            if (u == null)
            {
                return 1.0;
            }
            return 0.8 + 0.05 * Poziom(o, u);
        }

        /// <summary>Bonus do celu morale, gdy osadnik zajmuje się tym, co lubi (i nie choruje).</summary>
        public static double PremiaMorale(Osadnik o)
        {
            if (o.Chory > 0)
            {
                return 0.0;
            }
            string u = ZUmiejetnosciZajecia(o.Zajecie);
            return u != null ? PremiaMoralePasji[Pasja(o, u)] : 0.0;
        }

        /// <summary>Najwyższe umiejętności: (klucz, poziom, pasja) — najpierw poziom, potem pasja.</summary>
        public static List<(string Klucz, int Poziom, int Pasja)> Najlepsze(Osadnik o, int ile = 3)
        {
            Dictionary<string, Umiejetnosc> k = Zapewnij(o);
            var lista = new List<(string Klucz, int Poziom, int Pasja)>();
            foreach (KeyValuePair<string, OpisUmiejetnosci> para in Umiejetnosci)
            {
                lista.Add((para.Key, k[para.Key].P, k[para.Key].Pasja));
            }
            lista.Sort((a, b) =>
            {
                int c = b.Poziom.CompareTo(a.Poziom);
                if (c == 0)
                {
                    c = b.Pasja.CompareTo(a.Pasja);
                }
                return c != 0 ? c : string.CompareOrdinal(a.Klucz, b.Klucz);
            });
            return lista.GetRange(0, Math.Min(ile, lista.Count));
        }

        public static string OpisKrotki(Osadnik o, int ile = 3)
        {
            var czesci = new List<string>();
            foreach ((string u, int p, int pas) in Najlepsze(o, ile))
            {
                if (p == 0 && pas == 0)
                {
                    continue;
                }
                czesci.Add($"{OpisKlucza(u).Nazwa} {p}{OpisPasji(pas)}");
            }
            return czesci.Count > 0 ? string.Join(", ", czesci) : "bez wprawy";
        }

        /// <summary>
        /// Umiejętności od najwyższej, jedna linia każda — do karty osadnika.
        /// <paramref name="ile"/> skraca listę (mały ekran); umiejętność bieżącego
        /// <paramref name="zajecie"/> zawsze się w niej znajdzie, nawet jeśli jest słaba.
        /// </summary>
        public static List<string> OpisPelny(Osadnik o, int? ile = null, string zajecie = null)
        {
            List<(string Klucz, int Poziom, int Pasja)> wszystkie = Najlepsze(o, Umiejetnosci.Count);
            var wybrane = ile.HasValue ? wszystkie.GetRange(0, Math.Min(ile.Value, wszystkie.Count))
                                       : new List<(string Klucz, int Poziom, int Pasja)>(wszystkie);
            string wazna = ZUmiejetnosciZajecia(zajecie);
            if (wazna != null && !wybrane.Exists(t => t.Klucz == wazna))
            {
                wybrane.AddRange(wszystkie.FindAll(t => t.Klucz == wazna));
            }
            var linie = new List<string>();
            foreach ((string u, int p, int pas) in wybrane)
            {
                Umiejetnosc w = Zapewnij(o)[u];
                string pasek = new string('█', p / 2) + new string('░', 10 - p / 2);
                string postep = p >= MaxPoziom
                    ? ""
                    : $"  ({(int)w.Xp}/{(int)XpDoNastepnego(p)} xp)";
                string znacznik = u == wazna ? "▶" : " ";
                OpisUmiejetnosci opis = OpisKlucza(u);
                linie.Add($" {znacznik}{opis.Ikona} {Liczby.Dopelnij(opis.Nazwa, 10)} {pasek} "
                          + $"{p.ToString().PadLeft(2)} {Liczby.Dopelnij(OpisPasji(pas), 2)} {Tytul(p)}{postep}");
            }
            return linie;
        }

        // ------------------------------------------------------------------ //
        //  Nauka przez pracę                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>Dzień pracy w zajęciu: XP do umiejętności, awans, komunikaty o nowych poziomach.</summary>
        public static List<string> Cwicz(Osadnik o, string zajecie = null, double dni = 1.0)
        {
            var wiesci = new List<string>();
            string u = ZUmiejetnosciZajecia(string.IsNullOrEmpty(zajecie) ? o.Zajecie : zajecie);
            if (u == null)
            {
                return wiesci;
            }
            Umiejetnosc w = Zapewnij(o)[u];
            if (w.P >= MaxPoziom)
            {
                return wiesci;
            }
            w.Xp = w.Xp + XpNaDzien * MnoznikPasji[w.Pasja] * dni;
            while (w.P < MaxPoziom && w.Xp >= XpDoNastepnego(w.P))
            {
                w.Xp -= XpDoNastepnego(w.P);
                w.P += 1;
                bool nowyTytul = w.P == 4 || w.P == 8 || w.P == 12 || w.P == 16;
                wiesci.Add($"  📈  {o.Imie ?? "?"}: {OpisKlucza(u).Nazwa.ToLowerInvariant()} {w.P}{OpisPasji(w.Pasja)}"
                           + (nowyTytul ? $" — teraz {Tytul(w.P)}" : ""));
            }
            if (w.P >= MaxPoziom)
            {
                w.Xp = 0.0;
            }
            return wiesci;
        }
    }
}
