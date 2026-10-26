"""Ruch osadników po placu: siatka, ścieżki A*, rezerwacje pól i rytm dnia.

Scena osady (``grafika.widok_osady``) kiedyś kręciła osadników w kółko wokół
stanowiska — bez stanu, bez przeszkód, bez celu. Tu osadnik naprawdę idzie:
ma pole, na którym stoi, ścieżkę wokół budynków i jedno wolne miejsce przy
celu. Moduł nie zna pygame ani stanu gry — dostaje zbiór zablokowanych pól
i polecenia „idź tu”, więc daje się testować bez okna.

Czas sceny jest osobny od dnia świata (ten liczy ``game.swiat``): jedna doba
sceny to ``DLUGOSC_DNIA`` sekund, a w niej rytm — praca, wieczór przy ogniu,
sen przy chacie. Pora dnia daje też światło dla grafiki (``swiatlo``).

Pole to para całkowitych ``(x, y)`` na siatce ``SIATKA × SIATKA``; osadnik
stoi na środku pola, a w drodze ma pozycję ułamkową.
"""
from __future__ import annotations

import heapq
import math
from collections import deque

SIATKA = 7
PREDKOSC = 1.25            # pól na sekundę
DLUGOSC_DNIA = 90.0        # sekund sceny na dobę
CZEKANIE_NA_PRZEJSCIE = 0.8  # po tylu sekundach postoju szukamy obejścia

Pole = tuple[int, int]

# Ułamki doby, w których zaczyna się kolejna część dnia.
FAZY: tuple[tuple[float, str], ...] = (
    (0.00, "rano"),
    (0.10, "praca"),
    (0.62, "wieczor"),
    (0.82, "noc"),
)

_KIERUNKI: tuple[Pole, ...] = (
    (0, -1), (1, 0), (0, 1), (-1, 0),          # najpierw proste — przewidywalna kolejność
    (1, -1), (1, 1), (-1, 1), (-1, -1),
)

# Gdzie stawać przy budynku: od frontu (na ekranie „w dół”), potem z boków i z tyłu.
_PRZY_CELU: tuple[Pole, ...] = (
    (0, 1), (1, 0), (-1, 0), (0, -1),
    (1, 1), (-1, 1), (1, -1), (-1, -1),
)


# ------------------------------------------------------------------ #
#  Rytm dnia                                                           #
# ------------------------------------------------------------------ #

def ulamek_doby(t: float, dlugosc: float = DLUGOSC_DNIA) -> float:
    return (t % dlugosc) / dlugosc


def faza(t: float, dlugosc: float = DLUGOSC_DNIA) -> str:
    """Część doby w chwili ``t``: rano, praca, wieczor, noc."""
    u = ulamek_doby(t, dlugosc)
    nazwa = FAZY[0][1]
    for start, klucz in FAZY:
        if u >= start:
            nazwa = klucz
    return nazwa


def swiatlo(t: float, dlugosc: float = DLUGOSC_DNIA) -> float:
    """Jasność 0 (głucha noc) … 1 (południe), płynna — do oświetlenia sceny."""
    u = ulamek_doby(t, dlugosc)
    # Cosinus ustawiony tak, żeby najciemniej było w środku nocy (u ≈ 0,91),
    # a najjaśniej w środku pracy; wzmocniony i obcięty, bo dzień ma trwać
    # dłużej niż noc, a południe — być płaskim szczytem, nie ostrym wierzchołkiem.
    surowe = 0.5 - 0.5 * math.cos((u + 0.09) * math.tau)
    return max(0.0, min(1.0, 0.08 + 1.2 * surowe))


