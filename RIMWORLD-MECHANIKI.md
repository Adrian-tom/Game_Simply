# RimWorld → Simply: katalog mechanik i plan przeniesienia

Analiza statyczna RimWorld 1.6 (build z `Version.txt`: 1.6.4871 rev590) z Steama. Narzędzia: REA (metadane `Assembly-CSharp.dll`, Ghidra dla bibliotek natywnych), ILSpy (dekompilacja C#) oraz definicje XML z `Data\`. Gry nie uruchamiałem.

**Zasada przenoszenia:** bierzemy **pomysły i liczby jako punkt wyjścia do własnego balansu**, nie kod ani teksty. Wszystko przepisujemy po swojemu (Python w `game/`, potem port Unity), zgodnie z EULA gry.

---

## 1. Skala i co przeczytałem

| Co | Ilość |
|---|---|
| Zdekompilowane pliki C# | 9 217 (ok. 930 tys. linii, w tym 81 tys. wygenerowane przez Unity) |
| Typy w `Assembly-CSharp.dll` | 16 158 typów, 90 840 metod |
| Definicje XML (Core + 5 dodatków) | 18 147 elementów, 386 rodzajów |
| Core: myśli / tła / hediffy / prace | 285 / 784 / 194 / 195 |
| Pliki XML z definicjami | Core 582, Royalty 163, Ideology 225, Biotech 239, Anomaly 213, Odyssey 250 |

**Uczciwie o zakresie:** przeczytałem w całości lub w kluczowych częściach ok. 40 klas rdzenia (Storyteller, Need*, MentalBreaker, JobDriver, ThinkNode, SkillRecord, TendUtility, ArmorUtility, Trade*, IncidentWorker*, PawnGroupKindWorker) oraz wyciągnąłem dane z wszystkich XML-i do katalogów (`scratchpad\catalog\*.txt`). **Nie czytałem linia po linii** reszty 9 000 plików (UI, dialogi, dodatki, generatory map). Liczby poniżej są z kodu/XML, a wnioski „dla Simply” to moje propozycje.

---

## 2. Jak RimWorld jest zbudowany (wzorce warte skopiowania w duchu)

1. **Dane w XML, zachowanie w klasie** (`workerClass`, `compClass`, `jobDriverClass`). Nowa treść = nowy wpis, nie nowy kod. W Simply odpowiada temu słownik danych + funkcja obsługi (jak `ZAJECIA`, `CECHY_OSADNIKOW`).
2. **Jedna formuła na statystyki (StatDef):** wartość = baza + (poziom umiejętności × bonus) × zdolności (np. sprawność rąk) × modyfikatory cech/sprzętu/pomieszczenia. Jedno miejsce zamiast rozsianych `×1.25`.
3. **Drzewo decyzyjne (ThinkTree)** wybiera zadanie; **JobDriver** wykonuje je w krokach (Toil). Wyjątki w jednym węźle nie psują reszty.
4. **Zdarzenia przez Storytellera:** nie losowanie „co dzień”, tylko budżet punktów zagrożenia i cykle napięcia/odpoczynku.
5. **Rzadkie tiki:** potrzeby i sprawdzanie załamań co 150 ticków, narrator co 1000 (60 razy na dobę). Odpowiednik w Simply: wszystko liczymy raz na dzień, więc **uśredniamy** dobowe stawki.
6. **Zapis przez jeden interfejs** (`ExposeData`). Simply ma `savegame.py` z migracją: każdy nowy system dopisujemy tam z domyślnymi wartościami.

Skala czasu RimWorlda: **1 doba = 60 000 ticków, 1 godzina = 2 500, rok = 60 dni (cztery pory po 15)**. Simply: 1 dzień świata, rok 120 dni (pory po 30). Przeliczenia „na godzinę” zamieniamy na „na dzień” mnożnikiem 24.

---

## 3. Katalog mechanik

Legenda w kolumnie Simply: **jest** (mamy), **część** (jest uproszczone), **brak**.

### A. Storyteller i zdarzenia

**Jak działa w RimWorld**
- Co 1 000 ticków każdy „komponent” narratora proponuje zdarzenia. `IncidentWorker.CanFireNow` filtruje (dzień od osiedlenia, trudność, biom, scenariusz, populacja, zbyt wczesny powtórny strzał, stany blokujące zdarzenia).
- **Punkty zagrożenia** (`StorytellerUtility.DefaultThreatPointsNow`): krzywa z **bogactwa kolonii** (0 przy ≤14 tys., 2 400 przy 400 tys., 4 200 przy 1 mln) + **punkty za każdego kolonistę** (15 przy niskim bogactwie, 140 przy 400 tys., 200 przy 1 mln; zmniejszone o stan zdrowia i wiek), × losowy współczynnik × adaptacja × skala trudności × czynnik „dni od startu” (0,70 przy 10. dniu → 1,00 przy 40.). Zakres 35–10 000.
- **Adaptacja** (`StoryWatcher_Adaptation`): licznik „dni adaptacji” od −60 do +100, rośnie z czasem (wolniej, im wyżej), spada przy utracie kolonisty (−30 dni) lub powaleniu (−8…−3,5). Czynnik punktów: −30 → 0,40; 0 → 0,80; 30 → 1,00; 60 → 1,20; 120 → 1,60; 180 → 2,00. Czyli **gra łagodnieje po klęsce i twardnieje, gdy gracz wygrywa**.
- **Narratorzy (3 charaktery):**
  - *Cassandra*: duże zagrożenie w cyklu **4,6 dnia włączone / 6 dni wyłączone**, od dnia 11., 1–2 zdarzenia, min. odstęp 1,9 dnia; przed dniem 20. zawsze rajd.
  - *Phoebe*: łagodniej, 8 dni włączone / 8 wyłączone, od dnia 13., 1 zdarzenie.
  - *Randy*: losowo, średnio co **1,35 dnia** wybiera kategorię (ważoną), ale **najwyżej 13 dni bez dużego zagrożenia**, punkty ×0,5…1,5.
- **Wagi zdarzeń:** `baseChance` (np. rajd 7,4; infestacja 2,7; stado agresywnych zwierząt 2,0; szał pojedynczego zwierzęcia 5) × czynnik populacji × **„intencja populacji”** (przy małej liczbie osadników częściej zdarzenia dodające ludzi) × malejąca waga ostatnio wylosowanych (nie powtarza w kółko tego samego).
- **Kategorie:** ThreatBig, ThreatSmall, Misc, Disease (człowiek/zwierzę), FactionArrival (handlarze, goście, wędrowcy), AllyAssistance, OrbitalVisitor, ShipChunkDrop.
- **Rajd:** punkty → grupa wrogów kupowana do wyczerpania budżetu (`PawnGroupMaker`, każdy typ wroga ma koszt = siła bojowa) → **strategia** (natychmiastowy atak, atak mądry, oblężenie, sapery, zbieranie się przed atakiem) wybierana wagą zależną od punktów → **sposób przybycia** (z krawędzi pieszo, zrzut, środek mapy). Strategie mają własne mnożniki punktów (oblężenie ×0,8…0,65).

**Simply:** *część* — `obrona.py` ma licznik zagrożenia rosnący dziennie (+(1,4+dzień/70)×pora×bogactwo), zapowiedź przy 100, siła bandy 16+0,35×dzień+6×najazdy. `wydarzenia.py` losuje 5 zdarzeń sezonowo.

**Propozycja:** zastąpić licznik zagrożenia **budżetem punktów z bogactwa i liczby/stanu osadników**, dodać **adaptację** (śmierć osadnika → przerwa), **cykl napięcie/odpoczynek** dla dużych najazdów oraz **wagi + pamięć ostatnich zdarzeń** zamiast stałych prawdopodobieństw. Narrator jako wybór przy starcie (3 charaktery = 3 zestawy parametrów).

### B. Potrzeby

| Potrzeba | Mechanizm | Liczby |
|---|---|---|
| Jedzenie | spada stale; poniżej progu głód, przy zerze **niedożywienie** (hediff) | bazowy spadek 1,6 j./dobę (mnożony cechą, wiekiem i chorobami); niedożywienie +0,453/dobę, śmierć przy 1,0 |
| Sen | spada w dzień, rośnie podczas snu | zmęczony <28%, bardzo zmęczony <14%, wyczerpany ≈0; pełny sen ≈10,5 h; spadek ≈0,95/dobę |
| Rozrywka | wymaga **różnorodności** rodzajów zabawy (tolerancja spada przy powtarzaniu) | liczba potrzebnych rodzajów zależy od oczekiwań: 2 (skrajnie niskie) … 6 |
| Piękno, Komfort, Na dworze, Rozmiar pokoju | wyliczane z otoczenia | „seeker”: Piękno rośnie 0,32/h i opada 0,08/h; Komfort rośnie 0,60/h i opada 0,04/h |
| Używka | uzależnienie, głód chemiczny | opcjonalne |

Nastrój i potrzeby zamieniają się w **myśli** (sekcja C).

**Simply:** *część* — jedzenie z magazynu, opał zimą, zimno, morale liczone z kilku składników. Brak snu/rozrywki/piękna/komfortu osadnika.
**Propozycja:** dodać 3 potrzeby osadnika w skali dobowej: **sen** (wyczerpanie z przepracowania), **rozrywka** (tawerna, święta, gry; tolerancja na powtarzalność) i **komfort/piękno** (z jakości chaty, patrz J). Każda daje myśl o ujemnej/dodatniej wartości.

### C. Nastrój, myśli, załamania

**Jak działa**
- `nastrój = baza 0,32 + suma(myśli)/100`, ograniczony do 0–1; **poziom dąży do celu**: rośnie o 0,12/h, opada o 0,08/h (więc nastrój ma bezwładność).
- **Myśl (ThoughtDef):** wartość nastroju (np. −44…+40), czas trwania w dniach, limit stosowania (`stackLimit`), etapy zależne od nasilenia. Dwa rodzaje: **pamięciowe** (wspomnienie wygasa) i **sytuacyjne** (trwa, dopóki warunek prawdziwy). 265 myśli w Core, 948 razem.
- Przykłady wartości: głód −6…−44; ból −5…−20; zimno/upał −4…−16; brak snu −6…−18; brak rozrywki −20…+10; ślub +40 (30 dni); zjedzenie ludzkiego mięsa −20; śmierć współmałżonka −20; **katharsis po załamaniu +40 przez 3 dni**.
- **Progi załamania:** statystyka `MentalBreakThreshold` = 0,35 (modyfikowana cechami: Opanowany −0,09/−0,18, Neurotyk +0,20/+0,40). Mniejsze załamanie poniżej 0,35, większe poniżej 0,2 (×0,571), skrajne poniżej 0,05 (×1/7).
- **Częstość:** średni czas do załamania (MTB) **4 dni / 0,8 dnia / 0,5 dnia** dla poziomów; musi być poniżej progu ≥2 000 ticków i minąć ≥15 000 od poprzedniego załamania. Wybierane z puli stanów po nasileniu (49 stanów: szał, wędrówka, atak na pokój, picie, „porzucam osadę”, podpalanie, napad złości…).
- **Oczekiwania (Expectations):** z bogactwa mapy (do 15 tys. „skrajnie niskie”, 31 tys. „bardzo niskie”, 81 tys. „niskie”, 182 tys. „umiarkowane”, 308 tys. „wysokie”, wyżej „niebotyczne”) wynika, jak wiele kolonista **oczekuje**: więcej rodzajów rozrywki (2 → 6) i szybciej spadająca tolerancja na powtarzanie (0,18 → 0,07 na dobę).

**Simply:** *część* — morale 0–100 dąży do celu o 25% dziennie, liczone z kilku składników; odejście przy <20; mysli.py to inna rzecz (gabinet myśli bohatera).
**Propozycja (wysoki priorytet):** model **„lista myśli osadnika”**: `{id, wartosc, dni, limit, zrodlo}` + suma → nastrój. Progi załamania i MTB w dniach wprost z tabeli. Stany załamania dopasowane do fantasy (ucieczka, pijaństwo, bójka, złość na budynek, „porzuca osadę”). Katharsis jako sprężyna zwrotna. Oczekiwania rosnące z bogactwem osady.

### D. Cechy, tła, umiejętności, pasje

- **Umiejętności:** 12 (Strzelanie, Walka wręcz, Budowa, Górnictwo, Gotowanie, Rośliny, Zwierzęta, Rzemiosło, Sztuka, Medycyna, Społeczne, Intelekt), poziom **0–20**.
- **XP do następnego poziomu:** krzywa 1 000 (poziom 0) → 10 000 (9) → 30 000 (19).
- **Pasja:** brak ×0,35 · mała ×1,0 · duża ×1,5 tempa nauki. **Nasycenie:** po 4 000 XP/dobę nauka ×0,2. **Zapominanie** przy wysokich poziomach (połowa z cechą Świetna pamięć). Dziennie pracą na umiejętności rośnie XP, a „praca z pasją” daje +8/+14 nastroju.
- **Cechy (51 w Core+dodatkach):** spektra o kilku stopniach: Pracowitość (leniwy…pracowity), Nerwy (stalowe…), Naturalny nastrój (pesymista…sangwinik), Neurotyk, Szybkość chodu, Piękność, Odporność (chorowity…superodporny); oraz pojedyncze: Pirotoman (jedyne dozwolone załamanie: podpalanie), Żarłok (jedyne załamanie: obżarstwo), Żądny krwi, Psychopata, Dobry, Szorstki, Mazochista, Chciwy, Zazdrosny, Ascetyczny, Nocny marek, Szybko/Wolno się uczy, Świetna pamięć, Twardziel, Słaby (Wimp). Każda ma **konflikty** (np. Chciwy ↔ Ascetyczny), `statOffsets/statFactors`, zakazane prace i „jedyne dozwolone załamania”.
- **Tła (784):** dzieciństwo + dorosłość z premiami/wyłączeniami umiejętności i prac.

**Simply:** *część* — 10 cech z jednym mnożnikiem, doświadczenie w 3 progach (+10%/★), brak poziomów umiejętności osadnika.
**Propozycja:** poziomy 0–20 dla ~8 umiejętności osadnika (dopasowanych do fantasy), pasje, nasycenie; cechy ze stopniami i konfliktami; wydajność przez **jedną formułę stat** z pkt 2. Tło osadnika (z rekrutacji) jako para premii.

### E. Praca

- **Priorytety 1–4** (0 = wyłączone) na typ pracy, domyślnie 3. **23 typy prac** z naturalną kolejnością (Pożar 1400, Pacjent 1350, Lekarz 1300, Łóżko 1200, Podstawowa 1150, Strażnik więzienny 1100, Zwierzęta 1050, Gotowanie 1000, Polowanie 950, Budowa 900, Uprawa 700, Górnictwo 600, Ścinanie 500, Kowalstwo 470, Krawiectwo 450, Sztuka 430, Rzemiosło 400, Noszenie 300, Sprzątanie 200, Badania 100).
- **WorkGiver:** 155 małych klas, które skanują świat i zwracają zadanie; w obrębie typu mają własny priorytet. Wybór: najpierw najniższy numer priorytetu, w jego obrębie kolejność typu, **pierwsze znalezione zadanie wygrywa**.
- **Harmonogram godzinowy:** każda godzina doby = Cokolwiek / Praca / Rozrywka / Sen (+Medytacja z Royalty). Zmienia, co pionek *może* robić.
- Cechy wyłączają prace (np. Pirotoman nigdy nie gasi pożaru; Żądny krwi wymaga pracy bojowej).

**Simply:** *część* — jeden `zajecie` na osadnika, bez priorytetów i bez zadań wynikających z potrzeb (np. nikt nie zajmie się chorym z własnej woli).
**Propozycja:** tabela **priorytetów 1–4 dla każdego osadnika** i dzienny **alokator** (kto robi co, aż do limitu miejsc w budynkach), zasada „pilne najpierw” (pożar, chory, głód). Harmonogram upraszczamy do 3 bloków doby (praca / odpoczynek / swobodny).

### F. Zadania (JobDriver / Toil / ThinkNode)

Opisane osobno w [RimWorld-klasy-JobDriver-IncidentWorker-ThinkNode.md]. W skrócie: drzewo `ThinkNode` wybiera `Job`, `JobDriver` wykonuje go jako listę `Toil` z warunkami porażki i akcjami początku/ticka/końca.
**Simply:** *nie dotyczy bezpośrednio* — czas płynie dniami. **Przenosimy ideę kroków** tylko tam, gdzie zadania trwają kilka dni (wyprawa handlowa, budowa, uzdrawianie): zadanie = lista kroków z warunkami przerwania.

### G. Zdrowie i choroby

- **Hediffy (324 w katalogu, 194 w Core):** rany (Cut, Scratch, Bite, Stab, Gunshot, Shredded, Burn, Crack, Bruise, Frostbite), choroby (Flu, Plague, Malaria, Infekcja rany, Zatrucie), niedobory (Niedożywienie, Hipotermia, Udar), przewlekłe (Astma, Zaćma, Zły kręgosłup, Demencja, Nowotwór), brak części ciała.
- **Nasilenie (severity)** rośnie lub maleje wg **etapów**; każdy etap ma modyfikatory **zdolności** (Świadomość, Poruszanie, Manipulacja, Mowa, Jedzenie, Wzrok, Słuch, Oddychanie, Filtracja krwi, Pompowanie krwi, Metabolizm). Wydajność pracy to iloczyn zdolności.
- **Krwawienie** z niezaopatrzonych ran; **ból** sumuje się; powyżej progu bólu (0,8) pionek zostaje powalony. **Śmierć:** próg obrażeń 150×rozmiar ciała, lub utrata zdolności krytycznej, lub hediff z `lethalSeverity` (1,0).
- **Opatrywanie (Tend):** jakość = Medycyna lekarza (baza 0,2 +0,1 za poziom) × skuteczność leku, bez lekarza 0,75, **samoleczenie ×0,7**, bez leku max 0,7; łóżko szpitalne dodaje. Dobrze opatrzone rany nie krwawią i goją się szybciej, nie zakażają.
- **Zakażenie rany:** szansa na ranę 15% (skaleczenie/postrzał/pchnięcie), 30% (ugryzienie), 20% (poszarpanie), 25% (odmrożenie); infekcja rośnie 0,84/dobę bez odporności, malejąc przy opatrunku (−0,53) i budując odporność (0,64/dobę). Czystość pomieszczenia zmienia szansę (czysto ×0,2…brudno ×1).
- **Odporność:** każda choroba ma własne tempo „zachorowania” i „odporności”; wyścig kończy się wyzdrowieniem albo śmiercią. Choroby w Core: Grypa, Zaraza, Malaria, Robaki jelitowe, Śpiączka, Pasożyty mięśni, Rozkład narządów, Mechanity.

**Simply:** *część* — osadnik ma flagę `chory`, bohater ma `rany` z szansą po ciosie ≥22% max HP; lecznica przyspiesza.
**Propozycja:** lekka wersja hediffów: lista `{typ, nasilenie, etap, opatrzone}` per osadnik, 6–8 typów (rana, infekcja, grypa, zaraza, niedożywienie, wychłodzenie, złamanie), **etapy zmieniające wydajność**, wyścig choroba–odporność liczony dziennie, zawód uzdrowiciela z jakością opatrzenia. Czystość chaty zmienia ryzyko.

### H. Walka

- Trafienie → **część ciała** (z wagą pokrycia) → obrażenie typu (tnące/tępe/ostre/cieplne) → **pancerz** warstwami: przy każdej warstwie losowanie względem `rating − przebijanie`: wynik **< połowy oceny: 0 obrażeń**, **< oceny: obrażenie ×½ (ostre staje się tępe)**, inaczej pełne; ubranie traci 25% wartości obrażenia na zniszczeniu.
- Celność z umiejętności i zdolności; unik wręcz z umiejętności i ruchu; broń z jakością (7 poziomów) zmienia obrażenia/celność.
- Powalenie, ucieczka, jeńcy, rekrutacja jeńca, leczenie po walce.

**Simply:** *jest, inna filozofia* — tury k20, garda, zapowiedzi, żywioły. Nie podmieniać. **Można dodać:** formułę pancerza w trzech progach (czytelna i taktyczna) oraz jakość przedmiotów (K).

### I. Społeczność

- **Opinia** pawna o pawnie: −100…+100 = suma myśli „społecznych” (zniewagi, wspólne posiłki, pomoc) + relacje + cechy; **Przyjaciel ≥ +20, Rywal ≤ −20**.
- **Interakcje (Core 16):** Pogawędka, Głęboka rozmowa, Zniewaga, Obraza, Miłe słowa, Zalecanie się, Oświadczyny, Zerwanie, Nawiązanie więzi ze zwierzęciem, Oswajanie, Trening, Werbunek jeńca, Skłonienie do ucieczki. Skuteczność zależy od umiejętności Społeczne i cech.
- **Relacje (26):** rodzina (rodzic, rodzeństwo, kuzyn, dziadek…), małżonek, narzeczony, kochanek, byli, więź ze zwierzęciem. Śmierć bliskiej osoby = duża ujemna myśl (−14…−20).
- **Bójki społeczne** zależą od opinii i cech (Żądny krwi ×4).
- **Rytuały i spotkania** (Ideology/Royalty): ślub (+20/+40 nastroju), przyjęcia, koncerty.

**Simply:** *jest dobrze* — `wiezi.py` ma pary −100…+100, przyjaźnie, waśnie, pary, narodziny.
**Propozycja:** dołożyć **typy interakcji** jako dzienne zdarzenia społeczne (wpływ na opinię i myśli), **oświadczyny/ślub** jako rytuał z myślą +40, **żałobę** (myśli po śmierci bliskiej osoby) i bójki przy niskiej opinii + niskim nastroju.

### J. Pomieszczenia i budynki

- Pomieszczenie jest wykrywane automatycznie (zamknięta przestrzeń) i dostaje **rolę** z wyposażenia (Sypialnia, Jadalnia, Sala rekreacyjna, Szpital, Laboratorium, Warsztat, Magazyn, Koszary, Kuchnia, Grobowiec, Stodoła…).
- **Statystyki pokoju:** Wrażenie (od „okropne” <20 przez „przeciętne” ≥30 do „bardzo imponujące” ≥85), Bogactwo (od <500 „nędzne” do ≥100 tys. najwyższy stopień), Przestrzeń (<12,5 „ciasne” … >350), Piękno (−3,5 … 100), Czystość (<−1,1 „bardzo brudne” … >0,4 „sterylne”).
- Wpływ: nastrój (wspomnienia „spałem w imponującej sypialni/jadłem w ładnej jadalni”), wskaźnik zakażeń (czystość), szansa sukcesu operacji, szybkość badań (czystość 0,75…1,15), zatrucie jedzeniem.
- **Jakość 7-stopniowa** (Awful…Legendary): piękno ×(−0,1; 0,5; 1; 2; 3; 5; 8), komfort ×(0,76…1,7).

**Simply:** *brak* (budynki mają poziomy 1–n i koszt utrzymania, bez wnętrza).
**Propozycja:** każda chata/budynek ma **jakość** i **wyposażenie** (łóżko, stół, dekoracje); z nich liczone dwie liczby (komfort, piękno) wpływają na myśli i choroby. Bez siatki komórek: pomieszczenie = budynek z listą mebli.

### K. Uprawy, jedzenie, przepisy, jakość

- **Rośliny:** każda ma czas wzrostu (dni), plon, wrażliwość na żyzność gleby i zakres temperatur (np. ryż, ziemniak, kukurydza, truskawka, bawełna, korzeń leczniczy, drzewa). Uprawa ma **sezon**: poza zakresem temperatur rośnie wolniej lub ginie.
- **Jedzenie:** wartość odżywcza (Nutrition), zepsucie (rotting), zatrucie (szansa zależna od kucharza i brudu), posiłki proste/wykwintne/luksusowe (wspomnienia +/−, np. wykwintny posiłek +12, zjedzona pasza/karma −12, trup −12).
- **Przepisy i rachunki (Bill):** 190 przepisów; rachunek = „zrób X sztuk / do zapasu Y / bez końca” na stanowisku, z filtrem materiałów i minimalnej jakości; praca w ticku pracownika, wynik ma **jakość** zależną od umiejętności (losowanie).
- **Zepsucie** magazynów zależne od temperatury.

**Simply:** *jest, prostsze* — rzemiosło z zamówieniami celu zapasu (podobne do rachunków), psujące się zapasy, rolnictwo z mnożnikiem pory.
**Propozycja:** dodać **jakość wytworów** (losowanie od umiejętności osadnika) i **jakość posiłków** (myśli), oraz zepsucie zależne od temperatury pory roku.

### L. Handel, frakcje, goście

- Cena kupna = wartość × **1,4**, sprzedaży = wartość × **0,6** (min. 0,5 / 0,01), × typ handlarza × cecha towaru × trudność. Negocjator z umiejętności Społeczne: poprawa ceny **+1,5% za poziom, do 39,5%**.
- **Frakcje** (31 w katalogu): relacje wrogość/neutralność/sojusz, dobra wola ±100 zmieniana zdarzeniami (dary, pomoc, zabicie ich ludzi). Grupy: handlarze karawan, goście, wędrowcy, sojusznicy-posiłki.
- **Karawany** wyprawiane na świat; zdarzenia na trasie.

**Simply:** *jest* — `handel.py` (normy cen, dryf ±4%, karawany z ryzykiem), `shop.py`.
**Propozycja:** wprowadzić **dobrą wolę osad** (−100…+100) zmieniającą ceny i szanse najazdów; **negocjatora** z Społecznego jako bonus do cen; wizyty **gości i wędrowców** z propozycją dołączenia (pomysł „WandererJoin” z intencją populacji).

### M. Badania

164 projekty, koszt 300–6 000 punktów, **cztery epoki** (Neolit → Średniowiecze → Przemysł → Kosmos), z wymaganiami wstępnymi; szybkość = Intelekt × czystość pracowni (0,75…1,15) × wyposażenie. Odblokowuje przepisy i budynki.
**Simply:** *część* — przepisy odkrywane eksperymentem. **Propozycja:** małe drzewo ~25 projektów w 3 epokach (osada → miasteczko → gród) jako nowa pętla długoterminowa; badacz jako zawód.

### N. Pogoda, biomy, klimat

- **Biomy (15 w Core)**, pogoda (9 typów w Core), warunki gry (Zaćmienie, Rozbłysk słoneczny, Fala upałów, Fala mrozu, Opad toksyczny, Zima wulkaniczna, Burza błyskawic, Zorza) z czasem trwania i wpływem na nastrój, uprawy i energię.
- **Temperatura:** dobowy cykl ±7 °C (cos), sezonowy ±wg szerokości geograficznej; komfort człowieka 0…40 °C przesuwany ubraniem (izolacja); poza zakresem: wychłodzenie / udar (hediff z etapami).

**Simply:** *część* — pory roku wpływają na zbiory, opał, ceny; wydarzenia: powódź, pożar, zaraza, wilki, urodzaj.
**Propozycja:** dołożyć **warunki trwające kilka dni** (fala upałów, śnieżyca, susza, zaćmienie) jako prosty obiekt `{typ, dni_pozostało, efekty}` oraz **ubranie z izolacją** (zamiast flagi „ciepłe odzienie”).

### O. Questy

`QuestScriptDef` (156) generują zadania z częściami (QuestPart, 243 klasy): przyjmij, wyślij ludzi, dostarcz, obroń, zdobądź; nagrody i warunki wygaśnięcia. Narrator wydaje je według komponentu `RandomQuest`.
**Simply:** *jest, statyczne* — `quests.py` z kilkoma ustalonymi questami.
**Propozycja:** generator z szablonów (cel + nagroda + limit dni) zależny od punktów i populacji; „uchodźcy proszą o schronienie”, „handlarz prosi o eskortę” itp.

### P. Dodatki (jednozdaniowo, bez przenoszenia na razie)

| Dodatek | Rdzeń | Dla Simply |
|---|---|---|
| Royalty | tytuły szlacheckie, psycasty, żądania władcy | pomysł **tytułów osady** (wójt → burmistrz) jako awans |
| Ideology | memy, precepty, rytuały, kultura | **wierzenia osady** i rytuały (uzupełniają tawernę/świątynię) |
| Biotech | dzieci, geny, mechanitor | narodziny i **wzrost dzieci** (już są narodziny w `wiezi.py`) |
| Anomaly | encje, kontenment, groza | poza tonem gry |
| Odyssey | statek, nowe biomy, kosmos | poza tonem gry |

---

## 4. Zestawienie z Simply i rekomendowane etapy

| # | Mechanika | Dlaczego | Rozmiar | Ryzyko |
|---|---|---|---|---|
| 1 | **Myśli i nastrój osadnika** (C) | Daje „historię”, napędza wszystko inne | średni | niskie, z migracją zapisu |
| 2 | **Storyteller z punktami i adaptacją** (A) | Zastępuje licznik zagrożenia, wprost wpływa na balans | średni | średnie (balans) |
| 3 | **Umiejętności 0–20 i pasje** (D) | Osadnicy stają się różni | średni | niskie |
| 4 | **Priorytety pracy** (E) | Gracz decyduje o kolejności, a nie o jednym zawodzie | większy | średnie (UI) |
| 5 | **Zdrowie jako hediffy** (G) | Choroby i rany z konsekwencją | większy | średnie |
| 6 | **Potrzeby: sen, rozrywka, komfort** (B) | Zasilają system myśli | mały | niskie |
| 7 | **Jakość przedmiotów i chat** (J, K) | Powód do ulepszania i dekorowania | średni | niskie |
| 8 | **Interakcje społeczne i ślub** (I) | Rozbudowa `wiezi.py` | mały | niskie |
| 9 | **Dobra wola osad i goście** (L) | Spina handel, najazdy, populację | średni | średnie |
| 10 | **Drzewo badań** (M) | Długa pętla | większy | niskie |
| 11 | **Warunki pogodowe i ubranie** (N) | Urozmaicenie zimą | mały | niskie |
| 12 | **Generator questów** (O) | Treść | większy | średnie |

**Stan realizacji (26.10.2026, wersja pythonowa; port Unity jeszcze nie ruszył):**

| # | Stan | Co dokładnie jest, a czym różni się od RimWorlda |
|---|---|---|
| 3 | **zrobione** | `game/kompetencje.py`: 10 umiejętności 0–20 (nie 12 — bez Strzelania, Gotowania, Zwierząt, Sztuki), pasje jak w RimWorldzie: brak / mała 🔥 / duża 🔥🔥 (mnożniki nauki 0,35 / 1 / 1,5; dodatkowo +4/+8 do celu morale przy ulubionej pracy), krzywa XP z RimWorlda podzielona przez 25, start losowany z imienia |
| 10 | **zrobione w uproszczeniu** | `game/badania.py`: jeden „stół” (skryptorium) zamiast stołów i banków badań, 27 projektów w 3 epokach (neolit / średniowiecze / przemysł → tu: drewno i kamień / żelazo / mistrzostwo), punkty dzienne zamiast pracy pawna przy stole; bramki: wymagania, epoka, poziom skryptorium |
| 4 | częściowo | `game/budowa.py`: priorytet to rola (budowniczy → bezczynni → reszta „po godzinach”), nie macierz priorytetów 1–4 per praca. Pełne priorytety pracy wciąż do zrobienia |
| J | częściowo | Budowa jako proces w czasie z rusztowaniem (blueprint → frame → gotowe); bez jakości, bez pomieszczeń |

**Czego świadomie NIE przenosimy:** siatki komórek, wyszukiwania ścieżek i regionów, renderowania, zwierząt gospodarskich, dodatków Anomaly/Odyssey. To silnik symulacji w czasie rzeczywistym, a Simply jest dzienną strategią z RPG.

**Zasada techniczna:** każdy etap najpierw w Pythonie (`game/`, wzorzec) z testami `unittest`, potem port do `unity/`. Zapis (`savegame.py`) zawsze z migracją i wartościami domyślnymi. Liczby z tabel są **punktem startowym**, nie obietnicą: balans sprawdzamy botami jak w `ANALIZA.md` §5–6.

---

## 5. Czego nie sprawdziłem

- Pełnej treści 9 000 plików poza rdzeniem (UI, generatory map, dodatki).
- Dokładnych krzywych dla roślin, wagi myśli społecznych i listy wszystkich 949 myśli (mam je w katalogach, nie w tym dokumencie).
- Rzeczywistej rozgrywki: nic nie uruchamiałem, więc „jak to się czuje” to wnioskowanie z liczb.
- Rozbieżności numerów wersji (`Version.txt` 1.6.4871 vs. metadane `Assembly-CSharp` 1.6.9676.17735): nie zweryfikowałem w sieci.

Materiały robocze (poza repozytorium): `C:\Users\acer\AppData\Local\Temp\claude\P--Kod-VC-GitHub-Game-Simply\f46cc6ae-6644-4018-a46d-460813eba41e\scratchpad\` — `rw-src` (zdekompilowany C#), `catalog` (wyciągi z XML), `RimWorld-analiza.md`, `RimWorld-klasy-JobDriver-IncidentWorker-ThinkNode.md`.
