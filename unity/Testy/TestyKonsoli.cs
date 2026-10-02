using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraSimply.Grafika;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Sprawdza serce portu: logikę chodzącą na osobnym wątku i konsolę jako
    /// śluzę między nią a rysowaniem.
    ///
    /// To jedyna część, która nie jest przepisaniem Pythona, a nowym
    /// rozwiązaniem — więc tym bardziej musi być sprawdzona. Testujemy, że
    /// logika rzeczywiście się blokuje, że odpowiedź do niej dochodzi, że
    /// zamknięcie okna budzi ją wyjątkiem i że klawisz wysłany, gdy gra o nic
    /// nie pyta, nie zostaje zapamiętany na później.
    /// </summary>
    public static class TestyKonsoli
    {
        /// <summary>Ile czekamy na drugi wątek, zanim uznamy, że coś się zawiesiło.</summary>
        private const int LimitMs = 5000;

        public static void Uruchom()
        {
            SprawdzSluze();
            SprawdzZamkniecie();
            SprawdzPelnaGreNaWatku();
            SprawdzUkladKonsoli();
        }

        private static void SprawdzSluze()
        {
            Harness.Grupa("Konsola między wątkami");

            var konsola = new KonsolaKolejkowa();
            bool przedPytaniem = false;
            konsola.PrzedPytaniem = () => przedPytaniem = true;

            string odebrane = null;
            Task logika = Task.Run(() =>
            {
                konsola.Pisz("  Pytanie o kierunek");
                odebrane = konsola.Czytaj("  Twój wybór: ");
            });

            Harness.Sprawdz("logika zatrzymała się na pytaniu", Poczekaj(() => konsola.CzekaNaWejscie));
            Harness.Sprawdz("zaczepka przed pytaniem została wywołana", przedPytaniem);
            Harness.Sprawdz("zachęta jest widoczna dla widoku",
                            konsola.Zacheta.Contains("Twój wybór"));
            Harness.Sprawdz("to nie jest pytanie typu „naciśnij Enter”", !konsola.CzekaNaEnter);

            Harness.Sprawdz("odpowiedź została przyjęta", konsola.Odpowiedz("3"));
            Harness.Sprawdz("logika ruszyła dalej", logika.Wait(LimitMs));
            Harness.Rowne("logika dostała dokładnie to, co wpisał gracz", "3", odebrane);
            Harness.Sprawdz("po odpowiedzi konsola już nie czeka", !konsola.CzekaNaWejscie);
            Harness.Sprawdz("klawisz wysłany, gdy gra nie pyta, jest odrzucany",
                            !konsola.Odpowiedz("7"));

            List<string> linie = konsola.Linie();
            Harness.Sprawdz("zachęta i odpowiedź są w tej samej linii",
                            linie.Exists(l => l.Contains("Twój wybór:") && l.TrimEnd().EndsWith("3")));

            // Pytanie z „Enter” w treści włącza obsługę spacji i kliknięcia.
            var druga = new KonsolaKolejkowa();
            Task czekanie = Task.Run(() => druga.Czytaj("  [Naciśnij Enter, aby kontynuować...]"));
            Harness.Sprawdz("drugie pytanie też blokuje", Poczekaj(() => druga.CzekaNaWejscie));
            Harness.Sprawdz("rozpoznane jako pytanie o Enter", druga.CzekaNaEnter);
            druga.Odpowiedz("");
            Harness.Sprawdz("puste potwierdzenie przepuszcza logikę", czekanie.Wait(LimitMs));
        }

        private static void SprawdzZamkniecie()
        {
            Harness.Grupa("Zamknięcie okna w trakcie pytania");

            var konsola = new KonsolaKolejkowa();
            bool wyszloWyjatkiem = false;
            Task logika = Task.Run(() =>
            {
                try
                {
                    konsola.Czytaj("  Twój wybór: ");
                }
                catch (GraZamknieta)
                {
                    wyszloWyjatkiem = true;
                }
            });

            Harness.Sprawdz("logika czeka", Poczekaj(() => konsola.CzekaNaWejscie));
            konsola.Zamknij();
            Harness.Sprawdz("wątek logiki się zakończył", logika.Wait(LimitMs));
            Harness.Sprawdz("logika została obudzona wyjątkiem GraZamknieta", wyszloWyjatkiem);
            Harness.Sprawdz("konsola wie, że jest zamknięta", konsola.Zamknieta);

            // Drugie zamknięcie nie może niczego wywrócić — silnik woła je
            // i z OnDestroy, i z OnApplicationQuit.
            konsola.Zamknij();
            Harness.Sprawdz("powtórne zamknięcie jest bezpieczne", konsola.Zamknieta);
        }

        /// <summary>
        /// Pełna gra przepuszczona przez konsolę kolejkową, tak jak w Unity:
        /// logika na jednym wątku, „gracz” na drugim. Gdyby śluza się zacięła,
        /// test by nie wrócił — dlatego wszystko ma limit czasu.
        /// </summary>
        private static void SprawdzPelnaGreNaWatku()
        {
            Harness.Grupa("Pełna gra na dwóch wątkach (jak w Unity)");

            var konsola = new KonsolaKolejkowa();
            var ekran = new Ekran();
            var gra = new Logika.Gra(konsola, ekran, new Losowanie(7));
            Migawka migawka = null;
            konsola.PrzedPytaniem = () =>
            {
                Migawka nowa = ekran.Migawke();
                if (nowa != null)
                {
                    migawka = nowa;
                }
            };

            Task logika = Task.Run(() => gra.Uruchom());

            var scenariusz = new[]
            {
                "1", "Borys", "4", "",   // nowa gra, imię, Druid, Enter po karcie
                "1", "",                 // wyprawa, Enter po prowiancie
                "3", "",                 // na wschód, Enter po podróży
                "0", "",                 // powrót do obozu, Enter
                "0", "0",                // koniec gry, wyjście z menu
            };
            bool wszystkoWeszlo = true;
            foreach (string odpowiedz in scenariusz)
            {
                // Po każdej odpowiedzi czekamy, aż logika ją naprawdę odbierze.
                // Bez tego pętla potrafiłaby wsunąć kolejną odpowiedź, zanim
                // poprzednia zostanie zabrana z kolejki — a kolejka ma miejsce
                // na jedną, więc druga by przepadła.
                int przed = konsola.OdebraneOdpowiedzi;
                if (!Poczekaj(() => konsola.CzekaNaWejscie)
                    || !konsola.Odpowiedz(odpowiedz)
                    || !Poczekaj(() => konsola.OdebraneOdpowiedzi > przed))
                {
                    wszystkoWeszlo = false;
                    break;
                }
            }
            Harness.Sprawdz("każda odpowiedź trafiła do logiki", wszystkoWeszlo);
            Harness.Sprawdz("gra zakończyła się sama, bez zamykania okna", logika.Wait(LimitMs));

            Harness.Sprawdz("migawka świata powstała", migawka != null);
            if (migawka != null)
            {
                Harness.Rowne("migawka ma siatkę 9×9", 9, migawka.Pola.Length);
                Harness.Sprawdz("migawka zna postać", migawka.Hud != null);
                Harness.Rowne("migawka zna imię bohatera", "Borys", migawka.Hud.Imie);
                Harness.Rowne("migawka zna klasę", "Druid", migawka.Klasa);
                Harness.Sprawdz("druid ma manę w HUD", migawka.Hud.MaxMana > 0);
                Harness.Sprawdz("migawka zna pozycję gracza", migawka.GraczXy.HasValue);
                Harness.Sprawdz("migawka zna stan osady", migawka.Osada.HasValue);

                // Migawka musi być kopią: zmiana w świecie nie może jej ruszyć.
                PoleDoRysunku przed = migawka.Pola[0][0];
                Harness.Sprawdz("pola migawki mają biom", !string.IsNullOrEmpty(przed.Biom));
            }

            // Pulpit musi złożyć klatkę z tej migawki bez wyjątku.
            if (migawka != null)
            {
                var pulpit = new Pulpit();
                List<Napis> napisy = pulpit.Zloz(migawka.Pola, 0.0, migawka.Hud, migawka.Region,
                                                 migawka.GraczXy, migawka.Klasa,
                                                 migawka.WszystkoOdkryte, migawka.Osada);
                Harness.Rowne("klatka pulpitu ma 1280×720", 1280 * 720 * 4, pulpit.Klatka.Dane.Length);
                Harness.Sprawdz("pulpit zwrócił napisy HUD", napisy.Count > 5);
                Harness.Sprawdz("HUD pokazuje imię bohatera",
                                napisy.Exists(n => n.Tekst == "Borys"));
            }

            // Migawka tytułowa — stan przed stworzeniem postaci.
            Migawka tytul = Migawka.Tytulowa();
            var pulpitTytulu = new Pulpit();
            List<Napis> napisyTytulu = pulpitTytulu.Zloz(tytul.Pola, 0.0, null, tytul.Region,
                                                         null, null, true, null);
            Harness.Sprawdz("ekran tytułowy się składa", napisyTytulu.Count > 0);
            Harness.Sprawdz("ekran tytułowy nie rysuje pasków postaci",
                            !napisyTytulu.Exists(n => n.Tekst == "HP"));
        }

        private static void SprawdzUkladKonsoli()
        {
            Harness.Grupa("Układanie tekstu konsoli");

            Harness.Rowne("klawisz opcji z nawiasów", "3",
                          Grafika.Konsola.KlawiszOpcji("  [3]  ➡  wschód  →  🌲 las"));
            Harness.Rowne("opcja wieloznakowa", "0",
                          Grafika.Konsola.KlawiszOpcji("  [0]  🏕  Wróć do obozu"));
            Harness.Sprawdz("zwykła linia nie jest opcją",
                            Grafika.Konsola.KlawiszOpcji("  Biom: 🌲 las") == null);
            Harness.Sprawdz("linia bez zamknięcia nawiasu nie jest opcją",
                            Grafika.Konsola.KlawiszOpcji("  [3  wschód") == null);
            // Wyjście awaryjne, gdy krój nie ma ikon: emoji znikają, ale linie
            // ramek i układ kolumn zostają.
            // Z ikoną znika jedna spacja po niej; wcięcia i kolumny zostają,
            // więc menu nadal się czyta, choć odstęp bywa o znak szerszy.
            Harness.Rowne("emoji wycięte z opcji menu", "  [3]   wschód",
                          Grafika.Konsola.BezEmoji("  [3]  ➡  wschód"));
            Harness.Rowne("linia ramki przeżywa wycinanie", "  ════════════",
                          Grafika.Konsola.BezEmoji("  ════════════"));
            Harness.Rowne("tekst bez emoji zostaje bez zmian", "  Biom: las",
                          Grafika.Konsola.BezEmoji("  Biom: las"));
            Harness.Rowne("ikona spoza podstawowej płaszczyzny też znika", "   obóz",
                          Grafika.Konsola.BezEmoji("  🏕  obóz"));
            Harness.Rowne("ikony przy pojedynczych odstępach", "  Halina 148 zł",
                          Grafika.Konsola.BezEmoji("  🧙 Halina 💰 148 zł"));

            Harness.Sprawdz("ozdobnik rozpoznany",
                            Grafika.Konsola.CzyOzdobnik("  ════════════"));
            Harness.Sprawdz("tekst nie jest ozdobnikiem",
                            !Grafika.Konsola.CzyOzdobnik("  EKSPLORACJA"));

            // Zawijanie zachowuje wcięcie i dokłada dwa znaki w ciągu dalszym.
            List<string> zawiniete = Grafika.Konsola.Zawin(
                "    Falujące trawy i stary trakt ciągną się aż po horyzont.", 24);
            Harness.Sprawdz("długa linia została zawinięta", zawiniete.Count > 1);
            Harness.Sprawdz("pierwszy wiersz trzyma wcięcie", zawiniete[0].StartsWith("    "));
            Harness.Sprawdz("ciąg dalszy jest wcięty głębiej", zawiniete[1].StartsWith("      "));
            Harness.Sprawdz("żaden wiersz nie przekracza szerokości",
                            zawiniete.TrueForAll(w => w.Length <= 24 + 6));

            Harness.Rowne("krótka linia zostaje jednym wierszem", 1,
                          Grafika.Konsola.Zawin("  [1]  północ", 40).Count);

            // Okno widoku: przy nadmiarze linii widać koniec, nie początek.
            var linie = new List<string>();
            for (int i = 0; i < 60; i++)
            {
                linie.Add($"  linia {i}");
            }
            List<WierszKonsoli> widok = Grafika.Konsola.Widok(linie, 60, 10, 0, null, false,
                                                              out int ileWyzej);
            // Przy nadmiarze treści pierwszy wiersz zajmuje znacznik „wyżej jest
            // więcej”, więc na tekst zostaje o jeden mniej — tak samo jak w wersji
            // pythonowej.
            Harness.Rowne("dziesięć wierszy minus wiersz na znacznik", 9, widok.Count);
            Harness.Sprawdz("widać ostatnią linię", widok[widok.Count - 1].Tekst.Contains("linia 59"));
            Harness.Sprawdz("znacznik mówi, ile zostało wyżej", ileWyzej > 0);

            // Gdy treść się mieści, nie ma znacznika i nie tracimy wiersza.
            var krotkie = new List<string> { "  raz", "  dwa", "  trzy" };
            List<WierszKonsoli> caly = Grafika.Konsola.Widok(krotkie, 60, 10, 0, null, false,
                                                             out int bezZnacznika);
            Harness.Rowne("krótka treść mieści się cała", 3, caly.Count);
            Harness.Rowne("bez znacznika, gdy wszystko widać", 0, bezZnacznika);

            // Przewinięcie cofa widok.
            List<WierszKonsoli> cofniete = Grafika.Konsola.Widok(linie, 60, 10, 20, null, false,
                                                                 out int _);
            Harness.Sprawdz("przewinięcie pokazuje wcześniejsze wiersze",
                            cofniete[cofniete.Count - 1].Tekst.Contains("linia 39"));

            // Wpisywany tekst i kursor dochodzą do ostatniej linii.
            var pytanie = new List<string> { "  [1]  północ", "  Twój wybór: " };
            List<WierszKonsoli> zKursorem = Grafika.Konsola.Widok(pytanie, 60, 10, 0, "12", true,
                                                                  out int _);
            Harness.Sprawdz("wpisywany tekst jest widoczny",
                            zKursorem[zKursorem.Count - 1].Tekst.Contains("12"));
            Harness.Sprawdz("kursor jest widoczny",
                            zKursorem[zKursorem.Count - 1].Tekst.Contains("▌"));
            Harness.Sprawdz("opcja jest klikalna, gdy gra pyta",
                            zKursorem.Exists(w => w.Opcja == "1"));

            // Gdy gra nie pyta, nic nie jest klikalne — inaczej klik w stary
            // ekran wysłałby odpowiedź na pytanie, którego nie ma.
            List<WierszKonsoli> bezPytania = Grafika.Konsola.Widok(pytanie, 60, 10, 0, null, false,
                                                                   out int _);
            Harness.Sprawdz("bez pytania nic nie jest klikalne",
                            !bezPytania.Exists(w => w.Opcja != null));
        }

        /// <summary>Czeka na warunek z limitem czasu — test nie może wisieć bez końca.</summary>
        private static bool Poczekaj(Func<bool> warunek)
        {
            var zegar = System.Diagnostics.Stopwatch.StartNew();
            while (zegar.ElapsedMilliseconds < LimitMs)
            {
                if (warunek())
                {
                    return true;
                }
                Thread.Sleep(2);
            }
            return false;
        }
    }
}
