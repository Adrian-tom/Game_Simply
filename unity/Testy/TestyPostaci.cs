using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Porównuje liczby postaci, kalendarza i karmy z wzorcami z Pythona,
    /// a potem przechodzi całą pętlę gry na konsoli testowej — bez Unity.
    /// </summary>
    public static class TestyPostaci
    {
        public static void Uruchom(JsonElement wzorce)
        {
            Harness.Grupa("Postać: progi EXP, start klas i awanse");

            var progi = wzorce.GetProperty("progi_exp").EnumerateArray()
                              .Select(e => e.GetInt32()).ToList();
            bool progiOk = progi.Select((oczekiwane, poziom) => Gracz.ProgExp(poziom) == oczekiwane)
                                .All(x => x);
            Harness.Sprawdz($"progi EXP dla poziomów 0–{progi.Count - 1}", progiOk);

            foreach (JsonProperty wpis in wzorce.GetProperty("start").EnumerateObject())
            {
                string klasa = wpis.Name;
                JsonElement d = wpis.Value;
                var g = new Gracz("Test", klasa, new Losowanie(1));
                bool ok = g.MaxHp == d.GetProperty("max_hp").GetInt32()
                          && g.Atak == d.GetProperty("atak").GetInt32()
                          && g.Obrona == d.GetProperty("obrona").GetInt32()
                          && g.MaxMana == d.GetProperty("max_mana").GetInt32()
                          && g.Mikstury == d.GetProperty("mikstury").GetInt32()
                          && g.MiksturyMany == d.GetProperty("mikstury_many").GetInt32()
                          && g.Zloto == d.GetProperty("zloto").GetInt32();
                Harness.Sprawdz($"{klasa}: statystyki startowe", ok);

                var atrybutyOczekiwane = d.GetProperty("atrybuty").EnumerateArray()
                                          .Select(e => e.GetInt32()).ToList();
                var atrybutyOtrzymane = Atrybuty.Kolejnosc.Select(k => Atrybuty.Wartosc(g, k)).ToList();
                Harness.Sprawdz($"{klasa}: szóstka atrybutów",
                                atrybutyOczekiwane.SequenceEqual(atrybutyOtrzymane));

                var biegleOczekiwane = d.GetProperty("biegle").EnumerateArray()
                                        .Select(e => e.GetString()).ToList();
                Harness.RowneCiagi($"{klasa}: biegłości", biegleOczekiwane, g.BiegleSkille);
            }

            foreach (JsonProperty wpis in wzorce.GetProperty("awanse").EnumerateObject())
            {
                string klasa = wpis.Name;
                var g = new Gracz("Test", klasa, new Losowanie(1));
                bool ok = true;
                foreach (JsonElement krok in wpis.Value.EnumerateArray())
                {
                    g.Exp = g.ExpDoAwansu();
                    g.Awansuj();
                    ok &= g.Poziom == krok.GetProperty("poziom").GetInt32()
                          && g.MaxHp == krok.GetProperty("max_hp").GetInt32()
                          && g.Atak == krok.GetProperty("atak").GetInt32()
                          && g.Obrona == krok.GetProperty("obrona").GetInt32()
                          && g.MaxMana == krok.GetProperty("max_mana").GetInt32()
                          && g.PunktyAtrybutow == krok.GetProperty("pkt_atr").GetInt32()
                          && g.PunktyUmiejetnosci == krok.GetProperty("pkt_um").GetInt32()
                          && g.PunktyTalentow == krok.GetProperty("pkt_tal").GetInt32();
                }
                Harness.Sprawdz($"{klasa}: dwanaście awansów (HP, atak, obrona, punkty)", ok);
            }

            Harness.Grupa("Kalendarz i karma");

            foreach (JsonElement d in wzorce.GetProperty("kalendarz").EnumerateArray())
            {
                int dzien = d.GetProperty("dzien").GetInt32();
                bool ok = Kalendarz.PoraDnia(dzien).Klucz == d.GetProperty("pora").GetString()
                          && Kalendarz.Rok(dzien) == d.GetProperty("rok").GetInt32()
                          && Kalendarz.DzienPory(dzien) == d.GetProperty("dzien_pory").GetInt32()
                          && Kalendarz.DniDoZimy(dzien) == d.GetProperty("do_zimy").GetInt32();
                Harness.Sprawdz($"dzień {dzien}: pora, rok, dzień pory, dni do zimy", ok);
            }

            foreach (JsonElement d in wzorce.GetProperty("karma").EnumerateArray())
            {
                var g = new Gracz("Test", "Wojownik", new Losowanie(1))
                {
                    Karma = d.GetProperty("karma").GetInt32(),
                };
                bool ok = Karma.Poziom(g).Klucz == d.GetProperty("poziom").GetString()
                          && Karma.ModyfikatorRekrutacji(g) == d.GetProperty("rekrutacja").GetInt32()
                          && Karma.BonusSwiatyni(g) == d.GetProperty("swiatynia").GetInt32();
                Harness.Sprawdz($"karma {g.Karma}: poziom, rekrutacja, świątynia", ok);
                Harness.BliskoSiebie($"karma {g.Karma}: mnożnik cen",
                                     d.GetProperty("mnoznik_cen").GetDouble(),
                                     Karma.MnoznikCen(g), 1e-12);
            }

            SprawdzModyfikatory();
            SprawdzPrzetrwanie();
            SprawdzPetleGry();
        }

        /// <summary>
        /// Premia D&amp;D dzieli w dół, więc atrybut 9 daje −1, a nie 0.
        /// To miejsce, w którym C# najłatwiej rozjechałby się z Pythonem.
        /// </summary>
        private static void SprawdzModyfikatory()
        {
            Harness.Grupa("Premie atrybutów (dzielenie w dół jak w Pythonie)");

            var oczekiwane = new Dictionary<int, int>
            {
                { 8, -1 }, { 9, -1 }, { 10, 0 }, { 11, 0 }, { 12, 1 },
                { 13, 1 }, { 16, 3 }, { 20, 5 }, { 1, -5 }, { 7, -2 },
            };
            foreach (KeyValuePair<int, int> para in oczekiwane)
            {
                var g = new Gracz("Test", "Wojownik", new Losowanie(1));
                g.Atrybuty["sila"] = para.Key;
                Harness.Rowne($"siła {para.Key} → premia", para.Value, Atrybuty.Modyfikator(g, "sila"));
            }

            var w = new Gracz("Test", "Wojownik", new Losowanie(1));
            Harness.Rowne("biegłość na 1. poziomie", 2, Atrybuty.Bieglosc(w));
            w.Poziom = 5;
            Harness.Rowne("biegłość na 5. poziomie", 3, Atrybuty.Bieglosc(w));
            w.Poziom = 9;
            Harness.Rowne("biegłość na 9. poziomie", 4, Atrybuty.Bieglosc(w));
            Harness.Sprawdz("wojownik jest biegły w atletyce", Atrybuty.CzyBiegly(w, "atletyka"));
            Harness.Sprawdz("wojownik nie jest biegły w oszustwie",
                            !Atrybuty.CzyBiegly(w, "oszustwo"));
        }

        private static void SprawdzPrzetrwanie()
        {
            Harness.Grupa("Przetrwanie: rany, głód, prowiant");

            var g = new Gracz("Test", "Wojownik", new Losowanie(1));
            Harness.Rowne("na starcie zdrowy", "zdrowy", Przetrwanie.OpisStanu(g));
            Harness.BliskoSiebie("bez ran mnożnik ataku = 1", 1.0, Przetrwanie.MnoznikAtaku(g));

            Przetrwanie.DodajRane(g, "zlamana_reka");
            Harness.Sprawdz("rana jest zapamiętana", Przetrwanie.MaRane(g, "zlamana_reka"));
            Harness.BliskoSiebie("złamana ręka tnie atak o 20%", 0.8, Przetrwanie.MnoznikAtaku(g));
            Przetrwanie.DodajRane(g, "wstrzas");
            Harness.Rowne("wstrząs daje −3 do testów Mądrości", -3,
                          Przetrwanie.KaraTestu(g, "madrosc"));
            Harness.Rowne("wstrząs nie rusza Siły", 0, Przetrwanie.KaraTestu(g, "sila"));

            // Gojenie: 3 dni leczą wstrząs (3 dni), ale nie złamaną rękę (6 dni).
            List<string> wiesci = Przetrwanie.LeczRany(g, 3.0);
            Harness.Rowne("po trzech dniach goi się jedna rana", 1, wiesci.Count);
            Harness.Sprawdz("wstrząs minął", !Przetrwanie.MaRane(g, "wstrzas"));
            Harness.Sprawdz("złamana ręka została", Przetrwanie.MaRane(g, "zlamana_reka"));

            // Prowiant: pakujemy z magazynu, pojemność bez stajni to 8.
            var h = new Gracz("Test", "Druid", new Losowanie(1));
            Harness.Rowne("pojemność prowiantu bez stajni", 8, Przetrwanie.PojemnoscProwiantu(h));
            Przetrwanie.SpakujProwiant(h);
            Harness.Rowne("plecak pełny po spakowaniu", 8, h.Prowiant);
            Harness.Rowne("magazyn uszczuplony o spakowane racje", 22, h.Surowce.Wez("zywnosc"));
            Przetrwanie.RozpakujProwiant(h);
            Harness.Rowne("rozpakowanie wraca do magazynu", 30, h.Surowce.Wez("zywnosc"));

            // Głód: bez prowiantu dzień wyprawy zabiera HP i rośnie licznik.
            var i = new Gracz("Test", "Wojownik", new Losowanie(1));
            i.WObozie = false;
            i.Prowiant = 0;
            int przed = i.Hp;
            Przetrwanie.DzienWyprawy(i, new Losowanie(7));
            Harness.Rowne("pierwszy dzień bez jedzenia = 1 dzień głodu", 1, i.Glod);
            Harness.Rowne("głód zabiera 3 HP", przed - 3, i.Hp);
            Harness.BliskoSiebie("głód tnie atak o 10%", 0.9, Przetrwanie.MnoznikAtaku(i));
        }

        /// <summary>
        /// Przejście całej pętli gry na konsoli testowej: menu, tworzenie
        /// postaci, wyprawa, ruch, zbieractwo i powrót do obozu. Dowodzi, że
        /// logika da się przejść od początku do końca bez Unity.
        /// </summary>
        private static void SprawdzPetleGry()
        {
            Harness.Grupa("Pełna pętla gry na konsoli testowej");

            var odpowiedzi = new List<string>
            {
                "1",            // nowa gra
                "Halina",       // imię
                "1",            // Wojownik
                "",             // Enter po karcie postaci
                "1",            // wyrusz na wyprawę
                "",             // Enter po spakowaniu prowiantu
                "1", "",        // na północ + Enter po komunikacie podróży
                "6", "",        // zbierz surowce + Enter
                "5", "",        // rozglądnij się + Enter
                "0", "",        // wróć do obozu + Enter
                "2", "",        // karta postaci + Enter
                "0",            // zakończ grę
                "0",            // wyjście z menu głównego
            };
            var konsola = new KonsolaTestowa(odpowiedzi);
            var ekran = new Ekran();
            var gra = new Gra(konsola, ekran, new Losowanie(2024));
            gra.Uruchom();

            string wyjscie = konsola.Wyjscie;
            Harness.Sprawdz("pętla doszła do końca bez wyjątku i bez pytań w zapasie",
                            konsola.ZostaloOdpowiedzi == 0);
            Harness.Sprawdz("ekran tytułowy się pokazał", wyjscie.Contains("Nowa gra"));
            Harness.Sprawdz("postać powstała z podanym imieniem",
                            wyjscie.Contains("Halina rusza w świat jako Wojownik"));
            Harness.Sprawdz("wyprawa spakowała prowiant", wyjscie.Contains("Prowiant:"));
            Harness.Sprawdz("menu eksploracji się narysowało", wyjscie.Contains("EKSPLORACJA"));
            Harness.Sprawdz("nagłówek regionu pokazuje współrzędne", wyjscie.Contains("REGION [0, 0]"));
            Harness.Sprawdz("ruch na północ dał komunikat podróży", wyjscie.Contains("Idziesz na północ"));
            Harness.Sprawdz("zbieractwo przeszukało teren", wyjscie.Contains("Przeszukujesz"));
            Harness.Sprawdz("zwiad rzucił k20", wyjscie.Contains("k20:"));
            Harness.Sprawdz("powrót do obozu się udał", wyjscie.Contains("POWRÓT DO OBOZU"));
            Harness.Sprawdz("karta postaci pokazała atrybuty", wyjscie.Contains("Atrybuty:"));
            Harness.Sprawdz("gra pożegnała gracza", wyjscie.Contains("Do zobaczenia przy ogniu"));

            Harness.Sprawdz("ekran dostał postać do rysowania w trakcie gry",
                            konsola.Pytania.Count > 10);

            // Po zamknięciu gry ekran nie trzyma już postaci — widok wraca do tytułu.
            Harness.Sprawdz("ekran zwolnił postać po wyjściu", ekran.Gracz == null);
            Harness.Sprawdz("skróty ruchu są wyłączone po wyjściu", !ekran.SkrotyAktywne);
        }
    }
}
