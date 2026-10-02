using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraSimply.Grafika;
using GraSimply.Logika;
using UnityEngine;
using UnityEngine.UI;

namespace GraSimply.Gra
{
    /// <summary>
    /// Spina grę w Unity: uruchamia logikę na osobnym wątku, składa klatkę
    /// z warstwy Grafika i zamienia klawiaturę oraz mysz na odpowiedzi gracza.
    ///
    /// Cała warstwa wizualna to jedna tekstura 1280×720 rysowana w kodzie
    /// (patrz <see cref="Pulpit"/>) plus napisy w zwykłych komponentach Text.
    /// Interfejs budujemy tutaj, a nie w scenie, bo wtedy scena jest jednym
    /// obiektem z tym skryptem — nic się nie rozjeżdża przy zmianie wersji
    /// Unity i nie trzeba klikać po inspektorze.
    ///
    /// Układ i hierarchia tworzą się w <see cref="Awake"/>, więc wystarczy
    /// wrzucić ten komponent na pusty obiekt w scenie i nacisnąć Play.
    /// </summary>
    [AddComponentMenu("Gra Simply/Silnik gry")]
    public sealed class SilnikGry : MonoBehaviour
    {
        [Header("Krój pisma")]
        [Tooltip("Czcionki o stałej szerokości, w kolejności prób. " +
                 "Krój z emoji warto zostawić — bez niego ikony w tekście " +
                 "pokażą się jako puste prostokąty.")]
        public string[] NazwyCzcionek =
        {
            "Consolas", "Segoe UI Emoji", "DejaVu Sans Mono",
            "Liberation Mono", "Courier New", "Menlo", "Monaco",
        };

        [Tooltip("Rozmiar tekstu w konsoli.")]
        public int RozmiarKonsoli = 16;

        [Tooltip("Rozmiar tekstu w HUD.")]
        public int RozmiarHud = 17;

        [Tooltip("Rozmiar nagłówków (imię bohatera, tytuł).")]
        public int RozmiarDuzy = 24;

        [Tooltip("Rozmiar drobnych linii HUD (data, surowce, podpowiedzi).")]
        public int RozmiarMaly = 14;

        [Tooltip("Wyłącz, jeśli zamiast ikon widzisz puste prostokąty — " +
                 "tekst zostanie wtedy bez emoji, ale będzie czytelny.")]
        public bool PokazujEmoji = true;

        [Header("Gra")]
        [Tooltip("Zostaw 0, żeby świat był inny w każdej rozgrywce.")]
        public int ZiarnoLosowania;

        [Tooltip("Ile klatek na sekundę. Scena jest rysowana na procesorze, " +
                 "więc 30 wystarcza i oszczędza baterię.")]
        public int KlatekNaSekunde = 30;

        private Pulpit _pulpit;
        private Texture2D _tekstura;
        private RawImage _obraz;
        private RectTransform _korzen;

        private Font _krojKonsoli;
        private Font _krojHud;
        private Font _krojDuzy;
        private Font _krojMaly;
        private float _szerokoscZnaku = 9f;
        private int _wysokoscWiersza = 19;

        private readonly List<Text> _wiersze = new List<Text>();
        private readonly List<Text> _napisyHud = new List<Text>();
        private Text _znacznikGory;

        private KonsolaKolejkowa _konsola;
        private Ekran _ekran;
        private Task _watekLogiki;
        private Migawka _migawka;
        private Migawka _tytulowa;

        private bool _bladLogiki;
        private string _bufor = "";
        private int _przewiniecie;
        private int _ostatniaWersja = -1;
        private string _ostatniBufor;
        private bool _ostatniKursor;
        private double _czas;

        /// <summary>Klikalne obszary wiersza: prostokąt w pikselach pulpitu i klawisz opcji.</summary>
        private readonly List<(Rect Obszar, string Klawisz)> _klikalne =
            new List<(Rect, string)>();

        // ------------------------------------------------------------------ //
        //  Start                                                              //
        // ------------------------------------------------------------------ //

