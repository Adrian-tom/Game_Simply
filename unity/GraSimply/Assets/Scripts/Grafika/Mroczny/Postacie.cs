using System;
using System.Collections.Generic;

namespace GraSimply.Grafika.Mroczny
{
    /// <summary>
    /// Postać bohatera — ta sama sylwetka, ale strój i oręż zależne od klasy.
    ///
    /// Model powstaje w układzie postaci: x w prawo na ekranie, y ku
    /// obserwatorowi, z w górę. Potem cała bryła jest obracana tak, żeby lokalne
    /// „ku obserwatorowi” trafiło w rzeczywisty kierunek patrzenia kamery —
    /// postać stoi przodem do gracza, jak na sprite'ach Fallouta w spoczynku.
    /// Oręż trzyma w ręce po lewej stronie ekranu, czyli po stronie światła.
    /// </summary>
    public static class Postacie
    {
        /// <summary>Wzrost postaci w jednostkach świata (bok pola = 1).</summary>
        private const double S = 0.98;

        private static Material Mat(Barwa[] rampa, double faktura = 0.10, double czest = 40, double jasnosc = 0) =>
            new Material(rampa, faktura, czest, jasnosc);

        public static Szkic Bohater(string klasa)
        {
            string k = (klasa ?? "").ToLowerInvariant();
            switch (k)
            {
                case "mag":
                    return Mag();
                case "lotrzyk":
                case "łotrzyk":
                    return Lotrzyk();
                case "druid":
                    return Druid();
                case "nekromanta":
                    return Nekromanta();
                default:
                    return Wojownik();
            }
        }

        // ---- wspólny korpus ---------------------------------------------- //

        /// <summary>Indeksy materiałów wspólne dla wszystkich klas.</summary>
        private const int MSkora = 0, MButy = 1, MSpodnie = 2, MTulow = 3, MPas = 4;

        private static List<Bryla> Korpus(bool nogiWidoczne = true)
        {
            var c = new List<Bryla>();
            if (nogiWidoczne)
            {
                c.Add(Bryla.Stozkowa(P(-0.022, 0, 0.215), P(-0.026, 0.004, 0.035), 0.021 * S, 0.016 * S, MSpodnie));
                c.Add(Bryla.Stozkowa(P(0.022, 0, 0.215), P(0.026, -0.004, 0.035), 0.021 * S, 0.016 * S, MSpodnie));
            }
            c.Add(Bryla.Elipsoida(-0.026 * S, 0.012 * S, 0.018 * S, 0.016 * S, 0.026 * S, 0.018 * S, MButy));
            c.Add(Bryla.Elipsoida(0.026 * S, 0.008 * S, 0.018 * S, 0.016 * S, 0.026 * S, 0.018 * S, MButy));
            c.Add(Bryla.Elipsoida(0, 0, 0.30 * S, 0.056 * S, 0.040 * S, 0.088 * S, MTulow));
            c.Add(Bryla.Prostopadloscian(0, 0, 0.225 * S, 0.054 * S, 0.040 * S, 0.008 * S, MPas, 0.004));
            c.Add(Bryla.Kula(0, 0.004 * S, 0.435 * S, 0.036 * S, MSkora));
            c.Add(Bryla.Kapsula(P(0, 0, 0.385), P(0, 0, 0.41), 0.016 * S, MSkora));
            return c;
        }

        /// <summary>Ręka od barku do dłoni; strona −1 = lewa na ekranie, +1 = prawa.</summary>
        private static Bryla Reka(int strona, W3 dlon, int material)
        {
            var bark = P(0.058 * strona, 0, 0.365);
            return Bryla.Zlep(0.01, Bryla.Stozkowa(bark, dlon, 0.017 * S, 0.014 * S, material),
                              Bryla.Kula(dlon.X, dlon.Y, dlon.Z, 0.014 * S, MSkora));
        }

        private static W3 P(double x, double y, double z) => new W3(x * S, y * S, z * S);

