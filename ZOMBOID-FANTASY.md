# Zomboid fantasy — tryb przetrwania w czasie rzeczywistym (Unity)

Drugi tryb gry, obok fabularnej gry turowej: **wielki świat z kafli, bohater chodzi swobodnie, zaczyna bez
niczego i ze śpiworem**, zbiera zasoby, stawia ognisko i namiot, a potem zakłada osadę i nią zarządza.
Wybierasz go w menu głównym: **[2] Przetrwanie — świat w czasie rzeczywistym**.

![dzień](unity/podglad/wolny/wolny-dzien.png)
![noc](unity/podglad/wolny/wolny-noc.png)

## Sterowanie
| Klawisz | Co robi |
|---|---|
| WASD albo strzałki | ruch względem ekranu (w rzucie skośnym to przekątne siatki), ślizg wzdłuż przeszkód |
| E albo spacja (przytrzymaj) | zbieraj to, co jest przed bohaterem: ścinaj drzewa, łup skały, zrywaj jagody i zioła |
| 1 / 2 / 3 / 4 | rozłóż śpiwór / zapal ognisko (5 drewna, 3 kamienie) / postaw namiot (8 drewna, 6 włókna) / postaw chatę (12 drewna, 6 kamieni) |
| T | odłóż plecak do magazynu (przy obozie) |
| Tab | panel osady: W/S osadnik, A/D zajęcie |
| F | zjedz jagody |
| Z | śpij (przy śpiworze albo namiocie; czas płynie 40× szybciej, w namiocie regeneracja jest szybsza) |
| Esc | powrót do menu głównego |

## Faza 1: przetrwanie (zrobione)
- **Świat 2001×2001 kafli**, generowany z ziarna, bez zapisanych danych (`Logika/Wolny/GeneratorSwiata.cs`):
  biomy z szumu na współrzędnych świata (wzorzec z Project Zomboid, patrz `PROJECT-ZOMBOID-ANALIZA.md`) z rozmytymi,
  nieregularnymi granicami; woda w bagnach, ściany w ruinach, drzewa, skały, krzaki z jagodami, zioła.
  Polana startowa 9×9 jest pusta. Poza granicą — nieprzebyta mgła.
- **Ruch z kolizjami** na kaflach, prędkość 3,4 kafla/s, osie osobno (ślizg).
- **Zbieranie**: każde użycie trwa 0,9 s, drzewa i skały mają 3 użycia, krzaki i zioła odrastają po 2–3 dobach.
- **Potrzeby**: głód (jagody), zmęczenie (sen), życie; doba trwa 8 minut, noc jest ciemna, światło mają ognisko i latarnia.
- **Obóz**: po postawieniu namiotu i ogniska gra ogłasza, że obóz stoi.
- **Scena** izometryczna 32×16 px z drzewami, skałami, wodą, namiotem, ogniskiem; interfejs fantasy (pasek stanu, plecak,
  dziennik, podpowiedź).

## Faza 2: osada (zrobione)

![osada](unity/podglad/wolny/wolny-osada.png)
![panel](unity/podglad/wolny/wolny-panel.png)

- **Przybysze:** gdy stoją namiot i ognisko, o północy przychodzi pierwszy człowiek, potem kolejni (40% dziennie), o ile jest miejsce. Namiot daje 2 miejsca, **chata** (klawisz 4: 12 drewna, 6 kamieni) 3.
- **Magazyn:** T przy obozie odkłada drewno, kamień, zioła i jagody (jako żywność). Budowanie bierze najpierw z plecaka, potem z magazynu; F przy obozie je z zapasów, gdy plecak jest pusty. Włókno zostaje w plecaku.
- **Zarządzanie:** Tab otwiera panel: W/S wybiera osadnika, A/D zmienia zajęcie (bez zajęcia, drwal, kamieniarz, zielarz, myśliwy, górnik). Świat w tym czasie płynie dalej.
- **Gospodarka:** ten sam `Osada.DzienOsady` co w grze fabularnej, raz na dobę: produkcja do magazynu, jedzenie, opał zimą, morale, choroby, umiejętności i pasje. Bez jedzenia ludzie głodują i odchodzą.
- **Życie na mapie:** osadnicy idą trasą po kaflach (BFS omija przeszkody) do drzewa, skały, ziół lub krzaka właściwego dla zawodu, kręcą się przy pracy, a nocą śpią przy budowlach. Nad głowami mają imiona.

Świadome uproszczenia: praca osadników nie zużywa drzew i skał na mapie (produkcja idzie wprost do magazynu), chata stawia się od razu (bez rusztowań i budowniczych), skryptorium i budynki z gry fabularnej nie są jeszcze w tym trybie, a więzi i myśli są nieaktywne jak w całym porcie.

## Co dalej (jeszcze niezrobione)
1. Budowa kafel po kaflu z rusztowaniami, budynki produkcyjne (pola, tartak, skryptorium z badaniami).
2. Zagrożenia nocą (wilki, bandyci), obrona, palisada.
3. Zapis świata i postaci, narzędzia (siekiera, kilof), kupcy, drogi.
4. Punkty zainteresowania (ruiny, jaskinie, miasta) z istniejącego generatora regionów.

## Techniczne
`SwiatWolny.Aktualizuj(dt, Wejscie)` to czysta logika bez Unity (testy: `unity/Testy/TestyWolnego.cs`), `ScenaWolna` rysuje
stan, `SilnikGry` liczy klatkę na wątku głównym, a wątek logiki tylko czeka (`Gra.Przetrwanie_`).
Podgląd klatek: `cd unity/Testy && dotnet run -- --wolny <katalog>`.
