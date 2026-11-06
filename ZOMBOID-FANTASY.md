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
| B albo kafelek Buduj | tryb budowania: kafelki śpiwór, ognisko (5 drewna, 3 kamienie), namiot (8 drewna, 6 włókna), chata (12 drewna, 6 kamieni), palisada (3 drewna), brama (5 drewna), wieża (10 drewna, 6 kamieni); duch na mapie pokazuje miejsce (zielony: wolno, czerwony: nie) |
| lewy klik / Enter | postaw wybraną budowlę pod kursorem, do 6 kafli od bohatera; palisadę można stawiać przeciągnięciem |
| prawy klik / Esc | wycofaj wybór, potem wyjdź z trybu albo zamknij panel |
| T albo kafelek Magazyn | odłóż plecak do magazynu (przy obozie) |
| Tab albo kafelek Osada | panel osady: klik w osadnika, potem kafelek zajęcia (W/S i A/D też działają) |
| lewy klik albo J | lekki cios w stronę kursora |
| R albo K | ciężki cios: wolny, ale mocny i przerywa nawet wojownika |
| prawy klik (trzymaj) albo L | blok; wciśnięty tuż przed trafieniem to parowanie |
| Shift | unik z chwilą nietykalności |
| Q / C / V albo kafelki (tylko nocą) | ciosy specjalne: wir, szarża, okrzyk |
| F | zjedz jagody |
| Z | śpij (przy śpiworze albo namiocie; czas płynie 40× szybciej, w namiocie regeneracja jest szybsza) |
| Esc (gdy nic nie jest otwarte) | powrót do menu głównego |

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

- **Przybysze:** gdy stoją namiot i ognisko, o północy przychodzi pierwszy człowiek, potem kolejni (40% dziennie), o ile jest miejsce. Namiot daje 2 miejsca, **chata** (kafelek w trybie budowania: 12 drewna, 6 kamieni) 3.
- **Magazyn:** T przy obozie odkłada drewno, kamień, zioła i jagody (jako żywność). Budowanie bierze najpierw z plecaka, potem z magazynu; F przy obozie je z zapasów, gdy plecak jest pusty. Włókno zostaje w plecaku.
- **Zarządzanie:** Tab otwiera panel: klik wybiera osadnika, kafelek ustawia zajęcie (bez zajęcia, drwal, kamieniarz, zielarz, myśliwy, górnik). Świat w tym czasie płynie dalej.
- **Gospodarka:** ten sam `Osada.DzienOsady` co w grze fabularnej, raz na dobę: produkcja do magazynu, jedzenie, opał zimą, morale, choroby, umiejętności i pasje. Bez jedzenia ludzie głodują i odchodzą.
- **Życie na mapie:** osadnicy idą trasą po kaflach (BFS omija przeszkody) do drzewa, skały, ziół lub krzaka właściwego dla zawodu, kręcą się przy pracy, a nocą śpią przy budowlach. Nad głowami mają imiona.

Świadome uproszczenia: praca osadników nie zużywa drzew i skał na mapie (produkcja idzie wprost do magazynu), chata stawia się od razu (bez rusztowań i budowniczych), skryptorium i budynki z gry fabularnej nie są jeszcze w tym trybie, a więzi i myśli są nieaktywne jak w całym porcie.

## Faza 3: walka (zrobione)

![noc](unity/podglad/3d/walka-noc.png)

