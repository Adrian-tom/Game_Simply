"""Placyk osady z bliska: izometryczna siatka 7×7, budynki i chodzący osadnicy.

Widok mapy pokazuje osadę jako jeden kafelek z daleka. Tutaj gracz wchodzi
między chaty: każdy zbudowany budynek ma swoje miejsce na placu, każdy osadnik
stoi przy warsztacie swojego zajęcia i rusza się, a palisada rośnie razem
z poziomem. Scena rysuje się wprost ze stanu gry — nie trzyma własnej kopii
osady, więc postawienie kuźni widać od razu po powrocie do menu.

Podpisy imion zwracamy do okna (jak arena), bo czcionki żyją w warstwie UI,
a tu operujemy na pikselach sceny.
"""
from __future__ import annotations

import math
import random

import pygame

from grafika import teren
from grafika.piksele import (ciemniej, gradient_nieba, obrys, poswiata, sprite_z_tekstu,
                            winieta)
from grafika.scena import SZER, WYS
from grafika.teren import TH, TW

SIATKA = 7                      # 7×7 pól: obwód to palisada, wnętrze 5×5 to plac
SKOK = 1.4                      # odstęp pól w kaflach — budynki nie mogą się stykać
X0, Y0 = SZER // 2, 73          # ekranowy wierzchołek pola (0, 0)
HORYZONT = 54                   # wyżej niebo i daleki las, niżej łąka

# Gdzie stoi który budynek. Współrzędne tylko z wnętrza (1..5), żeby palisada
# miała własny obwód. Dom i ognisko zostają w środku — to serce obozu.
MIEJSCA: dict[str, tuple[int, int]] = {
    "dom": (3, 2),
    "kuznia": (1, 1),
    "huta": (2, 1),
    "spichlerz": (4, 1),
    "karawanseraj": (5, 1),
    "tartak": (1, 2),
    "sklep": (5, 2),
    "warsztat": (1, 3),
    "stajnie": (5, 3),
    "laboratorium": (1, 4),
    "tawerna": (5, 4),
    "lecznica": (2, 5),
    "targ": (4, 5),
    "wieza": (1, 5),
    "farma": (3, 5),
}

# Kolejność, w jakiej chaty zajmują wolne kafle placu.
MIEJSCA_CHAT: tuple[tuple[int, int], ...] = (
    (2, 2), (4, 2), (2, 3), (4, 3), (2, 4), (4, 4), (3, 4), (5, 5),
    (3, 1), (1, 1), (2, 1), (4, 1), (5, 2), (1, 2), (1, 3), (5, 3),
)

# Wygląd budynku: (połowa szerokości, wysokość ściany, ściana, dach, faktura,
# światło w oknie albo None, wysokość dachu)
WYGLAD: dict[str, tuple] = {
    "dom": (8, 9, teren.DREWNO, teren.DACHOWKA, "deski", (255, 206, 110), 6),
    "kuznia": (7, 7, teren.KAMIEN, teren.LUPEK, "cegla", (255, 140, 50), 5),
    "huta": (7, 8, teren.KAMIEN, teren.CZERWIEN, "cegla", (255, 120, 50), 5),
    "spichlerz": (7, 10, teren.DREWNO, teren.ZLOTO, "deski", None, 6),
    "karawanseraj": (8, 7, teren.TYNK, teren.DREWNO, "cegla", (240, 200, 130), 5),
    "tartak": (7, 6, teren.DREWNO, teren.DREWNO, "deski", None, 4),
    "sklep": (6, 7, teren.TYNK, teren.DACHOWKA, "cegla", (255, 206, 110), 5),
    "warsztat": (6, 7, teren.DREWNO, teren.LUPEK, "deski", (255, 190, 110), 5),
    "stajnie": (8, 6, teren.DREWNO, teren.DREWNO, "deski", None, 4),
    "laboratorium": (6, 8, teren.KAMIEN, teren.LUPEK, "cegla", (170, 230, 140), 5),
    "tawerna": (7, 8, teren.DREWNO, teren.DACHOWKA, "deski", (255, 190, 90), 5),
    "lecznica": (6, 7, teren.TYNK, teren.TYNK, "cegla", (210, 240, 255), 5),
    "targ": (8, 4, teren.DREWNO, teren.ZLOTO, "deski", None, 4),
    "wieza": (4, 16, teren.KAMIEN, teren.LUPEK, "cegla", None, 5),
}

