using System;
using System.Collections.Generic;
using System.Text;
using GraSimply.Grafika;
using GraSimply.Grafika.Trojwymiar;
using GraSimply.Logika;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Mapa regionu w 3D: każde pole gry to kwadrat <see cref="PolePola"/>×<see cref="PolePola"/> z gruntem w kolorze
    /// biomu, roślinnością i punktem orientacyjnym. Nieodkryte pola zostają ciemne i puste. Obsługuje też strzałki
    /// ruchu interfejsu (<see cref="IKadrPostaci"/>), bo tylko scena wie, gdzie na ekranie stoi bohater.
    /// </summary>
    public sealed class Mapa3D : IKadrPostaci
    {
        public const float PolePola = 4f;
        private const float RozmiarKadru = 11.5f;
        private const int Gestosc = 8;        // tekseli gruntu na pole
        private const double Szybkosc = 14.0;

        private readonly Scena3D _scena;
        private readonly Transform _korzen;
        private readonly Transform _obiekty;
        private readonly List<GameObject> _stoi = new List<GameObject>();

        private MeshRenderer _grunt;
        private Texture2D _tekstura;
        private string _klucz;
        private Postac3D _bohater;
        private double _hx, _hy;
        private bool _ustawiony;

        public (int X, int Y)? PozycjaPostaci { get; private set; }

        public Mapa3D(Scena3D scena)
        {
            _scena = scena;
            _korzen = new GameObject("Mapa").transform;
            _korzen.SetParent(scena.Korzen, false);
            _obiekty = new GameObject("Obiekty").transform;
            _obiekty.SetParent(_korzen, false);
        }

        public void Pokaz(bool tak)
        {
            _korzen.gameObject.SetActive(tak);
            if (!tak)
            {
                _ustawiony = false;
            }
        }

        public (int X, int Y) NaKadr(double wx, double wy, double wz = 0)
        {
            return _scena.NaPulpit(Scena3D.NaSwiat(wx * PolePola, wy * PolePola, wz)) ?? (0, 0);
        }

        public void Aktualizuj(Migawka m, double t, double dt, bool zmierzch)
        {
            Pokaz(true);
            PoleDoRysunku[][] pola = m.Pola;
            int n = pola.Length;
            string klucz = Klucz(m);
            if (klucz != _klucz)
            {
                _klucz = klucz;
                Zbuduj(m);
            }

            (int X, int Y) cel = m.GraczXy ?? (n / 2, n / 2);
            double cx = (cel.X + 0.5) * PolePola, cy = (cel.Y + 0.5) * PolePola;
            if (!_ustawiony)
            {
                _hx = cx;
                _hy = cy;
                _ustawiony = true;
            }
            double dx = cx - _hx, dy = cy - _hy, odl = Math.Sqrt(dx * dx + dy * dy);
            double krok = Szybkosc * dt;
            bool idzie = odl > 0.01;
            if (idzie)
            {
                double k = Math.Min(1.0, krok / odl);
                _hx += dx * k;
                _hy += dy * k;
            }
            _scena.Patrz(_hx, _hy, RozmiarKadru);
            _scena.Oswietl(zmierzch ? 0.15 : 1.0);
            _scena.Migocz(t);

            string model = KatalogModeli.PostacKlasy(m.Klasa);
            if (m.GraczXy.HasValue)
            {
                if (_bohater == null || _bohater.Model != model)
                {
                    if (_bohater != null)
                    {
                        _scena.Modele.Oddaj(_bohater.Obiekt);
                    }
                    _bohater = new Postac3D(_scena.Modele, model, _korzen, 1.9f);
                }
                _bohater.Obiekt.SetActive(true);
                _bohater.Ustaw(_hx, _hy, dx, dy);
                _bohater.Graj(idzie ? Postac3D.Bieg : Postac3D.Spoczynek);
                PozycjaPostaci = _scena.NaPulpit(Scena3D.NaSwiat(_hx, _hy));
            }
            else
            {
                _bohater?.Obiekt.SetActive(false);
                PozycjaPostaci = null;
            }
        }

        private static string Klucz(Migawka m)
        {
            var k = new StringBuilder();
            k.Append(m.Region).Append(m.WszystkoOdkryte).Append(m.Osada);
            foreach (PoleDoRysunku[] w in m.Pola)
            {
                foreach (PoleDoRysunku p in w)
                {
                    k.Append(p.Odkryte ? '1' : '0').Append(p.Biom).Append(':').Append(p.Punkt).Append(';');
                }
            }
            return k.ToString();
        }

        private void Zbuduj(Migawka m)
        {
            foreach (GameObject o in _stoi)
            {
                _scena.Modele.Oddaj(o);
            }
            _stoi.Clear();

            PoleDoRysunku[][] pola = m.Pola;
            int n = pola.Length;
            int margines = 3;                  // pola za krawędzią regionu: mapa nie urywa się w próżni
            int bok = (n + 2 * margines) * Gestosc;
            if (_tekstura == null || _tekstura.width != bok)
            {
                _tekstura = new Texture2D(bok, bok, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _grunt = _scena.NowyGrunt("GruntMapy", _tekstura);
                _grunt.transform.SetParent(_korzen, false);
            }
            var piksele = new Color32[bok * bok];
            var ciemno = new Color32(16, 20, 26, 255);
            for (int py = 0; py < bok; py++)
            {
                for (int px = 0; px < bok; px++)
                {
                    int x = px / Gestosc - margines, y = py / Gestosc - margines;
                    if (x < 0 || y < 0 || x >= n || y >= n || !(m.WszystkoOdkryte || pola[y][x].Odkryte))
                    {
                        piksele[py * bok + px] = ciemno;
                        continue;
                    }
                    string biom = string.IsNullOrEmpty(pola[y][x].Biom) ? "równiny" : pola[y][x].Biom;
                    double odcien = KatalogModeli.Mieszaj(px + m.Region.X * 7, py + m.Region.Y * 13) % 100 / 100.0;
                    piksele[py * bok + px] = Scena3D.Kolor(KatalogModeli.KolorGruntu(biom, odcien));
                }
            }
            _tekstura.SetPixels32(piksele);
            _tekstura.Apply(false);
            Scena3D.UstawProstokat(_grunt, -margines * PolePola, -margines * PolePola,
                                   (n + margines) * PolePola, (n + margines) * PolePola);

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    PoleDoRysunku p = pola[y][x];
                    if (m.WszystkoOdkryte || p.Odkryte)
                    {
                        Pole(m, p, x, y);
                    }
                }
            }
        }

        private void Pole(Migawka m, PoleDoRysunku p, int x, int y)
        {
            string biom = string.IsNullOrEmpty(p.Biom) ? "równiny" : p.Biom;
            bool punkt = !string.IsNullOrEmpty(p.Punkt);
            int rx = x + m.Region.X * 31, ry = y + m.Region.Y * 17;
            foreach ((WpisModelu model, float u, float v) in KatalogModeli.Roslinnosc(biom, rx, ry))
            {
                // Środek pola zostaje wolny dla bohatera i punktu orientacyjnego.
                if (Math.Abs(u - 0.5f) < (punkt ? 0.34f : 0.16f) && Math.Abs(v - 0.5f) < (punkt ? 0.34f : 0.16f))
                {
                    continue;
                }
                Postaw(model, (x + u) * PolePola, (y + v) * PolePola, KatalogModeli.Mieszaj(rx, ry + (int)(u * 100)) % 360);
            }
            if (!punkt)
            {
                return;
            }
            double cx = (x + 0.5) * PolePola, cy = (y + 0.5) * PolePola;
            Postaw(KatalogModeli.DlaPunktu(p.Punkt), cx - 1.1, cy - 1.1, 0f);
            if (p.Punkt == "obóz")
            {
                GameObject ognisko = Postaw(new WpisModelu("Oboz/campfire-pit", 0.4f), cx + 1.1, cy - 0.3, 0f);
                if (ognisko.transform.Find("Blask") == null)
                {
                    _scena.DodajOgien(ognisko.transform, 0.7f);
                }
                int chaty = m.Osada?.Chaty ?? 0;
                var miejsca = new[] { (-1.2, 0.8), (1.0, -1.2), (-1.5, -0.8) };
                for (int i = 0; i < Math.Min(chaty, miejsca.Length); i++)
                {
                    Postaw(new WpisModelu(KatalogModeli.Chata, 1.5f), cx + miejsca[i].Item1, cy + miejsca[i].Item2, 0f);
                }
            }
        }

        private GameObject Postaw(WpisModelu model, double x, double y, float obrot)
        {
            GameObject o = _scena.Modele.Wez(model, _obiekty);
            o.transform.position = Scena3D.NaSwiat(x, y);
            o.transform.rotation = Quaternion.Euler(0f, obrot, 0f);
            _stoi.Add(o);
            return o;
        }
    }
}
