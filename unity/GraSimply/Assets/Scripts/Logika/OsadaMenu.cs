using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>
    /// Menu osady: lista osadników z zajęciem i umiejętnością, zatrudnianie, karta osadnika.
    /// Zamówienia rzemieślników i sprzedaż na targu (z <c>menu_osady</c> w Pythonie) czekają
    /// na swoje moduły; wieści o więziach też.
    /// </summary>
    public static partial class Osada
    {
        private static string LiniaBilansu(Gracz gracz)
        {
            Dictionary<string, int> bilans = BilansDnia(gracz);
            if (bilans.Count == 0)
            {
                return "  Wczoraj: — (bilans pojawi się po pierwszym dniu)";
            }
            var czesci = new List<string>();
            foreach (string k in new[] { "zloto" }.Concat(Oboz.Surowce.Select(s => s.Key)))
            {
                int v = bilans.Wez(k);
                if (v != 0)
                {
                    string ikona = k == "zloto" ? "💰" : Oboz.Surowce.First(s => s.Key == k).Value.Ikona;
                    czesci.Add($"{ikona}{(v >= 0 ? "+" : "")}{v}");
                }
            }
            return "  Wczoraj: " + (czesci.Count > 0 ? string.Join("  ", czesci) : "bez zmian");
        }

        /// <summary>Poziom umiejętności używanej w obecnym zajęciu, np. „7🔥” (pusty dla bezczynnego).</summary>
        public static string ZnacznikUmiejetnosci(Osadnik o)
        {
            string u = Kompetencje.ZUmiejetnosciZajecia(o.Zajecie);
            return u == null ? "" : $"{Kompetencje.Poziom(o, u)}{Kompetencje.OpisPasji(Kompetencje.Pasja(o, u))}";
        }

        private static void KartaOsadnika(IKonsola konsola, Gracz gracz, Osadnik o)
        {
            konsola.Pisz($"\n  {o.Imie} — {o.Cecha} ({Cecha(o.Cecha).Opis}), morale {Liczby.Format(o.Morale, 0)}");
            konsola.Pisz("  Umiejętności (🔥 = pasja: szybsza nauka i lepszy humor przy tej pracy):");
            foreach (string linia in Kompetencje.OpisPelny(o, 5, o.Zajecie))
            {
                konsola.Pisz(linia);
            }
            List<string> klucze = Zajecia.Select(z => z.Key).ToList();
            for (int i = 0; i < klucze.Count; i++)
            {
                OpisZajecia info = Zajecie(klucze[i]);
                int? m = Miejsca(gracz, klucze[i]);
                string limit = m == null ? ""
                    : m.Value > 0 ? $"  [{IluWZawodzie(gracz, klucze[i])}/{m.Value}]" : $"  (wymaga: {info.Wymaga})";
                konsola.Pisz($"  [{(i + 1).ToString().PadLeft(2)}] {info.Ikona} {info.Nazwa} — {info.Opis}{limit}");
            }
            konsola.Pisz("  [W] 🚪 Wypędź z osady");
            konsola.Pisz("  [0] ↩ Wróć\n");
            string wybor = (konsola.Czytaj("  Nowe zajęcie: ") ?? "").Trim().ToLowerInvariant();
            if (wybor == "w")
            {
                Osadnicy(gracz).Remove(o);
                ZmienMorale(gracz, -5);
                konsola.Pisz($"  {o.Imie} odchodzi. Inni patrzą na to niechętnie (−5 morale).");
            }
            else if (int.TryParse(wybor, out int nr) && nr >= 1 && nr <= klucze.Count)
            {
                konsola.Pisz(UstawZajecie(gracz, o, klucze[nr - 1]));
            }
            else
            {
                return;
            }
            Utils.NacisnijEnter(konsola);
        }

        /// <summary>Osadnicy, zawody i bilans dnia.</summary>
        public static void MenuOsady(IKonsola konsola, Gracz gracz, Losowanie rng)
        {
            while (true)
            {
                konsola.Wyczysc();
                Utils.Linia(konsola, '═');
                konsola.Pisz($"  OSADA  —  {Kalendarz.OpisDaty(gracz.Czas)}");
                Utils.Linia(konsola, '═');
                List<Osadnik> lista = Osadnicy(gracz);
                int zywnosc = gracz.Surowce.Wez("zywnosc");
                int dzienne = BilansZywnosci(gracz);
                string zapas = dzienne < 0 ? $"starczy na ~{zywnosc / Math.Max(1, -dzienne)} dni" : "zapasy rosną";
                konsola.Pisz($"\n  Ludność: {lista.Count} osadników + {gracz.Rekruci.Count} w drużynie   "
                             + $"Chaty: {LiczbaChat(gracz)}/{MaxChaty}");
                konsola.Pisz($"  Morale: {IkonaMorale(SrednieMorale(gracz))} {Liczby.Format(SrednieMorale(gracz), 0)}   "
                             + $"Utrzymanie: {Oboz.UtrzymanieDzienne(gracz)} zł/dz.");
                konsola.Pisz($"  Żywność: {zywnosc} ({(dzienne >= 0 ? "+" : "")}{dzienne}/dz., {zapas})   "
                             + $"Do zimy: {Kalendarz.DniDoZimy(gracz.Czas)} dni");
                konsola.Pisz(LiniaBilansu(gracz));
                konsola.Pisz($"  {Oboz.LiniaSurowcow(gracz)}   Złoto: {gracz.Zloto}\n");
                if (lista.Count == 0)
                {
                    konsola.Pisz("  Nikt jeszcze nie mieszka w chatach. Zleć chatę ([11] w obozie) i zatrudnij osadnika.\n");
                }
                for (int i = 0; i < lista.Count; i++)
                {
                    Osadnik o = lista[i];
                    OpisZajecia info = Zajecie(o.Zajecie);
                    string chory = o.Chory > 0 ? "  🤒 chory" : "";
                    konsola.Pisz($"  [{(i + 1).ToString().PadLeft(2)}] {IkonaMorale(o.Morale)} {Liczby.Dopelnij(o.Imie, 8)} "
                                 + $"{info.Ikona} {info.Nazwa} {ZnacznikUmiejetnosci(o)}  · {o.Cecha}  · morale "
                                 + $"{Liczby.Format(o.Morale, 0)}{chory}");
                }
                konsola.Pisz();
                konsola.Pisz($"  [N]  🤝  Zatrudnij osadnika ({CenaOsadnika} zł, potrzebna wolna chata: {WolneChaty(gracz)})");
                konsola.Pisz("  [0]  ↩  Wróć\n");
                string wybor = (konsola.Czytaj("  Twój wybór: ") ?? "").Trim().ToLowerInvariant();
                if (wybor == "0")
                {
                    return;
                }
                if (wybor == "n")
                {
                    Zatrudnianie(konsola, gracz, rng);
                    continue;
                }
                if (int.TryParse(wybor, out int nr) && nr >= 1 && nr <= lista.Count)
                {
                    KartaOsadnika(konsola, gracz, lista[nr - 1]);
                    continue;
                }
                konsola.Pisz("  Nieprawidłowy wybór.");
                Utils.NacisnijEnter(konsola);
            }
        }

        /// <summary>Wieści z osady, które czekały na bohatera (ostatnie 12); kronika zostaje opróżniona.</summary>
        public static List<string> RozliczKronike(Gracz gracz)
        {
            List<string> wiesci = gracz.Kronika.ToList();
            gracz.Kronika.Clear();
            if (wiesci.Count > 12)
            {
                int pominiete = wiesci.Count - 12;
                wiesci = wiesci.GetRange(pominiete, 12);
                wiesci.Insert(0, $"  📰  (… i {pominiete} starszych wieści)");
            }
            return wiesci;
        }

        private static void Zatrudnianie(IKonsola konsola, Gracz gracz, Losowanie rng)
        {
            konsola.Pisz("\n  Czym ma się zająć? (zmienisz to potem w każdej chwili)");
            List<string> klucze = Zajecia.Select(z => z.Key).Where(z => z != "bezczynny").ToList();
            for (int i = 0; i < klucze.Count; i++)
            {
                konsola.Pisz($"  [{(i + 1).ToString().PadLeft(2)}] {Zajecie(klucze[i]).Ikona} {Zajecie(klucze[i]).Nazwa}");
            }
            string z = (konsola.Czytaj("  Zajęcie: ") ?? "").Trim();
            string zaj = int.TryParse(z, out int nz) && nz >= 1 && nz <= klucze.Count ? klucze[nz - 1] : "drwal";
            konsola.Pisz(ZatrudnijOsadnika(gracz, rng, zaj));
            Utils.NacisnijEnter(konsola);
        }
    }
}