def zadanie(osadnik: dict, stanowisko: Pole, dom: Pole, ognisko: Pole,
            tawerna: Pole | None, pora: str) -> tuple[Pole, str]:
    """Dokąd i po co idzie osadnik o tej porze: ``(pole, czynność)``.

    Chory nigdy nie opuszcza okolic domu. Wieczorem część osadników siada przy
    ogniu, część idzie do tawerny (o ile stoi) — wybór jest stały dla imienia,
    żeby nie zmieniali zdania co klatkę.
    """
    if osadnik.get("chory"):
        return dom, "odpoczynek"
    if pora in ("rano", "praca"):
        return stanowisko, "praca" if pora == "praca" else "marsz"
    if pora == "wieczor":
        if tawerna is not None and los_imienia(osadnik.get("imie", "")) % 2 == 0:
            return tawerna, "tawerna"
        return ognisko, "ognisko"
    return dom, "sen"


def los_imienia(imie: str) -> int:
    """Stała liczba z imienia (``hash`` zmienia się między uruchomieniami)."""
    return sum((i + 1) * ord(c) for i, c in enumerate(imie))


# ------------------------------------------------------------------ #
#  Siatka i ścieżki                                                    #
# ------------------------------------------------------------------ #

class Siatka:
    def __init__(self, rozmiar: int = SIATKA, zablokowane: set[Pole] | None = None) -> None:
        self.rozmiar = rozmiar
        self.zablokowane: set[Pole] = set(zablokowane or ())

    def w_siatce(self, p: Pole) -> bool:
        return 0 <= p[0] < self.rozmiar and 0 <= p[1] < self.rozmiar

    def wolne(self, p: Pole, dodatkowo: set[Pole] | frozenset = frozenset()) -> bool:
        return self.w_siatce(p) and p not in self.zablokowane and p not in dodatkowo

    def sasiedzi(self, p: Pole, dodatkowo: set[Pole] | frozenset = frozenset()):
        """Sąsiednie wolne pola z kosztem; po skosie tylko, gdy nie ścina rogu."""
        for dx, dy in _KIERUNKI:
            n = (p[0] + dx, p[1] + dy)
            if not self.wolne(n, dodatkowo):
                continue
            if dx and dy and not (self.wolne((p[0] + dx, p[1]), dodatkowo)
                                  and self.wolne((p[0], p[1] + dy), dodatkowo)):
                continue
            yield n, (math.sqrt(2) if dx and dy else 1.0)

    def najblizsze_wolne(self, p: Pole) -> Pole | None:
        """Najbliższe wolne pole (łącznie z ``p``), gdy ``p`` samo jest zajęte."""
        if self.wolne(p):
            return p
        for r in range(1, self.rozmiar + 1):
            ring = [(p[0] + dx, p[1] + dy)
                    for dx in range(-r, r + 1) for dy in range(-r, r + 1)
                    if max(abs(dx), abs(dy)) == r]
            for c in sorted(ring, key=lambda c: (abs(c[0] - p[0]) + abs(c[1] - p[1]), c)):
                if self.wolne(c):
                    return c
        return None


def _oktylowa(a: Pole, b: Pole) -> float:
    dx, dy = abs(a[0] - b[0]), abs(a[1] - b[1])
    return (dx + dy) + (math.sqrt(2) - 2) * min(dx, dy)


def sciezka(siatka: Siatka, start: Pole, cel: Pole,
            dodatkowo: set[Pole] | frozenset = frozenset()) -> list[Pole] | None:
    """Najkrótsza ścieżka A* od ``start`` do ``cel`` (bez ``start``), albo None.

    ``dodatkowo`` to pola chwilowo zajęte (inni osadnicy), omijane tylko w tym
    wyszukiwaniu. Przy remisach wygrywa mniejsze pole, więc wynik jest stały.
    """
    if start == cel:
        return []
    if not siatka.wolne(cel, dodatkowo):
        return None
    otwarte: list[tuple[float, float, Pole]] = [(_oktylowa(start, cel), 0.0, start)]
    koszt = {start: 0.0}
    skad: dict[Pole, Pole] = {}
    zamkniete: set[Pole] = set()
    while otwarte:
        _, g, p = heapq.heappop(otwarte)
        if p in zamkniete:
            continue
        if p == cel:
            wynik = [p]
            while wynik[-1] in skad:
                wynik.append(skad[wynik[-1]])
            wynik.reverse()
            return wynik[1:]
        zamkniete.add(p)
        for n, c in siatka.sasiedzi(p, dodatkowo):
            ng = g + c
            if ng < koszt.get(n, 1e9):
                koszt[n] = ng
                skad[n] = p
                heapq.heappush(otwarte, (ng + _oktylowa(n, cel), ng, n))
    return None