- **W dzień jak w Souls:** każdy cios i unik zużywa wytrzymałość (zielony pasek nad imieniem), a poniżej 6 punktów trzeba złapać oddech. Wrogów jest mało, najwyżej dwóch włóczęgów, ale każdy groźny: wojownik ma 120 życia i bije za 28.
- **Blok, parowanie, riposta:** blok działa tylko od przodu i płaci wytrzymałością, a gdy jej zabraknie, garda pęka. Wciśnięcie bloku w ostatnich 0,2 s przed trafieniem to parowanie: ogłusza napastnika i przez 1,3 s następny cios rani 2,6 razy mocniej.
- **Zapowiedzi:** przed ciosem pod wrogiem rośnie czerwona plama, więc jest czas na reakcję. Naraz uderza najwyżej dwóch.
- **W nocy tłumy:** od 21:00 do 5:00 co 1,5 godziny gry fala rusza z mroku na obóz, a jeśli bohater jest blisko albo obozu nie ma, na niego. Każda doba daje większą falę, a od drugiej dochodzą łotrzyki i magowie z pociskami, od czwartej wojownicy. Na scenie jest najwyżej 14 wrogów. Fala budzi śpiącego, a z wrogiem w pobliżu nie da się zasnąć.
- **Ciosy specjalne:** budzą się tylko nocą i działają na czas odnowienia: wir (dookoła, 8 s), szarża (przez wrogów, 10 s), okrzyk (ogłusza wszystkich blisko, 16 s).
- **Osada w ogniu:** szkielety niszczą budowle i zabijają osadników. O świcie reszta się rozsypuje, a budowle wracają do pełnej wytrzymałości.
- **Śmierć:** pobudka w obozie, przy śpiworze albo na polanie startowej. Plecak zostaje na ziemi, trzeba po niego wrócić i podnieść go klawiszem E.

Świadome uproszczenia: za zabijanie nie ma nagród ani rozwoju, osadnicy się nie bronią, a broń bohatera nie zmienia ciosów. Liczby balansu sprawdziły testy, ale nikt jeszcze nie ocenił ich w dłuższej grze.

## Faza 4: obrona i zapis (zrobione)

![fort](unity/podglad/3d/obrona-dzien.png)

- **Palisada i brama:** palisada zatrzymuje wszystkich. Przez bramę przechodzą bohater i osadnicy, a szkielety nie. Szkielet, któremu mur stanie na drodze, zaczyna go burzyć (palisada 90 życia, brama 70).
- **Wieża:** strzela sama do najbliższego wroga w zasięgu 7,5 kafla, 14 obrażeń co 1,1 s. Ma 160 życia.
- **Strażnik:** nowe zajęcie w panelu osady. Ma dwa razy więcej życia (90 zamiast 45), a wroga w zasięgu 12 kafli atakuje sam (13 obrażeń co 1,1 s), także w dzień. Nie zbiera surowców, a chory nie walczy.
- **O świcie:** ocalałe budowle i ludzie wracają do pełnego życia. Zniszczone budowle trzeba postawić od nowa.
- **Zapis:** gra zapisuje się o świcie, przy wyjściu do menu i przy zamknięciu okna. W menu pojawia się **[3] Kontynuuj przetrwanie**, a **[2]** zaczyna nowy świat i przy pierwszym zapisie zastępuje stary. Poprzedni zapis zostaje jako kopia i wraca, gdy główny plik jest uszkodzony.
- **Co jest w zapisie:** świat, potrzeby, plecak, budowle i ich rany, ścięte drzewa, magazyn, osadnicy z umiejętnościami, wrogowie i porzucone plecaki. Plik leży w `%USERPROFILE%\AppData\LocalLow\DefaultCompany\GraSimply\przetrwanie.txt`.

Świadome uproszczenia: szkielety nie szukają bramy ani dziury w murze, tylko idą prosto i burzą to, co stoi na drodze. Strażnicy nie dostają rozkazów. Czas odnowienia ciosów specjalnych, trwające ciosy i dokładne trasy osadników nie są zapisywane.

## Co dalej (jeszcze niezrobione)
1. Budowa kafel po kaflu z rusztowaniami, budynki produkcyjne (pola, tartak, skryptorium z badaniami).
2. Nagrody i rozwój za walkę.
3. Narzędzia (siekiera, kilof), kupcy, drogi.
4. Punkty zainteresowania (ruiny, jaskinie, miasta) z istniejącego generatora regionów.

## Techniczne
`SwiatWolny.Aktualizuj(dt, Wejscie)` to czysta logika bez Unity (testy: `unity/Testy/TestyWolnego.cs`), `ScenaWolna` rysuje
stan, `SilnikGry` liczy klatkę na wątku głównym, a wątek logiki tylko czeka (`Gra.Przetrwanie_`).
Podgląd klatek: `cd unity/Testy && dotnet run -- --wolny <katalog>`.
