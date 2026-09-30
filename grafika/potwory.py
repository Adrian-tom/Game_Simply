"""Sprite'y wrogów liczone w kodzie — rodzina z nazwy, bryły cieniowane światłem.

Każdy wróg składa się z kilku prymitywów (``kula`` = cieniowana elipsa,
wielokąty z rampą), a kolory i dodatki (rogi, kaptur, broń, skrzydła)
wynikają z rodziny. Bossowie są więksi i mają aurę.
"""
from __future__ import annotations

import math

import pygame

from grafika.piksele import ciemniej, jasniej, kula, obrys, wypelnij


def rampa(kolor) -> list:
    """Cztery odcienie od cienia do światła z jednego koloru bazowego."""
    return [ciemniej(kolor, 0.45), ciemniej(kolor, 0.7), tuple(kolor), jasniej(kolor, 1.2, 16)]


# rodzina: (typ sylwetki, kolor główny, kolor drugi, dodatki)
_RODZINY: list[tuple[str, tuple]] = [
    ("goblin", ("humanoid", (92, 150, 70), (110, 80, 50), {"uszy", "sztylet"}, 0.8)),
    ("szkielet", ("humanoid", (220, 214, 196), (70, 64, 60), {"czaszka", "miecz"}, 1.0)),
    ("ork", ("humanoid", (86, 128, 62), (96, 70, 52), {"kly", "topor"}, 1.2)),
    ("troll", ("troll", (104, 128, 98), (80, 70, 60), {"maczuga"}, 1.4)),
    ("wiedźma", ("humanoid", (190, 170, 150), (84, 50, 110), {"kapelusz", "laska"}, 1.0)),
    ("smok", ("smok", (170, 60, 44), (230, 170, 80), set(), 1.3)),
    ("hiena", ("bestia", (170, 140, 90), (90, 70, 50), {"cetki"}, 0.9)),
    ("wilk", ("bestia", (70, 72, 90), (40, 40, 56), set(), 1.0)),
    ("topielec", ("humanoid", (86, 126, 120), (50, 80, 70), {"wodorosty"}, 1.0)),
    ("harpii", ("ptak", (150, 110, 80), (200, 180, 150), set(), 1.0)),
    ("gryf", ("ptak", (200, 170, 90), (240, 236, 220), {"lwie"}, 1.4)),
    ("skorpion", ("skorpion", (150, 96, 50), (90, 56, 34), set(), 1.1)),
    ("strażnik ruin", ("humanoid", (130, 130, 124), (96, 96, 92), {"kamien", "miecz"}, 1.2)),
    ("licz", ("humanoid", (200, 200, 180), (60, 40, 90), {"czaszka", "kaptur", "laska"}, 1.2)),
    ("arcydemon", ("humanoid", (160, 50, 50), (60, 30, 30), {"rogi", "skrzydla", "miecz"}, 1.4)),
    ("strażniczka", ("humanoid", (220, 200, 160), (200, 190, 140), {"aureola", "miecz"}, 1.3)),
    ("otchłani", ("humanoid", (70, 50, 110), (30, 20, 50), {"rogi", "kaptur"}, 1.3)),
    ("herszt", ("humanoid", (214, 170, 130), (150, 40, 40), {"kaptur", "topor"}, 1.1)),
]
_DOMYSLNA = ("humanoid", (160, 130, 110), (90, 70, 60), {"miecz"}, 1.0)


def rodzina(nazwa: str) -> tuple:
    nazwa = nazwa.lower()
    for klucz, wpis in _RODZINY:
        if klucz in nazwa:
            return wpis
    return _DOMYSLNA


