using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GraSimply.Grafika;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Renderuje klatki pulpitu do plików, żeby dało się zobaczyć grafikę portu
    /// bez odpalania Unity.
    ///
    /// Zapisujemy surowe bajty RGBA plus plik z napisami HUD — tekst rysuje
    /// w grze Unity, więc tutaj może go tylko wypisać. Zamiana na PNG idzie
    /// przez <c>unity/narzedzia/podglad.py</c>, które ma pygame.
    ///
    /// Uruchomienie:
    ///     dotnet run -- --podglad ../podglad [klasyczny|mroczny]
    /// </summary>
    public static class Podglad
    {
        public static int Zapisz(string katalog)
        {
            Directory.CreateDirectory(katalog);
            // Odpowiedzi prowadzą prawdziwą grę do ekranu, który klatka pokazuje.
            var doEksploracji = new List<string> { "1", "Halina", "4", "", "1", "", "1", "" };
            var klatki = new List<(string Nazwa, bool Zmierzch, int Chaty, int Palisada,
                                   int Wieza, double Czas, bool Tytul, List<string> Odpowiedzi)>
            {
                ("tytul", false, 0, 0, 0, 0.0, true, new List<string>()),
                ("dzien", false, 2, 0, 0, 0.6, false, doEksploracji),
                ("zmierzch", true, 2, 0, 0, 1.4, false, doEksploracji),
                ("osada-rosnie", false, 6, 2, 1, 0.9, false, doEksploracji),
            };
            if (Styl.Aktywny.UkladFantasy)
            {
                // Układ fantasy pokazuje każdy rodzaj ekranu inaczej — podglądamy wszystkie.
                klatki.Add(("imie", false, 0, 0, 0, 0.3, true, new List<string> { "1" }));
                klatki.Add(("klasa", false, 0, 0, 0, 0.3, true, new List<string> { "1", "Halina" }));
                klatki.Add(("karta-postaci", false, 2, 0, 0, 0.3, false, new List<string> { "1", "Halina", "4" }));
                klatki.Add(("oboz", false, 2, 0, 0, 0.5, false, new List<string> { "1", "Halina", "4", "" }));
                klatki.Add(("podroz", false, 2, 0, 0, 0.8, false,
                            new List<string> { "1", "Halina", "4", "", "1", "", "1" }));
            }

            foreach ((string nazwa, bool zmierzch, int chaty, int palisada, int wieza,
                      double czas, bool tytul, List<string> odpowiedzi) in klatki)
            {
                var pulpit = new Pulpit();
                pulpit.Scena.Zmierzch = zmierzch;
                (List<string> linie, bool naEnter) = EkranKonsoli(odpowiedzi);
                var konsola = new StanKonsoli
                {
                    Linie = linie, Pyta = true, CzekaNaEnter = naEnter, Wpisywane = "", Kursor = true,
                };
                List<Napis> napisy;
                if (tytul)
                {
                    Migawka m = Migawka.Tytulowa();
                    napisy = pulpit.Zloz(m.Pola, czas, null, m.Region, null, null, true, null, konsola);
                }
                else
                {
                    Migawka m = Scenka(chaty, palisada, wieza);
                    napisy = pulpit.Zloz(m.Pola, czas, m.Hud, m.Region, m.GraczXy, m.Klasa,
                                         false, m.Osada, konsola);
                }

                if (!pulpit.UkladFantasy)
                {
                    napisy.AddRange(NapisyKonsoli(linie));
                }

                string raw = Path.Combine(katalog, nazwa + ".raw");
                File.WriteAllBytes(raw, pulpit.Klatka.Dane);

                var opis = new StringBuilder();
                // Trzecie pole nagłówka: krój stylu (ścieżka w Resources) albo „-”
                // — podglad.py bierze wtedy ten sam plik co gra.
                opis.AppendLine($"{Pulpit.Szerokosc} {Pulpit.Wysokosc} {Styl.Aktywny.Kroj ?? "-"}");
                foreach (Napis n in napisy)
                {
                    string tekst = Styl.Aktywny.Emoji ? n.Tekst : Grafika.Konsola.BezEmoji(n.Tekst);
                    string alfa = n.Kolor.A < 255 ? $",{n.Kolor.A}" : "";
                    opis.AppendLine($"{n.X}\t{n.Y}\t{n.Krój}\t{n.Kolor.R},{n.Kolor.G}," +
                                    $"{n.Kolor.B}{alfa}\t{tekst}");
                }
                File.WriteAllText(Path.Combine(katalog, nazwa + ".napisy"), opis.ToString(),
                                  new UTF8Encoding(false));
                Console.WriteLine($"  {nazwa}: {pulpit.Klatka.Dane.Length} bajtów, " +
                                  $"{napisy.Count} napisów");
            }

            Console.WriteLine($"Gotowe. Zamień na PNG: python unity/narzedzia/podglad.py {katalog}");
            return 0;
        }

        /// <summary>
        /// Przepuszcza prawdziwą grę przez konsolę testową, aż skończą się
        /// odpowiedzi, i zwraca ostatni ekran: linie (ostatnia to zachęta)
        /// i to, czy gra czeka tylko na Enter.
        /// </summary>
        private static (List<string> Linie, bool NaEnter) EkranKonsoli(List<string> odpowiedzi)
        {
            var konsola = new KonsolaTestowa(odpowiedzi);
            var gra = new Logika.Gra(konsola, new Ekran(), new Losowanie(2024));
            gra.Uruchom(); // kończy się wyjątkiem GraZamknieta, gdy zabraknie odpowiedzi
            var linie = new List<string>(konsola.Ekran.TrimEnd('\n').Split('\n'));
            string zacheta = konsola.Pytania.Count > 0 ? konsola.Pytania[konsola.Pytania.Count - 1] : "";
            return (linie, zacheta.IndexOf("Enter", StringComparison.Ordinal) >= 0);
        }

        /// <summary>
        /// Treść prawej kolumny: prawdziwy tekst gry, nie atrapa.
        ///
        /// Linie ostatniego ekranu gry układamy tak, jak zrobiłby to silnik
        /// w Unity w układzie klasycznym. Dzięki temu podgląd pokazuje, jak
        /// naprawdę komponuje się mapa z konsolą.
        /// </summary>
        private static List<Napis> NapisyKonsoli(List<string> linie)
        {
            (int kx, int ky, int kszer, int _) = Pulpit.ProstokatKonsoli;
            const int wysokoscWiersza = 19;
            int znakow = (kszer - 40) / Styl.Aktywny.SzerokoscZnaku;
            int mieszczSie = (Pulpit.ProstokatKonsoli.Wys - 36) / wysokoscWiersza;

            List<WierszKonsoli> widok = Grafika.Konsola.Widok(linie, znakow, mieszczSie, 0,
                                                              "", true, out int ileWyzej);
            var napisy = new List<Napis>();
            int odstep = ileWyzej > 0 ? 1 : 0;
            if (ileWyzej > 0)
            {
                napisy.Add(new Napis($"▲ wyżej jeszcze {ileWyzej} wierszy (kółko myszy)",
                                     kx + 20, ky + 18, Pulpit.Przygaszony, "maly"));
            }
            for (int i = 0; i < widok.Count; i++)
            {
                WierszKonsoli w = widok[i];
                Barwa kolor = w.Opcja != null ? Pulpit.Zloty
                    : w.Ozdobnik ? Pulpit.Ozdobnik
                    : Pulpit.Tekst;
                napisy.Add(new Napis(w.Tekst, kx + 20, ky + 18 + (i + odstep) * wysokoscWiersza,
                                     kolor, "konsola"));
            }
            return napisy;
        }

        /// <summary>
        /// Postać w świecie przygotowanym pod podgląd: kawałek mapy odkryty,
        /// bohater w obozie, trochę surowców w magazynie.
        /// </summary>
        private static Migawka Scenka(int chaty, int palisada, int wieza)
        {
            var gracz = new Gracz("Halina", "Druid", new Losowanie(2024)) { Seed = 7 };
            Mapa.ZapewnijMape(gracz);
            gracz.Chaty = chaty;
            if (palisada > 0)
            {
                gracz.PoziomyBudynkow["palisada"] = palisada;
            }
            if (wieza > 0)
            {
                gracz.PoziomyBudynkow["wieza"] = wieza;
            }
            gracz.Poziom = 4;
            gracz.Hp = (int)(gracz.MaxHp * 0.62);
            gracz.Mana = (int)(gracz.MaxMana * 0.45);
            gracz.Zloto = 148;
            gracz.Czas = 95; // zima — HUD pokazuje wtedy inną porę roku niż start
            gracz.Surowce["drewno"] = 24;
            gracz.Surowce["kamien"] = 11;
            gracz.Surowce["ziola"] = 7;
            gracz.Surowce["skora"] = 3;

            // Odkrywamy okolicę obozu, żeby na podglądzie była i mapa, i mgła.
            for (int y = 0; y < Mapa.Rozmiar; y++)
            {
                for (int x = 0; x < Mapa.Rozmiar; x++)
                {
                    if (Math.Abs(x - Mapa.Srodek) + Math.Abs(y - Mapa.Srodek) <= 4)
                    {
                        gracz.MapaPola[y][x].Odkryte = true;
                    }
                }
            }

            var ekran = new Ekran { Gracz = gracz };
            return ekran.Migawke();
        }
    }
}