        /// <summary>Obrót postaci przodem do kamery i obrys sprite'a.</summary>
        private static Szkic Gotowa(List<Bryla> czesci, Material[] mat, double wysokosc = 0.62)
        {
            double kat = Math.Atan2(-Rzut.DoObserwatora.X, Rzut.DoObserwatora.Y);
            Bryla b = Bryla.Suma(czesci.ToArray()).Obroc(kat);
            double r = 0.17 * S;
            return new Szkic(b, mat, new W3(-r, -r, -0.01), new W3(r, r, wysokosc * S));
        }

        // ---- klasy ------------------------------------------------------- //

        /// <summary>Wojownik: hełm z nosalem, kolczuga, czerwony wapenrok, miecz i tarcza.</summary>
        private static Szkic Wojownik()
        {
            var mat = new[]
            {
                Mat(Materialy.Skora, 0.05), Mat(Materialy.SkoraWyprawiona), Mat(Materialy.SkoraWyprawiona, 0.08, 40, 0.1),
                Mat(Materialy.Zelazo, 0.28, 120, 0.05), Mat(Materialy.SkoraWyprawiona, 0.06, 40, -0.1),
                Mat(Materialy.Zelazo, 0.06, 30, 0.12), Mat(Materialy.SukmanaCzerwona, 0.08, 30),
                Mat(Materialy.Deski, 0.1, 30), Mat(Materialy.Mosiadz, 0.06),
            };
            var c = Korpus();
            // Wapenrok: płachta na przodzie i z tyłu, od pasa po kolana.
            c.Add(Bryla.Prostopadloscian(0, 0.032 * S, 0.24 * S, 0.042 * S, 0.008 * S, 0.11 * S, 6, 0.004));
            // Hełm: czasza z nosalem.
            c.Add(Bryla.Kula(0, 0.0, 0.448 * S, 0.040 * S, 5)
                       .Przetnij(Bryla.Prostopadloscian(0, 0, 0.47 * S, 0.06, 0.06, 0.028 * S, 5)));
            c.Add(Bryla.Prostopadloscian(0, 0.038 * S, 0.43 * S, 0.004 * S, 0.004 * S, 0.018 * S, 5));
            // Ręka z mieczem (lewa na ekranie) i ręka z tarczą.
            var dlon = P(-0.082, 0.03, 0.235);
            c.Add(Reka(-1, dlon, 3));
            c.Add(Bryla.Prostopadloscian(dlon.X, dlon.Y, dlon.Z + 0.12 * S, 0.005 * S, 0.0035 * S, 0.105 * S, 5));
            c.Add(Bryla.Prostopadloscian(dlon.X, dlon.Y, dlon.Z + 0.012 * S, 0.022 * S, 0.006 * S, 0.004 * S, 8));
            c.Add(Bryla.Kula(dlon.X, dlon.Y, dlon.Z - 0.016 * S, 0.008 * S, 8));
            c.Add(Reka(1, P(0.075, 0.03, 0.27), 3));
            Bryla tarcza = Bryla.Elipsoida(0.088 * S, 0.05 * S, 0.29 * S, 0.010 * S, 0.055 * S, 0.068 * S, 7);
            c.Add(tarcza);
            c.Add(Bryla.Kula(0.096 * S, 0.05 * S, 0.29 * S, 0.012 * S, 8));
            return Gotowa(c, mat);
        }

