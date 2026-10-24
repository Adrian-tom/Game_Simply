using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Porównuje wygenerowane regiony z wzorcami z Pythona.
    ///
    /// Sprawdzane są całe siatki 9×9 — i biomy, i punkty orientacyjne — dla
    /// regionu startowego, sąsiadów, regionów dalekich (gdzie wchodzą mityczne
    /// punkty i miasta) oraz współrzędnych ujemnych. To pokrywa wszystkie
    /// gałęzie generatora i dowodzi, że ten sam zapis da w Unity tę samą mapę.
    /// </summary>
    public static class TestyMapy
    {
        public static void Uruchom(JsonElement wzorce)
        {
            Harness.Grupa("Generowanie świata zgodne z wersją pythonową");

            foreach (JsonProperty wpis in wzorce.EnumerateObject())
            {
                string[] czesci = wpis.Name.Split('|');
                long seed = long.Parse(czesci[0]);
                int rx = int.Parse(czesci[1]);
                int ry = int.Parse(czesci[2]);
                JsonElement dane = wpis.Value;

                int poziomOczekiwany = dane.GetProperty("poziom").GetInt32();
                Harness.Rowne($"region [{rx}, {ry}]: poziom trudności",
                              poziomOczekiwany, Mapa.PoziomRegionu(rx, ry));

                Pole[][] pola = Mapa.GenerujMape(poziomOczekiwany, seed, rx, ry);

                var biomyOczekiwane = Splaszcz(dane.GetProperty("biomy"));
                var biomyOtrzymane = pola.SelectMany(w => w.Select(p => p.Biom)).ToList();
                Harness.RowneCiagi($"seed {seed} region [{rx}, {ry}]: biomy (81 pól)",
                                   biomyOczekiwane, biomyOtrzymane);

                var punktyOczekiwane = Splaszcz(dane.GetProperty("punkty"));
                var punktyOtrzymane = pola.SelectMany(w => w.Select(p => p.Punkt ?? "")).ToList();
                Harness.RowneCiagi($"seed {seed} region [{rx}, {ry}]: punkty (81 pól)",
                                   punktyOczekiwane, punktyOtrzymane);
            }

            SprawdzRuch();
        }

        private static List<string> Splaszcz(JsonElement siatka)
        {
            return siatka.EnumerateArray()
                         .SelectMany(w => w.EnumerateArray().Select(p => p.GetString()))
                         .ToList();
        }

        /// <summary>
        /// Ruch przez krawędź regionu — najłatwiejsze miejsce na pomyłkę, bo
        /// Python dzieli i resztuje w dół, a C# obcina do zera. Wyjście na
        /// zachód z kolumny 0 musi trafić do regionu o x−1, na pole 8.
        /// </summary>
        private static void SprawdzRuch()
        {
            Harness.Grupa("Ruch po mapie i trwałość świata");

            var gracz = new Gracz("Test", "Wojownik", new Losowanie(1)) { Seed = 42 };
            Mapa.ZapewnijMape(gracz);
            Harness.Rowne("start na środku regionu (x)", Mapa.Srodek, gracz.MapaX);
            Harness.Rowne("start na środku regionu (y)", Mapa.Srodek, gracz.MapaY);
            Harness.Sprawdz("pole startowe jest odkryte", Mapa.PoleGracza(gracz).Odkryte);
            Harness.Rowne("na starcie odkryte jedno pole", 1, Mapa.LiczbaOdkrytych(gracz));

            // Na zachód aż za krawędź: 4 kroki do kolumny 0, piąty zmienia region.
            for (int i = 0; i < 4; i++)
            {
                Harness.Sprawdz($"krok {i + 1} na zachód zostaje w regionie",
                                !Mapa.PrzesunGracza(gracz, -1, 0));
            }
            Harness.Rowne("po czterech krokach jesteśmy w kolumnie 0", 0, gracz.MapaX);
            Harness.Sprawdz("piąty krok przekracza krawędź", Mapa.PrzesunGracza(gracz, -1, 0));
            Harness.Rowne("region przesunął się na zachód", -1, gracz.RegionX);
            Harness.Rowne("wchodzimy od wschodniej krawędzi", Mapa.Rozmiar - 1, gracz.MapaX);
            Harness.Rowne("poziom trudności rośnie z odległością", 2, gracz.MapaGen);

            // Powrót na wschód musi wrócić dokładnie tam, skąd wyszliśmy.
            Harness.Sprawdz("powrót na wschód znów przekracza krawędź",
                            Mapa.PrzesunGracza(gracz, 1, 0));
            Harness.Rowne("wracamy do regionu startowego", 0, gracz.RegionX);
            Harness.Rowne("wracamy na kolumnę 0", 0, gracz.MapaX);
            Harness.Rowne("świat pamięta dwa regiony", 2, Mapa.LiczbaRegionow(gracz));

            // Trwałość: ten sam region po powrocie ma te same pola i odkrycia.
            int odkryte = Mapa.LiczbaOdkrytych(gracz);
            Pole[][] przed = gracz.MapaPola;
            Mapa.PrzesunGracza(gracz, -1, 0);
            Mapa.PrzesunGracza(gracz, 1, 0);
            Harness.Sprawdz("powrót daje tę samą siatkę (ten sam obiekt)",
                            ReferenceEquals(przed, gracz.MapaPola));
            Harness.Rowne("odkrycia nie przepadły", odkryte, Mapa.LiczbaOdkrytych(gracz));

            // Regiony dalekie: trudność po Czebyszewie, nie po sumie.
            Harness.Rowne("region [3, 2] ma poziom 4", 4, Mapa.PoziomRegionu(3, 2));
            Harness.Rowne("region [-5, 1] ma poziom 6", 6, Mapa.PoziomRegionu(-5, 1));
            Harness.Rowne("opis regionu startowego", "region startowy (obóz)",
                          Mapa.OpisRegionu(new Gracz("T", "Mag", new Losowanie(1))));
        }
    }
}
