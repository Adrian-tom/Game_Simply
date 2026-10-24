using System;
using System.Collections.Generic;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>Wektor w układzie świata (pola, z w górę).</summary>
    public struct W3
    {
        public double X, Y, Z;

        public W3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static W3 operator +(W3 a, W3 b) => new W3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static W3 operator -(W3 a, W3 b) => new W3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static W3 operator *(W3 a, double k) => new W3(a.X * k, a.Y * k, a.Z * k);
        public static W3 operator *(double k, W3 a) => new W3(a.X * k, a.Y * k, a.Z * k);

        public double Dl => Math.Sqrt(X * X + Y * Y + Z * Z);

        public static double Skal(W3 a, W3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public W3 Norm()
        {
            double d = Dl;
            return d > 1e-12 ? this * (1.0 / d) : new W3(0, 0, 1);
        }
    }

    /// <summary>Wynik odpytania bryły: odległość i materiał najbliższej powierzchni.</summary>
    public struct Probka
    {
        public double D;
        public int Mat;

        public Probka(double d, int mat)
        {
            D = d;
            Mat = mat;
        }
    }

    /// <summary>
    /// Bryła opisana funkcją odległości (SDF): dla punktu zwraca, jak daleko jest
    /// do powierzchni i z jakiego materiału ta powierzchnia jest.
    ///
    /// Tak opisany obiekt da się złożyć z prostych kawałków (pień = walec,
    /// korona = zlepione kule, dach = graniastosłup), zaokrąglić połączenia
    /// i nadać mu fakturę przez zaburzenie odległości szumem — a potem
    /// „sfotografować” promieniami. Stąd bierze się gęsty, spójnie oświetlony
    /// detal, jaki Fallout miał dzięki prerenderowi z 3D.
    /// </summary>
    public abstract class Bryla
    {
        public abstract Probka Odl(W3 p);

        // ---- składanie ---------------------------------------------------- //

        public static Bryla Suma(params Bryla[] czesci) => new SumaBryl(czesci, 0);

        /// <summary>Suma z zaokrąglonym połączeniem — gałąź wrasta w pień, a nie przecina go.</summary>
        public static Bryla Zlep(double k, params Bryla[] czesci) => new SumaBryl(czesci, k);

        public Bryla Minus(Bryla inna) => new RoznicaBryl(this, inna);

        public Bryla Przetnij(Bryla inna) => new PrzeciecieBryl(this, inna);

        /// <summary>Faktura: powierzchnia zaburzona szumem 3D (kora, kępy liści, skała).</summary>
        public Bryla Chropowata(double amplituda, double czestosc, int ziarno) =>
            new ChropowataBryla(this, amplituda, czestosc, ziarno);

        public Bryla Przesun(double x, double y, double z) => new PrzesunietaBryla(this, new W3(x, y, z));

        /// <summary>Obrót wokół osi pionowej (radiany) — do budynków ustawionych pod kątem.</summary>
        public Bryla Obroc(double kat) => new ObroconaBryla(this, kat);

        /// <summary>Jednolite powiększenie względem początku układu (podstawy obiektu).</summary>
        public Bryla Skaluj(double k) => new SkalowanaBryla(this, k);

        // ---- prymitywy ---------------------------------------------------- //

        public static Bryla Kula(double x, double y, double z, double r, int mat) =>
            new KulaB(new W3(x, y, z), r, mat);

        public static Bryla Elipsoida(double x, double y, double z, double rx, double ry, double rz, int mat) =>
            new ElipsoidaB(new W3(x, y, z), new W3(rx, ry, rz), mat);

        /// <summary>Kapsuła — odcinek z promieniem. Gałęzie, kończyny, belki.</summary>
        public static Bryla Kapsula(W3 a, W3 b, double r, int mat) => new KapsulaB(a, b, r, r, mat);

        /// <summary>Kapsuła zwężająca się od ra do rb — pień, konar.</summary>
        public static Bryla Stozkowa(W3 a, W3 b, double ra, double rb, int mat) => new KapsulaB(a, b, ra, rb, mat);

        public static Bryla Prostopadloscian(double x, double y, double z, double px, double py, double pz,
                                             int mat, double zaokraglenie = 0) =>
            new ProstopadloscianB(new W3(x, y, z), new W3(px, py, pz), zaokraglenie, mat);

        /// <summary>Pionowy stożek o podstawie w (x, y, z).</summary>
        public static Bryla Stozek(double x, double y, double z, double r, double h, int mat) =>
            new StozekB(new W3(x, y, z), r, h, mat);

        /// <summary>
        /// Dach dwuspadowy: graniastosłup trójkątny o grzbiecie wzdłuż osi x,
        /// o środku podstawy w (x, y, z), połowie długości dx, połowie szerokości dy
        /// i wysokości h.
        /// </summary>
        public static Bryla Dach(double x, double y, double z, double dx, double dy, double h, int mat) =>
            new DachB(new W3(x, y, z), dx, dy, h, mat);
    }

    // ---------------------------------------------------------------------- //
    //  Implementacje                                                          //
    // ---------------------------------------------------------------------- //

    internal sealed class SumaBryl : Bryla
    {
        private readonly Bryla[] _czesci;
        private readonly double _k;

        public SumaBryl(Bryla[] czesci, double k)
        {
            _czesci = czesci;
            _k = k;
        }

        public override Probka Odl(W3 p)
        {
            Probka wynik = _czesci[0].Odl(p);
            for (int i = 1; i < _czesci.Length; i++)
            {
                Probka b = _czesci[i].Odl(p);
                if (_k <= 0)
                {
                    if (b.D < wynik.D)
                    {
                        wynik = b;
                    }
                    continue;
                }
                // Gładkie minimum (wielomianowe) — materiał bierze bliższa część.
                double h = Math.Max(0.0, Math.Min(1.0, 0.5 + 0.5 * (b.D - wynik.D) / _k));
                double d = b.D * (1 - h) + wynik.D * h - _k * h * (1 - h);
                wynik = new Probka(d, b.D < wynik.D ? b.Mat : wynik.Mat);
            }
            return wynik;
        }
    }

    internal sealed class RoznicaBryl : Bryla
    {
        private readonly Bryla _a, _b;

        public RoznicaBryl(Bryla a, Bryla b)
        {
            _a = a;
            _b = b;
        }

        public override Probka Odl(W3 p)
        {
            Probka a = _a.Odl(p);
            Probka b = _b.Odl(p);
            return -b.D > a.D ? new Probka(-b.D, b.Mat) : a;
        }
    }

    internal sealed class PrzeciecieBryl : Bryla
    {
        private readonly Bryla _a, _b;

        public PrzeciecieBryl(Bryla a, Bryla b)
        {
            _a = a;
            _b = b;
        }

        public override Probka Odl(W3 p)
        {
            Probka a = _a.Odl(p);
            Probka b = _b.Odl(p);
            return b.D > a.D ? new Probka(b.D, a.Mat) : a;
        }
    }

    internal sealed class ChropowataBryla : Bryla
    {
        private readonly Bryla _b;
        private readonly double _amp, _czest;
        private readonly int _ziarno;

        public ChropowataBryla(Bryla b, double amp, double czest, int ziarno)
        {
            _b = b;
            _amp = amp;
            _czest = czest;
            _ziarno = ziarno;
        }

        public override Probka Odl(W3 p)
        {
            Probka r = _b.Odl(p);
            // Daleko od powierzchni szum nie ma znaczenia — oszczędzamy liczenie.
            if (r.D > _amp * 3)
            {
                return r;
            }
            double n = Szum3.Wartosc(p.X * _czest, p.Y * _czest, p.Z * _czest, _ziarno)
                       + 0.5 * Szum3.Wartosc(p.X * _czest * 2.3, p.Y * _czest * 2.3, p.Z * _czest * 2.3, _ziarno + 1);
            return new Probka(r.D + (n / 1.5 - 0.5) * _amp, r.Mat);
        }
    }

    internal sealed class PrzesunietaBryla : Bryla
    {
        private readonly Bryla _b;
        private readonly W3 _o;

        public PrzesunietaBryla(Bryla b, W3 o)
        {
            _b = b;
            _o = o;
        }

        public override Probka Odl(W3 p) => _b.Odl(p - _o);
    }

    internal sealed class SkalowanaBryla : Bryla
    {
        private readonly Bryla _b;
        private readonly double _k;

        public SkalowanaBryla(Bryla b, double k)
        {
            _b = b;
            _k = k;
        }

        public override Probka Odl(W3 p)
        {
            Probka r = _b.Odl(p * (1.0 / _k));
            return new Probka(r.D * _k, r.Mat);
        }
    }

    internal sealed class ObroconaBryla : Bryla
    {
        private readonly Bryla _b;
        private readonly double _c, _s;

        public ObroconaBryla(Bryla b, double kat)
        {
            _b = b;
            _c = Math.Cos(-kat);
            _s = Math.Sin(-kat);
        }

        public override Probka Odl(W3 p) => _b.Odl(new W3(p.X * _c - p.Y * _s, p.X * _s + p.Y * _c, p.Z));
    }

    internal sealed class KulaB : Bryla
    {
        private readonly W3 _c;
        private readonly double _r;
        private readonly int _m;

        public KulaB(W3 c, double r, int m)
        {
            _c = c;
            _r = r;
            _m = m;
        }

        public override Probka Odl(W3 p) => new Probka((p - _c).Dl - _r, _m);
    }

    internal sealed class ElipsoidaB : Bryla
    {
        private readonly W3 _c, _r;
        private readonly int _m;

        public ElipsoidaB(W3 c, W3 r, int m)
        {
            _c = c;
            _r = r;
            _m = m;
        }

        public override Probka Odl(W3 p)
        {
            W3 q = p - _c;
            double k0 = new W3(q.X / _r.X, q.Y / _r.Y, q.Z / _r.Z).Dl;
            double k1 = new W3(q.X / (_r.X * _r.X), q.Y / (_r.Y * _r.Y), q.Z / (_r.Z * _r.Z)).Dl;
            return new Probka(k1 > 1e-9 ? k0 * (k0 - 1.0) / k1 : -Math.Min(_r.X, Math.Min(_r.Y, _r.Z)), _m);
        }
    }

    internal sealed class KapsulaB : Bryla
    {
        private readonly W3 _a, _b;
        private readonly double _ra, _rb;
        private readonly int _m;

        public KapsulaB(W3 a, W3 b, double ra, double rb, int m)
        {
            _a = a;
            _b = b;
            _ra = ra;
            _rb = rb;
            _m = m;
        }

        public override Probka Odl(W3 p)
        {
            W3 pa = p - _a;
            W3 ba = _b - _a;
            double h = Math.Max(0.0, Math.Min(1.0, W3.Skal(pa, ba) / Math.Max(1e-12, W3.Skal(ba, ba))));
            return new Probka((pa - ba * h).Dl - (_ra + (_rb - _ra) * h), _m);
        }
    }

    internal sealed class ProstopadloscianB : Bryla
    {
        private readonly W3 _c, _p;
        private readonly double _z;
        private readonly int _m;

        public ProstopadloscianB(W3 c, W3 p, double zaokraglenie, int m)
        {
            _c = c;
            _p = p;
            _z = zaokraglenie;
            _m = m;
        }

        public override Probka Odl(W3 p)
        {
            W3 q = p - _c;
            double qx = Math.Abs(q.X) - _p.X + _z;
            double qy = Math.Abs(q.Y) - _p.Y + _z;
            double qz = Math.Abs(q.Z) - _p.Z + _z;
            double zew = new W3(Math.Max(qx, 0), Math.Max(qy, 0), Math.Max(qz, 0)).Dl;
            double wew = Math.Min(Math.Max(qx, Math.Max(qy, qz)), 0.0);
            return new Probka(zew + wew - _z, _m);
        }
    }

    internal sealed class StozekB : Bryla
    {
        private readonly W3 _podstawa;
        private readonly double _r, _h;
        private readonly int _m;

        public StozekB(W3 podstawa, double r, double h, int m)
        {
            _podstawa = podstawa;
            _r = r;
            _h = h;
            _m = m;
        }

        public override Probka Odl(W3 p)
        {
            W3 q = p - _podstawa;
            double rho = Math.Sqrt(q.X * q.X + q.Y * q.Y);
            // Odległość od tworzącej (przybliżenie przez rzut na prostą) i od podstawy.
            double dl = Math.Sqrt(_r * _r + _h * _h);
            double bok = (rho * _h + (q.Z - _h) * _r) / dl;
            double dol = -q.Z;
            return new Probka(Math.Max(bok, dol), _m);
        }
    }

    internal sealed class DachB : Bryla
    {
        private readonly W3 _c;
        private readonly double _dx, _dy, _h;
        private readonly int _m;

        public DachB(W3 c, double dx, double dy, double h, int m)
        {
            _c = c;
            _dx = dx;
            _dy = dy;
            _h = h;
            _m = m;
        }

        public override Probka Odl(W3 p)
        {
            W3 q = p - _c;
            double dl = Math.Sqrt(_dy * _dy + _h * _h);
            // Dwie połacie: płaszczyzny przez grzbiet, plus dół i szczyty.
            double polac = (Math.Abs(q.Y) * _h + (q.Z - _h) * _dy) / dl;
            double dol = -q.Z;
            double szczyt = Math.Abs(q.X) - _dx;
            return new Probka(Math.Max(Math.Max(polac, dol), szczyt), _m);
        }
    }

    /// <summary>Szum wartości 3D — faktura brył.</summary>
    public static class Szum3
    {
        private static double Hash(int x, int y, int z, int s)
        {
            ulong n = (ulong)((long)x * 374761393L + (long)y * 668265263L + (long)z * 2147483647L
                              + (long)s * 982451653L) & 0xFFFFFFFFUL;
            n = ((n ^ (n >> 13)) * 1274126177UL) & 0xFFFFFFFFUL;
            return ((n ^ (n >> 16)) & 0xFFFFUL) / 65535.0;
        }

        public static double Wartosc(double x, double y, double z, int s)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y), z0 = (int)Math.Floor(z);
            double fx = x - x0, fy = y - y0, fz = z - z0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            fz = fz * fz * (3 - 2 * fz);
            double Lerp(double a, double b, double t) => a + (b - a) * t;
            double c000 = Hash(x0, y0, z0, s), c100 = Hash(x0 + 1, y0, z0, s);
            double c010 = Hash(x0, y0 + 1, z0, s), c110 = Hash(x0 + 1, y0 + 1, z0, s);
            double c001 = Hash(x0, y0, z0 + 1, s), c101 = Hash(x0 + 1, y0, z0 + 1, s);
            double c011 = Hash(x0, y0 + 1, z0 + 1, s), c111 = Hash(x0 + 1, y0 + 1, z0 + 1, s);
            return Lerp(Lerp(Lerp(c000, c100, fx), Lerp(c010, c110, fx), fy),
                        Lerp(Lerp(c001, c101, fx), Lerp(c011, c111, fx), fy), fz);
        }
    }
}
