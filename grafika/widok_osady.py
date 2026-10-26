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

from game import ruch
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

# Druga klatka marszu: nogi rozstawione. Razem z podskokiem daje kroki bez
# osobnych rysunków na każdy kierunek — ludzik jest symetryczny.
_OSADNIK_KROK = _OSADNIK[:8] + [
    ".c...c.",
    ".b...b.",
    ".o...o.",
]

# Gdzie stoi ognisko, gdzie brama od frontu i przez które pole przechodzi bohater.
OGNISKO = (3, 3)
BRAMA = (SIATKA // 2, SIATKA - 1)
POLE_BOHATERA = (3, 4)

# Siła poświaty w dzień (jak dawniej, ledwie muśnięcie) — w nocy rośnie do pełnej.
SILA_BLASKU: dict[str, float] = {
    "ogien": 0.45, "okno": 0.35, "kuznia": 0.5, "magia": 0.45, "latarnia": 0.3,
}
KOLOR_BLASKU: dict[str, tuple[int, int, int]] = {
    "ogien": (255, 150, 70), "okno": (255, 190, 110), "kuznia": (255, 120, 50),
    "magia": (170, 90, 255), "latarnia": (220, 190, 140),
}
PROMIEN_BLASKU: dict[str, int] = {
    "ogien": 34, "okno": 14, "kuznia": 18, "magia": 16, "latarnia": 14,
}

# Barwa światła: noc chłodna i granatowa, dzień biały, w przejściu cieplej.
KOLOR_NOCY = (64, 74, 124)
KOLOR_DNIA = (255, 255, 255)
KOLOR_ZMIERZCHU = (255, 196, 140)
PRZESUNIECIE_DNIA = 0.2 * ruch.DLUGOSC_DNIA   # scena zaczyna rano, nie w ciemności
NADROBIENIE = 12.0              # tyle sekund ruchu nadrabiamy po przerwie w oglądaniu sceny


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
        self._blask_baza = {n: poswiata(PROMIEN_BLASKU[n], KOLOR_BLASKU[n]) for n in KOLOR_BLASKU}
        self._blask_pamiec: dict = {}
        self._swiecenia: list[tuple[str, int, int]] = []   # co świeci w tej klatce
        self._ruch = ruch.Ruch()
        self._ostatni_t: float | None = None
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

    def _sprite_osadnika(self, zajecie: str, chory: bool, krok: int = 0,
                         lezy: bool = False) -> pygame.Surface:
        klucz = (zajecie, chory, krok, lezy)
        if klucz not in self._osadnik:
            koszula = KOLORY_ZAJEC.get(zajecie, KOLORY_ZAJEC["bezczynny"])
            skora = (168, 196, 150) if chory else (228, 176, 134)
            paleta = {
                "o": (22, 18, 28), "h": (94, 66, 44), "s": skora,
                "c": koszula, "b": (74, 52, 40),
            }
            spr = sprite_z_tekstu(_OSADNIK_KROK if krok else _OSADNIK, paleta)
            self._osadnik[klucz] = pygame.transform.rotate(spr, 90) if lezy else spr
        return self._osadnik[klucz]

    def _blask(self, nazwa: str, ciemnosc: float) -> pygame.Surface:
        """Poświata o sile zależnej od pory: w dzień muśnięcie, w nocy pełny blask."""
        dzien = SILA_BLASKU[nazwa]
        sila = dzien + (1.0 - dzien) * ciemnosc
        klucz = (nazwa, round(sila * 12))
        if klucz not in self._blask_pamiec:
            self._blask_pamiec[klucz] = _przygasz(self._blask_baza[nazwa].copy(), klucz[1] / 12)
        return self._blask_pamiec[klucz]

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

        czas_dnia = t + PRZESUNIECIE_DNIA
        self._ruszaj(gracz, plan, poziom_palisady, t, czas_dnia)
        self._swiecenia = []

        rzeczy: list[tuple[float, str, object]] = []
        for pole, wpis in plan.items():
            rzeczy.append((pole[0] + pole[1], "plan", (pole, wpis)))
        for pole in self._pola_palisady(gracz):
            rzeczy.append((pole[0] + pole[1] - 0.1, "slup", pole))
        for o in osadnicy(gracz):
            x, y = self._ruch.pozycja(o["imie"])
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
                px, py = self._figurka(cel, o, x, y, t)
                podpisy.append((px, py, f"{ikona_morale(o['morale'])} {o['imie']}",
                                (240, 228, 206)))
            else:
                self._gracz(cel, gracz)

        # Światło dnia nakładamy na całą scenę, a dopiero potem dodajemy to, co
        # świeci samo — ognisko, okna, kuźnię. Dlatego noc jest ciemna, a ogień
        # jedyny jasny (jak w stylu Mroczny), choć wszystko jest pixelartem.
        jasnosc = ruch.swiatlo(czas_dnia)
        cel.fill(self._barwa_swiatla(jasnosc), special_flags=pygame.BLEND_RGB_MULT)
        ciemnosc = 1.0 - jasnosc
        for nazwa, bx, by in self._swiecenia:
            p = self._blask(nazwa, ciemnosc)
            cel.blit(p, (bx - p.get_width() // 2, by - p.get_height() // 2),
                     special_flags=pygame.BLEND_RGB_ADD)
        self._palenisko(cel, t, ciemnosc)
        if zima:
            self._snieg(cel, t)
        cel.blit(self._winieta, (0, 0), special_flags=pygame.BLEND_RGB_MULT)
        return {"podpisy": podpisy}

    @staticmethod
    def _barwa_swiatla(jasnosc: float) -> tuple[int, int, int]:
        """Mnożnik koloru sceny: od granatowej nocy przez ciepły zmierzch do białego dnia."""
        baza = [KOLOR_NOCY[i] + (KOLOR_DNIA[i] - KOLOR_NOCY[i]) * jasnosc for i in range(3)]
        cieplo = 4.0 * jasnosc * (1.0 - jasnosc) * 0.35      # najmocniej w połowie przejścia
        return tuple(int(baza[i] + (KOLOR_ZMIERZCHU[i] - baza[i]) * cieplo) for i in range(3))

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
            # Poświata idzie na listę i trafia na scenę dopiero po nałożeniu
            # światła dnia — inaczej noc przyciemniłaby także ogień.
            self._swiecenia.append((blask, sx, sy - spr.get_height() // 2))

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

    # -------------------------------------------------------------- #
    #  Ruch osadników                                                  #
    # -------------------------------------------------------------- #

    def _zablokowane(self, plan: dict, palisada: int) -> set[tuple[int, int]]:
        """Pola, przez które nie da się przejść: budynki, ognisko, bohater i obwód placu.

        Pole uprawne jest płaskie — po zagonach się chodzi. Obwód zostaje
        otwarty tylko w bramie od frontu.
        """
        zablokowane = {pole for pole, w in plan.items() if w[0] in ("budynek", "chata", "namiot")}
        zablokowane |= {OGNISKO, POLE_BOHATERA}
        for i in range(SIATKA):
            zablokowane |= {(i, 0), (i, SIATKA - 1), (0, i), (SIATKA - 1, i)}
        zablokowane.discard(BRAMA)
        return zablokowane

    def _domy(self, plan: dict) -> list[tuple[int, int]]:
        """Pola, w których osadnicy śpią: chaty, a bez nich dom lub namiot."""
        chaty = sorted(p for p, w in plan.items() if w[0] == "chata")
        if chaty:
            return chaty
        pod_dachem = sorted(p for p, w in plan.items() if w[1] in ("dom", "namiot"))
        return pod_dachem or [OGNISKO]

    def _ruszaj(self, gracz, plan: dict, palisada: int, t: float, czas_dnia: float) -> None:
        """Wyznacz osadnikom cele wg pory doby i przesuń ich o czas, który upłynął."""
        from game.osada import osadnicy

        ludzie = osadnicy(gracz)
        self._ruch.przebuduj(self._zablokowane(plan, palisada))
        pierwsza = self._ostatni_t is None
        imiona = {o["imie"] for o in ludzie}
        for imie in [i for i in self._ruch.postacie if i not in imiona]:
            self._ruch.usun(imie)

        pora = ruch.faza(czas_dnia)
        domy = self._domy(plan)
        tawerna = next((p for p, w in plan.items() if w[1] == "tawerna"), None)
        for i, o in enumerate(ludzie):
            cel, czynnosc = ruch.zadanie(o, self._stanowisko(o, plan), domy[i % len(domy)],
                                         OGNISKO, tawerna, pora)
            if o["imie"] not in self._ruch.postacie:
                # Przy pierwszym obrazie osada już żyje; później przybysz wchodzi bramą.
                self._ruch.dodaj(o["imie"], cel if pierwsza else BRAMA)
            self._ruch.ustaw_cel(o["imie"], cel, czynnosc)

        # Scena stoi, gdy gracz siedzi w menu innego widoku. Po powrocie nie
        # zaczynamy od zera: nadrabiamy kilka sekund, żeby ludzie byli w drodze
        # albo na miejscu, a nie rozsypani po starych pozycjach.
        if pierwsza:
            uplynelo = NADROBIENIE
        else:
            uplynelo = max(0.0, t - self._ostatni_t)
            # Zwykła klatka to ułamek sekundy (chwilowe zacięcie ucinamy do 0,1 s);
            # dłuższa przerwa znaczy „sceny nie było widać” — wtedy nadrabiamy.
            uplynelo = min(uplynelo, NADROBIENIE if uplynelo > 0.5 else 0.1)
        self._ostatni_t = t
        while uplynelo > 1e-9:
            krok = min(0.1, uplynelo)
            self._ruch.aktualizuj(krok)
            uplynelo -= krok

    def _figurka(self, cel: pygame.Surface, o: dict, x: float, y: float,
                 t: float = 0.0) -> tuple[int, int]:
        post = self._ruch.postacie.get(o["imie"])
        idzie = bool(post and post.idzie)
        sen = bool(post and not idzie and post.czynnosc == "sen" and self._ruch.dotarl(o["imie"]))
        krok = 1 if idzie and int(post.dystans * 2.4) % 2 else 0
        spr = self._sprite_osadnika(o.get("zajecie") or "bezczynny", bool(o.get("chory")),
                                    krok, sen)
        odbicie = 0
        if idzie and int(post.dystans * 4.8) % 2:
            odbicie = 1                       # podskok na każdym kroku
        elif post and post.czynnosc == "praca" and not idzie:
            # Pracujący kołysze się jak przy młocie; faza z imienia, żeby nie pracowali chórem.
            odbicie = 1 if int((t + ruch.los_imienia(o["imie"]) % 7) * 2.6) % 2 else 0
        sx, sy = _srodek(x, y)
        cel.blit(self._cien, (sx - 6, sy - 1))
        cel.blit(spr, (sx - spr.get_width() // 2, sy - spr.get_height() - odbicie))
        return (sx, sy - spr.get_height())

    def _gracz(self, cel: pygame.Surface, gracz) -> None:
        spr = teren.sprite_gracza(getattr(gracz, "klasa", None))
        sx, sy = _srodek(3.0, 3.8)
        cel.blit(self._cien, (sx - 6, sy - 1))
        cel.blit(spr, (sx - spr.get_width() // 2, sy - spr.get_height()))

    def _palenisko(self, cel: pygame.Surface, t: float, ciemnosc: float = 0.0) -> None:
        """Ognisko z mapy to kilka pikseli; tu jest środkiem osady, więc dostaje
        obmurowanie i własny blask. Rysowane po nałożeniu światła dnia: ogień
        świeci sam, więc noc go nie przyciemnia."""
        sx, sy = _srodek(*OGNISKO)
        for k in range(10):
            kat = k / 10 * math.tau
            kx = sx + int(math.cos(kat) * 7)
            ky = sy + int(math.sin(kat) * 3.5)
            cel.fill((96, 92, 88) if k % 2 else (72, 68, 66), (kx - 1, ky - 1, 3, 2))
        p = self._blask("ogien", ciemnosc)
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
