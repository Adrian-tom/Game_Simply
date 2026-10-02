"""Portrety twarzy do widoku rozmowy — głowa i ramiona w skali portretowej.

Dlaczego osobny moduł, a nie ``_humanoid`` z ``potwory``: tamta figurka to cała
sylwetka rysowana na 5–6 pikseli głowy. Po przeskalowaniu do kadru rozmowy
twarz nie istnieje — zostaje kolorowy korpus. Tutaj rysujemy od razu portret:
głowa zajmuje większość kadru, więc oko, brew i usta mają po kilka pikseli
i da się po nich poznać, kto mówi i w jakim jest humorze.

Rysunek powstaje w małej skali (ok. 46×54 px) i jest powiększany całymi
pikselami — tak jak reszta grafiki w grze. Każdy rozmówca ma stałe rysy:
losowanie idzie z ziarna zrobionego z jego imienia, więc kowal zawsze wygląda
tak samo, a nowy NPC dostaje twarz bez dopisywania czegokolwiek.
"""
from __future__ import annotations

import math
import random

import pygame

from grafika.piksele import ciemniej, jasniej

# Płótno portretu przed powiększeniem. Głowa ma ok. 26 px — dość, by zmieścić
# oko (3 px), brew, nos i usta i nie zlać ich w jedną plamę.
SZER_P, WYS_P = 46, 60

_KONTUR = (26, 20, 24)
_BIALKO = (226, 222, 210)

# Odcienie skóry — od najjaśniejszej do najciemniejszej, wybierane ziarnem.
_SKORY = [
    (236, 198, 168), (222, 180, 146), (204, 158, 124),
    (176, 130, 98), (140, 100, 74), (104, 74, 56),
]

_WLOSY = [
    (40, 32, 28), (62, 44, 32), (96, 66, 38), (134, 96, 46),
    (176, 148, 92), (196, 192, 186), (150, 60, 36),
]


class Rysy:
    """Stałe cechy twarzy jednego rozmówcy."""

    __slots__ = ("skora", "wlosy", "fryzura", "zarost", "brwi", "nos", "usta",
                 "oczy", "zmarszczki", "blizna", "ubior", "naglowie")

    def __init__(self, **kw):
        for k, v in kw.items():
            setattr(self, k, v)


def rysy_z_imienia(mowi: str, ubior=(90, 70, 54), naglowie: str | None = None,
                   skora=None, kobieta: bool = False) -> Rysy:
    """
    Deterministyczne rysy z imienia. To samo imię = ta sama twarz, zawsze,
    bez trzymania czegokolwiek w pamięci ani w zapisie gry.
    """
    r = random.Random(mowi.lower())
    fryzury = (["dlugie", "dlugie", "zaczesane", "krotkie"] if kobieta
               else ["krotkie", "krotkie", "dlugie", "lysy", "zaczesane"])
    zarosty = [None] if kobieta else [None, None, "wasy", "broda", "broda_dluga", "szczecina"]
    return Rysy(
        skora=skora or _SKORY[r.randrange(len(_SKORY))],
        wlosy=_WLOSY[r.randrange(len(_WLOSY))],
        fryzura=r.choice(fryzury),
        zarost=r.choice(zarosty),
        brwi=r.choice(["proste", "proste", "uniesione", "zmarszczone", "grube"]),
        nos=r.choice(["prosty", "prosty", "garbaty", "zadarty", "szeroki"]),
        usta=r.choice(["waskie", "pelne", "waskie"]),
        oczy=r.choice([(70, 92, 60), (82, 70, 52), (60, 78, 104), (54, 46, 42)]),
        zmarszczki=r.random() < 0.35,
        blizna=r.random() < 0.18,
        ubior=ubior,
        naglowie=naglowie,
    )


# --------------------------------------------------------------------------- #
#  Rysowanie
# --------------------------------------------------------------------------- #

def _owal(pow_, cx, cy, rx, ry, kolor, cien):
    """Głowa: elipsa z cieniem po stronie odwróconej od światła (światło z lewej góry)."""
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            nx, ny = (x - cx) / rx, (y - cy) / ry
            if nx * nx + ny * ny > 1.0:
                continue
            # Żuchwa węższa niż czaszka — bez tego głowa jest jajkiem.
            if ny > 0.25:
                zwezenie = 1.0 - (ny - 0.25) * 0.45
                if abs(nx) > zwezenie:
                    continue
            pow_.set_at((x, y), cien if (nx + ny) > 0.45 else kolor)


