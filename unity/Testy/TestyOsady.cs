using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GraSimply.Logika;

namespace GraSimply.Testy
{
    /// <summary>
    /// Osada, budowa, badania i umiejętności — zgodność z wersją pythonową.
    /// Wzorce (<c>osada.json</c>) zrzuca <c>unity/narzedzia/wzorce_osady.py</c>:
    /// tabele przepisane ręcznie, wyniki funkcji na stałych danych i trzy scenariusze
    /// wielodniowe z migawką po każdym dniu (te same liczby, te same teksty, to samo losowanie).
    /// </summary>
    public static partial class TestyOsady
    {
        private const double Tol = 1e-9;

        public static void Uruchom(JsonElement w)
        {
            Harness.Grupa("Osada: losowanie napisem");
            LosowanieNapisu(w.GetProperty("losowanie_napisu"));
            Harness.Grupa("Osada: tabele zgodne z Pythonem");
            Tabele(w.GetProperty("tabele"));
            TabeleReszta(w.GetProperty("tabele"));
            Harness.Grupa("Umiejętności i pasje");
            Kompetencje_(w.GetProperty("kompetencje"));
            Harness.Grupa("Koszty budowy");
            Koszty(w.GetProperty("koszty"));
            Harness.Grupa("Badania: bramki i bonusy");
            Bramki(w.GetProperty("bramki"));
            Harness.Grupa("Osada: scenariusze wielodniowe");
            Scenariusze(w.GetProperty("scenariusze"));
            Harness.Grupa("Budowa i badania: własne sprawdzenia");
            Wlasne();
        }

        private static List<string> Napisy(JsonElement e)
        {
            return e.EnumerateArray().Select(x => x.GetString()).ToList();
        }

        private static string NapisLubNull(JsonElement e)
        {
            return e.ValueKind == JsonValueKind.Null ? null : e.GetString();
        }

        private static string Para(IEnumerable<KeyValuePair<string, int>> l)
        {
            return string.Join(";", l.Select(p => $"{p.Key}={p.Value}"));
        }

        private static List<KeyValuePair<string, int>> ParyInt(JsonElement e)
        {
            return e.EnumerateArray().Select(x => new KeyValuePair<string, int>(
                x[0].GetString(), x[1].GetInt32())).ToList();
        }

        private static void LosowanieNapisu(JsonElement w)
        {
            foreach (JsonProperty p in w.EnumerateObject())
            {
                JsonElement d = p.Value;
                string nazwa = p.Name.Length == 0 ? "(pusty napis)" : p.Name;
                Losowanie r = Losowanie.ZNapisu(p.Name);
                bool ok = d.GetProperty("random").EnumerateArray().All(x => r.Losowa() == x.GetDouble());
                Harness.Sprawdz($"„{nazwa}”: random() ×6", ok);
                r = Losowanie.ZNapisu(p.Name);
                ok = d.GetProperty("triangular").EnumerateArray()
                      .All(x => Math.Abs(r.Trojkatny(0, 6, 1.5) - x.GetDouble()) < 1e-15);
                Harness.Sprawdz($"„{nazwa}”: triangular(0, 6, 1.5) ×6", ok);
                r = Losowanie.ZNapisu(p.Name);
                List<int> probka = r.Probka(Enumerable.Range(0, 10).ToList(), 2);
                Harness.RowneCiagi($"„{nazwa}”: sample(10, 2)",
                                   d.GetProperty("sample").EnumerateArray().Select(x => x.GetInt32().ToString()),
                                   probka.Select(x => x.ToString()));
                ok = d.GetProperty("randint").EnumerateArray().All(x => r.Calkowita(2, 5) == x.GetInt32());
                Harness.Sprawdz($"„{nazwa}”: randint(2, 5) po próbce", ok);
            }
        }

