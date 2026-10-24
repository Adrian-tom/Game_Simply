using System;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>Jak barwić powierzchnię danego materiału.</summary>
    public sealed class Material
    {
        public Barwa[] Rampa;

        /// <summary>Siła faktury (szum 3D dodany do jasności).</summary>
        public double Faktura = 0.12;

        /// <summary>Gęstość faktury — ile „grudek” na jednostkę świata.</summary>
        public double Czestosc = 18;

        /// <summary>Przesunięcie jasności: ciemniejsze albo jaśniejsze tworzywo na tej samej rampie.</summary>
        public double Jasnosc;

        /// <summary>Świeci sam — nie zależy od światła, nocą nie gaśnie (ogień, okno, runa).</summary>
        public bool Swieci;

        /// <summary>
        /// Poziome pasy co tyle jednostek wysokości: bale w ścianie chaty, rzędy
        /// gontu na dachu. 0 = bez pasów.
        /// </summary>
        public double Pasy;

        /// <summary>
        /// Nalot na powierzchniach zwróconych w górę — mech na kamieniu, darń na
        /// skale. Pojawia się plamami, według faktury.
        /// </summary>
        public Barwa[] Nalot;

        public Material(Barwa[] rampa, double faktura = 0.12, double czestosc = 18, double jasnosc = 0,
                        bool swieci = false)
        {
            Rampa = rampa;
            Faktura = faktura;
            Czestosc = czestosc;
            Jasnosc = jasnosc;
            Swieci = swieci;
        }
    }

    /// <summary>Gotowy sprite: obraz i cień na gruncie, oba względem punktu podstawy.</summary>
    public sealed class SpriteBryly
    {
        public Plotno Obraz;

        /// <summary>Lewy górny róg obrazu względem ekranowego rzutu podstawy obiektu.</summary>
        public int Ox, Oy;

        public Plotno Cien;
        public int CienOx, CienOy;
    }

    /// <summary>
    /// „Fotografuje” bryłę w rzucie stylu mrocznego: promień na piksel, światło
    /// z jednego kierunku, cień własny, przesłonięcie otoczenia i cień rzucany
    /// na grunt.
    ///
    /// To mały odpowiednik tego, jak powstawały sprite'y Fallouta — model 3D
    /// renderowany raz do bitmapy w ustalonym rzucie i świetle. Barwy biorą się
    /// z ramp materiałów, a o wyborze sąsiedniego stopnia decyduje nieregularny
    /// szum pikselowy, więc obraz ma „ręczny” dithering zamiast siatki Bayera.
    /// </summary>
    public static class Fotograf
    {
        private const double Eps = 0.0018;
        private const int MaksKrokow = 96;

        /// <summary>Siła cienia rzucanego na grunt — ułamek przyciemnienia.</summary>
        private const byte AlfaCienia = 118;

        public static SpriteBryly Zrob(Bryla b, Material[] mat, W3 min, W3 max, int ziarno)
        {
            var wynik = new SpriteBryly();
            (int u0, int v0, int u1, int v1) = ProstokatRzutu(min, max, 0);
            int szer = u1 - u0 + 1;
            int wys = v1 - v0 + 1;
            var obraz = new Plotno(szer, wys);
            W3 doOka = new W3(Rzut.DoObserwatora.X, Rzut.DoObserwatora.Y, Rzut.DoObserwatora.Z);
            W3 kier = doOka * -1;
            W3 doSwiatla = new W3(Rzut.DoSwiatla.X, Rzut.DoSwiatla.Y, Rzut.DoSwiatla.Z);
            double zGora = max.Z + 0.02;

            for (int v = v0; v <= v1; v++)
            {
                for (int u = u0; u <= u1; u++)
                {
                    W3 start = PunktNaWysokosci(u + 0.5, v + 0.5, zGora);
                    if (!Przedzial(start, kier, min, max, out double t0, out double t1))
                    {
                        continue;
                    }
                    double t = t0;
                    bool trafiony = false;
                    Probka pr = default;
                    for (int k = 0; k < MaksKrokow && t <= t1; k++)
                    {
                        pr = b.Odl(start + kier * t);
                        if (pr.D < Eps)
                        {
                            trafiony = true;
                            break;
                        }
                        t += Math.Max(pr.D * 0.85, Eps * 0.6);
                    }
                    if (!trafiony)
                    {
                        continue;
                    }
                    W3 p = start + kier * t;
                    Material m = mat[Math.Max(0, Math.Min(mat.Length - 1, pr.Mat))];
                    Barwa kolor = Cieniuj(b, m, p, doSwiatla, doOka, u, v, ziarno);
                    obraz.Ustaw(u - u0, v - v0, kolor);
                }
            }
            wynik.Obraz = obraz;
            wynik.Ox = u0;
            wynik.Oy = v0;
            ZrobCien(b, min, max, doSwiatla, wynik);
            return wynik;
        }

        private static Barwa Cieniuj(Bryla b, Material m, W3 p, W3 doSwiatla, W3 doOka, int u, int v, int ziarno)
        {
            double grudka = Szum3.Wartosc(p.X * m.Czestosc, p.Y * m.Czestosc, p.Z * m.Czestosc, ziarno + 7) - 0.5;
            double szumPiks = Piksele.Szum(u, v, ziarno + 101);
            if (m.Swieci)
            {
                // Samoświecące: jasne jądro, ciemniejszy brzeg według faktury — bez światła.
                return Paleta.ZSzumem(m.Rampa, 0.75 + grudka * 0.6 + m.Jasnosc, szumPiks);
            }

            W3 n = Normalna(b, p);
            double lam = Math.Max(0.0, W3.Skal(n, doSwiatla));
            double cien = lam > 0 ? CienWlasny(b, p + n * 0.006, doSwiatla) : 0.0;
            double ao = Przeslonienie(b, p, n);
            // Krawędź sylwetki lekko przyciemniona — zamiast czarnego konturu.
            double kraw = Math.Max(0.0, W3.Skal(n, doOka));
            double s = 0.20 + 0.80 * lam * cien;
            s = s * (0.58 + 0.42 * ao) * (0.82 + 0.18 * kraw);
            s += grudka * m.Faktura + m.Jasnosc;
            if (m.Pasy > 0)
            {
                // Szczelina między balami: ciemna linia co m.Pasy wysokości, lekko falująca.
                double faza = (p.Z + grudka * m.Pasy * 0.25) / m.Pasy;
                double w = faza - Math.Floor(faza);
                if (w < 0.22)
                {
                    s -= 0.28;
                }
                else if (w > 0.78)
                {
                    s += 0.06;
                }
            }
            Barwa[] rampa = m.Rampa;
            if (m.Nalot != null && n.Z > 0.45 && grudka > -0.06)
            {
                rampa = m.Nalot;
                s -= 0.08;
            }
            return Paleta.ZSzumem(rampa, s, szumPiks);
        }

        private static W3 Normalna(Bryla b, W3 p)
        {
            const double h = 0.004;
            // Metoda czworościanu: cztery próbki zamiast sześciu.
            W3 k1 = new W3(1, -1, -1), k2 = new W3(-1, -1, 1), k3 = new W3(-1, 1, -1), k4 = new W3(1, 1, 1);
            W3 n = k1 * b.Odl(p + k1 * h).D + k2 * b.Odl(p + k2 * h).D
                   + k3 * b.Odl(p + k3 * h).D + k4 * b.Odl(p + k4 * h).D;
            return n.Norm();
        }

        /// <summary>Miękki cień od własnych części obiektu (dach na ścianę, korona na pień).</summary>
        private static double CienWlasny(Bryla b, W3 p, W3 doSwiatla)
        {
            double wynik = 1.0;
            double t = 0.01;
            for (int i = 0; i < 40 && t < 2.0; i++)
            {
                double d = b.Odl(p + doSwiatla * t).D;
                if (d < 0.0008)
                {
                    return 0.0;
                }
                wynik = Math.Min(wynik, 10.0 * d / t);
                t += Math.Max(d, 0.004);
            }
            return Math.Max(0.0, Math.Min(1.0, wynik));
        }

        /// <summary>Przesłonięcie otoczenia: wnęki i zagłębienia ciemniejsze niż powierzchnie wypukłe.</summary>
        private static double Przeslonienie(Bryla b, W3 p, W3 n)
        {
            double suma = 0;
            double waga = 1.0;
            for (int i = 1; i <= 4; i++)
            {
                double h = 0.018 * i;
                suma += (h - b.Odl(p + n * h).D) * waga;
                waga *= 0.6;
            }
            return Math.Max(0.0, Math.Min(1.0, 1.0 - 6.0 * suma));
        }

        /// <summary>
        /// Cień rzucany na grunt: dla każdego punktu gruntu w zasięgu sprawdzamy,
        /// czy droga do światła przecina obiekt. Pada w prawo i lekko w dół.
        /// </summary>
        private static void ZrobCien(Bryla b, W3 min, W3 max, W3 doSwiatla, SpriteBryly s)
        {
            // Zasięg cienia: rzut obrysu przesuniętego o tyle, ile kładzie się cień najwyższego punktu.
            double dl = max.Z / doSwiatla.Z;
            W3 przes = new W3(-doSwiatla.X * dl, -doSwiatla.Y * dl, 0);
            W3 cmin = new W3(Math.Min(min.X, min.X + przes.X), Math.Min(min.Y, min.Y + przes.Y), 0);
            W3 cmax = new W3(Math.Max(max.X, max.X + przes.X), Math.Max(max.Y, max.Y + przes.Y), 0);
            (int u0, int v0, int u1, int v1) = ProstokatRzutu(cmin, cmax, 0);
            var cien = new Plotno(u1 - u0 + 1, v1 - v0 + 1);
            bool cokolwiek = false;
            for (int v = v0; v <= v1; v++)
            {
                for (int u = u0; u <= u1; u++)
                {
                    W3 g = PunktNaWysokosci(u + 0.5, v + 0.5, 0.0);
                    if (g.X < cmin.X - 0.05 || g.X > cmax.X + 0.05 || g.Y < cmin.Y - 0.05 || g.Y > cmax.Y + 0.05)
                    {
                        continue;
                    }
                    double t = 0.004;
                    for (int i = 0; i < 48 && t < dl * 1.2 + 0.1; i++)
                    {
                        W3 q = g + doSwiatla * t;
                        if (q.Z > max.Z + 0.02)
                        {
                            break;
                        }
                        double d = b.Odl(q).D;
                        if (d < 0.003)
                        {
                            cien.Ustaw(u - u0, v - v0, new Barwa((byte)16, (byte)10, (byte)6, AlfaCienia));
                            cokolwiek = true;
                            break;
                        }
                        t += Math.Max(d, 0.005);
                    }
                }
            }
            s.Cien = cokolwiek ? cien : null;
            s.CienOx = u0;
            s.CienOy = v0;
        }

        // ------------------------------------------------------------------ //

        /// <summary>Prostokąt ekranu, w który rzutuje się prostopadłościan [min, max].</summary>
        private static (int, int, int, int) ProstokatRzutu(W3 min, W3 max, int margines)
        {
            double uMin = double.MaxValue, uMax = double.MinValue, vMin = double.MaxValue, vMax = double.MinValue;
            for (int i = 0; i < 8; i++)
            {
                double x = (i & 1) == 0 ? min.X : max.X;
                double y = (i & 2) == 0 ? min.Y : max.Y;
                double z = (i & 4) == 0 ? min.Z : max.Z;
                double u = Rzut.AX * x + Rzut.BX * y;
                double v = Rzut.AY * x + Rzut.BY * y - Rzut.Wysokosc * z;
                uMin = Math.Min(uMin, u);
                uMax = Math.Max(uMax, u);
                vMin = Math.Min(vMin, v);
                vMax = Math.Max(vMax, v);
            }
            return ((int)Math.Floor(uMin) - 1 - margines, (int)Math.Floor(vMin) - 1 - margines,
                    (int)Math.Ceiling(uMax) + 1 + margines, (int)Math.Ceiling(vMax) + 1 + margines);
        }

        /// <summary>Punkt świata na zadanej wysokości, który rzutuje się na piksel (u, v) względem podstawy.</summary>
        private static W3 PunktNaWysokosci(double u, double v, double z)
        {
            double vv = v + Rzut.Wysokosc * z;
            double det = Rzut.AX * Rzut.BY - Rzut.AY * Rzut.BX;
            double x = (Rzut.BY * u - Rzut.BX * vv) / det;
            double y = (-Rzut.AY * u + Rzut.AX * vv) / det;
            return new W3(x, y, z);
        }

        /// <summary>Odcinek promienia wewnątrz prostopadłościanu (metoda płyt).</summary>
        private static bool Przedzial(W3 o, W3 d, W3 min, W3 max, out double t0, out double t1)
        {
            t0 = 0;
            t1 = double.MaxValue;
            double[] oo = { o.X, o.Y, o.Z };
            double[] dd = { d.X, d.Y, d.Z };
            double[] mn = { min.X - 0.01, min.Y - 0.01, min.Z - 0.01 };
            double[] mx = { max.X + 0.01, max.Y + 0.01, max.Z + 0.01 };
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(dd[i]) < 1e-12)
                {
                    if (oo[i] < mn[i] || oo[i] > mx[i])
                    {
                        return false;
                    }
                    continue;
                }
                double a = (mn[i] - oo[i]) / dd[i];
                double c = (mx[i] - oo[i]) / dd[i];
                if (a > c)
                {
                    (a, c) = (c, a);
                }
                t0 = Math.Max(t0, a);
                t1 = Math.Min(t1, c);
                if (t0 > t1)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
