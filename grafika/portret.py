"""Widok rozmowy: portret rozmówcy na malarskim tle (w duchu Disco Elysium).

Tło to rozmyte plamy ochry i chłodnego błękitu (ciepłe światło, zimne cienie).
Na nim portret twarzy z ``grafika.twarze`` — głowa i ramiona w skali, w której
widać oczy, bo w grze opartej na rozmowach patrzy się rozmówcy w twarz.

Nie każdy „mówiący" jest człowiekiem: węzły w stylu ``PLAC PRZY SPICHLERZU``
czy ``BRAMA OSADY`` to narracja miejsca. Takie wpisy nie dostają twarzy —
rysujemy wtedy samo tło z lekką winietą, żeby nie udawać, że plac ma oczy.
"""
from __future__ import annotations

import math
import random

import pygame

from grafika.piksele import BAYER, gradient_nieba
from grafika.scena import SZER, WYS
from grafika.twarze import Rysy, mruganie, portret, rysy_z_imienia

# Skala 4: popiersie wypełnia kadr i jest ucięte tabliczką z imieniem —
# tak jak w DE. Przy 3 głowa tonęła w tle i rozmowa traciła intymność.
SKALA = 4

# Fragment nazwy mówiącego → (skóra, ubiór, nakrycie głowy, ewentualne rysy).
# Pierwsze trafienie wygrywa, więc wpisy szczegółowe idą przed ogólnymi.
_ROZMOWCY: list[tuple[str, dict]] = [
    ("grimbold", dict(ubior=(92, 62, 42), naglowie=None,
                      fryzura="krotkie", zarost="broda", brwi="grube", nos="szeroki",
                      zmarszczki=True, blizna=True)),
    ("herszt", dict(ubior=(140, 44, 40), naglowie="kaptur",
                    zarost="broda_dluga", brwi="zmarszczone", blizna=True)),
    ("burmistrz", dict(ubior=(58, 76, 136), naglowie="kapelusz",
                       fryzura="zaczesane", zarost="wasy", zmarszczki=True)),
    ("vasco", dict(ubior=(62, 108, 70), naglowie="kapelusz",
                   zarost="szczecina", brwi="uniesione")),
    ("kupiec", dict(ubior=(138, 108, 52), naglowie="kapelusz", zarost="wasy")),
    ("karczmarz", dict(ubior=(118, 88, 58), fryzura="lysy", zarost="broda",
                       nos="szeroki")),
    ("kapłan", dict(ubior=(216, 208, 190), naglowie="kaptur",
                    fryzura="lysy", zarost="broda_dluga", brwi="proste")),
    ("kaplan", dict(ubior=(216, 208, 190), naglowie="kaptur",
                    fryzura="lysy", zarost="broda_dluga")),
    ("rycerz", dict(ubior=(148, 154, 168), naglowie="helm",
                    zarost="broda", zmarszczki=True, blizna=True)),
    ("ashen", dict(ubior=(40, 40, 50), naglowie="kaptur_kosc",
                   skora=(206, 200, 192), fryzura="lysy", oczy=(150, 60, 50))),
    ("warta", dict(ubior=(78, 90, 110), naglowie="helm", zarost="szczecina")),
    ("starszyzna", dict(ubior=(104, 92, 70), fryzura="dlugie",
                        zarost="broda_dluga", zmarszczki=True)),
    ("obcy", dict(ubior=(100, 90, 70), naglowie="kaptur")),
    ("wędrown", dict(ubior=(96, 84, 64), naglowie="kaptur", zarost="szczecina")),
]

# Rozmówczynie — bez zarostu, inne fryzury. Lista jawna, bo zgadywanie płci
# z końcówki imienia myli się na imionach w rodzaju „Ashen" czy „Kora".
_KOBIETY = ("mirena", "kora", "mira", "zielarka", "łowczyni", "lowczyni",
            "wiedźma", "wiedzma", "karczmarka", "przekupka")

# Mówiący, którzy są miejscem albo głosem narracji — bez twarzy.
_MIEJSCA = ("plac", "brama", "spichlerz", "osada", "chata", "karczma", "obóz",
            "oboz", "droga", "las", "trakt", "rynek", "świątynia", "swiatynia")

