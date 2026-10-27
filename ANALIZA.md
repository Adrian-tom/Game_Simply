# Analiza gry — Pro RPG (Game_Simply)

Dokument opisuje architekturę, pętle rozgrywki, systemy i balans. Kod: Python 3.10+, wejście `main.py`.
Logika (`game/`) to czysty stdlib; okno z grafiką pixelart (`grafika/`) używa pygame-ce.

---

## 1. Co to jest

**Survival city builder RPG fantasy.** Bohater prowadzi osadę: wyprawy w trwały świat dają surowce,
łup i doświadczenie, a osada — jedzenie, rzemiosło i ludzi. Czas kosztuje: każdy dzień to jedzenie,
utrzymanie budynków i rosnące zagrożenie najazdem, a co 90 dni przychodzi zima.

Gatunki spięte w jedną pętlę: survival, city builder, RPG, obrona osady, rzemiosło i alchemia,
handel i karawany, narracja w stylu Disco Elysium.

**Kampania (akty, finał) jest świadomie odłożona** — gra jest sandboksem z presją czasu.

---

## 2. Pętle rozgrywki

```
                ┌──────────────── dzień świata (game/swiat.py) ────────────────┐
                │ pora roku · bohater je/goi rany · osada pracuje, je, pali,  │
                │ psuje zapasy, płaci utrzymanie · morale, choroby, odejścia │
                │ · karawany i ceny · myśli dojrzewają · sprawy osady        │
                │ · zagrożenie → zapowiedź → NAJAZD                           │
                └───────▲──────────────▲───────────────▲──────────────────────┘
                        │              │               │
   wyprawa (1 ruch = 1 dzień)   odpoczynek (1 dzień)   praca (2 dni)
        │ surowce, żywność, łup, EXP, rzadkie składniki
        ▼
   OBÓZ ── rozbudowa · zawody · zamówienia · rzemiosło · karawany · talenty · myśli · sprawy
```

Krótka pętla: wyprawa → powrót z surowcami → inwestycja w osadę.
Średnia pętla: pora roku — zbierać latem i jesienią, przetrwać zimę.
Długa pętla: najazdy rosną z czasem — osada musi rosnąć szybciej niż bandy.

---

## 3. Warstwy kodu

| Warstwa | Moduły | Rola |
|---|---|---|
| Wejście | `main.py` | Menu, obóz (22 opcje), śmierć i dziedzictwo |
| Czas | `swiat.py`, `kalendarz.py` | Dzienny cykl świata — jedyne miejsce, gdzie mija czas |
| Przetrwanie | `przetrwanie.py` | Prowiant, głód, zimno, rany |
| Osada | `osada.py`, `oboz.py`, `obrona.py`, `rekruci.py` | Osadnicy, zawody, budynki, najazdy, drużyna |
| Gospodarka | `rzemioslo.py`, `handel.py`, `shop.py`, `items.py` | Przepisy, karawany, sklep, ekwipunek |
| Walka | `combat.py`, `enemy.py`, `skills.py` | Tury, garda, zapowiedzi, żywioły, bossowie |
| Postać | `player.py`, `atrybuty.py`, `pochodzenie.py`, `talenty.py` | k20, cechy, talenty |
| Narracja | `rozmowy.py`, `mysli.py`, `dialogues.py`, `doradca.py` | Rozmowy DE, gabinet myśli, NPC |
| Świat | `mapa.py`, `world.py`, `miasto.py`, `mityczne.py` | Regiony, eksploracja, lokacje |
| Stan | `savegame.py`, `dziedzictwo.py` | Zapis z migracją, ród |
| Grafika | `grafika/*`, `ekran.py` | Okno pixelart, łącznik z logiką |

Zależności idą „w dół”. Moduły dnia (`swiat`) importują resztę leniwie, żeby nie tworzyć cykli.

### Okno graficzne

