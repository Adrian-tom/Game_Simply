using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>
    /// Punkt styku logiki gry z widokiem — odpowiednik <c>game/ekran.py</c>.
    ///
    /// Logika nie wie nic o Unity. Zapisuje tu tylko, *co* jest do pokazania
    /// (bieżąca postać, otwarty widok, skróty klawiszy), a warstwa Gra to czyta
    /// i rysuje. Dzięki temu całą logikę da się uruchomić i przetestować bez
    /// edytora, zwykłym <c>dotnet run</c>.
    ///
    /// Logika chodzi na osobnym wątku, a rysowanie na głównym, więc dostęp
    /// do stanu jest pod blokadą. Pola są drobne i czytane raz na klatkę,
    /// więc koszt blokady jest bez znaczenia, a wyrwany w połowie zapis
    /// dałby migotanie albo wyjątek w trakcie rysowania.
    /// </summary>
    public sealed class Ekran
    {
        private readonly object _zamek = new object();

        private Gracz _gracz;
        private Dictionary<string, string> _skroty = new Dictionary<string, string>();
        private string _widok;
        private string _rozmowa;

        /// <summary>Postać, której świat rysuje widok. null = ekran tytułowy.</summary>
        public Gracz Gracz
        {
            get
            {
                lock (_zamek)
                {
                    return _gracz;
                }
            }
            set
            {
                lock (_zamek)
                {
                    _gracz = value;
                }
            }
        }

        /// <summary>
        /// Otwarty widok osady z bliska: „oboz” albo „osada”. Gdy ustawiony,
        /// widok rysuje placyk z chatami zamiast mapy regionu.
        /// </summary>
        public string Widok
        {
            get
            {
                lock (_zamek)
                {
                    return _widok;
                }
            }
            set
            {
                lock (_zamek)
                {
                    _widok = value;
                }
            }
        }

        /// <summary>Kto mówi w trwającej rozmowie — widok rysuje wtedy jego portret.</summary>
        public string Rozmowa
        {
            get
            {
                lock (_zamek)
                {
                    return _rozmowa;
                }
            }
            set
            {
                lock (_zamek)
                {
                    _rozmowa = value;
                }
            }
        }

        /// <summary>Czy w tej chwili działają skróty ruchu (strzałki, spacja).</summary>
        public bool SkrotyAktywne
        {
            get
            {
                lock (_zamek)
                {
                    return _skroty.Count > 0;
                }
            }
        }

        /// <summary>
        /// Zamraża stan świata do narysowania jednej klatki.
        ///
        /// Wołane z wątku logiki (z <see cref="KonsolaKolejkowa"/>, zanim ta
        /// zablokuje się na pytaniu), więc listy i słowniki postaci są w tym
        /// momencie spójne. Wątek rysujący dostaje gotowe dane i nigdy nie
        /// zagląda do żywych struktur gry.
        /// </summary>
        public Migawka Migawke()
        {
            Gracz g;
            string widok;
            string rozmowa;
            lock (_zamek)
            {
                g = _gracz;
                widok = _widok;
                rozmowa = _rozmowa;
            }
            if (g == null || g.MapaPola == null)
            {
                return null;
            }
            StanHud hud = Migawka.HudZGracza(g);
            hud.Podpowiedz = SkrotyAktywne
                ? "strzałki: ruch  ·  spacja: zbadaj  ·  F2: noc  ·  F11: pełny ekran"
                : "kliknij opcję albo wpisz numer  ·  F2: noc  ·  F11: pełny ekran";
            return new Migawka
            {
                Pola = Migawka.ZPol(g.MapaPola),
                Hud = hud,
                Region = (g.RegionX, g.RegionY),
                GraczXy = (g.MapaX, g.MapaY),
                Klasa = g.Klasa,
                Osada = (g.Chaty, g.PoziomBudynku("palisada"), g.PoziomBudynku("wieza")),
                Widok = widok,
                Rozmowa = rozmowa,
            };
        }

        /// <summary>Klawisz specjalny (np. „gora”) → odpowiedź wpisywana za gracza.</summary>
        public void UstawSkroty(IDictionary<string, string> mapa)
        {
            lock (_zamek)
            {
                _skroty = mapa == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(mapa);
            }
        }

        /// <summary>Odpowiedź przypisana do klawisza specjalnego albo null.</summary>
        public string Skrot(string nazwa)
        {
            lock (_zamek)
            {
                return _skroty.TryGetValue(nazwa, out string odp) ? odp : null;
            }
        }

        /// <summary>
        /// Na czas menu pokazuje osadę z bliska i oddaje poprzedni widok
        /// na wyjściu. Menu zagnieżdżają się (osada → karta osadnika →
        /// warsztat) i mogą wyjść wyjątkiem, więc widok wraca w Dispose,
        /// a nie przez ustawienie na null.
        /// </summary>
        public System.IDisposable WidokOsady(string nazwa)
        {
            return new PrzywrocWidok(this, nazwa);
        }

        private sealed class PrzywrocWidok : System.IDisposable
        {
            private readonly Ekran _ekran;
            private readonly string _poprzedni;

            public PrzywrocWidok(Ekran ekran, string nazwa)
            {
                _ekran = ekran;
                _poprzedni = ekran.Widok;
                ekran.Widok = nazwa;
            }

            public void Dispose()
            {
                _ekran.Widok = _poprzedni;
            }
        }
    }
}
