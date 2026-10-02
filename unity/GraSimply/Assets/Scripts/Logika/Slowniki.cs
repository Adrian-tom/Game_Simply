using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>
    /// Drobne rozszerzenia słowników.
    ///
    /// Logika sięga po setki wartości, których może nie być (surowce, przedmioty,
    /// statystyki) — w Pythonie robi to <c>dict.get(klucz, 0)</c>. Biblioteczne
    /// <c>GetValueOrDefault</c> istnieje dopiero od .NET Standard 2.1, a Unity
    /// bywa ustawione na 2.0, więc trzymamy własną wersję i nie zależymy
    /// od poziomu API projektu.
    /// </summary>
    public static class Slowniki
    {
        public static TWartosc Wez<TKlucz, TWartosc>(this IDictionary<TKlucz, TWartosc> slownik,
                                                     TKlucz klucz,
                                                     TWartosc domyslne = default)
        {
            if (slownik == null)
            {
                return domyslne;
            }
            return slownik.TryGetValue(klucz, out TWartosc wartosc) ? wartosc : domyslne;
        }

        /// <summary>Dodaje do wartości pod kluczem (brak klucza = start od zera).</summary>
        public static void Dodaj<TKlucz>(this IDictionary<TKlucz, int> slownik, TKlucz klucz, int ile)
        {
            slownik[klucz] = slownik.Wez(klucz) + ile;
        }
    }
}
