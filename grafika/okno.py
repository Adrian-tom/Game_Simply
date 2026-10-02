"""Okno gry: pixelartowa mapa, HUD postaci i konsola tekstowa.

Okno przejmuje ``print``, ``input`` i czyszczenie ekranu (``os.system("cls")``),
więc cała logika gry działa w nim bez zmian. Ekrany, które mają już grafikę
(na razie mapa regionu), czytają stan z ``game.ekran``; reszta jest tekstem
w konsoli po prawej. Kolejne ekrany można przenosić na grafikę pojedynczo.

Układ (piksele logiczne, okno skaluje się do rozmiaru ekranu):

    ┌──────────── 640 ───────────┬──────────── 640 ────────────┐
    │  mapa 320×240 w skali ×2   │                              │
    │                        480 │           konsola            │
    ├────────────────────────────┤                              │
    │  HUD postaci           240 │                              │
    └────────────────────────────┴──────────────────────────────┘
"""
from __future__ import annotations

import builtins
import os
import re
import sys

import pygame

from game import ekran
from grafika.arena import ArenaWalki
from grafika.portret import WidokRozmowy
from grafika.widok_osady import WidokOsady
from grafika.scena import SZER, WYS, ScenaMapy

SZEROKOSC, WYSOKOSC = 1280, 720
SKALA_MAPY = 2
FPS = 30

TLO = (14, 12, 20)
RAMKA = (150, 124, 86)
RAMKA_CIEMNA = (70, 56, 44)
ZLOTY = (232, 200, 130)
TEKST = (224, 216, 200)
PRZYGASZONY = (150, 142, 132)

_REGION_TYTULOWY = (9999, 9999)  # klucz bufora kafli — poza zasięgiem prawdziwych regionów
_OPCJA = re.compile(r"^\s*\[([0-9A-Za-z]{1,3})\]")
_KLAWISZE_SKROTOW = {
    pygame.K_UP: "gora", pygame.K_DOWN: "dol", pygame.K_LEFT: "lewo", pygame.K_RIGHT: "prawo",
    pygame.K_SPACE: "spacja",
}


class OknoZamkniete(Exception):
    """Gracz zamknął okno — przerywa pętlę gry tak jak Ctrl+C w terminalu."""


# ------------------------------------------------------------------ #
#  Tekst z emoji                                                       #
# ------------------------------------------------------------------ #

def _czcionka(nazwy: list[str], rozmiar: int, plik: str | None = None) -> pygame.font.Font:
    if plik:
        sciezka = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts", plik)
        if os.path.exists(sciezka):
            return pygame.font.Font(sciezka, rozmiar)
    for nazwa in nazwy:
        sciezka = pygame.font.match_font(nazwa)
        if sciezka:
            return pygame.font.Font(sciezka, rozmiar)
    return pygame.font.Font(None, rozmiar + 4)


def _czy_emoji(ch: str) -> bool:
    o = ord(ch)
    # Ramki i bloki (U+2500–U+25FF) zostają w kroju mono — z nich jest baner tytułowy.
    return (o >= 0x2300 and not 0x2500 <= o <= 0x25FF) or o in (0xFE0F, 0x200D)


