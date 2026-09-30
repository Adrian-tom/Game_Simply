"""Widok rozmowy: portret rozmówcy na malarskim tle (w duchu Disco Elysium).

Tło to rozmyte plamy ochry i chłodnego błękitu (ciepłe światło, zimne cienie),
portret — humanoid z generatora potworów, ubrany według tego, kto mówi.
"""
from __future__ import annotations

import math
import random

import pygame

from grafika.piksele import BAYER, gradient_nieba
from grafika.potwory import _humanoid
from grafika.piksele import obrys
from grafika.scena import SZER, WYS

# fragment nazwy mówiącego → (skóra, ubiór, dodatki)
_STROJE = [
    ("grimbold", ((200, 150, 120), (90, 60, 40), {"topor"})),
    ("herszt", ((210, 160, 120), (150, 40, 40), {"kaptur", "topor"})),
    ("burmistrz", ((220, 180, 150), (60, 80, 140), set())),
    ("vasco", ((200, 160, 120), (60, 110, 70), {"sztylet"})),
    ("kupiec", ((210, 170, 130), (140, 110, 50), set())),
    ("karczmarz", ((220, 170, 140), (120, 90, 60), set())),
    ("kapłan", ((220, 190, 160), (220, 210, 190), {"aureola"})),
    ("rycerz", ((200, 170, 150), (150, 156, 170), {"miecz"})),
    ("ashen", ((180, 170, 170), (40, 40, 50), {"kaptur", "laska"})),
    ("obcy", ((200, 170, 140), (100, 90, 70), {"kaptur"})),
    ("warta", ((200, 160, 130), (80, 90, 110), {"miecz"})),
]


def _stroj(mowi: str):
    m = mowi.lower()
    for klucz, stroj in _STROJE:
        if klucz in m:
            return stroj
    r = random.Random(m)
    return ((190 + r.randint(0, 40), 150 + r.randint(0, 30), 120 + r.randint(0, 30)),
            (r.randint(60, 160), r.randint(50, 140), r.randint(40, 130)), set())


class WidokRozmowy:
    def __init__(self) -> None:
        self._tla: dict[str, pygame.Surface] = {}
        self._portrety: dict[str, pygame.Surface] = {}

    def _tlo(self, mowi: str) -> pygame.Surface:
        if mowi not in self._tla:
            r = random.Random(mowi)
            pow_ = gradient_nieba(SZER, WYS, (38, 42, 58), (92, 70, 58))
            plamy = pygame.Surface((SZER, WYS), pygame.SRCALPHA)
            for _ in range(22):  # plamy farby: ciepłe światło, zimne cienie
                cieply = r.random() < 0.55
                kolor = (200, 150, 80, 36) if cieply else (70, 100, 140, 40)
                x, y = r.randint(-40, SZER), r.randint(-20, WYS)
                pygame.draw.ellipse(plamy, kolor, (x, y, r.randint(40, 140), r.randint(30, 90)))
            pow_.blit(plamy, (0, 0))
            for y in range(0, WYS):  # ziarno płótna
                for x in range(0, SZER, 2):
                    if BAYER[y % 4][(x // 2) % 4] == 0 and r.random() < 0.5:
                        c = pow_.get_at((x, y))
                        pow_.set_at((x, y), (min(255, c.r + 10), min(255, c.g + 8), min(255, c.b + 6)))
            self._tla[mowi] = pow_
        return self._tla[mowi]

    def _portret(self, mowi: str) -> pygame.Surface:
        if mowi not in self._portrety:
            skora, ubior, dodatki = _stroj(mowi)
            p = pygame.Surface((80, 80), pygame.SRCALPHA)
            _humanoid(p, 40, 78, 1.0, skora, ubior, set(dodatki))
            p = obrys(p.subsurface(p.get_bounding_rect(min_alpha=1)).copy())
            self._portrety[mowi] = pygame.transform.scale_by(p, 3.2)
        return self._portrety[mowi]

    def rysuj(self, cel: pygame.Surface, mowi: str, t: float) -> None:
        cel.blit(self._tlo(mowi), (0, 0))
        portret = self._portret(mowi)
        oddech = int(math.sin(t * 1.6) * 1.5)
        x = SZER // 2 - portret.get_width() // 2
        y = 22 + oddech  # głowa u góry kadru, reszta znika za tabliczką z imieniem
        cel.blit(portret, (x, y))
        pygame.draw.rect(cel, (20, 16, 24), (0, WYS - 34, SZER, 34))
        pygame.draw.line(cel, (150, 124, 86), (0, WYS - 34), (SZER, WYS - 34))