        private static void Tabele(JsonElement t)
        {
            JsonElement[] budynki = t.GetProperty("budynki").EnumerateArray().ToArray();
            Harness.Rowne("liczba budynków", budynki.Length, Oboz.Budynki.Count);
            for (int i = 0; i < budynki.Length && i < Oboz.Budynki.Count; i++)
            {
                JsonElement b = budynki[i];
                KeyValuePair<string, OpisBudynku> c = Oboz.Budynki[i];
                bool ok = c.Key == b.GetProperty("klucz").GetString()
                    && c.Value.Nazwa == b.GetProperty("nazwa").GetString()
                    && c.Value.Ikona == b.GetProperty("ikona").GetString()
                    && c.Value.Max == b.GetProperty("max").GetInt32()
                    && c.Value.Utrzymanie == b.GetProperty("utrzymanie").GetInt32()
                    && c.Value.Opis == b.GetProperty("opis").GetString()
                    && c.Value.ZelazoOd == b.GetProperty("zelazo_od").GetInt32()
                    && Para(c.Value.Koszt) == Para(ParyInt(b.GetProperty("koszt")));
                Harness.Sprawdz($"budynek {b.GetProperty("klucz").GetString()}", ok);
            }

            JsonElement[] projekty = t.GetProperty("projekty").EnumerateArray().ToArray();
            Harness.Rowne("liczba projektów badań", projekty.Length, Badania.Projekty.Count);
            for (int i = 0; i < projekty.Length && i < Badania.Projekty.Count; i++)
            {
                JsonElement p = projekty[i];
                KeyValuePair<string, Projekt> c = Badania.Projekty[i];
                string bonus = string.Join(";", p.GetProperty("bonus").EnumerateArray()
                    .Select(x => $"{x[0].GetString()}={x[1].GetDouble():R}"));
                string mojBonus = string.Join(";", c.Value.Bonus.Select(x => $"{x.Key}={x.Value:R}"));
                bool ok = c.Key == p.GetProperty("id").GetString()
                    && c.Value.Nazwa == p.GetProperty("nazwa").GetString()
                    && c.Value.Era == p.GetProperty("era").GetInt32()
                    && c.Value.Koszt == p.GetProperty("koszt").GetInt32()
                    && c.Value.Opis == p.GetProperty("opis").GetString()
                    && string.Join(",", c.Value.Wymaga) == string.Join(",", Napisy(p.GetProperty("wymaga")))
                    && Para(c.Value.Odblokowuje) == Para(ParyInt(p.GetProperty("odblokowuje")))
                    && c.Value.Chaty == p.GetProperty("chaty").GetInt32()
                    && c.Value.EraPo == p.GetProperty("era_po").GetInt32()
                    && mojBonus == bonus;
                Harness.Sprawdz($"projekt {p.GetProperty("id").GetString()}", ok);
            }
        }

        private static void TabeleReszta(JsonElement t)
        {
            JsonElement[] zajecia = t.GetProperty("zajecia").EnumerateArray().ToArray();
            Harness.Rowne("liczba zajęć", zajecia.Length, Osada.Zajecia.Count);
            for (int i = 0; i < zajecia.Length && i < Osada.Zajecia.Count; i++)
            {
                JsonElement z = zajecia[i];
                OpisZajecia c = Osada.Zajecia[i].Value;
                bool ok = Osada.Zajecia[i].Key == z.GetProperty("klucz").GetString()
                    && c.Nazwa == z.GetProperty("nazwa").GetString() && c.Ikona == z.GetProperty("ikona").GetString()
                    && c.Opis == z.GetProperty("opis").GetString()
                    && c.Wymaga == NapisLubNull(z.GetProperty("wymaga"))
                    && c.Miejsca == z.GetProperty("miejsca").GetInt32();
                Harness.Sprawdz($"zajęcie {z.GetProperty("klucz").GetString()}", ok);
            }

            JsonElement[] cechy = t.GetProperty("cechy").EnumerateArray().ToArray();
            Harness.Rowne("liczba cech", cechy.Length, Osada.Cechy.Count);
            for (int i = 0; i < cechy.Length && i < Osada.Cechy.Count; i++)
            {
                JsonElement e = cechy[i];
                CechaOsadnika c = Osada.Cechy[i].Value;
                bool ok = Osada.Cechy[i].Key == e.GetProperty("klucz").GetString()
                    && c.Opis == e.GetProperty("opis").GetString()
                    && c.Praca == e.GetProperty("praca").GetDouble() && c.Je == e.GetProperty("je").GetInt32()
                    && c.Morale == e.GetProperty("morale").GetDouble() && c.Choroby == e.GetProperty("choroby").GetDouble();
                Harness.Sprawdz($"cecha {e.GetProperty("klucz").GetString()}", ok);
            }

            Harness.RowneCiagi("czas pracy budowy",
                t.GetProperty("praca").EnumerateArray().Select(x => $"{x[0].GetString()}={x[1].GetDouble():R}"),
                Budowa.Praca.Select(p => $"{p.Key}={p.Value:R}"));
            Harness.RowneCiagi("start: poziomy bez badań",
                t.GetProperty("start_poziomy").EnumerateArray().Select(x => $"{x[0].GetString()}={x[1].GetInt32()}"),
                Badania.StartPoziomy.Select(p => $"{p.Key}={p.Value}"));
            JsonElement st = t.GetProperty("stale");
            Harness.Rowne("skala kosztu badań", st.GetProperty("skala_kosztu").GetDouble(), Badania.SkalaKosztu);
            Harness.Rowne("baza punktów wiedzy", st.GetProperty("baza_punktow").GetDouble(), Badania.BazaPunktow);
            Harness.Rowne("limit pracy na plan", st.GetProperty("limit_planu").GetDouble(), Budowa.LimitPlanu);
            Harness.Rowne("chaty na starcie", st.GetProperty("start_chaty").GetInt32(), Badania.StartChaty);
            Harness.Rowne("koszt chaty", Para(ParyInt(st.GetProperty("koszt_chaty"))), Para(Osada.KosztChaty));
            Harness.RowneCiagi("umiejętności (kolejność)",
                t.GetProperty("umiejetnosci").EnumerateArray()
                 .Select(x => $"{x[0].GetString()}|{x[1].GetString()}|{x[2].GetString()}"),
                Kompetencje.Umiejetnosci.Select(u => $"{u.Key}|{u.Value.Nazwa}|{u.Value.Ikona}"));
            Harness.RowneCiagi("umiejętność zajęcia",
                t.GetProperty("umiejetnosc_zajecia").EnumerateArray().Select(x => $"{x[0].GetString()}={x[1].GetString()}"),
                Kompetencje.UmiejetnoscZajecia.Select(p => $"{p.Key}={p.Value}"));
        }

