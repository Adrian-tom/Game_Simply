# Project Zomboid → Simply: generowanie świata, ruch i grafika

Analiza statyczna instalacji Project Zomboid (build 42, `WorldVersion` 249 w `IsoWorld`), bez uruchamiania gry.
Narzędzia: REA (tabele eksportów bibliotek natywnych), własny czytnik plików `.class` z `projectzomboid.jar`
(stałe, pola, metody, pule stałych — skrypt roboczy poza repozytorium) oraz przegląd katalogów `media/`.
Gra jest napisana w Javie z natywnymi bibliotekami JNI; Ghidry nie użyłem do dekompilacji, bo eksporty
natywnych DLL-i (nazwy funkcji JNI) w pełni wskazują, co jest policzone natywnie, a logika świata siedzi w klasach Javy.

**Zasada jak przy RimWorldzie:** biorę pomysły i liczby jako punkt wyjścia do własnego rozwiązania, nie kod ani zasoby.

## 1. Co sprawdziłem (fakty z plików)

### Struktura świata (`zombie/iso`)
| Fakt | Wartość | Źródło |
|---|---|---|
| Kwadrat świata (pole) | podstawowa jednostka, `IsoGridSquare` | klasy |
| Chunk | **8×8 pól** (`CHUNK_SIZE_IN_SQUARES`, `CHUNKS_PER_WIDTH` = 8; stare 10) | `IsoChunkMap` |
| Okno załadowane wokół gracza | **13×13 chunków** (`START_CHUNK_GRID_WIDTH`) | `IsoChunkMap` |
| Komórka (cell) | **32×32 chunki = 256×256 pól** | `IsoCell` |
| Piętra | 64 poziomy (−32…31), parter = 32 | `IsoChunkMap` |
| Meta-siatka | komórki z **zonami, budynkami, pokojami** (`IsoMetaGrid`, `IsoMetaCell`), poza chunkami | klasy |
| Zapis | chunki zapisywane osobno, nagłówek 17 B, blok 64 KiB | `IsoChunk` |

### Generowanie proceduralne (`zombie/iso/worldgen`, nowość B42)
- `WorldGenParams`: ziarno z **napisu** (`seedString.hashCode()` → `java.util.Random`), zasięg komórek, zapis w `map_worldgen.bin`.
- `WorldGenSimplexGenerator`: **kilka niezależnych warstw szumu simplex** — `tree`, `noise` (rzeźba), `plant`, `bush`,
  `temperature`, `hygrometry`, `ore` — plus warstwa `selector`.
- `BiomeType`: cechy `Landscape`, `Plant`, `Bush`, `Temperature`, `Hygrometry`, `OreLevel`; `BiomeNoise` to rekord
  (`landscape;plant;bush;temperature;hygrometry;ore`); `BiomeMap`/`BiomeRaster` zamieniają kombinację cech na biom.
- `FeatureType`: `GROUND`, `PLANT`, `BUSH`, `TREE`, `ORE` — biom to **zestaw cech nakładanych na grunt**.
- `Blending` (z `BlendDirection`): po wygenerowaniu chunku **wygładza szew z sąsiednimi** (zmiana gruntu,
  usuwanie drzew przy krawędzi); `IsoChunk` trzyma `chunkGenerationStatus`, `blendingDone{Full,Partial}`.
- `RoadGenerator`: **drogi z triangulacji Delaunaya** losowych punktów, krawędzie filtrowane losowo,
  węzły (`RoadNexus`) łączą trasy; stałe `256.0` (rozmiar komórki) w obliczeniach.
- Reszta świata to ręcznie malowana mapa (`media/maps/*` — `.lotheader`/`.lotpack`, Muldraugh, Rosewood, Riverside…).

### Ruch i kolizje
- `IsoMovingObject`: pozycja ciągła `x,y,z`, poprzednia `lastX`, żądany ruch `reqMovement`, znaczniki
  kolizji z **krawędziami** pola `collidedN/E/S/W`, `collidedThisFrame`, `impulsex/y`.
- `IsoGameCharacter` ma klasę `LC_slideAwayFromWalls` — **ślizg wzdłuż ścian**, żeby postać nie klinowała się na rogach.
- `IsoPlayer.MoveVars`: `moveX, moveY, strafeX, strafeY, newFacing` — ruch wejściowy rozdzielony na osie i obrót.
- `IsoGridSquareCollisionData` (punkt trafienia, wyniki testów) — kolizje liczone na polach, nie na pikselach.
- `PZPathFind64.dll` (REA, 13 eksportów JNI): `initWorld`, `updateChunk`, `updateSquare`, `removeChunk`, `findPath`,
  `update`, `addVehicle`… — **wyszukiwanie ścieżek natywne, aktualizowane per chunk i per pole** (a nie przebudowywane globalnie).

### Grafika i światło
- Rzut izometryczny w `IsoUtils.XToScreen/YToScreen/XToIso`, skalowany `Core.tileScale`.
- `Lighting64.dll` (REA, 50 eksportów JNI): światło **liczone natywnie na osobnym wątku** — `squareSet`,
  `getVertLight` (światło w wierzchołkach pola), `addLight/addRoomLight/addTempLight`, `updateTorch`,
  `squareAddDoor/Window/Curtain/Thumpable` i `squareSetLightTransmission` (**przepuszczalność światła** przez drzwi, okna, zasłony),
  `getCanSee/getCouldSee/getSeen` (**pole widzenia i pamięć odkrytych pól**), `getDarkMulti` (przyciemnienie doby),
  `scrollUp/Down/Left/Right` (okno światła przesuwa się razem z oknem chunków).
