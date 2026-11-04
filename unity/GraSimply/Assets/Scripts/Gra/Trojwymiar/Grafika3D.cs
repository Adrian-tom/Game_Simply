using System;
using System.Collections.Generic;
using GraSimply.Grafika;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>Scena 3D całej gry: wybiera, który widok pokazać (otwarty świat, plac osady, mapa regionu).</summary>
    public sealed class Grafika3D
    {
        public readonly Scena3D Scena;

        private readonly Wolny3D _wolny;
        private readonly Osada3D _osada;
        private readonly Mapa3D _mapa;

        public Grafika3D()
        {
            Scena = new Scena3D();
            _wolny = new Wolny3D(Scena);
            _osada = new Osada3D(Scena);
            _mapa = new Mapa3D(Scena);
            _wolny.Pokaz(false);
            _osada.Pokaz(false);
            _mapa.Pokaz(false);
        }

        public IReadOnlyList<PodpisOsadnika> PodpisyWolnego => _wolny.Podpisy;
        public IReadOnlyList<PodpisOsadnika> PodpisyOsady => _osada.Podpisy;
        public (int X, int Y, double Postep)? PasekZbioru => _wolny.PasekZbioru;
        public IKadrPostaci Kadr => _mapa;

        public void RysujWolny(SwiatWolny swiat, double t, double dt, string klasa)
        {
            _osada.Pokaz(false);
            _mapa.Pokaz(false);
            _wolny.Aktualizuj(swiat, t, dt, klasa);
        }

        public void Rysuj(Migawka m, bool plac, double t, double dt, bool zmierzch)
        {
            _wolny.Pokaz(false);
            if (plac)
            {
                _mapa.Pokaz(false);
                _osada.Aktualizuj(m.Plac, t, zmierzch);
            }
            else
            {
                _osada.Pokaz(false);
                _mapa.Aktualizuj(m, t, dt, zmierzch);
            }
        }

        public (int X, int Y) KafelPodKursorem(Vector2 ekran)
        {
            (double x, double y) = Scena.PodKursorem(ekran);
            return ((int)Math.Floor(x), (int)Math.Floor(y));
        }
    }
}
