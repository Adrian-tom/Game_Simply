# Pro RPG – survival city builder RPG fantasy po polsku

Gra fantasy w Pythonie 3, w której prowadzisz **bohatera i jego osadę**. Wyprawy dają surowce,
łup i doświadczenie; osada daje jedzenie, rzemiosło i ludzi — ale je, marznie zimą,
choruje i przyciąga bandy. Wszystkie komunikaty są **po polsku**.

Gatunki w jednej pętli: **survival** (żywność, opał, pory roku, rany), **city builder**
(osadnicy z morale i cechami, 16 budynków z poziomami, łańcuchy produkcji),
**RPG** (5 klas, podklasy, talenty, walka turowa z zapowiedziami ruchów),
**obrona osady** (najazdy, fale, pojedynek z hersztem), **rzemiosło i alchemia**
(odkrywanie przepisów), **handel i karawany** oraz **narracja w stylu Disco Elysium**
(głosy umiejętności, białe i czerwone testy, gabinet myśli).

Gra działa w **oknie z grafiką pixelart** (pygame-ce): po lewej izometryczna mapa regionu
z oświetleniem, mgłą wojny i rosnącą osadą, **arena walki** (sprite'y 18 rodzajów wrogów,
zapowiedzi, garda, ogień, liczby obrażeń) albo **portret rozmówcy** na malarskim tle, pod spodem
HUD; po prawej konsola z klikalnymi opcjami.
Cała grafika jest liczona w kodzie. Bez pygame gra startuje w trybie tekstowym w terminalu.

Architektura, pętle gry i liczby z symulacji balansu: **[ANALIZA.md](ANALIZA.md)**.

---

## Uruchomienie

Na Windowsie kliknij dwukrotnie **`uruchom.bat`**. Albo:

```bash
pip install -r requirements.txt   # raz — grafika (pygame-ce)
python main.py                    # okno z grafiką
python main.py --tekst            # tryb tekstowy w terminalu
```

