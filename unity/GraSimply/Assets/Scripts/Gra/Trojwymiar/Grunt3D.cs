using GraSimply.Grafika;
using GraSimply.Grafika.Trojwymiar;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Płaski grunt ze zdjęciowymi warstwami. Wypełnia dwie małe mapy danych, po jednym tekselu na pole:
    /// skład (trawa, ziemia, skała, piasek) oraz odcień biomu z maską wody. Resztę robi shader.
    /// </summary>
    public sealed class Grunt3D
    {
        public readonly int Bok;

        private readonly MeshFilter _siatka;
        private readonly Material _material;
        private readonly Texture2D _sklad;
        private readonly Texture2D _barwa;
        private readonly Color32[] _skladP;
        private readonly Color32[] _barwaP;

        public Grunt3D(string nazwa, int bok, Transform rodzic, Material wzor)
        {
            Bok = bok;
            var o = new GameObject(nazwa);
            o.transform.SetParent(rodzic, false);
            _siatka = o.AddComponent<MeshFilter>();
            _siatka.sharedMesh = new Mesh { name = nazwa };
            MeshRenderer r = o.AddComponent<MeshRenderer>();
            _sklad = Mapa(bok);
            _barwa = Mapa(bok);
            _skladP = new Color32[bok * bok];
            _barwaP = new Color32[bok * bok];
            _material = new Material(wzor);
            _material.SetTexture("_Sklad", _sklad);
            _material.SetTexture("_Barwa", _barwa);
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
        }

        private static Texture2D Mapa(int bok)
        {
            return new Texture2D(bok, bok, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        public void Pole(int x, int y, string biom, double odcien, string pora = "lato")
        {
            (double t, double z, double s, double p) = KatalogModeli.SkladGruntu(biom);
            _skladP[y * Bok + x] = new Color32((byte)(t * 255), (byte)(z * 255), (byte)(s * 255), (byte)(p * 255));
            Barwa b = KatalogModeli.KolorGruntu(biom, odcien, pora);
            _barwaP[y * Bok + x] = new Color32(b.R, b.G, b.B, (byte)(biom == "woda" ? 255 : 0));
        }

        /// <summary>Nieodkryte pole mapy: prawie czarne.</summary>
        public void Ciemno(int x, int y)
        {
            _skladP[y * Bok + x] = new Color32(0, 255, 0, 0);
            _barwaP[y * Bok + x] = new Color32(14, 17, 22, 0);
        }

        public void Zastosuj()
        {
            _sklad.SetPixels32(_skladP);
            _sklad.Apply(false);
            _barwa.SetPixels32(_barwaP);
            _barwa.Apply(false);
        }

        public void Snieg(float ile)
        {
            _material.SetFloat("_Snieg", ile);
        }

        public void Prostokat(double x0, double y0, double x1, double y1)
        {
            Mesh m = _siatka.sharedMesh;
            m.Clear();
            m.vertices = new[]
            {
                Scena3D.NaSwiat(x0, y0), Scena3D.NaSwiat(x1, y0), Scena3D.NaSwiat(x0, y1), Scena3D.NaSwiat(x1, y1),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.tangents = new[] { new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1) };
            m.triangles = new[] { 0, 1, 3, 0, 3, 2 };
            m.RecalculateBounds();
        }
    }
}
