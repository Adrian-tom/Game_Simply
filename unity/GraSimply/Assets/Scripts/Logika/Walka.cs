using System;
using System.Collections.Generic;
using System.Linq;

namespace GraSimply.Logika
{
    /// <summary>
    /// Stan jednej walki: bufy, debufy i statusy obu stron.
    ///
    /// Pola bez pokrycia w tym etapie portu (tarcza runowa, formy, przyzwania,
    /// czasy odnowienia) zostają, bo czyta je wyświetlanie stanu i wejdą
    /// w życie razem z umiejętnościami klasowymi z <c>game/skills.py</c>.
    /// </summary>
    public sealed class StanWalki
    {
        public int Tura = 1;

        // Bufy gracza
        public double BuffAtakMnoznik = 1.0;
        public int BuffAtakTury;
        public double BuffObronaMnoznik = 1.0;
        public int BuffObronaTury;
        public bool BrakObronyTura;
        public bool LeczenieZablokowane;
        public int TarczaRunowa;
        public bool UnikAktywny;
        public double UnikSzansa = 0.75;
        public int RegeneracjaHp;
        public int RegeneracjaTury;
        public double NastepnyAtakMnoznik = 1.0;
        public bool LichOchrona;
        public int LichOchronaHp = 40;

        // Debufy wroga
        public int WrogOgluszoneTury;
        public int WrogTruciznaTury;
        public int WrogTruciznaObrazenia = 10;
        public int WrogOslabienieTury;
        public bool WrogRozpad;

        // Statusy gracza
        public int GraczTruciznaTury;
        public int GraczTruciznaObrazenia = 8;
        public int GraczKrwawienieTury;
        public int GraczKrwawienieObrazenia = 6;
        public int GraczOgluszoneTury;

        public bool JestBoss;

        // Formy i czasy odnowienia — czekają na umiejętności
        public string Forma;
        public int FormaTury;
        public double FormaAtak = 1.0;
        public double FormaObrona = 1.0;
        public double FormaKryt;
        public double FormaUnik;
        public int FormaRegen;
        public int FormaMana;
        public Dictionary<string, int> Cd = new Dictionary<string, int>();

        public bool TarczaLosuUzyta;
        public bool NieustepliwyUzyty;

        // Zapowiedzi, garda i efekty przedmiotów
        public string Zapowiedz;
        public int HpPrzyZapowiedzi;
        public bool Garda;
        public int WrogPodpalony;
        public int OlejTury;
        public int EliksirSilyTury;
        public bool OdpornoscOgnia;

        /// <summary>Tekst wiedzy o wrogu; null = jeszcze nie sprawdzono.</summary>
        public string WiedzaPokazana;

        /// <summary>Wróg tej walki — potrzebny przedmiotom działającym na niego.</summary>
        public Przeciwnik Wrog;
    }

    /// <summary>
    /// Walka turowa — port rdzenia <c>game/combat.py</c>.
    ///
    /// Jest tu wszystko, co nie wymaga modułów spoza portu: obrażenia z kritem,
    /// zapowiadane ruchy wroga wraz z gardą i kontrą, ogień i regeneracja,
    /// szał bossa, trucizna i krwawienie, uniki, ucieczka, mikstury i nagrody.
    ///
    /// Czego tu jeszcze nie ma, bo wisi na nieprzeniesionych modułach:
    /// umiejętności klasowe (<c>skills.py</c>), przedmioty bojowe i łupy
    /// (<c>items.py</c>, <c>rzemioslo.py</c>), towarzysze (<c>rekruci.py</c>),
    /// zadania (<c>quests.py</c>) i cechy pochodzenia (<c>pochodzenie.py</c>).
    /// Miejsca styku są oznaczone komentarzem, żeby kolejny etap wiedział, gdzie wejść.
    /// </summary>
    public static class Walka
    {
        private static readonly Dictionary<string, double> SzansaKryt =
            new Dictionary<string, double>
            {
                { "Wojownik", 0.10 },
                { "Mag", 0.08 },
                { "Lotrzyk", 0.22 },
                { "Druid", 0.06 },
                { "Nekromanta", 0.12 },
            };

        private static readonly Dictionary<string, string> OpisyZapowiedzi =
            new Dictionary<string, string>
            {
                { "ciezki_cios", "bierze potężny zamach — następny cios będzie podwójny. Garda zatrzyma 70%." },
                { "ogien", "nabiera powietrza — w następnej turze zieje ogniem, obrona nie pomoże. Garda −50%, eliksir ognioodporności −50%." },
                { "klatwa", "zaczyna inkantację klątwy. Zadaj mocny cios (20% jego życia) albo go ogłusz, by ją przerwać." },
            };

        private static readonly string[] StatusyWroga = { "trucizna", "krwawienie", "ogluszone" };

        // ------------------------------------------------------------------ //
        //  Obliczenia obrażeń                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>Obrażenia z losową wariancją ±20%.</summary>
        public static int ObliczObrazenia(int atak, int obrona, Losowanie rng)
        {
            int bazowe = Math.Max(1, atak - obrona);
            int wariancja = Math.Max(1, (int)(bazowe * 0.2));
            return rng.Calkowita(Math.Max(1, bazowe - wariancja), bazowe + wariancja);
        }

