"""Osobna mapa miasta: handel, gildia, NPC i najem osadników."""

from __future__ import annotations

from typing import TYPE_CHECKING

from game.dialogues import dialog_burmistrz, dialog_kupiec_miejski, dialog_kaplan
from game.osada import (
    CENA_OSADNIKA,
    dodaj_czas,
    menu_sprzedazy_surowcow,
    wolne_chaty,
    zatrudnij_osadnika,
)
from game.shop import otworz_kuznia, otworz_sklep
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

ROZMIAR_MIASTA = 3

# y, x → lokacja
_LOKACJE: dict[tuple[int, int], dict] = {
    (0, 0): {"id": "karczma", "nazwa": "Karczma Pod Lwem", "ikona": "🍺", "glif": "k"},
    (1, 0): {"id": "rynek", "nazwa": "Rynek handlowy", "ikona": "💰", "glif": "R"},
    (2, 0): {"id": "kuznia", "nazwa": "Miejska kuźnia", "ikona": "⚒", "glif": "u"},
    (0, 1): {"id": "zaulek", "nazwa": "Zaułek cienia", "ikona": "🕶", "glif": "z"},
    (1, 1): {"id": "plac", "nazwa": "Plac ratuszowy", "ikona": "🏛", "glif": "P"},
    (2, 1): {"id": "swiatynia", "nazwa": "Świątynia murów", "ikona": "🛕", "glif": "s"},
    (0, 2): {"id": "magazyn", "nazwa": "Spichlerz i waga", "ikona": "📦", "glif": "m"},
    (1, 2): {"id": "gildia", "nazwa": "Gildia najemników", "ikona": "🤝", "glif": "G"},
    (2, 2): {"id": "brama", "nazwa": "Brama południowa", "ikona": "🚪", "glif": "B"},
}

_KIERUNKI = {
    "1": ("północ", 0, -1),
    "2": ("zachód", -1, 0),
    "3": ("wschód", 1, 0),
    "4": ("południe", 0, 1),
}


def _lokacja(x: int, y: int) -> dict:
    return _LOKACJE[(x, y)]


def _rysuj(gracz: Gracz) -> None:
    x = getattr(gracz, "miasto_x", 2)
    y = getattr(gracz, "miasto_y", 2)
    tutaj = _lokacja(x, y)
    print(f"  MIASTO   pole ({x}, {y})  —  {tutaj['ikona']} {tutaj['nazwa']}")
    print()
    naglowek = "     " + "".join(f"{cx:>3}" for cx in range(ROZMIAR_MIASTA))
    print(naglowek)
    for cy in range(ROZMIAR_MIASTA):
        komorki = []
        for cx in range(ROZMIAR_MIASTA):
            loc = _lokacja(cx, cy)
            znak = "@" if cx == x and cy == y else loc["glif"]
            komorki.append(f"{znak:>3}")
        print(f"  {cy}  {''.join(komorki)}")
    print()
    print("  @ ty   k karczma   R rynek   u kuźnia   z zaułek")
    print("  P plac   s świątynia   m magazyn   G gildia   B brama")
    print()


