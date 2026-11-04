using System.Collections.Generic;
using GraSimply.Grafika.Trojwymiar;
using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Modele z <c>Resources/Modele</c>: wczytanie, sprowadzenie do zadanej wysokości i pula instancji,
    /// bo przy chodzeniu po świecie te same drzewa ciągle znikają i wracają.
    /// </summary>
    public sealed class Modele
    {
        private readonly Dictionary<string, GameObject> _zrodla = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, float> _wysokosci = new Dictionary<string, float>();
        private readonly Dictionary<string, Stack<GameObject>> _pula = new Dictionary<string, Stack<GameObject>>();
        private readonly Dictionary<GameObject, string> _klucze = new Dictionary<GameObject, string>();
        private readonly Transform _schowek;

        public Modele(Transform rodzic)
        {
            _schowek = new GameObject("Schowek").transform;
            _schowek.SetParent(rodzic, false);
            _schowek.gameObject.SetActive(false);
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
                o = wpis.Sciezka.StartsWith("zloz:") ? Zloz(wpis) : Pojedynczy(wpis);
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
            return Opakuj(o);
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
            return korzen;
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
