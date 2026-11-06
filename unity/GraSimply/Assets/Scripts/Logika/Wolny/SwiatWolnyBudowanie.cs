using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>
    /// Tryb budowania i kafelki interfejsu: pasek akcji zamiast cyfr. Budować wchodzi się kafelkiem
    /// (albo klawiszem B), wybiera budowlę z rzędu kafelków, a duch na mapie pokazuje, gdzie stanie
    /// i czy wolno. Lewy klik albo Enter stawia, prawy klik albo Esc wycofuje wybór.
    ///
    /// Kafelek przekazuje grze identyfikator (<see cref="Kliknij"/>): „a:…” akcja, „b:…” budowla,
    /// „z:…” zajęcie wybranego osadnika, „o:numer” wybór osadnika. Skróty klawiszowe wysyłają te
    /// same identyfikatory, więc klawiatura i mysz robią dokładnie to samo.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const double ZasiegBudowy = 6.0;

        /// <summary>Kolejność budowli w rzędzie kafelków.</summary>
        public static readonly string[] Budowlane = { "spiwor", "ognisko", "namiot", "chata", "palisada", "brama", "wieza" };

        public bool TrybBudowy;

        /// <summary>Budowla wybrana do postawienia (null = jeszcze nic).</summary>
        public string WybranaBudowla;

        private (int X, int Y)? _kursor;

        /// <summary>Czy starczy składników na budowlę (plecak razem z magazynem).</summary>
        public bool Stac(string klucz)
        {
            return Koszty.TryGetValue(klucz, out KeyValuePair<string, int>[] koszt) && Stac(koszt);
        }

        /// <summary>Kafel, którego dotyczy budowanie: pod kursorem, a bez myszy — przed bohaterem.</summary>
        public (int X, int Y) KafelBudowy()
        {
            return _kursor ?? KaflPrzed();
        }

        /// <summary>Czemu nie można tu postawić budowli (komunikat dla gracza) albo null, gdy można.</summary>
        public string PowodBudowy(string klucz, int x, int y)
        {
            double dx = x + 0.5 - X, dy = y + 0.5 - Y;
            if (dx * dx + dy * dy > ZasiegBudowy * ZasiegBudowy)
            {
                return "To za daleko — podejdź bliżej.";
            }
            if (!GeneratorSwiata.WGranicach(x, y) || Zajety(x, y) || Budowle.ContainsKey((x, y))
                || ObiektNa(x, y) != Obiekt.Brak)
            {
                return "Tu nie da się nic postawić.";
            }
            if (klucz != "spiwor" && X + Promien > x + 0.12 && X - Promien < x + 0.88
                && Y + Promien > y + 0.12 && Y - Promien < y + 0.88)
            {
                return "Stoisz w tym miejscu — odsuń się.";
            }
            if (!Stac(klucz))
            {
                string braki = string.Join(", ", Koszty[klucz].Select(p => $"{p.Value} {Nazwa(p.Key)}"));
                return $"Brakuje składników. Potrzeba: {braki}.";
            }
            return null;
        }

        /// <summary>Duch budowli na mapie: gdzie i czy wolno. Null, gdy nic nie jest wybrane.</summary>
        public (int X, int Y, string Klucz, bool Mozna)? PodgladBudowy()
        {
            if (!TrybBudowy || WybranaBudowla == null)
            {
                return null;
            }
            (int x, int y) = KafelBudowy();
            return (x, y, WybranaBudowla, PowodBudowy(WybranaBudowla, x, y) == null);
        }

        /// <summary>Lewy klik albo Enter w trybie budowania.</summary>
        private void PostawWybrana()
        {
            if (!TrybBudowy || WybranaBudowla == null)
            {
                return;
            }
            (int x, int y) = KafelBudowy();
            if (PostawNa(WybranaBudowla, x, y) && !Stac(WybranaBudowla))
            {
                WybranaBudowla = null;       // skończyły się składniki: nie zostawiamy czerwonego ducha
            }
        }

        /// <summary>Prawy klik albo Esc: najpierw wycofuje wybór, potem wychodzi z trybu i z panelu.</summary>
        public bool Wycofaj()
        {
            if (WybranaBudowla != null)
            {
                WybranaBudowla = null;
                return true;
            }
            if (TrybBudowy || PanelOtwarty)
            {
                TrybBudowy = false;
                PanelOtwarty = false;
                return true;
            }
            return false;
        }

        /// <summary>Kafelek albo skrót klawiszowy o danym identyfikatorze.</summary>
        public void Kliknij(string id)
        {
            if (string.IsNullOrEmpty(id) || Martwy)
            {
                return;
            }
            if (Spi)
            {
                Spi = false;
                Pisz("Wstajesz.");
                if (id == "a:spij")
                {
                    return;
                }
            }
            string wartosc = id.Length > 2 ? id.Substring(2) : "";
            if (id.StartsWith("b:"))
            {
                WybranaBudowla = WybranaBudowla == wartosc ? null : wartosc;
            }
            else if (id.StartsWith("s:"))
            {
                UzyjSpecjala(wartosc);
            }
            else if (id.StartsWith("z:"))
            {
                UstawZajecieWybranego(wartosc);
            }
            else if (id.StartsWith("o:"))
            {
                if (int.TryParse(wartosc, out int nr) && nr >= 0 && nr < Obozowisko.Osadnicy.Count)
                {
                    Wybrany = nr;
                }
            }
            else if (id == "a:buduj")
            {
                TrybBudowy = !TrybBudowy;
                WybranaBudowla = null;
                PanelOtwarty = false;
            }
            else if (id == "a:osada")
            {
                PanelOtwarty = !PanelOtwarty;
                TrybBudowy = false;
                WybranaBudowla = null;
            }
            else if (id == "a:magazyn")
            {
                Zloz();
            }
            else if (id == "a:jedz")
            {
                Jedz();
            }
            else if (id == "a:spij")
            {
                ZacznijSen();
            }
        }

        private void UstawZajecieWybranego(string zajecie)
        {
            List<Osadnik> ludzie = Obozowisko.Osadnicy;
            if (Wybrany < 0 || Wybrany >= ludzie.Count)
            {
                return;
            }
            Osadnik o = ludzie[Wybrany];
            Pisz(Osada.UstawZajecie(Obozowisko, o, zajecie));
            PostacOsadnika p = Postacie.FirstOrDefault(q => q.O == o);
            if (p != null && SrodekObozu().HasValue)
            {
                NowyCel(p, SrodekObozu().Value, false);
            }
        }
    }
}