# Blask rzucany przez budynek po ciemku — kuźnia i huta świecą na pomarańczowo.
BLASK: dict[str, str] = {
    "kuznia": "kuznia", "huta": "kuznia", "laboratorium": "magia",
    "tawerna": "okno", "dom": "okno", "lecznica": "latarnia",
}

# Gdzie pracuje kto. Brak budynku → osadnik wraca pod ognisko.
PRZY_BUDYNKU: dict[str, str] = {
    "rolnik": "farma", "tracz": "tartak", "hutnik": "huta",
    "rzemieslnik": "warsztat", "uzdrowiciel": "lecznica", "handlarz": "targ",
}
# Zajęcia bez budynku pracują na skraju placu — tam, gdzie las, skały i brama.
PRZY_KRAWEDZI: dict[str, tuple[int, int]] = {
    "drwal": (1, 1), "kamieniarz": (5, 1), "gornik": (5, 1),
    "zielarz": (1, 5), "mysliwy": (3, 5), "straznik": (3, 5),
}

# Koszula osadnika po zajęciu — zawód widać z daleka, bez czytania podpisu.
KOLORY_ZAJEC: dict[str, tuple[int, int, int]] = {
    "bezczynny": (120, 112, 100), "drwal": (96, 122, 70), "kamieniarz": (110, 110, 118),
    "zielarz": (86, 140, 92), "mysliwy": (128, 104, 58), "rolnik": (196, 174, 92),
    "gornik": (92, 88, 104), "tracz": (138, 104, 64), "hutnik": (176, 92, 54),
    "handlarz": (178, 148, 70), "rzemieslnik": (104, 116, 150), "straznik": (150, 58, 56),
    "uzdrowiciel": (188, 196, 206),
}

_OSADNIK = [
    "..ooo..",
    ".ohhho.",
    ".ohsho.",
    "..sss..",
    ".occco.",
    "occccco",
    "occccco",
    ".occco.",
    "..c.c..",
    "..b.b..",
    "..o.o..",
]


def _ekran(x: float, y: float) -> tuple[int, int]:
    """Kafel (x, y) → piksel górnego wierzchołka jego wierzchu."""
    return (X0 + int((x - y) * (TW / 2)), Y0 + int((x + y) * (TH / 2)))


def _srodek(x: float, y: float, h: int = 0) -> tuple[int, int]:
    """Środek pola placu. Pola są rzadsze niż kafle (SKOK), żeby budynki oddychały."""
    px, py = _ekran(x * SKOK, y * SKOK)
    return (px + TW // 2, py + TH // 2 - h)


def _przygasz(pow_: pygame.Surface, ile: float) -> pygame.Surface:
    """Poświata z mapy jest robiona pod noc; w dzień musi być ledwie muśnięciem."""
    pow_.fill((int(255 * ile),) * 3, special_flags=pygame.BLEND_RGB_MULT)
    return pow_


def _w_wielokacie(x: int, y: int, punkty) -> bool:
    w = False
    for i in range(len(punkty)):
        x1, y1 = punkty[i]
        x2, y2 = punkty[(i + 1) % len(punkty)]
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) / (y2 - y1) * (x2 - x1):
            w = not w
    return w


