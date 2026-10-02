using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Testy
{
    /// <summary>
    /// Najprostszy możliwy harness testowy: lista nazwanych sprawdzeń i licznik
    /// niepowodzeń. Celowo bez biblioteki z NuGeta — port ma dać się sprawdzić
    /// jednym <c>dotnet run</c>, także bez dostępu do sieci.
    /// </summary>
    public static class Harness
    {
        private static int _zdane;
        private static readonly List<string> Bledy = new List<string>();
        private static string _grupa = "";

        public static void Grupa(string nazwa)
        {
            _grupa = nazwa;
            Console.WriteLine();
            Console.WriteLine($"── {nazwa} ──");
        }

        public static void Sprawdz(string opis, bool warunek)
        {
            if (warunek)
            {
                _zdane++;
                Console.WriteLine($"  OK   {opis}");
            }
            else
            {
                Bledy.Add($"{_grupa} / {opis}");
                Console.WriteLine($"  BŁĄD {opis}");
            }
        }

        public static void Rowne<T>(string opis, T oczekiwane, T otrzymane)
        {
            bool ok = EqualityComparer<T>.Default.Equals(oczekiwane, otrzymane);
            Sprawdz(ok ? opis : $"{opis}  (oczekiwano {oczekiwane}, jest {otrzymane})", ok);
        }

        public static void RowneCiagi(string opis, IEnumerable<string> oczekiwane,
                                      IEnumerable<string> otrzymane)
        {
            var a = oczekiwane.ToList();
            var b = otrzymane.ToList();
            bool ok = a.Count == b.Count && !a.Where((t, i) => t != b[i]).Any();
            if (ok)
            {
                Sprawdz(opis, true);
                return;
            }
            int roznica = Enumerable.Range(0, Math.Min(a.Count, b.Count)).FirstOrDefault(i => a[i] != b[i]);
            Sprawdz($"{opis}  (pierwsza różnica na {roznica}: oczekiwano " +
                    $"„{(roznica < a.Count ? a[roznica] : "—")}”, jest " +
                    $"„{(roznica < b.Count ? b[roznica] : "—")}”)", false);
        }

        public static void BliskoSiebie(string opis, double oczekiwane, double otrzymane,
                                        double tolerancja = 1e-12)
        {
            bool ok = Math.Abs(oczekiwane - otrzymane) <= tolerancja;
            Sprawdz(ok ? opis : $"{opis}  (oczekiwano {oczekiwane:R}, jest {otrzymane:R})", ok);
        }

        public static int Podsumuj()
        {
            Console.WriteLine();
            Console.WriteLine(new string('─', 60));
            if (Bledy.Count == 0)
            {
                Console.WriteLine($"Wszystko zielone: {_zdane} sprawdzeń.");
                return 0;
            }
            Console.WriteLine($"Zdane: {_zdane}   Niezdane: {Bledy.Count}");
            foreach (string b in Bledy)
            {
                Console.WriteLine($"  × {b}");
            }
            return 1;
        }
    }
}
