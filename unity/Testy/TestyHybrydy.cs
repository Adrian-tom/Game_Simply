using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Grafika.Hybryda;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Styl hybrydowy: układ placu osady, chodzący osadnicy i światło sceny.
    ///
    /// Układ i ruch sprawdzamy liczbami (czy budynek stoi na swoim polu, czy nikt
    /// nie wchodzi w ścianę i nie stoi na jednym polu z drugim). Obraz — tym, co
    /// da się stwierdzić bez oglądania: że scena coś rysuje, że noc jest ciemniejsza
    /// od dnia, że ognisko świeci. Wygląd ocenia się okiem na podglądzie.
    /// </summary>
    public static class TestyHybrydy
    {
        private static readonly (string Imie, string Zajecie)[] Ludzie =
        {
            ("Jagna", "rolnik"), ("Olek", "drwal"), ("Wit", "straznik"), ("Hela", "handlarz"),
            ("Torin", "tracz"), ("Mila", "uzdrowiciel"), ("Bram", "rzemieslnik"),
            ("Nessa", "zielarz"), ("Gareth", "kamieniarz"),
        };

        public static void Uruchom()
        {
            Harness.Grupa("Układ placu osady");
            Uklad();
            Harness.Grupa("Scena osady: obraz i światło");
            Obraz();
            Harness.Grupa("Scena osady: osadnicy chodzą");
            Chodzenie();
            Harness.Grupa("Styl hybrydowy w pulpicie");
            Pulpit_();
        }

        private static StanOsady Stan(int chaty, Dictionary<string, int> budynki, int ludzi, string pora = "lato",
                                      Func<int, bool> chory = null)
        {
            var s = new StanOsady { Chaty = chaty, Pora = pora, Klasa = "wojownik" };
            foreach (KeyValuePair<string, int> b in budynki)
            {
                s.Budynki[b.Key] = b.Value;
            }
            for (int i = 0; i < ludzi && i < Ludzie.Length; i++)
            {
                s.Osadnicy.Add(new OsadnikDoRysunku
                {
                    Imie = Ludzie[i].Imie, Zajecie = Ludzie[i].Zajecie, Morale = 60,
                    Chory = chory != null && chory(i),
                });
            }
            return s;
        }

        private static readonly Dictionary<string, int> Srednia = new Dictionary<string, int>
        {
            { "dom", 1 }, { "kuznia", 1 }, { "tawerna", 1 }, { "farma", 2 }, { "tartak", 1 },
            { "palisada", 1 }, { "targ", 1 }, { "lecznica", 1 },
        };

        private static Dictionary<string, int> Pelna()
        {
            var d = new Dictionary<string, int>();
            foreach (string k in Migawka.BudynkiPlacu)
            {
                d[k] = 1;
            }
            return d;
        }

        // ------------------------------------------------------------------ //
        //  Układ                                                              //
        // ------------------------------------------------------------------ //

        private static void Uklad()
        {
            var plan = UkladPlacu.Rozklad(Stan(0, new Dictionary<string, int> { { "kuznia", 2 } }, 0));
            Harness.Rowne("budynek staje na swoim polu", "kuznia", plan[UkladPlacu.Miejsca["kuznia"]].Klucz);
            Harness.Rowne("z poziomem", 2, plan[UkladPlacu.Miejsca["kuznia"]].Poziom);

            plan = UkladPlacu.Rozklad(Stan(0, new Dictionary<string, int>(), 0));
            Harness.Sprawdz("niezbudowanego budynku nie ma na placu", !plan.Values.Any(w => w.Klucz == "kuznia"));

            plan = UkladPlacu.Rozklad(Stan(12, new Dictionary<string, int> { { "kuznia", 1 } }, 0));
            Harness.Rowne("chata nie staje na budynku", "kuznia", plan[UkladPlacu.Miejsca["kuznia"]].Klucz);
            Harness.Rowne("dwanaście chat się mieści", 12, plan.Values.Count(w => w.Rodzaj == "chata"));

            Harness.Sprawdz("namiot zajmuje miejsce domu, gdy domu nie ma",
                            UkladPlacu.Rozklad(Stan(1, new Dictionary<string, int>(), 0)).Values.Any(w => w.Rodzaj == "namiot"));
            Harness.Sprawdz("namiot ustępuje domowi",
                            !UkladPlacu.Rozklad(Stan(1, new Dictionary<string, int> { { "dom", 1 } }, 0))
                                .Values.Any(w => w.Rodzaj == "namiot"));

            var zablokowane = UkladPlacu.Zablokowane(UkladPlacu.Rozklad(Stan(2, Srednia, 0)));
            Harness.Sprawdz("brama jest przejściem", !zablokowane.Contains(UkladPlacu.Brama));
            Harness.Sprawdz("reszta obwodu zablokowana", zablokowane.Contains((0, 3)) && zablokowane.Contains((6, 2)));
            Harness.Sprawdz("ognisko i bohater zagradzają drogę",
                            zablokowane.Contains(UkladPlacu.Ognisko) && zablokowane.Contains(UkladPlacu.PoleBohatera));
            Harness.Sprawdz("kuźnia zagradza drogę", zablokowane.Contains(UkladPlacu.Miejsca["kuznia"]));
            Harness.Sprawdz("po polu uprawnym się chodzi", !zablokowane.Contains(UkladPlacu.Miejsca["farma"]));

            var planTartaku = UkladPlacu.Rozklad(Stan(0, new Dictionary<string, int> { { "tartak", 1 } }, 0));
            Harness.Rowne("osadnik stoi przy swoim warsztacie", UkladPlacu.Miejsca["tartak"],
                          UkladPlacu.Stanowisko("tracz", planTartaku));
            Harness.Rowne("bez warsztatu wraca pod ognisko", UkladPlacu.Ognisko,
                          UkladPlacu.Stanowisko("tracz", UkladPlacu.Rozklad(Stan(0, new Dictionary<string, int>(), 0))));
            Harness.Rowne("drwal pracuje na skraju", (1, 1), UkladPlacu.Stanowisko("drwal", planTartaku));

            Harness.Rowne("bez chat śpią w namiocie albo domu", 1,
                          UkladPlacu.Domy(UkladPlacu.Rozklad(Stan(0, new Dictionary<string, int>(), 0))).Count);
            Harness.Rowne("z chatami śpią w chatach", 4,
                          UkladPlacu.Domy(UkladPlacu.Rozklad(Stan(4, new Dictionary<string, int>(), 0))).Count);
            Harness.Rowne("palisada ma obwód bez bramy", 4 * (UkladPlacu.Siatka - 1) - 1,
                          UkladPlacu.PolaPalisady(1).Count);
            Harness.Rowne("bez palisady nie ma pól", 0, UkladPlacu.PolaPalisady(0).Count);
        }

        // ------------------------------------------------------------------ //
        //  Obraz                                                              //
        // ------------------------------------------------------------------ //

        private static Plotno Klatka(StanOsady osada, double czasDnia, bool zmierzch = false, ScenaOsady scena = null)
        {
            scena = scena ?? new ScenaOsady();
            var plotno = new Plotno(ScenaOsady.Szer, ScenaOsady.Wys, przezroczyste: false);
            scena.Rysuj(plotno, osada, czasDnia - ScenaOsady.PrzesuniecieDnia + RytmDnia.DlugoscDnia, zmierzch);
            return plotno;
        }

        private static double Srednia_(Plotno p, int x0 = 0, int y0 = 0, int szer = -1, int wys = -1)
        {
            szer = szer < 0 ? p.Szerokosc : szer;
            wys = wys < 0 ? p.Wysokosc : wys;
            double suma = 0;
            for (int y = y0; y < y0 + wys; y++)
            {
                for (int x = x0; x < x0 + szer; x++)
                {
                    Barwa b = p.Wez(x, y);
                    suma += b.R + b.G + b.B;
                }
            }
            return suma / (szer * wys * 3.0);
        }

        private static bool Rozne(Plotno a, Plotno b)
        {
            return !a.Dane.SequenceEqual(b.Dane);
        }

        private static void Obraz()
        {
            double d = RytmDnia.DlugoscDnia;
            StanOsady osada = Stan(4, Srednia, 7);

            Plotno poludnie = Klatka(osada, d * 0.35);
            Harness.Rowne("klatka ma rozmiar sceny", (640, 360), (poludnie.Szerokosc, poludnie.Wysokosc));
            Harness.Sprawdz("scena nie jest pusta", Srednia_(poludnie) > 20);

            bool wszystkieKryjace = true;
            for (int i = 3; i < poludnie.Dane.Length; i += 4)
            {
                wszystkieKryjace &= poludnie.Dane[i] == 255;
            }
            Harness.Sprawdz("każdy piksel jest kryjący", wszystkieKryjace);

            Plotno noc = Klatka(osada, d * 0.93);
            double jasnyDzien = Srednia_(poludnie), jasnaNoc = Srednia_(noc);
            Harness.Sprawdz($"noc jest ciemniejsza od dnia ({jasnaNoc:F1} < {jasnyDzien:F1})", jasnaNoc < jasnyDzien * 0.7);

            // Ognisko świeci nocą jaśniej niż otoczenie: to sedno stylu Mroczny.
            (int sx, int sy) = RzutOsady.SrodekPola(UkladPlacu.Ognisko.X, UkladPlacu.Ognisko.Y);
            double przyOgniu = Srednia_(noc, sx - 12, sy - 14, 24, 20);
            double daleko = Srednia_(noc, 20, 300, 60, 40);
            Harness.Sprawdz($"ognisko świeci w nocy ({przyOgniu:F1} > {daleko:F1})", przyOgniu > daleko + 25);

            Harness.Sprawdz("F2 wymusza noc", Srednia_(Klatka(osada, d * 0.35, zmierzch: true)) < jasnyDzien * 0.7);
            Harness.Sprawdz("zima wygląda inaczej niż lato",
                            Rozne(poludnie, Klatka(Stan(4, Srednia, 7, "zima"), d * 0.35)));
            Harness.Sprawdz("jesień wygląda inaczej niż lato",
                            Rozne(poludnie, Klatka(Stan(4, Srednia, 7, "jesien"), d * 0.35)));
            Harness.Sprawdz("rozbudowa zmienia obraz",
                            Rozne(Klatka(Stan(2, new Dictionary<string, int>(), 0), d * 0.35),
                                  Klatka(Stan(2, new Dictionary<string, int> { { "kuznia", 1 }, { "palisada", 1 } }, 0), d * 0.35)));

            // Scena jest deterministyczna: ten sam stan i czas dają ten sam obraz.
            Harness.Sprawdz("ten sam stan daje ten sam obraz", !Rozne(Klatka(osada, d * 0.4), Klatka(osada, d * 0.4)));

            // Wspólna paleta: po kwantyzacji obraz ma niewiele różnych barw.
            var barwy = new HashSet<int>();
            for (int y = 0; y < poludnie.Wysokosc; y++)
            {
                for (int x = 0; x < poludnie.Szerokosc; x++)
                {
                    Barwa b = poludnie.Wez(x, y);
                    barwy.Add((b.R << 16) | (b.G << 8) | b.B);
                }
            }
            Harness.Sprawdz($"obraz mieści się we wspólnej palecie ({barwy.Count} barw)", barwy.Count <= 700);

            var scena = new ScenaOsady();
            Klatka(Stan(12, Pelna(), 9), d * 0.4, scena: scena);
            Harness.Rowne("pełna osada: każdy osadnik ma podpis", 9, scena.Podpisy.Count);
            bool wKadrze = scena.Podpisy.All(p => p.X >= 0 && p.X <= ScenaOsady.Szer && p.Y >= -10 && p.Y <= ScenaOsady.Wys);
            Harness.Sprawdz("podpisy mieszczą się w kadrze", wKadrze);
        }

        // ------------------------------------------------------------------ //
        //  Chodzenie                                                          //
        // ------------------------------------------------------------------ //

        private static void Chodzenie()
        {
            double d = RytmDnia.DlugoscDnia;
            StanOsady osada = Stan(4, Srednia, 7);
            var plan = UkladPlacu.Rozklad(osada);
            HashSet<(int X, int Y)> zablokowane = UkladPlacu.Zablokowane(plan);

            var scena = new ScenaOsady();
            var plotno = new Plotno(ScenaOsady.Szer, ScenaOsady.Wys, przezroczyste: false);
            bool wszedl = false, naStosie = false, skok = false;
            var poprzednie = new Dictionary<string, (double X, double Y)>();
            double t0 = d * 2;
            for (int i = 0; i < 1800; i++)
            {
                double t = t0 + i * 0.05;
                scena.Rysuj(plotno, osada, t, false);
                var pola = new List<(int, int)>();
                foreach (OsadnikDoRysunku o in osada.Osadnicy)
                {
                    (double x, double y) = scena.Ruch.Pozycja(o.Imie);
                    wszedl |= zablokowane.Any(b => Math.Abs(x - b.X) < 0.45 && Math.Abs(y - b.Y) < 0.45);
                    pola.Add((scena.Ruch.Postacie[o.Imie].Pole.X, scena.Ruch.Postacie[o.Imie].Pole.Y));
                    if (i > 0 && poprzednie.TryGetValue(o.Imie, out (double X, double Y) p))
                    {
                        double dx = x - p.X, dy = y - p.Y;
                        skok |= Math.Sqrt(dx * dx + dy * dy) > SymulacjaRuchu.Predkosc * 0.05 + 1e-6;
                    }
                    poprzednie[o.Imie] = (x, y);
                }
                naStosie |= pola.Count != pola.Distinct().Count();
            }
            Harness.Sprawdz("nikt nie wchodzi w budynek, ognisko ani palisadę", !wszedl);
            Harness.Sprawdz("nikt nie stoi na jednym polu z drugim", !naStosie);
            Harness.Sprawdz("nikt nie teleportuje się między klatkami", !skok);

            // Pełna osada: plac zastawiony, a osadnicy dalej mają osobne pola.
            var pelna = Stan(12, Pelna(), 9);
            scena = new ScenaOsady();
            bool stos = false;
            for (int i = 0; i < 1200; i++)
            {
                scena.Rysuj(plotno, pelna, t0 + i * 0.1, false);
                var pola = pelna.Osadnicy.Select(o => scena.Ruch.Postacie[o.Imie].Pole).ToList();
                stos |= pola.Count != pola.Distinct().Count();
            }
            Harness.Sprawdz("pełna osada: dziewięciu osadników na osobnych polach", !stos);
            Harness.Rowne("pełna osada: wszyscy mają postać", 9, scena.Ruch.Postacie.Count);

            // Rytm: w nocy śpią przy chatach, w pracy stoją przy stanowiskach.
            scena = new ScenaOsady();
            scena.Rysuj(plotno, osada, d * 3 + d * 0.93 - ScenaOsady.PrzesuniecieDnia, false);
            var domy = UkladPlacu.Domy(plan);
            bool wszyscyPrzyDomu = osada.Osadnicy.All(o =>
            {
                (int X, int Y) pole = scena.Ruch.Postacie[o.Imie].Pole;
                return domy.Any(h => Math.Max(Math.Abs(pole.X - h.X), Math.Abs(pole.Y - h.Y)) <= 1);
            });
            Harness.Sprawdz("w nocy wszyscy są przy chatach", wszyscyPrzyDomu);

            scena = new ScenaOsady();
            scena.Rysuj(plotno, osada, d * 3 + d * 0.35 - ScenaOsady.PrzesuniecieDnia, false);
            (int X, int Y) tartak = UkladPlacu.Miejsca["tartak"];
            (int X, int Y) polTorina = scena.Ruch.Postacie["Torin"].Pole;
            string czynnoscZdrowego = scena.Ruch.Postacie["Torin"].Czynnosc;
            Harness.Sprawdz("w pracy tracz stoi przy tartaku",
                            Math.Max(Math.Abs(polTorina.X - tartak.X), Math.Abs(polTorina.Y - tartak.Y)) <= 1);

            // Chory nie idzie do pracy.
            var chorzy = Stan(4, Srednia, 7, chory: i => i == 4);   // Torin
            scena = new ScenaOsady();
            scena.Rysuj(plotno, chorzy, d * 3 + d * 0.35 - ScenaOsady.PrzesuniecieDnia, false);
            // Chata sąsiaduje z tartakiem, więc po samym polu tego nie widać — liczy się czynność.
            Harness.Rowne("chory tracz odpoczywa", "odpoczynek", scena.Ruch.Postacie["Torin"].Czynnosc);
            Harness.Rowne("zdrowy tracz pracuje", "praca", czynnoscZdrowego);

            // Przybysz wchodzi bramą, odejście usuwa postać.
            scena = new ScenaOsady();
            scena.Rysuj(plotno, osada, 100.0, false);
            scena.Rysuj(plotno, osada, 100.05, false);
            var z_nowym = Stan(4, Srednia, 7);
            z_nowym.Osadnicy.Add(new OsadnikDoRysunku { Imie = "Nowy", Zajecie = "drwal", Morale = 60 });
            scena.Rysuj(plotno, z_nowym, 100.1, false);
            (double nx, double ny) = scena.Ruch.Pozycja("Nowy");
            Harness.Sprawdz("przybysz pojawia się przy bramie",
                            ny > 4.5 && Math.Abs(nx - UkladPlacu.Brama.X) < 2.0);
            scena.Rysuj(plotno, osada, 100.15, false);
            Harness.Sprawdz("odejście osadnika usuwa jego postać", !scena.Ruch.Postacie.ContainsKey("Nowy"));

            // Po długiej przerwie scena nadrabia ruch.
            scena = new ScenaOsady();
            scena.Rysuj(plotno, osada, 100.0, false);
            var przed = osada.Osadnicy.ToDictionary(o => o.Imie, o => scena.Ruch.Pozycja(o.Imie));
            scena.Rysuj(plotno, osada, 100.0 + d * 0.6, false);
            Harness.Sprawdz("po długiej przerwie ludzie są gdzie indziej",
                            osada.Osadnicy.Any(o => scena.Ruch.Pozycja(o.Imie) != przed[o.Imie]));

            // Cofnięcie zegara (okno wystartowało od nowa) niczego nie psuje.
            scena = new ScenaOsady();
            scena.Rysuj(plotno, osada, 50.0, false);
            var pozycje = osada.Osadnicy.ToDictionary(o => o.Imie, o => scena.Ruch.Pozycja(o.Imie));
            scena.Rysuj(plotno, osada, 2.0, false);
            Harness.Sprawdz("cofnięcie zegara nie przesuwa nikogo",
                            osada.Osadnicy.All(o => scena.Ruch.Pozycja(o.Imie) == pozycje[o.Imie]));
        }

        // ------------------------------------------------------------------ //
        //  Pulpit                                                             //
        // ------------------------------------------------------------------ //

        private static void Pulpit_()
        {
            Styl poprzedni = Styl.Aktywny;
            try
            {
                Styl.Aktywny = Styl.Hybrydowy;
                Harness.Rowne("styl ma nazwę", "hybrydowy", Styl.Hybrydowy.Nazwa);
                Harness.Sprawdz("styl ma układ fantasy", Styl.Hybrydowy.UkladFantasy);
                Harness.Sprawdz("generator umie rysować osadę", Styl.Hybrydowy.Generator is IRysownikOsady);
                Harness.Sprawdz("generator zna kadr postaci", Styl.Hybrydowy.Generator is IKadrPostaci);
                Harness.Sprawdz("styl mroczny nie ma sceny osady", !(Styl.Mroczny.Generator is IRysownikOsady));
                Harness.Sprawdz("styl klasyczny nie ma sceny osady", !(Styl.Klasyczny.Generator is IRysownikOsady));
                Harness.Rowne("scena ma rozmiar jak w Mrocznym", Styl.Mroczny.Generator.SzerSceny, Styl.Hybrydowy.Generator.SzerSceny);

                Gracz gracz = Gracz_();
                var ekran = new Ekran { Gracz = gracz };
                Migawka mapa = ekran.Migawke();
                Harness.Sprawdz("bez otwartego widoku migawka nie niesie osady", mapa.Plac == null);

                ekran.Widok = "oboz";
                Migawka obozMigawka = ekran.Migawke();
                Harness.Sprawdz("w obozie migawka niesie osadę", obozMigawka.Plac != null);
                Harness.Rowne("z osadnikami", gracz.Osadnicy.Count, obozMigawka.Plac.Osadnicy.Count);
                Harness.Rowne("z liczbą chat", gracz.Chaty, obozMigawka.Plac.Chaty);
                Harness.Sprawdz("z porą roku", !string.IsNullOrEmpty(obozMigawka.Plac.Pora));

                var pulpit = new Pulpit();
                pulpit.Zloz(mapa.Pola, 1.0, mapa.Hud, mapa.Region, mapa.GraczXy, mapa.Klasa, false, mapa.Osada);
                byte[] zMapa = (byte[])pulpit.Klatka.Dane.Clone();
                pulpit.Zloz(obozMigawka.Pola, 1.0, obozMigawka.Hud, obozMigawka.Region, obozMigawka.GraczXy,
                            obozMigawka.Klasa, false, obozMigawka.Osada, default, null, obozMigawka.Plac);
                Harness.Sprawdz("obóz rysuje plac zamiast mapy", !zMapa.SequenceEqual(pulpit.Klatka.Dane));

                var napisy = pulpit.Zloz(obozMigawka.Pola, 1.0, obozMigawka.Hud, obozMigawka.Region, obozMigawka.GraczXy,
                                         obozMigawka.Klasa, false, obozMigawka.Osada, default, null, obozMigawka.Plac);
                foreach (OsadnikDoRysunku o in obozMigawka.Plac.Osadnicy)
                {
                    Harness.Sprawdz($"nad głową osadnika jest podpis „{o.Imie}”", napisy.Any(n => n.Tekst == o.Imie));
                }

                // Styl bez sceny osady ignoruje plac — nic się nie sypie.
                Styl.Aktywny = Styl.Mroczny;
                var inny = new Pulpit();
                inny.Zloz(mapa.Pola, 1.0, mapa.Hud, mapa.Region, mapa.GraczXy, mapa.Klasa, false, mapa.Osada);
                byte[] mrocznaMapa = (byte[])inny.Klatka.Dane.Clone();
                inny.Zloz(obozMigawka.Pola, 1.0, obozMigawka.Hud, obozMigawka.Region, obozMigawka.GraczXy,
                          obozMigawka.Klasa, false, obozMigawka.Osada, default, null, obozMigawka.Plac);
                Harness.Sprawdz("styl mroczny dalej pokazuje mapę w obozie", mrocznaMapa.SequenceEqual(inny.Klatka.Dane));
            }
            finally
            {
                Styl.Aktywny = poprzedni;
            }
        }

        private static Gracz Gracz_()
        {
            var gracz = new Gracz("Halina", "Druid", new Losowanie(2024)) { Seed = 7 };
            Mapa.ZapewnijMape(gracz);
            gracz.Chaty = 3;
            gracz.PoziomyBudynkow["kuznia"] = 1;
            gracz.PoziomyBudynkow["palisada"] = 1;
            gracz.Osadnicy.Add(new Osadnik { Imie = "Jagna", Zajecie = "rolnik", Morale = 60 });
            gracz.Osadnicy.Add(new Osadnik { Imie = "Olek", Zajecie = "drwal", Morale = 60 });
            return gracz;
        }
    }
}
