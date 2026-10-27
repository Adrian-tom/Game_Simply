using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Co stoi na jednym polu placu: rodzaj (budynek, chata, namiot, farma), klucz i poziom.</summary>
    public struct ElementPlacu
    {
        public string Rodzaj;
        public string Klucz;
        public int Poziom;

        public ElementPlacu(string rodzaj, string klucz, int poziom)
        {
            Rodzaj = rodzaj;
            Klucz = klucz;
            Poziom = poziom;
        }
    }

    /// <summary>
    /// Układ placu osady — odpowiednik rozkładu z <c>grafika/widok_osady.py</c>. Leży w logice,
    /// bo korzystają z niego i scena (rysowanie), i chodzenie bohatera po obozie:
    /// gdzie stoi który budynek, co zagradza drogę osadnikom, przy czym pracuje
    /// kto i gdzie śpi. Czysta logika, bez rysowania, więc da się ją sprawdzić
    /// bez obrazka.
    ///
    /// Plac to siatka 7×7: obwód to palisada, wnętrze 5×5 to plac. Współrzędne
    /// budynków leżą tylko we wnętrzu (1…5), żeby palisada miała własny obwód.
    /// </summary>
    public static class UkladPlacu
    {
        public const int Siatka = 7;

        /// <summary>
        /// Siatka ruchu jest większa od placu o dwa rzędy dziedzińca przed bramą.
        /// Na zastawionym placu (komplet budynków i chat) nie byłoby gdzie stanąć,
        /// a ludzie lądowaliby na jednym polu — nadmiar czeka więc przed bramą.
        /// </summary>
        public const int SiatkaRuchu = Siatka + 2;

        public static readonly (int X, int Y) Ognisko = (3, 3);

        /// <summary>Brama od frontu — jedyne przejście przez obwód.</summary>
        public static readonly (int X, int Y) Brama = (Siatka / 2, Siatka - 1);

        /// <summary>Pole, na którym stoi bohater — dom i ognisko zostają w środku obozu.</summary>
        public static readonly (int X, int Y) PoleBohatera = (3, 4);

        /// <summary>Gdzie stoi który budynek. Dom i ognisko — serce obozu.</summary>
        public static readonly Dictionary<string, (int X, int Y)> Miejsca = new Dictionary<string, (int, int)>
        {
            { "dom", (3, 2) },
            { "kuznia", (1, 1) },
            { "huta", (2, 1) },
            { "spichlerz", (4, 1) },
            { "karawanseraj", (5, 1) },
            { "tartak", (1, 2) },
            { "sklep", (5, 2) },
            { "warsztat", (1, 3) },
            { "stajnie", (5, 3) },
            { "laboratorium", (1, 4) },
            { "tawerna", (5, 4) },
            { "lecznica", (2, 5) },
            { "targ", (4, 5) },
            { "wieza", (1, 5) },
            { "farma", (3, 5) },
            { "skryptorium", (3, 1) },
        };

        /// <summary>Kolejność, w jakiej chaty zajmują wolne pola placu.</summary>
        public static readonly (int X, int Y)[] MiejscaChat =
        {
            (2, 2), (4, 2), (2, 3), (4, 3), (2, 4), (4, 4), (3, 4), (5, 5),
            (3, 1), (1, 1), (2, 1), (4, 1), (5, 2), (1, 2), (1, 3), (5, 3),
        };

        /// <summary>Gdzie pracuje kto. Brak budynku → osadnik wraca pod ognisko.</summary>
        public static readonly Dictionary<string, string> PrzyBudynku = new Dictionary<string, string>
        {
            { "rolnik", "farma" }, { "tracz", "tartak" }, { "hutnik", "huta" },
            { "rzemieslnik", "warsztat" }, { "uzdrowiciel", "lecznica" }, { "handlarz", "targ" },
            { "badacz", "skryptorium" },
        };

        /// <summary>Zajęcia bez budynku pracują na skraju placu — tam, gdzie las, skały i brama.</summary>
        public static readonly Dictionary<string, (int X, int Y)> PrzyKrawedzi = new Dictionary<string, (int, int)>
        {
            { "drwal", (1, 1) }, { "kamieniarz", (5, 1) }, { "gornik", (5, 1) },
            { "zielarz", (1, 5) }, { "mysliwy", (3, 5) }, { "straznik", (3, 5) },
        };

        /// <summary>Budynek → rodzaj poświaty po ciemku.</summary>
        public static readonly Dictionary<string, string> Blask = new Dictionary<string, string>
        {
            { "kuznia", "kuznia" }, { "huta", "kuznia" }, { "laboratorium", "magia" },
            { "tawerna", "okno" }, { "dom", "okno" }, { "lecznica", "latarnia" }, { "skryptorium", "okno" },
        };

        /// <summary>Co gdzie stoi: pole → element. Budynek ma pierwszeństwo przed chatą.</summary>
        public static Dictionary<(int X, int Y), ElementPlacu> Rozklad(StanOsady osada)
        {
            return Rozklad(osada, out _);
        }

        /// <summary>
        /// Jak <see cref="Rozklad(StanOsady)"/>, a do tego rusztowania stawiane na już stojących
        /// budynkach (rozbudowa): pole → postęp 0–1. Nowy budynek to wpis „budowa” w planie,
        /// chata w budowie zajmuje następny wolny kafel (postęp w procentach w polu Poziom).
        /// </summary>
        public static Dictionary<(int X, int Y), ElementPlacu> Rozklad(
            StanOsady osada, out Dictionary<(int X, int Y), double> nakladki)
        {
            nakladki = new Dictionary<(int X, int Y), double>();
            var plan = new Dictionary<(int X, int Y), ElementPlacu>();
            foreach (KeyValuePair<string, (int X, int Y)> miejsce in Miejsca)
            {
                int poziom = osada.Budynki.TryGetValue(miejsce.Key, out int p) ? p : 0;
                if (poziom > 0)
                {
                    plan[miejsce.Value] = new ElementPlacu(miejsce.Key == "farma" ? "farma" : "budynek",
                                                           miejsce.Key, poziom);
                }
            }

            // Zanim stanie dom, w obozie śpi się pod płótnem.
            if (!plan.ContainsKey(Miejsca["dom"]))
            {
                plan[Miejsca["dom"]] = new ElementPlacu("namiot", "namiot", 1);
            }

            var wolne = MiejscaChat.Where(p => !plan.ContainsKey(p)).ToList();
            for (int i = 0; i < Math.Min(osada.Chaty, wolne.Count); i++)
            {
                plan[wolne[i]] = new ElementPlacu("chata", "chata", i);
            }

            // Budowy w toku. Palisada nie ma pola — jej postęp widać w menu.
            int pierwszaWolna = Math.Min(osada.Chaty, wolne.Count);
            foreach ((string klucz, int _, double postep) in osada.Rusztowania)
            {
                int procent = (int)(postep * 100);
                if (klucz == "chata")
                {
                    if (pierwszaWolna < wolne.Count)
                    {
                        plan[wolne[pierwszaWolna]] = new ElementPlacu("budowa", "chata", procent);
                        pierwszaWolna++;
                    }
                    continue;
                }
                if (!Miejsca.TryGetValue(klucz, out (int X, int Y) pole))
                {
                    continue;
                }
                if (plan.TryGetValue(pole, out ElementPlacu stoi) && stoi.Rodzaj != "namiot")
                {
                    nakladki[pole] = postep;
                }
                else
                {
                    plan[pole] = new ElementPlacu("budowa", klucz, procent);
                }
            }
            return plan;
        }

        /// <summary>Pola, przez które nie da się przejść: budynki, ognisko, bohater i obwód placu.</summary>
        public static HashSet<(int X, int Y)> Zablokowane(Dictionary<(int X, int Y), ElementPlacu> plan,
                                                          (int X, int Y)? bohater = null)
        {
            var zablokowane = new HashSet<(int X, int Y)>();
            foreach (KeyValuePair<(int X, int Y), ElementPlacu> wpis in plan)
            {
                // Pole uprawne jest płaskie — po zagonach się chodzi.
                if (wpis.Value.Rodzaj != "farma")
                {
                    zablokowane.Add(wpis.Key);
                }
            }
            zablokowane.Add(Ognisko);
            zablokowane.Add(bohater ?? PoleBohatera);
            for (int i = 0; i < Siatka; i++)
            {
                zablokowane.Add((i, 0));
                zablokowane.Add((i, Siatka - 1));
                zablokowane.Add((0, i));
                zablokowane.Add((Siatka - 1, i));
            }
            zablokowane.Remove(Brama);
            // Dziedziniec to dwa rzędy pod bramą; reszta siatki poza placem jest zamknięta.
            for (int y = 0; y < SiatkaRuchu; y++)
            {
                for (int x = Siatka; x < SiatkaRuchu; x++)
                {
                    zablokowane.Add((x, y));
                }
            }
            return zablokowane;
        }

        /// <summary>Pole, przy którym kręci się osadnik o danym zajęciu.</summary>
        public static (int X, int Y) Stanowisko(string zajecie, Dictionary<(int X, int Y), ElementPlacu> plan,
                                                Dictionary<(int X, int Y), double> nakladki = null)
        {
            zajecie = string.IsNullOrEmpty(zajecie) ? "bezczynny" : zajecie;
            if (zajecie == "budowniczy")
            {
                // Przy pierwszym szkielecie (kolejność po polu), potem przy rozbudowie; bez budowy — pod ogniskiem.
                foreach (KeyValuePair<(int X, int Y), ElementPlacu> wpis in plan.OrderBy(w => w.Key.X).ThenBy(w => w.Key.Y))
                {
                    if (wpis.Value.Rodzaj == "budowa")
                    {
                        return wpis.Key;
                    }
                }
                if (nakladki != null && nakladki.Count > 0)
                {
                    return nakladki.Keys.OrderBy(p => p.X).ThenBy(p => p.Y).First();
                }
                return Ognisko;
            }
            if (PrzyBudynku.TryGetValue(zajecie, out string budynek))
            {
                foreach (KeyValuePair<(int X, int Y), ElementPlacu> wpis in plan)
                {
                    if (wpis.Value.Klucz == budynek)
                    {
                        return wpis.Key;
                    }
                }
            }
            if (PrzyKrawedzi.TryGetValue(zajecie, out (int X, int Y) krawedz))
            {
                return krawedz;
            }
            return Ognisko;
        }

        /// <summary>Pola, w których osadnicy śpią: chaty, a bez nich dom lub namiot.</summary>
        public static List<(int X, int Y)> Domy(Dictionary<(int X, int Y), ElementPlacu> plan)
        {
            var chaty = plan.Where(w => w.Value.Rodzaj == "chata").Select(w => w.Key)
                            .OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            if (chaty.Count > 0)
            {
                return chaty;
            }
            var podDachem = plan.Where(w => w.Value.Klucz == "dom" || w.Value.Klucz == "namiot")
                                .Select(w => w.Key).OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            return podDachem.Count > 0 ? podDachem : new List<(int X, int Y)> { Ognisko };
        }

        /// <summary>Pola obwodu zajęte przez palisadę (bez bramy) albo pusta lista.</summary>
        public static List<(int X, int Y)> PolaPalisady(int poziomPalisady)
        {
            var pola = new List<(int X, int Y)>();
            if (poziomPalisady <= 0)
            {
                return pola;
            }
            for (int y = 0; y < Siatka; y++)
            {
                for (int x = 0; x < Siatka; x++)
                {
                    bool obwod = x == 0 || y == 0 || x == Siatka - 1 || y == Siatka - 1;
                    if (obwod && (x, y) != Brama)
                    {
                        pola.Add((x, y));
                    }
                }
            }
            return pola;
        }
    }
}
