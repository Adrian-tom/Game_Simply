using System;
using System.IO;
using System.Linq;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Etapy M0 i M1 trybu miasta: przejście, kamera, drogi, domy i skład z zasięgiem.</summary>
    public static class TestyTrybuMiasta
    {
        public static void Uruchom()
        {
            Harness.Grupa("Tryb miasta: przejście, drogi i domy");
            Przejscie();
            Kamera();
            Stawianie();
            Domy();
            Zasieg();
            Wyburzanie();
            Zapis();
            Interfejs();
        }

        /// <summary>Świat już na szczeblu Miasto: ten sam skrót tworzy zapis startowy do ręcznych prób.</summary>
        private static SwiatWolny Miasto()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true, Godzina = 9.0, ObozZalozony = true };
            s.Szczebel = SwiatWolny.Szczeble.Length - 1;
            s.Budowle[(-10, -10)] = "ognisko";
            s.Budowle[(-12, -10)] = "namiot";
            s.Plecak["drewno"] = 5000;
            s.Plecak["kamien"] = 3000;
            s.KameraX = s.X;
            s.KameraY = s.Y;
            return s;
        }

        /// <summary>dotnet run -- --zapis-miasta plik [domów]: start na szczeblu Miasto, opcjonalnie z gotową zabudową do pomiaru klatek.</summary>
        public static int ZapisStartowy(string plik, int domow)
        {
            SwiatWolny s = Miasto();
            for (int i = 0; i < domow; i++)
            {
                int kolumna = i % 20, rzad = i / 20;
                Poloz(s, "droga", 8 + kolumna * 3, 7 + rzad * 3);
                Poloz(s, "droga", 9 + kolumna * 3, 7 + rzad * 3);
                Poloz(s, "droga", 10 + kolumna * 3, 7 + rzad * 3);
                Poloz(s, "dom", 8 + kolumna * 3, 8 + rzad * 3);
            }
            if (domow > 0)
            {
                Poloz(s, "sklad", 3, 4);
                Poloz(s, "droga", 5, 7);
                Poloz(s, "droga", 6, 7);
                Poloz(s, "droga", 7, 7);
            }
            File.WriteAllText(plik, s.DoZapisu());
            Console.WriteLine($"Zapisano {plik}: domów {s.Domy.Count}, dróg {s.Drogi.Count}.");
            return 0;
        }

        private static bool Poloz(SwiatWolny s, string klucz, int x, int y)
        {
            return s.PostawNa(klucz, x, y);
        }

        private static void Krok(SwiatWolny s, double sekund, Wejscie w = default)
        {
            for (double t = 0; t < sekund; t += 0.1)
            {
                s.Aktualizuj(0.1, w);
            }
        }

        private static void Przejscie()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true, Godzina = 22.0, ObozZalozony = true };
            s.Szczebel = 2;
            foreach (string b in SwiatWolny.Szczeble[3].Budynki)
            {
                s.Budowle[(30 + b.Length, 30)] = b;
            }
            s.Budowle[(-10, -10)] = "ognisko";
            s.Wrogowie.Add(new Wrog { Typ = TypWroga.Slugus, X = 5, Y = 5, Hp = 28, MaxHp = 28, Nocny = true });
            for (int i = 0; i < SwiatWolny.LudziNaMiasto; i++)
            {
                s.Obozowisko.Osadnicy.Add(new Osadnik { Imie = "Os" + i, Zajecie = "bezczynny", Morale = 60 });
            }
            s.Obozowisko.Zloto = 200;
            Harness.Sprawdz("przed awansem to jeszcze nie miasto", !s.TrybMiasta);
            s.X = 3.5;
            s.Y = 4.5;
            s.Aktualizuj(0.02, new Wejscie());
            Harness.Sprawdz("warunki spełnione jeszcze bez przeliczenia nie zmieniają trybu", !s.TrybMiasta);
            s.PostawNa("spiwor", 3, 6);
            Harness.Sprawdz("awans sam przełącza w tryb miasta", s.TrybMiasta);
            Harness.Sprawdz("gra przechodzi od razu na kamerę", s.KameraMiejska);
            Harness.BliskoSiebie("kamera startuje nad bohaterem", 3.5, s.KameraX);
            Harness.Sprawdz("dziennik ogłasza założenie miasta", s.Dziennik.Any(l => l.Contains("Założono miasto")));
            Harness.Rowne("panel budowy otwiera się na zakładce Miasto", "miasto", s.Kategoria);
            Harness.Sprawdz("szkielety z fali znikają", s.Wrogowie.All(g => g.Stan == StanWroga.Martwy));
            int wrogow = s.Wrogowie.Count(g => g.Stan != StanWroga.Martwy);
            Krok(s, 30);
            Harness.Rowne("w mieście nie ma nocnych fal", wrogow, s.Wrogowie.Count(g => g.Stan != StanWroga.Martwy));
        }

        private static void Kamera()
        {
            SwiatWolny s = Miasto();
            double hx = s.X, hy = s.Y, glod = s.Glod, energia = s.Energia;
            Krok(s, 2.0, new Wejscie { Dx = 1, Dy = 0 });
            Harness.Sprawdz("WASD przesuwa kamerę", s.KameraX > hx + 15);
            Harness.BliskoSiebie("bohater stoi w miejscu", hx, s.X);
            Harness.BliskoSiebie("bohater stoi w miejscu (oś Y)", hy, s.Y);
            Harness.BliskoSiebie("bohater nie głodnieje", glod, s.Glod);
            Harness.BliskoSiebie("bohater się nie męczy", energia, s.Energia);

            s.Aktualizuj(0.02, new Wejscie { Klik = "a:bohater" });
            Harness.Sprawdz("H wzywa bohatera", s.SterujeBohaterem && !s.KameraMiejska);
            Harness.BliskoSiebie("kamera wraca do bohatera", s.X, s.KameraX);
            Krok(s, 1.0, new Wejscie { Dx = -1, Dy = -1 });
            Harness.Sprawdz("teraz WASD rusza bohaterem", s.X < hx - 1);
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:bohater" });
            Harness.Sprawdz("H wraca do kamery", s.KameraMiejska);

            int jagody = s.Razem("jagody");
            s.Glod = 50;
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:jedz" });
            Harness.BliskoSiebie("w trybie kamery polecenia bohatera nie działają", 50.0, s.Glod, 0.01);
            Harness.Rowne("i nic nie schodzi z zapasów", jagody, s.Razem("jagody"));

            var przed = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true };
            przed.Aktualizuj(0.02, new Wejscie { Klik = "a:bohater" });
            Harness.Sprawdz("przed miastem klawisz H nic nie robi", !przed.SterujeBohaterem && !przed.TrybMiasta);
        }

        private static void Stawianie()
        {
            var obozowy = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true };
            Harness.Sprawdz("dom przed miastem jest zablokowany", obozowy.PowodBudowy("dom", 2, 2).Contains("miasto"));

            SwiatWolny s = Miasto();
            Harness.Sprawdz("daleko od bohatera też wolno budować", s.PowodBudowy("dom", 200, 200) == null || s.ObiektNa(200, 200) == Obiekt.Woda);
            Harness.Rowne("dom zajmuje 2×2", 2, SwiatWolny.Stopa("dom"));
            Harness.Rowne("skład zajmuje 3×3", 3, SwiatWolny.Stopa("sklad"));

            (int x, int y) = WolnaLaka(s, 3);
            int drewno = s.Razem("drewno");
            Harness.Sprawdz("dom staje", s.PostawNa("dom", x, y));
            Harness.Rowne("dom kosztuje drewno", drewno - 12, s.Razem("drewno"));
            Harness.Sprawdz("cała stopa domu jest zajęta", s.Zajety(x + 1, y + 1) && s.Zajety(x, y));
            Harness.Sprawdz("na stopie nie postawisz drogi", s.PowodBudowy("droga", x + 1, y) != null);
            Harness.Sprawdz("dom nie wejdzie na inny dom", s.PowodBudowy("dom", x + 1, y + 1) != null);
            Harness.Sprawdz("skład sprawdza wszystkie swoje kafle", s.PowodBudowy("sklad", x - 2, y - 2) != null);

            (int dx, int dy) = (x + 3, y);
            Harness.Sprawdz("droga staje", s.PostawNa("droga", dx, dy));
            Harness.Sprawdz("po drodze da się chodzić", !s.Zajety(dx, dy));

            (int lx, int ly) = NajblizszyObiekt(s, Obiekt.Drzewo);
            Harness.Sprawdz("w mieście las nie blokuje budowy", s.PowodBudowy("droga", lx, ly) == null);
            s.PostawNa("droga", lx, ly);
            Harness.Rowne("drzewo pod drogą zostaje wycięte", Obiekt.Brak, s.ObiektNa(lx, ly));

            (int ox, int oy) = WolnaLaka(s, 8);
            s.PolozOdcinekDrogi((ox, oy), (ox + 3, oy + 2));
            Harness.Sprawdz("ciągnięta droga nie ma dziur po skosie", s.Drogi.Contains((ox + 1, oy)) && s.Drogi.Contains((ox + 3, oy)) && s.Drogi.Contains((ox + 3, oy + 1)) && s.Drogi.Contains((ox + 3, oy + 2)));

            var biedny = Miasto();
            biedny.Plecak["kamien"] = 0;
            Harness.Sprawdz("bez kamienia nie ma drogi", biedny.PowodBudowy("droga", 5, 5) != null && !biedny.PostawNa("droga", 5, 5));
        }

        private static (int X, int Y) WolnaLaka(SwiatWolny s, int bok)
        {
            for (int r = 3; r < 200; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    bool wolne = true;
                    for (int a = -1; a <= bok + 3 && wolne; a++)
                    {
                        for (int b = -1; b <= bok + 3 && wolne; b++)
                        {
                            Obiekt o = s.ObiektNa(x + a, r + b);
                            wolne = o != Obiekt.Woda && o != Obiekt.Sciana && !s.Budowle.ContainsKey((x + a, r + b));
                        }
                    }
                    if (wolne)
                    {
                        return (x, r);
                    }
                }
            }
            throw new InvalidOperationException("Brak wolnej łąki.");
        }

        private static (int X, int Y) NajblizszyObiekt(SwiatWolny s, Obiekt o)
        {
            for (int r = 1; r < 200; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (s.ObiektNa(x, r) == o)
                    {
                        return (x, r);
                    }
                }
            }
            throw new InvalidOperationException("Brak obiektu.");
        }

        private static void Domy()
        {
            SwiatWolny s = Miasto();
            (int x, int y) = WolnaLaka(s, 8);
            s.PostawNa("dom", x, y);
            Krok(s, 30);
            Harness.Rowne("dom bez drogi stoi pusty", 0, s.Mieszkancy);
            Harness.Sprawdz("cel mówi o braku składu", CeleWolne.Aktualny(s).Id == "m-sklad");

            s.PostawNa("sklad", x + 4, y);
            Krok(s, 10);
            Harness.Rowne("sam skład bez drogi nie wystarczy", 0, s.Mieszkancy);
            Harness.Rowne("cel każe połączyć dom", "m-droga", CeleWolne.Aktualny(s).Id);

            s.PostawNa("droga", x + 2, y);
            s.PostawNa("droga", x + 3, y);
            Harness.Sprawdz("droga łączy dom ze składem", s.DomPodlaczony((x, y)));
            Krok(s, 8);
            Harness.Sprawdz("z drogą dom się zapełnia", s.Mieszkancy > 0);
            Krok(s, 60);
            Harness.Rowne("dom mieści tylu, ile ma miejsc", SwiatWolny.PojemnoscDomu, s.Mieszkancy);
            Harness.Sprawdz("pasek stanu liczy mieszkańców", WidokWolny.Hud(s).Region.Contains((SwiatWolny.PojemnoscDomu + s.Obozowisko.Osadnicy.Count).ToString()));

            s.PostawNa("wyburz", x + 2, y);
            Harness.Sprawdz("przerwana droga odcina dom", !s.DomPodlaczony((x, y)));
            Krok(s, 20);
            Harness.Sprawdz("odcięty dom pustoszeje", s.Mieszkancy < SwiatWolny.PojemnoscDomu);
        }

        private static void Zasieg()
        {
            SwiatWolny s = Miasto();
            (int x, int y) = WolnaLaka(s, 3);
            s.PostawNa("sklad", x, y);
            for (int i = 0; i <= SwiatWolny.ZasiegSkladu + 5; i++)
            {
                s.PostawNa("droga", x + 3 + i, y);
            }
            Harness.Sprawdz("droga przy składzie jest w zasięgu", s.DrogaWZasiegu(x + 3, y));
            Harness.Sprawdz("droga na granicy jest w zasięgu", s.DrogaWZasiegu(x + 3 + SwiatWolny.ZasiegSkladu, y));
            Harness.Sprawdz("droga za granicą jest poza zasięgiem", !s.DrogaWZasiegu(x + 3 + SwiatWolny.ZasiegSkladu + 1, y));
        }

        private static void Wyburzanie()
        {
            SwiatWolny s = Miasto();
            (int x, int y) = WolnaLaka(s, 3);
            s.PostawNa("dom", x, y);
            Harness.Sprawdz("nie wyburzysz pustego miejsca", s.PowodBudowy("wyburz", x + 5, y + 5) != null);
            Harness.Sprawdz("wyburzyć można od każdego kafla budynku", s.PostawNa("wyburz", x + 1, y + 1));
            Harness.Sprawdz("dom znika w całości", !s.Domy.ContainsKey((x, y)) && !s.Budowle.ContainsKey((x, y)) && !s.Zajety(x + 1, y + 1));
        }

        private static void Zapis()
        {
            SwiatWolny s = Miasto();
            (int x, int y) = WolnaLaka(s, 8);
            s.PostawNa("dom", x, y);
            s.PostawNa("sklad", x + 4, y);
            s.PostawNa("droga", x + 2, y);
            s.PostawNa("droga", x + 3, y);
            Krok(s, 12);
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:bohater" });
            SwiatWolny w = SwiatWolny.ZZapisu(s.DoZapisu());
            Harness.Sprawdz("po wczytaniu to dalej miasto", w.TrybMiasta);
            Harness.Sprawdz("zapis pamięta sterowanie bohaterem", w.SterujeBohaterem);
            Harness.Rowne("zapis pamięta drogi", 2, w.Drogi.Count);
            w.Aktualizuj(0.02, new Wejscie { Klik = "a:buduj" });
            Harness.Sprawdz("po wczytaniu panel budowy nie jest pusty", KafelkiWolne.Z(w).Budowle.Count > 0);
            Harness.Rowne("zapis pamięta mieszkańców", s.Mieszkancy, w.Mieszkancy);
            Harness.Sprawdz("po wczytaniu stopa budynku jest zajęta", w.Zajety(x + 1, y + 1));
            Harness.Sprawdz("po wczytaniu sieć dróg działa", w.DomPodlaczony((x, y)));
        }

        private static void Interfejs()
        {
            SwiatWolny s = Miasto();
            KafelkiWolne k = KafelkiWolne.Z(s);
            Harness.Sprawdz("pasek kamery: buduj, rada, bohater", k.Akcje.Select(a => a.Id).SequenceEqual(new[] { "a:buduj", "a:osada", "a:bohater" }));
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:bohater" });
            Harness.Sprawdz("pasek bohatera ma też jedzenie i sen", KafelkiWolne.Z(s).Akcje.Any(a => a.Id == "a:jedz"));
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:bohater" });
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:buduj" });
            s.Aktualizuj(0.02, new Wejscie { Klik = "k:miasto" });
            k = KafelkiWolne.Z(s);
            Harness.Sprawdz("zakładka Miasto ma drogę, dom, skład, wyburzanie", k.Budowle.Select(b => b.Id).SequenceEqual(new[] { "b:droga", "b:dom", "b:sklad", "b:wyburz" }));
            Harness.Rowne("cztery zakładki", 4, k.Kategorie.Count);
            Harness.Sprawdz("podpowiedź mówi o wyborze budowli", WidokWolny.Podpowiedz(s).Contains("Wybierz"));
            s.Wycofaj();
            Harness.Sprawdz("podpowiedź kamery", WidokWolny.Podpowiedz(s).Contains("kamera") || WidokWolny.Podpowiedz(s).Contains("Założono"));
            s.PanelOtwarty = true;
            Harness.Sprawdz("panel to teraz rada miasta", WidokWolny.Panel(s)[0].StartsWith("RADA"));
        }
    }
}
