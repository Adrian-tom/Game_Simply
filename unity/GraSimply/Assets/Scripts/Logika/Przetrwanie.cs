using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Opis rodzaju rany i jej skutków.</summary>
    public sealed class OpisRany
    {
        public string Nazwa;
        public string Ikona;
        public int Dni;
        public string Opis;
        public double Atak = 1.0;
        public double Leczenie = 1.0;
        public double Ucieczka;
        public bool BezUniku;
        public Dictionary<string, int> Testy = new Dictionary<string, int>();
    }

    /// <summary>Rana na postaci: rodzaj i ile dni jeszcze się goi.</summary>
    public sealed class Rana
    {
        public string Typ;
        public double Dni;
    }

    /// <summary>
    /// Przetrwanie bohatera: prowiant, głód, zimno i rany.
    ///
    /// Każdy dzień wyprawy zjada rację, zima rani bez ciepłego odzienia,
    /// a ciężkie ciosy zostawiają rany, które trzeba wyleczyć w obozie.
    /// </summary>
    public static class Przetrwanie
    {
        public static readonly Dictionary<string, OpisRany> Rany =
            new Dictionary<string, OpisRany>
            {
                {
                    "zlamana_reka", new OpisRany
                    {
                        Nazwa = "Złamana ręka", Ikona = "🦴", Dni = 6,
                        Opis = "Atak −20%.", Atak = 0.8,
                    }
                },
                {
                    "gleboka_rana", new OpisRany
                    {
                        Nazwa = "Głęboka rana", Ikona = "🩸", Dni = 5,
                        Opis = "Mikstury leczą o połowę słabiej.", Leczenie = 0.5,
                    }
                },
                {
                    "zwichnieta_noga", new OpisRany
                    {
                        Nazwa = "Zwichnięta noga", Ikona = "🦵", Dni = 4,
                        Opis = "Ucieczka −20%, brak pasywnego uniku.",
                        Ucieczka = -0.20, BezUniku = true,
                    }
                },
                {
                    "wstrzas", new OpisRany
                    {
                        Nazwa = "Wstrząs mózgu", Ikona = "💫", Dni = 3,
                        Opis = "−3 do testów Inteligencji i Mądrości.",
                        Testy = new Dictionary<string, int>
                        {
                            { "inteligencja", -3 }, { "madrosc", -3 },
                        },
                    }
                },
                {
                    "ciezka_rana", new OpisRany
                    {
                        Nazwa = "Ciężka rana", Ikona = "⚰", Dni = 8,
                        Opis = "Po upadku w boju: atak −15%, mikstury −30%, testy fizyczne −2.",
                        Atak = 0.85, Leczenie = 0.7,
                        Testy = new Dictionary<string, int>
                        {
                            { "sila", -2 }, { "zrecznosc", -2 },
                        },
                    }
                },
            };

        /// <summary>Rany, które mogą wyniknąć ze zwykłego ciosu (ciężka rana jest po upadku).</summary>
        private static readonly string[] RanyZCiosu =
        {
            "zlamana_reka", "gleboka_rana", "zwichnieta_noga", "wstrzas",
        };

        public static bool MaRane(Gracz gracz, string klucz)
        {
            return gracz.Rany.Any(r => r.Typ == klucz);
        }

        public static string DodajRane(Gracz gracz, string klucz, int? dni = null)
        {
            OpisRany info = Rany[klucz];
            int ile = dni ?? info.Dni;
            foreach (Rana r in gracz.Rany)
            {
                if (r.Typ == klucz)
                {
                    r.Dni = Math.Max(r.Dni, ile);
                    return $"  {info.Ikona}  {info.Nazwa} się odnawia ({Formatuj(r.Dni)} dni leczenia).";
                }
            }
            gracz.Rany.Add(new Rana { Typ = klucz, Dni = ile });
            gracz.Statystyki["rany"] = gracz.Statystyki.Wez("rany") + 1;
            return $"  {info.Ikona}  RANA: {info.Nazwa} — {info.Opis} ({ile} dni leczenia)";
        }

        /// <summary>
        /// Po ciosie: ciężkie trafienie albo niskie HP może zostawić ranę.
        /// Zwraca komunikat albo null, gdy rany nie było.
        /// </summary>
        public static string MozeZranic(Gracz gracz, int obrazenia, bool jestBoss, Losowanie rng)
        {
            if (obrazenia <= 0 || !gracz.Zyje())
            {
                return null;
            }
            bool ciezki = obrazenia >= gracz.MaxHp * 0.22;
            bool naKrawedzi = gracz.Hp <= gracz.MaxHp * 0.2;
            if (!ciezki && !naKrawedzi)
            {
                return null;
            }
            double szansa = 0.18 + (jestBoss ? 0.12 : 0.0) + (ciezki && naKrawedzi ? 0.10 : 0.0);
            if (gracz.MaTalent("zahartowany_w_boju"))
            {
                szansa /= 2;
            }
            if (rng.Losowa() >= szansa)
            {
                return null;
            }
            var wolne = RanyZCiosu.Where(k => !MaRane(gracz, k)).ToList();
            return wolne.Count == 0 ? null : DodajRane(gracz, rng.Wybierz(wolne));
        }

        /// <summary>Upływ czasu goi rany. Odpoczynek i lecznica przyspieszają (dni &gt; 1).</summary>
        public static List<string> LeczRany(Gracz gracz, double dni)
        {
            if (gracz.MaTalent("polowy_medyk"))
            {
                dni *= 2;
            }
            var msgs = new List<string>();
            foreach (Rana r in gracz.Rany.ToList())
            {
                r.Dni -= dni;
                if (r.Dni <= 0)
                {
                    gracz.Rany.Remove(r);
                    OpisRany info = Rany[r.Typ];
                    msgs.Add($"  {info.Ikona}  {info.Nazwa} się zagoiła.");
                }
            }
            return msgs;
        }

        /// <summary>Bandaż (oDni) albo maść (null = całkiem) na najcięższą ranę.</summary>
        public static string WyleczJedna(Gracz gracz, int? oDni = null)
        {
            if (gracz.Rany.Count == 0)
            {
                return null;
            }
            // Python bierze max() — przy remisie pierwszą z listy.
            Rana r = gracz.Rany[0];
            foreach (Rana kandydat in gracz.Rany)
            {
                if (kandydat.Dni > r.Dni)
                {
                    r = kandydat;
                }
            }
            OpisRany info = Rany[r.Typ];
            if (oDni == null)
            {
                gracz.Rany.Remove(r);
                return $"  {info.Ikona}  {info.Nazwa} wyleczona.";
            }
            r.Dni -= oDni.Value;
            if (r.Dni <= 0)
            {
                gracz.Rany.Remove(r);
                return $"  {info.Ikona}  {info.Nazwa} zagojona dzięki opatrunkowi.";
            }
            return $"  {info.Ikona}  {info.Nazwa}: opatrunek skraca leczenie ({Formatuj(r.Dni)} dni).";
        }

        /// <summary>Jak <c>max(1, round(dni))</c> w Pythonie — bankowe zaokrąglanie i dolny limit 1.</summary>
        private static int Formatuj(double dni)
        {
            return Math.Max(1, (int)Math.Round(dni, MidpointRounding.ToEven));
        }

        public static double MnoznikAtaku(Gracz gracz)
        {
            double m = 1.0;
            foreach (Rana r in gracz.Rany)
            {
                m *= Rany[r.Typ].Atak;
            }
            int glod = gracz.Glod;
            if (glod > 0)
            {
                double kara = 0.1 * Math.Min(glod, 4);
                if (gracz.MaTalent("hartowany"))
                {
                    kara /= 2;
                }
                m *= 1 - kara;
            }
            return m;
        }

        public static double MnoznikLeczenia(Gracz gracz)
        {
            double m = 1.0;
            foreach (Rana r in gracz.Rany)
            {
                m *= Rany[r.Typ].Leczenie;
            }
            if (gracz.MaTalent("zielarz"))
            {
                m *= 1.3;
            }
            return m;
        }

        public static double PremiaUcieczki(Gracz gracz)
        {
            return gracz.Rany.Sum(r => Rany[r.Typ].Ucieczka);
        }

        public static bool BezUniku(Gracz gracz)
        {
            return gracz.Rany.Any(r => Rany[r.Typ].BezUniku);
        }

        public static int KaraTestu(Gracz gracz, string atrybut)
        {
            int kara = gracz.Rany.Sum(r => Rany[r.Typ].Testy.Wez(atrybut));
            if (gracz.Glod >= 2)
            {
                kara -= 1;
            }
            return kara;
        }

        public static string OpisStanu(Gracz gracz)
        {
            var czesci = new List<string>();
            foreach (Rana r in gracz.Rany)
            {
                OpisRany info = Rany[r.Typ];
                czesci.Add($"{info.Ikona} {info.Nazwa} ({Formatuj(r.Dni)} dni)");
            }
            if (gracz.Glod > 0)
            {
                czesci.Add($"🍽 głód ({gracz.Glod} dni)");
            }
            return czesci.Count > 0 ? string.Join(", ", czesci) : "zdrowy";
        }

        // ------------------------------------------------------------------ //
        //  Prowiant i dzień wyprawy                                           //
        // ------------------------------------------------------------------ //

        public static bool MaOdzienie(Gracz gracz)
        {
            return gracz.Przedmioty.Wez("cieple_odzienie") > 0;
        }

        public static int PojemnoscProwiantu(Gracz gracz)
        {
            return 8 + 4 * gracz.PoziomBudynku("stajnie");
        }

        /// <summary>Przed wyprawą: racje z magazynu do plecaka.</summary>
        public static string SpakujProwiant(Gracz gracz)
        {
            int brakuje = PojemnoscProwiantu(gracz) - gracz.Prowiant;
            int bierzesz = Math.Max(0, Math.Min(brakuje, gracz.Surowce.Wez("zywnosc")));
            gracz.Surowce["zywnosc"] = gracz.Surowce.Wez("zywnosc") - bierzesz;
            gracz.Prowiant += bierzesz;
            int zjada = Kalendarz.PoraGracza(gracz).Klucz == "zima" ? 2 : 1;
            int dni = gracz.Prowiant / zjada;
            if (gracz.Prowiant == 0)
            {
                return "  🍖  Magazyn pusty — wyruszasz bez prowiantu. Głód przyjdzie szybko.";
            }
            return $"  🍖  Prowiant: {gracz.Prowiant} racji (wystarczy na ok. {dni} dni, " +
                   $"pojemność {PojemnoscProwiantu(gracz)}).";
        }

        public static void RozpakujProwiant(Gracz gracz)
        {
            if (gracz.Prowiant > 0)
            {
                gracz.Surowce["zywnosc"] = gracz.Surowce.Wez("zywnosc") + gracz.Prowiant;
                gracz.Prowiant = 0;
            }
        }

        /// <summary>Jeden dzień poza obozem: jedzenie, zimno, rany.</summary>
        public static List<string> DzienWyprawy(Gracz gracz, Losowanie rng)
        {
            var msgs = new List<string>();
            bool zima = Kalendarz.PoraGracza(gracz).Klucz == "zima";
            int potrzeba = zima ? 2 : 1;
            if (gracz.MaTalent("oszczedny") && rng.Losowa() < 0.25)
            {
                potrzeba -= 1;
            }
            int zjedzone = Math.Min(potrzeba, gracz.Prowiant);
            gracz.Prowiant -= zjedzone;
            if (zjedzone < potrzeba)
            {
                gracz.Glod += 1;
                int strata = 3 * gracz.Glod;
                if (gracz.MaTalent("hartowany"))
                {
                    strata /= 2;
                }
                gracz.Hp = Math.Max(1, gracz.Hp - strata);
                msgs.Add($"  🍽  Głód ({gracz.Glod} dni): −{strata} HP, atak słabnie. " +
                         "Wróć do obozu albo znajdź jedzenie.");
            }
            else if (gracz.Glod > 0)
            {
                gracz.Glod = 0;
                msgs.Add("  🍖  Najadasz się. Głód mija.");
            }
            if (zima && !MaOdzienie(gracz) && !gracz.MaTalent("hartowany"))
            {
                gracz.Hp = Math.Max(1, gracz.Hp - 4);
                msgs.Add("  ❄  Mróz przenika do kości (−4 HP). Ciepłe odzienie z warsztatu by pomogło.");
            }
            msgs.AddRange(LeczRany(gracz, 0.5)); // w drodze rany goją się wolno
            if (gracz.Prowiant > 0 && gracz.Prowiant <= 2)
            {
                msgs.Add($"  🍖  Zostały ci {gracz.Prowiant} racje.");
            }
            return msgs;
        }

        /// <summary>Dzień w obozie: bohater je z magazynu osady.</summary>
        public static string ZjedzZMagazynu(Gracz gracz)
        {
            if (gracz.Surowce.Wez("zywnosc") > 0)
            {
                gracz.Surowce["zywnosc"] -= 1;
                if (gracz.Glod > 0)
                {
                    gracz.Glod = 0;
                    return "  🍖  W obozie wreszcie jesz do syta.";
                }
                return null;
            }
            gracz.Glod += 1;
            return $"  🍽  W magazynie nie ma jedzenia — głodujesz ({gracz.Glod} dni).";
        }
    }
}