        // ------------------------------------------------------------------ //
        //  Umiejętności                                                       //
        // ------------------------------------------------------------------ //

        private static Osadnik Nowy(string imie, string zajecie = "rolnik", string cecha = "skromny", double morale = 60.0)
        {
            return Osada.Zapewnij(new Osadnik { Imie = imie, Zajecie = zajecie, Morale = morale, Cecha = cecha });
        }

        private static string Skille(Osadnik o)
        {
            return string.Join(";", Kompetencje.Umiejetnosci.Select(u =>
                $"{u.Key}:{o.Kompetencje[u.Key].P}:{o.Kompetencje[u.Key].Xp:R}:{o.Kompetencje[u.Key].Pasja}"));
        }

        private static void Kompetencje_(JsonElement w)
        {
            foreach (JsonElement e in w.GetProperty("osadnicy").EnumerateArray())
            {
                string imie = e.GetProperty("imie").GetString();
                string nazwa = imie.Length == 0 ? "(puste imię)" : imie;
                Osadnik o = Nowy(imie);
                string oczek = string.Join(";", e.GetProperty("umiejetnosci").EnumerateArray()
                    .Select(x => $"{x[0].GetString()}:{x[1].GetInt32()}:{x[2].GetDouble():R}:{x[3].GetInt32()}"));
                Harness.Rowne($"{nazwa}: umiejętności startowe z imienia", oczek, Skille(o));
                Harness.Rowne($"{nazwa}: opis krótki", e.GetProperty("krotki").GetString(), Kompetencje.OpisKrotki(o, 3));
                Harness.RowneCiagi($"{nazwa}: karta (5 pozycji)", Napisy(e.GetProperty("pelny")),
                                   Kompetencje.OpisPelny(o, 5, "rolnik"));
                Harness.RowneCiagi($"{nazwa}: karta pełna", Napisy(e.GetProperty("pelny_wszystko")),
                                   Kompetencje.OpisPelny(o));
            }

            foreach (JsonElement n in w.GetProperty("nauka").EnumerateArray())
            {
                int pasja = n.GetProperty("pasja").GetInt32();
                Osadnik o = Nowy("Nessa");
                o.Kompetencje["uprawa"].P = 0;
                o.Kompetencje["uprawa"].Xp = 0.0;
                o.Kompetencje["uprawa"].Pasja = pasja;
                var wiesci = new List<string>();
                bool probkiOk = true;
                JsonElement[] probki = n.GetProperty("probki").EnumerateArray().ToArray();
                for (int dzien = 0; dzien < 140; dzien++)
                {
                    wiesci.AddRange(Kompetencje.Cwicz(o, "rolnik", dzien % 3 != 0 ? 1.0 : 0.5));
                    Umiejetnosc u = o.Kompetencje["uprawa"];
                    probkiOk &= u.P == probki[dzien][0].GetInt32() && Math.Abs(u.Xp - probki[dzien][1].GetDouble()) < Tol;
                }
                Harness.Sprawdz($"nauka (pasja {pasja}): poziom i XP każdego dnia", probkiOk);
                Harness.RowneCiagi($"nauka (pasja {pasja}): komunikaty awansów", Napisy(n.GetProperty("wiesci")), wiesci);
            }

            int nr = 0;
            foreach (JsonElement m in w.GetProperty("migracje").EnumerateArray())
            {
                var dosw = m.GetProperty("dosw").EnumerateArray().ToDictionary(x => x[0].GetString(), x => x[1].GetInt32());
                var o = new Osadnik { Imie = "Stary", Zajecie = "rolnik" };
                Dictionary<string, Umiejetnosc> k = Kompetencje.Zapewnij(o, dosw);
                bool ok = m.GetProperty("umiejetnosci").EnumerateArray().All(x => k[x[0].GetString()].P == x[1].GetInt32());
                Harness.Sprawdz($"migracja gwiazdek #{++nr}", ok);
            }

            bool czynnikiOk = true;
            foreach (JsonElement c in w.GetProperty("czynniki").EnumerateArray())
            {
                Osadnik o = Nowy("Nessa", c[0].GetString());
                string u = Kompetencje.ZUmiejetnosciZajecia(c[0].GetString());
                if (u != null)
                {
                    o.Kompetencje[u].P = c[1].GetInt32();
                }
                czynnikiOk &= Math.Abs(Kompetencje.Czynnik(o) - c[2].GetDouble()) < Tol;
            }
            Harness.Sprawdz("czynnik wydajności od umiejętności", czynnikiOk);
            double[] krzywa = w.GetProperty("xp_krzywa").EnumerateArray().Select(x => x.GetDouble()).ToArray();
            Harness.Sprawdz("krzywa XP (poziomy −1…21)",
                Enumerable.Range(-1, 23).Select(p => Kompetencje.XpDoNastepnego(p))
                          .Zip(krzywa, (a, b) => Math.Abs(a - b) < Tol).All(x => x));
        }