class Pisarz:
    """Renderuje tekst krojem mono, a emoji kolorowym krojem systemowym."""

    def __init__(self, rozmiar: int, pogrubiony: bool = False):
        self.mono = _czcionka(["consolas", "dejavusansmono", "couriernew"], rozmiar,
                              "consolab.ttf" if pogrubiony else "consola.ttf")
        self.emoji = _czcionka([], rozmiar - 2, "seguiemj.ttf")
        self.wysokosc = self.mono.get_linesize() + 1
        self._cache: dict = {}

    def _odcinki(self, tekst: str):
        odc, biezacy, tryb = [], "", None
        for ch in tekst:
            e = _czy_emoji(ch)
            if tryb is not None and e != tryb:
                odc.append((biezacy, tryb))
                biezacy = ""
            biezacy += ch
            tryb = e
        if biezacy:
            odc.append((biezacy, tryb))
        return odc

    def render(self, tekst: str, kolor) -> pygame.Surface:
        klucz = (tekst, kolor)
        if klucz in self._cache:
            return self._cache[klucz]
        czesci = []
        for kawalek, emoji in self._odcinki(tekst):
            if emoji:
                kawalek = kawalek.replace("\ufe0f", "")
                if not kawalek:
                    continue
                czesci.append(self.emoji.render(kawalek, True, kolor))
            else:
                czesci.append(self.mono.render(kawalek, True, kolor))
        szer = sum(c.get_width() for c in czesci)
        pow_ = pygame.Surface((max(1, szer), self.wysokosc), pygame.SRCALPHA)
        x = 0
        for c in czesci:
            pow_.blit(c, (x, (self.wysokosc - c.get_height()) // 2))
            x += c.get_width()
        if len(self._cache) > 3000:
            self._cache.clear()
        self._cache[klucz] = pow_
        return pow_

    def szerokosc(self, tekst: str) -> int:
        return self.render(tekst, TEKST).get_width()


# ------------------------------------------------------------------ #
#  Konsola                                                             #
# ------------------------------------------------------------------ #

class Konsola:
    def __init__(self, rect: pygame.Rect, pisarz: Pisarz):
        self.rect = rect
        self.pisarz = pisarz
        self.linie: list[str] = [""]
        self.przewiniecie = 0
        self.opcje: list[tuple[pygame.Rect, str]] = []

    def pisz(self, tekst: str) -> None:
        for i, czesc in enumerate(tekst.split("\n")):
            if i:
                self.linie.append("")
            self.linie[-1] += czesc.replace("\t", "    ")
        if len(self.linie) > 1500:
            self.linie = self.linie[-1000:]
        self.przewiniecie = 0

    def wyczysc(self) -> None:
        self.linie = [""]
        self.przewiniecie = 0

    def _zawin(self, linia: str, szer: int) -> list[str]:
        if self.pisarz.szerokosc(linia) <= szer:
            return [linia]
        wcięcie = len(linia) - len(linia.lstrip(" "))
        slowa = linia.lstrip(" ").split(" ")
        wiersze, biezacy = [], ""
        for i, slowo in enumerate(slowa):
            if i == 0:
                biezacy = " " * wcięcie + slowo
                continue
            proba = f"{biezacy} {slowo}"
            if biezacy and self.pisarz.szerokosc(proba) > szer:
                wiersze.append(biezacy)
                biezacy = " " * (wcięcie + 2) + slowo
            else:
                biezacy = proba
        wiersze.append(biezacy)
        return wiersze

    def rysuj(self, cel: pygame.Surface, wpisywane: str | None, t: float, mysz) -> None:
        wnetrze = self.rect.inflate(-40, -36)
        linie = list(self.linie)
        if wpisywane is not None:
            kursor = "▌" if int(t * 2) % 2 == 0 else " "
            linie[-1] = linie[-1] + wpisywane + kursor
        wiersze: list[tuple[str, bool, str | None]] = []  # (tekst, pierwszy wiersz linii, klucz opcji)
        for linia in linie:
            m = _OPCJA.match(linia)
            for i, w in enumerate(self._zawin(linia, wnetrze.width)):
                wiersze.append((w, i == 0, m.group(1) if m else None))
        wys = self.pisarz.wysokosc
        miesci = wnetrze.height // wys
        self.przewiniecie = max(0, min(self.przewiniecie, len(wiersze) - miesci))
        koniec = len(wiersze) - self.przewiniecie
        poczatek = max(0, koniec - miesci)
        if poczatek > 0:  # pierwszy wiersz ustępuje miejsca znacznikowi
            poczatek += 1
        widoczne = wiersze[poczatek:koniec]
        self.opcje = []
        y = wnetrze.y
        if poczatek > 0:
            cel.blit(self.pisarz.render(f"▲ wyżej jeszcze {poczatek} wierszy (kółko myszy)", PRZYGASZONY),
                     (wnetrze.x, y))
            y += wys
        for tekst, _pierwszy, klucz in widoczne:
            kolor = TEKST
            pas = tekst.strip()
            if pas and set(pas) <= set("═─━-=·"):
                kolor = RAMKA
            if klucz and wpisywane is not None:
                obszar = pygame.Rect(wnetrze.x - 8, y - 1, wnetrze.width + 16, wys)
                self.opcje.append((obszar, klucz))
                if obszar.collidepoint(mysz):
                    pygame.draw.rect(cel, (52, 42, 34), obszar)
                    pygame.draw.rect(cel, RAMKA_CIEMNA, obszar, 1)
                    kolor = ZLOTY
            cel.blit(self.pisarz.render(tekst, kolor), (wnetrze.x, y))
            y += wys
        if self.przewiniecie:
            znak = self.pisarz.render(f"▼ jeszcze {self.przewiniecie} wierszy niżej (kółko myszy)", PRZYGASZONY)
            cel.blit(znak, (wnetrze.x, self.rect.bottom - 22))


# ------------------------------------------------------------------ #
#  Ramki i paski w stylu pixelart                                      #
# ------------------------------------------------------------------ #

def ramka(cel: pygame.Surface, rect: pygame.Rect, tlo=(20, 17, 28)) -> None:
    pygame.draw.rect(cel, tlo, rect)
    pygame.draw.rect(cel, RAMKA, rect, 2)
    pygame.draw.rect(cel, RAMKA_CIEMNA, rect.inflate(-8, -8), 2)
    for x, y in (rect.topleft, (rect.right - 6, rect.y), (rect.x, rect.bottom - 6),
                 (rect.right - 6, rect.bottom - 6)):
        cel.fill((226, 196, 120), (x, y, 6, 6))
        cel.fill(TLO, (x + 2, y + 2, 2, 2))


def pasek(cel: pygame.Surface, x: int, y: int, szer: int, ile: float, kolor) -> None:
    ile = max(0.0, min(1.0, ile))
    cel.fill((40, 30, 36), (x, y, szer, 14))
    wypelnione = int((szer - 4) * ile)
    cel.fill(kolor, (x + 2, y + 2, wypelnione, 10))
    cel.fill(tuple(min(255, k + 60) for k in kolor), (x + 2, y + 2, wypelnione, 2))
    cel.fill(tuple(int(k * 0.6) for k in kolor), (x + 2, y + 10, wypelnione, 2))
    for i in range(x + 2, x + szer - 2, 8):  # podziałka co 8 px
        cel.fill((24, 18, 22), (i, y + 2, 1, 10))


# ------------------------------------------------------------------ #
#  Okno                                                                #
# ------------------------------------------------------------------ #

class Okno:
    def __init__(self) -> None:
        pygame.init()
        self.ekran = pygame.display.set_mode((SZEROKOSC, WYSOKOSC), pygame.SCALED | pygame.RESIZABLE)
        pygame.display.set_caption("Pro RPG")
        self.zegar = pygame.time.Clock()
        self.t = 0.0
        self.pisarz = Pisarz(16)
        self.hud = Pisarz(17)
        self.duzy = Pisarz(24, pogrubiony=True)
        self.konsola = Konsola(pygame.Rect(648, 8, 624, 704), self.pisarz)
        self.scena = ScenaMapy()
        self.arena = ArenaWalki()
        self.rozmowa = WidokRozmowy()
        self.osada = WidokOsady()
        self.mapa = pygame.Surface((SZER, WYS))
        self._tytul = None
        self._print = builtins.print
        self._system = os.system

    # ---- zastępstwa funkcji wbudowanych ---------------------------- #
    def print(self, *args, sep=" ", end="\n", file=None, flush=False) -> None:
        if file is not None and file is not sys.stdout:
            self._print(*args, sep=sep, end=end, file=file, flush=flush)
            return
        sep = " " if sep is None else sep
        end = "\n" if end is None else end
        self.konsola.pisz(sep.join(str(a) for a in args) + end)

    def system(self, polecenie) -> int:
        if str(polecenie).strip() in ("cls", "clear"):
            self.konsola.wyczysc()
            return 0
        return self._system(polecenie)

    def input(self, zacheta: str = "") -> str:
        self.konsola.pisz(str(zacheta))
        czekaj_na_enter = "Enter" in str(zacheta)
        bufor = ""
        while True:
            for e in pygame.event.get():
                if e.type == pygame.QUIT:
                    raise OknoZamkniete
                if e.type == pygame.KEYDOWN:
                    if e.key == pygame.K_F2:
                        self.scena.zmierzch = not self.scena.zmierzch
                    elif e.key == pygame.K_F11:
                        pygame.display.toggle_fullscreen()
                    elif e.key in (pygame.K_RETURN, pygame.K_KP_ENTER):
                        return self._odpowiedz(bufor)
                    elif e.key == pygame.K_BACKSPACE:
                        bufor = bufor[:-1]
                    elif e.key == pygame.K_ESCAPE:
                        bufor = ""
                    elif not bufor and e.key in _KLAWISZE_SKROTOW:
                        nazwa = _KLAWISZE_SKROTOW[e.key]
                        if nazwa in ekran.skroty:
                            return self._odpowiedz(ekran.skroty[nazwa])
                        if czekaj_na_enter and nazwa == "spacja":
                            return self._odpowiedz("")
                elif e.type == pygame.TEXTINPUT:
                    if bufor or e.text.strip():  # spacja-skrót nie zostawia śladu w polu
                        bufor += e.text
                elif e.type == pygame.MOUSEWHEEL:
                    self.konsola.przewiniecie += e.y * 3
                elif e.type == pygame.MOUSEBUTTONDOWN and e.button == 1:
                    for obszar, klucz in self.konsola.opcje:
                        if obszar.collidepoint(e.pos):
                            return self._odpowiedz(klucz)
                    if czekaj_na_enter and self.konsola.rect.collidepoint(e.pos):
                        return self._odpowiedz("")
            self.rysuj(bufor)
            self.t += self.zegar.tick(FPS) / 1000

    def _odpowiedz(self, tekst: str) -> str:
        self.konsola.pisz(tekst + "\n")
        return tekst

    # ---- rysowanie ------------------------------------------------- #
    def rysuj(self, wpisywane: str | None = None) -> None:
        self.ekran.fill(TLO)
        gracz = ekran.gracz
        etykiety_walki = None
        podpisy_osady = None
        if ekran.walka is not None:
            etykiety_walki = self.arena.rysuj(self.mapa, ekran.walka, self.t)
        elif ekran.rozmowa:
            self.rozmowa.rysuj(self.mapa, ekran.rozmowa, self.t)
        elif ekran.widok and gracz is not None:
            podpisy_osady = self.osada.rysuj(self.mapa, gracz, ekran.widok, self.t)["podpisy"]
        elif gracz is not None and getattr(gracz, "mapa_pola", None):
            self.scena.rysuj(
                self.mapa, gracz.mapa_pola, self.t,
                region=(gracz.region_x, gracz.region_y),
                gracz_xy=(gracz.mapa_x, gracz.mapa_y), klasa=gracz.klasa,
                osada=_osada_do_rysunku(gracz),
            )
        else:
            self.scena.rysuj(self.mapa, self._pola_tytulowe(), self.t, region=_REGION_TYTULOWY,
                             wszystko_odkryte=True)
        pygame.transform.scale(self.mapa, (SZER * SKALA_MAPY, WYS * SKALA_MAPY),
                               self.ekran.subsurface((0, 0, SZER * SKALA_MAPY, WYS * SKALA_MAPY)))
        pygame.draw.rect(self.ekran, RAMKA, (0, 0, SZER * SKALA_MAPY, WYS * SKALA_MAPY), 2)
        if etykiety_walki:
            self._etykiety_walki(etykiety_walki)
        elif podpisy_osady:
            self._podpisy_osady(podpisy_osady)
        elif ekran.walka is None and ekran.rozmowa:
            self._napis(ekran.rozmowa, 24, WYS * SKALA_MAPY - 52, ZLOTY, duzy=True)
        hud = pygame.Rect(8, WYS * SKALA_MAPY + 8, SZER * SKALA_MAPY - 16, WYSOKOSC - WYS * SKALA_MAPY - 16)
        ramka(self.ekran, hud)
        if gracz is None:
            self._hud_tytulowy(hud)
        else:
            self._hud(hud, gracz)
        ramka(self.ekran, self.konsola.rect)
        self.konsola.rysuj(self.ekran, wpisywane, self.t, pygame.mouse.get_pos())
        pygame.display.flip()

    def _podpisy_osady(self, podpisy) -> None:
        """Imiona osadników nad figurkami.

        Czcionki żyją tutaj, nie w scenie, więc widok oddaje same pozycje
        w pikselach sceny. Osadnicy tłoczą się przy jednym warsztacie, więc
        podpis, który wpadłby na już narysowany, idzie wyżej — inaczej imiona
        zlewają się w jedno nieczytelne pasmo.
        """
        s = SKALA_MAPY
        zajete: list[pygame.Rect] = []
        for x, y, tekst, kolor in sorted(podpisy, key=lambda p: p[1]):
            szer = self.hud.szerokosc(tekst) + 6
            px = min(max(2, x * s - szer // 2), SZER * s - szer - 2)
            py = max(2, y * s - 18)
            r = pygame.Rect(px, py, szer, 16)
            while any(r.colliderect(z) for z in zajete) and r.top > 4:
                r.top -= 15
            zajete.append(r)
            plakietka = pygame.Surface(r.size, pygame.SRCALPHA)
            plakietka.fill((16, 12, 20, 150))
            self.ekran.blit(plakietka, r.topleft)
            self._napis(tekst, r.x + 3, r.y + 1, kolor)

    def _etykiety_walki(self, e: dict) -> None:
        w = ekran.walka
        if not w:
            return
        g, wrog = w["gracz"], w["wrog"]
        s = SKALA_MAPY
        self._napis(f"{g.imie}  {g.hp}/{g.max_hp}", e["gracz"][0] * s, e["gracz"][1] * s - 6, (240, 220, 200))
        poziom = f" (poz. {getattr(wrog, 'poziom', 1)})"
        tekst = f"{wrog.nazwa}{poziom}  {max(0, wrog.hp)}/{wrog.max_hp}"
        x = min(e["wrog"][0] * s - 40, SZER * s - 12 - self.hud.szerokosc(tekst))
        self._napis(tekst, max(8, x), e["wrog"][1] * s - 6, (250, 200, 180))
        for x, y, tekst in e["liczby"]:
            self._napis(tekst, x * s, y * s, (255, 90, 70), duzy=True)

    def _pola_tytulowe(self):
        if self._tytul is None:
            from game.mapa import generuj_mape
            self._tytul = generuj_mape(1, seed=19)
        return self._tytul

    def _napis(self, tekst, x, y, kolor=TEKST, duzy=False) -> int:
        pow_ = (self.duzy if duzy else self.hud).render(tekst, kolor)
        self.ekran.blit(pow_, (x, y))
        return pow_.get_width()

    def _hud_tytulowy(self, r: pygame.Rect) -> None:
        self._napis("PRO RPG", r.x + 24, r.y + 22, ZLOTY, duzy=True)
        self._napis("Fantasy RPG po polsku — obóz, trwały świat, testy k20.", r.x + 24, r.y + 62)
        self._napis("Wybierz opcję w konsoli: kliknij ją albo wpisz numer i Enter.", r.x + 24, r.y + 96, PRZYGASZONY)
        self._napis("F2: dzień / zmierzch    F11: pełny ekran", r.x + 24, r.y + 120, PRZYGASZONY)

    def _hud(self, r: pygame.Rect, g) -> None:
        from game.karma import etykieta as etykieta_karmy
        from game.oboz import linia_surowcow

        x, y = r.x + 24, r.y + 20
        klasa = g.klasa + (f" · {g.podklasa}" if getattr(g, "podklasa", None) else "")
        szer = self._napis(g.imie, x, y - 4, ZLOTY, duzy=True)
        self._napis(f"{klasa}  ·  poz. {g.poziom}", x + szer + 14, y + 2, PRZYGASZONY)
        zloto = f"💰 {g.zloto} zł"
        self._napis(zloto, r.right - 24 - self.hud.szerokosc(zloto), y + 2, ZLOTY)

        y += 38
        self._napis("HP", x, y - 2, (230, 190, 190))
        pasek(self.ekran, x + 56, y, 300, g.hp / max(1, g.max_hp), (196, 52, 52))
        self._napis(f"{g.hp}/{g.max_hp}", x + 368, y - 2)
        if g.max_mana > 0:
            y += 24
            self._napis("MANA", x, y - 2, (190, 200, 240))
            pasek(self.ekran, x + 56, y, 300, g.mana / max(1, g.max_mana), (70, 110, 220))
            self._napis(f"{g.mana}/{g.max_mana}", x + 368, y - 2)

        from game import kalendarz, przetrwanie
        from game.obrona import opis_zagrozenia
        from game.osada import bilans_zywnosci, ikona_morale, osadnicy, srednie_morale

        y += 28
        biom = g.aktualny_biom or "—"
        self._napis(f"{kalendarz.opis_daty(g)}  ·  region [{g.region_x}, {g.region_y}] poz. {g.mapa_gen}  ·  {biom}", x, y)
        y += 21
        prowiant = f"  ·  prowiant {g.prowiant}" if not getattr(g, "w_obozie", True) else ""
        zagrozenie = opis_zagrozenia(g)
        kolor = (236, 120, 100) if "NAJAZD" in zagrozenie or "najazd" in zagrozenie else PRZYGASZONY
        self._napis(f"🍖 {g.surowce.get('zywnosc', 0)} ({bilans_zywnosci(g):+d}/dz.){prowiant}  ·  ⚔ {zagrozenie}", x, y, kolor)
        y += 21
        m = srednie_morale(g)
        zdrowie = przetrwanie.opis_stanu(g)
        self._napis(f"Osada: {len(osadnicy(g))} os. {ikona_morale(m)} {m:.0f}  ·  {etykieta_karmy(g)}  ·  {zdrowie}",
                    x, y, PRZYGASZONY)
        y += 21
        self._napis(linia_surowcow(g), x, y, PRZYGASZONY)
        if ekran.skroty:
            podp = "strzałki: ruch  ·  spacja: zbadaj  ·  F2: noc"
        else:
            podp = "kliknij opcję albo wpisz numer  ·  F2: noc  ·  F11: pełny ekran"
        self._napis(podp, x, r.bottom - 30, (120, 112, 104))


def _osada_do_rysunku(gracz) -> tuple[int, int, int]:
    from game.oboz import poziom_budynku
    return (int(getattr(gracz, "chaty", 0) or 0), poziom_budynku(gracz, "palisada"), poziom_budynku(gracz, "wieza"))


def uruchom_w_oknie(funkcja) -> None:
    """Uruchamia pętlę gry w oknie; po wyjściu przywraca print/input/os.system."""
    okno = Okno()
    stare = builtins.print, builtins.input, os.system
    builtins.print, builtins.input, os.system = okno.print, okno.input, okno.system
    ekran.graficzny = True
    try:
        funkcja()
    except (OknoZamkniete, KeyboardInterrupt, EOFError):
        pass
    finally:
        builtins.print, builtins.input, os.system = stare
        ekran.graficzny = False
        ekran.ustaw_gracza(None)
        pygame.quit()