def _oko(pow_, x, y, rysy, otwarte: float):
    """Oko: białko, tęczówka, źrenica. ``otwarte`` 0..1 steruje mruganiem."""
    if otwarte < 0.25:
        pygame.draw.line(pow_, ciemniej(rysy.skora, 0.55), (x, y + 1), (x + 3, y + 1))
        return
    pow_.fill(_BIALKO, (x, y, 4, 2))
    pow_.fill(rysy.oczy, (x + 1, y, 2, 2))
    pow_.set_at((x + 1, y + 1), _KONTUR)          # źrenica
    pow_.set_at((x + 2, y), jasniej(_BIALKO, 1.1, 20))   # refleks
    # Powieka górna — bez niej spojrzenie jest wytrzeszczone.
    pygame.draw.line(pow_, ciemniej(rysy.skora, 0.5), (x, y - 1), (x + 3, y - 1))


def _brwi(pow_, lx, px, y, rysy):
    kolor = ciemniej(rysy.wlosy, 0.85)
    gr = 2 if rysy.brwi == "grube" else 1
    for strona, x in ((-1, lx), (1, px)):
        if rysy.brwi == "uniesione":
            pow_.fill(kolor, (x, y - 1, 4, gr))
            pow_.fill(kolor, (x + (0 if strona < 0 else 3), y, 1, gr))
        elif rysy.brwi == "zmarszczone":
            pow_.fill(kolor, (x, y, 4, gr))
            pow_.fill(kolor, (x + (3 if strona < 0 else 0), y - 1, 1, gr))
        else:
            pow_.fill(kolor, (x, y, 4, gr))


def _nos(pow_, cx, y, rysy):
    cien = ciemniej(rysy.skora, 0.72)
    if rysy.nos == "garbaty":
        pygame.draw.line(pow_, cien, (cx, y), (cx - 1, y + 4))
        pow_.set_at((cx - 1, y + 2), ciemniej(rysy.skora, 0.62))
    elif rysy.nos == "zadarty":
        pygame.draw.line(pow_, cien, (cx, y + 1), (cx, y + 3))
        pow_.set_at((cx + 1, y + 3), cien)
    elif rysy.nos == "szeroki":
        pow_.fill(cien, (cx - 1, y + 3, 3, 1))
        pygame.draw.line(pow_, cien, (cx, y), (cx, y + 3))
    else:
        pygame.draw.line(pow_, cien, (cx, y), (cx, y + 3))
    pow_.set_at((cx - 1, y + 4), ciemniej(rysy.skora, 0.6))   # nozdrze
    pow_.set_at((cx + 1, y + 4), ciemniej(rysy.skora, 0.6))


def _usta(pow_, cx, y, rysy, nastroj: str):
    kolor = ciemniej((190, 110, 100), 0.8)
    sz = 5 if rysy.usta == "pelne" else 4
    x = cx - sz // 2

    if nastroj == "gniew":
        pow_.fill(kolor, (x, y, sz, 1))
        pow_.set_at((x, y - 1), kolor)
        pow_.set_at((x + sz - 1, y - 1), kolor)
    elif nastroj == "usmiech":
        pow_.fill(kolor, (x + 1, y, sz - 2, 1))
        pow_.set_at((x, y - 1), kolor)
        pow_.set_at((x + sz - 1, y - 1), kolor)
    elif nastroj == "smutek":
        pow_.fill(kolor, (x + 1, y, sz - 2, 1))
        pow_.set_at((x, y + 1), kolor)
        pow_.set_at((x + sz - 1, y + 1), kolor)
    else:
        pow_.fill(kolor, (x, y, sz, 1))

    if rysy.usta == "pelne":
        pow_.fill(ciemniej(kolor, 0.8), (x + 1, y + 1, sz - 2, 1))


def _wlosy(pow_, cx, gora, rx, rysy):
    if rysy.fryzura == "lysy":
        return
    k, c = rysy.wlosy, ciemniej(rysy.wlosy, 0.65)

    # Czapka włosów na czaszce.
    for y in range(gora - 1, gora + 9):
        for x in range(cx - rx - 1, cx + rx + 2):
            nx, ny = (x - cx) / (rx + 1), (y - (gora + rx)) / (rx + 1)
            if nx * nx + ny * ny > 1.0 or y > gora + 7:
                continue
            pow_.set_at((x, y), c if (nx + ny) > 0.4 else k)

    if rysy.fryzura == "dlugie":
        for strona in (-1, 1):
            x = cx + strona * (rx - 1)
            pygame.draw.line(pow_, k, (x, gora + 5), (x + strona, gora + 20))
            pygame.draw.line(pow_, c, (x + strona * 2, gora + 6), (x + strona * 2, gora + 17))
    elif rysy.fryzura == "zaczesane":
        pow_.fill(c, (cx - rx, gora + 1, rx * 2, 1))


