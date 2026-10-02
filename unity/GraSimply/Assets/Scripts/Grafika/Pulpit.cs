using System;
using System.Collections.Generic;
using GraSimply.Logika;

namespace GraSimply.Grafika
{
    /// <summary>Napis do narysowania przez warstwę Unity: treść, pozycja, barwa i krój.</summary>
    public struct Napis
    {
        public string Tekst;
        public int X;
        public int Y;
        public Barwa Kolor;

        /// <summary>„hud”, „duzy” albo „maly” — warstwa Unity mapuje to na rozmiar czcionki.</summary>
        public string Krój;

        public Napis(string tekst, int x, int y, Barwa kolor, string krój = "hud")
        {
            Tekst = tekst;
            X = x;
            Y = y;
            Kolor = kolor;
            Krój = krój;
        }
    }

    /// <summary>
    /// Pulpit gry: cała klatka 1280×720 złożona w pikselach — scena mapy
    /// powiększona ×2, pixelartowe ramki i paski HUD.
    ///
    /// To port układu z <c>grafika/okno.py</c>. Siedzi w warstwie Grafika,
    /// a nie w Unity, z dwóch powodów: po pierwsze cały rysunek da się wtedy
    /// przetestować bez edytora, po drugie Unity dostaje jedną teksturę
    /// i listę napisów, więc warstwa silnika zostaje cienka.
    ///
    /// Tekst rysuje Unity (czcionki systemowe), bo własny bitmapowy krój nie
    /// udźwignąłby emoji z komunikatów gry.
    ///
    ///     ┌──────────── 640 ───────────┬──────────── 632 ────────────┐
    ///     │  mapa 320×240 w skali ×2   │                             │
    ///     │                        480 │           konsola           │
    ///     ├────────────────────────────┤                             │
    ///     │  HUD postaci           240 │                             │
    ///     └────────────────────────────┴─────────────────────────────┘
    /// </summary>
    public sealed class Pulpit
    {
        public const int Szerokosc = 1280;
        public const int Wysokosc = 720;

        /// <summary>Scena mapy jest rysowana w 320×240 i powiększana całkowitą krotnością.</summary>
        public const int SkalaMapy = 2;

        public static readonly Barwa Tlo = new Barwa(14, 12, 20);
        public static readonly Barwa RamkaJasna = new Barwa(150, 124, 86);
        public static readonly Barwa RamkaCiemna = new Barwa(70, 56, 44);
        public static readonly Barwa Zloty = new Barwa(232, 200, 130);
        public static readonly Barwa Tekst = new Barwa(224, 216, 200);
        public static readonly Barwa Przygaszony = new Barwa(150, 142, 132);

        private static readonly int MapaSzer = ScenaMapy.Szer * SkalaMapy;
        private static readonly int MapaWys = ScenaMapy.Wys * SkalaMapy;

        /// <summary>Prostokąt konsoli — warstwa Unity ustawia w nim wiersze tekstu.</summary>
        public static readonly (int X, int Y, int Szer, int Wys) ProstokatKonsoli =
            (648, 8, Szerokosc - 648 - 8, Wysokosc - 16);

        private static readonly (int X, int Y, int Szer, int Wys) ProstokatHud =
            (8, MapaWys + 8, MapaSzer - 16, Wysokosc - MapaWys - 16);

        private readonly Plotno _plotno = new Plotno(Szerokosc, Wysokosc, przezroczyste: false);
        private readonly Plotno _scena = new Plotno(ScenaMapy.Szer, ScenaMapy.Wys, przezroczyste: false);

        /// <summary>Gotowa klatka w układzie RGBA32 — wprost do <c>Texture2D</c>.</summary>
        public Plotno Klatka => _plotno;

        public ScenaMapy Scena { get; } = new ScenaMapy();

        /// <summary>
        /// Składa klatkę. Zwraca napisy do narysowania przez Unity w pikselach
        /// płótna (lewy górny róg to (0, 0)).
        /// </summary>
        public List<Napis> Zloz(PoleDoRysunku[][] pola, double t, StanHud hud,
                                (int X, int Y) region = default,
                                (int X, int Y)? graczXy = null, string klasa = null,
                                bool wszystkoOdkryte = false,
                                (int Chaty, int Palisada, int Wieza)? osada = null)
        {
            _plotno.Wypelnij(Tlo);
            Scena.Rysuj(_scena, pola, t, region, graczXy, klasa, wszystkoOdkryte, osada);

            Plotno powiekszona = _scena.Skaluj(SkalaMapy);
            _plotno.Nalóż(powiekszona, 0, 0);
            _plotno.Obramowanie(RamkaJasna, 0, 0, MapaSzer, MapaWys, 2);

            Ramka(ProstokatHud);
            Ramka(ProstokatKonsoli);

            var napisy = new List<Napis>();
            if (hud == null)
            {
                NapisyTytulowe(napisy);
            }
            else
            {
                NapisyHud(napisy, hud);
            }
            return napisy;
        }

