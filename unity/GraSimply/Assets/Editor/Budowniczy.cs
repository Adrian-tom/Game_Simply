using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GraSimply.Edytor
{
    /// <summary>
    /// Buduje grę jako zwykły program na Windows: menu „Gra Simply → Zbuduj grę (Windows)”.
    ///
    /// Po zbudowaniu gra odpala się jak każda inna — podwójnym kliknięciem w
    /// <c>GraSimply.exe</c>, bez Unity, bez Huba i bez czekania na import assetów.
    /// Edytor jest potrzebny tylko do zrobienia tej paczki.
    ///
    /// Gotowa gra ląduje w <c>Build/Windows/</c> obok katalogu <c>Assets</c>.
    /// Tego katalogu nie wersjonujemy — paczka waży dziesiątki megabajtów
    /// i odtwarza się z kodu w kilkadziesiąt sekund.
    /// </summary>
    public static class Budowniczy
    {
        private const string Scena = "Assets/Scenes/Gra.unity";
        private const string Katalog = "Build/Windows";
        private const string Plik = "GraSimply.exe";

        [MenuItem("Gra Simply/Zbuduj grę (Windows)", false, 20)]
        public static void Zbuduj()
        {
            if (!File.Exists(Scena))
            {
                Debug.LogError(
                    $"Nie ma sceny {Scena}. Zrób ją najpierw: Gra Simply → Utwórz scenę gry.");
                return;
            }

            // Lista scen do budowania bywa pusta — wtedy zbudowana gra wstaje
            // z pustym ekranem zamiast menu. Pilnujemy tego przy każdym budowaniu,
            // a nie tylko przy generowaniu sceny.
            DopilnujSceny();

            string korzen = Directory.GetParent(Application.dataPath)!.FullName;
            string wyjscie = Path.Combine(korzen, Katalog);
            Directory.CreateDirectory(wyjscie);

            var opcje = new BuildPlayerOptions
            {
                scenes = new[] { Scena },
                locationPathName = Path.Combine(wyjscie, Plik),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport raport = BuildPipeline.BuildPlayer(opcje);
            BuildSummary podsumowanie = raport.summary;

            if (podsumowanie.result != BuildResult.Succeeded)
            {
                Debug.LogError(
                    $"Budowanie nie wyszło ({podsumowanie.result}), " +
                    $"błędów: {podsumowanie.totalErrors}.");
                return;
            }

            double megabajty = podsumowanie.totalSize / 1024d / 1024d;
            Debug.Log(
                $"Gra zbudowana: {opcje.locationPathName}\n" +
                $"Rozmiar {megabajty:0.0} MB, czas {podsumowanie.totalTime.TotalSeconds:0} s. " +
                "Odpala się podwójnym kliknięciem.");
            // W trybie wsadowym (budowanie z wiersza poleceń) nie ma komu
            // patrzeć na Eksploratora — otwieramy go tylko z edytora.
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(opcje.locationPathName);
            }
        }

        /// <summary>Wstawia scenę gry do ustawień budowania, jeśli jej tam nie ma.</summary>
        private static void DopilnujSceny()
        {
            EditorBuildSettingsScene[] obecne = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene s in obecne)
            {
                if (s.path == Scena)
                {
                    return;
                }
            }

            var nowe = new EditorBuildSettingsScene[obecne.Length + 1];
            obecne.CopyTo(nowe, 0);
            nowe[obecne.Length] = new EditorBuildSettingsScene(Scena, true);
            EditorBuildSettings.scenes = nowe;
            Debug.Log($"Dopisano {Scena} do ustawień budowania.");
        }
    }
}