`grafika.okno` podmienia `print`, `input` i `os.system("cls")`, więc cała logika działa w oknie bez
zmian. Grafika czyta stan z `game.ekran`; logika nigdy nie importuje pygame. Mapa rysuje region 9×9
w rzucie izometrycznym, a obóz rośnie razem z osadą (chaty, palisada, wieża, wykarczowana okolica).

Okno wybiera widok po stanie w `game.ekran`: walka → arena, rozmowa → portret, `ekran.widok` →
scena osady, inaczej mapa regionu. Menu obozu i osady wchodzą w `ekran.widok_osady(...)` —
to menedżer kontekstu, więc zagnieżdżone menu i wyjście wyjątkiem same oddają poprzedni widok.
Scena (`grafika.widok_osady`) czyta stan gry na żywo: postawiony budynek widać od razu, a imiona
rysuje okno, bo czcionki żyją w warstwie UI. Osadnicy **naprawdę chodzą** (`game.ruch`: siatka 7×7,
ścieżki A*, rezerwacje pól, każdy ma własne miejsce przy celu): rano idą do stanowisk, wieczorem
siadają przy ogniu albo idą do tawerny, w nocy śpią przy chatach (leżą), chorzy zostają w domu.
Doba sceny (90 s) jest osobna od dnia świata. Scena ma dzień i noc: barwa światła mnoży cały obraz,
a ognisko, okna i kuźnia świecą po nałożeniu światła — pixelart z oświetleniem w duchu stylu Mroczny.

---

## 4. Systemy

**Dzień świata.** `swiat.minij_dni(gracz, n)` wywołuje każdy dzień po kolei. Wcześniej targ płacił
„za nieobecność”, a odpoczynek w obozie przynosił zysk — teraz osada zarabia zawsze, ale też zawsze
je i płaci. Wieści zwykłe trafiają do kroniki (pokazywanej w obozie), pilne — od razu na ekran.

**Pory roku.** Wiosna/lato/jesień/zima po 30 dni. Mnożniki zbiorów (zima 0,4), rolnictwa
(jesień 1,6, zima 0), opał zimą, zagrożenie (zima 1,3), ceny żywności (zima 1,5).

**Przetrwanie.** Prowiant: 8 racji + 4 za poziom stajni, zima 2 racje/dzień. Głód: −3 HP × dni,
−10% ataku za dzień (maks. −40%), nigdy nie zabija poza walką. Zima bez odzienia: −4 HP/dzień.
Rany: 18% szans po ciosie ≥22% max HP albo przy HP <20% (+12% boss), skutki w walce i testach.

**Osadnicy.** Morale dąży do celu (55 ± jedzenie, zimno, tawerna, karma, cecha, talenty, myśli,
wydarzenia, pasja przy ulubionej pracy +4/+8) o 25% dziennie. Wydajność = (0,6 + morale/125) × cecha
× umiejętność zawodu (0,8 + 0,05 × poziom, czyli 0,8–1,8) × premia z badań × narzędzia (+25%).
Morale <20 → 10% dziennie na odejście. Choroby 0,6%/dzień (×2 głód, ×2 zimno,
×0,5 lecznica), chory + głód/zimno → 4% dziennie na śmierć. Zadowoleni płacą dziesięcinę 0,5 zł.

**Umiejętności i pasje** (`game/kompetencje.py`). Dziesięć umiejętności na poziomach 0–20 (budowa,
górnictwo, uprawa, zbieractwo, łowiectwo, rzemiosło, handel, medycyna, walka, wiedza). Start losowany
z imienia osadnika (stałe w każdym zapisie): dwa talenty i 1–2 pasje (🔥 mała, 🔥🔥 duża). Dzień pracy
daje 60 XP × mnożnik pasji (brak ×0,35, mała ×1, duża ×1,5); XP do kolejnego poziomu rośnie od 40
do 400 (poz. 9) i 1200 (poz. 19) — krzywa RimWorlda podzielona przez 25. Stare gwiazdki z zapisów
(★★★) przechodzą w poziom 3 × liczba gwiazdek.

