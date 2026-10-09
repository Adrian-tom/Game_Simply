using System;
using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Paleta z ramp i sprowadzanie gotowej klatki do niej.
    ///
    /// Gry z lat 90. miały jedną paletę 256 barw na wszystko — teren, postaci
    /// i interfejs — i to ona najmocniej scalała obraz. Ten moduł buduje taką
    /// paletę z reguł, a nie z gotowego pliku: każda rodzina barw to rampa
    /// stopni na siatce 6-bitowej (każdy kanał to wielokrotność 4, jak w VGA),
    /// a ciemniejsze stopnie są bardziej nasycone niż jasne — tak zachowują się
    /// rampy w grach tamtej epoki i stąd bierze się ich „kurz”.
    /// </summary>
    public static class Paleta
    {
        /// <summary>Sprowadza składową do siatki 6-bitowej i rozwija z powrotem do 8 bitów.</summary>
        public static byte Siatka6(double v)
        {
            int v6 = (int)Math.Round(Math.Max(0.0, Math.Min(255.0, v)) * 63.0 / 255.0);
            return (byte)((v6 << 2) | (v6 >> 4));
        }

        public static Barwa Siatka6(double r, double g, double b)
        {
            return new Barwa(Siatka6(r), Siatka6(g), Siatka6(b));
        }

        /// <summary>HSV → RGB; odcień w stopniach, nasycenie i jasność 0…1.</summary>
        public static (double R, double G, double B) ZHsv(double h, double s, double v)
        {
            h = ((h % 360.0) + 360.0) % 360.0;
            s = Math.Max(0.0, Math.Min(1.0, s));
            v = Math.Max(0.0, Math.Min(1.0, v));
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;
            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return ((r + m) * 255.0, (g + m) * 255.0, (b + m) * 255.0);
        }

        /// <summary>
        /// Sufit nasycenia zależny od jasności: pełne nasycenie tylko w prawie-czerni,
        /// jasne tony zawsze przygaszone. To ta reguła odróżnia „kurz” od kolorowej
        /// palety fantasy.
        /// </summary>
        public static double SufitNasycenia(double v)
        {
            if (v > 0.55)
            {
                return 0.45;
            }
            if (v > 0.25)
            {
                return 0.65;
            }
            return 1.0;
        }

        /// <summary>
        /// Rampa stopni od cienia (indeks 0) do światła.
        /// </summary>
        /// <param name="odcien">Odcień w środku rampy, w stopniach.</param>
        /// <param name="nasycenieJasne">Nasycenie najjaśniejszego stopnia.</param>
        /// <param name="nasycenieCiemne">Nasycenie najciemniejszego stopnia — zwykle większe.</param>
        /// <param name="vCiemne">Jasność najciemniejszego stopnia (0…1).</param>
        /// <param name="vJasne">Jasność najjaśniejszego stopnia.</param>
        /// <param name="stopni">Ile stopni.</param>
        /// <param name="dryf">
        /// O ile stopni przesuwa się odcień ku ciemnemu końcowi. Ciepłe barwy
        /// ciemniejąc idą ku czerwieni (dryf ujemny), zielenie ku zgniłej
        /// zieleni-cyjanowi (dodatni).
        /// </param>
        public static Barwa[] Rampa(double odcien, double nasycenieJasne, double nasycenieCiemne,
                                    double vCiemne, double vJasne, int stopni, double dryf = 0.0)
        {
            var wynik = new Barwa[stopni];
            for (int i = 0; i < stopni; i++)
            {
                double t = stopni == 1 ? 1.0 : i / (double)(stopni - 1);
                double v = vCiemne + (vJasne - vCiemne) * t;
                double s = nasycenieCiemne + (nasycenieJasne - nasycenieCiemne) * t;
                s = Math.Min(s, SufitNasycenia(v));
                double h = odcien + dryf * (1.0 - t) - dryf * 0.5;
                (double r, double g, double b) = ZHsv(h, s, v);
                wynik[i] = Siatka6(r, g, b);
            }
            return wynik;
        }

        /// <summary>Barwa z rampy dla wartości 0…1, bez ditheringu.</summary>
        public static Barwa Z(Barwa[] rampa, double v)
        {
            v = Math.Max(0.0, Math.Min(1.0, v));
            int i = (int)Math.Round(v * (rampa.Length - 1));
            return rampa[i];
        }

        /// <summary>
        /// Barwa z rampy z drobnym, nieregularnym przejściem między sąsiednimi
        /// stopniami: zamiast siatki Bayera decyduje szum pikselowy. Dithering
        /// w grafice prerenderowanej był wpisany ręcznie i nie miał regularnego
        /// wzoru — siatka 4×4 od razu by to zdradziła.
        /// </summary>
        public static Barwa ZSzumem(Barwa[] rampa, double v, double szum01)
        {
            v = Math.Max(0.0, Math.Min(1.0, v)) * (rampa.Length - 1);
            int i = (int)v;
            if (v - i > szum01 && i < rampa.Length - 1)
            {
                i++;
            }
            return rampa[i];
        }
    }

    /// <summary>
    /// Sprowadza obraz do zadanej palety przez tablicę RGB555 → indeks,
    /// jak silniki z lat 90.: raz policzona, potem jedno odczytanie na piksel.
    ///
    /// Odległość liczona jest z wagami jasności (0,30 / 0,59 / 0,11), żeby
    /// kwantyzacja nie wywracała jasności — ludzkie oko wybacza przesunięcie
    /// odcienia, a nie wybacza piksela jaśniejszego niż sąsiedzi.
    /// </summary>
    public sealed class Kwantyzator
    {
        private readonly Barwa[] _paleta;
        private readonly ushort[] _lut = new ushort[32 * 32 * 32];
        private readonly HashSet<int> _nietykalne;

        /// <summary>Komórki RGB555 zawierające choć jedną barwę nietykalną — sito przed zbiorem.</summary>
        private readonly bool[] _komorkiNietykalne = new bool[32 * 32 * 32];

        /// <param name="paleta">Barwy, do których sprowadzamy obraz.</param>
        /// <param name="nietykalne">
        /// Barwy przepuszczane bez zmian i wyłączone z doboru — samoświecące.
        /// Gdyby brały udział w kwantyzacji, piksel jasnej ochry mógłby wpaść
        /// w barwę ognia (i migotać z płomieniem), a ogień zostałby sprowadzony
        /// do najbliższego brązu i zgasł.
        /// </param>
        public Kwantyzator(IEnumerable<Barwa> paleta, HashSet<int> nietykalne = null)
        {
            _nietykalne = nietykalne ?? new HashSet<int>();
            foreach (int c in _nietykalne)
            {
                _komorkiNietykalne[Komorka((byte)(c >> 16), (byte)(c >> 8), (byte)c)] = true;
            }
            var unikalne = new List<Barwa>();
            var widziane = new HashSet<int>();
            foreach (Barwa b in paleta)
            {
                int klucz = (b.R << 16) | (b.G << 8) | b.B;
                if (_nietykalne.Contains(klucz))
                {
                    continue;
                }
                if (widziane.Add(klucz))
                {
                    unikalne.Add(new Barwa(b.R, b.G, b.B));
                }
            }
            if (unikalne.Count == 0)
            {
                throw new ArgumentException("Pusta paleta.", nameof(paleta));
            }
            _paleta = unikalne.ToArray();

            for (int r5 = 0; r5 < 32; r5++)
            {
                for (int g5 = 0; g5 < 32; g5++)
                {
                    for (int b5 = 0; b5 < 32; b5++)
                    {
                        // Środek komórki, nie jej róg — inaczej cała siatka byłaby
                        // przesunięta o pół kroku w stronę ciemności.
                        double r = (r5 << 3) + 4;
                        double g = (g5 << 3) + 4;
                        double b = (b5 << 3) + 4;
                        int najlepszy = 0;
                        double minimum = double.MaxValue;
                        for (int i = 0; i < _paleta.Length; i++)
                        {
                            double dr = r - _paleta[i].R;
                            double dg = g - _paleta[i].G;
                            double db = b - _paleta[i].B;
                            double d = 0.30 * dr * dr + 0.59 * dg * dg + 0.11 * db * db;
                            if (d < minimum)
                            {
                                minimum = d;
                                najlepszy = i;
                            }
                        }
                        _lut[(r5 << 10) | (g5 << 5) | b5] = (ushort)najlepszy;
                    }
                }
            }
        }

        private static int Komorka(byte r, byte g, byte b) => ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);

        private bool Nietykalna(byte r, byte g, byte b) =>
            _komorkiNietykalne[Komorka(r, g, b)] && _nietykalne.Contains((r << 16) | (g << 8) | b);

        public int Rozmiar => _paleta.Length;

        public IReadOnlyList<Barwa> Barwy => _paleta;

        public Barwa Najblizsza(Barwa b)
        {
            if (Nietykalna(b.R, b.G, b.B))
            {
                return b;
            }
            return _paleta[_lut[Komorka(b.R, b.G, b.B)]];
        }

        /// <summary>Sprowadza całe płótno do palety, zostawiając przezroczystość bez zmian.</summary>
        public void Kwantyzuj(Plotno p)
        {
            byte[] d = p.Dane;
            for (int i = 0; i < d.Length; i += 4)
            {
                if (d[i + 3] == 0 || Nietykalna(d[i], d[i + 1], d[i + 2]))
                {
                    continue;
                }
                Barwa b = _paleta[_lut[Komorka(d[i], d[i + 1], d[i + 2])]];
                d[i] = b.R;
                d[i + 1] = b.G;
                d[i + 2] = b.B;
            }
        }

        /// <summary>Kwantyzuje tylko prostokąt — przydaje się, gdy chrom i scena mają osobne reguły.</summary>
        public void Kwantyzuj(Plotno p, int x0, int y0, int szer, int wys)
        {
            byte[] d = p.Dane;
            for (int y = Math.Max(0, y0); y < Math.Min(p.Wysokosc, y0 + wys); y++)
            {
                for (int x = Math.Max(0, x0); x < Math.Min(p.Szerokosc, x0 + szer); x++)
                {
                    int i = (y * p.Szerokosc + x) * 4;
                    if (Nietykalna(d[i], d[i + 1], d[i + 2]))
                    {
                        continue;
                    }
                    Barwa b = _paleta[_lut[Komorka(d[i], d[i + 1], d[i + 2])]];
                    d[i] = b.R;
                    d[i + 1] = b.G;
                    d[i + 2] = b.B;
                }
            }
        }
    }
}
