"""Moduł obsługujący sklep."""

from game.player import Gracz
from game.utils import wyswietl_linie, nacisnij_enter


_PRODUKTY = [
    {"nazwa": "Mikstura leczenia", "klucz": "mikstura", "cena": 20, "opis": "Leczy 40 HP"},
    {"nazwa": "Mikstura większa", "klucz": "mikstura_duza", "cena": 45, "opis": "Leczy 80 HP (dodaje 2 mikstury)"},
]


def otworz_sklep(gracz: Gracz) -> None:
    """Wyświetla sklep i obsługuje transakcje."""
    while True:
        wyswietl_linie()
        print(f"  🏪  SKLEP  |  Twoje złoto: {gracz.zloto} szt.\n")
        for i, produkt in enumerate(_PRODUKTY, 1):
            print(f"  [{i}] {produkt['nazwa']} — {produkt['cena']} złota  ({produkt['opis']})")
        print(f"  [0] Wyjdź ze sklepu\n")

        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            print("  Do widzenia! Wracasz na drogę przygód.")
            nacisnij_enter()
            return

        try:
            idx = int(wybor) - 1
            if idx < 0 or idx >= len(_PRODUKTY):
                print("  Nieprawidłowy wybór.")
                continue
        except ValueError:
            print("  Wpisz numer produktu.")
            continue

        produkt = _PRODUKTY[idx]

        if gracz.zloto < produkt["cena"]:
            print(f"  Nie masz wystarczająco złota! Potrzebujesz {produkt['cena']} szt.")
            nacisnij_enter()
            continue

        gracz.zloto -= produkt["cena"]
        if produkt["klucz"] == "mikstura":
            gracz.mikstury += 1
            print(f"  Kupiono: {produkt['nazwa']}. Mikstury: {gracz.mikstury}")
        elif produkt["klucz"] == "mikstura_duza":
            gracz.mikstury += 2
            print(f"  Kupiono: {produkt['nazwa']}. Mikstury: {gracz.mikstury}")
        nacisnij_enter()
