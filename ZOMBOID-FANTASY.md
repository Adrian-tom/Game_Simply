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
| 1 / 2 / 3 | rozłóż śpiwór / zapal ognisko (5 drewna, 3 kamienie) / postaw namiot (8 drewna, 6 włókna) |
| F | zjedz jagody |
| Z | śpij (przy śpiworze albo namiocie; czas płynie 40× szybciej, w namiocie regeneracja jest szybsza) |
| Esc | powrót do menu głównego |

## Co jest zrobione (faza 1)
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

## Co dalej (faza 2 i dalej — jeszcze niezrobione)
1. **Osada**: po założeniu obozu przybywają osadnicy (system z `Osada.cs`: zawody, umiejętności, pasje), budowa
   kafel po kaflu z rusztowaniami, skryptorium i badania jako warstwa zarządzania.
2. Zagrożenia nocą (wilki, bandyci), obrona, palisada.
3. Zapis świata i postaci, narzędzia (siekiera, kilof) skracające zbiory, wędrowni kupcy, drogi między punktami.
4. Wielka mapa z punktami zainteresowania (ruiny, jaskinie, miasta) z istniejącego generatora regionów.

## Techniczne
`SwiatWolny.Aktualizuj(dt, Wejscie)` to czysta logika bez Unity (testy: `unity/Testy/TestyWolnego.cs`), `ScenaWolna` rysuje
stan, `SilnikGry` liczy klatkę na wątku głównym, a wątek logiki tylko czeka (`Gra.Przetrwanie_`).
Podgląd klatek: `cd unity/Testy && dotnet run -- --wolny <katalog>`.
