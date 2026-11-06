using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Grafika.Wolny;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Podpowiedzi celu i bot, który gra według nich: czy prowadzą od zera do namiotu przed nocą.</summary>
    public static class TestyCelow
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: cele dla gracza i bot");
            Kolejnosc();
            PoNocy();
            Pilne();
            Plakietka();
            ZbieranieObokPlacu();
            UwagaSzkieletow();
            Bot();
        }

        private static SwiatWolny Swiat()
        {
            return new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true };
        }

        private static string Id(SwiatWolny s)
        {
            return CeleWolne.Aktualny(s)?.Id;
        }

        private static (int X, int Y) Wolny(SwiatWolny s)
        {
            for (int r = 1; r < 8; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        if (s.PowodBudowy("palisada", x, y) == null && !s.Budowle.ContainsKey((x, y)))
                        {
                            return (x, y);
                        }
                    }
                }
            }
            throw new System.InvalidOperationException("brak wolnego kafla");
        }

        private static void Kolejnosc()
        {
            SwiatWolny s = Swiat();
            Harness.Rowne("na starcie: zbierz drewno i kamień", "zbierz-ognisko", Id(s));
            Harness.Sprawdz("cel pokazuje postęp", CeleWolne.Aktualny(s).Etykieta.Contains("0/5"));
            Harness.Rowne("numer pierwszego celu", 1, CeleWolne.Aktualny(s).Numer);

            s.Plecak["drewno"] = 5;
            s.Plecak["kamien"] = 3;
            Harness.Rowne("mając składniki: postaw ognisko", "ognisko", Id(s));

            (int X, int Y) o = Wolny(s);
            Harness.Sprawdz("ognisko stoi", s.PostawNa("ognisko", o.X, o.Y));
            Harness.Rowne("po ognisku: drewno na namiot", "zbierz-namiot", Id(s));

            s.Plecak["drewno"] = 30;
            Harness.Rowne("mając drewno: postaw plac", "plac-namiot", Id(s));

            s.Plecak["drewno"] = 0;
            (int X, int Y) n = Wolny(s);
            Harness.Sprawdz("plac stoi", s.PostawNa("namiot", n.X, n.Y));
            Harness.Rowne("plac bez drewna: wraca do zbierania", "zbierz-namiot", Id(s));

            s.Plecak["drewno"] = 30;
            Harness.Rowne("plac z drewnem: buduj szkielet", "szkielet", Id(s));

            PlacBudowy plac = s.Place.Values.Single();
            plac.Etap = 1;
            Harness.Rowne("szkielet gotowy, brak włókna: zbieraj", "wlokno", Id(s));
            s.Plecak["wlokno"] = 20;
            Harness.Rowne("mając włókno: płótno", "plotno", Id(s));

            s.Place.Clear();
            s.Budowle[(n.X, n.Y)] = "namiot";
            Harness.Rowne("namiot stoi: szykuj się na noc", "noc", Id(s));
            s.Godzina = 22.0;
            Harness.Rowne("w nocy dalej cel nocy", "noc", Id(s));
            Harness.Sprawdz("w nocy tekst mówi o walce", CeleWolne.Aktualny(s).Tekst.Contains("przetrwaj"));

            s.Budowle.Remove((n.X, n.Y));
            Harness.Rowne("szkielety zniszczyły namiot: cel wraca sam", "plac-namiot", Id(s));

            var stary = new SwiatWolny(42);
            Harness.Sprawdz("bez trybu przetrwania nie ma celów", CeleWolne.Aktualny(stary) == null);
        }

        private static SwiatWolny ObozPoNocy()
        {
            SwiatWolny s = Swiat();
            s.Budowle[(2, 0)] = "ognisko";
            s.Budowle[(2, 2)] = "namiot";
            s.Dzien = 2;
            s.Godzina = 8.0;
            return s;
        }

        private static void PoNocy()
        {
            SwiatWolny s = ObozPoNocy();
            Harness.Rowne("po pierwszej nocy: palisada", "palisada", Id(s));

            s.Gosc = new Przybysz { O = new Osadnik { Imie = "Test", Zajecie = "bezczynny" }, Surowiec = "drewno", Ile = 1 };
            Harness.Rowne("przybysz ma pierwszeństwo", "przybysz", Id(s));
            s.Gosc = null;

            s.Obozowisko.Osadnicy.Add(new Osadnik { Imie = "Test", Zajecie = "bezczynny" });
            Harness.Rowne("osadnik bez pracy: daj zajęcie", "zajecie", Id(s));
            s.Obozowisko.Osadnicy[0].Zajecie = "drwal";

            for (int i = 0; i < CeleWolne.KafliPalisady; i++)
            {
                s.Budowle[(10 + i, 10)] = "palisada";
            }
            Harness.Rowne("palisada stoi: tartak", "tartak", Id(s));
            s.Budowle[(20, 20)] = "tartak";
            Harness.Rowne("tartak stoi: farma", "farma", Id(s));
            s.Budowle[(22, 20)] = "farma";
            Harness.Rowne("farma stoi: ktoś musi na niej pracować", "rolnik", Id(s));
            s.Obozowisko.Osadnicy[0].Zajecie = "rolnik";
            Harness.Rowne("brak wolnych miejsc: najpierw chata", "chata", Id(s));
            s.Budowle[(24, 20)] = "chata";
            s.Budowle[(26, 20)] = "chata";
            s.Obozowisko.Chaty = 8;
            Harness.Rowne("są miejsca: szczebel wioski", "szczebel", Id(s));
            Harness.Sprawdz("cel mówi, ilu ludzi brakuje", CeleWolne.Aktualny(s).Tekst.Contains("ludzie 1/4"));
            s.Szczebel = 1;
            Harness.Rowne("wioska: spichlerz", "spichlerz", Id(s));
            s.Budowle[(28, 20)] = "spichlerz";
            Harness.Rowne("potem tawerna", "tawerna", Id(s));
            s.Budowle[(30, 20)] = "tawerna";
            s.Szczebel = 2;
            Harness.Rowne("miasteczko: lecznica", "lecznica", Id(s));
            s.Budowle[(32, 20)] = "lecznica";
            Harness.Rowne("potem targ", "targ", Id(s));
            s.Budowle[(34, 20)] = "targ";
            Harness.Rowne("droga do miasta", "szczebel", Id(s));
            Harness.Sprawdz("cel mówi o złocie", CeleWolne.Aktualny(s).Tekst.Contains("złoto"));
            Harness.Sprawdz("najdłuższy cel się mieści: " + CeleWolne.Aktualny(s).Etykieta, CeleWolne.Aktualny(s).Etykieta.Length <= 56);
            s.Szczebel = 3;
            Harness.Rowne("miasto założone: zaczynają się cele miejskie", "m-sklad", Id(s));
        }

        private static void Pilne()
        {
            SwiatWolny s = ObozPoNocy();
            s.Glod = 20.0;
            Harness.Rowne("głód bez jedzenia: jagody", "jagody", Id(s));
            Harness.Rowne("sprawa pilna nie ma numeru", 0, CeleWolne.Aktualny(s).Numer);
            s.Plecak["jagody"] = 2;
            Harness.Rowne("głód z jedzeniem: zjedz", "jedz", Id(s));
            s.Plecak["jagody"] = 0;
            s.Obozowisko.Surowce["zywnosc"] = 5;
            s.X = 40.5;
            Harness.Rowne("zapasy daleko: gra nie każe jeść, tylko wrócić", "do-zapasow", Id(s));
            Harness.Sprawdz("kafelek Jedz jest wyszarzony z dala od zapasów", !KafelkiWolne.Z(s).Akcje.Single(a => a.Id == "a:jedz").Dostepny);
            s.X = 0.5;
            Harness.Rowne("przy obozie można jeść z zapasów", "jedz", Id(s));
            s.Obozowisko.Surowce["zywnosc"] = 0;
            s.Glod = 100.0;
            s.Energia = 10.0;
            Harness.Rowne("zmęczenie: spać", "spij", Id(s));
            s.Energia = 100.0;
            s.Porzucone.Add(new PorzuconyPlecak { X = 5, Y = 5, Zawartosc = new Dictionary<string, int>() });
            Harness.Rowne("porzucony plecak: najpierw on", "plecak", Id(s));
            s.Godzina = 22.0;
            Harness.Sprawdz("nocą plecak nie jest pilny", Id(s) != "plecak");
            s.Godzina = 8.0;
            var bezObozu = Swiat();
            bezObozu.Porzucone.Add(new PorzuconyPlecak { X = 5, Y = 5, Zawartosc = new Dictionary<string, int>() });
            Harness.Rowne("bez obozu najpierw odbudowa, nie plecak", "zbierz-ognisko", Id(bezObozu));
            s.Porzucone.Clear();
            s.Energia = 10.0;
            s.Budowle.Clear();
            Harness.Sprawdz("zmęczony bez posłania dowiaduje się o śpiworze", CeleWolne.Aktualny(s).Tekst.Contains("śpiwór"));
        }

        private static void Plakietka()
        {
            SwiatWolny s = Swiat();
            var pulpit = new Pulpit();
            var scena = new ScenaWolna();
            pulpit.CelGracza = CeleWolne.Aktualny(s).Etykieta;
            List<Napis> napisy = pulpit.ZlozWolny(scena, s, 1.0, WidokWolny.Hud(s), WidokWolny.Plecak(s), WidokWolny.Dziennik(s), "");
            Harness.Sprawdz("plakietka celu jest na ekranie", napisy.Any(n => n.Tekst.Contains("Cel 1/")));

            pulpit.CelGracza = "x";
            napisy = pulpit.ZlozWolny(scena, s, 1.0, WidokWolny.Hud(s), WidokWolny.Plecak(s), WidokWolny.Dziennik(s), "", new List<string> { "Panel" });
            Harness.Sprawdz("otwarty panel chowa plakietkę", !napisy.Any(n => n.Tekst == "x"));

            // Najdłuższe teksty muszą zmieścić się między plecakiem a dziennikiem.
            var stany = new List<SwiatWolny> { Swiat(), ObozPoNocy() };
            stany[1].Energia = 10.0;
            stany[1].Budowle.Remove((2, 2));
            foreach (SwiatWolny st in stany)
            {
                Harness.Sprawdz("cel mieści się w 56 znakach: " + CeleWolne.Aktualny(st).Etykieta, CeleWolne.Aktualny(st).Etykieta.Length <= 56);
            }
        }

        private static void ZbieranieObokPlacu()
        {
            SwiatWolny s = Swiat();
            for (int x = -30; x < 30; x++)
            {
                for (int y = -30; y < 30; y++)
                {
                    if (s.ObiektNa(x, y) == Obiekt.Drzewo && !s.Zajety(x + 1, y) && !s.Zajety(x + 2, y) && s.ObiektNa(x + 1, y) == Obiekt.Brak && s.ObiektNa(x + 2, y) == Obiekt.Brak)
                    {
                        s.X = x + 1.5;
                        s.Y = y + 0.5;
                        s.Kierunek = (-1.0, 0.0);
                        Harness.Sprawdz("plac bez materiałów stoi obok drzewa", s.PostawNa("namiot", x + 2, y));
                        for (int i = 0; i < 25; i++)
                        {
                            s.Aktualizuj(0.05, new Wejscie { Dzialaj = true });
                        }
                        Harness.Rowne("bez materiałów E ścina drzewo obok placu", 2, s.Razem("drewno"));
                        return;
                    }
                }
            }
            Harness.Sprawdz("znaleziono drzewo do testu", false);
        }

        /// <summary>Gdzie pójdzie nocny szkielet stojący przy ognisku, gdy bohater jest o tyle kafli od niego.</summary>
        private static Wrog SzkieletPrzyOgnisku(double odleglosc)
        {
            SwiatWolny s = Swiat();
            s.Godzina = 22.0;
            s.Budowle[(0, 0)] = "ognisko";
            var g = new Wrog { Typ = TypWroga.Slugus, X = 0.5, Y = 2.0, Hp = 28, MaxHp = 28, Nocny = true };
            s.Wrogowie.Add(g);
            s.X = 0.5 + odleglosc;
            s.Y = 2.0;
            s.Aktualizuj(0.05, new Wejscie());
            return g;
        }

        private static void UwagaSzkieletow()
        {
            Harness.Sprawdz("bohater 12 kafli dalej: szkielet idzie na niego", SzkieletPrzyOgnisku(12.0).CelBudowla == null);
            Harness.Sprawdz("bohater 20 kafli dalej: szkielet idzie na budowlę", SzkieletPrzyOgnisku(20.0).CelBudowla == (0, 0));
        }

        private static void Bot()
        {
            foreach (ProfilBota profil in ProfilBota.Wszystkie)
            {
                foreach (long seed in new[] { 42L, 1000L, 8919L })
                {
                    WynikRozgrywki w = SymulacjaWolna.Rozegraj(seed, profil, 1);
                    Harness.Sprawdz($"bot {profil.Nazwa}, świat {seed}: namiot przed 21:00", w.NamiotGodzina < SwiatWolny.NocOd);
                }
            }
            WynikRozgrywki a = SymulacjaWolna.Rozegraj(42, ProfilBota.Sprawny, 2);
            WynikRozgrywki b = SymulacjaWolna.Rozegraj(42, ProfilBota.Sprawny, 2);
            Harness.Sprawdz("ta sama rozgrywka bota daje ten sam wynik", a.NamiotSekunda == b.NamiotSekunda && a.Zabici == b.Zabici);
            Harness.Sprawdz("bot przeszedł pierwszą noc i walczył", a.Zabici > 0);
        }
    }
}
