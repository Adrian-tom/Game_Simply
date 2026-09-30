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


# Trwająca walka (gracz, wrog, stan) — okno rysuje wtedy arenę zamiast mapy.
walka: dict | None = None


def ustaw_walke(gracz, wrog, stan) -> None:
    global walka
    walka = {"gracz": gracz, "wrog": wrog, "stan": stan}


def koniec_walki() -> None:
    global walka
    walka = None


# Kto mówi w trwającej rozmowie — okno rysuje wtedy jego portret.
rozmowa: str | None = None


def ustaw_rozmowe(mowi: str | None) -> None:
    global rozmowa
    rozmowa = mowi


def ustaw_gracza(nowy: Gracz | None) -> None:
    global gracz
    gracz = nowy


def ustaw_skroty(mapa: dict[str, str] | None) -> None:
    global skroty
    skroty = dict(mapa or {})
