using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>„Zomboid fantasy”: świat z kafli, ruch z kolizjami, zbieranie, obóz, głód i sen.</summary>
    public static class TestyWolnego
    {
        private static (int X, int Y)? Znajdz(SwiatWolny s, Obiekt rodzaj, int promien = 60)
        {
            (int X, int Y)? naj = null;
            int d2 = int.MaxValue;
            for (int x = -promien; x <= promien; x++)
            {
                for (int y = -promien; y <= promien; y++)
                {
                    if (s.ObiektNa(x, y) == rodzaj && x * x + y * y < d2)
                    {
                        d2 = x * x + y * y;
                        naj = (x, y);
                    }
                }
            }
            return naj;
        }

        private static void Stoj(SwiatWolny s, double sekundy, Wejscie w = default)
        {
            for (double t = 0; t < sekundy; t += 0.05)
            {
                s.Aktualizuj(0.05, w);
            }
        }

        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: generowanie świata");
            Swiat();
            Harness.Grupa("Zomboid fantasy: ruch i kolizje");
            Ruch();
            Harness.Grupa("Zomboid fantasy: zbieranie");
            Zbieranie();
            Harness.Grupa("Zomboid fantasy: obóz, głód i sen");
            Oboz_();
            Harness.Grupa("Zomboid fantasy: interfejs i menu");
            Interfejs();
        }

        private static void Swiat()
        {
            var a = new GeneratorSwiata(42);
            var b = new GeneratorSwiata(42);
            bool takie = true;
            for (int x = -30; x < 30; x++)
            {
                for (int y = -30; y < 30; y++)
                {
                    takie &= a.ObiektNa(x, y) == b.ObiektNa(x, y) && a.Biom(x, y) == b.Biom(x, y);
                }
            }
            Harness.Sprawdz("ten sam seed daje ten sam świat", takie);
            Harness.Sprawdz("start: polana bez obiektów",
                            Enumerable.Range(-4, 9).All(x => Enumerable.Range(-4, 9).All(y => a.ObiektNa(x, y) == Obiekt.Brak)));
            Harness.Rowne("start leży na równinach", "równiny", a.Biom(0, 0));
            Harness.Rowne("poza granicą jest mgła", Obiekt.Sciana, a.ObiektNa(GeneratorSwiata.Granica + 1, 0));

            var licznik = new Dictionary<Obiekt, int>();
            var biomy = new HashSet<string>();
            for (int x = -240; x < 240; x += 2)
            {
                for (int y = -240; y < 240; y += 2)
                {
                    Obiekt o = a.ObiektNa(x, y);
                    licznik[o] = licznik.Wez(o) + 1;
                    biomy.Add(a.Biom(x, y));
                }
            }
            Harness.Rowne("na dużym obszarze są wszystkie biomy", 6, biomy.Count);
            foreach (Obiekt o in new[] { Obiekt.Drzewo, Obiekt.Skala, Obiekt.Krzak, Obiekt.Ziolo, Obiekt.Woda, Obiekt.Sciana })
            {
                Harness.Sprawdz($"na mapie jest {o}", licznik.Wez(o) > 50);
            }
            int wszystkie = licznik.Values.Sum();
            Harness.Sprawdz("większość kafli jest pusta (da się chodzić)", licznik[Obiekt.Brak] > wszystkie * 0.6);
            Harness.Rowne("świat 2000×2000 kafli", 2001, 2 * GeneratorSwiata.Granica + 1);
        }

        private static void Ruch()
        {
            var s = new SwiatWolny(42);
            double x0 = s.X, y0 = s.Y;
            Stoj(s, 1.0, new Wejscie { Dx = 1, Dy = 0 });
            Harness.BliskoSiebie("bohater idzie z prędkością", SwiatWolny.Predkosc, s.X - x0, 0.2);
            var skosny = new SwiatWolny(42);
            double sx = skosny.X, sy = skosny.Y;
            Stoj(skosny, 1.0, new Wejscie { Dx = 3, Dy = 3 });
            Harness.BliskoSiebie("po przekątnej nie jest szybszy", SwiatWolny.Predkosc,
                                 Math.Sqrt(Math.Pow(skosny.X - sx, 2) + Math.Pow(skosny.Y - sy, 2)), 0.2);

            (int X, int Y)? drzewo = Znajdz(new SwiatWolny(42), Obiekt.Drzewo);
            Harness.Sprawdz("w pobliżu jest drzewo", drzewo.HasValue);
            var t = new SwiatWolny(42);
            t.X = drzewo.Value.X + 0.5 - 2.0;
            t.Y = drzewo.Value.Y + 0.5;
            if (t.Zajety((int)Math.Floor(t.X), (int)Math.Floor(t.Y)))
            {
                t.X -= 1.5;
            }
            Stoj(t, 2.5, new Wejscie { Dx = 1, Dy = 0 });
            Harness.Sprawdz("drzewo zagradza drogę", t.X < drzewo.Value.X + 0.12);

            var skos = new SwiatWolny(42);
            skos.X = drzewo.Value.X + 0.5 - 1.5;
            skos.Y = drzewo.Value.Y + 0.5 + 0.30;
            double yPrzed = skos.Y;
            Stoj(skos, 0.4, new Wejscie { Dx = 1, Dy = 1 });
            Harness.Sprawdz("przy przeszkodzie bohater ślizga się wzdłuż niej (jedna oś idzie dalej)",
                            skos.Y > yPrzed || skos.X > drzewo.Value.X - 1.4);

            var granica = new SwiatWolny(1);
            granica.X = GeneratorSwiata.Granica - 0.4;
            granica.Y = 0.5;
            Stoj(granica, 1.0, new Wejscie { Dx = 1, Dy = 0 });
            Harness.Sprawdz("bohater nie wychodzi poza mapę", granica.X <= GeneratorSwiata.Granica + 1.0);
        }


        /// <summary>Bohater tuż przy najbliższym obiekcie danego rodzaju, który jest jego jedynym celem zbioru.</summary>
        private static SwiatWolny PrzyObiekcie(Obiekt rodzaj, out (int X, int Y) kafel)
        {
            var probny = new SwiatWolny(42);
            var kandydaci = new List<(int X, int Y)>();
            for (int x = -60; x <= 60; x++)
            {
                for (int y = -60; y <= 60; y++)
                {
                    if (probny.ObiektNa(x, y) == rodzaj)
                    {
                        kandydaci.Add((x, y));
                    }
                }
            }
            foreach ((int X, int Y) k in kandydaci.OrderBy(k => k.X * k.X + k.Y * k.Y))
            {
                var s = new SwiatWolny(42);
                s.X = k.X + 0.5 - 1.0;
                s.Y = k.Y + 0.5;
                s.Kierunek = (1.0, 0.0);
                if (!s.Zajety((int)Math.Floor(s.X), (int)Math.Floor(s.Y)) && s.ZnajdzCel() == k)
                {
                    kafel = k;
                    return s;
                }
            }
            throw new InvalidOperationException("brak odosobnionego obiektu");
        }

        private static void Zbieranie()
        {
            SwiatWolny s = PrzyObiekcie(Obiekt.Drzewo, out (int X, int Y) drzewo);
            Stoj(s, 0.45, new Wejscie { Dzialaj = true });
            Harness.Sprawdz("zbieranie trwa — postęp rośnie", s.Zbieranie.HasValue && s.Zbieranie.Value.Postep > 0.3);
            Harness.Rowne("jeszcze nic w plecaku", 0, s.Ile("drewno"));
            Stoj(s, 0.6, new Wejscie { Dzialaj = true });
            Harness.Rowne("po zbiorze są 2 drewna", 2, s.Ile("drewno"));
            Harness.Sprawdz("dziennik opisuje zbiór", s.Dziennik.Any(d => d.Contains("ścinasz drzewo") || d.Contains("Ścinasz drzewo")));
            Harness.Rowne("drzewo ma jeszcze 2 użycia", 2, s.Pozostalo(drzewo.X, drzewo.Y));
            Stoj(s, 4.0, new Wejscie { Dzialaj = true });
            Harness.Rowne("po trzech zbiorach 6 drewna", 6, s.Ile("drewno"));
            Harness.Rowne("drzewo znika", Obiekt.Brak, s.ObiektNa(drzewo.X, drzewo.Y));
            Harness.Sprawdz("można przejść tam, gdzie stało drzewo", !s.Zajety(drzewo.X, drzewo.Y));

            SwiatWolny k = PrzyObiekcie(Obiekt.Krzak, out (int X, int Y) krzak);
            Stoj(k, 4.0, new Wejscie { Dzialaj = true });
            Harness.Sprawdz("krzak daje jagody i włókno", k.Ile("jagody") == 4 && k.Ile("wlokno") == 2);
            Harness.Rowne("ogołocony krzak znika", Obiekt.Brak, k.ObiektNa(krzak.X, krzak.Y));
            for (int doba = 0; doba < 3; doba++)
            {
                k.Glod = k.Energia = k.Hp = 100.0;     // bohater przeżywa — sprawdzamy odrastanie, nie głód
                Stoj(k, SwiatWolny.DlugoscDobyS);
            }
            Harness.Rowne("po dwóch dobach krzak odrasta", Obiekt.Krzak, k.ObiektNa(krzak.X, krzak.Y));

            SwiatWolny z = PrzyObiekcie(Obiekt.Skala, out (int X, int Y) skala);
            z.Kierunek = (-1.0, 0.0);
            Stoj(z, 2.0, new Wejscie { Dzialaj = true });
            Harness.Rowne("skała stoi za plecami — nic nie zbiera", 0, z.Ile("kamien"));
        }

        private static void Interfejs()
        {
            var s = NaPolanie();
            s.Plecak["drewno"] = 3;
            StanHud hud = WidokWolny.Hud(s);
            Harness.Rowne("pasek: życie", 100, hud.Hp);
            Harness.Rowne("pasek: bohater zaczyna rano", true, hud.Data.Contains("07:00"));
            Harness.Rowne("pasek: cztery surowce z ikonami", 4, hud.ListaSurowcow.Count);
            Harness.Sprawdz("plecak pokazuje śpiwór i drewno",
                            WidokWolny.Plecak(s).Any(l => l.Contains("śpiwór")) && WidokWolny.Plecak(s).Any(l => l.Contains("drewna: 3")));
            Harness.Sprawdz("podpowiedź bez celu to klawisze", WidokWolny.Podpowiedz(s).Contains("WASD"));
            SwiatWolny przy = PrzyObiekcie(Obiekt.Drzewo, out (int X, int Y) _);
            Harness.Sprawdz("przy drzewie podpowiedź mówi, co zrobi E", WidokWolny.Podpowiedz(przy).StartsWith("E:"));

            var pulpit = new Pulpit();
            var scena = new GraSimply.Grafika.Wolny.ScenaWolna();
            List<Napis> napisy = pulpit.ZlozWolny(scena, s, 1.0, hud, WidokWolny.Plecak(s), WidokWolny.Dziennik(s), WidokWolny.Podpowiedz(s));
            Harness.Sprawdz("klatka pulpitu ma napisy interfejsu", napisy.Count > 5);
            byte[] dzien = (byte[])pulpit.Klatka.Dane.Clone();
            s.Godzina = 23.0;
            pulpit.ZlozWolny(scena, s, 1.0, WidokWolny.Hud(s), WidokWolny.Plecak(s), WidokWolny.Dziennik(s), "");
            Harness.Sprawdz("noc wygląda inaczej niż dzień", !dzien.SequenceEqual(pulpit.Klatka.Dane));

            var ekran = new Ekran();
            var konsola = new KonsolaTestowa(new[] { "2", "koniec", "0" });
            new Logika.Gra(konsola, ekran, new Losowanie(5)).Uruchom();
            Harness.Sprawdz("menu: tryb przetrwania wraca po „koniec” i gra wychodzi", ekran.Wolny == null
                            && konsola.Wyjscie.Contains("Przetrwanie"));
        }

        private static SwiatWolny NaPolanie()
        {
            var s = new SwiatWolny(42);
            s.X = 0.5;
            s.Y = 0.5;
            s.Kierunek = (1.0, 0.0);
            return s;
        }

        private static void Oboz_()
        {
            var s = NaPolanie();
            Harness.Rowne("start: tylko śpiwór", 1, s.Ile("spiwor"));
            Harness.Rowne("start: nic więcej w plecaku", 1, s.Plecak.Values.Sum());

            s.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            Harness.Sprawdz("ognisko bez składników się nie stawia", s.Budowle.Count == 0
                            && s.Dziennik.Last().Contains("Brakuje"));

            s.Plecak["drewno"] = 13;
            s.Plecak["kamien"] = 3;
            s.Plecak["wlokno"] = 6;
            s.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            (int X, int Y) kafel = s.KaflPrzed();
            Harness.Sprawdz("ognisko stoi przed bohaterem i kosztuje 5 drewna i 3 kamienie",
                            s.Budowle.Count == 1 && s.Ile("drewno") == 8 && s.Ile("kamien") == 0);
            Harness.Sprawdz("ognisko zagradza drogę", s.Zajety(1, 0) || s.Budowle.Keys.All(k => s.Zajety(k.X, k.Y)));

            s.Kierunek = (0.0, 1.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 3 });
            Harness.Sprawdz("namiot stoi (8 drewna, 6 włókna) i obóz jest założony",
                            s.Budowle.ContainsValue("namiot") && s.Ile("drewno") == 0 && s.Ile("wlokno") == 0 && s.ObozZalozony);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 3 });
            Harness.Sprawdz("na zajętym kaflu nie postawisz drugiej budowli", s.Budowle.Count == 2);

            // Głód i zmęczenie.
            var g = NaPolanie();
            Stoj(g, SwiatWolny.DlugoscDobyS / 24.0 * 10);
            Harness.Sprawdz("po 10 godzinach jest głodniej i bardziej zmęczony", g.Glod < 90 && g.Energia < 60);
            g.Plecak["jagody"] = 2;
            double glod = g.Glod;
            g.Aktualizuj(0.01, new Wejscie { Jedz = true });
            Harness.Sprawdz("jagody sycą", g.Glod > glod + 15 && g.Ile("jagody") == 1);

            // Sen w śpiworze.
            var sen = NaPolanie();
            sen.Godzina = 22.0;
            sen.Energia = 30.0;
            sen.Aktualizuj(0.05, new Wejscie { Spij = true });
            Harness.Sprawdz("bez śpiwora na ziemi nie da się spać", !sen.Spi);
            sen.Aktualizuj(0.05, new Wejscie { Buduj = 1 });
            sen.X = sen.KaflPrzed().X + 0.5 - 1.0;
            sen.Aktualizuj(0.05, new Wejscie { Spij = true });
            Harness.Sprawdz("przy rozłożonym śpiworze bohater zasypia", sen.Spi);
            double godzinaPrzed = sen.Godzina;
            Stoj(sen, 3.0);
            Harness.Sprawdz("sen przyspiesza czas", sen.Godzina - godzinaPrzed > 2.0 || sen.Dzien > 1);
            Stoj(sen, 20.0);
            Harness.Sprawdz("rano bohater jest wypoczęty i nie śpi", !sen.Spi && sen.Energia > 60 && sen.Dzien == 2);

            var noc = new SwiatWolny(1);
            noc.Godzina = 23.0;
            Harness.Sprawdz("noc jest ciemna, południe jasne", noc.Jasnosc < 0.2 && NaPolanie().Jasnosc > 0.99);

            var smierc = NaPolanie();
            Stoj(smierc, 6.0 * SwiatWolny.DlugoscDobyS);
            Harness.Sprawdz("głód i brak sił zabijają", smierc.Martwy);
        }
    }
}
