"""Trwały świat: siatka regionów, biomy, punkty orientacyjne, mgła wojny.

Region jest identyfikowany parą współrzędnych ``(region_x, region_y)``.
Raz wygenerowany region zostaje w ``gracz.regiony`` — można do niego wrócić
i zastać te same pola, odkrycia i zużyte miejsca zbierania.
Trudność (``mapa_gen``) wynika z odległości od regionu startowego (0, 0).
"""

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

# Szansa, że region poza startowym ma legowisko bossa (ok. co trzeci region).
_SZANSA_BOSSA = 0.34

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


def poziom_regionu(rx: int, ry: int) -> int:
    """Trudność regionu = 1 + odległość (Chebyshev) od regionu startowego."""
    return 1 + max(abs(int(rx)), abs(int(ry)))


def _ziarno_regionu(seed: int, rx: int, ry: int) -> int:
    """Powtarzalne ziarno dla regionu — ten sam świat po powrocie i po wczytaniu."""
    return (int(seed) * 1_000_003 + int(rx) * 73_856_093 + int(ry) * 19_349_663) & 0x7FFF_FFFF


def generuj_mape(mapa_gen: int = 1, seed: int = 0, rx: int = 0, ry: int = 0) -> list[list[dict]]:
    """Tworzy region ROZMIAR×ROZMIAR.

    Układ zależy od ``seed`` postaci i współrzędnych regionu, więc jest
    powtarzalny przy powrocie, ale inny w każdej nowej rozgrywce.
    """
    rng = random.Random(_ziarno_regionu(seed, rx, ry))
    biomy = _siatka_biomow(rng)
    startowy = (rx == 0 and ry == 0)
    pola: list[list[dict]] = []
    for y in range(ROZMIAR):
        wiersz = []
        for x in range(ROZMIAR):
            punkt = None
            if startowy and x == SRODEK and y == SRODEK:
                punkt = "obóz"
            elif rng.random() < 0.13:
                punkt = rng.choice(_PUNKTY_LOSOWE)
            wiersz.append(_puste_pole(biomy[y][x], punkt))
        pola.append(wiersz)

    if not startowy and rng.random() < _SZANSA_BOSSA:
        bx, by = rng.randint(0, ROZMIAR - 1), rng.randint(0, ROZMIAR - 1)
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


# ------------------------------------------------------------------ #
#  Trwały świat — słownik regionów                                     #
# ------------------------------------------------------------------ #

def klucz_regionu(rx: int, ry: int) -> str:
    """Klucz regionu w słowniku (string, bo zapis idzie do JSON)."""
    return f"{int(rx)},{int(ry)}"


def _regiony(gracz: Gracz) -> dict[str, list[list[dict]]]:
    mapy = getattr(gracz, "regiony", None)
    if not isinstance(mapy, dict):
        mapy = {}
        gracz.regiony = mapy
    return mapy


def region_pola(gracz: Gracz, rx: int, ry: int) -> list[list[dict]]:
    """Zwraca siatkę regionu — generuje ją tylko przy pierwszej wizycie."""
    mapy = _regiony(gracz)
    klucz = klucz_regionu(rx, ry)
    pola = mapy.get(klucz)
    if not _siatka_poprawna(pola):
        pola = generuj_mape(poziom_regionu(rx, ry), getattr(gracz, "seed", 0), rx, ry)
        mapy[klucz] = pola
    return pola


def liczba_regionow(gracz: Gracz) -> int:
    """Ile regionów gracz odwiedził (rozmiar trwałego świata)."""
    return len(_regiony(gracz))


def _siatka_poprawna(pola) -> bool:
    return bool(
        pola
        and len(pola) == ROZMIAR
        and pola[0]
        and len(pola[0]) == ROZMIAR
    )


