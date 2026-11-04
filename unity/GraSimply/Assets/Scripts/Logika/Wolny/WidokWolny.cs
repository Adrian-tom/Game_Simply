using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Tekst i liczby interfejsu trybu „Zomboid fantasy” — wszystko, co warstwa rysująca bierze ze stanu świata.</summary>
    public static class WidokWolny
    {
        public const string Klawisze =
            "WASD ruch · E zbieraj";

        /// <summary>Pasek stanu: życie, energia (jako „mana”), głód, pora doby i to, co niesie bohater.</summary>
        public static StanHud Hud(SwiatWolny s)
        {
            var hud = new StanHud
            {
                Imie = "Wędrowiec",
                Klasa = "Przetrwanie",
                Poziom = s.Dzien,
                Zloto = 0,
                Hp = (int)System.Math.Round(s.Hp),
                MaxHp = 100,
                Mana = (int)System.Math.Round(s.Energia),
                MaxMana = 100,
                Data = s.Czas,
                Region = s.ObozZalozony ? $"osada: {s.Obozowisko.Osadnicy.Count} os." : "bez obozu",
                Biom = s.Swiat.Biom((int)System.Math.Floor(s.X), (int)System.Math.Floor(s.Y)),
                Zywnosc = $"🍖 głód {(int)s.Glod}%",
                Zagrozenie = s.Spi ? "śpisz" : s.Jasnosc < 0.3 ? "noc" : "dzień",
                Osada = $"Energia {(int)s.Energia}%  ·  jagody {s.Ile("jagody")}",
                Surowce = Liczby.Format(s.X, 0) + ", " + Liczby.Format(s.Y, 0),
            };
            foreach (string klucz in new[] { "zywnosc", "drewno", "kamien", "ziola" })
            {
                int ile = klucz == "zywnosc" ? s.Razem("jagody") : s.Razem(klucz);
                hud.ListaSurowcow.Add(new KeyValuePair<string, int>(klucz, ile));
            }
            return hud;
        }

        /// <summary>Plecak w lewym górnym rogu: tylko to, co bohater ma, z włóknem, którego nie ma w pasku surowców.</summary>
        public static List<string> Plecak(SwiatWolny s)
        {
            var linie = new List<string> { "PLECAK" };
            foreach (KeyValuePair<string, int> p in s.Plecak.Where(p => p.Value > 0))
            {
                linie.Add($"{SwiatWolny.Nazwa(p.Key)}: {p.Value}");
            }
            if (linie.Count == 1)
            {
                linie.Add("pusty");
            }
            if (s.ObozZalozony)
            {
                linie.Add($"MAGAZYN (ludzie {s.Obozowisko.Osadnicy.Count}/{s.Obozowisko.Chaty})");
                foreach (KeyValuePair<string, OpisSurowca> r in Oboz.Surowce)
                {
                    int ile = s.Obozowisko.Surowce.Wez(r.Key);
                    if (ile > 0)
                    {
                        linie.Add($"{r.Value.Nazwa}: {ile}");
                    }
                }
            }
            return linie;
        }

        /// <summary>Panel osady (Tab): każdy osadnik z zajęciem, umiejętnością w tym zawodzie, morale i chorobą.</summary>
        public static List<string> Panel(SwiatWolny s)
        {
            if (!s.PanelOtwarty)
            {
                return null;
            }
            List<Osadnik> ludzie = s.Obozowisko.Osadnicy;
            var linie = new List<string> { $"OSADA — ludzie {ludzie.Count}/{s.Obozowisko.Chaty}" };
            if (ludzie.Count == 0)
            {
                linie.Add(s.ObozZalozony ? "Jeszcze nikt nie przyszedł. Pierwszy przybysz zjawi się o północy."
                                         : "Najpierw postaw namiot i ognisko.");
            }
            for (int i = 0; i < ludzie.Count; i++)
            {
                Osadnik o = ludzie[i];
                string u = Kompetencje.ZUmiejetnosciZajecia(o.Zajecie);
                string poziom = u == null ? "" : $" {Kompetencje.Poziom(o, u)}{new string('*', Kompetencje.Pasja(o, u))}";
                string znak = i == s.Wybrany ? ">" : " ";
                linie.Add($"{znak} {Liczby.Dopelnij(o.Imie, 8)} {Liczby.Dopelnij(Osada.Zajecie(o.Zajecie).Nazwa + poziom, 18)}"
                          + $" morale {Liczby.Format(o.Morale, 0)}{(o.Chory > 0 ? "  chory" : "")}");
            }
            if (ludzie.Count > 0)
            {
                linie.Add($"Najlepszy w: {Kompetencje.OpisKrotki(ludzie[s.Wybrany], 3).Replace("🔥", "*")}   (* = pasja)");
            }
            return linie;
        }

        public static List<string> Dziennik(SwiatWolny s, int ile = 4)
        {
            return s.Dziennik.Skip(System.Math.Max(0, s.Dziennik.Count - ile)).ToList();
        }

        /// <summary>Podpowiedź na dole: co zrobi E albo co stoi na przeszkodzie; w nocy ostrzeżenie.</summary>
        public static string Podpowiedz(SwiatWolny s)
        {
            if (s.Martwy)
            {
                return "Koniec drogi — Esc wraca do menu";
            }
            if (s.Spi)
            {
                return "Śpisz… dowolny ruch cię obudzi";
            }
            if (s.PanelOtwarty)
            {
                return "Kliknij osadnika, potem zajęcie · Tab zamknij";
            }
            if (s.TrybBudowy)
            {
                if (s.WybranaBudowla == null)
                {
                    return "Wybierz budowlę z kafelków · B wyjdź";
                }
                (int x, int y) = s.KafelBudowy();
                return s.PowodBudowy(s.WybranaBudowla, x, y) ?? "Lewy klik stawia · prawy klik wycofuje";
            }
            (int X, int Y)? cel = s.ZnajdzCel();
            if (cel.HasValue && SwiatWolny.Zbiory.TryGetValue(s.ObiektNa(cel.Value.X, cel.Value.Y), out OpisZbioru o))
            {
                return $"E: {o.Czynnosc}";
            }
            if (s.PrzyObozie() && (s.Ile("drewno") + s.Ile("kamien") + s.Ile("ziola") + s.Ile("jagody")) > 0)
            {
                return "T: odłóż plecak do magazynu";
            }
            return Klawisze;
        }
    }
}
