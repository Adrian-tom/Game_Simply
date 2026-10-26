using System;
using System.Collections.Generic;

namespace GraSimply.Logika
{
    /// <summary>
    /// Ruch osadników po placu — odpowiednik <c>game/ruch.py</c>: siatka,
    /// ścieżki A*, rezerwacje pól i rytm doby.
    ///
    /// Scena osady nie liczy ruchu sama. Dostaje od logiki zbiór zablokowanych
    /// pól i polecenia „idź tu”, a ta część — bez UnityEngine i bez rysowania —
    /// daje się testować zwykłym dotnetem i porównywać z Pythonem co do pozycji.
    ///
    /// Pole to para całkowitych <c>(X, Y)</c> na siatce <see cref="Siatka.Domyslna"/>
    /// × <see cref="Siatka.Domyslna"/>; osadnik stoi na środku pola, a w drodze
    /// ma pozycję ułamkową.
    /// </summary>
    public static class RytmDnia
    {
        /// <summary>Sekund sceny na dobę. Czas sceny jest osobny od dnia świata.</summary>
        public const double DlugoscDnia = 90.0;

        /// <summary>Ułamki doby, od których zaczyna się kolejna część dnia.</summary>
        private static readonly (double Start, string Nazwa)[] Fazy =
        {
            (0.00, "rano"),
            (0.10, "praca"),
            (0.62, "wieczor"),
            (0.82, "noc"),
        };

        public static double UlamekDoby(double t, double dlugosc = DlugoscDnia)
        {
            double r = t % dlugosc;
            if (r < 0)
            {
                r += dlugosc;
            }
            return r / dlugosc;
        }

        /// <summary>Część doby w chwili <paramref name="t"/>: rano, praca, wieczor, noc.</summary>
        public static string Faza(double t, double dlugosc = DlugoscDnia)
        {
            double u = UlamekDoby(t, dlugosc);
            string nazwa = Fazy[0].Nazwa;
            foreach ((double start, string klucz) in Fazy)
            {
                if (u >= start)
                {
                    nazwa = klucz;
                }
            }
            return nazwa;
        }

        /// <summary>Jasność 0 (głucha noc) … 1 (południe), płynna — do oświetlenia sceny.</summary>
        public static double Swiatlo(double t, double dlugosc = DlugoscDnia)
        {
            double u = UlamekDoby(t, dlugosc);
            // Cosinus ustawiony tak, żeby najciemniej było w środku nocy (u ≈ 0,91),
            // a najjaśniej w środku pracy; wzmocniony i obcięty, bo dzień ma trwać
            // dłużej niż noc, a południe — być płaskim szczytem.
            double surowe = 0.5 - 0.5 * Math.Cos((u + 0.09) * Math.PI * 2);
            return Math.Max(0.0, Math.Min(1.0, 0.08 + 1.2 * surowe));
        }

        /// <summary>
        /// Dokąd i po co idzie osadnik o tej porze doby. Chory nie opuszcza
        /// okolic domu. Wieczorem część siada przy ogniu, część idzie do
        /// tawerny (o ile stoi) — wybór jest stały dla imienia.
        /// </summary>
        public static ((int X, int Y) Pole, string Czynnosc) Zadanie(
            Osadnik osadnik, (int X, int Y) stanowisko, (int X, int Y) dom,
            (int X, int Y) ognisko, (int X, int Y)? tawerna, string pora)
        {
            if (osadnik.Chory > 0)
            {
                return (dom, "odpoczynek");
            }
            if (pora == "rano" || pora == "praca")
            {
                return (stanowisko, pora == "praca" ? "praca" : "marsz");
            }
            if (pora == "wieczor")
            {
                if (tawerna.HasValue && LosImienia(osadnik.Imie ?? "") % 2 == 0)
                {
                    return (tawerna.Value, "tawerna");
                }
                return (ognisko, "ognisko");
            }
            return (dom, "sen");
        }

