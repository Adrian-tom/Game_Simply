using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Cecha osadnika: opis i mnożniki (1 = bez wpływu).</summary>
    public sealed class CechaOsadnika
    {
        public string Opis;
        public double Praca = 1.0;
        public int Je = 1;
        public double Morale;
        public double Straz = 1.0;
        public bool Nieustraszony;
        public double Rzemioslo = 1.0;
        public double Handel = 1.0;
        public double Choroby = 1.0;
    }

    /// <summary>Zajęcie osadnika: nazwa, ikona, opis i ewentualny wymagany budynek.</summary>
    public sealed class OpisZajecia
    {
        public string Nazwa;
        public string Ikona;
        public string Opis;
        public string Wymaga;
        public int Miejsca = 1;
    }

    /// <summary>
    /// Osada: osadnicy z morale, cechami, umiejętnościami i pasjami, zawody i produkcja.
    /// Odpowiednik <c>game/osada.py</c>. Z dziennego cyklu przeniesione jest wszystko,
    /// co nie wymaga niezportowanych jeszcze modułów; miejsca styku (więzi, myśli,
    /// zamówienia rzemieślników, drużyna na zbiorach) są oznaczone komentarzem.
    /// </summary>
    public static partial class Osada
    {
        public const int MaxChaty = 12;
        public const int CenaOsadnika = 28;

        public static readonly Dictionary<string, int> KosztChaty = new Dictionary<string, int>
        {
            { "drewno", 8 }, { "kamien", 4 }, { "zloto", 20 },
        };

        private static readonly string[] Imiona =
        {
            "Nessa", "Torin", "Elka", "Bram", "Sira", "Olek", "Mila", "Gareth", "Iva", "Piotr",
            "Ruta", "Kael", "Dobrosz", "Jagna", "Wit", "Hela", "Marek", "Zofka", "Lech", "Agna",
            "Borys", "Tamara", "Janko", "Ursa",
        };

        private static KeyValuePair<string, CechaOsadnika> C(string k, CechaOsadnika c)
        {
            return new KeyValuePair<string, CechaOsadnika>(k, c);
        }

        /// <summary>Kolejność jak w Pythonie — od niej zależy losowanie cechy nowego osadnika.</summary>
        public static readonly List<KeyValuePair<string, CechaOsadnika>> Cechy =
            new List<KeyValuePair<string, CechaOsadnika>>
            {
                C("pracowity", new CechaOsadnika { Opis = "+25% pracy", Praca = 1.25 }),
                C("leniwy", new CechaOsadnika { Opis = "−25% pracy", Praca = 0.75 }),
                C("zarloczny", new CechaOsadnika { Opis = "je za dwóch", Je = 2 }),
                C("skromny", new CechaOsadnika { Opis = "łatwo go zadowolić (+5 morale)", Morale = 5 }),
                C("odwazny", new CechaOsadnika { Opis = "strażnik ×1,5, najazdy go nie łamią", Straz = 1.5, Nieustraszony = true }),
                C("tchorzliwy", new CechaOsadnika { Opis = "strażnik ×0,5", Straz = 0.5 }),
                C("zreczny", new CechaOsadnika { Opis = "+25% w tartaku, hucie i warsztacie", Rzemioslo = 1.25 }),
                C("gadatliwy", new CechaOsadnika { Opis = "+30% w handlu", Handel = 1.3 }),
                C("chorowity", new CechaOsadnika { Opis = "choruje dwa razy częściej", Choroby = 2.0 }),
                C("wesoly", new CechaOsadnika { Opis = "pogodny (+10 morale)", Morale = 10 }),
            };

        public static CechaOsadnika Cecha(string klucz)
        {
            foreach (KeyValuePair<string, CechaOsadnika> p in Cechy)
            {
                if (p.Key == klucz)
                {
                    return p.Value;
                }
            }
            return new CechaOsadnika();
        }

        private static KeyValuePair<string, OpisZajecia> Z(string k, string nazwa, string ikona, string opis,
                                                           string wymaga = null, int miejsca = 1)
        {
            return new KeyValuePair<string, OpisZajecia>(k, new OpisZajecia
            {
                Nazwa = nazwa, Ikona = ikona, Opis = opis, Wymaga = wymaga, Miejsca = miejsca,
            });
        }

        /// <summary>Wymaga: budynek; Miejsca: stanowisk na poziom budynku (bez „wymaga” — bez limitu).</summary>
        public static readonly List<KeyValuePair<string, OpisZajecia>> Zajecia =
            new List<KeyValuePair<string, OpisZajecia>>
            {
                Z("bezczynny", "bez zajęcia", "💤", "odpoczywa"),
                Z("drwal", "drwal", "🪓", "+2 drewna"),
                Z("kamieniarz", "kamieniarz", "🪨", "+2 kamienia"),
                Z("zielarz", "zielarz", "🌿", "+2 zioła (zależnie od pory roku)"),
                Z("mysliwy", "myśliwy", "🏹", "+2 żywności, czasem skóra"),
                Z("rolnik", "rolnik", "🌾", "+3 żywności × pora roku (jesień ×1,6, zima nic)", "farma", 2),
                Z("gornik", "górnik", "⛏", "+1–2 rudy"),
                Z("tracz", "tracz", "🪚", "2 drewna → 1 deska", "tartak", 2),
                Z("hutnik", "hutnik", "🔥", "2 rudy + 1 drewno → 1 żelazo", "huta", 2),
                Z("handlarz", "handlarz", "💰", "+2 zł, z targiem +4 i więcej"),
                Z("rzemieslnik", "rzemieślnik", "🔧", "wytwarza zamówienia z warsztatu i laboratorium", "warsztat", 2),
                Z("straznik", "strażnik", "🛡", "+6 do obrony osady"),
                Z("uzdrowiciel", "uzdrowiciel", "⚕", "leczy chorych, przyspiesza gojenie ran", "lecznica", 1),
                Z("budowniczy", "budowniczy", "🏗", "stawia to, co zlecisz w [11] — rośnie umiejętność Budowa"),
                Z("badacz", "badacz", "📜", "w skryptorium zdobywa punkty wiedzy na nowe budowle i epoki", "skryptorium", 1),
            };

        public static readonly Dictionary<string, string> AliasyZajec = new Dictionary<string, string>
        {
            { "zbiory", "drwal" }, { "handel", "handlarz" }, { "rzemioslo", "rzemieslnik" },
        };

        public static OpisZajecia Zajecie(string klucz)
        {
            foreach (KeyValuePair<string, OpisZajecia> p in Zajecia)
            {
                if (p.Key == klucz)
                {
                    return p.Value;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ //
        //  Osadnicy                                                           //
        // ------------------------------------------------------------------ //

        public static int LiczbaChat(Gracz gracz)
        {
            return gracz.Chaty;
        }

        /// <summary>
        /// Dopisuje to, czego osadnikowi brakuje: zajęcie, cechę (losowaną tylko dla nowego —
        /// <paramref name="rng"/> nie jest ruszany, gdy cecha już jest), umiejętności.
        /// </summary>
        public static Osadnik Zapewnij(Osadnik o, Losowanie rng = null)
        {
            string z = o.Zajecie;
            if (!string.IsNullOrEmpty(z) && AliasyZajec.TryGetValue(z, out string alias))
            {
                z = alias;
            }
            if (string.IsNullOrEmpty(z) || Zajecie(z) == null)
            {
                z = "drwal";
            }
            o.Zajecie = z;
            if (string.IsNullOrEmpty(o.Cecha))
            {
                o.Cecha = rng != null ? rng.Wybierz(Cechy.Select(p => p.Key).ToList()) : "skromny";
            }
            Kompetencje.Zapewnij(o);
            return o;
        }

        public static List<Osadnik> Osadnicy(Gracz gracz)
        {
            foreach (Osadnik o in gracz.Osadnicy)
            {
                Zapewnij(o);
            }
            return gracz.Osadnicy;
        }

        public static int WolneChaty(Gracz gracz)
        {
            return Math.Max(0, LiczbaChat(gracz) - Osadnicy(gracz).Count);
        }

        /// <summary>Ile stanowisk daje zajęcie (null = bez limitu).</summary>
        public static int? Miejsca(Gracz gracz, string zajecie)
        {
            OpisZajecia info = Zajecie(zajecie);
            if (info.Wymaga == null)
            {
                return null;
            }
            int poziom = gracz.PoziomBudynku(info.Wymaga);
            if (zajecie == "rzemieslnik")
            {
                poziom += gracz.PoziomBudynku("laboratorium");
            }
            return info.Miejsca * poziom;
        }

        public static int IluWZawodzie(Gracz gracz, string zajecie)
        {
            return Osadnicy(gracz).Count(o => o.Zajecie == zajecie);
        }

        public static string UstawZajecie(Gracz gracz, Osadnik o, string zajecie)
        {
            if (AliasyZajec.TryGetValue(zajecie, out string alias))
            {
                zajecie = alias;
            }
            OpisZajecia info = Zajecie(zajecie);
            if (info == null)
            {
                return "  Nie ma takiego zajęcia.";
            }
            if (o.Zajecie == zajecie)
            {
                return $"  {o.Imie} już pracuje jako {info.Nazwa}.";
            }
            int? wolne = Miejsca(gracz, zajecie);
            if (wolne.HasValue)
            {
                if (wolne.Value <= 0)
                {
                    return $"  Potrzebny budynek: {info.Wymaga}.";
                }
                if (IluWZawodzie(gracz, zajecie) >= wolne.Value)
                {
                    return $"  Brak wolnych stanowisk ({wolne.Value}) — rozbuduj {info.Wymaga}.";
                }
            }
            o.Zajecie = zajecie;
            return $"  {info.Ikona}  {o.Imie} zostaje: {info.Nazwa}.";
        }

        /// <summary>Morale (0,6–1,4) × cecha × umiejętność (0,8–1,8) × badania × talent Gospodarz.</summary>
        public static double MnoznikPracy(Gracz gracz, Osadnik o, string zajecie = null)
        {
            string z = string.IsNullOrEmpty(zajecie) ? o.Zajecie : zajecie;
            CechaOsadnika cecha = Cecha(o.Cecha);
            double m = 0.6 + o.Morale / 125;
            m *= cecha.Praca;
            if (z == "tracz" || z == "hutnik" || z == "rzemieslnik")
            {
                m *= cecha.Rzemioslo;
            }
            if (z == "handlarz")
            {
                m *= cecha.Handel;
            }
            m *= Kompetencje.Czynnik(o, z);
            m *= Badania.BonusZajecia(gracz, z);
            if (gracz.MaTalent("gospodarz"))
            {
                m *= 1.10;
            }
            return m;
        }

        /// <summary>Losowe zaokrąglenie — 1,3 dnia pracy daje czasem 1, czasem 2.</summary>
        public static int Zaokraglij(double x, Losowanie rng)
        {
            int calosc = (int)x;
            return calosc + (rng.Losowa() < x - calosc ? 1 : 0);
        }

        /// <summary>Mija <paramref name="ile"/> dni: pilne wieści od razu na ekran.</summary>
        public static void DodajCzas(IKonsola konsola, Gracz gracz, Losowanie rng, int ile = 1)
        {
            foreach (string wiesc in Swiat.MinijDni(gracz, rng, ile))
            {
                konsola.Pisz(wiesc);
            }
        }

        // ------------------------------------------------------------------ //
        //  Zatrudnianie, chaty, flagi                                         //
        // ------------------------------------------------------------------ //

        public static string ZatrudnijOsadnika(Gracz gracz, Losowanie rng, string zajecie = "drwal", bool darmo = false)
        {
            if (WolneChaty(gracz) <= 0)
            {
                return "  Brak wolnej chaty. Zbuduj chatę w rozbudowie obozu.";
            }
            if (!darmo && gracz.Zloto < CenaOsadnika)
            {
                return $"  Osadnik chce {CenaOsadnika} złota za przeprowadzkę.";
            }
            var zajete = new HashSet<string>(Osadnicy(gracz).Select(o => o.Imie));
            List<string> pula = Imiona.Where(n => !zajete.Contains(n)).ToList();
            if (pula.Count == 0)
            {
                pula = Imiona.ToList();
            }
            string imie = rng.Wybierz(pula);
            if (!darmo)
            {
                gracz.Zloto -= CenaOsadnika;
            }
            var nowy = Zapewnij(new Osadnik { Imie = imie, Zajecie = "bezczynny", Morale = 60.0 }, rng);
            Osadnicy(gracz).Add(nowy);
            string komunikat = UstawZajecie(gracz, nowy, zajecie);
            if (!komunikat.Contains("zostaje"))
            {
                nowy.Zajecie = "drwal";
            }
            gracz.Statystyki.Dodaj("zatrudnieni_osadnicy", 1);
            OpisZajecia info = Zajecie(nowy.Zajecie);
            string koszt = darmo ? "za darmo" : $"-{CenaOsadnika} złota";
            return $"  {info.Ikona}  {imie} ({nowy.Cecha}: {Cecha(nowy.Cecha).Opis}) wprowadza się"
                   + $" i zostaje: {info.Nazwa}.  Umie: {Kompetencje.OpisKrotki(nowy, 2)}.  ({koszt})";
        }

        /// <summary>Natychmiast wznosi chatę — niski poziom API (gra używa <see cref="Budowa"/>).</summary>
        public static string ZbudujChate(Gracz gracz)
        {
            if (gracz.Chaty >= MaxChaty)
            {
                return $"  Osada nie pomieści więcej chat (maks. {MaxChaty}).";
            }
            if (!Oboz.MozeZaplacic(gracz, KosztChaty))
            {
                return $"  Brakuje materiałów na chatę. Potrzeba: {Oboz.FormatKosztu(KosztChaty)}.";
            }
            Oboz.PobierzKoszt(gracz, KosztChaty);
            gracz.Chaty += 1;
            gracz.Statystyki["zbudowane_chaty"] = gracz.Chaty;
            return $"  🛖  Wznosisz chatę ({gracz.Chaty}/{MaxChaty}). Ktoś może się tu wprowadzić.";
        }

        public static double SrednieMorale(Gracz gracz)
        {
            List<Osadnik> l = Osadnicy(gracz);
            return l.Count > 0 ? l.Sum(o => o.Morale) / l.Count : 0.0;
        }

        public static string IkonaMorale(double m)
        {
            return m >= 75 ? "😄" : m >= 55 ? "🙂" : m >= 35 ? "😐" : "😠";
        }

        private static double FlagaD(Gracz g, string klucz)
        {
            return g.Flagi.TryGetValue(klucz, out object v) && v != null ? Convert.ToDouble(v) : 0.0;
        }

        private static int FlagaI(Gracz g, string klucz)
        {
            return (int)FlagaD(g, klucz);
        }

        private static void Plus(Dictionary<string, int> bilans, string klucz, int ile)
        {
            if (ile != 0)
            {
                bilans[klucz] = bilans.Wez(klucz) + ile;
            }
        }

        /// <summary>Bilans wczorajszego dnia (surowce i złoto) — do linii „Wczoraj”.</summary>
        public static Dictionary<string, int> BilansDnia(Gracz g)
        {
            return g.Flagi.TryGetValue("bilans_dnia", out object v) && v is Dictionary<string, int> d
                ? d : new Dictionary<string, int>();
        }

        /// <summary>Jednorazowe wydarzenie (najazd, święto, wyrok) — wygasa z dnia na dzień.</summary>
        public static void ZmienMorale(Gracz gracz, double ile)
        {
            gracz.Flagi["morale_wydarzenia"] = FlagaD(gracz, "morale_wydarzenia") + ile;
        }

        private static bool Przyswojona(Gracz g, string klucz)
        {
            return g.Mysli.TryGetValue(klucz, out Dictionary<string, object> w)
                   && w.TryGetValue("stan", out object s) && (s as string) == "przyswojona";
        }

        // ------------------------------------------------------------------ //
        //  Dzień osady                                                        //
        // ------------------------------------------------------------------ //

        private static void Produkcja(Gracz gracz, Losowanie rng, Pora pora, Dictionary<string, int> bilans,
                                      List<Osadnik> lista, List<string> kronika, List<string> pilne)
        {
            Dictionary<string, int> mag = gracz.Surowce;
            int narzedzia = gracz.Narzedzia;
            int wolneNarzedzia = narzedzia;
            var awanse = new List<string>();
            foreach (Osadnik o in lista)
            {
                string z = o.Zajecie;
                if (o.Chory > 0 || z == "bezczynny" || z == "budowniczy" || z == "badacz")
                {
                    continue;
                }
                double m = MnoznikPracy(gracz, o);
                if (wolneNarzedzia > 0 && z != "handlarz" && z != "straznik")
                {
                    m *= 1.25;
                    wolneNarzedzia -= 1;
                }
                switch (z)
                {
                    case "drwal": Plus(bilans, "drewno", Zaokraglij(2 * m, rng)); break;
                    case "kamieniarz": Plus(bilans, "kamien", Zaokraglij(2 * m, rng)); break;
                    case "zielarz": Plus(bilans, "ziola", Zaokraglij(2 * m * pora.Zbiory, rng)); break;
                    case "mysliwy":
                        Plus(bilans, "zywnosc", Zaokraglij(2 * m * Math.Max(0.5, pora.Zbiory), rng));
                        if (rng.Losowa() < 0.4)
                        {
                            Plus(bilans, "skora", 1);
                        }
                        break;
                    case "rolnik": Plus(bilans, "zywnosc", Zaokraglij(3 * m * pora.Rolnictwo, rng)); break;
                    case "gornik": Plus(bilans, "ruda", Zaokraglij(1.3 * m, rng)); break;
                    case "tracz":
                    {
                        int partie = Math.Min(Zaokraglij(m, rng),
                                              Atrybuty.PodzielWDol(mag.Wez("drewno") + bilans.Wez("drewno"), 2));
                        Plus(bilans, "drewno", -2 * partie);
                        Plus(bilans, "deski", partie);
                        break;
                    }
                    case "hutnik":
                    {
                        int ruda = mag.Wez("ruda") + bilans.Wez("ruda");
                        int drewno = mag.Wez("drewno") + bilans.Wez("drewno");
                        int partie = Math.Min(Math.Min(Zaokraglij(m, rng), Atrybuty.PodzielWDol(ruda, 2)), drewno);
                        Plus(bilans, "ruda", -2 * partie);
                        Plus(bilans, "drewno", -partie);
                        Plus(bilans, "zelazo", partie);
                        break;
                    }
                    case "handlarz":
                    {
                        double baza = gracz.PoziomBudynku("targ") > 0
                            ? 4 * (1 + 0.25 * (gracz.PoziomBudynku("targ") - 1)) : 2;
                        if (gracz.MaTalent("lichwiarz"))
                        {
                            baza *= 1.5;
                        }
                        Plus(bilans, "zloto", Zaokraglij(baza * m, rng));
                        break;
                    }
                    case "rzemieslnik":
                        // Zamówienia rzemieślników (game/rzemioslo.py) czekają na swój etap portu.
                        break;
                }
                awanse.AddRange(Kompetencje.Cwicz(o, z));
            }
            kronika.AddRange(awanse.Take(3));
            if (awanse.Count > 3)
            {
                kronika.Add($"  📈  (… i {awanse.Count - 3} innych awansów w umiejętnościach)");
            }

            // 1b. budowy i badania — ludzie przy rusztowaniach i w skryptorium.
            (List<string> kb, List<string> pb) = Budowa.DzienBudowy(gracz);
            kronika.AddRange(kb);
            pilne.AddRange(pb);
            (List<string> kr, List<string> pr) = Badania.DzienBadan(gracz);
            kronika.AddRange(kr);
            pilne.AddRange(pr);

            // Drużyna na zbiorach też pracuje co dzień — tabela rekrutów (game/rekruci.py) nie jest jeszcze w porcie.
            int zuzyte = 0;
            for (int i = 0; i < narzedzia - wolneNarzedzia; i++)
            {
                if (rng.Losowa() < 0.02)
                {
                    zuzyte++;
                }
            }
            if (zuzyte > 0)
            {
                gracz.Narzedzia = narzedzia - zuzyte;
                kronika.Add($"  🔧  Zużyło się {zuzyte} kompletów narzędzi (zostało {gracz.Narzedzia}).");
            }

            // Dziesięcina: zadowoleni osadnicy dokładają się do wspólnej kasy.
            int zadowoleni = lista.Count(o => o.Morale >= 40 && o.Chory <= 0);
            Plus(bilans, "zloto", Zaokraglij(0.5 * zadowoleni, rng));
            if (gracz.PoziomBudynku("targ") > 0)
            {
                double stragany = 2 * gracz.PoziomBudynku("targ") * (gracz.MaTalent("lichwiarz") ? 1.5 : 1.0);
                Plus(bilans, "zloto", Zaokraglij(stragany, rng));
            }
            foreach (KeyValuePair<string, int> para in bilans.ToList())
            {
                if (para.Key == "zloto")
                {
                    gracz.Zloto += para.Value;
                }
                else if (Oboz.Surowce.Any(s => s.Key == para.Key))
                {
                    mag[para.Key] = Math.Max(0, mag.Wez(para.Key) + para.Value);
                }
            }
        }

        /// <summary>Jeden dzień życia osady. Zwraca (wieści do kroniki, wieści pilne).</summary>
        public static (List<string> Kronika, List<string> Pilne) DzienOsady(Gracz gracz, Losowanie rng)
        {
            var kronika = new List<string>();
            var pilne = new List<string>();
            Dictionary<string, int> mag = gracz.Surowce;
            Pora pora = Kalendarz.PoraGracza(gracz);
            var bilans = new Dictionary<string, int>();
            gracz.Flagi["bilans_dnia"] = bilans;
            List<Osadnik> lista = Osadnicy(gracz);

            Produkcja(gracz, rng, pora, bilans, lista, kronika, pilne);

            // 2. utrzymanie budynków
            int utrzymanie = Oboz.UtrzymanieDzienne(gracz);
            bool zaniedbanie = false;
            if (utrzymanie > 0)
            {
                if (gracz.Zloto >= utrzymanie)
                {
                    gracz.Zloto -= utrzymanie;
                }
                else
                {
                    gracz.Zloto = 0;
                    zaniedbanie = true;
                    kronika.Add($"  💸  Zabrakło {utrzymanie} zł na utrzymanie budynków — ludzie narzekają.");
                }
                Plus(bilans, "zloto", -utrzymanie);
            }

            // 3. jedzenie
            int potrzeba = lista.Sum(o => Cecha(o.Cecha).Je) + gracz.Rekruci.Count;
            int zjedzone = Math.Min(potrzeba, mag.Wez("zywnosc"));
            mag["zywnosc"] = mag.Wez("zywnosc") - zjedzone;
            Plus(bilans, "zywnosc", -zjedzone);
            bool glodni = potrzeba > 0 && zjedzone < potrzeba;
            if (glodni)
            {
                int dni = FlagaI(gracz, "dni_glodu_osady") + 1;
                gracz.Flagi["dni_glodu_osady"] = dni;
                pilne.Add($"  🍽  GŁÓD W OSADZIE ({dni}. dzień): brakuje {potrzeba - zjedzone} racji! Morale spada.");
            }
            else
            {
                gracz.Flagi["dni_glodu_osady"] = 0;
            }

            // 4. opał zimą
            bool zimno = false;
            if (pora.Opal && (lista.Count > 0 || gracz.WObozie))
            {
                int opal = 1 + (int)Math.Ceiling(lista.Count / 4.0);
                int spalone = Math.Min(opal, mag.Wez("drewno"));
                mag["drewno"] = mag.Wez("drewno") - spalone;
                Plus(bilans, "drewno", -spalone);
                if (spalone < opal)
                {
                    zimno = true;
                    gracz.Flagi["zimno"] = true;
                    pilne.Add("  ❄  Brak drewna na opał — w chatach mróz. Ludzie chorują, morale leci.");
                }
                else
                {
                    gracz.Flagi["zimno"] = false;
                }
            }
            else
            {
                gracz.Flagi["zimno"] = false;
            }

            // 5. psucie żywności
            double tempo = new[] { 0.03, 0.01, 0.005, 0.0 }[Math.Min(3, gracz.PoziomBudynku("spichlerz"))];
            if (Przyswojona(gracz, "zimowy_glod"))
            {
                tempo /= 2;
            }
            if (tempo != 0 && mag.Wez("zywnosc") > 0)
            {
                int zepsute = Zaokraglij(mag["zywnosc"] * tempo, rng);
                if (zepsute != 0)
                {
                    mag["zywnosc"] -= zepsute;
                    Plus(bilans, "zywnosc", -zepsute);
                    gracz.Flagi["zepsute_razem"] = FlagaI(gracz, "zepsute_razem") + zepsute;
                }
            }

            // 6. morale, choroby, odejścia
            kronika.AddRange(MoraleIZdrowie(gracz, rng, glodni, zimno, zaniedbanie, pilne));

            // 6b. relacje między osadnikami (game/wiezi.py) — czekają na swój etap portu.

            // 7. wieść niesie się sama
            if (gracz.MaTalent("legenda_osady") && WolneChaty(gracz) > 0 && rng.Losowa() < 0.04)
            {
                kronika.Add("  🌟  " + ZatrudnijOsadnika(gracz, rng, "drwal", true).Trim()
                            + " Przyciągnęła go twoja sława.");
            }
            return (kronika, pilne);
        }

        private static double CelMorale(Gracz gracz, Osadnik o, bool glodni, bool zimno, bool zaniedbanie)
        {
            double cel = 55.0;
            cel += glodni ? -25 : 8;
            if (zimno)
            {
                cel -= 15;
            }
            if (zaniedbanie)
            {
                cel -= 5;
            }
            cel += 6 * gracz.PoziomBudynku("tawerna");
            cel += gracz.Karma >= 5 ? 5 : gracz.Karma <= -5 ? -5 : 0;
            cel += Cecha(o.Cecha).Morale;
            if (gracz.MaTalent("charyzmatyczny_wodz"))
            {
                cel += 10;
            }
            cel += FlagaD(gracz, "morale_wydarzenia");
            // Premia z przyswojonych myśli (game/mysli.py) i z więzi (game/wiezi.py) — jeszcze nie w porcie.
            if (o.Chory > 0)
            {
                cel -= 10;
            }
            cel += Kompetencje.PremiaMorale(o);
            return Math.Max(0.0, Math.Min(100.0, cel));
        }

        private static List<string> MoraleIZdrowie(Gracz gracz, Losowanie rng, bool glodni, bool zimno,
                                                   bool zaniedbanie, List<string> pilne)
        {
            var msgs = new List<string>();
            List<Osadnik> lista = Osadnicy(gracz);
            int lecznica = gracz.PoziomBudynku("lecznica");
            int uzdrowiciele = IluWZawodzie(gracz, "uzdrowiciel");
            foreach (Osadnik o in lista.ToList())
            {
                double cel = CelMorale(gracz, o, glodni, zimno, zaniedbanie);
                o.Morale = Liczby.Zaokraglij(o.Morale + (cel - o.Morale) * 0.25, 1);
                if (o.Chory > 0)
                {
                    o.Chory -= 1 + (uzdrowiciele > 0 ? 1 : 0);
                    if ((glodni || zimno) && rng.Losowa() < 0.04)
                    {
                        // Tu będzie żałoba bliskich (game/wiezi.py, po_stracie) — wołana przed usunięciem.
                        lista.Remove(o);
                        gracz.Flagi["morale_wydarzenia"] = FlagaD(gracz, "morale_wydarzenia") - 8;
                        pilne.Add($"  ⚰  {o.Imie} umiera z choroby, głodu i zimna. Osada pogrąża się w żałobie.");
                        continue;
                    }
                    if (o.Chory <= 0)
                    {
                        o.Chory = 0;
                        msgs.Add($"  💚  {o.Imie} wraca do zdrowia.");
                    }
                }
                else
                {
                    double szansa = 0.006 * Cecha(o.Cecha).Choroby;
                    szansa *= (glodni ? 2 : 1) * (zimno ? 2 : 1) * (lecznica > 0 ? 0.5 : 1);
                    if (rng.Losowa() < szansa)
                    {
                        o.Chory = rng.Calkowita(3, 6);
                        msgs.Add($"  🤒  {o.Imie} zachorował(a) — nie pracuje przez kilka dni.");
                    }
                }
                if (o.Morale < 20 && rng.Losowa() < 0.10)
                {
                    lista.Remove(o);
                    pilne.Add($"  🚪  {o.Imie} ma dość (morale {Liczby.Format(o.Morale, 0)}) i odchodzi z osady.");
                }
            }
            // wydarzenia wygasają
            double mod = FlagaD(gracz, "morale_wydarzenia");
            if (mod != 0)
            {
                gracz.Flagi["morale_wydarzenia"] = Math.Abs(mod) > 0.5 ? Liczby.Zaokraglij(mod * 0.85, 1) : 0.0;
            }
            return msgs;
        }

        /// <summary>Ile żywności przybyło/ubyło wczoraj (dodatnie = nadwyżka).</summary>
        public static int BilansZywnosci(Gracz gracz)
        {
            return BilansDnia(gracz).Wez("zywnosc");
        }
    }
}