- `IsoCell`: stałe `RTF_*` (warstwy renderu: podłoga, roślinność, cienie…), `mustSeeSquaresRadius` = 4,
  odcinanie ścian (`cutaway`) i rozpuszczanie przesłon, `CalculateVertColoursForTile`.

## 2. Co przeniosłem do Simply

| Wzorzec z Zomboid | Co zrobiłem | Gdzie |
|---|---|---|
| Biom jako **cechy terenu z szumu na współrzędnych świata** (temperatura, wilgotność, rzeźba) zamiast sklejania kawałków | Biom pola = funkcja `(ziarno, x, y)` świata: rzeźba, wilgotność, temperatura i „ruiny” z gładkiego szumu wartości, tabela progów wybiera biom (równiny, las, bagna, ruiny, wzgórza, kanion). Wcześniej każdy region miał własne, niezależne klastry Voronoi, więc biomy urywały się na granicy regionu | `game/szum.py`, `game/mapa.py` (`biom_swiata`), `Logika/Szum.cs`, `Logika/Mapa.cs` |
| **Ciągłość** przez granice chunków (`Blending`) | Dzieje się samo: ten sam szum po obu stronach granicy. Test: zgodność biomów po obu stronach granicy regionu > 60% (stary generator ~25%) | `tests/test_mapa.py` |
| Ziarno świata → powtarzalny wynik | Bez zmian (ziarno postaci), ale biom nie zależy już od kolejności losowań w regionie | — |
| Bezpieczny start | Pola w promieniu 2 od obozu to zawsze równiny (spawn w bezpiecznym miejscu) | `biom_swiata` |
| **Ślizg wzdłuż przeszkód** (`slideAwayFromWalls`) | Bohater chodzący po obozie, gdy przekątna jest zajęta, ślizga się jednym z dwóch kroków składowych (zrobione wcześniej w tej sesji, potwierdzone jako ten sam wzorzec) | `Logika/ChodzenieObozu.cs` |
| Ruch **względem ekranu** w rzucie izometrycznym | Strzałki to przekątne siatki; kroki natychmiastowe w logice, płynne na ekranie | `ChodzenieObozu.cs`, `ScenaOsady.cs` |

Zgodność Python ↔ C# dla nowego generatora pilnują wzorce `mapa.json` (te same biomy co do pola dla 13 regionów
i różnych ziaren) — wygenerowane na nowo; wszystkie 858 sprawdzeń C# i 280 testów Pythona przechodzą.

**Uwaga o istniejących zapisach:** regiony już odwiedzone są zapisane w pliku, więc zostają bez zmian. Nowo odkrywane
regiony dostaną biomy z szumu, więc na styku „starego” i „nowego” świata może być widoczny szew — tylko w starych zapisach.

## 3. Czego nie przeniosłem i dlaczego

| Pomysł | Powód |
|---|---|
| Szum **simplex** i wielowarstwowa tablica biomów (`BiomeMap`) | Mamy sześć biomów i siatkę 9×9 na region — prosty szum wartości z progami wystarcza; simplex można podmienić w `szum.py`/`Szum.cs` bez zmiany reszty |
| **Drogi z triangulacji Delaunaya** (`RoadGenerator`) | To zmiana rozgrywki (drogi skracałyby podróż, trzeba by zmienić koszt prowiantu i zbieractwo). Dobry kandydat na następny etap: łączyć karczmy, kuźnie i miasta w regionie |
| **Światło per wierzchołek** i pole widzenia (`getCanSee/Seen`) | Duży koszt w scenie (nowe bufory światła, nowe testy obrazu); mamy już światło dnia, ognisko i okna. Najtańszy krok: „pamięć” odkrytych pól jest już w `Pole.Odkryte` |
| **Przepuszczalność światła** drzwi i okien | Wymaga modelu pomieszczeń, którego w Simply nie ma |
| Chunki 8×8 i okno 13×13 ładowane wokół gracza | Nasz świat to regiony 9×9 generowane na żądanie i trzymane w słowniku — odpowiednik już istnieje |
| Natywne ścieżki per chunk (`PZPathFind`) | Mamy A* na siatce 7×7 placu; aktualizacja per pole byłaby przedwczesna |
| Ręcznie malowana mapa i meta-siatka zon/budynków | Inny model świata (mapa Kentucky); Simply generuje proceduralnie |

## 4. Czego nie sprawdziłem
- Sposobu, w jaki simplex i progi biomów są **dokładnie** policzone w `BiomeMap` (bytecode metod nie dekompilowałem) —
  to, co opisuję, wynika z nazw klas, pól, stałych i napisów, nie z odczytanej logiki.
- Rzeczywistego wyglądu gry; nic nie uruchamiałem.
- Czy 256×256 pól komórki wynika z formatu zapisu, czy z decyzji projektowej — wiem tylko, że tak jest w `IsoCell`.