        /// <summary>Obrażenia z szansą na trafienie krytyczne (×2).</summary>
        public static int AtakZKrytem(int atak, int obrona, string klasa, StanWalki stan,
                                      Gracz gracz, Losowanie rng, out bool krit)
        {
            int bazowe = ObliczObrazenia(atak, obrona, rng);
            double szansa;
            if (klasa == null || !SzansaKryt.TryGetValue(klasa, out szansa))
            {
                szansa = 0.10;
            }

            // Łotrzyk dostaje +10% szansy, gdy ma aktywny buff ataku.
            if (klasa == "Lotrzyk" && stan.BuffAtakTury > 0)
            {
                szansa += 0.10;
            }

            szansa += stan.FormaKryt;
            if (gracz != null)
            {
                szansa += Atrybuty.SzansaKrytaZrecznosc(gracz);
            }

            if (rng.Losowa() < szansa)
            {
                krit = true;
                return (int)(bazowe * 2);
            }

            krit = false;
            return bazowe;
        }

        // ------------------------------------------------------------------ //
        //  Ucieczka i mnożniki                                                 //
        // ------------------------------------------------------------------ //

        /// <summary>Ucieczka zależy od Zręczności, klasy i ran — nie jest rzutem monetą.</summary>
        public static double SzansaUcieczki(Gracz gracz, StanWalki stan)
        {
            double szansa = 0.35 + 0.06 * Atrybuty.Modyfikator(gracz, "zrecznosc");
            if (gracz.Klasa == "Lotrzyk")
            {
                szansa += 0.15;
            }

            if (gracz.MaTalent("szosty_zmysl"))
            {
                szansa += 0.15;
            }

            szansa += Przetrwanie.PremiaUcieczki(gracz);
            if (stan.JestBoss)
            {
                szansa -= 0.25;
            }

            if (gracz.TrybTrudnosci == "hardcore")
            {
                szansa -= 0.10;
            }

            return Math.Max(0.05, Math.Min(0.90, szansa));
        }

        /// <summary>Wszystko, co zmienia siłę ciosu poza bufami umiejętności.</summary>
        private static double MnoznikGracza(Gracz gracz, StanWalki stan)
        {
            double m = Przetrwanie.MnoznikAtaku(gracz);
            if (stan.EliksirSilyTury > 0)
            {
                m *= 1.4;
            }

            if (stan.JestBoss && gracz.MaTalent("zabojca_potworow"))
            {
                m *= 1.2;
            }

            return m;
        }

        private static string DopisekTypu(Przeciwnik przeciwnik, string typ)
        {
            double m = Wrogowie.MnoznikTypu(przeciwnik, typ);
            if (m > 1)
            {
                return $"  ({Wrogowie.NazwyTypow[typ]}: SŁABOŚĆ!)";
            }

            if (m < 1)
            {
                return $"  ({Wrogowie.NazwyTypow[typ]}: odporny)";
            }

            return "";
        }

        private static void Podpal(Przeciwnik przeciwnik, StanWalki stan, int tury, IKonsola konsola)
        {
            if (przeciwnik.Odpornosci.Contains("ogien"))
            {
                return;
            }

            if (stan.WrogPodpalony == 0 && przeciwnik.Regeneracja > 0)
            {
                konsola.Pisz($"  🔥  {przeciwnik.Nazwa} płonie — ogień nie pozwala ranom się zasklepić!");
            }

            stan.WrogPodpalony = Math.Max(stan.WrogPodpalony, tury);
        }

        // ------------------------------------------------------------------ //
        //  Wiedza o wrogu                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Bierny test wiedzy: kto ma głowę (INT/MDR), ten widzi słabości wroga.</summary>
        private static void PokazWiedzeOWrogu(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                              IKonsola konsola)
        {
            if (stan.WiedzaPokazana != null)
            {
                if (stan.WiedzaPokazana.Length > 0)
                {
                    konsola.Pisz(stan.WiedzaPokazana);
                }

                return;
            }

            int wiedza = 10 + Math.Max(Atrybuty.Modyfikator(gracz, "inteligencja"),
                                       Atrybuty.Modyfikator(gracz, "madrosc"));
            if (gracz.MaTalent("wewnetrzny_glos"))
            {
                wiedza += 2;
            }

            var czesci = new List<string>();
            if (przeciwnik.Slabosci.Count > 0)
            {
                czesci.Add("słaby na: " + string.Join(", ",
                    przeciwnik.Slabosci.OrderBy(t => t, StringComparer.Ordinal)
                                       .Select(t => Wrogowie.NazwyTypow[t])));
            }

            if (przeciwnik.Odpornosci.Count > 0)
            {
                czesci.Add("odporny na: " + string.Join(", ",
                    przeciwnik.Odpornosci.OrderBy(t => t, StringComparer.Ordinal)
                                         .Select(t => Wrogowie.NazwyTypow[t])));
            }

            if (przeciwnik.Regeneracja > 0)
            {
                czesci.Add("regeneruje się (ogień to przerywa)");
            }

            if (czesci.Count > 0 && wiedza >= 12)
            {
                stan.WiedzaPokazana = $"  🧠  WIEDZA [bierny test ST 12: sukces] — "
                                    + $"{przeciwnik.Nazwa}: {string.Join("; ", czesci)}.";
                konsola.Pisz(stan.WiedzaPokazana);
            }
            else
            {
                stan.WiedzaPokazana = "";
            }
        }

        // ------------------------------------------------------------------ //
        //  Zapowiedzi, ogień, szał                                             //
        // ------------------------------------------------------------------ //

        private static void PokazZapowiedz(Przeciwnik przeciwnik, StanWalki stan, IKonsola konsola)
        {
            if (!string.IsNullOrEmpty(stan.Zapowiedz))
            {
                konsola.Pisz($"\n  ⚠  ZAPOWIEDŹ: {przeciwnik.Nazwa} {OpisyZapowiedzi[stan.Zapowiedz]}");
            }
        }

