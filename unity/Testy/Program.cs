using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GraSimply.Testy
{
    /// <summary>
    /// Punkt wejścia testów portu. Czyta wzorce z <c>unity/Testy/wzorce</c>
    /// (zrzucone przez <c>unity/narzedzia/wzorce.py</c>) i porównuje je
    /// z wynikami C#. Kod wyjścia 0 = wszystko zgodne.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] argumenty)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (argumenty.Length >= 1 && argumenty[0] == "--podglad")
            {
                string cel = argumenty.Length >= 2 ? argumenty[1] : "podglad";
                Console.WriteLine($"Renderuję klatki podglądu do {cel}…");
                return Podglad.Zapisz(cel);
            }

            string katalog = KatalogWzorcow(argumenty);
            Console.WriteLine($"Wzorce: {katalog}");

            TestyLosowania.Uruchom(Wczytaj(katalog, "losowanie.json"));
            TestyMapy.Uruchom(Wczytaj(katalog, "mapa.json"));
            TestyPostaci.Uruchom(Wczytaj(katalog, "postac.json"));
            TestyKonsoli.Uruchom();

            string piksele = Path.Combine(katalog, "piksele.json");
            if (File.Exists(piksele))
            {
                TestyGrafiki.Uruchom(Wczytaj(katalog, "piksele.json"),
                                     Wczytaj(katalog, "kafle.json"),
                                     Wczytaj(katalog, "sprite.json"));
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("(pominięto testy grafiki — brak wzorców, " +
                                  "uruchom wzorce.py z zainstalowanym pygame)");
            }

            return Harness.Podsumuj();
        }

        private static string KatalogWzorcow(string[] argumenty)
        {
            if (argumenty.Length > 0)
            {
                return argumenty[0];
            }
            // Z katalogu bin/... wracamy do źródeł projektu testowego.
            var katalog = new DirectoryInfo(AppContext.BaseDirectory);
            while (katalog != null)
            {
                string kandydat = Path.Combine(katalog.FullName, "wzorce");
                if (Directory.Exists(kandydat))
                {
                    return kandydat;
                }
                katalog = katalog.Parent;
            }
            throw new DirectoryNotFoundException(
                "Nie znalazłem katalogu wzorce/. Uruchom unity/narzedzia/wzorce.py " +
                "albo podaj ścieżkę jako argument.");
        }

        private static JsonElement Wczytaj(string katalog, string nazwa)
        {
            string sciezka = Path.Combine(katalog, nazwa);
            using var strumien = File.OpenRead(sciezka);
            using JsonDocument dokument = JsonDocument.Parse(strumien);
            return dokument.RootElement.Clone();
        }
    }
}
