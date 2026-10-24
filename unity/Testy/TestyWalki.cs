using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Sprawdza bestiariusz i rdzeń walki.
    ///
    /// Pierwsza część porównuje losowanych wrogów z wzorcami z Pythona: ten sam
    /// seed ma dać tego samego przeciwnika z tymi samymi liczbami, bo inaczej
    /// ta sama wyprawa byłaby w Unity łatwiejsza albo trudniejsza niż w wersji
    /// pythonowej. Druga część przechodzi walkę na konsoli testowej — bez Unity.
    /// </summary>
    public static class TestyWalki
    {
        public static void Uruchom(JsonElement wzorce)
        {
            Bestiariusz(wzorce);
            Zdolnosci(wzorce);
            Obrazenia();
            PelnaWalka();
        }

        private static void Bestiariusz(JsonElement wzorce)
        {
            Harness.Grupa("Wrogowie: losowanie zgodne z Pythonem");

            bool losowaniOk = true;
            string pierwszyBlad = null;
            foreach (JsonElement w in wzorce.GetProperty("losowani").EnumerateArray())
            {
                var rng = new Losowanie(w.GetProperty("seed").GetInt32());
                Przeciwnik p = Wrogowie.LosujPrzeciwnika(
                    rng,
                    w.GetProperty("biom").GetString(),
                    w.GetProperty("mapa_gen").GetInt32(),
                    w.GetProperty("tryb").GetString());

                bool ok = p.Nazwa == w.GetProperty("nazwa").GetString()
                          && p.Poziom == w.GetProperty("poziom").GetInt32()
                          && p.MaxHp == w.GetProperty("max_hp").GetInt32()
                          && p.Atak == w.GetProperty("atak").GetInt32()
                          && p.Obrona == w.GetProperty("obrona").GetInt32()
                          && p.ExpNagroda == w.GetProperty("exp").GetInt32()
                          && p.LosoweZloto(rng) == w.GetProperty("zloto").GetInt32();
                if (!ok && pierwszyBlad == null)
                {
                    pierwszyBlad = $"seed {w.GetProperty("seed").GetInt32()}, "
                                 + $"region {w.GetProperty("mapa_gen").GetInt32()}: "
                                 + $"oczekiwano {w.GetProperty("nazwa").GetString()} "
                                 + $"{w.GetProperty("max_hp").GetInt32()} HP, "
                                 + $"jest {p.Nazwa} {p.MaxHp} HP";
                }

                losowaniOk &= ok;
            }

            Harness.Sprawdz(pierwszyBlad == null
                ? "losowani przeciwnicy (18 kombinacji seeda, regionu i trybu)"
                : $"losowani przeciwnicy — {pierwszyBlad}", losowaniOk);

            bool bossyOk = wzorce.GetProperty("bossowie").EnumerateArray().All(b =>
            {
                var rng = new Losowanie(b.GetProperty("seed").GetInt32());
                Przeciwnik p = Wrogowie.LosujBossa(rng, b.GetProperty("mapa_gen").GetInt32());
                return p.Nazwa == b.GetProperty("nazwa").GetString()
                       && p.Poziom == b.GetProperty("poziom").GetInt32()
                       && p.MaxHp == b.GetProperty("max_hp").GetInt32()
                       && p.Atak == b.GetProperty("atak").GetInt32()
                       && p.Obrona == b.GetProperty("obrona").GetInt32()
                       && p.ExpNagroda == b.GetProperty("exp").GetInt32();
            });
            Harness.Sprawdz("bossowie regionów", bossyOk);

            bool mityczniOk = wzorce.GetProperty("mityczni").EnumerateArray().All(m =>
            {
                var rng = new Losowanie(5);
                Przeciwnik p = Wrogowie.LosujMitycznego(m.GetProperty("typ").GetString(), rng, 2);
                return p.Nazwa == m.GetProperty("nazwa").GetString()
                       && p.Poziom == m.GetProperty("poziom").GetInt32()
                       && p.MaxHp == m.GetProperty("max_hp").GetInt32()
                       && p.Atak == m.GetProperty("atak").GetInt32()
                       && p.Obrona == m.GetProperty("obrona").GetInt32();
            });
            Harness.Sprawdz("wrogowie mitycznych lokacji", mityczniOk);

            bool hersztyOk = wzorce.GetProperty("herszty").EnumerateArray().All(h =>
            {
                Przeciwnik p = Wrogowie.HersztNajazdu(h.GetProperty("sila").GetInt32());
                return p.Nazwa == h.GetProperty("nazwa").GetString()
                       && p.Poziom == h.GetProperty("poziom").GetInt32()
                       && p.MaxHp == h.GetProperty("max_hp").GetInt32()
                       && p.Atak == h.GetProperty("atak").GetInt32();
            });
            Harness.Sprawdz("herszci najazdów rosną z siłą najazdu", hersztyOk);
        }

        private static void Zdolnosci(JsonElement wzorce)
        {
            Harness.Grupa("Wrogowie: zdolności wyliczane z nazwy");

            foreach (JsonProperty wpis in wzorce.GetProperty("zdolnosci").EnumerateObject())
            {
                Zdolnosci z = Wrogowie.ZdolnosciWroga(wpis.Name);
                JsonElement d = wpis.Value;

                var oczekiwaneZap = d.GetProperty("zapowiedzi").EnumerateObject()
                    .ToDictionary(x => x.Name, x => x.Value.GetDouble());
                bool zapOk = oczekiwaneZap.Count == z.Zapowiedzi.Count
                             && oczekiwaneZap.All(x => z.Zapowiedzi.ContainsKey(x.Key)
                                                       && Math.Abs(z.Zapowiedzi[x.Key] - x.Value) < 1e-9);

                var oczekiwaneSlab = d.GetProperty("slabosci").EnumerateArray()
                                      .Select(e => e.GetString()).ToList();
                var oczekiwaneOdp = d.GetProperty("odpornosci").EnumerateArray()
                                     .Select(e => e.GetString()).ToList();

                bool ok = zapOk
                          && z.Slabosci.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(oczekiwaneSlab)
                          && z.Odpornosci.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(oczekiwaneOdp)
                          && Math.Abs(z.Regeneracja - d.GetProperty("regeneracja").GetDouble()) < 1e-9;
                Harness.Sprawdz($"zdolności: {wpis.Name}", ok);
            }

            var troll = new Przeciwnik("Trolle", 100, 20, 8, 100, 20, 40);
            Harness.Rowne("troll jest słaby na ogień (×1.5)", 1.5,
                          Wrogowie.MnoznikTypu(troll, "ogien"));
            var szkielet = new Przeciwnik("Szkielet", 55, 11, 4, 45, 8, 18);
            Harness.Rowne("szkielet jest odporny na truciznę (×0.5)", 0.5,
                          Wrogowie.MnoznikTypu(szkielet, "trucizna"));
            Harness.Rowne("szkielet jest słaby na święte (×1.5)", 1.5,
                          Wrogowie.MnoznikTypu(szkielet, "swiete"));
            Harness.Rowne("brak cechy = mnożnik 1", 1.0,
                          Wrogowie.MnoznikTypu(szkielet, "ogien"));
        }

        private static void Obrazenia()
        {
            Harness.Grupa("Walka: obrażenia i ucieczka");

            var rng = new Losowanie(42);
            bool wZakresie = true;
            for (int i = 0; i < 400; i++)
            {
                // atak 30, obrona 10 → baza 20, wariancja 4 → wynik 16..24
                int obr = Walka.ObliczObrazenia(30, 10, rng);
                wZakresie &= obr >= 16 && obr <= 24;
            }

            Harness.Sprawdz("obrażenia trzymają się wariancji ±20%", wZakresie);

            bool zawszeDodatnie = true;
            for (int i = 0; i < 200; i++)
            {
                zawszeDodatnie &= Walka.ObliczObrazenia(3, 50, rng) >= 1;
            }

            Harness.Sprawdz("obrona nie zbija obrażeń poniżej 1", zawszeDodatnie);

            var gracz = new Gracz("Test", "Lotrzyk", new Losowanie(1));
            var stan = new StanWalki();
            double zwykla = Walka.SzansaUcieczki(gracz, stan);
            stan.JestBoss = true;
            double odBossa = Walka.SzansaUcieczki(gracz, stan);
            Harness.Sprawdz("od bossa ucieka się trudniej", odBossa < zwykla);
            Harness.Sprawdz("szansa ucieczki mieści się w 5–90%",
                            zwykla >= 0.05 && zwykla <= 0.90
                            && odBossa >= 0.05 && odBossa <= 0.90);

            var wojownik = new Gracz("Test", "Wojownik", new Losowanie(1));
            Harness.Sprawdz("łotrzyk ucieka lepiej niż wojownik",
                            Walka.SzansaUcieczki(gracz, new StanWalki())
                            > Walka.SzansaUcieczki(wojownik, new StanWalki()));
        }

        private static void PelnaWalka()
        {
            Harness.Grupa("Walka: pełne starcie na konsoli testowej");

            // Pierwsze "" to Enter po zapowiedzi starcia; dalej sam atak,
            // aż któraś strona padnie. Odpowiedzi starczy na długą walkę.
            var odpowiedzi = new List<string> { "" };
            odpowiedzi.AddRange(Enumerable.Repeat(new[] { "1", "" }, 300).SelectMany(x => x));
            var konsola = new KonsolaTestowa(odpowiedzi);
            var gracz = new Gracz("Bohater", "Wojownik", new Losowanie(1));
            var rng = new Losowanie(7);
            var wrog = new Przeciwnik("Goblin", 40, 8, 2, 30, 5, 15, "Mały, zielony i wredny.");

            string wynik = Walka.PrzeprowadzWalke(gracz, konsola, rng, "las", false, wrog);

            Harness.Sprawdz("starcie kończy się rozstrzygnięciem",
                            wynik == "wygrana" || wynik == "przegrana");
            Harness.Sprawdz("walka opisuje ciosy gracza",
                            konsola.Wyjscie.Contains("Atakujesz Goblin"));
            if (wynik == "wygrana")
            {
                Harness.Sprawdz("za wygraną jest złoto", konsola.Wyjscie.Contains("złota!"));
                Harness.Sprawdz("gracz zarobił złoto", gracz.Zloto > 30);
                Harness.Sprawdz("gracz dostał doświadczenie", gracz.Exp > 0 || gracz.Poziom > 1);
                Harness.Sprawdz("pokonany wróg nie żyje", !wrog.Zyje());
            }

            // Ucieczka: wybór [4] z dużą liczbą prób.
            var odpowiedzi2 = new List<string> { "" };
            odpowiedzi2.AddRange(Enumerable.Repeat(new[] { "4", "" }, 300).SelectMany(x => x));
            var konsola2 = new KonsolaTestowa(odpowiedzi2);
            var gracz2 = new Gracz("Zwiadowca", "Lotrzyk", new Losowanie(2));
            var wrog2 = new Przeciwnik("Goblin", 40, 8, 2, 30, 5, 15);
            string wynik2 = Walka.PrzeprowadzWalke(gracz2, konsola2, new Losowanie(3),
                                                   "las", false, wrog2);
            Harness.Sprawdz("ucieczka jest możliwa albo starcie się rozstrzyga",
                            wynik2 == "ucieczka" || wynik2 == "wygrana" || wynik2 == "przegrana");

            // Boss: szał po zejściu poniżej połowy życia.
            var stan = new StanWalki { JestBoss = true };
            var boss = new Przeciwnik("Smok Cienia", 200, 40, 10, 500, 80, 150);
            int atakPrzed = boss.Atak;
            boss.Hp = 90;
            var konsola3 = new KonsolaTestowa(new List<string>());
            typeof(Walka).GetMethod("SzalBossa",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { boss, stan, konsola3 });
            Harness.Sprawdz("boss poniżej połowy HP wpada w szał (+25% ataku)",
                            boss.Atak == (int)(atakPrzed * 1.25) && boss.SzalUzyty);
        }
    }
}
