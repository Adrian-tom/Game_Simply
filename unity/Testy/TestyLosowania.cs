using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Porównuje port MT19937 z wzorcami zrzuconymi z CPythona.
    ///
    /// Sprawdzamy nie tylko pierwsze liczby, ale całe sekwencje z każdej metody.
    /// Metody różnią się liczbą zużywanych słów generatora (np. odrzucone próby
    /// w <c>Ponizej</c>), więc błąd w jednej przesunąłby wszystko, co po niej.
    /// </summary>
    public static class TestyLosowania
    {
        private static readonly string[] Biomy =
        {
            "równiny", "ruiny", "las", "bagna", "wzgórza", "kanion"
        };

        public static void Uruchom(JsonElement wzorce)
        {
            Harness.Grupa("Generator losowy zgodny z CPythonem");

            foreach (JsonProperty wpis in wzorce.EnumerateObject())
            {
                long seed = long.Parse(wpis.Name);
                JsonElement dane = wpis.Value;

                var rng = new Losowanie(seed);
                var losowe = dane.GetProperty("random").EnumerateArray()
                                 .Select(e => e.GetDouble()).ToList();
                bool zgodneLosowe = losowe.All(oczekiwane => rng.Losowa() == oczekiwane);
                Harness.Sprawdz($"seed {seed}: random() × {losowe.Count}", zgodneLosowe);

                rng = new Losowanie(seed);
                int[] szerokosci = { 1, 3, 7, 8, 16, 31, 32 };
                var bity = dane.GetProperty("getrandbits").EnumerateArray()
                               .Select(e => e.GetUInt64()).ToList();
                bool zgodneBity = szerokosci.Select((k, i) => rng.Bity(k) == bity[i]).All(x => x);
                Harness.Sprawdz($"seed {seed}: getrandbits(1…32)", zgodneBity);

                rng = new Losowanie(seed);
                var calkowite = dane.GetProperty("randint_0_8").EnumerateArray()
                                    .Select(e => e.GetInt32()).ToList();
                bool zgodneCalkowite = calkowite.All(oczekiwane => rng.Calkowita(0, 8) == oczekiwane);
                Harness.Sprawdz($"seed {seed}: randint(0, 8) × {calkowite.Count}", zgodneCalkowite);

                rng = new Losowanie(seed);
                var zakresy = dane.GetProperty("randrange_1_2p31").EnumerateArray()
                                  .Select(e => e.GetInt32()).ToList();
                bool zgodneZakresy = zakresy.All(oczekiwane => rng.Zakres(1, int.MaxValue) == oczekiwane);
                Harness.Sprawdz($"seed {seed}: randrange(1, 2**31)", zgodneZakresy);

                rng = new Losowanie(seed);
                var probki = dane.GetProperty("sample").EnumerateArray()
                                 .Select(e => e.EnumerateArray().Select(x => x.GetString()).ToList())
                                 .ToList();
                bool zgodneProbki = true;
                for (int i = 0; i < probki.Count; i++)
                {
                    List<string> otrzymana = rng.Probka(Biomy, i + 4);
                    if (!otrzymana.SequenceEqual(probki[i]))
                    {
                        zgodneProbki = false;
                    }
                }
                Harness.Sprawdz($"seed {seed}: sample(biomy, k=4…6)", zgodneProbki);

                rng = new Losowanie(seed);
                var wybory = dane.GetProperty("choice").EnumerateArray()
                                 .Select(e => e.GetString()).ToList();
                bool zgodneWybory = wybory.All(oczekiwane => rng.Wybierz(Biomy) == oczekiwane);
                Harness.Sprawdz($"seed {seed}: choice(biomy) × {wybory.Count}", zgodneWybory);
            }
        }
    }
}