        /// <summary>Mag: szata do ziemi, spiczasty kapelusz z rondem, kostur z kulą światła.</summary>
        private static Szkic Mag()
        {
            var mat = new[]
            {
                Mat(Materialy.Skora, 0.05), Mat(Materialy.SkoraWyprawiona), Mat(Materialy.SukmanaModra),
                Mat(Materialy.SukmanaModra, 0.10), Mat(Materialy.Mosiadz, 0.06),
                Mat(Materialy.SukmanaModra, 0.12, 30, -0.06), Mat(Materialy.Deski, 0.12, 30),
                new Material(Materialy.Magia, 0.5, 30, 0.0, swieci: true), Mat(Materialy.Kosc, 0.06, 30, 0.1),
            };
            var c = Korpus(nogiWidoczne: false);
            c.Add(Bryla.Stozkowa(P(0, 0, 0.02), P(0, 0, 0.30), 0.075 * S, 0.045 * S, 2).Chropowata(0.006, 50, 3));
            // Kapelusz: rondo i spiczasta, lekko zgięta główka.
            c.Add(Bryla.Elipsoida(0, 0, 0.462 * S, 0.062 * S, 0.062 * S, 0.008 * S, 5));
            c.Add(Bryla.Stozkowa(P(0, 0, 0.465), P(0.015, -0.01, 0.60), 0.034 * S, 0.004 * S, 5));
            // Broda.
            c.Add(Bryla.Elipsoida(0, 0.03 * S, 0.405 * S, 0.022 * S, 0.016 * S, 0.034 * S, 8).Chropowata(0.004, 70, 5));
            var dlon = P(-0.08, 0.03, 0.27);
            c.Add(Reka(-1, dlon, 3));
            c.Add(Bryla.Kapsula(new W3(dlon.X, dlon.Y, 0.0), new W3(dlon.X, dlon.Y, 0.55 * S), 0.0065 * S, 6));
            c.Add(Bryla.Kula(dlon.X, dlon.Y, 0.57 * S, 0.020 * S, 7));
            c.Add(Reka(1, P(0.07, 0.025, 0.24), 3));
            return Gotowa(c, mat, 0.64);
        }

        /// <summary>Łotrzyk: kaptur, skórzany kubrak, płaszcz za plecami, sztylet.</summary>
        private static Szkic Lotrzyk()
        {
            var mat = new[]
            {
                Mat(Materialy.Skora, 0.05), Mat(Materialy.SkoraWyprawiona, 0.08, 40, -0.1), Mat(Materialy.SukmanaSzara, 0.08, 40, -0.2),
                Mat(Materialy.SkoraWyprawiona, 0.14, 50), Mat(Materialy.SkoraWyprawiona, 0.06, 40, -0.15),
                Mat(Materialy.SukmanaSzara, 0.10, 30, -0.12), Mat(Materialy.Zelazo, 0.06, 30, 0.18),
            };
            var c = Korpus();
            // Płaszcz: długa płachta za plecami i na ramionach.
            c.Add(Bryla.Elipsoida(0, -0.026 * S, 0.24 * S, 0.064 * S, 0.026 * S, 0.17 * S, 5).Chropowata(0.006, 40, 2));
            // Kaptur: nieco większa czasza nad głową, przesunięta w tył, z cieniem na twarzy.
            c.Add(Bryla.Kula(0, -0.006 * S, 0.445 * S, 0.044 * S, 5)
                       .Minus(Bryla.Kula(0, 0.04 * S, 0.43 * S, 0.034 * S, 5)));
            var dlon = P(-0.075, 0.04, 0.22);
            c.Add(Reka(-1, dlon, 3));
            c.Add(Bryla.Prostopadloscian(dlon.X, dlon.Y + 0.02 * S, dlon.Z + 0.03 * S, 0.004 * S, 0.004 * S,
                                         0.038 * S, 6).Obroc(0));
            c.Add(Reka(1, P(0.072, 0.02, 0.23), 3));
            return Gotowa(c, mat);
        }

