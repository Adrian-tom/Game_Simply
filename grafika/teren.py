"""Kafle izometryczne, dekoracje biomów i budynki punktów na mapie."""
from __future__ import annotations

import math

import pygame

from grafika.piksele import (
    BAYER,
    ciemniej,
    domek,
    jasniej,
    kula,
    obrys,
    sprite_z_tekstu,
    szum,
    szum_gladki,
    wypelnij,
    z_rampy,
)

TW, TH = 32, 16  # romb kafla

RAMPY = {
    "równiny": [(46, 78, 42), (70, 112, 50), (104, 148, 60), (146, 180, 78)],
    "las": [(26, 50, 36), (38, 72, 44), (56, 98, 52), (82, 128, 62)],
    "bagna": [(30, 48, 50), (42, 66, 62), (58, 88, 74), (84, 114, 86)],
    "wzgórza": [(66, 76, 60), (94, 106, 76), (124, 136, 92), (160, 166, 116)],
    "kanion": [(112, 58, 40), (152, 86, 50), (190, 120, 70), (222, 164, 102)],
    "ruiny": [(66, 64, 58), (92, 88, 78), (120, 114, 100), (152, 146, 126)],
}
WYSOKOSC = {"równiny": 4, "las": 5, "bagna": 1, "wzgórza": 11, "kanion": 8, "ruiny": 4}
ZIEMIA = [(48, 34, 28), (74, 52, 36), (100, 72, 46), (124, 92, 58)]
KAMIEN = [(52, 54, 60), (80, 82, 88), (110, 112, 116), (146, 146, 146)]
CZERWIEN = [(90, 44, 34), (128, 64, 42), (164, 92, 56), (196, 128, 80)]
WODA = [(22, 44, 58), (32, 66, 82), (52, 96, 110), (120, 170, 176)]


def rampa_biomu(biom: str) -> list:
    return RAMPY.get(biom, RAMPY["równiny"])


def wysokosc_pola(biom: str, x: int, y: int, s: int) -> int:
    return WYSOKOSC.get(biom, 4) + int(szum(x, y, s + 99) * 3)


def w_rombie(px: int, py: int) -> bool:
    return abs(px + 0.5 - TW / 2) / (TW / 2) + abs(py + 0.5 - TH / 2) / (TH / 2) <= 1.0


