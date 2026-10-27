using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>Co widać na jednym polu — tyle, ile potrzebuje rysowanie.</summary>
    public struct PoleDoRysunku
    {
        public string Biom;
        public string Punkt;
        public bool Odkryte;
    }

    /// <summary>Jeden osadnik w tyle, ile potrzebuje scena osady z bliska.</summary>
    public sealed class OsadnikDoRysunku
    {
        public string Imie;
        public string Zajecie;
        public bool Chory;
        public double Morale;
    }

    /// <summary>
    /// Osada do narysowania z bliska: kto mieszka, co stoi i jaka jest pora roku.
    /// Zamrożona kopia — scena nigdy nie zagląda do żywych list postaci.
    /// </summary>
    public sealed class StanOsady
    {
        public List<OsadnikDoRysunku> Osadnicy = new List<OsadnikDoRysunku>();

        /// <summary>Klucz budynku → poziom (0 = nie stoi).</summary>
        public Dictionary<string, int> Budynki = new Dictionary<string, int>();

        public int Chaty;

        /// <summary>Pole placu, na którym stoi bohater.</summary>
        public (int X, int Y) Bohater = UkladPlacu.PoleBohatera;

        /// <summary>Budowy w toku: (klucz budynku albo „chata”, poziom, postęp 0–1) — rusztowania na placu.</summary>
        public List<(string Klucz, int Poziom, double Postep)> Rusztowania =
            new List<(string Klucz, int Poziom, double Postep)>();

        /// <summary>„wiosna”, „lato”, „jesien” albo „zima” — kolor gruntu i śnieg.</summary>
        public string Pora = "lato";

        /// <summary>Klasa bohatera — stoi na placu przy ognisku.</summary>
        public string Klasa;
    }

    /// <summary>Stan postaci przepisany na to, co pokazuje HUD.</summary>
    public sealed class StanHud
    {
        public string Imie;
        public string Klasa;
        public int Poziom;
        public int Zloto;
        public int Hp;
        public int MaxHp;
        public int Mana;
        public int MaxMana;
        public string Data;
        public string Region;
        public string Biom;
        public string Zywnosc;
        public string Zagrozenie;
        public string Osada;
        public string Surowce;
        public string Podpowiedz;

        /// <summary>Surowce jako liczby, w kolejności z <see cref="Oboz.Surowce"/> — do ikon zamiast emoji.</summary>
        public List<KeyValuePair<string, int>> ListaSurowcow = new List<KeyValuePair<string, int>>();
    }

    /// <summary>
    /// Zamrożony stan świata do narysowania jednej klatki.
    ///
    /// Logika chodzi na osobnym wątku i swobodnie zmienia listy i słowniki
    /// postaci. Gdyby wątek rysujący czytał je wprost, prędzej czy później
    /// trafiłby na słownik w połowie zmiany — a to w .NET kończy się wyjątkiem
    /// albo zapętleniem, nie tylko brzydką klatką. Dlatego migawkę buduje sama
    /// logika (w <see cref="Ekran.Migawke"/>), a rysowanie dostaje dane,
    /// których już nikt nie rusza.
    ///
    /// Animacja (woda, ogień, chód postaci) jest liczona z czasu, więc obraz
    /// żyje między migawkami i nic nie zastyga.
    /// </summary>
    public sealed class Migawka
    {
        public PoleDoRysunku[][] Pola;
        public StanHud Hud;
        public (int X, int Y) Region;
        public (int X, int Y)? GraczXy;
        public string Klasa;
        public bool WszystkoOdkryte;
        public (int Chaty, int Palisada, int Wieza)? Osada;

        /// <summary>Osada do narysowania z bliska — tylko gdy otwarty jest widok <see cref="Widok"/>.</summary>
        public StanOsady Plac;

        /// <summary>Otwarty widok z bliska („oboz”, „osada”) albo null.</summary>
        public string Widok;

        /// <summary>Kto mówi w rozmowie albo null.</summary>
        public string Rozmowa;

        /// <summary>Region poza zasięgiem prawdziwych — ekran tytułowy ma własne kafle.</summary>
        public static readonly (int X, int Y) RegionTytulowy = (9999, 9999);

        /// <summary>Migawka ekranu tytułowego: mapa bez gracza, wszystko odkryte.</summary>
        public static Migawka Tytulowa(long seed = 19)
        {
            Pole[][] pola = Mapa.GenerujMape(1, seed);
            return new Migawka
            {
                Pola = ZPol(pola),
                Hud = null,
                Region = RegionTytulowy,
                GraczXy = null,
                WszystkoOdkryte = true,
            };
        }

        /// <summary>Przepisuje siatkę logiki na dane do rysowania.</summary>
        public static PoleDoRysunku[][] ZPol(Pole[][] pola)
        {
            var wynik = new PoleDoRysunku[pola.Length][];
            for (int y = 0; y < pola.Length; y++)
            {
                wynik[y] = new PoleDoRysunku[pola[y].Length];
                for (int x = 0; x < pola[y].Length; x++)
                {
                    Pole p = pola[y][x];
                    wynik[y][x] = new PoleDoRysunku
                    {
                        Biom = p.Biom,
                        Punkt = p.Punkt,
                        Odkryte = p.Odkryte,
                    };
                }
            }
            return wynik;
        }

        /// <summary>Budynki, które mają miejsce na placu — tylko ich poziomy są potrzebne scenie.</summary>
        public static readonly string[] BudynkiPlacu =
        {
            "dom", "kuznia", "huta", "spichlerz", "karawanseraj", "tartak", "sklep", "warsztat",
            "stajnie", "laboratorium", "tawerna", "lecznica", "targ", "wieza", "farma", "palisada",
            "skryptorium",
        };

        /// <summary>Zamraża osadę do narysowania z bliska. Wołane z wątku logiki.</summary>
        public static StanOsady PlacZGracza(Gracz g)
        {
            var plac = new StanOsady
            {
                Chaty = g.Chaty,
                Pora = Kalendarz.PoraGracza(g).Klucz,
                Klasa = g.Klasa,
                Bohater = g.PozycjaObozu,
            };
            foreach (string klucz in BudynkiPlacu)
            {
                plac.Budynki[klucz] = g.PoziomBudynku(klucz);
            }
            plac.Rusztowania.AddRange(Budowa.Rusztowania(g));
            foreach (Osadnik o in g.Osadnicy)
            {
                plac.Osadnicy.Add(new OsadnikDoRysunku
                {
                    Imie = o.Imie,
                    Zajecie = o.Zajecie,
                    Chory = o.Chory > 0,
                    Morale = o.Morale,
                });
            }
            return plac;
        }

        /// <summary>Buduje stan HUD z postaci. Wołane z wątku logiki.</summary>
        public static StanHud HudZGracza(Gracz g)
        {
            int zywnosc = g.Surowce.Wez("zywnosc");
            string prowiant = g.WObozie ? "" : $"  ·  prowiant {g.Prowiant}";
            var hud = new StanHud
            {
                Imie = g.Imie,
                Klasa = g.Klasa + (g.Podklasa != null ? $" · {g.Podklasa}" : ""),
                Poziom = g.Poziom,
                Zloto = g.Zloto,
                Hp = g.Hp,
                MaxHp = g.MaxHp,
                Mana = g.Mana,
                MaxMana = g.MaxMana,
                Data = Kalendarz.OpisDaty(g.Czas),
                Region = $"region [{g.RegionX}, {g.RegionY}] poz. {g.MapaGen}",
                Biom = string.IsNullOrEmpty(g.AktualnyBiom) ? "—" : g.AktualnyBiom,
                Zywnosc = $"🍖 {zywnosc}{prowiant}",
                // Zagrożenie najazdem policzy moduł obrony w etapie 2;
                // na razie HUD mówi wprost, czy bohater jest w obozie.
                Zagrozenie = g.WObozie ? "w obozie" : "na wyprawie",
                Osada = $"Osada: {g.Osadnicy.Count} os.  ·  {Karma.Etykieta(g)}  ·  " +
                        Przetrwanie.OpisStanu(g),
                Surowce = Oboz.LiniaSurowcow(g),
            };
            foreach (KeyValuePair<string, OpisSurowca> para in Oboz.Surowce)
            {
                hud.ListaSurowcow.Add(new KeyValuePair<string, int>(para.Key, g.Surowce.Wez(para.Key)));
            }
            return hud;
        }
    }
}