class WidokOsady:
    def __init__(self) -> None:
        self._kafle: dict = {}
        self._budynki: dict = {}
        self._osadnik: dict = {}
        self._niebo: dict = {}
        self._winieta = winieta(SZER, WYS)
        self._blask = {
            "ogien": _przygasz(poswiata(34, (255, 150, 70)), 0.45),
            "okno": _przygasz(poswiata(14, (255, 190, 110)), 0.35),
            "kuznia": _przygasz(poswiata(18, (255, 120, 50)), 0.5),
            "magia": _przygasz(poswiata(16, (170, 90, 255)), 0.45),
            "latarnia": _przygasz(poswiata(14, (220, 190, 140)), 0.3),
        }
        self._cien = pygame.Surface((12, 5), pygame.SRCALPHA)
        pygame.draw.ellipse(self._cien, (0, 0, 0, 90), self._cien.get_rect())

    # -------------------------------------------------------------- #
    #  Bufory obrazków                                                 #
    # -------------------------------------------------------------- #

    def _tlo(self, pora: str) -> pygame.Surface:
        """Niebo, pas dalekiego lasu i łąka po horyzont — żeby plac nie wisiał w próżni."""
        if pora in self._niebo:
            return self._niebo[pora]
        gora, dol, las, laka = {
            "wiosna": ((122, 170, 206), (206, 220, 206), (44, 72, 52), (74, 108, 56)),
            "lato": ((96, 158, 214), (224, 226, 198), (38, 66, 44), (86, 118, 52)),
            "jesien": ((140, 148, 170), (216, 196, 156), (62, 60, 42), (104, 100, 54)),
            "zima": ((126, 140, 164), (222, 228, 236), (52, 60, 66), (176, 184, 196)),
        }.get(pora, ((120, 170, 210), (196, 214, 222), (44, 72, 52), (74, 108, 56)))

        pow_ = gradient_nieba(SZER, WYS, gora, dol).copy()
        pow_.fill(laka, (0, HORYZONT, SZER, WYS - HORYZONT))
        for x in range(SZER):           # korony dalekiego lasu na linii horyzontu
            h = 4 + int(abs(math.sin(x * 0.21) * 3) + abs(math.sin(x * 0.07) * 4))
            pygame.draw.line(pow_, las, (x, HORYZONT - h), (x, HORYZONT + 1))
        for y in range(HORYZONT, WYS, 2):   # drobne pasma, żeby łąka nie była płaska
            pygame.draw.line(pow_, ciemniej(laka, 0.94), (0, y), (SZER, y))
        self._niebo[pora] = pow_
        return pow_

    def _kafel(self, biom: str, x: int, y: int) -> pygame.Surface:
        klucz = (biom, x, y)
        if klucz not in self._kafle:
            s = (x * 31 + y * 17) % 97
            self._kafle[klucz] = teren.kafel(biom, teren.wysokosc_pola(biom, x, y, s), s)
        return self._kafle[klucz]

    def _budynek(self, klucz: str, poziom: int) -> pygame.Surface:
        pamiec = (klucz, poziom)
        if pamiec in self._budynki:
            return self._budynki[pamiec]
        a, h, sciana, dach, faktura, okno, dach_h = WYGLAD[klucz]
        h += 2 * (poziom - 1)                    # rozbudowa rośnie w górę
        pow_ = pygame.Surface((2 * a + 6, h + dach_h + 8), pygame.SRCALPHA)
        cx, cy = pow_.get_width() // 2, pow_.get_height() - 2
        teren.domek(pow_, cx, cy, a, h, sciana, dach, hash(klucz) % 97, faktura,
                    okno=okno, dach_h=dach_h)
        self._budynki[pamiec] = obrys(pow_)
        return self._budynki[pamiec]

    def _sprite_osadnika(self, zajecie: str, chory: bool) -> pygame.Surface:
        klucz = (zajecie, chory)
        if klucz not in self._osadnik:
            koszula = KOLORY_ZAJEC.get(zajecie, KOLORY_ZAJEC["bezczynny"])
            skora = (168, 196, 150) if chory else (228, 176, 134)
            paleta = {
                "o": (22, 18, 28), "h": (94, 66, 44), "s": skora,
                "c": koszula, "b": (74, 52, 40),
            }
            self._osadnik[klucz] = sprite_z_tekstu(_OSADNIK, paleta)
        return self._osadnik[klucz]

    # -------------------------------------------------------------- #
    #  Układ placu                                                     #
    # -------------------------------------------------------------- #

    def _rozklad(self, gracz) -> dict:
        """Co gdzie stoi: {(x, y): (rodzaj, klucz, poziom)}."""
        from game.oboz import poziom_budynku
        from game.osada import liczba_chat

        plan: dict[tuple[int, int], tuple] = {}
        for klucz, pole in MIEJSCA.items():
            poziom = poziom_budynku(gracz, klucz)
            if poziom > 0:
                plan[pole] = ("farma" if klucz == "farma" else "budynek", klucz, poziom)

        # Chaty zajmują wolne kafle. Budynek ma pierwszeństwo przed chatą, bo
        # chata jest wymienna, a kuźnia stoi tam, gdzie ją narysowaliśmy.
        if ("budynek", "dom", 1) != plan.get(MIEJSCA["dom"]) and MIEJSCA["dom"] not in plan:
            # Zanim stanie dom, w obozie śpi się pod płótnem.
            plan[MIEJSCA["dom"]] = ("namiot", "namiot", 1)

        wolne = [p for p in MIEJSCA_CHAT if p not in plan]
        for i in range(min(liczba_chat(gracz), len(wolne))):
            plan[wolne[i]] = ("chata", "chata", i)
        return plan

    def _stanowisko(self, o: dict, plan: dict) -> tuple[int, int]:
        """Kafel, przy którym kręci się osadnik o danym zajęciu."""
        zajecie = o.get("zajecie") or "bezczynny"
        budynek = PRZY_BUDYNKU.get(zajecie)
        if budynek:
            for pole, (_, klucz, _) in plan.items():
                if klucz == budynek:
                    return pole
        if zajecie in PRZY_KRAWEDZI:
            return PRZY_KRAWEDZI[zajecie]
        return (3, 3)                       # pod ognisko

    # -------------------------------------------------------------- #
    #  Rysowanie                                                       #
    # -------------------------------------------------------------- #

    def rysuj(self, cel: pygame.Surface, gracz, tryb: str, t: float) -> dict:
        from game import kalendarz
        from game.osada import ikona_morale, osadnicy

        pora = kalendarz.pora(gracz)["klucz"]
        zima = pora == "zima"
        biom = "wzgórza" if zima else "równiny"
        plan = self._rozklad(gracz)

        cel.blit(self._tlo(pora), (0, 0))
        self._ziemia(cel, biom, zima)

        # Wszystko w jednej liście sortowanej po głębokości (x + y), inaczej
        # chata z tyłu zasłania tę z przodu.
        from game.oboz import poziom_budynku
        poziom_palisady = poziom_budynku(gracz, "palisada")
        self._plac(cel, zima, plan, poziom_palisady)

        rzeczy: list[tuple[float, str, object]] = []
        for pole, wpis in plan.items():
            rzeczy.append((pole[0] + pole[1], "plan", (pole, wpis)))
        for pole in self._pola_palisady(gracz):
            rzeczy.append((pole[0] + pole[1] - 0.1, "slup", pole))
        for o in osadnicy(gracz):
            x, y = self._pozycja_osadnika(o, plan, t)
            rzeczy.append((x + y + 0.5, "osadnik", (o, x, y)))
        rzeczy.append((3 + 3.8 + 0.4, "gracz", None))

        podpisy: list[tuple[int, int, str, tuple]] = []
        for _, rodzaj, dane in sorted(rzeczy, key=lambda r: r[0]):
            if rodzaj == "plan":
                self._element(cel, dane[0], dane[1])
            elif rodzaj == "slup":
                self._slup(cel, dane, poziom_palisady)
            elif rodzaj == "osadnik":
                o, x, y = dane
                px, py = self._figurka(cel, o, x, y)
                podpisy.append((px, py, f"{ikona_morale(o['morale'])} {o['imie']}",
                                (240, 228, 206)))
            else:
                self._gracz(cel, gracz)

        self._palenisko(cel, t)
        if zima:
            self._snieg(cel, t)
        cel.blit(self._winieta, (0, 0), special_flags=pygame.BLEND_RGB_MULT)
        return {"podpisy": podpisy}

    def _ziemia(self, cel: pygame.Surface, biom: str, zima: bool) -> None:
        """Sam wierzch kafli, rozlany po całym kadrze.

        Bloki z bokami robiłyby z placu latającą wyspę; płaskie romby zlewają
        się z łąką w tle, więc osada po prostu leży w krajobrazie.
        """
        puch = self._puch() if zima else None
        zasieg = int(WYS / TH) + SIATKA * 2
        for y in range(-zasieg, zasieg):
            for x in range(-zasieg, zasieg):
                px, py = _ekran(x, y)
                if px < -TW or px > SZER or py < HORYZONT - TH or py > WYS:
                    continue
                cel.blit(self._kafel(biom, x % 5, y % 5), (px, py), (0, 0, TW, TH))
                if puch is not None:
                    cel.blit(puch, (px, py))

    def _plac(self, cel: pygame.Surface, zima: bool, plan: dict, palisada: int) -> None:
        """Ubita ziemia pod osadą — bez niej chaty stoją na dzikiej łące.

        Plac sięga tylko tam, gdzie ktoś mieszka: w nowym obozie to placyk przy
        ognisku, a pełen romb po obwód palisady dopiero wtedy, gdy osada wyrosła.
        """
        pola = list(plan) + [(3, 3)]
        x0, x1 = min(p[0] for p in pola), max(p[0] for p in pola)
        y0, y1 = min(p[1] for p in pola), max(p[1] for p in pola)
        if palisada > 0:
            x0, y0, x1, y1 = 0, 0, SIATKA - 1, SIATKA - 1
        rogi = [_srodek(x0 - 0.7, y0 - 0.7), _srodek(x1 + 0.7, y0 - 0.7),
                _srodek(x1 + 0.7, y1 + 0.7), _srodek(x0 - 0.7, y1 + 0.7)]
        baza = (214, 220, 228) if zima else (118, 92, 62)
        pygame.draw.polygon(cel, baza, rogi)
        r = random.Random(11)
        for _ in range(900):                     # faktura, żeby plac nie był plamą
            x, y = r.randint(0, SZER - 1), r.randint(HORYZONT, WYS - 1)
            if _w_wielokacie(x, y, rogi):
                cel.set_at((x, y), ciemniej(baza, 0.88 + r.random() * 0.2))

    def _puch(self) -> pygame.Surface:
        """Śnieg jako jasna warstwa na wierzchu kafla — zamiast drugiego zestawu kafli."""
        if "puch" not in self._kafle:
            pow_ = pygame.Surface((TW, TH), pygame.SRCALPHA)
            for px in range(TW):
                for py in range(TH):
                    if teren.w_rombie(px, py):
                        pow_.set_at((px, py), (236, 240, 246, 150))
            self._kafle["puch"] = pow_
        return self._kafle["puch"]

    def _pola_palisady(self, gracz) -> list[tuple[int, int]]:
        from game.oboz import poziom_budynku
        if poziom_budynku(gracz, "palisada") <= 0:
            return []
        pola = []
        for y in range(SIATKA):
            for x in range(SIATKA):
                if not (x == 0 or y == 0 or x == SIATKA - 1 or y == SIATKA - 1):
                    continue
                if y == SIATKA - 1 and x == SIATKA // 2:
                    continue                 # brama od frontu
                pola.append((x, y))
        return pola

    def _slup(self, cel: pygame.Surface, pole, poziom: int) -> None:
        """Odcinek ostrokołu na jednym polu obwodu.

        Ostrokół biegnie wzdłuż krawędzi placu, a te w rzucie izometrycznym idą
        na ukos — stawianie na każdym polu poziomego płotka zostawiałoby dziury
        między polami. Róg dostaje oba kierunki, więc narożniki się domykają.
        Rysowane razem z resztą wedle głębokości: przednia ściana zasłania chaty,
        tylna stoi za nimi.
        """
        x, y = pole
        wys = 7 + 3 * poziom
        kierunki = []
        if y == 0 or y == SIATKA - 1:            # krawędź biegnąca wzdłuż osi X
            kierunki.append((TW / 2 * SKOK, TH / 2 * SKOK))
        if x == 0 or x == SIATKA - 1:            # krawędź wzdłuż osi Y
            kierunki.append((-TW / 2 * SKOK, TH / 2 * SKOK))
        sx, sy = _srodek(x, y)
        for dx, dy in kierunki:
            for k in range(-2, 3):               # pięć żerdzi na odcinek, bez przerw
                px = int(sx + dx * k / 5)
                py = int(sy + dy * k / 5)
                pygame.draw.line(cel, (58, 38, 26), (px, py), (px, py - wys))
                cel.set_at((px, py - wys), (120, 86, 52))
            pygame.draw.line(cel, (76, 52, 34),
                             (int(sx - dx / 2), int(sy - dy / 2) - wys + 4),
                             (int(sx + dx / 2), int(sy + dy / 2) - wys + 4))

    def _element(self, cel: pygame.Surface, pole, wpis) -> None:
        rodzaj, klucz, poziom = wpis
        if rodzaj == "farma":
            self._pole_uprawne(cel, pole, poziom)
            return
        if rodzaj == "namiot":
            spr = self._namiot()
        elif rodzaj == "chata":
            spr = self._budynek_chata(poziom)
        else:
            spr = self._budynek(klucz, poziom)
        sx, sy = _srodek(*pole)
        cel.blit(self._cien, (sx - 6, sy - 1))
        cel.blit(spr, (sx - spr.get_width() // 2, sy - spr.get_height() + 3))
        blask = BLASK.get(klucz)
        if blask:
            # W dzień to tylko ciepły akcent nad kuźnią czy oknem — pełna poświata
            # wypalała dziurę w scenie, bo tu nie ma nocy jak na mapie.
            p = self._blask[blask]
            cel.blit(p, (sx - p.get_width() // 2, sy - spr.get_height() // 2 - p.get_height() // 2),
                     special_flags=pygame.BLEND_RGB_ADD)

    def _namiot(self) -> pygame.Surface:
        if "namiot" not in self._budynki:
            self._budynki["namiot"] = teren.namiot()
        return self._budynki["namiot"]

    def _budynek_chata(self, i: int) -> pygame.Surface:
        klucz = ("chata", i % 3)
        if klucz not in self._budynki:
            pow_ = pygame.Surface((20, 26), pygame.SRCALPHA)
            dach = (teren.DACHOWKA, teren.ZLOTO, teren.LUPEK)[i % 3]
            teren.domek(pow_, 10, 23, 6, 6, teren.DREWNO, dach, 40 + i, "deski",
                        okno=(255, 200, 120), dach_h=5)
            self._budynki[klucz] = obrys(pow_)
        return self._budynki[klucz]

    def _pole_uprawne(self, cel: pygame.Surface, pole, poziom: int) -> None:
        """Zagony zamiast budynku — pola leżą na ziemi, nie stoją na niej."""
        sx, sy = _srodek(*pole)
        for i in range(-5, 6, 2):
            for j in range(-2, 3):
                x, y = sx + i + j, sy + j
                cel.set_at((x, y), (104, 76, 48) if i % 4 else (122, 92, 56))
                if poziom >= 2 and (i + j) % 3 == 0:
                    cel.set_at((x, y - 2), (168, 176, 86))
        if poziom >= 3:
            for i in range(-4, 5, 3):
                cel.set_at((sx + i, sy - 4), (206, 192, 110))

    def _pozycja_osadnika(self, o: dict, plan: dict, t: float) -> tuple[float, float]:
        """Osadnik krąży wokół swojego stanowiska — osada ma żyć, nie pozować."""
        bx, by = self._stanowisko(o, plan)
        r = random.Random(o["imie"])
        faza = r.random() * math.tau
        tempo = 0.25 + r.random() * 0.2
        promien = 0.3 if o.get("chory") else 0.5
        return (bx + math.cos(faza + t * tempo) * promien,
                by + math.sin(faza + t * tempo * 0.8) * promien)

    def _figurka(self, cel: pygame.Surface, o: dict, x: float, y: float) -> tuple[int, int]:
        spr = self._sprite_osadnika(o.get("zajecie") or "bezczynny", bool(o.get("chory")))
        sx, sy = _srodek(x, y)
        cel.blit(self._cien, (sx - 6, sy - 1))
        cel.blit(spr, (sx - spr.get_width() // 2, sy - spr.get_height()))
        return (sx, sy - spr.get_height())

    def _gracz(self, cel: pygame.Surface, gracz) -> None:
        spr = teren.sprite_gracza(getattr(gracz, "klasa", None))
        sx, sy = _srodek(3.0, 3.8)
        cel.blit(self._cien, (sx - 6, sy - 1))
        cel.blit(spr, (sx - spr.get_width() // 2, sy - spr.get_height()))

    def _palenisko(self, cel: pygame.Surface, t: float) -> None:
        """Ognisko z mapy to kilka pikseli; tu jest środkiem osady, więc dostaje
        obmurowanie i własny blask."""
        sx, sy = _srodek(3, 3)
        for k in range(10):
            kat = k / 10 * math.tau
            kx = sx + int(math.cos(kat) * 7)
            ky = sy + int(math.sin(kat) * 3.5)
            cel.fill((96, 92, 88) if k % 2 else (72, 68, 66), (kx - 1, ky - 1, 3, 2))
        p = self._blask["ogien"]
        cel.blit(p, (sx - p.get_width() // 2, sy - p.get_height() // 2 - 4),
                 special_flags=pygame.BLEND_RGB_ADD)
        for dx in (-3, 0, 3):
            teren.ognisko(cel, sx + dx, sy - 1, t + dx * 0.3)

    def _snieg(self, cel: pygame.Surface, t: float) -> None:
        for i in range(70):
            r = random.Random(i)
            x = int((r.random() * SZER + t * (6 + r.random() * 8)) % SZER)
            y = int((r.random() * WYS + t * (14 + r.random() * 16)) % WYS)
            cel.set_at((x, y), (232, 238, 246))
