using System.Linq;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Szczeble osady i budynki miejskie: odblokowanie, poziomy, gospodarka, zapis.</summary>
    public static class TestyMiasta
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: droga do miasta");
            KazdaBudowla();
            Odblokowanie();
            Awans();
            Gospodarka();
            Zapis();
            Stacja();
        }

        private static SwiatWolny Swiat()
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true, Godzina = 8.0, ObozZalozony = true };
            s.Budowle[(5, 5)] = "ognisko";
            s.Budowle[(7, 5)] = "namiot";
            s.X = 0.5;
            s.Y = 0.5;
            return s;
        }

        /// <summary>Stawia gotowy budynek przez zwykłą budowę etapami: plac, materiały, praca.</summary>
        private static void Zbuduj(SwiatWolny s, string klucz)
        {
            foreach (var m in SwiatWolny.KosztCalkowity(klucz))
            {
                s.Plecak[m.Key] = s.Ile(m.Key) + m.Value;
            }
            for (int x = -5; x <= 5; x++)
            {
                for (int y = -5; y <= 5; y++)
                {
                    if (s.PowodBudowy(klucz, x, y) == null && (x * x + y * y) <= 4 && (x != 0 || y != 0))
                    {
                        Harness.Sprawdz($"plac: {klucz}", s.PostawNa(klucz, x, y));
                        for (int i = 0; i < 6000 && s.Place.ContainsKey((x, y)); i++)
                        {
                            s.Glod = s.Energia = s.Hp = 100;
                            s.Godzina = 8.0;
                            s.Aktualizuj(0.1, new Wejscie { Dzialaj = true });
                        }
                        Harness.Rowne($"gotowe: {klucz}", klucz, s.Budowle[(x, y)]);
                        s.Budowle[(30 + s.Budowle.Count * 2, 30)] = klucz;
                        s.Budowle.Remove((x, y));
                        return;
                    }
                }
            }
            Harness.Sprawdz($"znaleziono miejsce: {klucz}", false);
        }

        /// <summary>Zwykła droga gracza: kafelek budowli, potem klik na mapie. Nic nie wstawiamy wprost do świata.</summary>
        private static void KazdaBudowla()
        {
            foreach (string klucz in SwiatWolny.Budowlane)
            {
                SwiatWolny s = Swiat();
                s.Szczebel = SwiatWolny.Szczeble.Length - 1;
                foreach (var m in SwiatWolny.KosztCalkowity(klucz))
                {
                    s.Plecak[m.Key] = m.Value;
                }
                s.Aktualizuj(0.02, new Wejscie { Klik = "a:buduj" });
                s.Aktualizuj(0.02, new Wejscie { Klik = "b:" + klucz });
                s.Aktualizuj(0.02, new Wejscie { Poloz = true, Kursor = (-1, -2) });
                Harness.Sprawdz($"gracz może postawić: {klucz}",
                                s.Budowle.TryGetValue((-1, -2), out string stoi) && (stoi == klucz || stoi == SwiatWolny.PrzedrostekPlacu + klucz));
            }
        }

        private static void Odblokowanie()
        {
            SwiatWolny s = Swiat();
            Harness.Sprawdz("spichlerz czeka na wioskę", s.PowodBudowy("spichlerz", 1, 1).Contains("wioska"));
            Harness.Sprawdz("targ czeka na miasteczko", s.PowodBudowy("targ", 1, 1).Contains("miasteczko"));
            s.Aktualizuj(0.02, new Wejscie { Klik = "a:buduj" });
            s.Aktualizuj(0.02, new Wejscie { Klik = "k:zaklady" });
            KafelkiWolne k = KafelkiWolne.Z(s);
            Kafelek spichlerz = k.Budowle.Single(b => b.Id == "b:spichlerz");
            Harness.Sprawdz("zamknięty kafelek mówi, od kiedy", !spichlerz.Dostepny && spichlerz.Stan.Value.Tekst == "od: wioska");
            Harness.Sprawdz("farma jest od początku", k.Budowle.Single(b => b.Id == "b:farma").Stan == null);
            Harness.Rowne("każda budowla ma etapy albo stawia się od razu", 0,
                          SwiatWolny.BudynkiMiejskie.Count(b => !SwiatWolny.Etapy.ContainsKey(b)));
            foreach (string b in SwiatWolny.BudynkiMiejskie)
            {
                Harness.Sprawdz($"{b} wytrzymuje więcej niż śpiwór", SwiatWolny.ZyciePoczatkowe(b) > SwiatWolny.ZyciePoczatkowe("spiwor"));
            }
        }

        private static void Awans()
        {
            SwiatWolny s = Swiat();
            Harness.Rowne("start: obóz", "Obóz", s.NazwaSzczebla);
            Harness.Sprawdz("braki mówią o ludziach i budynkach", s.BrakiSzczebla().Contains("ludzie 0/4") && s.BrakiSzczebla().Contains("farma") && s.BrakiSzczebla().Contains("chata"));
            Zbuduj(s, "farma");
            Harness.Rowne("sama farma nie wystarczy", 0, s.Szczebel);
            Zbuduj(s, "chata");
            for (int i = 0; i < 4; i++)
            {
                s.Gosc = new Przybysz { O = new Osadnik { Imie = "Os" + i, Zajecie = "bezczynny", Morale = 60 }, Surowiec = "drewno", Ile = 0 };
                s.Aktualizuj(0.02, new Wejscie { Klik = "g:tak" });
                s.Wycofaj();
            }
            Harness.Rowne("czworo ludzi, farma i chata: wioska", 1, s.Szczebel);
            Harness.Sprawdz("dziennik ogłasza awans", s.Dziennik.Any(l => l.Contains("to już wioska")));
            Harness.Sprawdz("pasek stanu pokazuje szczebel", WidokWolny.Hud(s).Region.StartsWith("Wioska"));
            s.Obozowisko.Osadnicy.Clear();
            s.Aktualizuj(0.02, new Wejscie());
            Harness.Rowne("szczebel nie spada, gdy ludzie odejdą", 1, s.Szczebel);
            Harness.Sprawdz("spichlerz jest już odblokowany", !(s.PowodBudowy("spichlerz", 1, 1) ?? "").Contains("rozwiń"));
        }

        private static (SwiatWolny S, Osadnik O) ZFarma(string zajecie)
        {
            SwiatWolny s = Swiat();
            Zbuduj(s, "farma");
            var o = new Osadnik { Imie = "Test", Zajecie = "bezczynny", Morale = 60 };
            s.Obozowisko.Osadnicy.Add(o);
            s.Postacie.Add(new PostacOsadnika { O = o });
            s.Wybrany = 0;
            s.Aktualizuj(0.02, new Wejscie { Klik = "z:" + zajecie });
            s.Obozowisko.Surowce["zywnosc"] = 20;
            return (s, o);
        }

        private static void Doba(SwiatWolny s)
        {
            s.Godzina = 23.999;
            s.Aktualizuj(0.1, new Wejscie());
        }

        private static void Gospodarka()
        {
            SwiatWolny s = Swiat();
            Harness.Rowne("bez farmy poziom 0", 0, s.Obozowisko.PoziomBudynku("farma"));
            Zbuduj(s, "farma");
            Harness.Rowne("farma na mapie to poziom 1", 1, s.Obozowisko.PoziomBudynku("farma"));
            Zbuduj(s, "farma");
            Harness.Rowne("druga farma to poziom 2", 2, s.Obozowisko.PoziomBudynku("farma"));
            Harness.Rowne("poziom daje stanowiska", 4, Osada.Miejsca(s.Obozowisko, "rolnik"));

            (SwiatWolny rolnik, Osadnik r) = ZFarma("rolnik");
            Harness.Rowne("osadnik zostaje rolnikiem", "rolnik", r.Zajecie);
            Doba(rolnik);
            (SwiatWolny bez, Osadnik _) = ZFarma("bezczynny");
            Doba(bez);
            Harness.Sprawdz("rolnik daje jedzenie", rolnik.Obozowisko.Surowce.Wez("zywnosc") > bez.Obozowisko.Surowce.Wez("zywnosc"));
            Harness.Sprawdz("utrzymanie nie wywołuje braku złota", !rolnik.Dziennik.Any(l => l.Contains("utrzymanie")));
            Harness.Rowne("złoto nie schodzi poniżej zera ani nie rośnie samo", 0, rolnik.Obozowisko.Zloto);

            SwiatWolny k = Swiat();
            Harness.Rowne("bez farmy zawód rolnika nie działa", null, Osada.Miejsca(k.Obozowisko, "drwal"));
            var o = new Osadnik { Imie = "Bez", Zajecie = "bezczynny", Morale = 60 };
            k.Obozowisko.Osadnicy.Add(o);
            k.Postacie.Add(new PostacOsadnika { O = o });
            k.PanelOtwarty = true;
            Harness.Sprawdz("kafelek rolnika czeka na farmę", KafelkiWolne.Z(k).Zajecia.All(z => z.Id != "z:rolnik"));
            k.Budowle[(20, 20)] = "farma";
            Harness.Sprawdz("z farmą kafelek się pojawia", KafelkiWolne.Z(k).Zajecia.Any(z => z.Id == "z:rolnik"));

            SwiatWolny mala = Swiat();
            int budzetMalej = mala.BudzetOsady;
            mala.Obozowisko.Osadnicy.Add(new Osadnik { Imie = "Nowy", Zajecie = "bezczynny", Morale = 60 });
            mala.Szczebel = 1;
            Harness.Rowne("osadnik i szczebel podnoszą zagrożenie", budzetMalej + 3, mala.BudzetOsady);
            mala.Dzien = 30;
            Harness.Rowne("sam upływ dni nie podnosi zagrożenia", budzetMalej + 3, mala.BudzetOsady);
            Harness.Sprawdz("większa osada to większa fala",
                            SwiatWolny.SkladFali(1, new Losowanie(7), 6).Count > SwiatWolny.SkladFali(1, new Losowanie(7)).Count);
        }

        private static void Zapis()
        {
            SwiatWolny s = Swiat();
            Zbuduj(s, "farma");
            s.Szczebel = 2;
            SwiatWolny w = SwiatWolny.ZZapisu(s.DoZapisu());
            Harness.Rowne("zapis pamięta szczebel", 2, w.Szczebel);
            Harness.Rowne("po wczytaniu poziomy budynków się zgadzają", 1, w.Obozowisko.PoziomBudynku("farma"));
        }

        private static void Stacja()
        {
            SwiatWolny s = Swiat();
            s.Budowle[(2, 0)] = "tartak";
            s.Plecak["drewno"] = 4;
            Harness.Sprawdz("plac chaty obok tartaku", s.PostawNa("chata", 0, 2));
            s.Plecak["kamien"] = 40;
            s.Place[(0, 2)].Etap = 1;
            s.Aktualizuj(0.02, new Wejscie { Klik = "r:belka" });
            for (int i = 0; i < 100; i++)
            {
                s.Aktualizuj(0.1, new Wejscie { Dzialaj = true });
            }
            Harness.Rowne("plac bez belek nie blokuje pracy w tartaku obok", 1, s.Razem("belka"));
        }
    }
}
