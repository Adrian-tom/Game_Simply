using System;
using System.Collections.Generic;
using System.Linq;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Harmonogram dnia osadników, przybysze do wyboru i ukryte ryzyko: choroba, zdrada, sabotaż.</summary>
    public static class TestyLudziWolnych
    {
        public static void Uruchom()
        {
            Harness.Grupa("Zomboid fantasy: harmonogram i przybysze");
            Harmonogram();
            Praca();
            Przybysze();
            Sekrety();
            Wypedzenie();
            Zapis();
            Interfejs();
        }

        private static SwiatWolny Oboz(int ludzi)
        {
            var s = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 8.0, ObozZalozony = true };
            s.Budowle[(3, 3)] = "ognisko";
            s.Budowle[(3, 4)] = "namiot";
            s.Budowle[(4, 4)] = "chata";
            s.Budowle[(5, 4)] = "chata";
            s.Plecak["jagody"] = 20;
            s.Plecak["drewno"] = 20;
            s.Obozowisko.Chaty = 8;
            s.Obozowisko.Surowce["zywnosc"] = 200;
            s.Obozowisko.Surowce["drewno"] = 100;
            s.Obozowisko.Surowce["kamien"] = 50;
            var r = new Losowanie(7);
            for (int i = 0; i < ludzi; i++)
            {
                Osada.ZatrudnijOsadnika(s.Obozowisko, r, "bezczynny", true);
                Osadnik o = s.Obozowisko.Osadnicy[i];
                o.Zajecie = "drwal";
                s.Postacie.Add(new PostacOsadnika { O = o, X = 1.5 + i, Y = 6.5, CelX = 1.5 + i, CelY = 6.5 });
            }
            return s;
        }

        /// <summary>Północ: pełna doba osady bez czekania, z bohaterem przy życiu.</summary>
        private static void Doba(SwiatWolny s)
        {
            s.Godzina = 23.999;
            s.Glod = s.Energia = s.Hp = 100;
            s.Aktualizuj(0.1, default);
            s.Wrogowie.Clear();
        }

        private static void Harmonogram()
        {
            Harness.Rowne("rano to pierwsza część doby", 0, SwiatWolny.NumerBloku(5.0));
            Harness.Rowne("południe to druga", 1, SwiatWolny.NumerBloku(12.0));
            Harness.Rowne("wieczór to trzecia", 2, SwiatWolny.NumerBloku(20.9));
            Harness.Rowne("noc to czwarta", 3, SwiatWolny.NumerBloku(21.0));
            Harness.Rowne("po północy nadal noc", 3, SwiatWolny.NumerBloku(4.9));

            SwiatWolny s = Oboz(2);
            PostacOsadnika p = s.Postacie[0];
            Harness.Sprawdz("domyślnie rano praca, a noc sen", p.Plan[0] == BlokPlanu.Praca && p.Plan[3] == BlokPlanu.Odpoczynek);
            s.PanelOtwarty = true;
            s.Aktualizuj(0.01, new Wejscie { Klik = "h:0" });
            Harness.Rowne("kafelek przełącza część doby", BlokPlanu.Odpoczynek, p.Plan[0]);
            Harness.Rowne("drugi osadnik bez zmian", BlokPlanu.Praca, s.Postacie[1].Plan[0]);
            s.Aktualizuj(0.01, new Wejscie { Klik = "h:wszyscy" });
            Harness.Rowne("plan dla wszystkich", BlokPlanu.Odpoczynek, s.Postacie[1].Plan[0]);
            s.Postacie[1].Plan[1] = BlokPlanu.Straz;
            Harness.Rowne("każdy ma własną kopię planu", BlokPlanu.Praca, p.Plan[1]);

            // Sen wynika z planu, a nie z pory doby.
            SwiatWolny d = Oboz(1);
            d.Postacie[0].Plan = new[] { BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek };
            for (int i = 0; i < 100; i++)
            {
                d.Aktualizuj(0.1, default);
            }
            Harness.Sprawdz("plan pozwala spać za dnia", d.Postacie[0].Spi || d.Postacie[0].Idzie);

            // Nocna warta.
            SwiatWolny n = Oboz(2);
            n.Godzina = 22.0;
            n.X = 30.5;
            n.Y = 0.5;
            PostacOsadnika cywil = n.Postacie[0], warta = n.Postacie[1];
            warta.Plan = new[] { BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Wolne, BlokPlanu.Straz };
            var g = new Wrog { Typ = TypWroga.Wojownik, X = warta.X + 5, Y = warta.Y, Hp = 120, MaxHp = 120, Stan = StanWroga.Stoi };
            n.Wrogowie.Add(g);
            n.Aktualizuj(0.02, default);
            Harness.Sprawdz("osadnik na warcie walczy", warta.WWalce);
            Harness.Sprawdz("śpiący nie walczy", !cywil.WWalce);

            SwiatWolny st = Oboz(1);
            st.Godzina = 8.0;
            st.Wybrany = 0;
            st.PanelOtwarty = true;
            st.Aktualizuj(0.01, new Wejscie { Klik = "z:straznik" });
            Harness.Rowne("strażnik dostaje nocną wartę", BlokPlanu.Straz, st.Postacie[0].Plan[3]);
            Harness.Rowne("a rano śpi", BlokPlanu.Odpoczynek, st.Postacie[0].Plan[0]);
            var blisko = new Wrog { Typ = TypWroga.Slugus, X = st.Postacie[0].X + 8, Y = st.Postacie[0].Y, Hp = 28, MaxHp = 28, Stan = StanWroga.Stoi };
            st.Wrogowie.Add(blisko);
            st.X = 30.5;
            st.Aktualizuj(0.02, default);
            Harness.Sprawdz("śpiący strażnik nie reaguje na dalekiego wroga", !st.Postacie[0].WWalce);
            blisko.X = st.Postacie[0].X + 2;
            st.Aktualizuj(0.02, default);
            Harness.Sprawdz("ale zrywa się, gdy wróg jest obok", st.Postacie[0].WWalce);
        }

        private static void Praca()
        {
            SwiatWolny s = Oboz(1);
            PostacOsadnika p = s.Postacie[0];
            s.X = 30.5;
            p.Plan = new[] { BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Praca };
            for (int i = 0; i < 6; i++)
            {
                Doba(s);
                p.O.Chory = 0;
            }
            Harness.Sprawdz("praca bez snu męczy", p.Zmeczenie >= 90);
            Harness.Rowne("zajęcie zostaje po liczeniu dnia", "drwal", p.O.Zajecie);

            SwiatWolny r = Oboz(1);
            r.X = 30.5;
            PostacOsadnika q = r.Postacie[0];
            q.Zmeczenie = 80;
            q.Plan = new[] { BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek };
            Doba(r);
            Harness.Sprawdz("sen zdejmuje zmęczenie", q.Zmeczenie < 80);

            double Drewno(BlokPlanu[] plan)
            {
                SwiatWolny w = Oboz(1);
                w.X = 30.5;
                w.Postacie[0].Plan = plan;
                w.Obozowisko.Surowce["drewno"] = 0;
                for (int i = 0; i < 10; i++)
                {
                    Doba(w);
                    w.Postacie[0].Zmeczenie = 0;
                    w.Postacie[0].O.Chory = 0;
                    w.Obozowisko.Surowce["zywnosc"] = 200;
                }
                return w.Obozowisko.Surowce["drewno"];
            }

            double bez = Drewno(new[] { BlokPlanu.Odpoczynek, BlokPlanu.Odpoczynek, BlokPlanu.Wolne, BlokPlanu.Odpoczynek });
            double dwa = Drewno(new[] { BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Wolne, BlokPlanu.Odpoczynek });
            double trzy = Drewno(new[] { BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Praca, BlokPlanu.Odpoczynek });
            Harness.Sprawdz($"plan bez pracy nie daje plonu ({bez} < {dwa})", bez < dwa);
            Harness.Sprawdz($"dodatkowa praca daje więcej ({dwa} < {trzy})", dwa < trzy);
        }

        private static SwiatWolny ZGosciem(string sekret = null)
        {
            SwiatWolny s = Oboz(0);
            var r = new Losowanie(9);
            s.Gosc = new Przybysz { O = Osada.NowyKandydat(r, new List<string>()), X = 4.5, Y = 2.5, Surowiec = "jagody", Ile = 3, Wskazowka = "Wygląda zwyczajnie.", Sekret = sekret };
            return s;
        }

        private static void Przybysze()
        {
            SwiatWolny s = Oboz(0);
            s.Hp = 100;
            s.Godzina = 4.9;
            s.Aktualizuj(0.1, default);
            Harness.Sprawdz("nocą nikogo nie ma", s.Gosc == null);
            s.Godzina = 22.0;
            s.Aktualizuj(0.01, default);
            s.Wrogowie.Clear();
            s.Godzina = 5.1;
            s.Aktualizuj(0.01, default);
            Harness.Sprawdz("pierwszy przybysz zjawia się o świcie", s.Gosc != null);
            Harness.Rowne("nikt nie wprowadza się sam", 0, s.Obozowisko.Osadnicy.Count);
            Harness.Sprawdz("przybysz stoi przy ognisku", Math.Abs(s.Gosc.X - 3.5) + Math.Abs(s.Gosc.Y - 3.5) < 6.0);
            Harness.Sprawdz("ma wskazówkę", !string.IsNullOrEmpty(s.Gosc.Wskazowka));
            Harness.Sprawdz("o coś prosi", s.Gosc.Ile > 0);
            Harness.Sprawdz("podpowiedź woła o decyzję", WidokWolny.Podpowiedz(s).StartsWith("G:"));

            SwiatWolny t = ZGosciem();
            string imie = t.Gosc.O.Imie;
            t.Aktualizuj(0.01, new Wejscie { Klik = "g:otworz" });
            Harness.Sprawdz("kafelek otwiera kartę przybysza", t.GoscOtwarty && WidokWolny.Panel(t)[0].Contains(imie));
            t.Aktualizuj(0.01, new Wejscie { Klik = "g:tak" });
            Harness.Rowne("przyjęcie dodaje osadnika", 1, t.Obozowisko.Osadnicy.Count);
            Harness.Rowne("przyjęcie kosztuje", 17, t.Razem("jagody") - t.Obozowisko.Surowce.Wez("zywnosc"));
            Harness.Rowne("osadnik jest na mapie", 1, t.Postacie.Count);
            Harness.Sprawdz("przybysza już nie ma", t.Gosc == null);

            SwiatWolny b = ZGosciem();
            b.Plecak["jagody"] = 0;
            b.Obozowisko.Surowce["zywnosc"] = 0;
            b.Gosc.Ile = 3;
            b.Aktualizuj(0.01, new Wejscie { Klik = "g:tak" });
            Harness.Rowne("bez zapłaty nie zostaje", 0, b.Obozowisko.Osadnicy.Count);
            Harness.Sprawdz("a dalej czeka", b.Gosc != null);

            SwiatWolny c = ZGosciem();
            c.Obozowisko.Chaty = 0;
            c.Budowle.Clear();
            c.Budowle[(3, 3)] = "ognisko";
            c.Obozowisko.Chaty = 0;
            c.Aktualizuj(0.01, new Wejscie { Klik = "g:tak" });
            Harness.Rowne("bez miejsca nie zostaje", 0, c.Obozowisko.Osadnicy.Count);

            SwiatWolny n = ZGosciem();
            n.Aktualizuj(0.01, new Wejscie { Klik = "g:nie" });
            Harness.Sprawdz("odprawiony odchodzi", n.Gosc == null && n.Obozowisko.Osadnicy.Count == 0);

            SwiatWolny w = ZGosciem();
            w.Godzina = 20.0;
            w.Aktualizuj(0.01, default);
            Harness.Sprawdz("bez decyzji odchodzi wieczorem", w.Gosc == null);

            // Statystycznie: wskazówki mylą i sekrety się zdarzają, ale nie zawsze.
            int zSekretem = 0, mylacych = 0, trafnych = 0;
            for (int i = 0; i < 300; i++)
            {
                var k = new SwiatWolny(1000 + i) { WalkaWlaczona = true, Godzina = 22.0, ObozZalozony = true };
                k.Budowle[(3, 3)] = "ognisko";
                k.Budowle[(3, 4)] = "namiot";
                k.Obozowisko.Chaty = 2;
                k.Aktualizuj(0.01, default);
                k.Wrogowie.Clear();
                k.Godzina = 5.1;
                k.Aktualizuj(0.01, default);
                if (k.Gosc == null)
                {
                    continue;
                }
                bool podejrzany = !k.Gosc.Wskazowka.StartsWith("Wygląda");
                zSekretem += k.Gosc.Sekret != null ? 1 : 0;
                mylacych += podejrzany && k.Gosc.Sekret == null ? 1 : 0;
                trafnych += podejrzany && k.Gosc.Sekret != null ? 1 : 0;
            }
            Harness.Sprawdz($"część przybyszów ma sekret ({zSekretem}/300)", zSekretem > 30 && zSekretem < 150);
            Harness.Sprawdz($"wskazówka czasem trafia ({trafnych})", trafnych > 10);
            Harness.Sprawdz($"a czasem myli uczciwego ({mylacych})", mylacych > 10);
        }

        private static void Sekrety()
        {
            string[] Scenariusz(string sekret, int dni, out SwiatWolny s)
            {
                s = Oboz(3);
                s.X = 30.5;
                s.Budowle[(8, 8)] = "palisada";
                s.Postacie[0].Sekret = sekret;
                for (int i = 0; i < dni; i++)
                {
                    Doba(s);
                    if (s.Postacie.All(q => q.Sekret != sekret))
                    {
                        break;      // dziennik jest krótki: czytamy go zaraz po zdarzeniu
                    }
                }
                return s.Dziennik.ToArray();
            }

            string[] d = Scenariusz(SwiatWolny.SekretChoroba, 20, out SwiatWolny c);
            Harness.Sprawdz("ukryta choroba w końcu wychodzi", d.Any(l => l.Contains("gorączkę")));
            Harness.Sprawdz("i wychodzi tylko raz", c.Postacie[0].Sekret == null);

            d = Scenariusz(SwiatWolny.SekretZdrada, 40, out SwiatWolny z);
            Harness.Sprawdz("zdrajca ucieka", d.Any(l => l.StartsWith("Zdrada")));
            Harness.Rowne("zdrajcy nie ma w osadzie", 2, z.Obozowisko.Osadnicy.Count);
            Harness.Rowne("i postaci też nie", 2, z.Postacie.Count);

            SwiatWolny wczesnie = Oboz(1);
            wczesnie.X = 30.5;
            wczesnie.Postacie[0].Sekret = SwiatWolny.SekretZdrada;
            for (int i = 0; i < 4; i++)
            {
                Doba(wczesnie);
            }
            Harness.Rowne("zdrada nie przychodzi od razu", 1, wczesnie.Obozowisko.Osadnicy.Count);

            d = Scenariusz(SwiatWolny.SekretSabotaz, 25, out SwiatWolny b);
            Harness.Sprawdz("sabotaż uderza bez podpisu", d.Any(l => l.Contains("zapasów") || l.Contains("muru")));
            Harness.Sprawdz("dziennik nie zdradza sprawcy", !d.Where(l => l.Contains("zapasów") || l.Contains("muru")).Any(l => l.Contains(b.Obozowisko.Osadnicy[0].Imie)));
            Harness.Sprawdz("ślad w końcu pada na kogoś", b.Postacie.Any(p => p.Podejrzany));
            Harness.Sprawdz("panel pokazuje podejrzenie", WidokPanelu(b).Any(l => l.Contains("?")));
        }

        private static List<string> WidokPanelu(SwiatWolny s)
        {
            s.PanelOtwarty = true;
            return WidokWolny.Panel(s);
        }

        private static void Wypedzenie()
        {
            SwiatWolny s = Oboz(3);
            s.Postacie[1].Sekret = SwiatWolny.SekretZdrada;
            double przed = s.Obozowisko.Osadnicy[0].Morale;
            s.PanelOtwarty = true;
            s.Wybrany = 1;
            s.Aktualizuj(0.01, new Wejscie { Klik = "x:wygnaj" });
            Harness.Rowne("wypędzony znika", 2, s.Obozowisko.Osadnicy.Count);
            Harness.Sprawdz("gra nie mówi, czy słusznie", !s.Dziennik.Last().Contains("zdraj"));
            double winny = przed - s.Obozowisko.Osadnicy[0].Morale;

            SwiatWolny n = Oboz(3);
            n.PanelOtwarty = true;
            n.Wybrany = 1;
            double p2 = n.Obozowisko.Osadnicy[0].Morale;
            n.Aktualizuj(0.01, new Wejscie { Klik = "x:wygnaj" });
            Harness.Sprawdz("niewinny kosztuje morale więcej", p2 - n.Obozowisko.Osadnicy[0].Morale > winny);
        }

        private static void Zapis()
        {
            SwiatWolny s = Oboz(2);
            s.Postacie[0].Plan = new[] { BlokPlanu.Straz, BlokPlanu.Wolne, BlokPlanu.Praca, BlokPlanu.Odpoczynek };
            s.Postacie[0].Zmeczenie = 33.5;
            s.Postacie[0].Sekret = SwiatWolny.SekretSabotaz;
            s.Postacie[0].DniWOsadzie = 4;
            s.Postacie[0].Podejrzany = true;
            s.Gosc = new Przybysz { O = Osada.NowyKandydat(new Losowanie(3), new List<string>()), X = 4.5, Y = 2.5, Surowiec = "drewno", Ile = 5, Wskazowka = "Kaszle i jest blady.", Sekret = SwiatWolny.SekretChoroba };
            string tekst = s.DoZapisu();
            SwiatWolny w = SwiatWolny.ZZapisu(tekst);
            PostacOsadnika p = w.Postacie[0];
            Harness.RowneCiagi("plan doby wraca", s.Postacie[0].Plan.Select(b => b.ToString()), p.Plan.Select(b => b.ToString()));
            Harness.Sprawdz("zmęczenie i dni wracają", p.Zmeczenie == 33.5 && p.DniWOsadzie == 4);
            Harness.Rowne("sekret zostaje ukryty, ale w zapisie", SwiatWolny.SekretSabotaz, p.Sekret);
            Harness.Sprawdz("podejrzenie wraca", p.Podejrzany);
            Harness.Sprawdz("drugi osadnik bez sekretu", w.Postacie[1].Sekret == null);
            Harness.Sprawdz("przybysz czeka dalej", w.Gosc != null && w.Gosc.O.Imie == s.Gosc.O.Imie && w.Gosc.Ile == 5 && w.Gosc.Sekret == SwiatWolny.SekretChoroba);
            Harness.Sprawdz("umiejętności przybysza wracają", w.Gosc.O.Kompetencje != null && w.Gosc.O.Kompetencje.Count == s.Gosc.O.Kompetencje.Count);
            Harness.Rowne("zapis wczytanego jest taki sam", tekst, w.DoZapisu());

            string stary = tekst.Replace("SimplyPrzetrwanie\t2", "SimplyPrzetrwanie\t1");
            string[] linie = stary.Split('\n').Select(l =>
            {
                if (!l.StartsWith("osadnik\t"))
                {
                    return l;
                }
                string[] c = l.Split('\t');
                return string.Join("\t", c.Take(10));
            }).Where(l => !l.StartsWith("gosc\t") && !l.StartsWith("decyzje")).ToArray();
            SwiatWolny v1 = SwiatWolny.ZZapisu(string.Join("\n", linie));
            Harness.Sprawdz("zapis ze starszej wersji się wczytuje", v1.Obozowisko.Osadnicy.Count == 2 && v1.Postacie[0].Plan.Length == 4 && v1.Postacie[0].Sekret == null);
        }

        private static void Interfejs()
        {
            SwiatWolny s = Oboz(8);
            s.PanelOtwarty = true;
            s.Wybrany = 7;
            KafelkiWolne k = KafelkiWolne.Z(s);
            Harness.Rowne("cztery części doby na kafelkach", 4, k.Plan.Count);
            Harness.Rowne("dwie decyzje o osadniku", 2, k.Decyzje.Count);
            Harness.Rowne("panel mieści sześciu", WidokWolny.WierszyOsadnikow, k.Osadnikow);
            Harness.Rowne("lista przesuwa się za wyborem", 2, k.PierwszyOsadnik);
            Harness.Sprawdz("wybrany jest widoczny", WidokWolny.Panel(s).Any(l => l.StartsWith(">") && l.Contains(s.Obozowisko.Osadnicy[7].Imie)));
            Harness.Sprawdz("panel pokazuje zmęczenie", WidokWolny.Panel(s).Any(l => l.Contains("zm.")));

            SwiatWolny g = ZGosciem();
            k = KafelkiWolne.Z(g);
            Harness.Rowne("przybysz dodaje kafelek do paska", 6, k.Akcje.Count);
            g.Aktualizuj(0.01, new Wejscie { Klik = "g:otworz" });
            k = KafelkiWolne.Z(g);
            Harness.Sprawdz("karta ma Przyjmij i Odpraw", k.Decyzje.Select(d => d.Id).SequenceEqual(new[] { "g:tak", "g:nie" }));
            Harness.Sprawdz("karta pokazuje cenę i wskazówkę", WidokWolny.Panel(g).Any(l => l.StartsWith("Prosi")) && WidokWolny.Panel(g).Any(l => l.StartsWith("Wrażenie")));
            g.Wycofaj();
            Harness.Sprawdz("Esc zamyka kartę, a przybysz czeka", !g.GoscOtwarty && !g.PanelOtwarty && g.Gosc != null);
        }
    }
}