        // ------------------------------------------------------------------ //
        //  Koszty i bramki                                                    //
        // ------------------------------------------------------------------ //

        public static Gracz Pusty(Losowanie rng = null)
        {
            var g = new Gracz("Wzorzec", "Wojownik", rng ?? new Losowanie(1));
            g.WObozie = true;
            g.Osadnicy.Clear();
            g.Wiezi.Clear();
            g.Talenty.Clear();
            return g;
        }

        private static void Koszty(JsonElement w)
        {
            bool ok = true;
            string pierwszyBlad = null;
            foreach (JsonElement e in w.EnumerateArray())
            {
                Gracz g = Pusty();
                g.Talenty.AddRange(Napisy(e.GetProperty("talent")));
                string klucz = e.GetProperty("klucz").GetString();
                int poziom = e.GetProperty("poziom").GetInt32();
                if (poziom > 1)
                {
                    Oboz.UstawPoziom(g, klucz, poziom - 1);
                }
                Dictionary<string, int> koszt = Oboz.KosztBudowy(g, klucz);
                bool brak = e.GetProperty("koszt").ValueKind == JsonValueKind.Null;
                string oczek = brak ? null : Para(ParyInt(e.GetProperty("koszt")));
                string mam = koszt == null ? null : Para(koszt);
                string tekstMam = koszt == null ? null : Oboz.FormatKosztu(koszt);
                if (oczek != mam || NapisLubNull(e.GetProperty("tekst")) != tekstMam)
                {
                    ok = false;
                    pierwszyBlad = pierwszyBlad ?? $"{klucz} poz. {poziom}: oczekiwano {oczek}, jest {mam}";
                }
            }
            Harness.Sprawdz("koszt i tekst kosztu każdego poziomu każdego budynku, z talentem i bez"
                            + (ok ? "" : "  (" + pierwszyBlad + ")"), ok);
        }

