using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>Osadnik w obozie.</summary>
    public sealed class Osadnik
    {
        public string Imie;
        public string Zajecie;
        public double Morale;
        public string Cecha;
        public int Chory;
        public Dictionary<string, int> Dosw = new Dictionary<string, int>();
    }

    /// <summary>Startowe statystyki i wzrosty klasy.</summary>
    internal sealed class StatystykiKlasy
    {
        public int MaxHp;
        public int Atak;
        public int Obrona;
        public int Mikstury;
        public int MaxMana;
        public int HpNaPoziom;
        public int AtakNaPoziom;
        public int ObronaNaPoziom;
    }

    /// <summary>Postać gracza.</summary>
    public sealed class Gracz
    {
        /// <summary>Progi EXP potrzebne do awansu na kolejny poziom (łączny EXP).</summary>
        public static readonly int[] ExpProgi =
        {
            0, 100, 250, 450, 700, 1000, 1400, 1900, 2500, 3200, 4000,
        };

        private static readonly Dictionary<string, StatystykiKlasy> StatystykiKlas =
            new Dictionary<string, StatystykiKlasy>
            {
                {
                    "Wojownik", new StatystykiKlasy
                    {
                        MaxHp = 120, Atak = 18, Obrona = 8, Mikstury = 2, MaxMana = 0,
                        HpNaPoziom = 25, AtakNaPoziom = 6, ObronaNaPoziom = 3,
                    }
                },
                {
                    "Mag", new StatystykiKlasy
                    {
                        MaxHp = 70, Atak = 10, Obrona = 2, Mikstury = 3, MaxMana = 50,
                        HpNaPoziom = 10, AtakNaPoziom = 3, ObronaNaPoziom = 1,
                    }
                },
                {
                    "Lotrzyk", new StatystykiKlasy
                    {
                        MaxHp = 90, Atak = 14, Obrona = 5, Mikstury = 2, MaxMana = 0,
                        HpNaPoziom = 15, AtakNaPoziom = 5, ObronaNaPoziom = 2,
                    }
                },
                {
                    "Druid", new StatystykiKlasy
                    {
                        MaxHp = 92, Atak = 13, Obrona = 4, Mikstury = 3, MaxMana = 55,
                        HpNaPoziom = 12, AtakNaPoziom = 3, ObronaNaPoziom = 2,
                    }
                },
                {
                    "Nekromanta", new StatystykiKlasy
                    {
                        MaxHp = 75, Atak = 12, Obrona = 3, Mikstury = 2, MaxMana = 60,
                        HpNaPoziom = 11, AtakNaPoziom = 4, ObronaNaPoziom = 1,
                    }
                },
            };

        public static IEnumerable<string> Klasy => StatystykiKlas.Keys;

        // ---- tożsamość ---------------------------------------------------- //
        public string Imie;
        public string Klasa;
        public string Podklasa;
        public bool PodklasaDostepna;
        public string Pochodzenie;
        public List<string> Cechy = new List<string>();

        // ---- świat -------------------------------------------------------- //
        public int MapaX = Mapa.Srodek;
        public int MapaY = Mapa.Srodek;
        public string AktualnyBiom = "równiny";

        /// <summary>Seed postaci — z niego powstaje cały trwały świat.</summary>
        public long Seed;

        public int RegionX;
        public int RegionY;
        public Dictionary<string, Pole[][]> Regiony = new Dictionary<string, Pole[][]>();

        /// <summary>Poziom trudności regionu = 1 + odległość od obozu.</summary>
        public int MapaGen = 1;

        /// <summary>Siatka bieżącego regionu — widok na Regiony, nie druga kopia.</summary>
        public Pole[][] MapaPola;

        // ---- rozwój ------------------------------------------------------- //
        public int Poziom = 1;
        public int Exp;
        public int Zloto = 30;
        public int MaxHp;
        public int Hp;
        public int Atak;
        public int Obrona;
        public int MaxMana;
        public int Mana;
        public int PunktyAtrybutow = 2; // start jak w BG3 — kilka punktów do rozdania
        public int PunktyUmiejetnosci;
        public int PunktyTalentow;
        public string TrybTrudnosci = "normalny";
        public int Karma;
        public bool BlogoslawienstwoWyprawy;

        private int _hpNaPoziom;
        private int _atakNaPoziom;
        private int _obronaNaPoziom;

        // ---- ekwipunek ---------------------------------------------------- //
        public int Mikstury;
        public int MiksturyDuze;
        public int MiksturyMany;
        public int Antidota;
        public Dictionary<string, string> Wyposazenie = new Dictionary<string, string>
        {
            { "bron", null }, { "zbroja", null },
        };
        public List<string> Plecak = new List<string>();
        public Dictionary<string, int> Przedmioty = new Dictionary<string, int>();
        public Dictionary<string, int> Skladniki = new Dictionary<string, int>();
        public List<string> Przepisy = new List<string>();
        public Dictionary<string, int> Ulepszenia = new Dictionary<string, int>
        {
            { "bron", 0 }, { "zbroja", 0 },
        };

        // ---- postęp ------------------------------------------------------- //
        public HashSet<string> Osiagniecia = new HashSet<string>();
        public HashSet<string> AktywneQuesty = new HashSet<string>();
        public HashSet<string> UkonczoneQuesty = new HashSet<string>();
        public Dictionary<string, int> QuestyStart = new Dictionary<string, int>();
        public Dictionary<string, int> Statystyki = new Dictionary<string, int>
        {
            { "zabite_potwory", 0 }, { "wygrane_walki", 0 }, { "zakupy", 0 },
            { "odwiedzone_swiatynie", 0 }, { "zebrane_surowce", 0 }, { "zbudowane_budynki", 0 },
        };
        public List<string> Umiejetnosci = new List<string>();
        public Dictionary<string, int> RangiUmiejetnosci = new Dictionary<string, int>();
        public List<string> Talenty = new List<string>();
        public Dictionary<string, int> TestyRozmow = new Dictionary<string, int>();
        public Dictionary<string, object> Flagi = new Dictionary<string, object>();

        // ---- osada -------------------------------------------------------- //
        public Dictionary<string, int> Surowce = new Dictionary<string, int>
        {
            { "zywnosc", 30 }, { "drewno", 3 }, { "kamien", 2 },
            { "ziola", 1 }, { "skora", 0 }, { "ruda", 0 },
        };
        public HashSet<string> Budynki = new HashSet<string>();
        public Dictionary<string, int> PoziomyBudynkow = new Dictionary<string, int>();
        public List<Dictionary<string, object>> Rekruci = new List<Dictionary<string, object>>();
        public bool ZbieraczeWPracy;
        public int Czas;
        public int CzasWyjscia;
        public int Chaty = 2;
        public List<Osadnik> Osadnicy = new List<Osadnik>();

        /// <summary>
        /// Relacje między osadnikami: klucz to para imion posortowana
        /// alfabetycznie, więc jedna wartość opisuje związek w obie strony.
        /// </summary>
        public Dictionary<string, double> Wiezi = new Dictionary<string, double>();

        public Dictionary<string, int> WatkiNpc = new Dictionary<string, int>();
        public double Zagrozenie;
        public int? NajazdZa;
        public int Najazdy;
        public List<string> Kronika = new List<string>();
        public List<string> Sprawy = new List<string>();
        public int Narzedzia;
        public Dictionary<string, int> ZapasyCel = new Dictionary<string, int> { { "mikstura", 3 } };
        public List<Dictionary<string, object>> Karawany = new List<Dictionary<string, object>>();
        public Dictionary<string, Dictionary<string, double>> Ceny =
            new Dictionary<string, Dictionary<string, double>>();

        // ---- przetrwanie i umysł ------------------------------------------ //
        public int Prowiant;
        public int Glod;
        public List<Rana> Rany = new List<Rana>();
        public bool WObozie = true;
        public int Pokolenie = 1;
        public List<Dictionary<string, object>> Rod = new List<Dictionary<string, object>>();
        public Dictionary<string, Dictionary<string, object>> Mysli =
            new Dictionary<string, Dictionary<string, object>>();
        public Dictionary<string, int> Atrybuty;
        public List<string> BiegleSkille;

        // ------------------------------------------------------------------ //
        //  Tworzenie                                                          //
        // ------------------------------------------------------------------ //

        public Gracz(string imie, string klasa = "Wojownik", Losowanie rng = null)
        {
            Imie = imie;
            Klasa = klasa;
            rng = rng ?? new Losowanie();
            Seed = rng.Zakres(1, int.MaxValue);

            if (!StatystykiKlas.TryGetValue(klasa, out StatystykiKlasy stat))
            {
                throw new ArgumentException($"Nieznana klasa: {klasa}", nameof(klasa));
            }
            MaxHp = stat.MaxHp;
            Hp = stat.MaxHp;
            Atak = stat.Atak;
            Obrona = stat.Obrona;
            Mikstury = stat.Mikstury;
            MiksturyMany = stat.MaxMana > 0 ? 1 : 0;
            MaxMana = stat.MaxMana;
            Mana = stat.MaxMana;
            _hpNaPoziom = stat.HpNaPoziom;
            _atakNaPoziom = stat.AtakNaPoziom;
            _obronaNaPoziom = stat.ObronaNaPoziom;

            // Obóz startowy: dwie chaty i dwoje ludzi, którzy uwierzyli w twój ogień.
            Osadnicy.Add(new Osadnik
            {
                Imie = "Olek", Zajecie = "drwal", Morale = 60.0, Cecha = "pracowity",
            });
            Osadnicy.Add(new Osadnik
            {
                Imie = "Jagna", Zajecie = "mysliwy", Morale = 60.0, Cecha = "wesoly",
            });
            // Olek i Jagna zaczynają jako znajomi — przeprowadzili się razem.
            Wiezi["Jagna|Olek"] = 20.0;

            Atrybuty = Logika.Atrybuty.StartoweAtrybuty(klasa);
            BiegleSkille = Logika.Atrybuty.BiegleSkilleKlasy(klasa);
        }

        // ------------------------------------------------------------------ //
        //  Stan                                                               //
        // ------------------------------------------------------------------ //

        public bool Zyje()
        {
            return Hp > 0;
        }

        public bool MaTalent(string klucz)
        {
            return Talenty != null && Talenty.Contains(klucz);
        }

        /// <summary>
        /// Poziom budynku w osadzie. Stare zapisy trzymały tylko zbiór nazw,
        /// więc obecność w <c>Budynki</c> liczy się jako poziom 1.
        /// </summary>
        public int PoziomBudynku(string klucz)
        {
            if (PoziomyBudynkow.TryGetValue(klucz, out int poziom))
            {
                return poziom;
            }
            return Budynki.Contains(klucz) ? 1 : 0;
        }

        /// <summary>Łączny EXP wymagany, aby opuścić dany poziom.</summary>
        public static int ProgExp(int poziom)
        {
            if (poziom < ExpProgi.Length)
            {
                return ExpProgi[poziom];
            }
            return ExpProgi[ExpProgi.Length - 1] + (poziom - ExpProgi.Length + 1) * 850;
        }

        public int ExpDoAwansu()
        {
            return ProgExp(Poziom);
        }

        /// <summary>Postęp w bieżącym poziomie: (zdobyte w poziomie, potrzeba na awans).</summary>
        public (int Zdobyte, int Potrzeba) ExpWPoziomie()
        {
            int poprzedni = Poziom <= 1 ? 0 : ProgExp(Poziom - 1);
            int potrzeba = ExpDoAwansu() - poprzedni;
            int zdobyte = Math.Max(0, Exp - poprzedni);
            return (zdobyte, potrzeba);
        }

        // ------------------------------------------------------------------ //
        //  Akcje                                                              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Leczy gracza i zmniejsza liczbę mikstur.
        ///
        /// Wersja pythonowa dolicza jeszcze premię z pochodzenia
        /// (<c>bonus_leczenia_mikstury</c>). Pochodzenia nie ma jeszcze
        /// w porcie (etap 2), więc na razie zostaje baza i mnożnik z ran —
        /// przy braku premii wynik jest ten sam.
        /// </summary>
        public string UzyjMiksture()
        {
            if (Mikstury <= 0)
            {
                return "Nie masz żadnych mikstur!";
            }
            int lecz = (int)(40 * Przetrwanie.MnoznikLeczenia(this));
            Mikstury -= 1;
            int poprzednie = Hp;
            Hp = Math.Min(Hp + lecz, MaxHp);
            return $"Użyłeś mikstury leczenia! Przywróciłeś {Hp - poprzednie} HP. (Mikstury: {Mikstury})";
        }

        public string UzyjMikstureDuza()
        {
            if (MiksturyDuze <= 0)
            {
                return "Nie masz większych mikstur!";
            }
            MiksturyDuze -= 1;
            int poprzednie = Hp;
            int lecz = (int)(80 * Przetrwanie.MnoznikLeczenia(this));
            Hp = Math.Min(Hp + lecz, MaxHp);
            return $"Użyłeś większej mikstury! Przywróciłeś {Hp - poprzednie} HP. " +
                   $"(Większe mikstury: {MiksturyDuze})";
        }

        public string UzyjMiksktureMany()
        {
            if (MaxMana <= 0)
            {
                return "Twoja klasa nie korzysta z many.";
            }
            if (MiksturyMany <= 0)
            {
                return "Nie masz mikstur many!";
            }
            MiksturyMany -= 1;
            int poprzednia = Mana;
            Mana = Math.Min(Mana + 30, MaxMana);
            return $"Użyłeś mikstury many! Przywróciłeś {Mana - poprzednia} many. " +
                   $"(Mikstury many: {MiksturyMany})";
        }

        public string UzyjAntidotum()
        {
            if (Antidota <= 0)
            {
                return null;
            }
            Antidota -= 1;
            return $"Wypijasz antidotum. (Antidota: {Antidota})";
        }

        /// <summary>
        /// Dodaje EXP i sprawdza awans. Zwraca listę komunikatów.
        ///
        /// Mnożnik EXP z pochodzenia („premia weterana”) dojdzie razem
        /// z modułem pochodzenia w etapie 2.
        /// </summary>
        public List<string> ZdobadzExp(int ilosc)
        {
            ilosc = Math.Max(1, ilosc);
            var komunikaty = new List<string> { $"Zdobyłeś {ilosc} EXP!" };
            Exp += ilosc;
            while (Exp >= ExpDoAwansu())
            {
                komunikaty.AddRange(Awansuj());
            }
            return komunikaty;
        }

        /// <summary>Awansuje gracza o jeden poziom.</summary>
        public List<string> Awansuj()
        {
            Poziom += 1;
            MaxHp += _hpNaPoziom;
            Hp = MaxHp;
            Atak += _atakNaPoziom;
            Obrona += _obronaNaPoziom;
            if (MaxMana > 0)
            {
                MaxMana += 10;
                Mana = MaxMana;
            }

            int pktAtr = Poziom % 5 == 0 ? 2 : 1;
            int pktSkilli = Poziom == 5 || Poziom == 10 || Poziom == 15 ? 2 : 1;
            PunktyAtrybutow += pktAtr;
            PunktyUmiejetnosci += pktSkilli;
            PunktyTalentow += 1;

            var komunikaty = new List<string>
            {
                $"*** AWANS NA POZIOM {Poziom}! ***",
                $"  Max HP: {MaxHp}  Atak: {Atak}  Obrona: {Obrona}",
            };
            if (MaxMana > 0)
            {
                komunikaty.Add($"  Max Mana: {MaxMana}");
            }
            komunikaty.Add("  HP i mana zostały w pełni uzupełnione!");
            komunikaty.Add($"  +{pktAtr} pkt. atrybutów, +{pktSkilli} pkt. umiejętności — " +
                           "rozdaj w obozie.");

            if (Poziom == 5 && Podklasa == null)
            {
                PodklasaDostepna = true;
                komunikaty.Add("  ⭐  Osiągnąłeś poziom 5! Możesz wybrać podklasę w obozie.");
            }
            return komunikaty;
        }

        /// <summary>Rejestruje wygraną walkę i aktualizuje statystyki dla questów.</summary>
        public void RejestrujWalke(string nazwaPotwora)
        {
            Statystyki.Dodaj("wygrane_walki", 1);
            Statystyki.Dodaj("zabite_potwory", 1);
            Statystyki.Dodaj($"zabite_{nazwaPotwora.ToLowerInvariant()}", 1);
        }

        /// <summary>Definicje osiągnięć: klucz, nazwa, warunek.</summary>
        private static readonly List<(string Klucz, string Nazwa, Func<Gracz, bool> Warunek)> Osiagi =
            new List<(string, string, Func<Gracz, bool>)>
            {
                ("pierwsze_kroki", "🥇 Pierwsze kroki", g => g.Statystyki.Wez("zabite_potwory") >= 1),
                ("rzeźnik", "🗡 Rzeźnik", g => g.Statystyki.Wez("zabite_potwory") >= 50),
                ("wojownik_mroku", "⚔ Wojownik Mroku", g => g.Statystyki.Wez("zabite_potwory") >= 100),
                ("odkrywca", "🗺 Odkrywca", g => Mapa.LiczbaRegionow(g) >= 10),
                ("podroznik", "🌍 Podróżnik", g => Mapa.LiczbaRegionow(g) >= 5),
                ("kartograf", "🗺 Kartograf", g => Mapa.LiczbaOdkrytych(g) >= Mapa.LiczbaPol()),
                ("bogacz", "💰 Bogacz", g => g.Zloto >= 500),
                ("kolekcjoner", "🧪 Kolekcjoner", g => g.Mikstury >= 10),
                ("legenda", "👑 Legenda", g => g.Poziom >= 10),
                ("kapitan", "🎖 Kapitan", g => g.Poziom >= 5),
                ("badacz_swiatyn", "🛕 Badacz Świątyń", g => g.Statystyki.Wez("odwiedzone_swiatynie") >= 5),
                ("osadnik", "🏕 Osadnik", g => g.Budynki.Count >= 1),
                ("starosta", "🏘 Starosta", g => g.Budynki.Count >= 4),
                ("druzynowy", "🤝 Drużynowy", g => g.Rekruci.Count >= 1),
                ("mitolog", "🌌 Mitolog", g => g.Statystyki.Wez("odwiedzone_mityczne") >= 1),
                ("pogromca_mitow", "🐉 Pogromca mitów", g => g.Statystyki.Wez("odwiedzone_mityczne") >= 3),
                ("obywatel", "🏙 Obywatel", g => g.Statystyki.Wez("odwiedzone_miasta") >= 1),
                ("gospodarz", "🛖 Gospodarz", g => g.Chaty >= 2 && g.Osadnicy.Count >= 1),
                ("hurtownik", "🛒 Hurtownik", g => g.Statystyki.Wez("zloto_z_targu") >= 80),
            };

        /// <summary>Sprawdza i odblokowuje nowe osiągnięcia. Zwraca listę nowych.</summary>
        public List<string> SprawdzOsiagniecia()
        {
            var nowe = new List<string>();
            foreach ((string klucz, string nazwa, Func<Gracz, bool> warunek) in Osiagi)
            {
                if (!Osiagniecia.Contains(klucz) && warunek(this))
                {
                    Osiagniecia.Add(klucz);
                    nowe.Add($"  🏆 OSIĄGNIĘCIE: {nazwa}");
                }
            }
            return nowe;
        }

        // ------------------------------------------------------------------ //
        //  Wyświetlanie                                                       //
        // ------------------------------------------------------------------ //

        public string PasekHp(int szerokosc = 20)
        {
            int wypelniony = (int)(Hp / (double)MaxHp * szerokosc);
            return "[" + new string('█', wypelniony) + new string('░', szerokosc - wypelniony) + "]";
        }

        public string PasekMany(int szerokosc = 20)
        {
            if (MaxMana == 0)
            {
                return "";
            }
            int wypelniony = (int)(Mana / (double)MaxMana * szerokosc);
            return "[" + new string('▓', wypelniony) + new string('░', szerokosc - wypelniony) + "]";
        }

        /// <summary>Karta postaci — ten sam układ co w wersji pythonowej.</summary>
        public string KartaPostaci()
        {
            string linia = new string('─', 40);
            string klasaStr = Klasa;
            if (Podklasa != null)
            {
                klasaStr += $" / {Podklasa}";
            }
            else if (PodklasaDostepna)
            {
                klasaStr += " ⭐ (wybierz podklasę!)";
            }
            string manaLinia = MaxMana > 0
                ? $"\n  🔮  Mana: {Mana}/{MaxMana} {PasekMany()}"
                : "";
            (int expTeraz, int expPotrzeba) = ExpWPoziomie();
            string pktLinia = PunktyAtrybutow > 0 || PunktyUmiejetnosci > 0
                ? $"\n  Do rozdania: {PunktyAtrybutow} atr.  {PunktyUmiejetnosci} um."
                : "";
            string cechy = Cechy.Count > 0 ? string.Join(", ", Cechy) : "— brak —";

            return $"\n{linia}\n" +
                   $"  🧙  Bohater: {Imie} [{klasaStr}]  (Poz. {Poziom})\n" +
                   $"  📜  Pochodzenie: {Pochodzenie ?? "— nieznane —"}\n" +
                   $"  ✨  Cechy: {cechy}\n" +
                   $"  ❤️  HP: {Hp}/{MaxHp} {PasekHp()}{manaLinia}\n" +
                   $"  ⚔  Atak: {Atak}   🛡  Obrona: {Obrona}\n" +
                   $"{Logika.Atrybuty.LiniaAtrybutow(this)}\n" +
                   $"  ⭐  EXP: {expTeraz}/{expPotrzeba}   (łącznie {Exp})   " +
                   $"💰 Złoto: {Zloto} szt.{pktLinia}\n" +
                   $"  🧪 Mikstury: {Mikstury}   💚 Większe: {MiksturyDuze}   " +
                   $"🔮 Many: {MiksturyMany}   🧴 Antidota: {Antidota}\n" +
                   $"{linia}";
        }
    }
}
