using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Grafika.Wolny;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Kafelki zamiast cyfr i tryb budowania: wybór, duch na mapie, stawianie myszą, panel osady.</summary>
    public static class TestyKafelkowWolnych
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: kafelki i tryb budowania");
            Tryb();
            Stawianie();
            Odmowy();
            Wycofywanie();
            PanelMysza();
            Rysowanie();
        }

        private static void Klik(SwiatWolny s, string id)
        {
            s.Aktualizuj(0.01, new Wejscie { Klik = id });
        }

        /// <summary>Kafel, na którym można postawić budowlę, poza kaflem bohatera.</summary>
        private static (int X, int Y) WolnyKafel(SwiatWolny s, string klucz)
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
            throw new InvalidOperationException("brak wolnego kafla przy starcie");
        }

        private static SwiatWolny Bogaty()
        {
            var s = new SwiatWolny(42);
            s.Plecak["drewno"] = 40;
            s.Plecak["kamien"] = 20;
            s.Plecak["wlokno"] = 20;
            return s;
        }

        private static void Tryb()
        {
            var s = new SwiatWolny(42);
            KafelkiWolne k = KafelkiWolne.Z(s);
            Harness.RowneCiagi("pasek akcji ma pięć kafelków",
                               new List<string> { "a:buduj", "a:magazyn", "a:osada", "a:jedz", "a:spij" },
                               k.Akcje.Select(a => a.Id).ToList());
            Harness.Rowne("poza trybem nie ma rzędu budowli", 0, k.Budowle.Count);
            Harness.Sprawdz("podpowiedź poza trybem to klawisze", WidokWolny.Podpowiedz(s).Contains("WASD"));

            Klik(s, "a:buduj");
            k = KafelkiWolne.Z(s);
            Harness.Sprawdz("kafelek Buduj włącza tryb", s.TrybBudowy && k.Akcje[0].Wybrany);
            Harness.Rowne("tryb pokazuje cztery budowle", 4, k.Budowle.Count);
            Harness.Sprawdz("bez wyboru nie ma ducha", s.PodgladBudowy() == null);
            Harness.Sprawdz("podpowiedź prosi o wybór", WidokWolny.Podpowiedz(s).StartsWith("Wybierz"));

            Harness.Sprawdz("śpiwór jest z plecaka i dostępny", k.Budowle[0].Dostepny && k.Budowle[0].Opis[0].Tekst == "z plecaka");
            Harness.Sprawdz("na ognisko nie ma składników", !k.Budowle[1].Dostepny && k.Budowle[1].Opis.All(o => !o.Ok));
            s.Plecak["drewno"] = 5;
            k = KafelkiWolne.Z(s);
            Harness.Sprawdz("każdy składnik liczy się osobno", k.Budowle[1].Opis[0].Ok && !k.Budowle[1].Opis[1].Ok);

            Klik(s, "b:ognisko");
            Harness.Rowne("kafelek wybiera budowlę", "ognisko", s.WybranaBudowla);
            Harness.Sprawdz("wybrany kafelek jest wyróżniony", KafelkiWolne.Z(s).Budowle[1].Wybrany);
            Klik(s, "b:ognisko");
            Harness.Sprawdz("drugi klik odznacza", s.WybranaBudowla == null);
            Klik(s, "b:namiot");
            Klik(s, "a:buduj");
            Harness.Sprawdz("wyjście z trybu czyści wybór", !s.TrybBudowy && s.WybranaBudowla == null);
        }

        private static void Stawianie()
        {
            SwiatWolny s = Bogaty();
            (int x, int y) = WolnyKafel(s, "namiot");
            s.Aktualizuj(0.01, new Wejscie { Kursor = (x, y), Poloz = true });
            Harness.Rowne("poza trybem klik w świat nic nie stawia", 0, s.Budowle.Count);

            Klik(s, "a:buduj");
            Klik(s, "b:namiot");
            s.Aktualizuj(0.01, new Wejscie { Kursor = (x, y) });
            (int X, int Y, string Klucz, bool Mozna)? duch = s.PodgladBudowy();
            Harness.Sprawdz("duch stoi pod kursorem i wolno budować",
                            duch.HasValue && duch.Value.X == x && duch.Value.Y == y && duch.Value.Klucz == "namiot" && duch.Value.Mozna);
            Harness.Sprawdz("podpowiedź mówi o kliku", WidokWolny.Podpowiedz(s).StartsWith("Lewy klik"));

            s.Aktualizuj(0.01, new Wejscie { Kursor = (x, y), Poloz = true });
            Harness.Rowne("klik stawia budowlę pod kursorem", "namiot", s.Budowle.TryGetValue((x, y), out string b) ? b : "");
            Harness.Rowne("namiot zabrał drewno", 32, s.Ile("drewno"));
            Harness.Rowne("namiot zabrał włókno", 14, s.Ile("wlokno"));
            Harness.Sprawdz("po postawieniu tryb i wybór zostają", s.TrybBudowy && s.WybranaBudowla == "namiot");

            var bez = new SwiatWolny(42);
            (int bx, int by) = WolnyKafel(bez, "spiwor");
            Klik(bez, "a:buduj");
            Klik(bez, "b:spiwor");
            bez.Aktualizuj(0.01, new Wejscie { Kursor = (bx, by), Poloz = true });
            Harness.Sprawdz("gdy składniki się skończą, wybór znika", bez.Budowle.ContainsKey((bx, by)) && bez.WybranaBudowla == null);

            var przod = Bogaty();
            Klik(przod, "a:buduj");
            Klik(przod, "b:ognisko");
            (int px, int py) = przod.KaflPrzed();
            przod.Aktualizuj(0.01, new Wejscie { Poloz = true });
            Harness.Sprawdz("Enter bez myszy stawia przed bohaterem", przod.Budowle.ContainsKey((px, py)));
        }

        private static void Odmowy()
        {
            SwiatWolny s = Bogaty();
            Harness.Sprawdz("za daleko", s.PowodBudowy("namiot", 10, 0).StartsWith("To za daleko"));
            Harness.Sprawdz("nie na bohaterze", s.PowodBudowy("namiot", 0, 0).StartsWith("Stoisz"));
            Harness.Sprawdz("śpiwór można rozłożyć pod sobą", s.PowodBudowy("spiwor", 0, 0) == null);

            (int x, int y) = WolnyKafel(s, "ognisko");
            Klik(s, "a:buduj");
            Klik(s, "b:ognisko");
            s.Aktualizuj(0.01, new Wejscie { Kursor = (x, y), Poloz = true });
            int drewna = s.Ile("drewno");
            s.Aktualizuj(0.01, new Wejscie { Kursor = (x, y) });
            Harness.Sprawdz("duch na zajętym kaflu jest czerwony", s.PodgladBudowy().HasValue && !s.PodgladBudowy().Value.Mozna);
            s.Aktualizuj(0.01, new Wejscie { Kursor = (x, y), Poloz = true });
            Harness.Rowne("odmowa nic nie kosztuje", drewna, s.Ile("drewno"));
            Harness.Sprawdz("odmowa trafia do dziennika", s.Dziennik.Last().StartsWith("Tu nie da się"));

            var biedny = new SwiatWolny(42);
            (int bx, int by) = WolnyKafel(biedny, "spiwor");
            Harness.Sprawdz("brak składników ma swój komunikat", biedny.PowodBudowy("chata", bx, by).StartsWith("Brakuje"));
        }

        private static void Wycofywanie()
        {
            SwiatWolny s = Bogaty();
            Klik(s, "a:buduj");
            Klik(s, "b:chata");
            Harness.Sprawdz("najpierw wycofuje wybór", s.Wycofaj() && s.TrybBudowy && s.WybranaBudowla == null);
            Harness.Sprawdz("potem wychodzi z trybu", s.Wycofaj() && !s.TrybBudowy);
            Harness.Sprawdz("na końcu nie ma czego wycofać (Esc wraca do menu)", !s.Wycofaj());
            Klik(s, "a:osada");
            Harness.Sprawdz("Esc zamyka też panel", s.Wycofaj() && !s.PanelOtwarty);
        }

        private static void PanelMysza()
        {
            SwiatWolny s = Bogaty();
            s.Obozowisko.Chaty = 4;
            var los = new Losowanie(7);
            Osada.ZatrudnijOsadnika(s.Obozowisko, los, "bezczynny", true);
            Osada.ZatrudnijOsadnika(s.Obozowisko, los, "bezczynny", true);
            s.Obozowisko.Osadnicy.ForEach(o => o.Zajecie = "bezczynny");

            Klik(s, "a:buduj");
            Klik(s, "a:osada");
            Harness.Sprawdz("osada zamyka tryb budowania", s.PanelOtwarty && !s.TrybBudowy);
            KafelkiWolne k = KafelkiWolne.Z(s);
            Harness.Rowne("sześć kafelków zajęć", 6, k.Zajecia.Count);
            Harness.Rowne("dwa wiersze osadników", 2, k.Osadnikow);
            Harness.Sprawdz("bieżące zajęcie jest wyróżnione", k.Zajecia.Single(z => z.Wybrany).Id == "z:bezczynny");

            Klik(s, "o:1");
            Harness.Rowne("klik w wiersz wybiera osadnika", 1, s.Wybrany);
            Klik(s, "o:9");
            Harness.Rowne("numer spoza listy nic nie zmienia", 1, s.Wybrany);
            Klik(s, "z:drwal");
            Harness.Rowne("kafelek zmienia zajęcie wybranego", "drwal", s.Obozowisko.Osadnicy[1].Zajecie);
            Harness.Rowne("pozostali bez zmian", "bezczynny", s.Obozowisko.Osadnicy[0].Zajecie);
            Harness.Sprawdz("panel nie opisuje już budowy", !WidokWolny.Panel(s).Any(l => l.Contains("BUDOWA")));

            var sen = new SwiatWolny(42);
            sen.Spi = true;
            Klik(sen, "a:spij");
            Harness.Sprawdz("kafelek Śpij budzi i nie kładzie z powrotem", !sen.Spi && sen.Dziennik.Last() == "Wstajesz.");
        }

        private static List<Napis> Zloz(Pulpit p, ScenaWolna scena, SwiatWolny s, (int X, int Y)? mysz = null)
        {
            return p.ZlozWolny(scena, s, 1.0, WidokWolny.Hud(s), WidokWolny.Plecak(s), WidokWolny.Dziennik(s),
                               WidokWolny.Podpowiedz(s), WidokWolny.Panel(s), KafelkiWolne.Z(s), mysz);
        }

        private static void Rysowanie()
        {
            SwiatWolny s = Bogaty();
            var pulpit = new Pulpit();
            var scena = new ScenaWolna();
            List<Napis> napisy = Zloz(pulpit, scena, s);
            Harness.Rowne("pięć przycisków paska", 5, pulpit.PrzyciskiWolne.Count);
            Harness.Sprawdz("nazwy kafelków są narysowane", napisy.Any(n => n.Tekst == "Magazyn"));

            Klik(s, "a:buduj");
            Zloz(pulpit, scena, s);
            Harness.Rowne("w trybie dziewięć przycisków", 9, pulpit.PrzyciskiWolne.Count);
            Harness.Sprawdz("kafelek chaty ma swój identyfikator", pulpit.PrzyciskiWolne.Any(b => b.Klawisz == "b:chata"));
            foreach (Przycisk b in pulpit.PrzyciskiWolne)
            {
                Harness.Sprawdz($"{b.Klawisz} mieści się na pulpicie i nie wchodzi na kule",
                                b.X >= 192 * PulpitFantasy.K && b.X + b.Szer <= 1280 && b.Y >= 0 && b.Y + b.Wys <= 720);
                Harness.Sprawdz($"{b.Klawisz} nie nachodzi na inny kafelek",
                                !pulpit.PrzyciskiWolne.Any(i => i != b && i.X < b.X + b.Szer && b.X < i.X + i.Szer
                                                                && i.Y < b.Y + b.Wys && b.Y < i.Y + i.Wys));
            }

            byte[] bezMyszy = (byte[])pulpit.Klatka.Dane.Clone();
            Przycisk pierwszy = pulpit.PrzyciskiWolne[1];      // [0] to wybrany „Buduj”: on jaśnieje cały czas
            Zloz(pulpit, scena, s, (pierwszy.X + 4, pierwszy.Y + 4));
            Harness.Sprawdz("kafelek pod kursorem jaśnieje", !bezMyszy.SequenceEqual(pulpit.Klatka.Dane));

            Zloz(pulpit, scena, s);
            byte[] bezDucha = (byte[])pulpit.Klatka.Dane.Clone();
            Klik(s, "b:namiot");
            Zloz(pulpit, scena, s);
            Harness.Sprawdz("duch budowli jest widoczny", !bezDucha.SequenceEqual(pulpit.Klatka.Dane));

            for (int x = -3; x <= 3; x++)
            {
                (int ex, int ey) = ScenaWolna.NaEkran(x + 0.5, 2.5, s.X, s.Y);
                (double wx, double wy) = ScenaWolna.NaSwiat(ex, ey, s.X, s.Y);
                Harness.Sprawdz($"piksel środka kafla {x},2 wraca na ten kafel",
                                (int)Math.Floor(wx) == x && (int)Math.Floor(wy) == 2);
            }
        }
    }
}
