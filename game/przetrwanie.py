"""Przetrwanie bohatera: prowiant, głód, zimno i rany.

Wcześniej czas nic nie kosztował, a HP było jedynym zasobem zdrowia.
Teraz każdy dzień wyprawy zjada rację, zima rani bez ciepłego odzienia,
a ciężkie ciosy zostawiają rany, które trzeba wyleczyć w obozie.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import kalendarz, talenty

if TYPE_CHECKING:
    from game.player import Gracz

# ------------------------------------------------------------------ #
#  Rany                                                                #
# ------------------------------------------------------------------ #

RANY: dict[str, dict] = {
    "zlamana_reka": {
        "nazwa": "Złamana ręka", "ikona": "🦴", "dni": 6,
        "opis": "Atak −20%.", "atak": 0.8,
    },
    "gleboka_rana": {
        "nazwa": "Głęboka rana", "ikona": "🩸", "dni": 5,
        "opis": "Mikstury leczą o połowę słabiej.", "leczenie": 0.5,
    },
    "zwichnieta_noga": {
        "nazwa": "Zwichnięta noga", "ikona": "🦵", "dni": 4,
        "opis": "Ucieczka −20%, brak pasywnego uniku.", "ucieczka": -0.20, "bez_uniku": True,
    },
    "wstrzas": {
        "nazwa": "Wstrząs mózgu", "ikona": "💫", "dni": 3,
        "opis": "−3 do testów Inteligencji i Mądrości.", "testy": {"inteligencja": -3, "madrosc": -3},
    },
    "ciezka_rana": {
        "nazwa": "Ciężka rana", "ikona": "⚰", "dni": 8,
        "opis": "Po upadku w boju: atak −15%, mikstury −30%, testy fizyczne −2.",
        "atak": 0.85, "leczenie": 0.7, "testy": {"sila": -2, "zrecznosc": -2},
    },
}


def rany(gracz: "Gracz") -> list[dict]:
    if getattr(gracz, "rany", None) is None:
        gracz.rany = []
    return gracz.rany


def ma_rane(gracz: "Gracz", klucz: str) -> bool:
    return any(r["typ"] == klucz for r in rany(gracz))


def dodaj_rane(gracz: "Gracz", klucz: str, dni: int | None = None) -> str:
    info = RANY[klucz]
    dni = dni or info["dni"]
    for r in rany(gracz):
        if r["typ"] == klucz:
            r["dni"] = max(r["dni"], dni)
            return f"  {info['ikona']}  {info['nazwa']} się odnawia ({r['dni']} dni leczenia)."
    rany(gracz).append({"typ": klucz, "dni": dni})
    gracz.statystyki["rany"] = gracz.statystyki.get("rany", 0) + 1
    return f"  {info['ikona']}  RANA: {info['nazwa']} — {info['opis']} ({dni} dni leczenia)"


def moze_zranic(gracz: "Gracz", obrazenia: int, jest_boss: bool) -> str | None:
    """Po ciosie: ciężkie trafienie albo niskie HP może zostawić ranę."""
    if obrazenia <= 0 or not gracz.zyje():
        return None
    ciezki = obrazenia >= gracz.max_hp * 0.22
    na_krawedzi = gracz.hp <= gracz.max_hp * 0.2
    if not (ciezki or na_krawedzi):
        return None
    szansa = 0.18 + (0.12 if jest_boss else 0.0) + (0.10 if ciezki and na_krawedzi else 0.0)
    if talenty.ma(gracz, "zahartowany_w_boju"):
        szansa /= 2
    if random.random() >= szansa:
        return None
    wolne = [k for k in ("zlamana_reka", "gleboka_rana", "zwichnieta_noga", "wstrzas") if not ma_rane(gracz, k)]
    if not wolne:
        return None
    return dodaj_rane(gracz, random.choice(wolne))


def lecz_rany(gracz: "Gracz", dni: float) -> list[str]:
    """Upływ czasu goi rany. Odpoczynek i lecznica przyspieszają (``dni`` > 1)."""
    if talenty.ma(gracz, "polowy_medyk"):
        dni *= 2
    msgs = []
    for r in list(rany(gracz)):
        r["dni"] -= dni
        if r["dni"] <= 0:
            rany(gracz).remove(r)
            info = RANY[r["typ"]]
            msgs.append(f"  {info['ikona']}  {info['nazwa']} się zagoiła.")
    return msgs


def wylecz_jedna(gracz: "Gracz", o_dni: int | None = None) -> str | None:
    """Bandaż (o_dni) albo maść (None = całkiem) na najcięższą ranę."""
    lista = rany(gracz)
    if not lista:
        return None
    r = max(lista, key=lambda w: w["dni"])
    info = RANY[r["typ"]]
    if o_dni is None:
        lista.remove(r)
        return f"  {info['ikona']}  {info['nazwa']} wyleczona."
    r["dni"] -= o_dni
    if r["dni"] <= 0:
        lista.remove(r)
        return f"  {info['ikona']}  {info['nazwa']} zagojona dzięki opatrunkowi."
    return f"  {info['ikona']}  {info['nazwa']}: opatrunek skraca leczenie ({max(1, round(r['dni']))} dni)."


def mnoznik_ataku(gracz: "Gracz") -> float:
    m = 1.0
    for r in rany(gracz):
        m *= RANY[r["typ"]].get("atak", 1.0)
    glod = int(getattr(gracz, "glod", 0) or 0)
    if glod:
        kara = 0.1 * min(glod, 4)
        if talenty.ma(gracz, "hartowany"):
            kara /= 2
        m *= 1 - kara
    return m


def mnoznik_leczenia(gracz: "Gracz") -> float:
    m = 1.0
    for r in rany(gracz):
        m *= RANY[r["typ"]].get("leczenie", 1.0)
    if talenty.ma(gracz, "zielarz"):
        m *= 1.3
    return m


def premia_ucieczki(gracz: "Gracz") -> float:
    return sum(RANY[r["typ"]].get("ucieczka", 0.0) for r in rany(gracz))


def bez_uniku(gracz: "Gracz") -> bool:
    return any(RANY[r["typ"]].get("bez_uniku") for r in rany(gracz))


def kara_testu(gracz: "Gracz", atrybut: str) -> int:
    kara = sum(RANY[r["typ"]].get("testy", {}).get(atrybut, 0) for r in rany(gracz))
    if int(getattr(gracz, "glod", 0) or 0) >= 2:
        kara -= 1
    return kara


def opis_stanu(gracz: "Gracz") -> str:
    czesci = []
    for r in rany(gracz):
        info = RANY[r["typ"]]
        czesci.append(f"{info['ikona']} {info['nazwa']} ({max(1, round(r['dni']))} dni)")
    glod = int(getattr(gracz, "glod", 0) or 0)
    if glod:
        czesci.append(f"🍽 głód ({glod} dni)")
    return ", ".join(czesci) if czesci else "zdrowy"


# ------------------------------------------------------------------ #
#  Prowiant i dzień wyprawy                                            #
# ------------------------------------------------------------------ #

def ma_odzienie(gracz: "Gracz") -> bool:
    return (getattr(gracz, "przedmioty", None) or {}).get("cieple_odzienie", 0) > 0


def pojemnosc_prowiantu(gracz: "Gracz") -> int:
    from game.oboz import poziom_budynku
    return 8 + 4 * poziom_budynku(gracz, "stajnie")


def spakuj_prowiant(gracz: "Gracz") -> str:
    """Przed wyprawą: racje z magazynu do plecaka."""
    mag = gracz.surowce
    brakuje = pojemnosc_prowiantu(gracz) - int(getattr(gracz, "prowiant", 0) or 0)
    bierzesz = max(0, min(brakuje, mag.get("zywnosc", 0)))
    mag["zywnosc"] = mag.get("zywnosc", 0) - bierzesz
    gracz.prowiant = int(getattr(gracz, "prowiant", 0) or 0) + bierzesz
    zjada = 2 if kalendarz.pora(gracz)["klucz"] == "zima" else 1
    dni = gracz.prowiant // zjada
    if gracz.prowiant == 0:
        return "  🍖  Magazyn pusty — wyruszasz bez prowiantu. Głód przyjdzie szybko."
    return f"  🍖  Prowiant: {gracz.prowiant} racji (wystarczy na ok. {dni} dni, pojemność {pojemnosc_prowiantu(gracz)})."


def rozpakuj_prowiant(gracz: "Gracz") -> None:
    if getattr(gracz, "prowiant", 0):
        gracz.surowce["zywnosc"] = gracz.surowce.get("zywnosc", 0) + gracz.prowiant
        gracz.prowiant = 0


def dzien_wyprawy(gracz: "Gracz") -> list[str]:
    """Jeden dzień poza obozem: jedzenie, zimno, rany."""
    msgs: list[str] = []
    zima = kalendarz.pora(gracz)["klucz"] == "zima"
    potrzeba = 2 if zima else 1
    if talenty.ma(gracz, "oszczedny") and random.random() < 0.25:
        potrzeba -= 1
    zjedzone = min(potrzeba, int(getattr(gracz, "prowiant", 0) or 0))
    gracz.prowiant = int(getattr(gracz, "prowiant", 0) or 0) - zjedzone
    if zjedzone < potrzeba:
        gracz.glod = int(getattr(gracz, "glod", 0) or 0) + 1
        strata = 3 * gracz.glod
        if talenty.ma(gracz, "hartowany"):
            strata //= 2
        gracz.hp = max(1, gracz.hp - strata)
        msgs.append(f"  🍽  Głód ({gracz.glod} dni): −{strata} HP, atak słabnie. Wróć do obozu albo znajdź jedzenie.")
    elif getattr(gracz, "glod", 0):
        gracz.glod = 0
        msgs.append("  🍖  Najadasz się. Głód mija.")
    if zima and not ma_odzienie(gracz) and not talenty.ma(gracz, "hartowany"):
        gracz.hp = max(1, gracz.hp - 4)
        msgs.append("  ❄  Mróz przenika do kości (−4 HP). Ciepłe odzienie z warsztatu by pomogło.")
    msgs += lecz_rany(gracz, 0.5)  # w drodze rany goją się wolno
    if 0 < gracz.prowiant <= 2:
        msgs.append(f"  🍖  Zostały ci {gracz.prowiant} racje.")
    return msgs


def zjedz_z_magazynu(gracz: "Gracz") -> str | None:
    """Dzień w obozie: bohater je z magazynu osady."""
    mag = gracz.surowce
    if mag.get("zywnosc", 0) > 0:
        mag["zywnosc"] -= 1
        if getattr(gracz, "glod", 0):
            gracz.glod = 0
            return "  🍖  W obozie wreszcie jesz do syta."
        return None
    gracz.glod = int(getattr(gracz, "glod", 0) or 0) + 1
    return f"  🍽  W magazynie nie ma jedzenia — głodujesz ({gracz.glod} dni)."
