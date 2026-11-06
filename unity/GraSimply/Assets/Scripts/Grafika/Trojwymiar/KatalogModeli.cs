using System;
using System.Collections.Generic;
using GraSimply.Logika.Wolny;

namespace GraSimply.Grafika.Trojwymiar
{
    /// <summary>Który model z <c>Resources/Modele</c> postawić i jak wysoki ma być (w jednostkach świata).</summary>
    public struct WpisModelu
    {
        public string Sciezka;
        public float Wysokosc;
        public bool LosowyObrot;

        public WpisModelu(string sciezka, float wysokosc, bool losowyObrot = false)
        {
            Sciezka = sciezka;
            Wysokosc = wysokosc;
            LosowyObrot = losowyObrot;
        }
    }

    /// <summary>Element budynku złożonego z części Quaterniusa (współrzędne autora, 1 jednostka = 1 m).</summary>
    public struct CzescBudynku
    {
        public string Sciezka;
        public float X, Y, Z, Obrot;

        public CzescBudynku(string sciezka, float x, float y, float z, float obrot = 0f)
        {
            Sciezka = sciezka;
            X = x;
            Y = y;
            Z = z;
            Obrot = obrot;
        }
    }

    /// <summary>
    /// Przypisanie rzeczy z gry do modeli: drzewa, budowle, postacie, punkty mapy i kolory gruntu.
    /// Bez Unity, więc testy sprawdzają, czy każdy wpis wskazuje plik, który naprawdę jest w projekcie.
    /// </summary>
    public static class KatalogModeli
    {
        public const string Chata = "zloz:chata";
        public const string Dwor = "zloz:dwor";

        private static readonly string[] Drzewa =
        {
            "Natura/island_tree_01_0", "Natura/island_tree_01_1", "Natura/island_tree_02_0", "Natura/island_tree_02_1",
            "Natura/tree_small_02_0", "Natura/tree_small_02_1", "Natura/fir_sapling_medium_0", "Natura/fir_sapling_medium_1",
            "Natura/fir_sapling_medium_2",
        };

        private static readonly string[] Skaly =
        {
            "Natura/rock_moss_set_01_0", "Natura/rock_moss_set_01_1", "Natura/rock_moss_set_01_2", "Natura/rock_moss_set_02_0",
            "Natura/rock_moss_set_02_1", "Natura/rock_moss_set_02_2", "Natura/boulder_01_0", "Natura/boulder_01_1",
            "Natura/rock_07_0", "Natura/rock_07_1",
        };

        private static readonly string[] Krzaki =
        {
            "Natura/shrub_01_0", "Natura/shrub_01_1", "Natura/shrub_04_0", "Natura/shrub_04_1", "Natura/shrub_04_2",
            "Natura/fern_02_0", "Natura/fern_02_1", "Natura/fern_02_2",
        };

        private static readonly string[] Ziola =
        {
            "Natura/nettle_plant_0", "Natura/nettle_plant_1", "Natura/nettle_plant_2", "Natura/dandelion_01_0",
            "Natura/shrub_03_0", "Natura/shrub_03_1", "Natura/shrub_03_2",
        };

        private static readonly string[] Suche =
        {
            "Natura/fir_sapling_medium_0", "Natura/fir_sapling_medium_1", "Natura/fir_sapling_medium_2", "Natura/tree_small_02_0",
        };

        private static readonly string[] Trawy =
        {
            "Natura/grass_medium_01_0", "Natura/grass_medium_01_1", "Natura/grass_medium_01_2", "Natura/grass_medium_01_3",
            "Natura/grass_medium_01_0", "Natura/grass_medium_01_1", "Natura/grass_medium_01_2", "Natura/grass_medium_01_3",
            "Natura/grass_medium_01_0", "Natura/grass_medium_01_1", "Natura/grass_medium_01_2", "Natura/grass_medium_01_3",
        };

