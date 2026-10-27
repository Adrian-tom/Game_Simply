"""Gładki szum wartości na współrzędnych świata — podstawa ciągłych biomów.

Wzorzec z Project Zomboid (analiza w ``PROJECT-ZOMBOID-ANALIZA.md``): świat nie jest
sklejany z niezależnych kawałków, tylko pytany o cechy w każdym punkcie — temperaturę,
wilgotność, rzeźbę terenu — z szumu zasianego jednym ziarnem. Dzięki temu las przechodzi
przez granicę regionu bez szwu. Tu szum jest prosty (wartości w węzłach siatki
z interpolacją wygładzoną), a całość na liczbach całkowitych i działaniach zmiennoprzecinkowych
IEEE, więc ``unity/.../Szum.cs`` daje te same liczby co ten moduł.
"""
from __future__ import annotations

import math

_MASKA = 0xFFFFFFFF


def _skrot(ix: int, iy: int, ziarno: int) -> float:
    """Pseudolosowa wartość 0–1 w węźle siatki (mieszanie 32-bitowe, bez ``hash`` Pythona)."""
    h = (ix * 374761393 + iy * 668265263 + ziarno * 2246822519) & _MASKA
    h = ((h ^ (h >> 13)) * 1274126177) & _MASKA
    h ^= h >> 16
    return h / 4294967296.0


def _wygladz(t: float) -> float:
    return t * t * (3.0 - 2.0 * t)


def szum2(x: float, y: float, ziarno: int) -> float:
    """Szum wartości 0–1: węzły co 1.0, wygładzona interpolacja dwuliniowa."""
    x0 = math.floor(x)
    y0 = math.floor(y)
    u = _wygladz(x - x0)
    v = _wygladz(y - y0)
    a = _skrot(x0, y0, ziarno)
    b = _skrot(x0 + 1, y0, ziarno)
    c = _skrot(x0, y0 + 1, ziarno)
    d = _skrot(x0 + 1, y0 + 1, ziarno)
    g = a + (b - a) * u
    d_ = c + (d - c) * u
    return g + (d_ - g) * v


def fbm(x: float, y: float, ziarno: int, oktawy: int = 3) -> float:
    """Kilka warstw szumu o malejącej amplitudzie — duże plamy z drobnym zróżnicowaniem."""
    suma = 0.0
    waga = 1.0
    razem = 0.0
    czestosc = 1.0
    for o in range(oktawy):
        suma += waga * szum2(x * czestosc, y * czestosc, (ziarno + 7919 * o) & 0x7FFFFFFF)
        razem += waga
        waga *= 0.5
        czestosc *= 2.0
    return suma / razem
