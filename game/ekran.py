"""Punkt styku logiki gry z oknem graficznym.

Logika nie importuje pygame. Zapisuje tu tylko, *co* jest do pokazania
(bieżąca postać, skróty klawiszy), a okno z pakietu ``grafika`` to czyta.
W trybie tekstowym nikt tego nie czyta i wszystko działa jak dawniej.
"""
from __future__ import annotations

from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from game.player import Gracz

# Ustawiane przez okno graficzne; w terminalu zostaje False.
graficzny: bool = False

# Postać, której świat rysuje okno. None = ekran tytułowy.
gracz: Gracz | None = None

# Klawisz specjalny (np. "gora") → odpowiedź wpisywana za gracza.
skroty: dict[str, str] = {}


def ustaw_gracza(nowy: Gracz | None) -> None:
    global gracz
    gracz = nowy


def ustaw_skroty(mapa: dict[str, str] | None) -> None:
    global skroty
    skroty = dict(mapa or {})
