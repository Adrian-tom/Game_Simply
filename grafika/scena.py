"""Scena mapy: region 9×9 w rzucie izometrycznym, z mgłą wojny i światłem.

Rysuje prosto z danych gry (``gracz.mapa_pola``) — nie trzyma własnej kopii
świata. Zbuforowane są tylko obrazki kafli i dekoracji, kluczowane tym,
co na polu widać, więc zmiana w grze (np. pokonany boss) odświeża się sama.
"""
from __future__ import annotations

import math

import pygame

from grafika import teren
from grafika.piksele import gradient_nieba, poswiata, winieta
from grafika.teren import TH, TW

SZER, WYS = 320, 240          # rozdzielczość sceny w pikselach gry
X0, Y0 = SZER // 2, 46        # ekranowa pozycja górnego wierzchołka pola (0, 0)


class ScenaMapy:
    def __init__(self) -> None:
        self.zmierzch = False
        self._kafle: dict = {}
        self._mgla: dict = {}
        self._dekor: dict = {}
        self._sprite_gracza: dict = {}
        self._obwodka = teren.obwodka()
        self._cien = pygame.Surface((12, 5), pygame.SRCALPHA)
        pygame.draw.ellipse(self._cien, (0, 0, 0, 90), self._cien.get_rect())
        self._niebo = {
            False: gradient_nieba(SZER, WYS, (120, 170, 210), (196, 214, 222)),
            True: gradient_nieba(SZER, WYS, (30, 28, 58), (86, 58, 84)),
        }
        self._winieta = winieta(SZER, WYS)
        self._blask = {
            "ogien": poswiata(44, (255, 150, 70)),
            "okno": poswiata(20, (255, 190, 110)),
            "kuznia": poswiata(26, (255, 120, 50)),
            "magia": poswiata(30, (170, 90, 255)),
            "krew": poswiata(26, (255, 60, 50)),
            "latarnia": poswiata(30, (220, 190, 140)),
        }

    # -------------------------------------------------------------- #
    def _ziarno(self, region, x: int, y: int) -> int:
        rx, ry = region
        return (x * 13 + y * 7 + rx * 101 + ry * 211 + 1) & 0xFFFF

    def _kafel(self, region, x, y, biom, klatka):
        klucz = (region, x, y, biom)
        if klucz not in self._kafle:
            s = self._ziarno(region, x, y)
            h = teren.wysokosc_pola(biom, x, y, s)
            klatki = 3 if biom == "bagna" else 1
            self._kafle[klucz] = (h, [teren.kafel(biom, h, s, k) for k in range(klatki)])
        h, warianty = self._kafle[klucz]
        return h, warianty[klatka % len(warianty)]

    def _zamglony(self, region, x, y, biom, kaf):
        klucz = (region, x, y, biom, self.zmierzch)
        if klucz not in self._mgla:
            self._mgla[klucz] = teren.zamglij(kaf, x, y, self.zmierzch)
        return self._mgla[klucz]

    def _dekoracje(self, region, x, y, biom, punkt):
        klucz = (region, x, y, biom, punkt)
        if klucz not in self._dekor:
            self._dekor[klucz] = teren.dekoracje(biom, punkt, self._ziarno(region, x, y))
        return self._dekor[klucz]

    def _gracz(self, klasa):
        if klasa not in self._sprite_gracza:
            self._sprite_gracza[klasa] = teren.sprite_gracza(klasa)
        return self._sprite_gracza[klasa]

    # -------------------------------------------------------------- #
    def rysuj(self, cel: pygame.Surface, pola, t: float, region=(0, 0),
              gracz_xy: tuple[int, int] | None = None, klasa: str | None = None,
              wszystko_odkryte: bool = False) -> None:
        """Rysuje region na powierzchni SZER×WYS."""
        cel.blit(self._niebo[self.zmierzch], (0, 0))
        swiatla = []
        pozycja_gracza = None
        klatka = int(t * 3) % 3
        n = len(pola)
        for suma in range(2 * n - 1):  # od tyłu do przodu — bliższe zasłaniają dalsze
            for x in range(n):
                y = suma - x
                if not 0 <= y < n:
                    continue
                pole = pola[y][x]
                biom = pole.get("biom", "równiny")
                sx, sy = X0 + (x - y) * (TW // 2), Y0 + (x + y) * (TH // 2)
                h, kaf = self._kafel(region, x, y, biom, klatka)
                srodek = (sx, sy - h + TH // 2)
                if not (wszystko_odkryte or pole.get("odkryte")):
                    cel.blit(self._zamglony(region, x, y, biom, kaf), (sx - TW // 2, sy - h))
                    continue
                cel.blit(kaf, (sx - TW // 2, sy - h))
                tu_gracz = gracz_xy == (x, y)
                if tu_gracz:
                    cel.blit(self._obwodka, (sx - TW // 2, sy - h))
                punkt = pole.get("punkt")
                for spr, dx, dy in self._dekoracje(region, x, y, biom, punkt):
                    if spr is None:
                        teren.ognisko(cel, srodek[0] + dx, srodek[1] + dy, t)
                        swiatla.append(("ogien", srodek[0] + dx, srodek[1] + dy - 2))
                    else:
                        cel.blit(spr, (srodek[0] + dx, srodek[1] + dy))
                if punkt in teren.SWIATLA_PUNKTOW:
                    rodzaj, dx, dy = teren.SWIATLA_PUNKTOW[punkt]
                    swiatla.append((rodzaj, srodek[0] + dx, srodek[1] + dy))
                if tu_gracz:
                    pozycja_gracza = (srodek[0] - 4 + (7 if punkt else 0), srodek[1] - 11 + (3 if punkt else 0))
                    swiatla.append(("latarnia", pozycja_gracza[0] + 4, pozycja_gracza[1] + 6))

        # Gracz na końcu, nad drzewami z pól przed nim: czytelność ważniejsza
        # niż ścisła głębia — w lesie inaczej ginie całkiem.
        if pozycja_gracza:
            px, py = pozycja_gracza
            bob = int(abs(math.sin(t * 3)) * 1.5)
            cel.blit(self._cien, (px - 1, py + 10))
            cel.blit(self._gracz(klasa), (px, py - bob))
            my = py - 7 - int(abs(math.sin(t * 2.2)) * 2)
            for i, szer in enumerate((5, 3, 1)):  # złoty grot nad głową
                cel.fill((236, 200, 110), (px + 4 - szer // 2, my + i, szer, 1))
            cel.fill((120, 90, 40), (px + 2, my - 1, 5, 1))

        if self.zmierzch:
            self._oswietl(cel, swiatla, t)
        cel.blit(self._winieta, (0, 0), special_flags=pygame.BLEND_RGB_MULT)

    def _oswietl(self, cel, swiatla, t):
        """Mapa światła: ciemne otoczenie + addytywne poświaty, potem mnożenie przez scenę."""
        mapa = pygame.Surface(cel.get_size())
        mapa.fill((112, 104, 168))
        for rodzaj, x, y in swiatla:
            b = self._blask[rodzaj]
            if rodzaj == "ogien":
                b = pygame.transform.scale_by(b, 1 + math.sin(t * 9) * 0.04 + math.sin(t * 13.7) * 0.03)
            mapa.blit(b, (x - b.get_width() // 2, y - b.get_height() // 2), special_flags=pygame.BLEND_RGB_ADD)
        cel.blit(mapa, (0, 0), special_flags=pygame.BLEND_RGB_MULT)
        for rodzaj, x, y in swiatla:  # delikatny bloom nad źródłami
            maly = pygame.transform.scale_by(self._blask[rodzaj], 0.35)
            maly.fill((70, 70, 70), special_flags=pygame.BLEND_RGB_MULT)
            cel.blit(maly, (x - maly.get_width() // 2, y - maly.get_height() // 2),
                     special_flags=pygame.BLEND_RGB_ADD)
