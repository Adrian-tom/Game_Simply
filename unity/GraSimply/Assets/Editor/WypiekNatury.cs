using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GraSimply.Edytor
{
    /// <summary>
    /// Wypieka realistyczne modele natury do obrazków oglądanych dokładnie z kąta kamery gry. Kamera się nie
    /// obraca, więc drzewo z milionami trójkątów kosztuje w grze dwa trójkąty, a wygląda jak pełny model.
    /// Najpierw <c>unity/narzedzia/pobierz_nature.py</c>, potem
    /// <c>-batchmode -executeMethod GraSimply.Edytor.WypiekNatury.Zrob</c> (bez <c>-nographics</c>).
    /// Wynik: <c>Resources/Natura</c> (obrazki) i <c>natura.txt</c> (punkt podstawy i rozmiar).
    /// </summary>
    public static class WypiekNatury
    {
        private const string Zrodla = "Assets/_Wypiek";
        private const string Cel = "Assets/Resources/Natura";
        private const float PochylenieKamery = 32f;
        private const float ObrotKamery = -45f;

        /// <summary>Model, ile ustawień z różnych stron, ile wariantów z paczki i bok obrazka.</summary>
        private static readonly (string Nazwa, int Obroty, int Warianty, int Bok)[] Lista =
        {
            ("island_tree_01", 2, 1, 512), ("island_tree_02", 2, 1, 512), ("tree_small_02", 2, 1, 512),
            ("fir_sapling_medium", 1, 3, 512),
            ("shrub_01", 1, 2, 512), ("shrub_03", 1, 3, 384), ("shrub_04", 1, 3, 384), ("fern_02", 1, 3, 384),
            ("rock_moss_set_01", 1, 3, 384), ("rock_moss_set_02", 1, 3, 384), ("boulder_01", 2, 1, 512),
            ("rock_07", 2, 1, 384), ("stone_01", 1, 1, 384),
            ("grass_medium_01", 1, 4, 384), ("dandelion_01", 1, 2, 256), ("nettle_plant", 1, 3, 384),
        };

        public static void Zrob()
        {
            Directory.CreateDirectory(Cel);
            var opis = new StringBuilder();
            foreach ((string nazwa, int obroty, int warianty, int bok) in Lista)
            {
                int numer = 0;
                for (int w = 0; w < warianty; w++)
                {
                    for (int i = 0; i < obroty; i++)
                    {
                        Wypiecz(nazwa, numer++, w, warianty, 360f / obroty * i + 25f, bok, opis);
                    }
                }
            }
            File.WriteAllText(Cel + "/natura.txt", opis.ToString());
            AssetDatabase.Refresh();
            Debug.Log("Wypiek natury gotowy");
        }

        private static void Wypiecz(string nazwa, int numer, int wariant, int warianty, float obrot, int bok, StringBuilder opis)
        {
            string plik = $"{Zrodla}/{nazwa}/{nazwa}.fbx";
            var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(plik);
            if (wzor == null)
            {
                Debug.LogError("Nie ma modelu " + plik);
                return;
            }
            GameObject o = Object.Instantiate(wzor);
            o.transform.position = Vector3.zero;
            o.transform.rotation = Quaternion.Euler(0f, obrot, 0f) * wzor.transform.rotation;
            UstawMaterialy(o, nazwa);
            ZostawWariant(o, wariant, warianty);

            Bounds b = Granice(o);
            float wysokosc = b.size.y;
            // Podstawa to środek modelu na ziemi, także gdy autor przesunął go względem zera.
            Vector3 podstawa = new Vector3(b.center.x, b.min.y, b.center.z);

            var slonce = new GameObject("Slonce").AddComponent<Light>();
            slonce.type = LightType.Directional;
            slonce.intensity = 1.2f;
            slonce.color = new Color(1f, 0.96f, 0.88f);
            slonce.shadows = LightShadows.Soft;
            slonce.transform.rotation = Quaternion.Euler(52f, 20f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.74f, 0.76f);
            QualitySettings.shadowDistance = Mathf.Max(100f, b.size.magnitude * 6f);
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

            var kamera = new GameObject("Kamera").AddComponent<Camera>();
            kamera.orthographic = true;
            kamera.clearFlags = CameraClearFlags.SolidColor;
            kamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            kamera.transform.rotation = Quaternion.Euler(PochylenieKamery, ObrotKamery, 0f);
            float zasieg = b.size.magnitude * 0.62f;
            kamera.orthographicSize = zasieg;
            kamera.transform.position = b.center - kamera.transform.forward * (zasieg * 4f);
            kamera.nearClipPlane = 0.01f;
            kamera.farClipPlane = zasieg * 10f;

            int duzy = bok * 2;
            var cel = new RenderTexture(duzy, duzy, 24, RenderTextureFormat.ARGB32);
            kamera.targetTexture = cel;
            kamera.Render();
            RenderTexture.active = cel;
            var obraz = new Texture2D(duzy, duzy, TextureFormat.RGBA32, false);
            obraz.ReadPixels(new Rect(0, 0, duzy, duzy), 0, 0);
            obraz.Apply();
            RenderTexture.active = null;

            Color32[] piks = Pomniejsz(obraz.GetPixels32(), duzy);
            (int x0, int y0, int x1, int y1) = Kadr(piks, bok);
            int szer = x1 - x0 + 1, wys = y1 - y0 + 1;
            var wynik = new Texture2D(szer, wys, TextureFormat.RGBA32, false);
            var wyciety = new Color32[szer * wys];
            for (int y = 0; y < wys; y++)
            {
                System.Array.Copy(piks, (y + y0) * bok + x0, wyciety, y * szer, szer);
            }
            if (nazwa.Contains("tree") || nazwa.Contains("fir_"))
            {
                Zageszcz(wyciety, szer, wys);
            }
            Podbarw(wyciety);
            wynik.SetPixels32(wyciety);
            string klucz = $"{nazwa}_{numer}";
            File.WriteAllBytes($"{Cel}/{klucz}.png", wynik.EncodeToPNG());

            // Rozmiary w wysokościach modelu: gra skaluje obrazek do żądanej wysokości w świecie.
            Vector3 p = kamera.WorldToViewportPoint(podstawa);
            float naPiksel = zasieg * 2f / bok;
            float ax = (p.x * bok - x0) / szer, ay = (p.y * bok - y0) / wys;
            opis.AppendLine(string.Join(";", klucz, F(ax), F(ay), F(szer * naPiksel / wysokosc), F(wys * naPiksel / wysokosc)));

            kamera.targetTexture = null;
            Object.DestroyImmediate(cel);
            Object.DestroyImmediate(obraz);
            Object.DestroyImmediate(wynik);
            Object.DestroyImmediate(kamera.gameObject);
            Object.DestroyImmediate(slonce.gameObject);
            Object.DestroyImmediate(o);
        }

        private static string F(float v)
        {
            return v.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void UstawMaterialy(GameObject o, string nazwa)
        {
            string[] tekstury = Directory.GetFiles($"{Zrodla}/{nazwa}/textures").Where(f => !f.EndsWith(".meta"))
                                         .Select(f => f.Replace('\\', '/')).ToArray();
            Shader shader = Shader.Find("GraSimply/Wypiek");
            foreach (Renderer r in o.GetComponentsInChildren<Renderer>())
            {
                Material[] m = r.sharedMaterials;
                for (int i = 0; i < m.Length; i++)
                {
                    var nowy = new Material(shader);
                    string czesc = m[i] == null ? "" : m[i].name;
                    Przypisz(nowy, "_MainTex", Szukaj(tekstury, czesc, "_diff_"));
                    Przypisz(nowy, "_Alfa", Szukaj(tekstury, czesc, "_alpha_", false));
                    Przypisz(nowy, "_Normalne", Szukaj(tekstury, czesc, "_nor_gl_"));
                    m[i] = nowy;
                }
                r.sharedMaterials = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        /// <summary>Paczki trzymają kilka roślin i poziomy szczegółów obok siebie: zostaje jedna, najdokładniejsza.</summary>
        private static void ZostawWariant(GameObject o, int wariant, int warianty)
        {
            List<Renderer> dokladne = o.GetComponentsInChildren<Renderer>().Where(r =>
            {
                int lod = r.name.LastIndexOf("_LOD", System.StringComparison.Ordinal);
                return lod < 0 || r.name.EndsWith("_LOD0");
            }).ToList();
            Renderer wybrany = warianty > 1 ? dokladne[wariant * dokladne.Count / warianty] : null;
            foreach (Renderer r in o.GetComponentsInChildren<Renderer>())
            {
                r.enabled = wybrany != null ? r == wybrany : dokladne.Contains(r);
            }
        }

        private static void Przypisz(Material m, string pole, string sciezka)
        {
            if (sciezka != null)
            {
                m.SetTexture(pole, AssetDatabase.LoadAssetAtPath<Texture2D>(sciezka));
            }
        }

        /// <summary>Tekstura danej części modelu („…_leaves_diff_1k”) albo wspólna, gdy model ma jedną.</summary>
        private static string Szukaj(string[] tekstury, string material, string rodzaj, bool zapas = true)
        {
            List<string> pasujace = tekstury.Where(t => Path.GetFileName(t).Contains(rodzaj)).ToList();
            string cala = material.ToLowerInvariant();
            // Najdłuższy pasujący przedrostek: „drzewo_leaves” ma wziąć „drzewo_leaves_diff”, a nie „drzewo_diff”.
            string dokladna = pasujace.Where(t =>
            {
                string n = Path.GetFileName(t).ToLowerInvariant();
                n = n.Substring(0, n.IndexOf(rodzaj, System.StringComparison.Ordinal));
                return cala == n || cala.StartsWith(n + "_") || cala.StartsWith(n + ".");
            }).OrderByDescending(t => Path.GetFileName(t).Length).FirstOrDefault();
            if (dokladna != null)
            {
                return dokladna;
            }
            if (!zapas)
            {
                // Alfa należy tylko do części z liśćmi: kora bez niej ma zostać pełna.
                return pasujace.Count == 1 && tekstury.Count(t => Path.GetFileName(t).Contains("_diff_")) == 1 ? pasujace[0] : null;
            }
            // Najkrótsza nazwa to część główna („tree_diff” przed „tree_leaves_diff”).
            return pasujace.OrderBy(t => t.Length).FirstOrDefault();
        }

        /// <summary>Średnia 2×2 ważona alfą: krawędzie liści są miękkie i bez czarnej obwódki.</summary>
        private static Color32[] Pomniejsz(Color32[] p, int duzy)
        {
            int bok = duzy / 2;
            var w = new Color32[bok * bok];
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    int r = 0, g = 0, b = 0, a = 0;
                    for (int j = 0; j < 2; j++)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Color32 c = p[(y * 2 + j) * duzy + x * 2 + i];
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    }
                    w[y * bok + x] = a == 0 ? new Color32(0, 0, 0, 0)
                                            : new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / 4));
                }
            }
            return w;
        }

        private static (int, int, int, int) Kadr(Color32[] p, int bok)
        {
            int x0 = bok, y0 = bok, x1 = 0, y1 = 0;
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    if (p[y * bok + x].a > 8)
                    {
                        x0 = Mathf.Min(x0, x);
                        x1 = Mathf.Max(x1, x);
                        y0 = Mathf.Min(y0, y);
                        y1 = Mathf.Max(y1, y);
                    }
                }
            }
            return x1 < x0 ? (0, 0, bok - 1, bok - 1) : (x0, y0, x1, y1);
        }

        /// <summary>
        /// Prześwity w koronie wypełnia ciemna głębia liści: z daleka drobne listki znikają, a drzewo
        /// ma nadal pełną bryłę zamiast rzadkiej mgiełki, przez którą widać trawę.
        /// </summary>
        private static void Zageszcz(Color32[] p, int szer, int wys)
        {
            const int promien = 4;
            var suma = new int[(szer + 1) * (wys + 1)];
            long r = 0, g = 0, b = 0, n = 0;
            for (int y = 0; y < wys; y++)
            {
                for (int x = 0; x < szer; x++)
                {
                    Color32 c = p[y * szer + x];
                    bool jest = c.a > 96;
                    suma[(y + 1) * (szer + 1) + x + 1] = (jest ? 1 : 0) + suma[y * (szer + 1) + x + 1]
                                                         + suma[(y + 1) * (szer + 1) + x] - suma[y * (szer + 1) + x];
                    if (jest)
                    {
                        r += c.r;
                        g += c.g;
                        b += c.b;
                        n++;
                    }
                }
            }
            if (n == 0)
            {
                return;
            }
            var glebia = new Color32((byte)(r / n * 0.42f), (byte)(g / n * 0.46f), (byte)(b / n * 0.38f), 255);
            for (int y = 0; y < wys; y++)
            {
                for (int x = 0; x < szer; x++)
                {
                    if (p[y * szer + x].a >= 200)
                    {
                        continue;
                    }
                    int x0 = Mathf.Max(0, x - promien), x1 = Mathf.Min(szer, x + promien + 1);
                    int y0 = Mathf.Max(0, y - promien), y1 = Mathf.Min(wys, y + promien + 1);
                    int ile = suma[y1 * (szer + 1) + x1] - suma[y0 * (szer + 1) + x1] - suma[y1 * (szer + 1) + x0] + suma[y0 * (szer + 1) + x0];
                    if (ile > (x1 - x0) * (y1 - y0) * 0.34f)
                    {
                        p[y * szer + x] = glebia;
                    }
                }
            }
        }

        /// <summary>Przezroczyste piksele dostają średni kolor obrazka: mipmapy nie ciągną czerni na krawędzie.</summary>
        private static void Podbarw(Color32[] p)
        {
            long r = 0, g = 0, b = 0, n = 0;
            foreach (Color32 c in p)
            {
                if (c.a > 128)
                {
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    n++;
                }
            }
            if (n == 0)
            {
                return;
            }
            var srednia = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 0);
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].a == 0)
                {
                    p[i] = srednia;
                }
            }
        }

        private static Bounds Granice(GameObject o)
        {
            Renderer[] r = o.GetComponentsInChildren<Renderer>().Where(x => x.enabled).ToArray();
            Bounds b = r[0].bounds;
            foreach (Renderer x in r)
            {
                b.Encapsulate(x.bounds);
            }
            return b;
        }
    }
}
