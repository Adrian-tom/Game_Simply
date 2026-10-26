using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Ruch osadników po placu: ścieżki, przeszkody, rezerwacje pól, rytm doby.
    ///
    /// Dwa rodzaje sprawdzeń. Własne (jak w <c>tests/test_ruch.py</c>) mówią,
    /// czy ruch ma sens: dochodzi, nie wchodzi w budynek, nikt nie stoi na
    /// jednym polu. Wzorcowe porównują port z Pythonem: te same trasy A*
    /// co do pola i te same pozycje po kilkuset krokach symulacji.
    /// </summary>
    public static class TestyRuchu
    {
        private const double Tolerancja = 1e-9;

        public static void Uruchom(JsonElement wzorce)
        {
            Harness.Grupa("Ruch osadników: ścieżki A*");
            Sciezki();
            Harness.Grupa("Ruch osadników: rytm doby");
            Rytm();
            Harness.Grupa("Ruch osadników: symulacja");
            Symulacja();
            Harness.Grupa("Ruch osadników zgodny z wersją pythonową");
            ZgodnoscSciezek(wzorce);
            ZgodnoscRytmu(wzorce);
            ZgodnoscSymulacji(wzorce);
        }

        // ------------------------------------------------------------------ //
        //  Własne sprawdzenia                                                 //
        // ------------------------------------------------------------------ //

        private static void Sciezki()
        {
            var s = new Siatka(7);
            var prosta = Sciezki_(s, (0, 0), (4, 0));
            Harness.Rowne("prosta ma tyle pól co odległość", 4, prosta.Count);
            Harness.Rowne("prosta kończy się w celu", (4, 0), prosta[prosta.Count - 1]);
            Harness.Rowne("start równy celowi to pusta trasa", 0, Sciezki_(s, (2, 2), (2, 2)).Count);

            var mur = new Siatka(7, new[] { (2, 0), (2, 1), (2, 2) });
            var wokol = Sciezki_(mur, (0, 1), (4, 1));
            Harness.Sprawdz("trasa omija przeszkodę", wokol != null && !wokol.Any(p => mur.Zablokowane.Contains(p)));
            Harness.Sprawdz("trasa wokół muru kończy się w celu", wokol != null && wokol.Last() == (4, 1));

            var zamkniety = new Siatka(7, new[]
            {
                (2, 2), (3, 2), (4, 2), (2, 3), (4, 3), (2, 4), (3, 4), (4, 4),
            });
            Harness.Sprawdz("zamknięty cel nie ma trasy", Sciezki_(zamkniety, (0, 0), (3, 3)) == null);
            Harness.Sprawdz("zablokowany cel nie ma trasy",
                            Sciezki_(new Siatka(7, new[] { (3, 3) }), (0, 0), (3, 3)) == null);
            Harness.Sprawdz("po skosie nie ścina rogu",
                            Sciezki_(new Siatka(7, new[] { (1, 0), (0, 1) }), (0, 0), (1, 1)) == null);
            Harness.Rowne("skos bywa krótszy niż dwa proste kroki", 3, Sciezki_(s, (0, 0), (3, 3)).Count);

            var blisko = new Siatka(7, new[] { (3, 3) }).NajblizszeWolne((3, 3));
            Harness.Sprawdz("najbliższe wolne jest sąsiadem zajętego pola",
                            blisko.HasValue && Math.Abs(blisko.Value.X - 3) + Math.Abs(blisko.Value.Y - 3) == 1);
        }

        private static List<(int X, int Y)> Sciezki_(Siatka s, (int, int) start, (int, int) cel)
        {
            return GraSimply.Logika.Sciezki.Znajdz(s, start, cel);
        }

        private static void Rytm()
        {
            double d = RytmDnia.DlugoscDnia;
            Harness.Rowne("rano", "rano", RytmDnia.Faza(0.0));
            Harness.Rowne("praca", "praca", RytmDnia.Faza(d * 0.3));
            Harness.Rowne("wieczór", "wieczor", RytmDnia.Faza(d * 0.7));
            Harness.Rowne("noc", "noc", RytmDnia.Faza(d * 0.9));
            Harness.Rowne("doba się powtarza", RytmDnia.Faza(5.0), RytmDnia.Faza(5.0 + 3 * d));
            Harness.Sprawdz("południe jaśniejsze od północy",
                            RytmDnia.Swiatlo(d * 0.3) > RytmDnia.Swiatlo(d * 0.9));

            bool ciagle = true;
            double poprzednie = RytmDnia.Swiatlo(0.0);
            for (int i = 1; i < 900; i++)
            {
                double teraz = RytmDnia.Swiatlo(i * 0.1);
                ciagle &= Math.Abs(teraz - poprzednie) < 0.05 && teraz >= 0 && teraz <= 1;
                poprzednie = teraz;
            }
            Harness.Sprawdz("światło jest ciągłe i mieści się w 0…1", ciagle);

            var chory = new Osadnik { Imie = "Olek", Chory = 3 };
            foreach (string pora in new[] { "rano", "praca", "wieczor", "noc" })
            {
                Harness.Rowne($"chory zostaje w domu ({pora})", (2, 2),
                              RytmDnia.Zadanie(chory, (1, 1), (2, 2), (3, 3), (5, 4), pora).Pole);
            }

            var zdrowy = new Osadnik { Imie = "Jagna" };
            Harness.Rowne("praca idzie do stanowiska", ((1, 1), "praca"),
                          RytmDnia.Zadanie(zdrowy, (1, 1), (2, 2), (3, 3), null, "praca"));
            Harness.Rowne("wieczór przy ognisku", ((3, 3), "ognisko"),
                          RytmDnia.Zadanie(zdrowy, (1, 1), (2, 2), (3, 3), null, "wieczor"));
            Harness.Rowne("noc w domu", ((2, 2), "sen"),
                          RytmDnia.Zadanie(zdrowy, (1, 1), (2, 2), (3, 3), null, "noc"));

            string[] imiona = { "Nessa", "Torin", "Elka", "Bram", "Sira", "Olek", "Mila", "Gareth" };
            var wybory = imiona.Select(i => RytmDnia.Zadanie(new Osadnik { Imie = i }, (1, 1), (2, 2),
                                                             (3, 3), (5, 4), "wieczor").Czynnosc).ToList();
            Harness.Sprawdz("wieczorem część idzie do tawerny, część do ogniska",
                            wybory.Contains("tawerna") && wybory.Contains("ognisko"));
        }

        private static void Symuluj(SymulacjaRuchu r, double sekundy, double krok = 0.05)
        {
            int n = (int)(sekundy / krok);
            for (int i = 0; i < n; i++)
            {
                r.Aktualizuj(krok);
            }
        }

        private static void Symulacja()
        {
            var r = new SymulacjaRuchu();
            r.Dodaj("Olek", (0, 0));
            r.UstawCel("Olek", (4, 0));
            Symuluj(r, 4 / SymulacjaRuchu.Predkosc + 0.3);
            Harness.Sprawdz("postać dochodzi w czasie wynikającym z prędkości", r.Dotarl("Olek"));
            Harness.BliskoSiebie("dochodzi na środek pola X", 4.0, r.Pozycja("Olek").X, 0.01);

            r = new SymulacjaRuchu();
            r.Dodaj("Olek", (0, 0));
            r.UstawCel("Olek", (6, 0));
            Symuluj(r, 3.0);
            Harness.Sprawdz("nie dochodzi wcześniej, niż pozwala prędkość",
                            r.Pozycja("Olek").X <= 3.0 * SymulacjaRuchu.Predkosc + 1e-6 && !r.Dotarl("Olek"));

            var mur = new HashSet<(int X, int Y)> { (2, 0), (2, 1), (2, 2), (2, 3), (2, 4) };
            r = new SymulacjaRuchu();
            r.Przebuduj(mur);
            r.Dodaj("Olek", (0, 2));
            r.UstawCel("Olek", (4, 2));
            bool wszedl = false;
            for (int i = 0; i < 240; i++)
            {
                r.Aktualizuj(0.05);
                (double x, double y) = r.Pozycja("Olek");
                wszedl |= mur.Any(b => Math.Abs(x - b.X) < 0.5 && Math.Abs(y - b.Y) < 0.5);
            }
            Harness.Sprawdz("nigdy nie wchodzi w budynek", !wszedl);
            Harness.Sprawdz("a mimo muru dochodzi do celu", r.Dotarl("Olek"));

            r = new SymulacjaRuchu();
            r.Przebuduj(new HashSet<(int X, int Y)> { (3, 3) });
            r.Dodaj("Olek", (0, 0));
            r.UstawCel("Olek", (3, 3), "praca");
            Symuluj(r, 10);
            Harness.Rowne("przy budynku staje od frontu", (3, 4), r.Postacie["Olek"].Pole);
            Harness.Rowne("czynność zostaje", "praca", r.Postacie["Olek"].Czynnosc);

            r = new SymulacjaRuchu();
            r.Przebuduj(new HashSet<(int X, int Y)> { (3, 3) });
            string[] ludzie = { "Ala", "Bram", "Cira", "Dan" };
            for (int i = 0; i < ludzie.Length; i++)
            {
                r.Dodaj(ludzie[i], (i, 0));
                r.UstawCel(ludzie[i], (3, 3));
            }
            Symuluj(r, 20);
            var pola = ludzie.Select(i => r.Postacie[i].Pole).ToList();
            Harness.Rowne("czterej do jednego celu stają na różnych polach", pola.Count, pola.Distinct().Count());
            Harness.Sprawdz("i wszyscy dotarli", ludzie.All(i => r.Dotarl(i)));

            var mur2 = new HashSet<(int X, int Y)>();
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    if (y != 3 && y != 4)
                    {
                        mur2.Add((x, y));
                    }
                }
            }
            r = new SymulacjaRuchu();
            r.Przebuduj(mur2);
            r.Dodaj("Ala", (0, 3));
            r.Dodaj("Bram", (6, 3));
            r.UstawCel("Ala", (6, 3));
            r.UstawCel("Bram", (0, 3));
            Symuluj(r, 30);
            Harness.Sprawdz("mijają się w dwupolowym przejściu", r.Dotarl("Ala") && r.Dotarl("Bram"));

            r = new SymulacjaRuchu();
            r.Dodaj("Olek", (3, 3));
            r.Przebuduj(new HashSet<(int X, int Y)> { (3, 3) });
            Harness.Sprawdz("nowy budynek zrzuca stojącego z pola", r.Postacie["Olek"].Pole != (3, 3));

            r = new SymulacjaRuchu();
            r.Dodaj("Ala", (2, 2));
            r.Usun("Ala");
            r.Dodaj("Bram", (2, 2));
            Harness.Rowne("usunięty zwalnia pole", (2, 2), r.Postacie["Bram"].Pole);

            r = new SymulacjaRuchu();
            r.Dodaj("Olek", (0, 0));
            r.UstawCel("Olek", (6, 0));
            Symuluj(r, 1.0);
            r.UstawCel("Olek", (0, 6));
            Symuluj(r, 12);
            Harness.Sprawdz("zmiana celu w marszu", r.Dotarl("Olek") && r.Postacie["Olek"].Pole == (0, 6));

            // Wolne pole wprost przed celem leży w zamkniętej kieszeni: miejsce trzeba
            // wybrać tam, dokąd da się dojść, inaczej postać zostałaby przy starym celu.
            r = new SymulacjaRuchu();
            r.Przebuduj(new HashSet<(int X, int Y)>
            {
                (2, 2), (3, 2), (4, 2), (2, 3), (4, 3), (2, 4), (3, 4), (4, 4),
            });
            r.Dodaj("Olek", (0, 0));
            r.UstawCel("Olek", (3, 2));
            Symuluj(r, 12);
            (int px, int py) = r.Postacie["Olek"].Pole;
            Harness.Sprawdz("nie wybiera wolnego pola za murem",
                            r.Dotarl("Olek") && (px, py) != (3, 3) && Math.Max(Math.Abs(px - 3), Math.Abs(py - 2)) <= 1);

            // Jedno wolne pole i brama na dziewięciu ludzi: nadmiar czeka na dziedzińcu.
            var zastawiony = new HashSet<(int X, int Y)>();
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    bool plac = x < 7 && y < 7 && (x, y) != (3, 5) && (x, y) != (3, 6);
                    if (plac || x >= 7)
                    {
                        zastawiony.Add((x, y));
                    }
                }
            }
            r = new SymulacjaRuchu(9);
            r.Przebuduj(zastawiony);
            for (int i = 0; i < 9; i++)
            {
                r.Dodaj($"Osada{i}", (3, 5));
                r.UstawCel($"Osada{i}", (3, 5));
            }
            Symuluj(r, 15);
            var polaOsady = r.Postacie.Values.Select(p => p.Pole).ToList();
            Harness.Rowne("zastawiony plac: dziewięciu na osobnych polach", polaOsady.Count, polaOsady.Distinct().Count());
            Harness.Sprawdz("zastawiony plac: nikt w murze", !polaOsady.Any(p => zastawiony.Contains(p)));

            r = new SymulacjaRuchu();
            r.Dodaj("Olek", (1, 1));
            Symuluj(r, 1.0);
            Harness.BliskoSiebie("w bezruchu dystans nie rośnie", 0.0, r.Postacie["Olek"].Dystans);
            r.UstawCel("Olek", (1, 4));
            Symuluj(r, 5.0);
            Harness.BliskoSiebie("dystans rośnie tylko w marszu", 3.0, r.Postacie["Olek"].Dystans, 0.01);
        }

        // ------------------------------------------------------------------ //
        //  Zgodność z Pythonem                                                //
        // ------------------------------------------------------------------ //

        private static (int X, int Y) Pole(JsonElement e)
        {
            return (e[0].GetInt32(), e[1].GetInt32());
        }

        private static List<(int X, int Y)> ListaPol(JsonElement e)
        {
            return e.EnumerateArray().Select(Pole).ToList();
        }

        private static void ZgodnoscSciezek(JsonElement wzorce)
        {
            foreach (JsonElement p in wzorce.GetProperty("sciezki").EnumerateArray())
            {
                string opis = p.GetProperty("opis").GetString();
                var siatka = new Siatka(7, ListaPol(p.GetProperty("zablokowane")));
                var dodatkowo = new HashSet<(int X, int Y)>(ListaPol(p.GetProperty("dodatkowo")));
                var trasa = GraSimply.Logika.Sciezki.Znajdz(siatka, Pole(p.GetProperty("start")),
                                                             Pole(p.GetProperty("cel")), dodatkowo);
                JsonElement oczekiwana = p.GetProperty("trasa");
                if (oczekiwana.ValueKind == JsonValueKind.Null)
                {
                    Harness.Sprawdz($"trasa „{opis}”: brak, jak w Pythonie", trasa == null);
                    continue;
                }
                var wzor = ListaPol(oczekiwana);
                Harness.Sprawdz($"trasa „{opis}”: te same pola co w Pythonie ({wzor.Count})",
                                trasa != null && trasa.SequenceEqual(wzor));
            }

            foreach (JsonElement n in wzorce.GetProperty("najblizsze").EnumerateArray())
            {
                var siatka = new Siatka(7, ListaPol(n.GetProperty("zablokowane")));
                (int X, int Y)? wynik = siatka.NajblizszeWolne(Pole(n.GetProperty("pole")));
                JsonElement wzor = n.GetProperty("wynik");
                bool zgodne = wzor.ValueKind == JsonValueKind.Null
                    ? !wynik.HasValue
                    : wynik.HasValue && wynik.Value == Pole(wzor);
                Harness.Sprawdz($"najbliższe wolne dla {n.GetProperty("pole")}: jak w Pythonie", zgodne);
            }
        }

        private static void ZgodnoscRytmu(JsonElement wzorce)
        {
            foreach (JsonElement r in wzorce.GetProperty("rytm").EnumerateArray())
            {
                double t = r.GetProperty("t").GetDouble();
                Harness.Rowne($"faza dla t={t}", r.GetProperty("faza").GetString(), RytmDnia.Faza(t));
                Harness.BliskoSiebie($"światło dla t={t}", r.GetProperty("swiatlo").GetDouble(),
                                     RytmDnia.Swiatlo(t), Tolerancja);
            }

            int zgodne = 0, wszystkie = 0;
            foreach (JsonElement z in wzorce.GetProperty("zadania").EnumerateArray())
            {
                var o = new Osadnik { Imie = z.GetProperty("imie").GetString(), Chory = z.GetProperty("chory").GetInt32() };
                JsonElement tw = z.GetProperty("tawerna");
                (int X, int Y)? tawerna = tw.ValueKind == JsonValueKind.Null ? ((int, int)?)null : Pole(tw);
                var wynik = RytmDnia.Zadanie(o, (1, 1), (2, 2), (3, 3), tawerna, z.GetProperty("pora").GetString());
                wszystkie++;
                if (wynik.Pole == Pole(z.GetProperty("pole")) && wynik.Czynnosc == z.GetProperty("czynnosc").GetString())
                {
                    zgodne++;
                }
            }
            Harness.Rowne($"zadanie osadnika: {wszystkie} przypadków jak w Pythonie", wszystkie, zgodne);

            foreach (JsonProperty los in wzorce.GetProperty("losy").EnumerateObject())
            {
                Harness.Rowne($"los z imienia „{los.Name}”", los.Value.GetInt64(), RytmDnia.LosImienia(los.Name));
            }
        }

        private static void ZgodnoscSymulacji(JsonElement wzorce)
        {
            foreach (JsonElement s in wzorce.GetProperty("symulacje").EnumerateArray())
            {
                string nazwa = s.GetProperty("nazwa").GetString();
                var r = new SymulacjaRuchu(s.GetProperty("rozmiar").GetInt32());
                r.Przebuduj(new HashSet<(int X, int Y)>(ListaPol(s.GetProperty("zablokowane"))));
                foreach (JsonElement p in s.GetProperty("postacie").EnumerateArray())
                {
                    r.Dodaj(p[0].GetString(), Pole(p[1]));
                }

                double dt = s.GetProperty("dt").GetDouble();
                int kroki = s.GetProperty("kroki").GetInt32();
                int co = s.GetProperty("co").GetInt32();
                var polecenia = s.GetProperty("polecenia").EnumerateArray().ToList();
                var przebudowy = s.GetProperty("przebudowy").EnumerateArray().ToList();
                var probki = s.GetProperty("probki").EnumerateArray().ToList();

                int numer = 0, zgodnePozycje = 0, zgodnePola = 0, zgodneDotarcia = 0, razem = 0;
                double najwiekszaRoznica = 0.0;
                string pierwszaRoznica = null;
                for (int krok = 0; krok < kroki; krok++)
                {
                    foreach (JsonElement pol in polecenia)
                    {
                        if (pol.GetProperty("krok").GetInt32() == krok)
                        {
                            r.UstawCel(pol.GetProperty("imie").GetString(), Pole(pol.GetProperty("pole")),
                                       pol.GetProperty("czynnosc").GetString());
                        }
                    }
                    foreach (JsonElement pr in przebudowy)
                    {
                        if (pr.GetProperty("krok").GetInt32() == krok)
                        {
                            r.Przebuduj(new HashSet<(int X, int Y)>(ListaPol(pr.GetProperty("zablokowane"))));
                        }
                    }
                    r.Aktualizuj(dt);
                    if ((krok + 1) % co != 0)
                    {
                        continue;
                    }
                    JsonElement probka = probki[numer++];
                    foreach (JsonProperty poz in probka.GetProperty("pozycje").EnumerateObject())
                    {
                        razem++;
                        (double x, double y) = r.Pozycja(poz.Name);
                        double roznica = Math.Max(Math.Abs(x - poz.Value[0].GetDouble()),
                                                  Math.Abs(y - poz.Value[1].GetDouble()));
                        najwiekszaRoznica = Math.Max(najwiekszaRoznica, roznica);
                        if (roznica <= Tolerancja)
                        {
                            zgodnePozycje++;
                        }
                        else if (pierwszaRoznica == null)
                        {
                            pierwszaRoznica = $"krok {krok + 1}, {poz.Name}: python ({poz.Value[0]}, {poz.Value[1]}), C# ({x:R}, {y:R})";
                        }
                        if (r.Postacie[poz.Name].Pole == Pole(probka.GetProperty("pola").GetProperty(poz.Name)))
                        {
                            zgodnePola++;
                        }
                        if (r.Dotarl(poz.Name) == probka.GetProperty("dotarl").GetProperty(poz.Name).GetBoolean())
                        {
                            zgodneDotarcia++;
                        }
                    }
                }
                Harness.Sprawdz($"symulacja „{nazwa}”: pozycje jak w Pythonie ({razem} próbek, " +
                                $"największa różnica {najwiekszaRoznica:E1})" +
                                (pierwszaRoznica == null ? "" : $" — {pierwszaRoznica}"),
                                zgodnePozycje == razem);
                Harness.Rowne($"symulacja „{nazwa}”: pola jak w Pythonie", razem, zgodnePola);
                Harness.Rowne($"symulacja „{nazwa}”: dotarcia jak w Pythonie", razem, zgodneDotarcia);
            }
        }
    }
}
