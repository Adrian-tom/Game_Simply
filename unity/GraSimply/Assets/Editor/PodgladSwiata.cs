using System.Collections.Generic;
using System.IO;
using GraSimply.Gra.Trojwymiar;
using GraSimply.Grafika;
using GraSimply.Logika;
using GraSimply.Logika.Wolny;
using UnityEditor;
using UnityEngine;

namespace GraSimply.Edytor
{
    /// <summary>
    /// Zrzuty scen 3D bez uruchamiania gry: <c>-batchmode -executeMethod GraSimply.Edytor.PodgladSwiata.Zrob</c>
    /// (bez <c>-nographics</c>). Na obraz z kamery nakłada interfejs z pulpitu; napisy rysuje dopiero Unity UI,
    /// więc tu ich nie widać. Materiały sceny powstają tu przy pierwszym uruchomieniu.
    /// </summary>
    public static class PodgladSwiata
    {
        private static string _wyjscie;

        public static void UtworzMaterialy()
        {
            Directory.CreateDirectory("Assets/Resources/Materialy");
            Utworz("Grunt", "GraSimply/Grunt", m =>
            {
                foreach (string warstwa in new[] { "Trawa", "Ziemia", "Skala", "Piasek" })
                {
                    string w = "Assets/Resources/Tekstury/Grunt/" + warstwa;
                    m.SetTexture("_" + warstwa, AssetDatabase.LoadAssetAtPath<Texture2D>(w + "_Kolor.jpg"));
                    m.SetTexture("_" + warstwa + "N", AssetDatabase.LoadAssetAtPath<Texture2D>(w + "_Normalne.jpg"));
                }
            });
            Utworz("Natura", "GraSimply/Natura", m => m.enableInstancing = true);
            Utworz("Detal", "GraSimply/Detal", m => m.SetTexture("_Detal", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Tekstury/Grunt/Skala_Kolor.jpg")));
            Utworz("Filtr", "GraSimply/Filtr", m => { });
            Utworz("Drobina", "Legacy Shaders/Particles/Additive", m => { });
            Utworz("Plomien", "Unlit/Color", m => m.color = new Color(1f, 0.6f, 0.15f));
            Utworz("Duch", "Legacy Shaders/Transparent/Diffuse", m => m.color = new Color(0.35f, 1f, 0.45f, 0.55f));
            AssetDatabase.SaveAssets();
        }

        private static void Utworz(string nazwa, string shader, System.Action<Material> ustaw)
        {
            string sciezka = $"Assets/Resources/Materialy/{nazwa}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(sciezka);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, sciezka);
            }
            m.shader = Shader.Find(shader);
            ustaw(m);
            EditorUtility.SetDirty(m);
        }

        public static void Zrob()
        {
            UtworzMaterialy();
            AssetDatabase.Refresh();
            _wyjscie = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "PodgladSwiata");
            Directory.CreateDirectory(_wyjscie);
            Styl.Aktywny = Styl.Hybrydowy;

            var g = new Grafika3D();
            var cel = new RenderTexture(Pulpit.Szerokosc, Pulpit.Wysokosc, 24);
            g.Scena.Kamera.targetTexture = cel;
            g.Scena.EkranNaPulpit = e => new Vector2(e.x, Pulpit.Wysokosc - e.y);
            var pulpit = new Pulpit();

            Wolny(g, pulpit, cel);
            Osada(g, pulpit, cel);
            Mapa(g, pulpit, cel);
            Debug.Log("Podglad swiata gotowy: " + _wyjscie);
        }

        private static void Wolny(Grafika3D g, Pulpit pulpit, RenderTexture cel)
        {
            var s = new SwiatWolny(42);
            s.Plecak["drewno"] = 60;
            s.Plecak["kamien"] = 30;
            s.Plecak["wlokno"] = 20;
            s.Kierunek = (1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            s.Kierunek = (0.0, 1.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 3 });
            s.Kierunek = (-1.0, 0.0);
            s.Aktualizuj(0.05, new Wejscie { Buduj = 4 });
            s.Kierunek = (0.7, 0.7);

            KlatkaWolna(g, pulpit, cel, s, "swiat-dzien");

            s.Aktualizuj(0.01, new Wejscie { Klik = "a:buduj" });
            s.Aktualizuj(0.01, new Wejscie { Klik = "b:namiot" });
            s.Aktualizuj(0.01, new Wejscie { Kursor = (2, 2) });
            KlatkaWolna(g, pulpit, cel, s, "swiat-budowa");
            s.Aktualizuj(0.01, new Wejscie { Klik = "a:buduj" });

            s.Godzina = 22.0;
            KlatkaWolna(g, pulpit, cel, s, "swiat-noc");

            var f = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 21.0, Dzien = 6 };
            f.Kierunek = (1.0, 0.0);
            f.Aktualizuj(0.05, new Wejscie { Buduj = 2 });
            f.Kierunek = (0.7, 0.7);
            for (int i = 0; i < 300; i++)
            {
                f.Hp = 100;
                f.Aktualizuj(0.05, default);
            }
            KlatkaWolna(g, pulpit, cel, f, "walka-noc");

