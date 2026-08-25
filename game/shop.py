"""Moduł obsługujący sklep i kuźnię."""

from game.player import Gracz
from game.items import (
    EKWIPUNEK,
    SKLEP_ASORTYMENT,
    KUZNIA_ASORTYMENT,
    zaloz,
    wyswietl_przedmiot,
    dodaj_do_plecaka,
    menu_sprzedazy,
)
from game.quests import sprawdz_questy
from game.pochodzenie import cena_dla
from game.utils import wyswietl_linie, nacisnij_enter


_MIKSTURY = [
    {"nazwa": "Mikstura leczenia", "klucz": "mikstura", "cena": 20, "ikona": "🧪", "opis": "Leczy 40 HP"},
    {"nazwa": "Mikstura większa", "klucz": "mikstura_duza", "cena": 45, "ikona": "💚", "opis": "Leczy 80 HP od razu"},
    {"nazwa": "Mikstura many", "klucz": "mana", "cena": 25, "ikona": "🔮", "opis": "Przywraca 30 many"},
    {"nazwa": "Antidotum", "klucz": "antidotum", "cena": 18, "ikona": "🧴", "opis": "Zdejmuje truciznę i krwawienie"},
]


def _kup_ekwipunek(gracz: Gracz, klucz: str) -> None:
    """Obsługuje zakup — przedmiot trafia do plecaka, potem jest zakładany."""
    item = EKWIPUNEK[klucz]
    cena = cena_dla(gracz, item["cena"])
    if gracz.zloto < cena:
        print(f"  Nie masz wystarczająco złota! Potrzebujesz {cena} szt.")
        nacisnij_enter()
        return

    gracz.zloto -= cena
    gracz.statystyki["zakupy"] = gracz.statystyki.get("zakupy", 0) + 1
    dodaj_do_plecaka(gracz, klucz)
    komunikat = zaloz(gracz, klucz)
    print(f"  Kupiono: {item['nazwa']}! Poprzedni ekwipunek wrócił do plecaka.")
    print(komunikat)
    for msg in sprawdz_questy(gracz):
        print(msg)
    nacisnij_enter()


def _kup_konsumable(gracz: Gracz, produkt: dict) -> None:
    """Obsługuje zakup mikstur i antidotum."""
    cena = cena_dla(gracz, produkt["cena"])
    if gracz.zloto < cena:
        print(f"  Nie masz wystarczająco złota! Potrzebujesz {cena} szt.")
        nacisnij_enter()
        return

    if produkt["klucz"] == "mana" and gracz.max_mana <= 0:
        print("  Twoja klasa nie korzysta z many — ten towar nic ci nie da.")
        nacisnij_enter()
        return

    gracz.zloto -= cena
    gracz.statystyki["zakupy"] = gracz.statystyki.get("zakupy", 0) + 1
    klucz = produkt["klucz"]
    if klucz == "mikstura":
        gracz.mikstury += 1
        print(f"  Kupiono: {produkt['nazwa']}. Mikstury: {gracz.mikstury}")
    elif klucz == "mikstura_duza":
        gracz.mikstury_duze = getattr(gracz, "mikstury_duze", 0) + 1
        print(f"  Kupiono: {produkt['nazwa']}. Większe mikstury: {gracz.mikstury_duze}")
    elif klucz == "mana":
        gracz.mikstury_many = getattr(gracz, "mikstury_many", 0) + 1
        print(f"  Kupiono: {produkt['nazwa']}. Mikstury many: {gracz.mikstury_many}")
    elif klucz == "antidotum":
        gracz.antidota = getattr(gracz, "antidota", 0) + 1
        print(f"  Kupiono: {produkt['nazwa']}. Antidota: {gracz.antidota}")
    for msg in sprawdz_questy(gracz):
        print(msg)
    nacisnij_enter()


def otworz_sklep(gracz: Gracz) -> None:
    """Wyświetla sklep ogólny (mikstury + lekki ekwipunek) i obsługuje transakcje."""
    while True:
        wyswietl_linie()
        print(f"  🏪  SKLEP  |  Twoje złoto: {gracz.zloto} szt.\n")

        print("  ─── Mikstury i medykamenty ───")
        for i, produkt in enumerate(_MIKSTURY, 1):
            cena = cena_dla(gracz, produkt["cena"])
            znizka = f" (zniżka: {produkt['cena']}→{cena})" if cena < produkt["cena"] else ""
            print(f"  [{i}] {produkt.get('ikona', '🧪')} {produkt['nazwa']} — {cena} złota{znizka}  ({produkt['opis']})")

        print(f"\n  ─── Ekwipunek ───")
        offset = len(_MIKSTURY)
        for j, klucz in enumerate(SKLEP_ASORTYMENT, 1):
            wyswietl_przedmiot(klucz, offset + j, gracz)

        print(f"\n  [S] 💰 Sprzedaj przedmiot z plecaka")
        print(f"  [0] 🚶 Wyjdź ze sklepu\n")

        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            print("  Do widzenia! Wracasz na drogę przygód.")
            nacisnij_enter()
            return

        if wybor.lower() == "s":
            menu_sprzedazy(gracz)
            continue

        try:
            idx = int(wybor) - 1
        except ValueError:
            print("  Wpisz numer produktu.")
            continue

        if 0 <= idx < len(_MIKSTURY):
            _kup_konsumable(gracz, _MIKSTURY[idx])
            continue

        eq_idx = idx - len(_MIKSTURY)
        if 0 <= eq_idx < len(SKLEP_ASORTYMENT):
            _kup_ekwipunek(gracz, SKLEP_ASORTYMENT[eq_idx])
            continue

        print("  Nieprawidłowy wybór.")


def otworz_kuznia(gracz: Gracz, tytul: str = "KUŹNIA GRIMBOLD'A") -> None:
    """Wyświetla kuźnię (ciężki ekwipunek bojowy) i obsługuje transakcje."""
    while True:
        wyswietl_linie()
        print(f"  ⚒  {tytul}  |  Twoje złoto: {gracz.zloto} szt.\n")
        print("  ─── Broń i zbroja ───")
        for i, klucz in enumerate(KUZNIA_ASORTYMENT, 1):
            wyswietl_przedmiot(klucz, i, gracz)
        print(f"\n  [S] 💰 Sprzedaj przedmiot z plecaka")
        print(f"  [0] 🚶 Wyjdź z kuźni\n")

        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            print("  Kowal kiwa głową. Do następnego razu!")
            nacisnij_enter()
            return

        if wybor.lower() == "s":
            menu_sprzedazy(gracz)
            continue

        try:
            idx = int(wybor) - 1
        except ValueError:
            print("  Wpisz numer przedmiotu.")
            continue

        if 0 <= idx < len(KUZNIA_ASORTYMENT):
            _kup_ekwipunek(gracz, KUZNIA_ASORTYMENT[idx])
            continue

        print("  Nieprawidłowy wybór.")
