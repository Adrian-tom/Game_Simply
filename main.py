"""
Główny plik gry RPG – Pro RPG (tekstowa gra fantasy po polsku).

Uruchomienie:
    python main.py
"""

from game.player import Gracz
from game.shop import otworz_sklep
from game.skills import PODKLASY
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter, baner_tytulowy
from game.world import wyrusz_w_podroz


# ------------------------------------------------------------------ #
#  Menu główne                                                         #
# ------------------------------------------------------------------ #

def menu_glowne() -> str:
    """Wyświetla menu główne i zwraca wybór gracza."""
    baner_tytulowy()
    print("  [1]  Nowa gra")
    print("  [2]  Wczytaj grę  (niedostępne — brak zapisu)")
    print("  [3]  Wyjście")
    print()
    return input("  Twój wybór: ").strip()


# ------------------------------------------------------------------ #
#  Tworzenie postaci                                                   #
# ------------------------------------------------------------------ #

def _wybierz_klase() -> str:
    """Prowadzi gracza przez wybór klasy postaci. Zwraca nazwę klasy."""
    wyswietl_linie("─")
    print("  Wybierz klasę postaci:\n")
    print("  [1]  ⚔  Wojownik    — Wysoki HP i obrona, walka wręcz")
    print("  [2]  🔮 Mag         — Niski HP, za to potężne zaklęcia (mana)")
    print("  [3]  🗡  Łotrzyk     — Zwinny oszust z kontrolą i trafieniami krytycznymi")
    print("  [4]  🌿 Druid       — Uzdrowiciel natury, regeneracja i żywiołowe zaklęcia (mana)")
    print("  [5]  💀 Nekromanta  — Mistrz mroku, wysysa życie i osłabia wrogów (mana)")
    print()
    while True:
        wybor = input("  Twój wybór: ").strip()
        if wybor == "1":
            return "Wojownik"
        if wybor == "2":
            return "Mag"
        if wybor == "3":
            return "Lotrzyk"
        if wybor == "4":
            return "Druid"
        if wybor == "5":
            return "Nekromanta"
        print("  Nieprawidłowy wybór. Wpisz 1, 2, 3, 4 lub 5.")


def stworz_postac() -> Gracz:
    """Prowadzi gracza przez tworzenie nowej postaci."""
    wyczysc()
    wyswietl_linie("═")
    print("  TWORZENIE POSTACI")
    wyswietl_linie("═")
    while True:
        imie = input("\n  Podaj imię swojego bohatera: ").strip()
        if imie:
            break
        print("  Imię nie może być puste. Spróbuj ponownie.")

    print()
    klasa = _wybierz_klase()
    gracz = Gracz(imie, klasa)
    print(f"\n  Witaj, {gracz.imie} [{klasa}]! Twoja przygoda się rozpoczyna...")
    print(gracz)
    nacisnij_enter()
    return gracz


# ------------------------------------------------------------------ #
#  Wybór podklasy                                                      #
# ------------------------------------------------------------------ #

def _wybierz_podklase_dialog(gracz: Gracz) -> None:
    """Wyświetla menu wyboru podklasy i zapisuje wybór w obiekcie gracza."""
    wyczysc()
    wyswietl_linie("═")
    print(f"  WYBÓR PODKLASY  —  {gracz.klasa}")
    wyswietl_linie("═")
    print(f"\n  Osiągnąłeś poziom 5! Czas wybrać swoją specjalizację.\n")

    podklasy = PODKLASY[gracz.klasa]
    for i, pk in enumerate(podklasy, 1):
        print(f"  [{i}] {pk['nazwa']}")
        print(f"       {pk['opis']}")
        print()

    while True:
        wybor = input("  Twój wybór: ").strip()
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(podklasy):
                wybrana = podklasy[idx]
                komunikaty = gracz.wybierz_podklase(wybrana["klucz"])
                print()
                for msg in komunikaty:
                    print(msg)
                nacisnij_enter()
                return
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")


