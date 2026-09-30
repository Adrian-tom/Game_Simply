# Pro RPG – gra fantasy po polsku z grafiką pixelart

Gra RPG fantasy w Pythonie 3. Wszystkie komunikaty, menu i nazwy akcji są **po polsku**.

Gra działa w **oknie z grafiką pixelart** (pygame-ce): po lewej izometryczna mapa
regionu z oświetleniem i mgłą wojny oraz HUD postaci, po prawej konsola z tekstem
gry i klikalnymi opcjami. Cała grafika jest liczona w kodzie — w repo nie ma
plików PNG. Bez pygame gra startuje w klasycznym trybie tekstowym w terminalu.

Szczegółowy opis pętli gry, systemów i zależności: **[ANALIZA.md](ANALIZA.md)**.

---

## Uruchomienie

Na Windowsie kliknij dwukrotnie **`uruchom.bat`** (albo w PowerShellu):

```bat
uruchom.bat
```

Albo bezpośrednio:

```bash
pip install -r requirements.txt   # raz — grafika (pygame-ce)
python main.py                    # okno z grafiką
python main.py --tekst            # tryb tekstowy w terminalu
```

Wymagania: **Python 3.10+** i **pygame-ce** (tylko do okna). `uruchom.bat` najpierw szuka
Pythona w `%USERPROFILE%\.local\bin\`, a przy pierwszym starcie sam doinstalowuje pygame-ce
(`pip install --user`). Jeśli instalacja się nie uda, gra rusza w trybie tekstowym.

### Sterowanie w oknie

| Klawisz / mysz | Działanie |
|---|---|
| klik na opcję `[n]` w konsoli | wybór opcji (to samo co wpisanie numeru i Enter) |
| cyfry + `Enter` | wybór opcji, wpisywanie imienia itd. |
| strzałki (na wyprawie) | ruch: ⬆ północ, ⬇ południe, ⬅ zachód, ➡ wschód |
| `Spacja` (na wyprawie) | zbadaj pole / wejdź (`[5]`) |
| `Spacja` / klik (przy „Naciśnij Enter”) | dalej |
| kółko myszy | przewijanie konsoli |
| `F2` | dzień / zmierzch (oświetlenie ognisk, okien, portali) |
| `F11` | pełny ekran (okno można też dowolnie powiększać) |

---

## Funkcje gry

| Funkcja | Opis |
|---|---|
| **Menu główne** | Nowa gra / Wczytaj zapis / Wyjście |
| **Tworzenie postaci** | Imię, klasa, pochodzenie, 3 cechy z puli 50 (los 4→wybór 1, trzy razy), tryb trudności |
| **Atrybuty (BG3)** | Siła, Zręczność, Kondycja, Inteligencja, Mądrość, Charyzma — testy k20 vs ST |
| **Testy na mapie** | Wspinaczka, zamki, zastraszanie, perswazja, uniki, tropy, pułapki |
| **Klasy i podklasy** | Wojownik, Mag, Łotrzyk, Druid, Nekromanta — specjalizacja od poziomu 5 |
| **Umiejętności** | Rangi 1–5, cooldowny, moc rośnie z poziomem i rangą; księga w obozie `[10]` |
| **Nekromanta** | Przywołania (szkielet, ghul, widmo, krwawy sługa) — atakują i mogą przejąć cios |
| **Druid** | Przemiany (niedźwiedź, wilk, kruk, duch) — buffy na kilka tur walki |
| **Levelowanie** | EXP w bieżącym poziomie; +1 pkt. atrybutów i umiejętności (bonus na 5/10/15) |
| **Walka turowa** | Atak, przedmioty, umiejętności, ucieczka; buffy, krytyki, statusy |
| **Przeciwnicy** | Potwory ogólne i biomowe; trolle regenerują, wiedźma klnie, smok zieje ogniem |
| **Bossowie** | Co 3. region, na konkretnym polu ☠ |
| **Mapa** | Region 9×9 (81 pól) z ikonami biomów i budynków, mgłą wojny ❔ |
| **Trwały świat** | Regiony leżą na siatce wokół obozu `[0, 0]`. Wyjście za krawędź i powrót wraca **w to samo miejsce** — odkryte pola i zużyte zbiory zostają |
| **Seed postaci** | Każda nowa gra losuje własny świat; ten sam region po powrocie wygląda tak samo |
| **Ikony** | Biomy, punkty, kierunki, menu obozu, walka, sklep, questy — katalog `game/ikony.py` |
| **Ekwipunek** | Broń i zbroja, plecak, sprzedaż za 50% ceny — stary przedmiot nie znika |
| **Sklep i kuźnia** | Mikstury (leczenie, większa, mana), antidotum, ekwipunek |
| **Questy** | Tablica w obozie — zabójstwa, zakupy, świątynie. Postęp liczy się **od przyjęcia**, nie od początku gry |
| **Osiągnięcia** | Odblokowywane przy powrocie do obozu (m.in. Kartograf: odkryj cały region) |
| **Zapis** | Autosave po wyprawie i po każdym ruchu; zapis **atomowy** (przerwanie nie niszczy poprzedniego); Hardcore kasuje zapis po śmierci |
| **Świat** | Biomy, karczmy, świątynie, kuźnie, zdarzenia moralne |
| **Karma** | Reputacja z realnymi skutkami: ceny u kupców ±15%, łatwiejsza/trudniejsza rekrutacja, hojniejszy lub skąpy dar świątyni |
| **Obóz** | Rozbudowa: sklep, dom, kuźnia, stajnie, targ, warsztat, chaty osadników |
| **Zbieractwo** | Na wyprawie `[6]` zbierasz surowce (2 razy na pole); biomy dają różne materiały |
| **Drużyna** | Rekrutacja: max 1 w walce; reszta: zbiory, handel albo rzemiosło. Postacie z dialogów — fortuna albo CHA 18+ / perswazja |
| **Dialogi** | Każde NPC ma wątek fabularny (kolejne etapy) i opcję dołączenia do osady |
| **Miasto** | Od regionu 2 ikona 🏙 — osobna mapa 3×3 (rynek, kuźnia, ratusz, gildia, magazyn) |
| **Osada** | Praca w obozie `[15]`; chaty i osadnicy `[16]`; targ płaci złoto za dni nieobecności |
| **Mityczne miejsca** | Od regionu 2: 🌀 portal, 🐉 leże smoka, ☁ latająca wyspa — unikalni wrogowie i łup |

---

## Legenda mapy

| Ikona | Znaczenie | Ikona | Znaczenie |
|---|---|---|---|
| 👤 | ty | ❔ | nieodkryte |
| 🌾 | równiny | 🏚 | ruiny |
| 🌲 | las | 🐸 | bagna |
| ⛰ | wzgórza | 🏜 | kanion |
| 🏕 | obóz | 🍺 | karczma |
| ⚒ | kuźnia | 🛕 | świątynia |
| 🕳 | jaskinia | ☠ | boss |
| 🌀 | portal | 🐉 | leże smoka |
| ☁ | latająca wyspa | 🏙 | miasto |

---

## Struktura plików

```
Game_Simply/
├── main.py          # Punkt wejścia – menu i obóz; okno albo --tekst
├── uruchom.bat      # Skrót Windows — doinstalowuje pygame-ce i odpala grę
├── requirements.txt
├── README.md
├── ANALIZA.md       # Architektura, pętle gry, systemy
└── game/
    ├── ikony.py     # Katalog ikon (biomy, punkty, wrogowie)
    ├── player.py    # Postać, EXP, atrybuty, rangi, osiągnięcia
    ├── atrybuty.py  # Karta postaci, testy k20, biegłości
    ├── pochodzenie.py  # Pochodzenie i 50 cech kreacji
    ├── enemy.py     # Przeciwnicy, bossowie, skalowanie
    ├── combat.py    # Walka turowa i umiejętności
    ├── world.py     # Podróż, budynki, zdarzenia
    ├── mapa.py      # Trwały świat: siatka regionów 9×9, biomy, mgła wojny
    ├── karma.py     # Reputacja i jej skutki (ceny, rekrutacja, świątynie)
    ├── items.py     # Ekwipunek, plecak, sprzedaż
    ├── shop.py      # Sklep i kuźnia
    ├── skills.py    # Umiejętności, rangi, księga, podklasy
    ├── quests.py    # Tablica questów
    ├── dialogues.py # Dialogi NPC, wątki, rekrutacja
    ├── oboz.py      # Surowce i rozbudowa obozu
    ├── osada.py     # Praca, chaty, osadnicy, targ
    ├── miasto.py    # Osobna mapa miasta
    ├── rekruci.py   # Najemnicy i towarzysze
    ├── mityczne.py  # Portale, leże smoka, latająca wyspa
    ├── savegame.py  # Zapis JSON
    ├── ekran.py     # Łącznik logiki z oknem (bieżąca postać, skróty klawiszy)
    └── utils.py     # Wyświetlanie, input