def zapewnij_mape(gracz: Gracz) -> None:
    """Gwarantuje spójny stan świata: seed, współrzędne regionu i bieżąca siatka.

    Obsługuje też stare zapisy sprzed trwałego świata — ich jedyna mapa ląduje
    w słowniku regionów na pozycji odpowiadającej dawnemu ``mapa_gen``.
    """
    if not getattr(gracz, "seed", 0):
        gracz.seed = random.randrange(1, 2 ** 31)

    mapy = _regiony(gracz)
    ma_wspolrzedne = hasattr(gracz, "region_x") and hasattr(gracz, "region_y")

    if not mapy and not ma_wspolrzedne:
        # Migracja starego zapisu: jedna mapa, tylko numer regionu.
        stary_gen = max(1, int(getattr(gracz, "mapa_gen", 1) or 1))
        gracz.region_x, gracz.region_y = stary_gen - 1, 0
        stara_siatka = getattr(gracz, "mapa_pola", None)
        if _siatka_poprawna(stara_siatka):
            mapy[klucz_regionu(gracz.region_x, gracz.region_y)] = stara_siatka
        else:
            gracz.mapa_x, gracz.mapa_y = SRODEK, SRODEK

    gracz.region_x = int(getattr(gracz, "region_x", 0) or 0)
    gracz.region_y = int(getattr(gracz, "region_y", 0) or 0)
    gracz.mapa_gen = poziom_regionu(gracz.region_x, gracz.region_y)
    gracz.mapa_pola = region_pola(gracz, gracz.region_x, gracz.region_y)

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
        f"  🗺  REGION [{gracz.region_x}, {gracz.region_y}]  ·  poziom {gracz.mapa_gen}"
        f"   pole ({gracz.mapa_x}, {gracz.mapa_y})"
        f"   odkryte {liczba_odkrytych(gracz)}/{liczba_pol()}"
    )
    print(f"  📍  {opis_regionu(gracz)}")
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
        rx = int(getattr(gracz, "region_x", 0)) + nx // ROZMIAR
        ry = int(getattr(gracz, "region_y", 0)) + ny // ROZMIAR
        if czy_region_znany(gracz, rx, ry):
            return f"🧭 znany region [{rx}, {ry}]"
        return f"🌄 nowy region [{rx}, {ry}]"
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
    """Przesuwa gracza. Zwraca True, gdy przekroczono krawędź i zmieniono region.

    Region po drugiej stronie krawędzi jest trwały: wyjście na wschód i powrót
    na zachód wraca dokładnie tam, skąd się wyszło.
    """
    zapewnij_mape(gracz)
    nx = gracz.mapa_x + dx
    ny = gracz.mapa_y + dy
    nowa = False

    if nx < 0 or ny < 0 or nx >= ROZMIAR or ny >= ROZMIAR:
        # Krawędź: przechodzimy do sąsiedniego regionu, wchodząc od strony przeciwnej.
        gracz.region_x += nx // ROZMIAR
        gracz.region_y += ny // ROZMIAR
        gracz.mapa_x = nx % ROZMIAR
        gracz.mapa_y = ny % ROZMIAR
        gracz.mapa_gen = poziom_regionu(gracz.region_x, gracz.region_y)
        gracz.mapa_pola = region_pola(gracz, gracz.region_x, gracz.region_y)
        nowa = True
    else:
        gracz.mapa_x = nx
        gracz.mapa_y = ny

    odkryj_pole(gracz)
    return nowa


def czy_region_znany(gracz: Gracz, rx: int, ry: int) -> bool:
    """Czy gracz był już w tym regionie (jest w słowniku świata)."""
    return klucz_regionu(rx, ry) in _regiony(gracz)


def opis_regionu(gracz: Gracz) -> str:
    """Krótki opis położenia regionu względem obozu — dla nagłówka mapy."""
    rx, ry = int(getattr(gracz, "region_x", 0)), int(getattr(gracz, "region_y", 0))
    if rx == 0 and ry == 0:
        return "region startowy (obóz)"
    czesci = []
    if ry < 0:
        czesci.append(f"{abs(ry)}× na północ")
    elif ry > 0:
        czesci.append(f"{ry}× na południe")
    if rx < 0:
        czesci.append(f"{abs(rx)}× na zachód")
    elif rx > 0:
        czesci.append(f"{rx}× na wschód")
    return " i ".join(czesci) + " od obozu"
