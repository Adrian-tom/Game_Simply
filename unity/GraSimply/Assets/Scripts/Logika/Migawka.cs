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
            return hud;
        }
    }
}