        /// <summary>Stała liczba z imienia (<c>GetHashCode</c> zmienia się między uruchomieniami).</summary>
        public static long LosImienia(string imie)
        {
            long suma = 0;
            for (int i = 0; i < imie.Length; i++)
            {
                suma += (i + 1) * (long)imie[i];
            }
            return suma;
        }
    }

    /// <summary>Siatka przeszkód placu.</summary>
    public sealed class Siatka
    {
        public const int Domyslna = 7;

        public readonly int Rozmiar;
        public HashSet<(int X, int Y)> Zablokowane = new HashSet<(int, int)>();

        // Najpierw proste — przewidywalna kolejność przeglądania sąsiadów.
        internal static readonly (int Dx, int Dy)[] Kierunki =
        {
            (0, -1), (1, 0), (0, 1), (-1, 0),
            (1, -1), (1, 1), (-1, 1), (-1, -1),
        };

        /// <summary>Gdzie stawać przy budynku: od frontu (na ekranie „w dół”), potem z boków i z tyłu.</summary>
        internal static readonly (int Dx, int Dy)[] PrzyCelu =
        {
            (0, 1), (1, 0), (-1, 0), (0, -1),
            (1, 1), (-1, 1), (1, -1), (-1, -1),
        };

        public Siatka(int rozmiar = Domyslna, IEnumerable<(int X, int Y)> zablokowane = null)
        {
            Rozmiar = rozmiar;
            if (zablokowane != null)
            {
                Zablokowane = new HashSet<(int, int)>(zablokowane);
            }
        }

        public bool WSiatce((int X, int Y) p)
        {
            return p.X >= 0 && p.X < Rozmiar && p.Y >= 0 && p.Y < Rozmiar;
        }

        public bool Wolne((int X, int Y) p, ICollection<(int X, int Y)> dodatkowo = null)
        {
            return WSiatce(p) && !Zablokowane.Contains(p)
                   && (dodatkowo == null || !dodatkowo.Contains(p));
        }

        /// <summary>Sąsiednie wolne pola z kosztem; po skosie tylko, gdy nie ścina rogu.</summary>
        public IEnumerable<((int X, int Y) Pole, double Koszt)> Sasiedzi(
            (int X, int Y) p, ICollection<(int X, int Y)> dodatkowo = null)
        {
            foreach ((int dx, int dy) in Kierunki)
            {
                (int X, int Y) n = (p.X + dx, p.Y + dy);
                if (!Wolne(n, dodatkowo))
                {
                    continue;
                }
                if (dx != 0 && dy != 0
                    && !(Wolne((p.X + dx, p.Y), dodatkowo) && Wolne((p.X, p.Y + dy), dodatkowo)))
                {
                    continue;
                }
                yield return (n, dx != 0 && dy != 0 ? Math.Sqrt(2) : 1.0);
            }
        }

