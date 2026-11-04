using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ZapiszOsade(katalog);
            Console.WriteLine($"Gotowe: {katalog} (pozycja {s.X:F1}, {s.Y:F1}, biom {s.Swiat.Biom((int)s.X, (int)s.Y)})");
            return 0;
        }

        /// <summary>Osada po kilkunastu dniach: ludzie przy pracy, magazyn, chata, otwarty panel (cały pulpit z napisami).</summary>
        private static void ZapiszOsade(string katalog)
        {
            Styl.Aktywny = Styl.Hybrydowy;
            var s = new SwiatWolny(42);
            s.Plecak["drewno"] = 40;
            s.Plecak["kamien"] = 20;
            s.Plecak["wlokno"] = 6;
            s.Kierunek = (1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            s.Kierunek = (0.0, 1.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 3 });
            s.Kierunek = (-1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 4 });
            s.Aktualizuj(0.05, new Wejscie { Zloz = true });
            s.Obozowisko.Surowce["zywnosc"] = 300;
            for (int dzien = s.Dzien; s.Dzien < dzien + 14;)
            {
                s.Glod = s.Energia = s.Hp = 100.0;
                for (int i = 0; i < 200; i++)
                {
                    s.Aktualizuj(0.1, default);
                }
            }
            string[] zawody = { "drwal", "kamieniarz", "mysliwy", "zielarz", "drwal" };
            for (int i = 0; i < s.Obozowisko.Osadnicy.Count; i++)
            {
                Osada.UstawZajecie(s.Obozowisko, s.Obozowisko.Osadnicy[i], zawody[i % zawody.Length]);
            }
            s.Godzina = 11.0;
            for (int i = 0; i < 400; i++)
            {
                s.Aktualizuj(0.05, default);
            }
            var scena = new ScenaWolna();
            var pulpit = new Pulpit();
            Klatka(katalog, "wolny-osada", pulpit, scena, s);
            s.Aktualizuj(0.01, new Wejscie { Panel = true });
            Klatka(katalog, "wolny-panel", pulpit, scena, s);
            s.Aktualizuj(0.01, new Wejscie { Klik = "a:buduj" });
            Klatka(katalog, "wolny-tryb-budowy", pulpit, scena, s);
            s.Plecak["drewno"] = 8;
            s.Plecak["wlokno"] = 6;
            s.Aktualizuj(0.01, new Wejscie { Klik = "b:namiot" });
            (int X, int Y) wolny = (0, 0);
            for (int r = 2; r <= 5 && s.PowodBudowy("namiot", wolny.X, wolny.Y) != null; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        if (s.PowodBudowy("namiot", (int)s.X + x, (int)s.Y + y) == null)
                        {
                            wolny = ((int)s.X + x, (int)s.Y + y);
                        }
                    }
                }
            }
            s.Aktualizuj(0.01, new Wejscie { Kursor = wolny });
            Klatka(katalog, "wolny-duch-zielony", pulpit, scena, s);
            s.Aktualizuj(0.01, new Wejscie { Kursor = s.Budowle.Keys.First() });
            Klatka(katalog, "wolny-duch-czerwony", pulpit, scena, s);
        }

        private static void Klatka(string katalog, string nazwa, Pulpit pulpit, ScenaWolna scena, SwiatWolny s)
        {
            List<Napis> napisy = pulpit.ZlozWolny(scena, s, 7.0, WidokWolny.Hud(s), WidokWolny.Plecak(s),
                                                  WidokWolny.Dziennik(s), WidokWolny.Podpowiedz(s), WidokWolny.Panel(s),
                                                  KafelkiWolne.Z(s));
            Podglad.ZapiszKlatke(katalog, nazwa, pulpit, napisy);
        }

        private static void Zapisz(string katalog, string nazwa, ScenaWolna scena, SwiatWolny s, double t)
        {
            var plotno = new Plotno(ScenaWolna.Szer, ScenaWolna.Wys, przezroczyste: false);
            scena.Rysuj(plotno, s, t);
            File.WriteAllBytes(Path.Combine(katalog, nazwa + ".raw"), plotno.Dane);
        }
    }
}
