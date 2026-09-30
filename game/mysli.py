"""Gabinet myśli (jak w Disco Elysium): myśli odkryte w rozmowach i wydarzeniach.

Myśl trzeba PRZYSWOIĆ: zajmuje miejsce w gabinecie przez kilka dni,
w tym czasie ciąży (mała kara), a po przyswojeniu daje trwały efekt.
Miejsca są dwa (trzy z talentem Głęboka myśl).
"""
from __future__ import annotations

from typing import TYPE_CHECKING

from game import talenty
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

MYSLI: dict[str, dict] = {
    "stal_pamieta": {
        "nazwa": "Stal pamięta", "dni": 6,
        "tekst": "Każde ostrze nosi w sobie wybór tego, kto je kuł — i tego, kto je dzierży.",
        "w_toku": "−1 do perswazji (rozmyślasz o winie)", "kara": {"perswazja": -1},
        "efekt": "+2 do ataku na stałe",
    },
    "cena_krwi": {
        "nazwa": "Cena krwi", "dni": 5,
        "tekst": "Każdy miecz, który nie został dobyty, to czyjś syn, który wróci do domu.",
        "w_toku": "−1 do zastraszania", "kara": {"zastraszanie": -1},
        "efekt": "+2 do perswazji na stałe", "premia": {"perswazja": 2},
    },
    "twarda_reka": {
        "nazwa": "Twarda ręka", "dni": 4,
        "tekst": "Porządek nie bierze się z dobrego serca. Bierze się z tego, że ktoś pilnuje.",
        "w_toku": "−1 do perswazji", "kara": {"perswazja": -1},
        "efekt": "+2 do zastraszania, ale −3 do morale osady", "premia": {"zastraszanie": 2}, "morale": -3,
    },
    "palenisko_krolestwem": {
        "nazwa": "Palenisko też bywa królestwem", "dni": 7,
        "tekst": "Nie trzeba korony, żeby ludzie przy twoim ogniu czuli się bezpieczni.",
        "w_toku": "−1 do oszustwa", "kara": {"oszustwo": -1},
        "efekt": "+5 do morale osady na stałe", "morale": 5,
    },
    "zimowy_glod": {
        "nazwa": "Pamięć głodnej zimy", "dni": 5,
        "tekst": "Kto raz liczył ziarna w garści, nigdy już nie zostawi spichlerza otwartego.",
        "w_toku": "−1 do przetrwania", "kara": {"przetrwanie": -1},
        "efekt": "żywność psuje się o połowę wolniej",
    },
    "wszystko_na_sprzedaz": {
        "nazwa": "Wszystko jest na sprzedaż", "dni": 5,
        "tekst": "Nawet cisza ma swoją cenę. Trzeba tylko wiedzieć, komu ją sprzedać.",
        "w_toku": "−1 do spostrzegawczości", "kara": {"spostrzegawczosc": -1},
        "efekt": "karawany i skup płacą o 5% więcej",
    },
    "cien_przodka": {
        "nazwa": "Cień przodka", "dni": 8,
        "tekst": "Chodzisz w butach kogoś, kto poległ. Jeszcze uwierają — ale prowadzą.",
        "w_toku": "−1 do wszystkich testów", "kara": {"*": -1},
        "efekt": "+1 do wszystkich testów na stałe", "premia": {"*": 1},
    },
}


def _mysli(gracz: "Gracz") -> dict[str, dict]:
    if getattr(gracz, "mysli", None) is None:
        gracz.mysli = {}
    return gracz.mysli


def stan(gracz: "Gracz", klucz: str) -> str | None:
    return (_mysli(gracz).get(klucz) or {}).get("stan")


def przyswojona(gracz: "Gracz", klucz: str) -> bool:
    return stan(gracz, klucz) == "przyswojona"


def miejsca(gracz: "Gracz") -> int:
    return 3 if talenty.ma(gracz, "gleboka_mysl") else 2


