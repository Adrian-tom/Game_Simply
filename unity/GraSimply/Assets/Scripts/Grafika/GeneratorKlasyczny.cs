using System;
using System.Collections.Generic;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Styl klasyczny: dokładnie to, co rysowała wersja pythonowa.
    ///
    /// Cienka nakładka na <see cref="Teren"/> i liczby przeniesione wprost
    /// z <c>grafika/scena.py</c> — przesunięcia postaci, osady i światła są
    /// tutaj takie same co do piksela, bo na nich stoją testy zgodności.
    /// Nie poprawiać „przy okazji”: każda zmiana tu to zmiana wyglądu
    /// pierwowzoru, a ten ma zostać.
    /// </summary>
    public sealed class GeneratorKlasyczny : IGeneratorTerenu
    {
        public static readonly GeneratorKlasyczny Instancja = new GeneratorKlasyczny();

        private Plotno _cien;

        private GeneratorKlasyczny()
        {
        }

        public int TW => Teren.TW;
        public int TH => Teren.TH;
        public int SzerSceny => 320;
        public int WysSceny => 240;
        public int SkalaSceny => 2;
        public int Y0 => 46;

        public int WysokoscPola(string biom, int x, int y, int s)
        {
            return Teren.WysokoscPola(biom, x, y, s);
        }

        public int Klatki(string biom)
        {
            return biom == "bagna" ? 3 : 1;
        }

        public Plotno Kafel(string biom, int h, int s, int klatka)
        {
            return Teren.Kafel(biom, h, s, klatka);
        }

        public Plotno Zamglij(Plotno kafel, int x, int y, bool noc)
        {
            return Teren.Zamglij(kafel, x, y, noc);
        }

        public Plotno Obwodka()
        {
            return Teren.Obwodka();
        }

        public List<Dekoracja> Dekoracje(string biom, string punkt, int s)
        {
            return Teren.Dekoracje(biom, punkt, s);
        }

        public Plotno OsadaObozu(int chaty, int palisada, int wieza)
        {
            return Teren.OsadaObozu(chaty, palisada, wieza);
        }

        public (int Dx, int Dy) KotwicaOsady => (-26, -30);

        public void Ognisko(Plotno cel, int x, int y, double t)
        {
            Teren.Ognisko(cel, x, y, t);
        }

        public int SwiatloOgniskaDy => -2;

        public Dictionary<string, (string Rodzaj, int Dx, int Dy)> SwiatlaPunktow => Teren.SwiatlaPunktow;

        public (int X, int Y) PozycjaGracza((int X, int Y) srodek, bool naPunkcie)
        {
            return (srodek.X - 4 + (naPunkcie ? 7 : 0), srodek.Y - 11 + (naPunkcie ? 3 : 0));
        }

        public (int Dx, int Dy) SwiatloGracza => (4, 6);

        public void RysujGracza(Plotno cel, int px, int py, string klasa, double t)
        {
            if (_cien == null)
            {
                _cien = new Plotno(12, 5);
                _cien.Elipsa(new Barwa(0, 0, 0, 90), 0, 0, 12, 5);
            }
            int bob = (int)(Math.Abs(Math.Sin(t * 3)) * 1.5);
            cel.Nalóż(_cien, px - 1, py + 10);
            cel.Nalóż(Teren.SpriteGracza(klasa), px, py - bob);
            int my = py - 7 - (int)(Math.Abs(Math.Sin(t * 2.2)) * 2);
            var szerokosci = new[] { 5, 3, 1 };
            for (int i = 0; i < szerokosci.Length; i++)
            {
                // złoty grot nad głową
                int szer = szerokosci[i];
                cel.Wypelnij(new Barwa(236, 200, 110), px + 4 - szer / 2, my + i, szer, 1);
            }
            cel.Wypelnij(new Barwa(120, 90, 40), px + 2, my - 1, 5, 1);
        }

        public Plotno Tlo(bool zmierzch)
        {
            Styl s = Styl.Aktywny;
            return zmierzch
                ? Piksele.GradientNieba(SzerSceny, WysSceny, s.NieboGoraZmierzch, s.NieboDolZmierzch)
                : Piksele.GradientNieba(SzerSceny, WysSceny, s.NieboGora, s.NieboDol);
        }

        public Plotno Winieta()
        {
            return Piksele.Winieta(SzerSceny, WysSceny);
        }

        public void Obrobka(Plotno cel, double t)
        {
            // Pierwowzór nie miał obróbki końcowej.
        }
    }
}
