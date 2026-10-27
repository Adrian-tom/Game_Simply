using System;
using System.Collections.Generic;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Rodzaj obiektu stojącego na kaflu świata.</summary>
    public enum Obiekt
    {
        Brak,
        Drzewo,
        Skala,
        Krzak,
        Ziolo,
        Woda,
        Sciana,
    }

    /// <summary>
    /// Proceduralny świat z kafli dla trybu „Zomboid fantasy”. Nie ma żadnych zapisanych danych:
    /// biom, woda i obiekty kafla są funkcją (ziarno, x, y), więc mapa jest ogromna, zajmuje zero
    /// pamięci i wygląda tak samo po każdym uruchomieniu. Zmienia się tylko to, co gracz ruszył
    /// (ścięte drzewa, postawiony namiot) — to trzyma <see cref="SwiatWolny"/>.
    ///
    /// Wzorzec z Project Zomboid: cechy terenu z szumu na współrzędnych świata. Biom pola 8×8 kafli
    /// bierzemy z <see cref="Mapa.BiomSwiata"/>, a granice rozmywamy szumem współrzędnych, żeby
    /// przechodziły nieregularnie jak w <c>Blending</c> z Zomboid, a nie po linii.
    /// </summary>
    public sealed class GeneratorSwiata
    {
        /// <summary>Bok pola biomu w kaflach (jedno pole starej mapy regionu).</summary>
        public const int KafleNaPole = 8;

        /// <summary>Świat ma kwadratowy zasięg ±Granica kafli od obozu; dalej jest nieprzebyta mgła.</summary>
        public const int Granica = 1000;

        /// <summary>Promień wokół punktu startu, w którym nic nie stoi — miejsce na obóz.</summary>
        public const int PolanaStartowa = 4;

        private readonly long _seed;
        private readonly Dictionary<(int X, int Y), string> _biomy = new Dictionary<(int X, int Y), string>();

        public GeneratorSwiata(long seed)
        {
            _seed = seed & 0x7FFFFFFFL;
        }

        private static int PodzielWDol(int a, int b)
        {
            return (int)Math.Floor(a / (double)b);
        }

        public static bool WGranicach(int x, int y)
        {
            return Math.Abs(x) <= Granica && Math.Abs(y) <= Granica;
        }

        /// <summary>Biom kafla: pole biomu z lekko rozmytą granicą. Obóz leży w środku pola z równinami.</summary>
        public string Biom(int x, int y)
        {
            double jx = x + (Szum.Szum2(x / 5.0, y / 5.0, _seed + 11) - 0.5) * 7.0;
            double jy = y + (Szum.Szum2(x / 5.0, y / 5.0, _seed + 12) - 0.5) * 7.0;
            int gx = PodzielWDol((int)Math.Floor(jx) + KafleNaPole / 2, KafleNaPole) + Mapa.Srodek;
            int gy = PodzielWDol((int)Math.Floor(jy) + KafleNaPole / 2, KafleNaPole) + Mapa.Srodek;
            if (!_biomy.TryGetValue((gx, gy), out string biom))
            {
                biom = Mapa.BiomSwiata(_seed, gx, gy);
                if (_biomy.Count > 4000)
                {
                    _biomy.Clear();
                }
                _biomy[(gx, gy)] = biom;
            }
            return biom;
        }

        private double Los(int x, int y, int sol)
        {
            return Szum.Losowa(x, y, (_seed + 977L * sol) & 0x7FFFFFFFL);
        }

        /// <summary>Obiekt stojący na kaflu (przed uwzględnieniem tego, co gracz już ściął).</summary>
        public Obiekt ObiektNa(int x, int y)
        {
            if (!WGranicach(x, y))
            {
                return Obiekt.Sciana;
            }
            if (Math.Max(Math.Abs(x), Math.Abs(y)) <= PolanaStartowa)
            {
                return Obiekt.Brak;
            }
            string biom = Biom(x, y);
            double gestosc = Szum.Szum2(x / 6.0, y / 6.0, _seed + 21);
            double los = Los(x, y, 1);
            switch (biom)
            {
                case "las":
                    if (los < 0.08 + 0.32 * gestosc) return Obiekt.Drzewo;
                    if (los < 0.13 + 0.32 * gestosc) return Obiekt.Krzak;
                    return los < 0.15 + 0.32 * gestosc ? Obiekt.Ziolo : Obiekt.Brak;
                case "bagna":
                    if (Szum.Szum2(x / 4.0, y / 4.0, _seed + 31) > 0.60) return Obiekt.Woda;
                    if (los < 0.05) return Obiekt.Drzewo;
                    return los < 0.12 ? Obiekt.Ziolo : Obiekt.Brak;
                case "wzgórza":
                    if (los < 0.06 + 0.10 * gestosc) return Obiekt.Skala;
                    return los < 0.08 + 0.10 * gestosc ? Obiekt.Drzewo : Obiekt.Brak;
                case "kanion":
                    if (los < 0.10 + 0.12 * gestosc) return Obiekt.Skala;
                    return los < 0.115 + 0.12 * gestosc ? Obiekt.Krzak : Obiekt.Brak;
                case "ruiny":
                    if (gestosc > 0.55 && los < 0.30) return Obiekt.Sciana;
                    if (los < 0.05) return Obiekt.Skala;
                    return los < 0.08 ? Obiekt.Ziolo : Obiekt.Brak;
                default: // równiny
                    if (los < 0.012 + 0.04 * gestosc) return Obiekt.Drzewo;
                    if (los < 0.03 + 0.04 * gestosc) return Obiekt.Krzak;
                    return los < 0.045 + 0.04 * gestosc ? Obiekt.Ziolo : Obiekt.Brak;
            }
        }

        /// <summary>Odcień kafla 0–1 (ziarno gruntu) — żeby trawa nie była jednolita.</summary>
        public double Odcien(int x, int y)
        {
            return Los(x, y, 2);
        }

        /// <summary>Czy kafel jest w wodzie (przydatne rysowaniu — woda to też podłoże, nie tylko obiekt).</summary>
        public bool CzyWoda(int x, int y)
        {
            return ObiektNa(x, y) == Obiekt.Woda;
        }
    }
}
