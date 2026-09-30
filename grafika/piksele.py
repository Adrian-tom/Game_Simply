"""Prymitywy pixelartu liczone w kodzie — bez plików PNG.

Wrażenie bryły daje kilka reguł naraz: jedno źródło światła (z lewej góry),
cieniowanie po normalnej, rampy 4 kolorów z ditheringiem Bayera zamiast
gładkich gradientów i ciemny kontur 1 px wokół każdej bryły.
"""
from __future__ import annotations

import math

import pygame

SWIATLO = (-0.55, -0.65, 0.52)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
KONTUR = (18, 16, 24)

Kolor = tuple[int, int, int]


def szum(x: int, y: int, s: int = 0) -> float:
    """Powtarzalny szum 0..1 — ten sam piksel zawsze dostaje tę samą wartość."""
    n = (x * 374761393 + y * 668265263 + s * 982451653) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0


def szum_gladki(x: float, y: float, skala: float, s: int = 0) -> float:
    """Szum wartości z interpolacją — plamy zamiast pojedynczych pikseli."""
    gx, gy = x / skala, y / skala
    x0, y0 = int(math.floor(gx)), int(math.floor(gy))
    fx, fy = gx - x0, gy - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = szum(x0, y0, s), szum(x0 + 1, y0, s)
    c, d = szum(x0, y0 + 1, s), szum(x0 + 1, y0 + 1, s)
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy


def z_rampy(rampa: list[Kolor], v: float, x: int, y: int) -> Kolor:
    """Wartość 0..1 → kolor z rampy, z ditheringiem między sąsiednimi stopniami."""
    v = min(max(v, 0.0), 1.0) * (len(rampa) - 1)
    i = int(v)
    if v - i > BAYER[y % 4][x % 4] / 16 and i < len(rampa) - 1:
        i += 1
    return rampa[i]


def ciemniej(c, f: float = 0.6) -> Kolor:
    return tuple(int(k * f) for k in c[:3])


def jasniej(c, f: float = 1.18, plus: int = 8) -> Kolor:
    return tuple(min(255, int(k * f) + plus) for k in c[:3])


def kula(rx: int, ry: int, rampa: list[Kolor], s: int = 0, grudki: float = 0.18) -> pygame.Surface:
    """Elipsa cieniowana jak bryła (normalna · światło). Korony drzew, skały, krzaki."""
    pow_ = pygame.Surface((2 * rx + 1, 2 * ry + 1), pygame.SRCALPHA)
    lx, ly, lz = SWIATLO
    for y in range(2 * ry + 1):
        for x in range(2 * rx + 1):
            nx, ny = (x - rx) / (rx + 0.5), (y - ry) / (ry + 0.5)
            r2 = nx * nx + ny * ny
            if r2 > 1:
                continue
            nz = math.sqrt(1 - r2)
            d = max(0.0, nx * lx + ny * ly + nz * lz)
            d += (szum_gladki(x, y, 2.2, s) - 0.5) * grudki * 2
            kolor = z_rampy(rampa, d * 1.05, x, y)
            if r2 > 0.78 and (nx + ny) > 0.2:
                kolor = ciemniej(rampa[0], 0.7)  # cień własny po stronie odwróconej od światła
            pow_.set_at((x, y), kolor)
    return pow_


