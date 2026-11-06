using System;
using System.Collections.Generic;
using GraSimply.Grafika.Mroczny;
using GraSimply.Logika;

namespace GraSimply.Grafika
{
    /// <summary>
    /// Nakładka trybu przetrwania, rysowana wprost w 1280×720, bez powiększania z 640×360.
    ///
    ///     ┌──────────── wstęga, surowce ───────────┬── panel boczny ──┐
    ///     │ plecak                        dziennik │ zakładki budowy, │
    ///     │                                        │ lista budowli    │
    ///     │                @                       │ albo osada       │
    ///     │         cel, podpowiedź                │                  │
    ///     │ (życie)(portret)(energia) [B][T][Tab]… │                  │
    ///     └────────────────────────────────────────┴──────────────────┘
    ///
    /// Panel pojawia się tylko wtedy, gdy jest w czym wybierać: bohater stoi na środku ekranu,
    /// więc mapa wokół niego zostaje widoczna, a liczba pozycji nie zmienia dolnego paska.
    /// </summary>
    internal sealed class PulpitWolny
    {
        public const int W = Pulpit.Szerokosc, H = Pulpit.Wysokosc;

        /// <summary>Lewa krawędź i szerokość panelu bocznego.</summary>
        public const int PanelX = 880, PanelSzer = W - PanelX;

        /// <summary>Lewa krawędź paska akcji: na lewo od niej są kule życia, energii i portret.</summary>
        public const int LewaPaska = 322;

        public const int WysKafla = 64, SzerKafla = 86, Odstep = 6;

        /// <summary>Id tła panelu: kliknięcie w panel nie może trafić w świat pod spodem.</summary>
        public const string TloPanelu = "-";

        private const string Kroj = PulpitFantasy.Kroj, KrojMaly = "piksM";
        private const int WrPadding = 14;

