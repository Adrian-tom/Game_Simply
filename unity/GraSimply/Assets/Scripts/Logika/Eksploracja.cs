using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>
    /// Wyprawa: mapa regionu, ruch na cztery strony, zwiad i zbieractwo.
    ///
    /// To przeniesiona pętla z <c>game/world.py</c>. Trzyma kształt oryginału —
    /// blokujące pytanie o wybór, menu rysowane od nowa po każdej akcji —
    /// bo w Unity logika chodzi na osobnym wątku i może sobie pozwolić
    /// na blokowanie dokładnie tak jak w terminalu.
    ///
    /// Czego tu jeszcze nie ma względem wersji pythonowej: walki, zdarzeń
    /// narracyjnych, budynków do wejścia i rozmów. To etap 2 — miejsca,
    /// w których się wpinają, są oznaczone w kodzie.
    /// </summary>
    public sealed class Eksploracja
    {
        /// <summary>Klawisze specjalne okna → odpowiedzi menu eksploracji.</summary>
        private static readonly Dictionary<string, string> SkrotyEksploracji =
            new Dictionary<string, string>
            {
                { "gora", "1" }, { "lewo", "2" }, { "prawo", "3" },
                { "dol", "4" }, { "spacja", "5" },
            };

        private static readonly (string Tytul, string Opis)[] NoweSrodowiska =
        {
            ("Wkraczasz na nowe ziemie...", "Horyzont odsłania przed tobą zupełnie nowy kraj."),
            ("Krajobraz się zmienia.", "Czujesz, że te tereny różnią się od wszystkiego, co widziałeś wcześniej."),
            ("Przekraczasz niewidzialną granicę.", "Powietrze staje się inne — to nowy region."),
            ("Świat zdaje się rozszerzać.", "Za horyzontem kryje się jeszcze więcej przygód."),
            ("Nowe środowisko, nowe wyzwania.", "Teren zmienia się gwałtownie — ruszasz dalej w nieznane."),
        };

        private readonly Gracz _gracz;
        private readonly IKonsola _konsola;
        private readonly Ekran _ekran;
        private readonly Losowanie _rng;

        public Eksploracja(Gracz gracz, IKonsola konsola, Ekran ekran, Losowanie rng)
        {
            _gracz = gracz;
            _konsola = konsola;
            _ekran = ekran;
            _rng = rng;
        }

        /// <summary>Pętla wyprawy. Zwraca „oboz” albo „przegrana”.</summary>
        public string Uruchom()
        {
            _gracz.WObozie = false;
            _gracz.CzasWyjscia = _gracz.Czas;
            Mapa.ZapewnijMape(_gracz);
            _konsola.Pisz(Przetrwanie.SpakujProwiant(_gracz));
            if (Kalendarz.PoraGracza(_gracz).Klucz == "zima" && !Przetrwanie.MaOdzienie(_gracz))
            {
                _konsola.Pisz("  ❄  Zima, a ty bez ciepłego odzienia — mróz będzie ranił " +
                              "co dzień (warsztat: ciepłe odzienie).");
            }
            Utils.NacisnijEnter(_konsola);

            while (true)
            {
                string wybor = MenuEksploracji();

                if (wybor == "0")
                {
                    return ZakonczWyprawe();
                }

                var kierunek = Mapa.Kierunki.FirstOrDefault(k => k.Klawisz == wybor);
                if (kierunek.Klawisz != null)
                {
                    // Licznik regionów rośnie tylko wtedy, gdy świat wygenerował
                    // nowy — to odróżnia odkrycie od powrotu w znane strony.
                    int regionowPrzed = Mapa.LiczbaRegionow(_gracz);
                    bool zmianaRegionu = Mapa.PrzesunGracza(_gracz, kierunek.Dx, kierunek.Dy);
                    foreach (string wiesc in Swiat.MinijDni(_gracz, _rng))
                    {
                        _konsola.Pisz(wiesc);
                    }
                    if (!_gracz.Zyje())
                    {
                        return "przegrana";
                    }
                    if (zmianaRegionu)
                    {
                        PokazZmianeRegionu(Mapa.LiczbaRegionow(_gracz) > regionowPrzed);
                    }
                    PokazWejscieNaPole(kierunek.Nazwa);
                    continue;
                }

                if (wybor == "5")
                {
                    string wynik = ZbadajPole();
                    if (wynik == "oboz")
                    {
                        return ZakonczWyprawe();
                    }
                    continue;
                }

                if (wybor == "6")
                {
                    string wynik = Oboz.ZbierzNaPolu(_gracz, _konsola, _rng);
                    if (wynik == "walka")
                    {
                        // Etap 2: tutaj wchodzi przeprowadz_walke() z combat.py.
                        _konsola.Pisz("\n  ⚔️  Przy zbieraniu ktoś cię zaskoczył! " +
                                      "(walka dojdzie w kolejnym etapie portu)");
                    }
                    Utils.NacisnijEnter(_konsola);
                }
            }
        }

        private string ZakonczWyprawe()
        {
            _gracz.WObozie = true;
            Przetrwanie.RozpakujProwiant(_gracz);
            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');
            _konsola.Pisz("  🏕  POWRÓT DO OBOZU");
            Utils.Linia(_konsola, '═');
            int dni = Math.Max(0, _gracz.Czas - _gracz.CzasWyjscia);
            _konsola.Pisz($"\n  Wracasz po {dni} dniach. Pozycja na mapie zostaje tam, " +
                          "gdzie ją zostawiłeś.");
            _konsola.Pisz($"  {Oboz.LiniaSurowcow(_gracz)}");
            Utils.NacisnijEnter(_konsola);
            return "oboz";
        }

        // ------------------------------------------------------------------ //
        //  Mapa                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Nagłówek mapy. Siatkę z glifami rysujemy tylko w trybie tekstowym —
        /// w oknie pokazuje ją scena obok konsoli, więc byłaby dublem.
        /// </summary>
        public void RysujMape(bool graficzny)
        {
            Mapa.ZapewnijMape(_gracz);
            Pole pole = Mapa.PoleGracza(_gracz);
            string miejsce = string.IsNullOrEmpty(pole.Punkt)
                ? ""
                : $"  ·  {Mapa.OpisPunktu(pole.Punkt)}";
            _konsola.Pisz($"  🗺  REGION [{_gracz.RegionX}, {_gracz.RegionY}]  ·  " +
                          $"poziom {_gracz.MapaGen}   pole ({_gracz.MapaX}, {_gracz.MapaY})   " +
                          $"odkryte {Mapa.LiczbaOdkrytych(_gracz)}/{Mapa.LiczbaPol()}");
            _konsola.Pisz($"  📍  {Mapa.OpisRegionu(_gracz)}");
            _konsola.Pisz($"  Biom: {Ikony.EtykietaBiomu(pole.Biom)}{miejsce}");
            _konsola.Pisz();
            if (graficzny)
            {
                return;
            }
            _konsola.Pisz("     " + string.Join(" ", Enumerable.Range(0, Mapa.Rozmiar)
                                                              .Select(x => x.ToString().PadLeft(2))));
            for (int y = 0; y < Mapa.Rozmiar; y++)
            {
                var komorki = new List<string>();
                for (int x = 0; x < Mapa.Rozmiar; x++)
                {
                    komorki.Add(Mapa.GlifPola(_gracz, x, y).PadLeft(2));
                }
                _konsola.Pisz($"  {y}  {string.Join(" ", komorki)}");
            }
            _konsola.Pisz();
            _konsola.Pisz($"  {Ikony.GraczMapa} ty   {Ikony.Mgla} nieodkryte");
            (List<string> biomy, List<string> punkty) = Mapa.SymboleOdkryte(_gracz);
            if (biomy.Count > 0)
            {
                _konsola.Pisz("  " + string.Join("   ", biomy.Select(Ikony.EtykietaBiomu)));
            }
            if (punkty.Count > 0)
            {
                _konsola.Pisz("  " + string.Join("   ", punkty.Select(Mapa.OpisPunktu)));
            }
            _konsola.Pisz();
        }

        private string MenuEksploracji()
        {
            while (true)
            {
                _konsola.Wyczysc();
                Utils.Linia(_konsola, '═');
                _konsola.Pisz("  🧭  EKSPLORACJA");
                Utils.Linia(_konsola, '═');
                _konsola.Pisz();
                RysujMape(_ekran != null);

                Pole pole = Mapa.PoleGracza(_gracz);
                foreach ((string klawisz, string nazwa, int dx, int dy) in Mapa.Kierunki)
                {
                    string cel = Mapa.EtykietaKierunku(_gracz, dx, dy);
                    _konsola.Pisz($"  [{klawisz}]  {Ikony.Kierunek(nazwa)}  {nazwa.PadRight(10)}  →  {cel}");
                }

                _konsola.Pisz(OpisAkcjiPola(pole));
                int zost = Oboz.PozostaleZbiory(pole);
                bool bezZbiorow = pole.Punkt == "obóz" || pole.Punkt == "boss" || pole.Punkt == "miasto"
                                  || Array.IndexOf(Mapa.PunktyMityczne, pole.Punkt) >= 0;
                if (bezZbiorow)
                {
                    _konsola.Pisz("  [6]  🧺  Zbierz surowce (tu niedostępne)");
                }
                else if (zost > 0)
                {
                    _konsola.Pisz($"  [6]  🧺  Zbierz surowce  (zostało {zost} na tym polu)");
                }
                else
                {
                    _konsola.Pisz("  [6]  🧺  Zbierz surowce  (pole wyczerpane)");
                }
                _konsola.Pisz($"  {Oboz.LiniaSurowcow(_gracz)}");
                _konsola.Pisz("  [0]  🏕  Wróć do obozu (pozycja zostaje)");
                _konsola.Pisz();

                _ekran?.UstawSkroty(SkrotyEksploracji);
                string wybor;
                try
                {
                    wybor = (_konsola.Czytaj("  Twój wybór: ") ?? "").Trim();
                }
                finally
                {
                    _ekran?.UstawSkroty(null);
                }
                if (wybor == "0" || wybor == "5" || wybor == "6"
                    || Mapa.Kierunki.Any(k => k.Klawisz == wybor))
                {
                    return wybor;
                }
                _konsola.Pisz("  Nieprawidłowy wybór.");
                Utils.NacisnijEnter(_konsola);
            }
        }

        private static string OpisAkcjiPola(Pole pole)
        {
            if (pole.Punkt == "obóz")
            {
                return "  [5]  🏕  Wejdź do obozu";
            }
            if (pole.Punkt == "boss")
            {
                return "  [5]  ☠  Wejdź w głąb legowiska";
            }
            if (Array.IndexOf(Mapa.PunktyMityczne, pole.Punkt) >= 0)
            {
                return $"  [5]  {Ikony.Punkt(pole.Punkt)}  Wejdź: {Mapa.OpisPunktu(pole.Punkt)}";
            }
            if (pole.Punkt == "miasto")
            {
                return "  [5]  🏙  Wejdź do miasta (osobna mapa)";
            }
            if (!string.IsNullOrEmpty(pole.Punkt))
            {
                string nazwa = Swiat.NazwaBudynku(pole.Biom, pole.Punkt);
                return $"  [5]  {Ikony.Punkt(pole.Punkt)}  Wejdź: {nazwa}";
            }
            return "  [5]  👁  Rozglądnij się po okolicy";
        }

        // ------------------------------------------------------------------ //
        //  Komunikaty podróży                                                 //
        // ------------------------------------------------------------------ //

        private void PokazZmianeRegionu(bool pierwszyRaz)
        {
            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');
            _konsola.Pisz(pierwszyRaz
                ? $"  ✨  NOWY REGION [{_gracz.RegionX}, {_gracz.RegionY}]  ·  " +
                  $"poziom {_gracz.MapaGen}  ✨"
                : $"  🧭  ZNANY REGION [{_gracz.RegionX}, {_gracz.RegionY}]  ·  " +
                  $"poziom {_gracz.MapaGen}");
            Utils.Linia(_konsola, '═');

            if (pierwszyRaz)
            {
                (string tytul, string opis) = _rng.Wybierz(NoweSrodowiska);
                _konsola.Pisz($"\n  {tytul}");
                _konsola.Pisz($"  {opis}");
                _konsola.Pisz("\n  Stoisz na skraju nieznanych ziem — tu wszystko trzeba poznać od nowa.");
            }
            else
            {
                _konsola.Pisz($"\n  Wracasz w znajome strony: {Mapa.OpisRegionu(_gracz)}.");
                _konsola.Pisz("  Odkryte pola i ślady po tobie zostały tam, gdzie je zostawiłeś.");
                _konsola.Pisz($"  Odkryte: {Mapa.LiczbaOdkrytych(_gracz)}/{Mapa.LiczbaPol()} " +
                              "pól tego regionu.");
            }
            _konsola.Pisz();
            Utils.NacisnijEnter(_konsola);
        }

        private void PokazWejscieNaPole(string nazwaKierunku)
        {
            Pole pole = Mapa.PoleGracza(_gracz);
            Biom biom = Swiat.SzablonBiomu(pole.Biom);
            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');
            _konsola.Pisz("  🧭  PODRÓŻ");
            Utils.Linia(_konsola, '═');
            _konsola.Pisz($"  {Ikony.Kierunek(nazwaKierunku)}  Idziesz na {nazwaKierunku}.");
            _konsola.Pisz($"  Trafiasz na: {Ikony.EtykietaBiomu(biom.Nazwa)}.");
            _konsola.Pisz($"  {biom.Opis}");
            if (pole.Punkt == "boss")
            {
                _konsola.Pisz("\n  ☠  Powietrze gęstnieje. Coś potężnego czeka na tym polu.");
            }
            else if (pole.Punkt == "obóz")
            {
                _konsola.Pisz("\n  🏕  Widzisz znajome palenisko — to twój obóz.");
            }
            else
            {
                // Miasto i punkty mityczne mają własne wejścia, więc Python nie
                // zapowiada ich jako „budynku na polu” — tutaj też nie zapowiadamy.
                string nazwa = Swiat.BudynekNaPolu(pole.Biom, pole.Punkt);
                if (nazwa != null)
                {
                    _konsola.Pisz($"\n  {Ikony.Punkt(pole.Punkt)}  Na polu stoi: {nazwa} " +
                                  $"({pole.Punkt}).");
                }
            }
            pole.Odwiedzone = true;
            Utils.NacisnijEnter(_konsola);
        }

        /// <summary>
        /// Akcja [5] na polu. Zwraca „oboz”, gdy gracz wraca do obozu,
        /// w innym razie null.
        /// </summary>
        private string ZbadajPole()
        {
            Pole pole = Mapa.PoleGracza(_gracz);
            if (pole.Punkt == "obóz")
            {
                return "oboz";
            }

            _konsola.Wyczysc();
            Utils.Linia(_konsola, '═');

            if (!string.IsNullOrEmpty(pole.Punkt))
            {
                // Etap 2: tutaj wchodzą zdarzenia budynków z world.py
                // (karczma, kuźnia, świątynia, jaskinia) i walki z bossami.
                string nazwa = Swiat.NazwaBudynku(pole.Biom, pole.Punkt);
                _konsola.Pisz($"  {Ikony.Punkt(pole.Punkt)}  {nazwa.ToUpperInvariant()}");
                Utils.Linia(_konsola, '═');
                _konsola.Pisz($"\n  Stoisz przed: {nazwa}.");
                _konsola.Pisz("  Wnętrza i zdarzenia tego miejsca dojdą w kolejnym etapie portu.");
                Utils.NacisnijEnter(_konsola);
                return null;
            }

            _konsola.Pisz("  👁  ZWIAD");
            Utils.Linia(_konsola, '═');
            Biom biom = Swiat.SzablonBiomu(pole.Biom);
            _konsola.Pisz($"\n  {biom.Opis}");

            // Test spostrzegawczości — pokazuje, że system k20 działa w porcie.
            int st = Atrybuty.Trudnosc(_gracz, 12);
            WynikTestu wynik = Atrybuty.RzucTest(_gracz, "spostrzegawczosc", st, _rng);
            _konsola.Pisz();
            _konsola.Pisz(Atrybuty.OpisTestu(wynik));
            if (wynik.Sukces)
            {
                _konsola.Pisz("\n  Wypatrujesz ślady na ziemi i zapamiętujesz drogę — " +
                              "okolica nie ma już przed tobą tajemnic.");
            }
            else
            {
                _konsola.Pisz("\n  Nic poza szumem wiatru. Ruszaj dalej.");
            }
            Utils.NacisnijEnter(_konsola);
            return null;
        }
    }
}
