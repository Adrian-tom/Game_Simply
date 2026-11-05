using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraSimply.Grafika;
using GraSimply.Grafika.Trojwymiar;
using GraSimply.Logika.Wolny;

namespace GraSimply.Testy
{
    /// <summary>
    /// To, co w grafice 3D da się sprawdzić bez silnika: katalog wskazuje istniejące pliki, wybór modelu
    /// jest stabilny, a interfejs na scenę 3D zostawia przezroczystość tam, gdzie ma być widać świat.
    /// </summary>
    public static class TestyGrafiki3D
    {
        public static void Uruchom()
        {
            Harness.Grupa("Grafika 3D: katalog modeli i nakładka interfejsu");
            Katalog();
            Wybory();
            Nakladka();
        }

        private static string KatalogModeliNaDysku()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            {
                string s = Path.Combine(d.FullName, "unity", "GraSimply", "Assets", "Resources", "Modele");
                if (Directory.Exists(s))
                {
                    return s;
                }
                s = Path.Combine(d.FullName, "GraSimply", "Assets", "Resources", "Modele");
                if (Directory.Exists(s))
                {
                    return s;
                }
            }
            throw new DirectoryNotFoundException("Nie ma Assets/Resources/Modele");
        }

        private static void Katalog()
        {
            string korzen = KatalogModeliNaDysku();
            List<string> brak = KatalogModeli.WszystkieSciezki().Distinct()
                                             .Where(s => !File.Exists(s.StartsWith("Natura/") ? Path.Combine(korzen, "..", s + ".png") : Path.Combine(korzen, s + ".fbx"))).ToList();
            Harness.Sprawdz("każdy model z katalogu jest w projekcie" + (brak.Count > 0 ? ": brak " + string.Join(", ", brak) : ""),
                            brak.Count == 0);
            string licencje = Path.Combine(korzen, "..", "..", "Licencje-modeli");
            Harness.Rowne("licencje wszystkich paczek modeli i tekstur leżą w projekcie", 8, Directory.GetFiles(licencje, "*.txt").Length);
            foreach (string p in new[] { "Grunt", "Plomien", "Duch", "Natura", "Detal", "Filtr", "Drobina" })
            {
                Harness.Sprawdz($"materiał {p} jest w Resources", File.Exists(Path.Combine(korzen, "..", "Materialy", p + ".mat")));
            }
        }

