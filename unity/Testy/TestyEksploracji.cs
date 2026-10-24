using System.Collections.Generic;
using System.Linq;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Testy pętli wyprawy — czyli tego, czego wcześniejszy zestaw nie dotykał.
    ///
    /// Porównania generatora, mapy i pikseli nie widziały tej warstwy wcale,
    /// a to w niej siedział najpoważniejszy błąd portu: „pierwszy raz w tym
    /// regionie” liczone po liczbie odkrytych pól zamiast po tym, czy świat
    /// naprawdę wygenerował nowy region. Powrót do znanych stron ogłaszał
    /// wtedy odkrycie i zużywał dodatkowe losowanie, przez co każdy następny
    /// rzut rozjeżdżał się z wersją pythonową przy tym samym seedzie.
    /// </summary>
    public static class TestyEksploracji
    {
        public static void Uruchom()
        {
            SprawdzRozpoznawanieRegionow();
            SprawdzStrumienLosowan();
            SprawdzNazwyBudynkow();
            SprawdzOdpoczynek();
            SprawdzOstrzezenieZimowe();
        }

        /// <summary>
        /// Wyjście z regionu i powrót do niego: odkrycie ogłaszamy raz,
        /// a każde kolejne wejście to powrót w znane strony.
        /// </summary>
        private static void SprawdzRozpoznawanieRegionow()
        {
            Harness.Grupa("Rozpoznawanie nowych i znanych regionów");

            var odpowiedzi = new List<string> { "" }; // Enter po spakowaniu prowiantu
            for (int i = 0; i < 4; i++)
            {
                odpowiedzi.Add("3");                  // wschód wewnątrz regionu
                odpowiedzi.Add("");                   // Enter po komunikacie podróży
            }
            // Piąty krok przekracza krawędź — dochodzi ekran zmiany regionu.
            odpowiedzi.AddRange(new[] { "3", "", "" });  // → region [1, 0], pierwszy raz
            odpowiedzi.AddRange(new[] { "2", "", "" });  // ← powrót do [0, 0], znany
            odpowiedzi.AddRange(new[] { "3", "", "" });  // → znowu [1, 0], ma być znany
            odpowiedzi.AddRange(new[] { "0", "" });      // powrót do obozu

            var konsola = new KonsolaTestowa(odpowiedzi);
            var gracz = new Gracz("Test", "Wojownik", new Losowanie(1)) { Seed = 2024 };
            Mapa.ZapewnijMape(gracz);
            var wyprawa = new Eksploracja(gracz, konsola, null, new Losowanie(2024));
            wyprawa.Uruchom();

            string wyjscie = konsola.Wyjscie;
            int nowe = Zlicz(wyjscie, "NOWY REGION");
            int znane = Zlicz(wyjscie, "ZNANY REGION");

            Harness.Rowne("region [1, 0] ogłoszony jako nowy dokładnie raz", 1, nowe);
            Harness.Rowne("dwa powroty w znane strony", 2, znane);
            Harness.Sprawdz("powrót do regionu startowego rozpoznany",
                            wyjscie.Contains("ZNANY REGION [0, 0]"));
            Harness.Sprawdz("drugie wejście do [1, 0] to już powrót, nie odkrycie",
                            wyjscie.Contains("ZNANY REGION [1, 0]"));
            Harness.Sprawdz("świat zapamiętał oba regiony", Mapa.LiczbaRegionow(gracz) == 2);
        }

        /// <summary>
        /// Ekran nowego regionu losuje opis z tego samego generatora, co
        /// zbieractwo i dzień wyprawy. Ogłoszenie odkrycia o jeden raz za dużo
        /// przesuwałoby więc każde następne losowanie — a z nim cały świat.
        /// Sprawdzamy to wprost: dwa przebiegi po tej samej trasie, jeden
        /// z powrotem do znanego regionu, muszą zostawić generator w tym samym
        /// miejscu co przebieg bez powrotu plus jego własne losowania.
        /// </summary>
        private static void SprawdzStrumienLosowan()
        {
            Harness.Grupa("Powrót w znane strony nie zużywa losowania");

            // Ekran nowego regionu losuje opis z tej samej instancji generatora,
            // co zbieractwo i rzuty na spostrzegawczość. Mierzymy więc zużycie
            // wprost: wejście w nieznany region ma coś pobrać, powrót w znany —
            // nic. Inaczej ten sam seed dałby w Unity inny świat niż w Pythonie.
            var rngNowy = new Losowanie(99);
            int zuzyteNowy = PrzejdzPrzezKrawedz(rngNowy, regionZnany: false,
                                                 out string wyjscieNowy);

            var rngZnany = new Losowanie(99);
            int zuzyteZnany = PrzejdzPrzezKrawedz(rngZnany, regionZnany: true,
                                                  out string wyjscieZnany);

            Harness.Sprawdz("wejście w nieznany region ogłasza odkrycie",
                            wyjscieNowy.Contains("NOWY REGION [1, 0]"));
            Harness.Sprawdz("wejście w znany region ogłasza powrót",
                            wyjscieZnany.Contains("ZNANY REGION [1, 0]"));
            Harness.Sprawdz($"odkrycie pobiera losowanie na opis ({zuzyteNowy} słów)",
                            zuzyteNowy > 0);
            Harness.Rowne("powrót nie rusza generatora", 0, zuzyteZnany);
        }

        /// <summary>
        /// Jeden krok na wschód przez krawędź regionu. Zwraca, ile słów
        /// generatora zużyła cała wyprawa.
        /// </summary>
        private static int PrzejdzPrzezKrawedz(Losowanie rng, bool regionZnany,
                                               out string wyjscie)
        {
            var gracz = new Gracz("Test", "Wojownik", new Losowanie(1)) { Seed = 2024 };
            Mapa.ZapewnijMape(gracz);
            if (regionZnany)
            {
                // Odwiedziny bez ruchu: region trafia do słownika świata,
                // więc kolejne wejście jest powrotem, a nie odkryciem.
                Mapa.RegionPola(gracz, 1, 0);
            }
            // Stajemy przy wschodniej krawędzi, żeby jeden krok ją przekroczył.
            gracz.MapaX = Mapa.Rozmiar - 1;

            var odpowiedzi = new List<string>
            {
                "",        // Enter po spakowaniu prowiantu
                "3",       // wschód — przez krawędź
                "",        // Enter po ekranie zmiany regionu
                "",        // Enter po komunikacie podróży
                "0", "",   // powrót do obozu
            };
            var konsola = new KonsolaTestowa(odpowiedzi);
            int przed = rng.Zuzyte;
            new Eksploracja(gracz, konsola, null, rng).Uruchom();
            wyjscie = konsola.Wyjscie;
            return rng.Zuzyte - przed;
        }

        /// <summary>
        /// Nazwy budynków w biomach muszą być te z <c>game/world.py</c>,
        /// a nie wymyślone — w porcie wzgórza i kanion miały zmyślone.
        /// </summary>
        private static void SprawdzNazwyBudynkow()
        {
            Harness.Grupa("Nazwy budynków zgodne z wersją pythonową");

            var oczekiwane = new (string Biom, string Punkt, string Nazwa)[]
            {
                ("równiny", "karczma", "zajazd na rozstaju"),
                ("równiny", "jaskinia", "stara strażnica"),
                ("równiny", "kuźnia", "wiejska kuźnia"),
                ("ruiny", "świątynia", "opuszczona biblioteka"),
                ("ruiny", "jaskinia", "pęknięta wieża maga"),
                ("las", "karczma", "drewniana chatka"),
                ("las", "jaskinia", "myśliwska wieża"),
                ("bagna", "karczma", "zapadła chata zielarki"),
                ("bagna", "świątynia", "pochylona kaplica"),
                ("wzgórza", "jaskinia", "kamienna strażnica"),
                ("wzgórza", "karczma", "górski posterunek"),
                ("wzgórza", "kuźnia", "kuźnia pod szczytem"),
                ("kanion", "jaskinia", "wykuta brama kopalni"),
                ("kanion", "świątynia", "opuszczony magazyn kupców"),
            };
            foreach ((string biom, string punkt, string nazwa) in oczekiwane)
            {
                Harness.Rowne($"{biom} / {punkt}", nazwa, Swiat.NazwaBudynku(biom, punkt));
            }

            // Obóz, boss, miasto i punkty mityczne mają własne wejścia, więc
            // Python nie zapowiada ich jako budynku stojącego na polu.
            foreach (string punkt in new[] { "obóz", "boss", "miasto", "portal",
                                             "leze_smoka", "latajaca_wyspa" })
            {
                Harness.Sprawdz($"„{punkt}” nie jest budynkiem na polu",
                                Swiat.BudynekNaPolu("las", punkt) == null);
            }
            Harness.Rowne("karczma w lesie jest budynkiem na polu", "drewniana chatka",
                          Swiat.BudynekNaPolu("las", "karczma"));
        }

        /// <summary>
        /// Odpoczynek leczy tyle, co w main.py: baza 30, dom i lecznica
        /// dokładają. Port liczył to od maksymalnego HP, więc Mag odzyskiwał
        /// prawie dwa razy mniej niż powinien, a Wojownik po awansach więcej.
        /// </summary>
        private static void SprawdzOdpoczynek()
        {
            Harness.Grupa("Leczenie odpoczynku niezależne od klasy");

            foreach (string klasa in new[] { "Wojownik", "Mag", "Lotrzyk", "Druid", "Nekromanta" })
            {
                var g = new Gracz("Test", klasa, new Losowanie(1)) { Seed = 7 };
                Mapa.ZapewnijMape(g);
                g.Hp = 1;
                var konsola = new KonsolaTestowa(new[] { "" });
                new Gra(konsola, null, new Losowanie(5)).Odpoczynek(g);
                Harness.Rowne($"{klasa}: odpoczynek bez budynków leczy 30 HP", 31, g.Hp);
            }

            var zDomem = new Gracz("Test", "Mag", new Losowanie(1)) { Seed = 7 };
            Mapa.ZapewnijMape(zDomem);
            zDomem.Hp = 1;
            zDomem.PoziomyBudynkow["dom"] = 1;
            zDomem.PoziomyBudynkow["lecznica"] = 2;
            var k2 = new KonsolaTestowa(new[] { "" });
            new Gra(k2, null, new Losowanie(5)).Odpoczynek(zDomem);
            // 30 + 25*1 + 15*2 = 85, ale Mag ma tylko 70 HP maks.
            Harness.Rowne("dom i lecznica podnoszą leczenie (do pułapu max HP)",
                          zDomem.MaxHp, zDomem.Hp);
        }

        private static void SprawdzOstrzezenieZimowe()
        {
            Harness.Grupa("Ostrzeżenie o mrozie przed wyruszeniem");

            var zima = new Gracz("Test", "Wojownik", new Losowanie(1)) { Seed = 7, Czas = 95 };
            Mapa.ZapewnijMape(zima);
            Harness.Rowne("dzień 95 to zima", "zima", Kalendarz.PoraGracza(zima).Klucz);
            var konsola = new KonsolaTestowa(new[] { "", "0", "" });
            new Eksploracja(zima, konsola, null, new Losowanie(3)).Uruchom();
            Harness.Sprawdz("bez ciepłego odzienia zima ostrzega przed wyjściem",
                            konsola.Wyjscie.Contains("mróz będzie ranił"));

            var zOdzieniem = new Gracz("Test", "Wojownik", new Losowanie(1)) { Seed = 7, Czas = 95 };
            Mapa.ZapewnijMape(zOdzieniem);
            zOdzieniem.Przedmioty["cieple_odzienie"] = 1;
            var k2 = new KonsolaTestowa(new[] { "", "0", "" });
            new Eksploracja(zOdzieniem, k2, null, new Losowanie(3)).Uruchom();
            Harness.Sprawdz("z odzieniem ostrzeżenia nie ma",
                            !k2.Wyjscie.Contains("mróz będzie ranił"));

            var lato = new Gracz("Test", "Wojownik", new Losowanie(1)) { Seed = 7, Czas = 40 };
            Mapa.ZapewnijMape(lato);
            var k3 = new KonsolaTestowa(new[] { "", "0", "" });
            new Eksploracja(lato, k3, null, new Losowanie(3)).Uruchom();
            Harness.Sprawdz("latem ostrzeżenia nie ma",
                            !k3.Wyjscie.Contains("mróz będzie ranił"));
        }

        private static int Zlicz(string tekst, string czego)
        {
            int ile = 0;
            int od = 0;
            while ((od = tekst.IndexOf(czego, od, System.StringComparison.Ordinal)) >= 0)
            {
                ile++;
                od += czego.Length;
            }
            return ile;
        }
    }
}
