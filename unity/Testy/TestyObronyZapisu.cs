using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Grafika.Wolny;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Obrona osady (palisada, brama, wieże, straż) oraz zapis i wczytanie świata.</summary>
    public static class TestyObronyZapisu
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: obrona osady i zapis gry");
            Palisada();
            Wieza();
            Straz();
            Zapis();
            Plik();
        }

        private static SwiatWolny Bogaty()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 8.0 };
            s.Plecak["drewno"] = 80;
            s.Plecak["kamien"] = 40;
            s.Plecak["wlokno"] = 20;
            return s;
        }

        private static Wrog Dodaj(SwiatWolny s, TypWroga t, double x, double y, bool nocny = false)
        {
            double hp = SwiatWolny.Cechy(t).Hp;
            var g = new Wrog { Typ = t, X = x, Y = y, Hp = hp, MaxHp = hp, Nocny = nocny };
            s.Wrogowie.Add(g);
            return g;
        }

        private static (int X, int Y) Wolny(SwiatWolny s, string klucz)
        {
            for (int r = 2; r <= 5; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        if (s.PowodBudowy(klucz, x, y) == null)
                        {
                            return (x, y);
                        }
                    }
                }
            }
            throw new InvalidOperationException("brak wolnego kafla");
        }

        private static void Palisada()
        {
            SwiatWolny s = Bogaty();
            (int x, int y) = Wolny(s, "palisada");
            Harness.Sprawdz("palisada stawia się z kafelków", s.PostawNa("palisada", x, y));
            Harness.Rowne("palisada kosztuje drewno", 77, s.Ile("drewno"));
            Harness.Sprawdz("palisada zatrzymuje bohatera", s.Zajety(x, y));
            (int bx, int by) = Wolny(s, "brama");
            s.PostawNa("brama", bx, by);
            Harness.Sprawdz("przez bramę przechodzą swoi", !s.Zajety(bx, by));
            s.Kategoria = "obrona";
            Harness.Rowne("trzy kafelki obrony", 3, KafelkiWolne.Z(Tryb(s)).Budowle.Count);
            s.TrybBudowy = false;

            // Szkielet przed bramą nie przejdzie: musi ją zburzyć.
            var m = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 8.0 };
            m.X = 8.5;
            m.Y = 0.5;
            m.Budowle[(4, 0)] = "brama";
            Wrog g = Dodaj(m, TypWroga.Wojownik, 3.3, 0.5);
            for (int i = 0; i < 40; i++)
            {
                m.Hp = 100;
                m.Aktualizuj(0.05, default);
            }
            Harness.Sprawdz("szkielet nie przechodzi przez bramę", g.X < 4.0);
            for (int i = 0; i < 600 && m.Budowle.ContainsKey((4, 0)); i++)
            {
                m.Hp = 100;
                m.Aktualizuj(0.05, default);
            }
            Harness.Sprawdz("zablokowany szkielet burzy to, co stoi mu na drodze", !m.Budowle.ContainsKey((4, 0)));
            Harness.Sprawdz("zburzenie bramy trafia do dziennika", m.Dziennik.Any(l => l.Contains("bram")));

            Harness.Sprawdz("palisada jest mocniejsza od śpiwora", SwiatWolny.ZyciePoczatkowe("palisada") > SwiatWolny.ZyciePoczatkowe("spiwor"));
            Harness.Sprawdz("wieża jest najmocniejsza", SwiatWolny.ZyciePoczatkowe("wieza") >= SwiatWolny.ZyciePoczatkowe("chata"));
        }

        private static SwiatWolny Tryb(SwiatWolny s)
        {
            s.TrybBudowy = true;
            return s;
        }

        private static void Wieza()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 8.0 };
            s.Budowle[(4, 4)] = "wieza";
            Wrog blisko = Dodaj(s, TypWroga.Wojownik, 7.5, 4.5);
            Wrog daleko = Dodaj(s, TypWroga.Wojownik, 30.5, 4.5);
            daleko.Stan = StanWroga.Stoi;
            blisko.Stan = StanWroga.Stoi;
            s.X = 0.5;
            s.Y = 0.5;
            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("wieża strzela do wroga w zasięgu", blisko.Hp < blisko.MaxHp);
            Harness.Rowne("wieża nie sięga dalekich", daleko.MaxHp, daleko.Hp);
            Harness.Rowne("strzała leci", 1, s.Strzaly.Count);
            double hp = blisko.Hp;
            for (int i = 0; i < 20; i++)
            {
                s.Aktualizuj(0.02, default);
            }
            Harness.Rowne("wieża ma rytm, a nie ciągły ogień", hp, blisko.Hp);
            Harness.Rowne("strzała doleciała i znikła", 0, s.Strzaly.Count);
            for (int i = 0; i < 60; i++)
            {
                s.Aktualizuj(0.02, default);
            }
            Harness.Sprawdz("potem strzela znowu", blisko.Hp < hp);
        }

        private static SwiatWolny ZLudzmi(int ile)
        {
            SwiatWolny s = Bogaty();
            s.Budowle[(3, 3)] = "ognisko";
            s.Budowle[(3, 4)] = "namiot";
            s.Obozowisko.Chaty = 6;
            var r = new Losowanie(7);
            for (int i = 0; i < ile; i++)
            {
                Osada.ZatrudnijOsadnika(s.Obozowisko, r, "bezczynny", true);
            }
            s.ObozZalozony = true;
            s.Postacie.AddRange(s.Obozowisko.Osadnicy.Select((o, i) => new PostacOsadnika { O = o, X = 1.5 + i, Y = 5.5, CelX = 1.5 + i, CelY = 5.5 }));
            return s;
        }

        private static void Straz()
        {
            Harness.Sprawdz("strażnik jest w panelu", SwiatWolny.ZajeciaPanelu.Contains("straznik"));
            SwiatWolny s = ZLudzmi(2);
            PostacOsadnika st = s.Postacie[0], cywil = s.Postacie[1];
            st.O.Zajecie = "straznik";
            cywil.O.Zajecie = "bezczynny";
            Wrog g = Dodaj(s, TypWroga.Slugus, st.X + 5, st.Y);
            g.Stan = StanWroga.Stoi;
            double x = st.X;
            s.X = 40.5;
            s.Y = 0.5;
            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("strażnik ma więcej życia", st.Zycie >= SwiatWolny.ZycieStraznika);
            Harness.Sprawdz("zwykły osadnik zostaje przy swoim życiu", Math.Abs(cywil.Zycie - SwiatWolny.ZycieOsadnika) < 1e-9);
            for (int i = 0; i < 50; i++)
            {
                s.Aktualizuj(0.02, default);
            }
            Harness.Sprawdz("strażnik idzie na wroga", st.WWalce && st.X > x + 1.0);
            Harness.Sprawdz("zwykły osadnik nie walczy", !cywil.WWalce);
            for (int i = 0; i < 400 && g.Stan != StanWroga.Martwy; i++)
            {
                s.Hp = 100;
                s.Aktualizuj(0.02, default);
            }
            Harness.Sprawdz("strażnik zabija wroga", g.Stan == StanWroga.Martwy);
            s.Wrogowie.Clear();
            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("bez wrogów wraca do swoich spraw", !st.WWalce);
            st.O.Zajecie = "drwal";
            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("po zmianie zajęcia traci zbroję", !st.Uzbrojony && st.Zycie <= SwiatWolny.ZycieOsadnika);
        }

        private static void Zapis()
        {
            SwiatWolny s = ZLudzmi(2);
            s.X = 12.25;
            s.Y = -3.75;
            s.Kierunek = (0.6, 0.8);
            s.Godzina = 22.5;
            s.Dzien = 7;
            s.Glod = 61.5;
            s.Energia = 33.25;
            s.Hp = 77.125;
            s.Wytrzymalosc = 44.0;
            s.Zabici = 13;
            s.Budowle[(5, 5)] = "palisada";
            s.ZycieBudowli[(5, 5)] = 12.5;
            s.Budowle[(6, 5)] = "wieza";
            s.Obozowisko.Surowce["drewno"] = 55;
            s.Obozowisko.Czas = 6;
            Osadnik o = s.Obozowisko.Osadnicy[0];
            o.Zajecie = "straznik";
            o.Morale = 41.5;
            o.Chory = 2;
            Kompetencje.Poziom(o, "budowa");
            s.Porzucone.Add(new PorzuconyPlecak { X = 9.5, Y = 8.5, Zawartosc = new Dictionary<string, int> { { "drewno", 4 } } });
            (int ox, int oy) = ZnajdzDrzewo(s);
            s.Plecak["jagody"] = 3;

            for (int i = 0; i < 12 && s.ObiektNa(ox, oy) != Obiekt.Brak; i++)
            {
                double sx = s.X, sy = s.Y, gh = s.Godzina;
                s.X = ox - 0.9;
                s.Y = oy + 0.5;
                s.Kierunek = (1.0, 0.0);
                s.Aktualizuj(0.1, new Wejscie { Dzialaj = true });
                s.X = sx;
                s.Y = sy;
                s.Godzina = gh;
                for (int k = 0; k < 10; k++)
                {
                    s.X = ox - 0.9;
                    s.Y = oy + 0.5;
                    s.Aktualizuj(0.1, new Wejscie { Dzialaj = true });
                    s.X = sx;
                    s.Y = sy;
                }
            }
            Harness.Sprawdz("drzewo do zapisu jest ścięte", s.ObiektNa(ox, oy) == Obiekt.Brak);

            s.Postacie[0].Zycie = 66.0;
            s.Postacie[0].X = 2.25;
            s.Wrogowie.Clear();
            s.Hp = 77.125;
            Dodaj(s, TypWroga.Mag, 20.5, 20.5, true).Hp = 9.0;
            s.X = 12.25;
            s.Y = -3.75;
            double czas = s.GodzinGry, godzina = s.Godzina, glod = s.Glod, hp = s.Hp;
            string tekst = s.DoZapisu();
            SwiatWolny w = SwiatWolny.ZZapisu(tekst);

            Harness.Rowne("ziarno świata wraca", s.Seed, w.Seed);
            Harness.Sprawdz("świat jest ten sam", w.Swiat.Biom(100, 50) == s.Swiat.Biom(100, 50));
            Harness.Sprawdz("pozycja wraca co do ułamka", w.X == 12.25 && w.Y == -3.75);
            Harness.Sprawdz("kierunek wraca", w.Kierunek == s.Kierunek);
            Harness.Sprawdz("potrzeby wracają", w.Glod == glod && w.Hp == hp && w.Godzina == godzina && w.Dzien == s.Dzien);
            Harness.Rowne("czas gry wraca", czas, w.GodzinGry);
            Harness.Rowne("plecak wraca", s.Ile("jagody"), w.Ile("jagody"));
            Harness.Rowne("budowle wracają", s.Budowle.Count, w.Budowle.Count);
            Harness.Rowne("palisada ma swoje rany", 12.5, w.ZycieBudowli[(5, 5)]);
            Harness.Rowne("miejsca do spania liczą się od nowa", s.Budowle.Values.Count(b => b == "namiot") * SwiatWolny.MiejscaNamiotu, w.Obozowisko.Chaty);
            Harness.Rowne("magazyn wraca", 55, w.Obozowisko.Surowce["drewno"]);
            Harness.Rowne("ludzie wracają", 2, w.Obozowisko.Osadnicy.Count);
            Osadnik ow = w.Obozowisko.Osadnicy[0];
            Harness.Sprawdz("osadnik wraca z zajęciem, morale i chorobą", ow.Imie == o.Imie && ow.Zajecie == "straznik" && ow.Morale == 41.5 && ow.Chory == 2 && ow.Cecha == o.Cecha);
            Harness.Sprawdz("umiejętności i pasje wracają", Kompetencje.Poziom(ow, "budowa") == Kompetencje.Poziom(o, "budowa") && ow.Kompetencje.Count == o.Kompetencje.Count);
            Harness.Sprawdz("postać osadnika wraca na miejsce", w.Postacie[0].X == 2.25 && w.Postacie[0].Zycie == 66.0 && w.Postacie[0].O == ow);
            Harness.Sprawdz("wróg wraca ranny", w.Wrogowie.Count == 1 && w.Wrogowie[0].Typ == TypWroga.Mag && w.Wrogowie[0].Hp == 9.0 && w.Wrogowie[0].MaxHp > 9.0);
            Harness.Rowne("porzucony plecak wraca", 4, w.Porzucone.Single().Zawartosc["drewno"]);
            Harness.Sprawdz("ścięte drzewo nie odrasta po wczytaniu", w.ObiektNa(ox, oy) == Obiekt.Brak);
            Harness.Sprawdz("walka jest włączona", w.WalkaWlaczona);
            Harness.Rowne("zabici się zgadzają", 13, w.Zabici);
            Harness.Rowne("zapis wczytanego świata jest taki sam", tekst, w.DoZapisu());

            w.Aktualizuj(0.05, default);
            Harness.Sprawdz("wczytany świat działa dalej", w.GodzinGry > czas);

            Harness.Rzuca("obcy plik jest odrzucany", () => SwiatWolny.ZZapisu("cokolwiek\t1"));
            Harness.Rzuca("urwany zapis jest odrzucany", () => SwiatWolny.ZZapisu(tekst.Substring(0, tekst.Length / 2)));
            Harness.Rzuca("zapis z przyszłej wersji jest odrzucany", () => SwiatWolny.ZZapisu(tekst.Replace("SimplyPrzetrwanie\t5", "SimplyPrzetrwanie\t99")));

            SwiatWolny swit = Bogaty();
            swit.Godzina = 22.0;
            swit.Aktualizuj(0.02, default);
            Harness.Sprawdz("w nocy nie ma autozapisu", !swit.PotrzebnyZapis);
            swit.Godzina = 5.5;
            swit.Hp = 100;
            swit.Aktualizuj(0.02, default);
            Harness.Sprawdz("o świcie gra prosi o zapis", swit.PotrzebnyZapis);
        }

        private static (int X, int Y) ZnajdzDrzewo(SwiatWolny s)
        {
            for (int r = 5; r < 80; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (s.ObiektNa(x, r) == Obiekt.Drzewo && !s.Zajety(x - 1, r))
                    {
                        return (x, r);
                    }
                }
            }
            throw new InvalidOperationException("brak drzewa");
        }

        private static void Plik()
        {
            string katalog = Path.Combine(Path.GetTempPath(), "simply-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(katalog);
            string stara = ZapisWolny.Sciezka;
            try
            {
                ZapisWolny.Sciezka = null;
                Harness.Sprawdz("bez ścieżki nie ma zapisu", !ZapisWolny.Istnieje && !ZapisWolny.Zapisz(Bogaty()) && ZapisWolny.Wczytaj() == null);

                ZapisWolny.Sciezka = Path.Combine(katalog, "przetrwanie.txt");
                Harness.Sprawdz("na początku brak pliku", !ZapisWolny.Istnieje && ZapisWolny.Wczytaj() == null);
                SwiatWolny s = Bogaty();
                s.Dzien = 3;
                Harness.Sprawdz("zapis się udaje", ZapisWolny.Zapisz(s) && ZapisWolny.Istnieje);
                Harness.Rowne("wczytanie zwraca świat", 3, ZapisWolny.Wczytaj().Dzien);
                s.Dzien = 4;
                ZapisWolny.Zapisz(s);
                Harness.Rowne("nowy zapis zastępuje stary", 4, ZapisWolny.Wczytaj().Dzien);
                Harness.Sprawdz("stary zostaje jako kopia", File.Exists(ZapisWolny.Sciezka + ".kopia"));
                Harness.Sprawdz("nie zostają pliki tymczasowe", !File.Exists(ZapisWolny.Sciezka + ".tmp"));

                File.WriteAllText(ZapisWolny.Sciezka, "uszkodzony");
                SwiatWolny k = ZapisWolny.Wczytaj();
                Harness.Rowne("przy uszkodzeniu rusza kopia", 3, k?.Dzien ?? -1);
                File.WriteAllText(ZapisWolny.Sciezka + ".kopia", "też uszkodzona");
                Harness.Sprawdz("gdy oba zepsute, gra nie pada", ZapisWolny.Wczytaj() == null);
            }
            finally
            {
                ZapisWolny.Sciezka = stara;
                Directory.Delete(katalog, true);
            }
        }
    }
}
