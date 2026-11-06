using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GraSimply.Logika.Wolny
{
    /// <summary>
    /// Plik zapisu trybu przetrwania. Zapis idzie przez plik tymczasowy, a poprzednia wersja zostaje jako kopia,
    /// więc przerwane zapisywanie albo uszkodzony plik nie zabierają całej osady.
    /// </summary>
    public static class ZapisWolny
    {
        /// <summary>Ścieżkę podaje silnik gry; null = zapis wyłączony (testy i gra bez okna).</summary>
        public static string Sciezka;

        public static bool Istnieje => Sciezka != null && (File.Exists(Sciezka) || File.Exists(Sciezka + ".kopia"));

        /// <summary>Zapisuje świat. False, gdy się nie udało: gra toczy się dalej, a stary zapis zostaje.</summary>
        public static bool Zapisz(SwiatWolny s)
        {
            if (Sciezka == null)
            {
                return false;
            }
            try
            {
                string tymczasowy = Sciezka + ".tmp";
                File.WriteAllText(tymczasowy, s.DoZapisu(), new UTF8Encoding(false));
                if (File.Exists(Sciezka))
                {
                    File.Replace(tymczasowy, Sciezka, Sciezka + ".kopia");
                }
                else
                {
                    File.Move(tymczasowy, Sciezka);
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Wczytuje zapis, a przy uszkodzeniu jego kopię. Null, gdy nie ma czego wczytać.</summary>
        public static SwiatWolny Wczytaj()
        {
            if (Sciezka == null)
            {
                return null;
            }
            foreach (string plik in new[] { Sciezka, Sciezka + ".kopia" })
            {
                try
                {
                    if (File.Exists(plik))
                    {
                        return SwiatWolny.ZZapisu(File.ReadAllText(plik, Encoding.UTF8));
                    }
                }
                catch (Exception e) when (e is FormatException || e is IOException || e is UnauthorizedAccessException
                                          || e is IndexOutOfRangeException || e is ArgumentException || e is OverflowException)
                {
                    // Spróbujmy kopii.
                }
            }
            return null;
        }
    }

    public sealed partial class SwiatWolny
    {
        private const string Naglowek = "SimplyPrzetrwanie";
        private const int WersjaZapisu = 2;

        /// <summary>Ustawiane o świcie: silnik gry zapisuje wtedy świat na dysk.</summary>
        public bool PotrzebnyZapis;

        private static string L(double v)
        {
            return v.ToString("R", CultureInfo.InvariantCulture);
        }

        private static double D(string s)
        {
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        private static int I(string s)
        {
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        private static string Bez(string s)
        {
            return (s ?? "").Replace('\t', ' ').Replace('\n', ' ');
        }

        /// <summary>Stan świata jako tekst: jedna rzecz w wierszu, pola po tabulatorze.</summary>
        public string DoZapisu()
        {
            var t = new StringBuilder();
            void W(params object[] pola)
            {
                t.Append(string.Join("\t", pola.Select(p => p is double d ? L(d) : Convert.ToString(p, CultureInfo.InvariantCulture)))).Append('\n');
            }

            W(Naglowek, WersjaZapisu, Seed);
            W("stan", X, Y, Kierunek.X, Kierunek.Y, Godzina, Dzien, Glod, Energia, Hp, Wytrzymalosc, ObozZalozony ? 1 : 0,
              _czasGry, _bylPrzybysz ? 1 : 0, Wybrany, _nastepnaFala, _nastepnyWloczega, _bylaNoc ? 1 : 0, Zabici, _nocPoprzednio ? 1 : 0);
            foreach (KeyValuePair<string, int> p in Plecak)
            {
                W("plecak", p.Key, p.Value);
            }
            foreach (KeyValuePair<(int X, int Y), string> b in Budowle)
            {
                W("budowla", b.Key.X, b.Key.Y, b.Value, ZycieBudowli.TryGetValue(b.Key, out double z) ? z : -1.0);
            }
            foreach (KeyValuePair<(int X, int Y), Zuzycie> z in _zuzyte)
            {
                W("zuzyte", z.Key.X, z.Key.Y, z.Value.Uzyte, z.Value.OdrostGodzina);
            }
            W("oboz", Obozowisko.Czas);
            foreach (KeyValuePair<string, int> r in Obozowisko.Surowce)
            {
                W("surowiec", r.Key, r.Value);
            }
            foreach (Osadnik o in Obozowisko.Osadnicy)
            {
                PostacOsadnika p = Postacie.FirstOrDefault(q => q.O == o);
                W("osadnik", Bez(o.Imie), o.Zajecie, o.Morale, Bez(o.Cecha), o.Chory, p?.X ?? X, p?.Y ?? Y, p?.Zycie ?? ZycieOsadnika, p != null && p.Uzbrojony ? 1 : 0,
                  p == null ? "" : string.Join(",", p.Plan.Select(b => (int)b)), p?.Zmeczenie ?? 0.0, p?.Sekret ?? "", p?.DniWOsadzie ?? 0, p != null && p.Podejrzany ? 1 : 0);
                if (o.Kompetencje != null)
                {
                    foreach (KeyValuePair<string, Umiejetnosc> u in o.Kompetencje)
                    {
                        W("umiejetnosc", u.Key, u.Value.P, u.Value.Xp, u.Value.Pasja);
                    }
                }
            }
            W("decyzje", _dobreDecyzje);
            if (Gosc != null)
            {
                W("gosc", Bez(Gosc.O.Imie), Gosc.O.Morale, Bez(Gosc.O.Cecha), Gosc.X, Gosc.Y, Gosc.Surowiec, Gosc.Ile, Bez(Gosc.Wskazowka), Gosc.Sekret ?? "");
                foreach (KeyValuePair<string, Umiejetnosc> u in Gosc.O.Kompetencje ?? new Dictionary<string, Umiejetnosc>())
                {
                    W("umiejetnosc", u.Key, u.Value.P, u.Value.Xp, u.Value.Pasja);
                }
            }
            foreach (Wrog g in Wrogowie.Where(g => g.Stan != StanWroga.Martwy))
            {
                W("wrog", (int)g.Typ, g.X, g.Y, g.Hp, g.Nocny ? 1 : 0, g.Stan == StanWroga.Stoi ? 1 : 0);
            }
            foreach (PorzuconyPlecak p in Porzucone)
            {
                W("porzucony", p.X, p.Y);
                foreach (KeyValuePair<string, int> r in p.Zawartosc)
                {
                    W("rzecz", r.Key, r.Value);
                }
            }
            W("koniec");
            return t.ToString();
        }

        /// <summary>Świat z tekstu zapisu. Rzuca wyjątkiem, gdy plik jest uszkodzony albo niepełny.</summary>
        public static SwiatWolny ZZapisu(string tekst)
        {
            string[] wiersze = tekst.Split('\n');
            string[] n = wiersze[0].Split('\t');
            if (n[0].Trim() != Naglowek || I(n[1]) > WersjaZapisu)
            {
                throw new FormatException("To nie jest zapis trybu przetrwania.");
            }
            if (!wiersze.Any(w => w.Trim() == "koniec"))
            {
                throw new FormatException("Zapis jest niepełny.");
            }
            var s = new SwiatWolny(long.Parse(n[2], CultureInfo.InvariantCulture)) { WalkaWlaczona = true };
            s.Plecak.Clear();
            s.Dziennik.Clear();
            s.Obozowisko.Surowce.Clear();
            Osadnik osadnik = null;
            PorzuconyPlecak porzucony = null;
            foreach (string wiersz in wiersze.Skip(1))
            {
                string[] c = wiersz.TrimEnd('\r').Split('\t');
                switch (c[0])
                {
                    case "stan":
                        s.X = D(c[1]);
                        s.Y = D(c[2]);
                        s.Kierunek = (D(c[3]), D(c[4]));
                        s.Godzina = D(c[5]);
                        s.Dzien = I(c[6]);
                        s.Glod = D(c[7]);
                        s.Energia = D(c[8]);
                        s.Hp = D(c[9]);
                        s.Wytrzymalosc = D(c[10]);
                        s.ObozZalozony = c[11] == "1";
                        s._czasGry = D(c[12]);
                        s._bylPrzybysz = c[13] == "1";
                        s.Wybrany = I(c[14]);
                        s._nastepnaFala = D(c[15]);
                        s._nastepnyWloczega = D(c[16]);
                        s._bylaNoc = c[17] == "1";
                        s.Zabici = I(c[18]);
                        s._nocPoprzednio = c[19] == "1";
                        break;
                    case "plecak":
                        s.Plecak[c[1]] = I(c[2]);
                        break;
                    case "budowla":
                        s.Budowle[(I(c[1]), I(c[2]))] = c[3];
                        if (D(c[4]) >= 0)
                        {
                            s.ZycieBudowli[(I(c[1]), I(c[2]))] = D(c[4]);
                        }
                        break;
                    case "zuzyte":
                        s._zuzyte[(I(c[1]), I(c[2]))] = new Zuzycie { Uzyte = I(c[3]), OdrostGodzina = D(c[4]) };
                        break;
                    case "oboz":
                        s.Obozowisko.Czas = I(c[1]);
                        break;
                    case "surowiec":
                        s.Obozowisko.Surowce[c[1]] = I(c[2]);
                        break;
                    case "osadnik":
                        osadnik = new Osadnik { Imie = c[1], Zajecie = c[2], Morale = D(c[3]), Cecha = c[4], Chory = I(c[5]) };
                        s.Obozowisko.Osadnicy.Add(osadnik);
                        var postac = new PostacOsadnika
                        {
                            O = osadnik, X = D(c[6]), Y = D(c[7]), CelX = D(c[6]), CelY = D(c[7]), Zycie = D(c[8]), Uzbrojony = c[9] == "1",
                        };
                        // Zapis ze starszej wersji nie ma planu ani sekretów: zostają wartości domyślne.
                        if (c.Length > 14)
                        {
                            postac.Plan = c[10].Split(',').Select(b => (BlokPlanu)I(b)).ToArray();
                            postac.Zmeczenie = D(c[11]);
                            postac.Sekret = c[12].Length == 0 ? null : c[12];
                            postac.DniWOsadzie = I(c[13]);
                            postac.Podejrzany = c[14] == "1";
                        }
                        s.Postacie.Add(postac);
                        break;
                    case "decyzje":
                        s._dobreDecyzje = I(c[1]);
                        break;
                    case "gosc":
                        osadnik = new Osadnik { Imie = c[1], Zajecie = "bezczynny", Morale = D(c[2]), Cecha = c[3] };
                        s.Gosc = new Przybysz
                        {
                            O = osadnik, X = D(c[4]), Y = D(c[5]), Surowiec = c[6], Ile = I(c[7]), Wskazowka = c[8],
                            Sekret = c[9].Length == 0 ? null : c[9],
                        };
                        break;
                    case "umiejetnosc":
                        osadnik.Kompetencje = osadnik.Kompetencje ?? new Dictionary<string, Umiejetnosc>();
                        osadnik.Kompetencje[c[1]] = new Umiejetnosc { P = I(c[2]), Xp = D(c[3]), Pasja = I(c[4]) };
                        break;
                    case "wrog":
                        var typ = (TypWroga)I(c[1]);
                        s.Wrogowie.Add(new Wrog
                        {
                            Typ = typ, X = D(c[2]), Y = D(c[3]), Hp = D(c[4]), MaxHp = Cechy(typ).Hp, Nocny = c[5] == "1",
                            Stan = c[6] == "1" ? StanWroga.Stoi : StanWroga.Idzie,
                        });
                        break;
                    case "porzucony":
                        porzucony = new PorzuconyPlecak { X = D(c[1]), Y = D(c[2]), Zawartosc = new Dictionary<string, int>() };
                        s.Porzucone.Add(porzucony);
                        break;
                    case "rzecz":
                        porzucony.Zawartosc[c[1]] = I(c[2]);
                        break;
                }
            }
            s.PrzeliczMiejsca();
            s.Wybrany = Math.Max(0, Math.Min(s.Wybrany, s.Obozowisko.Osadnicy.Count - 1));
            s.Pisz($"Wracasz do swojego świata. Dzień {s.Dzien}.");
            return s;
        }
    }
}