        /// <summary>Stabilny „los” z pozycji: ten sam kafel zawsze dostaje ten sam model.</summary>
        public static int Mieszaj(int x, int y)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663;
                h ^= h >> 13;
                h *= 1274126177;
                return (h ^ (h >> 16)) & 0x7FFFFFFF;
            }
        }

        private static string Z(string[] lista, int x, int y)
        {
            return lista[Mieszaj(x, y) % lista.Length];
        }

        /// <summary>Obiekt kafla w otwartym świecie. Null, gdy nie ma czego stawiać (woda jest kolorem gruntu).</summary>
        public static WpisModelu? DlaObiektu(Obiekt o, int x, int y, string biom = "")
        {
            switch (o)
            {
                case Obiekt.Drzewo:
                    bool suche = biom == "bagna" || biom == "ruiny";
                    return new WpisModelu(Z(suche ? Suche : Drzewa, x, y), 2.7f + Mieszaj(y, x) % 8 * 0.1f);
                case Obiekt.Skala:
                    return new WpisModelu(Z(Skaly, x, y), 0.72f);
                case Obiekt.Krzak:
                    return new WpisModelu(Z(Krzaki, x, y), 0.72f);
                case Obiekt.Ziolo:
                    return new WpisModelu(Z(Ziola, x, y), 0.5f);
                case Obiekt.Sciana:
                    return new WpisModelu("Budynki/mountain_A", 2.2f, true);
                default:
                    return null;
            }
        }

        /// <summary>Budowla gracza w otwartym świecie.</summary>
        public static WpisModelu DlaBudowli(string klucz)
        {
            switch (klucz)
            {
                case "spiwor": return new WpisModelu("Oboz/bedroll", 0.22f);
                case "ognisko": return new WpisModelu("Oboz/campfire-pit", 0.3f);
                case "namiot": return new WpisModelu("Oboz/tent-canvas", 1.0f);
                case "chata": return new WpisModelu(Chata, 1.5f);
                case "palisada": return new WpisModelu("Oboz/fence-fortified", 1.05f);
                case "brama": return new WpisModelu("Oboz/fence-doorway", 1.05f);
                case "wieza": return new WpisModelu("Budynki/building_tower_A_blue", 2.4f);
                case "warsztat": return new WpisModelu("Budynki/building_home_B_blue", 1.9f);
                case "tartak": return new WpisModelu("Budynki/building_lumbermill_blue", 1.9f);
                case "kuznia": return new WpisModelu("Budynki/building_blacksmith_blue", 2.0f);
                case "farma": return new WpisModelu("Budynki/building_windmill_blue", 2.3f);
                case "spichlerz": return new WpisModelu("Budynki/building_home_A_blue", 1.9f);
                case "tawerna": return new WpisModelu("Budynki/building_tavern_blue", 2.0f);
                case "lecznica": return new WpisModelu("Budynki/building_church_blue", 2.1f);
                case "targ": return new WpisModelu("Budynki/building_market_blue", 1.9f);
                default: throw new ArgumentException("Nieznana budowla: " + klucz);
            }
        }

        /// <summary>Plac budowy: najpierw deski na ziemi, potem rusztowanie, a na końcu budynek jeszcze niżej niż gotowy.</summary>
        public static WpisModelu DlaEtapu(string klucz, int etap, int etapow)
        {
            WpisModelu gotowy = DlaBudowli(klucz);
            if (etap >= etapow - 1 && etapow > 1)
            {
                return new WpisModelu(gotowy.Sciezka, gotowy.Wysokosc * 0.8f);
            }
            return etap == 0 ? new WpisModelu("Oboz/structure-floor", 0.25f) : new WpisModelu("Oboz/structure", 1.0f);
        }

        /// <summary>Budynek na placu osady.</summary>
        public static WpisModelu DlaBudynku(string klucz, int poziom)
        {
            float w = 1.7f + Math.Min(poziom, 3) * 0.2f;
            switch (klucz)
            {
                case "dom": return new WpisModelu(Dwor, 2.5f);
                case "kuznia": return new WpisModelu("Budynki/building_blacksmith_blue", w);
                case "huta": return new WpisModelu("Budynki/building_mine_blue", w);
                case "spichlerz": return new WpisModelu("Budynki/building_windmill_blue", w + 0.5f);
                case "karawanseraj": return new WpisModelu("Budynki/building_market_blue", w);
                case "tartak": return new WpisModelu("Budynki/building_lumbermill_blue", w);
                case "sklep": return new WpisModelu("Budynki/building_home_B_blue", w);
                case "warsztat": return new WpisModelu("Budynki/building_home_A_blue", w);
                case "stajnie": return new WpisModelu("Budynki/building_barracks_blue", w);
                case "laboratorium": return new WpisModelu("Budynki/building_tower_A_blue", w + 0.6f);
                case "tawerna": return new WpisModelu("Budynki/building_tavern_blue", w);
                case "lecznica": return new WpisModelu("Budynki/building_well_blue", w - 0.4f);
                case "targ": return new WpisModelu("Budynki/building_market_blue", w);
                case "wieza": return new WpisModelu("Budynki/building_tower_B_blue", w + 1.2f);
                case "skryptorium": return new WpisModelu("Budynki/building_church_blue", w + 0.4f);
                case "farma": return new WpisModelu("Budynki/resource_lumber", 0.4f);
                default: return new WpisModelu("Budynki/building_home_A_blue", w);
            }
        }

        /// <summary>Punkt orientacyjny na mapie regionu.</summary>
        public static WpisModelu DlaPunktu(string punkt)
        {
            switch (punkt)
            {
                case "obóz": return new WpisModelu("Oboz/tent-canvas", 1.8f);
                case "karczma": return new WpisModelu("Budynki/building_tavern_blue", 2.6f);
                case "kuźnia": return new WpisModelu("Budynki/building_blacksmith_blue", 2.6f);
                case "świątynia": return new WpisModelu("Budynki/building_church_blue", 3.2f);
                case "miasto": return new WpisModelu("Budynki/building_castle_blue", 4.6f);
                case "jaskinia": return new WpisModelu("Budynki/building_mine_blue", 2.6f);
                case "boss": return new WpisModelu("Postacie/Skeleton_Warrior", 2.2f);
                case "portal": return new WpisModelu("Budynki/building_tower_A_blue", 3.6f);
                case "leze_smoka": return new WpisModelu("Budynki/mountain_B_grass_trees", 3.4f);
                case "latajaca_wyspa": return new WpisModelu("Budynki/hills_A_trees", 2.6f);
                default: return new WpisModelu("Budynki/weaponrack", 1.1f);
            }
        }

        /// <summary>Co rośnie na polu mapy regionu: lista (model, przesunięcie w polu 0–1).</summary>
        public static List<(WpisModelu Model, float U, float V)> Roslinnosc(string biom, int x, int y)
        {
            var wynik = new List<(WpisModelu, float, float)>();
            int ile, h = Mieszaj(x, y);
            string[] lista;
            float wysokosc;
            switch (biom)
            {
                case "las": lista = Drzewa; ile = 7; wysokosc = 3.4f; break;
                case "bagna": lista = Suche; ile = 4; wysokosc = 3.0f; break;
                case "wzgórza": lista = new[] { "Budynki/hill_single_A", "Budynki/hill_single_B", "Budynki/hill_single_C" }; ile = 3; wysokosc = 1.1f; break;
                case "kanion": lista = Skaly; ile = 5; wysokosc = 1.3f; break;
                case "ruiny": lista = Skaly; ile = 3; wysokosc = 0.8f; break;
                default: lista = Krzaki; ile = 4; wysokosc = 0.8f; break;
            }
            for (int i = 0; i < ile; i++)
            {
                int m = Mieszaj(h + i * 31, i * 17 + x);
                string sciezka = lista[m % lista.Length];
                float u = 0.1f + (m >> 4) % 80 / 100f, v = 0.1f + (m >> 11) % 80 / 100f;
                bool model = !sciezka.StartsWith("Natura/");
                wynik.Add((new WpisModelu(sciezka, wysokosc * (0.85f + (m >> 18) % 30 / 100f), model), u, v));
            }
            return wynik;
        }

        /// <summary>Drobne kępki na polu danego biomu: obraz, miejsce w polu 0–1 i wysokość. Skała i pustynia są gołe.</summary>
        public static List<(string Sciezka, float U, float V, float Wysokosc)> Kepki(string biom, int x, int y)
        {
            var wynik = new List<(string, float, float, float)>();
            int ile;
            switch (biom)
            {
                case "las": ile = 3; break;
                case "bagna": ile = 2; break;
                case "wzgórza": ile = 1; break;
                case "ruiny": ile = 1; break;
                case "równiny": ile = 3; break;
                default: return wynik;
            }
            for (int i = 0; i < ile; i++)
            {
                int m = Mieszaj(x * 3 + i * 41 + 7, y * 5 - i * 23);
                wynik.Add((Trawy[m % Trawy.Length], (m >> 5) % 100 / 100f, (m >> 12) % 100 / 100f, 0.24f + (m >> 19) % 20 / 100f));
            }
            return wynik;
        }

        public static WpisModelu DlaWroga(TypWroga t)
        {
            switch (t)
            {
                case TypWroga.Wojownik: return new WpisModelu("Postacie/Skeleton_Warrior", 1.5f);
                case TypWroga.Mag: return new WpisModelu("Postacie/Skeleton_Mage", 1.25f);
                case TypWroga.Lotr: return new WpisModelu("Postacie/Skeleton_Rogue", 1.1f);
                default: return new WpisModelu("Postacie/Skeleton_Minion", 1.05f);
            }
        }

        public static readonly WpisModelu PorzuconyPlecak = new WpisModelu("Budynki/sack", 0.42f);

        public static string PostacKlasy(string klasa)
        {
            switch (klasa)
            {
                case "Mag": return "Postacie/Mage";
                case "Lotrzyk": return "Postacie/Rogue";
                case "Druid": return "Postacie/Ranger";
                case "Nekromanta": return "Postacie/Skeleton_Mage";
                default: return "Postacie/Knight";
            }
        }

        public static string PostacZajecia(string zajecie)
        {
            switch (zajecie)
            {
                case "drwal": return "Postacie/Barbarian";
                case "straznik": return "Postacie/Knight";
                case "zielarz":
                case "uczony": return "Postacie/Mage";
                case "mysliwy":
                case "rolnik": return "Postacie/Ranger";
                case "bezczynny": return "Postacie/Rogue_Hooded";
                default: return "Postacie/Rogue";
            }
        }

        /// <summary>Części domu: cztery ściany, drzwi, okna i dach. Wspólny układ dla chaty i dworu.</summary>
        public static List<CzescBudynku> Czesci(string zlozenie)
        {
            bool dwor = zlozenie == Dwor;
            string sciana = dwor ? "Wioska/Wall_UnevenBrick_Straight" : "Wioska/Wall_Plaster_Straight";
            string drzwi = dwor ? "Wioska/Wall_UnevenBrick_Door_Round" : "Wioska/Wall_Plaster_Door_Round";
            string okno = dwor ? "Wioska/Wall_UnevenBrick_Window_Wide_Round" : "Wioska/Wall_Plaster_Window_Wide_Round";
            string naroznik = dwor ? "Wioska/Corner_Exterior_Brick" : "Wioska/Corner_Exterior_Wood";
            var c = new List<CzescBudynku>
            {
                // Przód widać z kamery, więc tam idą drzwi i okno; tyłu prawie nie widać.
                new CzescBudynku(drzwi, -1f, 0f, -2f, 180f), new CzescBudynku(okno, 1f, 0f, -2f, 180f),
                new CzescBudynku(sciana, -1f, 0f, 2f), new CzescBudynku(sciana, 1f, 0f, 2f),
                new CzescBudynku(okno, 2f, 0f, -1f, 90f), new CzescBudynku(sciana, 2f, 0f, 1f, 90f),
                new CzescBudynku(sciana, -2f, 0f, -1f, -90f), new CzescBudynku(sciana, -2f, 0f, 1f, -90f),
                new CzescBudynku(naroznik, 2f, 0f, 2f), new CzescBudynku(naroznik, -2f, 0f, 2f),
                new CzescBudynku(naroznik, 2f, 0f, -2f), new CzescBudynku(naroznik, -2f, 0f, -2f),
                new CzescBudynku("Wioska/Roof_RoundTiles_4x4", 0f, 3f, 0f),
            };
            if (dwor)
            {
                c.Add(new CzescBudynku("Wioska/Prop_Chimney", 1.2f, 3.6f, 1.2f));
            }
            return c;
        }

        /// <summary>Udział warstw zdjęciowych na polu biomu: trawa, ziemia, skała, piasek. Sumują się do 1.</summary>
        public static (double Trawa, double Ziemia, double Skala, double Piasek) SkladGruntu(string biom)
        {
            switch (biom)
            {
                case "las": return (0.55, 0.45, 0.0, 0.0);
                case "bagna": return (0.25, 0.75, 0.0, 0.0);
                case "wzgórza": return (0.55, 0.2, 0.25, 0.0);
                case "kanion": return (0.0, 0.15, 0.55, 0.3);
                case "ruiny": return (0.2, 0.35, 0.45, 0.0);
                case "woda": return (0.0, 0.2, 0.0, 0.8);
                case "plac": return (0.0, 0.65, 0.05, 0.3);
                default: return (0.85, 0.15, 0.0, 0.0);
            }
        }

        /// <summary>Odcień mnożony przez teksturę gruntu: biom, delikatna zmienność i pora roku.</summary>
        public static Barwa KolorGruntu(string biom, double odcien = 0.5, string pora = "lato")
        {
            (double r, double g, double b) k;
            switch (biom)
            {
                case "las": k = (0.58, 0.82, 0.58); break;
                case "bagna": k = (0.58, 0.66, 0.48); break;
                case "wzgórza": k = (0.84, 0.92, 0.68); break;
                case "kanion": k = (0.98, 0.66, 0.48); break;
                case "ruiny": k = (0.8, 0.77, 0.7); break;
                case "woda": k = (0.9, 0.9, 0.85); break;
                case "plac": k = (0.92, 0.84, 0.7); break;
                default: k = (0.78, 1.0, 0.74); break;
            }
            if (biom != "woda")
            {
                if (pora == "jesien")
                {
                    k = (k.r * 1.15, k.g * 0.88, k.b * 0.75);
                }
                else if (pora == "zima")
                {
                    k = (0.5 * k.r + 0.45, 0.5 * k.g + 0.47, 0.5 * k.b + 0.5);
                }
                else if (pora == "wiosna")
                {
                    k = (k.r * 0.96, k.g * 1.06, k.b * 1.02);
                }
            }
            double d = 0.92 + 0.16 * Math.Max(0.0, Math.Min(1.0, odcien));
            return new Barwa(Bajt(k.r * d), Bajt(k.g * d), Bajt(k.b * d));
        }

        private static int Bajt(double v)
        {
            return (int)Math.Max(0.0, Math.Min(255.0, v * 255.0));
        }

        /// <summary>Wszystkie ścieżki modeli używane przez grę — dla testu „czy pliki istnieją”.</summary>
        public static IEnumerable<string> WszystkieSciezki()
        {
            var lista = new List<string>();
            lista.AddRange(Drzewa);
            lista.AddRange(Skaly);
            lista.AddRange(Krzaki);
            lista.AddRange(Ziola);
            lista.AddRange(Suche);
            lista.AddRange(Trawy);
            lista.Add(DlaObiektu(Obiekt.Sciana, 0, 0).Value.Sciezka);
            foreach (string k in new[] { "spiwor", "ognisko", "namiot", "palisada", "brama", "wieza", "warsztat", "tartak", "kuznia", "farma", "spichlerz", "tawerna", "lecznica", "targ" })
            {
                lista.Add(DlaBudowli(k).Sciezka);
            }
            foreach (string k in new[]
                     {
                         "kuznia", "huta", "spichlerz", "karawanseraj", "tartak", "sklep", "warsztat", "stajnie",
                         "laboratorium", "tawerna", "lecznica", "targ", "wieza", "skryptorium", "farma",
                     })
            {
                lista.Add(DlaBudynku(k, 1).Sciezka);
            }
            foreach (string p in new[]
                     {
                         "obóz", "karczma", "kuźnia", "świątynia", "miasto", "jaskinia", "boss", "portal",
                         "leze_smoka", "latajaca_wyspa", "inny",
                     })
            {
                lista.Add(DlaPunktu(p).Sciezka);
            }
            foreach (string b in new[] { "las", "bagna", "wzgórza", "kanion", "ruiny", "równiny" })
            {
                foreach ((WpisModelu m, float _, float _) in Roslinnosc(b, 2, 3))
                {
                    lista.Add(m.Sciezka);
                }
            }
            foreach (string k in new[] { "Wojownik", "Mag", "Lotrzyk", "Druid", "Nekromanta" })
            {
                lista.Add(PostacKlasy(k));
            }
            foreach (string z in new[] { "drwal", "kamieniarz", "zielarz", "mysliwy", "bezczynny", "inny" })
            {
                lista.Add(PostacZajecia(z));
            }
            foreach (string z in new[] { Chata, Dwor })
            {
                foreach (CzescBudynku c in Czesci(z))
                {
                    lista.Add(c.Sciezka);
                }
            }
            foreach (TypWroga t in new[] { TypWroga.Slugus, TypWroga.Lotr, TypWroga.Wojownik, TypWroga.Mag })
            {
                lista.Add(DlaWroga(t).Sciezka);
            }
            lista.Add(PorzuconyPlecak.Sciezka);
            lista.Add("Oboz/structure");
            lista.Add("Oboz/structure-floor");
            lista.Add("Oboz/fence");
            return lista;
        }
    }
}
