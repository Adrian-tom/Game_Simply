using System.Collections.Generic;
using GraSimply.Logika;

namespace GraSimply.Grafika
{
    /// <summary>Napis nad głową osadnika: pozycja na płótnie sceny, treść i barwa.</summary>
    public struct PodpisOsadnika
    {
        public int X;
        public int Y;
        public string Tekst;
        public Barwa Kolor;

        public PodpisOsadnika(int x, int y, string tekst, Barwa kolor)
        {
            X = x;
            Y = y;
            Tekst = tekst;
            Kolor = kolor;
        }
    }

    /// <summary>
    /// Styl, który umie narysować osadę z bliska — plac z chatami i chodzącymi
    /// osadnikami zamiast mapy regionu. Tylko hybrydowy ma taką scenę; pozostałe
    /// style dalej pokazują mapę.
    /// </summary>
    public interface IRysownikOsady
    {
        /// <summary>Rysuje plac na płótnie sceny. <paramref name="zmierzch"/> wymusza noc (F2).</summary>
        void RysujOsade(Plotno cel, StanOsady osada, double t, bool zmierzch);

        /// <summary>Podpisy osadników z ostatniej klatki — tekst rysuje warstwa interfejsu.</summary>
        IReadOnlyList<PodpisOsadnika> Podpisy { get; }
    }

    /// <summary>
    /// Styl, który wie, gdzie w ostatnim kadrze stoi postać gracza — interfejs
    /// stawia tam strzałki ruchu.
    /// </summary>
    public interface IKadrPostaci
    {
        /// <summary>Gdzie w ostatnim kadrze stoi postać (podstawa) albo null.</summary>
        (int X, int Y)? PozycjaPostaci { get; }

        /// <summary>Punkt świata w pikselach ostatniego kadru.</summary>
        (int X, int Y) NaKadr(double wx, double wy, double wz = 0);
    }
}
