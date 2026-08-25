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
from game.savegame import zapisz_gre, wczytaj_gre, zapis_istnieje, usun_zapis


# ------------------------------------------------------------------ #
#  Menu główne                                                         #
# ------------------------------------------------------------------ #

def menu_glowne() -> str:
    """Wyświetla menu główne i zwraca wybór gracza."""
    baner_tytulowy()
    print("  [1]  Nowa gra")
    if zapis_istnieje():
        print("  [2]  Wczytaj grę  ✔")
    else:
        print("  [2]  Wczytaj grę  (brak zapisu)")
    print("  [3]  Wyjście")
    print()
    return input("  Twój wybór: ").strip()


# ------------------------------------------------------------------ #
#  Tryb trudności                                                      #
# ------------------------------------------------------------------ #

_TRYBY_TRUDNOSCI = {
    "1": {"nazwa": "Łatwy",    "opis": "Więcej złota i mikstur, wrogowie słabsi.",      "klucz": "latwy"},
    "2": {"nazwa": "Normalny", "opis": "Standardowa rozgrywka.",                         "klucz": "normalny"},
    "3": {"nazwa": "Hardcore", "opis": "Permadeath — śmierć usuwa zapis gry. Trudniejsi wrogowie.", "klucz": "hardcore"},
}


def _wybierz_trudnosc() -> str:
    """Prowadzi gracza przez wybór trybu trudności. Zwraca klucz trybu."""
    wyswietl_linie("─")
    print("  Wybierz tryb trudności:\n")
    for k, v in _TRYBY_TRUDNOSCI.items():
        print(f"  [{k}]  {v['nazwa']} — {v['opis']}")
    print()
    while True:
        wybor = input("  Twój wybór: ").strip()
        if wybor in _TRYBY_TRUDNOSCI:
            return _TRYBY_TRUDNOSCI[wybor]["klucz"]
        print("  Nieprawidłowy wybór. Wpisz 1, 2 lub 3.")


def _zastosuj_trudnosc(gracz: Gracz, tryb: str) -> None:
    """Modyfikuje statystyki gracza wg wybranego trybu trudności."""
    gracz.tryb_trudnosci = tryb
    if tryb == "latwy":
        gracz.zloto += 20
        gracz.mikstury += 2
        gracz.hp = min(gracz.max_hp, gracz.hp + 20)
    elif tryb == "hardcore":
        gracz.max_hp = max(1, int(gracz.max_hp * 0.85))
        gracz.hp = gracz.max_hp


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
    print()
    tryb = _wybierz_trudnosc()
    gracz = Gracz(imie, klasa)
    _zastosuj_trudnosc(gracz, tryb)
    print(f"\n  Witaj, {gracz.imie} [{klasa}]! Tryb: {tryb.capitalize()}. Twoja przygoda się rozpoczyna...")
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
#  Rozdzielanie atrybutów                                              #
# ------------------------------------------------------------------ #

def _rozdziel_atrybuty(gracz: Gracz) -> None:
    """Pozwala graczowi rozdzielić punkty atrybutów po levelupie."""
    while getattr(gracz, "punkty_atrybutow", 0) > 0:
        wyczysc()
        wyswietl_linie("═")
        print(f"  PUNKTY ATRYBUTÓW  —  Dostępne: {gracz.punkty_atrybutow}")
        wyswietl_linie("═")
        print(f"\n  Aktualnie: HP {gracz.max_hp}  Atak {gracz.atak}  Obrona {gracz.obrona}\n")
        print("  [1]  +15 Max HP")
        print("  [2]  +3 Atak")
        print("  [3]  +2 Obrona")
        if gracz.max_mana > 0:
            print("  [4]  +10 Max Mana")
        print()
        wybor = input("  Twój wybór: ").strip()

        if wybor == "1":
            gracz.max_hp += 15
            gracz.hp = min(gracz.hp + 15, gracz.max_hp)
            gracz.punkty_atrybutow -= 1
            print("  ❤  Max HP +15!")
        elif wybor == "2":
            gracz.atak += 3
            gracz.punkty_atrybutow -= 1
            print("  ⚔  Atak +3!")
        elif wybor == "3":
            gracz.obrona += 2
            gracz.punkty_atrybutow -= 1
            print("  🛡  Obrona +2!")
        elif wybor == "4" and gracz.max_mana > 0:
            gracz.max_mana += 10
            gracz.mana = min(gracz.mana + 10, gracz.max_mana)
            gracz.punkty_atrybutow -= 1
            print("  🔮  Max Mana +10!")
        else:
            print("  Nieprawidłowy wybór.")
            continue
        nacisnij_enter()


# ------------------------------------------------------------------ #
#  Osiągnięcia                                                         #
# ------------------------------------------------------------------ #

def _sprawdz_i_wyswietl_osiagniecia(gracz: Gracz) -> None:
    """Sprawdza osiągnięcia i wyświetla nowe."""
    nowe = gracz.sprawdz_osiagniecia()
    for msg in nowe:
        print(msg)
    if nowe:
        nacisnij_enter()


