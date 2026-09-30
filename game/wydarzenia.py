"""Wydarzenia sezonowe: los, na który osada może się przygotować.

Każde wydarzenie ma porę roku, dzienną szansę i coś, co je łagodzi —
budynek, zawód albo wcześniejszą decyzję z rozmowy (np. czysta studnia).
Dzięki temu decyzje z różnych systemów wracają w nieoczekiwanych momentach.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import kalendarz
from game.oboz import _magazyn, poziom_budynku

if TYPE_CHECKING:
    from game.player import Gracz

SZANSA_DZIENNA = 0.025


def _powodz(gracz: "Gracz") -> list[str]:
    mag = _magazyn(gracz)
    ochrona = poziom_budynku(gracz, "spichlerz")
    czesc = 0.25 / (1 + ochrona)
    straty = {k: int(mag.get(k, 0) * czesc) for k in ("zywnosc", "ziola", "drewno")}
    for k, v in straty.items():
        mag[k] -= v
    razem = sum(straty.values())
    dopisek = " Spichlerz uratował większość." if ochrona else " Spichlerz by pomógł."
    return [f"  🌊  WIOSENNA POWÓDŹ! Rzeka wystąpiła z brzegów — przepadło {razem} jednostek zapasów.{dopisek}"]


def _pozar(gracz: "Gracz") -> list[str]:
    from game.osada import ilu_w_zawodzie, zmien_morale

    gaszacy = ilu_w_zawodzie(gracz, "straznik") + (2 if gracz.flagi.get("czysta_studnia") else 0)
    if gaszacy >= 3:
        return ["  🔥  Iskra z paleniska zajęła strzechę — strażnicy ugasili ją w kilka chwil."]
    mag = _magazyn(gracz)
    strata = int(mag.get("drewno", 0) * 0.3) + int(mag.get("deski", 0) * 0.3)
    mag["drewno"] = mag.get("drewno", 0) - int(mag.get("drewno", 0) * 0.3)
    mag["deski"] = mag.get("deski", 0) - int(mag.get("deski", 0) * 0.3)
    zmien_morale(gracz, -6)
    return [f"  🔥  POŻAR W OSADZIE! Spłonęło {strata} drewna i desek. Więcej strażników i studnia pomogłyby gasić."]


def _zaraza(gracz: "Gracz") -> list[str]:
    from game.osada import osadnicy

    if gracz.flagi.get("czysta_studnia"):
        return ["  💧  W okolicznych wsiach szaleje zaraza — czysta studnia chroni twoją osadę."]
    lecznica = poziom_budynku(gracz, "lecznica")
    szansa = 0.35 / (1 + lecznica)
    chorzy = [o for o in osadnicy(gracz) if random.random() < szansa]
    for o in chorzy:
        o["chory"] = max(o.get("chory", 0), random.randint(4, 7))
    if not chorzy:
        return ["  🤒  Zaraza krąży po okolicy, ale omija twoją osadę."]
    return [f"  🤒  ZARAZA! Choruje {len(chorzy)} osadników" + (" (lecznica ogranicza szkody)." if lecznica else ".")]


def _wilki(gracz: "Gracz") -> list[str]:
    from game.osada import zmien_morale

    if poziom_budynku(gracz, "palisada") >= 1:
        return ["  🐺  Wilki krążą nocą wokół palisady, ale nie mają jak wejść."]
    mag = _magazyn(gracz)
    strata = min(mag.get("zywnosc", 0), random.randint(4, 10))
    mag["zywnosc"] = mag.get("zywnosc", 0) - strata
    zmien_morale(gracz, -3)
    return [f"  🐺  Głodne wilki wdarły się do spiżarni: −{strata} żywności. Palisada by je zatrzymała."]


def _urodzaj(gracz: "Gracz") -> list[str]:
    from game.osada import ilu_w_zawodzie

    rolnicy = ilu_w_zawodzie(gracz, "rolnik") + ilu_w_zawodzie(gracz, "zielarz")
    bonus = 6 + 4 * rolnicy
    _magazyn(gracz)["zywnosc"] = _magazyn(gracz).get("zywnosc", 0) + bonus
    return [f"  🌻  Urodzajny rok! Pola i łąki dały więcej niż zwykle: +{bonus} żywności."]


# pora → (klucz, funkcja)
WYDARZENIA = {
    "wiosna": [("powodz", _powodz), ("urodzaj", _urodzaj)],
    "lato": [("pozar", _pozar), ("urodzaj", _urodzaj)],
    "jesien": [("zaraza", _zaraza), ("urodzaj", _urodzaj)],
    "zima": [("wilki", _wilki), ("zaraza", _zaraza)],
}


def dzien_wydarzen(gracz: "Gracz") -> list[str]:
    """Najwyżej jedno wydarzenie danego rodzaju na porę roku."""
    if random.random() >= SZANSA_DZIENNA:
        return []
    pora = kalendarz.pora(gracz)
    klucz, funkcja = random.choice(WYDARZENIA[pora["klucz"]])
    znacznik = f"wydarzenie_{klucz}_{kalendarz.rok(gracz)}_{pora['klucz']}"
    if gracz.flagi.get(znacznik):
        return []
    gracz.flagi[znacznik] = True
    return funkcja(gracz)