# Nakrycie głowy i ubiór bohatera zależne od klasy — „TY" ma wyglądać jak ty.
_KLASY = {
    "Wojownik": dict(ubior=(150, 156, 170), naglowie="helm", zarost="broda"),
    "Mag": dict(ubior=(64, 70, 112), naglowie="kapelusz", fryzura="dlugie",
                zarost="broda_dluga", zmarszczki=True),
    "Lotrzyk": dict(ubior=(72, 64, 58), naglowie="kaptur", zarost="szczecina"),
    "Druid": dict(ubior=(74, 96, 66), naglowie="wieniec", fryzura="dlugie",
                  zarost="broda_dluga"),
    "Nekromanta": dict(ubior=(46, 44, 52), naglowie="kaptur_kosc",
                       skora=(206, 200, 192), fryzura="lysy"),
}


def _czy_miejsce(mowi: str) -> bool:
    m = mowi.lower()
    # „KOWAL — PLAC" to wciąż człowiek; miejscem jest tylko wtedy, gdy nazwa
    # ZACZYNA się od słowa opisującego lokację.
    return any(m.startswith(s) for s in _MIEJSCA)


def _czy_kobieta(mowi: str) -> bool:
    m = mowi.lower()
    return any(k in m for k in _KOBIETY)


def rysy_rozmowcy(mowi: str) -> Rysy:
    """Rysy dla nazwy mówiącego: z tabeli, z klasy bohatera albo losowe ze ziarna."""
    m = mowi.lower()
    kobieta = _czy_kobieta(mowi)

    if m.startswith("ty"):
        from game import ekran
        klasa = getattr(ekran.gracz, "klasa", None) if ekran.gracz else None
        dod = dict(_KLASY.get(klasa, {}))
        imie = getattr(ekran.gracz, "imie", "ty") if ekran.gracz else "ty"
        rysy = rysy_z_imienia(imie, ubior=dod.pop("ubior", (90, 70, 54)),
                              naglowie=dod.pop("naglowie", None),
                              skora=dod.pop("skora", None))
        for k, v in dod.items():
            setattr(rysy, k, v)
        return rysy

    for klucz, dod in _ROZMOWCY:
        if klucz in m:
            dod = dict(dod)
            rysy = rysy_z_imienia(mowi, ubior=dod.pop("ubior", (90, 70, 54)),
                                  naglowie=dod.pop("naglowie", None),
                                  skora=dod.pop("skora", None), kobieta=kobieta)
            for k, v in dod.items():
                if kobieta and k == "zarost":
                    continue
                setattr(rysy, k, v)
            return rysy

    return rysy_z_imienia(mowi, kobieta=kobieta)


class WidokRozmowy:
    def __init__(self) -> None:
        self._tla: dict[str, pygame.Surface] = {}
        self._rysy: dict[str, Rysy] = {}
        self._klatki: dict[tuple[str, int], pygame.Surface] = {}

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

    def _portret(self, mowi: str, otwarte: float) -> pygame.Surface:
        if mowi not in self._rysy:
            self._rysy[mowi] = rysy_rozmowcy(mowi)

        # Trzy warianty oka (otwarte / przymknięte / zamknięte) starczą na
        # mrugnięcie i pozwalają trzymać gotowe klatki zamiast rysować co ramkę.
        stopien = 2 if otwarte > 0.7 else (1 if otwarte > 0.2 else 0)
        klucz = (mowi, stopien)

        if klucz not in self._klatki:
            mala = portret(self._rysy[mowi], otwarte=[0.0, 0.4, 1.0][stopien])
            self._klatki[klucz] = pygame.transform.scale_by(mala, SKALA)
        return self._klatki[klucz]

    def rysuj(self, cel: pygame.Surface, mowi: str, t: float) -> None:
        cel.blit(self._tlo(mowi), (0, 0))

        if not _czy_miejsce(mowi):
            ziarno = (hash(mowi) % 100) / 100.0
            p = self._portret(mowi, mruganie(t, ziarno))
            oddech = int(math.sin(t * 1.6 + ziarno * 6.0) * 1.5)
            cel.blit(p, (SZER // 2 - p.get_width() // 2, 6 + oddech))

        pygame.draw.rect(cel, (20, 16, 24), (0, WYS - 34, SZER, 34))
        pygame.draw.line(cel, (150, 124, 86), (0, WYS - 34), (SZER, WYS - 34))
