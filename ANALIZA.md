# Analiza gry — Pro RPG (Game_Simply)

Dokument opisuje architekturę, pętle rozgrywki i systemy. Kod jest w Pythonie 3.10+, wejście: `main.py`.
Logika (`game/`) to czysty stdlib; okno z grafiką pixelart (`grafika/`) używa pygame-ce.

---

## 1. Co to jest

RPG fantasy z grafiką pixelart w oknie (albo w trybie tekstowym): obóz jako hub, trwała mapa regionu 9×9 (81 pól), walka turowa, atrybuty w stylu BG3 (k20), osada z ekonomią czasu i miasto z własną siatką.

Gracz nie „przechodzi poziomy lochu” — wraca do obozu, rozbudowuje go i wraca w to samo miejsce na mapie. To pętla **wyprawa → łup/czas → obóz → inwestycja**.

---

## 2. Pętle rozgrywki

```
menu główne
  └─ nowa gra / zapis
       └─ OBOZ (hub)
            ├─ wyprawa  →  mapa świata  →  miasto / karczma / walka / zbiory
            │                └─ powrót: zbiory rekrutów + targ (złoto × dni nieobecności)
            ├─ rozbudowa, praca, osada, drużyna
            └─ sklep / kuźnia / questy / karta / księga
```

**Czas świata** (`gracz.czas`) rośnie przy ruchu na mapie, w mieście i przy pracy. Targ nie płaci za siedzenie w obozie — tylko za nieobecność od wyjścia na wyprawę.

---

## 3. Warstwy kodu

| Warstwa | Moduły | Rola |
|---|---|---|
| Wejście | `main.py` | Menu, obóz, pętla życia postaci |
| Stan | `player.py`, `savegame.py` | Postać JSON |
| Świat | `mapa.py`, `world.py`, `miasto.py`, `mityczne.py` | Ruch, zdarzenia, lokacje |
| Walka | `combat.py`, `enemy.py`, `skills.py` | Tury, skill-e, bossowie |
| Postać | `atrybuty.py`, `pochodzenie.py`, `items.py` | k20, cechy, ekwipunek |
| Hub | `oboz.py`, `osada.py`, `rekruci.py`, `shop.py`, `quests.py` | Budynki, osadnicy, handel |
| Fabuła | `dialogues.py` | Wątki NPC + rekrutacja |
| UI | `ikony.py`, `utils.py` | Ikony, czyszczenie ekranu |
| Łącznik | `ekran.py` | Co pokazać w oknie: bieżąca postać, skróty klawiszy |
| Grafika | `grafika/okno.py`, `scena.py`, `teren.py`, `piksele.py` | Okno pygame-ce: mapa, HUD, konsola |

Zależności idą „w dół”: `world` woła miasto i osadę; `dialogues` woła rekrutów dopiero w opcji rozmowy (bez cyklu na imporcie).

### Okno graficzne

`grafika.okno` podmienia `print`, `input` i `os.system("cls")`, więc **cała istniejąca logika
działa w oknie bez zmian**: tekst trafia do konsoli po prawej, a `input` czeka na klawisz
albo klik w opcję `[n]`. Grafika czyta stan z `game.ekran` (bieżąca postać i skróty klawiszy),
a logika nigdy nie importuje pygame. Dzięki temu ekrany można przenosić na grafikę pojedynczo —
na razie grafikę ma mapa regionu; walka, dialogi, sklep i miasto są jeszcze tekstem w konsoli.

Mapa to region 9×9 w rzucie izometrycznym (320×240 pikseli, skala ×2). Wrażenie bryły dają:
jedno źródło światła i cieniowanie po normalnej, rampy 4 kolorów z ditheringiem Bayera,
kontury 1 px, boki kafli w warstwach skały i — w trybie zmierzchu — mapa światła
(ogniska, okna, portale, latarnia gracza) mnożona przez scenę.

---

## 4. Mapa i ikony

Region 9×9 ma biomy (klastry Voronoi) i stałe punkty. Mgła wojny: pole nieodkryte = ❔. Gracz = 👤. Stare zapisy 5×5 są regenerowane; pozycja skacze do środka.

Katalog ikon jest w `game/ikony.py` (biomy, punkty, kierunki, wrogowie). `mapa.py` rysuje siatkę tymi glifami zamiast liter `T/~/#`. Opisy kierunku na eksploracji pokazują np. `🌲 las, 🍺 karczma`.

