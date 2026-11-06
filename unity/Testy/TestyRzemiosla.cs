using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Budowa etapami, realne koszty, zespół przy pracy, warsztaty ze zleceniami i narzędzia.</summary>
    public static class TestyRzemiosla
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: budowa etapami i warsztaty");
            Koszty();
            Etapy();
            Zespol();
            Warsztat();
            Narzedzia();
            Zniszczenie();
            Zapis();
            Interfejs();
        }

        private static SwiatWolny Swiat()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true, Godzina = 8.0 };
            s.Plecak["drewno"] = 400;
            s.Plecak["kamien"] = 200;
            s.Plecak["wlokno"] = 100;
            return s;
        }

        private static void Pracuj(SwiatWolny s, double sekund)
        {
            for (double t = 0; t < sekund; t += 0.05)
            {
                s.Glod = s.Energia = s.Hp = 100;
                s.Godzina = 8.0;
                s.Aktualizuj(0.05, new Wejscie { Dzialaj = true });
            }
        }

        private static (int X, int Y) Miejsce(SwiatWolny s)
        {
            for (int x = 1; x <= 2; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (s.PowodBudowy("namiot", x, y) == null)
                    {
                        return (x, y);
                    }
                }
            }
            throw new InvalidOperationException("brak miejsca");
        }

        private static void Koszty()
        {
            int Wart(KeyValuePair<string, int>[] m, string k) => m.Where(p => p.Key == k).Sum(p => p.Value);
            KeyValuePair<string, int>[] namiot = SwiatWolny.KosztCalkowity("namiot");
            Harness.Rowne("namiot to 30 drewna", 30, Wart(namiot, "drewno"));
            Harness.Rowne("i 20 włókna", 20, Wart(namiot, "wlokno"));
            KeyValuePair<string, int>[] chata = SwiatWolny.KosztCalkowity("chata");
            Harness.Rowne("chata to 40 kamieni", 40, Wart(chata, "kamien"));
            double drewnoNaChate = Wart(chata, "belka") * 4.0 + Wart(chata, "deska") * 1.5;
            Harness.Sprawdz($"chata to około 150 drewna po przetworzeniu ({drewnoNaChate})", drewnoNaChate > 130 && drewnoNaChate < 170);
            double czas = SwiatWolny.Etapy["chata"].Sum(e => e.Praca);
            Harness.Sprawdz($"sam budujesz chatę około pół doby ({czas} s)", czas >= 200 && czas <= 280);
            Harness.Sprawdz("ognisko dalej jest tanie i od razu", !SwiatWolny.Etapy.ContainsKey("ognisko"));

            SwiatWolny s = Swiat();
            Harness.Sprawdz("plac można założyć bez materiałów", new SwiatWolny(42) { BudowaZEtapami = true }.PowodBudowy("namiot", 2, 0) == null);
            s.Plecak["spiwor"] = 1;
            (int x, int y) = Miejsce(s);
            Harness.Sprawdz("śpiwór stawia się od razu", s.PostawNa("spiwor", x, y) && s.Budowle[(x, y)] == "spiwor" && s.Place.Count == 0);

            var stary = new SwiatWolny(42);
            stary.Plecak["drewno"] = 20;
            stary.Plecak["wlokno"] = 20;
            Harness.Sprawdz("bez etapów namiot staje od razu jak dawniej", stary.PostawNa("namiot", 2, 0) && stary.Budowle[(2, 0)] == "namiot");
        }

        private static void Etapy()
        {
            SwiatWolny s = Swiat();
            (int x, int y) = Miejsce(s);
            Harness.Sprawdz("namiot zaczyna się od placu", s.PostawNa("namiot", x, y));
            Harness.Rowne("na mapie jest plac, a nie namiot", SwiatWolny.PrzedrostekPlacu + "namiot", s.Budowle[(x, y)]);
            Harness.Rowne("materiały jeszcze nie zeszły", 400, s.Ile("drewno"));
            Harness.Sprawdz("plac blokuje przejście", s.Zajety(x, y));
            Harness.Sprawdz("plac nie daje miejsc do spania", s.Obozowisko.Chaty == 0 && s.MiejsceSnu() == null);

            Pracuj(s, 1.0);
            Harness.Rowne("pierwszy etap pobiera swoje", 370, s.Ile("drewno"));
            Harness.Rowne("włókna jeszcze nie", 100, s.Ile("wlokno"));
            Harness.Sprawdz("pracę widać na pasku", s.PostepPracy.HasValue && s.PostepPracy.Value.Postep > 0);
            Harness.Sprawdz("i bohater pracuje", s.BohaterPracuje);

            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("bez E nie ma postępu", !s.BohaterPracuje);

            Pracuj(s, 30.0);
            Harness.Rowne("po szkielecie drugi etap", 1, s.Place[(x, y)].Etap);
            Pracuj(s, 1.0);
            Harness.Rowne("drugi etap pobiera włókno", 80, s.Ile("wlokno"));
            Harness.Sprawdz("namiot jeszcze nie stoi", s.Budowle[(x, y)].StartsWith(SwiatWolny.PrzedrostekPlacu));
            Pracuj(s, 20.0);
            Harness.Rowne("po ostatnim etapie stoi namiot", "namiot", s.Budowle[(x, y)]);
            Harness.Rowne("plac znika", 0, s.Place.Count);
            Harness.Rowne("namiot daje miejsca", SwiatWolny.MiejscaNamiotu, s.Obozowisko.Chaty);

            // Brak materiału zatrzymuje etap.
            SwiatWolny b = Swiat();
            b.Plecak["drewno"] = 5;
            (x, y) = Miejsce(b);
            b.PostawNa("namiot", x, y);
            Pracuj(b, 2.0);
            Harness.Rowne("bez materiałów etap stoi", 0.0, b.Place[(x, y)].Praca);
            Harness.Rowne("i nic nie pobiera", 5, b.Ile("drewno"));
            Harness.Sprawdz("podpowiedź mówi, czego brakuje", b.Brak != null && b.Brak.Contains("25"));
            Harness.Sprawdz("widać to w podpowiedzi", WidokWolny.Podpowiedz(b).Contains("Brakuje"));
            b.Plecak["drewno"] = 30;
            Pracuj(b, 1.0);
            Harness.Sprawdz("po donieniu praca rusza", b.Place[(x, y)].Praca > 0);

            // Budowę bierze przed zbieraniem i nie można jednocześnie iść.
            SwiatWolny r = Swiat();
            (x, y) = Miejsce(r);
            r.PostawNa("namiot", x, y);
            Pracuj(r, 0.5);
            Harness.Sprawdz("przy placu E buduje, a nie zbiera", r.Zbieranie == null);
        }

        private static void Zespol()
        {
            Harness.BliskoSiebie("jedna osoba to pełna siła", 1.0, SwiatWolny.SilaZespolu(new[] { 1.0 }), 1e-9);
            double dwie = SwiatWolny.SilaZespolu(new[] { 1.0, 1.0 });
            double trzy = SwiatWolny.SilaZespolu(new[] { 1.0, 1.0, 1.0 });
            double piec = SwiatWolny.SilaZespolu(new[] { 1.0, 1.0, 1.0, 1.0, 1.0 });
            Harness.Sprawdz($"dwie szybciej niż jedna, ale nie dwa razy ({dwie})", dwie > 1.5 && dwie < 2.0);
            Harness.Sprawdz($"trzecia dodaje mniej ({trzy})", trzy - dwie < dwie - 1.0);
            Harness.Sprawdz($"tłum ma granice ({piec})", piec < 3.5);

            double Czas(int budowniczych, bool bohater)
            {
                SwiatWolny s = Swiat();
                (int x, int y) = Miejsce(s);
                s.PostawNa("palisada", x, y);
                var r = new Losowanie(3);
                s.Obozowisko.Chaty = 8;
                for (int i = 0; i < budowniczych; i++)
                {
                    Osada.ZatrudnijOsadnika(s.Obozowisko, r, "bezczynny", true);
                    Osadnik o = s.Obozowisko.Osadnicy[i];
                    o.Zajecie = "budowniczy";
                    s.Postacie.Add(new PostacOsadnika { O = o, X = x + 0.5 + 0.9 * (i + 1), Y = y + 1.5, CelX = x, CelY = y });
                }
                if (!bohater)
                {
                    s.X = 20.5;
                }
                double t = 0;
                while (s.Place.Count > 0 && t < 60)
                {
                    s.Godzina = 8.0;
                    s.Glod = s.Energia = s.Hp = 100;
                    s.Aktualizuj(0.05, new Wejscie { Dzialaj = bohater });
                    t += 0.05;
                }
                return t;
            }

            double sam = Czas(0, true), we2 = Czas(2, true), bezBohatera = Czas(1, false);
            Harness.Sprawdz($"sam bohater buduje cały czas pracy ({sam:F1} s)", sam > 5 && sam < 7);
            Harness.Sprawdz($"z dwoma osadnikami szybciej ({we2:F1} s)", we2 < sam * 0.7);
            Harness.Sprawdz($"budowniczy pracuje też bez bohatera ({bezBohatera:F1} s)", bezBohatera < 60);

            SwiatWolny n = Swiat();
            (int nx, int ny) = Miejsce(n);
            n.PostawNa("palisada", nx, ny);
            n.Obozowisko.Chaty = 2;
            Osada.ZatrudnijOsadnika(n.Obozowisko, new Losowanie(3), "bezczynny", true);
            n.Obozowisko.Osadnicy[0].Zajecie = "drwal";
            n.Postacie.Add(new PostacOsadnika { O = n.Obozowisko.Osadnicy[0], X = nx + 1.5, Y = ny + 1.5 });
            n.X = 20.5;
            for (int i = 0; i < 40; i++)
            {
                n.Aktualizuj(0.05, default);
            }
            Harness.Rowne("drwal nie buduje", 0.0, n.Place[(nx, ny)].Praca);
        }

        private static SwiatWolny ZTartakiem()
        {
            SwiatWolny s = Swiat();
            s.Budowle[(2, 0)] = "tartak";
            s.Budowle[(2, 2)] = "warsztat";
            return s;
        }

        private static void Warsztat()
        {
            SwiatWolny s = Swiat();
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:deska" });
            Harness.Sprawdz("bez tartaku nie ma zlecenia", s.WKolejce("deska") == 0 && s.Dziennik.Last().Contains("tartak"));

            s = ZTartakiem();
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:deska" });
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:belka" });
            Harness.Rowne("zlecenie w kolejce", 1, s.WKolejce("deska"));
            Harness.Rowne("materiał schodzi dopiero przy pracy", 400, s.Ile("drewno"));
            for (int i = 0; i < 20; i++)
            {
                s.Aktualizuj(0.05, default);
            }
            Harness.Rowne("bez pracowników nic się nie robi", 400, s.Ile("drewno"));
            Pracuj(s, 6.5);
            Harness.Rowne("bohater przy tartaku robi deski", 2, s.Wyroby.Wez("deska"));
            Harness.Sprawdz("drewno zeszło", s.Ile("drewno") < 400);
            Harness.Rowne("kolejka idzie po kolei", 0, s.WKolejce("deska"));
            Harness.Rowne("następne czeka", 1, s.WKolejce("belka"));
            Pracuj(s, 8.5);
            Harness.Rowne("potem belka", 1, s.Wyroby.Wez("belka"));
            Harness.Rowne("wyroby liczą się do budowy", 1, s.Razem("belka"));

            for (int i = 0; i < 12; i++)
            {
                s.Aktualizuj(0.02, new Wejscie { Klik = "r:deska" });
            }
            Harness.Rowne("kolejka ma limit", 8, s.WKolejce("deska"));

            SwiatWolny b = ZTartakiem();
            b.Plecak["drewno"] = 0;
            b.Aktualizuj(0.02, new Wejscie { Klik = "r:belka" });
            Pracuj(b, 1.0);
            Harness.Sprawdz("brak drewna wstrzymuje zlecenie", b.WKolejce("belka") == 1 && b.Brak != null);

            // Rzemieślnik pracuje sam.
            SwiatWolny r = ZTartakiem();
            r.Obozowisko.Chaty = 2;
            Osada.ZatrudnijOsadnika(r.Obozowisko, new Losowanie(3), "bezczynny", true);
            r.Obozowisko.Osadnicy[0].Zajecie = "rzemieslnik";
            r.Postacie.Add(new PostacOsadnika { O = r.Obozowisko.Osadnicy[0], X = 2.5, Y = 1.5, CelX = 2.5, CelY = 1.5 });
            r.X = 20.5;
            r.Aktualizuj(0.02, new Wejscie { Klik = "r:deska" });
            for (int i = 0; i < 200; i++)
            {
                r.Godzina = 8.0;
                r.Glod = r.Energia = r.Hp = 100;
                r.Aktualizuj(0.05, default);
            }
            Harness.Rowne("rzemieślnik robi zlecenie bez bohatera", 2, r.Wyroby.Wez("deska"));

            // Nocą śpi, jeśli tak mówi plan.
            SwiatWolny sen = ZTartakiem();
            sen.Obozowisko.Chaty = 2;
            Osada.ZatrudnijOsadnika(sen.Obozowisko, new Losowanie(3), "bezczynny", true);
            sen.Obozowisko.Osadnicy[0].Zajecie = "rzemieslnik";
            sen.Postacie.Add(new PostacOsadnika { O = sen.Obozowisko.Osadnicy[0], X = 2.5, Y = 1.5 });
            sen.X = 20.5;
            sen.Godzina = 23.0;
            sen.Aktualizuj(0.02, new Wejscie { Klik = "r:deska" });
            for (int i = 0; i < 60; i++)
            {
                sen.Godzina = 23.0;
                sen.Hp = sen.Glod = sen.Energia = 100;
                sen.Wrogowie.Clear();
                sen.Aktualizuj(0.05, default);
            }
            Harness.Rowne("plan doby obowiązuje też w warsztacie", 0, sen.Wyroby.Wez("deska"));

            SwiatWolny p = ZTartakiem();
            p.Obozowisko.Chaty = 2;
            Osada.ZatrudnijOsadnika(p.Obozowisko, new Losowanie(3), "bezczynny", true);
            p.Postacie.Add(new PostacOsadnika { O = p.Obozowisko.Osadnicy[0], X = 3.5, Y = 3.5 });
            p.PanelOtwarty = true;
            p.Wybrany = 0;
            p.Aktualizuj(0.01, new Wejscie { Klik = "z:rzemieslnik" });
            Harness.Rowne("z panelu można przydzielić rzemieślnika", "rzemieslnik", p.Obozowisko.Osadnicy[0].Zajecie);
            SwiatWolny bez = Swiat();
            bez.Obozowisko.Chaty = 2;
            Osada.ZatrudnijOsadnika(bez.Obozowisko, new Losowanie(3), "bezczynny", true);
            bez.Postacie.Add(new PostacOsadnika { O = bez.Obozowisko.Osadnicy[0], X = 3.5, Y = 3.5 });
            bez.Obozowisko.Osadnicy[0].Zajecie = "drwal";
            bez.PanelOtwarty = true;
            bez.Aktualizuj(0.01, new Wejscie { Klik = "z:rzemieslnik" });
            Harness.Rowne("bez zakładu nie ma rzemieślnika", "drwal", bez.Obozowisko.Osadnicy[0].Zajecie);
        }

        private static void Narzedzia()
        {
            SwiatWolny s = ZTartakiem();
            s.Wyroby["belka"] = 5;
            s.Wyroby["lina"] = 3;
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:mlotek" });
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:mlotek" });
            Harness.Rowne("narzędzia nie zleca się dwa razy", 1, s.WKolejce("mlotek"));
            s.X = 2.5;
            s.Y = 3.5;
            Pracuj(s, 10.5);
            Harness.Sprawdz("młotek gotowy", s.Narzedzia.Contains("mlotek"));
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:mlotek" });
            Harness.Rowne("gotowego też nie", 0, s.WKolejce("mlotek"));
            s.Budowle.Remove((2, 2));
            s.Budowle.Remove((2, 0));

            double Budowa(bool mlotek)
            {
                SwiatWolny b = Swiat();
                if (mlotek)
                {
                    b.Narzedzia.Add("mlotek");
                }
                (int x, int y) = Miejsce(b);
                b.PostawNa("palisada", x, y);
                double t = 0;
                while (b.Place.Count > 0 && t < 30)
                {
                    b.Godzina = 8.0;
                    b.Aktualizuj(0.05, new Wejscie { Dzialaj = true });
                    t += 0.05;
                }
                return t;
            }

            Harness.Sprawdz("młotek przyspiesza budowę", Budowa(true) < Budowa(false) * 0.85);

            double Obrazenia(bool miecz)
            {
                SwiatWolny f = Swiat();
                f.Kierunek = (1.0, 0.0);
                if (miecz)
                {
                    f.Narzedzia.Add("miecz");
                }
                var g = new Wrog { Typ = TypWroga.Wojownik, X = f.X + 1.2, Y = f.Y, Hp = 120, MaxHp = 120 };
                f.Wrogowie.Add(g);
                f.Aktualizuj(0.02, new Wejscie { Cios = true });
                for (int i = 0; i < 24; i++)
                {
                    f.Aktualizuj(0.02, default);
                }
                return 120 - g.Hp;
            }

            Harness.Sprawdz("miecz rani mocniej", Obrazenia(true) > Obrazenia(false));

            double Rana(bool zbroja)
            {
                SwiatWolny f = Swiat();
                if (zbroja)
                {
                    f.Narzedzia.Add("zbroja");
                }
                f.Pociski.Add(new Pocisk { X = f.X - 1.0, Y = f.Y, VX = 6, Zycie = 2, Obrazenia = 20 });
                for (int i = 0; i < 25; i++)
                {
                    f.Aktualizuj(0.02, default);
                }
                return 100 - f.Hp;
            }

            Harness.Sprawdz("zbroja chroni", Rana(true) < Rana(false));

            double Scinka(bool siekiera)
            {
                SwiatWolny f = Swiat();
                if (siekiera)
                {
                    f.Narzedzia.Add("siekiera");
                }
                for (int r = 4; r < 80; r++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        if (f.ObiektNa(x, r) == Obiekt.Drzewo && !f.Zajety(x - 1, r))
                        {
                            f.X = x - 0.6;
                            f.Y = r + 0.5;
                            f.Kierunek = (1.0, 0.0);
                            double t = 0;
                            double start = f.Ile("drewno");
                            while (f.Ile("drewno") == start && t < 5)
                            {
                                f.Aktualizuj(0.02, new Wejscie { Dzialaj = true });
                                t += 0.02;
                            }
                            return t;
                        }
                    }
                }
                throw new InvalidOperationException("brak drzewa");
            }

            Harness.Sprawdz("siekiera szybciej ścina", Scinka(true) < Scinka(false));
        }

        private static void Zniszczenie()
        {
            SwiatWolny s = Swiat();
            (int x, int y) = Miejsce(s);
            s.PostawNa("namiot", x, y);
            s.Budowle[(15, 15)] = "ognisko";     // jest obóz, więc szkielet nie idzie prosto na bohatera
            s.X = 25.5;
            var g = new Wrog { Typ = TypWroga.Wojownik, X = x - 0.5, Y = y + 0.5, Hp = 120, MaxHp = 120, Nocny = true, CelBudowla = (x, y) };
            s.Wrogowie.Add(g);
            for (int i = 0; i < 800 && s.Place.Count > 0; i++)
            {
                s.Godzina = 22.0;
                s.Glod = s.Energia = s.Hp = 100;
                s.Wrogowie.RemoveAll(w => w != g);
                s.Aktualizuj(0.05, default);
            }
            Harness.Rowne("szkielety niszczą plac", 0, s.Place.Count);
            Harness.Sprawdz("kafel jest wolny", !s.Budowle.ContainsKey((x, y)));
        }

        private static void Zapis()
        {
            SwiatWolny s = ZTartakiem();
            (int x, int y) = Miejsce(s);
            s.PostawNa("chata", x, y);
            s.Place[(x, y)].Etap = 1;
            s.Place[(x, y)].Praca = 12.5;
            s.Place[(x, y)].Zaplacony = true;
            s.Wyroby["deska"] = 7;
            s.Narzedzia.Add("kilof");
            s.Aktualizuj(0.01, new Wejscie { Klik = "r:belka" });
            s.Zlecenia["tartak"][0].Praca = 3.25;
            s.Zlecenia["tartak"][0].Zaplacone = true;
            string tekst = s.DoZapisu();
            SwiatWolny w = SwiatWolny.ZZapisu(tekst);
            PlacBudowy p = w.Place[(x, y)];
            Harness.Sprawdz("plac wraca z etapem i pracą", p.Klucz == "chata" && p.Etap == 1 && p.Praca == 12.5 && p.Zaplacony);
            Harness.Rowne("wyroby wracają", 7, w.Wyroby.Wez("deska"));
            Harness.Sprawdz("narzędzia wracają", w.Narzedzia.Contains("kilof"));
            Harness.Sprawdz("kolejka wraca z postępem", w.Zlecenia["tartak"][0].Co.Id == "belka" && w.Zlecenia["tartak"][0].Praca == 3.25 && w.Zlecenia["tartak"][0].Zaplacone);
            Harness.Sprawdz("etapy są włączone po wczytaniu", w.BudowaZEtapami);
            Harness.Rowne("zapis wczytanego jest taki sam", tekst, w.DoZapisu());
        }

        private static void Interfejs()
        {
            SwiatWolny s = Swiat();
            s.TrybBudowy = true;
            KafelkiWolne k = KafelkiWolne.Z(s);
            Harness.Rowne("cztery zakładki", 4, k.Kategorie.Count);
            Harness.Rowne("obóz na start", "spiwor", k.Budowle[0].Ikona);
            s.Aktualizuj(0.01, new Wejscie { Klik = "k:zaklady" });
            k = KafelkiWolne.Z(s);
            Harness.Sprawdz("zakładka zakładów", k.Budowle.Select(b => b.Ikona).SequenceEqual(SwiatWolny.Stacje));
            Harness.Sprawdz("zakładka jest wyróżniona", k.Kategorie.Single(c => c.Wybrany).Id == "k:zaklady");
            s.Aktualizuj(0.01, new Wejscie { Klik = "k:rzemioslo" });
            k = KafelkiWolne.Z(s);
            Harness.Rowne("przepisy na kafelkach", SwiatWolny.Przepisy.Length, k.Budowle.Count);
            Harness.Sprawdz("bez zakładu przepis jest przygaszony", !k.Budowle[0].Dostepny);
            Harness.Sprawdz("kafelek przepisu mówi, czego potrzeba", k.Budowle[0].Opis.Any(o => o.Tekst.Contains("drewna")));
            Harness.Sprawdz("kliknięcie kategorii nie stawia budowli", s.Place.Count == 0);
            Harness.Sprawdz("budowniczy jest w panelu", SwiatWolny.ZajeciaPanelu.Contains("budowniczy"));

            SwiatWolny z = ZTartakiem();
            z.Kategoria = "rzemioslo";
            z.TrybBudowy = true;
            z.Aktualizuj(0.01, new Wejscie { Klik = "r:deska" });
            Harness.Sprawdz("kafelek pokazuje kolejkę", KafelkiWolne.Z(z).Budowle.First(b => b.Id == "r:deska").Opis.Any(o => o.Tekst.Contains("kolejka 1")));

            SwiatWolny b = Swiat();
            (int x, int y) = Miejsce(b);
            b.PostawNa("namiot", x, y);
            Harness.Sprawdz("podpowiedź przy placu", WidokWolny.Podpowiedz(b).Contains("namiot") && WidokWolny.Podpowiedz(b).StartsWith("E:"));
            b.Plecak["spiwor"] = 0;
            Harness.Sprawdz("plecak pokazuje wyroby", true);
            b.Wyroby["belka"] = 3;
            Harness.Sprawdz("plecak pokazuje wyroby", WidokWolny.Plecak(b).Any(l => l.Contains("belki") && l.Contains("3")));
        }
    }
}
