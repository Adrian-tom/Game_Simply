using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>
    /// Pętla gry: ekran tytułowy, tworzenie postaci, obóz i wyprawa.
    ///
    /// Odpowiednik <c>main.py</c>. Z obozu działa na razie tylko wyjście
    /// na wyprawę i karta postaci — menu obozu, warsztat, targ i rozmowy
    /// czekają na etap 2 portu.
    /// </summary>
    public sealed class Gra
    {
        private static readonly (string Klawisz, string Klasa, string Ikona, string Opis)[] Klasy =
        {
            ("1", "Wojownik", "⚔", "Dużo HP i obrony. Prosty i wytrzymały."),
            ("2", "Mag", "🔮", "Mało HP, dużo many i zaklęć obszarowych."),
            ("3", "Lotrzyk", "🗡", "Zwinny, trafienia krytyczne, zamki i kradzież."),
            ("4", "Druid", "🌿", "Natura, leczenie i przetrwanie w dziczy."),
            ("5", "Nekromanta", "💀", "Wysysa życie, przywołuje sługi, budzi strach."),
        };

        private readonly IKonsola _konsola;
        private readonly Ekran _ekran;
        private readonly Losowanie _rng;

        public Gra(IKonsola konsola, Ekran ekran, Losowanie rng = null)
        {
            _konsola = konsola;
            _ekran = ekran;
            _rng = rng ?? new Losowanie();
        }

        /// <summary>Uruchamia grę. Wraca, gdy gracz wyjdzie albo zamknie okno.</summary>
        public void Uruchom()
        {
            try
            {
                while (true)
                {
                    string wybor = MenuGlowne();
                    if (wybor == "1")
                    {
                        Gracz gracz = StworzPostac();
                        if (gracz != null)
                        {
                            PetlaObozu(gracz);
                        }
                    }
                    else if (wybor == "0")
                    {
                        _konsola.Pisz("\n  Do zobaczenia przy ogniu.");
                        return;
                    }
                }
            }
            catch (GraZamknieta)
            {
                // Gracz zamknął okno — wychodzimy spokojnie, tak jak Ctrl+C w terminalu.
            }
            finally
            {
                if (_ekran != null)
                {
                    _ekran.Gracz = null;
                    _ekran.UstawSkroty(null);
                }
            }
        }

        private string MenuGlowne()
        {
            while (true)
            {
                Utils.BanerTytulowy(_konsola);
                _konsola.Pisz("  [1]  🎮  Nowa gra");
                _konsola.Pisz("  [0]  🚪  Wyjście");
                _konsola.Pisz();
                string wybor = (_konsola.Czytaj("  Twój wybór: ") ?? "").Trim();
                if (wybor == "1" || wybor == "0")
                {
                    return wybor;
                }
                _konsola.Pisz("  Nieprawidłowy wybór.");
                Utils.NacisnijEnter(_konsola);
            }
        }

        private Gracz StworzPostac()
        {
            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');
            _konsola.Pisz("  🧙  TWORZENIE POSTACI");
            Utils.Linia(_konsola, '═');
            _konsola.Pisz();
            string imie = (_konsola.Czytaj("  Imię bohatera: ") ?? "").Trim();
            if (imie.Length == 0)
            {
                imie = "Bezimienny";
            }

            string klasa = null;
            while (klasa == null)
            {
                _konsola.Wyczysc();
                Utils.Linia(_konsola, '═');
                _konsola.Pisz("  ⚔  WYBÓR KLASY");
                Utils.Linia(_konsola, '═');
                _konsola.Pisz();
                foreach ((string klawisz, string nazwa, string ikona, string opis) in Klasy)
                {
                    _konsola.Pisz($"  [{klawisz}]  {ikona}  {nazwa.PadRight(12)}{opis}");
                }
                _konsola.Pisz();
                string wybor = (_konsola.Czytaj("  Twój wybór: ") ?? "").Trim();
                klasa = Klasy.FirstOrDefault(k => k.Klawisz == wybor).Klasa;
                if (klasa == null)
                {
                    _konsola.Pisz("  Nieprawidłowy wybór.");
                    Utils.NacisnijEnter(_konsola);
                }
            }

            var gracz = new Gracz(imie, klasa, _rng);
            Mapa.ZapewnijMape(gracz);
            if (_ekran != null)
            {
                _ekran.Gracz = gracz;
            }

            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');
            _konsola.Pisz($"  ✨  {gracz.Imie} rusza w świat jako {gracz.Klasa}.");
            Utils.Linia(_konsola, '═');
            _konsola.Pisz(gracz.KartaPostaci());
            _konsola.Pisz($"\n  Twój świat ma numer {gracz.Seed} — ten sam numer " +
                          "da zawsze tę samą mapę.");
            Utils.NacisnijEnter(_konsola);
            return gracz;
        }

        private void PetlaObozu(Gracz gracz)
        {
            while (true)
            {
                if (!gracz.Zyje())
                {
                    _konsola.Wyczysc();
                    Utils.Linia(_konsola, '═');
                    _konsola.Pisz("  ☠  KONIEC DROGI");
                    Utils.Linia(_konsola, '═');
                    _konsola.Pisz($"\n  {gracz.Imie} padł na {gracz.Poziom} poziomie.");
                    Utils.NacisnijEnter(_konsola);
                    return;
                }

                string wybor = MenuObozu(gracz);
                if (wybor == "1")
                {
                    using (_ekran?.WidokOsady(null))
                    {
                        var wyprawa = new Eksploracja(gracz, _konsola, _ekran, _rng);
                        wyprawa.Uruchom();
                    }
                    foreach (string nowe in gracz.SprawdzOsiagniecia())
                    {
                        _konsola.Pisz(nowe);
                    }
                }
                else if (wybor == "2")
                {
                    _konsola.Wyczysc();
                    _konsola.Pisz(gracz.KartaPostaci());
                    Utils.NacisnijEnter(_konsola);
                }
                else if (wybor == "3")
                {
                    Odpoczynek(gracz);
                }
                else if (wybor == "0")
                {
                    return;
                }
            }
        }

        private string MenuObozu(Gracz gracz)
        {
            while (true)
            {
                using (_ekran?.WidokOsady("oboz"))
                {
                    _konsola.Wyczysc();
                    Utils.Linia(_konsola, '═');
                    _konsola.Pisz("  🏕  OBÓZ");
                    Utils.Linia(_konsola, '═');
                    _konsola.Pisz($"  {Kalendarz.OpisDaty(gracz.Czas)}");
                    _konsola.Pisz($"  {Karma.Etykieta(gracz)}  ·  {Przetrwanie.OpisStanu(gracz)}");
                    _konsola.Pisz($"  Osada: {gracz.Osadnicy.Count} os.  ·  chaty: {gracz.Chaty}");
                    _konsola.Pisz($"  {Oboz.LiniaSurowcow(gracz)}");
                    _konsola.Pisz();
                    _konsola.Pisz("  [1]  🧭  Wyrusz na wyprawę");
                    _konsola.Pisz("  [2]  🧙  Karta postaci");
                    _konsola.Pisz("  [3]  🔥  Odpocznij przy ogniu (1 dzień)");
                    _konsola.Pisz("  [0]  🚪  Zakończ grę");
                    _konsola.Pisz();
                    _konsola.Pisz("  (Warsztat, targ, rozmowy i sprawy osady dojdą " +
                                  "w kolejnym etapie portu.)");
                    _konsola.Pisz();
                    string wybor = (_konsola.Czytaj("  Twój wybór: ") ?? "").Trim();
                    if (wybor == "0" || wybor == "1" || wybor == "2" || wybor == "3")
                    {
                        return wybor;
                    }
                    _konsola.Pisz("  Nieprawidłowy wybór.");
                    Utils.NacisnijEnter(_konsola);
                }
            }
        }

        private void Odpoczynek(Gracz gracz)
        {
            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');
            _konsola.Pisz("  🔥  ODPOCZYNEK");
            Utils.Linia(_konsola, '═');
            int lecz = Math.Max(1, gracz.MaxHp / 4);
            int przed = gracz.Hp;
            gracz.Hp = Math.Min(gracz.MaxHp, gracz.Hp + lecz);
            if (gracz.MaxMana > 0)
            {
                gracz.Mana = gracz.MaxMana;
            }
            _konsola.Pisz($"\n  Noc przy ogniu przywraca {gracz.Hp - przed} HP.");
            foreach (string wiesc in Swiat.MinijDni(gracz, _rng, 1, odpoczynek: true))
            {
                _konsola.Pisz(wiesc);
            }
            Utils.NacisnijEnter(_konsola);
        }
    }
}