        /// <summary>Ogień pali, regeneracja leczy — chyba że ogień ją blokuje. True = wróg padł.</summary>
        private static bool PlomienieIRegeneracja(Przeciwnik przeciwnik, StanWalki stan,
                                                  IKonsola konsola)
        {
            if (stan.WrogPodpalony > 0)
            {
                int dmg = Math.Max(3, (int)(przeciwnik.MaxHp * 0.04
                                            * Wrogowie.MnoznikTypu(przeciwnik, "ogien")));
                przeciwnik.Hp = Math.Max(0, przeciwnik.Hp - dmg);
                stan.WrogPodpalony -= 1;
                konsola.Pisz($"  🔥  {przeciwnik.Nazwa} płonie: −{dmg} HP.");
                if (!przeciwnik.Zyje())
                {
                    return true;
                }
            }
            else if (przeciwnik.Regeneracja > 0 && przeciwnik.Hp < przeciwnik.MaxHp)
            {
                int regen = Math.Max(3, (int)(przeciwnik.MaxHp * przeciwnik.Regeneracja));
                int faktyczne = Math.Min(regen, przeciwnik.MaxHp - przeciwnik.Hp);
                przeciwnik.Hp += faktyczne;
                konsola.Pisz($"  💚  {przeciwnik.Nazwa} regeneruje {faktyczne} HP! (ogień by to przerwał)");
            }

            return false;
        }

        private static void SzalBossa(Przeciwnik przeciwnik, StanWalki stan, IKonsola konsola)
        {
            if (stan.JestBoss && !przeciwnik.SzalUzyty && przeciwnik.Hp <= przeciwnik.MaxHp * 0.5)
            {
                przeciwnik.SzalUzyty = true;
                przeciwnik.Atak = (int)(przeciwnik.Atak * 1.25);
                konsola.Pisz($"  🔥  {przeciwnik.Nazwa} wpada w SZAŁ! Atak +25% do końca walki.");
            }
        }

        /// <summary>Ochrona Licha albo talent „nieustępliwy”. True = śmierć anulowana.</summary>
        private static bool SprobujOcalic(Gracz gracz, StanWalki stan, IKonsola konsola)
        {
            if (gracz.Zyje())
            {
                return false;
            }

            if (stan.LichOchrona)
            {
                stan.LichOchrona = false;
                gracz.Hp = stan.LichOchronaHp;
                konsola.Pisz($"  💀  Ochrona Licha zadziałała! Zamiast umrzeć, odnawiasz {gracz.Hp} HP!");
                return true;
            }

            if (gracz.MaTalent("nieustepliwy") && !stan.NieustepliwyUzyty)
            {
                stan.NieustepliwyUzyty = true;
                gracz.Hp = 1;
                konsola.Pisz("  ⚔  Nieustępliwy! Chwiejesz się, ale stoisz — 1 HP.");
                return true;
            }

            // Tutaj w wersji pythonowej wchodzi jeszcze „tarcza losu” z pochodzenia.
            return false;
        }

        private static void ZadajGraczowi(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                          int obrazenia, string opis, IKonsola konsola, Losowanie rng)
        {
            if (obrazenia <= 0)
            {
                return;
            }

            gracz.Hp = Math.Max(0, gracz.Hp - obrazenia);
            konsola.Pisz($"  {opis} Otrzymujesz {obrazenia} obrażeń!");
            if (SprobujOcalic(gracz, stan, konsola))
            {
                return;
            }

            string rana = Przetrwanie.MozeZranic(gracz, obrazenia, stan.JestBoss, rng);
            if (!string.IsNullOrEmpty(rana))
            {
                konsola.Pisz(rana);
            }
        }

        private static void WykonajZapowiedz(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                             IKonsola konsola, Losowanie rng)
        {
            string typ = stan.Zapowiedz;
            stan.Zapowiedz = null;
            bool garda = stan.Garda;

            if (typ == "ciezki_cios")
            {
                int obr = ObliczObrazenia(przeciwnik.Atak, gracz.Obrona, rng) * 2;
                if (garda)
                {
                    obr = Math.Max(1, (int)(obr * 0.3));
                }

                string gdzie = garda ? " na twoją gardę" : "";
                ZadajGraczowi(gracz, przeciwnik, stan, obr,
                              $"💥  {przeciwnik.Nazwa} spuszcza potężny cios{gdzie}!", konsola, rng);
            }
            else if (typ == "ogien")
            {
                bool unikForma = rng.Losowa() < stan.FormaUnik;
                bool unikZrecznosc = !Przetrwanie.BezUniku(gracz)
                                     && rng.Losowa() < Atrybuty.SzansaUnikuZrecznosc(gracz);
                if (unikForma || unikZrecznosc)
                {
                    konsola.Pisz($"  💨  Uskakujesz przed ogniem {przeciwnik.Nazwa}!");
                    return;
                }

                double baza = rng.Calkowita(16, 28) * (1 + 0.10 * (przeciwnik.Poziom - 1));
                if (stan.JestBoss)
                {
                    baza *= 1.4;
                }

                if (garda)
                {
                    baza *= 0.5;
                }

                if (stan.OdpornoscOgnia)
                {
                    baza *= 0.5;
                }

                int obr = (int)baza;
                if (stan.TarczaRunowa > 0)
                {
                    int absorbcja = Math.Min(stan.TarczaRunowa, obr);
                    stan.TarczaRunowa -= absorbcja;
                    obr -= absorbcja;
                    konsola.Pisz($"  🔮  Tarcza runowa absorbuje {absorbcja} obrażeń ognia!");
                }

                ZadajGraczowi(gracz, przeciwnik, stan, obr,
                              $"🔥  {przeciwnik.Nazwa} zieje ogniem!", konsola, rng);
            }
            else if (typ == "klatwa")
            {
                int strata = stan.HpPrzyZapowiedzi - przeciwnik.Hp;
                if (strata >= przeciwnik.MaxHp * 0.2)
                {
                    konsola.Pisz($"  ✋  Twój cios przerywa inkantację {przeciwnik.Nazwa}! Klątwa się rozpada.");
                    return;
                }

                stan.GraczTruciznaTury = Math.Max(stan.GraczTruciznaTury, 3);
                stan.GraczTruciznaObrazenia =
                    (int)((stan.JestBoss ? 12 : 7) * (1 + 0.1 * (przeciwnik.Poziom - 1)));
                konsola.Pisz($"  ⚗  {przeciwnik.Nazwa} kończy klątwę! Trucizna: "
                           + $"−{stan.GraczTruciznaObrazenia} HP na turę przez 3 tury (antidotum pomoże).");
            }
        }