        private static void Bramki(JsonElement w)
        {
            int nr = 0;
            foreach (JsonElement s in w.EnumerateArray())
            {
                nr++;
                Gracz g = Pusty();
                int skr = s.GetProperty("skryptorium").GetInt32();
                if (skr > 0)
                {
                    Oboz.UstawPoziom(g, "skryptorium", skr);
                }
                Badania.Stan(g).Zrobione = Napisy(s.GetProperty("zrobione"));
                string opis = $"stan #{nr}";
                Harness.Rowne($"{opis}: epoka", s.GetProperty("era").GetInt32(), Badania.Era(g));
                Harness.Rowne($"{opis}: limit chat", s.GetProperty("limit_chat").GetInt32(), Badania.LimitChat(g));
                Harness.RowneCiagi($"{opis}: powody blokad",
                    s.GetProperty("blokady").EnumerateArray().Select(x => $"{x[0].GetString()}|{NapisLubNull(x[1])}"),
                    Badania.Projekty.Select(p => $"{p.Key}|{Badania.PowodBlokady(g, p.Key)}"));
                Harness.RowneCiagi($"{opis}: dostępne projekty", Napisy(s.GetProperty("dostepne")), Badania.Dostepne(g));
                Harness.RowneCiagi($"{opis}: maksymalne poziomy",
                    s.GetProperty("maks").EnumerateArray().Select(x => $"{x[0].GetString()}={x[1].GetInt32()}"),
                    Oboz.Budynki.Select(b => $"{b.Key}={Badania.MaksPoziom(g, b.Key)}"));
                Harness.Sprawdz($"{opis}: premie do zawodów",
                    s.GetProperty("bonus").EnumerateArray()
                     .All(x => Math.Abs(Badania.BonusZajecia(g, x[0].GetString()) - x[1].GetDouble()) < Tol));
                Harness.RowneCiagi($"{opis}: opisy efektów",
                    s.GetProperty("opisy").EnumerateArray().Select(x => $"{x[0].GetString()}|{x[1].GetString()}"),
                    Badania.Projekty.Select(p => $"{p.Key}|{Badania.OpisEfektow(p.Key)}"));
                Harness.Sprawdz($"{opis}: który projekt odblokowuje poziom",
                    s.GetProperty("projekt_dla").EnumerateArray()
                     .All(x => Badania.ProjektDla(x[0].GetString(), x[1].GetInt32()) == NapisLubNull(x[2])));
                Harness.Rowne($"{opis}: projekt na więcej chat",
                              NapisLubNull(s.GetProperty("projekt_dla_chat")), Badania.ProjektDlaChat(g));
            }
        }

        // ------------------------------------------------------------------ //
        //  Scenariusze                                                        //
        // ------------------------------------------------------------------ //

        private static Gracz ZeScenariusza(JsonElement stan)
        {
            Gracz g = Pusty();
            g.Czas = stan.GetProperty("czas").GetInt32();
            g.Zloto = stan.GetProperty("zloto").GetInt32();
            g.Chaty = stan.GetProperty("chaty").GetInt32();
            g.Narzedzia = stan.TryGetProperty("narzedzia", out JsonElement n) ? n.GetInt32() : 0;
            if (stan.TryGetProperty("talenty", out JsonElement t))
            {
                g.Talenty.AddRange(Napisy(t));
            }
            g.Surowce = Oboz.Surowce.ToDictionary(s => s.Key, s => 0);
            foreach (JsonProperty p in stan.GetProperty("surowce").EnumerateObject())
            {
                g.Surowce[p.Name] = p.Value.GetInt32();
            }
            foreach (JsonProperty p in stan.GetProperty("poziomy").EnumerateObject())
            {
                Oboz.UstawPoziom(g, p.Name, p.Value.GetInt32());
            }
            foreach (JsonElement o in stan.GetProperty("osadnicy").EnumerateArray())
            {
                g.Osadnicy.Add(new Osadnik
                {
                    Imie = o.GetProperty("imie").GetString(), Zajecie = o.GetProperty("zajecie").GetString(),
                    Cecha = o.GetProperty("cecha").GetString(), Morale = o.GetProperty("morale").GetDouble(),
                    Chory = o.GetProperty("chory").GetInt32(),
                });
            }
            if (stan.TryGetProperty("zrobione", out JsonElement z))
            {
                Badania.Stan(g).Zrobione = Napisy(z);
            }
            return g;
        }

