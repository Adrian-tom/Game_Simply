"""Kalendarz świata: rok = cztery pory po 30 dni.

Pora roku zmienia zbiory, rolnictwo, potrzebę opału, ceny żywności
i to, jak często bandy ruszają na osady. Zima jest sprawdzianem zapasów.
"""
from __future__ import annotations

DNI_PORY = 30
DNI_ROKU = 4 * DNI_PORY

PORY: list[dict] = [
    {
        "klucz": "wiosna", "nazwa": "Wiosna", "ikona": "🌱",
        "zbiory": 1.0, "rolnictwo": 1.0, "opal": False, "zagrozenie": 1.0, "ceny_zywnosci": 1.0,
        "opis": "Ziemia odmarza. Czas siać i odbudowywać zapasy.",
    },
    {
        "klucz": "lato", "nazwa": "Lato", "ikona": "☀",
        "zbiory": 1.2, "rolnictwo": 1.2, "opal": False, "zagrozenie": 1.1, "ceny_zywnosci": 0.9,
        "opis": "Długie dni. Drogi suche, bandy ruchliwe.",
    },
    {
        "klucz": "jesien", "nazwa": "Jesień", "ikona": "🍂",
        "zbiory": 1.1, "rolnictwo": 1.6, "opal": False, "zagrozenie": 1.0, "ceny_zywnosci": 0.75,
        "opis": "Żniwa. Co zbierzesz teraz, zjesz zimą.",
    },
    {
        "klucz": "zima", "nazwa": "Zima", "ikona": "❄",
        "zbiory": 0.4, "rolnictwo": 0.0, "opal": True, "zagrozenie": 1.3, "ceny_zywnosci": 1.5,
        "opis": "Mróz. Pola śpią, opał znika, głodne bandy schodzą z gór.",
    },
]


def _dzien(czas) -> int:
    return int(getattr(czas, "czas", czas) or 0)


def pora(czas) -> dict:
    """Pora roku dla gracza albo liczby dni."""
    return PORY[(_dzien(czas) % DNI_ROKU) // DNI_PORY]


def rok(czas) -> int:
    return _dzien(czas) // DNI_ROKU + 1


def dzien_pory(czas) -> int:
    return _dzien(czas) % DNI_PORY + 1


def dni_do_zimy(czas) -> int:
    """0 = już zima."""
    d = _dzien(czas) % DNI_ROKU
    start_zimy = 3 * DNI_PORY
    return 0 if d >= start_zimy else start_zimy - d


def opis_daty(czas) -> str:
    p = pora(czas)
    return f"{p['ikona']} {p['nazwa']}, dzień {dzien_pory(czas)}/{DNI_PORY} · rok {rok(czas)}"