            var o = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 21.0, Dzien = 5 };
            for (int x = -5; x <= 5; x++)
            {
                foreach (int y in new[] { -5, 5 })
                {
                    if (o.ObiektNa(x, y) == Obiekt.Brak)
                    {
                        o.Budowle[(x, y)] = x == 0 ? "brama" : "palisada";
                    }
                }
            }
            for (int y = -4; y <= 4; y++)
            {
                foreach (int x in new[] { -5, 5 })
                {
                    if (o.ObiektNa(x, y) == Obiekt.Brak)
                    {
                        o.Budowle[(x, y)] = "palisada";
                    }
                }
            }
            o.Budowle[(4, 4)] = "wieza";
            o.Budowle[(0, 1)] = "ognisko";
            o.Budowle[(-2, 2)] = "chata";
            o.Budowle[(2, -2)] = "namiot";
            o.Obozowisko.Chaty = 4;
            GraSimply.Logika.Osada.ZatrudnijOsadnika(o.Obozowisko, new Losowanie(5), "straznik", true);
            o.Postacie.Add(new PostacOsadnika { O = o.Obozowisko.Osadnicy[0], X = 2.5, Y = 2.5, CelX = 2.5, CelY = 2.5 });
            o.Obozowisko.Osadnicy[0].Zajecie = "straznik";
            o.Postacie[0].Plan = SwiatWolny.DomyslnyPlan("straznik");
            for (int i = 0; i < 120; i++)
            {
                o.Hp = 100;
                o.Aktualizuj(0.05, default);
            }
            KlatkaWolna(g, pulpit, cel, o, "obrona-noc");
            o.Godzina = 12.0;
            KlatkaWolna(g, pulpit, cel, o, "obrona-dzien");

            var ludzie = new SwiatWolny(42) { WalkaWlaczona = true, Godzina = 9.0, ObozZalozony = true };
            ludzie.Budowle[(0, 3)] = "ognisko";
            ludzie.Budowle[(2, 3)] = "namiot";
            ludzie.Budowle[(-2, 3)] = "chata";
            ludzie.Obozowisko.Chaty = 5;
            var los = new Losowanie(11);
            for (int i = 0; i < 3; i++)
            {
                GraSimply.Logika.Osada.ZatrudnijOsadnika(ludzie.Obozowisko, los, "drwal", true);
                ludzie.Postacie.Add(new PostacOsadnika { O = ludzie.Obozowisko.Osadnicy[i], X = -1.5 + i, Y = 1.5 + i, CelX = -1.5 + i, CelY = 1.5 + i });
            }
            ludzie.PanelOtwarty = true;
            ludzie.Aktualizuj(0.02, default);
            KlatkaWolna(g, pulpit, cel, ludzie, "panel-harmonogram");
            ludzie.PanelOtwarty = false;
            ludzie.Godzina = 5.2;
            for (int i = 0; i < 40 && ludzie.Gosc == null; i++)
            {
                ludzie.Godzina = 22.0;
                ludzie.Aktualizuj(0.02, default);
                ludzie.Wrogowie.Clear();
                ludzie.Godzina = 5.2;
                ludzie.Aktualizuj(0.02, default);
            }
            ludzie.Aktualizuj(0.02, new Wejscie { Klik = "g:otworz" });
            KlatkaWolna(g, pulpit, cel, ludzie, "karta-przybysza");

            var b = new SwiatWolny(42) { WalkaWlaczona = true, BudowaZEtapami = true, Godzina = 9.0, ObozZalozony = true };
            b.Plecak["drewno"] = 200;
            b.Plecak["kamien"] = 100;
            b.Plecak["wlokno"] = 40;
            b.Budowle[(-2, 0)] = "tartak";
            b.PostawNa("chata", 2, 0);
            b.PostawNa("namiot", 0, 2);
            b.PostawNa("kuznia", 2, 2);
            foreach (PlacBudowy plac in b.Place.Values)
            {
                plac.Etap = plac.Klucz == "namiot" ? 1 : plac.Klucz == "kuznia" ? 2 : 0;
            }
            b.Aktualizuj(0.02, new Wejscie { Klik = "a:buduj" });
            KlatkaWolna(g, pulpit, cel, b, "budowa-zakladki");
            b.Aktualizuj(0.02, new Wejscie { Klik = "k:rzemioslo" });
            KlatkaWolna(g, pulpit, cel, b, "budowa-przepisy");
            b.Aktualizuj(0.02, new Wejscie { Klik = "a:buduj" });
            for (int i = 0; i < 40; i++)
            {
                b.Aktualizuj(0.05, new Wejscie { Dzialaj = true });
            }
            KlatkaWolna(g, pulpit, cel, b, "budowa-plac");

