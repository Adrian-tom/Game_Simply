using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    public sealed partial class SwiatWolny
    {
        /// <summary>Jeden krok symulacji: ruch, zbieranie, budowanie, głód, zmęczenie, upływ doby.</summary>
        public void Aktualizuj(double dt, Wejscie w)
        {
            if (Martwy || dt <= 0)
            {
                return;
            }
            dt = Math.Min(dt, 0.1);
            _kursor = w.Kursor;
            if (w.Panel)
            {
                PanelOtwarty = !PanelOtwarty;
            }
            Kliknij(w.Klik);
            if (w.Poloz)
            {
                PostawWybrana();
            }
            RuszPostacie(dt);
            if (PanelOtwarty)
            {
                // Panel nie zatrzymuje świata, ale klawisze idą do niego, a nie do bohatera.
                ObsluzPanel(w);
                w = new Wejscie();
            }
            double mnoznikRuchu = WalkaWlaczona ? KrokWalki(dt, w) : 1.0;
            double dlugosc = Math.Sqrt(w.Dx * w.Dx + w.Dy * w.Dy);
            bool rusza = dlugosc > 1e-9;

            if (Spi)
            {
                if (rusza || w.Dzialaj || w.Buduj > 0 || w.Cios || w.Ciezki || w.Unik)
                {
                    Spi = false;
                    Pisz("Wstajesz.");
                }
            }
            else
            {
                if (rusza)
                {
                    if (mnoznikRuchu > 0)
                    {
                        if (!w.Blok && Akcja == AkcjaBohatera.Brak)
                        {
                            Kierunek = (w.Dx / dlugosc, w.Dy / dlugosc);
                        }
                        Ruch(w.Dx / dlugosc * Predkosc * mnoznikRuchu * dt, w.Dy / dlugosc * Predkosc * mnoznikRuchu * dt);
                    }
                    _zbior = null;
                }
                else
                {
                    AkcjaStojac(dt, w);
                }
                if (w.Zloz)
                {
                    Zloz();
                }
                if (w.Jedz)
                {
                    Jedz();
                }
                if (w.Spij)
                {
                    ZacznijSen();
                }
            }

            UplywCzasu(dt * 24.0 / DlugoscDobyS * (Spi ? SzybkoscSnu : 1.0));
        }

        private void Ruch(double dx, double dy)
        {
            // Osie osobno: gdy jedna jest zablokowana, bohater ślizga się wzdłuż przeszkody
            // (to samo robi Project Zomboid w IsoGameCharacter.slideAwayFromWalls).
            if (Wolne(X + dx, Y) && GeneratorSwiata.WGranicach((int)Math.Floor(X + dx), (int)Math.Floor(Y)))
            {
                X += dx;
            }
            if (Wolne(X, Y + dy) && GeneratorSwiata.WGranicach((int)Math.Floor(X), (int)Math.Floor(Y + dy)))
            {
                Y += dy;
            }
        }

        private void AkcjaStojac(double dt, Wejscie w)
        {
            if (w.Buduj > 0 && w.Buduj < KluczeBudowli.Length)
            {
                Postaw(KluczeBudowli[w.Buduj]);
                _zbior = null;
                return;
            }
            if (!w.Dzialaj)
            {
                _zbior = null;
                return;
            }
            (int X, int Y)? cel = ZnajdzCel();
            if (cel == null)
            {
                _zbior = null;
                return;
            }
            if (_zbior == null || _zbior.Value.X != cel.Value.X || _zbior.Value.Y != cel.Value.Y)
            {
                _zbior = (cel.Value.X, cel.Value.Y, 0.0);
            }
            double postep = _zbior.Value.Postep + dt / CzasZbioru;
            if (postep >= 1.0)
            {
                Zbierz(cel.Value.X, cel.Value.Y);
                _zbior = null;
            }
            else
            {
                _zbior = (cel.Value.X, cel.Value.Y, postep);
            }
        }

        /// <summary>Najbliższy obiekt do zebrania w zasięgu i w półkolu przed bohaterem.</summary>
        public (int X, int Y)? ZnajdzCel()
        {
            (int X, int Y)? najlepszy = null;
            double naj = double.MaxValue;
            int cx = (int)Math.Floor(X), cy = (int)Math.Floor(Y);
            for (int x = cx - 2; x <= cx + 2; x++)
            {
                for (int y = cy - 2; y <= cy + 2; y++)
                {
                    if (!Zbiory.ContainsKey(ObiektNa(x, y)))
                    {
                        continue;
                    }
                    double dx = x + 0.5 - X, dy = y + 0.5 - Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d > Zasieg)
                    {
                        continue;
                    }
                    double kierunek = (dx * Kierunek.X + dy * Kierunek.Y) / Math.Max(d, 1e-9);
                    if (kierunek < -0.2)
                    {
                        continue;
                    }
                    double wynik = d - 0.4 * kierunek;
                    if (wynik < naj)
                    {
                        naj = wynik;
                        najlepszy = (x, y);
                    }
                }
            }
            return najlepszy;
        }

        private void Zbierz(int x, int y)
        {
            Obiekt o = ObiektNa(x, y);
            if (!Zbiory.TryGetValue(o, out OpisZbioru opis))
            {
                return;
            }
            if (!_zuzyte.TryGetValue((x, y), out Zuzycie z))
            {
                z = new Zuzycie();
                _zuzyte[(x, y)] = z;
            }
            z.Uzyte++;
            if (z.Uzyte >= opis.Uzyc && opis.OdrostGodzin > 0)
            {
                z.OdrostGodzina = _czasGry + opis.OdrostGodzin;
            }
            var czesci = new List<string>();
            foreach (KeyValuePair<string, int> p in opis.Plon)
            {
                Dodaj(p.Key, p.Value);
                czesci.Add($"+{p.Value} {Nazwa(p.Key)}");
            }
            Pisz($"{char.ToUpperInvariant(opis.Czynnosc[0])}{opis.Czynnosc.Substring(1)}: {string.Join(", ", czesci)}.");
        }

        public static string Nazwa(string przedmiot)
        {
            switch (przedmiot)
            {
                case "drewno": return "drewna";
                case "kamien": return "kamienie";
                case "jagody": return "jagody";
                case "ziola": return "zioła";
                case "wlokno": return "włókna";
                case "spiwor": return "śpiwór";
                default: return przedmiot;
            }
        }

        /// <summary>Kafel tuż przed bohaterem — tu ląduje stawiana budowla.</summary>
        public (int X, int Y) KaflPrzed()
        {
            return ((int)Math.Floor(X + Kierunek.X * 1.2), (int)Math.Floor(Y + Kierunek.Y * 1.2));
        }

        private bool Stac(KeyValuePair<string, int>[] koszt)
        {
            return koszt.All(p => Razem(p.Key) >= p.Value);
        }

        /// <summary>Stawia budowlę przed bohaterem, jeśli jest miejsce i składniki. Zwraca powodzenie.</summary>
        public bool Postaw(string klucz)
        {
            (int x, int y) = KaflPrzed();
            return PostawNa(klucz, x, y);
        }

        /// <summary>Stawia budowlę na wskazanym kaflu. Zwraca powodzenie; powód odmowy trafia do dziennika.</summary>
        public bool PostawNa(string klucz, int x, int y)
        {
            if (!Koszty.TryGetValue(klucz, out KeyValuePair<string, int>[] koszt))
            {
                return false;
            }
            string powod = PowodBudowy(klucz, x, y);
            if (powod != null)
            {
                Pisz(powod);
                return false;
            }
            foreach (KeyValuePair<string, int> p in koszt)
            {
                Zabierz(p.Key, p.Value);
            }
            Budowle[(x, y)] = klucz;
            string nazwa = klucz == "spiwor" ? "Rozkładasz śpiwór." : klucz == "ognisko" ? "Palisz ognisko."
                         : klucz == "chata" ? $"Stawiasz chatę (+{MiejscaChaty} miejsca)." : "Stawiasz namiot.";
            Pisz(nazwa);
            PrzeliczMiejsca();
            if (!ObozZalozony && Budowle.ContainsValue("namiot") && Budowle.ContainsValue("ognisko"))
            {
                ObozZalozony = true;
                Pisz("Obóz stoi: namiot i ognisko. To dobre miejsce na osadę — jutro ktoś do ciebie dołączy.");
            }
            return true;
        }

        private void Jedz()
        {
            if (Ile("jagody") > 0)
            {
                Plecak["jagody"] = Ile("jagody") - 1;
                Glod = Math.Min(100.0, Glod + 18.0);
                Pisz("Zjadasz jagody.");
            }
            else if (PrzyObozie() && Obozowisko.Surowce.Wez("zywnosc") > 0)
            {
                Obozowisko.Surowce["zywnosc"] -= 1;
                Glod = Math.Min(100.0, Glod + 18.0);
                Pisz("Jesz z zapasów osady.");
            }
            else
            {
                Pisz("Nie masz nic do jedzenia.");
            }
        }

        /// <summary>Śpiwór lub namiot w zasięgu dwóch kafli i kilka kroków do odpoczynku.</summary>
        public string MiejsceSnu()
        {
            string wynik = null;
            foreach (KeyValuePair<(int X, int Y), string> b in Budowle)
            {
                if (b.Value != "spiwor" && b.Value != "namiot")
                {
                    continue;
                }
                double dx = b.Key.X + 0.5 - X, dy = b.Key.Y + 0.5 - Y;
                if (dx * dx + dy * dy <= 2.5 * 2.5 && (wynik == null || b.Value == "namiot"))
                {
                    wynik = b.Value;
                }
            }
            return wynik;
        }

        private void ZacznijSen()
        {
            if (MiejsceSnu() == null)
            {
                Pisz("Żeby spać, stań przy rozłożonym śpiworze albo namiocie.");
                return;
            }
            if (WalkaWlaczona && WrogBlisko(12.0))
            {
                Pisz("Nie zaśniesz z wrogiem tak blisko.");
                return;
            }
            if (Energia >= 90.0)
            {
                Pisz("Nie jesteś jeszcze zmęczony.");
                return;
            }
            Spi = true;
            _snNocny = Godzina >= 18 || Godzina < 5;
            Pisz("Kładziesz się spać.");
        }

        private void UplywCzasu(double godzin)
        {
            _czasGry += godzin;
            Godzina += godzin;
            while (Godzina >= 24.0)
            {
                Godzina -= 24.0;
                Dzien++;
                Pisz($"Zaczyna się dzień {Dzien}.");
                DzienObozu();
            }

            if (Spi)
            {
                bool namiot = MiejsceSnu() == "namiot";
                Energia = Math.Min(100.0, Energia + godzin * (namiot ? 14.0 : 9.0));
                Glod = Math.Max(0.0, Glod - godzin * 0.8);
                if (Energia >= 100.0 || (_snNocny && Godzina >= 6.0 && Godzina < 12.0) || Glod <= 5.0)
                {
                    Spi = false;
                    Pisz(Glod <= 5.0 ? "Budzisz się z głodu." : "Budzisz się wypoczęty.");
                }
            }
            else
            {
                Glod = Math.Max(0.0, Glod - godzin * 1.6);
                Energia = Math.Max(0.0, Energia - godzin * 5.0);
            }

            if (Glod <= 0.0 || Energia <= 0.0)
            {
                Hp -= godzin * 4.0;
            }
            else if (Glod > 30.0)
            {
                Hp = Math.Min(100.0, Hp + godzin * 1.5);
            }
            if (Hp <= 0.0 && !Martwy && WalkaWlaczona)
            {
                Smierc();
            }
            else if (Hp <= 0.0 && !Martwy)
            {
                Martwy = true;
                Spi = false;
                Pisz("Siły cię opuściły. Koniec drogi.");
            }
        }
    }
}