# ------------------------------------------------------------------ #
#  Postacie                                                            #
# ------------------------------------------------------------------ #

class Postac:
    def __init__(self, imie: str, pole: Pole) -> None:
        self.imie = imie
        self.x = float(pole[0])
        self.y = float(pole[1])
        self.pole: Pole = pole                  # ostatnie osiągnięte pole
        self.nastepne: Pole | None = None       # pole, do którego właśnie idzie
        self.trasa: list[Pole] = []
        self.cel: Pole | None = None            # pole docelowe (miejsce do stania)
        self.zamiar: Pole | None = None         # pole, o które poproszono (np. budynek)
        self.czynnosc = "stoi"
        self.dystans = 0.0                      # przebyta droga — napędza animację kroku
        self.postoj = 0.0                       # jak długo czeka na zajęte pole
        self.kierunek = (0.0, 1.0)

    @property
    def idzie(self) -> bool:
        return self.nastepne is not None


class Ruch:
    """Wszystkie postacie placu, ich rezerwacje i krok symulacji."""

    def __init__(self, rozmiar: int = SIATKA) -> None:
        self.siatka = Siatka(rozmiar)
        self.postacie: dict[str, Postac] = {}
        self._rez: dict[Pole, str] = {}         # pole → kto na nim stoi albo do niego idzie

    # -- zarządzanie ------------------------------------------------- #

    def dodaj(self, imie: str, pole: Pole) -> Postac:
        pole = self._wolne_miejsce(pole, imie) or pole
        p = Postac(imie, pole)
        self.postacie[imie] = p
        self._rez[pole] = imie
        return p

    def usun(self, imie: str) -> None:
        p = self.postacie.pop(imie, None)
        if p is None:
            return
        for pole in [k for k, v in self._rez.items() if v == imie]:
            del self._rez[pole]

    def przebuduj(self, zablokowane: set[Pole]) -> None:
        """Nowy układ przeszkód (ktoś postawił budynek). Zepchnięci z pola schodzą na wolne."""
        if zablokowane == self.siatka.zablokowane:
            return
        self.siatka.zablokowane = set(zablokowane)
        for p in self.postacie.values():
            zajete = p.pole in zablokowane or (p.nastepne in zablokowane if p.nastepne else False)
            if zajete:
                self._rez = {k: v for k, v in self._rez.items() if v != p.imie}
                nowe = self._wolne_miejsce(p.pole, p.imie) or self.siatka.najblizsze_wolne(p.pole)
                if nowe is not None:
                    p.pole, p.x, p.y = nowe, float(nowe[0]), float(nowe[1])
                    self._rez[nowe] = p.imie
                p.nastepne, p.trasa = None, []
            p.cel = p.zamiar = None             # cele przeliczy następne polecenie

    # -- polecenia --------------------------------------------------- #

    def ustaw_cel(self, imie: str, pole: Pole, czynnosc: str = "stoi") -> None:
        """Poślij postać w okolice ``pole`` (może być zajęte budynkiem)."""
        p = self.postacie[imie]
        p.czynnosc = czynnosc
        if p.zamiar == pole and p.cel is not None:
            return
        miejsce = self._wolne_miejsce(pole, imie)
        p.zamiar = pole
        if miejsce is None:
            return
        start = p.nastepne or p.pole
        trasa = sciezka(self.siatka, start, miejsce)
        if trasa is None:
            return
        self._zwolnij_cel(p)
        p.cel = miejsce
        p.trasa = trasa
        if not p.idzie and not trasa:
            self._rez[p.pole] = imie

    def _zwolnij_cel(self, p: Postac) -> None:
        if p.cel is not None and self._rez.get(p.cel) == p.imie and p.cel not in (p.pole, p.nastepne):
            del self._rez[p.cel]

    def _wolne_miejsce(self, pole: Pole, imie: str) -> Pole | None:
        """Najbliższe pole przy ``pole``, na którym nikt inny nie stoi i do którego nikt nie idzie."""
        cudze = {k for k, v in self._rez.items() if v != imie}
        cudze |= {q.cel for q in self.postacie.values() if q.imie != imie and q.cel is not None}
        kolejka = deque([pole])
        widziane = {pole}
        while kolejka:
            p = kolejka.popleft()
            if self.siatka.wolne(p) and p not in cudze:
                return p
            for dx, dy in _PRZY_CELU:
                n = (p[0] + dx, p[1] + dy)
                if n not in widziane and self.siatka.w_siatce(n):
                    widziane.add(n)
                    # Przez zablokowane pole przechodzimy tylko „okiem” — szukamy brzegu budynku.
                    kolejka.append(n)
        return None

    # -- krok -------------------------------------------------------- #

    def aktualizuj(self, dt: float) -> None:
        for imie in sorted(self.postacie):
            self._krok(self.postacie[imie], dt)

    def _krok(self, p: Postac, dt: float) -> None:
        zostalo = PREDKOSC * dt
        while zostalo > 1e-9:
            if p.nastepne is None:
                if not p.trasa:
                    return
                kolejne = p.trasa[0]
                if self._rez.get(kolejne, p.imie) != p.imie:
                    p.postoj += dt
                    if p.postoj >= CZEKANIE_NA_PRZEJSCIE:
                        self._obejdz(p)
                        p.postoj = 0.0
                    return
                p.postoj = 0.0
                p.trasa.pop(0)
                p.nastepne = kolejne
                self._rez[kolejne] = p.imie
            cx, cy = float(p.nastepne[0]), float(p.nastepne[1])
            dx, dy = cx - p.x, cy - p.y
            d = math.hypot(dx, dy)
            if d > 1e-9:
                p.kierunek = (dx / d, dy / d)
            if d <= zostalo:
                p.x, p.y = cx, cy
                p.dystans += d
                zostalo -= d
                if self._rez.get(p.pole) == p.imie and p.pole != p.nastepne:
                    del self._rez[p.pole]
                p.pole, p.nastepne = p.nastepne, None
            else:
                p.x += dx / d * zostalo
                p.y += dy / d * zostalo
                p.dystans += zostalo
                zostalo = 0.0

    def _obejdz(self, p: Postac) -> None:
        """Ktoś blokuje drogę: policz trasę z pominięciem pól zajętych przez innych."""
        if p.cel is None:
            return
        cudze = {k for k, v in self._rez.items() if v != p.imie}
        trasa = sciezka(self.siatka, p.pole, p.cel, cudze)
        if trasa is not None:
            p.trasa = trasa
            return
        # Wąskie przejście i nikt nie może się wyminąć. Ustępuje ten, którego
        # imię jest później w alfabecie — drugi dostaje wolną drogę.
        blokujacy = self._rez.get(p.trasa[0]) if p.trasa else None
        if blokujacy is None or p.imie < blokujacy:
            return
        for n, _ in self.siatka.sasiedzi(p.pole, cudze):
            dalej = sciezka(self.siatka, n, p.cel)
            if dalej is not None and p.trasa and n != p.trasa[0]:
                p.trasa = [n] + dalej
                return

    # -- odczyt ------------------------------------------------------ #

    def pozycja(self, imie: str) -> tuple[float, float]:
        p = self.postacie[imie]
        return (p.x, p.y)

    def dotarl(self, imie: str) -> bool:
        p = self.postacie[imie]
        return not p.idzie and not p.trasa and (p.cel is None or p.pole == p.cel)
