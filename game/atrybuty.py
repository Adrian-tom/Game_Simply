"""Atrybuty i testy umiejętności w stylu Baldur's Gate 3 (k20 + premia)."""

from __future__ import annotations

import random
from dataclasses import dataclass
from typing import TYPE_CHECKING

from game.utils import wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

MAX_ATRYBUT = 20
MIN_ATRYBUT = 8

ATRYBUTY: dict[str, dict] = {
    "sila": {
        "nazwa": "Siła",
        "skrot": "SIL",
        "ikona": "💪",
        "opis": "Wspinaczka, pchanie, siła ciosu",
    },
    "zrecznosc": {
        "nazwa": "Zręczność",
        "skrot": "ZRĘ",
        "ikona": "🏃",
        "opis": "Uniki, zamki, trafienia krytyczne",
    },
    "kondycja": {
        "nazwa": "Kondycja",
        "skrot": "KON",
        "ikona": "❤️",
        "opis": "Wytrzymałość i zapas HP",
    },
    "inteligencja": {
        "nazwa": "Inteligencja",
        "skrot": "INT",
        "ikona": "🧠",
        "opis": "Wiedza, magia, rozwiązywanie zagadek",
    },
    "madrosc": {
        "nazwa": "Mądrość",
        "skrot": "MDR",
        "ikona": "🦉",
        "opis": "Spostrzegawczość i przetrwanie",
    },
    "charyzma": {
        "nazwa": "Charyzma",
        "skrot": "CHA",
        "ikona": "✨",
        "opis": "Perswazja, zastraszanie, oszustwo",
    },
}

KOLEJNOSC_ATRYBUTOW: tuple[str, ...] = (
    "sila",
    "zrecznosc",
    "kondycja",
    "inteligencja",
    "madrosc",
    "charyzma",
)

SKILLE: dict[str, dict] = {
    "atletyka": {
        "nazwa": "Atletyka",
        "atrybut": "sila",
        "ikona": "🧗",
        "opis": "Wspinaczka, skoki, siłowe próby",
    },
    "akrobatyka": {
        "nazwa": "Akrobatyka",
        "atrybut": "zrecznosc",
        "ikona": "🤸",
        "opis": "Uniki, równowaga, zejście z ciosu",
    },
    "zwinne_palce": {
        "nazwa": "Zwinne palce",
        "atrybut": "zrecznosc",
        "ikona": "🔑",
        "opis": "Zamki, kradzież, rozbrajanie pułapek",
    },
    "spostrzegawczosc": {
        "nazwa": "Spostrzegawczość",
        "atrybut": "madrosc",
        "ikona": "👁",
        "opis": "Ukryte przejścia, pułapki, kłamstwa",
    },
    "przetrwanie": {
        "nazwa": "Przetrwanie",
        "atrybut": "madrosc",
        "ikona": "🐾",
        "opis": "Dzicz, tropy, żywioły",
    },
    "perswazja": {
        "nazwa": "Perswazja",
        "atrybut": "charyzma",
        "ikona": "💬",
        "opis": "Negocjacje i przekonywanie",
    },
    "zastraszanie": {
        "nazwa": "Zastraszanie",
        "atrybut": "charyzma",
        "ikona": "😠",
        "opis": "Groźby i dominacja",
    },
    "oszustwo": {
        "nazwa": "Oszustwo",
        "atrybut": "charyzma",
        "ikona": "🃏",
        "opis": "Blef, kłamstwo, zgrywanie roli",
    },
}

# Start jak w BG3: 8–16, klasa ma jedną „główną” statystykę
_START_ATRYBUTOW: dict[str, dict[str, int]] = {
    "Wojownik": {
        "sila": 16, "zrecznosc": 12, "kondycja": 15,
        "inteligencja": 8, "madrosc": 10, "charyzma": 10,
    },
    "Mag": {
        "sila": 8, "zrecznosc": 12, "kondycja": 12,
        "inteligencja": 16, "madrosc": 13, "charyzma": 10,
    },
    "Lotrzyk": {
        "sila": 8, "zrecznosc": 16, "kondycja": 12,
        "inteligencja": 12, "madrosc": 10, "charyzma": 14,
    },
    "Druid": {
        "sila": 10, "zrecznosc": 12, "kondycja": 13,
        "inteligencja": 10, "madrosc": 16, "charyzma": 10,
    },
    "Nekromanta": {
        "sila": 8, "zrecznosc": 12, "kondycja": 12,
        "inteligencja": 16, "madrosc": 10, "charyzma": 13,
    },
}

_BIEGLOSCI_KLAS: dict[str, tuple[str, ...]] = {
    "Wojownik": ("atletyka", "zastraszanie"),
    "Mag": ("spostrzegawczosc", "perswazja"),
    "Lotrzyk": ("zwinne_palce", "akrobatyka", "oszustwo"),
    "Druid": ("przetrwanie", "spostrzegawczosc"),
    "Nekromanta": ("zastraszanie", "oszustwo"),
}