# ------------------------------------------------------------------ #
#  Menu akcji w obozie                                                 #
# ------------------------------------------------------------------ #

def menu_obozu(gracz: Gracz) -> str:
    """Wyświetla menu obozu i zwraca wybór gracza."""
    wyczysc()
    wyswietl_linie("═")
    print(f"  OBÓZ  —  {gracz.imie}  (Poz. {gracz.poziom})")
    wyswietl_linie("═")
    print(gracz)
    print()
    print("  [1]  Wyrusz na przygodę")
    print("  [2]  Sklep")
    print("  [3]  Odpoczynek (+30 HP, koszt: 10 złota)")
    if gracz.podklasa_dostepna:
        print("  [4]  ⭐ Wybierz podklasę! (Dostępna)")
        print("  [5]  Wróć do menu głównego")
    else:
        print("  [4]  Wróć do menu głównego")
    print()
    return input("  Twój wybór: ").strip()


def odpoczynek(gracz: Gracz) -> None:
    """Gracz odpoczywa, odnawiając HP (i manę) za złoto."""
    koszt = 10
    if gracz.zloto < koszt:
        print(f"\n  Nie masz wystarczająco złota (potrzebujesz {koszt} szt.).")
    else:
        poprzednie = gracz.hp
        gracz.zloto -= koszt
        gracz.hp = min(gracz.hp + 30, gracz.max_hp)
        faktyczne = gracz.hp - poprzednie
        print(f"\n  😴  Odpocząłeś i odzyskałeś {faktyczne} HP. (-{koszt} złota)")
        if gracz.max_mana > 0:
            gracz.mana = gracz.max_mana
            print(f"  🔮  Mana uzupełniona do {gracz.max_mana}!")
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Pętla nowej gry                                                     #
# ------------------------------------------------------------------ #

def nowa_gra() -> None:
    """Główna pętla rozgrywki."""
    gracz = stworz_postac()

    while True:
        wybor = menu_obozu(gracz)

        if wybor == "1":
            wynik = wyrusz_w_podroz(gracz)
            if wynik == "przegrana":
                wyczysc()
                wyswietl_linie("═")
                print("  KONIEC GRY")
                wyswietl_linie("═")
                print(f"\n  {gracz.imie} poległ w walce...")
                print(f"  Osiągnięty poziom: {gracz.poziom}")
                print(f"  Zdobyte EXP: {gracz.exp}")
                wyswietl_linie()
                nacisnij_enter()
                return

        elif wybor == "2":
            otworz_sklep(gracz)

        elif wybor == "3":
            odpoczynek(gracz)

        elif wybor == "4":
            if gracz.podklasa_dostepna:
                _wybierz_podklase_dialog(gracz)
            else:
                print("\n  Wracasz do menu głównego...")
                nacisnij_enter()
                return

        elif wybor == "5" and gracz.podklasa_dostepna:
            print("\n  Wracasz do menu głównego...")
            nacisnij_enter()
            return

        else:
            print("  Nieprawidłowy wybór.")
            nacisnij_enter()


# ------------------------------------------------------------------ #
#  Punkt wejścia                                                       #
# ------------------------------------------------------------------ #

def main() -> None:
    """Główna pętla programu z menu startowym."""
    while True:
        wybor = menu_glowne()

        if wybor == "1":
            nowa_gra()

        elif wybor == "2":
            wyczysc()
            print("\n  Funkcja zapisu gry nie jest jeszcze dostępna.")
            print("  Wybierz 'Nowa gra', aby rozpocząć przygodę!")
            nacisnij_enter()

        elif wybor == "3":
            wyczysc()
            print("\n  Dziękujemy za grę! Do zobaczenia w następnej przygodzie!\n")
            break

        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2 lub 3.")
            nacisnij_enter()


if __name__ == "__main__":
    main()