def _zarost(pow_, cx, y_usta, rx, rysy):
    if not rysy.zarost:
        return
    k, c = rysy.wlosy, ciemniej(rysy.wlosy, 0.7)

    if rysy.zarost in ("wasy", "broda", "broda_dluga"):
        pow_.fill(k, (cx - 3, y_usta - 2, 7, 1))        # wąsy nad ustami

    if rysy.zarost == "szczecina":
        # Szum zamiast (x+y) % 2: regularna krata czytała się jak wzór tkaniny,
        # nie jak zarost. Ziarno z koloru włosów — ten sam NPC ma ten sam zarost.
        r = random.Random(sum(rysy.wlosy) * 7 + cx)
        for y in range(y_usta - 1, y_usta + 7):
            for x in range(cx - rx + 2, cx + rx - 1):
                if (x - cx) ** 2 / 36 + (y - y_usta - 3) ** 2 / 25 > 1:
                    continue
                if r.random() < 0.55:
                    pow_.set_at((x, y), c if r.random() < 0.7 else k)

    if rysy.zarost in ("broda", "broda_dluga"):
        dol = y_usta + (7 if rysy.zarost == "broda" else 13)
        for y in range(y_usta + 1, dol):
            szer = max(1, int((rx - 2) * (1.0 - (y - y_usta) / (dol - y_usta) * 0.55)))
            pow_.fill(k if y < dol - 2 else c, (cx - szer, y, szer * 2, 1))


def _naglowie(pow_, cx, gora, rx, rysy):
    n = rysy.naglowie
    if not n:
        return
    u, c = rysy.ubior, ciemniej(rysy.ubior, 0.65)

    if n == "kaptur":
        for y in range(gora - 4, gora + 30):
            for x in range(cx - rx - 5, cx + rx + 6):
                nx, ny = (x - cx) / (rx + 5), (y - (gora + rx + 2)) / (rx + 8)
                if nx * nx + ny * ny > 1.0:
                    continue
                # Otwór kaptura: twarz zostaje widoczna, reszta zasłonięta.
                tx, ty = (x - cx) / (rx + 1), (y - (gora + rx + 3)) / (rx + 3)
                if tx * tx + ty * ty < 1.0:
                    continue
                pow_.set_at((x, y), c if (nx + ny) > 0.35 else u)
    elif n == "kapelusz":
        pow_.fill(c, (cx - rx - 7, gora + 1, (rx + 7) * 2, 2))        # rondo
        pow_.fill(u, (cx - rx + 2, gora - 7, (rx - 2) * 2, 9))        # główka
        pow_.fill(ciemniej(u, 0.5), (cx - rx + 2, gora - 1, (rx - 2) * 2, 2))   # opaska
    elif n == "helm":
        for y in range(gora - 3, gora + 10):
            for x in range(cx - rx - 2, cx + rx + 3):
                nx, ny = (x - cx) / (rx + 2), (y - (gora + rx)) / (rx + 2)
                if nx * nx + ny * ny > 1.0 or y > gora + 9:
                    continue
                pow_.set_at((x, y), (120, 126, 136) if (nx + ny) < 0.35 else (78, 84, 94))
        pow_.fill((150, 156, 166), (cx - 1, gora + 4, 2, 14))         # nosal
    elif n == "kaptur_kosc":
        for y in range(gora - 3, gora + 12):
            for x in range(cx - rx - 2, cx + rx + 3):
                nx, ny = (x - cx) / (rx + 2), (y - (gora + rx)) / (rx + 2)
                if nx * nx + ny * ny > 1.0 or y > gora + 8:
                    continue
                pow_.set_at((x, y), (214, 208, 190) if (nx + ny) < 0.35 else (168, 160, 144))
    elif n == "wieniec":
        for i in range(9):
            a = math.pi * (0.12 + 0.76 * i / 8)
            x = int(cx - math.cos(a) * (rx + 1))
            y = int(gora + rx - math.sin(a) * (rx + 3))
            pow_.fill((74, 104, 58), (x - 1, y, 3, 2))