def wypelnij(pow_: pygame.Surface, punkty, rampa, baza: float, s: int,
             wzor: str = "", x0: int = 0, y0: int = 0) -> None:
    """Wielokąt z fakturą (deski, cegła, dachówka) liczoną per piksel."""
    maska = pygame.Surface(pow_.get_size(), pygame.SRCALPHA)
    pygame.draw.polygon(maska, (255, 255, 255, 255), punkty)
    xs, ys = [p[0] for p in punkty], [p[1] for p in punkty]
    for y in range(max(0, min(ys)), min(pow_.get_height(), max(ys) + 1)):
        for x in range(max(0, min(xs)), min(pow_.get_width(), max(xs) + 1)):
            if maska.get_at((x, y)).a == 0:
                continue
            v = baza + (szum(x, y, s) - 0.5) * 0.25
            lx, ly = x - x0, y - y0
            if wzor == "deski" and lx % 3 == 0:
                v -= 0.3
            elif wzor == "cegla" and (ly % 3 == 0 or (lx + (ly // 3) * 2) % 5 == 0):
                v -= 0.3
            elif wzor == "dach" and ly % 2 == 0:
                v -= 0.25 if (lx + ly) % 4 else 0.45
            pow_.set_at((x, y), z_rampy(rampa, v, x, y))


def domek(pow_: pygame.Surface, cx: int, cy: int, a: int, h: int, sciana, dach, s: int,
          wzor: str = "deski", okno: Kolor | None = None, dach_h: int | None = None) -> None:
    """Prostopadłościan izometryczny z dachem dwuspadowym. (cx, cy) = środek podstawy."""
    b = a // 2
    L, F, R, B = (cx - a, cy), (cx, cy + b), (cx + a, cy), (cx, cy - b)

    def up(p, d=h):
        return (p[0], p[1] - d)

    wypelnij(pow_, [L, F, up(F), up(L)], sciana, 0.62, s, wzor, cx - a, cy)
    wypelnij(pow_, [F, R, up(R), up(F)], sciana, 0.28, s + 1, wzor, cx, cy)
    if okno:
        pow_.fill(okno, (cx - a // 2 - 1, cy - h // 2 + b // 2 - 1, 2, 3))
    dh = dach_h if dach_h is not None else a
    P1 = ((L[0] + B[0]) // 2, (L[1] + B[1]) // 2 - h - dh)
    P2 = ((F[0] + R[0]) // 2, (F[1] + R[1]) // 2 - h - dh)
    wypelnij(pow_, [up(B), up(R), P2, P1], dach, 0.2, s + 2, "dach", cx, cy)
    wypelnij(pow_, [up(F), up(R), P2], sciana, 0.2, s + 3, wzor, cx, cy)
    wypelnij(pow_, [up(L), up(F), P2, P1], dach, 0.72, s + 4, "dach", cx, cy)
    pygame.draw.line(pow_, ciemniej(dach[0], 0.6), P1, P2)


def obrys(pow_: pygame.Surface, kolor: Kolor = KONTUR) -> pygame.Surface:
    """Ciemny kontur 1 px — oddziela bryłę od tła."""
    wynik = pygame.Surface(pow_.get_size(), pygame.SRCALPHA)
    w, h = pow_.get_size()
    for y in range(h):
        for x in range(w):
            if pow_.get_at((x, y)).a:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if 0 <= x + dx < w and 0 <= y + dy < h and pow_.get_at((x + dx, y + dy)).a:
                    wynik.set_at((x, y), kolor)
                    break
    wynik.blit(pow_, (0, 0))
    return wynik


def poswiata(r: int, kolor: Kolor, stopnie: int = 5) -> pygame.Surface:
    """Światło punktowe w pasmach z ditheringiem — pikselowe, nie gładkie."""
    pow_ = pygame.Surface((2 * r, 2 * r))
    for y in range(2 * r):
        for x in range(2 * r):
            d = math.hypot(x - r, (y - r) * 1.6) / r
            v = max(0.0, 1 - d) ** 1.4
            q = v * stopnie
            i = int(q) + (1 if q - int(q) > BAYER[y % 4][x % 4] / 16 else 0)
            f = min(i, stopnie) / stopnie
            pow_.set_at((x, y), tuple(int(k * f) for k in kolor))
    return pow_


def sprite_z_tekstu(wiersze: list[str], paleta: dict[str, Kolor]) -> pygame.Surface:
    """Sprite zapisany jako linijki znaków: każdy znak = kolor z palety, reszta przezroczysta."""
    pow_ = pygame.Surface((len(wiersze[0]), len(wiersze)), pygame.SRCALPHA)
    for y, w in enumerate(wiersze):
        for x, ch in enumerate(w):
            if ch in paleta:
                pow_.set_at((x, y), paleta[ch])
    return pow_


def gradient_nieba(w: int, h: int, gora: Kolor, dol: Kolor, pasma: int = 12) -> pygame.Surface:
    pow_ = pygame.Surface((w, h))
    for y in range(h):
        for x in range(w):
            f = y / h + (BAYER[y % 4][x % 4] / 16 - 0.5) * 0.08
            f = min(max(round(f * pasma) / pasma, 0.0), 1.0)
            pow_.set_at((x, y), tuple(int(a + (b - a) * f) for a, b in zip(gora, dol)))
    return pow_


def winieta(w: int, h: int) -> pygame.Surface:
    """Maska do mnożenia: ciemniejsze rogi, drobne pasma z ditheringiem."""
    pow_ = pygame.Surface((w, h))
    for y in range(h):
        for x in range(w):
            d = math.hypot((x - w / 2) / (w / 2), (y - h / 2) / (h / 2))
            v = 1 - max(0.0, d - 0.7) * 0.8
            v = round((v + (BAYER[y % 4][x % 4] / 16 - 0.5) / 24) * 24) / 24
            pow_.set_at((x, y), (int(255 * min(v, 1.0)),) * 3)
    return pow_