def odkryj(gracz: "Gracz", klucz: str) -> str:
    if klucz in _mysli(gracz):
        return ""
    _mysli(gracz)[klucz] = {"stan": "odkryta", "dni": 0}
    return f"  💭  NOWA MYŚL: „{MYSLI[klucz]['nazwa']}” — przyswój ją w gabinecie myśli ([21] w obozie)."


def premia_testu(gracz: "Gracz", skill: str) -> int:
    suma = 0
    for klucz, wpis in _mysli(gracz).items():
        info = MYSLI.get(klucz)
        if not info:
            continue
        slownik = info.get("premia", {}) if wpis["stan"] == "przyswojona" else (
            info.get("kara", {}) if wpis["stan"] == "w_toku" else {})
        suma += slownik.get(skill, 0) + slownik.get("*", 0)
    return suma


def premia_morale(gracz: "Gracz") -> float:
    return sum(MYSLI[k].get("morale", 0) for k, w in _mysli(gracz).items()
               if w["stan"] == "przyswojona" and k in MYSLI)


def dzien_mysli(gracz: "Gracz") -> list[str]:
    msgs = []
    tempo = 2 if talenty.ma(gracz, "gleboka_mysl") else 1
    for klucz, wpis in _mysli(gracz).items():
        if wpis["stan"] != "w_toku":
            continue
        wpis["dni"] -= tempo
        if wpis["dni"] <= 0:
            wpis["stan"] = "przyswojona"
            info = MYSLI[klucz]
            if klucz == "stal_pamieta":
                gracz.atak += 2
            msgs.append(f"  💡  MYŚL PRZYSWOJONA: „{info['nazwa']}” — {info['efekt']}.")
    return msgs


def menu_mysli(gracz: "Gracz") -> None:
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  🧠  GABINET MYŚLI")
        wyswietl_linie("═")
        w_toku = [k for k, w in _mysli(gracz).items() if w["stan"] == "w_toku"]
        print(f"\n  Miejsca: {len(w_toku)}/{miejsca(gracz)} zajęte. Myśl w toku ciąży; przyswojona zostaje na zawsze.\n")
        klucze = [k for k in _mysli(gracz) if k in MYSLI]
        if not klucze:
            print("  Nie masz jeszcze żadnych myśli. Przychodzą z rozmów, decyzji i tego, co przeżyjesz.\n")
        for i, k in enumerate(klucze, 1):
            info, wpis = MYSLI[k], _mysli(gracz)[k]
            if wpis["stan"] == "przyswojona":
                stan_txt = f"✅ przyswojona — {info['efekt']}"
            elif wpis["stan"] == "w_toku":
                stan_txt = f"⏳ przyswajasz ({max(0, wpis['dni'])} dni) — teraz: {info['w_toku']}"
            else:
                stan_txt = f"💭 odkryta — po przyswojeniu: {info['efekt']} ({info['dni']} dni)"
            print(f"  [{i}] „{info['nazwa']}”")
            print(f"      {info['tekst']}")
            print(f"      {stan_txt}\n")
        print("  [0] Wróć\n")
        wybor = input("  Przyswój / porzuć myśl nr: ").strip()
        if wybor == "0":
            return
        if not (wybor.isdigit() and 1 <= int(wybor) <= len(klucze)):
            continue
        k = klucze[int(wybor) - 1]
        wpis = _mysli(gracz)[k]
        if wpis["stan"] == "odkryta":
            if len(w_toku) >= miejsca(gracz):
                print("  Gabinet pełny. Poczekaj, aż któraś myśl dojrzeje, albo porzuć jedną.")
            else:
                wpis["stan"], wpis["dni"] = "w_toku", MYSLI[k]["dni"]
                print(f"  Zaczynasz przyswajać „{MYSLI[k]['nazwa']}”.")
        elif wpis["stan"] == "w_toku":
            wpis["stan"], wpis["dni"] = "odkryta", 0
            print("  Odkładasz tę myśl na później.")
        else:
            print("  Tej myśli już się nie pozbędziesz. Stała się częścią ciebie.")
        nacisnij_enter()
