using System.Collections.Generic;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Tysiące drobnych kępek trawy bez tysięcy obiektów: każda odmiana to jedna wspólna siatka przebudowywana,
    /// gdy zmieni się teren. Karta dostaje jedno polecenie rysowania na odmianę.
    /// </summary>
    public sealed class Trawnik
    {
        private sealed class Odmiana
        {
            public MeshFilter Siatka;
            public OpisNatury Opis;
            public readonly List<Vector3> Pozycje = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Vector2> Przesuniecia = new List<Vector2>();
            public readonly List<int> Trojkaty = new List<int>();
        }

        private readonly Modele _modele;
        private readonly Transform _rodzic;
        private readonly Dictionary<string, Odmiana> _odmiany = new Dictionary<string, Odmiana>();

        public Trawnik(Modele modele, Transform rodzic)
        {
            _modele = modele;
            _rodzic = rodzic;
        }

        public void Wyczysc()
        {
            foreach (Odmiana o in _odmiany.Values)
            {
                o.Pozycje.Clear();
                o.Uv.Clear();
                o.Przesuniecia.Clear();
                o.Trojkaty.Clear();
            }
        }

        public void Dodaj(string sciezka, double x, double y, float wysokosc)
        {
            if (!_odmiany.TryGetValue(sciezka, out Odmiana o))
            {
                o = Nowa(sciezka);
                _odmiany[sciezka] = o;
            }
            float w = o.Opis.Szerokosc * wysokosc, h = o.Opis.Wysokosc * wysokosc;
            float l = -o.Opis.KotwicaX * w, p = (1f - o.Opis.KotwicaX) * w, d = -o.Opis.KotwicaY * h, g = (1f - o.Opis.KotwicaY) * h;
            Vector3 s = Scena3D.NaSwiat(x, y);
            int i = o.Pozycje.Count;
            o.Pozycje.Add(s);
            o.Pozycje.Add(s);
            o.Pozycje.Add(s);
            o.Pozycje.Add(s);
            o.Przesuniecia.Add(new Vector2(l, d));
            o.Przesuniecia.Add(new Vector2(p, d));
            o.Przesuniecia.Add(new Vector2(l, g));
            o.Przesuniecia.Add(new Vector2(p, g));
            o.Uv.Add(new Vector2(0, 0));
            o.Uv.Add(new Vector2(1, 0));
            o.Uv.Add(new Vector2(0, 1));
            o.Uv.Add(new Vector2(1, 1));
            o.Trojkaty.Add(i);
            o.Trojkaty.Add(i + 2);
            o.Trojkaty.Add(i + 1);
            o.Trojkaty.Add(i + 1);
            o.Trojkaty.Add(i + 2);
            o.Trojkaty.Add(i + 3);
        }

        public void Zbuduj()
        {
            foreach (Odmiana o in _odmiany.Values)
            {
                Mesh m = o.Siatka.sharedMesh;
                m.Clear();
                m.SetVertices(o.Pozycje);
                m.SetUVs(0, o.Uv);
                m.SetUVs(1, o.Przesuniecia);
                m.SetTriangles(o.Trojkaty, 0);
                m.RecalculateBounds();
                Bounds b = m.bounds;
                b.Expand(4f);
                m.bounds = b;
            }
        }

        private Odmiana Nowa(string sciezka)
        {
            var g = new GameObject("Trawnik " + sciezka);
            g.transform.SetParent(_rodzic, false);
            var m = new Mesh { name = sciezka, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            MeshFilter f = g.AddComponent<MeshFilter>();
            f.sharedMesh = m;
            MeshRenderer r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = _modele.MaterialNatury(sciezka);
            // Kępki są drobne, a cień każdej kosztowałby więcej, niż widać różnicy.
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return new Odmiana { Siatka = f, Opis = _modele.Opis(sciezka) };
        }
    }
}