        /// <summary>Najbliższe wolne pole (łącznie z <paramref name="p"/>), gdy samo jest zajęte.</summary>
        public (int X, int Y)? NajblizszeWolne((int X, int Y) p)
        {
            if (Wolne(p))
            {
                return p;
            }
            for (int r = 1; r <= Rozmiar; r++)
            {
                var pierscien = new List<(int X, int Y)>();
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r)
                        {
                            pierscien.Add((p.X + dx, p.Y + dy));
                        }
                    }
                }
                pierscien.Sort((a, b) =>
                {
                    int da = Math.Abs(a.X - p.X) + Math.Abs(a.Y - p.Y);
                    int db = Math.Abs(b.X - p.X) + Math.Abs(b.Y - p.Y);
                    if (da != db)
                    {
                        return da.CompareTo(db);
                    }
                    return a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
                });
                foreach ((int X, int Y) c in pierscien)
                {
                    if (Wolne(c))
                    {
                        return c;
                    }
                }
            }
            return null;
        }
    }

    public static class Sciezki
    {
        private static double Oktylowa((int X, int Y) a, (int X, int Y) b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return (dx + dy) + (Math.Sqrt(2) - 2) * Math.Min(dx, dy);
        }

        /// <summary>
        /// Najkrótsza ścieżka A* od <paramref name="start"/> do <paramref name="cel"/>
        /// (bez startu) albo null. <paramref name="dodatkowo"/> to pola chwilowo
        /// zajęte (inni osadnicy), omijane tylko w tym wyszukiwaniu. Przy remisach
        /// wygrywa mniejsze pole, więc wynik jest stały — i taki sam jak w Pythonie.
        /// </summary>
        public static List<(int X, int Y)> Znajdz(Siatka siatka, (int X, int Y) start, (int X, int Y) cel,
                                                  ICollection<(int X, int Y)> dodatkowo = null)
        {
            if (start == cel)
            {
                return new List<(int, int)>();
            }
            if (!siatka.Wolne(cel, dodatkowo))
            {
                return null;
            }

            // Kolejka priorytetowa: (f, g, x, y) — krotka porównuje się leksykograficznie,
            // jak w Pythonie. Unity nie ma PriorityQueue, a SortedSet daje to samo.
            var otwarte = new SortedSet<(double F, double G, int X, int Y)>
            {
                (Oktylowa(start, cel), 0.0, start.X, start.Y),
            };
            var koszt = new Dictionary<(int, int), double> { { start, 0.0 } };
            var skad = new Dictionary<(int, int), (int X, int Y)>();
            var zamkniete = new HashSet<(int, int)>();
            while (otwarte.Count > 0)
            {
                (double _, double g, int px, int py) = otwarte.Min;
                otwarte.Remove(otwarte.Min);
                (int X, int Y) p = (px, py);
                if (zamkniete.Contains(p))
                {
                    continue;
                }
                if (p == cel)
                {
                    var wynik = new List<(int X, int Y)> { p };
                    while (skad.TryGetValue(wynik[wynik.Count - 1], out (int X, int Y) poprzednie))
                    {
                        wynik.Add(poprzednie);
                    }
                    wynik.Reverse();
                    wynik.RemoveAt(0);
                    return wynik;
                }
                zamkniete.Add(p);
                foreach (((int X, int Y) n, double c) in siatka.Sasiedzi(p, dodatkowo))
                {
                    double ng = g + c;
                    if (!koszt.TryGetValue(n, out double stary) || ng < stary)
                    {
                        koszt[n] = ng;
                        skad[n] = p;
                        otwarte.Add((ng + Oktylowa(n, cel), ng, n.X, n.Y));
                    }
                }
            }
            return null;
        }
    }

    /// <summary>Jedna postać na placu: gdzie stoi, dokąd idzie i co robi.</summary>
    public sealed class PostacRuchu
    {
        public readonly string Imie;
        public double X;
        public double Y;

        /// <summary>Ostatnie osiągnięte pole.</summary>
        public (int X, int Y) Pole;

        /// <summary>Pole, do którego właśnie idzie (null = stoi).</summary>
        public (int X, int Y)? Nastepne;

        public List<(int X, int Y)> Trasa = new List<(int, int)>();

        /// <summary>Pole docelowe — miejsce do stania.</summary>
        public (int X, int Y)? Cel;

        /// <summary>Pole, o które poproszono (np. budynek).</summary>
        public (int X, int Y)? Zamiar;

        public string Czynnosc = "stoi";

        /// <summary>Przebyta droga — napędza animację kroku.</summary>
        public double Dystans;

        /// <summary>Jak długo czeka na zajęte pole.</summary>
        public double Postoj;

        public (double X, double Y) Kierunek = (0.0, 1.0);

        public PostacRuchu(string imie, (int X, int Y) pole)
        {
            Imie = imie;
            X = pole.X;
            Y = pole.Y;
            Pole = pole;
        }

        public bool Idzie => Nastepne.HasValue;
    }

    /// <summary>Wszystkie postacie placu, ich rezerwacje i krok symulacji.</summary>
    public sealed class SymulacjaRuchu
    {
        /// <summary>Po tylu sekundach postoju szukamy obejścia.</summary>
        public const double CzekanieNaPrzejscie = 0.8;

        /// <summary>Pól na sekundę.</summary>
        public const double Predkosc = 1.25;

        public readonly Siatka Siatka;

        // Kolejność dodawania — jak słownik w Pythonie; od niej zależy, kogo
        // przebudowa układu przesunie pierwszego.
        private readonly List<string> _kolejnosc = new List<string>();
        private readonly Dictionary<string, PostacRuchu> _postacie = new Dictionary<string, PostacRuchu>();

        /// <summary>Pole → kto na nim stoi albo do niego idzie.</summary>
        private Dictionary<(int, int), string> _rez = new Dictionary<(int, int), string>();

        public SymulacjaRuchu(int rozmiar = Siatka.Domyslna)
        {
            Siatka = new Siatka(rozmiar);
        }

        public IReadOnlyDictionary<string, PostacRuchu> Postacie => _postacie;

        public IReadOnlyList<string> Imiona => _kolejnosc;

        // -- zarządzanie ------------------------------------------------- //

        public PostacRuchu Dodaj(string imie, (int X, int Y) pole)
        {
            // Gdy nie ma wolnego miejsca przy celu (zastawiony plac), staje na najbliższym
            // wolnym polu choćby zajętym przez innego — byle nie wewnątrz budynku.
            pole = WolneMiejsce(pole, imie) ?? Siatka.NajblizszeWolne(pole) ?? pole;
            var p = new PostacRuchu(imie, pole);
            _postacie[imie] = p;
            _kolejnosc.Remove(imie);
            _kolejnosc.Add(imie);
            _rez[pole] = imie;
            return p;
        }

        public void Usun(string imie)
        {
            if (!_postacie.Remove(imie))
            {
                return;
            }
            _kolejnosc.Remove(imie);
            var doUsuniecia = new List<(int, int)>();
            foreach (KeyValuePair<(int, int), string> para in _rez)
            {
                if (para.Value == imie)
                {
                    doUsuniecia.Add(para.Key);
                }
            }
            foreach ((int, int) pole in doUsuniecia)
            {
                _rez.Remove(pole);
            }
        }

        /// <summary>Nowy układ przeszkód (ktoś postawił budynek). Zepchnięci z pola schodzą na wolne.</summary>
        public void Przebuduj(ISet<(int X, int Y)> zablokowane)
        {
            if (Siatka.Zablokowane.SetEquals(zablokowane))
            {
                return;
            }
            Siatka.Zablokowane = new HashSet<(int, int)>(zablokowane);
            foreach (string imie in _kolejnosc)
            {
                PostacRuchu p = _postacie[imie];
                bool zajete = zablokowane.Contains(p.Pole)
                              || (p.Nastepne.HasValue && zablokowane.Contains(p.Nastepne.Value));
                if (zajete)
                {
                    var nowe = new Dictionary<(int, int), string>();
                    foreach (KeyValuePair<(int, int), string> para in _rez)
                    {
                        if (para.Value != p.Imie)
                        {
                            nowe[para.Key] = para.Value;
                        }
                    }
                    _rez = nowe;
                    (int X, int Y)? miejsce = WolneMiejsce(p.Pole, p.Imie) ?? Siatka.NajblizszeWolne(p.Pole);
                    if (miejsce.HasValue)
                    {
                        p.Pole = miejsce.Value;
                        p.X = miejsce.Value.X;
                        p.Y = miejsce.Value.Y;
                        _rez[miejsce.Value] = p.Imie;
                    }
                    p.Nastepne = null;
                    p.Trasa = new List<(int, int)>();
                }
                p.Cel = null;       // cele przeliczy następne polecenie
                p.Zamiar = null;
            }
        }

        // -- polecenia --------------------------------------------------- //

        /// <summary>Poślij postać w okolice <paramref name="pole"/> (może być zajęte budynkiem).</summary>
        public void UstawCel(string imie, (int X, int Y) pole, string czynnosc = "stoi")
        {
            PostacRuchu p = _postacie[imie];
            p.Czynnosc = czynnosc;
            if (p.Zamiar.HasValue && p.Zamiar.Value == pole && p.Cel.HasValue)
            {
                return;
            }
            (int X, int Y) start = p.Nastepne ?? p.Pole;
            // Miejsce musi być osiągalne: bez tego zastawiony plac dałby wolne pole
            // za murem, do którego nie ma drogi, i postać stałaby w miejscu.
            (int X, int Y)? miejsce = WolneMiejsce(pole, imie, Osiagalne(start));
            p.Zamiar = pole;
            if (!miejsce.HasValue)
            {
                return;
            }
            List<(int X, int Y)> trasa = Sciezki.Znajdz(Siatka, start, miejsce.Value);
            if (trasa == null)
            {
                return;
            }
            ZwolnijCel(p);
            p.Cel = miejsce;
            p.Trasa = trasa;
            if (!p.Idzie && trasa.Count == 0)
            {
                _rez[p.Pole] = imie;
            }
        }

        private void ZwolnijCel(PostacRuchu p)
        {
            if (!p.Cel.HasValue)
            {
                return;
            }
            (int X, int Y) cel = p.Cel.Value;
            if (_rez.TryGetValue(cel, out string kto) && kto == p.Imie
                && cel != p.Pole && !(p.Nastepne.HasValue && p.Nastepne.Value == cel))
            {
                _rez.Remove(cel);
            }
        }

        /// <summary>Pola, do których da się dojść ze <paramref name="start"/> po wolnych polach (bez rezerwacji).</summary>
        private HashSet<(int X, int Y)> Osiagalne((int X, int Y) start)
        {
            var widziane = new HashSet<(int X, int Y)> { start };
            var kolejka = new Queue<(int X, int Y)>();
            kolejka.Enqueue(start);
            while (kolejka.Count > 0)
            {
                (int X, int Y) p = kolejka.Dequeue();
                foreach (((int X, int Y) n, double _) in Siatka.Sasiedzi(p))
                {
                    if (widziane.Add(n))
                    {
                        kolejka.Enqueue(n);
                    }
                }
            }
            return widziane;
        }

        /// <summary>
        /// Najbliższe pole przy <paramref name="pole"/>, na którym nikt inny nie stoi i do którego nikt nie idzie.
        /// <paramref name="dostepne"/> zawęża wybór do pól, do których postać może dojść.
        /// </summary>
        private (int X, int Y)? WolneMiejsce((int X, int Y) pole, string imie,
                                             ISet<(int X, int Y)> dostepne = null)
        {
            var cudze = new HashSet<(int X, int Y)>();
            foreach (KeyValuePair<(int, int), string> para in _rez)
            {
                if (para.Value != imie)
                {
                    cudze.Add(para.Key);
                }
            }
            foreach (string inne in _kolejnosc)
            {
                PostacRuchu q = _postacie[inne];
                if (q.Imie != imie && q.Cel.HasValue)
                {
                    cudze.Add(q.Cel.Value);
                }
            }
            var kolejka = new Queue<(int X, int Y)>();
            kolejka.Enqueue(pole);
            var widziane = new HashSet<(int, int)> { pole };
            while (kolejka.Count > 0)
            {
                (int X, int Y) p = kolejka.Dequeue();
                if (Siatka.Wolne(p) && !cudze.Contains(p) && (dostepne == null || dostepne.Contains(p)))
                {
                    return p;
                }
                foreach ((int dx, int dy) in Siatka.PrzyCelu)
                {
                    (int X, int Y) n = (p.X + dx, p.Y + dy);
                    if (!widziane.Contains(n) && Siatka.WSiatce(n))
                    {
                        widziane.Add(n);
                        // Przez zablokowane pole przechodzimy tylko „okiem” — szukamy brzegu budynku.
                        kolejka.Enqueue(n);
                    }
                }
            }
            return null;
        }

        // -- krok -------------------------------------------------------- //

        public void Aktualizuj(double dt)
        {
            var imiona = new List<string>(_kolejnosc);
            imiona.Sort(string.CompareOrdinal);
            foreach (string imie in imiona)
            {
                Krok(_postacie[imie], dt);
            }
        }

        private void Krok(PostacRuchu p, double dt)
        {
            double zostalo = Predkosc * dt;
            while (zostalo > 1e-9)
            {
                if (!p.Nastepne.HasValue)
                {
                    if (p.Trasa.Count == 0)
                    {
                        return;
                    }
                    (int X, int Y) kolejne = p.Trasa[0];
                    if (_rez.TryGetValue(kolejne, out string zajmujacy) && zajmujacy != p.Imie)
                    {
                        p.Postoj += dt;
                        if (p.Postoj >= CzekanieNaPrzejscie)
                        {
                            Obejdz(p);
                            p.Postoj = 0.0;
                        }
                        return;
                    }
                    p.Postoj = 0.0;
                    p.Trasa.RemoveAt(0);
                    p.Nastepne = kolejne;
                    _rez[kolejne] = p.Imie;
                }
                (int X, int Y) cel = p.Nastepne.Value;
                double dx = cel.X - p.X;
                double dy = cel.Y - p.Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > 1e-9)
                {
                    p.Kierunek = (dx / d, dy / d);
                }
                if (d <= zostalo)
                {
                    p.X = cel.X;
                    p.Y = cel.Y;
                    p.Dystans += d;
                    zostalo -= d;
                    if (_rez.TryGetValue(p.Pole, out string kto) && kto == p.Imie && p.Pole != cel)
                    {
                        _rez.Remove(p.Pole);
                    }
                    p.Pole = cel;
                    p.Nastepne = null;
                }
                else
                {
                    p.X += dx / d * zostalo;
                    p.Y += dy / d * zostalo;
                    p.Dystans += zostalo;
                    zostalo = 0.0;
                }
            }
        }

        /// <summary>Ktoś blokuje drogę: policz trasę z pominięciem pól zajętych przez innych.</summary>
        private void Obejdz(PostacRuchu p)
        {
            if (!p.Cel.HasValue)
            {
                return;
            }
            var cudze = new HashSet<(int X, int Y)>();
            foreach (KeyValuePair<(int, int), string> para in _rez)
            {
                if (para.Value != p.Imie)
                {
                    cudze.Add(para.Key);
                }
            }
            List<(int X, int Y)> trasa = Sciezki.Znajdz(Siatka, p.Pole, p.Cel.Value, cudze);
            if (trasa != null)
            {
                p.Trasa = trasa;
                return;
            }
            // Wąskie przejście i nikt nie może się wyminąć. Ustępuje ten, którego
            // imię jest później w alfabecie — drugi dostaje wolną drogę.
            string blokujacy = null;
            if (p.Trasa.Count > 0)
            {
                _rez.TryGetValue(p.Trasa[0], out blokujacy);
            }
            if (blokujacy == null || string.CompareOrdinal(p.Imie, blokujacy) < 0)
            {
                return;
            }
            foreach (((int X, int Y) n, double _) in Siatka.Sasiedzi(p.Pole, cudze))
            {
                List<(int X, int Y)> dalej = Sciezki.Znajdz(Siatka, n, p.Cel.Value);
                if (dalej != null && p.Trasa.Count > 0 && n != p.Trasa[0])
                {
                    var nowa = new List<(int X, int Y)> { n };
                    nowa.AddRange(dalej);
                    p.Trasa = nowa;
                    return;
                }
            }
        }

        // -- odczyt ------------------------------------------------------ //

        public (double X, double Y) Pozycja(string imie)
        {
            PostacRuchu p = _postacie[imie];
            return (p.X, p.Y);
        }

        public bool Dotarl(string imie)
        {
            PostacRuchu p = _postacie[imie];
            return !p.Idzie && p.Trasa.Count == 0 && (!p.Cel.HasValue || p.Pole == p.Cel.Value);
        }
    }
}