def _ramiona(pow_, cx, y, rysy):
    """Barki i szyja — portret bez nich wisi w powietrzu jak odcięta głowa."""
    u, c = rysy.ubior, ciemniej(rysy.ubior, 0.62)
    pow_.fill(ciemniej(rysy.skora, 0.78), (cx - 4, y - 4, 8, 5))      # szyja
    pow_.fill(ciemniej(rysy.skora, 0.6), (cx - 4, y - 4, 8, 1))       # cień żuchwy

    for i, yy in enumerate(range(y, WYS_P)):
        szer = min(SZER_P // 2 - 1, 7 + i * 2)
        pow_.fill(u, (cx - szer, yy, szer * 2, 1))
        pow_.fill(c, (cx - szer, yy, 2, 1))
        pow_.fill(c, (cx + szer - 2, yy, 2, 1))

    pow_.fill(jasniej(u, 1.15, 10), (cx - 1, y, 2, WYS_P - y))        # zapięcie


def _kontur(pow_: pygame.Surface) -> pygame.Surface:
    """Ciemna obwódka wokół sylwetki — oddziela portret od malarskiego tła."""
    w, h = pow_.get_size()
    wynik = pygame.Surface((w, h), pygame.SRCALPHA)
    wynik.blit(pow_, (0, 0))
    for y in range(h):
        for x in range(w):
            if pow_.get_at((x, y)).a:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                sx, sy = x + dx, y + dy
                if 0 <= sx < w and 0 <= sy < h and pow_.get_at((sx, sy)).a:
                    wynik.set_at((x, y), _KONTUR)
                    break
    return wynik


def portret(rysy: Rysy, nastroj: str = "spokoj", otwarte: float = 1.0) -> pygame.Surface:
    """Rysuje głowę i ramiona na płótnie SZER_P×WYS_P (bez powiększenia)."""
    pow_ = pygame.Surface((SZER_P, WYS_P), pygame.SRCALPHA)

    cx = SZER_P // 2
    rx, ry = 11, 13
    # cy = 24, nie 20: rondo kapelusza i czubek kaptura sięgają 8 px
    # ponad czaszkę i przy 20 wychodziły poza płótno — głowy były ścięte.
    cy = 24
    gora = cy - ry

    _ramiona(pow_, cx, cy + ry + 2, rysy)
    _owal(pow_, cx, cy, rx, ry, rysy.skora, ciemniej(rysy.skora, 0.76))

    # Uszy — dwa piksele, ale bez nich głowa wygląda jak jajko.
    for strona in (-1, 1):
        pow_.fill(ciemniej(rysy.skora, 0.85), (cx + strona * rx - (1 if strona > 0 else 0), cy - 1, 1, 4))

    y_oczu = cy - 2
    _brwi(pow_, cx - 7, cx + 4, y_oczu - 3, rysy)
    _oko(pow_, cx - 7, y_oczu, rysy, otwarte)
    _oko(pow_, cx + 4, y_oczu, rysy, otwarte)
    _nos(pow_, cx, y_oczu + 2, rysy)
    _usta(pow_, cx, y_oczu + 9, rysy, nastroj)

    if rysy.zmarszczki:
        c = ciemniej(rysy.skora, 0.8)
        pow_.fill(c, (cx - 6, gora + 5, 12, 1))
        pow_.set_at((cx - 8, y_oczu + 4), c)
        pow_.set_at((cx + 8, y_oczu + 4), c)

    if rysy.blizna:
        pygame.draw.line(pow_, ciemniej(rysy.skora, 0.62),
                         (cx + 5, cy - 9), (cx + 8, cy + 1))

    _zarost(pow_, cx, y_oczu + 9, rx, rysy)
    _wlosy(pow_, cx, gora, rx, rysy)
    _naglowie(pow_, cx, gora, rx, rysy)

    return _kontur(pow_)


def mruganie(t: float, ziarno: float = 0.0) -> float:
    """
    0..1: ile oko jest otwarte. Mrugnięcie co ~4 s, krótkie.

    Przesunięcie fazy ziarnem sprawia, że dwie postacie obok siebie nie mrugają
    jednocześnie — zsynchronizowane mruganie natychmiast czyta się jak animacja
    sterowana zegarem, a nie jak żywa twarz.
    """
    faza = (t * 0.25 + ziarno) % 1.0
    if faza > 0.97:
        return 0.0
    if faza > 0.955:
        return 0.4
    return 1.0