def kafel(biom: str, h: int, s: int, klatka: int = 0) -> pygame.Surface:
    """Blok terenu: wierzch z fakturą biomu + boki w warstwach skały."""
    grubosc = h + 6
    pow_ = pygame.Surface((TW, TH + grubosc), pygame.SRCALPHA)
    rampa = rampa_biomu(biom)
    boki = {"wzgórza": KAMIEN, "kanion": CZERWIEN, "ruiny": KAMIEN}.get(biom, ZIEMIA)
    for px in range(TW):
        gora = 8 + px // 2 if px < 16 else 16 - (px - 16) // 2
        for d in range(grubosc):
            y = gora + d
            if y >= pow_.get_height():
                break
            baza = 0.62 if px < 16 else 0.3  # lewy bok w świetle, prawy w cieniu
            warstwa = szum_gladki(px * 0.2, y, 3.0, s + 7)
            v = baza + (warstwa - 0.5) * 0.35 + (szum(px, y, s) - 0.5) * 0.15
            if (y + (px // 7)) % 5 == 0:
                v -= 0.2
            kolor = z_rampy(boki, v, px, y)
            if d < 2 and biom in ("równiny", "las", "wzgórza"):
                kolor = rampa[1 if px < 16 else 0]  # darń zwisająca nad krawędzią
            pow_.set_at((px, y), kolor)
    for py in range(TH):
        for px in range(TW):
            if not w_rombie(px, py):
                continue
            gx, gy = px + s * 31, py * 2 + s * 17
            v = 0.5 + (szum_gladki(gx, gy, 4.0, s) - 0.5) * 0.7 + (szum(px, py, s) - 0.5) * 0.2
            kolor = z_rampy(rampa, v, px, py)
            if biom == "bagna" and szum_gladki(gx, gy, 5.0, s + 3) < 0.55:
                fala = (px + py * 2 + klatka * 3) % 11 == 0 and szum(px, py, s + klatka) > 0.6
                kolor = WODA[3] if fala else z_rampy(WODA, 0.35 + (szum(px, py, s + 9) - 0.5) * 0.3, px, py)
            elif biom == "równiny" and szum(px, py, s + 5) > 0.975:
                kolor = (226, 208, 96) if szum(px, py, s + 6) > 0.5 else (214, 120, 150)
            if not w_rombie(px, py - 1) or (px < 16 and not w_rombie(px - 1, py)):
                kolor = jasniej(kolor)
            elif not w_rombie(px, py + 1):
                kolor = ciemniej(kolor, 0.8)
            pow_.set_at((px, py), kolor)
    return pow_


def zamglij(kaf: pygame.Surface, x: int, y: int, noc: bool) -> pygame.Surface:
    """Nieodkryte pole: przyciemniony blok z rzadką mgiełką na wierzchu."""
    m = kaf.copy()
    m.fill((70, 66, 96, 255) if noc else (128, 136, 158, 255), special_flags=pygame.BLEND_RGBA_MULT)
    kol = (52, 50, 74) if noc else (150, 158, 180)
    for py in range(TH):
        for px in range(TW):
            if w_rombie(px, py) and szum_gladki(px + x * 32, py * 2 + y * 16, 6, 5) > 0.6 \
                    and BAYER[py % 4][px % 4] < 3:
                m.set_at((px, py), kol)
    return m


def obwodka(kolor=(236, 200, 110)) -> pygame.Surface:
    """Romb podświetlający pole gracza."""
    pow_ = pygame.Surface((TW, TH), pygame.SRCALPHA)
    for py in range(TH):
        for px in range(TW):
            if w_rombie(px, py) and not (w_rombie(px - 1, py) and w_rombie(px + 1, py)
                                         and w_rombie(px, py - 1) and w_rombie(px, py + 1)):
                if (px + py) % 2 == 0:
                    pow_.set_at((px, py), kolor)
    return pow_


# ------------------------------------------------------------------ #
#  Roślinność i skały                                                  #
# ------------------------------------------------------------------ #

def sosna(wys: int, s: int) -> pygame.Surface:
    szer = wys // 2 + 3
    pow_ = pygame.Surface((szer * 2 + 1, wys + 3), pygame.SRCALPHA)
    cx = szer
    pow_.fill((70, 46, 32), (cx - 1, wys - 3, 2, 5))
    pow_.set_at((cx, wys - 2), (48, 30, 24))
    rampa = RAMPY["las"]
    for i in range(3):
        top = int(i * wys * 0.26)
        dol = top + int(wys * 0.46)
        for y in range(top, min(dol, wys - 1)):
            t = (y - top) / max(1, dol - top)
            pol = int(1 + t * (szer - 1) * (0.7 + 0.3 * (i + 1) / 3))
            for x in range(cx - pol, cx + pol + 1):
                nx = (x - cx) / (pol + 0.5)
                v = 0.62 - 0.55 * nx - t * 0.25 + (szum(x, y, s) - 0.5) * 0.35
                pow_.set_at((x, y), z_rampy(rampa, v, x, y))
    return obrys(pow_)


def drzewo_lisciaste(s: int) -> pygame.Surface:
    pow_ = pygame.Surface((17, 20), pygame.SRCALPHA)
    pow_.fill((84, 58, 38), (7, 11, 2, 9))
    pow_.fill((60, 40, 28), (8, 11, 1, 9))
    rampa = [(38, 70, 40), (62, 104, 48), (98, 142, 56), (150, 184, 80)]
    pow_.blit(kula(7, 6, rampa, s, 0.3), (1, 0))
    return obrys(pow_)


def skala(rx: int, ry: int, s: int, rampa=KAMIEN) -> pygame.Surface:
    return obrys(kula(rx, ry, rampa, s, 0.25))


def trzcina(s: int) -> pygame.Surface:
    pow_ = pygame.Surface((7, 8), pygame.SRCALPHA)
    for i in range(4):
        x = int(szum(i, 0, s) * 6)
        h = 3 + int(szum(i, 1, s) * 5)
        for y in range(8 - h, 8):
            pow_.set_at((x, y), (96, 120, 58) if y > 8 - h + 1 else (132, 96, 52))
    return pow_


def kolumna(s: int, wys: int) -> pygame.Surface:
    pow_ = pygame.Surface((7, wys + 2), pygame.SRCALPHA)
    for y in range(1, wys + 2):
        for x in range(1, 6):
            v = 0.9 - (x - 1) * 0.2 + (szum(x, y, s) - 0.5) * 0.3
            pow_.set_at((x, y), z_rampy(KAMIEN, v, x, y))
    pow_.fill(KAMIEN[3], (0, 1, 7, 1))
    for x in range(1, 6):  # złamany szczyt
        if szum(x, 0, s) > 0.5:
            pow_.set_at((x, 0), KAMIEN[2])
    return obrys(pow_)


# ------------------------------------------------------------------ #
#  Budynki i punkty                                                    #
# ------------------------------------------------------------------ #

TYNK = [(120, 104, 86), (164, 146, 120), (206, 190, 160), (232, 222, 196)]
DREWNO = [(70, 46, 32), (104, 70, 44), (140, 98, 60), (170, 126, 80)]
DACHOWKA = [(80, 34, 30), (120, 52, 38), (160, 74, 48), (196, 104, 66)]
LUPEK = [(40, 46, 60), (58, 66, 84), (80, 90, 110), (110, 120, 138)]
ZLOTO = [(150, 120, 60), (196, 160, 80), (228, 196, 110), (250, 230, 160)]


def namiot() -> pygame.Surface:
    pow_ = pygame.Surface((30, 24), pygame.SRCALPHA)
    plotno = [(96, 70, 46), (140, 106, 66), (184, 148, 96), (216, 190, 136)]
    wypelnij(pow_, [(2, 18), (13, 23), (13, 4)], plotno, 0.75, 3, "deski", 2, 18)
    wypelnij(pow_, [(13, 23), (26, 16), (15, 2), (13, 4)], plotno, 0.3, 4, "deski", 13, 23)
    pow_.fill((34, 24, 20), (9, 15, 3, 6))
    pygame.draw.line(pow_, (80, 56, 40), (13, 4), (15, 2))
    return obrys(pow_)


def budynek(rodzaj: str, s: int) -> pygame.Surface:
    pow_ = pygame.Surface((34, 38), pygame.SRCALPHA)
    if rodzaj == "karczma":
        domek(pow_, 16, 30, 11, 9, DREWNO, DACHOWKA, s, "deski", okno=(255, 206, 110))
        pow_.fill((210, 170, 80), (27, 19, 3, 3))  # szyld
    elif rodzaj == "kuźnia":
        domek(pow_, 16, 30, 10, 8, KAMIEN, LUPEK, s, "cegla", okno=(255, 140, 50), dach_h=6)
        wypelnij(pow_, [(21, 8), (24, 9), (24, 20), (21, 19)], KAMIEN, 0.5, s + 9, "cegla", 21, 8)
    elif rodzaj == "świątynia":
        domek(pow_, 16, 31, 10, 11, TYNK, ZLOTO, s, "cegla", dach_h=4)
        pow_.blit(kula(5, 5, TYNK, s, 0.05), (11, 6))
        pow_.fill((240, 210, 110), (16, 3, 1, 4))
        pow_.fill((240, 210, 110), (15, 4, 3, 1))
    elif rodzaj == "miasto":
        domek(pow_, 10, 32, 7, 7, TYNK, DACHOWKA, s, "deski", okno=(255, 206, 110))
        domek(pow_, 23, 33, 7, 5, DREWNO, DACHOWKA, s + 5, "deski")
        domek(pow_, 17, 26, 5, 14, KAMIEN, LUPEK, s + 9, "cegla", dach_h=8)
    return obrys(pow_)


def jaskinia(s: int) -> pygame.Surface:
    pow_ = pygame.Surface((30, 20), pygame.SRCALPHA)
    pow_.blit(kula(14, 9, KAMIEN, s, 0.3), (0, 1))
    pygame.draw.ellipse(pow_, (14, 12, 18), (9, 10, 8, 10))
    pygame.draw.ellipse(pow_, (30, 26, 34), (10, 11, 6, 8), 1)
    return obrys(pow_)


def obelisk(kolor) -> pygame.Surface:
    pow_ = pygame.Surface((12, 24), pygame.SRCALPHA)
    rampa = [(26, 22, 34), (44, 38, 56), (66, 58, 82), (96, 86, 114)]
    wypelnij(pow_, [(3, 22), (6, 23), (6, 2), (5, 1)], rampa, 0.7, 2)
    wypelnij(pow_, [(6, 23), (9, 22), (7, 2), (6, 2)], rampa, 0.25, 3)
    pow_.fill(kolor, (5, 9, 2, 3))
    return obrys(pow_)


def krag_kamieni() -> pygame.Surface:
    pow_ = pygame.Surface((26, 16), pygame.SRCALPHA)
    for i in range(8):
        a = i / 8 * math.tau
        x, y = 13 + math.cos(a) * 10, 8 + math.sin(a) * 5
        pow_.blit(skala(2, 2, i), (int(x) - 2, int(y) - 3))
    return pow_


def leze_smoka(s: int) -> pygame.Surface:
    pow_ = pygame.Surface((32, 22), pygame.SRCALPHA)
    pow_.blit(kula(15, 10, CZERWIEN, s, 0.3), (0, 1))
    for i in range(5):  # kości i złoto u wejścia
        pow_.set_at((8 + i * 4, 18 + i % 2), (230, 222, 200) if i % 2 else (240, 200, 90))
    pygame.draw.ellipse(pow_, (20, 10, 10), (11, 9, 10, 11))
    return obrys(pow_)


def latajaca_wyspa(s: int) -> pygame.Surface:
    pow_ = pygame.Surface((26, 30), pygame.SRCALPHA)
    wypelnij(pow_, [(1, 12), (25, 12), (14, 28), (11, 28)], KAMIEN, 0.45, s)
    pow_.blit(kula(12, 4, RAMPY["równiny"], s, 0.2), (1, 8))
    pow_.blit(drzewo_lisciaste(s), (5, -6))
    return obrys(pow_)


def sprite_punktu(punkt: str, s: int) -> pygame.Surface:
    if punkt == "obóz":
        return namiot()
    if punkt in ("karczma", "kuźnia", "świątynia", "miasto"):
        return budynek(punkt, s)
    if punkt == "jaskinia":
        return jaskinia(s)
    if punkt == "boss":
        return obelisk((230, 50, 50))
    if punkt == "portal":
        return krag_kamieni()
    if punkt == "leze_smoka":
        return leze_smoka(s)
    if punkt == "latajaca_wyspa":
        return latajaca_wyspa(s)
    return obelisk((120, 200, 255))


# Światła punktów (rodzaj poświaty, przesunięcie od środka pola).
SWIATLA_PUNKTOW = {
    "karczma": ("okno", -6, -10),
    "kuźnia": ("kuznia", -5, -8),
    "miasto": ("okno", -6, -8),
    "portal": ("magia", 0, -2),
    "boss": ("krew", 0, -12),
    "leze_smoka": ("kuznia", 0, -4),
}

_MIEJSCA = [(-8, -1), (6, -2), (-2, 3), (9, 3), (-10, 3), (2, -4), (0, 0)]
_ILE = {"las": 4, "równiny": 1, "bagna": 3, "wzgórza": 2, "kanion": 2, "ruiny": 3}


def dekoracje(biom: str, punkt: str | None, s: int) -> list[tuple[pygame.Surface | None, int, int]]:
    """Lista (sprite, dx, dy) względem środka wierzchu pola. None = ognisko rysowane na żywo."""
    if punkt:
        spr = sprite_punktu(punkt, s)
        dx = -2 if punkt == "obóz" else 0
        wynik = [(spr, dx - spr.get_width() // 2, 4 - spr.get_height())]
        if punkt == "latajaca_wyspa":
            wynik = [(spr, -spr.get_width() // 2, -spr.get_height() - 4)]
        if punkt == "obóz":
            wynik.append((None, 8, 2))
        return wynik
    wynik = []
    for i in range(_ILE.get(biom, 1)):
        if biom != "las" and szum(i, s, 5) < 0.35:
            continue
        mx, my = _MIEJSCA[(i + s) % len(_MIEJSCA)]
        if biom == "las":
            spr = sosna(18 + int(szum(i, s, 1) * 8), s + i)
        elif biom == "równiny":
            spr = drzewo_lisciaste(s + i) if szum(i, s, 2) > 0.5 else skala(3, 2, s + i, RAMPY["równiny"])
        elif biom == "bagna":
            spr = trzcina(s + i)
        elif biom == "wzgórza":
            spr = skala(4 + i, 3 + i // 2, s + i)
        elif biom == "kanion":
            spr = skala(4, 5, s + i, CZERWIEN)
        else:
            spr = kolumna(s + i, 6 + int(szum(i, s, 3) * 8))
        wynik.append((spr, mx - spr.get_width() // 2, my - spr.get_height() + 1))
    wynik.sort(key=lambda d: d[2] + d[0].get_height())
    return wynik


# ------------------------------------------------------------------ #
#  Postać gracza                                                       #
# ------------------------------------------------------------------ #

_SYLWETKA = [
    "...ooo...",
    "..ohhHo..",
    "..ohhHo..",
    "..osssoo.",
    ".ocaagCow",
    "ocaaaaCow",
    "ocagaaCow",
    "ocaaaaCo.",
    ".ocaaCCo.",
    "..oCCCo..",
    "..obobo..",
    "..ob.bo..",
    "..oo.oo..",
]

# Kolor płaszcza i hełmu po klasie — sylwetka ta sama, klasę widać po barwach.
_KOLORY_KLAS = {
    "wojownik": ((164, 46, 48), (106, 28, 38), (182, 188, 202)),
    "mag": ((64, 82, 170), (40, 50, 110), (90, 110, 200)),
    "łotrzyk": ((70, 74, 62), (44, 48, 40), (60, 56, 50)),
    "druid": ((76, 124, 58), (48, 82, 40), (120, 96, 60)),
    "nekromanta": ((70, 40, 86), (40, 22, 52), (200, 196, 180)),
}


def sprite_gracza(klasa: str | None) -> pygame.Surface:
    plaszcz, plaszcz_c, helm = _KOLORY_KLAS.get((klasa or "").lower(), _KOLORY_KLAS["wojownik"])
    paleta = {
        "o": (22, 18, 28), "h": helm, "H": ciemniej(helm, 0.65), "s": (228, 176, 134),
        "c": plaszcz, "C": plaszcz_c, "a": (146, 152, 166), "g": (232, 192, 84),
        "b": (74, 52, 40), "w": (216, 222, 232),
    }
    return sprite_z_tekstu(_SYLWETKA, paleta)


def ognisko(cel: pygame.Surface, x: int, y: int, t: float) -> None:
    for i, (dx, dy) in enumerate(((-2, 0), (2, 0), (0, 1), (-1, -1), (1, -1))):
        cel.set_at((x + dx, y + dy), (90, 60, 40) if i % 2 else (66, 44, 30))
    for i in range(10):
        faza = (t * 2.3 + i * 0.37) % 1.0
        fx = x + int(math.sin(i * 2.1 + t * 7) * (1.5 - faza))
        fy = y - int(faza * 7)
        kolor = (255, 240, 170) if faza < 0.25 else (255, 170, 60) if faza < 0.6 else (210, 70, 40)
        cel.set_at((fx, fy), kolor)
    for i in range(4):  # iskry
        faza = (t * 0.6 + i * 0.25) % 1.0
        cel.set_at((x + int(math.sin(i * 5 + t * 2) * 4), y - 8 - int(faza * 18)),
                   (255, 190, 90) if faza < 0.7 else (140, 80, 50))