        /// <summary>Druid: zielona szata, kaptur z porożem, sękaty kostur z kępą liści.</summary>
        private static Szkic Druid()
        {
            var mat = new[]
            {
                Mat(Materialy.Skora, 0.05), Mat(Materialy.SkoraWyprawiona), Mat(Materialy.SukmanaZielona),
                Mat(Materialy.SukmanaZielona, 0.12), Mat(Materialy.Kora, 0.06, 40, -0.05),
                Mat(Materialy.SukmanaZielona, 0.12, 30, -0.08), Mat(Materialy.Kora, 0.16, 50),
                Mat(Materialy.Listowie, 0.22, 40), Mat(Materialy.Kosc, 0.06, 30),
            };
            var c = Korpus(nogiWidoczne: false);
            c.Add(Bryla.Stozkowa(P(0, 0, 0.02), P(0, 0, 0.30), 0.072 * S, 0.046 * S, 2).Chropowata(0.007, 40, 4));
            c.Add(Bryla.Kula(0, -0.006 * S, 0.445 * S, 0.044 * S, 5)
                       .Minus(Bryla.Kula(0, 0.04 * S, 0.43 * S, 0.034 * S, 5)));
            // Poroże: dwa rozwidlone odrosty z kaptura.
            foreach (int s in new[] { -1, 1 })
            {
                var a = P(0.026 * s, -0.01, 0.47);
                var b = P(0.06 * s, -0.02, 0.56);
                c.Add(Bryla.Stozkowa(a, b, 0.006 * S, 0.003 * S, 8));
                c.Add(Bryla.Stozkowa(P(0.045 * s, -0.015, 0.52), P(0.05 * s, 0.02, 0.57), 0.004 * S, 0.002 * S, 8));
            }
            var dlon = P(-0.08, 0.03, 0.27);
            c.Add(Reka(-1, dlon, 3));
            c.Add(Bryla.Stozkowa(new W3(dlon.X, dlon.Y, 0.0), new W3(dlon.X + 0.01, dlon.Y, 0.54 * S),
                                 0.008 * S, 0.006 * S, 6).Chropowata(0.004, 60, 9));
            c.Add(Bryla.Kula(dlon.X + 0.01, dlon.Y, 0.56 * S, 0.026 * S, 7).Chropowata(0.008, 50, 10));
            c.Add(Reka(1, P(0.07, 0.025, 0.24), 3));
            return Gotowa(c, mat, 0.64);
        }

        /// <summary>Nekromanta: ciemna szata z kapturem, blada trupia twarz, kosa.</summary>
        private static Szkic Nekromanta()
        {
            var mat = new[]
            {
                Mat(Materialy.Kosc, 0.06, 40, 0.05), Mat(Materialy.SkoraWyprawiona), Mat(Materialy.SukmanaFioletowa),
                Mat(Materialy.SukmanaFioletowa, 0.12), Mat(Materialy.Kosc, 0.06, 40, -0.1),
                Mat(Materialy.SukmanaFioletowa, 0.12, 30, -0.1), Mat(Materialy.Kora, 0.10, 40, -0.15),
                Mat(Materialy.Zelazo, 0.08, 30, 0.1), new Material(Materialy.Magia, 0.4, 30, -0.2, swieci: true),
            };
            var c = Korpus(nogiWidoczne: false);
            c.Add(Bryla.Stozkowa(P(0, 0, 0.02), P(0, 0, 0.30), 0.078 * S, 0.046 * S, 2).Chropowata(0.007, 40, 5));
            c.Add(Bryla.Kula(0, -0.008 * S, 0.448 * S, 0.046 * S, 5)
                       .Minus(Bryla.Kula(0, 0.042 * S, 0.43 * S, 0.034 * S, 5)));
            // Oczy jarzące się w cieniu kaptura.
            c.Add(Bryla.Kula(-0.012 * S, 0.034 * S, 0.44 * S, 0.0045 * S, 8));
            c.Add(Bryla.Kula(0.012 * S, 0.034 * S, 0.44 * S, 0.0045 * S, 8));
            var dlon = P(-0.08, 0.03, 0.27);
            c.Add(Reka(-1, dlon, 3));
            c.Add(Bryla.Kapsula(new W3(dlon.X, dlon.Y, 0.0), new W3(dlon.X, dlon.Y, 0.58 * S), 0.006 * S, 6));
            // Ostrze kosy: łuk z trzech odcinków.
            var g = new W3(dlon.X, dlon.Y, 0.57 * S);
            var k1 = g + new W3(0.04 * S, 0.0, 0.01 * S);
            var k2 = k1 + new W3(0.04 * S, 0.0, -0.02 * S);
            var k3 = k2 + new W3(0.025 * S, 0.0, -0.035 * S);
            c.Add(Bryla.Stozkowa(g, k1, 0.007 * S, 0.006 * S, 7));
            c.Add(Bryla.Stozkowa(k1, k2, 0.006 * S, 0.004 * S, 7));
            c.Add(Bryla.Stozkowa(k2, k3, 0.004 * S, 0.0015 * S, 7));
            c.Add(Reka(1, P(0.07, 0.025, 0.24), 3));
            return Gotowa(c, mat, 0.64);
        }
    }
}
