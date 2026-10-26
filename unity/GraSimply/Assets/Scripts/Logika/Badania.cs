using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Jeden projekt badań: koszt w punktach wiedzy i to, co po sobie zostawia.</summary>
    public sealed class Projekt
    {
        public string Nazwa;
        public int Era;
        public int Koszt;
        public string Opis;
        public string[] Wymaga = new string[0];

        /// <summary>Budynek → najwyższy poziom, jaki projekt pozwala postawić.</summary>
        public List<KeyValuePair<string, int>> Odblokowuje = new List<KeyValuePair<string, int>>();

        /// <summary>Nowy limit chat albo 0, gdy projekt go nie zmienia.</summary>
        public int Chaty;

        /// <summary>Epoka, w którą wchodzi osada po tym projekcie, albo 0.</summary>
        public int EraPo;

        /// <summary>Zajęcie → mnożnik wydajności (np. płodozmian: rolnik ×1,10).</summary>
        public List<KeyValuePair<string, double>> Bonus = new List<KeyValuePair<string, double>>();
    }

    /// <summary>Stan nauki w skryptorium: poznane projekty, bieżący, postęp i zapas punktów.</summary>
    public sealed class StanBadan
    {
        public List<string> Zrobione = new List<string>();
        public string Aktualny;
        public Dictionary<string, double> Postep = new Dictionary<string, double>();

        /// <summary>Punkty wypracowane bez projektu — nie giną w całości, trafiają do następnego.</summary>
        public double Zapas;
    }

    /// <summary>
    /// Badania: skryptorium, epoki i poznawanie nowych budowli — jak stół badawczy
    /// w RimWorldzie. Odpowiednik <c>game/badania.py</c>.
    ///
    /// Na starcie osada umie niewiele: dom, pola, palisadę i skryptorium. Resztę —
    /// tartak, kuźnię, targ, wyższe poziomy budynków i więcej chat — trzeba
    /// <b>poznać</b>. W skryptorium siedzą badacze (jeden na poziom budynku), każdy
    /// dzień pracy daje punkty wiedzy, a projekt kończy się, gdy zbierze się ich tyle,
    /// ile kosztuje. Trzy epoki: I Drewno i kamień, II Żelazo i rzemiosło, III Mistrzostwo.
    /// </summary>
    public static class Badania
    {
        public static readonly Dictionary<int, string> Ery = new Dictionary<int, string>
        {
            { 1, "Drewno i kamień" }, { 2, "Żelazo i rzemiosło" }, { 3, "Mistrzostwo" },
        };

        /// <summary>Co osada umie bez badań: budynek → najwyższy poziom, jaki można postawić.</summary>
        public static readonly Dictionary<string, int> StartPoziomy = new Dictionary<string, int>
        {
            { "dom", 1 }, { "farma", 1 }, { "palisada", 1 }, { "skryptorium", 1 },
        };

        public const int StartChaty = 4;
        public const double BazaPunktow = 5.0;          // punktów wiedzy dziennie od badacza o mnożniku 1,0
        public const double PremiaSkryptorium = 0.35;   // za każdy poziom skryptorium ponad pierwszy
        public const int PunktyBohatera = 4;            // dzień studiów bohatera (plus premia z inteligencji)
        public const double LimitZapasu = 20.0;

        /// <summary>Jedno pokrętło tempa nauki; wartości w tabeli są „bazowe”, tu mnożone.</summary>
        public const double SkalaKosztu = 1.4;

        private static KeyValuePair<string, int> O(string budynek, int poziom)
        {
            return new KeyValuePair<string, int>(budynek, poziom);
        }

        private static KeyValuePair<string, double> Z(string zajecie, double mnoznik)
        {
            return new KeyValuePair<string, double>(zajecie, mnoznik);
        }

        private static KeyValuePair<string, Projekt> P(
            string id, string nazwa, int era, int koszt, string opis, string[] wymaga = null,
            KeyValuePair<string, int>[] odblokowuje = null, int chaty = 0, int eraPo = 0,
            KeyValuePair<string, double>[] bonus = null)
        {
            return new KeyValuePair<string, Projekt>(id, new Projekt
            {
                Nazwa = nazwa, Era = era, Koszt = Liczby.ZaokragliDoInt(koszt * SkalaKosztu), Opis = opis,
                Wymaga = wymaga ?? new string[0],
                Odblokowuje = new List<KeyValuePair<string, int>>(odblokowuje ?? new KeyValuePair<string, int>[0]),
                Chaty = chaty, EraPo = eraPo,
                Bonus = new List<KeyValuePair<string, double>>(bonus ?? new KeyValuePair<string, double>[0]),
            });
        }

        private static string[] W(params string[] id)
        {
            return id;
        }

        private static KeyValuePair<string, int>[] Ods(params KeyValuePair<string, int>[] o)
        {
            return o;
        }

        private static KeyValuePair<string, double>[] Bon(params KeyValuePair<string, double>[] b)
        {
            return b;
        }

        /// <summary>Kolejność jak w Pythonie — od niej zależy numeracja w menu badań.</summary>
        public static readonly List<KeyValuePair<string, Projekt>> Projekty = new List<KeyValuePair<string, Projekt>>
        {
            // ---- Epoka I: drewno i kamień ----------------------------------------------
            P("obrobka_drewna", "Obróbka drewna", 1, 20, "Piły i kliny: deski z pni.",
              odblokowuje: Ods(O("tartak", 1)), bonus: Bon(Z("tracz", 1.10))),
            P("kamieniarstwo", "Kamieniarstwo", 1, 25, "Łupanie, ciosanie i układanie kamienia.",
              odblokowuje: Ods(O("spichlerz", 1)), bonus: Bon(Z("kamieniarz", 1.10))),
            P("plodozmian", "Płodozmian", 1, 25, "Pola odpoczywają na przemian — plony rosną.",
              odblokowuje: Ods(O("farma", 2)), bonus: Bon(Z("rolnik", 1.10))),
            P("handel", "Handel i miara", 1, 30, "Wagi, odważniki i umowy ustne zamieniają się w targ.",
              W("obrobka_drewna"), Ods(O("sklep", 1), O("targ", 1)), bonus: Bon(Z("handlarz", 1.10))),
            P("zielarstwo", "Zielarstwo", 1, 25, "Które liście leczą, a które trują.",
              odblokowuje: Ods(O("lecznica", 1)), bonus: Bon(Z("zielarz", 1.15))),
            P("wspolne_ognisko", "Wspólne ognisko", 1, 25, "Wieczory przy kuflu zbliżają ludzi.",
              W("obrobka_drewna"), Ods(O("tawerna", 1))),
            P("ostrokol", "Ostrokół", 1, 30, "Zaostrzone pale, wieża z bali i warta na szczycie.",
              W("obrobka_drewna", "kamieniarstwo"), Ods(O("palisada", 2), O("wieza", 1)),
              bonus: Bon(Z("straznik", 1.10))),
            P("zwoje", "Zwoje i woskówki", 1, 35, "Zapisana wiedza nie ginie z człowiekiem. Większe skryptorium.",
              odblokowuje: Ods(O("skryptorium", 2))),
            P("osadnictwo", "Osadnictwo", 1, 30, "Plan wsi: do ośmiu chat.", chaty: 8),
            P("wiek_zelaza", "Wiek żelaza", 1, 60, "Przełom: metal zastępuje kamień i kość.",
              W("obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje"), eraPo: 2),
            // ---- Epoka II: żelazo i rzemiosło ------------------------------------------
            P("wytop", "Wytop rudy", 2, 60, "Piec, miech i żar: z rudy robi się żelazo.",
              odblokowuje: Ods(O("huta", 1)), bonus: Bon(Z("hutnik", 1.10), Z("gornik", 1.10))),
            P("kowalstwo", "Kowalstwo", 2, 70, "Kuźnia, kowadło i młot — broń oraz narzędzia.",
              W("wytop"), Ods(O("kuznia", 1), O("huta", 2), O("tartak", 2))),
            P("rzemioslo", "Warsztaty rzemieślnicze", 2, 60, "Stół, imadło i miejsce na wszystko.",
              W("obrobka_drewna"), Ods(O("warsztat", 1)), bonus: Bon(Z("rzemieslnik", 1.10))),
            P("hodowla", "Hodowla koni", 2, 50, "Silne konie niosą ludzi i prowiant.",
              W("plodozmian", "handel"), Ods(O("stajnie", 1))),
            P("szlaki", "Szlaki handlowe", 2, 70, "Wytyczone trakty i umowy z sąsiadami.",
              W("handel"), Ods(O("karawanseraj", 1), O("targ", 2), O("sklep", 2))),
            P("murowanie", "Murowanie", 2, 70, "Wapno i zaprawa: ściany stoją dziesiątki lat.",
              W("kamieniarstwo"), Ods(O("dom", 2), O("spichlerz", 2), O("palisada", 3))),
            P("plug_zelazny", "Pług żelazny", 2, 70, "Głęboka orka, większy plon, mocniejsze konie.",
              W("kowalstwo"), Ods(O("farma", 3), O("stajnie", 2)), bonus: Bon(Z("rolnik", 1.15))),
            P("medycyna", "Medycyna", 2, 60, "Nastawianie kości i płukanie ran.",
              W("zielarstwo"), Ods(O("lecznica", 2))),
            P("archiwum", "Archiwum", 2, 80, "Katalogi, półki i klucz w ręku skryby. Największe skryptorium.",
              W("zwoje"), Ods(O("skryptorium", 3))),
            P("festyny", "Święta i festyny", 2, 90, "Dożynki i kiermasze — tawerna rośnie w rozmach.",
              W("wspolne_ognisko"), Ods(O("tawerna", 2))),
            P("wielki_trakt", "Wielki trakt", 2, 70, "Droga, którą przychodzą nowi: do dwunastu chat.",
              W("osadnictwo"), chaty: 12),
            P("wiek_mistrzow", "Wiek mistrzów", 2, 140, "Przełom: cechy, gildie i prawdziwe mistrzostwo.",
              W("kowalstwo", "murowanie", "archiwum"), eraPo: 3),
            // ---- Epoka III: mistrzostwo ------------------------------------------------
            P("alchemia", "Alchemia", 3, 110, "Retorty, tygle i cierpliwość: eliksiry.",
              W("medycyna"), Ods(O("laboratorium", 1))),
            P("fortyfikacje", "Fortyfikacje", 3, 120, "Blanki, machikuły i wysoka strażnica.",
              W("murowanie"), Ods(O("wieza", 2)), bonus: Bon(Z("straznik", 1.10))),
            P("spichlerze", "Spichlerze mistrzowskie", 3, 100, "Wentylowane, suche i bezpieczne przed gryzoniami.",
              W("murowanie"), Ods(O("spichlerz", 3))),
            P("gildie", "Gildie", 3, 120, "Cechy dbają o jakość i szkolą uczniów.",
              W("szlaki", "rzemioslo"), Ods(O("warsztat", 2), O("kuznia", 2), O("targ", 3), O("karawanseraj", 2))),
            P("wielka_architektura", "Wielka architektura", 3, 160, "Sklepienia, filary i dachy na wieki.",
              W("gildie", "fortyfikacje"),
              Ods(O("dom", 3), O("kuznia", 3), O("tartak", 3), O("huta", 3), O("laboratorium", 2), O("karawanseraj", 3))),
        };

        public static Projekt Opis(string id)
        {
            foreach (KeyValuePair<string, Projekt> para in Projekty)
            {
                if (para.Key == id)
                {
                    return para.Value;
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ //
        //  Stan                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Stan badań gracza (zawsze istnieje — konstruktor <see cref="Gracz"/> go zakłada).</summary>
        public static StanBadan Stan(Gracz gracz)
        {
            if (gracz.Badania == null)
            {
                gracz.Badania = new StanBadan();
            }
            return gracz.Badania;
        }

        /// <summary>Zapis sprzed badań: wszystko jest już poznane, żeby nic się nie zablokowało.</summary>
        public static void MigrujStaryZapis(Gracz gracz)
        {
            StanBadan b = Stan(gracz);
            b.Zrobione = Projekty.Select(p => p.Key).ToList();
            b.Aktualny = null;
            b.Postep = new Dictionary<string, double>();
            b.Zapas = 0.0;
        }

        /// <summary>Poznane projekty (bez nieznanych identyfikatorów).</summary>
        public static HashSet<string> Zrobione(Gracz gracz)
        {
            var wynik = new HashSet<string>();
            foreach (string id in Stan(gracz).Zrobione)
            {
                if (Opis(id) != null)
                {
                    wynik.Add(id);
                }
            }
            return wynik;
        }

        /// <summary>Bieżąca epoka: 1 plus największy przełom, który już się dokonał.</summary>
        public static int Era(Gracz gracz)
        {
            int wynik = 1;
            foreach (string id in Zrobione(gracz))
            {
                int po = Opis(id).EraPo;
                if (po > 0)
                {
                    wynik = Math.Max(wynik, po);
                }
            }
            return wynik;
        }

        private static int Poziom(IEnumerable<KeyValuePair<string, int>> lista, string klucz)
        {
            foreach (KeyValuePair<string, int> para in lista)
            {
                if (para.Key == klucz)
                {
                    return para.Value;
                }
            }
            return 0;
        }

        /// <summary>Najwyższy poziom budynku, który osada potrafi postawić (0 = jeszcze nie umie).</summary>
        public static int MaksPoziom(Gracz gracz, string klucz)
        {
            int najwyzszy = StartPoziomy.Wez(klucz);
            foreach (string id in Zrobione(gracz))
            {
                najwyzszy = Math.Max(najwyzszy, Poziom(Opis(id).Odblokowuje, klucz));
            }
            return najwyzszy;
        }

        public static int LimitChat(Gracz gracz)
        {
            int limit = StartChaty;
            foreach (string id in Zrobione(gracz))
            {
                if (Opis(id).Chaty > 0)
                {
                    limit = Math.Max(limit, Opis(id).Chaty);
                }
            }
            return limit;
        }

        /// <summary>Najtańszy projekt, który pozwala postawić budynek <paramref name="klucz"/> na danym poziomie.</summary>
        public static string ProjektDla(string klucz, int poziom)
        {
            string najlepszy = null;
            foreach (KeyValuePair<string, Projekt> para in Projekty)
            {
                if (Poziom(para.Value.Odblokowuje, klucz) < poziom)
                {
                    continue;
                }
                if (najlepszy == null || (para.Value.Era, para.Value.Koszt).CompareTo(
                        (Opis(najlepszy).Era, Opis(najlepszy).Koszt)) < 0)
                {
                    najlepszy = para.Key;
                }
            }
            return najlepszy;
        }

        /// <summary>Najbliższy niepoznany projekt podnoszący limit chat.</summary>
        public static string ProjektDlaChat(Gracz gracz)
        {
            string najlepszy = null;
            int limit = LimitChat(gracz);
            HashSet<string> poznane = Zrobione(gracz);
            foreach (KeyValuePair<string, Projekt> para in Projekty)
            {
                if (para.Value.Chaty <= 0 || para.Value.Chaty <= limit || poznane.Contains(para.Key))
                {
                    continue;
                }
                if (najlepszy == null || para.Value.Chaty < Opis(najlepszy).Chaty)
                {
                    najlepszy = para.Key;
                }
            }
            return najlepszy;
        }

        /// <summary>Iloczyn premii z poznanych projektów dla danego zajęcia (np. płodozmian dla rolnika).</summary>
        public static double BonusZajecia(Gracz gracz, string zajecie)
        {
            double m = 1.0;
            HashSet<string> poznane = Zrobione(gracz);
            foreach (KeyValuePair<string, Projekt> para in Projekty)
            {
                if (!poznane.Contains(para.Key))
                {
                    continue;
                }
                foreach (KeyValuePair<string, double> b in para.Value.Bonus)
                {
                    if (b.Key == zajecie)
                    {
                        m *= b.Value;
                    }
                }
            }
            return m;
        }

        /// <summary>Czemu projektu nie można teraz zacząć albo null, gdy można.</summary>
        public static string PowodBlokady(Gracz gracz, string id)
        {
            Projekt p = Opis(id);
            HashSet<string> poznane = Zrobione(gracz);
            if (poznane.Contains(id))
            {
                return "już poznane";
            }
            List<string> brakujace = p.Wymaga.Where(w => !poznane.Contains(w)).Select(w => Opis(w).Nazwa).ToList();
            if (brakujace.Count > 0)
            {
                return "najpierw: " + string.Join(", ", brakujace);
            }
            if (Era(gracz) < p.Era)
            {
                return $"wymaga epoki {p.Era} ({Ery[p.Era]})";
            }
            if (gracz.PoziomBudynku("skryptorium") < p.Era)
            {
                return $"wymaga skryptorium na poziomie {p.Era}";
            }
            return null;
        }

        public static List<string> Dostepne(Gracz gracz)
        {
            return Projekty.Where(p => PowodBlokady(gracz, p.Key) == null).Select(p => p.Key).ToList();
        }

        /// <summary>Co daje projekt: nowe budowle, poziomy, chaty, epoka i premie.</summary>
        public static string OpisEfektow(string id)
        {
            Projekt p = Opis(id);
            var czesci = new List<string>();
            foreach (KeyValuePair<string, int> o in p.Odblokowuje)
            {
                string nazwa = Oboz.OpisBudynkuKlucza(o.Key).Nazwa;
                czesci.Add(o.Value == 1 ? nazwa : $"{nazwa} do poz. {o.Value}");
            }
            if (p.Chaty > 0)
            {
                czesci.Add($"do {p.Chaty} chat");
            }
            if (p.EraPo > 0)
            {
                czesci.Add($"EPOKA {p.EraPo}: {Ery[p.EraPo]}");
            }
            foreach (KeyValuePair<string, double> b in p.Bonus)
            {
                czesci.Add($"{b.Key} +{Liczby.ZaokragliDoInt((b.Value - 1) * 100)}%");
            }
            return czesci.Count > 0 ? string.Join(", ", czesci) : "—";
        }

        // ------------------------------------------------------------------ //
        //  Praca badaczy                                                      //
        // ------------------------------------------------------------------ //

        public static List<Osadnik> Badacze(Gracz gracz)
        {
            return Osada.Osadnicy(gracz).Where(o => o.Zajecie == "badacz" && o.Chory <= 0).ToList();
        }

        public static double MnoznikSkryptorium(Gracz gracz)
        {
            int poziom = gracz.PoziomBudynku("skryptorium");
            return poziom <= 0 ? 0.0 : 1.0 + PremiaSkryptorium * (poziom - 1);
        }

        /// <summary>Ile punktów wiedzy osada wypracowuje dziennie (bez studiów bohatera).</summary>
        public static double PunktyDzienne(Gracz gracz)
        {
            double suma = 0.0;
            foreach (Osadnik o in Badacze(gracz))
            {
                suma += BazaPunktow * Osada.MnoznikPracy(gracz, o);
            }
            return suma * MnoznikSkryptorium(gracz);
        }

        public static int PunktyStudiowBohatera(Gracz gracz)
        {
            return Math.Max(1, PunktyBohatera + Atrybuty.Modyfikator(gracz, "inteligencja"));
        }

        public static double Postep(Gracz gracz, string id)
        {
            return Stan(gracz).Postep.Wez(id);
        }

        /// <summary>Szacunek dni do końca bieżącego projektu albo null, gdy badania stoją.</summary>
        public static int? DniDoKonca(Gracz gracz)
        {
            StanBadan b = Stan(gracz);
            if (string.IsNullOrEmpty(b.Aktualny))
            {
                return null;
            }
            double tempo = PunktyDzienne(gracz);
            if (tempo <= 0)
            {
                return null;
            }
            double zostalo = Opis(b.Aktualny).Koszt - Postep(gracz, b.Aktualny) - b.Zapas;
            return Math.Max(1, (int)Math.Ceiling(Math.Max(0.0, zostalo) / tempo));
        }

        public static string WybierzProjekt(Gracz gracz, string id)
        {
            if (Opis(id) == null)
            {
                return "  Nie ma takiego badania.";
            }
            string powod = PowodBlokady(gracz, id);
            if (powod != null)
            {
                return $"  Nie można teraz badać: {Opis(id).Nazwa} — {powod}.";
            }
            Stan(gracz).Aktualny = id;
            return $"  📜  Badacze zajmują się: {Opis(id).Nazwa}." + WchlonZapas(gracz);
        }

        /// <summary>Punkty wypracowane bez projektu trafiają do świeżo wybranego.</summary>
        private static string WchlonZapas(Gracz gracz)
        {
            StanBadan b = Stan(gracz);
            if (b.Zapas <= 0 || string.IsNullOrEmpty(b.Aktualny))
            {
                return "";
            }
            double ile = b.Zapas;
            b.Zapas = 0.0;
            string koniec = Dodaj(gracz, ile);
            return $"  (+{Liczby.Format(ile, 0)} pkt z zapasu)" + (koniec.Length > 0 ? " " + koniec : "");
        }

        /// <summary>Dolicza punkty do bieżącego projektu; zwraca komunikat, gdy się skończył.</summary>
        private static string Dodaj(Gracz gracz, double ile)
        {
            StanBadan b = Stan(gracz);
            string id = b.Aktualny;
            if (string.IsNullOrEmpty(id))
            {
                b.Zapas = Math.Min(LimitZapasu, b.Zapas + ile);
                return "";
            }
            b.Postep[id] = b.Postep.Wez(id) + ile;
            if (b.Postep[id] < Opis(id).Koszt)
            {
                return "";
            }
            double nadwyzka = b.Postep[id] - Opis(id).Koszt;
            b.Postep.Remove(id);
            b.Zrobione.Add(id);
            b.Aktualny = null;
            b.Zapas = Math.Min(LimitZapasu, b.Zapas + nadwyzka);
            gracz.Statystyki["badania"] = Zrobione(gracz).Count;
            Projekt p = Opis(id);
            string tekst = $"  📜  BADANIA UKOŃCZONE: {p.Nazwa}!  Odblokowano: {OpisEfektow(id)}.";
            if (p.EraPo > 0)
            {
                tekst += $"\n  🌅  Wchodzicie w epokę: {Ery[p.EraPo]}.";
            }
            return tekst;
        }

        /// <summary>Dzień pracy skryptorium. Zwraca (wieści do kroniki, wieści pilne).</summary>
        public static (List<string> Kronika, List<string> Pilne) DzienBadan(Gracz gracz)
        {
            var kronika = new List<string>();
            var pilne = new List<string>();
            double ile = PunktyDzienne(gracz);
            if (ile <= 0)
            {
                return (kronika, pilne);
            }
            foreach (Osadnik o in Badacze(gracz))
            {
                kronika.AddRange(Kompetencje.Cwicz(o, "badacz"));
            }
            string wiesc = Dodaj(gracz, ile);
            if (wiesc.Length > 0)
            {
                pilne.Add(wiesc);
            }
            return (kronika, pilne);
        }

        /// <summary>Bohater spędza dzień w skryptorium: punkty wiedzy do bieżącego projektu i mija dzień.</summary>
        public static string Studiuj(IKonsola konsola, Gracz gracz, Losowanie rng)
        {
            if (gracz.PoziomBudynku("skryptorium") <= 0)
            {
                return "  Najpierw potrzebne jest skryptorium.";
            }
            if (string.IsNullOrEmpty(Stan(gracz).Aktualny))
            {
                return "  Wybierz najpierw projekt badań.";
            }
            int ile = PunktyStudiowBohatera(gracz);
            string tekst = $"  📖  Spędzasz dzień nad zwojami: +{ile} pkt wiedzy.";
            string koniec = Dodaj(gracz, ile);
            if (koniec.Length > 0)
            {
                tekst += "\n" + koniec;
            }
            Osada.DodajCzas(konsola, gracz, rng, 1);
            return tekst;
        }

        /// <summary>Linia w menu obozu: co się bada albo co trzeba zrobić.</summary>
        public static string EtykietaMenu(Gracz gracz)
        {
            if (gracz.PoziomBudynku("skryptorium") <= 0)
            {
                return "Skryptorium (badania) — najpierw zbuduj je w [11]";
            }
            StanBadan b = Stan(gracz);
            if (!string.IsNullOrEmpty(b.Aktualny))
            {
                Projekt p = Opis(b.Aktualny);
                return $"Skryptorium — {p.Nazwa} {Liczby.Format(Postep(gracz, b.Aktualny), 0)}/{p.Koszt}";
            }
            return "Skryptorium — wybierz badania" + (Dostepne(gracz).Count > 0 ? "  ⭐" : "");
        }

        // ------------------------------------------------------------------ //
        //  Menu                                                               //
        // ------------------------------------------------------------------ //

        private static string Pasek(double czesc, int szer = 20)
        {
            int wypelnione = Math.Max(0, Math.Min(szer, Liczby.ZaokragliDoInt(czesc * szer)));
            return new string('█', wypelnione) + new string('░', szer - wypelnione);
        }

        /// <summary>Skryptorium: bieżący projekt, lista nauk, studia bohatera.</summary>
        public static void MenuBadan(IKonsola konsola, Gracz gracz, Losowanie rng)
        {
            while (true)
            {
                konsola.Wyczysc();
                Utils.Linia(konsola, '═');
                konsola.Pisz($"  SKRYPTORIUM  —  {Kalendarz.OpisDaty(gracz.Czas)}");
                Utils.Linia(konsola, '═');
                int poziom = gracz.PoziomBudynku("skryptorium");
                StanBadan b = Stan(gracz);
                if (poziom <= 0)
                {
                    konsola.Pisz("\n  Bez skryptorium nikt nie spisuje wiedzy. Zbuduj je w rozbudowie obozu [11].");
                    Utils.NacisnijEnter(konsola);
                    return;
                }
                int e = Era(gracz);
                int wolne = Osada.Miejsca(gracz, "badacz") ?? 0;
                konsola.Pisz($"\n  Epoka {e}: {Ery[e]}   ·   skryptorium poz. {poziom}   ·   "
                             + $"badaczy {Osada.IluWZawodzie(gracz, "badacz")}/{wolne}   ·   "
                             + $"wiedza: {Liczby.Format(PunktyDzienne(gracz), 1)} pkt/dzień");
                if (!string.IsNullOrEmpty(b.Aktualny))
                {
                    Projekt p = Opis(b.Aktualny);
                    double zrobione = Postep(gracz, b.Aktualny);
                    int? dni = DniDoKonca(gracz);
                    string eta = dni.HasValue ? $"~{dni.Value} dni" : "stoi (brak badacza — [16] w osadzie)";
                    konsola.Pisz($"\n  W TOKU: {p.Nazwa}  [{Pasek(zrobione / p.Koszt)}] "
                                 + $"{Liczby.Format(zrobione, 0)}/{p.Koszt}  {eta}");
                    konsola.Pisz($"          {p.Opis}");
                    konsola.Pisz($"          Da: {OpisEfektow(b.Aktualny)}");
                }
                else
                {
                    konsola.Pisz("\n  Badacze czekają na polecenie — wybierz projekt."
                                 + (b.Zapas != 0 ? $"  (zapas {Liczby.Format(b.Zapas, 0)} pkt)" : ""));
                }
                List<string> dost = Dostepne(gracz);
                konsola.Pisz("\n  DO POZNANIA:");
                if (dost.Count == 0)
                {
                    konsola.Pisz("   — nic nowego; rozbuduj skryptorium albo dokończ przełom epoki.");
                }
                for (int i = 0; i < dost.Count; i++)
                {
                    Projekt p = Opis(dost[i]);
                    string znak = dost[i] == b.Aktualny ? "▶" : " ";
                    konsola.Pisz($"  {znak}[{(i + 1).ToString().PadLeft(2)}] {Liczby.Dopelnij(p.Nazwa, 26)} "
                                 + $"{p.Koszt.ToString().PadLeft(4)} pkt — {OpisEfektow(dost[i])}");
                }
                List<string> zablokowane = Projekty.Select(p => p.Key)
                    .Where(id => { string r = PowodBlokady(gracz, id); return r != null && r != "już poznane"; })
                    .ToList();
                if (zablokowane.Count > 0)
                {
                    konsola.Pisz("\n  ZAMKNIĘTE (pierwsze trzy):");
                    foreach (string id in zablokowane.Take(3))
                    {
                        konsola.Pisz($"   🔒 {Opis(id).Nazwa} — {PowodBlokady(gracz, id)}");
                    }
                }
                konsola.Pisz($"\n  Poznane: {Zrobione(gracz).Count}/{Projekty.Count}");
                konsola.Pisz($"\n  [S] 📖 Studiuj sam (1 dzień, +{PunktyStudiowBohatera(gracz)} pkt)");
                konsola.Pisz("  [0] ↩ Wróć\n");
                string wybor = (konsola.Czytaj("  Twój wybór: ") ?? "").Trim().ToLowerInvariant();
                if (wybor == "0")
                {
                    return;
                }
                if (wybor == "s")
                {
                    konsola.Pisz(Studiuj(konsola, gracz, rng));
                    Utils.NacisnijEnter(konsola);
                    continue;
                }
                if (int.TryParse(wybor, out int nr) && nr >= 1 && nr <= dost.Count)
                {
                    konsola.Pisz(WybierzProjekt(gracz, dost[nr - 1]));
                    Utils.NacisnijEnter(konsola);
                    continue;
                }
                konsola.Pisz("  Nieprawidłowy wybór.");
                Utils.NacisnijEnter(konsola);
            }
        }
    }
}