**Budynki.** 17 typów, poziom n kosztuje koszt × n (od 2. poziomu także deski i żelazo; pola i
palisada dostają żelazo dopiero od 3. poziomu, żeby epoka I nie wymagała metalu), utrzymanie
w złocie dziennie. Brak złota na utrzymanie = spadek morale.

**Budowa w czasie** (`game/budowa.py`). Zlecenie w `[11]` od razu pobiera materiały i zakłada plan
(maks. 4 naraz); budynek staje po zebraniu „dni pracy” (baza 2–5 × poziom, chata 2). Pracują
budowniczowie (pełna moc × umiejętność Budowa), bezczynni (35%) i reszta „po godzinach” (10%);
jeden plan przyjmuje dziennie najwyżej 2,5 pracy. Bohater może poświęcić dzień (≈1,5 + premia
z Siły). Anulowanie zwraca 100% bez prac, do 50% przy prawie skończonym. W scenie osady plan
to rosnący szkielet z desek na polu budynku (rozbudowa: rusztowanie na budynku).

**Badania** (`game/badania.py`). Nowa osada umie: dom, pola, palisadę i skryptorium na poz. 1 oraz
4 chaty. Resztę — tartak, kuźnię, targ, wyższe poziomy, więcej chat — poznaje się w skryptorium:
badacze (1 na poziom) dają ~5 pkt wiedzy dziennie × skryptorium (+35%/poziom) × wydajność.
27 projektów w trzech epokach (I Drewno i kamień, II Żelazo i rzemiosło, III Mistrzostwo);
wejście w epokę to osobny projekt przełomowy, a projekty epoki n wymagają skryptorium na poz. n.
Projekt daje nowe budowle/poziomy, limit chat (8, 12) albo premię do zawodu (+10–15%). Bohater może
studiować sam (3–4 pkt/dzień). Stary zapis dostaje wszystko jako poznane. Mnożnik kosztów
`SKALA_KOSZTU` to jedyne pokrętło tempa.

**Obrona.** Zagrożenie +(1,4 + dzień/70) × pora × bogactwo dziennie; przy 100 zapowiedź najazdu
za 3–4 dni (+2 z wieżą). Siła bandy = 16 + 0,35 × dzień + 6 × liczba najazdów (±15%).
Obrona = 18 × palisada + 10 × wieża + strażnicy (6 × wydajność × cecha) + drużyna na murach.
Z bohaterem w obozie: rozmowa z hersztem (okup, perswazja, zastraszanie, oszustwo, głosy
odsłaniające ranę herszta albo głód bandy) → 3 fale z taktyką → pojedynek.

**Rzemiosło.** 14 przepisów; 6 znanych od startu, reszta do odkrycia (eksperyment z pary składników,
rozmowy, zwoje kupca). Rzemieślnicy uzupełniają zapasy do celu ustawionego w zamówieniach.

**Handel.** Każda osada handlowa ma normy cen (specjalności), ceny dryfują ±4% dziennie
i wracają do normy. Karawana: 2 × odległość + 1 dni, ryzyko 6% + 7% × odległość − 5% × eskorta.

**Rozmowy.** Graf węzłów z głosami (bierny test: premia + 10 ≥ ST), białymi i czerwonymi testami
(szansa w %), efektami (karma, złoto, myśli, przepisy, morale, rekrutacja, wynik rozmowy).
Stan nieudanych testów zapisuje się w `gracz.testy_rozmow` (biały wraca po wzroście premii).

**Śmierć.** Normalny: omdlenie (−30% złota, prowiant, połowa łupów i eliksirów, ciężka rana,
3 dni świata bez bohatera). Hardcore: dziedzic z drużyny lub osady przejmuje wszystko,
co należy do osady (`dziedzictwo.POLA_OSADY`).

---

## 5. Balans walki (symulacje botów)

