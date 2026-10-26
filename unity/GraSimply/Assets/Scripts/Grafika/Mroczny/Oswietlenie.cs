using System;
using System.Collections.Generic;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>Punktowe źródło światła na płótnie sceny — ogień, okno, latarnia postaci.</summary>
    public sealed class Swiatlo
    {
        public double X, Y, Promien;
        public (double R, double G, double B) Kolor;
        public bool Migocze;
    }

    /// <summary>
    /// Noc stylu mrocznego i jego wspólna paleta — wydzielone z generatora
    /// regionu, żeby korzystała z nich też scena osady (styl hybrydowy).
    /// Zachowanie jest dokładnie to samo: generator regionu woła tu to, co
    /// robił wcześniej sam.
    /// </summary>
    public static class Oswietlenie
    {
        /// <summary>Światło nocy: przyciemnienie bez źródeł, lekko w stronę błękitu.</summary>
        public static readonly (double R, double G, double B) Noc = (0.36, 0.37, 0.47);

        private static bool[] _komorkiSwiecace;
        private static Kwantyzator _kwantyzator;
        private static readonly object Zamek = new object();

        /// <summary>
        /// Każdy piksel mnożony przez natężenie — otoczenie albo najjaśniejsze
        /// z pobliskich świateł (max, nie suma: dwa ogniska obok siebie nie
        /// prześwietlają ziemi). Barwy samoświecące przepuszczamy bez zmian.
        /// </summary>
        /// <param name="otoczenie">Natężenie tam, gdzie żadne światło nie sięga: 1 = dzień, <see cref="Noc"/> = noc.</param>
        public static void Zastosuj(Plotno cel, IEnumerable<Swiatlo> swiatla, double t,
                                    (double R, double G, double B) otoczenie)
        {
            int szer = cel.Szerokosc, wys = cel.Wysokosc;
            var natR = new float[szer * wys];
            var natG = new float[szer * wys];
            var natB = new float[szer * wys];
            for (int i = 0; i < natR.Length; i++)
            {
                natR[i] = (float)otoczenie.R;
                natG[i] = (float)otoczenie.G;
                natB[i] = (float)otoczenie.B;
            }
            foreach (Swiatlo s in swiatla)
            {
                double r = s.Promien;
                if (s.Migocze)
                {
                    r *= 1 + Math.Sin(t * 9 + s.X) * 0.04 + Math.Sin(t * 13.7 + s.Y) * 0.03;
                }
                int x0 = Math.Max(0, (int)(s.X - r)), x1 = Math.Min(szer - 1, (int)(s.X + r));
                // Na gruncie światło rozlewa się w spłaszczoną elipsę, jak sam rzut.
                int y0 = Math.Max(0, (int)(s.Y - r * 0.62)), y1 = Math.Min(wys - 1, (int)(s.Y + r * 0.62));
                for (int y = y0; y <= y1; y++)
                {
                    double dy = (y - s.Y) / 0.62;
                    for (int x = x0; x <= x1; x++)
                    {
                        double dx = x - s.X;
                        double d = Math.Sqrt(dx * dx + dy * dy) / r;
                        if (d >= 1)
                        {
                            continue;
                        }
                        double f = 1 - d;
                        f = f * f * (3 - 2 * f); // gładkie wygaszenie, bez twardego kręgu
                        int i = y * szer + x;
                        natR[i] = Math.Max(natR[i], (float)(s.Kolor.R * f));
                        natG[i] = Math.Max(natG[i], (float)(s.Kolor.G * f));
                        natB[i] = Math.Max(natB[i], (float)(s.Kolor.B * f));
                    }
                }
            }
            bool[] swiecace = KomorkiSwiecace();
            byte[] dane = cel.Dane;
            for (int i = 0; i < natR.Length; i++)
            {
                int j = i * 4;
                byte r = dane[j], g = dane[j + 1], b = dane[j + 2];
                if (swiecace[((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3)] && Materialy.CzySamoswiecaca(r, g, b))
                {
                    continue;
                }
                dane[j] = (byte)Math.Min(255, r * natR[i]);
                dane[j + 1] = (byte)Math.Min(255, g * natG[i]);
                dane[j + 2] = (byte)Math.Min(255, b * natB[i]);
            }
        }

        /// <summary>Komórki RGB555, w które wpada jakaś barwa samoświecąca — szybkie sito przed zbiorem.</summary>
        private static bool[] KomorkiSwiecace()
        {
            lock (Zamek)
            {
                if (_komorkiSwiecace == null)
                {
                    var k = new bool[32 * 32 * 32];
                    foreach (int c in Materialy.Samoswiecace)
                    {
                        int r = (c >> 16) & 255, g = (c >> 8) & 255, b = c & 255;
                        k[((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3)] = true;
                    }
                    _komorkiSwiecace = k;
                }
                return _komorkiSwiecace;
            }
        }

        /// <summary>Paleta główna: wszystkie rampy materiałów plus czerń. Ogień i światła nietykalne.</summary>
        public static Kwantyzator Paleta()
        {
            lock (Zamek)
            {
                if (_kwantyzator == null)
                {
                    var barwy = new List<Barwa> { new Barwa(0, 0, 0) };
                    foreach (Barwa[] rampa in Materialy.WszystkieRampy())
                    {
                        barwy.AddRange(rampa);
                    }
                    // Przyciemnione wersje ramp — noc i cienie potrzebują stopni poniżej
                    // najciemniejszego stopnia materiału, inaczej wszystko zlewa się w jedną barwę.
                    foreach (Barwa[] rampa in Materialy.WszystkieRampy())
                    {
                        foreach (Barwa b in rampa)
                        {
                            if (!Materialy.CzySamoswiecaca(b.R, b.G, b.B))
                            {
                                barwy.Add(GraSimply.Grafika.Paleta.Siatka6(b.R * 0.5, b.G * 0.5, b.B * 0.55));
                                barwy.Add(GraSimply.Grafika.Paleta.Siatka6(b.R * 0.28, b.G * 0.28, b.B * 0.34));
                            }
                        }
                    }
                    _kwantyzator = new Kwantyzator(barwy, Materialy.Samoswiecace);
                }
                return _kwantyzator;
            }
        }
    }
}
