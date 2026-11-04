using System.Collections.Generic;
using System.Globalization;
using GraSimply.Grafika.Trojwymiar;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>Punkt podstawy i rozmiar wypieczonej rośliny (w wysokościach modelu).</summary>
    public struct OpisNatury
    {
        public float KotwicaX, KotwicaY, Szerokosc, Wysokosc;
    }

    /// <summary>
    /// Modele i wypieczona natura: wczytanie, sprowadzenie do zadanej wysokości, realistyczny materiał, cień
    /// przy ziemi oraz pula instancji, bo przy chodzeniu po świecie te same rzeczy ciągle znikają i wracają.
    /// </summary>
    public sealed class Modele
    {
        private readonly Dictionary<string, GameObject> _zrodla = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, float> _wysokosci = new Dictionary<string, float>();
        private readonly Dictionary<string, Stack<GameObject>> _pula = new Dictionary<string, Stack<GameObject>>();
        private readonly Dictionary<GameObject, string> _klucze = new Dictionary<GameObject, string>();
        private readonly Dictionary<(Material, bool), Material> _detal = new Dictionary<(Material, bool), Material>();
        private readonly Dictionary<string, Material> _natura = new Dictionary<string, Material>();
        private readonly Dictionary<string, OpisNatury> _opisy = new Dictionary<string, OpisNatury>();
        private readonly Dictionary<string, Mesh> _siatkiNatury = new Dictionary<string, Mesh>();
        private readonly Material _szablonDetalu = Resources.Load<Material>("Materialy/Detal");
        private readonly Material _szablonNatury = Resources.Load<Material>("Materialy/Natura");
        private readonly Material _kontakt;
        private readonly Mesh _kwadrat;
        private readonly Transform _schowek;

        public Modele(Transform rodzic)
        {
            _schowek = new GameObject("Schowek").transform;
            _schowek.SetParent(rodzic, false);
            _schowek.gameObject.SetActive(false);
            _kontakt = new Material(Resources.Load<Material>("Materialy/Duch"))
            {
                color = new Color(0f, 0f, 0f, 0.36f),
                mainTexture = Plama(),
            };
            _kwadrat = Kwadrat();
            TextAsset opis = Resources.Load<TextAsset>("Natura/natura");
            foreach (string linia in opis.text.Split('\n'))
            {
                string[] c = linia.Trim().Split(';');
                if (c.Length == 5)
                {
                    _opisy["Natura/" + c[0]] = new OpisNatury
                    {
                        KotwicaX = L(c[1]), KotwicaY = L(c[2]), Szerokosc = L(c[3]), Wysokosc = L(c[4]),
                    };
                }
            }
        }

        private static float L(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }

        public OpisNatury Opis(string sciezka)
        {
            return _opisy[sciezka];
        }

        public Material MaterialNatury(string sciezka)
        {
            if (!_natura.TryGetValue(sciezka, out Material m))
            {
                m = new Material(_szablonNatury) { mainTexture = Resources.Load<Texture2D>(sciezka) };
                // Skany drzew mają szare, jakby przysuszone liście: las ma być zielony, a skały zostają.
                if (sciezka.Contains("tree") || sciezka.Contains("fir_"))
                {
                    m.SetColor("_Odcien", new Color(0.74f, 1f, 0.6f));
                }
                _natura[sciezka] = m;
            }
            return m;
        }

        private GameObject Zrodlo(string sciezka)
        {
            if (!_zrodla.TryGetValue(sciezka, out GameObject z))
            {
                z = Resources.Load<GameObject>("Modele/" + sciezka);
                if (z == null)
                {
                    Debug.LogError("Nie ma modelu: " + sciezka);
                }
                _zrodla[sciezka] = z;
            }
            return z;
        }

        /// <summary>Instancja modelu o zadanej wysokości; podstawa stoi na y = 0.</summary>
        public GameObject Wez(WpisModelu wpis, Transform rodzic)
        {
            string klucz = wpis.Sciezka + "@" + wpis.Wysokosc.ToString("F2");
            GameObject o;
            if (_pula.TryGetValue(klucz, out Stack<GameObject> stos) && stos.Count > 0)
            {
                o = stos.Pop();
            }
            else
            {
                o = wpis.Sciezka.StartsWith("Natura/") ? Roslina(wpis)
                  : wpis.Sciezka.StartsWith("zloz:") ? Zloz(wpis) : Pojedynczy(wpis);
                _klucze[o] = klucz;
            }
            o.transform.SetParent(rodzic, false);
            return o;
        }

        public void Oddaj(GameObject o)
        {
            if (!_klucze.TryGetValue(o, out string klucz))
            {
                Object.Destroy(o);
                return;
            }
            if (!_pula.TryGetValue(klucz, out Stack<GameObject> stos))
            {
                stos = new Stack<GameObject>();
                _pula[klucz] = stos;
            }
            o.transform.SetParent(_schowek, false);
            stos.Push(o);
        }

        private GameObject Pojedynczy(WpisModelu wpis)
        {
            GameObject z = Zrodlo(wpis.Sciezka);
            GameObject o = z == null ? new GameObject("brak " + wpis.Sciezka) : Object.Instantiate(z);
            if (!_wysokosci.TryGetValue(wpis.Sciezka, out float h))
            {
                h = Mathf.Max(0.01f, Granice(o).size.y);
                _wysokosci[wpis.Sciezka] = h;
            }
            o.transform.localScale *= wpis.Wysokosc / h;
            bool postac = wpis.Sciezka.StartsWith("Postacie/");
            Realistycznie(o, postac);
            Vector3 rozmiar = Granice(o).size;
            GameObject opak = Opakuj(o);
            float szer = postac ? wpis.Wysokosc * 0.6f : Mathf.Max(rozmiar.x, rozmiar.z) * 1.15f;
            Kontakt(opak.transform, szer);
            return opak;
        }

        /// <summary>Dwa trójkąty z wypieczonym obrazem. Shader sam obraca je do kamery, więc obrót rodzica nie ma znaczenia.</summary>
        private GameObject Roslina(WpisModelu wpis)
        {
            OpisNatury opis = _opisy[wpis.Sciezka];
            string klucz = wpis.Sciezka + "@" + wpis.Wysokosc.ToString("F2");
            if (!_siatkiNatury.TryGetValue(klucz, out Mesh m))
            {
                float w = opis.Szerokosc * wpis.Wysokosc, h = opis.Wysokosc * wpis.Wysokosc;
                float l = -opis.KotwicaX * w, p = (1f - opis.KotwicaX) * w, d = -opis.KotwicaY * h, g = (1f - opis.KotwicaY) * h;
                m = new Mesh { name = klucz };
                m.vertices = new[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
                m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                m.uv2 = new[] { new Vector2(l, d), new Vector2(p, d), new Vector2(l, g), new Vector2(p, g) };
                m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
                m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                m.bounds = new Bounds(Vector3.zero, Vector3.one * (Mathf.Max(w, h) * 2.5f + 2f));
                _siatkiNatury[klucz] = m;
            }
            var o = new GameObject(wpis.Sciezka);
            o.AddComponent<MeshFilter>().sharedMesh = m;
            MeshRenderer r = o.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialNatury(wpis.Sciezka);
            if (wpis.Wysokosc > 0.3f)
            {
                Kontakt(o.transform, opis.Szerokosc * wpis.Wysokosc * (wpis.Wysokosc > 1.5f ? 0.45f : 0.8f));
            }
            return o;
        }

        /// <summary>Dom z części Quaterniusa: współrzędne autora w metrach, całość skalowana do wysokości.</summary>
        private GameObject Zloz(WpisModelu wpis)
        {
            var korzen = new GameObject(wpis.Sciezka);
            foreach (CzescBudynku c in KatalogModeli.Czesci(wpis.Sciezka))
            {
                GameObject z = Zrodlo(c.Sciezka);
                if (z == null)
                {
                    continue;
                }
                GameObject o = Object.Instantiate(z, korzen.transform);
                o.transform.localPosition = new Vector3(c.X, c.Y, c.Z);
                o.transform.localRotation = Quaternion.Euler(0f, c.Obrot, 0f) * z.transform.localRotation;
            }
            float h = Mathf.Max(0.01f, Granice(korzen).size.y);
            korzen.transform.localScale = Vector3.one * (wpis.Wysokosc / h);
            Realistycznie(korzen, false);
            Vector3 rozmiar = Granice(korzen).size;
            GameObject opak = Opakuj(korzen);
            Kontakt(opak.transform, Mathf.Max(rozmiar.x, rozmiar.z) * 1.2f);
            return opak;
        }

        /// <summary>Stonowane barwy i faktura zdjęciowa zamiast czystych kolorów atlasu; postacie mają je łagodniej.</summary>
        private void Realistycznie(GameObject o, bool postac)
        {
            foreach (Renderer r in o.GetComponentsInChildren<Renderer>())
            {
                Material[] m = r.sharedMaterials;
                for (int i = 0; i < m.Length; i++)
                {
                    if (m[i] == null)
                    {
                        continue;
                    }
                    if (!_detal.TryGetValue((m[i], postac), out Material nowy))
                    {
                        nowy = new Material(_szablonDetalu) { mainTexture = m[i].mainTexture };
                        // Quaternius ma już własne tekstury: faktura byłaby tam drugą warstwą brudu.
                        bool wlasna = m[i].name.StartsWith("MI_");
                        nowy.SetFloat("_SilaDetalu", postac ? 0.16f : wlasna ? 0.12f : 0.5f);
                        nowy.SetFloat("_Nasycenie", postac ? 0.85f : wlasna ? 0.92f : 0.66f);
                        _detal[(m[i], postac)] = nowy;
                    }
                    m[i] = nowy;
                }
                r.sharedMaterials = m;
            }
        }

        /// <summary>Ciemna plama tam, gdzie model styka się z ziemią: stoi na gruncie, a nie nad nim.</summary>
        private void Kontakt(Transform rodzic, float szerokosc)
        {
            var o = new GameObject("Kontakt");
            o.transform.SetParent(rodzic, false);
            o.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            o.transform.localScale = new Vector3(szerokosc, 1f, szerokosc);
            o.AddComponent<MeshFilter>().sharedMesh = _kwadrat;
            MeshRenderer r = o.AddComponent<MeshRenderer>();
            r.sharedMaterial = _kontakt;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static Mesh Kwadrat()
        {
            var m = new Mesh { name = "Kontakt" };
            m.vertices = new[] { new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f) };
            m.uv = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            m.triangles = new[] { 0, 1, 3, 0, 3, 2 };
            return m;
        }

        private static Texture2D Plama()
        {
            const int bok = 48;
            var t = new Texture2D(bok, bok, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var p = new Color32[bok * bok];
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    float d = new Vector2(x - bok / 2f + 0.5f, y - bok / 2f + 0.5f).magnitude / (bok / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    p[y * bok + x] = new Color32(255, 255, 255, (byte)(255f * a * a * (3f - 2f * a)));
                }
            }
            t.SetPixels32(p);
            t.Apply(false);
            return t;
        }

        /// <summary>Pusty rodzic pod model: skala i obrót autora zostają, a my przesuwamy i obracamy rodzica.</summary>
        private static GameObject Opakuj(GameObject model)
        {
            var o = new GameObject(model.name);
            model.transform.SetParent(o.transform, false);
            return o;
        }

        public static Bounds Granice(GameObject o)
        {
            Renderer[] r = o.GetComponentsInChildren<Renderer>();
            if (r.Length == 0)
            {
                return new Bounds(o.transform.position, Vector3.one);
            }
            Bounds b = r[0].bounds;
            foreach (Renderer x in r)
            {
                b.Encapsulate(x.bounds);
            }
            return b;
        }

        /// <summary>Podmienia materiał we wszystkich częściach modelu (duch budowli).</summary>
        public static void UstawMaterial(GameObject o, Material material)
        {
            foreach (Renderer r in o.GetComponentsInChildren<Renderer>())
            {
                var m = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < m.Length; i++)
                {
                    m[i] = material;
                }
                r.sharedMaterials = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
