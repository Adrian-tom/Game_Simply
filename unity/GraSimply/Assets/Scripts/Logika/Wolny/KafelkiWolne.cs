using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Jeden kafelek interfejsu: co pokazać i jaki identyfikator trafia do <see cref="SwiatWolny.Kliknij"/>.</summary>
    public sealed class Kafelek
    {
        public string Id;
        public string Nazwa;
        public string Ikona;

        /// <summary>Linie pod nazwą; Ok = false rysuje się na czerwono (np. brakujący składnik).</summary>
        public List<(string Tekst, bool Ok)> Opis = new List<(string Tekst, bool Ok)>();

        /// <summary>Krótki stan na prawo od nazwy (np. „kolejka 2”); Ok = false rysuje się na czerwono.</summary>
        public (string Tekst, bool Ok)? Stan;

        public bool Dostepny = true;
        public bool Wybrany;
    }

    /// <summary>Wszystkie kafelki na jedną klatkę. Rząd budowli i zajęcia są puste, gdy nie ma ich gdzie pokazać.</summary>
    public sealed class KafelkiWolne
    {
        public List<Kafelek> Akcje = new List<Kafelek>();
        public List<Kafelek> Budowle = new List<Kafelek>();
        public List<Kafelek> Zajecia = new List<Kafelek>();

        /// <summary>Ciosy specjalne: tylko nocą i gdy nic innego nie zajmuje miejsca nad paskiem.</summary>
        public List<Kafelek> Specjalne = new List<Kafelek>();

        /// <summary>Plan doby wybranego osadnika i decyzje o nim.</summary>
        public List<Kafelek> Plan = new List<Kafelek>();

        /// <summary>Zakładki trybu budowania.</summary>
        public List<Kafelek> Kategorie = new List<Kafelek>();
        public List<Kafelek> Decyzje = new List<Kafelek>();

        /// <summary>Widoczni osadnicy jako wiersze panelu: kto, co robi i jak się czuje.</summary>
        public List<Kafelek> Osadnicy = new List<Kafelek>();

        /// <summary>Numer osadnika w pierwszym wierszu panelu: długa lista przewija się z wyborem.</summary>
        public int PierwszyOsadnik;

        /// <summary>Tyle pierwszych wierszy panelu (po nagłówku) to klikalni osadnicy.</summary>
        public int Osadnikow;

        public static KafelkiWolne Z(SwiatWolny s)
        {
            var k = new KafelkiWolne();
            k.Akcje.Add(Akcja("a:buduj", "Buduj", "B", "buduj", true, s.TrybBudowy));
            k.Akcje.Add(Akcja("a:magazyn", "Magazyn", "T", "magazyn", s.PrzyObozie(), false));
            k.Akcje.Add(Akcja("a:osada", "Osada", "Tab", "osada", true, s.PanelOtwarty));
            k.Akcje.Add(Akcja("a:jedz", "Jedz", "F", "jedz", s.Razem("jagody") > 0, false));
            k.Akcje.Add(Akcja("a:spij", "Śpij", "Z", "spij", s.MiejsceSnu() != null, s.Spi));
            if (s.Gosc != null)
            {
                k.Akcje.Add(Akcja("g:otworz", "Przybysz", "G", "osada", true, s.GoscOtwarty));
            }
            if (s.GoscOtwarty && s.Gosc != null)
            {
                bool stac = s.Razem(s.Gosc.Surowiec) >= s.Gosc.Ile && Osada.WolneChaty(s.Obozowisko) > 0;
                var tak = new Kafelek { Id = "g:tak", Nazwa = "Przyjmij", Ikona = "osada", Dostepny = stac };
                tak.Opis.Add(($"{s.Gosc.Ile} {SwiatWolny.Nazwa(s.Gosc.Surowiec)}", s.Razem(s.Gosc.Surowiec) >= s.Gosc.Ile));
                var nie = new Kafelek { Id = "g:nie", Nazwa = "Odpraw", Ikona = "szarza" };
                nie.Opis.Add(("bez kosztu", true));
                k.Decyzje.Add(tak);
                k.Decyzje.Add(nie);
                return k;
            }
            if (s.WalkaWlaczona && s.CzyNoc && !s.TrybBudowy && !s.PanelOtwarty)
            {
                foreach ((string id, string nazwa, string klawisz, double _) in SwiatWolny.Specjalne)
                {
                    double zostalo = s.Odnowienie.TryGetValue(id, out double o) ? o : 0;
                    var c = new Kafelek { Id = "s:" + id, Nazwa = nazwa, Ikona = id, Dostepny = zostalo <= 0 };
                    c.Opis.Add((zostalo > 0 ? $"{System.Math.Ceiling(zostalo):0} s" : "[" + klawisz + "]", zostalo <= 0));
                    k.Specjalne.Add(c);
                }
            }
            if (s.TrybBudowy)
            {
                foreach ((string id, string nazwa, string[] budowle) in SwiatWolny.Kategorie)
                {
                    k.Kategorie.Add(new Kafelek { Id = "k:" + id, Nazwa = nazwa, Ikona = id == "rzemioslo" ? "magazyn" : id == "obrona" ? "palisada" : id == "zaklady" ? "kuznia" : "namiot", Wybrany = s.Kategoria == id });
                    if (s.Kategoria != id)
                    {
                        continue;
                    }
                    foreach (string klucz in budowle)
                    {
                        k.Budowle.Add(Budowla(s, klucz));
                    }
                    if (id == "rzemioslo")
                    {
                        foreach (Przepis p in SwiatWolny.Przepisy)
                        {
                            k.Budowle.Add(KafelekPrzepisu(s, p));
                        }
                    }
                }
            }
            if (s.PanelOtwarty && s.Obozowisko.Osadnicy.Count > 0)
            {
                k.PierwszyOsadnik = WidokWolny.PierwszyWidoczny(s);
                k.Osadnikow = System.Math.Min(WidokWolny.WierszyOsadnikow, s.Obozowisko.Osadnicy.Count - k.PierwszyOsadnik);
                for (int i = 0; i < k.Osadnikow; i++)
                {
                    k.Osadnicy.Add(WierszOsadnika(s, k.PierwszyOsadnik + i));
                }
                if (s.WalkaWlaczona)
                {
                    Osadnik wybrany = s.Obozowisko.Osadnicy[s.Wybrany];
                    PostacOsadnika p = s.Postacie.Find(q => q.O == wybrany);
                    for (int i = 0; p != null && i < 4; i++)
                    {
                        var b = new Kafelek { Id = "h:" + i, Nazwa = SwiatWolny.NazwyBlokow[i], Ikona = IkonaBloku(p.Plan[i]) };
                        b.Opis.Add((NazwaBloku(p.Plan[i]), true));
                        k.Plan.Add(b);
                    }
                    k.Decyzje.Add(new Kafelek { Id = "h:wszyscy", Nazwa = "Plan dla wszystkich", Ikona = "osada" });
                    k.Decyzje.Add(new Kafelek { Id = "x:wygnaj", Nazwa = "Wypędź", Ikona = "szarza" });
                }
                string obecne = s.Obozowisko.Osadnicy[s.Wybrany].Zajecie;
                foreach (string z in SwiatWolny.ZajeciaPanelu)
                {
                    k.Zajecia.Add(new Kafelek { Id = "z:" + z, Nazwa = Osada.Zajecie(z).Nazwa, Ikona = z, Wybrany = z == obecne });
                }
            }
            return k;
        }

        private static Kafelek WierszOsadnika(SwiatWolny s, int nr)
        {
            Osadnik o = s.Obozowisko.Osadnicy[nr];
            string u = Kompetencje.ZUmiejetnosciZajecia(o.Zajecie);
            string poziom = u == null ? "" : $" {Kompetencje.Poziom(o, u)}{new string('*', Kompetencje.Pasja(o, u))}";
            PostacOsadnika p = s.WalkaWlaczona ? s.Postacie.Find(q => q.O == o) : null;
            string stan = $"morale {Liczby.Format(o.Morale, 0)}" + (p == null ? "" : $", zmęczenie {Liczby.Format(p.Zmeczenie, 0)}{(p.Podejrzany ? ", ?" : "")}")
                        + (o.Chory > 0 ? ", chory" : "");
            var k = new Kafelek { Id = "o:" + nr, Nazwa = $"{o.Imie}: {Osada.Zajecie(o.Zajecie).Nazwa}{poziom}", Ikona = o.Zajecie, Wybrany = nr == s.Wybrany };
            k.Opis.Add((stan, o.Chory <= 0));
            return k;
        }

        public static string NazwaBloku(BlokPlanu b)
        {
            return b == BlokPlanu.Praca ? "praca" : b == BlokPlanu.Odpoczynek ? "sen" : b == BlokPlanu.Wolne ? "wolne" : "straż";
        }

        private static string IkonaBloku(BlokPlanu b)
        {
            return b == BlokPlanu.Praca ? "buduj" : b == BlokPlanu.Odpoczynek ? "spij" : b == BlokPlanu.Wolne ? "jedz" : "straznik";
        }

        private static Kafelek Akcja(string id, string nazwa, string klawisz, string ikona, bool dostepny, bool wybrany)
        {
            var k = new Kafelek { Id = id, Nazwa = nazwa, Ikona = ikona, Dostepny = dostepny, Wybrany = wybrany };
            k.Opis.Add(("[" + klawisz + "]", true));
            return k;
        }

        private static Kafelek Budowla(SwiatWolny s, string klucz)
        {
            string nazwa = SwiatWolny.NazwaBudowli(klucz);
            nazwa = char.ToUpperInvariant(nazwa[0]) + nazwa.Substring(1);
            var k = new Kafelek
            {
                Id = "b:" + klucz, Nazwa = nazwa, Ikona = klucz, Dostepny = true, Wybrany = s.WybranaBudowla == klucz,
            };
            KeyValuePair<string, int>[] koszt = s.BudowaZEtapami ? SwiatWolny.KosztCalkowity(klucz) : SwiatWolny.Koszty[klucz];
            k.Dostepny = koszt.All(p => s.Razem(p.Key) >= p.Value);
            foreach (KeyValuePair<string, int> p in koszt)
            {
                k.Opis.Add((p.Key == "spiwor" ? "z plecaka" : $"{p.Value} {SwiatWolny.Nazwa(p.Key)}", s.Razem(p.Key) >= p.Value));
            }
            return k;
        }

        private static Kafelek KafelekPrzepisu(SwiatWolny s, Przepis p)
        {
            bool stoi = s.StoiStacja(p.Stacja);
            bool ma = p.Narzedzie && s.Narzedzia.Contains(p.Id);
            var k = new Kafelek { Id = "r:" + p.Id, Nazwa = p.Nazwa, Ikona = p.Stacja, Dostepny = stoi && !ma };
            foreach (KeyValuePair<string, int> m in p.Wejscie)
            {
                k.Opis.Add(($"{m.Value} {SwiatWolny.Nazwa(m.Key)}", s.Razem(m.Key) >= m.Value));
            }
            int kolejka = s.WKolejce(p.Id);
            k.Stan = (ma ? "masz" : !stoi ? "brak: " + SwiatWolny.NazwaBudowli(p.Stacja) : kolejka > 0 ? $"kolejka {kolejka}" : "kliknij", stoi || ma);
            return k;
        }
    }
}
