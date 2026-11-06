using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Strzała wieży: tylko dla oka, bo obrażenia wchodzą od razu przy wystrzale.</summary>
    public sealed class Strzala
    {
        public double X0, Y0, X1, Y1, Czas;
    }

    /// <summary>
    /// Obrona osady: palisada zatrzymuje szkielety, brama przepuszcza tylko swoich, wieże strzelają same,
    /// a osadnik z zajęciem „strażnik” idzie na wroga z bronią. To, czego szkielety nie obejdą, muszą zburzyć.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const double ZasiegWiezy = 7.5;
        public const double ObrazeniaWiezy = 14.0;
        public const double RytmWiezy = 1.1;
        public const double ZasiegStrazy = 12.0;
        public const double ZycieStraznika = 90.0;
        public const double ZycieOsadnika = 45.0;

        public readonly List<Strzala> Strzaly = new List<Strzala>();

        private readonly Dictionary<(int X, int Y), double> _rytmWiez = new Dictionary<(int X, int Y), double>();

        private static bool Mur(string klucz)
        {
            return klucz == "palisada" || klucz == "brama" || klucz == "wieza";
        }

        /// <summary>Jak <see cref="Wolne"/>, tylko brama dla szkieletów jest ścianą.</summary>
        private bool WolneDlaWroga(double px, double py)
        {
            if (!Wolne(px, py))
            {
                return false;
            }
            int x0 = (int)Math.Floor(px - Promien), x1 = (int)Math.Floor(px + Promien);
            int y0 = (int)Math.Floor(py - Promien), y1 = (int)Math.Floor(py + Promien);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    if (Budowle.TryGetValue((x, y), out string b) && b == "brama"
                        && px + Promien > x + 0.12 && px - Promien < x + 0.88 && py + Promien > y + 0.12 && py - Promien < y + 0.88)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Wrog NajblizszyWrog(double x, double y, double zasieg)
        {
            Wrog naj = null;
            double d0 = zasieg;
            foreach (Wrog g in Wrogowie)
            {
                if (g.Stan == StanWroga.Martwy)
                {
                    continue;
                }
                double d = Dystans(g.X, g.Y, x, y);
                if (d <= d0)
                {
                    d0 = d;
                    naj = g;
                }
            }
            return naj;
        }

        private void KrokObrony(double dt)
        {
            for (int i = Strzaly.Count - 1; i >= 0; i--)
            {
                Strzaly[i].Czas += dt / 0.2;
                if (Strzaly[i].Czas >= 1.0)
                {
                    Strzaly.RemoveAt(i);
                }
            }
            foreach (KeyValuePair<(int X, int Y), string> b in Budowle)
            {
                if (b.Value != "wieza")
                {
                    continue;
                }
                double rytm = (_rytmWiez.TryGetValue(b.Key, out double r) ? r : 0.0) - dt;
                _rytmWiez[b.Key] = rytm;
                double wx = b.Key.X + 0.5, wy = b.Key.Y + 0.5;
                Wrog cel = rytm <= 0 ? NajblizszyWrog(wx, wy, ZasiegWiezy) : null;
                if (cel != null)
                {
                    _rytmWiez[b.Key] = RytmWiezy;
                    Strzaly.Add(new Strzala { X0 = wx, Y0 = wy, X1 = cel.X, Y1 = cel.Y });
                    ZadajWrogowi(cel, ObrazeniaWiezy, false);
                }
            }
            foreach (PostacOsadnika p in Postacie)
            {
                KrokStraznika(p, dt);
            }
        }

        private void KrokStraznika(PostacOsadnika p, double dt)
        {
            if (p.O.Zajecie != "straznik")
            {
                p.WWalce = false;
                if (p.Uzbrojony)
                {
                    p.Uzbrojony = false;
                    p.Zycie = Math.Min(p.Zycie, ZycieOsadnika);
                }
                return;
            }
            if (!p.Uzbrojony)
            {
                p.Uzbrojony = true;
                p.Zycie = Math.Max(p.Zycie, ZycieStraznika);
            }
            p.AtakCd = Math.Max(0, p.AtakCd - dt);
            Wrog g = NajblizszyWrog(p.X, p.Y, ZasiegStrazy);
            if (g == null || p.O.Chory > 0)
            {
                p.WWalce = false;
                return;
            }
            p.WWalce = true;
            p.Trasa.Clear();
            p.Spi = false;
            p.CelX = g.X;
            p.CelY = g.Y;
            double dx = g.X - p.X, dy = g.Y - p.Y, d = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy));
            if (d <= 1.35)
            {
                p.Idzie = false;
                if (p.AtakCd <= 0)
                {
                    p.AtakCd = 1.1;
                    p.Ataki++;
                    ZadajWrogowi(g, 13, false);
                }
                return;
            }
            p.Idzie = true;
            double krok = 2.8 * dt;
            if (Wolne(p.X + dx / d * krok, p.Y))
            {
                p.X += dx / d * krok;
            }
            if (Wolne(p.X, p.Y + dy / d * krok))
            {
                p.Y += dy / d * krok;
            }
            p.Dystans += krok;
        }
    }
}
