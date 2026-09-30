"""Rzemiosło i alchemia: przepisy, odkrycia, zamówienia, ulepszenia sprzętu.

Trzy pracownie: warsztat (podstawy), laboratorium (alchemia) i kuźnia
(narzędzia, ulepszenia). Część przepisów znasz od początku, resztę trzeba
ODKRYĆ — eksperymentem w laboratorium (dwa składniki; test Inteligencji
podpowiada trop) albo w rozmowach. Rzemieślnicy z osady realizują
zamówienia: ustawiasz docelowy zapas, oni pilnują, żeby go uzupełniać.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import talenty
from game.oboz import SUROWCE, _magazyn, ma_budynek, poziom_budynku
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

SKLADNIKI: dict[str, dict] = {
    "grzyb": {"nazwa": "grzyb jaskiniowy", "ikona": "🍄", "skad": "bagna, jaskinie, topielce"},
    "kwiat_pustyni": {"nazwa": "kwiat pustyni", "ikona": "🌵", "skad": "kanion, skorpiony"},
    "luska": {"nazwa": "łuska smoka", "ikona": "🐉", "skad": "smoki, leże smoka"},
    "esencja": {"nazwa": "esencja cienia", "ikona": "🌑", "skad": "wiedźmy, licze, portal, ruiny"},
    "pioro": {"nazwa": "pióro harpii", "ikona": "🪶", "skad": "harpie, gryf, wzgórza"},
}

# wynik: ("pole", atrybut gracza) | ("przedmiot", klucz) | ("narzedzia", None)
PRZEPISY: dict[str, dict] = {
    "mikstura": {"nazwa": "Mikstura leczenia", "stacja": "warsztat", "koszt": {"ziola": 3},
                 "wynik": ("pole", "mikstury"), "znany": True},
    "antidotum": {"nazwa": "Antidotum", "stacja": "warsztat", "koszt": {"ziola": 2, "skora": 1},
                  "wynik": ("pole", "antidota"), "znany": True},
    "mana": {"nazwa": "Mikstura many", "stacja": "warsztat", "koszt": {"ziola": 2, "ruda": 1},
             "wynik": ("pole", "mikstury_many"), "znany": True},
    "bandaz": {"nazwa": "Bandaże (×2)", "stacja": "warsztat", "koszt": {"ziola": 1, "skora": 1},
               "wynik": ("przedmiot", "bandaz"), "ile": 2, "znany": True},
    "cieple_odzienie": {"nazwa": "Ciepłe odzienie", "stacja": "warsztat", "koszt": {"skora": 4, "deski": 1},
                        "wynik": ("przedmiot", "cieple_odzienie"), "znany": True,
                        "opis": "chroni przed mrozem na zimowych wyprawach"},
    "olej_ognisty": {"nazwa": "Olej ognisty", "stacja": "warsztat", "koszt": {"drewno": 3, "ruda": 1},
                     "wynik": ("przedmiot", "olej_ognisty"), "odkrycie": ("drewno", "ruda")},
    "mikstura_duza": {"nazwa": "Mikstura większa", "stacja": "laboratorium", "koszt": {"ziola": 4, "grzyb": 1},
                      "wynik": ("pole", "mikstury_duze"), "odkrycie": ("ziola", "grzyb")},
    "masc": {"nazwa": "Maść na rany", "stacja": "laboratorium", "koszt": {"ziola": 3, "grzyb": 1, "skora": 1},
             "wynik": ("przedmiot", "masc"), "odkrycie": ("grzyb", "skora"),
             "opis": "leczy jedną ranę od razu (poza walką)"},
    "eliksir_sily": {"nazwa": "Eliksir siły", "stacja": "laboratorium", "koszt": {"ziola": 2, "kwiat_pustyni": 1},
                     "wynik": ("przedmiot", "eliksir_sily"), "odkrycie": ("ziola", "kwiat_pustyni")},
    "eliksir_ognia": {"nazwa": "Eliksir ognioodporności", "stacja": "laboratorium", "koszt": {"ziola": 2, "luska": 1},
                      "wynik": ("przedmiot", "eliksir_ognia"), "odkrycie": ("ziola", "luska")},
    "bomba": {"nazwa": "Bomba", "stacja": "laboratorium", "koszt": {"zelazo": 1, "ruda": 2},
              "wynik": ("przedmiot", "bomba"), "odkrycie": ("zelazo", "ruda")},
    "napar_jasnosci": {"nazwa": "Napar jasności", "stacja": "laboratorium", "koszt": {"ziola": 2, "pioro": 1},
                       "wynik": ("przedmiot", "napar_jasnosci"), "odkrycie": ("ziola", "pioro"),
                       "opis": "+2 do wszystkich testów w jednej rozmowie"},
    "esencja_mroku": {"nazwa": "Eliksir cienia", "stacja": "laboratorium", "koszt": {"esencja": 1, "grzyb": 1},
                      "wynik": ("przedmiot", "bomba"), "ile": 2, "odkrycie": ("esencja", "grzyb"),
                      "opis": "dwie bomby z esencji cienia"},
    "narzedzia": {"nazwa": "Narzędzia", "stacja": "kuznia", "koszt": {"zelazo": 2, "deski": 1},
                  "wynik": ("narzedzia", None), "znany": True,
                  "opis": "każdy komplet: +25% pracy jednego osadnika (zużywa się)"},
}

NAZWY_PRZEDMIOTOW = {
    "bandaz": "🩹 bandaż", "cieple_odzienie": "🧥 ciepłe odzienie", "olej_ognisty": "🛢 olej ognisty",
    "masc": "🧴 maść na rany", "eliksir_sily": "💪 eliksir siły", "eliksir_ognia": "🧯 eliksir ognioodporności",
    "bomba": "💣 bomba", "napar_jasnosci": "🍵 napar jasności",
}

_LUP_WROGOW = {
    "smok": ("luska", 0.6), "wiedźma": ("esencja", 0.45), "otchłani": ("esencja", 0.9),
    "licz": ("esencja", 0.7), "harpii": ("pioro", 0.5), "gryf": ("pioro", 0.9),
    "skorpion": ("kwiat_pustyni", 0.35), "topielec": ("grzyb", 0.4), "troll": ("grzyb", 0.3),
}


# ------------------------------------------------------------------ #
#  Magazyn składników i przedmiotów                                    #
# ------------------------------------------------------------------ #

def skladniki(gracz: "Gracz") -> dict[str, int]:
    if getattr(gracz, "skladniki", None) is None:
        gracz.skladniki = {}
    return gracz.skladniki


def przedmioty(gracz: "Gracz") -> dict[str, int]:
    if getattr(gracz, "przedmioty", None) is None:
        gracz.przedmioty = {}
    return gracz.przedmioty


def dodaj_skladnik(gracz: "Gracz", klucz: str, ile: int = 1) -> str:
    skladniki(gracz)[klucz] = skladniki(gracz).get(klucz, 0) + ile
    info = SKLADNIKI[klucz]
    return f"  {info['ikona']}  +{ile} {info['nazwa']}"


def lup_skladnikow(gracz: "Gracz", przeciwnik, jest_boss: bool) -> list[str]:
    nazwa = przeciwnik.nazwa.lower()
    for fragment, (klucz, szansa) in _LUP_WROGOW.items():
        if fragment in nazwa and random.random() < min(1.0, szansa * (2 if jest_boss else 1)):
            return [dodaj_skladnik(gracz, klucz, 2 if jest_boss else 1)]
    return []


def _ile_masz(gracz: "Gracz", klucz: str) -> int:
    if klucz in SKLADNIKI:
        return skladniki(gracz).get(klucz, 0)
    return _magazyn(gracz).get(klucz, 0)


def _nazwa(klucz: str) -> str:
    if klucz in SKLADNIKI:
        return f"{SKLADNIKI[klucz]['ikona']}{SKLADNIKI[klucz]['nazwa']}"
    return f"{SUROWCE[klucz]['ikona']}{SUROWCE[klucz]['nazwa']}"


def _format(koszt: dict) -> str:
    return ", ".join(f"{ile} {_nazwa(k)}" for k, ile in koszt.items())


def znane(gracz: "Gracz") -> list[str]:
    wlasne = set(getattr(gracz, "przepisy", None) or [])
    return [k for k, p in PRZEPISY.items() if p.get("znany") or k in wlasne]


def naucz(gracz: "Gracz", klucz: str) -> str:
    if klucz in znane(gracz):
        return ""
    gracz.przepisy = list(getattr(gracz, "przepisy", None) or []) + [klucz]
    p = PRZEPISY[klucz]
    return f"  📜  NOWY PRZEPIS: {p['nazwa']} ({p['stacja']}) — {_format(p['koszt'])}"


def stan_zapasu(gracz: "Gracz", klucz: str) -> int:
    typ, pole = PRZEPISY[klucz]["wynik"]
    if typ == "pole":
        return int(getattr(gracz, pole, 0) or 0)
    if typ == "przedmiot":
        return przedmioty(gracz).get(pole, 0)
    return int(getattr(gracz, "narzedzia", 0) or 0)


def mozna_wytworzyc(gracz: "Gracz", klucz: str) -> tuple[bool, str]:
    p = PRZEPISY[klucz]
    if klucz not in znane(gracz):
        return False, "nieznany przepis"
    if not ma_budynek(gracz, p["stacja"]):
        return False, f"wymaga: {p['stacja']}"
    brak = [k for k, ile in p["koszt"].items() if _ile_masz(gracz, k) < ile]
    if brak:
        return False, "brakuje: " + ", ".join(_nazwa(k) for k in brak)
    return True, ""


def wytworz(gracz: "Gracz", klucz: str) -> str:
    mozna, powod = mozna_wytworzyc(gracz, klucz)
    if not mozna:
        return f"  Nie można: {powod}."
    p = PRZEPISY[klucz]
    for k, ile in p["koszt"].items():
        if k in SKLADNIKI:
            skladniki(gracz)[k] -= ile
        else:
            _magazyn(gracz)[k] -= ile
    ile = p.get("ile", 1)
    if talenty.ma(gracz, "alchemik") and random.random() < 0.3:
        ile += 1
    typ, pole = p["wynik"]
    if typ == "pole":
        setattr(gracz, pole, int(getattr(gracz, pole, 0) or 0) + ile)
    elif typ == "przedmiot":
        przedmioty(gracz)[pole] = przedmioty(gracz).get(pole, 0) + ile
    else:
        gracz.narzedzia = int(getattr(gracz, "narzedzia", 0) or 0) + ile
    gracz.statystyki["wytworzone"] = gracz.statystyki.get("wytworzone", 0) + ile
    return f"  🔧  Wytworzono: {p['nazwa']} ×{ile}."


def praca_rzemieslnikow(gracz: "Gracz", moc: float) -> list[str]:
    """Rzemieślnicy (suma ich wydajności) uzupełniają zamówione zapasy."""
    cele = getattr(gracz, "zapasy_cel", None) or {}
    ile = int(moc) + (1 if random.random() < moc - int(moc) else 0)
    zrobione = []
    for _ in range(ile):
        for klucz, cel in cele.items():
            if klucz in PRZEPISY and stan_zapasu(gracz, klucz) < cel and mozna_wytworzyc(gracz, klucz)[0]:
                wytworz(gracz, klucz)
                zrobione.append(PRZEPISY[klucz]["nazwa"])
                break
    if zrobione:
        return [f"  🔧  Rzemieślnicy wytworzyli: {', '.join(zrobione)}."]
    return []


# ------------------------------------------------------------------ #
#  Eksperymenty i ulepszenia                                           #
# ------------------------------------------------------------------ #

_DO_EKSPERYMENTOW = ["ziola", "drewno", "ruda", "skora", "zelazo", "grzyb", "kwiat_pustyni", "luska", "esencja", "pioro"]


def eksperyment(gracz: "Gracz", a: str, b: str) -> list[str]:
    """Łączysz dwa składniki. Trafione połączenie = nowy przepis; pudło może dać trop."""
    from game.atrybuty import przeprowadz_test

    for k in (a, b):
        if _ile_masz(gracz, k) < 1:
            return [f"  Brakuje: {_nazwa(k)}."]
    for k in (a, b):
        if k in SKLADNIKI:
            skladniki(gracz)[k] -= 1
        else:
            _magazyn(gracz)[k] -= 1
    para = {a, b}
    nieznane = [k for k, p in PRZEPISY.items() if k not in znane(gracz) and "odkrycie" in p]
    for k in nieznane:
        if set(PRZEPISY[k]["odkrycie"]) == para:
            return ["  ⚗  Mieszanina syczy, zmienia kolor… i wreszcie się stabilizuje!", naucz(gracz, k)]
    msgs = ["  ⚗  Opary, osad, smród. Nic trwałego z tego nie wyszło."]
    if talenty.ma(gracz, "eksperymentator"):
        bliskie = [k for k in nieznane if para & set(PRZEPISY[k]["odkrycie"])]
        if bliskie and random.random() < 0.25:
            k = random.choice(bliskie)
            return msgs + ["  💡  Eksperymentator: widzisz, czego zabrakło!", naucz(gracz, k)]
    if nieznane:
        wynik = przeprowadz_test(gracz, "spostrzegawczosc", 12)
        if wynik.sukces:
            k = random.choice(nieznane)
            skladnik = random.choice(PRZEPISY[k]["odkrycie"])
            msgs.append(f"  💡  TROP: {_nazwa(skladnik)} reaguje obiecująco — spróbuj go z czymś innym.")
    return msgs


def poziom_ulepszenia_max(gracz: "Gracz") -> int:
    return min(poziom_budynku(gracz, "kuznia") + 1, 4) + (1 if talenty.ma(gracz, "mistrz_kuzni") else 0)


def koszt_ulepszenia(gracz: "Gracz", slot: str) -> dict:
    poziom = (getattr(gracz, "ulepszenia", None) or {}).get(slot, 0) + 1
    koszt = {"zelazo": 2 * poziom, "zloto": 25 * poziom}
    if talenty.ma(gracz, "mistrz_kuzni"):
        koszt = {k: max(1, int(v * 0.7)) for k, v in koszt.items()}
    return koszt


def ulepsz(gracz: "Gracz", slot: str) -> str:
    if not ma_budynek(gracz, "kuznia"):
        return "  Potrzebna kuźnia w obozie."
    if getattr(gracz, "ulepszenia", None) is None:
        gracz.ulepszenia = {"bron": 0, "zbroja": 0}
    obecny = gracz.ulepszenia.get(slot, 0)
    if obecny >= poziom_ulepszenia_max(gracz):
        return "  Wyżej się nie da — rozbuduj kuźnię."
    koszt = koszt_ulepszenia(gracz, slot)
    if _magazyn(gracz).get("zelazo", 0) < koszt["zelazo"] or gracz.zloto < koszt["zloto"]:
        return f"  Potrzeba {koszt['zelazo']} żelaza i {koszt['zloto']} zł."
    _magazyn(gracz)["zelazo"] -= koszt["zelazo"]
    gracz.zloto -= koszt["zloto"]
    gracz.ulepszenia[slot] = obecny + 1
    if slot == "bron":
        gracz.atak += 2
    else:
        gracz.obrona += 2
    nazwa = "Broń" if slot == "bron" else "Zbroja"
    return f"  🔨  {nazwa} ulepszona do +{obecny + 1} ({'+2 atak' if slot == 'bron' else '+2 obrona'})."


# ------------------------------------------------------------------ #
#  Menu                                                                #
# ------------------------------------------------------------------ #

def menu_zamowien(gracz: "Gracz") -> None:
    if getattr(gracz, "zapasy_cel", None) is None:
        gracz.zapasy_cel = {}
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  📋  ZAMÓWIENIA DLA RZEMIEŚLNIKÓW")
        wyswietl_linie("═")
        print("\n  Rzemieślnicy (zawód w osadzie) co dzień uzupełniają zapasy do celu.\n")
        klucze = [k for k in znane(gracz)]
        for i, k in enumerate(klucze, 1):
            p = PRZEPISY[k]
            cel = gracz.zapasy_cel.get(k, 0)
            stacja = "" if ma_budynek(gracz, p["stacja"]) else f"  (brak: {p['stacja']})"
            print(f"  [{i:>2}] {p['nazwa']:24} masz {stan_zapasu(gracz, k):>3}   cel {cel:>3}{stacja}")
        print("\n  [0] Wróć\n")
        wybor = input("  Zmień cel dla: ").strip()
        if wybor == "0":
            return
        if wybor.isdigit() and 1 <= int(wybor) <= len(klucze):
            odp = input("  Nowy cel (0 = nie rób): ").strip()
            if odp.isdigit():
                gracz.zapasy_cel[klucze[int(wybor) - 1]] = min(50, int(odp))


def _menu_eksperymentu(gracz: "Gracz") -> None:
    print("\n  Wybierz dwa różne składniki:")
    for i, k in enumerate(_DO_EKSPERYMENTOW, 1):
        print(f"  [{i:>2}] {_nazwa(k)} ({_ile_masz(gracz, k)})")
    a = input("  Pierwszy: ").strip()
    b = input("  Drugi: ").strip()
    if not (a.isdigit() and b.isdigit()) or a == b:
        return
    ia, ib = int(a) - 1, int(b) - 1
    if not (0 <= ia < len(_DO_EKSPERYMENTOW) and 0 <= ib < len(_DO_EKSPERYMENTOW)):
        return
    for msg in eksperyment(gracz, _DO_EKSPERYMENTOW[ia], _DO_EKSPERYMENTOW[ib]):
        if msg:
            print(msg)
    nacisnij_enter()


def menu_rzemiosla(gracz: "Gracz") -> None:
    from game import przetrwanie

    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  ⚗  RZEMIOSŁO I ALCHEMIA")
        wyswietl_linie("═")
        stacje = [f"{n} {'✔' if ma_budynek(gracz, k) else '—'}" for k, n in
                  (("warsztat", "🔧 warsztat"), ("laboratorium", "⚗ laboratorium"), ("kuznia", "🔨 kuźnia"))]
        print("\n  " + "   ".join(stacje))
        skl = ", ".join(f"{SKLADNIKI[k]['ikona']}{v}" for k, v in skladniki(gracz).items() if v) or "brak"
        print(f"  Rzadkie składniki: {skl}")
        prz = ", ".join(f"{NAZWY_PRZEDMIOTOW.get(k, k)} ×{v}" for k, v in przedmioty(gracz).items() if v) or "brak"
        print(f"  Przedmioty: {prz}")
        print(f"  Narzędzia: {getattr(gracz, 'narzedzia', 0)}   Ulepszenia: broń +{gracz.ulepszenia.get('bron', 0)},"
              f" zbroja +{gracz.ulepszenia.get('zbroja', 0)}  (maks. +{poziom_ulepszenia_max(gracz)})\n")
        klucze = list(PRZEPISY)
        for i, k in enumerate(klucze, 1):
            p = PRZEPISY[k]
            if k not in znane(gracz):
                print(f"  [{i:>2}] ❔ nieznany przepis ({p['stacja']})")
                continue
            mozna, powod = mozna_wytworzyc(gracz, k)
            znak = "✔" if mozna else "·"
            opis = f" — {p['opis']}" if p.get("opis") else ""
            print(f"  [{i:>2}] {znak} {p['nazwa']}: {_format(p['koszt'])}{opis}" + ("" if mozna else f"  ({powod})"))
        print()
        if ma_budynek(gracz, "laboratorium"):
            print("  [E] ⚗  Eksperyment — połącz dwa składniki, może odkryjesz przepis")
        if ma_budynek(gracz, "kuznia"):
            kb, kz = koszt_ulepszenia(gracz, "bron"), koszt_ulepszenia(gracz, "zbroja")
            print(f"  [B] 🔨 Ulepsz broń ({kb['zelazo']} żelaza, {kb['zloto']} zł)   "
                  f"[Z] 🔨 Ulepsz zbroję ({kz['zelazo']} żelaza, {kz['zloto']} zł)")
        if przedmioty(gracz).get("masc", 0) and przetrwanie.rany(gracz):
            print("  [M] 🧴 Nałóż maść na najcięższą ranę")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "e" and ma_budynek(gracz, "laboratorium"):
            _menu_eksperymentu(gracz)
            continue
        if wybor in ("b", "z"):
            print(ulepsz(gracz, "bron" if wybor == "b" else "zbroja"))
        elif wybor == "m" and przedmioty(gracz).get("masc", 0):
            przedmioty(gracz)["masc"] -= 1
            print(przetrwanie.wylecz_jedna(gracz) or "  Nie masz ran.")
        elif wybor.isdigit() and 1 <= int(wybor) <= len(klucze):
            print(wytworz(gracz, klucze[int(wybor) - 1]))
        else:
            continue
        nacisnij_enter()