def _wejdz_do_lokacji(gracz: Gracz) -> None:
    loc = _lokacja(getattr(gracz, "miasto_x", 2), getattr(gracz, "miasto_y", 2))
    ident = loc["id"]
    wyczysc()
    wyswietl_linie("═")
    print(f"  {loc['ikona']}  {loc['nazwa']}")
    wyswietl_linie("═")

    if ident == "brama":
        print("\n  Strażnicy kiwają grotami. Za plecami szum traktów.")
        print("  Możesz wrócić na mapę świata z menu miasta [0].")
        nacisnij_enter()
        return

    if ident == "rynek":
        print("\n  Kupcy krzyczą ceny, które zmieniają się co dziesięć oddechów.")
        print("  [1] 🏪  Sklep miejski")
        print("  [2] 🐪  Porozmawiać z kupcem Vasceem")
        print("  [0] 🚶  Wyjść na bruk\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "1":
            otworz_sklep(gracz)
        elif wybor == "2":
            dialog_kupiec_miejski(gracz)
        return

    if ident == "kuznia":
        print("\n  Iskry skaczą po kamieniu. Miejski kowal nie pyta o imię.")
        nacisnij_enter()
        otworz_kuznia(gracz, tytul="MIEJSKA KUŹNIA")
        return

    if ident == "karczma":
        print("\n  Piwo tu droższe, plotki ostrzejsze. Nikt nie ufa obcym do dna kufla.")
        print("  [1] 😴  Odpocząć (+25 HP, 8 złota)")
        print("  [2] 👂  Słuchać rozmów (+kilka sztuk złota)")
        print("  [0] 🚶  Wyjść\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "1":
            if gracz.zloto < 8:
                print("  Nie stać cię na izbę.")
            else:
                gracz.zloto -= 8
                poprzednie = gracz.hp
                gracz.hp = min(gracz.max_hp, gracz.hp + 25)
                print(f"  Odpoczywasz. HP +{gracz.hp - poprzednie}.")
            nacisnij_enter()
        elif wybor == "2":
            zysk = 6
            gracz.zloto += zysk
            print(f"  Pijak pomylił cię z windykatorem i wcisnął {zysk} złota „na milczenie”.")
            nacisnij_enter()
        return

    if ident == "plac":
        print("\n  Burmistrz Mirena stoi na stopniach, jakby ratusz mógł runąć bez jej barków.")
        nacisnij_enter()
        dialog_burmistrz(gracz)
        return

    if ident == "swiatynia":
        print("\n  Kadzidło gryzie w gardło. Kapłan przy murach ma ten sam zakon co w polu.")
        nacisnij_enter()
        dialog_kaplan(gracz)
        return

    if ident == "magazyn":
        print("\n  Waga kłamie o pół funta na korzyść miasta. Możesz sprzedać surowce.")
        nacisnij_enter()
        menu_sprzedazy_surowcow(gracz)
        return

    if ident == "gildia":
        print("\n  Tutaj kupuje się ręce do pracy, nie przysięgi.")
        print(f"  Wolne chaty w twojej osadzie: {wolne_chaty(gracz)}")
        print(f"  Cena przeprowadzki: {CENA_OSADNIKA} złota")
        print("  [1] 🌾  Zatrudnij zbieracza")
        print("  [2] 💰  Zatrudnij handlarza")
        print("  [3] 🔧  Zatrudnij rzemieślnika")
        print("  [0] 🚶  Wyjść\n")
        wybor = input("  Twój wybór: ").strip()
        mapa = {"1": "zbiory", "2": "handel", "3": "rzemioslo"}
        if wybor in mapa:
            print(zatrudnij_osadnika(gracz, mapa[wybor]))
            nacisnij_enter()
        return

    if ident == "zaulek":
        print("\n  Mokry bruk. Ktoś tu gubi księgi długów i zęby.")
        print("  [1] 🔍  Przeszukać nisze (ryzyko)")
        print("  [0] 🚶  Cofnąć się na światło\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "1":
            from game.atrybuty import przeprowadz_test, trudnosc
            st = trudnosc(gracz, 14)
            wynik = przeprowadz_test(gracz, "zwinne_palce", st)
            if wynik.sukces:
                zysk = 22
                gracz.zloto += zysk
                print(f"  W szczelinie sakiewka gildii. +{zysk} złota.")
            else:
                strata = min(12, gracz.zloto)
                gracz.zloto -= strata
                print(f"  Kieszonkowiec był szybszy. −{strata} złota.")
            nacisnij_enter()
        return


def wejdz_do_miasta(gracz: Gracz) -> None:
    """Osobna mapa 3×3. Start przy bramie."""
    gracz.miasto_x = 2
    gracz.miasto_y = 2
    gracz.statystyki["odwiedzone_miasta"] = gracz.statystyki.get("odwiedzone_miasta", 0) + 1
    wyczysc()
    wyswietl_linie("═")
    print("  MIASTO ZA MURAMI")
    wyswietl_linie("═")
    print("\n  Brama zamyka za tobą trakt. Tu obowiązują inne ceny i inne kłamstwa.")
    print("  Poruszasz się po dzielnicach. Handel jest legalny — prawie.")
    nacisnij_enter()

    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  🏙  MAPA MIASTA")
        wyswietl_linie("═")
        _rysuj(gracz)
        loc = _lokacja(gracz.miasto_x, gracz.miasto_y)
        print(f"  Jesteś: {loc['ikona']} {loc['nazwa']}\n")
        print("  [1] ⬆ Północ  [2] ⬅ Zachód  [3] ➡ Wschód  [4] ⬇ Południe")
        print("  [5] 🚪 Wejdź / porozmawiaj")
        print("  [0] 🏕 Wyjdź za bramę (powrót na mapę świata)\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            print("  Straż otwiera wrota. Wracasz na trakt.")
            nacisnij_enter()
            return
        if wybor in _KIERUNKI:
            _, dx, dy = _KIERUNKI[wybor]
            nx = gracz.miasto_x + dx
            ny = gracz.miasto_y + dy
            if nx < 0 or ny < 0 or nx >= ROZMIAR_MIASTA or ny >= ROZMIAR_MIASTA:
                print("  Mur. Dalej tylko blanki i kusze.")
                nacisnij_enter()
                continue
            gracz.miasto_x = nx
            gracz.miasto_y = ny
            dodaj_czas(gracz, 1)
            continue
        if wybor == "5":
            dodaj_czas(gracz, 1)
            _wejdz_do_lokacji(gracz)
            continue
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
