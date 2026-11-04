using UnityEngine;

namespace GraSimply.Gra.Trojwymiar
{
    /// <summary>Filtr na obrazie kamery 3D: poświata, kolor i winieta. Interfejs leży wyżej, więc zostaje ostry.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class FiltrObrazu : MonoBehaviour
    {
        public Material Material;

        /// <summary>0 dzień, 1 noc: chłodniejszy obraz.</summary>
        public float Noc;

        private void OnRenderImage(RenderTexture zrodlo, RenderTexture cel)
        {
            if (Material == null)
            {
                Graphics.Blit(zrodlo, cel);
                return;
            }
            int w = Mathf.Max(1, zrodlo.width / 4), h = Mathf.Max(1, zrodlo.height / 4);
            RenderTexture jasne = RenderTexture.GetTemporary(w, h, 0, zrodlo.format);
            RenderTexture pion = RenderTexture.GetTemporary(w, h, 0, zrodlo.format);
            RenderTexture poziom = RenderTexture.GetTemporary(w, h, 0, zrodlo.format);
            jasne.filterMode = pion.filterMode = poziom.filterMode = FilterMode.Bilinear;

            Graphics.Blit(zrodlo, jasne, Material, 0);
            Graphics.Blit(jasne, poziom, Material, 1);
            Graphics.Blit(poziom, pion, Material, 2);

            Material.SetTexture("_Poswiata", pion);
            Material.SetFloat("_Noc", Noc);
            Graphics.Blit(zrodlo, cel, Material, 3);

            RenderTexture.ReleaseTemporary(jasne);
            RenderTexture.ReleaseTemporary(pion);
            RenderTexture.ReleaseTemporary(poziom);
        }
    }
}
