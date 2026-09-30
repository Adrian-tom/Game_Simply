"""Osada: osadnicy z morale, cechami i doświadczeniem, zawody i łańcuchy produkcji.

Osada żyje co dzień — niezależnie od tego, czy bohater jest w obozie
(wcześniej targ płacił za „nieobecność”, a siedzenie w obozie dawało darmowe
złoto). Teraz każdy dzień to produkcja, ale też jedzenie, opał zimą,
psujące się zapasy i utrzymanie budynków. Dzienny rachunek liczy
``dzien_osady``, wywoływane przez ``game.swiat``.
"""

from __future__ import annotations

import math
import random
from typing import TYPE_CHECKING

from game import kalendarz, talenty
from game.oboz import (
    SUROWCE,
    _format_kosztu,
    _magazyn,
    _moze_zaplacic,
    _pobierz_koszt,
    dodaj_surowiec,
    linia_surowcow,
    ma_budynek,
    poziom_budynku,
    utrzymanie_dzienne,
)
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

MAX_CHATY = 12
KOSZT_CHATY = {"drewno": 8, "kamien": 4, "zloto": 20}
CENA_OSADNIKA = 28

_IMIONA_OSADNIKOW = (
    "Nessa", "Torin", "Elka", "Bram", "Sira", "Olek", "Mila", "Gareth", "Iva", "Piotr",
    "Ruta", "Kael", "Dobrosz", "Jagna", "Wit", "Hela", "Marek", "Zofka", "Lech", "Agna",
    "Borys", "Tamara", "Janko", "Ursa",
)

CECHY_OSADNIKOW: dict[str, dict] = {
    "pracowity": {"opis": "+25% pracy", "praca": 1.25},
    "leniwy": {"opis": "−25% pracy", "praca": 0.75},
    "zarloczny": {"opis": "je za dwóch", "je": 2},
    "skromny": {"opis": "łatwo go zadowolić (+5 morale)", "morale": 5},
    "odwazny": {"opis": "strażnik ×1,5, najazdy go nie łamią", "straz": 1.5, "nieustraszony": True},
    "tchorzliwy": {"opis": "strażnik ×0,5", "straz": 0.5},
    "zreczny": {"opis": "+25% w tartaku, hucie i warsztacie", "rzemioslo": 1.25},
    "gadatliwy": {"opis": "+30% w handlu", "handel": 1.3},
    "chorowity": {"opis": "choruje dwa razy częściej", "choroby": 2.0},
    "wesoly": {"opis": "pogodny (+10 morale)", "morale": 10},
}

# wymaga: budynek; miejsca: stanowisk na poziom budynku (None = bez limitu)
ZAJECIA: dict[str, dict] = {
    "bezczynny": {"nazwa": "bez zajęcia", "ikona": "💤", "opis": "odpoczywa"},
    "drwal": {"nazwa": "drwal", "ikona": "🪓", "opis": "+2 drewna"},
    "kamieniarz": {"nazwa": "kamieniarz", "ikona": "🪨", "opis": "+2 kamienia"},
    "zielarz": {"nazwa": "zielarz", "ikona": "🌿", "opis": "+2 zioła (zależnie od pory roku)"},
    "mysliwy": {"nazwa": "myśliwy", "ikona": "🏹", "opis": "+2 żywności, czasem skóra"},
    "rolnik": {"nazwa": "rolnik", "ikona": "🌾", "wymaga": "farma", "miejsca": 2,
               "opis": "+3 żywności × pora roku (jesień ×1,6, zima nic)"},
    "gornik": {"nazwa": "górnik", "ikona": "⛏", "opis": "+1–2 rudy"},
    "tracz": {"nazwa": "tracz", "ikona": "🪚", "wymaga": "tartak", "miejsca": 2,
              "opis": "2 drewna → 1 deska"},
    "hutnik": {"nazwa": "hutnik", "ikona": "🔥", "wymaga": "huta", "miejsca": 2,
               "opis": "2 rudy + 1 drewno → 1 żelazo"},
    "handlarz": {"nazwa": "handlarz", "ikona": "💰", "opis": "+2 zł, z targiem +4 i więcej"},
    "rzemieslnik": {"nazwa": "rzemieślnik", "ikona": "🔧", "wymaga": "warsztat", "miejsca": 2,
                    "opis": "wytwarza zamówienia z warsztatu i laboratorium"},
    "straznik": {"nazwa": "strażnik", "ikona": "🛡", "opis": "+6 do obrony osady"},
    "uzdrowiciel": {"nazwa": "uzdrowiciel", "ikona": "⚕", "wymaga": "lecznica", "miejsca": 1,
                    "opis": "leczy chorych, przyspiesza gojenie ran"},
}
ALIASY_ZAJEC = {"zbiory": "drwal", "handel": "handlarz", "rzemioslo": "rzemieslnik"}
_PROGI_DOSWIADCZENIA = (10, 30, 60)

