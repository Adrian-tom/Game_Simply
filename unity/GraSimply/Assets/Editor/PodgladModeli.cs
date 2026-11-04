using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GraSimply.Edytor
{
    /// <summary>
    /// Plansze ze wszystkimi modelami z <c>Resources/Modele</c>: sprawdzenie importu, tekstur i rozmiarów
    /// bez otwierania edytora. Uruchamianie: <c>-batchmode -executeMethod GraSimply.Edytor.PodgladModeli.Zrob</c>
    /// (bez <c>-nographics</c>, bo kamera musi naprawdę rysować).
    /// </summary>
    public static class PodgladModeli
    {
        public static void Zrob()
        {
            string wyjscie = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "PodgladModeli");
            Directory.CreateDirectory(wyjscie);
            var raport = new StringBuilder();
            foreach (string katalog in new[] { "Postacie", "Budynki", "Oboz", "Wioska", "Bronie" })
            {
                Plansza(katalog, wyjscie, raport);
            }
            File.WriteAllText(Path.Combine(wyjscie, "rozmiary.txt"), raport.ToString());
            Debug.Log("Podglad modeli gotowy: " + wyjscie);
        }

        private static void Plansza(string katalog, string wyjscie, StringBuilder raport)
        {
            GameObject[] modele = Resources.LoadAll<GameObject>("Modele/" + katalog);
            if (modele.Length == 0)
            {
                raport.AppendLine($"{katalog}: brak modeli");
                return;
            }
            var korzen = new GameObject("Plansza");
            int bok = Mathf.CeilToInt(Mathf.Sqrt(modele.Length));
            float najwiekszy = 0.1f;
            var ustawione = modele.Select(m => Object.Instantiate(m, korzen.transform)).ToArray();
            for (int i = 0; i < ustawione.Length; i++)
            {
                ustawione[i].transform.position = Vector3.zero;
                Bounds b = Granice(ustawione[i]);
                najwiekszy = Mathf.Max(najwiekszy, Mathf.Max(b.size.x, b.size.z));
                raport.AppendLine($"{katalog}/{modele[i].name}: rozmiar {b.size.x:F2}x{b.size.y:F2}x{b.size.z:F2} srodek {b.center.x:F2},{b.center.y:F2},{b.center.z:F2}"
                                  + $" materialy {string.Join(",", ustawione[i].GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Select(m => m == null ? "BRAK" : m.name + (m.mainTexture == null ? "(bez tekstury)" : "")).Distinct())}"
                                  + $" klipy {string.Join(",", ustawione[i].GetComponentsInChildren<Animation>().SelectMany(a => a.Cast<AnimationState>()).Select(s => s.name))}");
            }
            float krok = najwiekszy * 1.25f;
            for (int i = 0; i < ustawione.Length; i++)
            {
                ustawione[i].transform.position = new Vector3((i % bok) * krok, 0f, (i / bok) * krok);
            }

            var swiatlo = new GameObject("Slonce").AddComponent<Light>();
            swiatlo.type = LightType.Directional;
            swiatlo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            var kamera = new GameObject("Kamera").AddComponent<Camera>();
            kamera.orthographic = true;
            kamera.clearFlags = CameraClearFlags.SolidColor;
            kamera.backgroundColor = new Color(0.16f, 0.2f, 0.18f);
            Vector3 srodek = new Vector3((bok - 1) * krok / 2f, 0f, ((ustawione.Length - 1) / bok) * krok / 2f);
            kamera.transform.rotation = Quaternion.Euler(35f, -45f, 0f);
            kamera.transform.position = srodek - kamera.transform.forward * (bok * krok * 3f);
            kamera.orthographicSize = bok * krok * 0.72f;
            kamera.farClipPlane = bok * krok * 8f;

            var cel = new RenderTexture(1400, 1000, 24);
            kamera.targetTexture = cel;
            kamera.Render();
            RenderTexture.active = cel;
            var obraz = new Texture2D(cel.width, cel.height, TextureFormat.RGB24, false);
            obraz.ReadPixels(new Rect(0, 0, cel.width, cel.height), 0, 0);
            obraz.Apply();
            File.WriteAllBytes(Path.Combine(wyjscie, katalog + ".png"), obraz.EncodeToPNG());
            RenderTexture.active = null;
            kamera.targetTexture = null;
            Object.DestroyImmediate(cel);
            Object.DestroyImmediate(kamera.gameObject);
            Object.DestroyImmediate(swiatlo.gameObject);
            Object.DestroyImmediate(korzen);
        }

        private static Bounds Granice(GameObject o)
        {
            Renderer[] r = o.GetComponentsInChildren<Renderer>();
            if (r.Length == 0)
            {
                return new Bounds(o.transform.position, Vector3.zero);
            }
            Bounds b = r[0].bounds;
            foreach (Renderer x in r)
            {
                b.Encapsulate(x.bounds);
            }
            return b;
        }
    }
}
