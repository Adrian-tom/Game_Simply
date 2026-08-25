"""Surowce z mapy i rozbudowa obozu."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.mapa import pole_gracza, odkryj_pole, opis_punktu, PUNKTY_MITYCZNE
from game.quests import sprawdz_questy
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

MAX_ZBIOROW_NA_POLU = 2

SUROWCE: dict[str, dict] = {
    "drewno": {"nazwa": "drewno", "ikona": "🌲"},
    "kamien": {"nazwa": "kamień", "ikona": "🪨"},
    "ziola": {"nazwa": "zioła", "ikona": "🌿"},
    "skora": {"nazwa": "skóra", "ikona": "🦌"},
    "ruda": {"nazwa": "ruda", "ikona": "⛏"},
}

# biom → lista (klucz, szansa, min, max)
_ZBIORY_BIOM: dict[str, list[tuple[str, float, int, int]]] = {
    "równiny": [
        ("ziola", 0.75, 1, 3),
        ("drewno", 0.50, 1, 2),
        ("kamien", 0.20, 1, 1),
    ],
    "las": [
        ("drewno", 0.90, 2, 4),
        ("ziola", 0.40, 1, 2),
        ("skora", 0.30, 1, 2),
    ],
    "bagna": [
        ("ziola", 0.90, 2, 4),
        ("drewno", 0.35, 1, 2),
        ("skora", 0.15, 1, 1),
    ],
    "ruiny": [
        ("kamien", 0.85, 2, 4),
        ("ruda", 0.30, 1, 2),
        ("drewno", 0.20, 1, 1),
    ],
    "wzgórza": [
        ("kamien", 0.70, 2, 3),
        ("ruda", 0.50, 1, 2),
        ("drewno", 0.25, 1, 2),
    ],
    "kanion": [
        ("ruda", 0.75, 2, 3),
        ("kamien", 0.55, 1, 3),
    ],
}

BUDYNKI: dict[str, dict] = {
    "sklep": {
        "nazwa": "Sklep",
        "ikona": "🏪",
        "opis": "Kupiec w obozie — mikstury i podstawowy ekwipunek.",
        "koszt": {"drewno": 6, "kamien": 3, "zloto": 15},
    },
    "dom": {
        "nazwa": "Dom",
        "ikona": "🏠",
        "opis": "Lepszy odpoczynek: +80 HP za 5 złota, pełna mana.",
        "koszt": {"drewno": 10, "kamien": 5, "ziola": 4, "zloto": 25},
    },
    "kuznia": {
        "nazwa": "Kuźnia",
        "ikona": "🔨",
        "opis": "Kowal w obozie — broń i ciężkie zbroje.",
        "koszt": {"drewno": 6, "kamien": 8, "ruda": 5, "zloto": 40},
    },
    "stajnie": {
        "nazwa": "Stajnie",
        "ikona": "🐴",
        "opis": "Szybka podróż do odkrytych punktów w regionie.",
        "koszt": {"drewno": 8, "kamien": 4, "skora": 5, "zloto": 30},
    },
    "targ": {
        "nazwa": "Targ",
        "ikona": "🛒",
        "opis": "Stoiska osadników. Każdy dzień nieobecności = złoto.",
        "koszt": {"drewno": 8, "kamien": 4, "skora": 3, "zloto": 35},
    },
    "warsztat": {
        "nazwa": "Warsztat",
        "ikona": "🔧",
        "opis": "Wytwarzanie mikstur. Rzemieślnicy pracują, gdy ciebie nie ma.",
        "koszt": {"drewno": 6, "kamien": 6, "ruda": 3, "ziola": 4, "zloto": 30},
    },
}


def _magazyn(gracz: Gracz) -> dict[str, int]:
    if getattr(gracz, "surowce", None) is None:
        gracz.surowce = {k: 0 for k in SUROWCE}
    for k in SUROWCE:
        gracz.surowce.setdefault(k, 0)
    return gracz.surowce


def dodaj_surowiec(gracz: Gracz, klucz: str, ile: int) -> None:
    if ile <= 0 or klucz not in SUROWCE:
        return
    mag = _magazyn(gracz)
    mag[klucz] = mag.get(klucz, 0) + ile
    gracz.statystyki["zebrane_surowce"] = gracz.statystyki.get("zebrane_surowce", 0) + ile


def ma_budynek(gracz: Gracz, klucz: str) -> bool:
    return klucz in getattr(gracz, "budynki", set())


def linia_surowcow(gracz: Gracz) -> str:
    mag = _magazyn(gracz)
    czesci = [
        f"{SUROWCE[k]['ikona']}{mag.get(k, 0)}"
        for k in SUROWCE
    ]
    return "Surowce: " + "  ".join(czesci)


def opis_obozu(gracz: Gracz) -> str:
    zbudowane = [
        f"{BUDYNKI[k]['ikona']} {BUDYNKI[k]['nazwa']}"
        for k in BUDYNKI
        if ma_budynek(gracz, k)
    ]
    chaty = int(getattr(gracz, "chaty", 0) or 0)
    if chaty:
        zbudowane.append(f"🛖 {chaty}× chata")
    if not zbudowane:
        return "namiot i palenisko"
    return ", ".join(zbudowane)


def _format_kosztu(koszt: dict) -> str:
    czesci = []
    for k, ile in koszt.items():
        if k == "zloto":
            czesci.append(f"{ile} zł")
        else:
            info = SUROWCE[k]
            czesci.append(f"{info['ikona']}{ile} {info['nazwa']}")
    return ", ".join(czesci)


def _moze_zaplacic(gracz: Gracz, koszt: dict) -> bool:
    mag = _magazyn(gracz)
    if gracz.zloto < koszt.get("zloto", 0):
        return False
    for k, ile in koszt.items():
        if k == "zloto":
            continue
        if mag.get(k, 0) < ile:
            return False
    return True


def _pobierz_koszt(gracz: Gracz, koszt: dict) -> None:
    mag = _magazyn(gracz)
    gracz.zloto -= koszt.get("zloto", 0)
    for k, ile in koszt.items():
        if k == "zloto":
            continue
        mag[k] = mag.get(k, 0) - ile


def pozostale_zbiory(pole: dict) -> int:
    uzyte = int(pole.get("zbierania", 0))
    return max(0, MAX_ZBIOROW_NA_POLU - uzyte)


def zbierz_na_polu(gracz: Gracz) -> str:
    """
    Zbiera surowce z aktualnego pola.
    Zwraca: 'ok', 'blokada', 'wyczerpane' albo 'walka'.
    """
    pole = pole_gracza(gracz)
    punkt = pole.get("punkt")
    if punkt == "obóz":
        print("\n  Przy palenisku nie ma czego zbierać. Wyjdź w teren.")
        return "blokada"
    if punkt == "boss" or punkt in PUNKTY_MITYCZNE or punkt == "miasto":
        print("\n  Nie pora na zbieractwo — to miejsce jest zbyt niebezpieczne.")
        return "blokada"
    if pozostale_zbiory(pole) <= 0:
        print("\n  To pole jest już ogołocone. Spróbuj indziej albo w nowym regionie.")
        return "wyczerpane"

    biom = pole.get("biom", "równiny")
    tabela = _ZBIORY_BIOM.get(biom, _ZBIORY_BIOM["równiny"])
    zyski: list[tuple[str, int]] = []
    for klucz, szansa, mn, mx in tabela:
        if random.random() <= szansa:
            zyski.append((klucz, random.randint(mn, mx)))
    if not zyski:
        klucz, _, mn, mx = tabela[0]
        zyski.append((klucz, random.randint(mn, mx)))

    pole["zbierania"] = int(pole.get("zbierania", 0)) + 1
    print(f"\n  Przeszukujesz {biom}...")
    for klucz, ile in zyski:
        dodaj_surowiec(gracz, klucz, ile)
        info = SUROWCE[klucz]
        print(f"  {info['ikona']}  +{ile} {info['nazwa']}")
    print(f"  {linia_surowcow(gracz)}")
    zost = pozostale_zbiory(pole)
    if zost:
        print(f"  (Na tym polu zostało zbiorów: {zost})")
    else:
        print("  (Pole wyczerpane.)")

    if random.random() < 0.12:
        return "walka"
    return "ok"


def zbuduj(gracz: Gracz, klucz: str) -> str:
    """Próbuje wznieść budynek. Zwraca komunikat."""
    info = BUDYNKI[klucz]
    if ma_budynek(gracz, klucz):
        return f"  {info['nazwa']} już stoi w obozie."
    if not _moze_zaplacic(gracz, info["koszt"]):
        return (
            f"  Brakuje materiałów na {info['nazwa'].lower()}."
            f"  Potrzeba: {_format_kosztu(info['koszt'])}."
        )
    _pobierz_koszt(gracz, info["koszt"])
    if getattr(gracz, "budynki", None) is None:
        gracz.budynki = set()
    gracz.budynki.add(klucz)
    gracz.statystyki["zbudowane_budynki"] = len(gracz.budynki)
    return (
        f"  {info['ikona']}  Wznosisz: {info['nazwa']}!"
        f"  ({info['opis']})"
    )


def menu_rozbudowy(gracz: Gracz) -> None:
    """Obozowe menu budowy."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  ROZBUDOWA OBOZU")
        wyswietl_linie("═")
        print(f"\n  Teraz: {opis_obozu(gracz)}")
        print(f"  {linia_surowcow(gracz)}")
        print(f"  Złoto: {gracz.zloto} szt.\n")
        print("  Zbieraj surowce na wyprawie — [6] przy eksploracji.")
        print("  Las = drewno, bagna = zioła, wzgórza/kanion = ruda i kamień.\n")

        for i, (klucz, info) in enumerate(BUDYNKI.items(), 1):
            if ma_budynek(gracz, klucz):
                status = "✔ zbudowane"
            else:
                status = _format_kosztu(info["koszt"])
            print(f"  [{i}] {info['ikona']} {info['nazwa']}  — {info['opis']}")
            print(f"      {status}")
        from game.osada import MAX_CHATY, KOSZT_CHATY, liczba_chat, zbuduj_chate
        nr_chaty = len(BUDYNKI) + 1
        print(
            f"\n  [{nr_chaty}] 🛖 Chata osadnika  — miejsce do życia i pracy"
            f"  ({liczba_chat(gracz)}/{MAX_CHATY})"
        )
        print(f"      {_format_kosztu(KOSZT_CHATY)}")
        print("\n  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            klucze = list(BUDYNKI)
            if 0 <= idx < len(klucze):
                print(zbuduj(gracz, klucze[idx]))
                for msg in sprawdz_questy(gracz):
                    print(msg)
                nacisnij_enter()
                continue
            if idx == len(klucze):
                print(zbuduj_chate(gracz))
                for msg in sprawdz_questy(gracz):
                    print(msg)
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def odkryte_punkty(gracz: Gracz) -> list[tuple[int, int, dict]]:
    """Odkryte pola z punktem orientacyjnym (bez bossa)."""
    pola = getattr(gracz, "mapa_pola", None) or []
    wynik = []
    for y, wiersz in enumerate(pola):
        for x, pole in enumerate(wiersz):
            if not pole.get("odkryte"):
                continue
            punkt = pole.get("punkt")
            if punkt and punkt != "boss":
                wynik.append((x, y, pole))
    return wynik


def menu_stajni(gracz: Gracz) -> None:
    """Szybka podróż do odkrytego punktu w bieżącym regionie."""
    if not ma_budynek(gracz, "stajnie"):
        print("\n  Nie masz jeszcze stajni.")
        nacisnij_enter()
        return

    cele = odkryte_punkty(gracz)
    wyczysc()
    wyswietl_linie("═")
    print("  STAJNIE  —  szybka podróż")
    wyswietl_linie("═")
    print("\n  Koń zawiezie cię do znanego miejsca w tym regionie.")
    print("  Nowe regiony i bossowie wymagają zwykłej drogi.\n")
    if not cele:
        print("  Nie znasz jeszcze żadnego punktu na mapie.")
        print("  Odkryj karczmę, kuźnię, świątynię, jaskinię albo obóz.")
        nacisnij_enter()
        return

    for i, (x, y, pole) in enumerate(cele, 1):
        nazwa = opis_punktu(pole.get("punkt"))
        tu = "  ← tu jesteś" if x == gracz.mapa_x and y == gracz.mapa_y else ""
        print(f"  [{i}] {nazwa}  ({x}, {y})  {pole['biom']}{tu}")
    print("  [0] Wróć\n")
    wybor = input("  Cel podróży: ").strip()
    if wybor == "0":
        return
    try:
        idx = int(wybor) - 1
        if 0 <= idx < len(cele):
            x, y, pole = cele[idx]
            if x == gracz.mapa_x and y == gracz.mapa_y:
                print("  Już tu jesteś.")
                nacisnij_enter()
                return
            gracz.mapa_x = x
            gracz.mapa_y = y
            odkryj_pole(gracz)
            from game.osada import dodaj_czas
            dodaj_czas(gracz, 1)
            print(
                f"\n  🐴  Galopujesz do: {opis_punktu(pole.get('punkt'))}"
                f" ({pole['biom']})."
            )
            nacisnij_enter()
            return
    except ValueError:
        pass
    print("  Nieprawidłowy wybór.")
    nacisnij_enter()
