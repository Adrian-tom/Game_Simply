# Koncept: tryb miasta

Stan: decyzje podjęte 7.11.2026 (1A, 2A, 3A, 4A). Etapy M0 i M1 są zrobione, następny jest M2.

### Co działa po M0 i M1

- Awans na szczebel Miasto sam przełącza grę: kamera unosi się, a nocne fale ustają do etapu M5.
- **Sterowanie:** WASD kamera, kółko przybliżenie, B budowa, Tab rada miasta, H bohater i z powrotem. Gdy steruje się kamerą, bohater czeka i nie głodnieje.
- **Budowa:** droga (ciągnięta myszą), dom 2×2, skład 3×3, wyburzanie. Las i skały pod budową znikają bez plonu.
- **Sieć:** dom zapełnia się tylko przy drodze prowadzącej do składu, do 24 kafli drogi. Droga w zasięgu jest jasna, poza zasięgiem czerwona, a odcięty dom ma znak „!”.
- **Start do testów:** `cd unity/Testy && dotnet run -- --zapis-miasta plik.txt 0`, potem `GraSimply.exe --zapis plik.txt` i Kontynuuj. Druga liczba to gotowe domy, np. 200 do pomiaru.
- **Pomiar:** `--pomiar plik.txt` dopisuje co 5 sekund liczbę klatek.

Uproszczenia do czasu kolejnych etapów: budowa miejska jest natychmiastowa, domy nie mają potrzeb, a mieszkańcy nie płacą podatków. Skład ma tymczasowo model zamku, a materiały trzeba mieć z wcześniejszej gry albo zebrać bohaterem.

Pomiar na laptopie z 200 domami (w kadrze widać tylko część): stałe 30 klatek na sekundę, czyli limit ustawiony w grze. Prawdziwy test wydajności czeka na pełne miasto w kadrze i wózki z M2.

## O co chodzi

Gra zaczyna się jako przetrwanie: jeden bohater, śpiwór i nocne szkielety. Gdy osada wejdzie na szczebel **Miasto**, gra sama zmienia się w city buildera: kamera idzie w górę, a gracz planuje dzielnice, drogi, kopalnie, tartaki, domy i koszary. Świat, budynki i ludzie zostają ci sami, więc to dalszy ciąg tej samej rozgrywki, a nie nowa gra.

Mechaniki to wzorce gatunku, a nazwy, klasy mieszkańców, budynki i świat są nasze. Nie odtwarzamy Anno 1800: bierzemy z gatunku tylko to, jak działają potrzeby i logistyka.

## Co bierzemy z gatunku