Bot gra rozsądnie: umiejętności ofensywne, mikstura przy HP <35%, garda na zapowiedziany cios,
przedmioty bojowe u przygotowanego gracza. Wyniki średnio dla 5 klas i regionów 1–4, 50 walk
na przypadek (skrypty w historii sesji; parametry w `enemy.BAZA_*` i `WZROST_*`):

| Sytuacja | Przed przebudową | Po |
|---|---|---|
| Zwykły wróg, gracz na poziomie 2 × region | ~70% wygranych na **każdym** poziomie (wróg rósł z graczem) | **87%**, strata HP ~26% |
| Zwykły wróg, gracz 2 poziomy niżej | ~70% | **82%** |
| Boss, gracz przygotowany (sprzęt, mikstury, olej, eliksiry) | **0%** (0 na 4200 walk) | **51%** |
| Boss, gracz bez przygotowania | 0% | **38%** |

Przyczyny starych problemów: boss rósł o 20% za poziom gracza (na poz. 1 Wojownik zadawał mu 1 obrażenie),
troll regenerował 8% HP na turę od poz. 2 — Mag zadawał mu 4.

## 6. Balans osady (bot „zarządca”, 2 lata gry)

Bot buduje według planu (farma, palisada, spichlerz, dom, sklep, tartak, wieża…), zatrudnia
rolników, myśliwych i strażników, poluje, gdy brakuje jedzenia, czeka w obozie na zapowiedziany najazd.
15 kampanii po 240 dni: 13 dotrwało do końca, osada 7–10 osadników, 10–15 poziomów budynków,
~10 najazdów, średnio ~35% odpartych, 0–3 omdlenia. Osada pozostawiona sama sobie (bez zarządzania)
upada w pierwszym roku — to zamierzone.

---

## 6b. Balans budowy i badań (autopilot, 240 dni)

Skrypt pomocniczy (poza repozytorium, bo to jednorazowa miara) sterował osadą: obsada stanowisk
wg priorytetów (badacz, budowniczy, tracz, hutnik, rolnik…), kolejka budów i badań wg listy, rekrutacja.
Najazdy wyłączone, a bohater „zbiera na mapie” stałą dawkę surowców i **założony** dochód
złota (5, 10 albo 20 zł dziennie — to założenie, nie pomiar z gry). Wynik z 5 ziaren na dochód:

| Dochód bohatera | Epoka II | Epoka III | Wszystkie 27 badań | Poziomy budynków po 240 dniach |
|---|---|---|---|---|
| 5 zł/dzień | ~dzień 32–34 | **nie** (brak złota na skryptorium poz. 2) | 10/27 | 11–12 z 44 |
| 10 zł/dzień | ~dzień 32–34 | ~dzień 76–84 | dzień 144–159 | 31–33 z 44 |
| 20 zł/dzień | ~dzień 32–35 | ~dzień 74–82 | dzień 102–128 | 41–43 z 44 |

Wniosek: tempo nauki narzuca czas (epoka II po ~miesiącu, III po ~2,5 miesiąca), a budowanie — złoto.
Przy ~5 zł/dzień osada utyka na bramce epoki II, bo utrzymanie i koszty zjadają dochód; prawdziwy
dochód z wypraw i zadań wymaga osobnego pomiaru. Wyniki §6 (15 kampanii) powstały **przed**
budową w czasie i badaniami i nie były powtarzane.

---

## 7. Mocne strony i ograniczenia

**Działa.** Czas ma koszt i presję. Każdy system zasila inny: wyprawy → surowce → budynki →
produkcja → rzemiosło → przedmioty bojowe → bossowie i obrona. Awans daje realną przewagę.
Decyzje w rozmowach mają skutki mechaniczne (morale, myśli, przepisy, najazd odwołany).

