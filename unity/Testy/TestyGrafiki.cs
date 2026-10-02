using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Grafika;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Porównuje pixelart z wersją pythonową — piksel w piksel.
    ///
    /// Kafel terenu to najgęstszy kawałek matematyki w całej grafice: szum
    /// gładki, szum punktowy, rampy z ditheringiem Bayera, warstwy skały
    /// i darń na krawędzi. Jeśli kafle i sprite'y zgadzają się co do bajtu,
    /// to znaczy, że port liczy dokładnie to samo, a nie tylko podobnie.
    /// </summary>
    public static class TestyGrafiki
    {
        public static void Uruchom(JsonElement piksele, JsonElement kafle, JsonElement sprite)
        {
            SprawdzPrymitywy(piksele);
            SprawdzKafle(kafle);
            SprawdzSprite(sprite);
            SprawdzScene();
        }

        private static void SprawdzPrymitywy(JsonElement w)
        {
            Harness.Grupa("Prymitywy pixelartu: szum, rampy, rozjaśnianie");

            bool szumOk = true;
            foreach (JsonElement d in w.GetProperty("szum").EnumerateArray())
            {
                double otrzymane = Piksele.Szum(d.GetProperty("x").GetInt32(),
                                                d.GetProperty("y").GetInt32(),
                                                d.GetProperty("s").GetInt32());
                szumOk &= otrzymane == d.GetProperty("v").GetDouble();
            }
            Harness.Sprawdz("szum punktowy co do bitu", szumOk);

            bool gladkiOk = true;
            foreach (JsonElement d in w.GetProperty("szum_gladki").EnumerateArray())
            {
                double otrzymane = Piksele.SzumGladki(d.GetProperty("x").GetDouble(),
                                                      d.GetProperty("y").GetDouble(),
                                                      d.GetProperty("skala").GetDouble(),
                                                      d.GetProperty("s").GetInt32());
                gladkiOk &= otrzymane == d.GetProperty("v").GetDouble();
            }
            Harness.Sprawdz("szum gładki (interpolowany) co do bitu", gladkiOk);

            bool rampaOk = true;
            foreach (JsonElement d in w.GetProperty("z_rampy_las").EnumerateArray())
            {
                Barwa otrzymana = Piksele.ZRampy(Teren.Rampy["las"],
                                                 d.GetProperty("v").GetDouble(),
                                                 d.GetProperty("x").GetInt32(),
                                                 d.GetProperty("y").GetInt32());
                rampaOk &= PorownajBarwe(d.GetProperty("kolor"), otrzymana);
            }
            Harness.Sprawdz("rampa z ditheringiem Bayera (w tym wartości poza 0–1)", rampaOk);

            double[] ciemne = { 0.6, 0.7, 0.8, 1.0 };
            bool ciemnoOk = w.GetProperty("ciemniej").EnumerateArray()
                             .Select((d, i) => PorownajBarwe(d, Piksele.Ciemniej(Teren.Kamien[2], ciemne[i])))
                             .All(x => x);
            Harness.Sprawdz("przyciemnianie (obcinanie jak int() w Pythonie)", ciemnoOk);

            double[] jasne = { 1.18, 1.0, 1.5 };
            bool jasnoOk = w.GetProperty("jasniej").EnumerateArray()
                            .Select((d, i) => PorownajBarwe(d, Piksele.Jasniej(Teren.Kamien[1], jasne[i])))
                            .All(x => x);
            Harness.Sprawdz("rozjaśnianie z nasyceniem na 255", jasnoOk);
        }

        private static void SprawdzKafle(JsonElement w)
        {
            Harness.Grupa("Kafle terenu, mgła wojny i obwódka — piksel w piksel");

            foreach (string biom in new[] { "równiny", "las", "bagna", "wzgórza", "kanion", "ruiny" })
            {
                JsonElement d = w.GetProperty($"kafel|{biom}");
                int s = d.GetProperty("s").GetInt32();
                int h = d.GetProperty("wysokosc_pola").GetInt32();
                Harness.Rowne($"{biom}: wysokość pola z szumu", h, Teren.WysokoscPola(biom, 3, 4, s));
                PorownajPlotno($"kafel {biom} ({Teren.TW}×{Teren.TH + h + 6})", d, Teren.Kafel(biom, h, s));
            }

            foreach (int klatka in new[] { 1, 2 })
            {
                JsonElement d = w.GetProperty($"kafel|bagna|k{klatka}");
                PorownajPlotno($"kafel bagna, klatka animacji wody {klatka}", d,
                               Teren.Kafel("bagna", 1, 77, klatka));
            }

            Plotno kafelLasu = Teren.Kafel("las", 5, 1234);
            PorownajPlotno("mgła wojny w dzień", w.GetProperty("mgla|dzien"),
                           Teren.Zamglij(kafelLasu, 2, 6, false));
            PorownajPlotno("mgła wojny w nocy", w.GetProperty("mgla|noc"),
                           Teren.Zamglij(kafelLasu, 2, 6, true));
            PorownajPlotno("obwódka pola gracza", w.GetProperty("obwodka"), Teren.Obwodka());
        }

        private static void SprawdzSprite(JsonElement w)
        {
            Harness.Grupa("Roślinność, skały i sylwetka bohatera — piksel w piksel");

            PorownajPlotno("kula cieniowana po normalnej (7×6)", w.GetProperty("kula|7x6"),
                           Piksele.Kula(7, 6, Teren.Rampy["las"], 3, 0.3));
            PorownajPlotno("sosna o wysokości 22", w.GetProperty("sosna|22"), Teren.Sosna(22, 5));
            PorownajPlotno("drzewo liściaste (kula + kontur)", w.GetProperty("drzewo_lisciaste"),
                           Teren.DrzewoLisciaste(8));
            PorownajPlotno("skała 4×3", w.GetProperty("skala|4x3"), Teren.Skala(4, 3, 12));
            PorownajPlotno("trzcina na bagnach", w.GetProperty("trzcina"), Teren.Trzcina(6));
            PorownajPlotno("złamana kolumna w ruinach", w.GetProperty("kolumna|9"), Teren.Kolumna(4, 9));

            foreach (string klasa in new[] { "wojownik", "mag", "łotrzyk", "druid", "nekromanta" })
            {
                PorownajPlotno($"sylwetka bohatera: {klasa}", w.GetProperty($"gracz|{klasa}"),
                               Teren.SpriteGracza(klasa));
            }
        }

        /// <summary>
        /// Złożenie całej sceny. Tu nie porównujemy z Pythonem — rysunek
        /// zawiera budynki wypełniane wielokątami i półprzejrzysty cień,
        /// a rasteryzacja wielokątów w pygame ma własne reguły brzegowe.
        /// Sprawdzamy więc to, co musi być prawdą zawsze: że scena ma właściwy
        /// rozmiar, że mgła naprawdę zasłania nieodkryte pola, że dzień różni
        /// się od zmierzchu i że nic nie wychodzi za płótno.
        /// </summary>
        private static void SprawdzScene()
        {
            Harness.Grupa("Złożenie sceny mapy");

            var scena = new ScenaMapy();
            var pola = new PoleDoRysunku[9][];
            for (int y = 0; y < 9; y++)
            {
                pola[y] = new PoleDoRysunku[9];
                for (int x = 0; x < 9; x++)
                {
                    pola[y][x] = new PoleDoRysunku
                    {
                        Biom = new[] { "równiny", "las", "bagna", "wzgórza", "kanion", "ruiny" }[(x + y) % 6],
                        Odkryte = x + y < 9,
                        Punkt = x == 4 && y == 4 ? "obóz" : null,
                    };
                }
            }

            var cel = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);
            scena.Rysuj(cel, pola, 0.0, (0, 0), (4, 4), "Wojownik",
                        osada: (2, 1, 0));
            Harness.Rowne("scena ma szerokość 320", 320, cel.Szerokosc);
            Harness.Rowne("scena ma wysokość 240", 240, cel.Wysokosc);
            Harness.Rowne("bufor RGBA ma 320×240×4 bajtów", 320 * 240 * 4, cel.Dane.Length);
            Harness.Sprawdz("scena nie jest pusta", cel.Dane.Any(b => b != 0));
            Harness.Sprawdz("każdy piksel sceny jest kryjący",
                            Enumerable.Range(0, 320 * 240).All(i => cel.Dane[i * 4 + 3] == 255));

            // Mgła: pole nieodkryte musi wyglądać inaczej niż to samo pole odkryte.
            var jasne = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);
            scena.Rysuj(jasne, pola, 0.0, (0, 0), (4, 4), "Wojownik",
                        wszystkoOdkryte: true, osada: (2, 1, 0));
            Harness.Sprawdz("odkrycie całej mapy zmienia obraz",
                            !cel.Dane.SequenceEqual(jasne.Dane));

            // Zmierzch: mapa światła musi przyciemnić scenę.
            var noc = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);
            scena.Zmierzch = true;
            scena.Rysuj(noc, pola, 0.0, (0, 0), (4, 4), "Wojownik", osada: (2, 1, 0));
            scena.Zmierzch = false;
            Harness.Sprawdz("zmierzch różni się od dnia", !noc.Dane.SequenceEqual(cel.Dane));
            double sredniaDzien = SredniaJasnosc(cel);
            double sredniaNoc = SredniaJasnosc(noc);
            Harness.Sprawdz($"zmierzch jest ciemniejszy niż dzień " +
                            $"({sredniaNoc:F1} < {sredniaDzien:F1})", sredniaNoc < sredniaDzien);

            // Animacja: inna chwila w czasie daje inną klatkę (woda, ogień, chód).
            var pozniej = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);
            scena.Rysuj(pozniej, pola, 0.4, (0, 0), (4, 4), "Wojownik", osada: (2, 1, 0));
            Harness.Sprawdz("scena się animuje w czasie", !pozniej.Dane.SequenceEqual(cel.Dane));

            // Ekran tytułowy: bez postaci, wszystko odkryte — też musi się narysować.
            var tytul = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);
            scena.Rysuj(tytul, pola, 0.0, (9999, 9999), wszystkoOdkryte: true);
            Harness.Sprawdz("scena tytułowa (bez gracza) się rysuje", tytul.Dane.Any(b => b != 0));

            // Rosnąca osada zmienia widok obozu — to sprzęgnięcie logiki z grafiką.
            var duzaOsada = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);
            scena.Rysuj(duzaOsada, pola, 0.0, (0, 0), (4, 4), "Wojownik", osada: (6, 2, 1));
            Harness.Sprawdz("osada z wieżą i palisadą wygląda inaczej niż obóz startowy",
                            !duzaOsada.Dane.SequenceEqual(cel.Dane));
        }

        private static double SredniaJasnosc(Plotno p)
        {
            long suma = 0;
            for (int i = 0; i < p.Dane.Length; i += 4)
            {
                suma += p.Dane[i] + p.Dane[i + 1] + p.Dane[i + 2];
            }
            return suma / (double)(p.Dane.Length / 4 * 3);
        }

        private static bool PorownajBarwe(JsonElement oczekiwana, Barwa otrzymana)
        {
            var k = oczekiwana.EnumerateArray().Select(e => e.GetInt32()).ToList();
            return k[0] == otrzymana.R && k[1] == otrzymana.G && k[2] == otrzymana.B;
        }

        /// <summary>
        /// Porównuje płótno z wzorcem bajt po bajcie. Przy różnicy mówi, ile
        /// pikseli się rozjechało i który jest pierwszy — bez tego szukanie
        /// błędu w cieniowaniu byłoby zgadywaniem.
        /// </summary>
        private static void PorownajPlotno(string opis, JsonElement wzorzec, Plotno otrzymane)
        {
            int w = wzorzec.GetProperty("w").GetInt32();
            int h = wzorzec.GetProperty("h").GetInt32();
            if (w != otrzymane.Szerokosc || h != otrzymane.Wysokosc)
            {
                Harness.Sprawdz($"{opis}  (rozmiar: oczekiwano {w}×{h}, " +
                                $"jest {otrzymane.Szerokosc}×{otrzymane.Wysokosc})", false);
                return;
            }
            var piksele = wzorzec.GetProperty("piksele").EnumerateArray()
                                 .Select(e => (byte)e.GetInt32()).ToArray();
            int rozne = 0;
            string pierwszy = null;
            for (int i = 0; i < piksele.Length; i += 4)
            {
                if (piksele[i] == otrzymane.Dane[i] && piksele[i + 1] == otrzymane.Dane[i + 1]
                    && piksele[i + 2] == otrzymane.Dane[i + 2] && piksele[i + 3] == otrzymane.Dane[i + 3])
                {
                    continue;
                }
                rozne++;
                if (pierwszy == null)
                {
                    int px = i / 4 % w;
                    int py = i / 4 / w;
                    pierwszy = $"({px}, {py}): oczekiwano " +
                               $"({piksele[i]}, {piksele[i + 1]}, {piksele[i + 2]}, {piksele[i + 3]}), " +
                               $"jest ({otrzymane.Dane[i]}, {otrzymane.Dane[i + 1]}, " +
                               $"{otrzymane.Dane[i + 2]}, {otrzymane.Dane[i + 3]})";
                }
            }
            int wszystkie = piksele.Length / 4;
            Harness.Sprawdz(rozne == 0
                ? $"{opis} — {wszystkie} pikseli zgodnych"
                : $"{opis}  ({rozne}/{wszystkie} pikseli różnych, pierwszy {pierwszy})", rozne == 0);
        }
    }
}
