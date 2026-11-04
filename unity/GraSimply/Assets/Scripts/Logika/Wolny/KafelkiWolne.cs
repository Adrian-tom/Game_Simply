using System.Collections.Generic;

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

        public bool Dostepny = true;
        public bool Wybrany;
    }

    /// <summary>Wszystkie kafelki na jedną klatkę. Rząd budowli i zajęcia są puste, gdy nie ma ich gdzie pokazać.</summary>
    public sealed class KafelkiWolne
    {
        public List<Kafelek> Akcje = new List<Kafelek>();
        public List<Kafelek> Budowle = new List<Kafelek>();
        public List<Kafelek> Zajecia = new List<Kafelek>();

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
            if (s.TrybBudowy)
            {
                foreach (string klucz in SwiatWolny.Budowlane)
                {
                    k.Budowle.Add(Budowla(s, klucz));
                }
            }
            if (s.PanelOtwarty && s.Obozowisko.Osadnicy.Count > 0)
            {
                k.Osadnikow = s.Obozowisko.Osadnicy.Count;
                string obecne = s.Obozowisko.Osadnicy[s.Wybrany].Zajecie;
                foreach (string z in SwiatWolny.ZajeciaPanelu)
                {
                    k.Zajecia.Add(new Kafelek { Id = "z:" + z, Nazwa = Osada.Zajecie(z).Nazwa, Ikona = z, Wybrany = z == obecne });
                }
            }
            return k;
        }

        private static Kafelek Akcja(string id, string nazwa, string klawisz, string ikona, bool dostepny, bool wybrany)
        {
            var k = new Kafelek { Id = id, Nazwa = nazwa, Ikona = ikona, Dostepny = dostepny, Wybrany = wybrany };
            k.Opis.Add(("[" + klawisz + "]", true));
            return k;
        }

        private static Kafelek Budowla(SwiatWolny s, string klucz)
        {
            string nazwa = klucz == "spiwor" ? "Śpiwór" : klucz == "ognisko" ? "Ognisko" : klucz == "namiot" ? "Namiot" : "Chata";
            var k = new Kafelek
            {
                Id = "b:" + klucz, Nazwa = nazwa, Ikona = klucz, Dostepny = s.Stac(klucz), Wybrany = s.WybranaBudowla == klucz,
            };
            foreach (KeyValuePair<string, int> p in SwiatWolny.Koszty[klucz])
            {
                string linia = p.Key == "spiwor" ? "z plecaka" : $"{p.Value} {SwiatWolny.Nazwa(p.Key)}";
                k.Opis.Add((linia, s.Razem(p.Key) >= p.Value));
            }
            return k;
        }
    }
}