        private static string Wykonaj(Gracz g, JsonElement a)
        {
            switch (a.GetProperty("typ").GetString())
            {
                case "zlec": return Budowa.Zlec(g, a.GetProperty("arg").GetString());
                case "projekt": return Badania.WybierzProjekt(g, a.GetProperty("arg").GetString());
                case "anuluj": return Budowa.Anuluj(g, a.GetProperty("arg").GetInt32());
                case "zajecie":
                    Osadnik o = Osada.Osadnicy(g).First(x => x.Imie == a.GetProperty("imie").GetString());
                    return Osada.UstawZajecie(g, o, a.GetProperty("arg").GetString());
                case "auto_projekt":
                    if (string.IsNullOrEmpty(Badania.Stan(g).Aktualny))
                    {
                        foreach (string pid in Badania.Dostepne(g))
                        {
                            return Badania.WybierzProjekt(g, pid);
                        }
                    }
                    return "";
            }
            throw new InvalidOperationException("nieznana akcja");
        }

        private static double Flaga(Gracz g, string k)
        {
            return g.Flagi.TryGetValue(k, out object v) && v != null ? Convert.ToDouble(v) : 0.0;
        }

        private static string Skleja<T>(IEnumerable<T> l)
        {
            return string.Join("|", l);
        }

