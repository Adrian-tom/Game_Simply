using System;
using System.Globalization;

namespace GraSimply.Logika
{
    /// <summary>
    /// Zaokrąglanie i formatowanie liczb tak, jak robi to Python.
    ///
    /// Różnica jest drobna, ale psuje porównania: <c>round(0.15, 1)</c> w Pythonie
    /// daje 0,1 (liczba 0,15 jest w binarnej reprezentacji odrobinę mniejsza),
    /// a <c>Math.Round(0.15, 1)</c> w .NET — 0,2, bo najpierw mnoży przez 10 i trafia
    /// dokładnie w połowę. Podobnie <c>f"{2.5:.0f}"</c> to „2”, a <c>ToString("F0")</c> — „3”.
    /// Morale osadników i paski postępu przechodzą przez takie zaokrąglenia
    /// codziennie, więc port liczy je na dokładnej wartości bitowej liczby
    /// (mantysa × 2^wykładnik), bez pośrednich błędów.
    ///
    /// Zakres: do trzech cyfr po przecinku i liczby poniżej 2^53 / 1000 —
    /// z zapasem tyle, ile gra kiedykolwiek formatuje.
    /// </summary>
    public static class Liczby
    {
        private static readonly ulong[] Potegi10 = { 1UL, 10UL, 100UL, 1000UL };

        /// <summary>Jak <c>round(x, cyfry)</c>: remis rozstrzyga do parzystej, na dokładnej wartości.</summary>
        public static double Zaokraglij(double x, int cyfry = 0)
        {
            if (!SkalowanaCalkowita(x, cyfry, out long q, out bool ujemny))
            {
                return Math.Round(x, Math.Min(Math.Max(cyfry, 0), 15), MidpointRounding.ToEven);
            }
            double wynik = q / (double)Potegi10[cyfry];
            return ujemny ? -wynik : wynik;
        }

        /// <summary>Jak <c>round(x)</c> zwracające int (remis do parzystej).</summary>
        public static int ZaokragliDoInt(double x)
        {
            return (int)Zaokraglij(x, 0);
        }

        /// <summary>Jak <c>f"{x:.{cyfry}f}"</c> — z tym samym rozstrzyganiem remisów co Python.</summary>
        public static string Format(double x, int cyfry)
        {
            if (!SkalowanaCalkowita(x, cyfry, out long q, out bool ujemny))
            {
                return x.ToString("F" + cyfry, CultureInfo.InvariantCulture);
            }
            string znak = ujemny ? "-" : "";
            if (cyfry == 0)
            {
                return znak + q.ToString(CultureInfo.InvariantCulture);
            }
            long p = (long)Potegi10[cyfry];
            return znak + (q / p).ToString(CultureInfo.InvariantCulture) + "."
                   + (q % p).ToString(CultureInfo.InvariantCulture).PadLeft(cyfry, '0');
        }

        /// <summary>
        /// |x| · 10^cyfry zaokrąglone do całkowitej (remis do parzystej), policzone
        /// na mantysie i wykładniku — bez błędu, jaki dałoby samo mnożenie liczb zmiennoprzecinkowych.
        /// </summary>
        private static bool SkalowanaCalkowita(double x, int cyfry, out long q, out bool ujemny)
        {
            q = 0;
            ujemny = false;
            if (cyfry < 0 || cyfry >= Potegi10.Length || double.IsNaN(x) || double.IsInfinity(x))
            {
                return false;
            }
            long bity = BitConverter.DoubleToInt64Bits(x);
            ujemny = bity < 0;
            int wykladnikBity = (int)((bity >> 52) & 0x7FF);
            ulong ulamek = (ulong)bity & 0xFFFFFFFFFFFFFUL;
            ulong mantysa = wykladnikBity == 0 ? ulamek : (ulamek | (1UL << 52));
            int e = (wykladnikBity == 0 ? 1 : wykladnikBity) - 1075;
            if (e >= 0)
            {
                return false;          // liczba całkowita ≥ 2^52 — nic do zaokrąglania, zostaje wersja biblioteczna
            }
            ulong licznik = mantysa * Potegi10[cyfry];     // < 2^53 · 1000 < 2^63
            int s = -e;
            ulong iloraz;
            if (s >= 64)
            {
                iloraz = 0UL;
            }
            else
            {
                iloraz = licznik >> s;
                ulong reszta = licznik & ((1UL << s) - 1UL);
                ulong polowa = 1UL << (s - 1);
                if (reszta > polowa || (reszta == polowa && (iloraz & 1UL) == 1UL))
                {
                    iloraz += 1UL;
                }
            }
            if (iloraz > (ulong)long.MaxValue)
            {
                return false;
            }
            q = (long)iloraz;
            return true;
        }

        /// <summary>
        /// Dopełnia napis spacjami do <paramref name="szerokosc"/> znaków, licząc punkty kodowe,
        /// a nie jednostki UTF-16 (Python: <c>f"{s:&lt;10}"</c>). Emoji spoza podstawowej
        /// płaszczyzny to w .NET dwie jednostki, w Pythonie jeden znak.
        /// </summary>
        public static string Dopelnij(string tekst, int szerokosc, bool zLewej = false)
        {
            tekst = tekst ?? "";
            int znaki = 0;
            foreach (char c in tekst)
            {
                if (!char.IsLowSurrogate(c))
                {
                    znaki++;
                }
            }
            string spacje = new string(' ', Math.Max(0, szerokosc - znaki));
            return zLewej ? spacje + tekst : tekst + spacje;
        }
    }
}