Wymagania: **Python 3.10+** i **pygame-ce** (tylko do okna). `uruchom.bat` szuka Pythona
w `%USERPROFILE%\.local\bin\`, a przy pierwszym starcie sam doinstalowuje pygame-ce.

### Sterowanie w oknie

| Klawisz / mysz | Działanie |
|---|---|
| klik na opcję `[n]` w konsoli | wybór opcji (to samo co numer i Enter) |
| cyfry + `Enter` | wybór opcji, wpisywanie imienia itd. |
| strzałki (na wyprawie) | ruch: ⬆ północ, ⬇ południe, ⬅ zachód, ➡ wschód |
| `Spacja` (na wyprawie) | zbadaj pole / wejdź (`[5]`) |
| `Spacja` / klik (przy „Naciśnij Enter”) | dalej |
| kółko myszy | przewijanie konsoli |
| `F2` | dzień / zmierzch |
| `F11` | pełny ekran |

---

## Systemy gry

| System | Co robi |
|---|---|
| **Czas i pory roku** | Każdy dzień (ruch na mapie, odpoczynek, praca) to dzień osady. Rok = wiosna, lato, jesień, zima po 30 dni. Jesienią żniwa, zimą pola nie rodzą, trzeba palić drewno, a bandy są głodniejsze |
| **Przetrwanie** | Na wyprawę bierzesz prowiant (8 racji, +4 za poziom stajni). Bez jedzenia głód: −HP i słabszy atak. Zimą bez ciepłego odzienia mróz rani co dzień |
| **Rany** | Ciężkie ciosy zostawiają rany (złamana ręka −20% ataku, głęboka rana −50% mikstur, zwichnięta noga, wstrząs). Goją się z czasem — szybciej przy odpoczynku, w lecznicy, po maści |
| **Osadnicy** | Morale, cecha (pracowity, leniwy, żarłoczny, odważny, chorowity…), doświadczenie w zawodzie (★), choroby. Głodni i zmarznięci pracują gorzej, chorują i odchodzą |
| **Więzi** | Osadnicy zaprzyjaźniają się i kłócą. Wspólna praca i zgodne charaktery zbliżają, sprzeczne cechy dzielą, a głód i zimno kłócą wszystkich. Przyjaciel obok podnosi morale, wróg ciągnie w dół. Para z więzią ≥ 80 jest wyłączna — nikt nie ma dwóch partnerów — i może doczekać się dziecka, jedynego darmowego osadnika w grze (potrzebna wolna chata, żywność, dobre morale i przerwa po poprzednim dziecku). Śmierć albo odejście bliskiego łamie tych, którzy go kochali |
| **Zawody** | Drwal, kamieniarz, zielarz, myśliwy, rolnik, górnik, tracz, hutnik, handlarz, rzemieślnik, strażnik, uzdrowiciel |
| **Łańcuchy produkcji** | Drewno → tartak → deski; ruda + drewno → huta → żelazo; żelazo → kuźnia → narzędzia (+25% pracy) i ulepszenia broni/zbroi |
| **Budynki** | 16 budynków, każdy z poziomami i utrzymaniem: farma, spichlerz, tartak, huta, kuźnia, warsztat, laboratorium, lecznica, targ, karawanseraj, tawerna, palisada, wieża, stajnie, sklep, dom (+ chaty) |
| **Obrona osady** | Zagrożenie rośnie z czasem, zimą i z bogactwem. Zwiadowcy (lub wieża — dokładnie) zapowiadają najazd. W obozie: rozmowa z hersztem, 3 fale z wyborem taktyki, pojedynek. Pod nieobecność osada broni się sama |
| **Rzemiosło i alchemia** | 14 przepisów w warsztacie, laboratorium i kuźni. Część trzeba **odkryć**: eksperyment z dwóch składników (test podpowiada trop), rozmowy, zwoje. Rzadkie składniki z biomów i potworów. Rzemieślnicy realizują **zamówienia** (docelowy zapas) |
| **Handel i karawany** | Osady handlowe z własnymi cenami (Brzezie, Kamienny Bród, Port Veldmar + odkryte miasta). Ceny dryfują, żywność drożeje zimą. Karawana jedzie kilka dni, może wpaść w zasadzkę (eskorta pomaga), wraca ze złotem albo zamówionym towarem |
| **Walka** | Trudność zależy od regionu (region N = wrogowie poz. 2N−1..2N). Wrogowie **zapowiadają** ciężki cios, zionięcie ogniem, klątwę — odpowiadasz **gardą** `[5]`, eliksirem albo przerwaniem. Słabości i odporności (ogień, święte, trucizna), podpalenie blokuje regenerację trolli. Bossowie wpadają w szał przy 50% HP. Ucieczka zależy od Zręczności |
| **Przedmioty bojowe** | Bandaż, eliksir siły, eliksir ognioodporności, olej ognisty, bomba, napar jasności (+2 w rozmowie) |
| **Śmierć** | Normalny: omdlenie — tracisz prowiant, 30% złota, część łupów, budzisz się z ciężką raną po 3 dniach. **Hardcore: dziedzic** z drużyny lub osady przejmuje osadę (połowa poziomu, dorobek osady, +punkty talentów) — gra kończy się, gdy nie zostawisz nikogo |
| **Drzewko talentów** | 6 gałęzi × 5 węzłów: Wojaczka, Przetrwanie, Przywództwo, Rzemiosło, Handel, Umysł. 1 punkt za awans |
| **Rozmowy (Disco Elysium)** | Głosy umiejętności wtrącają się same (bierny test) i odsłaniają opcje. **Białe** testy ⚪ wracają, gdy się rozwiniesz; **czerwone** 🔴 — jedna szansa. Przy każdym teście trudność i szansa w %. Grimbold, herszt najazdu, sprawy osady |
| **Gabinet myśli** | Myśli z rozmów i decyzji. Przyswajanie trwa kilka dni (kara), potem trwały efekt |
| **Sprawy osady** | Kłótnie o racje, chorzy, złodzieje, przybysze (może szpieg?), wędrowny kupiec, święto plonów, pijany strażnik — decyzje z testami i skutkami dla morale i karmy |
| **Doradca** | W obozie 1–2 najpilniejsze podpowiedzi: głód za kilka dni, zima bez opału, najazd jutro, ludzie bez pracy |
| **Postać** | 5 klas i podklasy od poz. 5, atrybuty w stylu BG3 (testy k20), pochodzenie i 3 cechy z puli 50, rangi umiejętności 1–5 |
| **Świat** | Trwała mapa regionów 9×9 z biomami, mgłą wojny, karczmami, kuźniami, świątyniami, jaskiniami, miastami, bossami i miejscami mitycznymi |
| **Questy** | 13 zadań — od zabijania goblinów po przetrwanie zimy i karawany. Postęp liczy się od przyjęcia |
| **Zapis** | Autosave, zapis atomowy, stare zapisy migrują się same |

---

## Jak grać — pierwsze dni

1. **Nowa gra** → imię, klasa, pochodzenie, cechy, trudność. Startujesz z namiotem, dwiema chatami,
   dwojgiem osadników (drwal i myśliwy) i 30 racjami.
2. **Najpierw jedzenie.** Postaw **pola uprawne** `[11]` i przestaw kogoś na rolnika `[16]`.
   Myśliwy i polowanie w pracy `[15]` pomagają na start.
3. **Wyprawy** `[1]` dają drewno, kamień, zioła, rudę, łup i doświadczenie. Pilnuj prowiantu i HP.
4. **Przed pierwszym najazdem** (zwykle ok. 2. miesiąca): palisada, strażnik, zapas drewna na smołę.
5. **Jesienią** gromadź żywność i drewno na zimę. Uszyj ciepłe odzienie w warsztacie.
6. **Rozwijaj się**: talenty `[20]`, myśli `[21]`, rzemiosło `[17]`, karawany `[19]`, sprawy osady `[22]`.

Menu obozu: `[1]` wyprawa · `[2]` sklep · `[3]` odpoczynek · `[4]` ekwipunek · `[5]` questy ·
`[6]` osiągnięcia · `[7]` karta postaci · `[8]` podklasa · `[9]` mapa · `[10]` księga umiejętności ·
`[11]` rozbudowa · `[12]` kuźnia · `[13]` stajnie · `[14]` drużyna · `[15]` praca · `[16]` osada ·
`[17]` rzemiosło · `[18]` obrona · `[19]` handel · `[20]` talenty · `[21]` gabinet myśli · `[22]` sprawy osady.

---

## Struktura plików

```
Game_Simply/
├── main.py            # Punkt wejścia — menu, obóz, śmierć i dziedzictwo; okno albo --tekst
├── uruchom.bat        # Skrót Windows — doinstalowuje pygame-ce i odpala grę
├── game/
│   ├── swiat.py       # Dzienny cykl świata — jedno miejsce, w którym mija czas
│   ├── kalendarz.py   # Pory roku
│   ├── przetrwanie.py # Prowiant, głód, zimno, rany
│   ├── osada.py       # Osadnicy, zawody, produkcja, morale, choroby, praca, targ
│   ├── wiezi.py       # Relacje między osadnikami: przyjaźnie, waśnie, pary, narodziny
│   ├── oboz.py        # Surowce, budynki z poziomami, zbieractwo na mapie
│   ├── obrona.py      # Zagrożenie, najazdy, fale, herszt
│   ├── rzemioslo.py   # Przepisy, odkrycia, zamówienia, ulepszenia, składniki
│   ├── handel.py      # Osady handlowe, ceny, karawany
│   ├── dziedzictwo.py # Omdlenie i dziedzic osady
│   ├── talenty.py     # Drzewko talentów
│   ├── rozmowy.py     # Silnik rozmów (głosy, białe/czerwone testy) + treść
│   ├── mysli.py       # Gabinet myśli
│   ├── doradca.py     # Podpowiedzi w obozie
│   ├── combat.py      # Walka turowa, garda, zapowiedzi, żywioły, przedmioty
│   ├── enemy.py       # Wrogowie, zdolności, poziomy regionów, bossowie, herszt
│   ├── player.py      # Postać
│   ├── atrybuty.py    # Atrybuty i testy k20
│   ├── world.py, mapa.py, miasto.py, mityczne.py   # Świat i eksploracja
│   ├── skills.py, items.py, shop.py, quests.py, rekruci.py, dialogues.py, karma.py, pochodzenie.py
│   ├── savegame.py    # Zapis JSON (z migracją starych zapisów)
│   └── ekran.py, ikony.py, utils.py
├── grafika/           # Okno pixelart (pygame-ce) — logika gry nic stąd nie importuje
│   ├── okno.py        # Okno, konsola, HUD, przejęcie print/input
│   ├── scena.py       # Izometryczna mapa regionu, rosnąca osada
│   ├── arena.py       # Ekran walki
│   ├── potwory.py     # Sprite'y wrogów z brył
│   ├── portret.py     # Widok rozmowy
│   ├── teren.py       # Kafle, roślinność, budynki, sprite gracza
│   └── piksele.py     # Prymitywy pixelartu
└── tests/             # 99 testów unittest
```

---

## Testy

```bash
python -m unittest discover -s tests -v
```

Testy logiki nie wymagają niczego poza Pythonem; testy okna (`tests/test_grafika.py`) potrzebują
pygame-ce i bez niego są pomijane. Okno renderuje się bez monitora (`SDL_VIDEODRIVER=dummy`).
Testy obejmują m.in. trwały świat, zapis, questy, karmę, umiejętności, kalendarz, głód i rany,
produkcję osady i łańcuchy, obronę i najazdy, karawany, talenty, myśli, dziedzictwo,
rozmowy (białe/czerwone testy, spójność grafów), rzemiosło i odkrycia, gardę, ogień i ucieczkę.
CI uruchamia je na Pythonie 3.10 i 3.13 — najpierw bez pygame, potem z grafiką.

---

## Licencja

Projekt otwarty – możesz dowolnie rozbudowywać i modyfikować kod.