        /// <summary>Porównuje stan po jednym dniu z migawką Pythona; zwraca opis pierwszej różnicy albo null.</summary>
        private static string Roznica(Gracz g, JsonElement m, List<string> kronika, List<string> pilne, List<string> wyniki)
        {
            if (g.Czas != m.GetProperty("czas").GetInt32()) return "czas";
            if (g.Zloto != m.GetProperty("zloto").GetInt32())
                return $"złoto ({g.Zloto} zamiast {m.GetProperty("zloto").GetInt32()})";
            foreach (JsonElement s in m.GetProperty("surowce").EnumerateArray())
            {
                if (g.Surowce.Wez(s[0].GetString()) != s[1].GetInt32()) return $"surowiec {s[0].GetString()}";
            }
            if (g.Narzedzia != m.GetProperty("narzedzia").GetInt32()) return "narzędzia";
            if (g.Chaty != m.GetProperty("chaty").GetInt32()) return "chaty";
            string poz = Skleja(g.PoziomyBudynkow.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
            if (poz != Skleja(m.GetProperty("poziomy").EnumerateArray().Select(x => $"{x[0].GetString()}={x[1].GetInt32()}")))
                return "poziomy budynków";
            JsonElement[] os = m.GetProperty("osadnicy").EnumerateArray().ToArray();
            if (os.Length != g.Osadnicy.Count) return $"liczba osadników ({g.Osadnicy.Count} zamiast {os.Length})";
            for (int i = 0; i < os.Length; i++)
            {
                Osadnik o = g.Osadnicy[i];
                if (o.Imie != os[i][0].GetString() || o.Zajecie != os[i][1].GetString()) return $"osadnik #{i} (imię/zajęcie)";
                if (Math.Abs(o.Morale - os[i][2].GetDouble()) > Tol)
                    return $"morale {o.Imie} ({o.Morale:R} zamiast {os[i][2].GetDouble():R})";
                if (o.Chory != os[i][3].GetInt32()) return $"choroba {o.Imie}";
                foreach (JsonElement u in os[i][4].EnumerateArray())
                {
                    Umiejetnosc k = o.Kompetencje[u[0].GetString()];
                    if (k.P != u[1].GetInt32() || Math.Abs(k.Xp - u[2].GetDouble()) > Tol)
                        return $"umiejętność {u[0].GetString()} u {o.Imie}";
                }
            }
            List<PlanBudowy> plany = Budowa.Plany(g);
            JsonElement[] pp = m.GetProperty("plany").EnumerateArray().ToArray();
            if (plany.Count != pp.Length) return "liczba planów budowy";
            for (int i = 0; i < pp.Length; i++)
            {
                if (plany[i].Id != pp[i][0].GetInt32() || plany[i].Klucz != pp[i][1].GetString()
                    || plany[i].Poziom != pp[i][2].GetInt32() || Math.Abs(plany[i].Zrobiona - pp[i][3].GetDouble()) > Tol)
                    return $"plan #{i}";
            }
            JsonElement b = m.GetProperty("badania");
            StanBadan sb = Badania.Stan(g);
            if (Skleja(sb.Zrobione) != Skleja(Napisy(b.GetProperty("zrobione")))) return "poznane badania";
            if (sb.Aktualny != NapisLubNull(b.GetProperty("aktualny"))) return "bieżące badanie";
            if (Math.Abs(sb.Zapas - b.GetProperty("zapas").GetDouble()) > Tol) return "zapas punktów";
            foreach (JsonElement x in b.GetProperty("postep").EnumerateArray())
            {
                if (Math.Abs(sb.Postep.Wez(x[0].GetString()) - x[1].GetDouble()) > Tol) return "postęp badania";
            }
            JsonElement fl = m.GetProperty("flagi");
            foreach (string k in new[] { "dni_glodu_osady", "morale_wydarzenia", "zepsute_razem" })
            {
                if (Math.Abs(Flaga(g, k) - fl.GetProperty(k).GetDouble()) > Tol) return $"flaga {k}";
            }
            string bil = Skleja(Osada.BilansDnia(g).OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
            if (bil != Skleja(m.GetProperty("bilans").EnumerateArray().Select(x => $"{x[0].GetString()}={x[1].GetInt32()}")))
                return "bilans dnia";
            if (Skleja(kronika) != Skleja(Napisy(m.GetProperty("kronika")))) return "kronika";
            if (Skleja(pilne) != Skleja(Napisy(m.GetProperty("pilne")))) return "wieści pilne";
            if (Skleja(wyniki) != Skleja(Napisy(m.GetProperty("wyniki")))) return "wyniki poleceń";
            if (Math.Abs(Badania.PunktyDzienne(g) - m.GetProperty("punkty_dzienne").GetDouble()) > Tol) return "punkty dzienne";
            if (Math.Abs(Budowa.MocBudowy(g) - m.GetProperty("moc_budowy").GetDouble()) > Tol) return "moc budowy";
            return null;
        }

        private static void Scenariusze(JsonElement w)
        {
            foreach (JsonElement s in w.EnumerateArray())
            {
                string nazwa = s.GetProperty("nazwa").GetString();
                Gracz g = ZeScenariusza(s.GetProperty("stan"));
                var rng = new Losowanie(s.GetProperty("seed").GetInt32());
                JsonElement[] akcje = s.GetProperty("akcje").EnumerateArray().ToArray();
                JsonElement[] probki = s.GetProperty("probki").EnumerateArray().ToArray();
                string blad = null;
                for (int dzien = 0; dzien < probki.Length && blad == null; dzien++)
                {
                    var wyniki = new List<string>();
                    foreach (JsonElement a in akcje)
                    {
                        bool codziennie = a.TryGetProperty("codziennie", out JsonElement c) && c.GetBoolean();
                        if (codziennie || a.GetProperty("dzien").GetInt32() == dzien)
                        {
                            wyniki.Add(Wykonaj(g, a));
                        }
                    }
                    g.Czas += 1;
                    (List<string> kronika, List<string> pilne) = Osada.DzienOsady(g, rng);
                    string r = Roznica(g, probki[dzien], kronika, pilne, wyniki);
                    blad = r == null ? null : $"dzień {dzien + 1}: {r}";
                }
                Harness.Sprawdz($"{nazwa}: {probki.Length} dni zgodne z Pythonem"
                                + (blad != null ? "  (" + blad + ")" : ""), blad == null);
            }
        }

        // ------------------------------------------------------------------ //
        //  Własne sprawdzenia (jak tests/test_budowa.py i test_badania.py)    //
        // ------------------------------------------------------------------ //

        private static Gracz Bogaty()
        {
            Gracz g = Pusty(new Losowanie(7));
            g.Zloto = 500;
            foreach (KeyValuePair<string, OpisSurowca> s in Oboz.Surowce)
            {
                g.Surowce[s.Key] = 200;
            }
            return g;
        }

        private static Osadnik Budowniczy(Gracz g, string imie = "Bud", int poziom = 4)
        {
            Osadnik o = Nowy(imie, "budowniczy");
            o.Kompetencje["budowa"].P = poziom;
            o.Kompetencje["budowa"].Xp = 0.0;
            o.Kompetencje["budowa"].Pasja = 0;
            g.Osadnicy.Add(o);
            return o;
        }

        private static void Wlasne()
        {
            Gracz g = Bogaty();
            double zloto = g.Zloto;
            Harness.Sprawdz("bez badań nie zlecisz tartaku",
                            Budowa.Zlec(g, "tartak").Contains("poznaj: Obróbka drewna") && Budowa.Plany(g).Count == 0 && g.Zloto == zloto);
            Harness.Sprawdz("zlecenie pobiera materiały od razu, budynek jeszcze nie stoi",
                            Budowa.Zlec(g, "dom").Contains("Zlecono") && g.PoziomBudynku("dom") == 0 && g.Zloto == 475);
            Harness.Sprawdz("ten sam budynek nie może być w budowie dwa razy", Budowa.Zlec(g, "dom").Contains("rusztowaniach"));
            Harness.Rowne("anulowanie bez prac zwraca 100%", true,
                          Budowa.Anuluj(g, Budowa.Plany(g)[0].Id).Contains("Zwrot 100%") && g.Zloto == 500);

            g = Bogaty();
            Budowniczy(g);
            Budowa.Zlec(g, "palisada");
            Budowa.DzienBudowy(g);
            Harness.Sprawdz("po dniu palisada jeszcze się buduje", g.PoziomBudynku("palisada") == 0);
            (List<string> _, List<string> pilne) = Budowa.DzienBudowy(g);
            Harness.Sprawdz("po dwóch dniach palisada stoi", g.PoziomBudynku("palisada") == 1
                            && pilne.Any(x => x.Contains("UKOŃCZONO")) && Budowa.Plany(g).Count == 0);
            Harness.Sprawdz("budowniczy uczy się budowy", g.Osadnicy[0].Kompetencje["budowa"].Xp > 0);

            g = Bogaty();
            for (int i = 0; i < 4; i++)
            {
                Budowniczy(g, "B" + i);
            }
            Budowa.Zlec(g, "dom");
            Budowa.Zlec(g, "farma");
            Budowa.DzienBudowy(g);
            Harness.Sprawdz("plan przyjmuje dziennie najwyżej limit, reszta idzie do następnego",
                            Math.Abs(Budowa.Plany(g)[0].Zrobiona - Budowa.LimitPlanu) < Tol && Budowa.Plany(g)[1].Zrobiona > 0);

            g = Bogaty();
            g.Chaty = 4;
            Harness.Sprawdz("limit chat blokuje czwartą ponad start", Budowa.Zlec(g, "chata").Contains("limit chat"));
            Badania.Stan(g).Zrobione.Add("osadnictwo");
            g.Chaty = 7;
            Harness.Sprawdz("po badaniu osadnictwa wolno do ośmiu chat",
                            Budowa.Zlec(g, "chata").Contains("Zlecono") && Budowa.Zlec(g, "chata").Contains("limit chat"));

            g = Bogaty();
            Budowa.Zlec(g, "dom");
            g.WObozie = false;
            Harness.Sprawdz("poza obozem bohater nie pomaga przy budowie",
                            Budowa.Pomoz(new KonsolaTestowa(new string[0]), g, new Losowanie(1), Budowa.Plany(g)[0].Id).Contains("tylko w obozie"));
            g.WObozie = true;
            int czas = g.Czas;
            string tekst = Budowa.Pomoz(new KonsolaTestowa(new string[0]), g, new Losowanie(1), Budowa.Plany(g)[0].Id);
            Harness.Sprawdz("dzień bohatera przy budowie dopisuje pracę i mija dzień",
                            tekst.Contains("Cały dzień przy budowie") && g.Czas == czas + 1 && Budowa.Plany(g)[0].Zrobiona >= Budowa.PracaBohatera);

            g = Pusty(new Losowanie(3));
            Oboz.UstawPoziom(g, "skryptorium", 1);
            g.Osadnicy.Add(Nowy("Nessa", "badacz"));
            Badania.WybierzProjekt(g, "obrobka_drewna");
            var rng = new Losowanie(3);
            for (int d = 0; d < 12; d++)
            {
                g.Czas++;
                Osada.DzienOsady(g, rng);
            }
            Harness.Sprawdz("badacz kończy projekt i odblokowuje tartak",
                            Badania.Zrobione(g).Contains("obrobka_drewna") && Badania.MaksPoziom(g, "tartak") == 1);
            Harness.Sprawdz("badacz wymaga skryptorium",
                            Osada.UstawZajecie(Pusty(), Nowy("X", "drwal"), "badacz").Contains("Potrzebny budynek"));
            g = Pusty();
            Badania.MigrujStaryZapis(g);
            Harness.Sprawdz("stary zapis ma wszystko poznane", Budowa.PowodBlokady(g, "kuznia") == null);
            Harness.Sprawdz("wszystkie badania: epoka III i 12 chat", Badania.Era(g) == 3 && Badania.LimitChat(g) == 12);
        }
    }
}
