"""Arena walki: bohater kontra wróg w barwach biomu, z efektami stanu walki.

Czyta ``game.ekran.walka`` (gracz, wróg, stan) — sama nic nie zmienia.
Trafienia wykrywa po spadku HP między klatkami: błysk i unosząca się liczba.
"""
from __future__ import annotations

import math

import pygame

from grafika import teren
from grafika.piksele import gradient_nieba, jasniej, szum
from grafika.potwory import sprite_wroga
from grafika.scena import SZER, WYS

_TLO_BIOMU = {
    "las": ((70, 110, 120), (150, 170, 150)), "bagna": ((60, 80, 90), (120, 140, 120)),
    "wzgórza": ((110, 150, 190), (190, 200, 210)), "kanion": ((200, 150, 110), (240, 200, 150)),
    "ruiny": ((90, 90, 110), (160, 150, 150)), "równiny": ((120, 170, 210), (200, 220, 225)),
}


class ArenaWalki:
    def __init__(self) -> None:
        self._sprite: dict = {}
        self._tla: dict = {}
        self._hp: dict[str, int] = {}
        self._trafienia: list[dict] = []

    def _tlo(self, biom: str) -> pygame.Surface:
        if biom not in self._tla:
            gora, dol = _TLO_BIOMU.get(biom, _TLO_BIOMU["równiny"])
            pow_ = gradient_nieba(SZER, WYS, gora, dol)
            rampa = teren.rampa_biomu(biom)
            for y in range(150, WYS):
                for x in range(SZER):
                    v = 0.35 + (szum(x // 3, y // 2, 7) - 0.5) * 0.4 + (y - 150) / 300
                    pow_.set_at((x, y), rampa[min(3, max(0, int(v * 4)))])
            mgla = tuple(int(g * 0.8) for g in gora)
            for i in range(9):  # dalekie tło: sylwetki drzew albo skał za mgiełką
                x = int(szum(i, 1, 3) * 300)
                if biom in ("las", "bagna", "równiny"):
                    spr = teren.sosna(20 + int(szum(i, 2, 3) * 14), i)
                else:
                    spr = teren.skala(8 + int(szum(i, 2, 3) * 8), 6 + int(szum(i, 4, 3) * 6), i, rampa)
                spr = spr.copy()
                spr.fill((*mgla, 255), special_flags=pygame.BLEND_RGBA_MULT)
                spr.fill((40, 40, 50, 0), special_flags=pygame.BLEND_RGBA_ADD)
                pow_.blit(spr, (x, 152 - spr.get_height() + int(szum(i, 5, 3) * 6)))
            pygame.draw.ellipse(pow_, jasniej(rampa[1], 1.05, 4), (40, 158, 240, 60))
            self._tla[biom] = pow_
        return self._tla[biom]

    def _wrog(self, nazwa: str, boss: bool) -> pygame.Surface:
        klucz = (nazwa, boss)
        if klucz not in self._sprite:
            spr = sprite_wroga(nazwa, boss)
            if spr.get_height() < 70:
                spr = pygame.transform.scale_by(spr, 2)
            self._sprite[klucz] = spr
        return self._sprite[klucz]

    def _bohater(self, klasa: str) -> pygame.Surface:
        klucz = ("gracz", klasa)
        if klucz not in self._sprite:
            self._sprite[klucz] = pygame.transform.scale_by(teren.sprite_gracza(klasa), 4)
        return self._sprite[klucz]

    def _trafienie(self, kto: str, hp: int, x: int, y: int, t: float) -> bool:
        stare = self._hp.get(kto)
        self._hp[kto] = hp
        if stare is not None and hp < stare:
            self._trafienia.append({"x": x, "y": y, "ile": stare - hp, "t": t, "kto": kto})
        return any(tr["kto"] == kto and t - tr["t"] < 0.25 for tr in self._trafienia)

    def rysuj(self, cel: pygame.Surface, walka: dict, t: float) -> dict:
        """Rysuje arenę. Zwraca pozycje etykiet (w pikselach sceny) dla okna."""
        gracz, wrog, stan = walka["gracz"], walka["wrog"], walka["stan"]
        cel.blit(self._tlo(getattr(gracz, "aktualny_biom", "równiny") or "równiny"), (0, 0))
        boss = bool(stan.get("jest_boss"))

        bohater = self._bohater(gracz.klasa)
        bx, by = 70, 200 - bohater.get_height() - int(abs(math.sin(t * 2.5)) * 2)
        wspr = self._wrog(wrog.nazwa, boss)
        wx = 235 - wspr.get_width() // 2
        wy = 204 - wspr.get_height() + int(math.sin(t * 1.7) * 2)

        for x, szer in ((bx + bohater.get_width() // 2, 30), (wx + wspr.get_width() // 2, wspr.get_width())):
            cien = pygame.Surface((szer, 8), pygame.SRCALPHA)
            pygame.draw.ellipse(cien, (0, 0, 0, 80), cien.get_rect())
            cel.blit(cien, (x - szer // 2, 198))

        if boss:  # aura bossa
            for r in range(3):
                pygame.draw.ellipse(cel, (160, 40, 60), (wx - 6 - r * 3, wy + wspr.get_height() - 14 - r,
                                                         wspr.get_width() + 12 + r * 6, 18 + r * 2), 1)

        blysk_b = self._trafienie("gracz", gracz.hp, bx + 18, by, t)
        blysk_w = self._trafienie("wrog", max(0, wrog.hp), wx + wspr.get_width() // 2, wy, t)
        for spr, x, y, blysk in ((bohater, bx, by, blysk_b), (wspr, wx, wy, blysk_w)):
            if blysk:
                spr = spr.copy()
                spr.fill((255, 255, 255, 0), special_flags=pygame.BLEND_RGBA_MAX)
            cel.blit(spr, (x, y))

        # sługa nekromanty / przyzwanie
        sluga = stan.get("przyzwanie")
        if sluga:
            s = pygame.transform.scale_by(sprite_wroga("Szkielet"), 1.2)
            cel.blit(s, (bx + 44, 204 - s.get_height()))

        # efekty stanu
        if stan.get("wrog_podpalony", 0) > 0 or stan.get("olej_tury", 0) > 0:
            cel_x, cel_y, szer = (wx, wy, wspr.get_width()) if stan.get("wrog_podpalony", 0) > 0 else (bx, by, 36)
            for i in range(14):
                faza = (t * 1.8 + i * 0.13) % 1.0
                x = cel_x + int(szum(i, 3, 9) * szer)
                y = cel_y + wspr.get_height() // 2 - int(faza * 30) if cel_x == wx else by + 20 - int(faza * 20)
                cel.set_at((x, y), (255, 230, 120) if faza < 0.3 else (255, 140, 40) if faza < 0.7 else (180, 50, 30))
        if stan.get("garda"):
            pygame.draw.ellipse(cel, (120, 180, 255), (bx + 28, by + 10, 16, 26), 2)
            pygame.draw.ellipse(cel, (200, 230, 255), (bx + 31, by + 14, 10, 18), 1)
        if stan.get("zapowiedz"):
            puls = int(abs(math.sin(t * 6)) * 2)
            x, y = wx + wspr.get_width() // 2, wy - 14 - puls
            pygame.draw.polygon(cel, (240, 60, 50), [(x, y - 6), (x - 6, y + 5), (x + 6, y + 5)])
            cel.fill((255, 240, 200), (x, y - 2, 1, 4))
            cel.set_at((x, y + 3), (255, 240, 200))
        if stan.get("gracz_trucizna_tury", 0) > 0:
            for i in range(5):
                faza = (t + i * 0.2) % 1.0
                cel.set_at((bx + 6 + i * 6, by + 10 - int(faza * 14)), (120, 220, 90))

        # paski HP
        def pasek(x, y, szer, ile, kolor):
            cel.fill((30, 20, 26), (x - 1, y - 1, szer + 2, 6))
            cel.fill(kolor, (x, y, int(szer * max(0.0, min(1.0, ile))), 4))
            cel.fill(jasniej(kolor, 1.2, 30), (x, y, int(szer * max(0.0, min(1.0, ile))), 1))

        pasek(bx - 4, by - 10, 44, gracz.hp / max(1, gracz.max_hp), (200, 50, 50))
        pasek(wx + wspr.get_width() // 2 - 30, wy - 26, 60, wrog.hp / max(1, wrog.max_hp), (200, 60, 40))

        # unoszące się liczby obrażeń
        etykiety = {"gracz": (bx - 4, by - 22), "wrog": (wx + wspr.get_width() // 2 - 30, wy - 38), "liczby": []}
        self._trafienia = [tr for tr in self._trafienia if t - tr["t"] < 1.2]
        for tr in self._trafienia:
            etykiety["liczby"].append((tr["x"], tr["y"] - int((t - tr["t"]) * 22), f"-{tr['ile']}"))
        return etykiety