        /// <summary>Koniec tury wroga: garda przechodzi w kontrę, wróg może zapowiedzieć ruch.</summary>
        private static void PoTurzeWroga(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                         IKonsola konsola, Losowanie rng)
        {
            if (stan.Garda)
            {
                stan.Garda = false;
                double kontra = gracz.MaTalent("kontra") ? 1.6 : 1.3;
                stan.NastepnyAtakMnoznik = Math.Max(stan.NastepnyAtakMnoznik, kontra);
                konsola.Pisz($"  🛡  Garda otwiera kontrę: następny cios ×{kontra:0.0}.");
            }

            if (!gracz.Zyje() || !przeciwnik.Zyje() || !string.IsNullOrEmpty(stan.Zapowiedz))
            {
                return;
            }

            foreach (KeyValuePair<string, double> para in przeciwnik.Zapowiedzi)
            {
                if (rng.Losowa() < para.Value)
                {
                    stan.Zapowiedz = para.Key;
                    stan.HpPrzyZapowiedzi = przeciwnik.Hp;
                    konsola.Pisz($"  ⚠  {przeciwnik.Nazwa} {OpisyZapowiedzi[para.Key]}");
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Tura przeciwnika                                                    //
        // ------------------------------------------------------------------ //

        private static void TuraPrzeciwnika(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                            IKonsola konsola, Losowanie rng)
        {
            if (stan.WrogTruciznaTury > 0)
            {
                int dam = Math.Max(1, (int)(stan.WrogTruciznaObrazenia
                                            * Wrogowie.MnoznikTypu(przeciwnik, "trucizna")));
                przeciwnik.Hp = Math.Max(0, przeciwnik.Hp - dam);
                stan.WrogTruciznaTury -= 1;
                konsola.Pisz($"  ☠  Trucizna! {przeciwnik.Nazwa} traci {dam} HP."
                           + $" (Pozostało tur: {stan.WrogTruciznaTury})");
                if (!przeciwnik.Zyje())
                {
                    return;
                }
            }

            if (PlomienieIRegeneracja(przeciwnik, stan, konsola))
            {
                return;
            }

            if (stan.WrogOgluszoneTury > 0)
            {
                stan.WrogOgluszoneTury -= 1;
                if (!string.IsNullOrEmpty(stan.Zapowiedz))
                {
                    konsola.Pisz($"  ❄  Ogłuszenie przerywa to, co {przeciwnik.Nazwa} szykował!");
                    stan.Zapowiedz = null;
                }

                konsola.Pisz($"  ❄  {przeciwnik.Nazwa} jest ogłuszony i pomija turę!");
                PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                return;
            }

            SzalBossa(przeciwnik, stan, konsola);
            if (!string.IsNullOrEmpty(stan.Zapowiedz))
            {
                WykonajZapowiedz(gracz, przeciwnik, stan, konsola, rng);
                PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                return;
            }

            int efektywnaObrona;
            if (stan.BrakObronyTura)
            {
                efektywnaObrona = 0;
                stan.BrakObronyTura = false;
            }
            else if (stan.BuffObronaTury > 0)
            {
                efektywnaObrona = Math.Max(0, (int)(gracz.Obrona * stan.BuffObronaMnoznik));
                stan.BuffObronaTury -= 1;
                if (stan.BuffObronaTury == 0)
                {
                    stan.BuffObronaMnoznik = 1.0;
                }
            }
            else
            {
                efektywnaObrona = gracz.Obrona;
            }

            efektywnaObrona = Math.Max(0, (int)(efektywnaObrona * stan.FormaObrona));

            int obrazenia = ObliczObrazenia(przeciwnik.Atak, efektywnaObrona, rng);
            if (stan.Garda)
            {
                obrazenia = Math.Max(1, obrazenia / 2);
            }

            if (stan.WrogOslabienieTury > 0)
            {
                obrazenia = Math.Max(1, (int)(obrazenia * 0.5));
                stan.WrogOslabienieTury -= 1;
            }

            if (stan.UnikAktywny)
            {
                stan.UnikAktywny = false;
                if (rng.Losowa() < stan.UnikSzansa)
                {
                    konsola.Pisz($"  💨  Uniknąłeś ataku {przeciwnik.Nazwa}!");
                    PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                    return;
                }

                konsola.Pisz($"  💨  Próbowałeś uniknąć, ale {przeciwnik.Nazwa} trafił!");
            }

            if (stan.FormaUnik > 0 && rng.Losowa() < stan.FormaUnik)
            {
                konsola.Pisz($"  Unikasz ataku {przeciwnik.Nazwa} w postaci {stan.Forma}!");
                PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                return;
            }

            double pasywnyUnik = Przetrwanie.BezUniku(gracz)
                ? 0.0
                : Atrybuty.SzansaUnikuZrecznosc(gracz);
            if (pasywnyUnik > 0 && rng.Losowa() < pasywnyUnik)
            {
                konsola.Pisz($"  💨  Zręczność! Unikasz ciosu {przeciwnik.Nazwa}!");
                PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                return;
            }

            if (stan.TarczaRunowa > 0)
            {
                int absorbcja = Math.Min(stan.TarczaRunowa, obrazenia);
                stan.TarczaRunowa -= absorbcja;
                obrazenia -= absorbcja;
                if (absorbcja > 0)
                {
                    konsola.Pisz($"  🔮  Tarcza runowa absorbuje {absorbcja} obrażeń!"
                               + $" (Pozostało: {stan.TarczaRunowa})");
                }
            }

            if (obrazenia <= 0)
            {
                konsola.Pisz($"  🔮  Tarcza runowa całkowicie zablokowała atak {przeciwnik.Nazwa}!");
                PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                return;
            }

            gracz.Hp = Math.Max(0, gracz.Hp - obrazenia);
            string garda = stan.Garda ? "  (garda)" : "";
            konsola.Pisz($"  💀  {przeciwnik.Nazwa} atakuje cię! Otrzymujesz {obrazenia} obrażeń.{garda}");

            if (SprobujOcalic(gracz, stan, konsola))
            {
                PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
                return;
            }

            string rana = Przetrwanie.MozeZranic(gracz, obrazenia, stan.JestBoss, rng);
            if (!string.IsNullOrEmpty(rana))
            {
                konsola.Pisz(rana);
            }

            // Szansa wroga na nałożenie statusu (bossowie 2× szansa).
            double szansaStatus = stan.JestBoss ? 0.20 : 0.10;
            if (gracz.Zyje() && rng.Losowa() < szansaStatus)
            {
                string status = rng.Wybierz(StatusyWroga);
                if (status == "trucizna" && stan.GraczTruciznaTury == 0)
                {
                    stan.GraczTruciznaTury = 3;
                    stan.GraczTruciznaObrazenia = stan.JestBoss ? 14 : 8;
                    konsola.Pisz($"  ⚗  {przeciwnik.Nazwa} cię zatruł! Tracisz "
                               + $"{stan.GraczTruciznaObrazenia} HP na turę przez 3 tury.");
                }
                else if (status == "krwawienie" && stan.GraczKrwawienieTury == 0)
                {
                    stan.GraczKrwawienieTury = 3;
                    stan.GraczKrwawienieObrazenia = stan.JestBoss ? 12 : 6;
                    konsola.Pisz($"  🩸  {przeciwnik.Nazwa} spowodował krwawienie! Tracisz "
                               + $"{stan.GraczKrwawienieObrazenia} HP na turę przez 3 tury.");
                }
                else if (status == "ogluszone" && stan.GraczOgluszoneTury == 0)
                {
                    // Odporność na ogłuszenie daje pochodzenie — nie ma go w porcie.
                    stan.GraczOgluszoneTury = 1;
                    konsola.Pisz($"  ❄  {przeciwnik.Nazwa} ogłuszył cię! Pomijasz następną turę!");
                }
            }

            PoTurzeWroga(gracz, przeciwnik, stan, konsola, rng);
        }

        // ------------------------------------------------------------------ //
        //  Koniec rundy                                                        //
        // ------------------------------------------------------------------ //

        private static void KoniecRundy(StanWalki stan, Gracz gracz, IKonsola konsola)
        {
            if (stan.BuffAtakTury > 0)
            {
                stan.BuffAtakTury -= 1;
                if (stan.BuffAtakTury == 0)
                {
                    stan.BuffAtakMnoznik = 1.0;
                    if (stan.LeczenieZablokowane)
                    {
                        stan.LeczenieZablokowane = false;
                        konsola.Pisz("  Szał berserka minął. Możesz znów się leczyć.");
                    }
                }
            }

            if (stan.RegeneracjaTury > 0)
            {
                int wyleczone = Math.Min(stan.RegeneracjaHp, gracz.MaxHp - gracz.Hp);
                gracz.Hp += wyleczone;
                stan.RegeneracjaTury -= 1;
                konsola.Pisz($"  🌱  Regeneracja! Odnawiasz {wyleczone} HP."
                           + $" (Pozostało tur: {stan.RegeneracjaTury})");
            }

            if (stan.GraczTruciznaTury > 0)
            {
                int dam = Math.Max(1, stan.GraczTruciznaObrazenia);
                gracz.Hp = Math.Max(0, gracz.Hp - dam);
                stan.GraczTruciznaTury -= 1;
                konsola.Pisz($"  ⚗  Trucizna działa! Tracisz {dam} HP."
                           + $" (Pozostało tur: {stan.GraczTruciznaTury})");
            }

            if (stan.GraczKrwawienieTury > 0)
            {
                int dam = Math.Max(1, stan.GraczKrwawienieObrazenia);
                gracz.Hp = Math.Max(0, gracz.Hp - dam);
                stan.GraczKrwawienieTury -= 1;
                konsola.Pisz($"  🩸  Krwawisz! Tracisz {dam} HP."
                           + $" (Pozostało tur: {stan.GraczKrwawienieTury})");
            }

            if (stan.OlejTury > 0)
            {
                stan.OlejTury -= 1;
                if (stan.OlejTury == 0)
                {
                    konsola.Pisz("  🛢  Olej na ostrzu wypalił się.");
                }
            }

            if (stan.EliksirSilyTury > 0)
            {
                stan.EliksirSilyTury -= 1;
                if (stan.EliksirSilyTury == 0)
                {
                    konsola.Pisz("  💪  Eliksir siły przestaje działać.");
                }
            }

            foreach (string klucz in stan.Cd.Keys.ToList())
            {
                if (stan.Cd[klucz] > 0)
                {
                    stan.Cd[klucz] -= 1;
                    if (stan.Cd[klucz] <= 0)
                    {
                        stan.Cd.Remove(klucz);
                    }
                }
            }

            stan.Tura += 1;
        }

        // ------------------------------------------------------------------ //
        //  Wyświetlanie stanu                                                  //
        // ------------------------------------------------------------------ //

        private static void WyswietlStanWalki(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                              IKonsola konsola)
        {
            Utils.Linia(konsola);
            konsola.Pisz($"  {przeciwnik}");

            string mana = "";
            if (gracz.MaxMana > 0)
            {
                mana = $"   🔮 {gracz.Mana}/{gracz.MaxMana} {gracz.PasekMany()}";
            }

            konsola.Pisz($"  🧙 {gracz.Imie}  ❤️ {gracz.Hp}/{gracz.MaxHp} {gracz.PasekHp()}{mana}");

            var efekty = new List<string>();
            if (stan.BuffAtakTury > 0)
            {
                efekty.Add($"Atak ×{stan.BuffAtakMnoznik:0.0} ({stan.BuffAtakTury} tur)");
            }

            if (stan.BuffObronaTury > 0)
            {
                efekty.Add($"Obrona ×{stan.BuffObronaMnoznik:0.0} ({stan.BuffObronaTury} tur)");
            }

            if (stan.TarczaRunowa > 0)
            {
                efekty.Add($"Tarcza runowa ({stan.TarczaRunowa} HP)");
            }

            if (stan.LeczenieZablokowane)
            {
                efekty.Add("Leczenie zablokowane");
            }

            if (stan.NastepnyAtakMnoznik > 1.0)
            {
                efekty.Add($"Następny atak ×{stan.NastepnyAtakMnoznik:0.0}");
            }

            if (stan.GraczTruciznaTury > 0)
            {
                efekty.Add($"⚗ Zatruty ({stan.GraczTruciznaTury} tur, -{stan.GraczTruciznaObrazenia} HP)");
            }

            if (stan.GraczKrwawienieTury > 0)
            {
                efekty.Add($"🩸 Krwawienie ({stan.GraczKrwawienieTury} tur, -{stan.GraczKrwawienieObrazenia} HP)");
            }

            if (stan.GraczOgluszoneTury > 0)
            {
                efekty.Add($"❄ Ogłuszony ({stan.GraczOgluszoneTury} tur)");
            }

            if (stan.WrogTruciznaTury > 0)
            {
                efekty.Add($"{przeciwnik.Nazwa} zatruty ({stan.WrogTruciznaTury} tur)");
            }

            if (stan.WrogOgluszoneTury > 0)
            {
                efekty.Add($"{przeciwnik.Nazwa} ogłuszony ({stan.WrogOgluszoneTury} tur)");
            }

            if (stan.WrogPodpalony > 0)
            {
                efekty.Add($"🔥 {przeciwnik.Nazwa} płonie ({stan.WrogPodpalony} tur)");
            }

            if (stan.OlejTury > 0)
            {
                efekty.Add($"🛢 Płonące ostrze ({stan.OlejTury} tur)");
            }

            if (stan.EliksirSilyTury > 0)
            {
                efekty.Add($"💪 Eliksir siły ({stan.EliksirSilyTury} tur)");
            }

            if (stan.OdpornoscOgnia)
            {
                efekty.Add("🧯 Odporność na ogień");
            }

            if (stan.JestBoss)
            {
                efekty.Add("⚠ BOSS!");
            }

            if (efekty.Count > 0)
            {
                konsola.Pisz($"  Efekty: {string.Join(", ", efekty)}");
            }

            string stanZdrowia = Przetrwanie.OpisStanu(gracz);
            if (stanZdrowia != "zdrowy")
            {
                konsola.Pisz($"  Twój stan: {stanZdrowia}");
            }

            PokazWiedzeOWrogu(gracz, przeciwnik, stan, konsola);
            Utils.Linia(konsola);
        }

        // ------------------------------------------------------------------ //
        //  Tura gracza                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Submenu przedmiotów. True = gracz zużył turę.</summary>
        private static bool MenuPrzedmiotow(Gracz gracz, StanWalki stan, IKonsola konsola)
        {
            while (true)
            {
                konsola.Pisz("\n  === 🧪 PRZEDMIOTY ===");
                konsola.Pisz($"  [1] 🧪  Mikstura leczenia ({gracz.Mikstury}) — +40 HP");
                konsola.Pisz($"  [2] 💚  Mikstura większa ({gracz.MiksturyDuze}) — +80 HP");
                if (gracz.MaxMana > 0)
                {
                    konsola.Pisz($"  [3] 🔮  Mikstura many ({gracz.MiksturyMany}) — +30 many");
                }

                konsola.Pisz($"  [4] 🧴  Antidotum ({gracz.Antidota}) — zdejmuje truciznę i krwawienie");
                konsola.Pisz("  [0] ↩  Wróć\n");

                string wybor = (konsola.Czytaj("  Wybierz przedmiot: ") ?? "").Trim();
                if (wybor == "0")
                {
                    return false;
                }

                if (wybor == "1" || wybor == "2")
                {
                    if (stan.LeczenieZablokowane)
                    {
                        konsola.Pisz("  Nie możesz się leczyć podczas szału berserka!");
                        continue;
                    }

                    if (wybor == "1")
                    {
                        if (gracz.Mikstury <= 0)
                        {
                            konsola.Pisz("  Nie masz mikstur leczenia!");
                            continue;
                        }

                        konsola.Pisz($"\n  🧪  {gracz.UzyjMiksture()}");
                    }
                    else
                    {
                        if (gracz.MiksturyDuze <= 0)
                        {
                            konsola.Pisz("  Nie masz większych mikstur!");
                            continue;
                        }

                        konsola.Pisz($"\n  🧪  {gracz.UzyjMikstureDuza()}");
                    }

                    return true;
                }

                if (wybor == "3")
                {
                    if (gracz.MaxMana <= 0)
                    {
                        konsola.Pisz("  Twoja klasa nie korzysta z many.");
                        continue;
                    }

                    if (gracz.MiksturyMany <= 0)
                    {
                        konsola.Pisz("  Nie masz mikstur many!");
                        continue;
                    }

                    konsola.Pisz($"\n  🔮  {gracz.UzyjMiksktureMany()}");
                    return true;
                }

                if (wybor == "4")
                {
                    string msg = gracz.UzyjAntidotum();
                    if (msg == null)
                    {
                        konsola.Pisz("  Nie masz antidotum!");
                        continue;
                    }

                    stan.GraczTruciznaTury = 0;
                    stan.GraczKrwawienieTury = 0;
                    konsola.Pisz($"\n  ⚗  {msg}");
                    konsola.Pisz("  Trucizna i krwawienie ustępują.");
                    return true;
                }

                konsola.Pisz("  Nieprawidłowy wybór.");
            }
        }

        /// <summary>Tura gracza. Zwraca „wygrana”, „ucieczka” albo null (walka trwa).</summary>
        private static string TuraGracza(Gracz gracz, Przeciwnik przeciwnik, StanWalki stan,
                                         IKonsola konsola, Losowanie rng)
        {
            while (true)
            {
                // Czyścimy i rysujemy od nowa przy każdym obrocie pętli, tak samo
                // jak menu obozu. Inaczej po nieprawidłowym wyborze na ekranie
                // zostaje drugi komplet plakietek, a pasek życia wroga pokazuje
                // stan sprzed ciosu.
                konsola.Wyczysc();
                WyswietlStanWalki(gracz, przeciwnik, stan, konsola);
                PokazZapowiedz(przeciwnik, stan, konsola);
                konsola.Pisz("\n  Co robisz?");
                konsola.Pisz("  [1] ⚔  Atakuj");
                if (stan.LeczenieZablokowane)
                {
                    konsola.Pisz("  [2] 🧪  Przedmioty — leczenie ZABLOKOWANE (szał berserka)");
                }
                else
                {
                    konsola.Pisz($"  [2] 🧪  Przedmioty (mikstury: {gracz.Mikstury})");
                }

                konsola.Pisz("  [3] ✨  Umiejętności — dojdą w kolejnym etapie portu");
                konsola.Pisz($"  [4] 🏃  Uciekaj (szansa {(int)(SzansaUcieczki(gracz, stan) * 100)}%)");
                konsola.Pisz("  [5] 🛡  Garda — połowa obrażeń w tej turze, potem kontra");

                string wybor = (konsola.Czytaj("\n  Twój wybór: ") ?? "").Trim();

                if (wybor == "1")
                {
                    int efektywnyAtak = Math.Max(1, (int)(gracz.Atak
                        * stan.BuffAtakMnoznik
                        * stan.NastepnyAtakMnoznik
                        * stan.FormaAtak
                        * MnoznikGracza(gracz, stan)));
                    stan.NastepnyAtakMnoznik = 1.0;

                    bool krit;
                    int obrazenia = AtakZKrytem(efektywnyAtak, przeciwnik.Obrona, gracz.Klasa,
                                                stan, gracz, rng, out krit);
                    przeciwnik.Hp -= obrazenia;
                    if (krit)
                    {
                        konsola.Pisz($"\n  ⚡ KRYTYCZNE TRAFIENIE! Atakujesz {przeciwnik.Nazwa}! "
                                   + $"Zadajesz {obrazenia} obrażeń!");
                    }
                    else
                    {
                        konsola.Pisz($"\n  ⚔  Atakujesz {przeciwnik.Nazwa}! Zadajesz {obrazenia} obrażeń.");
                    }

                    if (stan.OlejTury > 0 && przeciwnik.Zyje())
                    {
                        int ogien = (int)((8 + 3 * gracz.Poziom)
                                          * Wrogowie.MnoznikTypu(przeciwnik, "ogien"));
                        przeciwnik.Hp -= ogien;
                        konsola.Pisz($"  🛢  Płonące ostrze dokłada {ogien} obrażeń od ognia!"
                                   + DopisekTypu(przeciwnik, "ogien"));
                        Podpal(przeciwnik, stan, 1, konsola);
                    }

                    return przeciwnik.Zyje() ? null : "wygrana";
                }

                if (wybor == "2")
                {
                    if (MenuPrzedmiotow(gracz, stan, konsola))
                    {
                        return null;
                    }

                    continue;
                }

                if (wybor == "3")
                {
                    // Tutaj wchodzi _menu_umiejetnosci() razem z portem skills.py.
                    konsola.Pisz("  Umiejętności klasowe jeszcze nie weszły do portu.");
                    Utils.NacisnijEnter(konsola);
                    continue;
                }

                if (wybor == "4")
                {
                    if (rng.Losowa() < SzansaUcieczki(gracz, stan))
                    {
                        konsola.Pisz("\n  🏃  Udało ci się uciec!");
                        return "ucieczka";
                    }

                    konsola.Pisz("\n  🏃  Nie udało się uciec — przeciwnik blokuje drogę!");
                    return null;
                }

                if (wybor == "5")
                {
                    stan.Garda = true;
                    konsola.Pisz("\n  🛡  Przyjmujesz gardę. Czekasz na jego ruch.");
                    return null;
                }

                konsola.Pisz("  Nieprawidłowy wybór. Wpisz 1–5.");
                Utils.NacisnijEnter(konsola);
            }
        }

        // ------------------------------------------------------------------ //
        //  Nagrody                                                             //
        // ------------------------------------------------------------------ //

        private static void OdnowManePoWalce(Gracz gracz)
        {
            if (gracz.MaxMana > 0)
            {
                gracz.Mana = Math.Min(gracz.Mana + 20, gracz.MaxMana);
            }
        }

        private static void ZakonczenieWygrana(Gracz gracz, Przeciwnik przeciwnik, bool jestBoss,
                                               IKonsola konsola, Losowanie rng)
        {
            int zloto = przeciwnik.LosoweZloto(rng);
            if (jestBoss)
            {
                gracz.Mikstury += 1;
            }

            gracz.Zloto += zloto;
            gracz.RejestrujWalke(przeciwnik.Nazwa);
            List<string> komunikaty = gracz.ZdobadzExp(przeciwnik.ExpNagroda);
            // Tutaj w wersji pythonowej dochodzą jeszcze questy, łupy i składniki.
            OdnowManePoWalce(gracz);

            Utils.Linia(konsola);
            if (jestBoss)
            {
                konsola.Pisz($"\n  🏆🏆  BOSS POKONANY: {przeciwnik.Nazwa}!");
                konsola.Pisz("  Legendarny łup:");
                konsola.Pisz($"  💰  Zdobyłeś {zloto} złota!");
                konsola.Pisz("  🧪  Bonus: 1 mikstura leczenia!");
            }
            else
            {
                konsola.Pisz($"\n  🏆  Pokonałeś {przeciwnik.Nazwa}!");
                konsola.Pisz($"  💰  Zdobyłeś {zloto} złota!");
            }

            foreach (string msg in komunikaty)
            {
                konsola.Pisz($"  {msg}");
            }

            Utils.NacisnijEnter(konsola);
        }

        // ------------------------------------------------------------------ //
        //  Główna pętla                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Główna pętla walki. Zwraca „wygrana”, „przegrana” albo „ucieczka”.
        /// </summary>
        public static string PrzeprowadzWalke(Gracz gracz, IKonsola konsola, Losowanie rng,
                                              string biom = null, bool jestBoss = false,
                                              Przeciwnik przeciwnik = null)
        {
            if (przeciwnik == null)
            {
                przeciwnik = jestBoss
                    ? Wrogowie.LosujBossa(rng, gracz.MapaGen, gracz.TrybTrudnosci)
                    : Wrogowie.LosujPrzeciwnika(rng, biom, gracz.MapaGen, gracz.TrybTrudnosci);
            }

            var stan = new StanWalki { JestBoss = jestBoss, Wrog = przeciwnik };

            konsola.Wyczysc();
            if (jestBoss)
            {
                konsola.Pisz("\n  *** ⚠ BOSS! ***");
                konsola.Pisz($"  {przeciwnik.Opis}");
                konsola.Pisz($"  Stoisz przed: {Ikony.Wrog(przeciwnik.Nazwa)} {przeciwnik.Nazwa}!\n");
            }
            else
            {
                konsola.Pisz("\n  *** ⚔ STARCIE! ***");
                if (!string.IsNullOrEmpty(biom))
                {
                    konsola.Pisz($"  Biom: {Ikony.EtykietaBiomu(biom)}");
                }

                konsola.Pisz($"  {przeciwnik.Opis}");
                konsola.Pisz($"  Napotkałeś: {Ikony.Wrog(przeciwnik.Nazwa)} {przeciwnik.Nazwa}!\n");
            }

            Utils.NacisnijEnter(konsola);

            while (gracz.Zyje() && przeciwnik.Zyje())
            {
                string wynik;
                if (stan.GraczOgluszoneTury > 0)
                {
                    // Tura gracza rysuje stan sama; przy pominiętej turze musimy
                    // pokazać go tutaj, żeby gracz wiedział, co się dzieje.
                    konsola.Wyczysc();
                    WyswietlStanWalki(gracz, przeciwnik, stan, konsola);
                    stan.GraczOgluszoneTury -= 1;
                    konsola.Pisz("  ❄  Jesteś ogłuszony! Pomijasz turę.");
                    wynik = null;
                }
                else
                {
                    wynik = TuraGracza(gracz, przeciwnik, stan, konsola, rng);
                }

                if (wynik == "ucieczka")
                {
                    OdnowManePoWalce(gracz);
                    return "ucieczka";
                }

                // Tutaj w wersji pythonowej chodzą jeszcze tury towarzysza i przyzwania.

                if (wynik == "wygrana")
                {
                    ZakonczenieWygrana(gracz, przeciwnik, jestBoss, konsola, rng);
                    return "wygrana";
                }

                if (przeciwnik.Zyje())
                {
                    TuraPrzeciwnika(gracz, przeciwnik, stan, konsola, rng);
                }

                // Wróg mógł paść od trucizny albo ognia w swojej turze.
                if (!przeciwnik.Zyje())
                {
                    ZakonczenieWygrana(gracz, przeciwnik, jestBoss, konsola, rng);
                    return "wygrana";
                }

                if (!gracz.Zyje())
                {
                    konsola.Pisz($"\n  💀  Zostałeś pokonany przez {przeciwnik.Nazwa}...");
                    Utils.NacisnijEnter(konsola);
                    return "przegrana";
                }

                KoniecRundy(stan, gracz, konsola);

                if (!gracz.Zyje())
                {
                    konsola.Pisz("\n  💀  Padłeś od zatrucia lub krwawienia...");
                    Utils.NacisnijEnter(konsola);
                    return "przegrana";
                }

                Utils.NacisnijEnter(konsola);
            }

            return "wygrana";
        }
    }
}