@dataclass
class WynikTestu:
    skill: str
    st: int
    rzut: int
    premia: int
    suma: int
    sukces: bool
    krytyczny: bool
    wpadka: bool


def startowe_atrybuty(klasa: str) -> dict[str, int]:
    baza = _START_ATRYBUTOW.get(klasa) or _START_ATRYBUTOW["Wojownik"]
    return dict(baza)


def biegle_skille_klasy(klasa: str) -> list[str]:
    return list(_BIEGLOSCI_KLAS.get(klasa, ()))


def zapewnij_atrybuty(gracz: Gracz) -> None:
    """Uzupełnia brakujące pola (nowa postać albo stary zapis)."""
    if not getattr(gracz, "atrybuty", None):
        gracz.atrybuty = startowe_atrybuty(gracz.klasa)
    else:
        start = startowe_atrybuty(gracz.klasa)
        for k, v in start.items():
            gracz.atrybuty.setdefault(k, v)
        for k, v in list(gracz.atrybuty.items()):
            gracz.atrybuty[k] = max(1, min(MAX_ATRYBUT, int(v)))
    if not getattr(gracz, "biegle_skille", None):
        gracz.biegle_skille = biegle_skille_klasy(gracz.klasa)


def wartosc(gracz: Gracz, atrybut: str) -> int:
    zapewnij_atrybuty(gracz)
    return int(gracz.atrybuty.get(atrybut, 10))


def modyfikator(gracz: Gracz, atrybut: str) -> int:
    """Premia D&D: (wartość − 10) // 2."""
    return (wartosc(gracz, atrybut) - 10) // 2


def tekst_modyfikatora(mod: int) -> str:
    return f"+{mod}" if mod >= 0 else str(mod)


def biegosc(gracz: Gracz) -> int:
    """Premia z biegłości jak w 5e / BG3."""
    poziom = max(1, int(getattr(gracz, "poziom", 1)))
    return 2 + (poziom - 1) // 4


def czy_biegly(gracz: Gracz, skill: str) -> bool:
    zapewnij_atrybuty(gracz)
    return skill in (getattr(gracz, "biegle_skille", None) or [])


def premia_skilla(gracz: Gracz, skill: str) -> int:
    info = SKILLE[skill]
    premia = modyfikator(gracz, info["atrybut"])
    if czy_biegly(gracz, skill):
        premia += biegosc(gracz)
    from game.pochodzenie import premia_testu_cech
    premia += premia_testu_cech(gracz, skill)
    return premia


def trudnosc(gracz: Gracz, baza: int) -> int:
    """ST rośnie lekko z numerem regionu."""
    extra = max(0, int(getattr(gracz, "mapa_gen", 1)) - 1) // 2
    return min(20, max(8, baza + extra))


def rzuc_test(gracz: Gracz, skill: str, st: int) -> WynikTestu:
    """Cichy rzut k20. Nat 20 = sukces, nat 1 = porażka."""
    rzut = random.randint(1, 20)
    premia = premia_skilla(gracz, skill)
    suma = rzut + premia
    krytyczny = rzut == 20
    wpadka = rzut == 1
    if krytyczny:
        sukces = True
    elif wpadka:
        sukces = False
    else:
        sukces = suma >= st
    return WynikTestu(skill, st, rzut, premia, suma, sukces, krytyczny, wpadka)


def opis_testu(wynik: WynikTestu) -> str:
    info = SKILLE[wynik.skill]
    atr = ATRYBUTY[info["atrybut"]]["nazwa"]
    if wynik.krytyczny:
        status = "KRYTYCZNY SUKCES!"
    elif wynik.wpadka:
        status = "KRYTYCZNA WPADKA"
    elif wynik.sukces:
        status = "SUKCES"
    else:
        status = "porażka"
    znak = tekst_modyfikatora(wynik.premia)
    return (
        f"  {info.get('ikona', '🎲')}  {info['nazwa']} ({atr})  ST {wynik.st}\n"
        f"      k20: {wynik.rzut} {znak} = {wynik.suma}  →  {status}"
    )


def przeprowadz_test(
    gracz: Gracz,
    skill: str,
    st: int,
    *,
    pauza: bool = False,
) -> WynikTestu:
    """Rzuca test i wypisuje wynik jak w BG3."""
    wynik = rzuc_test(gracz, skill, st)
    print()
    print(opis_testu(wynik))
    if pauza:
        nacisnij_enter()
    return wynik


def szansa_kryta_zrecznosc(gracz: Gracz) -> float:
    """+2% krytyka za każdy punkt premii z Zręczności + cechy."""
    from game.pochodzenie import suma_flagi
    return 0.02 * max(0, modyfikator(gracz, "zrecznosc")) + suma_flagi(gracz, "kryt")