        private static void Wybory()
        {
            WpisModelu? a = KatalogModeli.DlaObiektu(Obiekt.Drzewo, 5, 9, "las");
            Harness.Rowne("ten sam kafel daje ten sam model", a.Value.Sciezka, KatalogModeli.DlaObiektu(Obiekt.Drzewo, 5, 9, "las").Value.Sciezka);
            int rozne = Enumerable.Range(0, 40).Select(x => KatalogModeli.DlaObiektu(Obiekt.Drzewo, x, 3, "las").Value.Sciezka).Distinct().Count();
            Harness.Sprawdz($"las nie jest jednym drzewem ({rozne} rodzajów)", rozne >= 4);
            Harness.Sprawdz("na bagnach drzewa są inne niż w lesie",
                            !Enumerable.Range(0, 40).Any(x => KatalogModeli.DlaObiektu(Obiekt.Drzewo, x, 3, "bagna").Value.Sciezka.Contains("island_tree")));
            string[] opisy = File.ReadAllLines(Path.Combine(KatalogModeliNaDysku(), "..", "Natura", "natura.txt"));
            List<string> bezOpisu = KatalogModeli.WszystkieSciezki().Distinct().Where(s => s.StartsWith("Natura/"))
                                                 .Where(s => !opisy.Any(l => l.StartsWith(s.Substring(7) + ";"))).ToList();
            Harness.Sprawdz("każdy obraz natury ma opis podstawy" + (bezOpisu.Count > 0 ? ": brak " + string.Join(", ", bezOpisu) : ""), bezOpisu.Count == 0);
            Harness.Sprawdz("wypieczona natura zawsze patrzy na kamerę, więc nie losuje obrotu", !KatalogModeli.DlaObiektu(Obiekt.Drzewo, 1, 1, "las").Value.LosowyObrot);
            Harness.Sprawdz("równina ma trawę", KatalogModeli.Kepki("równiny", 3, 4).Count > 0);
            Harness.Sprawdz("kanion jest goły", KatalogModeli.Kepki("kanion", 3, 4).Count == 0);
            Harness.Sprawdz("kępka stoi wewnątrz pola", KatalogModeli.Kepki("las", 9, 2).All(k => k.U >= 0f && k.U < 1f && k.V >= 0f && k.V < 1f));
            Harness.Rowne("kępki są powtarzalne", KatalogModeli.Kepki("las", 9, 2)[0].Sciezka, KatalogModeli.Kepki("las", 9, 2)[0].Sciezka);
            Harness.Sprawdz("woda to kolor gruntu, nie model", KatalogModeli.DlaObiektu(Obiekt.Woda, 1, 1) == null);
            Harness.Sprawdz("pusty kafel nie ma modelu", KatalogModeli.DlaObiektu(Obiekt.Brak, 1, 1) == null);
            foreach (string b in SwiatWolny.Budowlane)
            {
                Harness.Sprawdz($"budowla {b} ma model", !string.IsNullOrEmpty(KatalogModeli.DlaBudowli(b).Sciezka));
            }
            Harness.Rowne("chata składa się z części wioski", KatalogModeli.Chata, KatalogModeli.DlaBudowli("chata").Sciezka);
            Harness.Sprawdz("dwór ma komin, a chata nie",
                            KatalogModeli.Czesci(KatalogModeli.Dwor).Any(c => c.Sciezka.Contains("Chimney"))
                            && !KatalogModeli.Czesci(KatalogModeli.Chata).Any(c => c.Sciezka.Contains("Chimney")));
            Harness.Sprawdz("zimą grunt jest jaśniejszy",
                            KatalogModeli.KolorGruntu("las", 0.5, "zima").B > KatalogModeli.KolorGruntu("las", 0.5, "lato").B);
            foreach (string b in new[] { "las", "bagna", "wzgórza", "kanion", "ruiny", "woda", "plac", "równiny" })
            {
                (double t, double z, double s, double p) = KatalogModeli.SkladGruntu(b);
                Harness.Sprawdz($"skład gruntu {b} sumuje się do 1", Math.Abs(t + z + s + p - 1.0) < 1e-9);
            }
            foreach (string warstwa in new[] { "Trawa", "Ziemia", "Skala", "Piasek" })
            {
                string d = Path.Combine(KatalogModeliNaDysku(), "..", "Tekstury", "Grunt", warstwa);
                Harness.Sprawdz($"warstwa {warstwa} ma teksturę i normalne",
                                File.Exists(d + "_Kolor.jpg") && File.Exists(d + "_Normalne.jpg"));
            }
            Harness.Sprawdz("woda nie zamarza w kolorze", KatalogModeli.KolorGruntu("woda", 0.5, "zima").B == KatalogModeli.KolorGruntu("woda", 0.5, "lato").B);
        }

        private static void Nakladka()
        {
            var s = new SwiatWolny(42);
            var pulpit = new Pulpit();
            pulpit.ZlozWolnyNaklad(WidokWolny.Hud(s), WidokWolny.Plecak(s), WidokWolny.Dziennik(s), WidokWolny.Podpowiedz(s),
                                   null, null, KafelkiWolne.Z(s), null, (320, 150, 0.5));
            Plotno k = pulpit.Klatka;
            Harness.Rowne("środek ekranu jest przezroczysty: widać świat 3D", 0, (int)k.Alfa(640, 400));
            Harness.Sprawdz("pasek zbierania jest kryjący", k.Alfa(640, 301) == 255);
            Harness.Sprawdz("tło plecaka jest półprzezroczyste", k.Alfa(12, 90) > 0 && k.Alfa(12, 90) < 255);
            Przycisk p = pulpit.PrzyciskiWolne[0];
            Harness.Sprawdz("kafelek paska przykrywa scenę", k.Alfa(p.X + p.Szer - 6, p.Y + 6) >= 200);
            Harness.Rowne("kafelki działają też na nakładce", 5, pulpit.PrzyciskiWolne.Count);
        }
    }
}
