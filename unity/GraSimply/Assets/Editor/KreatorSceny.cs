using GraSimply.Gra;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GraSimply.Edytor
{
    /// <summary>
    /// Tworzy scenę gry jednym kliknięciem: menu „Gra Simply → Utwórz scenę gry”.
    ///
    /// Scena gry to jeden obiekt ze skryptem <see cref="SilnikGry"/> — resztę
    /// (kanwę, obraz, napisy) silnik buduje w kodzie przy starcie. Dlatego
    /// scenę opłaca się generować, a nie trzymać w repozytorium jako plik,
    /// który może się rozjechać między wersjami Unity.
    ///
    /// Gotowa scena leży w <c>Assets/Scenes/Gra.unity</c>. Jeśli ta w repozytorium
    /// nie chce się otworzyć (inna wersja edytora), wystarczy użyć tego menu —
    /// nadpisze ją świeżą, zapisaną przez twoje Unity.
    /// </summary>
    public static class KreatorSceny
    {
        private const string Katalog = "Assets/Scenes";
        private const string Sciezka = Katalog + "/Gra.unity";

        [MenuItem("Gra Simply/Utwórz scenę gry", false, 10)]
        public static void Utworz()
        {
            if (!AssetDatabase.IsValidFolder(Katalog))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            UnityEngine.SceneManagement.Scene scena = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var obiekt = new GameObject("Gra");
            obiekt.AddComponent<SilnikGry>();
            Undo.RegisterCreatedObjectUndo(obiekt, "Obiekt gry");

            bool zapisana = EditorSceneManager.SaveScene(scena, Sciezka);
            if (!zapisana)
            {
                Debug.LogError($"Nie udało się zapisać sceny w {Sciezka}.");
                return;
            }

            DodajDoUstawienBudowania();
            Debug.Log($"Scena gotowa: {Sciezka}. Naciśnij Play.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(Sciezka));
        }

        /// <summary>Bez tego zbudowana gra wystartowałaby z pustą scenę.</summary>
        private static void DodajDoUstawienBudowania()
        {
            EditorBuildSettingsScene[] obecne = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene s in obecne)
            {
                if (s.path == Sciezka)
                {
                    return;
                }
            }
            var nowe = new EditorBuildSettingsScene[obecne.Length + 1];
            obecne.CopyTo(nowe, 0);
            nowe[obecne.Length] = new EditorBuildSettingsScene(Sciezka, true);
            EditorBuildSettings.scenes = nowe;
        }
    }
}