def _humanoid(p: pygame.Surface, cx: int, dol: int, sk: float, skora, ubior, dodatki: set) -> None:
    rs, ru = rampa(skora), rampa(ubior)
    h = lambda v: int(v * sk)  # noqa: E731
    # nogi
    for dx in (-h(4), h(2)):
        wypelnij(p, [(cx + dx, dol - h(12)), (cx + dx + h(3), dol - h(12)), (cx + dx + h(3), dol), (cx + dx, dol)],
                 ru, 0.4 if dx < 0 else 0.25, 3)
    # tułów
    tulow = kula(h(7), h(9), ru, 5, 0.2)
    p.blit(tulow, (cx - h(7), dol - h(12) - h(16)))
    if "wodorosty" in dodatki:
        for i in range(5):
            p.fill((50, 110, 60), (cx - h(6) + i * h(3), dol - h(20), 1, h(10)))
    # ramiona
    for strona in (-1, 1):
        x = cx + strona * h(8)
        wypelnij(p, [(x - 1, dol - h(26)), (x + 2, dol - h(26)), (x + 2, dol - h(14)), (x - 1, dol - h(14))],
                 rs if "czaszka" not in dodatki else rampa((200, 196, 180)), 0.6 if strona < 0 else 0.3, 7)
    # głowa
    glowa = kula(h(5), h(5), rs, 9, 0.1)
    gy = dol - h(12) - h(16) - h(9)
    p.blit(glowa, (cx - h(5), gy))
    oczy = (255, 60, 40) if {"czaszka", "rogi"} & dodatki else (20, 16, 20)
    p.fill(oczy, (cx - h(3), gy + h(4), max(1, h(1.5)), max(1, h(1.5))))
    p.fill(oczy, (cx + h(1), gy + h(4), max(1, h(1.5)), max(1, h(1.5))))
    if "uszy" in dodatki:
        for strona in (-1, 1):
            x = cx + strona * h(5)
            wypelnij(p, [(x, gy + h(4)), (x + strona * h(5), gy + h(1)), (x, gy + h(6))], rs, 0.6, 11)
    if "kly" in dodatki:
        p.fill((240, 236, 210), (cx - h(2), gy + h(7), 1, h(2)))
        p.fill((240, 236, 210), (cx + h(2), gy + h(7), 1, h(2)))
    if "rogi" in dodatki:
        for strona in (-1, 1):
            x = cx + strona * h(3)
            wypelnij(p, [(x, gy + h(2)), (x + strona * h(6), gy - h(6)), (x + strona * h(2), gy + h(1))],
                     rampa((60, 50, 48)), 0.7, 13)
    if "kapelusz" in dodatki:
        wypelnij(p, [(cx - h(8), gy + h(2)), (cx + h(8), gy + h(2)), (cx + h(2), gy - h(12))], ru, 0.5, 15)
    if "kaptur" in dodatki:
        wypelnij(p, [(cx - h(6), gy + h(8)), (cx - h(6), gy - h(1)), (cx, gy - h(4)), (cx + h(6), gy - h(1)),
                     (cx + h(6), gy + h(8)), (cx + h(4), gy + h(3)), (cx - h(4), gy + h(3))], ru, 0.35, 17)
    if "aureola" in dodatki:
        pygame.draw.ellipse(p, (250, 230, 150), (cx - h(7), gy - h(5), h(14), h(4)), 1)
    if "skrzydla" in dodatki:
        for strona in (-1, 1):
            x = cx + strona * h(7)
            wypelnij(p, [(x, dol - h(26)), (x + strona * h(16), dol - h(38)), (x + strona * h(14), dol - h(16))],
                     rampa((70, 30, 34)), 0.5, 19)
    # broń w prawej ręce
    rx = cx + h(9)
    if dodatki & {"miecz", "sztylet"}:
        dl = h(14) if "miecz" in dodatki else h(7)
        p.fill((200, 206, 216), (rx + 1, dol - h(16) - dl, max(1, h(1.5)), dl))
        p.fill((140, 110, 60), (rx - 1, dol - h(16), h(5), max(1, h(1.5))))
    elif "topor" in dodatki:
        p.fill((110, 80, 50), (rx + 1, dol - h(30), max(1, h(1.5)), h(18)))
        wypelnij(p, [(rx + 2, dol - h(30)), (rx + h(8), dol - h(32)), (rx + h(8), dol - h(24)), (rx + 2, dol - h(25))],
                 rampa((170, 176, 186)), 0.6, 21)
    elif "laska" in dodatki:
        p.fill((100, 70, 40), (rx + 1, dol - h(34), max(1, h(1.5)), h(34)))
        p.blit(kula(h(2) + 1, h(2) + 1, rampa((140, 200, 255)), 23, 0.0), (rx - h(1), dol - h(38)))
    elif "maczuga" in dodatki:
        p.fill((110, 80, 50), (rx, dol - h(28), h(2), h(16)))
        p.blit(kula(h(4), h(5), rampa((120, 90, 60)), 25, 0.3), (rx - h(3), dol - h(36)))
    if "kamien" in dodatki:
        for i in range(6):
            p.fill((90, 90, 86), (cx - h(5) + i * h(2), dol - h(22) + (i % 3) * h(3), 1, 1))