**Słabe.**
- Grafikę mają mapa, walka, portrety rozmówców oraz obóz i osada (scena z chatami, palisadą
  i osadnikami przy warsztatach). Same menu nadal są tekstem w konsoli okna — teraz jednak nad
  sceną osady, a nie nad obcą mapą. **Pozostała dziura**: handel i rzemiosło bez własnej scenerii.
- Mag i Druid słabiej radzą sobie z bossami (częściowo przez to, jak gra nimi bot).
- Rozmów w nowym silniku jest 10; `dialogues.py` został warstwą danych, przez którą `rozmowy.py`
  prowadzi wszystkich nazwanych NPC w nowym silniku.
- Brak kampanii (świadomie).

---

## 8. Co dalej

0. **Port na Unity** (`unity/`) — etap 1 zrobiony: pionowy wycinek od ekranu
   tytułowego po ruch po mapie, zbieractwo i testy k20, z logiką na osobnym wątku
   i pixelartem liczonym w C#. Zgodność z tą wersją jest sprawdzana testami:
   ten sam seed daje ten sam świat, a kafle i sprite'y zgadzają się co do piksela.
   Etap 2: walka, rozmowy, osada i zapis gry. Szczegóły: `unity/README.md`.
   Jest już trzeci styl, **hybrydowy** (pixelart w światłach Mrocznego), ze sceną
   osady z bliska i chodzącymi osadnikami (`Logika/Ruch.cs` zgodny z `game/ruch.py`
   co do bitu); to domyślny styl portu.
1. **Kampania** — akty, finał, epilog zależny od karmy i osady (odłożona na osobną iterację).
2. Menu jako okna pixelart (scena obozu i osady już jest; tekstowe pozostają listy wyborów),
   własna sceneria dla targu i warsztatu.
3. Więcej spraw osady i myśli; nowe wydarzenia sezonowe (powódź wiosną, pożar latem, zaraza).
4. Osadnicy po imieniu w rozmowach — więzi z `wiezi.py` jako temat rozmowy
   (poróżnieni proszą o rozsądzenie sporu, para o zgodę na chatę).
5. **Mechaniki z RimWorlda** — katalog i 12 etapów przeniesienia w `RIMWORLD-MECHANIKI.md`
   (myśli i nastrój osadnika, storyteller z punktami zagrożenia i adaptacją, umiejętności
   z pasjami, priorytety pracy, zdrowie jako hediffy…). Najpierw wersja pythonowa z testami,
   potem port Unity. Kolejność wybiera właściciel projektu.
   **Zrobione w Pythonie (26.10.2026):** umiejętności 0–20 i pasje, budowa w czasie
   z budowniczymi, badania z epokami. **Zportowane na Unity (27.10.2026):** wszystkie trzy
   razem z dziennym cyklem osady, menu `[11]/[16]/[23]` i sceną z rusztowaniami; zgodność
   z Pythonem sprawdzają scenariusze wielodniowe w `unity/Testy`. Zostają w Unity: więzi,
   myśli, zamówienia rzemieślników, handel, obrona, zapis.

---

## 9. Jak testować po zmianach

```bash
python -m unittest discover -s tests -v
```

Ręcznie po większej zmianie:

- `python main.py` — okno: HUD pokazuje porę roku, żywność z bilansem, zagrożenie, morale.
- Nowa gra → obóz: doradca podpowiada farmę; `[16]` osadnicy z cechami i umiejętnościami; `[11]` rozbudowa
  (zlecenie, pasek postępu, „Pomagaj przy budowie”); `[23]` skryptorium (po zbudowaniu go w `[11]`).
- Wyprawa: prowiant maleje co ruch; w lesie `[6]` daje drewno i żywność.
- Kilka odpoczynków `[3]`: „Wczoraj: …” w `[16]`, wieści z osady w obozie.
- Walka: zapowiedź → `[5]` garda → komunikat o kontrze.
- `[17]` rzemiosło, `[19]` karawana, `[20]` talent, `[22]` sprawa osady (po kilku dniach).