        /// <summary>Pixelartowa ramka: dwie linie i ćwieki w narożnikach.</summary>
        private void Ramka((int X, int Y, int Szer, int Wys) r, Barwa? tlo = null)
        {
            _plotno.Wypelnij(tlo ?? new Barwa(20, 17, 28), r.X, r.Y, r.Szer, r.Wys);
            _plotno.Obramowanie(RamkaJasna, r.X, r.Y, r.Szer, r.Wys, 2);
            _plotno.Obramowanie(RamkaCiemna, r.X + 4, r.Y + 4, r.Szer - 8, r.Wys - 8, 2);
            var ćwieki = new[]
            {
                (r.X, r.Y), (r.X + r.Szer - 6, r.Y),
                (r.X, r.Y + r.Wys - 6), (r.X + r.Szer - 6, r.Y + r.Wys - 6),
            };
            foreach ((int x, int y) in ćwieki)
            {
                _plotno.Wypelnij(new Barwa(226, 196, 120), x, y, 6, 6);
                _plotno.Wypelnij(Tlo, x + 2, y + 2, 2, 2);
            }
        }

        /// <summary>Pasek z podziałką, połyskiem u góry i cieniem u dołu.</summary>
        private void Pasek(int x, int y, int szer, double ile, Barwa kolor)
        {
            ile = Math.Max(0.0, Math.Min(1.0, ile));
            _plotno.Wypelnij(new Barwa(40, 30, 36), x, y, szer, 14);
            int wypelnione = (int)((szer - 4) * ile);
            _plotno.Wypelnij(kolor, x + 2, y + 2, wypelnione, 10);
            _plotno.Wypelnij(new Barwa(Math.Min(255, kolor.R + 60), Math.Min(255, kolor.G + 60),
                                       Math.Min(255, kolor.B + 60)), x + 2, y + 2, wypelnione, 2);
            _plotno.Wypelnij(new Barwa((int)(kolor.R * 0.6), (int)(kolor.G * 0.6), (int)(kolor.B * 0.6)),
                             x + 2, y + 10, wypelnione, 2);
            for (int i = x + 2; i < x + szer - 2; i += 8)
            {
                // podziałka co 8 px
                _plotno.Wypelnij(new Barwa(24, 18, 22), i, y + 2, 1, 10);
            }
        }

        private static void NapisyTytulowe(List<Napis> napisy)
        {
            int x = ProstokatHud.X + 24;
            int y = ProstokatHud.Y;
            napisy.Add(new Napis("GRA SIMPLY", x, y + 22, Zloty, "duzy"));
            napisy.Add(new Napis("Fantasy RPG po polsku — obóz, trwały świat, testy k20.",
                                 x, y + 62, Tekst));
            napisy.Add(new Napis("Wybierz opcję w konsoli: kliknij ją albo wpisz numer i Enter.",
                                 x, y + 96, Przygaszony));
            napisy.Add(new Napis("F2: dzień / zmierzch    F11: pełny ekran", x, y + 120, Przygaszony));
        }

        private void NapisyHud(List<Napis> napisy, StanHud h)
        {
            int x = ProstokatHud.X + 24;
            int y = ProstokatHud.Y + 20;

            napisy.Add(new Napis(h.Imie, x, y - 4, Zloty, "duzy"));
            // Szerokość imienia zna tylko Unity (zależy od kroju), więc klasa
            // idzie w drugiej kolumnie o stałym odstępie — bez mierzenia tekstu.
            napisy.Add(new Napis($"{h.Klasa}  ·  poz. {h.Poziom}", x + 210, y + 2, Przygaszony));
            napisy.Add(new Napis($"💰 {h.Zloto} zł", ProstokatHud.X + ProstokatHud.Szer - 150,
                                 y + 2, Zloty));

            y += 38;
            napisy.Add(new Napis("HP", x, y - 2, new Barwa(230, 190, 190)));
            Pasek(x + 56, y, 300, h.Hp / (double)Math.Max(1, h.MaxHp), new Barwa(196, 52, 52));
            napisy.Add(new Napis($"{h.Hp}/{h.MaxHp}", x + 368, y - 2, Tekst));
            if (h.MaxMana > 0)
            {
                y += 24;
                napisy.Add(new Napis("MANA", x, y - 2, new Barwa(190, 200, 240)));
                Pasek(x + 56, y, 300, h.Mana / (double)Math.Max(1, h.MaxMana), new Barwa(70, 110, 220));
                napisy.Add(new Napis($"{h.Mana}/{h.MaxMana}", x + 368, y - 2, Tekst));
            }

            // Cztery dolne linie idą mniejszym krojem. Mieszczą dużo tekstu,
            // a panel ma stałą szerokość — w większym rozmiarze data z regionem
            // i biomem wychodziły poza ramkę przy szerszych czcionkach.
            y += 28;
            napisy.Add(new Napis($"{h.Data}  ·  {h.Region}  ·  {h.Biom}", x, y, Tekst, "maly"));
            y += 20;
            bool najazd = (h.Zagrozenie ?? "").IndexOf("najazd", StringComparison.OrdinalIgnoreCase) >= 0;
            napisy.Add(new Napis($"{h.Zywnosc}  ·  ⚔ {h.Zagrozenie}", x, y,
                                 najazd ? new Barwa(236, 120, 100) : Przygaszony, "maly"));
            y += 20;
            napisy.Add(new Napis(h.Osada, x, y, Przygaszony, "maly"));
            y += 20;
            napisy.Add(new Napis(h.Surowce, x, y, Przygaszony, "maly"));
            napisy.Add(new Napis(h.Podpowiedz, x, ProstokatHud.Y + ProstokatHud.Wys - 28,
                                 new Barwa(120, 112, 104), "maly"));
        }
    }
}
