using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>Ikony UI — biomy, punkty, kierunki. Jedno źródło prawdy.</summary>
    public static class Ikony
    {
        public const string GraczMapa = "👤";
        public const string Mgla = "❔";

        /// <summary>
        /// Kolejność ma znaczenie: legenda mapy wypisuje odkryte biomy właśnie
        /// w tym porządku, więc to nie jest zwykły słownik.
        /// </summary>
        public static readonly List<KeyValuePair<string, string>> Biomy =
            new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("równiny", "🌾"),
                new KeyValuePair<string, string>("ruiny", "🏚"),
                new KeyValuePair<string, string>("las", "🌲"),
                new KeyValuePair<string, string>("bagna", "🐸"),
                new KeyValuePair<string, string>("wzgórza", "⛰"),
                new KeyValuePair<string, string>("kanion", "🏜"),
            };

        public static readonly List<KeyValuePair<string, string>> Punkty =
            new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("obóz", "🏕"),
                new KeyValuePair<string, string>("karczma", "🍺"),
                new KeyValuePair<string, string>("kuźnia", "⚒"),
                new KeyValuePair<string, string>("świątynia", "🛕"),
                new KeyValuePair<string, string>("jaskinia", "🕳"),
                new KeyValuePair<string, string>("boss", "☠"),
                new KeyValuePair<string, string>("portal", "🌀"),
                new KeyValuePair<string, string>("leze_smoka", "🐉"),
                new KeyValuePair<string, string>("latajaca_wyspa", "☁"),
                new KeyValuePair<string, string>("miasto", "🏙"),
            };

        private static readonly Dictionary<string, string> Kierunki =
            new Dictionary<string, string>
            {
                { "północ", "⬆" }, { "zachód", "⬅" }, { "wschód", "➡" }, { "południe", "⬇" },
            };

        public static string Biom(string nazwa)
        {
            if (string.IsNullOrEmpty(nazwa))
            {
                return "🌍";
            }
            foreach (KeyValuePair<string, string> para in Biomy)
            {
                if (para.Key == nazwa)
                {
                    return para.Value;
                }
            }
            return "🌍";
        }

        public static string Punkt(string klucz)
        {
            if (string.IsNullOrEmpty(klucz))
            {
                return "";
            }
            foreach (KeyValuePair<string, string> para in Punkty)
            {
                if (para.Key == klucz)
                {
                    return para.Value;
                }
            }
            return "📍";
        }

        public static string Kierunek(string nazwa)
        {
            return Kierunki.TryGetValue(nazwa, out string ikona) ? ikona : "•";
        }

        public static string EtykietaBiomu(string nazwa)
        {
            return string.IsNullOrEmpty(nazwa) ? "" : $"{Biom(nazwa)} {nazwa}";
        }

        public static string EtykietaPunktu(string klucz, string nazwa = null)
        {
            if (string.IsNullOrEmpty(klucz))
            {
                return "";
            }
            return $"{Punkt(klucz)} {(string.IsNullOrEmpty(nazwa) ? klucz : nazwa)}";
        }

        public static string GlifPola(string biom, string punkt, bool ty, bool odkryte)
        {
            if (ty)
            {
                return GraczMapa;
            }
            if (!odkryte)
            {
                return Mgla;
            }
            return string.IsNullOrEmpty(punkt) ? Biom(biom) : Punkt(punkt);
        }
    }
}
