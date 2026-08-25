"""Trwała mapa regionu: biomy, punkty orientacyjne, mgła wojny."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.ikony import (
    GRACZ_MAPA,
    IKONY_BIOM,
    IKONY_PUNKT,
    MGŁA,
    etykieta_biomu,
    etykieta_punktu,
    glif_pola as _glif_ikona,
)

if TYPE_CHECKING:
    from game.player import Gracz

ROZMIAR = 9
SRODEK = ROZMIAR // 2
_BOSS_CO_ILE_MAP = 3

BIOMY_NAZWY: tuple[str, ...] = (
    "równiny",
    "ruiny",
    "las",
    "bagna",
    "wzgórza",
    "kanion",
)

_KIERUNKI: dict[str, tuple[str, int, int]] = {
    "1": ("północ", 0, -1),
    "2": ("zachód", -1, 0),
    "3": ("wschód", 1, 0),
    "4": ("południe", 0, 1),
}

_PUNKTY_LOSOWE = ("karczma", "kuźnia", "świątynia", "jaskinia")
PUNKTY_MITYCZNE = ("portal", "leze_smoka", "latajaca_wyspa")


def _puste_pole(biom: str, punkt: str | None = None) -> dict:
    return {
        "biom": biom,
        "odkryte": False,
        "odwiedzone": False,
        "zbierania": 0,
        "punkt": punkt,
    }


def _siatka_biomow(rng: random.Random) -> list[list[str]]:
    """Klastry biomów (Voronoi) — region wygląda jak mapa, nie jak szum."""
    n = ROZMIAR
    ile = rng.randint(4, 6)
    wybrane = rng.sample(list(BIOMY_NAZWY), k=ile)
    ziarna = [
        (rng.randint(0, n - 1), rng.randint(0, n - 1), biom)
        for biom in wybrane
    ]
    siatka: list[list[str]] = []
    for y in range(n):
        wiersz: list[str] = []
        for x in range(n):
            naj = min(ziarna, key=lambda z: (z[0] - x) ** 2 + (z[1] - y) ** 2)
            wiersz.append(naj[2])
        siatka.append(wiersz)
    return siatka


def liczba_pol() -> int:
    return ROZMIAR * ROZMIAR


def generuj_mape(mapa_gen: int) -> list[list[dict]]:
    """Tworzy nowy region ROZMIAR×ROZMIAR. Układ zależy od numeru mapy (powtarzalny)."""
    rng = random.Random(4242 + mapa_gen * 17)
    biomy = _siatka_biomow(rng)
    pola: list[list[dict]] = []
    for y in range(ROZMIAR):
        wiersz = []
        for x in range(ROZMIAR):
            punkt = None
            if mapa_gen == 1 and x == SRODEK and y == SRODEK:
                punkt = "obóz"
            elif rng.random() < 0.13:
                punkt = rng.choice(_PUNKTY_LOSOWE)
            wiersz.append(_puste_pole(biomy[y][x], punkt))
        pola.append(wiersz)

    if mapa_gen > 1 and mapa_gen % _BOSS_CO_ILE_MAP == 0:
        bx, by = rng.randint(0, ROZMIAR - 1), rng.randint(0, ROZMIAR - 1)
        if pola[by][bx]["punkt"] == "obóz":
            bx = (bx + 2) % ROZMIAR
        pola[by][bx]["punkt"] = "boss"

    if mapa_gen >= 2:
        szansa = 0.42 if mapa_gen < 5 else 0.58
        if rng.random() < szansa:
            wolne = [
                (x, y)
                for y in range(ROZMIAR)
                for x in range(ROZMIAR)
                if pola[y][x]["punkt"] not in ("obóz", "boss")
            ]
            if wolne:
                mx, my = rng.choice(wolne)
                pola[my][mx]["punkt"] = rng.choice(PUNKTY_MITYCZNE)

    if mapa_gen >= 2:
        szansa_miasta = 0.62 if mapa_gen < 4 else 0.82
        if rng.random() < szansa_miasta:
            wolne_miasto = [
                (x, y)
                for y in range(ROZMIAR)
                for x in range(ROZMIAR)
                if pola[y][x]["punkt"] not in ("obóz", "boss")
                and pola[y][x]["punkt"] not in PUNKTY_MITYCZNE
            ]
            if wolne_miasto:
                cx, cy = rng.choice(wolne_miasto)
                pola[cy][cx]["punkt"] = "miasto"

    return pola


def zapewnij_mape(gracz: Gracz) -> None:
    """Gwarantuje, że gracz ma wygenerowany region (stare zapisy też)."""
    pola = getattr(gracz, "mapa_pola", None)
    inny_rozmiar = bool(
        pola and (len(pola) != ROZMIAR or not pola[0] or len(pola[0]) != ROZMIAR)
    )
    if not pola or inny_rozmiar:
        gracz.mapa_pola = generuj_mape(getattr(gracz, "mapa_gen", 1))
        if inny_rozmiar:
            gracz.mapa_x = SRODEK
            gracz.mapa_y = SRODEK
    _przytnij_pozycje(gracz)
    odkryj_pole(gracz)


def _przytnij_pozycje(gracz: Gracz) -> None:
    gracz.mapa_x = max(0, min(ROZMIAR - 1, int(getattr(gracz, "mapa_x", SRODEK))))
    gracz.mapa_y = max(0, min(ROZMIAR - 1, int(getattr(gracz, "mapa_y", SRODEK))))


def pole_na(gracz: Gracz, x: int, y: int) -> dict:
    return gracz.mapa_pola[y][x]


def pole_gracza(gracz: Gracz) -> dict:
    zapewnij_mape(gracz)
    return pole_na(gracz, gracz.mapa_x, gracz.mapa_y)


def odkryj_pole(gracz: Gracz) -> None:
    """Oznacza aktualne pole jako odkryte. Mapa musi już istnieć."""
    pole = pole_na(gracz, gracz.mapa_x, gracz.mapa_y)
    pole["odkryte"] = True
    gracz.aktualny_biom = pole["biom"]


def liczba_odkrytych(gracz: Gracz) -> int:
    pola = getattr(gracz, "mapa_pola", None) or []
    return sum(1 for wiersz in pola for p in wiersz if p.get("odkryte"))


def _symbole_odkryte(gracz: Gracz) -> tuple[list[str], list[str]]:
    """Biomy i lokacje z odkrytych pól — w kolejności katalogu ikon."""
    biomy: set[str] = set()
    punkty: set[str] = set()
    for wiersz in getattr(gracz, "mapa_pola", None) or []:
        for pole in wiersz:
            if not pole.get("odkryte"):
                continue
            biom = pole.get("biom")
            if biom:
                biomy.add(biom)
            punkt = pole.get("punkt")
            if punkt:
                punkty.add(punkt)
    lista_biomow = [n for n in IKONY_BIOM if n in biomy]
    lista_punktow = [k for k in IKONY_PUNKT if k in punkty]
    return lista_biomow, lista_punktow


def glif_pola(gracz: Gracz, x: int, y: int) -> str:
    pole = pole_na(gracz, x, y)
    return _glif_ikona(
        pole.get("biom", "równiny"),
        pole.get("punkt"),
        ty=(x == gracz.mapa_x and y == gracz.mapa_y),
        odkryte=bool(pole.get("odkryte")),
    )


def rysuj_mape(gracz: Gracz) -> None:
    """Rysuje siatkę regionu z legendą ikon."""
    zapewnij_mape(gracz)
    pole = pole_gracza(gracz)
    punkt = pole.get("punkt")
    miejsce = f"  ·  {opis_punktu(punkt)}" if punkt else ""
    print(
        f"  🗺  MAPA #{gracz.mapa_gen}   pole ({gracz.mapa_x}, {gracz.mapa_y})"
        f"   odkryte {liczba_odkrytych(gracz)}/{liczba_pol()}"
    )
    print(f"  Biom: {etykieta_biomu(pole['biom'])}{miejsce}")
    print()
    naglowek = "     " + " ".join(f"{x:>2}" for x in range(ROZMIAR))
    print(naglowek)
    for y in range(ROZMIAR):
        komorki = " ".join(f"{glif_pola(gracz, x, y):>2}" for x in range(ROZMIAR))
        print(f"  {y}  {komorki}")
    print()
    print(f"  {GRACZ_MAPA} ty   {MGŁA} nieodkryte")
    biomy, punkty = _symbole_odkryte(gracz)
    if biomy:
        print("  " + "   ".join(etykieta_biomu(n) for n in biomy))
    if punkty:
        print("  " + "   ".join(opis_punktu(k) for k in punkty))
    print()


def opis_punktu(punkt: str | None) -> str:
    if not punkt:
        return ""
    nazwy = {
        "obóz": "obóz",
        "karczma": "karczma",
        "kuźnia": "kuźnia",
        "świątynia": "świątynia",
        "jaskinia": "jaskinia",
        "boss": "legowisko bossa",
        "portal": "portal do innego wymiaru",
        "leze_smoka": "leże smoka",
        "latajaca_wyspa": "latająca wyspa",
        "miasto": "miasto za murami",
    }
    return etykieta_punktu(punkt, nazwy.get(punkt, punkt))


def etykieta_kierunku(gracz: Gracz, dx: int, dy: int) -> str:
    """Co widać w danym kierunku (biom, jeśli pole odkryte)."""
    nx, ny = gracz.mapa_x + dx, gracz.mapa_y + dy
    if nx < 0 or ny < 0 or nx >= ROZMIAR or ny >= ROZMIAR:
        return "🌄 nowy region"
    pole = pole_na(gracz, nx, ny)
    if not pole.get("odkryte"):
        return f"{MGŁA} ???"
    if pole.get("punkt") == "obóz":
        return etykieta_punktu("obóz", "obóz")
    txt = etykieta_biomu(pole["biom"])
    punkt = pole.get("punkt")
    if punkt:
        txt += f", {opis_punktu(punkt)}"
    return txt


def kierunki() -> dict[str, tuple[str, int, int]]:
    return _KIERUNKI


def przesun_gracza(gracz: Gracz, dx: int, dy: int) -> bool:
    """
    Przesuwa gracza. Zwraca True, gdy przekroczono krawędź i wylosowano nowy region.
    """
    zapewnij_mape(gracz)
    nx = gracz.mapa_x + dx
    ny = gracz.mapa_y + dy
    nowa = False
    if nx < 0 or ny < 0 or nx >= ROZMIAR or ny >= ROZMIAR:
        gracz.mapa_gen += 1
        gracz.mapa_x = nx % ROZMIAR
        gracz.mapa_y = ny % ROZMIAR
        gracz.mapa_pola = generuj_mape(gracz.mapa_gen)
        nowa = True
    else:
        gracz.mapa_x = nx
        gracz.mapa_y = ny
    odkryj_pole(gracz)
    return nowa
