using System.Collections.Generic;
using GraSimply.Grafika.Mroczny;
using GraSimply.Logika;

namespace GraSimply.Grafika.Hybryda
{
    /// <summary>
    /// Styl „Hybrydowy”: pixelart i Mroczny w jednym obrazie.
    ///
    /// Z Mrocznego — rzut skośny jak w Falloucie, ciągły grunt gasnący w czerń,
    /// noc, która ma tylko światła ognia i okien, wspólna paleta i cały
    /// interfejs fantasy. Z klasycznego — obiekty jako pixelart: chaty, namiot
    /// i ludziki rysowane z faktur i linijek tekstu, powiększone ×2.
    ///
    /// Mapa regionu jest rysowana generatorem Mrocznego (ten sam grunt,
    /// światło i paleta); scena osady z bliska — <see cref="ScenaOsady"/>.
    /// </summary>
    public sealed class GeneratorHybrydowy : IRysownikRegionu, IRysownikOsady, IKadrPostaci
    {
        private readonly GeneratorMroczny _region = new GeneratorMroczny();
        private readonly ScenaOsady _osada = new ScenaOsady();

        public int SzerSceny => _region.SzerSceny;

        public int WysSceny => _region.WysSceny;

        public int SkalaSceny => _region.SkalaSceny;

        /// <summary>Scena osady — do testów i podglądu.</summary>
        public ScenaOsady Osada => _osada;

        public void Rysuj(Plotno cel, PoleDoRysunku[][] pola, double t, (int X, int Y) region,
                          (int X, int Y)? graczXy, string klasa, bool wszystkoOdkryte,
                          (int Chaty, int Palisada, int Wieza)? osada, bool zmierzch)
        {
            _region.Rysuj(cel, pola, t, region, graczXy, klasa, wszystkoOdkryte, osada, zmierzch);
        }

        public void RysujOsade(Plotno cel, StanOsady osada, double t, bool zmierzch)
        {
            _osada.Rysuj(cel, osada, t, zmierzch);
        }

        public IReadOnlyList<PodpisOsadnika> Podpisy => _osada.Podpisy;

        public (int X, int Y)? PozycjaPostaci => _region.PozycjaPostaci;

        public (int X, int Y) NaKadr(double wx, double wy, double wz = 0)
        {
            return _region.NaKadr(wx, wy, wz);
        }
    }
}
