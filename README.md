# 🗡️ Pro RPG – Tekstowa gra fantasy po polsku

Prosta, w pełni tekstowa (CLI) gra RPG fantasy napisana w Pythonie 3.  
Wszystkie komunikaty, menu i nazwy akcji są **po polsku**.

---

## ▶️ Uruchomienie

```bash
python main.py
```

> Wymagania: **Python 3.10+** (brak zewnętrznych bibliotek).

---

## 🎮 Funkcje gry

| Funkcja | Opis |
|---|---|
| **Menu główne** | Nowa gra / Wczytaj (placeholder) / Wyjście |
| **Tworzenie postaci** | Własne imię, startowe statystyki |
| **System walki** | Turowa walka z losowymi przeciwnikami |
| **Przeciwnicy** | Goblin, Szkielet, Ork, Trolle, Wiedźma, Młody smok |
| **Akcje w walce** | Atak ⚔, Użyj mikstury 🧪, Ucieczka 🏃 |
| **Levelowanie** | EXP → awans poziomu → wzrost statystyk |
| **Mikstury** | Gracz startuje z 3 miksturami leczenia |
| **Obóz** | Odpoczynek za złoto, wizyta w sklepie |
| **Sklep** | Zakup mikstur za złoto zdobyte z potworów |

---

## 📁 Struktura plików

```
Game_Simply/
├── main.py          # Punkt wejścia – uruchamia grę
├── requirements.txt # Brak zależności zewnętrznych
├── README.md        # Ten plik
└── game/
    ├── __init__.py  # Inicjalizacja pakietu
    ├── player.py    # Klasa Gracz (statystyki, EXP, mikstury)
    ├── enemy.py     # Klasy przeciwników i fabryka losowania
    ├── combat.py    # System walki turowej
    ├── shop.py      # Prosty sklep
    └── utils.py     # Funkcje pomocnicze (wyświetlanie, input)
```

---

## 🧑‍💻 Jak grać?

1. Uruchom grę: `python main.py`
2. Wybierz **Nowa gra** i wpisz imię bohatera.
3. W **Obozie** wybierz akcję:
   - `[1]` Wyrusz na przygodę – starcie z losowym potworem.
   - `[2]` Sklep – kup mikstury za zdobyte złoto.
   - `[3]` Odpoczynek – odnów 30 HP za 10 złota.
   - `[4]` Wróć do menu głównego.
4. Podczas walki:
   - `[1]` Atakuj wroga.
   - `[2]` Użyj mikstury leczenia.
   - `[3]` Spróbuj uciec (50% szans).
5. Po pokonaniu wroga zdobywasz **EXP** i **złoto**.
6. Zgromadź wystarczająco EXP, aby awansować poziom i zwiększyć statystyki!

---

## 📜 Licencja

Projekt otwarty – możesz dowolnie rozbudowywać i modyfikować kod.
