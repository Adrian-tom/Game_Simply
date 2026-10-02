using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GraSimply.Logika
{
    /// <summary>
    /// Konsola dla gry z oknem: logika pisze i czeka na wejście z jednego wątku,
    /// a rysowanie i klawiatura siedzą na drugim.
    ///
    /// To jest sedno przeniesienia gry do Unity. Wersja pythonowa była
    /// blokującym programem konsolowym — <c>input()</c> zatrzymywał wykonanie
    /// i sam pompował zdarzenia okna od środka. W Unity nie wolno zablokować
    /// głównego wątku, bo stanęłoby rysowanie. Zamiast przepisywać całą logikę
    /// na maszynę stanów, puszczamy ją na osobnym wątku: <see cref="Czytaj"/>
    /// blokuje ten wątek, a główny spokojnie rysuje i w swoim czasie wkłada
    /// odpowiedź do kolejki. Dzięki temu pętle w rodzaju
    /// <c>while (true) { … Czytaj(); }</c> przenoszą się jeden do jednego.
    ///
    /// Bufor linii jest pod blokadą, bo czyta go wątek rysujący.
    /// </summary>
    public sealed class KonsolaKolejkowa : IKonsola
    {
        /// <summary>Ile linii trzymamy, zanim najstarsze zaczną wypadać.</summary>
        private const int MaxLinii = 1500;
        private const int PoPrzycieciu = 1000;

        private readonly object _zamek = new object();
        private readonly List<string> _linie = new List<string> { "" };
        private readonly BlockingCollection<string> _wejscie =
            new BlockingCollection<string>(boundedCapacity: 1);
        private readonly CancellationTokenSource _zamkniecie = new CancellationTokenSource();

        private int _wersja;
        private volatile bool _czeka;
        private volatile bool _czekaNaEnter;
        private volatile string _zacheta = "";
        private int _odebrane;

        /// <summary>
        /// Wołane na wątku logiki tuż przed zablokowaniem na pytaniu.
        ///
        /// Tu silnik odświeża migawkę świata: to jedyny moment, w którym logika
        /// na pewno nie jest w połowie zmiany, bo właśnie skończyła rysować menu
        /// i czeka na gracza.
        /// </summary>
        public Action PrzedPytaniem;

        /// <summary>Rośnie przy każdej zmianie treści — widok przebudowuje się tylko wtedy.</summary>
        public int Wersja
        {
            get
            {
                lock (_zamek)
                {
                    return _wersja;
                }
            }
        }

        /// <summary>Czy logika stoi teraz na <see cref="Czytaj"/> i czeka na gracza.</summary>
        public bool CzekaNaWejscie => _czeka;

        /// <summary>
        /// Ile odpowiedzi logika już odebrała. Rośnie tylko w górę, więc jest
        /// pewnym sygnałem „poprzednia poszła dalej” — w przeciwieństwie do
        /// <see cref="Wersja"/>, która rośnie też od samego rysowania menu,
        /// i do <see cref="CzekaNaWejscie"/>, którego opadnięcie można przegapić
        /// między dwoma pytaniami.
        /// </summary>
        public int OdebraneOdpowiedzi => _odebrane;

        /// <summary>Czy pytanie jest typu „naciśnij Enter” — wtedy spacja i klik też zadziałają.</summary>
        public bool CzekaNaEnter => _czekaNaEnter;

        public string Zacheta => _zacheta;

        // ---- strona logiki (wątek gry) ---------------------------------- //

        public void Pisz(string tekst = "")
        {
            lock (_zamek)
            {
                Dopisz((tekst ?? "") + "\n");
            }
        }

        public void Wyczysc()
        {
            lock (_zamek)
            {
                _linie.Clear();
                _linie.Add("");
                _wersja++;
            }
        }

        public string Czytaj(string zacheta = "")
        {
            // Zachęta idzie bez łamania linii — odpowiedź gracza dopisuje się
            // w tej samej linii, dokładnie jak input() w terminalu.
            lock (_zamek)
            {
                Dopisz(zacheta ?? "");
            }
            _zacheta = zacheta ?? "";
            _czekaNaEnter = (zacheta ?? "").IndexOf("Enter", StringComparison.Ordinal) >= 0;
            PrzedPytaniem?.Invoke();
            _czeka = true;
            try
            {
                string odpowiedz = _wejscie.Take(_zamkniecie.Token);
                // Flagę gasimy natychmiast, przed dopisaniem do linii. Gdyby
                // została podniesiona choć chwilę dłużej, klawisz naciśnięty
                // w tym momencie trafiłby do kolejki jako odpowiedź na pytanie,
                // którego gra jeszcze nie zadała.
                _czeka = false;
                System.Threading.Interlocked.Increment(ref _odebrane);
                lock (_zamek)
                {
                    Dopisz(odpowiedz + "\n");
                }
                return odpowiedz;
            }
            catch (OperationCanceledException)
            {
                throw new GraZamknieta();
            }
            catch (ObjectDisposedException)
            {
                throw new GraZamknieta();
            }
            finally
            {
                _czeka = false;
                _czekaNaEnter = false;
            }
        }

        private void Dopisz(string tekst)
        {
            string[] czesci = tekst.Split('\n');
            for (int i = 0; i < czesci.Length; i++)
            {
                if (i > 0)
                {
                    _linie.Add("");
                }
                _linie[_linie.Count - 1] += czesci[i].Replace("\t", "    ");
            }
            if (_linie.Count > MaxLinii)
            {
                _linie.RemoveRange(0, _linie.Count - PoPrzycieciu);
            }
            _wersja++;
        }

        // ---- strona widoku (wątek główny) ------------------------------- //

        /// <summary>Kopia linii do narysowania — kopia, bo logika pisze dalej w tle.</summary>
        public List<string> Linie()
        {
            lock (_zamek)
            {
                return new List<string>(_linie);
            }
        }

        /// <summary>
        /// Podaje odpowiedź gracza. Zwraca false, gdy logika akurat nie pyta —
        /// wtedy klawisz trzeba zignorować, a nie kolejkować na przyszłość.
        /// </summary>
        public bool Odpowiedz(string tekst)
        {
            return _czeka && _wejscie.TryAdd(tekst ?? "");
        }

        /// <summary>Zamknięcie okna — budzi logikę wyjątkiem <see cref="GraZamknieta"/>.</summary>
        public void Zamknij()
        {
            if (!_zamkniecie.IsCancellationRequested)
            {
                _zamkniecie.Cancel();
            }
        }

        public bool Zamknieta => _zamkniecie.IsCancellationRequested;
    }
}
