using System;
using System.Collections.Generic;
using GraSimply.Grafika;
using GraSimply.Grafika.Trojwymiar;
using GraSimply.Logika.Wolny;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Otwarty świat „Zomboid fantasy” w 3D. Nie ma własnego stanu gry: co klatkę czyta <see cref="SwiatWolny"/>
    /// i dopasowuje do niego obiekty w oknie wokół bohatera. Świat jest ogromny, więc na scenie stoi tylko to okno.
    /// </summary>
    public sealed class Wolny3D
    {
        public const float RozmiarKadru = 6.6f;
        private const int Zasieg = 17;          // pól od bohatera: tyle mieści kadr z zapasem
        private const double OdswiezenieS = 0.25;

        private readonly Scena3D _scena;
        private readonly Transform _korzen;
        private readonly Grunt3D _grunt;
        private readonly Trawnik _trawnik;

        private readonly Dictionary<(int X, int Y), (string Klucz, GameObject Obiekt)> _stoi =
            new Dictionary<(int X, int Y), (string Klucz, GameObject Obiekt)>();

        private readonly Dictionary<PostacOsadnika, Postac3D> _osadnicy = new Dictionary<PostacOsadnika, Postac3D>();
        private readonly List<PodpisOsadnika> _podpisy = new List<PodpisOsadnika>();

        private Postac3D _bohater;
        private Light _latarnia;
        private GameObject _duch;
        private string _kluczDucha;
        private (int X, int Y) _kafel = (int.MaxValue, int.MaxValue);
        private double _odswiez;
        private double _px, _py;

        public IReadOnlyList<PodpisOsadnika> Podpisy => _podpisy;

        /// <summary>Pasek zbierania nad obiektem (piksele natywne, postęp) albo null.</summary>
        public (int X, int Y, double Postep)? PasekZbioru { get; private set; }

        public Wolny3D(Scena3D scena)
        {
            _scena = scena;
            _korzen = new GameObject("Wolny").transform;
            _korzen.SetParent(scena.Korzen, false);
            _grunt = scena.NowyGrunt("GruntWolny", 2 * Zasieg + 1, _korzen);
            _trawnik = new Trawnik(scena.Modele, _korzen);
        }

        public void Pokaz(bool tak)
        {
            _korzen.gameObject.SetActive(tak);
        }

        public void Aktualizuj(SwiatWolny s, double t, double dt, string klasa)
        {
            Pokaz(true);
            _scena.Patrz(s.X, s.Y, RozmiarKadru);
            _scena.Oswietl(s.Spi ? Math.Min(s.Jasnosc, 0.35) : s.Jasnosc);

            (int X, int Y) kafel = ((int)Math.Floor(s.X), (int)Math.Floor(s.Y));
            _odswiez -= dt;
            if (kafel != _kafel || _odswiez <= 0)
            {
                bool ruch = kafel != _kafel;
                _kafel = kafel;
                _odswiez = OdswiezenieS;
                if (ruch)
                {
                    Grunt(s, kafel);
                    Kepki(s, kafel);
                }
                Obiekty(s, kafel);
            }

            Bohater(s, klasa, dt);
            Osadnicy(s);
            Duch(s);
            _scena.Migocz(t);

            PasekZbioru = null;
            if (s.Zbieranie.HasValue)
            {
                (int X, int Y)? p = _scena.NaPulpit(Scena3D.NaSwiat(s.Zbieranie.Value.X + 0.5, s.Zbieranie.Value.Y + 0.5, 1.6));
                if (p.HasValue)
                {
                    PasekZbioru = (p.Value.X, p.Value.Y, s.Zbieranie.Value.Postep);
                }
            }
        }

        private void Grunt(SwiatWolny s, (int X, int Y) c)
        {
            int bok = 2 * Zasieg + 1;
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    int tx = c.X - Zasieg + x, ty = c.Y - Zasieg + y;
                    string biom = !GeneratorSwiata.WGranicach(tx, ty) ? "ruiny"
                                : s.ObiektNa(tx, ty) == Obiekt.Woda ? "woda" : s.Swiat.Biom(tx, ty);
                    _grunt.Pole(x, y, biom, s.Swiat.Odcien(tx, ty));
                }
            }
            _grunt.Zastosuj();
            // Środki tekseli wypadają w środkach pól, więc kolory zlewają się płynnie między sąsiadami.
            _grunt.Prostokat(c.X - Zasieg, c.Y - Zasieg, c.X + Zasieg + 1, c.Y + Zasieg + 1);
        }

        private void Kepki(SwiatWolny s, (int X, int Y) c)
        {
            _trawnik.Wyczysc();
            for (int y = c.Y - Zasieg; y <= c.Y + Zasieg; y++)
            {
                for (int x = c.X - Zasieg; x <= c.X + Zasieg; x++)
                {
                    if (!GeneratorSwiata.WGranicach(x, y) || s.ObiektNa(x, y) == Obiekt.Woda)
                    {
                        continue;
                    }
                    foreach ((string sciezka, float u, float v, float h) in KatalogModeli.Kepki(s.Swiat.Biom(x, y), x, y))
                    {
                        _trawnik.Dodaj(sciezka, x + u, y + v, h);
                    }
                }
            }
            _trawnik.Zbuduj();
        }

        private void Obiekty(SwiatWolny s, (int X, int Y) c)
        {
            var doUsuniecia = new List<(int X, int Y)>();
            foreach (KeyValuePair<(int X, int Y), (string Klucz, GameObject Obiekt)> w in _stoi)
            {
                if (Math.Abs(w.Key.X - c.X) > Zasieg || Math.Abs(w.Key.Y - c.Y) > Zasieg)
                {
                    doUsuniecia.Add(w.Key);
                }
            }
            foreach ((int X, int Y) k in doUsuniecia)
            {
                Usun(k);
            }
            for (int y = c.Y - Zasieg; y <= c.Y + Zasieg; y++)
            {
                for (int x = c.X - Zasieg; x <= c.X + Zasieg; x++)
                {
                    Pole(s, x, y);
                }
            }
        }

        private void Pole(SwiatWolny s, int x, int y)
        {
            WpisModelu? wpis = null;
            string budowla = null;
            if (s.Budowle.TryGetValue((x, y), out budowla))
            {
                wpis = KatalogModeli.DlaBudowli(budowla);
            }
            else
            {
                Obiekt o = s.ObiektNa(x, y);
                if (o != Obiekt.Brak && o != Obiekt.Woda)
                {
                    wpis = KatalogModeli.DlaObiektu(o, x, y, s.Swiat.Biom(x, y));
                }
            }
            string klucz = wpis?.Sciezka;
            if (_stoi.TryGetValue((x, y), out (string Klucz, GameObject Obiekt) stoi))
            {
                if (stoi.Klucz == klucz)
                {
                    return;
                }
                Usun((x, y));
            }
            if (!wpis.HasValue)
            {
                return;
            }
            GameObject g = _scena.Modele.Wez(wpis.Value, _korzen);
            g.transform.position = Scena3D.NaSwiat(x + 0.5, y + 0.5);
            g.transform.rotation = wpis.Value.LosowyObrot
                ? Quaternion.Euler(0f, KatalogModeli.Mieszaj(x, y) % 360, 0f)
                : Quaternion.identity;
            if (budowla == "ognisko" && g.transform.Find("Blask") == null)
            {
                _scena.DodajOgien(g.transform, 0.5f);
            }
            _stoi[(x, y)] = (klucz, g);
        }

        private void Usun((int X, int Y) k)
        {
            _scena.Modele.Oddaj(_stoi[k].Obiekt);
            _stoi.Remove(k);
        }

        private void Bohater(SwiatWolny s, string klasa, double dt)
        {
            string model = KatalogModeli.PostacKlasy(klasa);
            if (_bohater == null || _bohater.Model != model)
            {
                if (_bohater != null)
                {
                    _scena.Modele.Oddaj(_bohater.Obiekt);
                }
                _bohater = new Postac3D(_scena.Modele, model, _korzen);
                _latarnia = new GameObject("Latarnia").AddComponent<Light>();
                _latarnia.transform.SetParent(_bohater.Obiekt.transform, false);
                _latarnia.transform.localPosition = new Vector3(0.25f, 1.1f, 0.3f);
                _latarnia.type = LightType.Point;
                _latarnia.color = new Color(1f, 0.82f, 0.56f);
                _latarnia.range = 8f;
            }
            bool idzie = Math.Abs(s.X - _px) + Math.Abs(s.Y - _py) > 1e-5;
            _px = s.X;
            _py = s.Y;
            _bohater.Ustaw(s.X, s.Y, s.Kierunek.X, s.Kierunek.Y);
            _bohater.Graj(s.Spi || s.Martwy ? Postac3D.Sen : idzie ? Postac3D.Bieg : s.Zbieranie.HasValue ? Postac3D.Praca : Postac3D.Spoczynek);
            _latarnia.intensity = Mathf.Lerp(1.8f, 0f, Mathf.Clamp01((float)s.Jasnosc * 1.6f));
        }

        private void Osadnicy(SwiatWolny s)
        {
            _podpisy.Clear();
            foreach (PostacOsadnika z in new List<PostacOsadnika>(_osadnicy.Keys))
            {
                if (!s.Postacie.Contains(z))
                {
                    _scena.Modele.Oddaj(_osadnicy[z].Obiekt);
                    _osadnicy.Remove(z);
                }
            }
            foreach (PostacOsadnika p in s.Postacie)
            {
                string model = KatalogModeli.PostacZajecia(p.O.Zajecie);
                if (!_osadnicy.TryGetValue(p, out Postac3D z) || z.Model != model)
                {
                    if (z != null)
                    {
                        _scena.Modele.Oddaj(z.Obiekt);
                    }
                    z = new Postac3D(_scena.Modele, model, _korzen);
                    _osadnicy[p] = z;
                }
                z.Ustaw(p.X, p.Y, p.CelX - p.X, p.CelY - p.Y);
                bool przyPracy = !p.Idzie && !p.Spi && p.O.Zajecie != "bezczynny";
                z.Graj(p.Spi ? Postac3D.Sen : p.Idzie ? Postac3D.Chod : przyPracy ? Postac3D.Praca : Postac3D.Spoczynek);
                (int X, int Y)? e = _scena.NaPulpit(Scena3D.NaSwiat(p.X, p.Y, Postac3D.Wzrost + 0.2));
                if (e.HasValue)
                {
                    Barwa kolor = p.O.Morale < 35 ? new Barwa(232, 96, 80) : Styl.Aktywny.Tekst;
                    _podpisy.Add(new PodpisOsadnika(e.Value.X, e.Value.Y, p.O.Imie, kolor));
                }
            }
        }

        private void Duch(SwiatWolny s)
        {
            (int X, int Y, string Klucz, bool Mozna)? p = s.PodgladBudowy();
            if (!p.HasValue)
            {
                if (_duch != null)
                {
                    _duch.SetActive(false);
                }
                return;
            }
            if (_kluczDucha != p.Value.Klucz)
            {
                if (_duch != null)
                {
                    UnityEngine.Object.Destroy(_duch);
                }
                // Własna instancja, bez puli: podmienione materiały nie mogą wrócić do prawdziwych budowli.
                GameObject wzor = _scena.Modele.Wez(KatalogModeli.DlaBudowli(p.Value.Klucz), _korzen);
                _duch = UnityEngine.Object.Instantiate(wzor, _korzen);
                _scena.Modele.Oddaj(wzor);
                _kluczDucha = p.Value.Klucz;
            }
            _duch.SetActive(true);
            Modele.UstawMaterial(_duch, p.Value.Mozna ? _scena.DuchTak : _scena.DuchNie);
            _duch.transform.position = Scena3D.NaSwiat(p.Value.X + 0.5, p.Value.Y + 0.5);
        }
    }
}