        private static Barwa Hx(int rgb) => new Barwa((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

        private static readonly Barwa ZlotoJ = Hx(0xFCE08C), Zloto = Hx(0xC8A050), ZlotoC = Hx(0x6C4C24);
        private static readonly Barwa Cien = Hx(0x1A0F08), Krew = Hx(0x8C1810), KrewJ = Hx(0xD04030);
        private static readonly Barwa Mana = Hx(0x203C94), ManaJ = Hx(0x5078E0), Wstega = Hx(0x6C1008);
        private static readonly Barwa Szklo = Hx(0x140C08), Odblask = Hx(0xFFF0D0), Brak = new Barwa(232, 96, 80);
        private static readonly Barwa Czern = Hx(0x0A0604), Braz = Hx(0x3C2410), Wybor = Hx(0x4A3210), Jasny = Hx(0xFFF4C0);

        private readonly List<Napis> _napisy = new List<Napis>();
        private readonly List<Przycisk> _przyciski = new List<Przycisk>();
        private readonly Dictionary<string, Plotno> _ikony = new Dictionary<string, Plotno>();
        private readonly Dictionary<string, Plotno> _ikonySurowcow = new Dictionary<string, Plotno>();
        private readonly Dictionary<string, SpriteBryly> _portrety = new Dictionary<string, SpriteBryly>();

        private Plotno _c;
        private (int X, int Y)? _mysz;

        public List<Przycisk> Przyciski => _przyciski;

        /// <summary>Pasek postępu nad obiektem (piksele sceny 640×360, 0–1); rysowany raz, potem się czyści.</summary>
        public (int X, int Y, double Postep)? PasekSwiata;

        /// <summary>Wytrzymałość 0–1 nad imieniem bohatera; null, gdy tryb nie ma walki.</summary>
        public double? Wytrzymalosc;

        /// <summary>Paski życia nad wrogami (piksele sceny 640×360).</summary>
        public IReadOnlyList<(int X, int Y, double Zycie)> PaskiWrogow;

        /// <summary>Bieżący cel gracza: plakietka nad podpowiedzią; znika przy panelu osady.</summary>
        public string CelGracza;

        /// <summary>Dokłada interfejs do <paramref name="cel"/>, na którym leży już scena albo przezroczystość.</summary>
        public List<Napis> Zloz(Plotno cel, StanHud hud, string klasa, IReadOnlyList<string> plecak,
                                IReadOnlyList<string> dziennik, string podpowiedz, IReadOnlyList<string> panel,
                                IReadOnlyList<PodpisOsadnika> podpisy, Logika.Wolny.KafelkiWolne k, (int X, int Y)? mysz)
        {
            _c = cel;
            _mysz = mysz;
            _napisy.Clear();
            _przyciski.Clear();
            bool budowa = k != null && k.Kategorie.Count > 0;
            bool szuflada = panel != null || budowa;

            if (podpisy != null && panel == null)
            {
                foreach (PodpisOsadnika p in podpisy)
                {
                    Tekst(p.Tekst, p.X * PulpitFantasy.K - Szer(p.Tekst, false) / 2, (p.Y - PulpitFantasy.Wiersz + 2) * PulpitFantasy.K, p.Kolor);
                }
            }
            PaskiNaScenie();
            Naglowek(hud, szuflada ? PanelX / 2 : W / 2);
            Plecak(plecak);
            Dziennik(dziennik, szuflada ? PanelX - 12 : W - 12);
            Postac(hud, klasa);

            int gora = H - 8 - WysKafla;
            string nad = null;
            if (k != null)
            {
                nad = RzadPaska(k.Akcje, gora);
                if (k.Specjalne.Count > 0)
                {
                    gora -= WysKafla + Odstep;
                    nad = RzadPaska(k.Specjalne, gora) ?? nad;
                }
            }
            int prawa = szuflada ? PanelX - 8 : W - 8;
            int y = gora - 6;
            string pod = nad ?? podpowiedz;
            if (!string.IsNullOrEmpty(pod))
            {
                y = Plakietka(pod, LewaPaska, y, prawa, nad != null ? Jasny : Styl.Aktywny.Wyrozniony) - 4;
            }
            if (!string.IsNullOrEmpty(CelGracza) && panel == null)
            {
                Plakietka(CelGracza, LewaPaska, y, prawa, Styl.Aktywny.Tekst);
            }

            if (szuflada)
            {
                Przyciemnij(PanelX, 0, PanelSzer, H, 232, Czern);
                _c.Wypelnij(Zloto, PanelX, 0, 2, H);
                _c.Wypelnij(ZlotoC, PanelX + 2, 0, 1, H);
                if (budowa)
                {
                    Budowa(k);
                }
                else
                {
                    Osada(panel, k);
                }
                _przyciski.Add(new Przycisk { X = PanelX, Y = 0, Szer = PanelSzer, Wys = H, Klawisz = TloPanelu });
            }
            return new List<Napis>(_napisy);
        }

        // ------------------------------------------------------------------ //
        //  Panel boczny                                                       //
        // ------------------------------------------------------------------ //

        private static int LewaPanelu => PanelX + WrPadding;

        private static int SzerPanelu => PanelSzer - 2 * WrPadding;

        private void Budowa(Logika.Wolny.KafelkiWolne k)
        {
            Tekst("BUDOWA", LewaPanelu, 12, Styl.Aktywny.Wyrozniony);
            int y = Zakladki(k.Kategorie, 46);
            foreach (Logika.Wolny.Kafelek b in k.Budowle)
            {
                y = Wiersz(b, y) + 4;
            }
        }

        /// <summary>Zakładki dzielą szerokość panelu według długości nazw, żeby żadna nie była ucięta.</summary>
        private int Zakladki(List<Logika.Wolny.Kafelek> z, int y)
        {
            int tekstu = 0;
            foreach (Logika.Wolny.Kafelek t in z)
            {
                tekstu += Szer(t.Nazwa, true);
            }
            int zapas = Math.Max(0, (SzerPanelu - Odstep * (z.Count - 1) - tekstu) / Math.Max(1, z.Count));
            int x = LewaPanelu;
            foreach (Logika.Wolny.Kafelek t in z)
            {
                int w = Szer(t.Nazwa, true) + zapas;
                Tlo(t, x, y, w, 36);
                TekstMaly(t.Nazwa, x + zapas / 2, y + 7, Nad(x, y, w, 36) ? Jasny : Styl.Aktywny.Tekst);
                Dodaj(t.Id, x, y, w, 36);
                x += w + Odstep;
            }
            return y + 36 + 8;
        }

        /// <summary>Wiersz listy: ikona, nazwa i cały opis, który zawija się, gdy jest długi. Zwraca dół wiersza.</summary>
        private int Wiersz(Logika.Wolny.Kafelek k, int y)
        {
            const int wciecie = 56;
            int w = SzerPanelu;
            int szerOpisu = w - wciecie - 8;
            int h = 8 + 26 + 22 * Math.Max(1, LiczWierszeOpisu(k, szerOpisu)) + 6;
            int x = LewaPanelu;
            Tlo(k, x, y, w, h);
            Ikona(k.Ikona, x + 28, y + h / 2);
            bool nad = Nad(x, y, w, h);
            Tekst(k.Nazwa, x + wciecie, y + 6, !k.Dostepny ? Styl.Aktywny.Przygaszony : nad ? Jasny : Styl.Aktywny.Tekst);
            if (k.Stan.HasValue)
            {
                string stan = k.Stan.Value.Tekst;
                TekstMaly(stan, x + w - 8 - Szer(stan, true), y + 9, k.Stan.Value.Ok ? Styl.Aktywny.Przygaszony : Brak);
            }
            RysujOpis(k, x + wciecie, y + 34, szerOpisu);
            Dodaj(k.Id, x, y, w, h);
            return y + h;
        }

        private static string CzescOpisu(Logika.Wolny.Kafelek k, int i)
        {
            return k.Opis[i].Tekst + (i < k.Opis.Count - 1 ? ", " : "");
        }

        private static int LiczWierszeOpisu(Logika.Wolny.Kafelek k, int szer)
        {
            int wierszy = 1, x = 0;
            for (int i = 0; i < k.Opis.Count; i++)
            {
                int s = Szer(CzescOpisu(k, i), true);
                if (x > 0 && x + s > szer)
                {
                    wierszy++;
                    x = 0;
                }
                x += s;
            }
            return wierszy;
        }

        private void RysujOpis(Logika.Wolny.Kafelek k, int x0, int y, int szer)
        {
            int x = 0;
            for (int i = 0; i < k.Opis.Count; i++)
            {
                string t = CzescOpisu(k, i);
                int s = Szer(t, true);
                if (x > 0 && x + s > szer)
                {
                    y += 22;
                    x = 0;
                }
                TekstMaly(t, x0 + x, y, k.Opis[i].Ok ? Styl.Aktywny.Wyrozniony : Brak);
                x += s;
            }
        }

        private void Osada(IReadOnlyList<string> panel, Logika.Wolny.KafelkiWolne k)
        {
            Tekst(panel[0], LewaPanelu, 12, Styl.Aktywny.Wyrozniony);
            int y = 44;
            if (k != null)
            {
                foreach (Logika.Wolny.Kafelek o in k.Osadnicy)
                {
                    Tlo(o, LewaPanelu, y, SzerPanelu, 48);
                    TekstMaly(o.Nazwa, LewaPanelu + 8, y + 3, Nad(LewaPanelu, y, SzerPanelu, 48) ? Jasny : Styl.Aktywny.Tekst);
                    if (o.Opis.Count > 0)
                    {
                        TekstMaly(o.Opis[0].Tekst, LewaPanelu + 8, y + 24, o.Opis[0].Ok ? Styl.Aktywny.Tekst : Brak);
                    }
                    Dodaj(o.Id, LewaPanelu, y, SzerPanelu, 48);
                    y += 50;
                }
            }
            int pomin = 1 + (k != null ? k.Osadnicy.Count : 0);
            int znakow = (int)(SzerPanelu / 11.5);
            for (int i = pomin; i < panel.Count; i++)
            {
                foreach (string l in Konsola.Zawin(panel[i], znakow))
                {
                    TekstMaly(l.Trim(), LewaPanelu, y, Styl.Aktywny.Tekst);
                    y += 22;
                }
            }
            if (k == null)
            {
                return;
            }
            y += 6;
            if (k.Plan.Count > 0)
            {
                int w = (SzerPanelu - Odstep * (k.Plan.Count - 1)) / k.Plan.Count;
                for (int i = 0; i < k.Plan.Count; i++)
                {
                    Logika.Wolny.Kafelek b = k.Plan[i];
                    int x = LewaPanelu + i * (w + Odstep);
                    Tlo(b, x, y, w, 54);
                    TekstMaly(b.Nazwa, x + (w - Szer(b.Nazwa, true)) / 2, y + 4, Styl.Aktywny.Tekst);
                    TekstMaly(b.Opis[0].Tekst, x + (w - Szer(b.Opis[0].Tekst, true)) / 2, y + 27, Styl.Aktywny.Wyrozniony);
                    Dodaj(b.Id, x, y, w, 54);
                }
                y += 54 + Odstep;
            }
            y = Decyzje(k.Decyzje, y);
            for (int i = 0; i < k.Zajecia.Count; i++)
            {
                Logika.Wolny.Kafelek z = k.Zajecia[i];
                int w = (SzerPanelu - Odstep) / 2;
                int x = LewaPanelu + i % 2 * (w + Odstep);
                int zy = y + i / 2 * 36;
                Tlo(z, x, zy, w, 32);
                IkonyWolneMale(z.Ikona, x + 16, zy + 16);
                TekstMaly(z.Nazwa, x + 34, zy + 5, Nad(x, zy, w, 32) ? Jasny : Styl.Aktywny.Tekst);
                Dodaj(z.Id, x, zy, w, 32);
            }
        }

        /// <summary>Decyzja z opisem to pełny wiersz, a bez opisu krótki przycisk w rzędzie obok innych.</summary>
        private int Decyzje(List<Logika.Wolny.Kafelek> decyzje, int y)
        {
            int x = LewaPanelu;
            int reszta = SzerPanelu;
            bool rzad = false;
            foreach (Logika.Wolny.Kafelek d in decyzje)
            {
                if (d.Opis.Count > 0)
                {
                    y = Wiersz(d, y) + 4;
                    continue;
                }
                int w = Math.Min(reszta, Szer(d.Nazwa, true) + 22);
                Tlo(d, x, y, w, 34);
                TekstMaly(d.Nazwa, x + 11, y + 6, Nad(x, y, w, 34) ? Jasny : Styl.Aktywny.Tekst);
                Dodaj(d.Id, x, y, w, 34);
                x += w + Odstep;
                reszta -= w + Odstep;
                rzad = true;
            }
            return rzad ? y + 34 + Odstep : y;
        }

        private void Tlo(Logika.Wolny.Kafelek k, int x, int y, int w, int h)
        {
            bool nad = Nad(x, y, w, h);
            Przyciemnij(x, y, w, h, nad || k.Wybrany ? 245 : 215, k.Wybrany ? Wybor : nad ? Braz : Czern);
            _c.Obramowanie(nad || k.Wybrany ? ZlotoJ : ZlotoC, x, y, w, h, k.Wybrany ? 2 : 1);
        }

        private void Dodaj(string id, int x, int y, int w, int h)
        {
            _przyciski.Add(new Przycisk { X = x, Y = y, Szer = w, Wys = h, Klawisz = id });
        }

        // ------------------------------------------------------------------ //
        //  Dolny pasek                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>Ikona z klawiszem. Nazwę pokazuje podpowiedź, gdy kursor stoi nad kafelkiem; zwraca ją.</summary>
        private string RzadPaska(List<Logika.Wolny.Kafelek> rzad, int y)
        {
            string nad = null;
            for (int i = 0; i < rzad.Count; i++)
            {
                Logika.Wolny.Kafelek k = rzad[i];
                int x = LewaPaska + i * (SzerKafla + Odstep);
                Tlo(k, x, y, SzerKafla, WysKafla);
                Ikona(k.Ikona, x + SzerKafla / 2, y + 22);
                string dopisek = k.Opis.Count > 0 ? k.Opis[0].Tekst : "";
                Barwa kolor = !k.Dostepny ? Styl.Aktywny.Przygaszony : Styl.Aktywny.Wyrozniony;
                TekstMaly(dopisek, x + (SzerKafla - Szer(dopisek, true)) / 2, y + 41, kolor);
                Dodaj(k.Id, x, y, SzerKafla, WysKafla);
                if (Nad(x, y, SzerKafla, WysKafla))
                {
                    nad = $"{k.Nazwa} {dopisek}";
                }
            }
            return nad;
        }

        /// <summary>Plakietka na jeden albo więcej wierszy, od dołu w górę. Zwraca jej górną krawędź.</summary>
        private int Plakietka(string napis, int x, int dol, int prawa, Barwa kolor)
        {
            int znakow = Math.Max(10, (int)((prawa - x - 14) / 11.5));
            List<string> wiersze = new List<string>();
            foreach (string l in Konsola.Zawin(napis, znakow))
            {
                wiersze.Add(l.Trim());
            }
            int w = 0;
            foreach (string l in wiersze)
            {
                w = Math.Max(w, Szer(l, true));
            }
            int h = wiersze.Count * 22 + 6;
            int y = dol - h;
            Przyciemnij(x, y, w + 14, h, 200, Czern);
            _c.Obramowanie(ZlotoC, x, y, w + 14, h, 1);
            for (int i = 0; i < wiersze.Count; i++)
            {
                TekstMaly(wiersze[i], x + 7, y + 3 + i * 22, kolor);
            }
            return y;
        }

        // ------------------------------------------------------------------ //
        //  Góra ekranu                                                        //
        // ------------------------------------------------------------------ //

        private void Naglowek(StanHud hud, int cx)
        {
            string napis = $"{EkranGry.Czysty(hud.Biom ?? "")} · {EkranGry.Czysty(hud.Region ?? "")} · {EkranGry.Czysty(hud.Data ?? "")}".ToUpperInvariant();
            int w = Szer(napis, false) + 40;
            int x0 = cx - w / 2;
            _c.Wypelnij(Krew, x0 - 12, 10, 12, 28);
            _c.Wypelnij(Krew, x0 + w, 10, 12, 28);
            _c.Wypelnij(Wstega, x0, 6, w, 34);
            _c.Wypelnij(Zloto, x0, 8, w, 2);
            _c.Wypelnij(Zloto, x0, 36, w, 2);
            Tekst(napis, cx - Szer(napis, false) / 2, 9, Styl.Aktywny.Wyrozniony);

            var pozycje = new List<(string Ikona, string Liczba)>();
            foreach (KeyValuePair<string, int> s in hud.ListaSurowcow)
            {
                pozycje.Add((s.Key, s.Value.ToString()));
            }
            pozycje.Add(("zloto", hud.Zloto.ToString()));
            int szer = 0;
            foreach ((string _, string l) in pozycje)
            {
                szer += 24 + Szer(l, true) + 14;
            }
            int x = cx - szer / 2;
            foreach ((string ik, string l) in pozycje)
            {
                IkonaSurowca(ik, x, 50);
                TekstMaly(l, x + 24, 47, l == "0" ? Styl.Aktywny.Przygaszony : Styl.Aktywny.Tekst);
                x += 24 + Szer(l, true) + 14;
            }
        }

        private void Plecak(IReadOnlyList<string> plecak)
        {
            if (plecak.Count == 0)
            {
                return;
            }
            int w = 0;
            foreach (string l in plecak)
            {
                w = Math.Max(w, Szer(l, true));
            }
            Przyciemnij(8, 84, w + 20, plecak.Count * 23 + 10, 170, Czern);
            int y = 89;
            foreach (string l in plecak)
            {
                TekstMaly(l, 18, y, Styl.Aktywny.Tekst);
                y += 23;
            }
        }

        private void Dziennik(IReadOnlyList<string> dziennik, int prawa)
        {
            var wiersze = new List<string>();
            foreach (string d in dziennik)
            {
                foreach (string w in Konsola.Zawin(Konsola.BezEmoji(d).Trim(), 34))
                {
                    wiersze.Add(w.Trim());
                }
            }
            int y = 84;
            for (int i = 0; i < wiersze.Count && i < 10; i++)
            {
                int alfa = Math.Max(90, 230 - i * 20);
                Barwa k = i == 0 ? Styl.Aktywny.Wyrozniony : Styl.Aktywny.Tekst;
                TekstMaly(wiersze[i], prawa - Szer(wiersze[i], true), y, new Barwa(k.R, k.G, k.B, (byte)alfa));
                y += 23;
            }
        }

        // ------------------------------------------------------------------ //
        //  Postać: kule życia i energii, portret                              //
        // ------------------------------------------------------------------ //

        private void Postac(StanHud hud, string klasa)
        {
            int y = H - 52;
            const int r = 42;
            Kula(54, y, r, hud.Hp / (double)Math.Max(1, hud.MaxHp), Krew, KrewJ);
            string zycie = $"{hud.Hp}/{hud.MaxHp}";
            TekstMaly(zycie, 54 - Szer(zycie, true) / 2, y - 11, KolorZycia(hud));
            if (hud.MaxMana > 0)
            {
                Kula(250, y, r, hud.Mana / (double)Math.Max(1, hud.MaxMana), Mana, ManaJ);
                string energia = $"{hud.Mana}/{hud.MaxMana}";
                TekstMaly(energia, 250 - Szer(energia, true) / 2, y - 11, Styl.Aktywny.Tekst);
            }
            Medalion(152, y, r, klasa);

            string podpis = $"{hud.Imie} · {EkranGry.Czysty(hud.Klasa)} {hud.Poziom}";
            int dol = y - r - 8;
            int gora = Plakietka(podpis, 4, dol, LewaPaska - 4, Styl.Aktywny.Wyrozniony);
            if (Wytrzymalosc.HasValue)
            {
                double w = Math.Max(0.0, Math.Min(1.0, Wytrzymalosc.Value));
                const int szer = 288;
                _c.Wypelnij(Cien, 7, gora - 13, szer + 2, 10);
                _c.Wypelnij(w < 0.25 ? new Barwa(214, 96, 48) : new Barwa(116, 196, 84), 8, gora - 12, (int)(szer * w), 8);
            }
        }

        private static Barwa KolorZycia(StanHud hud)
        {
            double ile = hud.Hp / (double)Math.Max(1, hud.MaxHp);
            return ile > 0.5 ? Styl.Aktywny.Tekst : ile > 0.25 ? Hx(0xFCFC7C) : Hx(0xFC4030);
        }

        private void Kula(int cx, int cy, int r, double ile, Barwa ciemna, Barwa jasna)
        {
            ile = Math.Max(0, Math.Min(1, ile));
            double poziom = r - 2 * r * ile;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    double d = Math.Sqrt(x * x + y * y);
                    if (d > r)
                    {
                        continue;
                    }
                    Barwa k = Szklo;
                    if (y >= poziom)
                    {
                        bool lustro = y < poziom + 3 + Math.Sin(x * 0.4);
                        bool blask = x + y < -r * 0.6 && d > r * 0.5;
                        k = (lustro || blask) && d <= r - 3 ? jasna : ciemna;
                    }
                    _c.Ustaw(cx + x, cy + y, k);
                }
            }
            Okrag(cx, cy, r + 1, ZlotoC);
            Okrag(cx, cy, r + 2, Zloto);
            Okrag(cx, cy, r + 3, Zloto);
            Okrag(cx, cy, r + 4, Cien);
            for (int i = 0; i < 14; i++)
            {
                double a = Math.PI * (1.12 + i * 0.03);
                _c.Ustaw(cx + (int)Math.Round(Math.Cos(a) * (r - 6)), cy + (int)Math.Round(Math.Sin(a) * (r - 6)), Odblask);
            }
        }

