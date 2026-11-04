using System;
using System.Collections.Generic;
using GraSimply.Grafika;
using GraSimply.Grafika.Hybryda;
using GraSimply.Grafika.Trojwymiar;
using GraSimply.Logika;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Plac osady w 3D. Układ i ruch osadników liczy ten sam kod co w 2D (<see cref="UkladPlacu"/>,
    /// <see cref="ScenaOsady"/>), więc ludzie chodzą tymi samymi trasami i żyją tym samym rytmem dnia.
    /// </summary>
    public sealed class Osada3D
    {
        public const float Pole = 2.5f;
        private const float RozmiarKadru = 10.6f;
        private const int Tlo = 5;            // pól lasu za obwodem placu

        private readonly Scena3D _scena;
        private readonly Transform _korzen;
        private readonly ScenaOsady _ruch = new ScenaOsady();
        private readonly Dictionary<(int X, int Y), (string Klucz, GameObject Obiekt)> _stoi =
            new Dictionary<(int X, int Y), (string Klucz, GameObject Obiekt)>();
        private readonly Dictionary<string, Postac3D> _ludzie = new Dictionary<string, Postac3D>();
        private readonly List<GameObject> _palisada = new List<GameObject>();
        private readonly List<PodpisOsadnika> _podpisy = new List<PodpisOsadnika>();

        private MeshRenderer _grunt;
        private Texture2D _tekstura;
        private string _pora;
        private int _poziomPalisady = -1;
        private Postac3D _bohater;
        private GameObject _ognisko;

        public IReadOnlyList<PodpisOsadnika> Podpisy => _podpisy;

        public Osada3D(Scena3D scena)
        {
            _scena = scena;
            _korzen = new GameObject("Osada").transform;
            _korzen.SetParent(scena.Korzen, false);
        }

        public void Pokaz(bool tak)
        {
            _korzen.gameObject.SetActive(tak);
        }

        private static double Wx(double x)
        {
            return (x + 0.5) * Pole;
        }

        public void Aktualizuj(StanOsady osada, double t, bool zmierzch)
        {
            Pokaz(true);
            Dictionary<(int X, int Y), ElementPlacu> plan = _ruch.Aktualizuj(osada, t, zmierzch);
            double srodek = Wx(UkladPlacu.Ognisko.X);
            _scena.Patrz(srodek, srodek + 1.0, RozmiarKadru);
            _scena.Oswietl(_ruch.Jasnosc);
            Grunt(osada.Pora, plan);
            Ognisko();
            Budynki(plan);
            Palisada(osada);
            Ludzie(osada);
            Bohater(osada, t);
            _scena.Migocz(t);
        }

        private void Grunt(string pora, Dictionary<(int X, int Y), ElementPlacu> plan)
        {
            if (_pora == pora)
            {
                return;
            }
            _pora = pora;
            int n = UkladPlacu.SiatkaRuchu + 2 * Tlo, gestosc = 6, bok = n * gestosc;
            if (_tekstura == null)
            {
                _tekstura = new Texture2D(bok, bok, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _grunt = _scena.NowyGrunt("GruntOsady", _tekstura);
                _grunt.transform.SetParent(_korzen, false);
                Las();
            }
            var piksele = new Color32[bok * bok];
            for (int py = 0; py < bok; py++)
            {
                for (int px = 0; px < bok; px++)
                {
                    int x = px / gestosc - Tlo, y = py / gestosc - Tlo;
                    bool plac = x >= 0 && y >= 0 && x < UkladPlacu.Siatka && y < UkladPlacu.Siatka;
                    double odcien = KatalogModeli.Mieszaj(px, py) % 100 / 100.0;
                    piksele[py * bok + px] = Scena3D.Kolor(KatalogModeli.KolorGruntu(plac ? "plac" : "równiny", odcien, pora));
                }
            }
            _tekstura.SetPixels32(piksele);
            _tekstura.Apply(false);
            Scena3D.UstawProstokat(_grunt, -Tlo * Pole, -Tlo * Pole, (n - Tlo) * Pole, (n - Tlo) * Pole);
        }

        /// <summary>Drzewa dookoła: osada stoi na polanie, a nie w próżni.</summary>
        private void Las()
        {
            int n = UkladPlacu.SiatkaRuchu;
            for (int y = -Tlo; y < n + Tlo; y++)
            {
                for (int x = -Tlo; x < n + Tlo; x++)
                {
                    bool wPolanie = x >= -1 && y >= -1 && x <= n && y <= n;
                    int h = KatalogModeli.Mieszaj(x + 40, y + 90);
                    if (wPolanie || h % 100 > 62)
                    {
                        continue;
                    }
                    WpisModelu? m = KatalogModeli.DlaObiektu(Logika.Wolny.Obiekt.Drzewo, x, y, "las");
                    GameObject o = _scena.Modele.Wez(m.Value, _korzen);
                    o.transform.position = Scena3D.NaSwiat(Wx(x) + h % 7 * 0.1, Wx(y) + h % 5 * 0.1);
                    o.transform.rotation = Quaternion.Euler(0f, h % 360, 0f);
                }
            }
        }

        private void Ognisko()
        {
            if (_ognisko != null)
            {
                return;
            }
            _ognisko = _scena.Modele.Wez(new WpisModelu("Oboz/campfire-pit", 0.42f), _korzen);
            _ognisko.transform.position = Scena3D.NaSwiat(Wx(UkladPlacu.Ognisko.X), Wx(UkladPlacu.Ognisko.Y));
            _scena.DodajOgien(_ognisko.transform, 0.7f);
        }

        private static WpisModelu Model(ElementPlacu el)
        {
            switch (el.Rodzaj)
            {
                case "namiot": return new WpisModelu("Oboz/tent-canvas", 1.5f);
                case "chata": return new WpisModelu(KatalogModeli.Chata, 1.8f);
                case "budowa": return new WpisModelu("Oboz/structure", 0.7f + 1.0f * Math.Min(1, el.Poziom / 100f));
                default: return KatalogModeli.DlaBudynku(el.Klucz, el.Poziom);
            }
        }

        private void Budynki(Dictionary<(int X, int Y), ElementPlacu> plan)
        {
            foreach ((int X, int Y) k in new List<(int X, int Y)>(_stoi.Keys))
            {
                if (!plan.ContainsKey(k))
                {
                    _scena.Modele.Oddaj(_stoi[k].Obiekt);
                    _stoi.Remove(k);
                }
            }
            foreach (KeyValuePair<(int X, int Y), ElementPlacu> w in plan)
            {
                WpisModelu m = Model(w.Value);
                string klucz = m.Sciezka + m.Wysokosc.ToString("F2");
                if (_stoi.TryGetValue(w.Key, out (string Klucz, GameObject Obiekt) s))
                {
                    if (s.Klucz == klucz)
                    {
                        continue;
                    }
                    _scena.Modele.Oddaj(s.Obiekt);
                }
                GameObject o = _scena.Modele.Wez(m, _korzen);
                o.transform.position = Scena3D.NaSwiat(Wx(w.Key.X), Wx(w.Key.Y));
                // Budynek zwrócony w stronę ogniska, żeby plac miał środek.
                float obrot = Mathf.Atan2(UkladPlacu.Ognisko.X - w.Key.X, -(UkladPlacu.Ognisko.Y - w.Key.Y)) * Mathf.Rad2Deg;
                o.transform.rotation = Quaternion.Euler(0f, Mathf.Round(obrot / 90f) * 90f, 0f);
                _stoi[w.Key] = (klucz, o);
            }
        }

        private void Palisada(StanOsady osada)
        {
            int poziom = osada.Budynki.TryGetValue("palisada", out int p) ? p : 0;
            if (poziom == _poziomPalisady)
            {
                return;
            }
            _poziomPalisady = poziom;
            foreach (GameObject o in _palisada)
            {
                _scena.Modele.Oddaj(o);
            }
            _palisada.Clear();
            foreach ((int x, int y) in UkladPlacu.PolaPalisady(poziom))
            {
                GameObject o = _scena.Modele.Wez(new WpisModelu("Oboz/fence", 0.9f + 0.2f * poziom), _korzen);
                o.transform.position = Scena3D.NaSwiat(Wx(x), Wx(y));
                o.transform.rotation = Quaternion.Euler(0f, (x + y) % 2 == 0 ? 0f : 90f, 0f);
                _palisada.Add(o);
            }
        }

        private void Ludzie(StanOsady osada)
        {
            _podpisy.Clear();
            var obecni = new HashSet<string>();
            foreach (OsadnikDoRysunku o in osada.Osadnicy)
            {
                if (!_ruch.Ruch.Postacie.TryGetValue(o.Imie, out PostacRuchu p))
                {
                    continue;
                }
                obecni.Add(o.Imie);
                string model = KatalogModeli.PostacZajecia(o.Zajecie);
                if (!_ludzie.TryGetValue(o.Imie, out Postac3D z) || z.Model != model)
                {
                    if (z != null)
                    {
                        _scena.Modele.Oddaj(z.Obiekt);
                    }
                    z = new Postac3D(_scena.Modele, model, _korzen, 1.3f);
                    _ludzie[o.Imie] = z;
                }
                (double x, double y) = _ruch.Ruch.Pozycja(o.Imie);
                z.Ustaw(Wx(x), Wx(y), p.Kierunek.X, p.Kierunek.Y);
                bool sen = !p.Idzie && p.Czynnosc == "sen" && _ruch.Ruch.Dotarl(o.Imie);
                z.Graj(sen ? Postac3D.Sen : p.Idzie ? Postac3D.Chod : p.Czynnosc == "praca" ? Postac3D.Praca : Postac3D.Spoczynek);
                (int X, int Y)? e = _scena.NaPulpit(Scena3D.NaSwiat(Wx(x), Wx(y), 1.55));
                if (e.HasValue)
                {
                    Barwa kolor = o.Morale < 35 ? new Barwa(232, 96, 80) : Styl.Aktywny.Tekst;
                    _podpisy.Add(new PodpisOsadnika(e.Value.X, e.Value.Y, o.Imie, kolor));
                }
            }
            foreach (string imie in new List<string>(_ludzie.Keys))
            {
                if (!obecni.Contains(imie))
                {
                    _scena.Modele.Oddaj(_ludzie[imie].Obiekt);
                    _ludzie.Remove(imie);
                }
            }
        }

        private void Bohater(StanOsady osada, double t)
        {
            string model = KatalogModeli.PostacKlasy(osada.Klasa);
            if (_bohater == null || _bohater.Model != model)
            {
                if (_bohater != null)
                {
                    _scena.Modele.Oddaj(_bohater.Obiekt);
                }
                _bohater = new Postac3D(_scena.Modele, model, _korzen, 1.35f);
            }
            (double x, double y) = _ruch.PozycjaBohatera(osada.Bohater, t);
            double kx = osada.Bohater.X - x, ky = osada.Bohater.Y - y;
            bool idzie = Math.Abs(kx) + Math.Abs(ky) > 0.01;
            _bohater.Ustaw(Wx(x), Wx(y), idzie ? kx : 0.7, idzie ? ky : 0.7);
            _bohater.Graj(idzie ? Postac3D.Bieg : Postac3D.Spoczynek);
        }
    }
}
