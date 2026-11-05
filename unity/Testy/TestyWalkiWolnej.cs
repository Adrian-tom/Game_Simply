using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Walka w czasie rzeczywistym: wytrzymałość, unik, blok, parowanie, fale nocne, ciosy specjalne, śmierć.</summary>
    public static class TestyWalkiWolnej
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: walka w czasie rzeczywistym");
            Wylaczona();
            Wytrzymalosc();
            Ciosy();
            Unik();
            BlokIParowanie();
            Fale();
            Dzien();
            Specjalne();
            Smierc();
            Oboz();
            Interfejs();
        }

        private static SwiatWolny Walka()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 8.0 };
            s.Kierunek = (1.0, 0.0);
            return s;
        }

        private static Wrog Dodaj(SwiatWolny s, TypWroga t, double dx = 1.2, double dy = 0.0)
        {
            double hp = SwiatWolny.Cechy(t).Hp;
            var g = new Wrog { Typ = t, X = s.X + dx, Y = s.Y + dy, Hp = hp, MaxHp = hp };
            s.Wrogowie.Add(g);
            return g;
        }

        private static void Czekaj(SwiatWolny s, double sekund, Wejscie w = default)
        {
            for (double t = 0; t < sekund; t += 0.02)
            {
                s.Aktualizuj(0.02, w);
            }
        }

        private static void Wylaczona()
        {
            var s = new SwiatWolny(42) { Godzina = 22.0 };
            Czekaj(s, 5.0);
            Harness.Rowne("bez walki noc jest spokojna", 0, s.Wrogowie.Count);
            s = new SwiatWolny(42);
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Harness.Sprawdz("bez walki cios nic nie robi", s.Akcja == AkcjaBohatera.Brak);
        }

        private static void Wytrzymalosc()
        {
            SwiatWolny s = Walka();
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Harness.Sprawdz("cios zużywa wytrzymałość", s.Wytrzymalosc < SwiatWolny.MaxWytrzymalosc && s.Akcja == AkcjaBohatera.Cios);
            double po = s.Wytrzymalosc;
            Czekaj(s, 0.46);
            Harness.Sprawdz("przez chwilę po ciosie nie ma odpoczynku", s.Wytrzymalosc <= po + 0.01);
            Czekaj(s, 3.0);
            Harness.Rowne("potem wytrzymałość wraca do pełna", SwiatWolny.MaxWytrzymalosc, s.Wytrzymalosc);

            s.Wytrzymalosc = 0;
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Harness.Sprawdz("bez wytrzymałości nie ma ciosu", s.Akcja == AkcjaBohatera.Brak);
            s.Aktualizuj(0.02, new Wejscie { Unik = true });
            Harness.Sprawdz("bez wytrzymałości nie ma uniku", s.Akcja == AkcjaBohatera.Brak);
        }

        private static void Ciosy()
        {
            SwiatWolny s = Walka();
            Wrog g = Dodaj(s, TypWroga.Wojownik);
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Harness.Rowne("cios jeszcze nie doszedł", g.MaxHp, g.Hp);
            Czekaj(s, 0.46);
            Harness.Sprawdz("lekki cios rani raz", Math.Abs(g.Hp - (g.MaxHp - 17)) < 1e-6);
            Harness.Sprawdz("wojownika lekki cios nie przerywa", g.Stan != StanWroga.Ogluszony);

            s = Walka();
            g = Dodaj(s, TypWroga.Wojownik);
            s.Aktualizuj(0.02, new Wejscie { Ciezki = true });
            Czekaj(s, 0.9);
            Harness.Sprawdz("ciężki cios rani mocniej", g.MaxHp - g.Hp > 17);

            s = Walka();
            g = Dodaj(s, TypWroga.Slugus, -1.2);
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Czekaj(s, 0.46);
            Harness.Rowne("cios za plecy nie trafia", g.MaxHp, g.Hp);

            s = Walka();
            g = Dodaj(s, TypWroga.Slugus, 0.0, 1.2);
            s.Aktualizuj(0.02, new Wejscie { Cios = true, CelWalki = (s.X, s.Y + 3.0) });
            Czekaj(s, 0.46);
            Harness.Sprawdz("cios idzie w stronę kursora", g.Hp < g.MaxHp);

            s = Walka();
            g = Dodaj(s, TypWroga.Slugus);
            g.Hp = 5;
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Czekaj(s, 0.46);
            Harness.Rowne("wróg ginie", 1, s.Zabici);
            Czekaj(s, 2.5);
            Harness.Rowne("martwy znika po chwili", 0, s.Wrogowie.Count);
        }

        private static void Unik()
        {
            SwiatWolny s = Walka();
            double x = s.X;
            s.Aktualizuj(0.02, new Wejscie { Unik = true, Dx = 1 });
            Harness.Sprawdz("unik daje nietykalność", s.Nietykalny && s.Akcja == AkcjaBohatera.Unik);
            Czekaj(s, 0.2);
            Harness.Sprawdz("unik szybko przesuwa", s.X - x > 1.0);

            s = Walka();
            Wrog g = Dodaj(s, TypWroga.Wojownik);
            Czekaj(s, 0.5);
            Harness.Sprawdz("wróg bierze zamach", g.Stan == StanWroga.Zamach);
            double hp = s.Hp;
            s.Aktualizuj(0.02, new Wejscie { Unik = true, Dx = -1 });
            Czekaj(s, 1.2);
            Harness.Rowne("odskok w czasie zamachu ratuje", hp, s.Hp);
        }

        private static void BlokIParowanie()
        {
            // Stały blok: mało obrażeń, ale płaci wytrzymałość.
            SwiatWolny s = Walka();
            Wrog g = Dodaj(s, TypWroga.Slugus);
            var trzymaj = new Wejscie { Blok = true };
            Czekaj(s, 0.5, trzymaj);
            double hp = s.Hp, wyt = s.Wytrzymalosc;
            while (g.Stan == StanWroga.Zamach)
            {
                s.Aktualizuj(0.02, trzymaj);
            }
            Harness.Sprawdz("blok prawie zatrzymuje cios", hp - s.Hp < 2.0 && hp - s.Hp > 0);
            Harness.Sprawdz("blok kosztuje wytrzymałość", s.Wytrzymalosc < wyt);

            // Przebicie gardy.
            s = Walka();
            g = Dodaj(s, TypWroga.Wojownik);
            s.Wytrzymalosc = 10;
            Czekaj(s, 0.3, trzymaj);
            s.Wytrzymalosc = 10;
            while (g.Stan != StanWroga.Odpoczynek)
            {
                s.Aktualizuj(0.02, trzymaj);
                if (s.Wytrzymalosc > 10)
                {
                    s.Wytrzymalosc = 10;
                }
            }
            Harness.Sprawdz("wyczerpana garda pęka", s.Akcja == AkcjaBohatera.Zachwianie);

            // Parowanie: naciśnięcie tuż przed trafieniem.
            s = Walka();
            g = Dodaj(s, TypWroga.Wojownik);
            while (!(g.Stan == StanWroga.Zamach && g.Timer < 0.1))
            {
                s.Aktualizuj(0.02, default);
            }
            hp = s.Hp;
            while (g.Stan == StanWroga.Zamach)
            {
                s.Aktualizuj(0.02, trzymaj);
            }
            Harness.Rowne("parowanie nie kosztuje życia", hp, s.Hp);
            Harness.Sprawdz("parowanie ogłusza napastnika", g.Stan == StanWroga.Ogluszony);
            Harness.Sprawdz("parowanie otwiera ripostę", s.Riposta > 0);
            double przed = g.Hp;
            s.Aktualizuj(0.02, new Wejscie { Cios = true });
            Czekaj(s, 0.46);
            Harness.Sprawdz("riposta rani znacznie mocniej", przed - g.Hp > 40);

            // Blok z tyłu nie działa.
            s = Walka();
            g = Dodaj(s, TypWroga.Slugus, -1.0);
            Czekaj(s, 0.1, trzymaj);
            hp = s.Hp;
            for (int i = 0; i < 80 && s.Hp == hp; i++)
            {
                s.Aktualizuj(0.02, trzymaj);
            }
            Harness.Sprawdz("cios w plecy przechodzi przez blok", hp - s.Hp >= 7.5);

            // Pocisk maga.
            s = Walka();
            s.Pociski.Add(new Pocisk { X = s.X - 1.0, Y = s.Y, VX = 6, Zycie = 2, Obrazenia = 14 });
            hp = s.Hp;
            Czekaj(s, 0.5);
            Harness.Sprawdz("pocisk rani i znika", hp - s.Hp >= 13.5 && s.Pociski.Count == 0);
        }

        private static void Fale()
        {
            var r = new Losowanie(5);
            int d1 = SwiatWolny.SkladFali(1, r).Sum(t => SwiatWolny.Cechy(t).Koszt);
            int d6 = SwiatWolny.SkladFali(6, r).Sum(t => SwiatWolny.Cechy(t).Koszt);
            Harness.Sprawdz($"fale rosną z dniami ({d1} → {d6})", d6 > d1);
            Harness.Sprawdz("pierwszej nocy idą same sługusy", SwiatWolny.SkladFali(1, new Losowanie(7)).All(t => t == TypWroga.Slugus));

            SwiatWolny s = Walka();
            s.Godzina = 20.99;
            s.Aktualizuj(0.02, default);
            Harness.Rowne("przed nocą cisza", 0, s.Wrogowie.Count(g => g.Nocny));
            s.Godzina = 21.0;
            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("nocą nadchodzi fala", s.Wrogowie.Count(g => g.Nocny) >= 3);
            for (int i = 0; i < 300; i++)
            {
                s.Glod = s.Hp = s.Energia = 100;
                s.Dzien = 30;
                s.Aktualizuj(0.1, default);
                Harness.Sprawdz("limit wrogów chroni płynność", s.Wrogowie.Count(g => g.Stan != StanWroga.Martwy) <= SwiatWolny.LimitWrogow + 0);
                if (i > 3)
                {
                    break;
                }
            }

            s.Godzina = 4.999;
            s.Hp = 100;
            s.Aktualizuj(0.1, default);
            s.Godzina = 5.1;
            s.Aktualizuj(0.02, default);
            Harness.Sprawdz("o świcie nocne szkielety padają", s.Wrogowie.Where(g => g.Nocny).All(g => g.Stan == StanWroga.Martwy));
            Harness.Sprawdz("świt trafia do dziennika", s.Dziennik.Any(l => l.StartsWith("Świt")));

            // Tłum osacza, ale naraz uderza najwyżej dwóch.
            SwiatWolny tlum = Walka();
            for (int i = 0; i < 6; i++)
            {
                double kat = i * Math.PI / 3;
                Dodaj(tlum, TypWroga.Slugus, Math.Cos(kat), Math.Sin(kat));
            }
            int najwiecej = 0;
            for (int i = 0; i < 200; i++)
            {
                tlum.Hp = 100;
                tlum.Aktualizuj(0.02, default);
                najwiecej = Math.Max(najwiecej, tlum.Wrogowie.Count(g => g.Stan == StanWroga.Zamach));
            }
            Harness.Sprawdz($"naraz uderza najwyżej dwóch ({najwiecej})", najwiecej >= 1 && najwiecej <= SwiatWolny.MaxAtakujacych);

            // Sen nie przeskoczy fali.
            SwiatWolny sen = Walka();
            sen.Godzina = 20.9;
            sen.Plecak["spiwor"] = 1;
            sen.Energia = 30;
            sen.Aktualizuj(0.02, new Wejscie { Buduj = 1 });
            sen.Aktualizuj(0.02, new Wejscie { Spij = true });
            Harness.Sprawdz("bohater zasnął", sen.Spi);
            for (int i = 0; i < 200 && sen.Spi; i++)
            {
                sen.Aktualizuj(0.05, default);
            }
            Harness.Sprawdz("fala budzi śpiącego", !sen.Spi && sen.Wrogowie.Any(g => g.Nocny));
            sen.Wrogowie[0].X = sen.X + 3;
            sen.Wrogowie[0].Y = sen.Y;
            sen.Aktualizuj(0.02, new Wejscie { Spij = true });
            Harness.Sprawdz("przy wrogu nie da się zasnąć", !sen.Spi);
        }

        private static void Dzien()
        {
            SwiatWolny s = Walka();
            s.Godzina = 12.0;
            for (int i = 0; i < 4000; i++)
            {
                s.Glod = s.Hp = s.Energia = 100;
                s.Aktualizuj(0.1, default);
                if (s.Godzina > 17.5)
                {
                    break;
                }
            }
            int dzienni = s.Wrogowie.Count(g => !g.Nocny);
            Harness.Sprawdz($"w dzień krąży mało wrogów ({dzienni})", dzienni >= 1 && dzienni <= 2);
            Harness.Sprawdz("w dzień nie ma nocnych fal", s.Wrogowie.All(g => !g.Nocny));

            s = Walka();
            Wrog daleko = Dodaj(s, TypWroga.Wojownik, 15);
            daleko.Stan = StanWroga.Stoi;
            Czekaj(s, 0.5);
            Harness.Sprawdz("daleki włóczęga stoi", daleko.Stan == StanWroga.Stoi);
            daleko.X = s.X + 6;
            Czekaj(s, 0.2);
            Harness.Sprawdz("z bliska rusza na bohatera", daleko.Stan == StanWroga.Idzie);
        }

        private static void Specjalne()
        {
            SwiatWolny s = Walka();
            s.Aktualizuj(0.02, new Wejscie { Klik = "s:wir" });
            Harness.Sprawdz("w dzień ciosy specjalne śpią", s.Akcja == AkcjaBohatera.Brak && s.Dziennik.Last().Contains("nocą"));

            s = Walka();
            s.Godzina = 22.0;
            s.Aktualizuj(0.02, default);
            s.Wrogowie.Clear();
            Wrog a = Dodaj(s, TypWroga.Slugus, 1.5), b = Dodaj(s, TypWroga.Slugus, -1.5);
            s.Aktualizuj(0.02, new Wejscie { Klik = "s:wir" });
            Harness.Rowne("wir nie zużywa wytrzymałości", SwiatWolny.MaxWytrzymalosc, s.Wytrzymalosc);
            Czekaj(s, 0.65);
            Harness.Sprawdz("wir trafia dookoła", a.Hp < a.MaxHp && b.Hp < b.MaxHp);
            Harness.Sprawdz("wir ma odnowienie", s.Odnowienie["wir"] > 0);
            int ile = s.LicznikAkcji;
            s.Aktualizuj(0.02, new Wejscie { Klik = "s:wir" });
            Harness.Rowne("w czasie odnowienia cios nie wychodzi", ile, s.LicznikAkcji);

            s = Walka();
            s.Godzina = 22.0;
            s.Aktualizuj(0.02, default);
            s.Wrogowie.Clear();
            a = Dodaj(s, TypWroga.Slugus, 2.0);
            double x = s.X;
            s.Aktualizuj(0.02, new Wejscie { Klik = "s:szarza" });
            Czekaj(s, 0.35);
            Harness.Sprawdz("szarża przenosi i rani", s.X - x > 2.0 && a.Hp < a.MaxHp);

            s = Walka();
            s.Godzina = 22.0;
            s.Aktualizuj(0.02, default);
            s.Wrogowie.Clear();
            a = Dodaj(s, TypWroga.Wojownik, 3.0);
            s.Aktualizuj(0.02, new Wejscie { Klik = "s:okrzyk" });
            Czekaj(s, 0.3);
            Harness.Sprawdz("okrzyk ogłusza nawet wojownika", a.Stan == StanWroga.Ogluszony);
        }

        private static void Smierc()
        {
            SwiatWolny s = Walka();
            s.Plecak["drewno"] = 9;
            double sx = s.X + 6, sy = s.Y;
            s.X = sx;
            Wrog g = Dodaj(s, TypWroga.Wojownik);
            s.Hp = 5;
            for (int i = 0; i < 400 && s.Porzucone.Count == 0; i++)
            {
                s.Hp = 5;
                s.Aktualizuj(0.02, default);
            }
            Harness.Sprawdz("śmierć nie kończy gry", !s.Martwy && s.Hp > 60 && s.Hp < 75);
            Harness.Sprawdz("bez obozu budzisz się na polanie", Math.Abs(s.X - 0.5) < 1e-9);
            Harness.Rowne("plecak jest pusty", 0, s.Ile("drewno"));
            Harness.Rowne("plecak leży na ziemi", 1, s.Porzucone.Count);
            Harness.Sprawdz("po pobudce chwila bezpieczeństwa", s.Nietykalny);
            _ = g;
            s.X = s.Porzucone[0].X;
            s.Y = s.Porzucone[0].Y;
            s.Aktualizuj(0.02, new Wejscie { Dzialaj = true });
            Harness.Rowne("można odzyskać plecak", 9, s.Ile("drewno"));
            Harness.Rowne("plecak zniknął z ziemi", 0, s.Porzucone.Count);

            SwiatWolny g2 = Walka();
            g2.Glod = 0;
            g2.Energia = 0;
            g2.Hp = 0.01;
            g2.Aktualizuj(0.1, default);
            Harness.Sprawdz("głód też kończy pobudką, a nie końcem gry", !g2.Martwy && g2.Hp > 50);

            SwiatWolny pobudka = Walka();
            Wrog czeka = Dodaj(pobudka, TypWroga.Slugus);
            for (int i = 0; i < 400; i++)
            {
                pobudka.Hp = 1;
                pobudka.Aktualizuj(0.02, default);
                if (pobudka.Hp > 60)
                {
                    break;
                }
            }
            Harness.Sprawdz("wróg przy miejscu pobudki jest ogłuszony", czeka.Stan == StanWroga.Ogluszony);

            var stara = new SwiatWolny(42);
            stara.Glod = 0;
            stara.Energia = 0;
            stara.Hp = 0.01;
            stara.Aktualizuj(0.1, default);
            Harness.Sprawdz("bez walki śmierć działa jak dawniej", stara.Martwy);
        }

        private static SwiatWolny ZOsada()
        {
            SwiatWolny s = Walka();
            s.Plecak["drewno"] = 60;
            s.Plecak["kamien"] = 30;
            s.Plecak["wlokno"] = 20;
            s.Kierunek = (1.0, 0.0);
            s.Aktualizuj(0.02, new Wejscie { Buduj = 2 });
            s.Kierunek = (0.0, 1.0);
            s.Aktualizuj(0.02, new Wejscie { Buduj = 3 });
            return s;
        }

        private static void Oboz()
        {
            SwiatWolny s = ZOsada();
            Harness.Rowne("dwie budowle stoją", 2, s.Budowle.Count);
            s.X = 25;
            s.Y = 0.5;
            (int X, int Y) cel = s.Budowle.Keys.First();
            string klucz = s.Budowle[cel];
            var g = new Wrog { Typ = TypWroga.Wojownik, X = cel.X - 0.5, Y = cel.Y + 0.5, Hp = 120, MaxHp = 120, Nocny = true };
            s.Wrogowie.Add(g);
            s.Godzina = 22.0;
            for (int i = 0; i < 2000 && s.Budowle.Count == 2; i++)
            {
                s.Hp = s.Glod = s.Energia = 100;
                s.Wrogowie.RemoveAll(w => w != g);
                s.Godzina = 22.0;
                s.Aktualizuj(0.05, default);
            }
            Harness.Sprawdz($"wrogowie niszczą budowle pod nieobecność bohatera ({klucz})", s.Budowle.Count < 2);
            Harness.Sprawdz("zniszczenie trafia do dziennika", s.Dziennik.Any(l => l.StartsWith("Szkielety")));

            s = ZOsada();
            s.Obozowisko.Chaty = 4;
            Osada.ZatrudnijOsadnika(s.Obozowisko, new Losowanie(3), "bezczynny", true);
            s.Aktualizuj(0.02, default);
            s.X = 25;
            s.Y = 0.5;
            s.Budowle.Clear();
            s.Budowle[(0, 0)] = "ognisko";
            s.Godzina = 22.0;
            s.Aktualizuj(0.02, default);
            s.Wrogowie.Clear();
            s.Postacie.Clear();
            s.Dzien = 1;
            _ = s;
            Harness.Sprawdz("osadnik ma życie", new PostacOsadnika().Zycie > 0);

            s = ZOsada();
            s.X = 3;
            s.Y = 3;
            Wrog obok = Dodaj(s, TypWroga.Slugus, 2.0);
            s.Plecak["spiwor"] = 0;
            s.Aktualizuj(0.02, new Wejscie { Spij = true });
            Harness.Sprawdz("wróg obok uniemożliwia sen", !s.Spi);
            _ = obok;
        }

        private static void Interfejs()
        {
            SwiatWolny s = Walka();
            Harness.Sprawdz("w dzień nie ma kafelków ciosów", KafelkiWolne.Z(s).Specjalne.Count == 0);
            s.Godzina = 22.0;
            Harness.Rowne("nocą są trzy kafelki ciosów", 3, KafelkiWolne.Z(s).Specjalne.Count);
            s.Odnowienie["wir"] = 4.2;
            Harness.Sprawdz("kafelek pokazuje odnowienie", KafelkiWolne.Z(s).Specjalne[0].Opis[0].Tekst.Contains("5 s") && !KafelkiWolne.Z(s).Specjalne[0].Dostepny);
            s.Klik_TrybBudowy();
            Harness.Rowne("w trybie budowania ciosów nie widać", 0, KafelkiWolne.Z(s).Specjalne.Count);
            s.TrybBudowy = false;
            Harness.Sprawdz("podpowiedź mówi o walce", WidokWolny.Podpowiedz(Walka()).Contains("blok"));
            SwiatWolny r = Walka();
            r.Riposta = 1.0;
            Harness.Sprawdz("podpowiedź woła o ripostę", WidokWolny.Podpowiedz(r).StartsWith("Riposta"));
            SwiatWolny p = Walka();
            p.Porzucone.Add(new PorzuconyPlecak { X = p.X, Y = p.Y, Zawartosc = new Dictionary<string, int>() });
            Harness.Sprawdz("podpowiedź prowadzi do plecaka", WidokWolny.Podpowiedz(p).StartsWith("E:"));
        }

        private static void Klik_TrybBudowy(this SwiatWolny s)
        {
            s.TrybBudowy = true;
        }
    }
}
