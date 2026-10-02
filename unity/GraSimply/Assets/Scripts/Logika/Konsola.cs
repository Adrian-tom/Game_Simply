using System;
using System.Collections.Generic;
using System.Text;

namespace GraSimply.Logika
{
    /// <summary>
    /// Wyjątek rzucany, gdy gracz zamyka grę w trakcie czekania na wejście —
    /// przerywa pętlę logiki tak jak Ctrl+C w terminalu.
    /// </summary>
    public sealed class GraZamknieta : Exception
    {
        public GraZamknieta() : base("Gra została zamknięta.")
        {
        }
    }

    /// <summary>
    /// Konsola gry: to, co w Pythonie robiły <c>print</c>, <c>input</c>
    /// i czyszczenie ekranu.
    ///
    /// Wersja pythonowa podmieniała wbudowane funkcje, żeby logika mogła
    /// zostać nietknięta. W C# nie ma sensu tego udawać — logika dostaje
    /// ten interfejs i woła go wprost. Implementacja w Unity dokłada
    /// kolejkę między wątkami, a implementacja testowa karmi logikę
    /// gotową listą odpowiedzi.
    /// </summary>
    public interface IKonsola
    {
        /// <summary>Jak <c>print</c> — dopisuje tekst i łamie linię.</summary>
        void Pisz(string tekst = "");

        /// <summary>Jak <c>os.system("cls")</c> — czyści zawartość konsoli.</summary>
        void Wyczysc();

        /// <summary>
        /// Jak <c>input</c> — pokazuje zachętę i blokuje do odpowiedzi gracza.
        /// Może rzucić <see cref="GraZamknieta"/>, gdy okno zostanie zamknięte.
        /// </summary>
        string Czytaj(string zacheta = "");
    }

    /// <summary>Pomocniki rysowania tekstu — odpowiednik <c>game/utils.py</c>.</summary>
    public static class Utils
    {
        public static void Linia(IKonsola konsola, char znak = '─', int szerokosc = 44)
        {
            konsola.Pisz("  " + new string(znak, szerokosc));
        }

        public static void NacisnijEnter(IKonsola konsola,
                                         string komunikat = "  [Naciśnij Enter, aby kontynuować...]")
        {
            konsola.Czytaj(komunikat);
        }

        public static void BanerTytulowy(IKonsola konsola)
        {
            konsola.Wyczysc();
            Linia(konsola, '═');
            konsola.Pisz("  ██████╗ ██████╗  ██████╗      ██████╗ ██████╗  ██████╗ ");
            konsola.Pisz("  ██╔══██╗██╔══██╗██╔═══██╗    ██╔══██╗██╔══██╗██╔════╝ ");
            konsola.Pisz("  ██████╔╝██████╔╝██║   ██║    ██████╔╝██████╔╝██║  ███╗");
            konsola.Pisz("  ██╔═══╝ ██╔══██╗██║   ██║    ██╔══██╗██╔═══╝ ██║   ██║");
            konsola.Pisz("  ██║     ██║  ██║╚██████╔╝    ██║  ██║██║     ╚██████╔╝");
            konsola.Pisz("  ╚═╝     ╚═╝  ╚═╝ ╚═════╝     ╚═╝  ╚═╝╚═╝      ╚═════╝ ");
            Linia(konsola, '═');
            konsola.Pisz("         ⚔  Fantasy RPG po polsku  🛡");
            Linia(konsola, '═');
            konsola.Pisz();
        }
    }

    /// <summary>
    /// Konsola do testów: zbiera wypisany tekst, a na pytania odpowiada
    /// z przygotowanej kolejki. Pozwala przejść całą pętlę gry bez Unity.
    /// </summary>
    public sealed class KonsolaTestowa : IKonsola
    {
        private readonly Queue<string> _odpowiedzi;
        private readonly StringBuilder _wyjscie = new StringBuilder();

        public KonsolaTestowa(IEnumerable<string> odpowiedzi)
        {
            _odpowiedzi = new Queue<string>(odpowiedzi);
        }

        /// <summary>Wszystko, co logika wypisała od początku (bez czyszczeń).</summary>
        public string Wyjscie => _wyjscie.ToString();

        /// <summary>Ostatni „ekran” — tekst wypisany po ostatnim czyszczeniu.</summary>
        public string Ekran { get; private set; } = "";

        public List<string> Pytania { get; } = new List<string>();

        public int ZostaloOdpowiedzi => _odpowiedzi.Count;

        public void Pisz(string tekst = "")
        {
            _wyjscie.Append(tekst).Append('\n');
            Ekran += tekst + "\n";
        }

        public void Wyczysc()
        {
            Ekran = "";
        }

        public string Czytaj(string zacheta = "")
        {
            Pisz(zacheta);
            Pytania.Add(zacheta);
            if (_odpowiedzi.Count == 0)
            {
                // Brak scenariusza = koniec testu; zachowujemy się jak zamknięcie okna,
                // żeby pętla logiki wyszła tą samą drogą co w prawdziwej grze.
                throw new GraZamknieta();
            }
            string odp = _odpowiedzi.Dequeue();
            Pisz(odp);
            return odp;
        }
    }
}
