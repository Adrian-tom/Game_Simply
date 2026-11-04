using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Grafika.Wolny;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Faza 2 „Zomboid fantasy”: magazyn, przybysze, miejsca do spania, praca osadników i panel.</summary>
    public static class TestyOsadyWolnej
    {
        private static SwiatWolny Zalozony(int zywnosc = 200, long seed = 42)
        {
            var s = new SwiatWolny(seed);
            s.Plecak["drewno"] = 13;
            s.Plecak["kamien"] = 3;
            s.Plecak["wlokno"] = 6;
            s.Kierunek = (1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            s.Kierunek = (0.0, 1.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 3 });
            s.Obozowisko.Surowce["zywnosc"] = zywnosc;
            return s;
        }

        /// <summary>Mija doba; bohater przeżywa, bo sprawdzamy osadę, a nie jego głód.</summary>
        private static void Doby(SwiatWolny s, int ile)
        {
            int cel = s.Dzien + ile;
            while (s.Dzien < cel)
            {
                s.Glod = s.Energia = s.Hp = 100.0;
                for (int i = 0; i < 200; i++)
                {
                    s.Aktualizuj(0.1, default);
                }
            }
        }

        public static void Uruchom()
        {
            Harness.Grupa("Osada w świecie: przybysze i miejsca");
            Przybysze();
            Harness.Grupa("Osada w świecie: magazyn i budowanie");
            Magazyn();
            Harness.Grupa("Osada w świecie: praca, głód, chodzenie");
            Praca();
            Harness.Grupa("Osada w świecie: panel i interfejs");
            Panel();
        }

        private static void Przybysze()
        {
            var bez = new SwiatWolny(42);
            Doby(bez, 3);
            Harness.Rowne("bez obozu nikt nie przychodzi", 0, bez.Obozowisko.Osadnicy.Count);

            SwiatWolny s = Zalozony();
            Harness.Sprawdz("obóz założony, namiot daje dwa miejsca", s.ObozZalozony && s.Obozowisko.Chaty == SwiatWolny.MiejscaNamiotu);
            Doby(s, 1);
            Harness.Rowne("pierwszy przybysz zjawia się następnego dnia", 1, s.Obozowisko.Osadnicy.Count);
            Harness.Rowne("stoi na mapie", 1, s.Postacie.Count);
            Harness.Sprawdz("ma umiejętności i cechę", s.Obozowisko.Osadnicy[0].Kompetencje != null
                            && !string.IsNullOrEmpty(s.Obozowisko.Osadnicy[0].Cecha));
            Doby(s, 12);
            Harness.Rowne("więcej ludzi niż miejsc nie przyjdzie", 2, s.Obozowisko.Osadnicy.Count);

            s.Plecak["drewno"] = 12;
            s.Plecak["kamien"] = 6;
            s.Kierunek = (-1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 4 });
            Harness.Rowne("chata dodaje trzy miejsca", SwiatWolny.MiejscaNamiotu + SwiatWolny.MiejscaChaty, s.Obozowisko.Chaty);
            Doby(s, 25);
            Harness.Sprawdz("z chatą przychodzą kolejni", s.Obozowisko.Osadnicy.Count > 2);
            Harness.Rowne("każdy osadnik ma postać na mapie", s.Obozowisko.Osadnicy.Count, s.Postacie.Count);

            SwiatWolny a = Zalozony(), b = Zalozony();
            Doby(a, 6);
            Doby(b, 6);
            Harness.RowneCiagi("ten sam świat daje tych samych ludzi",
                               a.Obozowisko.Osadnicy.Select(o => o.Imie), b.Obozowisko.Osadnicy.Select(o => o.Imie));
        }

        private static void Magazyn()
        {
            SwiatWolny s = Zalozony(0);
            s.Plecak["drewno"] = 7;
            s.Plecak["kamien"] = 2;
            s.Plecak["jagody"] = 3;
            s.Plecak["wlokno"] = 4;
            s.Aktualizuj(0.05, new Wejscie { Zloz = true });
            Harness.Sprawdz("T odkłada drewno i kamień do magazynu",
                            s.Obozowisko.Surowce["drewno"] == 7 && s.Obozowisko.Surowce["kamien"] == 2 && s.Ile("drewno") == 0);
            Harness.Rowne("jagody stają się żywnością osady", 3, s.Obozowisko.Surowce["zywnosc"]);
            Harness.Rowne("włókno zostaje w plecaku", 4, s.Ile("wlokno"));

            double x = s.X;
            s.X = 40.5;
            s.Plecak["kamien"] = 1;
            Harness.Sprawdz("daleko od obozu magazynu nie ma", !s.Zloz() && s.Ile("kamien") == 1);
            s.X = x;

            s.Glod = 50.0;
            s.Plecak["jagody"] = 0;
            s.Aktualizuj(0.01, new Wejscie { Jedz = true });
            Harness.Sprawdz("przy obozie bohater je z zapasów", s.Glod > 60 && s.Obozowisko.Surowce["zywnosc"] == 2);

            SwiatWolny c = Zalozony(0);
            c.Obozowisko.Surowce["drewno"] = 20;
            c.Obozowisko.Surowce["kamien"] = 5;
            c.Plecak["kamien"] = 1;
            c.Kierunek = (-1.0, 0.0);
            c.Aktualizuj(0.05, new Wejscie { Buduj = 4 });
            Harness.Sprawdz("chata powstaje z plecaka i magazynu (najpierw plecak)",
                            c.Budowle.ContainsValue("chata") && c.Ile("kamien") == 0
                            && c.Obozowisko.Surowce["kamien"] == 0 && c.Obozowisko.Surowce["drewno"] == 8);
        }

        private static void Praca()
        {
            SwiatWolny s = Zalozony();
            Doby(s, 1);
            Osadnik o = s.Obozowisko.Osadnicy[0];
            Osada.UstawZajecie(s.Obozowisko, o, "drwal");
            int drewno = s.Obozowisko.Surowce["drewno"];
            int zywnosc = s.Obozowisko.Surowce["zywnosc"];
            double xp = o.Kompetencje["zbieractwo"].Xp + 1000 * o.Kompetencje["zbieractwo"].P;
            Doby(s, 2);
            Harness.Sprawdz("drwal przynosi drewno do magazynu", s.Obozowisko.Surowce["drewno"] > drewno);
            Harness.Sprawdz("osadnicy jedzą z magazynu", s.Obozowisko.Surowce["zywnosc"] < zywnosc);
            Harness.Sprawdz("praca podnosi umiejętność",
                            o.Kompetencje["zbieractwo"].Xp + 1000 * o.Kompetencje["zbieractwo"].P > xp);

            SwiatWolny g = Zalozony(0);
            Doby(g, 3);
            Harness.Sprawdz("bez jedzenia osada głoduje i gra to mówi", g.Dziennik.Any(d => d.Contains("GŁÓD")));

            SwiatWolny c = Zalozony();
            Doby(c, 1);
            PostacOsadnika p = c.Postacie[0];
            c.Godzina = 10.0;
            bool wPrzeszkodzie = false;
            double dystans = p.Dystans;
            Osada.UstawZajecie(c.Obozowisko, p.O, "drwal");
            for (int i = 0; i < 400; i++)
            {
                c.Aktualizuj(0.05, default);
                double fx = p.X - Math.Floor(p.X), fy = p.Y - Math.Floor(p.Y);
                wPrzeszkodzie |= c.Zajety((int)Math.Floor(p.X), (int)Math.Floor(p.Y))
                                 && fx > 0.4 && fx < 0.6 && fy > 0.4 && fy < 0.6;
            }
            Harness.Sprawdz("osadnik chodzi po świecie", p.Dystans > dystans + 0.5);
            Harness.Sprawdz("osadnik nie wchodzi w przeszkody", !wPrzeszkodzie);
            c.Godzina = 22.0;
            for (int i = 0; i < 300; i++)
            {
                c.Aktualizuj(0.05, default);
            }
            Harness.Sprawdz("nocą osadnik śpi przy budowli", p.Spi);
        }

        private static void Panel()
        {
            SwiatWolny s = Zalozony();
            Doby(s, 12);
            Harness.Rowne("dwóch ludzi do zarządzania", 2, s.Obozowisko.Osadnicy.Count);
            Harness.Sprawdz("panel jest zamknięty", WidokWolny.Panel(s) == null);
            s.Aktualizuj(0.01, new Wejscie { Panel = true });
            Harness.Sprawdz("Tab otwiera panel", s.PanelOtwarty && WidokWolny.Panel(s).Count >= 3);
            double x = s.X;
            s.Aktualizuj(0.05, new Wejscie { Dx = 1, Nawig = 1 });
            Harness.Sprawdz("w panelu bohater stoi", s.X == x);
            Harness.Rowne("góra/dół wybiera osadnika", 1, s.Wybrany);
            s.Aktualizuj(0.05, new Wejscie { Nawig = 1 });
            Harness.Rowne("wybór zawija się", 0, s.Wybrany);
            string przed = s.Obozowisko.Osadnicy[0].Zajecie;
            s.Aktualizuj(0.05, new Wejscie { Zmien = 1 });
            Harness.Sprawdz("lewo/prawo zmienia zajęcie", s.Obozowisko.Osadnicy[0].Zajecie != przed);
            Harness.Sprawdz("panel pokazuje wybranego osadnika",
                            WidokWolny.Panel(s).Any(l => l.StartsWith(">") && l.Contains(s.Obozowisko.Osadnicy[0].Imie)));
            Harness.Sprawdz("podpowiedź mówi o panelu", WidokWolny.Podpowiedz(s).Contains("Tab"));
            Harness.Sprawdz("plecak pokazuje magazyn", WidokWolny.Plecak(s).Any(l => l.StartsWith("MAGAZYN")));
            Harness.Rowne("pasek liczy ludzi", "osada: 2 os.", WidokWolny.Hud(s).Region);

            var pulpit = new Pulpit();
            var scena = new ScenaWolna();
            List<Napis> napisy = pulpit.ZlozWolny(scena, s, 1.0, WidokWolny.Hud(s), WidokWolny.Plecak(s),
                                                  WidokWolny.Dziennik(s), WidokWolny.Podpowiedz(s), WidokWolny.Panel(s));
            Harness.Sprawdz("panel jest narysowany", napisy.Any(n => n.Tekst.StartsWith("OSADA")));
            Harness.Sprawdz("imiona nie wchodzą na otwarty panel",
                            !scena.Podpisy.Any(p => napisy.Any(n => n.Tekst == p.Tekst)));
            napisy = pulpit.ZlozWolny(scena, s, 1.0, WidokWolny.Hud(s), WidokWolny.Plecak(s),
                                      WidokWolny.Dziennik(s), WidokWolny.Podpowiedz(s));
            Harness.Sprawdz("bez panelu imiona widocznych osadników są w napisach",
                            scena.Podpisy.All(p => napisy.Any(n => n.Tekst == p.Tekst)));
        }
    }
}
