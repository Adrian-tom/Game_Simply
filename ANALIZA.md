# Analiza gry — Pro RPG (Game_Simply)

Dokument opisuje architekturę, pętle rozgrywki i systemy. Kod jest w Pythonie 3.10+ (stdlib), wejście: `main.py`.

---

## 1. Co to jest

Tekstowe RPG fantasy: obóz jako hub, trwała mapa regionu 9×9 (81 pól), walka turowa, atrybuty w stylu BG3 (k20), osada z ekonomią czasu i miasto z własną siatką.

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

Zależności idą „w dół”: `world` woła miasto i osadę; `dialogues` woła rekrutów dopiero w opcji rozmowy (bez cyklu na imporcie).

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

**Walka.** Tura: atak / przedmioty / umiejętności / ucieczka. Krytyki z Zręczności, uniki, statusy (trucizna, krwawienie). Jeden towarzysz walki. Nekromanta: przyzwanie; Druid: forma na kilka tur.

**Atrybuty.** SIL/ZRĘ/KON/INT/MDR/CHA, test k20 vs ST (rośnie lekko z numerem regionu). Nat 20 / nat 1.

**Ekonomia obozu.** Surowce z mapy i zbieraczy → budynki. Chaty (do 6) = miejsca dla osadników (zbiory / handel / rzemiosło). Targ: złoto ∝ dni × (3 + 4×handlarze). Warsztat: mikstury ze ziół podczas nieobecności.

**Rekrutacja.** Najemnicy karczmy: złoto. Nazwane NPC: 180–280 zł **albo** CHA 18+ (bez rzutu) / CHA 16+ i perswazja ST 18–20. Zajęcia: walka, zbiory, handel, rzemiosło. Limit miejsc 8–10 (dom daje extra).

**Miasto.** Osobna mapa 3×3, start przy bramie. Rynek = sklep, kuźnia = ciężki ekwipunek, gildia = najem osadnika (wymaga wolnej chaty), ratusz = Mirena, zaułek = test złodziejski.

**Zapis.** `savegame.json`: pozycja, mapa, atrybuty, budynki, rekruci, `czas`, `chaty`, `osadnicy`, `watki_npc`. Hardcore kasuje plik po śmierci.

---

## 6. Fabuła w dialogach

Osiem postaci z trzystopniowym wątkiem (`gracz.watki_npc`): Boldan (córka), Aldric (dług gildii), Grimbold (przeklęte ostrze), Eremiel (rozłam zakonu), Alderon (przysięga), Ashen (Kamienne Serce), Mirena (głód miasta), Vasco (cichy udział). Wątek nie jest osobnym questem na tablicy — to narracja w rozmowie, która motywuje rekrutację.

---

## 7. Skalowanie trudności

Wrogowie skalują się z poziomem gracza, numerem regionu i trybem (łatwy / normalny / hardcore). Boss co 3. region. ST testów: baza + `(mapa_gen-1)//2`, cap 20.

---

## 8. Mocne strony i ograniczenia

**Działa.** Jedna pętla hub–mapa bez gubienia pozycji. Osada wiąże czas wyprawy ze złotem. Ikony czynią CLI czytelnym. Stdlib only. Questy, plecak, miasto, 8 wątków NPC i rekrutacja CHA już są w grze.

**Słabe.** Brak grafiki poza emoji (szerokość znaków bywa nierówna w starym `cmd.exe`). Walka jest tekstowa i powtarzalna przy długim grindzie. Wątki NPC nie blokują się wzajemnie (Lira/Vasco to lore, nie flaga questa). Miasto nie zapisuje pozycji — każde wejście zaczyna przy bramie. Karma jest liczona, ale prawie nic z niej nie wynika. Jeden slot zapisu, brak zakończenia kampanii, warsztat ma 3 przepisy.

---

## 9. Do zrobienia

Niedokończone dopięcie (nie nowe systemy):

1. **Pozycja w mieście** — serializować `miasto_x/y` albo nie resetować do bramy przy każdym wejściu.
2. **Wątki → questy** — etapy `watki_npc` jako flagi tablicy (Lira/Vasco, dług Aldrica, Kamienne Serce).
3. **Karma w mechanice** — ceny, wydarzenia w mieście, dialogi.
4. **Warsztat** — więcej niż 3 przepisy.
5. **Kod** — handlery skilli zamiast `if/elif` w `combat.py`; testy; ewentualnie 3 sloty zapisu.
6. **Terminal** — wyrównanie komórek mapy albo tryb ASCII dla starego `cmd.exe`.

---

## 10. Co można dodać

- Kampania 8–12 wypraw, 3 akty, finalny boss i ekran końcowy (teraz regiony się powtarzają).
- Wydarzenia w mieście i ewentualnie więcej dzielnic (nie od razu 5×5 miasta).
- Drugi towarzysz walki albo pierścień/amulet — bez szóstej klasy.
- Opcjonalny kolor w terminalu (Colorama/Rich) z fallbackiem; stdlib only zostaje plusem.

**Nie teraz:** GUI, nowa klasa, zależności pip wymagane do odpalenia `uruchom.bat`.

Szczegóły i priorytety: canvas Analiza gry obok czatu.

---

## 11. Jak testować po zmianach

- `python -c "import main"` z katalogu repo (nie stub Windows Store — `uruchom.bat` albo `python3.14`).
- Nowa gra → obóz: ikony w menu `[1]`–`[16]`.
- Wyprawa: mapa 9×9 (81 pól) z 🌾/🌲/👤, licznik odkryte N/81, kierunki ze strzałkami.
- Region 2+: pole 🏙 → mapa miasta.
- Powrót z wyprawy przy zbudowanym targu: komunikat o złocie za dni.