def _troll(p, cx, dol, sk, skora, ubior, dodatki):
    rs = rampa(skora)
    h = lambda v: int(v * sk)  # noqa: E731
    for dx in (-h(6), h(3)):
        p.blit(kula(h(3), h(6), rs, 30 + dx, 0.2), (cx + dx, dol - h(12)))
    p.blit(kula(h(11), h(12), rs, 31, 0.3), (cx - h(11), dol - h(34)))
    p.blit(kula(h(3), h(9), rs, 32, 0.2), (cx - h(14), dol - h(26)))
    p.blit(kula(h(3), h(9), rs, 33, 0.2), (cx + h(9), dol - h(28)))
    p.blit(kula(h(5), h(4), rs, 34, 0.2), (cx - h(3), dol - h(40)))
    p.fill((250, 220, 90), (cx - h(1), dol - h(38), max(1, h(1.2)), max(1, h(1.2))))
    p.fill((250, 220, 90), (cx + h(2), dol - h(38), max(1, h(1.2)), max(1, h(1.2))))
    p.fill((110, 80, 50), (cx + h(11), dol - h(40), h(2), h(16)))
    p.blit(kula(h(4), h(5), rampa((120, 90, 60)), 35, 0.3), (cx + h(8), dol - h(48)))


def _bestia(p, cx, dol, sk, kolor, drugi, dodatki):
    rk = rampa(kolor)
    h = lambda v: int(v * sk)  # noqa: E731
    for dx in (-h(10), -h(6), h(4), h(8)):
        p.fill(rk[1], (cx + dx, dol - h(9), max(2, h(2)), h(9)))
    p.blit(kula(h(13), h(7), rk, 40, 0.3), (cx - h(13), dol - h(20)))
    if "cetki" in dodatki:
        for i in range(7):
            p.fill(drugi, (cx - h(9) + i * h(3), dol - h(16) + (i % 2) * h(3), max(1, h(1.5)), max(1, h(1.5))))
    p.blit(kula(h(6), h(5), rk, 41, 0.2), (cx - h(20), dol - h(24)))
    wypelnij(p, [(cx - h(22), dol - h(18)), (cx - h(28), dol - h(16)), (cx - h(22), dol - h(14))], rk, 0.5, 42)
    for dx in (-h(18), -h(13)):
        wypelnij(p, [(cx + dx, dol - h(26)), (cx + dx + h(2), dol - h(32)), (cx + dx + h(4), dol - h(26))], rk, 0.4, 43)
    p.fill((255, 200, 60), (cx - h(19), dol - h(22), max(1, h(1.5)), max(1, h(1.5))))
    wypelnij(p, [(cx + h(12), dol - h(18)), (cx + h(22), dol - h(24)), (cx + h(13), dol - h(14))], rk, 0.3, 44)