CENY_SUROWCOW = {
    "zywnosc": 3, "drewno": 2, "kamien": 2, "ziola": 4, "skora": 5, "ruda": 8, "deski": 6, "zelazo": 16,
}

PRACE: list[dict] = [
    {"nazwa": "Rąbanie drewna", "ikona": "🪓", "opis": "Kilka godzin przy siekierze.", "czas": 2,
     "nagrody": {"drewno": (2, 4), "zloto": (1, 3)}},
    {"nazwa": "Łamanie kamienia", "ikona": "⛏", "opis": "Żwir i głazy na fundamenty chat.", "czas": 2,
     "nagrody": {"kamien": (2, 3), "zloto": (1, 2)}},
    {"nazwa": "Zbieranie ziół", "ikona": "🌿", "opis": "Skrawek łąki za obozem.", "czas": 2,
     "nagrody": {"ziola": (2, 4), "zloto": (1, 3)}},
    {"nazwa": "Polowanie", "ikona": "🏹", "opis": "Sidła i cisza w lesie.", "czas": 2,
     "nagrody": {"zywnosc": (2, 5), "skora": (0, 2)}},
    {"nazwa": "Praca na targu", "ikona": "🛒", "opis": "Liczenie monet i przekonywanie chłopów.", "czas": 2,
     "wymaga": "targ", "nagrody": {"zloto": (8, 14)}},
]


# ------------------------------------------------------------------ #
#  Czas — cienkie opakowanie na dzienny cykl świata                    #
# ------------------------------------------------------------------ #

def dodaj_czas(gracz: Gracz, ile: int = 1) -> None:
    """Mija ``ile`` dni: produkcja, jedzenie, zagrożenie, karawany. Pilne wieści od razu na ekran."""
    from game.swiat import minij_dni
    for msg in minij_dni(gracz, ile):
        print(msg)


def oznacz_wyjscie(gracz: Gracz) -> None:
    gracz.czas_wyjscia = getattr(gracz, "czas", 0)
    gracz.w_obozie = False


def rozlicz_powrot_do_obozu(gracz: Gracz) -> list[str]:
    """Powrót (albo koniec dnia w obozie): wieści z osady, które czekały na bohatera."""
    gracz.w_obozie = True
    wiesci = list(getattr(gracz, "kronika", None) or [])
    gracz.kronika = []
    if len(wiesci) > 12:
        pominiete = len(wiesci) - 12
        wiesci = wiesci[-12:]
        wiesci.insert(0, f"  📰  (… i {pominiete} starszych wieści)")
    return wiesci


# ------------------------------------------------------------------ #
#  Osadnicy                                                            #
# ------------------------------------------------------------------ #

def liczba_chat(gracz: Gracz) -> int:
    return int(getattr(gracz, "chaty", 0) or 0)


def _zapewnij_osadnika(o: dict) -> dict:
    """Stary zapis: dopisz morale, cechę, doświadczenie; stare zajęcia → nowe."""
    o["zajecie"] = ALIASY_ZAJEC.get(o.get("zajecie"), o.get("zajecie") or "drwal")
    if o["zajecie"] not in ZAJECIA:
        o["zajecie"] = "drwal"
    o.setdefault("morale", 60.0)
    o.setdefault("cecha", random.choice(list(CECHY_OSADNIKOW)))
    o.setdefault("dosw", {})
    o.setdefault("chory", 0)
    o.pop("ikona", None)
    return o


def osadnicy(gracz: Gracz) -> list[dict]:
    if getattr(gracz, "osadnicy", None) is None:
        gracz.osadnicy = []
    for o in gracz.osadnicy:
        _zapewnij_osadnika(o)
    return gracz.osadnicy


def wolne_chaty(gracz: Gracz) -> int:
    return max(0, liczba_chat(gracz) - len(osadnicy(gracz)))


def ludnosc(gracz: Gracz) -> int:
    """Wszyscy, którzy jedzą z magazynu (bez bohatera): osadnicy i drużyna."""
    return len(osadnicy(gracz)) + len(getattr(gracz, "rekruci", None) or [])


def miejsca(gracz: Gracz, zajecie: str) -> int | None:
    info = ZAJECIA[zajecie]
    if "wymaga" not in info:
        return None
    poziom = poziom_budynku(gracz, info["wymaga"])
    if zajecie == "rzemieslnik":
        poziom += poziom_budynku(gracz, "laboratorium")
    return info.get("miejsca", 1) * poziom


