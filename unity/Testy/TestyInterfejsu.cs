using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Testy interfejsu fantasy: czy tekst konsoli rozkłada się na właściwe
    /// części (tytuł, opcje, kierunki ruchu, Enter, pole tekstowe) i czy
    /// złożona klatka ma przyciski dla każdej opcji.
    ///
    /// Ekrany biorą się z prawdziwej gry przepuszczonej przez konsolę testową —
    /// zmiana tekstów w logice, która rozbiłaby układ, wyjdzie tutaj.
    /// </summary>
    public static class TestyInterfejsu
    {
        public static void Uruchom()
        {
            SprawdzEkranEksploracji();
            SprawdzEkranTytulowy();
            SprawdzPytaniaOTekstIEnter();
            SprawdzKlatkeFantasy();
        }

        private static (List<string> Linie, bool NaEnter) Ekran(params string[] odpowiedzi)
        {
            var konsola = new KonsolaTestowa(odpowiedzi);
            new Logika.Gra(konsola, new Ekran(), new Losowanie(2024)).Uruchom();
            var linie = new List<string>(konsola.Ekran.TrimEnd('\n').Split('\n'));
            string zacheta = konsola.Pytania.Count > 0 ? konsola.Pytania[konsola.Pytania.Count - 1] : "";
            return (linie, zacheta.IndexOf("Enter", StringComparison.Ordinal) >= 0);
        }

        private static readonly string[] DoEksploracji = { "1", "Halina", "4", "", "1", "" };

        private static void SprawdzEkranEksploracji()
        {
            Harness.Grupa("Interfejs: ekran eksploracji rozłożony na części");
            (List<string> linie, bool naEnter) = Ekran(DoEksploracji);
            EkranGry e = EkranGry.Z(linie, true, naEnter);
            Harness.Rowne("tytuł bez emoji", "EKSPLORACJA", e.Tytul);
            Harness.Sprawdz("nie czeka na Enter", !e.CzekaNaEnter);
            Harness.Sprawdz("nie czeka na tekst", !e.CzekaNaTekst);
            Harness.Sprawdz("ma kierunki ruchu", e.MaKierunki);

            var kierunki = e.Opcje.Where(o => o.Kierunek.HasValue).ToDictionary(o => o.Tekst, o => o.Kierunek.Value);
            Harness.Rowne("cztery kierunki", 4, kierunki.Count);
            Harness.Sprawdz("północ to y−1", kierunki.TryGetValue("Północ", out var n) && n == (0, -1));
            Harness.Sprawdz("wschód to x+1", kierunki.TryGetValue("Wschód", out var w) && w == (1, 0));
            Harness.Sprawdz("klawisze ruchu 1–4",
                            e.Opcje.Where(o => o.Kierunek.HasValue).Select(o => o.Klawisz)
                             .SequenceEqual(new[] { "1", "2", "3", "4" }));
            Harness.Sprawdz("cel nieznanego pola to ???", e.Opcje.First(o => o.Klawisz == "1").Cel == "???");
            Harness.Sprawdz("opcja 0 bez kierunku", e.Opcje.Any(o => o.Klawisz == "0" && !o.Kierunek.HasValue));
            Harness.Sprawdz("linia surowców nie trafia do treści (pokazują ją ikony)",
                            !e.Tresc.Any(l => l.StartsWith("Surowce:")));
            Harness.Sprawdz("w tekstach nie ma emoji",
                            e.Opcje.All(o => o.Tekst.All(c => !char.IsSurrogate(c))));
        }

        private static void SprawdzEkranTytulowy()
        {
            Harness.Grupa("Interfejs: ekran tytułowy i wybór klasy");
            (List<string> linie, bool naEnter) = Ekran();
            EkranGry e = EkranGry.Z(linie, true, naEnter);
            Harness.Sprawdz("logo rozpoznane", e.MaLogo);
            Harness.Sprawdz("logo nie trafia do treści", !e.Tresc.Any(l => l.Contains("╗") || l.Contains("╚")));
            Harness.RowneCiagi("opcje: nowa gra i wyjście", new[] { "1", "0" }, e.Opcje.Select(o => o.Klawisz));

            (linie, naEnter) = Ekran("1", "Halina");
            e = EkranGry.Z(linie, true, naEnter);
            Harness.Rowne("tytuł wyboru klasy", "WYBÓR KLASY", e.Tytul);
            Harness.Rowne("pięć klas", 5, e.Opcje.Count);
            Harness.Sprawdz("nazwa klasy oddzielona od opisu myślnikiem",
                            e.Opcje[0].Tekst.StartsWith("Wojownik — "));
        }

        private static void SprawdzPytaniaOTekstIEnter()
        {
            Harness.Grupa("Interfejs: pytanie o imię i ekrany z Enterem");
            (List<string> linie, bool naEnter) = Ekran("1");
            EkranGry e = EkranGry.Z(linie, true, naEnter);
            Harness.Sprawdz("pytanie o imię czeka na tekst", e.CzekaNaTekst);
            Harness.Rowne("zachęta bez spacji i emoji", "Imię bohatera:", e.Zacheta);

            (linie, naEnter) = Ekran("1", "Halina", "4");
            e = EkranGry.Z(linie, true, naEnter);
            Harness.Sprawdz("karta postaci czeka na Enter", e.CzekaNaEnter);
            Harness.Sprawdz("pasek HP z „█” to treść, nie logo", e.Tresc.Any(l => l.StartsWith("HP:")));
            Harness.Sprawdz("komunikat o Enterze nie trafia do treści",
                            !e.Tresc.Any(l => l.StartsWith("[Naciśnij")));
        }

        private static void SprawdzKlatkeFantasy()
        {
            Harness.Grupa("Interfejs: klatka w układzie fantasy");
            Styl poprzedni = Styl.Aktywny;
            try
            {
                Styl.Aktywny = Styl.Mroczny;
                var gracz = new Gracz("Halina", "Druid", new Losowanie(2024)) { Seed = 7 };
                Mapa.ZapewnijMape(gracz);
                Migawka m = new Ekran { Gracz = gracz }.Migawke();
                (List<string> linie, bool naEnter) = Ekran(DoEksploracji);
                var pulpit = new Pulpit();
                Harness.Sprawdz("styl mroczny układa ekran sam", pulpit.UkladFantasy);
                var konsola = new StanKonsoli { Linie = linie, Pyta = true, CzekaNaEnter = naEnter, Wpisywane = "" };
                List<Napis> napisy = pulpit.Zloz(m.Pola, 0.5, m.Hud, m.Region, m.GraczXy, m.Klasa, false,
                                                 m.Osada, konsola);

                var klawisze = pulpit.Przyciski.Select(p => p.Klawisz).ToList();
                foreach (string k in new[] { "1", "2", "3", "4", "0" })
                {
                    Harness.Sprawdz($"przycisk opcji {k}", klawisze.Contains(k));
                }
                Harness.Sprawdz("przyciski mieszczą się w pulpicie",
                                pulpit.Przyciski.All(p => p.X >= 0 && p.Y >= 0
                                                          && p.X + p.Szer <= Pulpit.Szerokosc
                                                          && p.Y + p.Wys <= Pulpit.Wysokosc));
                Harness.Sprawdz("napis strzałki północy", napisy.Any(n => n.Tekst.Contains("Północ")));
                Harness.Sprawdz("napisy krojem pikselowym", napisy.All(n => n.Krój == "piks"));
                Harness.Sprawdz("liczby HP na kuli", napisy.Any(n => n.Tekst == $"{m.Hud.Hp}/{m.Hud.MaxHp}"));

                byte[] d = pulpit.Klatka.Dane;
                int jasnych = 0;
                for (int i = 0; i < d.Length; i += 4 * 97)
                {
                    if (d[i] + d[i + 1] + d[i + 2] > 120)
                    {
                        jasnych++;
                    }
                }
                Harness.Sprawdz("klatka nie jest pusta", jasnych > 500);

                // Przycisk pod kursorem jaśnieje, a klik w niego wysyła jego klawisz.
                Przycisk p1 = pulpit.Przyciski.First(p => p.Klawisz == "1");
                byte[] przed = (byte[])d.Clone();
                pulpit.Zloz(m.Pola, 0.5, m.Hud, m.Region, m.GraczXy, m.Klasa, false, m.Osada, konsola,
                            (p1.X + 4, p1.Y + 4));
                int idx = ((p1.Y + 6) * Pulpit.Szerokosc + p1.X + 6) * 4;
                Harness.Sprawdz("plakietka pod kursorem jaśnieje",
                                pulpit.Klatka.Dane[idx] + pulpit.Klatka.Dane[idx + 1]
                                > przed[idx] + przed[idx + 1]);

                Styl.Aktywny = Styl.Klasyczny;
                var klasyczny = new Pulpit();
                klasyczny.Zloz(m.Pola, 0.5, m.Hud, m.Region, m.GraczXy, m.Klasa, false, m.Osada);
                Harness.Sprawdz("układ klasyczny nie ma przycisków pulpitu", klasyczny.Przyciski.Count == 0);
            }
            finally
            {
                Styl.Aktywny = poprzedni;
            }
        }
    }
}
