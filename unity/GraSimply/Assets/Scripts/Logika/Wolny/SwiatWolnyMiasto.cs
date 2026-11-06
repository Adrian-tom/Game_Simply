using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika.Wolny
{
    /// <summary>Szczebel rozwoju osady i to, czego wymaga.</summary>
    public sealed class OpisSzczebla
    {
        public string Nazwa;
        public int Ludzi;
        public string[] Budynki = new string[0];
        public int Zlota;
    }

    /// <summary>
    /// Droga od obozu do miasta. Szczebel rośnie sam, gdy osada spełni warunki, i nie spada, gdy ktoś odejdzie:
    /// raz odblokowane budynki zostają. Budynki miejskie działają przez ten sam kod co w grze fabularnej:
    /// to, co stoi na mapie, zamienia się na poziomy budynków ukrytego <see cref="Gracz"/>, a
    /// <see cref="Osada.DzienOsady"/> liczy z nich jedzenie, psucie zapasów, morale, leczenie i handel.
    /// </summary>
    public sealed partial class SwiatWolny
    {
        public const int LudziNaMiasto = 30;

        public static readonly OpisSzczebla[] Szczeble =
        {
            new OpisSzczebla { Nazwa = "Obóz" },
            new OpisSzczebla { Nazwa = "Wioska", Ludzi = 4, Budynki = new[] { "farma", "chata" } },
            new OpisSzczebla { Nazwa = "Miasteczko", Ludzi = 12, Budynki = new[] { "spichlerz", "tawerna" } },
            new OpisSzczebla { Nazwa = "Miasto", Ludzi = LudziNaMiasto, Budynki = new[] { "lecznica", "targ" }, Zlota = 200 },
        };

        /// <summary>Budynki liczone jak w grze fabularnej: każdy na mapie to jeden poziom.</summary>
        public static readonly string[] BudynkiMiejskie = { "farma", "spichlerz", "tawerna", "lecznica", "targ" };

        /// <summary>Zawody z budynków: pokazują się w panelu dopiero, gdy budynek stoi.</summary>
        public static readonly (string Zajecie, string Budynek)[] ZajeciaZBudynkow =
        {
            ("rolnik", "farma"), ("uzdrowiciel", "lecznica"), ("handlarz", "targ"),
        };

        /// <summary>Numer szczebla z <see cref="Szczeble"/>; trafia do zapisu.</summary>
        public int Szczebel;

        public string NazwaSzczebla => Szczeble[Szczebel].Nazwa;

        /// <summary>Od którego szczebla wolno postawić budowlę; wszystko spoza tej listy jest dostępne od początku.</summary>
        public static int WymaganySzczebel(string klucz)
        {
            switch (klucz)
            {
                case "spichlerz":
                case "tawerna": return 1;
                case "lecznica":
                case "targ": return 2;
                default: return 0;
            }
        }

        /// <summary>Co jeszcze jest potrzebne do następnego szczebla; pusta lista na ostatnim.</summary>
        public List<string> BrakiSzczebla()
        {
            var braki = new List<string>();
            if (Szczebel + 1 >= Szczeble.Length)
            {
                return braki;
            }
            OpisSzczebla n = Szczeble[Szczebel + 1];
            int ludzi = Obozowisko.Osadnicy.Count;
            if (ludzi < n.Ludzi)
            {
                braki.Add($"ludzie {ludzi}/{n.Ludzi}");
            }
            braki.AddRange(n.Budynki.Where(b => !Budowle.ContainsValue(b)).Select(NazwaBudowli));
            if (Obozowisko.Zloto < n.Zlota)
            {
                braki.Add($"złoto {Obozowisko.Zloto}/{n.Zlota}");
            }
            return braki;
        }

        /// <summary>Awans, gdy warunki są spełnione. Działa tylko w pełnych zasadach i po założeniu obozu.</summary>
        private void SprawdzSzczebel()
        {
            if (!BudowaZEtapami || !ObozZalozony)
            {
                return;
            }
            while (Szczebel + 1 < Szczeble.Length && BrakiSzczebla().Count == 0)
            {
                Szczebel++;
                PotrzebnyZapis = true;
                Pisz($"Osada rośnie: to już {NazwaSzczebla.ToLowerInvariant()}!");
            }
        }

        /// <summary>Poziomy budynków osady wynikają z tego, co stoi na mapie.</summary>
        private void PrzeliczBudynkiMiejskie()
        {
            foreach (string klucz in BudynkiMiejskie)
            {
                int ile = Budowle.Values.Count(b => b == klucz);
                if (ile > 0)
                {
                    Oboz.UstawPoziom(Obozowisko, klucz, ile);
                }
                else
                {
                    Obozowisko.Budynki.Remove(klucz);
                    Obozowisko.PoziomyBudynkow.Remove(klucz);
                }
            }
        }

        /// <summary>Budynek, przy którym stoi osadnik danego zawodu; null, gdy zawód nie ma takiego miejsca.</summary>
        private static string BudynekPracy(string zajecie)
        {
            foreach ((string z, string b) in ZajeciaZBudynkow)
            {
                if (z == zajecie)
                {
                    return b;
                }
            }
            return null;
        }
    }
}
