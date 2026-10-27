using System;
using System.IO;
using GraSimply.Grafika;
using GraSimply.Grafika.Wolny;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>Klatki sceny „Zomboid fantasy” do obejrzenia: surowe bajty RGBA + wymiary (zamienia je podglad.py).</summary>
    public static class PodgladWolny
    {
        public static int Zapisz(string katalog)
        {
            Directory.CreateDirectory(katalog);
            var scena = new ScenaWolna();
            var s = new SwiatWolny(42);
            s.Plecak["drewno"] = 20;
            s.Plecak["kamien"] = 6;
            s.Plecak["wlokno"] = 12;
            s.Plecak["jagody"] = 3;
            s.X = 0.5;
            s.Y = 0.5;
            s.Kierunek = (1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            s.Kierunek = (0.0, 1.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 3 });
            s.Kierunek = (-1.0, -1.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 1 });

            Zapisz(katalog, "wolny-dzien", scena, s, 3.0);
            // Spacer w głąb lasu: bohater idzie na wschód, aż trafi na gęstszy teren.
            for (int i = 0; i < 700; i++)
            {
                s.Aktualizuj(0.05, new Wejscie { Dx = -1, Dy = -0.4 });
            }
            Zapisz(katalog, "wolny-spacer", scena, s, 4.0);
            s.Godzina = 22.0;
            for (int i = 0; i < 3; i++)
            {
                s.Aktualizuj(0.05, default);
            }
            Zapisz(katalog, "wolny-noc", scena, s, 5.0);
            Console.WriteLine($"Gotowe: {katalog} (pozycja {s.X:F1}, {s.Y:F1}, biom {s.Swiat.Biom((int)s.X, (int)s.Y)})");
            return 0;
        }

        private static void Zapisz(string katalog, string nazwa, ScenaWolna scena, SwiatWolny s, double t)
        {
            var plotno = new Plotno(ScenaWolna.Szer, ScenaWolna.Wys, przezroczyste: false);
            scena.Rysuj(plotno, s, t);
            File.WriteAllBytes(Path.Combine(katalog, nazwa + ".raw"), plotno.Dane);
        }
    }
}
