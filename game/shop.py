"""Moduł obsługujący sklep i kuźnię."""

from game.player import Gracz
from game.items import EKWIPUNEK, SKLEP_ASORTYMENT, KUZNIA_ASORTYMENT, zaloz, wyswietl_przedmiot
from game.utils import wyswietl_linie, nacisnij_enter


_MIKSTURY = [
    {"nazwa": "Mikstura leczenia", "klucz": "mikstura", "cena": 20, "opis": "Leczy 40 HP"},
    {"nazwa": "Mikstura większa", "klucz": "mikstura_duza", "cena": 45, "opis": "Leczy 80 HP (dodaje 2 mikstury)"},
]


def _kup_ekwipunek(gracz: Gracz, klucz: str) -> None:
    """Obsługuje zakup i zakładanie przedmiotu ekwipunku."""
    item = EKWIPUNEK[klucz]
    if gracz.zloto < item["cena"]:
        print(f"  Nie masz wystarczająco złota! Potrzebujesz {item['cena']} szt.")
        nacisnij_enter()
        return

    gracz.zloto -= item["cena"]
    gracz.statystyki["zakupy"] = gracz.statystyki.get("zakupy", 0) + 1
    komunikat = zaloz(gracz, klucz)
    print(f"  Kupiono i założono: {item['nazwa']}!")
    print(komunikat)
    nacisnij_enter()


def otworz_sklep(gracz: Gracz) -> None:
    """Wyświetla sklep ogólny (mikstury + lekki ekwipunek) i obsługuje transakcje."""
    while True:
        wyswietl_linie()
        print(f"  🏪  SKLEP  |  Twoje złoto: {gracz.zloto} szt.\n")

        print("  ─── Mikstury ───")
        for i, produkt in enumerate(_MIKSTURY, 1):
            print(f"  [{i}] {produkt['nazwa']} — {produkt['cena']} złota  ({produkt['opis']})")

        print(f"\n  ─── Ekwipunek ───")
        offset = len(_MIKSTURY)
        for j, klucz in enumerate(SKLEP_ASORTYMENT, 1):
            wyswietl_przedmiot(klucz, offset + j, gracz)

        print(f"\n  [0] Wyjdź ze sklepu\n")

        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            print("  Do widzenia! Wracasz na drogę przygód.")
            nacisnij_enter()
            return

        try:
            idx = int(wybor) - 1
        except ValueError:
            print("  Wpisz numer produktu.")
            continue

        # Mikstury
        if 0 <= idx < len(_MIKSTURY):
            produkt = _MIKSTURY[idx]
            if gracz.zloto < produkt["cena"]:
                print(f"  Nie masz wystarczająco złota! Potrzebujesz {produkt['cena']} szt.")
                nacisnij_enter()
                continue
            gracz.zloto -= produkt["cena"]
            gracz.statystyki["zakupy"] = gracz.statystyki.get("zakupy", 0) + 1
            if produkt["klucz"] == "mikstura":
                gracz.mikstury += 1
                print(f"  Kupiono: {produkt['nazwa']}. Mikstury: {gracz.mikstury}")
            elif produkt["klucz"] == "mikstura_duza":
                gracz.mikstury += 2
                print(f"  Kupiono: {produkt['nazwa']}. Mikstury: {gracz.mikstury}")
            nacisnij_enter()
            continue

        # Ekwipunek
        eq_idx = idx - len(_MIKSTURY)
        if 0 <= eq_idx < len(SKLEP_ASORTYMENT):
            _kup_ekwipunek(gracz, SKLEP_ASORTYMENT[eq_idx])
            continue

        print("  Nieprawidłowy wybór.")


def otworz_kuznia(gracz: Gracz) -> None:
    """Wyświetla kuźnię (ciężki ekwipunek bojowy) i obsługuje transakcje."""
    while True:
        wyswietl_linie()
        print(f"  ⚒  KUŹNIA GRIMBOLD'A  |  Twoje złoto: {gracz.zloto} szt.\n")
        print("  ─── Broń i zbroja ───")
        for i, klucz in enumerate(KUZNIA_ASORTYMENT, 1):
            wyswietl_przedmiot(klucz, i, gracz)
        print(f"\n  [0] Wyjdź z kuźni\n")

        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            print("  Kowal kiwa głową. Do następnego razu!")
            nacisnij_enter()
            return

        try:
            idx = int(wybor) - 1
        except ValueError:
            print("  Wpisz numer przedmiotu.")
            continue

        if 0 <= idx < len(KUZNIA_ASORTYMENT):
            _kup_ekwipunek(gracz, KUZNIA_ASORTYMENT[idx])
            continue

        print("  Nieprawidłowy wybór.")