        private void Awake()
        {
            // Bez wyzerowania vSync Unity ignoruje targetFrameRate i rysuje
            // z czestotliwoscia monitora — scena liczona na procesorze nie ma
            // po co chodzic 144 razy na sekunde.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(15, KlatekNaSekunde);
            _pulpit = new Pulpit();
            _tekstura = new Texture2D(Pulpit.Szerokosc, Pulpit.Wysokosc, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            ZbudujInterfejs();
        }

        private void Start()
        {
            _ekran = new Ekran();
            _konsola = new KonsolaKolejkowa();
            // Migawkę budujemy na wątku logiki, nim ten zablokuje się na pytaniu.
            _konsola.PrzedPytaniem = () =>
            {
                Migawka nowa = _ekran.Migawke();
                if (nowa != null)
                {
                    _migawka = nowa;
                }
                else
                {
                    _migawka = null; // ekran tytułowy
                }
            };

            var rng = ZiarnoLosowania != 0 ? new Losowanie(ZiarnoLosowania) : new Losowanie();
            var gra = new Logika.Gra(_konsola, _ekran, rng);
            _watekLogiki = Task.Run(() =>
            {
                try
                {
                    gra.Uruchom();
                }
                catch (GraZamknieta)
                {
                    // Okno zamknięte w trakcie pytania — normalne wyjście.
                }
                catch (Exception e)
                {
                    // Wyjątek na wątku w tle przepadłby bez śladu, a to najgorszy
                    // rodzaj błędu do szukania. Wypisujemy go i pokazujemy w konsoli gry.
                    Debug.LogException(e);
                    _konsola.Pisz();
                    _konsola.Pisz($"  ‼  Błąd logiki gry: {e.GetType().Name}: {e.Message}");
                    _konsola.Pisz("  Szczegóły są w konsoli Unity.");
                    // Po błędzie nie zamykamy gry od razu — inaczej ten komunikat
                    // mignąłby na jedną klatkę i nikt by go nie przeczytał.
                    _bladLogiki = true;
                }
            });
        }

        // ------------------------------------------------------------------ //
        //  Budowa interfejsu                                                  //
        // ------------------------------------------------------------------ //

        private void ZbudujInterfejs()
        {
            _krojKonsoli = WczytajKroj(RozmiarKonsoli);
            _krojHud = WczytajKroj(RozmiarHud);
            _krojDuzy = WczytajKroj(RozmiarDuzy);
            _krojMaly = WczytajKroj(RozmiarMaly);
            ZmierzKroj();

            var kanwaObiekt = new GameObject("Kanwa", typeof(Canvas), typeof(CanvasScaler));
            kanwaObiekt.transform.SetParent(transform, false);
            var kanwa = kanwaObiekt.GetComponent<Canvas>();
            kanwa.renderMode = RenderMode.ScreenSpaceOverlay;
            var skaler = kanwaObiekt.GetComponent<CanvasScaler>();
            skaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            skaler.referenceResolution = new Vector2(Pulpit.Szerokosc, Pulpit.Wysokosc);
            // Expand, nie MatchWidthOrHeight: pulpit ma sztywne 1280x720 i ma byc
            // widoczny w calosci. Przy dopasowaniu „w polowie szerokosc, w polowie
            // wysokosc" kadr miesci sie tylko przy dokladnie 16:9, a na 16:10 czy
            // 4:3 wychodzilby poza ekran razem z ramkami i koncowkami wierszy.
            // Wzorzec robi to samo: pygame.SCALED wpisuje stala powierzchnie
            // w okno z zachowaniem proporcji.
            skaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            // Korzeń o dokładnym rozmiarze pulpitu z punktem odniesienia
            // w lewym górnym rogu — wtedy pozycje z Grafiki przekładają się
            // wprost na pozycje w interfejsie.
            var korzenObiekt = new GameObject("Pulpit", typeof(RectTransform));
            korzenObiekt.transform.SetParent(kanwaObiekt.transform, false);
            _korzen = korzenObiekt.GetComponent<RectTransform>();
            _korzen.anchorMin = new Vector2(0.5f, 0.5f);
            _korzen.anchorMax = new Vector2(0.5f, 0.5f);
            _korzen.pivot = new Vector2(0f, 1f);
            _korzen.sizeDelta = new Vector2(Pulpit.Szerokosc, Pulpit.Wysokosc);
            _korzen.anchoredPosition = new Vector2(-Pulpit.Szerokosc / 2f, Pulpit.Wysokosc / 2f);

            var obrazObiekt = new GameObject("Obraz", typeof(RawImage));
            obrazObiekt.transform.SetParent(_korzen, false);
            _obraz = obrazObiekt.GetComponent<RawImage>();
            _obraz.texture = _tekstura;
            // Tekstury w Unity rosną od dołu, a płótno od góry — odwracamy UV,
            // zamiast przepisywać bufor przy każdej klatce.
            _obraz.uvRect = new Rect(0f, 1f, 1f, -1f);
            _obraz.raycastTarget = false;
            Rozciagnij(_obraz.rectTransform, 0, 0, Pulpit.Szerokosc, Pulpit.Wysokosc);

            (int kx, int ky, int kszer, int kwys) = Pulpit.ProstokatKonsoli;
            int ileWierszy = Math.Max(4, (kwys - 36) / _wysokoscWiersza);
            for (int i = 0; i < ileWierszy; i++)
            {
                Text t = NowyNapis($"Wiersz {i}", _krojKonsoli, RozmiarKonsoli);
                Rozciagnij(t.rectTransform, kx + 20, ky + 18 + i * _wysokoscWiersza,
                           kszer - 40, _wysokoscWiersza);
                _wiersze.Add(t);
            }
            // Znacznik „wyżej jest więcej” siedzi w miejscu pierwszego wiersza:
            // Widok() oddaje mu ten wiersz, kiedy treść nie mieści się w panelu.
            _znacznikGory = NowyNapis("Znacznik", _krojKonsoli, RozmiarKonsoli);
            Rozciagnij(_znacznikGory.rectTransform, kx + 20, ky + 18, kszer - 40, _wysokoscWiersza);
            _znacznikGory.color = Barwa2Kolor(Pulpit.Przygaszony);
        }

        private Text NowyNapis(string nazwa, Font krój, int rozmiar)
        {
            var obiekt = new GameObject(nazwa, typeof(Text));
            obiekt.transform.SetParent(_korzen, false);
            var t = obiekt.GetComponent<Text>();
            t.font = krój;
            t.fontSize = rozmiar;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            t.raycastTarget = false;
            t.color = Barwa2Kolor(Pulpit.Tekst);
            t.text = "";
            return t;
        }

        /// <summary>Ustawia prostokąt w pikselach pulpitu, licząc od lewego górnego róg.</summary>
        private static void Rozciagnij(RectTransform r, int x, int y, int szer, int wys)
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(szer, wys);
            r.anchoredPosition = new Vector2(x, -y);
        }