        private void Okrag(int cx, int cy, int r, Barwa k)
        {
            int kroki = r * 7;
            for (int i = 0; i < kroki; i++)
            {
                double a = i * 2 * Math.PI / kroki;
                _c.Ustaw(cx + (int)Math.Round(Math.Cos(a) * r), cy + (int)Math.Round(Math.Sin(a) * r), k);
            }
        }

        private const double SkalaPortretu = 4.8;

        private void Medalion(int cx, int cy, int r, string klasa)
        {
            string k = klasa ?? "";
            if (!_portrety.TryGetValue(k, out SpriteBryly s))
            {
                s = Postacie.Bohater(k).Skaluj(SkalaPortretu).Zrob(11);
                _portrety[k] = s;
            }
            int gx = -s.Ox;
            int gy = -s.Oy - (int)(Postacie.WysokoscGlowy * SkalaPortretu * Rzut.Wysokosc);
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y > r * r)
                    {
                        continue;
                    }
                    int sx = gx + x, sy = gy + y + r / 4;
                    bool mamy = sx >= 0 && sy >= 0 && sx < s.Obraz.Szerokosc && sy < s.Obraz.Wysokosc && s.Obraz.Alfa(sx, sy) > 0;
                    _c.Ustaw(cx + x, cy + y, mamy ? s.Obraz.Wez(sx, sy) : Hx(0x331F10));
                }
            }
            Okrag(cx, cy, r + 1, ZlotoC);
            Okrag(cx, cy, r + 2, Zloto);
            Okrag(cx, cy, r + 3, ZlotoJ);
            Okrag(cx, cy, r + 4, Cien);
        }

        // ------------------------------------------------------------------ //
        //  Prymitywy                                                          //
        // ------------------------------------------------------------------ //

        private void PaskiNaScenie()
        {
            const int k = PulpitFantasy.K;
            if (PasekSwiata.HasValue)
            {
                (int bx, int by, double postep) = PasekSwiata.Value;
                _c.Wypelnij(Cien, bx * k - 36, by * k - 2, 72, 14);
                _c.Wypelnij(ZlotoJ, bx * k - 34, by * k, (int)(68 * Math.Max(0.0, Math.Min(1.0, postep))), 10);
                PasekSwiata = null;
            }
            if (PaskiWrogow != null)
            {
                foreach ((int wx, int wy, double zycie) in PaskiWrogow)
                {
                    _c.Wypelnij(Cien, wx * k - 26, wy * k - 2, 52, 10);
                    _c.Wypelnij(new Barwa(214, 48, 38), wx * k - 24, wy * k, (int)Math.Ceiling(48 * Math.Max(0.0, Math.Min(1.0, zycie))), 6);
                }
            }
        }

        /// <summary>Ikony są rysowane w małej siatce, więc dwukrotnie powiększone trzymają ten sam styl co świat.</summary>
        private void Ikona(string klucz, int cx, int cy)
        {
            if (!_ikony.TryGetValue(klucz, out Plotno p))
            {
                var male = new Plotno(32, 32);
                Wolny.IkonyWolne.Rysuj(male, klucz, 16, 16);
                p = male.Skaluj(2);
                _ikony[klucz] = p;
            }
            _c.Nalóż(p, cx - 32, cy - 32);
        }

        private void IkonyWolneMale(string klucz, int cx, int cy)
        {
            Wolny.IkonyWolne.Rysuj(_c, klucz, cx, cy);
        }

        private void IkonaSurowca(string klucz, int x, int y)
        {
            if (!_ikonySurowcow.TryGetValue(klucz, out Plotno p))
            {
                var male = new Plotno(12, 12);
                IkonyPikselowe.Rysuj(male, klucz, 0, 0);
                p = male.Skaluj(2);
                _ikonySurowcow[klucz] = p;
            }
            _c.Nalóż(p, x, y);
        }

        private bool Nad(int x, int y, int w, int h)
        {
            return _mysz.HasValue && _mysz.Value.X >= x && _mysz.Value.X < x + w && _mysz.Value.Y >= y && _mysz.Value.Y < y + h;
        }

        private void Przyciemnij(int x, int y, int w, int h, int alfa, Barwa k)
        {
            byte[] d = _c.Dane;
            for (int py = Math.Max(0, y); py < Math.Min(H, y + h); py++)
            {
                for (int px = Math.Max(0, x); px < Math.Min(W, x + w); px++)
                {
                    int i = (py * W + px) * 4;
                    if (d[i + 3] == 0)
                    {
                        d[i] = k.R;
                        d[i + 1] = k.G;
                        d[i + 2] = k.B;
                        d[i + 3] = (byte)alfa;
                        continue;
                    }
                    d[i] = (byte)((d[i] * (255 - alfa) + k.R * alfa) / 255);
                    d[i + 1] = (byte)((d[i + 1] * (255 - alfa) + k.G * alfa) / 255);
                    d[i + 2] = (byte)((d[i + 2] * (255 - alfa) + k.B * alfa) / 255);
                    d[i + 3] = (byte)Math.Max((int)d[i + 3], alfa);
                }
            }
        }

        /// <summary>Szerokość napisu w pikselach: krój 22 px ma 14 px na znak, a krój 18 px około 11,5.</summary>
        private static int Szer(string t, bool maly)
        {
            return maly ? (int)Math.Ceiling(t.Length * 11.5) : t.Length * 14;
        }

        private void Tekst(string t, int x, int y, Barwa kolor)
        {
            Napisz(t, x, y, kolor, Kroj);
        }

        private void TekstMaly(string t, int x, int y, Barwa kolor)
        {
            Napisz(t, x, y, kolor, KrojMaly);
        }

        private void Napisz(string t, int x, int y, Barwa kolor, string kroj)
        {
            if (string.IsNullOrEmpty(t))
            {
                return;
            }
            _napisy.Add(new Napis(t, x + 2, y + 2, new Barwa(Cien.R, Cien.G, Cien.B, kolor.A), kroj));
            _napisy.Add(new Napis(t, x, y, kolor, kroj));
        }
    }
}
