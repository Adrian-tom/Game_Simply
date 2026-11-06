using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Jeden etap budowy: co zużywa i ile pracy (sekund jednej osoby) wymaga.</summary>
    public sealed class EtapBudowy
    {
        public string Nazwa;
        public KeyValuePair<string, int>[] Materialy;
        public double Praca;
    }

    /// <summary>Budowa w toku na kaflu. Materiały etapu pobiera się na jego początku.</summary>
    public sealed class PlacBudowy
    {
        public string Klucz;
        public int X, Y, Etap;
        public double Praca;
        public bool Zaplacony;
    }

    /// <summary>Przepis warsztatu: co zużywa, co daje i ile pracy zajmuje.</summary>
    public sealed class Przepis
    {
        public string Id, Nazwa, Stacja;
        public KeyValuePair<string, int>[] Wejscie;
        public int Ilosc;
        public double Praca;

        /// <summary>Narzędzie albo broń: robi się raz, a działa przez całą grę.</summary>
        public bool Narzedzie;
    }

    public sealed class Zlecenie
    {
        public Przepis Co;
        public double Praca;
        public bool Zaplacone;
    }

    /// <summary>
    /// Budowa etapami i warsztaty ze zleceniami. Porządne budowle mają realne koszty i trwają: każdy etap ma
    /// własne materiały i pracę, a przy budowie pracuje bohater (trzymając E) i osadnicy z zawodu budowniczy.
    /// Więcej rąk to szybciej, ale każda kolejna osoba daje mniej, bo przeszkadzają sobie nawzajem. Materiały
    /// przetworzone (belki, deski, lina) i narzędzia powstają w zakładach, według kolejki zleceń.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const double ZasiegPracy = 2.2;
        public const double UdzialKolejnego = 0.75;
        public const string PrzedrostekPlacu = "plac:";

        private static KeyValuePair<string, int>[] M(params object[] p)
        {
            var w = new KeyValuePair<string, int>[p.Length / 2];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = new KeyValuePair<string, int>((string)p[2 * i], (int)p[2 * i + 1]);
            }
            return w;
        }

        private static EtapBudowy E(string nazwa, double praca, params object[] m)
        {
            return new EtapBudowy { Nazwa = nazwa, Praca = praca, Materialy = M(m) };
        }

        /// <summary>Śpiwór i ognisko stawia się od razu; tu jest wszystko, co wymaga budowy.</summary>
        public static readonly Dictionary<string, EtapBudowy[]> Etapy = new Dictionary<string, EtapBudowy[]>
        {
            { "namiot", new[] { E("Szkielet", 30, "drewno", 30), E("Płótno", 20, "wlokno", 20) } },
            { "chata", new[] { E("Fundament", 50, "kamien", 40), E("Ściany", 120, "belka", 25), E("Dach", 70, "deska", 30) } },
            { "palisada", new[] { E("Pale", 6, "drewno", 8) } },
            { "brama", new[] { E("Skrzydła", 15, "drewno", 25) } },
            { "wieza", new[] { E("Podstawa", 50, "kamien", 40), E("Konstrukcja", 60, "belka", 15), E("Platforma", 30, "deska", 15, "lina", 2) } },
            { "warsztat", new[] { E("Fundament", 25, "kamien", 15), E("Konstrukcja", 50, "drewno", 40), E("Stoły", 15, "drewno", 15) } },
            { "tartak", new[] { E("Fundament", 30, "kamien", 20), E("Konstrukcja", 60, "drewno", 60), E("Piła", 20, "drewno", 20, "kamien", 5) } },
            { "kuznia", new[] { E("Palenisko", 60, "kamien", 50), E("Ściany", 60, "belka", 12), E("Komin", 30, "kamien", 20) } },
        };

        public static readonly Przepis[] Przepisy =
        {
            new Przepis { Id = "deska", Nazwa = "Deski", Stacja = "tartak", Wejscie = M("drewno", 3), Ilosc = 2, Praca = 6 },
            new Przepis { Id = "belka", Nazwa = "Belka", Stacja = "tartak", Wejscie = M("drewno", 4), Ilosc = 1, Praca = 8 },
            new Przepis { Id = "lina", Nazwa = "Lina", Stacja = "warsztat", Wejscie = M("wlokno", 3), Ilosc = 1, Praca = 5 },
            new Przepis { Id = "mlotek", Nazwa = "Młotek", Stacja = "warsztat", Wejscie = M("belka", 1, "lina", 1), Ilosc = 1, Praca = 10, Narzedzie = true },
            new Przepis { Id = "siekiera", Nazwa = "Siekiera", Stacja = "warsztat", Wejscie = M("belka", 1, "kamien", 5), Ilosc = 1, Praca = 12, Narzedzie = true },
            new Przepis { Id = "kilof", Nazwa = "Kilof", Stacja = "warsztat", Wejscie = M("belka", 1, "kamien", 5), Ilosc = 1, Praca = 12, Narzedzie = true },
            new Przepis { Id = "miecz", Nazwa = "Miecz", Stacja = "kuznia", Wejscie = M("ruda", 4, "belka", 1), Ilosc = 1, Praca = 18, Narzedzie = true },
            new Przepis { Id = "zbroja", Nazwa = "Zbroja", Stacja = "kuznia", Wejscie = M("ruda", 8, "lina", 2), Ilosc = 1, Praca = 30, Narzedzie = true },
        };

        public static readonly string[] Stacje = { "warsztat", "tartak", "kuznia" };

        /// <summary>Zakładki trybu budowania; „rzemioslo” pokazuje przepisy zamiast budowli.</summary>
        public static readonly (string Id, string Nazwa, string[] Budowle)[] Kategorie =
        {
            ("oboz", "Obóz", new[] { "spiwor", "ognisko", "namiot", "chata" }),
            ("obrona", "Obrona", new[] { "palisada", "brama", "wieza" }),
            ("zaklady", "Zakłady", Stacje),
            ("rzemioslo", "Rzemiosło", new string[0]),
        };

        /// <summary>Włączone w grze: budowa etapami i warsztaty. Bez tego budowle stają od razu, jak dawniej.</summary>
        public bool BudowaZEtapami;

        public string Kategoria = "oboz";

        public readonly Dictionary<(int X, int Y), PlacBudowy> Place = new Dictionary<(int X, int Y), PlacBudowy>();
        public readonly Dictionary<string, int> Wyroby = new Dictionary<string, int>();
        public readonly HashSet<string> Narzedzia = new HashSet<string>();
        public readonly Dictionary<string, List<Zlecenie>> Zlecenia = new Dictionary<string, List<Zlecenie>>();

        /// <summary>Czego brakuje na placu lub w zakładzie, przy którym stoi bohater; null = można pracować.</summary>
        public string Brak { get; private set; }

        /// <summary>Praca bohatera w tej klatce: tile dla paska nad budową albo zakładem.</summary>
        public (int X, int Y, double Postep)? PostepPracy { get; private set; }

        public bool BohaterPracuje => PostepPracy.HasValue;

        public static KeyValuePair<string, int>[] KosztCalkowity(string klucz)
        {
            if (!Etapy.TryGetValue(klucz, out EtapBudowy[] e))
            {
                return Koszty[klucz];
            }
            return e.SelectMany(x => x.Materialy).GroupBy(m => m.Key)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Sum(m => m.Value))).ToArray();
        }

        public bool WymagaBudowy(string klucz)
        {
            return BudowaZEtapami && Etapy.ContainsKey(klucz);
        }

        public bool StoiStacja(string stacja)
        {
            return Budowle.ContainsValue(stacja);
        }

        /// <summary>Wspólna robota: każda kolejna osoba dodaje mniej, a umiejętność i narzędzie przyspieszają.</summary>
        public static double SilaZespolu(IEnumerable<double> sprawnosci)
        {
            double sila = 0.0, udzial = 1.0;
            foreach (double s in sprawnosci.OrderByDescending(x => x))
            {
                sila += s * udzial;
                udzial *= UdzialKolejnego;
            }
            return sila;
        }

        /// <summary>Budowniczy idzie na najbliższy plac, a rzemieślnik do zakładu ze zleceniem.</summary>
        private bool MiejscePracy(PostacOsadnika p, out (int X, int Y) gdzie)
        {
            gdzie = default;
            IEnumerable<(int X, int Y)> kandydaci;
            if (p.O.Zajecie == "budowniczy")
            {
                kandydaci = Place.Keys;
            }
            else if (p.O.Zajecie == "rzemieslnik")
            {
                kandydaci = Budowle.Where(b => Zlecenia.TryGetValue(b.Value, out List<Zlecenie> l) && l.Count > 0).Select(b => b.Key);
            }
            else
            {
                return false;
            }
            List<(int X, int Y)> lista = kandydaci.OrderBy(k => Dystans(p.X, p.Y, k.X + 0.5, k.Y + 0.5)).ToList();
            if (lista.Count == 0)
            {
                return false;
            }
            gdzie = lista[0];
            return true;
        }

        private void PostawPlac(string klucz, int x, int y)
        {
            Budowle[(x, y)] = PrzedrostekPlacu + klucz;
            Place[(x, y)] = new PlacBudowy { Klucz = klucz, X = x, Y = y };
            Pisz($"Plac budowy: {NazwaBudowli(klucz)}. Stań obok i trzymaj E albo przyślij budowniczych.");
        }

        public static string NazwaBudowli(string klucz)
        {
            switch (klucz)
            {
                case "namiot": return "namiot";
                case "chata": return "chata";
                case "palisada": return "palisada";
                case "brama": return "brama";
                case "wieza": return "wieża";
                case "warsztat": return "warsztat";
                case "tartak": return "tartak";
                case "kuznia": return "kuźnia";
                case "ognisko": return "ognisko";
                default: return "śpiwór";
            }
        }

        private void UkonczBudowle(string klucz, int x, int y)
        {
            Budowle[(x, y)] = klucz;
            if (klucz != "palisada")
            {
                Pisz(klucz == "spiwor" ? "Rozkładasz śpiwór." : klucz == "ognisko" ? "Palisz ognisko."
                   : klucz == "chata" ? $"Chata gotowa (+{MiejscaChaty} miejsca)." : $"Gotowe: {NazwaBudowli(klucz)}.");
            }
            PrzeliczMiejsca();
            if (!ObozZalozony && Budowle.ContainsValue("namiot") && Budowle.ContainsValue("ognisko"))
            {
                ObozZalozony = true;
                Pisz("Obóz stoi: namiot i ognisko. To dobre miejsce na osadę — jutro ktoś do ciebie dołączy.");
            }
        }

        // ------------------------------------------------------------------ //
        //  Praca przy budowach i w zakładach                                  //
        // ------------------------------------------------------------------ //

        private bool Pracownik(PostacOsadnika p, string zawod, double x, double y)
        {
            return p.O.Zajecie == zawod && p.O.Chory <= 0 && !p.WWalce && BlokTeraz(p) == BlokPlanu.Praca
                   && Dystans(p.X, p.Y, x, y) <= ZasiegPracy + 0.4;
        }

        private static double Sprawnosc(Osadnik o, string zawod)
        {
            string u = Kompetencje.ZUmiejetnosciZajecia(zawod);
            return 1.0 + (u == null ? 0.0 : Kompetencje.Poziom(o, u) * 0.04);
        }

        /// <summary>Jedna klatka prac. Zwraca true, gdy bohater pracował i nie ma już czym zbierać.</summary>
        private bool KrokBudowy(double dt, Wejscie w)
        {
            Brak = null;
            PostepPracy = null;
            bool bohater = w.Dzialaj && !Spi && !Martwy && Akcja == AkcjaBohatera.Brak;
            bool zajety = false;

            PlacBudowy najblizszy = null;
            double naj = ZasiegPracy;
            foreach (PlacBudowy p in Place.Values)
            {
                double d = Dystans(X, Y, p.X + 0.5, p.Y + 0.5);
                if (d <= naj)
                {
                    naj = d;
                    najblizszy = p;
                }
            }

            foreach (PlacBudowy p in Place.Values.ToList())
            {
                var sklad = new List<double>();
                bool tu = bohater && p == najblizszy;
                if (tu)
                {
                    sklad.Add(Narzedzia.Contains("mlotek") ? 1.3 : 1.0);
                    zajety = true;
                }
                sklad.AddRange(Postacie.Where(q => Pracownik(q, "budowniczy", p.X + 0.5, p.Y + 0.5)).Select(q => Sprawnosc(q.O, "budowniczy")));
                if (sklad.Count > 0)
                {
                    PracujNaPlacu(p, SilaZespolu(sklad), dt, tu);
                }
            }
            if (najblizszy != null && Brak == null && Place.ContainsKey((najblizszy.X, najblizszy.Y)) && !najblizszy.Zaplacony)
            {
                Brak = BrakMaterialow(Etapy[najblizszy.Klucz][najblizszy.Etap].Materialy);
            }

            foreach (string stacja in Stacje)
            {
                zajety |= KrokStacji(stacja, bohater && najblizszy == null, dt);
            }
            return zajety;
        }

        private string BrakMaterialow(KeyValuePair<string, int>[] m)
        {
            List<string> braki = m.Where(p => Razem(p.Key) < p.Value).Select(p => $"{p.Value - Razem(p.Key)} {Nazwa(p.Key)}").ToList();
            return braki.Count == 0 ? null : string.Join(", ", braki);
        }

        private void PracujNaPlacu(PlacBudowy p, double sila, double dt, bool bohater)
        {
            EtapBudowy e = Etapy[p.Klucz][p.Etap];
            if (!p.Zaplacony)
            {
                string brak = BrakMaterialow(e.Materialy);
                if (brak != null)
                {
                    if (bohater)
                    {
                        Brak = brak;
                    }
                    return;
                }
                foreach (KeyValuePair<string, int> m in e.Materialy)
                {
                    Zabierz(m.Key, m.Value);
                }
                p.Zaplacony = true;
            }
            p.Praca += sila * dt;
            if (bohater)
            {
                PostepPracy = (p.X, p.Y, Math.Min(1.0, p.Praca / e.Praca));
            }
            if (p.Praca < e.Praca)
            {
                return;
            }
            foreach (PostacOsadnika q in Postacie.Where(q => Pracownik(q, "budowniczy", p.X + 0.5, p.Y + 0.5)).ToList())
            {
                Kompetencje.Cwicz(q.O, "budowniczy", 0.3);
            }
            p.Praca = 0.0;
            p.Zaplacony = false;
            p.Etap++;
            if (p.Etap >= Etapy[p.Klucz].Length)
            {
                Place.Remove((p.X, p.Y));
                UkonczBudowle(p.Klucz, p.X, p.Y);
            }
            else if (Etapy[p.Klucz].Length > 1)
            {
                Pisz($"{NazwaBudowli(p.Klucz)}: etap {p.Etap} z {Etapy[p.Klucz].Length} gotowy.");
            }
        }

        private bool KrokStacji(string stacja, bool bohater, double dt)
        {
            if (!Zlecenia.TryGetValue(stacja, out List<Zlecenie> kolejka) || kolejka.Count == 0)
            {
                return false;
            }
            List<(int X, int Y)> kafle = Budowle.Where(b => b.Value == stacja).Select(b => b.Key).ToList();
            if (kafle.Count == 0)
            {
                return false;
            }
            (int X, int Y) k = kafle.OrderBy(t => Dystans(X, Y, t.X + 0.5, t.Y + 0.5)).First();
            var sklad = new List<double>();
            bool tu = bohater && Dystans(X, Y, k.X + 0.5, k.Y + 0.5) <= ZasiegPracy;
            if (tu)
            {
                sklad.Add(1.0);
            }
            sklad.AddRange(Postacie.Where(q => kafle.Any(t => Pracownik(q, "rzemieslnik", t.X + 0.5, t.Y + 0.5))).Select(q => Sprawnosc(q.O, "rzemieslnik")));
            if (sklad.Count == 0)
            {
                return false;
            }
            Zlecenie z = kolejka[0];
            if (!z.Zaplacone)
            {
                string brak = BrakMaterialow(z.Co.Wejscie);
                if (brak != null)
                {
                    if (tu)
                    {
                        Brak = brak;
                    }
                    return tu;
                }
                foreach (KeyValuePair<string, int> m in z.Co.Wejscie)
                {
                    Zabierz(m.Key, m.Value);
                }
                z.Zaplacone = true;
            }
            z.Praca += SilaZespolu(sklad) * dt;
            if (tu)
            {
                PostepPracy = (k.X, k.Y, Math.Min(1.0, z.Praca / z.Co.Praca));
            }
            if (z.Praca >= z.Co.Praca)
            {
                kolejka.RemoveAt(0);
                if (z.Co.Narzedzie)
                {
                    Narzedzia.Add(z.Co.Id);
                    Pisz($"Gotowe: {z.Co.Nazwa.ToLowerInvariant()}.");
                }
                else
                {
                    Wyroby[z.Co.Id] = Wyroby.Wez(z.Co.Id) + z.Co.Ilosc;
                }
            }
            return tu;
        }

        /// <summary>Kafelek przepisu: dopisuje zlecenie do kolejki zakładu.</summary>
        private void DodajZlecenie(string id)
        {
            Przepis p = Przepisy.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                return;
            }
            if (!StoiStacja(p.Stacja))
            {
                Pisz($"Najpierw postaw: {NazwaBudowli(p.Stacja)}.");
                return;
            }
            if (!Zlecenia.TryGetValue(p.Stacja, out List<Zlecenie> kolejka))
            {
                kolejka = new List<Zlecenie>();
                Zlecenia[p.Stacja] = kolejka;
            }
            if (p.Narzedzie && (Narzedzia.Contains(id) || kolejka.Any(z => z.Co == p)))
            {
                return;
            }
            if (kolejka.Count >= 8)
            {
                Pisz("Kolejka zakładu jest pełna.");
                return;
            }
            kolejka.Add(new Zlecenie { Co = p });
        }

        public int WKolejce(string id)
        {
            return Zlecenia.Values.Sum(k => k.Count(z => z.Co.Id == id));
        }
    }
}
