using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>Opis biomu: nazwa, zdanie wprowadzające i nazwy budynków.</summary>
    public sealed class Biom
    {
        public string Nazwa;
        public string Opis;

        /// <summary>Typ punktu → nazwa, jaką nosi w tym biomie (np. „zajazd na rozstaju”).</summary>
        public Dictionary<string, string> Budynki = new Dictionary<string, string>();
    }

    /// <summary>
    /// Dane świata i upływ czasu.
    ///
    /// Wersja pythonowa prowadzi tu pełny cykl dnia: osada pracuje, je, pali
    /// opał, rośnie zagrożenie najazdem, ruszają karawany, dojrzewają myśli.
    /// Te systemy czekają na etap 2 — na razie przenosimy część dotyczącą
    /// bohatera (pora roku, jedzenie, gojenie ran), bo bez niej ruch po mapie
    /// nie miałby kosztu.
    /// </summary>
    public static class Swiat
    {
        public static readonly Dictionary<string, Biom> Biomy = new Dictionary<string, Biom>
        {
            {
                "równiny", new Biom
                {
                    Nazwa = "równiny",
                    Opis = "Falujące trawy i stary trakt ciągną się aż po horyzont.",
                    Budynki = new Dictionary<string, string>
                    {
                        { "karczma", "zajazd na rozstaju" },
                        { "jaskinia", "stara strażnica" },
                        { "kuźnia", "wiejska kuźnia" },
                    },
                }
            },
            {
                "ruiny", new Biom
                {
                    Nazwa = "ruiny",
                    Opis = "Pęknięte mury i kolumny przypominają o dawnym królestwie.",
                    Budynki = new Dictionary<string, string>
                    {
                        { "świątynia", "opuszczona biblioteka" },
                        { "jaskinia", "pęknięta wieża maga" },
                    },
                }
            },
            {
                "las", new Biom
                {
                    Nazwa = "las",
                    Opis = "Gęste korony drzew tłumią światło i każdy krok brzmi podejrzanie.",
                    Budynki = new Dictionary<string, string>
                    {
                        { "karczma", "drewniana chatka" },
                        { "jaskinia", "myśliwska wieża" },
                    },
                }
            },
            {
                "bagna", new Biom
                {
                    Nazwa = "bagna",
                    Opis = "Mgła snuje się nad cuchnącą wodą, a teren zdradliwie chlupocze.",
                    Budynki = new Dictionary<string, string>
                    {
                        { "karczma", "zapadła chata zielarki" },
                        { "świątynia", "pochylona kaplica" },
                    },
                }
            },
            {
                "wzgórza", new Biom
                {
                    Nazwa = "wzgórza",
                    Opis = "Ścieżka wspina się między skałami i daje szeroki widok na okolicę.",
                    Budynki = new Dictionary<string, string>
                    {
                        { "jaskinia", "kamienna strażnica" },
                        { "karczma", "górski posterunek" },
                        { "kuźnia", "kuźnia pod szczytem" },
                    },
                }
            },
            {
                "kanion", new Biom
                {
                    Nazwa = "kanion",
                    Opis = "Czerwone ściany wąwozu odbijają każdy dźwięk twoich kroków.",
                    Budynki = new Dictionary<string, string>
                    {
                        { "jaskinia", "wykuta brama kopalni" },
                        { "świątynia", "opuszczony magazyn kupców" },
                    },
                }
            },
        };

        public static Biom SzablonBiomu(string nazwa)
        {
            return Biomy.TryGetValue(nazwa ?? "", out Biom b) ? b : Biomy["równiny"];
        }

        /// <summary>Nazwa budynku stojącego na polu — w barwach biomu, jeśli ma własną.</summary>
        public static string NazwaBudynku(string biom, string punkt)
        {
            if (string.IsNullOrEmpty(punkt))
            {
                return "";
            }
            Biom szablon = SzablonBiomu(biom);
            return szablon.Budynki.TryGetValue(punkt, out string nazwa)
                ? nazwa
                : Mapa.OpisPunktu(punkt);
        }

        /// <summary>
        /// Budynek, który stoi na polu jako zwykła lokacja — albo null.
        ///
        /// Obóz, legowisko bossa, miasto i punkty mityczne mają własne wejścia
        /// i własne sceny, więc nie liczą się jako „budynek na polu”. To
        /// odpowiednik <c>_budynek_z_pola</c> z <c>game/world.py</c>, który
        /// dla tych punktów zwraca None.
        /// </summary>
        public static string BudynekNaPolu(string biom, string punkt)
        {
            if (string.IsNullOrEmpty(punkt) || punkt == "obóz" || punkt == "boss"
                || punkt == "miasto" || System.Array.IndexOf(Mapa.PunktyMityczne, punkt) >= 0)
            {
                return null;
            }
            return NazwaBudynku(biom, punkt);
        }

        /// <summary>
        /// Upływ dni dla bohatera. Zwraca wieści warte pokazania od razu.
        ///
        /// Pełny cykl osady dojdzie w etapie 2; tu mija czas, zmienia się pora
        /// roku i bohater ponosi koszt dnia (prowiant albo głód, gojenie ran).
        /// </summary>
        public static List<string> MinijDni(Gracz gracz, Losowanie rng, int dni = 1,
                                            bool odpoczynek = false)
        {
            var pilne = new List<string>();
            for (int i = 0; i < System.Math.Max(0, dni); i++)
            {
                string staraPora = Kalendarz.PoraGracza(gracz).Klucz;
                gracz.Czas += 1;
                Pora pora = Kalendarz.PoraGracza(gracz);
                if (pora.Klucz != staraPora)
                {
                    if (staraPora == "zima")
                    {
                        gracz.Statystyki.Dodaj("przetrwane_zimy", 1);
                    }
                    pilne.Add($"  {pora.Ikona}  NADCHODZI {pora.Nazwa.ToUpperInvariant()}. {pora.Opis}");
                    if (pora.Klucz == "jesien")
                    {
                        pilne.Add("  🍂  Za 30 dni zima: zgromadź żywność i drewno na opał.");
                    }
                }

                if (gracz.WObozie)
                {
                    string wiesc = Przetrwanie.ZjedzZMagazynu(gracz);
                    if (wiesc != null)
                    {
                        pilne.Add(wiesc);
                    }
                    pilne.AddRange(Przetrwanie.LeczRany(gracz, odpoczynek ? 2.0 : 1.0));
                }
                else
                {
                    pilne.AddRange(Przetrwanie.DzienWyprawy(gracz, rng));
                }

                if (!gracz.Zyje())
                {
                    break;
                }
            }
            return pilne;
        }
    }
}