grafika/             # Okno pixelart (pygame-ce) — logika gry nic stąd nie importuje
    ├── okno.py      # Okno, konsola z emoji i klikalnymi opcjami, HUD, przejęcie print/input
    ├── scena.py     # Izometryczna mapa regionu: kafle, mgła, światło, gracz
    ├── teren.py     # Kafle biomów, drzewa, skały, budynki, sprite gracza
    └── piksele.py   # Prymitywy: cieniowanie brył, dithering, kontury, poświaty
```

---

## Jak grać?

1. Uruchom grę: dwuklik **`uruchom.bat`** albo `python main.py`. W oknie klikaj opcje w konsoli
   albo wpisuj numery; na wyprawie chodź strzałkami.
2. Wybierz **Nowa gra**, imię, klasę, pochodzenie, 3 cechy i tryb trudności.
3. W **Obozie**:
   - `[1]` 🗺 Wyrusz na przygodę
   - `[2]` 🏪 Sklep — po zbudowaniu w obozie (albo karczma w terenie)
   - `[3]` 😴 Odpoczynek — lepszy po zbudowaniu domu
   - `[4]` 🎒 Ekwipunek (plecak, zakładanie, zdejmowanie)
   - `[5]` 📜 Tablica questów
   - `[6]` 🏆 Osiągnięcia
   - `[7]` 📋 Karta postaci (6 atrybutów, biegłości, rozdział punktów)
   - `[9]` 🗺 Mapa okolicy
   - `[10]` 📖 Księga umiejętności (rangi, podgląd następnych skilli)
   - `[11]` 🏗 Rozbudowa obozu (sklep, dom, kuźnia, stajnie, targ, warsztat, chaty)
   - `[14]` 🤝 Drużyna — najemnicy i zrekrutowane postacie
   - `[15]` 🪓 Praca w obozie (złoto i surowce, mija czas)
   - `[16]` 🛖 Osada — osadnicy, warsztat, sprzedaż surowców na targu
4. Na wyprawie: ⬆ północ / ⬅ zachód / ➡ wschód / ⬇ południe. Krawędź mapy prowadzi do sąsiedniego regionu —
   świat jest trwały, więc możesz wrócić tą samą drogą. Im dalej od obozu `[0, 0]`, tym trudniejsi wrogowie.
   Zbadaj pole (`[5]`), żeby wejść do karczmy, kuźni, świątyni, jaskini, **miasta** albo mitycznego miejsca.
   Na pustym polu mogą pojawić się testy (wspinaczka, zamki, bandyci, tropy).
   Od regionu 2 na mapie mogą pojawić się 🌀 portal, 🐉 leże smoka, ☁ latająca wyspa i **🏙 miasto**.
   W mieście poruszasz się po dzielnicach: handel na rynku, kuźnia, ratusz (Mirena), gildia (osadnicy).
   Rozmowa z NPC odsłania wątek postaci; na końcu możesz ich zrekrutować za dużą sumę albo ekstremalną charyzmę (CHA 18+ albo perswazja przy CHA 16+).
   `[6]` zbiera surowce (las = drewno, bagna = zioła, wzgórza/kanion = ruda).
   `[0]` wraca do obozu, ale pozycja na mapie zostaje. Targ i osadnicy rozliczają dni, których nie było cię w obozie.
5. Podczas walki: ⚔ atak, 🧪 przedmioty, ✨ umiejętności (z CD i rangą) lub 🏃 ucieczka.
   Nekromanta przywołuje sługę (jeden na raz); Druid zmienia formę na kilka tur.
6. Po walce: EXP, złoto, czasem łup do plecaka.
7. Na poziomie 5 wybierz podklasę w obozie. Punkty atrybutów wydajesz na karcie `[7]`, punkty umiejętności w księdze.

---

## Testy

Testy korzystają ze standardowej biblioteki (`unittest`). Testy logiki nie wymagają niczego;
testy okna (`tests/test_grafika.py`) potrzebują pygame-ce i bez niego są pomijane.

```bash
python -m unittest discover -s tests -v
```

Okno renderuje się też bez monitora (`SDL_VIDEODRIVER=dummy`), więc testy grafiki działają w CI.

Pokrywają trwały świat (powroty przez krawędź, seed, migrację starych zapisów),
zapis (round-trip, atomowość, uszkodzony plik), questy (postęp od przyjęcia),
karmę (ceny, rekrutacja, świątynie) oraz rejestr umiejętności (każdy skill ma handler
i da się go wywołać), a także okno: scenę mapy, mgłę, noc, klikalne opcje konsoli
i oddanie `print`/`input` po zamknięciu. CI uruchamia testy na Pythonie 3.10 i 3.13 —
najpierw bez pygame (tryb tekstowy musi zostać samodzielny), potem z grafiką.

---

## Licencja

Projekt otwarty – możesz dowolnie rozbudowywać i modyfikować kod.
