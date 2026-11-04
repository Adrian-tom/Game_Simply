using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GraSimply.Edytor
{
    /// <summary>
    /// Ustawienia importu modeli z <c>Assets/Resources/Modele</c>. Dzięki temu paczki autorów można
    /// po prostu skopiować do katalogu — nikt nie musi niczego wyklikiwać w inspektorze.
    /// Postacie i animacje idą jako Legacy: klipy z osobnych plików działają wtedy na każdej postaci
    /// ze wspólnym szkieletem Rig_Medium, bez kontrolera animacji.
    /// </summary>
    public sealed class ImportModeli : AssetPostprocessor
    {
        private const string Korzen = "Assets/Resources/Modele/";
        private const string Tekstury = "Assets/Resources/Tekstury/";

        private void OnPreprocessModel()
        {
            if (assetPath.StartsWith("Assets/_Wypiek/"))
            {
                var ciezki = (ModelImporter)assetImporter;
                ciezki.importAnimation = false;
                ciezki.animationType = ModelImporterAnimationType.None;
                ciezki.indexFormat = ModelImporterIndexFormat.UInt32;
                ciezki.meshCompression = ModelImporterMeshCompression.Off;
                return;
            }
            if (!assetPath.StartsWith(Korzen))
            {
                return;
            }
            var importer = (ModelImporter)assetImporter;
            bool postac = assetPath.Contains("/Postacie/");
            importer.animationType = postac ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            importer.importAnimation = postac;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,
                                             ModelImporterMaterialSearch.Local);
        }

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/_Wypiek/") && assetPath.Contains("_nor_gl_"))
            {
                ((TextureImporter)assetImporter).textureType = TextureImporterType.NormalMap;
                return;
            }
            if (assetPath.StartsWith("Assets/Resources/Natura/"))
            {
                var natura = (TextureImporter)assetImporter;
                natura.alphaIsTransparency = true;
                // Bez tego drobne liście w mniejszych mipmapach znikają i korona robi się dziurawa.
                natura.mipMapsPreserveCoverage = true;
                natura.alphaTestReferenceValue = 0.3f;
                natura.textureCompression = TextureImporterCompression.Compressed;
                natura.wrapMode = TextureWrapMode.Clamp;
                return;
            }
            if (assetPath.StartsWith(Tekstury))
            {
                var tekstura = (TextureImporter)assetImporter;
                tekstura.textureType = assetPath.Contains("_Normalne") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                tekstura.maxTextureSize = 1024;
                tekstura.wrapMode = TextureWrapMode.Repeat;
                tekstura.anisoLevel = 4;
                tekstura.textureCompression = TextureImporterCompression.Compressed;
                return;
            }
            if (!assetPath.StartsWith(Korzen))
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }

        /// <summary>Atlasy paczek mają małe, ostre plamy koloru: bez mipmap barwy nie zlewają się w oddali.</summary>
        private void OnPostprocessMaterial(Material material)
        {
            if (!assetPath.StartsWith(Korzen))
            {
                return;
            }
            string katalog = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (material.mainTexture == null)
            {
                Texture2D tekstura = ZnajdzTeksture(katalog, material.name);
                if (tekstura != null)
                {
                    material.mainTexture = tekstura;
                    material.color = Color.white;
                }
            }
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
        }

        /// <summary>Tekstura z tego samego katalogu dobrana po nazwie materiału albo jedyna, jaka tam leży.</summary>
        private static Texture2D ZnajdzTeksture(string katalog, string nazwa)
        {
            string[] sciezki = AssetDatabase.FindAssets("t:Texture2D", new[] { katalog })
                                            .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            string szukana = nazwa.Replace("M_", "").Replace("MI_", "").ToLowerInvariant();
            string sciezka = sciezki.FirstOrDefault(s => System.IO.Path.GetFileNameWithoutExtension(s)
                                                                      .ToLowerInvariant().Contains(szukana));
            if (sciezka == null && sciezki.Length == 1)
            {
                sciezka = sciezki[0];
            }
            return sciezka == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(sciezka);
        }
    }
}