def _menu_osiagniec(gracz: Gracz) -> None:
    """Wyświetla listę osiągnięć gracza."""
    wyczysc()
    wyswietl_linie("═")
    print("  OSIĄGNIĘCIA")
    wyswietl_linie("═")
    if not gracz.osiagniecia:
        print("\n  Brak odblokowanych osiągnięć. Eksploruj świat!")
    else:
        print(f"\n  Odblokowane: {len(gracz.osiagniecia)}/{len(gracz._OSIAGNIECIA)}\n")
        for klucz, nazwa, _ in gracz._OSIAGNIECIA:
            status = "✔" if klucz in gracz.osiagniecia else "·"
            print(f"  [{status}]  {nazwa}")
    print()
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Statystyki końca gry                                                #
# ------------------------------------------------------------------ #

def _pokaz_statystyki_konca(gracz: Gracz) -> None:
    """Wyświetla podsumowanie rozgrywki po śmierci lub ukończeniu."""
    wyczysc()
    wyswietl_linie("═")
    print("  PODSUMOWANIE PRZYGODY")
    wyswietl_linie("═")
    print(f"\n  Bohater: {gracz.imie} [{gracz.klasa}]")
    print(f"  Osiągnięty poziom: {gracz.poziom}")
    print(f"  Zdobyte EXP: {gracz.exp}")
    print(f"  Odwiedzone mapy: {gracz.mapa_gen}")
    print(f"  Złoto przy śmierci: {gracz.zloto} szt.")
    print(f"  Zabite potwory: {gracz.statystyki.get('zabite_potwory', 0)}")
    print(f"  Wygrane walki: {gracz.statystyki.get('wygrane_walki', 0)}")
    print(f"  Odwiedzone świątynie: {gracz.statystyki.get('odwiedzone_swiatynie', 0)}")
    if gracz.osiagniecia:
        print(f"\n  Odblokowane osiągnięcia ({len(gracz.osiagniecia)}):")
        for klucz, nazwa, _ in gracz._OSIAGNIECIA:
            if klucz in gracz.osiagniecia:
                print(f"  {nazwa}")
    wyswietl_linie()
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Menu akcji w obozie                                                 #
# ------------------------------------------------------------------ #

def menu_obozu(gracz: Gracz) -> str:
    """Wyświetla menu obozu i zwraca wybór gracza."""
    wyczysc()
    wyswietl_linie("═")
    tryb_str = f"  [{getattr(gracz, 'tryb_trudnosci', 'normalny').upper()}]"
    print(f"  OBÓZ  —  {gracz.imie}  (Poz. {gracz.poziom}){tryb_str}")
    wyswietl_linie("═")
    print(gracz)
    if getattr(gracz, "punkty_atrybutow", 0) > 0:
        print(f"\n  ⭐  Masz {gracz.punkty_atrybutow} punkt(y) atrybutów do rozdziału!")
    print()
    print("  [1]  Wyrusz na przygodę")
    print("  [2]  Sklep")
    print("  [3]  Odpoczynek (+30 HP, koszt: 10 złota)")
    print("  [4]  Osiągnięcia")
    if getattr(gracz, "punkty_atrybutow", 0) > 0:
        print("  [5]  ⭐ Rozdziel atrybuty")
    if gracz.podklasa_dostepna:
        print("  [6]  ⭐ Wybierz podklasę! (Dostępna)")
    print("  [0]  Wróć do menu głównego")
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

def nowa_gra(gracz: Gracz | None = None) -> None:
    """Główna pętla rozgrywki."""
    if gracz is None:
        gracz = stworz_postac()

    while True:
        # Sprawdź osiągnięcia po każdym powrocie do obozu
        _sprawdz_i_wyswietl_osiagniecia(gracz)

        wybor = menu_obozu(gracz)

        if wybor == "1":
            wynik = wyrusz_w_podroz(gracz)
            # Autosave po każdej wyprawie
            zapisz_gre(gracz)
            if wynik == "przegrana":
                wyczysc()
                wyswietl_linie("═")
                print("  KONIEC GRY")
                wyswietl_linie("═")
                print(f"\n  {gracz.imie} poległ w walce...")
                wyswietl_linie()
                _pokaz_statystyki_konca(gracz)
                # Hardcore: usuń zapis po śmierci
                if getattr(gracz, "tryb_trudnosci", "normalny") == "hardcore":
                    usun_zapis()
                    print("  ☠  HARDCORE — zapis usunięty.\n")
                    nacisnij_enter()
                return

        elif wybor == "2":
            otworz_sklep(gracz)

        elif wybor == "3":
            odpoczynek(gracz)

        elif wybor == "4":
            _menu_osiagniec(gracz)

        elif wybor == "5" and getattr(gracz, "punkty_atrybutow", 0) > 0:
            _rozdziel_atrybuty(gracz)

        elif wybor == "6" and gracz.podklasa_dostepna:
            _wybierz_podklase_dialog(gracz)

        elif wybor == "0":
            zapisz_gre(gracz)
            print("\n  Gra zapisana. Wracasz do menu głównego...")
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
            gracz = wczytaj_gre()
            if gracz is None:
                wyczysc()
                print("\n  Brak zapisu gry lub zapis jest uszkodzony.")
                print("  Wybierz 'Nowa gra', aby rozpocząć przygodę!")
                nacisnij_enter()
            else:
                wyczysc()
                wyswietl_linie("═")
                print("  WCZYTANO GRĘ")
                wyswietl_linie("═")
                print(gracz)
                nacisnij_enter()
                nowa_gra(gracz)

        elif wybor == "3":
            wyczysc()
            print("\n  Dziękujemy za grę! Do zobaczenia w następnej przygodzie!\n")
            break

        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2 lub 3.")
            nacisnij_enter()


if __name__ == "__main__":
    main()