            var w = new SwiatWolny(42);
            (w.X, w.Y) = BrzegWody(w);
            KlatkaWolna(g, pulpit, cel, w, "swiat-woda");
        }

        /// <summary>Wolny kafel, od którego najbliżej do wody: tam widać brzeg i fale.</summary>
        private static (double X, double Y) BrzegWody(SwiatWolny s)
        {
            for (int r = 1; r < 400; r++)
            {
                for (int x = -r; x <= r; x++)
                {
                    foreach (int y in new[] { -r, r })
                    {
                        if (s.ObiektNa(x, y) == Obiekt.Woda && s.ObiektNa(x + 4, y + 2) == Obiekt.Brak)
                        {
                            return (x + 4.5, y + 2.5);
                        }
                    }
                }
            }
            return (0.5, 0.5);
        }

        private static void KlatkaWolna(Grafika3D g, Pulpit pulpit, RenderTexture cel, SwiatWolny s, string nazwa)
        {
            g.RysujWolny(s, 3.0, 0.3, "Wojownik");
            g.Scena.Migocz(3.0);
            pulpit.ZlozWolnyNaklad(WidokWolny.Hud(s), WidokWolny.Plecak(s), WidokWolny.Dziennik(s), WidokWolny.Podpowiedz(s),
                                   WidokWolny.Panel(s), g.PodpisyWolnego, KafelkiWolne.Z(s), null, g.PasekZbioru,
                                   s.WalkaWlaczona ? s.Wytrzymalosc / SwiatWolny.MaxWytrzymalosc : (double?)null, g.PaskiWrogow);
            Zapisz(g, pulpit, cel, nazwa);
        }

        private static void Osada(Grafika3D g, Pulpit pulpit, RenderTexture cel)
        {
            var plac = new StanOsady { Klasa = "Mag", Chaty = 4, Pora = "lato" };
            foreach (string k in new[] { "dom", "kuznia", "tawerna", "spichlerz", "wieza", "tartak", "targ", "farma" })
            {
                plac.Budynki[k] = 1;
            }
            plac.Budynki["palisada"] = 1;
            foreach (string i in new[] { "Olek", "Jagna", "Kael" })
            {
                plac.Osadnicy.Add(new OsadnikDoRysunku { Imie = i, Zajecie = i == "Olek" ? "drwal" : "mysliwy", Morale = 60 });
            }
            var m = new Migawka { Plac = plac, Pola = new PoleDoRysunku[0][], Widok = "oboz", Klasa = "Mag" };
            g.Rysuj(m, true, 20.0, 0.1, false);
            pulpit.ZlozNaklad(null, "Mag", null, default, null, g.PodpisyOsady, g.Kadr);
            Zapisz(g, pulpit, cel, "osada-dzien");
            g.Rysuj(m, true, 20.2, 0.1, true);
            pulpit.ZlozNaklad(null, "Mag", null, default, null, g.PodpisyOsady, g.Kadr);
            Zapisz(g, pulpit, cel, "osada-noc");
            plac.Pora = "zima";
            g.Rysuj(m, true, 20.4, 0.1, false);
            pulpit.ZlozNaklad(null, "Mag", null, default, null, g.PodpisyOsady, g.Kadr);
            Zapisz(g, pulpit, cel, "osada-zima");
            plac.Pora = "lato";
        }

        private static void Mapa(Grafika3D g, Pulpit pulpit, RenderTexture cel)
        {
            Migawka m = Migawka.Tytulowa();
            m.GraczXy = (4, 4);
            m.Klasa = "Lotrzyk";
            g.Rysuj(m, false, 1.0, 0.1, false);
            pulpit.ZlozNaklad(null, "Lotrzyk", m.GraczXy, default, null, null, g.Kadr);
            Zapisz(g, pulpit, cel, "mapa");
        }

        private static void Zapisz(Grafika3D g, Pulpit pulpit, RenderTexture cel, string nazwa)
        {
            g.Scena.Kamera.Render();
            RenderTexture.active = cel;
            var obraz = new Texture2D(cel.width, cel.height, TextureFormat.RGB24, false);
            obraz.ReadPixels(new Rect(0, 0, cel.width, cel.height), 0, 0);
            obraz.Apply();
            RenderTexture.active = null;
            byte[] d = pulpit.Klatka.Dane;
            Color32[] p = obraz.GetPixels32();
            for (int y = 0; y < cel.height; y++)
            {
                for (int x = 0; x < cel.width; x++)
                {
                    int i = (y * cel.width + x) * 4;
                    int a = d[i + 3];
                    if (a == 0)
                    {
                        continue;
                    }
                    int j = (cel.height - 1 - y) * cel.width + x;
                    Color32 k = p[j];
                    p[j] = new Color32((byte)((d[i] * a + k.r * (255 - a)) / 255), (byte)((d[i + 1] * a + k.g * (255 - a)) / 255),
                                       (byte)((d[i + 2] * a + k.b * (255 - a)) / 255), 255);
                }
            }
            obraz.SetPixels32(p);
            File.WriteAllBytes(Path.Combine(_wyjscie, nazwa + ".png"), obraz.EncodeToPNG());
        }
    }
}
