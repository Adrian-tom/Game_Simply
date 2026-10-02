"""Relacje między osadnikami: przyjaźnie, waśnie, pary i narodziny.

Osadnicy mieli imiona, morale i cechy, ale żyli obok siebie, a nie ze sobą.
Ten moduł robi z listy liczb grupę ludzi, o których coś wiesz: kto z kim
pracuje, kto kogo nie znosi, kto się z kim związał i czyja śmierć kogo złamie.

Jak to działa:

* Każda para ma **więź** od −100 (nienawiść) do +100 (oddanie). Rośnie, gdy
  ludzie robią to samo i mają zgodne charaktery; spada przy sprzecznych
  cechach, głodzie i zimnie — bieda kłóci ludzi.
* Więź wpływa na **morale**: przyjaciel obok podnosi, wróg ciągnie w dół.
  Dzięki temu nie jest to ozdoba, tylko coś, co trzeba brać pod uwagę przy
  rozdzielaniu zajęć.
* Odejście albo śmierć osadnika **uderza w jego bliskich** — mocniej niż
  ogólna żałoba. Tu zaczyna boleć utrata konkretnego człowieka.
* Dwie osoby z więzią ≥ 80 mogą zostać **parą**; para w osadzie z zapasem
  jedzenia i wolną chatą może doczekać się **dziecka** (nowy osadnik za darmo).

Relacje trzymamy w ``gracz.wiezi``: {"Nessa|Torin": 42.0} — klucz to para imion
posortowana alfabetycznie, więc jedna wartość opisuje związek w obie strony
i nie da się doprowadzić do stanu, w którym A lubi B, ale B o tym nie wie.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from game.player import Gracz

# Progi, po przekroczeniu których więź zmienia nazwę i daje o sobie znać.
PROG_PRZYJAZN = 45.0
PROG_PARA = 80.0
PROG_WASN = -40.0
PROG_WROGOSC = -75.0

MIN_WIEZ, MAX_WIEZ = -100.0, 100.0

# Ile dni rodzina odpoczywa po narodzinach. Bez tej przerwy jedna zgrana para
# rodzi w dwa lata pół osady i darmowi osadnicy przestają być nagrodą.
PRZERWA_NARODZIN = 90

# Cechy, które się zazębiają (razem im lepiej) i takie, które się tną.
_ZGODNE = {
    frozenset(("pracowity", "pracowity")): 1.4,
    frozenset(("wesoly", "skromny")): 1.3,
    frozenset(("wesoly", "gadatliwy")): 1.3,
    frozenset(("odwazny", "odwazny")): 1.2,
    frozenset(("skromny", "skromny")): 1.1,
}

_SPRZECZNE = {
    frozenset(("pracowity", "leniwy")): -1.6,     # jeden haruje, drugi patrzy
    frozenset(("odwazny", "tchorzliwy")): -1.2,
    frozenset(("zarloczny", "skromny")): -0.9,
    frozenset(("gadatliwy", "chorowity")): -0.6,
    frozenset(("leniwy", "leniwy")): -0.4,        # obaj liczą, że zrobi ten drugi
}


def _klucz(a: str, b: str) -> str:
    """Para imion jako jeden klucz — zawsze w tej samej kolejności."""
    return "|".join(sorted((a, b)))


def _mapa(gracz: Gracz) -> dict[str, float]:
    if getattr(gracz, "wiezi", None) is None:
        gracz.wiezi = {}
    return gracz.wiezi


def wiez(gracz: Gracz, a: str, b: str) -> float:
    if a == b:
        return 0.0
    return float(_mapa(gracz).get(_klucz(a, b), 0.0))


def zmien(gracz: Gracz, a: str, b: str, ile: float) -> float:
    if a == b:
        return 0.0
    m = _mapa(gracz)
    k = _klucz(a, b)
    obecna = m.get(k, 0.0)

    # Im bliżej skrajności, tym wolniej. Bez tego dryf ~2 pkt/dzień wysyca
    # każdą parę na 100 w ciągu kilku tygodni i po roku wszyscy kochają
    # wszystkich jednakowo — relacje przestają cokolwiek różnicować.
    if (ile > 0) == (obecna >= 0):
        ile *= max(0.12, 1.0 - abs(obecna) / 110.0)

    # Zaokrąglamy od razu, bo to ta wartość ląduje w zapisie gry i ona decyduje
    # o progach. Gdyby próg sprawdzać przed zaokrągleniem, więź 79,96 przeszłaby
    # test „poniżej progu", a zapisała się jako 80,0 — i wyłączność pary nigdy
    # więcej by się nie uruchomiła, bo `obecna` byłaby już ponad progiem.
    nowa = round(max(MIN_WIEZ, min(MAX_WIEZ, obecna + ile)), 1)

    # Para to związek wyłączny: kto ma partnera, nie przekroczy progu z nikim
    # innym. Bez tego ośmioosobowa osada robi się jednym wielkim małżeństwem.
    if nowa >= PROG_PARA and obecna < PROG_PARA:
        if partner(gracz, a) or partner(gracz, b):
            nowa = PROG_PARA - 1.0

    m[k] = nowa
    return nowa


def nazwa_wiezi(w: float) -> str:
    if w >= PROG_PARA:
        return "para"
    if w >= PROG_PRZYJAZN:
        return "przyjaciele"
    if w <= PROG_WROGOSC:
        return "wrogowie"
    if w <= PROG_WASN:
        return "waśń"
    return "obojętni"


def bliscy(gracz: Gracz, imie: str, prog: float = PROG_PRZYJAZN) -> list[tuple[str, float]]:
    """Osadnicy, z którymi `imie` ma więź co najmniej `prog`, od najmocniejszej."""
    wynik = []
    for klucz, w in _mapa(gracz).items():
        a, b = klucz.split("|")
        if imie == a or imie == b:
            if w >= prog:
                wynik.append((b if imie == a else a, w))
    return sorted(wynik, key=lambda p: -p[1])


def skonfliktowani(gracz: Gracz, imie: str, prog: float = PROG_WASN) -> list[tuple[str, float]]:
    wynik = []
    for klucz, w in _mapa(gracz).items():
        a, b = klucz.split("|")
        if (imie == a or imie == b) and w <= prog:
            wynik.append((b if imie == a else a, w))
    return sorted(wynik, key=lambda p: p[1])


def partner(gracz: Gracz, imie: str) -> str | None:
    """Imię osoby, z którą `imie` tworzy parę, albo None."""
    for inne, w in bliscy(gracz, imie, PROG_PARA):
        return inne
    return None


def opis_relacji(gracz: Gracz, imie: str) -> str:
    """Jedno zdanie o relacjach osadnika — do karty osadnika w menu osady."""
    p = partner(gracz, imie)
    przyjaciele = [i for i, _ in bliscy(gracz, imie) if i != p]
    wrogowie = [i for i, _ in skonfliktowani(gracz, imie)]

    czesci = []
    if p:
        czesci.append(f"w parze z {p}")
    if przyjaciele:
        czesci.append("przyjaźni się z " + ", ".join(przyjaciele[:2]))
    if wrogowie:
        czesci.append("w waśni z " + ", ".join(wrogowie[:2]))
    return "; ".join(czesci) if czesci else "trzyma się na uboczu"


# ------------------------------------------------------------------ #
#  Dzień w osadzie
# ------------------------------------------------------------------ #

def _dryf_pary(gracz: Gracz, a: dict, b: dict, glodni: bool, zimno: bool) -> float:
    """O ile zmienia się więź między dwojgiem ludzi przez jeden dzień."""
    zmiana = 0.6   # samo mieszkanie razem powoli zbliża

    if a.get("zajecie") == b.get("zajecie") and a.get("zajecie") != "bezczynny":
        zmiana += 0.8   # wspólna robota zbliża najbardziej

    cechy = frozenset((a.get("cecha"), b.get("cecha")))
    zmiana += _ZGODNE.get(cechy, 0.0)
    zmiana += _SPRZECZNE.get(cechy, 0.0)

    # Bieda kłóci ludzi — i to jest sprzężenie, które ma boleć: głód obniża
    # morale, a niskie morale przez waśnie obniża je jeszcze bardziej.
    if glodni:
        zmiana -= 1.4
    if zimno:
        zmiana -= 0.8

    if a.get("chory") or b.get("chory"):
        zmiana -= 0.3

    return zmiana


def przelicz_dzien(gracz: Gracz, glodni: bool = False, zimno: bool = False) -> list[str]:
    """
    Jeden dzień relacji: dryf więzi, nowe przyjaźnie i waśnie, pary, narodziny.
    Zwraca wieści do kroniki osady.
    """
    from game.osada import osadnicy, wolne_chaty

    lista = osadnicy(gracz)
    if len(lista) < 2:
        return []

    wiesci: list[str] = []
    imiona = {o["imie"] for o in lista}

    # Więzi po ludziach, których już nie ma — inaczej rosłyby w nieskończoność.
    for klucz in [k for k in _mapa(gracz) if not set(k.split("|")) <= imiona]:
        del _mapa(gracz)[klucz]

    for i, a in enumerate(lista):
        for b in lista[i + 1:]:
            przed = wiez(gracz, a["imie"], b["imie"])
            po = zmien(gracz, a["imie"], b["imie"],
                       _dryf_pary(gracz, a, b, glodni, zimno) * random.uniform(0.5, 1.5))

            # Wieść tylko przy przekroczeniu progu — inaczej kronika to szum.
            if przed < PROG_PRZYJAZN <= po:
                wiesci.append(f"  🤝  {a['imie']} i {b['imie']} zaprzyjaźnili się.")
            elif przed > PROG_WASN >= po:
                wiesci.append(f"  💢  {a['imie']} i {b['imie']} poróżnili się na dobre.")
            elif przed < PROG_PARA <= po:
                wiesci.append(f"  💍  {a['imie']} i {b['imie']} są parą. Osada świętuje.")
                from game.osada import zmien_morale
                zmien_morale(gracz, 6)

    wiesci += _narodziny(gracz, lista, wolne_chaty(gracz), glodni)
    return wiesci


def _narodziny(gracz: Gracz, lista: list[dict], wolne: int, glodni: bool) -> list[str]:
    """
    Para w osadzie, która ma czym karmić i gdzie mieszkać, może doczekać się
    dziecka. To jedyny darmowy osadnik w grze — i nagroda za to, że osada jest
    naprawdę zadbana, a nie tylko zapełniona.
    """
    if glodni or wolne <= 0 or len(lista) < 2:
        return []

    # Żywność leży w magazynie osady (gracz.surowce), nie w polu gracz.zywnosc —
    # tamto pole nie istnieje i warunek nigdy się nie spełniał.
    from game.osada import _magazyn
    if _magazyn(gracz).get("zywnosc", 0) < 40:
        return []

    pary = set()
    for klucz, w in _mapa(gracz).items():
        if w >= PROG_PARA:
            pary.add(klucz)

    for klucz in sorted(pary):
        a, b = klucz.split("|")
        rodzice = [o for o in lista if o["imie"] in (a, b)]
        if len(rodzice) < 2 or any(o.get("chory") for o in rodzice):
            continue
        if min(o.get("morale", 0) for o in rodzice) < 60:
            continue
        dzien = int(getattr(gracz, "czas", 0) or 0)
        if any(dzien - int(o.get("ostatnie_dziecko", -PRZERWA_NARODZIN)) < PRZERWA_NARODZIN
               for o in rodzice):
            continue
        if random.random() >= 0.015:
            continue

        from game.osada import zatrudnij_osadnika, osadnicy, zmien_morale
        komunikat = zatrudnij_osadnika(gracz, "bezczynny", darmo=True)
        if "wprowadza się" not in komunikat:
            return []

        for o in rodzice:
            o["ostatnie_dziecko"] = dzien

        dziecko = osadnicy(gracz)[-1]
        dziecko["rodzice"] = [a, b]
        dziecko["morale"] = 75.0
        # Dziecko od początku kocha rodziców; to robi z rodziny realny węzeł,
        # który boli, gdy ktoś z niego zniknie.
        zmien(gracz, dziecko["imie"], a, 70)
        zmien(gracz, dziecko["imie"], b, 70)
        zmien_morale(gracz, 8)
        return [f"  👶  {a} i {b} doczekali się dziecka: {dziecko['imie']}. "
                f"Osada ma nowe ręce do pracy."]

    return []


def premia_morale(gracz: Gracz, o: dict) -> float:
    """
    Ile więzi dokładają do morale osadnika. Przyjaciele podnoszą, wrogowie ciągną
    w dół; para liczy się podwójnie. Limit ±18, żeby relacje nie przykryły głodu
    i zimna — one mają zostać najważniejsze.
    """
    imie = o.get("imie")
    if not imie:
        return 0.0

    suma = 0.0
    for inne, w in bliscy(gracz, imie, PROG_PRZYJAZN):
        suma += 10.0 if w >= PROG_PARA else 5.0
    for _inne, w in skonfliktowani(gracz, imie, PROG_WASN):
        suma -= 8.0 if w <= PROG_WROGOSC else 4.0

    return max(-18.0, min(18.0, suma))


def po_stracie(gracz: Gracz, imie: str, powod: str = "odeszl") -> list[str]:
    """
    Osadnik zniknął z osady — jego bliscy to odchorują.

    Wywoływane PRZED usunięciem go z listy, żeby dało się jeszcze odczytać,
    kto był z nim związany.
    """
    wiesci: list[str] = []
    from game.osada import osadnicy

    zostali = {o["imie"]: o for o in osadnicy(gracz) if o["imie"] != imie}
    smierc = powod == "zmarl"

    for inne, w in bliscy(gracz, imie, PROG_PRZYJAZN):
        o = zostali.get(inne)
        if o is None:
            continue
        cios = (18.0 if w >= PROG_PARA else 9.0) * (1.6 if smierc else 1.0)
        o["morale"] = max(0.0, o["morale"] - cios)
        if w >= PROG_PARA:
            wiesci.append(f"  💔  {inne} traci {imie}. " +
                          ("Nie chce z nikim rozmawiać." if smierc else "Zostaje sam(a)."))
        else:
            wiesci.append(f"  😔  {inne} ciężko znosi stratę {imie}.")

    # Wrogowie nie żałują — ale śmierć człowieka studzi nawet ich.
    for inne, _w in skonfliktowani(gracz, imie, PROG_WASN):
        o = zostali.get(inne)
        if o is not None and not smierc:
            o["morale"] = min(100.0, o["morale"] + 4.0)

    for klucz in [k for k in _mapa(gracz) if imie in k.split("|")]:
        del _mapa(gracz)[klucz]

    return wiesci