def _smok(p, cx, dol, sk, kolor, drugi, dodatki):
    rk, rd = rampa(kolor), rampa(drugi)
    h = lambda v: int(v * sk)  # noqa: E731
    for strona, skrzydlo in ((1, 0.25), (-1, 0.55)):
        x = cx + strona * h(2)
        wypelnij(p, [(x, dol - h(26)), (x + strona * h(24), dol - h(46)), (x + strona * h(20), dol - h(24)),
                     (x + strona * h(12), dol - h(20))], rk, skrzydlo, 50)
    wypelnij(p, [(cx + h(10), dol - h(10)), (cx + h(28), dol - h(6)), (cx + h(30), dol - h(2)), (cx + h(8), dol - h(4))],
             rk, 0.35, 51)
    for dx in (-h(8), h(6)):
        p.fill(rk[0], (cx + dx, dol - h(10), h(3), h(10)))
    p.blit(kula(h(14), h(10), rk, 52, 0.25), (cx - h(14), dol - h(28)))
    p.blit(kula(h(8), h(5), rd, 53, 0.1), (cx - h(10), dol - h(20)))
    wypelnij(p, [(cx - h(8), dol - h(24)), (cx - h(14), dol - h(40)), (cx - h(10), dol - h(42)), (cx - h(4), dol - h(26))],
             rk, 0.55, 54)
    p.blit(kula(h(6), h(4), rk, 55, 0.2), (cx - h(20), dol - h(46)))
    p.fill((255, 220, 90), (cx - h(18), dol - h(45), max(1, h(1.5)), max(1, h(1.5))))
    for i in range(3):
        wypelnij(p, [(cx - h(14) + i * h(3), dol - h(48)), (cx - h(13) + i * h(3), dol - h(54)),
                     (cx - h(12) + i * h(3), dol - h(48))], rd, 0.7, 56 + i)


def _ptak(p, cx, dol, sk, kolor, drugi, dodatki):
    rk, rd = rampa(kolor), rampa(drugi)
    h = lambda v: int(v * sk)  # noqa: E731
    for strona, jasn in ((1, 0.3), (-1, 0.6)):
        wypelnij(p, [(cx, dol - h(26)), (cx + strona * h(22), dol - h(40)), (cx + strona * h(18), dol - h(20)),
                     (cx + strona * h(6), dol - h(18))], rk, jasn, 60)
    for dx in (-h(4), h(2)):
        p.fill((200, 170, 60), (cx + dx, dol - h(8), max(1, h(1.5)), h(8)))
    p.blit(kula(h(8), h(10), rd, 61, 0.2), (cx - h(8), dol - h(28)))
    p.blit(kula(h(5), h(5), rd if "lwie" in dodatki else rk, 62, 0.1), (cx - h(5), dol - h(38)))
    wypelnij(p, [(cx - h(5), dol - h(33)), (cx - h(11), dol - h(31)), (cx - h(5), dol - h(30))], rampa((230, 190, 60)), 0.6, 63)
    p.fill((20, 16, 20), (cx - h(2), dol - h(35), max(1, h(1.5)), max(1, h(1.5))))


def _skorpion(p, cx, dol, sk, kolor, drugi, dodatki):
    rk = rampa(kolor)
    h = lambda v: int(v * sk)  # noqa: E731
    for i in range(4):
        p.fill(rk[0], (cx - h(10) + i * h(6), dol - h(6), max(1, h(1.5)), h(6)))
    for i in range(4):
        p.blit(kula(h(6), h(4), rk, 70 + i, 0.2), (cx - h(12) + i * h(6), dol - h(12)))
    for i in range(5):
        kat = math.pi * (0.1 + i * 0.2)
        x = cx + h(14) + int(math.cos(kat) * h(10))
        y = dol - h(12) - int(math.sin(kat) * h(16))
        p.blit(kula(h(3), h(3), rk, 75 + i, 0.2), (x - h(3), y - h(3)))
    p.fill((250, 240, 200), (cx + h(4), dol - h(30), max(1, h(1.5)), h(4)))
    for strona in (-1, 1):
        y = dol - h(10) + strona * h(3)
        p.blit(kula(h(4), h(3), rk, 80, 0.2), (cx - h(22), y - h(3)))


_RYSOWNICY = {"humanoid": _humanoid, "troll": _troll, "bestia": _bestia, "smok": _smok,
              "ptak": _ptak, "skorpion": _skorpion}


def sprite_wroga(nazwa: str, boss: bool = False) -> pygame.Surface:
    typ, kolor, drugi, dodatki, rozmiar = rodzina(nazwa)
    sk = rozmiar * (1.35 if boss else 1.0)
    p = pygame.Surface((int(80 * sk), int(80 * sk)), pygame.SRCALPHA)
    cx, dol = p.get_width() // 2, p.get_height() - 2
    _RYSOWNICY[typ](p, cx, dol, sk, kolor, drugi, dodatki)
    przyciety = p.subsurface(p.get_bounding_rect(min_alpha=1)).copy()
    return obrys(przyciety)
