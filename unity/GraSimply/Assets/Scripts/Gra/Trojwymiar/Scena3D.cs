using System;
using System.Collections.Generic;
using GraSimply.Grafika;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Wspólna oprawa sceny 3D: kamera izometryczna, słońce, grunt z tekstury i ogniska. Współrzędne gry
    /// (x, y) to w świecie Unity (x, −z): dzięki temu oś x gry biegnie na ekranie w prawo w dół,
    /// a oś y w lewo w dół, czyli tak samo jak w rzucie 2D i przy tym samym sterowaniu WASD.
    /// </summary>
    public sealed class Scena3D
    {
        public readonly Transform Korzen;
        public readonly Camera Kamera;
        public readonly Modele Modele;
        public readonly Material Plomien;
        public readonly Material DuchTak;
        public readonly Material DuchNie;

        private readonly Light _slonce;
        private readonly Material _materialGruntu;
        private readonly List<Ogien> _ognie = new List<Ogien>();

        /// <summary>Piksel ekranu → piksel pulpitu 1280×720. Podaje silnik gry albo generator podglądu.</summary>
        public Func<Vector2, Vector2?> EkranNaPulpit = e => e;

        public Scena3D()
        {
            Korzen = new GameObject("Swiat3D").transform;
            Modele = new Modele(Korzen);

            Kamera = new GameObject("Kamera").AddComponent<Camera>();
            Kamera.transform.SetParent(Korzen, false);
            Kamera.orthographic = true;
            Kamera.nearClipPlane = 0.1f;
            Kamera.farClipPlane = 400f;
            Kamera.clearFlags = CameraClearFlags.SolidColor;
            Kamera.backgroundColor = new Color(0.07f, 0.09f, 0.1f);
            Kamera.transform.rotation = Quaternion.Euler(32f, -45f, 0f);

            _slonce = new GameObject("Slonce").AddComponent<Light>();
            _slonce.transform.SetParent(Korzen, false);
            _slonce.type = LightType.Directional;
            _slonce.shadows = LightShadows.Soft;
            _slonce.shadowStrength = 0.65f;
            _slonce.transform.rotation = Quaternion.Euler(52f, 20f, 0f);

            QualitySettings.shadowDistance = 90f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.pixelLightCount = 6;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = false;

            _materialGruntu = Resources.Load<Material>("Materialy/Grunt");
            Plomien = Resources.Load<Material>("Materialy/Plomien");
            Material duch = Resources.Load<Material>("Materialy/Duch");
            DuchTak = new Material(duch) { color = new Color(0.35f, 1f, 0.45f, 0.55f) };
            DuchNie = new Material(duch) { color = new Color(1f, 0.25f, 0.2f, 0.55f) };
        }

        public static Vector3 NaSwiat(double x, double y, double h = 0)
        {
            return new Vector3((float)x, (float)h, -(float)y);
        }

        /// <summary>Kamera patrzy na punkt gry (x, y); rozmiar to połowa wysokości kadru w jednostkach świata.</summary>
        public void Patrz(double x, double y, float rozmiar)
        {
            Kamera.orthographicSize = rozmiar;
            Kamera.transform.position = NaSwiat(x, y) - Kamera.transform.forward * 150f;
        }

        /// <summary>Doba: słońce gaśnie i robi się chłodniej, a pomarańczowo o zmierzchu.</summary>
        public void Oswietl(double jasnosc)
        {
            float j = Mathf.Clamp01((float)jasnosc);
            float zmierzch = 1f - Mathf.Abs(j - 0.55f) / 0.45f;
            _slonce.intensity = Mathf.Lerp(0.05f, 0.82f, j);
            _slonce.color = Color.Lerp(new Color(1f, 0.96f, 0.86f), new Color(1f, 0.62f, 0.38f), Mathf.Clamp01(zmierzch));
            RenderSettings.ambientLight = Color.Lerp(new Color(0.1f, 0.13f, 0.22f), new Color(0.4f, 0.41f, 0.44f), j);
            foreach (Ogien o in _ognie)
            {
                o.Swiatlo.intensity = Mathf.Lerp(1.5f, 0.3f, j);
            }
        }

        /// <summary>Punkt świata w pikselach natywnych pulpitu (640×360); null, gdy poza kadrem.</summary>
        public (int X, int Y)? NaPulpit(Vector3 swiat)
        {
            Vector3 e = Kamera.WorldToScreenPoint(swiat);
            Vector2? p = EkranNaPulpit(new Vector2(e.x, e.y));
            if (!p.HasValue)
            {
                return null;
            }
            return ((int)(p.Value.x / PulpitNatywny), (int)(p.Value.y / PulpitNatywny));
        }

        private const float PulpitNatywny = 2f;

        /// <summary>Pole gry pod pikselem ekranu (promień z kamery na ziemię).</summary>
        public (double X, double Y) PodKursorem(Vector2 ekran)
        {
            Ray r = Kamera.ScreenPointToRay(ekran);
            float t = Mathf.Abs(r.direction.y) < 1e-5f ? 0f : -r.origin.y / r.direction.y;
            Vector3 p = r.origin + r.direction * t;
            return (p.x, -p.z);
        }

        // ---- grunt ------------------------------------------------------- //

        /// <summary>Płaski prostokąt od (x0, y0) do (x1, y1) w polach gry, cały pokryty jedną teksturą.</summary>
        public MeshRenderer NowyGrunt(string nazwa, Texture2D tekstura)
        {
            var o = new GameObject(nazwa);
            o.transform.SetParent(Korzen, false);
            MeshRenderer r = o.AddComponent<MeshRenderer>();
            o.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = nazwa };
            r.sharedMaterial = new Material(_materialGruntu) { mainTexture = tekstura };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
            return r;
        }

        public static void UstawProstokat(MeshRenderer grunt, double x0, double y0, double x1, double y1)
        {
            Mesh m = grunt.GetComponent<MeshFilter>().sharedMesh;
            m.Clear();
            m.vertices = new[]
            {
                NaSwiat(x0, y0), NaSwiat(x1, y0), NaSwiat(x0, y1), NaSwiat(x1, y1),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.triangles = new[] { 0, 1, 3, 0, 3, 2 };
            m.RecalculateBounds();
        }

        public static Color32 Kolor(Barwa b)
        {
            return new Color32(b.R, b.G, b.B, 255);
        }

        // ---- ogień ------------------------------------------------------- //

        private sealed class Ogien
        {
            public Light Swiatlo;
            public Transform[] Jezory;
            public float Faza;
        }

        /// <summary>Płomień i migoczące światło pod podanym obiektem (ognisko).</summary>
        public void DodajOgien(Transform rodzic, float rozmiar)
        {
            var o = new Ogien { Faza = UnityEngine.Random.value * 10f, Jezory = new Transform[3] };
            var kolory = new[] { new Color(1f, 0.42f, 0.1f), new Color(1f, 0.68f, 0.14f), new Color(1f, 0.92f, 0.5f) };
            for (int i = 0; i < 3; i++)
            {
                GameObject j = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                UnityEngine.Object.Destroy(j.GetComponent<Collider>());
                j.transform.SetParent(rodzic, false);
                j.transform.position = rodzic.position + new Vector3(0f, rozmiar * (0.4f + i * 0.08f), 0f);
                MeshRenderer r = j.GetComponent<MeshRenderer>();
                r.sharedMaterial = new Material(Plomien) { color = kolory[i] };
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                o.Jezory[i] = j.transform;
            }
            var s = new GameObject("Blask").AddComponent<Light>();
            s.transform.SetParent(rodzic, false);
            s.transform.position = rodzic.position + Vector3.up * rozmiar * 1.6f;
            s.type = LightType.Point;
            s.color = new Color(1f, 0.62f, 0.3f);
            s.range = 11f;
            o.Swiatlo = s;
            _ognie.Add(o);
        }

        /// <summary>Animacja płomieni; wołana raz na klatkę.</summary>
        public void Migocz(double t)
        {
            _ognie.RemoveAll(o => o.Swiatlo == null);
            foreach (Ogien o in _ognie)
            {
                if (!o.Swiatlo.gameObject.activeInHierarchy)
                {
                    continue;
                }
                for (int i = 0; i < o.Jezory.Length; i++)
                {
                    float f = Mathf.Sin((float)t * (7f + i * 2.3f) + o.Faza + i);
                    float s = (0.46f - i * 0.12f) * (1f + 0.14f * f);
                    o.Jezory[i].localScale = new Vector3(s, s * 1.5f, s) / Mathf.Max(0.01f, o.Jezory[i].parent.lossyScale.x);
                }
                o.Swiatlo.range = 10.5f + Mathf.Sin((float)t * 9f + o.Faza);
            }
        }
    }
}
