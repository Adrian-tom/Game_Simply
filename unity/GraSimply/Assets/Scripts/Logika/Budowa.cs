using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Plan budowy w toku: co, który poziom, ile pracy zrobiono i za ile materiałów go zlecono.</summary>
    public sealed class PlanBudowy
    {
        public int Id;

        /// <summary>Klucz budynku albo „chata”.</summary>
        public string Klucz;
        public int Poziom;
        public double Zrobiona;
        public double Wymagana;

        /// <summary>Kopia kosztu — z niej liczy się zwrot przy anulowaniu.</summary>
        public Dictionary<string, int> Koszt = new Dictionary<string, int>();
    }

    /// <summary>
    /// Budowa w czasie: plany, budowniczowie i rusztowania — jak blueprint w RimWorldzie.
    /// Odpowiednik <c>game/budowa.py</c>.
    ///
    /// Zlecenie budowy <b>od razu pobiera materiały</b> i zakłada plan; potem dzień po
    /// dniu przybywa pracy, aż budynek stanie. Pracują budowniczowie (pełna moc, rośnie
    /// ich umiejętność Budowa), bezczynni osadnicy (35%) i pozostali „po godzinach” (10%),
    /// a jeden plan przyjmuje dziennie co najwyżej <see cref="LimitPlanu"/> pracy.
    /// Czego wolno budować, mówi <see cref="Badania"/>; ile trwa — tabela <see cref="Praca"/>.
    /// </summary>
    public static class Budowa
    {
        public const int MaksPlanow = 4;
        public const double LimitPlanu = 2.5;            // tyle pracy dziennie przyjmie jeden plan
        public const double WagaBezczynnego = 0.35;
        public const double WagaPozostalych = 0.10;
        public const double PracaBohatera = 1.5;         // dzień bohatera przy budowie (plus premia z siły)
        public const double ZwrotMin = 0.5;              // anulowanie prawie skończonej budowy zwraca połowę
        public const double KosztChatyPraca = 2.0;

        /// <summary>Dni pracy budowniczego (mnożnik 1,0) na poziom 1; poziom n wymaga baza × n.</summary>
        public static readonly Dictionary<string, double> Praca = new Dictionary<string, double>
        {
            { "sklep", 2.0 }, { "dom", 3.0 }, { "farma", 2.0 }, { "spichlerz", 3.0 }, { "tartak", 3.0 },
            { "huta", 4.0 }, { "kuznia", 4.0 }, { "warsztat", 3.0 }, { "laboratorium", 5.0 },
            { "lecznica", 3.0 }, { "targ", 3.0 }, { "karawanseraj", 5.0 }, { "tawerna", 3.0 },
            { "palisada", 2.0 }, { "wieza", 4.0 }, { "stajnie", 3.0 }, { "skryptorium", 3.0 },
        };

        public static double PracaPotrzebna(string klucz, int poziom)
        {
            if (klucz == "chata")
            {
                return KosztChatyPraca;
            }
            return Praca.TryGetValue(klucz, out double baza) ? baza * poziom : 3.0 * poziom;
        }

        public static List<PlanBudowy> Plany(Gracz gracz)
        {
            if (gracz.Budowy == null)
            {
                gracz.Budowy = new List<PlanBudowy>();
            }
            return gracz.Budowy;
        }

        public static PlanBudowy PlanDla(Gracz gracz, string klucz)
        {
            return Plany(gracz).FirstOrDefault(p => p.Klucz == klucz);
        }

        public static string NazwaPlanu(PlanBudowy plan)
        {
            if (plan.Klucz == "chata")
            {
                return "Chata";
            }
            return $"{Oboz.OpisBudynkuKlucza(plan.Klucz).Nazwa} (poz. {plan.Poziom})";
        }

        public static string IkonaPlanu(PlanBudowy plan)
        {
            return plan.Klucz == "chata" ? "🛖" : Oboz.OpisBudynkuKlucza(plan.Klucz).Ikona;
        }

        /// <summary>Ułamek ukończenia 0–1.</summary>
        public static double PostepPlanu(PlanBudowy plan)
        {
            return plan.Wymagana != 0 ? Math.Max(0.0, Math.Min(1.0, plan.Zrobiona / plan.Wymagana)) : 1.0;
        }

        // ------------------------------------------------------------------ //
        //  Ludzie                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>Kto buduje i z jaką mocą dzienną: (osadnik, moc). Chorzy odpoczywają.</summary>
        public static List<(Osadnik Osadnik, double Moc)> Robotnicy(Gracz gracz)
        {
            var wynik = new List<(Osadnik Osadnik, double Moc)>();
            foreach (Osadnik o in Osada.Osadnicy(gracz))
            {
                if (o.Chory > 0)
                {
                    continue;
                }
                double waga = o.Zajecie == "budowniczy" ? 1.0
                            : o.Zajecie == "bezczynny" ? WagaBezczynnego : WagaPozostalych;
                wynik.Add((o, waga * Osada.MnoznikPracy(gracz, o, "budowniczy")));
            }
            return wynik;
        }

        /// <summary>Ile pracy osada wykonuje dziennie, gdyby miała co budować.</summary>
        public static double MocBudowy(Gracz gracz)
        {
            double suma = 0.0;
            foreach ((Osadnik _, double moc) in Robotnicy(gracz))
            {
                suma += moc;
            }
            return suma;
        }

        public static double PracaBohateraDzien(Gracz gracz)
        {
            return PracaBohatera + 0.25 * Math.Max(0, Atrybuty.Modyfikator(gracz, "sila"));
        }

        // ------------------------------------------------------------------ //
        //  Zlecanie i anulowanie                                              //
        // ------------------------------------------------------------------ //

        public static int? NastepnyPoziom(Gracz gracz, string klucz)
        {
            int n = gracz.PoziomBudynku(klucz) + 1;
            return n <= Oboz.OpisBudynkuKlucza(klucz).Max ? n : (int?)null;
        }

        /// <summary>Czemu nie da się teraz zlecić budowy (bez sprawdzania portfela) albo null.</summary>
        public static string PowodBlokady(Gracz gracz, string klucz)
        {
            if (klucz == "chata")
            {
                int razem = Osada.LiczbaChat(gracz) + Plany(gracz).Count(p => p.Klucz == "chata");
                if (razem >= Osada.MaxChaty)
                {
                    return $"maks. {Osada.MaxChaty} chat";
                }
                if (razem >= Badania.LimitChat(gracz))
                {
                    string projekt = Badania.ProjektDlaChat(gracz);
                    return "limit chat — poznaj: " + (projekt != null ? Badania.Opis(projekt).Nazwa : "—");
                }
                return null;
            }
            int? nast = NastepnyPoziom(gracz, klucz);
            if (nast == null)
            {
                return "najwyższy poziom";
            }
            if (PlanDla(gracz, klucz) != null)
            {
                return "w budowie";
            }
            if (nast.Value > Badania.MaksPoziom(gracz, klucz))
            {
                string projekt = Badania.ProjektDla(klucz, nast.Value);
                return "nieznane — poznaj: " + (projekt != null ? Badania.Opis(projekt).Nazwa : "—");
            }
            return null;
        }

        private static Dictionary<string, int> KosztPlanu(Gracz gracz, string klucz)
        {
            return klucz == "chata" ? new Dictionary<string, int>(Osada.KosztChaty) : Oboz.KosztBudowy(gracz, klucz);
        }

        /// <summary>Jak <c>str.capitalize()</c> w Pythonie: pierwsza litera wielka, reszta mała.</summary>
        private static string Kapitalizuj(string s)
        {
            return s.Length == 0 ? s : s.Substring(0, 1).ToUpperInvariant() + s.Substring(1).ToLowerInvariant();
        }

        /// <summary>Zakłada plan budowy (materiały schodzą od razu). Zwraca komunikat.</summary>
        public static string Zlec(Gracz gracz, string klucz)
        {
            if (klucz != "chata" && Oboz.OpisBudynkuKlucza(klucz) == null)
            {
                return "  Nie ma takiej budowli.";
            }
            string nazwa = klucz == "chata" ? "chatę" : Oboz.OpisBudynkuKlucza(klucz).Nazwa.ToLowerInvariant();
            string powod = PowodBlokady(gracz, klucz);
            if (powod == "w budowie")
            {
                return $"  {Kapitalizuj(nazwa)} już stoi na rusztowaniach.";
            }
            if (powod != null)
            {
                return $"  Nie można teraz zlecić ({nazwa}): {powod}.";
            }
            if (Plany(gracz).Count >= MaksPlanow)
            {
                return $"  Kolejka budów jest pełna ({MaksPlanow}) — dokończ coś albo anuluj plan.";
            }
            Dictionary<string, int> koszt = KosztPlanu(gracz, klucz);
            if (!Oboz.MozeZaplacic(gracz, koszt))
            {
                return $"  Brakuje materiałów na {nazwa}.  Potrzeba: {Oboz.FormatKosztu(koszt)}.";
            }
            Oboz.PobierzKoszt(gracz, koszt);
            int poziom = klucz == "chata" ? 1 : gracz.PoziomBudynku(klucz) + 1;
            int ostatni = Plany(gracz).Count > 0 ? Plany(gracz).Max(p => p.Id) : 0;
            var plan = new PlanBudowy
            {
                Id = ostatni + 1, Klucz = klucz, Poziom = poziom, Zrobiona = 0.0,
                Wymagana = PracaPotrzebna(klucz, poziom), Koszt = koszt,
            };
            Plany(gracz).Add(plan);
            int? dni = DniDoKonca(gracz, plan);
            string eta = dni.HasValue ? $"ok. {dni.Value} dni"
                                      : "ale nikt nie ma czasu budować — przydziel budowniczego [16]";
            return $"  📐  Zlecono budowę: {IkonaPlanu(plan)} {NazwaPlanu(plan)}.  "
                   + $"Praca: {Liczby.Format(plan.Wymagana, 0)} dni roboczych, {eta}.";
        }

        /// <summary>Zdejmuje plan. Zwrot materiałów: 100% bez prac, do 50% przy prawie skończonym.</summary>
        public static string Anuluj(Gracz gracz, int idPlanu)
        {
            PlanBudowy plan = Plany(gracz).FirstOrDefault(p => p.Id == idPlanu);
            if (plan == null)
            {
                return "  Nie ma takiego planu.";
            }
            double ulamek = 1.0 - (1.0 - ZwrotMin) * PostepPlanu(plan);
            var zwrot = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<string, int> para in plan.Koszt)
            {
                int oddaj = (int)(para.Value * ulamek);
                if (para.Key == "zloto")
                {
                    gracz.Zloto += oddaj;
                }
                else
                {
                    gracz.Surowce[para.Key] = gracz.Surowce.Wez(para.Key) + oddaj;
                }
                if (oddaj != 0)
                {
                    zwrot.Add(new KeyValuePair<string, int>(para.Key, oddaj));
                }
            }
            Plany(gracz).Remove(plan);
            return $"  ✖  Anulowano: {NazwaPlanu(plan)}.  Zwrot {Liczby.ZaokragliDoInt(ulamek * 100)}%: "
                   + $"{Oboz.FormatKosztu(zwrot)}.";
        }

        // ------------------------------------------------------------------ //
        //  Czas                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Szacunek dni do ukończenia planu przy obecnej mocy albo null, gdy nikt nie buduje.</summary>
        public static int? DniDoKonca(Gracz gracz, PlanBudowy plan)
        {
            double moc = MocBudowy(gracz);
            if (moc <= 0)
            {
                return null;
            }
            var zostalo = new Dictionary<int, double>();
            foreach (PlanBudowy p in Plany(gracz))
            {
                zostalo[p.Id] = Math.Max(0.0, p.Wymagana - p.Zrobiona);
            }
            for (int dzien = 1; dzien < 400; dzien++)
            {
                double pula = moc;
                foreach (PlanBudowy p in Plany(gracz))
                {
                    if (zostalo[p.Id] <= 0)
                    {
                        continue;
                    }
                    double bierze = Math.Min(Math.Min(pula, LimitPlanu), zostalo[p.Id]);
                    zostalo[p.Id] -= bierze;
                    pula -= bierze;
                }
                if (zostalo[plan.Id] <= 1e-9)
                {
                    return dzien;
                }
            }
            return null;
        }

        private static string Ukoncz(Gracz gracz, PlanBudowy plan)
        {
            Plany(gracz).Remove(plan);
            if (plan.Klucz == "chata")
            {
                gracz.Chaty += 1;
                gracz.Statystyki["zbudowane_chaty"] = gracz.Chaty;
                return $"  🛖  Chata stoi ({gracz.Chaty}/{Osada.MaxChaty})! Ktoś może się tu wprowadzić.";
            }
            OpisBudynku info = Oboz.OpisBudynkuKlucza(plan.Klucz);
            Oboz.UstawPoziom(gracz, plan.Klucz, Math.Max(gracz.PoziomBudynku(plan.Klucz), plan.Poziom));
            gracz.Statystyki.Dodaj("ukonczone_budowy", 1);
            return plan.Poziom == 1
                ? $"  {info.Ikona}  UKOŃCZONO: {info.Nazwa}!  ({info.Opis})"
                : $"  {info.Ikona}  UKOŃCZONO: {info.Nazwa} rozbudowana do poziomu {plan.Poziom}!";
        }

        /// <summary>
        /// Ukończona budowa mogła domknąć zadanie. Questów (<c>game/quests.py</c>) port jeszcze
        /// nie ma — to miejsce styku: tu dojdzie wołanie sprawdzania zadań.
        /// </summary>
        private static List<string> ZQuestami(Gracz gracz, List<string> wiesci)
        {
            return wiesci;
        }

        /// <summary>Jeden dzień pracy na budowach. Zwraca (wieści do kroniki, wieści pilne).</summary>
        public static (List<string> Kronika, List<string> Pilne) DzienBudowy(Gracz gracz)
        {
            var kronika = new List<string>();
            var pilne = new List<string>();
            List<PlanBudowy> kolejka = Plany(gracz);
            if (kolejka.Count == 0)
            {
                return (kronika, pilne);
            }
            List<(Osadnik Osadnik, double Moc)> ludzie = Robotnicy(gracz);
            double moc = 0.0;
            foreach ((Osadnik _, double m) in ludzie)
            {
                moc += m;
            }
            double pula = moc;
            double wykorzystano = 0.0;
            foreach (PlanBudowy plan in kolejka.ToList())
            {
                double bierze = Math.Min(Math.Min(pula, LimitPlanu), plan.Wymagana - plan.Zrobiona);
                if (bierze <= 0)
                {
                    continue;
                }
                plan.Zrobiona += bierze;
                pula -= bierze;
                wykorzystano += bierze;
                if (plan.Zrobiona >= plan.Wymagana - 1e-9)
                {
                    pilne.Add(Ukoncz(gracz, plan));
                }
            }
            if (wykorzystano > 0)
            {
                double czesc = wykorzystano / moc;
                foreach ((Osadnik o, double _) in ludzie)
                {
                    if (o.Zajecie == "budowniczy")
                    {
                        kronika.AddRange(Kompetencje.Cwicz(o, "budowniczy", czesc));
                    }
                    else if (o.Zajecie == "bezczynny")
                    {
                        kronika.AddRange(Kompetencje.Cwicz(o, "budowniczy", 0.5 * czesc));
                    }
                }
            }
            return (kronika, ZQuestami(gracz, pilne));
        }

        /// <summary>Bohater spędza dzień przy budowie: dopisuje swoją pracę, a reszta osady pracuje jak zwykle.</summary>
        public static string Pomoz(IKonsola konsola, Gracz gracz, Losowanie rng, int idPlanu)
        {
            PlanBudowy plan = Plany(gracz).FirstOrDefault(p => p.Id == idPlanu);
            if (plan == null)
            {
                return "  Nie ma takiego planu.";
            }
            if (!gracz.WObozie)
            {
                return "  Przy budowie możesz pracować tylko w obozie.";
            }
            double dodane = Math.Min(PracaBohateraDzien(gracz), plan.Wymagana - plan.Zrobiona);
            plan.Zrobiona += dodane;
            gracz.Statystyki.Dodaj("dni_budowy", 1);
            var tekst = new List<string>
            {
                $"  🔨  Cały dzień przy budowie: {NazwaPlanu(plan)} +{Liczby.Format(dodane, 1)} dnia pracy.",
            };
            if (plan.Zrobiona >= plan.Wymagana - 1e-9)
            {
                tekst.AddRange(ZQuestami(gracz, new List<string> { Ukoncz(gracz, plan) }));
            }
            Osada.DodajCzas(konsola, gracz, rng, 1);
            return string.Join("\n", tekst);
        }

        /// <summary>Co stoi na rusztowaniach: (klucz, poziom, postęp 0–1) — dla widoku osady.</summary>
        public static List<(string Klucz, int Poziom, double Postep)> Rusztowania(Gracz gracz)
        {
            return Plany(gracz).Select(p => (p.Klucz, p.Poziom, PostepPlanu(p))).ToList();
        }
