using System;
using System.Collections.Generic;
using GraSimply.Grafika.Mroczny;
using GraSimply.Logika;

namespace GraSimply.Grafika
{
    /// <summary>Klikalny prostokąt na pulpicie (piksele 1280×720) i odpowiedź, którą wysyła.</summary>
    public sealed class Przycisk
    {
        public int X, Y, Szer, Wys;

        /// <summary>Co wysłać grze; pusty napis = Enter.</summary>
        public string Klawisz;

        public bool Zawiera(float x, float y) => x >= X && y >= Y && x < X + Szer && y < Y + Wys;
    }

    /// <summary>Co jest w konsoli i na co gra czeka — wejście dla interfejsu, który sam układa tekst.</summary>
    public struct StanKonsoli
    {
        public IList<string> Linie;
        public bool Pyta;
        public bool CzekaNaEnter;

        /// <summary>To, co gracz wpisuje (gdy gra pyta), albo null.</summary>
        public string Wpisywane;

        public bool Kursor;
    }

    /// <summary>
    /// Interfejs fantasy: mapa na cały ekran i lekkie elementy na niej.
    ///
    ///     ┌──────────── wstęga: biom · region · data ────────────┐
    ///     │ info o polu           surowce              dziennik ─┤
    ///     │                ↖ 1 Północ                            │
    ///     │      2 Zachód ←   (postać)   → 3 Wschód              │
    ///     │                       ↓ 4 Południe                   │
    ///     │ (HP)(portret)(MANA)                   [5 Rozejrzyj]  │
    ///     └──────────────────────────────────────[0 Wróć     ]──┘
    ///
    /// Opcje ruchu stają się strzałkami wokół postaci, pozostałe — złotymi
    /// plakietkami w prawym dolnym rogu. Narracja trafia do dziennika na
    /// mapie, a dłuższe ekrany i menu bez mapy (tytuł, wybór klasy, obóz)
    /// do półprzezroczystego panelu na środku.
    ///
    /// Rysuje w natywnych 640×360 i powiększa ×2 — interfejs ma te same
    /// grube piksele co mapa. Napisy wychodzą w pikselach 1280×720 krojem
    /// pikselowym 22 px (dwukrotność natywnych 11 px), więc też trzymają siatkę.
    /// </summary>
    internal sealed class PulpitFantasy
    {
        public const int W = 640, H = 360, K = 2;

        /// <summary>Krój pikselowy: 7 px na znak i 14 px na wiersz w natywnej rozdzielczości.</summary>
        public const int Znak = 7, Wiersz = 14;

        /// <summary>Nazwa kroju napisów — Unity i podglad.py dają mu 22 px.</summary>
        public const string Kroj = "piks";