Sprawdzone na wiki Anno 1800 ([produkcja](https://anno1800.fandom.com/wiki/Production), [populacja](https://anno1800.fandom.com/wiki/Population)):

| Wzorzec w Anno 1800 | Jak to wygląda u nas |
|---|---|
| Towary wożą wózki po drogach, a budynek musi być w zasięgu magazynu | Tak samo: droga i magazyn decydują, co działa |
| Dom przechodzi na wyższą klasę, gdy jest pełny, ma spełnione potrzeby podstawowe i są materiały | Tak samo, ale z własnymi klasami |
| Potrzeby podstawowe zwiększają liczbę mieszkańców w domu | Tak samo |
| Potrzeby dodatkowe dają zadowolenie i wyższe podatki | Tak samo |

## Pętla gry

1. Stawiasz domy przy drodze i dajesz im dostęp do targu.
2. Mieszkańcy czegoś potrzebują, np. jedzenia, ubrań czy piwa.
3. Budujesz łańcuch produkcji i magazyn, który to dostarcza.
4. Domy się zapełniają i płacą podatki.
5. Klasa awansuje, dostajesz nowe budynki, ale też nowe potrzeby i wyższe koszty.
6. Większe miasto przyciąga większe zagrożenie, więc rozbudowujesz obronę.

### Klasy mieszkańców (nazwy robocze)

| Klasa | Potrzeby podstawowe | Potrzeby dodatkowe | Co odblokowuje |
|---|---|---|---|
| Osadnicy | jedzenie, drewno na opał | tawerna | farmy, tartaki, kamieniołomy |
| Rzemieślnicy | chleb, ubrania | piwo, kościół | kopalnie, huty, kuźnie |
| Mieszczanie | mięso, narzędzia | wino, łaźnia | koszary, skryptorium, rynek |
| Patrycjusze | zbroje, książki | teatr | zamek, wielkie budowle |

### Przykładowe łańcuchy

- **Chleb:** farma → młyn → piekarnia → targ
- **Narzędzia:** kopalnia rudy → huta (z węglem z drewna) → kuźnia → targ
- **Ubrania:** owce → tkalnia → krawiec → targ
- **Uzbrojenie:** kopalnia → huta → płatnerz → koszary

## Co już mamy, a czego brak

| Potrzebne | Stan |
|---|---|
| Świat | ±1000 kafli i generator bez zapisanych kafli, więc na miasto wystarczy |
| Modele | są: domy (2), tartak, kopalnia, koszary, rynek, tawerna, kościół, zamek, kuźnia, wiatrak, młyn wodny, studnia, wieże, strzelnica |
| Budynki i badania | 17 budynków i 27 projektów badań z Nowej gry do przerobienia |
| Interfejs | panel boczny 1280×720 się nadaje; trzeba dodać nakładki zasięgu i potrzeb |
| **Drogi** | **brak modelu i logiki**; drogi trzeba dorobić na siatce gruntu |
| **Tryb kamery** | **brak**; dziś kamera idzie za bohaterem |
| **Symulacja miasta** | **brak**; dzisiejsza gospodarka liczy się raz dziennie dla pojedynczych osób |
| **Budynki na kilka kafli** | **brak**; dziś każdy budynek zajmuje jeden kafel |
| **Wydajność** | **niezmierzona**; setek budynków i wózków gra jeszcze nie widziała |

## Dwa problemy do rozwiązania wcześniej

**1. Do miasta dziś nikt nie dochodzi.** W symulacji żaden bot nie osiągnął nawet miasteczka. Bot nie umie budować obrony, więc wynik jest zaniżony, ale bez naprawy tryb miasta będzie treścią, której mało kto zobaczy.

**2. Droga jest długa.** Przybysz przychodzi najwyżej raz na dobę, a doba trwa 8 minut. 30 osadników to co najmniej 30 dób, czyli do 4 godzin gry, mniej przy przesypianiu nocy. Do tego dochodzą chaty, które wymagają tartaku i po 240 sekund pracy jednej osoby na każdą. Warto obniżyć próg albo przyspieszyć napływ ludzi.

Żeby pracować nad trybem miasta bez tej drogi, dodamy skrót startowy tylko do testów, który zaczyna grę od razu na szczeblu Miasto.

## Decyzje

Wybrane: 1A, 2A, 3A, 4A. Pozostałe warianty zostają dla porządku.

### 1. Jak wygląda przejście

- **A. Na tej samej mapie (polecam):** krótka scena założenia miasta, kamera płynnie idzie w górę. Wszystko, co zbudowałeś, stoi dalej. Najmocniej działa, ale stara, chaotyczna zabudowa zostaje i trzeba ją przebudować.
- **B. Nowy, wybrany teren:** gracz wybiera dolinę na mapie, a stara osada zostaje jako dzielnica-zalążek. Czystszy start, ale traci się ciągłość.
- **C. Osobna mapa regionu:** miasto na oddzielnej, przygotowanej mapie, a stara osada staje się bonusem startowym. Najłatwiejsze technicznie, ale to już prawie osobna gra.

### 2. Co z bohaterem

- **A. Zostaje jako postać na zawołanie (polecam):** domyślnie steruje się kamerą, a klawisz przełącza na bohatera, który może iść na wyprawę albo bronić murów. Zachowuje walkę, którą już mamy.
- **B. Znika:** czysty city builder, sama kamera. Najprościej, ale wyrzuca cały system walki z dalszej gry.
- **C. Staje się dowódcą:** bohater nie chodzi sam, tylko prowadzi oddziały z koszar. Najciekawsze, ale wymaga nowego systemu oddziałów.

### 3. Jakie zagrożenie w mieście

- **A. Zapowiedziane najazdy (polecam):** zamiast fal co noc przychodzi co jakiś czas najazd, zapowiedziany z wyprzedzeniem, a siła zależy od wielkości miasta. Gra ma już podobny mechanizm w Nowej grze. Daje czas na spokojne budowanie.
- **B. Nocne fale jak dziś, ale obronę prowadzą strażnicy i wieże:** ciągłość z przetrwaniem, ale przy dużym mieście noc może stale odciągać od budowania.
- **C. Bez wrogów, sama ekonomia:** porażka to bunt, głód i bankructwo. Najspokojniej, ale koszary i mury tracą sens.

### 4. Kim są mieszkańcy

- **A. Liczby w domach, a nazwani osadnicy zostają jako rada (polecam):** dom ma np. 8 mieszkańców, a ulice ożywia kilku przechodniów. Twoi nazwani ludzie obejmują ważne stanowiska. Tak się skaluje i nie gubi osób, które gracz zna.
- **B. Każdy mieszkaniec nazwany i śledzony:** bardzo żywe, ale nie do opanowania przy setkach ludzi, ani dla gracza, ani dla wydajności.
- **C. Same liczby, nazwani znikają:** najprościej, ale gracz traci ludzi, na których pracował.

## Plan budowy

Każdy etap da się zagrać i ma własne testy, a bot sprawdza, czy miasto się utrzymuje.

| Etap | Co powstaje | Skąd wiadomo, że działa |
|---|---|---|
| M0 | Skrót startowy, przejście, kamera miejska | Awans na Miasto przełącza tryb, zapis to pamięta |
| M1 | Drogi, budynki na kilka kafli, domy, magazyn z zasięgiem | Dom bez drogi nie działa, a z drogą się zapełnia |
| M2 | Wózki, jeden łańcuch: farma → młyn → piekarnia → targ | Odcięcie drogi zatrzymuje dostawy chleba |
| M3 | Potrzeby, klasy, podatki, utrzymanie | Dom awansuje, a brak chleba go cofa; bot nie bankrutuje |
| M4 | Pełne łańcuchy, kopalnie, huty, reszta budynków | Bot utrzymuje wszystkie klasy |
| M5 | Koszary, mury, zagrożenie według decyzji 3 | Najazd da się odeprzeć bez udziału bohatera |
| M6 | Badania, wielkie budowle, wydajność | Miasto z setkami budynków działa płynnie na laptopie |

Najpierw warto zrobić M0 i M1 i dać Ci to do zagrania. Dopiero wtedy będzie widać, czy to jest ta gra, której chcesz, zanim powstanie reszta.

## Czego ten koncept nie sprawdził

- Liczby i nazwy w tabelach klas i łańcuchów to szkic do strojenia, a nie policzony balans.
- Wydajność setek budynków i wózków na Twoim laptopie jest nieznana. To największe ryzyko techniczne, więc M1 i M2 trzeba mierzyć od razu.
- Modele z obecnych paczek nie wystarczą na każdy budynek, bo brakuje np. piekarni i tkalni. Część będzie na początku dzieliła wygląd, a później może wymagać nowych darmowych modeli CC0.
