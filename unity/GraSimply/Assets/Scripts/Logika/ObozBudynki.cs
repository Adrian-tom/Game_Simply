using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>Opis budynku: nazwa, poziomy, utrzymanie i koszt pierwszego poziomu.</summary>
    public sealed class OpisBudynku
    {
        public string Nazwa;
        public string Ikona;
        public int Max;

        /// <summary>Złoto dziennie za poziom.</summary>
        public int Utrzymanie;
        public string Opis;

        /// <summary>Cena poziomu 1 w kolejności z Pythona (od niej zależy tekst kosztu).</summary>
        public List<KeyValuePair<string, int>> Koszt;

        /// <summary>
        /// Od którego poziomu do ceny dochodzi żelazo (domyślnie od 2.). Pola i palisada
        /// dostają je dopiero od 3., żeby epoka I nie wymagała metalu, którego jeszcze nie ma skąd wziąć.
        /// </summary>
        public int ZelazoOd = 2;
    }

    /// <summary>
    /// Budynki osady: tabela, koszty i poziomy — odpowiednik tej części <c>game/oboz.py</c>.
    /// Poziom n kosztuje koszt × n, a od 2. poziomu dochodzą deski i żelazo.
    /// Czas budowy i bramki badań leżą w <see cref="Budowa"/> i <see cref="Badania"/>.
    /// </summary>
    public static partial class Oboz
    {
        private static KeyValuePair<string, int> K(string klucz, int ile)
        {
            return new KeyValuePair<string, int>(klucz, ile);
        }

        private static KeyValuePair<string, OpisBudynku> B(string klucz, string nazwa, string ikona, int max,
                                                           int utrzymanie, string opis, int zelazoOd,
                                                           params KeyValuePair<string, int>[] koszt)
        {
            return new KeyValuePair<string, OpisBudynku>(klucz, new OpisBudynku
            {
                Nazwa = nazwa, Ikona = ikona, Max = max, Utrzymanie = utrzymanie, Opis = opis,
                ZelazoOd = zelazoOd, Koszt = new List<KeyValuePair<string, int>>(koszt),
            });
        }

        /// <summary>Kolejność jak w Pythonie — od niej zależy numeracja w menu rozbudowy.</summary>
        public static readonly List<KeyValuePair<string, OpisBudynku>> Budynki =
            new List<KeyValuePair<string, OpisBudynku>>
            {
                B("sklep", "Sklep", "🏪", 2, 1, "Kupiec w obozie — mikstury i podstawowy ekwipunek.", 2,
                  K("drewno", 6), K("kamien", 3), K("zloto", 15)),
                B("dom", "Dom", "🏠", 3, 0, "Twój dom: lepszy odpoczynek (+25 HP za poziom) i miejsca dla drużyny.", 2,
                  K("drewno", 10), K("kamien", 5), K("ziola", 4), K("zloto", 25)),
                B("farma", "Pola uprawne", "🌾", 3, 0, "Rolnicy (2 na poziom) dają żywność — dużo jesienią, nic zimą.", 3,
                  K("drewno", 8), K("kamien", 2), K("zloto", 15)),
                B("spichlerz", "Spichlerz", "🏚", 3, 0, "Żywność psuje się wolniej: 3% → 1% / 0,5% / 0% dziennie.", 2,
                  K("drewno", 10), K("kamien", 6), K("zloto", 20)),
                B("tartak", "Tartak", "🪚", 3, 1, "Tracze (2 na poziom) robią deski z drewna: 2 drewna → 1 deska.", 2,
                  K("drewno", 12), K("kamien", 4), K("ruda", 2), K("zloto", 25)),
                B("huta", "Huta", "🔥", 3, 2, "Hutnicy (2 na poziom) wytapiają żelazo: 2 rudy + 1 drewno → 1 żelazo.", 2,
                  K("kamien", 12), K("drewno", 6), K("ruda", 4), K("zloto", 40)),
                B("kuznia", "Kuźnia", "🔨", 3, 1, "Kowal: broń, zbroje, narzędzia i ulepszenia sprzętu z żelaza.", 2,
                  K("drewno", 6), K("kamien", 8), K("ruda", 5), K("zloto", 40)),
                B("warsztat", "Warsztat", "🔧", 2, 1, "Mikstury, bandaże, odzienie, olej. Rzemieślnicy pracują na zamówienia.", 2,
                  K("drewno", 6), K("kamien", 6), K("ruda", 3), K("ziola", 4), K("zloto", 30)),
                B("laboratorium", "Laboratorium", "⚗", 2, 2, "Alchemia: eliksiry, maści, bomby. Eksperymenty odkrywają przepisy.", 2,
                  K("kamien", 8), K("deski", 4), K("ruda", 3), K("ziola", 6), K("zloto", 50)),
                B("lecznica", "Lecznica", "⚕", 2, 1, "Rany goją się szybciej, osadnicy rzadziej chorują. Miejsce dla uzdrowiciela.", 2,
                  K("drewno", 8), K("kamien", 4), K("ziola", 6), K("zloto", 30)),
                B("targ", "Targ", "🛒", 3, 1, "Stoiska: 2 zł dziennie za poziom + zarobek handlarzy.", 2,
                  K("drewno", 8), K("kamien", 4), K("skora", 3), K("zloto", 35)),
                B("karawanseraj", "Karawanseraj", "🐪", 3, 1, "Karawany do innych osad — jedna na poziom.", 2,
                  K("drewno", 10), K("deski", 6), K("skora", 6), K("zloto", 60)),
                B("tawerna", "Tawerna", "🍺", 2, 1, "Wieczory przy kuflu: morale osadników +6 za poziom.", 2,
                  K("drewno", 10), K("deski", 4), K("zloto", 35)),
                B("palisada", "Palisada", "🪵", 3, 0, "Obrona osady +18 za poziom.", 3,
                  K("drewno", 15), K("zloto", 10)),
                B("wieza", "Wieża strażnicza", "🗼", 2, 1, "Obrona +10 za poziom. Najazd widać wcześniej i dokładniej.", 2,
                  K("kamien", 10), K("deski", 4), K("zloto", 30)),
                B("stajnie", "Stajnie", "🐴", 2, 1, "Szybka podróż i +4 racje prowiantu na poziom.", 2,
                  K("drewno", 8), K("kamien", 4), K("skora", 5), K("zloto", 30)),
                B("skryptorium", "Skryptorium", "📜", 3, 1,
                  "Badacze (1 na poziom) poznają nowe budowle, poziomy i epoki — jak stół badawczy.", 3,
                  K("drewno", 8), K("kamien", 4), K("zloto", 20)),
            };

        public static OpisBudynku OpisBudynkuKlucza(string klucz)
        {
            foreach (KeyValuePair<string, OpisBudynku> para in Budynki)
            {
                if (para.Key == klucz)
                {
                    return para.Value;
                }
            }
            return null;
        }

        public static bool MaBudynek(Gracz gracz, string klucz)
        {
            return gracz.PoziomBudynku(klucz) > 0;
        }

        /// <summary>Złoto dziennie za utrzymanie wszystkich budynków.</summary>
        public static int UtrzymanieDzienne(Gracz gracz)
        {
            int suma = 0;
            foreach (KeyValuePair<string, OpisBudynku> para in Budynki)
            {
                suma += para.Value.Utrzymanie * gracz.PoziomBudynku(para.Key);
            }
            return suma;
        }

        /// <summary>Koszt jako tekst: „🌲10 drewno, 🪨5 kamień, 25 zł”.</summary>
        public static string FormatKosztu(IEnumerable<KeyValuePair<string, int>> koszt)
        {
            var czesci = new List<string>();
            foreach (KeyValuePair<string, int> para in koszt)
            {
                if (para.Key == "zloto")
                {
                    czesci.Add($"{para.Value} zł");
                }
                else
                {
                    OpisSurowca info = Opis(para.Key);
                    czesci.Add($"{info.Ikona}{para.Value} {info.Nazwa}");
                }
            }
            return string.Join(", ", czesci);
        }

        public static bool MozeZaplacic(Gracz gracz, Dictionary<string, int> koszt)
        {
            if (gracz.Zloto < koszt.Wez("zloto"))
            {
                return false;
            }
            foreach (KeyValuePair<string, int> para in koszt)
            {
                if (para.Key != "zloto" && gracz.Surowce.Wez(para.Key) < para.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public static void PobierzKoszt(Gracz gracz, Dictionary<string, int> koszt)
        {
            gracz.Zloto -= koszt.Wez("zloto");
            foreach (KeyValuePair<string, int> para in koszt)
            {
                if (para.Key != "zloto")
                {
                    gracz.Surowce[para.Key] = gracz.Surowce.Wez(para.Key) - para.Value;
                }
            }
        }

        /// <summary>Koszt następnego poziomu albo null, gdy budynek jest na maksimum.</summary>
        public static Dictionary<string, int> KosztBudowy(Gracz gracz, string klucz)
        {
            OpisBudynku info = OpisBudynkuKlucza(klucz);
            int nastepny = gracz.PoziomBudynku(klucz) + 1;
            if (nastepny > info.Max)
            {
                return null;
            }
            var koszt = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> para in info.Koszt)
            {
                koszt[para.Key] = para.Value * nastepny;
            }
            if (nastepny >= 2)
            {
                koszt["deski"] = koszt.Wez("deski") + 4 * nastepny;
                if (nastepny >= info.ZelazoOd)
                {
                    koszt["zelazo"] = koszt.Wez("zelazo") + 2 * (nastepny - 1);
                }
            }
            if (gracz.MaTalent("architekt"))
            {
                var tansze = new Dictionary<string, int>();
                foreach (KeyValuePair<string, int> para in koszt)
                {
                    tansze[para.Key] = Math.Max(1, (int)(para.Value * 0.8));
                }
                koszt = tansze;
            }
            return koszt;
        }

        /// <summary>Zapisuje poziom budynku (bez opłat) — wspólny koniec natychmiastowej i rozłożonej w czasie budowy.</summary>
        public static void UstawPoziom(Gracz gracz, string klucz, int poziom)
        {
            gracz.Budynki.Add(klucz);
            gracz.PoziomyBudynkow[klucz] = poziom;
            gracz.Statystyki["zbudowane_budynki"] = gracz.Budynki.Count;
        }

        /// <summary>
        /// Natychmiast wznosi budynek albo podnosi poziom — niski poziom API. Gra używa
        /// <see cref="Budowa"/> (plan, czas, budowniczowie, badania); ta funkcja zostaje
        /// dla testów, nagród i migracji, które nie mają czekać na ludzi.
        /// </summary>
        public static string Zbuduj(Gracz gracz, string klucz)
        {
            OpisBudynku info = OpisBudynkuKlucza(klucz);
            Dictionary<string, int> koszt = KosztBudowy(gracz, klucz);
            if (koszt == null)
            {
                return $"  {info.Nazwa} ma już najwyższy poziom ({info.Max}).";
            }
            if (!MozeZaplacic(gracz, koszt))
            {
                return $"  Brakuje materiałów na {info.Nazwa.ToLowerInvariant()}.  Potrzeba: {FormatKosztu(koszt)}.";
            }
            PobierzKoszt(gracz, koszt);
            int nowy = gracz.PoziomBudynku(klucz) + 1;
            UstawPoziom(gracz, klucz, nowy);
            return nowy == 1
                ? $"  {info.Ikona}  Wznosisz: {info.Nazwa}!  ({info.Opis})"
                : $"  {info.Ikona}  {info.Nazwa} rozbudowana do poziomu {nowy}!";
        }
    }
}