def ilu_w_zawodzie(gracz: Gracz, zajecie: str) -> int:
    return sum(1 for o in osadnicy(gracz) if o["zajecie"] == zajecie)


def ustaw_zajecie(gracz: Gracz, o: dict, zajecie: str) -> str:
    zajecie = ALIASY_ZAJEC.get(zajecie, zajecie)
    info = ZAJECIA.get(zajecie)
    if not info:
        return "  Nie ma takiego zajęcia."
    if o.get("zajecie") == zajecie:
        return f"  {o['imie']} już pracuje jako {info['nazwa']}."
    wolne = miejsca(gracz, zajecie)
    if wolne is not None:
        if wolne <= 0:
            return f"  Potrzebny budynek: {info['wymaga']}."
        if ilu_w_zawodzie(gracz, zajecie) >= wolne:
            return f"  Brak wolnych stanowisk ({wolne}) — rozbuduj {info['wymaga']}."
    o["zajecie"] = zajecie
    return f"  {info['ikona']}  {o['imie']} zostaje: {info['nazwa']}."


def poziom_doswiadczenia(o: dict, zajecie: str | None = None) -> int:
    xp = (o.get("dosw") or {}).get(zajecie or o["zajecie"], 0)
    return sum(1 for prog in _PROGI_DOSWIADCZENIA if xp >= prog)


def mnoznik_pracy(gracz: Gracz, o: dict) -> float:
    """Morale (0,6–1,4) × cecha × doświadczenie (+10%/stopień) × talent Gospodarz."""
    cecha = CECHY_OSADNIKOW.get(o.get("cecha"), {})
    m = 0.6 + o.get("morale", 60) / 125
    m *= cecha.get("praca", 1.0)
    if o["zajecie"] in ("tracz", "hutnik", "rzemieslnik"):
        m *= cecha.get("rzemioslo", 1.0)
    if o["zajecie"] == "handlarz":
        m *= cecha.get("handel", 1.0)
    m *= 1 + 0.10 * poziom_doswiadczenia(o)
    if talenty.ma(gracz, "gospodarz"):
        m *= 1.10
    return m


def _zaokraglij(x: float) -> int:
    """Losowe zaokrąglenie — 1,3 dnia pracy daje czasem 1, czasem 2."""
    calosc = int(x)
    return calosc + (1 if random.random() < x - calosc else 0)


def zatrudnij_osadnika(gracz: Gracz, zajecie: str = "drwal", *, darmo: bool = False) -> str:
    if wolne_chaty(gracz) <= 0:
        return "  Brak wolnej chaty. Zbuduj chatę w rozbudowie obozu."
    if not darmo and gracz.zloto < CENA_OSADNIKA:
        return f"  Osadnik chce {CENA_OSADNIKA} złota za przeprowadzkę."
    zajete = {o.get("imie") for o in osadnicy(gracz)}
    pula = [n for n in _IMIONA_OSADNIKOW if n not in zajete] or list(_IMIONA_OSADNIKOW)
    imie = random.choice(pula)
    if not darmo:
        gracz.zloto -= CENA_OSADNIKA
    nowy = _zapewnij_osadnika({"imie": imie, "zajecie": "bezczynny", "morale": 60.0})
    osadnicy(gracz).append(nowy)
    komunikat = ustaw_zajecie(gracz, nowy, zajecie)
    if "zostaje" not in komunikat:
        nowy["zajecie"] = "drwal"
    gracz.statystyki["zatrudnieni_osadnicy"] = gracz.statystyki.get("zatrudnieni_osadnicy", 0) + 1
    cecha = nowy["cecha"]
    info = ZAJECIA[nowy["zajecie"]]
    koszt = "za darmo" if darmo else f"-{CENA_OSADNIKA} złota"
    return (
        f"  {info['ikona']}  {imie} ({cecha}: {CECHY_OSADNIKOW[cecha]['opis']}) wprowadza się"
        f" i zostaje: {info['nazwa']}.  ({koszt})"
    )


def zbuduj_chate(gracz: Gracz) -> str:
    ile = liczba_chat(gracz)
    if ile >= MAX_CHATY:
        return f"  Osada nie pomieści więcej chat (maks. {MAX_CHATY})."
    if not _moze_zaplacic(gracz, KOSZT_CHATY):
        return f"  Brakuje materiałów na chatę. Potrzeba: {_format_kosztu(KOSZT_CHATY)}."
    _pobierz_koszt(gracz, KOSZT_CHATY)
    gracz.chaty = ile + 1
    gracz.statystyki["zbudowane_chaty"] = gracz.chaty
    return f"  🛖  Wznosisz chatę ({gracz.chaty}/{MAX_CHATY}). Ktoś może się tu wprowadzić."


