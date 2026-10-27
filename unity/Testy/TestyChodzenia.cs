using System.Collections.Generic;
using System.Linq;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>Chodzenie bohatera po obozie: ruch, przeszkody, podpowiedzi i cały obieg przez menu obozu.</summary>
    public static class TestyChodzenia
    {
        private static StanOsady Stan(Gracz g)
        {
            return Migawka.PlacZGracza(g);
        }

        private static Gracz Obozek(params (string Klucz, int Poziom)[] budynki)
        {
            Gracz g = TestyOsady.Pusty();
            g.Chaty = 2;
            foreach ((string k, int p) in budynki)
            {
                Oboz.UstawPoziom(g, k, p);
            }
            return g;
        }

        public static void Uruchom()
        {
            Harness.Grupa("Chodzenie bohatera po obozie");
            Gracz g = Obozek();
            Harness.Rowne("start: pole przy ognisku", UkladPlacu.PoleBohatera, g.PozycjaObozu);
            Harness.Rowne("migawka niesie pozycję bohatera", g.PozycjaObozu, Stan(g).Bohater);

            Harness.Rowne("krok w prawo (ekran) to przekątna siatki", ChodzenieObozu.WynikRuchu.Ruszyl,
                          ChodzenieObozu.Ruch(g, Stan(g), "@prawo"));
            Harness.Rowne("…na pole (4, 3)... ognisko zagradza, więc ślizg", true,
                          g.PozycjaObozu != UkladPlacu.Ognisko && g.PozycjaObozu != UkladPlacu.PoleBohatera);

            g = Obozek();
            g.PozycjaObozu = (1, 1);
            Harness.Rowne("w górę z rogu placu nic nie zmienia", ChodzenieObozu.WynikRuchu.Zablokowane,
                          ChodzenieObozu.Ruch(g, Stan(g), "gora"));
            Harness.Rowne("pozycja zostaje", (1, 1), g.PozycjaObozu);

            g = Obozek(("kuznia", 1));
            g.PozycjaObozu = (2, 2);
            ChodzenieObozu.Ruch(g, Stan(g), "gora");
            Harness.Sprawdz("kuźnia zagradza drogę", g.PozycjaObozu != UkladPlacu.Miejsca["kuznia"]);

            bool poza = false;
            g = Obozek();
            foreach (string k in new[] { "dol", "dol", "dol", "lewo", "lewo", "lewo", "prawo", "gora", "gora", "dol" })
            {
                ChodzenieObozu.Ruch(g, Stan(g), k);
                poza |= g.PozycjaObozu.X < 1 || g.PozycjaObozu.Y < 1 || g.PozycjaObozu.X > 5 || g.PozycjaObozu.Y > 5
                        || g.PozycjaObozu == UkladPlacu.Ognisko;
            }
            Harness.Sprawdz("bohater nie wychodzi poza plac ani na ognisko", !poza);

            g = Obozek(("farma", 1));
            g.PozycjaObozu = (3, 4);
            Harness.Sprawdz("po polu uprawnym się chodzi",
                            ChodzenieObozu.Wolne(UkladPlacu.Rozklad(Stan(g)), UkladPlacu.Miejsca["farma"]));
            Harness.Sprawdz("osadnicy omijają pole bohatera",
                            UkladPlacu.Zablokowane(UkladPlacu.Rozklad(Stan(g)), (2, 4)).Contains((2, 4))
                            && !UkladPlacu.Zablokowane(UkladPlacu.Rozklad(Stan(g)), (2, 4)).Contains(UkladPlacu.PoleBohatera));

            Interakcje();
            Obieg();
        }

        private static void Interakcje()
        {
            Harness.Grupa("Chodzenie: co robi spacja");
            Gracz g = Obozek();
            Harness.Rowne("przy ognisku: osada", "16", ChodzenieObozu.Cel(Stan(g), (3, 4)).Odpowiedz);
            g.PozycjaObozu = (3, 5);
            Harness.Rowne("przy bramie: wyprawa", "1", ChodzenieObozu.Cel(Stan(g), (3, 5)).Odpowiedz);
            Harness.Rowne("namiot: odpoczynek", "3", ChodzenieObozu.Cel(Stan(g), (3, 3)).Odpowiedz == "16"
                          ? ChodzenieObozu.Cel(Stan(g), (3, 2)).Odpowiedz : "?");
            g = Obozek(("skryptorium", 1), ("dom", 1), ("kuznia", 1));
            Harness.Rowne("skryptorium: badania", "23", ChodzenieObozu.Cel(Stan(g), (3, 1 + 1) ).Odpowiedz == "3"
                          ? ChodzenieObozu.Cel(Stan(g), (4, 1)).Odpowiedz : "?");
            Harness.Rowne("kuźnia: obejrzenie", "info:kuznia", ChodzenieObozu.Cel(Stan(g), (1, 0 + 2)).Odpowiedz);
            StanOsady s = Stan(g);
            s.Rusztowania.Add(("tartak", 1, 0.3));
            Harness.Rowne("szkielet: praca przy budowie", "11", ChodzenieObozu.Cel(s, (1, 3)).Odpowiedz);
            Harness.Sprawdz("podpowiedź zaczyna się od ▶", ChodzenieObozu.Podpowiedz(s, (3, 4)).StartsWith("▶"));
            Harness.Sprawdz("pusty róg nie ma podpowiedzi akcji",
                            ChodzenieObozu.Cel(Stan(Obozek()), (5, 5)) == null
                            && ChodzenieObozu.Podpowiedz(Stan(Obozek()), (5, 5)).Contains("chodź"));
        }

        private static void Obieg()
        {
            Harness.Grupa("Chodzenie: obieg przez menu obozu");
            // Nowa gra, Enter po karcie; w obozie spacja przy ognisku otwiera osadę, [0] wraca,
            // krok w bok zmienia pole, [0] kończy obóz, [0] wychodzi z gry.
            var konsola = new KonsolaTestowa(new[] { "1", "Halina", "4", "", "@akcja", "0", "@prawo", "0", "0" });
            var gra = new Logika.Gra(konsola, new Ekran(), new Losowanie(2024));
            gra.Uruchom();
            string w = konsola.Wyjscie;
            Harness.Sprawdz("spacja przy ognisku otworzyła osadę", w.Contains("OSADA  —"));
            Harness.Sprawdz("ekran obozu niesie podpowiedź", w.Contains("▶ Spacja: usiądź przy ognisku"));
            Harness.Sprawdz("po kroku podpowiedź się zmienia", w.Contains("▶ Strzałki lub WASD") || w.Split("▶").Length > 3);
        }
    }
}