| System | Przykład |
|---|---|
| Biomy | 🌾 równiny, 🌲 las, 🐸 bagna, ⛰ wzgórza, 🏜 kanion, 🏚 ruiny |
| Budynki świata | 🏕 obóz, 🍺 karczma, ⚒ kuźnia, 🛕 świątynia, 🕳 jaskinia |
| Rzadkie | ☠ boss, 🌀 portal, 🐉 leże, ☁ wyspa, 🏙 miasto |
| Hub | 🗺 wyprawa, 🏪 sklep, 🤝 drużyna, 🪓 praca, 🛖 osada |

Ikony nie zmieniają mechaniki — skracają odczyt menu i mapy.

---

## 5. Systemy mechaniczne

**Walka.** Tura: atak / przedmioty / umiejętności / ucieczka. Efekt każdej umiejętności to
osobna funkcja `_sk_*(k: Kontekst)` w rejestrze `HANDLERY_UMIEJETNOSCI`. Krytyki z Zręczności, uniki, statusy (trucizna, krwawienie). Jeden towarzysz walki. Nekromanta: przyzwanie; Druid: forma na kilka tur.

**Atrybuty.** SIL/ZRĘ/KON/INT/MDR/CHA, test k20 vs ST (rośnie lekko z numerem regionu). Nat 20 / nat 1.

**Ekonomia obozu.** Surowce z mapy i zbieraczy → budynki. Chaty (do 6) = miejsca dla osadników (zbiory / handel / rzemiosło). Targ: złoto ∝ dni × (3 + 4×handlarze). Warsztat: mikstury ze ziół podczas nieobecności.

**Karma.** `game/karma.py` czyta wartość zbieraną przez zdarzenia i wątki: ceny ±15%,
modyfikator ST rekrutacji (−3…+3) i lustrzany dla zastraszania, siła daru świątyni.

**Rekrutacja.** Najemnicy karczmy: złoto. Nazwane NPC: 180–280 zł **albo** CHA 18+ (bez rzutu) / CHA 16+ i perswazja ST 18–20. Zajęcia: walka, zbiory, handel, rzemiosło. Limit miejsc 8–10 (dom daje extra).

**Miasto.** Osobna mapa 3×3, start przy bramie. Rynek = sklep, kuźnia = ciężki ekwipunek, gildia = najem osadnika (wymaga wolnej chaty), ratusz = Mirena, zaułek = test złodziejski.

**Zapis.** `savegame.json` obok pliku gry: pozycja, `seed`, `regiony`, atrybuty, budynki,
rekruci, `czas`, `chaty`, `osadnicy`, `watki_npc`, `questy_start`. Zapis atomowy
(tmp + `os.replace`). Hardcore kasuje plik po śmierci.

---

## 6. Fabuła w dialogach

Osiem postaci z trzystopniowym wątkiem (`gracz.watki_npc`): Boldan (córka), Aldric (dług gildii), Grimbold (przeklęte ostrze), Eremiel (rozłam zakonu), Alderon (przysięga), Ashen (Kamienne Serce), Mirena (głód miasta), Vasco (cichy udział). Wątek nie jest osobnym questem na tablicy — to narracja w rozmowie, która motywuje rekrutację.

---

## 7. Skalowanie trudności

Wrogowie skalują się z poziomem gracza, poziomem regionu (`1 + odległość od obozu`) i trybem
(łatwy / normalny / hardcore). Boss w około co trzecim regionie poza startowym. ST testów: baza + `(mapa_gen-1)//2`, cap 20.

---

## 8. Mocne strony i ograniczenia

**Działa.** Jedna pętla hub–mapa bez gubienia pozycji. Świat jest trwały — regiony leżą na
siatce wokół obozu `[0, 0]` i można do nich wracać. Osada wiąże czas wyprawy ze złotem.
Karma ma realne skutki. Ikony czynią CLI czytelnym. Stdlib only, także w testach.

**Słabe.** Grafikę ma na razie tylko mapa regionu — pozostałe ekrany to tekst w konsoli okna.
Postać gracza to mały sprite (9×13 px), różny tylko kolorami klas. Walka jest tekstowa i powtarzalna przy długim grindzie. Wątki NPC nie blokują się wzajemnie.
Miasto nie zapisuje pozycji — każde wejście zaczyna przy bramie. Jeden slot zapisu.
**Brak zakończenia kampanii — gra jest świadomie sandboksem** (patrz sekcja 10).

---

## 9. Zrobione

Naprawy i systemy domknięte w gałęzi `feat/sandbox-swiat-i-naprawy`:

1. **Trwały świat** — `gracz.regiony` trzyma każdy odwiedzony region pod kluczem `"rx,ry"`.
   Wyjście za krawędź i powrót wraca w to samo miejsce, z zachowanymi odkryciami i zbiorami.
   Trudność (`mapa_gen`) = `1 + odległość Chebysheva od (0, 0)`, więc powrót bliżej obozu
   realnie osłabia wrogów.
2. **Seed postaci** — `gracz.seed` losowany przy tworzeniu i zapisywany; region generuje się
   z `(seed, rx, ry)`. Każda nowa gra to inny świat, ale ten sam region po powrocie jest
   identyczny.
3. **Questy nie zaliczają się wstecz** — `gracz.questy_start` zamraża statystykę przy
   przyjęciu; postęp to różnica od tej chwili.
4. **Zapis atomowy** — plik tymczasowy + `os.replace()` + `fsync`. Ścieżka liczona od pliku
   gry (`Path(__file__)`), nie od katalogu roboczego. Uszkodzony zapis podnosi
   `ZapisUszkodzony` zamiast cicho udawać brak pliku.
5. **Odpoczynek kosztuje dzień** — `dodaj_czas(gracz, 1)` plus rozliczenie targu i osadników.
   Wcześniej pełne HP i mana kosztowały 5 złota w nieskończoność.
6. **Karma ma skutki** — `game/karma.py`: ceny u kupców ±15%, modyfikator ST rekrutacji
   (i lustrzany dla zastraszania), siła daru świątyni. Reputacja widoczna w obozie i sklepie.
7. **Czyste wyjście** — `KeyboardInterrupt`/`EOFError` łapane w `__main__`, bez tracebacku.
8. **Handlery zamiast łańcucha `if/elif`** — `HANDLERY_UMIEJETNOSCI` mapuje klucz na funkcję
   `_sk_*(k: Kontekst)`. 48 umiejętności, 48 handlerów, zero gałęzi. Refaktor zweryfikowany
   testem różnicowym (432 przypadki: stary łańcuch vs nowy rejestr, identyczny wynik).
9. **Testy i CI** — 43 testy `unittest`, workflow na Pythonie 3.10 i 3.13.
10. **Higiena repo** — `__pycache__` wypisany z gita (`.gitignore` sam tego nie robi).

---

## 10. Co dalej

**Kampania (świadomie odłożona).** Gra jest teraz sandboksem: regiony ciągną się w cztery
strony bez końca i bez finału. Zakończenie — akty, finalny boss, epilog zależny od karmy
i osady — to następny krok, celowo zostawiony na osobną iterację.

Poza kampanią:

- Pozycja w mieście (`miasto_x/y` do zapisu) zamiast resetu do bramy.
- Etapy `watki_npc` jako flagi na tablicy questów.
- Więcej przepisów w warsztacie niż 3.
- Mapa świata: podgląd siatki regionów, nie tylko bieżącego.
- Trzy sloty zapisu.
- Wyrównanie komórek mapy albo tryb ASCII dla starego `cmd.exe` (tryb `--tekst`).

**Grafika — kolejne ekrany** (po mapie regionu):

1. Obóz jako scena (namiot, palenisko, zbudowane budynki) i menu jako okna pixelart.
2. Walka: sprite'y wrogów, animacje ciosów, paski HP nad głowami.
3. Dialogi w stylu Disco Elysium z testami k20 (wątki NPC już są w `dialogues.py`).
4. Miasto 3×3, osada, sklep i ekwipunek jako osobne ekrany.

**Nie teraz:** nowa klasa. Wersja w Unity została porzucona (30.09.2026) — zostajemy przy
pixelarcie liczonym w kodzie w pygame-ce.

---

## 11. Jak testować po zmianach

Automatycznie:

```bash
python -m unittest discover -s tests -v
```

Ręcznie, po większej zmianie:

- `python -c "import main"` z katalogu repo.
- `python main.py` — okno: ekran tytułowy z mapą, klik w `[1]` Nowa gra, HUD po lewej na dole.
- `python main.py --tekst` — ten sam przebieg w terminalu.
- Nowa gra → obóz: ikony w menu `[1]`–`[16]`, linia „Reputacja".
- Wyprawa: nagłówek `REGION [0, 0] · poziom 1`, siatka 9×9, licznik odkryte N/81.
- Dojdź do krawędzi i wróć — musi pokazać `ZNANY REGION` i te same odkryte pola.
- Odpoczynek `[3]`: „Minął dzień" i rozliczenie targu, jeśli stoi.
- Przyjmij questa mając już dorobek — nie może zaliczyć się od razu.
