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
        private const int ZasiegMiasta = 34;    // pól od kamery miejskiej przy największym oddaleniu
        private const float KadrMiastaMin = 6.6f, KadrMiastaMax = 11f;
        private const double OdswiezenieS = 0.25;

        private readonly Scena3D _scena;
        private readonly Transform _korzen;
        private readonly Grunt3D _grunt;
        private Grunt3D _gruntMiasta;
        private float _kadr = RozmiarKadru, _kadrMiasta = 9.5f;
        private bool _miejski;
        private int _wersjaDrog = -1;
        private Mesh _drogiWZasiegu, _drogiPoza;
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
        private int _licznikAkcji;
        private Postac3D _gosc;
        private readonly Dictionary<Wrog, Wrog3D> _wrogowie = new Dictionary<Wrog, Wrog3D>();
        private readonly List<GameObject> _pociski = new List<GameObject>();
        private readonly List<GameObject> _strzaly = new List<GameObject>();
        private readonly Dictionary<PorzuconyPlecak, GameObject> _plecaki = new Dictionary<PorzuconyPlecak, GameObject>();
        private readonly List<(int X, int Y, double Zycie)> _paski = new List<(int X, int Y, double Zycie)>();

        public IReadOnlyList<(int X, int Y, double Zycie)> PaskiWrogow => _paski;

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

        public void Przybliz(float ile)
        {
            _kadrMiasta = Mathf.Clamp(_kadrMiasta - ile * 0.8f, KadrMiastaMin, KadrMiastaMax);
        }

        public void Aktualizuj(SwiatWolny s, double t, double dt, string klasa)
        {
            Pokaz(true);
            bool miejski = s.KameraMiejska;
            double cx = miejski ? s.KameraX : s.X, cy = miejski ? s.KameraY : s.Y;
            // Płynne przejście: kamera unosi się i opada zamiast skakać.
            _kadr = Mathf.MoveTowards(_kadr, miejski ? _kadrMiasta : RozmiarKadru, (float)dt * 2.5f);
            _scena.Patrz(cx, cy, _kadr);
            _scena.Oswietl(s.Spi ? Math.Min(s.Jasnosc, 0.35) : s.Jasnosc);

            if (miejski != _miejski)
            {
                _miejski = miejski;
                _kafel = (int.MaxValue, int.MaxValue);
                _gruntMiasta = _gruntMiasta ?? _scena.NowyGrunt("GruntMiasta", 2 * ZasiegMiasta + 1, _korzen);
                _gruntMiasta.Pokaz(miejski);
                _grunt.Pokaz(!miejski);
            }
            // W mieście okno przeskakuje co 4 pola, żeby ruch kamery nie przeliczał świata w każdej chwili.
            (int X, int Y) kafel = miejski
                ? ((int)Math.Floor(cx / 4.0) * 4, (int)Math.Floor(cy / 4.0) * 4)
                : ((int)Math.Floor(cx), (int)Math.Floor(cy));
            _odswiez -= dt;
            if (kafel != _kafel || _odswiez <= 0)
            {
                bool ruch = kafel != _kafel;
                _kafel = kafel;
                _odswiez = miejski ? OdswiezenieS * 2 : OdswiezenieS;
                if (ruch)
                {
                    Grunt(s, kafel);
                    Kepki(s, kafel);
                }
                Obiekty(s, kafel);
            }
            Drogi(s);

            Bohater(s, klasa, dt);
            Wrogowie(s);
            Pociski(s);
            Strzaly(s);
            Plecaki(s);
            Zdarzenia(s);
            Osadnicy(s);
            Przybysz(s);
            DomyBezDrogi(s);
            Duch(s);
            _scena.Migocz(t);

            PasekZbioru = null;
            if (s.PostepPracy.HasValue)
            {
                (int X, int Y)? b = _scena.NaPulpit(Scena3D.NaSwiat(s.PostepPracy.Value.X + 0.5, s.PostepPracy.Value.Y + 0.5, 1.6));
                if (b.HasValue)
                {
                    PasekZbioru = (b.Value.X, b.Value.Y, s.PostepPracy.Value.Postep);
                }
            }
            else if (s.Zbieranie.HasValue)
            {
                (int X, int Y)? p = _scena.NaPulpit(Scena3D.NaSwiat(s.Zbieranie.Value.X + 0.5, s.Zbieranie.Value.Y + 0.5, 1.6));
                if (p.HasValue)
                {
                    PasekZbioru = (p.Value.X, p.Value.Y, s.Zbieranie.Value.Postep);
                }
            }
        }

        private int ZasiegTeraz => _miejski ? ZasiegMiasta : Zasieg;

        /// <summary>Drogi to jedna płaska siatka: przebudowuje się tylko po zmianie sieci.</summary>
        private void Drogi(SwiatWolny s)
        {
            if (s.WersjaDrog == _wersjaDrog)
            {
                return;
            }
            _wersjaDrog = s.WersjaDrog;
            _drogiWZasiegu = _drogiWZasiegu ?? NowaSiatkaDrog("DrogiWZasiegu", new Color(0.72f, 0.66f, 0.56f));
            _drogiPoza = _drogiPoza ?? NowaSiatkaDrog("DrogiPozaZasiegiem", new Color(0.62f, 0.3f, 0.24f));
            var w = new List<(int X, int Y)>();
            var p = new List<(int X, int Y)>();
            foreach ((int X, int Y) d in s.Drogi)
            {
                (s.DrogaWZasiegu(d.X, d.Y) ? w : p).Add(d);
            }
            WypelnijDrogi(_drogiWZasiegu, w);
            WypelnijDrogi(_drogiPoza, p);
        }

        private Mesh NowaSiatkaDrog(string nazwa, Color kolor)
        {
            var o = new GameObject(nazwa);
            o.transform.SetParent(_korzen, false);
            var mesh = new Mesh { name = nazwa, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = o.AddComponent<MeshRenderer>();
            var material = new Material(Resources.Load<Material>("Materialy/Detal"));
            material.SetColor("_Odcien", kolor);
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return mesh;
        }

        private static void WypelnijDrogi(Mesh mesh, List<(int X, int Y)> kafle)
        {
            var wierzcholki = new Vector3[kafle.Count * 4];
            var normalne = new Vector3[kafle.Count * 4];
            var trojkaty = new int[kafle.Count * 6];
            for (int i = 0; i < kafle.Count; i++)
            {
                (int x, int y) = kafle[i];
                wierzcholki[4 * i] = Scena3D.NaSwiat(x, y, 0.03);
                wierzcholki[4 * i + 1] = Scena3D.NaSwiat(x + 1, y, 0.03);
                wierzcholki[4 * i + 2] = Scena3D.NaSwiat(x + 1, y + 1, 0.03);
                wierzcholki[4 * i + 3] = Scena3D.NaSwiat(x, y + 1, 0.03);
                for (int n = 0; n < 4; n++)
                {
                    normalne[4 * i + n] = Vector3.up;
                }
                // Oś Y gry biegnie w minus Z świata: ta kolejność patrzy w górę, ku kamerze.
                trojkaty[6 * i] = 4 * i;
                trojkaty[6 * i + 1] = 4 * i + 1;
                trojkaty[6 * i + 2] = 4 * i + 2;
                trojkaty[6 * i + 3] = 4 * i;
                trojkaty[6 * i + 4] = 4 * i + 2;
                trojkaty[6 * i + 5] = 4 * i + 3;
            }
            mesh.Clear();
            mesh.vertices = wierzcholki;
            mesh.normals = normalne;
            mesh.triangles = trojkaty;
            mesh.RecalculateBounds();
        }

        /// <summary>Dom bez drogi do składu ma nad sobą czerwony znak: nawet przy wielu domach widać, co jest odcięte.</summary>
        private void DomyBezDrogi(SwiatWolny s)
        {
            foreach ((int X, int Y) d in s.Domy.Keys)
            {
                if (Math.Abs(d.X - _kafel.X) > ZasiegTeraz || Math.Abs(d.Y - _kafel.Y) > ZasiegTeraz || s.DomPodlaczony(d))
                {
                    continue;
                }
                (int X, int Y)? e = _scena.NaPulpit(Scena3D.NaSwiat(d.X + 1.0, d.Y + 1.0, 2.2));
                if (e.HasValue)
                {
                    _podpisy.Add(new PodpisOsadnika(e.Value.X, e.Value.Y, "!", new Barwa(232, 96, 80)));
                }
            }
        }

        private void Grunt(SwiatWolny s, (int X, int Y) c)
        {
            int zasieg = ZasiegTeraz;
            Grunt3D grunt = _miejski ? _gruntMiasta : _grunt;
            int bok = 2 * zasieg + 1;
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    int tx = c.X - zasieg + x, ty = c.Y - zasieg + y;
                    string biom = !GeneratorSwiata.WGranicach(tx, ty) ? "ruiny"
                                : s.ObiektNa(tx, ty) == Obiekt.Woda ? "woda" : s.Swiat.Biom(tx, ty);
                    grunt.Pole(x, y, biom, s.Swiat.Odcien(tx, ty));
                }
            }
            grunt.Zastosuj();
            // Środki tekseli wypadają w środkach pól, więc kolory zlewają się płynnie między sąsiadami.
            grunt.Prostokat(c.X - zasieg, c.Y - zasieg, c.X + zasieg + 1, c.Y + zasieg + 1);
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
            int zasieg = ZasiegTeraz;
            var doUsuniecia = new List<(int X, int Y)>();
            foreach (KeyValuePair<(int X, int Y), (string Klucz, GameObject Obiekt)> w in _stoi)
            {
                if (Math.Abs(w.Key.X - c.X) > zasieg || Math.Abs(w.Key.Y - c.Y) > zasieg)
                {
                    doUsuniecia.Add(w.Key);
                }
            }
            foreach ((int X, int Y) k in doUsuniecia)
            {
                Usun(k);
            }
            for (int y = c.Y - zasieg; y <= c.Y + zasieg; y++)
            {
                for (int x = c.X - zasieg; x <= c.X + zasieg; x++)
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
                wpis = s.Place.TryGetValue((x, y), out PlacBudowy plac)
                    ? KatalogModeli.DlaEtapu(plac.Klucz, plac.Etap, SwiatWolny.Etapy[plac.Klucz].Length)
                    : KatalogModeli.DlaBudowli(budowla);
            }
            else
            {
                Obiekt o = s.ObiektNa(x, y);
                if (o != Obiekt.Brak && o != Obiekt.Woda)
                {
                    wpis = KatalogModeli.DlaObiektu(o, x, y, s.Swiat.Biom(x, y));
                }
            }
            string klucz = wpis?.Sciezka + wpis?.Wysokosc;
            if (_stoi.TryGetValue((x, y), out (string Klucz, GameObject Obiekt) stoi))
            {
                if (stoi.Klucz == klucz)
                {
                    if (budowla == "palisada" || budowla == "brama")
                    {
                        Uloz(s, stoi.Obiekt, x, y);
                    }
                    return;
                }
                Usun((x, y));
            }
            if (!wpis.HasValue)
            {
                return;
            }
            GameObject g = _scena.Modele.Wez(wpis.Value, _korzen);
            double srodek = budowla != null ? SwiatWolny.Stopa(budowla) / 2.0 : 0.5;
            g.transform.position = Scena3D.NaSwiat(x + srodek, y + srodek);
            g.transform.rotation = wpis.Value.LosowyObrot
                ? Quaternion.Euler(0f, KatalogModeli.Mieszaj(x, y) % 360, 0f)
                : Quaternion.identity;
            if (budowla == "palisada" || budowla == "brama")
            {
                Uloz(s, g, x, y);
            }
            if (budowla == "ognisko" && g.transform.Find("Blask") == null)
            {
                _scena.DodajOgien(g.transform, 0.5f);
            }
            _stoi[(x, y)] = (klucz, g);
        }

        /// <summary>Płot leży przy krawędzi swojego modelu: obraca się wzdłuż sąsiadów i wraca na środek kafla.</summary>
        private static void Uloz(SwiatWolny s, GameObject g, int x, int y)
        {
            bool Mur(int a, int b) => s.Budowle.TryGetValue((a, b), out string k) && (k == "palisada" || k == "brama" || k == "wieza");
            bool wzdluzY = (Mur(x, y - 1) || Mur(x, y + 1)) && !(Mur(x - 1, y) || Mur(x + 1, y));
            Quaternion obrot = Quaternion.Euler(0f, wzdluzY ? 90f : 0f, 0f);
            g.transform.rotation = obrot;
            g.transform.position = Scena3D.NaSwiat(x + 0.5, y + 0.5) + obrot * new Vector3(0f, 0f, 0.46f);
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
                _latarnia.range = 11f;
            }
            bool idzie = Math.Abs(s.X - _px) + Math.Abs(s.Y - _py) > 1e-5;
            _px = s.X;
            _py = s.Y;
            _bohater.Ustaw(s.X, s.Y, s.Kierunek.X, s.Kierunek.Y);
            if (s.LicznikAkcji != _licznikAkcji)
            {
                _licznikAkcji = s.LicznikAkcji;
                _bohater.Cios(KlipAkcji(s.Akcja), s.DlugoscAkcji);
            }
            if (s.Akcja == AkcjaBohatera.Brak)
            {
                _bohater.Graj(s.Spi || s.Martwy ? Postac3D.Sen : s.Blokuje ? Postac3D.Blok : idzie ? Postac3D.Bieg
                            : s.Zbieranie.HasValue || s.BohaterPracuje ? Postac3D.Praca : Postac3D.Spoczynek);
            }
            _latarnia.intensity = Mathf.Lerp(1.8f, 0f, Mathf.Clamp01((float)s.Jasnosc * 1.6f));
        }

        private static string KlipAkcji(AkcjaBohatera a)
        {
            switch (a)
            {
                case AkcjaBohatera.Cios: return Postac3D.CiosLekki;
                case AkcjaBohatera.Ciezki: return Postac3D.CiosCiezki;
                case AkcjaBohatera.Unik: return Postac3D.Unik;
                case AkcjaBohatera.Wir: return Postac3D.CiosWir;
                case AkcjaBohatera.Szarza: return Postac3D.CiosSztych;
                case AkcjaBohatera.Okrzyk: return Postac3D.CiosKrzyk;
                default: return Postac3D.Rana;
            }
        }

        private sealed class Wrog3D
        {
            public Postac3D Postac;
            public Transform Znak;
            public int Zamachy;
            public StanWroga Stan;
        }

        private static string KlipCiosu(TypWroga t)
        {
            switch (t)
            {
                case TypWroga.Wojownik: return Postac3D.CiosCiezki;
                case TypWroga.Lotr: return Postac3D.CiosSztych;
                case TypWroga.Mag: return Postac3D.Zaklecie;
                default: return Postac3D.CiosPiesc;
            }
        }

        private void Wrogowie(SwiatWolny s)
        {
            _paski.Clear();
            foreach (Wrog g in new List<Wrog>(_wrogowie.Keys))
            {
                if (!s.Wrogowie.Contains(g))
                {
                    _wrogowie[g].Znak.gameObject.SetActive(false);
                    _scena.Modele.Oddaj(_wrogowie[g].Postac.Obiekt);
                    _wrogowie.Remove(g);
                }
            }
            foreach (Wrog g in s.Wrogowie)
            {
                WpisModelu wpis = KatalogModeli.DlaWroga(g.Typ);
                if (!_wrogowie.TryGetValue(g, out Wrog3D w))
                {
                    var p = new Postac3D(_scena.Modele, wpis.Sciezka, _korzen, wpis.Wysokosc);
                    w = new Wrog3D { Postac = p, Znak = _scena.Modele.Znak(p.Obiekt.transform), Stan = (StanWroga)(-1) };
                    _wrogowie[g] = w;
                }
                w.Postac.Ustaw(g.X, g.Y, g.KierX, g.KierY);
                var c = SwiatWolny.Cechy(g.Typ);
                if (g.Zamachy != w.Zamachy)
                {
                    w.Zamachy = g.Zamachy;
                    w.Postac.Cios(KlipCiosu(g.Typ), c.Zamach + 0.2);
                }
                if (g.Stan != w.Stan)
                {
                    w.Stan = g.Stan;
                    if (g.Stan == StanWroga.Martwy)
                    {
                        w.Postac.Cios(Postac3D.Smierc, 0.8);
                    }
                    else if (g.Stan == StanWroga.Ogluszony)
                    {
                        w.Postac.Cios(Postac3D.Rana, Math.Max(0.3, g.Timer));
                    }
                    else if (g.Stan == StanWroga.Idzie)
                    {
                        w.Postac.Graj(g.Typ == TypWroga.Lotr ? Postac3D.Bieg : Postac3D.Chod);
                    }
                    else if (g.Stan != StanWroga.Zamach)
                    {
                        w.Postac.Graj(Postac3D.Spoczynek);
                    }
                }
                bool zamach = g.Stan == StanWroga.Zamach;
                w.Znak.gameObject.SetActive(zamach);
                if (zamach)
                {
                    // Plama rośnie aż do ciosu: pełna znaczy „teraz odskocz albo paruj”.
                    float postep = 1f - Mathf.Clamp01((float)(g.Timer / c.Zamach));
                    float r = (g.Typ == TypWroga.Mag ? 1.2f : (float)c.Zasieg * 2.4f) * (0.35f + 0.65f * postep);
                    w.Znak.localScale = new Vector3(r, 1f, r);
                }
                if (g.Stan != StanWroga.Martwy && (g.Hp < g.MaxHp || zamach))
                {
                    (int X, int Y)? e = _scena.NaPulpit(Scena3D.NaSwiat(g.X, g.Y, wpis.Wysokosc + 0.25));
                    if (e.HasValue)
                    {
                        _paski.Add((e.Value.X, e.Value.Y, g.Hp / g.MaxHp));
                    }
                }
            }
        }

        private void Przybysz(SwiatWolny s)
        {
            if (s.Gosc == null)
            {
                if (_gosc != null)
                {
                    _scena.Modele.Oddaj(_gosc.Obiekt);
                    _gosc = null;
                }
                return;
            }
            _gosc = _gosc ?? new Postac3D(_scena.Modele, "Postacie/Rogue_Hooded", _korzen);
            _gosc.Ustaw(s.Gosc.X, s.Gosc.Y, s.X - s.Gosc.X, s.Y - s.Gosc.Y);
            _gosc.Graj(Postac3D.Spoczynek);
            (int X, int Y)? e = _scena.NaPulpit(Scena3D.NaSwiat(s.Gosc.X, s.Gosc.Y, Postac3D.Wzrost + 0.2));
            if (e.HasValue)
            {
                _podpisy.Add(new PodpisOsadnika(e.Value.X, e.Value.Y, s.Gosc.O.Imie + " ?", new Barwa(255, 214, 120)));
            }
        }

        private void Pociski(SwiatWolny s)
        {
            while (_pociski.Count < s.Pociski.Count)
            {
                GameObject k = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.Destroy(k.GetComponent<Collider>());
                k.transform.SetParent(_korzen, false);
                k.transform.localScale = Vector3.one * 0.3f;
                MeshRenderer r = k.GetComponent<MeshRenderer>();
                r.sharedMaterial = new Material(_scena.Plomien) { color = new Color(0.72f, 0.4f, 1f) };
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _pociski.Add(k);
            }
            for (int i = 0; i < _pociski.Count; i++)
            {
                bool jest = i < s.Pociski.Count;
                _pociski[i].SetActive(jest);
                if (jest)
                {
                    _pociski[i].transform.position = Scena3D.NaSwiat(s.Pociski[i].X, s.Pociski[i].Y, 0.75);
                }
            }
        }

        private void Strzaly(SwiatWolny s)
        {
            while (_strzaly.Count < s.Strzaly.Count)
            {
                GameObject k = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.Destroy(k.GetComponent<Collider>());
                k.transform.SetParent(_korzen, false);
                k.transform.localScale = Vector3.one * 0.13f;
                MeshRenderer r = k.GetComponent<MeshRenderer>();
                r.sharedMaterial = new Material(_scena.Plomien) { color = new Color(1f, 0.92f, 0.6f) };
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _strzaly.Add(k);
            }
            for (int i = 0; i < _strzaly.Count; i++)
            {
                bool jest = i < s.Strzaly.Count;
                _strzaly[i].SetActive(jest);
                if (jest)
                {
                    Strzala a = s.Strzaly[i];
                    _strzaly[i].transform.position = Scena3D.NaSwiat(a.X0 + (a.X1 - a.X0) * a.Czas, a.Y0 + (a.Y1 - a.Y0) * a.Czas, 2.0 - 1.3 * a.Czas);
                }
            }
        }

        private void Plecaki(SwiatWolny s)
        {
            foreach (PorzuconyPlecak p in new List<PorzuconyPlecak>(_plecaki.Keys))
            {
                if (!s.Porzucone.Contains(p))
                {
                    _scena.Modele.Oddaj(_plecaki[p]);
                    _plecaki.Remove(p);
                }
            }
            foreach (PorzuconyPlecak p in s.Porzucone)
            {
                if (!_plecaki.ContainsKey(p))
                {
                    GameObject o = _scena.Modele.Wez(KatalogModeli.PorzuconyPlecak, _korzen);
                    o.transform.position = Scena3D.NaSwiat(p.X, p.Y);
                    _plecaki[p] = o;
                }
            }
        }

        private void Zdarzenia(SwiatWolny s)
        {
            foreach (ZdarzenieWalki z in s.Zdarzenia)
            {
                Vector3 m = Scena3D.NaSwiat(z.X, z.Y, 0.75);
                switch (z.Rodzaj)
                {
                    case "riposta":
                        _scena.Iskry(m, new Color(1f, 0.85f, 0.3f), 26);
                        _scena.Wstrzas(0.14f);
                        break;
                    case "parowanie":
                        _scena.Iskry(m, new Color(1f, 0.95f, 0.55f), 18);
                        _scena.Wstrzas(0.08f);
                        break;
                    case "blok":
                        _scena.Iskry(m, new Color(0.8f, 0.85f, 0.95f), 6);
                        break;
                    case "rana":
                        _scena.Iskry(m, new Color(1f, 0.15f, 0.1f), 12);
                        _scena.Wstrzas(0.2f);
                        break;
                    case "okrzyk":
                        _scena.Iskry(m, new Color(0.45f, 0.75f, 1f), 40);
                        _scena.Wstrzas(0.1f);
                        break;
                    case "budowla":
                        _scena.Iskry(m, new Color(0.75f, 0.5f, 0.25f), 6);
                        break;
                    default:
                        _scena.Iskry(m, new Color(1f, 0.72f, 0.35f), 8);
                        break;
                }
            }
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
                if (p.Ataki != z.Licznik)
                {
                    z.Licznik = p.Ataki;
                    z.Cios(Postac3D.CiosLekki, 0.5);
                }
                if (p.WWalce && p.AtakCd > 0.6)
                {
                    continue;
                }
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
                if (p.Value.Klucz == "droga" || p.Value.Klucz == "wyburz")
                {
                    // Droga i wyburzanie nie mają modelu: duchem jest płaski kafel.
                    _duch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    UnityEngine.Object.Destroy(_duch.GetComponent<Collider>());
                    _duch.transform.SetParent(_korzen, false);
                    _duch.transform.localScale = new Vector3(1f, 0.06f, 1f);
                }
                else
                {
                    // Własna instancja, bez puli: podmienione materiały nie mogą wrócić do prawdziwych budowli.
                    GameObject wzor = _scena.Modele.Wez(KatalogModeli.DlaBudowli(p.Value.Klucz), _korzen);
                    _duch = UnityEngine.Object.Instantiate(wzor, _korzen);
                    _scena.Modele.Oddaj(wzor);
                }
                _kluczDucha = p.Value.Klucz;
            }
            _duch.SetActive(true);
            Modele.UstawMaterial(_duch, p.Value.Mozna ? _scena.DuchTak : _scena.DuchNie);
            double srodekDucha = SwiatWolny.Stopa(p.Value.Klucz) / 2.0;
            _duch.transform.position = Scena3D.NaSwiat(p.Value.X + srodekDucha, p.Value.Y + srodekDucha);
        }
    }
}
