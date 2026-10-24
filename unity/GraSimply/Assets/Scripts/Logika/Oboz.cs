using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    public sealed class OpisSurowca
    {
        public string Nazwa;
        public string Ikona;
    }

    /// <summary>Jeden wiersz tabeli zbiorów: surowiec, szansa i widełki ilości.</summary>
    internal struct Zbior
    {
        public string Klucz;
        public double Szansa;
        public int Min;
        public int Max;

        public Zbior(string klucz, double szansa, int min, int max)
        {
            Klucz = klucz;
            Szansa = szansa;
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Magazyn osady i zbieractwo w terenie.
    ///
    /// Z <c>game/oboz.py</c> przeniesiona jest na razie część dotycząca
    /// surowców — budowanie, ulepszanie i utrzymanie budynków należą
    /// do osady i czekają na etap 2.
    /// </summary>
    public static class Oboz
    {
        public const int MaxZbiorowNaPolu = 2;

        /// <summary>Kolejność jak w Pythonie — od niej zależy linia surowców w HUD.</summary>
        public static readonly List<KeyValuePair<string, OpisSurowca>> Surowce =
            new List<KeyValuePair<string, OpisSurowca>>
            {
                new KeyValuePair<string, OpisSurowca>("zywnosc", new OpisSurowca { Nazwa = "żywność", Ikona = "🍖" }),
                new KeyValuePair<string, OpisSurowca>("drewno", new OpisSurowca { Nazwa = "drewno", Ikona = "🌲" }),
                new KeyValuePair<string, OpisSurowca>("kamien", new OpisSurowca { Nazwa = "kamień", Ikona = "🪨" }),
                new KeyValuePair<string, OpisSurowca>("ziola", new OpisSurowca { Nazwa = "zioła", Ikona = "🌿" }),
                new KeyValuePair<string, OpisSurowca>("skora", new OpisSurowca { Nazwa = "skóra", Ikona = "🦌" }),
                new KeyValuePair<string, OpisSurowca>("ruda", new OpisSurowca { Nazwa = "ruda", Ikona = "⛏" }),
                new KeyValuePair<string, OpisSurowca>("deski", new OpisSurowca { Nazwa = "deski", Ikona = "🪵" }),
                new KeyValuePair<string, OpisSurowca>("zelazo", new OpisSurowca { Nazwa = "żelazo", Ikona = "🔩" }),
            };

        /// <summary>Biom → co i z jaką szansą da się tam zebrać.</summary>
        private static readonly Dictionary<string, Zbior[]> ZbioryBiomu =
            new Dictionary<string, Zbior[]>
            {
                {
                    "równiny", new[]
                    {
                        new Zbior("ziola", 0.75, 1, 3), new Zbior("zywnosc", 0.60, 1, 3),
                        new Zbior("drewno", 0.50, 1, 2), new Zbior("kamien", 0.20, 1, 1),
                    }
                },
                {
                    "las", new[]
                    {
                        new Zbior("drewno", 0.90, 2, 4), new Zbior("zywnosc", 0.55, 1, 3),
                        new Zbior("ziola", 0.40, 1, 2), new Zbior("skora", 0.30, 1, 2),
                    }
                },
                {
                    "bagna", new[]
                    {
                        new Zbior("ziola", 0.90, 2, 4), new Zbior("zywnosc", 0.35, 1, 2),
                        new Zbior("drewno", 0.35, 1, 2), new Zbior("skora", 0.15, 1, 1),
                    }
                },
                {
                    "ruiny", new[]
                    {
                        new Zbior("kamien", 0.85, 2, 4), new Zbior("ruda", 0.30, 1, 2),
                        new Zbior("drewno", 0.20, 1, 1),
                    }
                },
                {
                    "wzgórza", new[]
                    {
                        new Zbior("kamien", 0.70, 2, 3), new Zbior("ruda", 0.50, 1, 2),
                        new Zbior("drewno", 0.25, 1, 2), new Zbior("zywnosc", 0.20, 1, 2),
                    }
                },
                {
                    "kanion", new[]
                    {
                        new Zbior("ruda", 0.75, 2, 3), new Zbior("kamien", 0.55, 1, 3),
                    }
                },
            };

        /// <summary>Rzadkie składniki alchemiczne ze zbieractwa: biom → (składnik, szansa).</summary>
        private static readonly Dictionary<string, (string Klucz, double Szansa)> SkladnikiBiomu =
            new Dictionary<string, (string, double)>
            {
                { "bagna", ("grzyb", 0.18) },
                { "kanion", ("kwiat_pustyni", 0.20) },
                { "wzgórza", ("pioro", 0.08) },
                { "ruiny", ("esencja", 0.06) },
            };

        private static OpisSurowca Opis(string klucz)
        {
            foreach (KeyValuePair<string, OpisSurowca> para in Surowce)
            {
                if (para.Key == klucz)
                {
                    return para.Value;
                }
            }
            return null;
        }

        public static void DodajSurowiec(Gracz gracz, string klucz, int ile)
        {
            if (ile <= 0 || Opis(klucz) == null)
            {
                return;
            }
            gracz.Surowce.Dodaj(klucz, ile);
            gracz.Statystyki.Dodaj("zebrane_surowce", ile);
        }

        public static string LiniaSurowcow(Gracz gracz)
        {
            var czesci = new List<string>();
            foreach (KeyValuePair<string, OpisSurowca> para in Surowce)
            {
                czesci.Add($"{para.Value.Ikona}{gracz.Surowce.Wez(para.Key)}");
            }
            return "Surowce: " + string.Join("  ", czesci);
        }

        public static int PozostaleZbiory(Pole pole)
        {
            return Math.Max(0, MaxZbiorowNaPolu - pole.Zbierania);
        }

        /// <summary>
        /// Zbiera surowce z aktualnego pola.
        /// Zwraca „ok”, „blokada”, „wyczerpane” albo „walka”.
        /// </summary>
        public static string ZbierzNaPolu(Gracz gracz, IKonsola konsola, Losowanie rng)
        {
            Pole pole = Mapa.PoleGracza(gracz);
            string punkt = pole.Punkt;
            if (punkt == "obóz")
            {
                konsola.Pisz("\n  Przy palenisku nie ma czego zbierać. Wyjdź w teren.");
                return "blokada";
            }
            if (punkt == "boss" || punkt == "miasto" || Array.IndexOf(Mapa.PunktyMityczne, punkt) >= 0)
            {
                konsola.Pisz("\n  Nie pora na zbieractwo — to miejsce jest zbyt niebezpieczne.");
                return "blokada";
            }
            if (PozostaleZbiory(pole) <= 0)
            {
                konsola.Pisz("\n  To pole jest już ogołocone. Spróbuj indziej albo w nowym regionie.");
                return "wyczerpane";
            }

            string biom = pole.Biom ?? "równiny";
            Zbior[] tabela = ZbioryBiomu.TryGetValue(biom, out Zbior[] t) ? t : ZbioryBiomu["równiny"];
            Pora pora = Kalendarz.PoraGracza(gracz);
            double mnoznik = pora.Zbiory * (gracz.MaTalent("tropiciel") ? 1.5 : 1.0);

            var zyski = new List<(string Klucz, int Ile)>();
            foreach (Zbior z in tabela)
            {
                if (rng.Losowa() <= z.Szansa)
                {
                    double ile = rng.Calkowita(z.Min, z.Max) * mnoznik;
                    // Część dziesiętna rozstrzygana losowo — przy mnożniku 1.1
                    // co dziesiąty zbiór jest o jeden większy, a nie zawsze.
                    int calosc = (int)ile + (rng.Losowa() < ile - (int)ile ? 1 : 0);
                    if (calosc > 0)
                    {
                        zyski.Add((z.Klucz, calosc));
                    }
                }
            }
            if (zyski.Count == 0)
            {
                Zbior z = tabela[0];
                zyski.Add((z.Klucz, Math.Max(1, (int)(rng.Calkowita(z.Min, z.Max) * mnoznik))));
            }

            pole.Zbierania += 1;
            konsola.Pisz($"\n  Przeszukujesz {biom}...");
            if (pora.Klucz == "zima")
            {
                konsola.Pisz("  ❄  Zima: pod śniegiem niewiele da się znaleźć.");
            }
            foreach ((string klucz, int ile) in zyski)
            {
                if (klucz == "zywnosc" && !gracz.WObozie)
                {
                    gracz.Prowiant += ile;
                    konsola.Pisz($"  🍖  +{ile} racji do plecaka (prowiant: {gracz.Prowiant})");
                    continue;
                }
                DodajSurowiec(gracz, klucz, ile);
                OpisSurowca info = Opis(klucz);
                konsola.Pisz($"  {info.Ikona}  +{ile} {info.Nazwa}");
            }
            if (SkladnikiBiomu.TryGetValue(biom, out (string Klucz, double Szansa) rzadki)
                && rng.Losowa() < rzadki.Szansa)
            {
                gracz.Skladniki.Dodaj(rzadki.Klucz, 1);
                konsola.Pisz($"  ✨  Znajdujesz: {rzadki.Klucz}  (rzadki składnik!)");
            }
            konsola.Pisz($"  {LiniaSurowcow(gracz)}");
            int zost = PozostaleZbiory(pole);
            konsola.Pisz(zost > 0
                ? $"  (Na tym polu zostało zbiorów: {zost})"
                : "  (Pole wyczerpane.)");

            double szansaWalki = 0.12 * (gracz.MaTalent("szosty_zmysl") ? 0.5 : 1.0);
            return rng.Losowa() < szansaWalki ? "walka" : "ok";
        }
    }
}
