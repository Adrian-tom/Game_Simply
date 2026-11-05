using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>
    /// Życie w powietrzu: cienie chmur (maska na słońcu) oraz drobiny wokół kadru. Za dnia to unoszące się
    /// pyłki, nocą świetliki. Czysto wizualne: nie wpływa na zasady gry.
    /// </summary>
    public sealed class Klimat
    {
        private const float RozmiarChmur = 46f;
        private const float WiatrChmur = 0.55f;

        private readonly Light _slonce;
        private readonly ParticleSystem _drobiny;
        private readonly Material _material;
        private Vector3 _srodek;

        public Klimat(Transform rodzic, Light slonce, Material drobina)
        {
            _slonce = slonce;
            _slonce.cookie = Chmury();
            _slonce.cookieSize = RozmiarChmur;

            var o = new GameObject("Drobiny");
            o.transform.SetParent(rodzic, false);
            _drobiny = o.AddComponent<ParticleSystem>();
            _drobiny.Stop();
            ParticleSystem.MainModule m = _drobiny.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            m.maxParticles = 120;
            m.gravityModifier = 0f;
            ParticleSystem.EmissionModule e = _drobiny.emission;
            e.rateOverTime = 15f;
            ParticleSystem.ShapeModule s = _drobiny.shape;
            s.shapeType = ParticleSystemShapeType.Box;
            s.scale = new Vector3(24f, 2.4f, 24f);
            ParticleSystem.NoiseModule n = _drobiny.noise;
            n.enabled = true;
            n.strength = 0.5f;
            n.frequency = 0.35f;
            n.scrollSpeed = 0.2f;
            ParticleSystem.ColorOverLifetimeModule c = _drobiny.colorOverLifetime;
            c.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            c.color = g;
            ParticleSystemRenderer r = o.GetComponent<ParticleSystemRenderer>();
            _material = new Material(drobina) { mainTexture = Kropka() };
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _drobiny.Play();
        }

        /// <summary>Wołane raz na klatkę z punktem, na który patrzy kamera.</summary>
        public void Aktualizuj(Vector3 srodek, float jasnosc, double t)
        {
            // Maska jest przypięta do słońca: przesuwanie go przesuwa chmury po całym świecie.
            _slonce.transform.position = new Vector3((float)t * WiatrChmur, 40f, (float)t * WiatrChmur * 0.45f);
            if ((srodek - _srodek).sqrMagnitude > 1e-4f)
            {
                _drobiny.transform.position = srodek + Vector3.up * 1.2f;
                _srodek = srodek;
            }
            ParticleSystem.MainModule m = _drobiny.main;
            // Nocą świetliki: złotozielone i większe; za dnia ledwie widoczny pyłek.
            m.startColor = Color.Lerp(new Color(0.75f, 1f, 0.35f, 1f), new Color(1f, 0.97f, 0.8f, 0.42f), jasnosc);
            m.startSize = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.09f, 0.04f, jasnosc), Mathf.Lerp(0.18f, 0.08f, jasnosc));
        }

        /// <summary>Płaty światła i cienia z kilku warstw szumu. Tekstura się zawija, więc nie widać szwu.</summary>
        private static Texture2D Chmury()
        {
            const int bok = 256;
            var t = new Texture2D(bok, bok, TextureFormat.Alpha8, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var p = new Color32[bok * bok];
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    float a = Warstwa(x, y, bok, 3) * 0.62f + Warstwa(x, y, bok, 7) * 0.28f + Warstwa(x, y, bok, 17) * 0.1f;
                    float cien = Mathf.SmoothStep(0.42f, 0.66f, a);
                    p[y * bok + x] = new Color32(0, 0, 0, (byte)(255f * Mathf.Lerp(1f, 0.58f, cien)));
                }
            }
            t.SetPixels32(p);
            t.Apply(false);
            return t;
        }

        /// <summary>Szum okresowy: mieszanie czterech rogów torusa daje brak szwu na krawędzi tekstury.</summary>
        private static float Warstwa(int x, int y, int bok, int skala)
        {
            float u = (float)x / bok, v = (float)y / bok;
            float a = Mathf.PerlinNoise(u * skala, v * skala);
            float b = Mathf.PerlinNoise((u - 1f) * skala + 100f, v * skala);
            float c = Mathf.PerlinNoise(u * skala, (v - 1f) * skala + 100f);
            float d = Mathf.PerlinNoise((u - 1f) * skala + 100f, (v - 1f) * skala + 100f);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        public static Texture2D Kropka()
        {
            const int bok = 32;
            var t = new Texture2D(bok, bok, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var p = new Color32[bok * bok];
            for (int y = 0; y < bok; y++)
            {
                for (int x = 0; x < bok; x++)
                {
                    float d = new Vector2(x - bok / 2f + 0.5f, y - bok / 2f + 0.5f).magnitude / (bok / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    p[y * bok + x] = new Color32(255, 255, 255, (byte)(255f * a * a));
                }
            }
            t.SetPixels32(p);
            t.Apply(false);
            return t;
        }
    }
}