def srednie_morale(gracz: Gracz) -> float:
    lista = osadnicy(gracz)
    return sum(o["morale"] for o in lista) / len(lista) if lista else 0.0


def ikona_morale(m: float) -> str:
    return "😄" if m >= 75 else "🙂" if m >= 55 else "😐" if m >= 35 else "😠"


# ------------------------------------------------------------------ #
#  Dzień osady                                                         #
# ------------------------------------------------------------------ #

def _bilans(gracz: Gracz) -> dict:
    flagi = gracz.flagi
    flagi["bilans_dnia"] = {}
    return flagi["bilans_dnia"]


def _plus(bilans: dict, klucz: str, ile: float) -> None:
    if ile:
        bilans[klucz] = bilans.get(klucz, 0) + ile


def dzien_osady(gracz: Gracz) -> tuple[list[str], list[str]]:
    """Jeden dzień życia osady. Zwraca (wieści do kroniki, wieści pilne)."""
    if getattr(gracz, "flagi", None) is None:
        gracz.flagi = {}
    kronika: list[str] = []
    pilne: list[str] = []
    mag = _magazyn(gracz)
    pora = kalendarz.pora(gracz)
    bilans = _bilans(gracz)
    lista = osadnicy(gracz)

    # --- 1. praca ---
    rzemieslnicy = 0.0
    narzedzia = int(getattr(gracz, "narzedzia", 0) or 0)
    wolne_narzedzia = narzedzia
    for o in lista:
        if o.get("chory", 0) > 0 or o["zajecie"] == "bezczynny":
            continue
        m = mnoznik_pracy(gracz, o)
        if wolne_narzedzia > 0 and o["zajecie"] not in ("handlarz", "straznik"):
            m *= 1.25
            wolne_narzedzia -= 1
        z = o["zajecie"]
        if z == "drwal":
            _plus(bilans, "drewno", _zaokraglij(2 * m))
        elif z == "kamieniarz":
            _plus(bilans, "kamien", _zaokraglij(2 * m))
        elif z == "zielarz":
            _plus(bilans, "ziola", _zaokraglij(2 * m * pora["zbiory"]))
        elif z == "mysliwy":
            _plus(bilans, "zywnosc", _zaokraglij(2 * m * max(0.5, pora["zbiory"])))
            if random.random() < 0.4:
                _plus(bilans, "skora", 1)
        elif z == "rolnik":
            _plus(bilans, "zywnosc", _zaokraglij(3 * m * pora["rolnictwo"]))
        elif z == "gornik":
            _plus(bilans, "ruda", _zaokraglij(1.3 * m))
        elif z == "tracz":
            partie = min(_zaokraglij(m), (mag.get("drewno", 0) + bilans.get("drewno", 0)) // 2)
            _plus(bilans, "drewno", -2 * partie)
            _plus(bilans, "deski", partie)
        elif z == "hutnik":
            dostepne_ruda = mag.get("ruda", 0) + bilans.get("ruda", 0)
            dostepne_drewno = mag.get("drewno", 0) + bilans.get("drewno", 0)
            partie = min(_zaokraglij(m), dostepne_ruda // 2, dostepne_drewno)
            _plus(bilans, "ruda", -2 * partie)
            _plus(bilans, "drewno", -partie)
            _plus(bilans, "zelazo", partie)
        elif z == "handlarz":
            baza = 4 * (1 + 0.25 * (poziom_budynku(gracz, "targ") - 1)) if ma_budynek(gracz, "targ") else 2
            if talenty.ma(gracz, "lichwiarz"):
                baza *= 1.5
            _plus(bilans, "zloto", _zaokraglij(baza * m))
        elif z == "rzemieslnik":
            rzemieslnicy += m
        o.setdefault("dosw", {})[z] = o["dosw"].get(z, 0) + 1

    # drużyna na zbiorach też pracuje co dzień (połowa tego, co przynosiła z wyprawy)
    from game.rekruci import REKRUCI
    for r in getattr(gracz, "rekruci", None) or []:
        info = REKRUCI.get(r.get("klucz"))
        if info and r.get("zajecie") == "zbiory":
            mn, mx = info["zbior_ile"]
            _plus(bilans, info["zbior"], _zaokraglij(random.uniform(mn, mx) / 2))

    zuzyte = sum(1 for _ in range(narzedzia - wolne_narzedzia) if random.random() < 0.02)
    if zuzyte:
        gracz.narzedzia = narzedzia - zuzyte
        kronika.append(f"  🔧  Zużyło się {zuzyte} kompletów narzędzi (zostało {gracz.narzedzia}).")

    # Dziesięcina: zadowoleni osadnicy dokładają się do wspólnej kasy.
    zadowoleni = sum(1 for o in lista if o.get("morale", 0) >= 40 and not o.get("chory"))
    _plus(bilans, "zloto", _zaokraglij(0.5 * zadowoleni))

    if ma_budynek(gracz, "targ"):
        stragany = 2 * poziom_budynku(gracz, "targ") * (1.5 if talenty.ma(gracz, "lichwiarz") else 1.0)
        _plus(bilans, "zloto", _zaokraglij(stragany))

    for klucz, ile in list(bilans.items()):
        if klucz == "zloto":
            gracz.zloto += int(ile)
        elif klucz in SUROWCE:
            mag[klucz] = max(0, mag.get(klucz, 0) + int(ile))

    if rzemieslnicy:
        from game.rzemioslo import praca_rzemieslnikow
        for msg in praca_rzemieslnikow(gracz, rzemieslnicy):
            kronika.append(msg)

    # --- 2. utrzymanie budynków ---
    utrzymanie = utrzymanie_dzienne(gracz)
    zaniedbanie = False
    if utrzymanie:
        if gracz.zloto >= utrzymanie:
            gracz.zloto -= utrzymanie
        else:
            gracz.zloto = 0
            zaniedbanie = True
            kronika.append(f"  💸  Zabrakło {utrzymanie} zł na utrzymanie budynków — ludzie narzekają.")
        _plus(bilans, "zloto", -utrzymanie)

    # --- 3. jedzenie ---
    potrzeba = sum(CECHY_OSADNIKOW.get(o.get("cecha"), {}).get("je", 1) for o in lista)
    potrzeba += len(getattr(gracz, "rekruci", None) or [])
    zjedzone = min(potrzeba, mag.get("zywnosc", 0))
    mag["zywnosc"] = mag.get("zywnosc", 0) - zjedzone
    _plus(bilans, "zywnosc", -zjedzone)
    glodni = potrzeba > 0 and zjedzone < potrzeba
    if glodni:
        gracz.flagi["dni_glodu_osady"] = gracz.flagi.get("dni_glodu_osady", 0) + 1
        dni = gracz.flagi["dni_glodu_osady"]
        pilne.append(f"  🍽  GŁÓD W OSADZIE ({dni}. dzień): brakuje {potrzeba - zjedzone} racji! Morale spada.")
    else:
        gracz.flagi["dni_glodu_osady"] = 0

    # --- 4. opał zimą ---
    zimno = False
    if pora["opal"] and (lista or getattr(gracz, "w_obozie", True)):
        opal = 1 + math.ceil(len(lista) / 4)
        spalone = min(opal, mag.get("drewno", 0))
        mag["drewno"] = mag.get("drewno", 0) - spalone
        _plus(bilans, "drewno", -spalone)
        if spalone < opal:
            zimno = True
            gracz.flagi["zimno"] = True
            pilne.append("  ❄  Brak drewna na opał — w chatach mróz. Ludzie chorują, morale leci.")
        else:
            gracz.flagi["zimno"] = False
    else:
        gracz.flagi["zimno"] = False

    # --- 5. psucie żywności ---
    tempo = {0: 0.03, 1: 0.01, 2: 0.005, 3: 0.0}[min(3, poziom_budynku(gracz, "spichlerz"))]
    from game.mysli import przyswojona
    if przyswojona(gracz, "zimowy_glod"):
        tempo /= 2
    if tempo and mag.get("zywnosc", 0) > 0:
        zepsute = _zaokraglij(mag["zywnosc"] * tempo)
        if zepsute:
            mag["zywnosc"] -= zepsute
            _plus(bilans, "zywnosc", -zepsute)
            gracz.flagi["zepsute_razem"] = gracz.flagi.get("zepsute_razem", 0) + zepsute

    # --- 6. morale, choroby, odejścia ---
    kronika += _morale_i_zdrowie(gracz, glodni, zimno, zaniedbanie, pilne)

    # --- 7. wieść niesie się sama ---
    if talenty.ma(gracz, "legenda_osady") and wolne_chaty(gracz) > 0 and random.random() < 0.04:
        kronika.append("  🌟  " + zatrudnij_osadnika(gracz, "drwal", darmo=True).strip() + " Przyciągnęła go twoja sława.")
    return kronika, pilne


def _cel_morale(gracz: Gracz, o: dict, glodni: bool, zimno: bool, zaniedbanie: bool) -> float:
    cel = 55.0
    cel += -25 if glodni else 8
    if zimno:
        cel -= 15
    if zaniedbanie:
        cel -= 5
    cel += 6 * poziom_budynku(gracz, "tawerna")
    karma = int(getattr(gracz, "karma", 0) or 0)
    cel += 5 if karma >= 5 else -5 if karma <= -5 else 0
    cel += CECHY_OSADNIKOW.get(o.get("cecha"), {}).get("morale", 0)
    if talenty.ma(gracz, "charyzmatyczny_wodz"):
        cel += 10
    cel += float(gracz.flagi.get("morale_wydarzenia", 0))
    from game.mysli import premia_morale
    cel += premia_morale(gracz)
    if o.get("chory", 0) > 0:
        cel -= 10
    return max(0.0, min(100.0, cel))


def _morale_i_zdrowie(gracz: Gracz, glodni: bool, zimno: bool, zaniedbanie: bool, pilne: list[str]) -> list[str]:
    msgs: list[str] = []
    lista = osadnicy(gracz)
    lecznica = poziom_budynku(gracz, "lecznica")
    uzdrowiciele = ilu_w_zawodzie(gracz, "uzdrowiciel")
    for o in list(lista):
        cel = _cel_morale(gracz, o, glodni, zimno, zaniedbanie)
        o["morale"] = round(o["morale"] + (cel - o["morale"]) * 0.25, 1)
        if o.get("chory", 0) > 0:
            o["chory"] -= 1 + (1 if uzdrowiciele else 0)
            if (glodni or zimno) and random.random() < 0.04:
                lista.remove(o)
                gracz.flagi["morale_wydarzenia"] = gracz.flagi.get("morale_wydarzenia", 0) - 8
                pilne.append(f"  ⚰  {o['imie']} umiera z choroby, głodu i zimna. Osada pogrąża się w żałobie.")
                continue
            if o["chory"] <= 0:
                o["chory"] = 0
                msgs.append(f"  💚  {o['imie']} wraca do zdrowia.")
        else:
            szansa = 0.006 * CECHY_OSADNIKOW.get(o.get("cecha"), {}).get("choroby", 1.0)
            szansa *= (2 if glodni else 1) * (2 if zimno else 1) * (0.5 if lecznica else 1)
            if random.random() < szansa:
                o["chory"] = random.randint(3, 6)
                msgs.append(f"  🤒  {o['imie']} zachorował(a) — nie pracuje przez kilka dni.")
        if o["morale"] < 20 and random.random() < 0.10:
            lista.remove(o)
            pilne.append(f"  🚪  {o['imie']} ma dość (morale {o['morale']:.0f}) i odchodzi z osady.")
    # wydarzenia wygasają
    mod = float(gracz.flagi.get("morale_wydarzenia", 0))
    if mod:
        gracz.flagi["morale_wydarzenia"] = round(mod * 0.85, 1) if abs(mod) > 0.5 else 0
    return msgs


def bilans_zywnosci(gracz: Gracz) -> int:
    """Ile żywności przybyło/ubyło wczoraj (dodatnie = nadwyżka)."""
    return int((getattr(gracz, "flagi", None) or {}).get("bilans_dnia", {}).get("zywnosc", 0))


def zmien_morale(gracz: Gracz, ile: float) -> None:
    """Jednorazowe wydarzenie (najazd, święto, wyrok) — wygasa z dnia na dzień."""
    gracz.flagi["morale_wydarzenia"] = float(gracz.flagi.get("morale_wydarzenia", 0)) + ile


# ------------------------------------------------------------------ #
#  Menu osady                                                          #
# ------------------------------------------------------------------ #

def _linia_bilansu(gracz: Gracz) -> str:
    bilans = (getattr(gracz, "flagi", None) or {}).get("bilans_dnia") or {}
    if not bilans:
        return "  Wczoraj: — (bilans pojawi się po pierwszym dniu)"
    czesci = []
    for k in ["zloto"] + list(SUROWCE):
        v = int(bilans.get(k, 0))
        if v:
            ikona = "💰" if k == "zloto" else SUROWCE[k]["ikona"]
            czesci.append(f"{ikona}{v:+d}")
    return "  Wczoraj: " + ("  ".join(czesci) if czesci else "bez zmian")


def _menu_zajec(gracz: Gracz, o: dict) -> None:
    cecha = o.get("cecha")
    print(f"\n  {o['imie']} — {cecha} ({CECHY_OSADNIKOW[cecha]['opis']}), morale {o['morale']:.0f}")
    doswiadczenie = ", ".join(
        f"{ZAJECIA[z]['nazwa']} {'★' * poziom_doswiadczenia(o, z)}" for z, xp in (o.get("dosw") or {}).items()
        if poziom_doswiadczenia(o, z) and z in ZAJECIA
    )
    if doswiadczenie:
        print(f"  Doświadczenie: {doswiadczenie}")
    klucze = [z for z in ZAJECIA]
    for i, z in enumerate(klucze, 1):
        info = ZAJECIA[z]
        m = miejsca(gracz, z)
        limit = "" if m is None else f"  [{ilu_w_zawodzie(gracz, z)}/{m}]" if m else f"  (wymaga: {info['wymaga']})"
        print(f"  [{i:>2}] {info['ikona']} {info['nazwa']} — {info['opis']}{limit}")
    print("  [W] 🚪 Wypędź z osady")
    print("  [0] ↩ Wróć\n")
    wybor = input("  Nowe zajęcie: ").strip().lower()
    if wybor == "w":
        osadnicy(gracz).remove(o)
        zmien_morale(gracz, -5)
        print(f"  {o['imie']} odchodzi. Inni patrzą na to niechętnie (−5 morale).")
    elif wybor.isdigit() and 1 <= int(wybor) <= len(klucze):
        print(ustaw_zajecie(gracz, o, klucze[int(wybor) - 1]))
    else:
        return
    nacisnij_enter()


def menu_osady(gracz: Gracz) -> None:
    """Osadnicy, zawody, bilans dnia, zamówienia, targ."""
    from game.obrona import sila_obrony
    while True:
        wyczysc()
        wyswietl_linie("═")
        print(f"  OSADA  —  {kalendarz.opis_daty(gracz)}")
        wyswietl_linie("═")
        mag = _magazyn(gracz)
        lista = osadnicy(gracz)
        zywnosc = mag.get("zywnosc", 0)
        dzienne = bilans_zywnosci(gracz)
        zapas = f"starczy na ~{zywnosc // max(1, -dzienne)} dni" if dzienne < 0 else "zapasy rosną"
        print(f"\n  Ludność: {len(lista)} osadników + {len(gracz.rekruci or [])} w drużynie   Chaty: {liczba_chat(gracz)}/{MAX_CHATY}")
        print(f"  Morale: {ikona_morale(srednie_morale(gracz))} {srednie_morale(gracz):.0f}   Obrona: {sila_obrony(gracz)}"
              f"   Utrzymanie: {utrzymanie_dzienne(gracz)} zł/dz.")
        print(f"  Żywność: {zywnosc} ({dzienne:+d}/dz., {zapas})   Do zimy: {kalendarz.dni_do_zimy(gracz)} dni")
        print(_linia_bilansu(gracz))
        print(f"  {linia_surowcow(gracz)}   Złoto: {gracz.zloto}\n")
        if not lista:
            print("  Nikt jeszcze nie mieszka w chatach. Zbuduj chatę ([11] w obozie) i zatrudnij osadnika.\n")
        for i, o in enumerate(lista, 1):
            info = ZAJECIA[o["zajecie"]]
            gwiazdki = "★" * poziom_doswiadczenia(o)
            chory = "  🤒 chory" if o.get("chory") else ""
            print(f"  [{i:>2}] {ikona_morale(o['morale'])} {o['imie']:8} {info['ikona']} {info['nazwa']} {gwiazdki}"
                  f"  · {o['cecha']}  · morale {o['morale']:.0f}{chory}")
        print()
        print(f"  [N]  🤝  Zatrudnij osadnika ({CENA_OSADNIKA} zł, potrzebna wolna chata: {wolne_chaty(gracz)})")
        print("  [Z]  📋  Zamówienia dla rzemieślników")
        if ma_budynek(gracz, "targ"):
            print("  [T]  🛒  Sprzedaj surowce na targu")
        print("  [0]  ↩  Wróć\n")
        wybor = input("  Twój wybór: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "n":
            print("\n  Czym ma się zająć? (zmienisz to potem w każdej chwili)")
            klucze = [z for z in ZAJECIA if z != "bezczynny"]
            for i, z in enumerate(klucze, 1):
                print(f"  [{i:>2}] {ZAJECIA[z]['ikona']} {ZAJECIA[z]['nazwa']}")
            z = input("  Zajęcie: ").strip()
            zaj = klucze[int(z) - 1] if z.isdigit() and 1 <= int(z) <= len(klucze) else "drwal"
            print(zatrudnij_osadnika(gracz, zaj))
            nacisnij_enter()
            continue
        if wybor == "z":
            from game.rzemioslo import menu_zamowien
            menu_zamowien(gracz)
            continue
        if wybor == "t" and ma_budynek(gracz, "targ"):
            menu_sprzedazy_surowcow(gracz)
            continue
        if wybor.isdigit() and 1 <= int(wybor) <= len(lista):
            _menu_zajec(gracz, lista[int(wybor) - 1])
            continue
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


# ------------------------------------------------------------------ #
#  Praca bohatera, warsztat, sprzedaż                                  #
# ------------------------------------------------------------------ #

def _wykonaj_prace(gracz: Gracz, praca: dict) -> None:
    nagrody = praca.get("nagrody") or {}
    print(f"\n  {praca['nazwa']}. Mija {praca['czas']} dni.")
    for klucz, zakres in nagrody.items():
        mn, mx = zakres
        ile = random.randint(mn, mx)
        if ile <= 0:
            continue
        if klucz == "zloto":
            gracz.zloto += ile
            print(f"  💰  +{ile} złota")
        else:
            dodaj_surowiec(gracz, klucz, ile)
            info = SUROWCE[klucz]
            print(f"  {info['ikona']}  +{ile} {info['nazwa']}")
    dodaj_czas(gracz, int(praca.get("czas", 1)))
    print(f"  {linia_surowcow(gracz)}")
    print(f"  Złoto: {gracz.zloto} szt.")


def menu_pracy(gracz: Gracz) -> None:
    """Praca fizyczna bohatera w obozie — surowce i trochę złota, mija czas."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  PRACA W OBOZIE")
        wyswietl_linie("═")
        print(f"\n  {kalendarz.opis_daty(gracz)}")
        print(f"  {linia_surowcow(gracz)}")
        print(f"  Złoto: {gracz.zloto} szt.\n")
        print("  Każdy dzień pracy to też dzień osady: jedzenie, utrzymanie, zagrożenie.\n")
        dostepne = [p for p in PRACE if not p.get("wymaga") or ma_budynek(gracz, p["wymaga"])]
        for i, praca in enumerate(dostepne, 1):
            print(f"  [{i}] {praca.get('ikona', '⚒')} {praca['nazwa']}  ({praca['czas']} dni) — {praca['opis']}")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        if wybor.isdigit() and 1 <= int(wybor) <= len(dostepne):
            _wykonaj_prace(gracz, dostepne[int(wybor) - 1])
            nacisnij_enter()
            return
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def menu_warsztatu(gracz: Gracz) -> None:
    from game.rzemioslo import menu_rzemiosla
    menu_rzemiosla(gracz)


def cena_skupu(gracz: Gracz, klucz: str) -> int:
    cena = CENY_SUROWCOW[klucz]
    if talenty.ma(gracz, "targowanie"):
        cena = cena * 1.1
    from game.mysli import przyswojona
    if przyswojona(gracz, "wszystko_na_sprzedaz"):
        cena *= 1.05
    if klucz == "zywnosc":
        cena *= kalendarz.pora(gracz)["ceny_zywnosci"]
    return max(1, int(round(cena)))


def menu_sprzedazy_surowcow(gracz: Gracz) -> None:
    """Sprzedaż surowców po cenach lokalnych (targ w obozie / magazyn miejski)."""
    while True:
        mag = _magazyn(gracz)
        wyczysc()
        wyswietl_linie("═")
        print("  SPRZEDAŻ SUROWCÓW")
        wyswietl_linie("═")
        print(f"\n  Złoto: {gracz.zloto} szt.   (lepsze ceny dają karawany do innych osad)\n")
        klucze = list(SUROWCE)
        for i, k in enumerate(klucze, 1):
            info = SUROWCE[k]
            print(f"  [{i}] {info['ikona']} {info['nazwa']}: {mag.get(k, 0)}  (cena {cena_skupu(gracz, k)} zł / szt.)")
        print("  [0] Wróć\n")
        wybor = input("  Co sprzedajesz: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(klucze):
                k = klucze[idx]
                if mag.get(k, 0) <= 0:
                    print("  Nie masz tego surowca.")
                    nacisnij_enter()
                    continue
                ile = int(input(f"  Ile sztuk {SUROWCE[k]['nazwa']}? ").strip())
                if ile <= 0 or mag.get(k, 0) < ile:
                    print("  Tyle nie masz.")
                    nacisnij_enter()
                    continue
                mag[k] -= ile
                zysk = ile * cena_skupu(gracz, k)
                gracz.zloto += zysk
                print(f"  Sprzedano {ile} × {SUROWCE[k]['nazwa']} za {zysk} złota.")
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
