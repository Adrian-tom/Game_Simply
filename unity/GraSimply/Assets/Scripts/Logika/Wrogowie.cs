using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>Szablon przeciwnika: surowe liczby, z których powstaje wróg danego poziomu.</summary>
    public sealed class SzablonWroga
    {
        public string Nazwa;
        public int Hp;
        public int Atak;
        public int Obrona;
        public int ExpNagroda;
        public int ZlotoMin;
        public int ZlotoMax;
        public string Opis = "";
    }

    /// <summary>Przeciwnik w walce — odpowiednik klasy <c>Przeciwnik</c> z <c>game/enemy.py</c>.</summary>
    public sealed class Przeciwnik
    {
        public string Nazwa;
        public int MaxHp;
        public int Hp;
        public int Atak;
        public int Obrona;
        public int ExpNagroda;
        public string Opis;
        public int Poziom = 1;

        private readonly int _zlotoMin;
        private readonly int _zlotoMax;

        /// <summary>Ruchy zapowiadane z wyprzedzeniem: nazwa → szansa na turę.</summary>
        public Dictionary<string, double> Zapowiedzi;
        public HashSet<string> Slabosci;
        public HashSet<string> Odpornosci;
        public double Regeneracja;
        public bool SzalUzyty;

        public Przeciwnik(string nazwa, int hp, int atak, int obrona, int expNagroda,
                          int zlotoMin, int zlotoMax, string opis = "")
        {
            Nazwa = nazwa;
            MaxHp = hp;
            Hp = hp;
            Atak = atak;
            Obrona = obrona;
            ExpNagroda = expNagroda;
            _zlotoMin = zlotoMin;
            _zlotoMax = zlotoMax;
            Opis = opis;

            // Zdolności wynikają z nazwy — dzięki temu ręcznie stworzony
            // Przeciwnik("Troll", …) w teście zachowuje się jak ten z fabryki.
            Zdolnosci z = Wrogowie.ZdolnosciWroga(nazwa);
            Zapowiedzi = new Dictionary<string, double>(z.Zapowiedzi);
            Slabosci = new HashSet<string>(z.Slabosci);
            Odpornosci = new HashSet<string>(z.Odpornosci);
            Regeneracja = z.Regeneracja;
            SzalUzyty = false;
        }

        public bool Zyje()
        {
            return Hp > 0;
        }

        public int LosoweZloto(Losowanie rng)
        {
            return rng.Calkowita(_zlotoMin, _zlotoMax);
        }

        public string PasekHp(int szerokosc = 20)
        {
            int wypelniony = (int)((double)Hp / MaxHp * szerokosc);
            return "[" + new string('█', Math.Max(0, wypelniony))
                       + new string('░', Math.Max(0, szerokosc - wypelniony)) + "]";
        }

        public override string ToString()
        {
            return $"{Ikony.Wrog(Nazwa)} {Nazwa}  ❤️ {Hp}/{MaxHp} {PasekHp()}";
        }
    }

    /// <summary>Zdolności wyliczone z nazwy wroga.</summary>
    public sealed class Zdolnosci
    {
        public Dictionary<string, double> Zapowiedzi = new Dictionary<string, double>();
        public HashSet<string> Slabosci = new HashSet<string>();
        public HashSet<string> Odpornosci = new HashSet<string>();
        public double Regeneracja;
    }

    /// <summary>
    /// Bestiariusz i fabryka przeciwników — port <c>game/enemy.py</c>.
    ///
    /// Trudność zależy od REGIONU, nie od poziomu gracza: region N ma wrogów
    /// na poziomach 2N-1..2N, więc awans naprawdę czyni bohatera silniejszym
    /// od okolicy, a dalekie regiony są groźne, póki się do nich nie dorośnie.
    /// </summary>
    public static class Wrogowie
    {
        public const double WzrostNaPoziom = 0.13;
        public const double WzrostBossaNaPoziom = 0.10;

        /// <summary>Mnożniki bazy strojone symulacją — patrz ANALIZA.md, „Balans walki”.</summary>
        public static readonly double[] BazaZwyklych = { 1.2, 1.3, 1.1 };
        public static readonly double[] BazaBossow = { 0.65, 0.9, 0.7 };

        public static readonly Dictionary<string, string> NazwyTypow =
            new Dictionary<string, string>
            {
                { "ogien", "ogień" },
                { "swiete", "święte" },
                { "trucizna", "trucizna" },
            };

        private static SzablonWroga S(string nazwa, int hp, int atak, int obrona,
                                      int exp, int zlotoMin, int zlotoMax, string opis)
        {
            return new SzablonWroga
            {
                Nazwa = nazwa, Hp = hp, Atak = atak, Obrona = obrona,
                ExpNagroda = exp, ZlotoMin = zlotoMin, ZlotoMax = zlotoMax, Opis = opis,
            };
        }

        /// <summary>Kolejność ma znaczenie: groźniejsze gatunki dochodzą z poziomem regionu.</summary>
        private static readonly SzablonWroga[] Szablony =
        {
            S("Goblin", 40, 8, 2, 30, 5, 15, "Mały, zielony i wredny."),
            S("Szkielet", 55, 11, 4, 45, 8, 18, "Ożywione kości dawnego wojownika."),
            S("Ork", 80, 16, 6, 70, 15, 30, "Potężny, zielonoskóry wojownik."),
            S("Trolle", 100, 20, 8, 100, 20, 40, "Ogromne stworzenie regenerujące zdrowie."),
            S("Wiedźma", 60, 22, 3, 90, 25, 45, "Czarownica rzucająca mroczne zaklęcia."),
            S("Młody smok", 130, 28, 12, 150, 40, 70, "Skrzydlata bestia ziejąca ogniem."),
        };

        private static readonly Dictionary<string, SzablonWroga[]> SzablonyBiom =
            new Dictionary<string, SzablonWroga[]>
            {
                { "równiny", new[] { S("Hiena stepowa", 52, 12, 3, 42, 8, 18,
                    "Drapieżnik czający się w wysokiej trawie.") } },
                { "ruiny", new[] { S("Strażnik ruin", 72, 15, 7, 68, 14, 28,
                    "Dawny obrońca, który nie zaznał spokoju po śmierci.") } },
                { "las", new[] { S("Wilk cienia", 60, 14, 4, 55, 10, 20,
                    "Bezgłośny drapieżnik stapiający się z cieniem drzew.") } },
                { "bagna", new[] { S("Topielec", 68, 15, 5, 62, 12, 24,
                    "Zgniła istota wynurzająca się z bagiennej toni.") } },
                { "wzgórza", new[] { S("Harpii zwiadowca", 66, 17, 4, 70, 14, 26,
                    "Skrzydlata bestia krążąca nad skalistymi grzbietami.") } },
                { "kanion", new[] { S("Skalny skorpion", 78, 18, 6, 78, 16, 32,
                    "Pancerny drapieżnik polujący między rozgrzanymi skałami.") } },
            };

        private static readonly SzablonWroga[] Bossowie =
        {
            S("Smok Cienia", 230, 30, 12, 500, 80, 150,
              "Starożytny smok opatulony mrokiem, władca tych ziem."),
            S("Licz Prawieczny", 200, 28, 10, 480, 70, 130,
              "Nieumarły czarownik gromadzący dusze poległych przez wieki."),
            S("Król Trolli", 260, 26, 13, 520, 90, 160,
              "Potworny władca trolli, którego ryk rozrysa góry."),
            S("Arcydemon Khaor", 210, 33, 11, 560, 100, 180,
              "Demon przyzwany z głębin otchłani, żądny zniszczenia."),
            S("Strażniczka Wieczności", 240, 29, 12, 540, 85, 165,
              "Pradawna istota pilnująca przejścia między światami."),
        };

        private static readonly Dictionary<string, SzablonWroga> Mityczni =
            new Dictionary<string, SzablonWroga>
            {
                { "portal", S("Strażnik Otchłani", 210, 30, 11, 420, 70, 120,
                    "Istota ze szczeliny między światami. Powietrze wokół niej pęka.") },
                { "leze_smoka", S("Stary smok Ashkaryx", 270, 33, 14, 580, 110, 190,
                    "Pradawny smok w swoim leżu. Skarbiec lśni pod jego brzuchem.") },
                { "latajaca_wyspa", S("Gryf Niebios", 220, 31, 12, 460, 80, 140,
                    "Skrzydlaty strażnik unoszącej się wyspy. Wiatr tnie jak ostrza.") },
            };

        /// <summary>Jeden wpis bestiariusza: czym dany wróg zaskakuje.</summary>
        private sealed class WpisZdolnosci
        {
            public Dictionary<string, double> Zapowiedzi;
            public string[] Slabosci;
            public string[] Odpornosci;
            public double Regeneracja;
        }

        private static WpisZdolnosci Z(Dictionary<string, double> zapowiedzi = null,
                                       string[] slabosci = null,
                                       string[] odpornosci = null,
                                       double regeneracja = 0.0)
        {
            return new WpisZdolnosci
            {
                Zapowiedzi = zapowiedzi ?? new Dictionary<string, double>(),
                Slabosci = slabosci ?? new string[0],
                Odpornosci = odpornosci ?? new string[0],
                Regeneracja = regeneracja,
            };
        }

        private static Dictionary<string, double> Zap(string a, double x)
        {
            return new Dictionary<string, double> { { a, x } };
        }

        private static Dictionary<string, double> Zap(string a, double x, string b, double y)
        {
            return new Dictionary<string, double> { { a, x }, { b, y } };
        }

        /// <summary>
        /// Klucz = fragment nazwy pisany małymi literami. „zapowiedzi” to szansa
        /// na turę, że wróg zapowie ruch — wykona go dopiero w NASTĘPNEJ turze,
        /// więc gracz zdąży odpowiedzieć gardą, miksturą albo ucieczką.
        /// </summary>
        private static readonly List<KeyValuePair<string, WpisZdolnosci>> ZdolnosciWg =
            new List<KeyValuePair<string, WpisZdolnosci>>
            {
                new KeyValuePair<string, WpisZdolnosci>("goblin", Z(slabosci: new[] { "ogien" })),
                new KeyValuePair<string, WpisZdolnosci>("szkielet",
                    Z(odpornosci: new[] { "trucizna" }, slabosci: new[] { "swiete" })),
                new KeyValuePair<string, WpisZdolnosci>("ork", Z(Zap("ciezki_cios", 0.25))),
                new KeyValuePair<string, WpisZdolnosci>("troll",
                    Z(Zap("ciezki_cios", 0.2), slabosci: new[] { "ogien" }, regeneracja: 0.05)),
                new KeyValuePair<string, WpisZdolnosci>("wiedźma",
                    Z(Zap("klatwa", 0.35), slabosci: new[] { "swiete" })),
                new KeyValuePair<string, WpisZdolnosci>("smok",
                    Z(Zap("ogien", 0.34), odpornosci: new[] { "ogien" })),
                new KeyValuePair<string, WpisZdolnosci>("strażnik ruin",
                    Z(Zap("ciezki_cios", 0.25), slabosci: new[] { "swiete" },
                      odpornosci: new[] { "trucizna" })),
                new KeyValuePair<string, WpisZdolnosci>("topielec", Z(slabosci: new[] { "ogien" })),
                new KeyValuePair<string, WpisZdolnosci>("skorpion", Z(Zap("klatwa", 0.2))),
                new KeyValuePair<string, WpisZdolnosci>("harpii", Z(Zap("ciezki_cios", 0.2))),
                new KeyValuePair<string, WpisZdolnosci>("licz",
                    Z(Zap("klatwa", 0.3, "ciezki_cios", 0.15), slabosci: new[] { "swiete" },
                      odpornosci: new[] { "trucizna" })),
                new KeyValuePair<string, WpisZdolnosci>("król trolli",
                    Z(Zap("ciezki_cios", 0.3), slabosci: new[] { "ogien" }, regeneracja: 0.04)),
                new KeyValuePair<string, WpisZdolnosci>("arcydemon",
                    Z(Zap("ogien", 0.25, "ciezki_cios", 0.2), slabosci: new[] { "swiete" },
                      odpornosci: new[] { "ogien" })),
                new KeyValuePair<string, WpisZdolnosci>("strażniczka",
                    Z(Zap("ciezki_cios", 0.3), odpornosci: new[] { "trucizna" })),
                new KeyValuePair<string, WpisZdolnosci>("otchłani",
                    Z(Zap("klatwa", 0.25, "ciezki_cios", 0.2), slabosci: new[] { "swiete" })),
                new KeyValuePair<string, WpisZdolnosci>("gryf", Z(Zap("ciezki_cios", 0.35))),
                new KeyValuePair<string, WpisZdolnosci>("herszt", Z(Zap("ciezki_cios", 0.3))),
            };

        /// <summary>Łączy wpisy pasujące do nazwy — „Król Trolli” dostaje też cechy trolla.</summary>
        public static Zdolnosci ZdolnosciWroga(string nazwa)
        {
            string n = (nazwa ?? "").ToLowerInvariant();
            var wynik = new Zdolnosci();
            foreach (KeyValuePair<string, WpisZdolnosci> para in ZdolnosciWg)
            {
                if (n.IndexOf(para.Key, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                foreach (KeyValuePair<string, double> z in para.Value.Zapowiedzi)
                {
                    wynik.Zapowiedzi[z.Key] = z.Value;
                }

                foreach (string s in para.Value.Slabosci)
                {
                    wynik.Slabosci.Add(s);
                }

                foreach (string o in para.Value.Odpornosci)
                {
                    wynik.Odpornosci.Add(o);
                }

                wynik.Regeneracja = Math.Max(wynik.Regeneracja, para.Value.Regeneracja);
            }

            wynik.Slabosci.ExceptWith(wynik.Odpornosci);
            return wynik;
        }

        /// <summary>Słabość ×1.5, odporność ×0.5, reszta ×1.</summary>
        public static double MnoznikTypu(Przeciwnik przeciwnik, string typ)
        {
            if (przeciwnik.Slabosci.Contains(typ))
            {
                return 1.5;
            }

            if (przeciwnik.Odpornosci.Contains(typ))
            {
                return 0.5;
            }

            return 1.0;
        }

        public static int PoziomWroga(int mapaGen, Losowanie rng)
        {
            return Math.Max(1, 2 * mapaGen - 1 + rng.Calkowita(0, 1));
        }

        public static int PoziomBossa(int mapaGen)
        {
            return Math.Max(3, 2 * mapaGen + 1);
        }

        private static double MnoznikTrudnosci(string tryb)
        {
            if (tryb == "hardcore")
            {
                return 1.2;
            }

            if (tryb == "latwy")
            {
                return 0.85;
            }

            return 1.0;
        }

        private static Przeciwnik ZSzablonu(SzablonWroga szablon, int poziom, double skala,
                                            double[] baza = null)
        {
            baza = baza ?? BazaZwyklych;
            var wrog = new Przeciwnik(
                szablon.Nazwa,
                (int)(szablon.Hp * skala * baza[0]),
                (int)(szablon.Atak * skala * baza[1]),
                (int)(szablon.Obrona * skala * baza[2]),
                (int)(szablon.ExpNagroda * (1 + (poziom - 1) * 0.15)),
                (int)(szablon.ZlotoMin * skala),
                (int)(szablon.ZlotoMax * skala),
                szablon.Opis);
            wrog.Poziom = poziom;
            return wrog;
        }

        /// <summary>Boss regionu: poziom 2N+1 — wyzwanie, do którego trzeba się przygotować.</summary>
        public static Przeciwnik LosujBossa(Losowanie rng, int mapaGen = 1, string tryb = "normalny")
        {
            SzablonWroga szablon = rng.Wybierz(Bossowie);
            int poziom = PoziomBossa(mapaGen);
            double skala = (1 + (poziom - 3) * WzrostBossaNaPoziom) * MnoznikTrudnosci(tryb);
            return ZSzablonu(szablon, poziom, skala, BazaBossow);
        }

        /// <summary>Unikalny wróg mitycznej lokacji — poziom bossa +1.</summary>
        public static Przeciwnik LosujMitycznego(string typ, Losowanie rng, int mapaGen = 1,
                                                 string tryb = "normalny")
        {
            SzablonWroga szablon;
            if (typ == null || !Mityczni.TryGetValue(typ, out szablon))
            {
                szablon = Mityczni["portal"];
            }

            int poziom = PoziomBossa(mapaGen) + 1;
            double skala = (1 + (poziom - 3) * WzrostBossaNaPoziom) * MnoznikTrudnosci(tryb);
            return ZSzablonu(szablon, poziom, skala, BazaBossow);
        }

        /// <summary>
        /// Losuje przeciwnika dla regionu. Poziom gracza nie wpływa na siłę —
        /// decyduje poziom regionu, dlatego nie ma go w sygnaturze.
        /// </summary>
        public static Przeciwnik LosujPrzeciwnika(Losowanie rng, string biom = null,
                                                  int mapaGen = 1, string tryb = "normalny",
                                                  int poziom = -1)
        {
            if (poziom < 0)
            {
                poziom = PoziomWroga(mapaGen, rng);
            }

            // Groźniejsze gatunki dochodzą z poziomem: troll od 3., wiedźma od 4., smok od 6.
            int ile = 2 + (poziom >= 2 ? 1 : 0) + (poziom >= 3 ? 1 : 0)
                        + (poziom >= 4 ? 1 : 0) + (poziom >= 6 ? 1 : 0);
            var dostepne = new List<SzablonWroga>();
            for (int i = 0; i < Math.Min(Szablony.Length, ile); i++)
            {
                dostepne.Add(Szablony[i]);
            }

            SzablonWroga[] zBiomu;
            if (biom != null && SzablonyBiom.TryGetValue(biom, out zBiomu))
            {
                dostepne.AddRange(zBiomu);
            }

            SzablonWroga szablon = rng.Wybierz(dostepne);
            double skala = (1 + (poziom - 1) * WzrostNaPoziom) * MnoznikTrudnosci(tryb);
            return ZSzablonu(szablon, poziom, skala);
        }

        /// <summary>Przywódca bandy napadającej na osadę — rośnie razem z siłą najazdu.</summary>
        public static Przeciwnik HersztNajazdu(int sila, string tryb = "normalny")
        {
            int poziom = Math.Max(1, Math.Min(14, sila / 25));
            SzablonWroga szablon = S("Herszt bandy", 95, 17, 6, 90, 20, 45,
                                     "Przywódca najazdu. Blizny liczy jak trofea.");
            double skala = (1 + (poziom - 1) * WzrostNaPoziom) * MnoznikTrudnosci(tryb);
            return ZSzablonu(szablon, poziom, skala);
        }
    }
}