        private Font WczytajKroj(int rozmiar)
        {
            try
            {
                Font f = Font.CreateDynamicFontFromOSFont(NazwyCzcionek, rozmiar);
                if (f != null)
                {
                    return f;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Nie udało się wziąć czcionki systemowej: {e.Message}");
            }
            foreach (string wbudowana in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
            {
                try
                {
                    Font f = Resources.GetBuiltinResource<Font>(wbudowana);
                    if (f != null)
                    {
                        return f;
                    }
                }
                catch (Exception)
                {
                    // Nazwa wbudowanej czcionki zmieniała się między wersjami Unity;
                    // próbujemy kolejnej.
                }
            }
            Debug.LogError("Brak jakiejkolwiek czcionki — tekst się nie pokaże.");
            return null;
        }

        /// <summary>
        /// Mierzy krój konsoli. Zawijanie liczy znaki, nie piksele, więc trzeba
        /// znać szerokość jednego znaku — krój jest o stałej szerokości, więc
        /// wystarczy zmierzyć jeden.
        /// </summary>
        private void ZmierzKroj()
        {
            _wysokoscWiersza = RozmiarKonsoli + 3;
            _szerokoscZnaku = RozmiarKonsoli * 0.6f;
            if (_krojKonsoli == null)
            {
                return;
            }
            try
            {
                _krojKonsoli.RequestCharactersInTexture("M", RozmiarKonsoli);
                if (_krojKonsoli.GetCharacterInfo('M', out CharacterInfo info, RozmiarKonsoli)
                    && info.advance > 0)
                {
                    _szerokoscZnaku = info.advance;
                }
                if (_krojKonsoli.lineHeight > 1f)
                {
                    _wysokoscWiersza = Mathf.CeilToInt(_krojKonsoli.lineHeight) + 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Nie udało się zmierzyć kroju, biorę wartości " +
                                 $"przybliżone: {e.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        //  Klatka                                                             //
        // ------------------------------------------------------------------ //

        private void Update()
        {
            _czas += Time.unscaledDeltaTime;
            ObsluzWejscie();
            Rysuj();
            if (_watekLogiki != null && _watekLogiki.IsCompleted && !_konsola.Zamknieta)
            {
                _watekLogiki = null;
                if (!_bladLogiki)
                {
                    // Logika doszła do końca (gracz wybrał wyjście) — zamykamy grę.
                    // Po błędzie zostajemy na ekranie z komunikatem, aż gracz
                    // sam zamknie okno.
                    Wyjdz();
                }
            }
        }

        private void Rysuj()
        {
            Migawka m = _migawka;
            if (m == null)
            {
                _tytulowa = _tytulowa ?? Migawka.Tytulowa();
                m = _tytulowa;
            }

            List<Napis> napisy = _pulpit.Zloz(m.Pola, _czas, m.Hud, m.Region, m.GraczXy,
                                              m.Klasa, m.WszystkoOdkryte, m.Osada);
            _tekstura.LoadRawTextureData(_pulpit.Klatka.Dane);
            _tekstura.Apply(false);

            PokazNapisyHud(napisy);
            PokazKonsole();
        }

        private void PokazNapisyHud(List<Napis> napisy)
        {
            while (_napisyHud.Count < napisy.Count)
            {
                _napisyHud.Add(NowyNapis($"HUD {_napisyHud.Count}", _krojHud, RozmiarHud));
            }
            for (int i = 0; i < _napisyHud.Count; i++)
            {
                Text t = _napisyHud[i];
                if (i >= napisy.Count)
                {
                    t.text = "";
                    continue;
                }
                Napis n = napisy[i];
                bool duzy = n.Krój == "duzy";
                bool maly = n.Krój == "maly";
                t.font = duzy ? _krojDuzy : maly ? _krojMaly : _krojHud;
                t.fontSize = duzy ? RozmiarDuzy : maly ? RozmiarMaly : RozmiarHud;
                t.fontStyle = duzy ? FontStyle.Bold : FontStyle.Normal;
                t.color = Barwa2Kolor(n.Kolor);
                t.text = Przytnij(n.Tekst);
                Rozciagnij(t.rectTransform, n.X, n.Y, Pulpit.Szerokosc - n.X,
                           duzy ? RozmiarDuzy + 8 : _wysokoscWiersza);
            }
        }

        private void PokazKonsole()
        {
            bool pyta = _konsola.CzekaNaWejscie;
            string wpisywane = pyta ? _bufor : null;
            bool kursor = pyta && (int)(_czas * 2) % 2 == 0;

            // Przebudowa tylko wtedy, gdy coś się zmieniło — inaczej co klatkę
            // układalibyśmy od nowa kilkaset wierszy tekstu bez powodu.
            int wersja = _konsola.Wersja;
            if (wersja == _ostatniaWersja && wpisywane == _ostatniBufor && kursor == _ostatniKursor)
            {
                return;
            }
            _ostatniaWersja = wersja;
            _ostatniBufor = wpisywane;
            _ostatniKursor = kursor;

            (int kx, int ky, int kszer, int _) = Pulpit.ProstokatKonsoli;
            int znakow = Math.Max(16, (int)((kszer - 40) / Math.Max(1f, _szerokoscZnaku)));
            List<WierszKonsoli> widok = Grafika.Konsola.Widok(_konsola.Linie(), znakow,
                                                              _wiersze.Count, _przewiniecie,
                                                              wpisywane, kursor, out int ileWyzej);
            // Kiedy jest znacznik, treść zaczyna się o wiersz niżej — Widok()
            // zwrócił wtedy o jeden wiersz mniej, więc wszystko się spina.
            int odstep = ileWyzej > 0 ? 1 : 0;
            _znacznikGory.text = ileWyzej > 0
                ? $"▲ wyżej jeszcze {ileWyzej} wierszy (kółko myszy)"
                : "";

            _klikalne.Clear();
            for (int i = 0; i < _wiersze.Count; i++)
            {
                Text t = _wiersze[i];
                int wIndeks = i - odstep;
                if (wIndeks < 0 || wIndeks >= widok.Count)
                {
                    t.text = "";
                    continue;
                }
                WierszKonsoli w = widok[wIndeks];
                t.text = Przytnij(w.Tekst);
                t.color = Barwa2Kolor(w.Ozdobnik ? Pulpit.RamkaJasna : Pulpit.Tekst);
                if (w.Opcja != null)
                {
                    int y = ky + 18 + i * _wysokoscWiersza;
                    _klikalne.Add((new Rect(kx + 12, y - 1, kszer - 24, _wysokoscWiersza),
                                   w.Opcja));
                    t.color = Barwa2Kolor(Pulpit.Zloty);
                }
            }
        }

        /// <summary>Tekst do pokazania — bez emoji, jeśli gracz je wyłączył.</summary>
        private string Przytnij(string tekst)
        {
            if (tekst == null)
            {
                return "";
            }
            return PokazujEmoji ? tekst : Grafika.Konsola.BezEmoji(tekst);
        }

        private static Color Barwa2Kolor(Barwa b)
        {
            return new Color32(b.R, b.G, b.B, b.A);
        }

        // ------------------------------------------------------------------ //
        //  Wejście                                                            //
        // ------------------------------------------------------------------ //

        private void ObsluzWejscie()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                _pulpit.Scena.Zmierzch = !_pulpit.Scena.Zmierzch;
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }

            float kolko = Input.mouseScrollDelta.y;
            if (Mathf.Abs(kolko) > 0.01f)
            {
                _przewiniecie = Math.Max(0, _przewiniecie + (int)(kolko * 3));
                _ostatniaWersja = -1; // wymuś przebudowę widoku
            }

            if (!_konsola.CzekaNaWejscie)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Odpowiedz(_bufor);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _bufor = "";
                return;
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (_bufor.Length > 0)
                {
                    _bufor = _bufor.Substring(0, _bufor.Length - 1);
                }
                return;
            }

            // Skróty ruchu działają tylko przy pustym polu — inaczej strzałka
            // w środku wpisywanego tekstu wysyłałaby bohatera w podróż.
            if (_bufor.Length == 0 && SprawdzSkroty())
            {
                return;
            }

            foreach (char c in Input.inputString)
            {
                if (c == '\b' || c == '\n' || c == '\r')
                {
                    continue; // obsłużone wyżej przez GetKeyDown
                }
                if (!char.IsControl(c))
                {
                    _bufor += c;
                }
            }

            SprawdzKlikniecie();
        }

        private bool SprawdzSkroty()
        {
            var skroty = new (KeyCode Klawisz, string Nazwa)[]
            {
                (KeyCode.UpArrow, "gora"), (KeyCode.DownArrow, "dol"),
                (KeyCode.LeftArrow, "lewo"), (KeyCode.RightArrow, "prawo"),
                (KeyCode.Space, "spacja"),
            };
            foreach ((KeyCode klawisz, string nazwa) in skroty)
            {
                if (!Input.GetKeyDown(klawisz))
                {
                    continue;
                }
                string odpowiedz = _ekran.Skrot(nazwa);
                if (odpowiedz != null)
                {
                    Odpowiedz(odpowiedz);
                    return true;
                }
                if (nazwa == "spacja" && _konsola.CzekaNaEnter)
                {
                    Odpowiedz("");
                    return true;
                }
            }
            return false;
        }

        private void SprawdzKlikniecie()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _korzen, Input.mousePosition, null, out Vector2 lokalna))
            {
                return;
            }
            var punkt = new Vector2(lokalna.x, -lokalna.y);
            foreach ((Rect obszar, string klawisz) in _klikalne)
            {
                if (obszar.Contains(punkt))
                {
                    Odpowiedz(klawisz);
                    return;
                }
            }
            (int kx, int ky, int kszer, int kwys) = Pulpit.ProstokatKonsoli;
            if (_konsola.CzekaNaEnter && new Rect(kx, ky, kszer, kwys).Contains(punkt))
            {
                Odpowiedz("");
            }
        }

        private void Odpowiedz(string tekst)
        {
            if (_konsola.Odpowiedz(tekst))
            {
                _bufor = "";
                _przewiniecie = 0;
            }
        }

        // ------------------------------------------------------------------ //
        //  Koniec                                                             //
        // ------------------------------------------------------------------ //

        private void Wyjdz()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            // Bez tego wątek logiki zostałby zablokowany na pytaniu i w edytorze
            // przeżyłby wyjście z trybu gry.
            _konsola?.Zamknij();
            try
            {
                _watekLogiki?.Wait(500);
            }
            catch (Exception)
            {
                // Zamknięcie w trakcie pytania kończy wątek wyjątkiem — tak ma być.
            }
        }

        private void OnApplicationQuit()
        {
            _konsola?.Zamknij();
        }
    }
}
