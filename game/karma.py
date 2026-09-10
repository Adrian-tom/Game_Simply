"""Karma — reputacja bohatera i jej realne skutki w świecie.

Karma była wcześniej liczona w wielu miejscach (zdarzenia moralne, wątki NPC,
cechy pochodzenia), ale nigdzie nie czytana — poza ekranem końcowym. Ten moduł
zamienia ją w mechanikę: ceny u kupców, opór przy rekrutacji i reakcję świątyń.

Skala jest symetryczna wokół zera. Dodatnia karma = ludzie ci ufają, ujemna =
boją się ciebie i liczą sobie drożej, ale strach też bywa argumentem.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from game.player import Gracz


# (próg_dolny, klucz, nazwa, ikona) — pierwszy pasujący od góry wygrywa.
PROGI: tuple[tuple[int, str, str, str], ...] = (
    (12, "swiety", "Święty", "😇"),
    (5, "prawy", "Prawy", "🕊"),
    (-4, "neutralny", "Neutralny", "⚖"),
    (-11, "podejrzany", "Podejrzany", "🌑"),
    (-10 ** 9, "okrutny", "Okrutny", "💀"),
)

# Maksymalna zmiana ceny w obie strony (±15%).
_MAX_MODYFIKATOR_CEN = 0.15
# Karma, przy której modyfikator cen osiąga maksimum.
_KARMA_NASYCENIA = 15


def wartosc_karmy(gracz: Gracz) -> int:
    return int(getattr(gracz, "karma", 0) or 0)


def poziom(gracz: Gracz) -> tuple[str, str, str]:
    """Zwraca (klucz, nazwa, ikona) dla aktualnej karmy."""
    k = wartosc_karmy(gracz)
    for prog, klucz, nazwa, ikona in PROGI:
        if k >= prog:
            return klucz, nazwa, ikona
    return "okrutny", "Okrutny", "💀"


def etykieta(gracz: Gracz) -> str:
    """Krótki opis reputacji do nagłówków menu."""
    _, nazwa, ikona = poziom(gracz)
    return f"{ikona} {nazwa} ({wartosc_karmy(gracz):+d})"


def _znormalizowana(gracz: Gracz) -> float:
    """Karma sprowadzona do zakresu [-1.0, 1.0]."""
    k = wartosc_karmy(gracz)
    return max(-1.0, min(1.0, k / _KARMA_NASYCENIA))


def mnoznik_cen(gracz: Gracz) -> float:
    """Mnożnik cen u kupców: dobra sława potania, zła podnosi.

    Zwraca wartość z zakresu [0.85, 1.15].
    """
    return 1.0 - _MAX_MODYFIKATOR_CEN * _znormalizowana(gracz)


def opis_cen(gracz: Gracz) -> str:
    """Zdanie o tym, jak kupcy traktują bohatera — albo pusty string."""
    mnoznik = mnoznik_cen(gracz)
    procent = int(round(abs(1.0 - mnoznik) * 100))
    if procent < 1:
        return ""
    if mnoznik < 1.0:
        return f"  🕊  Twoja sława otwiera sakiewki kupców: ceny niższe o {procent}%."
    return f"  🌑  Kupcy patrzą na ciebie krzywo: ceny wyższe o {procent}%."


def modyfikator_rekrutacji(gracz: Gracz) -> int:
    """Zmiana ST próby przekonania NPC do dołączenia.

    Dobra sława ułatwia (ujemny modyfikator ST), zła utrudnia — porządni ludzie
    nie garną się do kogoś, kto zostawia za sobą trupy.
    """
    k = wartosc_karmy(gracz)
    if k >= 12:
        return -3
    if k >= 5:
        return -1
    if k <= -12:
        return 3
    if k <= -5:
        return 1
    return 0


def modyfikator_zastraszania(gracz: Gracz) -> int:
    """Zmiana ST zastraszania — tu zła sława pomaga.

    Lustrzane odbicie rekrutacji: kogo nie da się przekonać, tego można złamać.
    """
    return -modyfikator_rekrutacji(gracz)


def bonus_swiatyni(gracz: Gracz) -> int:
    """Dodatkowe HP z błogosławieństwa świątyni zależne od reputacji."""
    klucz, _, _ = poziom(gracz)
    return {"swiety": 25, "prawy": 10, "neutralny": 0, "podejrzany": -10, "okrutny": -20}[klucz]