def szansa_uniku_zrecznosc(gracz: Gracz) -> float:
    """Pasywny unik: 3% × premia ZRĘ + cechy, max 28%. Biegłość w akrobatyce +4%."""
    from game.pochodzenie import suma_flagi
    baza = 0.03 * max(0, modyfikator(gracz, "zrecznosc"))
    if czy_biegly(gracz, "akrobatyka"):
        baza += 0.04
    baza += suma_flagi(gracz, "unik")
    return min(0.28, baza)


def podnies_atrybut(gracz: Gracz, klucz: str) -> str:
    """Wydaje 1 pkt. atrybutów. Zwraca komunikat."""
    zapewnij_atrybuty(gracz)
    if klucz not in ATRYBUTY:
        return "  Nieznany atrybut."
    if getattr(gracz, "punkty_atrybutow", 0) <= 0:
        return "  Brak punktów atrybutów."
    if gracz.atrybuty[klucz] >= MAX_ATRYBUT:
        return f"  {ATRYBUTY[klucz]['nazwa']} jest już na maksimum ({MAX_ATRYBUT})."
    poprzedni_mod = modyfikator(gracz, klucz)
    gracz.atrybuty[klucz] += 1
    gracz.punkty_atrybutow -= 1
    nowy_mod = modyfikator(gracz, klucz)
    extra = []
    if klucz == "kondycja":
        gracz.max_hp += 8
        gracz.hp = min(gracz.max_hp, gracz.hp + 8)
        extra.append("+8 max HP")
    elif klucz == "sila" and nowy_mod > poprzedni_mod:
        gracz.atak += 1
        extra.append("+1 Atak")
    elif klucz == "zrecznosc" and nowy_mod > poprzedni_mod:
        gracz.obrona += 1
        extra.append("+1 Obrona")
    elif klucz == "inteligencja" and gracz.max_mana > 0 and nowy_mod > poprzedni_mod:
        gracz.max_mana += 8
        gracz.mana = min(gracz.max_mana, gracz.mana + 8)
        extra.append("+8 max many")
    bonus = f"  ({', '.join(extra)})" if extra else ""
    return (
        f"  {ATRYBUTY[klucz]['nazwa']}: {gracz.atrybuty[klucz]}"
        f" ({tekst_modyfikatora(nowy_mod)}){bonus}"
    )


def linia_atrybutow(gracz: Gracz) -> str:
    zapewnij_atrybuty(gracz)
    czesci = []
    for k in KOLEJNOSC_ATRYBUTOW:
        info = ATRYBUTY[k]
        wart = wartosc(gracz, k)
        czesci.append(f"{info['ikona']}{info['skrot']} {wart}")
    return "  Atrybuty: " + "  ".join(czesci)


def wyswietl_karte_postaci(gracz: Gracz) -> None:
    """Pełna karta: 6 atrybutów, biegłości, premie do testów."""
    from game.utils import wyczysc

    zapewnij_atrybuty(gracz)
    wyczysc()
    wyswietl_linie("═")
    print(f"  📋  KARTA POSTACI  —  {gracz.imie} [{gracz.klasa}]")
    wyswietl_linie("═")
    print(f"  Biegłość: +{biegosc(gracz)}   Punkty atrybutów: {gracz.punkty_atrybutow}")
    print()
    print("  Atrybuty (jak w Baldur's Gate 3):")
    for k in KOLEJNOSC_ATRYBUTOW:
        info = ATRYBUTY[k]
        wart = wartosc(gracz, k)
        mod = modyfikator(gracz, k)
        print(
            f"    {info['ikona']} {info['skrot']:3}  {info['nazwa']:14} {wart:2}"
            f"  ({tekst_modyfikatora(mod):>3})  — {info['opis']}"
        )
    print()
    print("  Testy umiejętności (k20 + premia vs ST):")
    for skill, info in SKILLE.items():
        premia = premia_skilla(gracz, skill)
        znacznik = "●" if czy_biegly(gracz, skill) else "○"
        print(
            f"    [{znacznik}] {info.get('ikona', '🎲')} {info['nazwa']:18} {tekst_modyfikatora(premia):>3}"
            f"   ({ATRYBUTY[info['atrybut']]['nazwa']})"
        )
    print()
    print("  ● = biegłość (dodaje premię z poziomu)   ○ = bez biegłości")
    print(f"  Unik pasywny: {int(szansa_uniku_zrecznosc(gracz) * 100)}%"
          f"   Bonus krytyka: +{int(szansa_kryta_zrecznosc(gracz) * 100)}%")
    from game.pochodzenie import nazwa_pochodzenia, nazwy_cech
    print(f"  Pochodzenie: {nazwa_pochodzenia(gracz)}")
    print(f"  Cechy: {nazwy_cech(gracz)}")
    print()
    nacisnij_enter()