        private static Barwa Hx(int rgb) => new Barwa((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);

        private static readonly Barwa ZlotoJ = Hx(0xFCE08C), Zloto = Hx(0xC8A050), ZlotoC = Hx(0x6C4C24);
        private static readonly Barwa Cien = Hx(0x1A0F08), Krew = Hx(0x8C1810), KrewJ = Hx(0xD04030);
        private static readonly Barwa Mana = Hx(0x203C94), ManaJ = Hx(0x5078E0), Wstega = Hx(0x6C1008);
        private static readonly Barwa Szklo = Hx(0x140C08), Odblask = Hx(0xFFF0D0);

        private static readonly Barwa[] Drewno =
        {
            Hx(0x1A0F08), Hx(0x26170C), Hx(0x331F10), Hx(0x412814), Hx(0x50321A), Hx(0x5E3C20),
        };

        private readonly Plotno _natywne = new Plotno(W, H, przezroczyste: false);
        private readonly List<Napis> _napisy = new List<Napis>();
        private readonly List<Przycisk> _przyciski = new List<Przycisk>();
        private readonly Dictionary<string, SpriteBryly> _portrety = new Dictionary<string, SpriteBryly>();

        /// <summary>Ostatnia narracja (ekran z „naciśnij Enter”) — zostaje w dzienniku i blednie.</summary>
        private readonly List<string> _dziennik = new List<string>();

        private (int X, int Y)? _mysz;

        public List<Przycisk> Przyciski => _przyciski;

        /// <summary>Płótno sceny w natywnej rozdzielczości — scena rysuje się wprost do niego.</summary>
        public Plotno Natywne => _natywne;

        // ------------------------------------------------------------------ //
        //  Składanie klatki                                                   //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Dokłada interfejs do narysowanej już sceny i powiększa całość do
        /// <paramref name="cel"/>. Zwraca napisy (piksele 1280×720).
        /// </summary>
        /// <param name="podpisy">Imiona osadników nad głowami (scena osady) — w pikselach natywnych sceny.</param>
        public List<Napis> Zloz(Plotno cel, StanHud hud, string klasa, (int X, int Y)? graczXy,
                                StanKonsoli konsola, (int X, int Y)? mysz,
                                IReadOnlyList<PodpisOsadnika> podpisy = null)
        {
            _napisy.Clear();
            _przyciski.Clear();
            if (podpisy != null)
            {
                foreach (PodpisOsadnika p in podpisy)
                {
                    TekstSrodek(p.Tekst, p.X, p.Y - Wiersz + 2, p.Kolor);
                }
            }
            _mysz = mysz.HasValue ? (mysz.Value.X / K, mysz.Value.Y / K) : ((int, int)?)null;

            EkranGry ekran = EkranGry.Z(konsola.Linie, konsola.Pyta, konsola.CzekaNaEnter);
            if (ekran.CzekaNaEnter)
            {
                _dziennik.Clear();
                if (ekran.Tytul.Length > 0)
                {
                    _dziennik.Add(ekran.Tytul);
                }
                _dziennik.AddRange(ekran.Tresc);
            }

            if (hud != null)
            {
                Naglowek(hud);
                Postac(hud, klasa);
            }
            else
            {
                WstegaZTekstem("GRA SIMPLY", W / 2, 4);
                Tekst("F2: noc · F11: pełny ekran", 6, H - Wiersz - 2, Hx(0x7C6A50));
            }

            bool krotkaNarracja = ekran.CzekaNaEnter && WierszeDziennika().Count <= 8;
            if (ekran.MaKierunki && graczXy.HasValue)
            {
                Strzalki(ekran);
                InfoOPolu(ekran.Tresc);
                Dziennik(false);
                var reszta = ekran.Opcje.FindAll(o => !o.Kierunek.HasValue);
                StosPrzyciskow(reszta, konsola.Wpisywane, konsola.Kursor);
            }
            else if (krotkaNarracja)
            {
                Dziennik(true);
                StosPrzyciskow(new List<OpcjaEkranu> { new OpcjaEkranu { Klawisz = "", Tekst = "Dalej »" } }, null, false);
            }
            else if (podpisy != null && konsola.Pyta && !ekran.CzekaNaTekst && !ekran.CzekaNaEnter
                     && ekran.Opcje.Count > 0)
            {
                // Plac osady jest sceną, a nie tłem: menu obozu nie może go przykryć
                // panelem na środku. Treść idzie do rogu, opcje do plakietek z boku.
                InfoObozu(ekran);
                OpcjeObozu(ekran.Opcje, konsola.Wpisywane, konsola.Kursor);
            }
            else if (konsola.Pyta)
            {
                Panel(ekran, konsola);
            }
            else
            {
                Dziennik(false); // logika liczy — zostaje to, co było
            }

            Powieksz(_natywne, cel);
            return new List<Napis>(_napisy);
        }

        private static int NajdluzszaLinia(List<string> linie)
        {
            int m = 0;
            foreach (string l in linie)
            {
                m = Math.Max(m, l.Length);
            }
            return m;
        }

        // ------------------------------------------------------------------ //
        //  Nagłówek: wstęga, surowce                                          //
        // ------------------------------------------------------------------ //

        private void Naglowek(StanHud hud)
        {
            string region = EkranGry.Czysty(hud.Region ?? "");
            string data = EkranGry.Czysty(hud.Data ?? "");
            string biom = EkranGry.Czysty(hud.Biom ?? "");
            WstegaZTekstem($"{biom} · {region} · {data}".ToUpperInvariant(), W / 2, 4);

            // Surowce i złoto: ikony z liczbami pod wstęgą.
            var pozycje = new List<(string Ikona, string Liczba)>();
            foreach (KeyValuePair<string, int> s in hud.ListaSurowcow)
            {
                pozycje.Add((s.Key, s.Value.ToString()));
            }
            pozycje.Add(("zloto", hud.Zloto.ToString()));
            int szer = 0;
            foreach ((string _, string l) in pozycje)
            {
                szer += 10 + l.Length * Znak + 8;
            }
            int x = W / 2 - szer / 2;
            foreach ((string ik, string l) in pozycje)
            {
                IkonyPikselowe.Rysuj(_natywne, ik, x, 30);
                bool zero = l == "0";
                Tekst(l, x + 10, 27, zero ? Styl.Aktywny.Przygaszony : Styl.Aktywny.Tekst);
                x += 10 + l.Length * Znak + 8;
            }
        }

        private void WstegaZTekstem(string tekst, int cx, int y)
        {
            int w = tekst.Length * Znak + 30;
            int x0 = cx - w / 2;
            // końce wstęgi z wcięciem
            for (int dy = 0; dy < 17; dy++)
            {
                int wciecie = 4 - Math.Abs(dy - 8) / 2;
                for (int dx = wciecie; dx < 10; dx++)
                {
                    _natywne.Ustaw(x0 - 10 + dx, y + 2 + dy, Krew);
                    _natywne.Ustaw(x0 + w + 9 - dx, y + 2 + dy, Krew);
                }
            }
            _natywne.Wypelnij(Wstega, x0, y, w, 21);
            _natywne.Wypelnij(Zloto, x0, y + 1, w, 1);
            _natywne.Wypelnij(Zloto, x0, y + 18, w, 1);
            _natywne.Wypelnij(Cien, x0, y + 21, w, 1);
            TekstSrodek(tekst, cx, y + 3, Styl.Aktywny.Wyrozniony);
        }

        // ------------------------------------------------------------------ //
        //  Postać: kule życia i many, portret                                 //
        // ------------------------------------------------------------------ //

        private void Postac(StanHud hud, string klasa)
        {
            const int y = 326;
            Kula(34, y, 26, hud.Hp / (double)Math.Max(1, hud.MaxHp), Krew, KrewJ);
            if (hud.MaxMana > 0)
            {
                Kula(158, y, 26, hud.Mana / (double)Math.Max(1, hud.MaxMana), Mana, ManaJ);
                TekstSrodek($"{hud.Mana}/{hud.MaxMana}", 158, y - 6, Styl.Aktywny.Tekst);
            }
            TekstSrodek($"{hud.Hp}/{hud.MaxHp}", 34, y - 6, KolorZycia(hud));
            Medalion(96, y - 4, 28, klasa);
            string podpis = $"{hud.Imie} · {EkranGry.Czysty(hud.Klasa)} {hud.Poziom}";
            Plakietka(podpis, 96 - (podpis.Length * Znak + 10) / 2, y - 48, Styl.Aktywny.Wyrozniony, false);
        }

        /// <summary>Liczby życia bieleją, żółkną i czerwienieją z ubywaniem HP — czytelne bez patrzenia na kulę.</summary>
        private static Barwa KolorZycia(StanHud hud)
        {
            double ile = hud.Hp / (double)Math.Max(1, hud.MaxHp);
            return ile > 0.5 ? Styl.Aktywny.Tekst : ile > 0.25 ? Hx(0xFCFC7C) : Hx(0xFC4030);
        }

        /// <summary>Szklana kula z cieczą w złotej obręczy; poziom cieczy = ułamek.</summary>
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
                    Barwa k;
                    if (y >= poziom)
                    {
                        double fala = Math.Sin(x * 0.6) * 0.6;
                        bool lustro = y < poziom + 2 + fala;
                        bool blask = x + y < -r * 0.6 && d > r * 0.5;
                        k = (lustro || blask) && d <= r - 2 ? jasna : ciemna;
                    }
                    else
                    {
                        k = Szklo;
                    }
                    _natywne.Ustaw(cx + x, cy + y, k);
                }
            }
            Okrag(cx, cy, r + 1, ZlotoC);
            Okrag(cx, cy, r + 2, Zloto);
            Okrag(cx, cy, r + 3, Cien);
            // odblask szkła: krótki łuk w lewym górnym rogu
            for (int i = 0; i < 9; i++)
            {
                double a = Math.PI * (1.12 + i * 0.045);
                _natywne.Ustaw(cx + (int)Math.Round(Math.Cos(a) * (r - 4)), cy + (int)Math.Round(Math.Sin(a) * (r - 4)),
                               i % 4 == 0 ? Szklo : Odblask);
            }
        }

        private void Okrag(int cx, int cy, int r, Barwa k)
        {
            int kroki = (int)(r * 7);
            for (int i = 0; i < kroki; i++)
            {
                double a = i * 2 * Math.PI / kroki;
                _natywne.Ustaw(cx + (int)Math.Round(Math.Cos(a) * r), cy + (int)Math.Round(Math.Sin(a) * r), k);
            }
        }

        /// <summary>Powiększenie postaci w portrecie — głowa ma wypełnić medalion, nie cała sylwetka.</summary>
        private const double SkalaPortretu = 3.2;

        /// <summary>Portret: głowa i ramiona postaci tej klasy, w okrągłej złotej obręczy.</summary>
        private void Medalion(int cx, int cy, int r, string klasa)
        {
            string k = klasa ?? "";
            if (!_portrety.TryGetValue(k, out SpriteBryly s))
            {
                s = Postacie.Bohater(k).Skaluj(SkalaPortretu).Zrob(11);
                _portrety[k] = s;
            }
            // Środek głowy w obrazie sprite'a: podstawa leży w (−Ox, −Oy).
            int gx = -s.Ox;
            int gy = -s.Oy - (int)(Postacie.WysokoscGlowy * SkalaPortretu * Rzut.Wysokosc);
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    double d = Math.Sqrt(x * x + y * y);
                    if (d > r)
                    {
                        continue;
                    }
                    double v = 0.18 + 0.3 * (1 - Math.Sqrt((x + r * 0.3) * (x + r * 0.3) + (y + r * 0.4) * (y + r * 0.4)) / (2 * r));
                    Barwa b = Drewno[Math.Max(0, Math.Min(Drewno.Length - 1, (int)(v * Drewno.Length)))];
                    int sx = gx + x, sy = gy + y + r / 4;
                    if (sx >= 0 && sy >= 0 && sx < s.Obraz.Szerokosc && sy < s.Obraz.Wysokosc
                        && s.Obraz.Alfa(sx, sy) > 0)
                    {
                        b = s.Obraz.Wez(sx, sy);
                    }
                    _natywne.Ustaw(cx + x, cy + y, b);
                }
            }
            Okrag(cx, cy, r + 1, ZlotoC);
            Okrag(cx, cy, r + 2, Zloto);
            Okrag(cx, cy, r + 3, ZlotoJ);
            Okrag(cx, cy, r + 4, Cien);
        }

        // ------------------------------------------------------------------ //
        //  Ruch: strzałki na mapie                                            //
        // ------------------------------------------------------------------ //

        private void Strzalki(EkranGry ekran)
        {
            if (!(Styl.Aktywny.Generator is IKadrPostaci gen) || !gen.PozycjaPostaci.HasValue)
            {
                StosPrzyciskow(ekran.Opcje.FindAll(o => o.Kierunek.HasValue), null, false);
                return;
            }
            (int px, int py) = gen.PozycjaPostaci.Value;
            int oy = py - (int)(0.28 * Rzut.Wysokosc); // od piersi, nie od stóp
            (int sx, int sy) = (px, py);
            foreach (OpcjaEkranu o in ekran.Opcje)
            {
                if (!o.Kierunek.HasValue)
                {
                    continue;
                }
                // Kierunek ekranowy = różnica rzutów dwóch punktów świata odległych o pole.
                (int ax, int ay) = gen.NaKadr(0, 0);
                (int bx, int by) = gen.NaKadr(o.Kierunek.Value.Dx, o.Kierunek.Value.Dy);
                double dx = bx - ax, dy = by - ay;
                double d = Math.Sqrt(dx * dx + dy * dy);
                dx /= d;
                dy /= d;
                double dl = Math.Min(d * 1.1, 92);
                int x0 = (int)(sx + dx * 22), y0 = (int)(oy + dy * 22);
                int x1 = (int)(sx + dx * (22 + dl)), y1 = (int)(oy + dy * (22 + dl));

                string cel = string.IsNullOrEmpty(o.Cel) ? "" : o.Cel == "???" ? " ???" : $" · {o.Cel}";
                string napis = $"{o.Klawisz} {o.Tekst}{cel}";
                int w = napis.Length * Znak + 10;
                int lx = (int)(x1 + dx * (w / 2 + 6)) - w / 2;
                int ly = (int)(y1 + dy * 14) - 8;
                lx = Math.Max(4, Math.Min(W - w - 4, lx));
                ly = Math.Max(44, Math.Min(H - 100, ly));
                bool nad = Nad(lx, ly, w, 16);
                Barwa kolor = nad ? Hx(0xFFF4C0) : ZlotoJ;
                Strzalka(x0, y0, x1, y1, kolor);
                Plakietka(napis, lx, ly, Styl.Aktywny.Tekst, true, o.Klawisz);
            }
        }

        private void Strzalka(int x0, int y0, int x1, int y1, Barwa k)
        {
            Gruba(x0 + 1, y0 + 1, x1 + 1, y1 + 1, Cien);
            Gruba(x0, y0, x1, y1, k);
            double dx = x1 - x0, dy = y1 - y0;
            double d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            double ux = dx / d, uy = dy / d;
            // grot: trójkąt wypełniany liniami
            for (int i = 0; i <= 6; i++)
            {
                double t = i / 6.0;
                double bx = x1 + ux * 6 * (1 - t), by = y1 + uy * 6 * (1 - t);
                double szer = 4 * t;
                _natywne.Odcinek(k, (int)Math.Round(bx - uy * szer), (int)Math.Round(by + ux * szer),
                                 (int)Math.Round(bx + uy * szer), (int)Math.Round(by - ux * szer));
            }
        }

        private void Gruba(int x0, int y0, int x1, int y1, Barwa k)
        {
            _natywne.Odcinek(k, x0, y0, x1, y1);
            _natywne.Odcinek(k, x0, y0 + 1, x1, y1 + 1);
        }

        private void InfoOPolu(List<string> tresc)
        {
            int y = 44;
            foreach (string surowa in tresc)
            {
                // Region i biom są już na wstędze — z linii regionu zostaje pole i postęp odkrywania.
                string linia = surowa;
                if (linia.StartsWith("Biom:", StringComparison.Ordinal))
                {
                    continue;
                }
                int pole = linia.IndexOf("pole", StringComparison.Ordinal);
                if (linia.StartsWith("REGION", StringComparison.Ordinal) && pole > 0)
                {
                    linia = linia.Substring(pole).Replace("  ", " · ");
                }
                foreach (string w in Konsola.Zawin(linia, 38))
                {
                    if (y > 44 + 3 * Wiersz)
                    {
                        return;
                    }
                    Tekst(w.Trim(), 6, y, Styl.Aktywny.Przygaszony);
                    y += Wiersz;
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Dziennik i przyciski                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Narracja w prawym górnym rogu; bieżąca jasna, zapamiętana blednie ku dołowi.</summary>
        private const int ZnakowDziennika = 42;

        private List<string> WierszeDziennika()
        {
            var wiersze = new List<string>();
            foreach (string l in _dziennik)
            {
                foreach (string w in Konsola.Zawin(l, ZnakowDziennika))
                {
                    wiersze.Add(w.Trim());
                }
            }
            return wiersze;
        }

        private void Dziennik(bool biezacy)
        {
            List<string> wiersze = WierszeDziennika();
            int y = 44;
            for (int i = 0; i < wiersze.Count && i < 12; i++)
            {
                string w = wiersze[i];
                int alfa = biezacy ? 255 : Math.Max(70, 210 - i * 22);
                Barwa k = i == 0 ? Styl.Aktywny.Wyrozniony : Styl.Aktywny.Tekst;
                Tekst(w, W - 6 - w.Length * Znak, y, new Barwa(k.R, k.G, k.B, (byte)alfa));
                y += Wiersz;
            }
        }

        /// <summary>Plakietki opcji jedna nad drugą w prawym dolnym rogu.</summary>
        private void StosPrzyciskow(List<OpcjaEkranu> opcje, string wpisywane, bool kursor)
        {
            int y = H - 6 - 16;
            for (int i = opcje.Count - 1; i >= 0; i--)
            {
                OpcjaEkranu o = opcje[i];
                string napis = string.IsNullOrEmpty(o.Klawisz) ? o.Tekst : $"{o.Klawisz} {o.Tekst}";
                if (napis.Length > 44)
                {
                    napis = napis.Substring(0, 43) + "…";
                }
                int w = napis.Length * Znak + 10;
                Plakietka(napis, W - 6 - w, y, Styl.Aktywny.Tekst, true, o.Klawisz ?? "");
                y -= 19;
            }
            if (!string.IsNullOrEmpty(wpisywane))
            {
                string pole = "› " + wpisywane + (kursor ? "▌" : " ");
                Plakietka(pole, W - 6 - (pole.Length * Znak + 10), y, Styl.Aktywny.Wyrozniony, false);
            }
        }

        // ------------------------------------------------------------------ //
        //  Menu obozu na placu osady: z boku, żeby nie zasłaniać sceny         //
        // ------------------------------------------------------------------ //

        /// <summary>Szerokość kolumny informacji obozu w znakach — mieści się nad skrajem placu.</summary>
        private const int ZnakowInfoObozu = 26;

        /// <summary>Ile wierszy informacji obozu pokazujemy — reszta zasłoniłaby plac.</summary>
        private const int WierszyInfoObozu = 5;

        /// <summary>Tytuł i krótkie podsumowanie obozu w lewym górnym rogu, na ciemnej plakietce.</summary>
        private void InfoObozu(EkranGry e)
        {
            var wiersze = new List<string>();
            foreach (string l in e.Tresc)
            {
                // Uwagi w nawiasach („dojdą w kolejnym etapie”) to przypisy dla grającego
                // w konsolę — na placu zajęłyby tylko miejsce.
                if (l.TrimStart().StartsWith("(", StringComparison.Ordinal))
                {
                    continue;
                }
                foreach (string w in Konsola.Zawin(l, ZnakowInfoObozu))
                {
                    string c = w.Trim();
                    if (c.Length > 0)
                    {
                        wiersze.Add(c);
                    }
                }
            }
            if (wiersze.Count > WierszyInfoObozu)
            {
                wiersze.RemoveRange(WierszyInfoObozu, wiersze.Count - WierszyInfoObozu);
            }
            bool tytul = e.Tytul.Length > 0;
            if (wiersze.Count == 0 && !tytul)
            {
                return;
            }
            int znakow = tytul ? e.Tytul.Length : 0;
            foreach (string w in wiersze)
            {
                znakow = Math.Max(znakow, w.Length);
            }
            int wys = (wiersze.Count + (tytul ? 1 : 0)) * Wiersz + 8;
            Przyciemnij(4, 42, znakow * Znak + 14, wys, 170);
            int y = 46;
            if (tytul)
            {
                Tekst(e.Tytul.ToUpperInvariant(), 10, y, Styl.Aktywny.Wyrozniony);
                y += Wiersz;
            }
            foreach (string w in wiersze)
            {
                Tekst(w, 10, y, Styl.Aktywny.Tekst);
                y += Wiersz;
            }
        }

        /// <summary>
        /// Plakietki opcji obozu w prawym górnym rogu, od góry w dół — to wolny kąt
        /// sceny (lustrzany do informacji po lewej); dół zajmuje dziedziniec, na którym
        /// czekają osadnicy. Przy dłuższej liście kolejne kolumny idą w lewo.
        /// </summary>
        private void OpcjeObozu(List<OpcjaEkranu> opcje, string wpisywane, bool kursor)
        {
            const int naKolumne = 9;
            const int gora = 46;
            int kolumn = (opcje.Count + naKolumne - 1) / naKolumne;
            int x = W - 6;
            int dolPierwszej = gora;
            for (int c = 0; c < kolumn; c++)
            {
                int od = c * naKolumne;
                int ile = Math.Min(naKolumne, opcje.Count - od);
                int szer = 0;
                var napisy = new string[ile];
                for (int i = 0; i < ile; i++)
                {
                    OpcjaEkranu o = opcje[od + i];
                    string napis = string.IsNullOrEmpty(o.Klawisz) ? o.Tekst : $"{o.Klawisz} {o.Tekst}";
                    if (napis.Length > 38)
                    {
                        napis = napis.Substring(0, 37) + "…";
                    }
                    napisy[i] = napis;
                    szer = Math.Max(szer, napis.Length * Znak + 10);
                }
                x -= szer;
                int y = gora;
                for (int i = 0; i < ile; i++)
                {
                    Plakietka(napisy[i], x, y, Styl.Aktywny.Tekst, true, opcje[od + i].Klawisz ?? "", szer);
                    y += 19;
                }
                if (c == 0)
                {
                    dolPierwszej = y;
                }
                x -= 6;
            }
            if (!string.IsNullOrEmpty(wpisywane))
            {
                string pole = "› " + wpisywane + (kursor ? "▌" : " ");
                Plakietka(pole, W - 6 - (pole.Length * Znak + 10), dolPierwszej, Styl.Aktywny.Wyrozniony, false);
            }
        }

        // ------------------------------------------------------------------ //
        //  Panel na środku: menu bez mapy, dłuższe ekrany, pytania o tekst    //
        // ------------------------------------------------------------------ //

        private void Panel(EkranGry e, StanKonsoli k)
        {
            const int maksZnakow = 58;
            var tresc = new List<string>();
            foreach (string l in e.Tresc)
            {
                foreach (string w in Konsola.Zawin(l, maksZnakow))
                {
                    tresc.Add(w.Trim());
                }
            }
            int maksTresci = (H - 120) / Wiersz - e.Opcje.Count;
            if (tresc.Count > maksTresci && maksTresci > 0)
            {
                tresc.RemoveRange(0, tresc.Count - maksTresci); // ważniejszy koniec — to, co właśnie się stało
            }
            var opcje = new List<string>();
            foreach (OpcjaEkranu o in e.Opcje)
            {
                string n = $"{o.Klawisz}  {o.Tekst}";
                opcje.Add(n.Length > maksZnakow ? n.Substring(0, maksZnakow - 1) + "…" : n);
            }
            string pole = null;
            if (e.CzekaNaTekst)
            {
                pole = (k.Wpisywane ?? "") + (k.Kursor ? "▌" : " ");
            }

            int znakow = Math.Max(24, Math.Max(NajdluzszaLinia(tresc), NajdluzszaLinia(opcje)));
            znakow = Math.Max(znakow, e.Zacheta.Length + 16);
            int szer = Math.Min(W - 40, znakow * Znak + 28);
            int wys = 22 + tresc.Count * Wiersz + (tresc.Count > 0 ? 6 : 0) + opcje.Count * 19
                      + (pole != null ? 2 * Wiersz + 10 : 0) + (e.CzekaNaEnter ? 22 : 0) + 8;
            int x = W / 2 - szer / 2;
            int y = Math.Max(44, H / 2 - wys / 2 - 10);
            Przyciemnij(x, y, szer, wys, 215);
            Ramka(x, y, szer, wys);
            string tytul = e.Tytul.Length > 0 ? e.Tytul.ToUpperInvariant() : e.MaLogo ? "NOWA PRZYGODA" : "";
            if (tytul.Length > 0)
            {
                WstegaZTekstem(tytul, W / 2, y - 10);
            }

            int ty = y + 18;
            foreach (string l in tresc)
            {
                Tekst(l, x + 14, ty, Styl.Aktywny.Tekst);
                ty += Wiersz;
            }
            if (tresc.Count > 0)
            {
                ty += 6;
            }
            foreach (string n in opcje)
            {
                string klawisz = n.Substring(0, n.IndexOf(' '));
                Plakietka(n, x + 10, ty, Styl.Aktywny.Tekst, true, klawisz, szer - 20);
                ty += 19;
            }
            if (pole != null)
            {
                Tekst(e.Zacheta, x + 14, ty, Styl.Aktywny.Przygaszony);
                ty += Wiersz + 2;
                Przyciemnij(x + 12, ty, szer - 24, 16, 255);
                _natywne.Obramowanie(ZlotoC, x + 12, ty, szer - 24, 16, 1);
                Tekst(pole, x + 18, ty + 1, Styl.Aktywny.Wyrozniony);
                ty += 18;
            }
            if (e.CzekaNaEnter)
            {
                ty += 2;
                Plakietka("Dalej »", x + szer - 14 - (7 * Znak + 10), ty, Styl.Aktywny.Tekst, true, "");
            }
        }

        // ------------------------------------------------------------------ //
        //  Prymitywy                                                          //
        // ------------------------------------------------------------------ //

        private bool Nad(int x, int y, int w, int h)
        {
            return _mysz.HasValue && _mysz.Value.X >= x && _mysz.Value.X < x + w
                   && _mysz.Value.Y >= y && _mysz.Value.Y < y + h;
        }

        /// <summary>
        /// Plakietka: ciemne półprzezroczyste tło ze złotą krawędzią. Klikalna,
        /// jeśli ma klawisz; pod kursorem myszy jaśnieje.
        /// </summary>
        private void Plakietka(string napis, int x, int y, Barwa kolor, bool klikalna, string klawisz = null,
                               int szer = 0)
        {
            int w = szer > 0 ? szer : napis.Length * Znak + 10;
            const int h = 16;
            bool nad = klikalna && Nad(x, y, w, h);
            Przyciemnij(x, y, w, h, nad ? 235 : 190, nad ? Hx(0x3C2410) : Hx(0x0A0604));
            _natywne.Obramowanie(nad ? ZlotoJ : ZlotoC, x, y, w, h, 1);
            int spacja = klikalna && !string.IsNullOrEmpty(klawisz) ? napis.IndexOf(' ') : -1;
            if (spacja > 0)
            {
                // klawisz złotem, reszta zwykłym kolorem
                Tekst(napis.Substring(0, spacja), x + 5, y + 1, Styl.Aktywny.Wyrozniony);
                Tekst(napis.Substring(spacja), x + 5 + spacja * Znak, y + 1, nad ? Hx(0xFFF4C0) : kolor);
            }
            else
            {
                Tekst(napis, x + 5, y + 1, nad ? Hx(0xFFF4C0) : kolor);
            }
            if (klikalna && klawisz != null)
            {
                _przyciski.Add(new Przycisk { X = x * K, Y = y * K, Szer = w * K, Wys = h * K, Klawisz = klawisz });
            }
        }

        private void Ramka(int x, int y, int w, int h)
        {
            _natywne.Obramowanie(Cien, x - 1, y - 1, w + 2, h + 2, 1);
            _natywne.Wypelnij(ZlotoJ, x, y, w, 1);
            _natywne.Wypelnij(ZlotoJ, x, y, 1, h);
            _natywne.Wypelnij(ZlotoC, x + w - 1, y, 1, h);
            _natywne.Wypelnij(ZlotoC, x, y + h - 1, w, 1);
            _natywne.Obramowanie(Zloto, x + 1, y + 1, w - 2, h - 2, 1);
            foreach ((int cx, int cy) in new[] { (x, y), (x + w - 1, y), (x, y + h - 1), (x + w - 1, y + h - 1) })
            {
                Okucie(cx, cy);
            }
        }

        /// <summary>Złote okucie narożnika z czerwonym kamieniem.</summary>
        private void Okucie(int cx, int cy)
        {
            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) <= 3)
                    {
                        _natywne.Ustaw(cx + dx, cy + dy, dx + dy < -1 ? ZlotoJ : dx + dy < 2 ? Zloto : ZlotoC);
                    }
                }
            }
            _natywne.Ustaw(cx, cy, KrewJ);
            _natywne.Ustaw(cx + 1, cy, Krew);
            _natywne.Ustaw(cx, cy + 1, Krew);
        }

        private void Przyciemnij(int x, int y, int w, int h, int alfa, Barwa? kolor = null)
        {
            Barwa k = kolor ?? Hx(0x0A0604);
            byte[] d = _natywne.Dane;
            for (int py = Math.Max(0, y); py < Math.Min(H, y + h); py++)
            {
                for (int px = Math.Max(0, x); px < Math.Min(W, x + w); px++)
                {
                    int i = (py * W + px) * 4;
                    d[i] = (byte)((d[i] * (255 - alfa) + k.R * alfa) / 255);
                    d[i + 1] = (byte)((d[i + 1] * (255 - alfa) + k.G * alfa) / 255);
                    d[i + 2] = (byte)((d[i + 2] * (255 - alfa) + k.B * alfa) / 255);
                }
            }
        }

        /// <summary>Napis w pikselach natywnych; wychodzi do Unity w 1280×720, z cieniem pod spodem.</summary>
        private void Tekst(string t, int x, int y, Barwa kolor)
        {
            if (string.IsNullOrEmpty(t))
            {
                return;
            }
            _napisy.Add(new Napis(t, x * K + 2, y * K + 2, new Barwa(Cien.R, Cien.G, Cien.B, kolor.A), Kroj));
            _napisy.Add(new Napis(t, x * K, y * K, kolor, Kroj));
        }

        private void TekstSrodek(string t, int cx, int y, Barwa kolor)
        {
            Tekst(t, cx - t.Length * Znak / 2, y, kolor);
        }

        /// <summary>Powiększenie ×2 wprost do płótna pulpitu — bez nowego płótna co klatkę.</summary>
        private static void Powieksz(Plotno z, Plotno cel)
        {
            byte[] zd = z.Dane, cd = cel.Dane;
            int cw = cel.Szerokosc;
            for (int y = 0; y < z.Wysokosc && y * K < cel.Wysokosc; y++)
            {
                int wiersz = y * K * cw * 4;
                for (int x = 0; x < z.Szerokosc && x * K < cw; x++)
                {
                    int zi = (y * z.Szerokosc + x) * 4;
                    int ci = wiersz + x * K * 4;
                    for (int r = 0; r < K; r++)
                    {
                        cd[ci + r * 4] = zd[zi];
                        cd[ci + r * 4 + 1] = zd[zi + 1];
                        cd[ci + r * 4 + 2] = zd[zi + 2];
                        cd[ci + r * 4 + 3] = 255;
                    }
                }
                Buffer.BlockCopy(cd, wiersz, cd, wiersz + cw * 4, cw * 4);
            }
        }
    }
}
