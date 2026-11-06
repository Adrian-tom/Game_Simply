using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Jeden cel dla gracza. Numer 0 oznacza pilną sprawę poza kolejką (głód, sen, plecak).</summary>
    public sealed class CelWolny
    {
        public string Id;
        public int Numer;
        public string Tekst;

        public string Etykieta => Numer > 0 ? $"Cel {Numer}/{CeleWolne.Liczba}: {Tekst}" : Tekst;
    }

    /// <summary>
    /// Podpowiedzi celu: co gracz powinien zrobić teraz, od pierwszego drewna po tartak. Cel wynika
    /// wyłącznie ze stanu świata, więc nie trzeba go zapisywać: stary zapis od razu pokaże właściwy
    /// etap, a gdy szkielety zniszczą namiot, gra sama wróci do celu „postaw namiot”.
    /// </summary>
    public static class CeleWolne
    {
        public const int Liczba = 20;
        public const int NaOgnisko = 5, KamieniNaOgnisko = 3, NaSzkielet = 30, NaPlotno = 20, KafliPalisady = 6;

        /// <summary>Bieżący cel albo null, gdy gracz przeszedł już wszystkie (albo gra nie ma tego trybu).</summary>
        public static CelWolny Aktualny(SwiatWolny s)
        {
            if (!s.WalkaWlaczona || !s.BudowaZEtapami || s.Martwy)
            {
                return null;
            }
            return Pilny(s) ?? Kolejny(s);
        }

        private static CelWolny Pilny(SwiatWolny s)
        {
            // Bez obozu ważniejsza jest odbudowa: po plecak na drugi koniec mapy idzie się, gdy jest dokąd wrócić.
            if (s.Porzucone.Count > 0 && s.SrodekObozu().HasValue && !s.CzyNoc)
            {
                return Cel("plecak", 0, "Odzyskaj plecak: leży tam, gdzie padłeś (E)");
            }
            if (s.Glod <= 25.0)
            {
                return s.Razem("jagody") > 0 || (s.PrzyObozie() && s.Obozowisko.Surowce.Wez("zywnosc") > 0)
                    ? Cel("jedz", 0, "Jesteś głodny: F zjedz")
                    : Cel("jagody", 0, "Jesteś głodny: zerwij jagody z krzaka (E)");
            }
            if (s.Energia <= 20.0)
            {
                if (s.MiejsceSnu() != null)
                {
                    return Cel("spij", 0, "Jesteś zmęczony: Z połóż się spać");
                }
                bool maGdzie = s.Budowle.ContainsValue("namiot") || s.Budowle.ContainsValue("spiwor");
                return Cel("spij", 0, maGdzie ? "Jesteś zmęczony: wróć do posłania i naciśnij Z"
                                              : "Jesteś zmęczony: rozłóż śpiwór (B) i naciśnij Z");
            }
            return null;
        }

        private static CelWolny Kolejny(SwiatWolny s)
        {
            if (!s.Budowle.ContainsValue("ognisko"))
            {
                return s.Razem("drewno") < NaOgnisko || s.Razem("kamien") < KamieniNaOgnisko
                    ? Cel("zbierz-ognisko", 1, $"zbierz drewno ({s.Razem("drewno")}/{NaOgnisko}) i kamień ({s.Razem("kamien")}/{KamieniNaOgnisko}), E")
                    : Cel("ognisko", 2, "postaw ognisko: B, kafelek Ognisko, klik");
            }

            if (!s.Budowle.ContainsValue("namiot"))
            {
                PlacBudowy plac = s.Place.Values.FirstOrDefault(p => p.Klucz == "namiot");
                if (plac == null)
                {
                    return s.Razem("drewno") < NaSzkielet
                        ? Cel("zbierz-namiot", 3, $"zbierz drewno na namiot ({s.Razem("drewno")}/{NaSzkielet})")
                        : Cel("plac-namiot", 4, "postaw namiot: B, kafelek Namiot, klik");
                }
                if (plac.Etap == 0)
                {
                    return !plac.Zaplacony && s.Razem("drewno") < NaSzkielet
                        ? Cel("zbierz-namiot", 3, $"zbierz drewno na namiot ({s.Razem("drewno")}/{NaSzkielet})")
                        : Cel("szkielet", 5, "trzymaj E przy placu: szkielet namiotu");
                }
                return !plac.Zaplacony && s.Razem("wlokno") < NaPlotno
                    ? Cel("wlokno", 6, $"zbierz włókno z krzaków ({s.Razem("wlokno")}/{NaPlotno})")
                    : Cel("plotno", 7, "trzymaj E przy placu: płótno namiotu");
            }

            bool nocMinela = s.Dzien > 2 || (s.Dzien == 2 && s.Godzina >= SwiatWolny.NocDo);
            if (!nocMinela)
            {
                return s.CzyNoc
                    ? Cel("noc", 8, "przetrwaj noc: LPM cios, PPM blok")
                    : Cel("noc", 8, "o 21:00 szkielety: zbieraj i wróć do obozu");
            }

            // Przybysza nie da się przywołać, więc czekanie na niego nie blokuje kolejnych celów.
            if (s.Gosc != null)
            {
                return Cel("przybysz", 9, "G: przybysz czeka na decyzję");
            }
            if (s.Obozowisko.Osadnicy.Any(o => o.Zajecie == "bezczynny"))
            {
                return Cel("zajecie", 9, "daj osadnikowi pracę: Tab, kafelek zajęcia");
            }
            int palisada = s.Budowle.Values.Count(b => b == "palisada");
            if (palisada < KafliPalisady)
            {
                return Cel("palisada", 10, $"postaw palisadę ({palisada}/{KafliPalisady}): B, zakładka Obrona");
            }
            if (!s.Budowle.ContainsValue("tartak"))
            {
                return Cel("tartak", 11, "zbuduj tartak: B, zakładka Zakłady");
            }
            if (!s.Budowle.ContainsValue("farma"))
            {
                return Cel("farma", 12, "zbuduj farmę: B, zakładka Zakłady");
            }
            if (s.Obozowisko.Osadnicy.Count > 0 && s.Obozowisko.Osadnicy.All(o => o.Zajecie != "rolnik"))
            {
                return Cel("rolnik", 13, "daj komuś pracę rolnika: Tab, kafelek Rolnik");
            }
            return DoSzczebla(s, 1, 14)
                ?? Budynek(s, "spichlerz", 15) ?? Budynek(s, "tawerna", 16)
                ?? DoSzczebla(s, 2, 17)
                ?? Budynek(s, "lecznica", 18) ?? Budynek(s, "targ", 19)
                ?? DoSzczebla(s, 3, 20);
        }

        private static CelWolny Budynek(SwiatWolny s, string klucz, int numer)
        {
            if (s.Budowle.ContainsValue(klucz))
            {
                return null;
            }
            string nazwa = klucz == "tawerna" ? "tawernę" : SwiatWolny.NazwaBudowli(klucz);
            return Cel(klucz, numer, $"zbuduj {nazwa}: B, zakładka Zakłady");
        }

        /// <summary>Droga na szczebel: najpierw miejsca do spania, bo bez nich przybysze idą dalej.</summary>
        private static CelWolny DoSzczebla(SwiatWolny s, int szczebel, int numer)
        {
            if (s.Szczebel >= szczebel)
            {
                return null;
            }
            if (Osada.WolneChaty(s.Obozowisko) <= 0 || !s.Budowle.ContainsValue("chata"))
            {
                return Cel("chata", numer, $"zbuduj chatę: ludzie {s.Obozowisko.Osadnicy.Count}/{s.Obozowisko.Chaty} miejsc");
            }
            OpisSzczebla n = SwiatWolny.Szczeble[szczebel];
            return Cel("szczebel", numer, $"{n.Nazwa.ToLowerInvariant()}: {string.Join(", ", s.BrakiSzczebla())}");
        }

        private static CelWolny Cel(string id, int numer, string tekst)
        {
            return new CelWolny { Id = id, Numer = numer, Tekst = tekst };
        }
    }
}
