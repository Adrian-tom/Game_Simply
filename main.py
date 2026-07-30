"""
Główny plik gry RPG – Pro RPG (tekstowa gra fantasy po polsku).

Uruchomienie:
    python main.py
"""

from game.player import Gracz
from game.combat import przeprowadz_walke
from game.shop import otworz_sklep
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter, baner_tytulowy


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

    gracz = Gracz(imie)
    print(f"\n  Witaj, {gracz.imie}! Twoja przygoda się rozpoczyna...")
    print(gracz)
    nacisnij_enter()
    return gracz


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
    print("  [1]  Wyrusz na przygodę (walka)")
    print("  [2]  Sklep")
    print("  [3]  Odpoczynek (+30 HP, koszt: 10 złota)")
    print("  [4]  Wróć do menu głównego")
    print()
    return input("  Twój wybór: ").strip()


def odpoczynek(gracz: Gracz) -> None:
    """Gracz odpoczywa, odnawiając HP za złoto."""
    koszt = 10
    if gracz.zloto < koszt:
        print(f"\n  Nie masz wystarczająco złota (potrzebujesz {koszt} szt.).")
    else:
        poprzednie = gracz.hp
        gracz.zloto -= koszt
        gracz.hp = min(gracz.hp + 30, gracz.max_hp)
        faktyczne = gracz.hp - poprzednie
        print(f"\n  😴  Odpocząłeś i odzyskałeś {faktyczne} HP. (-{koszt} złota)")
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
            wynik = przeprowadz_walke(gracz)
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
            print("\n  Wracasz do menu głównego...")
            nacisnij_enter()
            return

        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2, 3 lub 4.")
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
