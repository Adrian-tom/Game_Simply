"""Badania: skryptorium, epoki i poznawanie nowych budowli — jak stół badawczy w RimWorldzie.

Na starcie osada umie niewiele: dom, pola, palisadę i skryptorium. Resztę —
tartak, kuźnię, targ, wyższe poziomy budynków i więcej chat — trzeba **poznać**.
W skryptorium siedzą badacze (jeden na poziom budynku), każdy dzień pracy daje
punkty wiedzy, a projekt kończy się, gdy zbierze się ich tyle, ile kosztuje.

Są trzy epoki. Wejście w następną to osobny, drogi projekt przełomowy, a same
projekty wyższej epoki wymagają skryptorium na odpowiednim poziomie — więc
„wyższy poziom budowy” otwiera się przez naukę i przez rozbudowę biblioteki:

* **I. Drewno i kamień** — podstawy: obróbka drewna, kamieniarstwo, handel;
* **II. Żelazo i rzemiosło** — huta, kuźnia, mury, szlaki handlowe;
* **III. Mistrzostwo** — alchemia, fortyfikacje, gildie, wielka architektura.

Stan trzymamy w ``gracz.badania``: ``zrobione`` (lista id), ``aktualny``,
``postep`` (id → punkty) i ``zapas`` (punkty wypracowane bez projektu — nie giną
w całości, żeby jeden dzień bez wyboru nie kosztował pracy badaczy).
"""
from __future__ import annotations

import math
from typing import TYPE_CHECKING

from game import atrybuty, kalendarz, kompetencje
from game.oboz import BUDYNKI, poziom_budynku
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

ERY: dict[int, str] = {1: "Drewno i kamień", 2: "Żelazo i rzemiosło", 3: "Mistrzostwo"}

# Co osada umie bez żadnych badań: budynek → najwyższy poziom, jaki można postawić.
START_POZIOMY: dict[str, int] = {"dom": 1, "farma": 1, "palisada": 1, "skryptorium": 1}
START_CHATY = 4

BAZA_PUNKTOW = 5.0             # punktów wiedzy dziennie od jednego badacza o mnożniku 1,0
PREMIA_SKRYPTORIUM = 0.35      # za każdy poziom skryptorium ponad pierwszy
PUNKTY_BOHATERA = 4            # dzień studiów bohatera (plus premia z inteligencji)
LIMIT_ZAPASU = 20.0
SKALA_KOSZTU = 1.4             # jedno pokrętło tempa nauki; wartości w PROJEKTY to „bazowe”, tu mnożone


def _p(nazwa: str, era: int, koszt: int, opis: str, *, wymaga: tuple = (), odblokowuje: dict | None = None,
       chaty: int | None = None, era_po: int | None = None, bonus: dict | None = None) -> dict:
    return {"nazwa": nazwa, "era": era, "koszt": round(koszt * SKALA_KOSZTU), "opis": opis, "wymaga": wymaga,
            "odblokowuje": odblokowuje or {}, "chaty": chaty, "era_po": era_po, "bonus": bonus or {}}


PROJEKTY: dict[str, dict] = {
    # ---- Epoka I: drewno i kamień ------------------------------------------------
    "obrobka_drewna": _p("Obróbka drewna", 1, 20, "Piły i kliny: deski z pni.",
                         odblokowuje={"tartak": 1}, bonus={"tracz": 1.10}),
    "kamieniarstwo": _p("Kamieniarstwo", 1, 25, "Łupanie, ciosanie i układanie kamienia.",
                        odblokowuje={"spichlerz": 1}, bonus={"kamieniarz": 1.10}),
    "plodozmian": _p("Płodozmian", 1, 25, "Pola odpoczywają na przemian — plony rosną.",
                     odblokowuje={"farma": 2}, bonus={"rolnik": 1.10}),
    "handel": _p("Handel i miara", 1, 30, "Wagi, odważniki i umowy ustne zamieniają się w targ.",
                 wymaga=("obrobka_drewna",), odblokowuje={"sklep": 1, "targ": 1}, bonus={"handlarz": 1.10}),
    "zielarstwo": _p("Zielarstwo", 1, 25, "Które liście leczą, a które trują.",
                     odblokowuje={"lecznica": 1}, bonus={"zielarz": 1.15}),
    "wspolne_ognisko": _p("Wspólne ognisko", 1, 25, "Wieczory przy kuflu zbliżają ludzi.",
                          wymaga=("obrobka_drewna",), odblokowuje={"tawerna": 1}),
    "ostrokol": _p("Ostrokół", 1, 30, "Zaostrzone pale, wieża z bali i warta na szczycie.",
                   wymaga=("obrobka_drewna", "kamieniarstwo"),
                   odblokowuje={"palisada": 2, "wieza": 1}, bonus={"straznik": 1.10}),
    "zwoje": _p("Zwoje i woskówki", 1, 35, "Zapisana wiedza nie ginie z człowiekiem. Większe skryptorium.",
                odblokowuje={"skryptorium": 2}),
    "osadnictwo": _p("Osadnictwo", 1, 30, "Plan wsi: do ośmiu chat.", chaty=8),
    "wiek_zelaza": _p("Wiek żelaza", 1, 60, "Przełom: metal zastępuje kamień i kość.",
                      wymaga=("obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje"), era_po=2),
    # ---- Epoka II: żelazo i rzemiosło --------------------------------------------
    "wytop": _p("Wytop rudy", 2, 60, "Piec, miech i żar: z rudy robi się żelazo.",
                odblokowuje={"huta": 1}, bonus={"hutnik": 1.10, "gornik": 1.10}),
    "kowalstwo": _p("Kowalstwo", 2, 70, "Kuźnia, kowadło i młot — broń oraz narzędzia.",
                    wymaga=("wytop",), odblokowuje={"kuznia": 1, "huta": 2, "tartak": 2}),
    "rzemioslo": _p("Warsztaty rzemieślnicze", 2, 60, "Stół, imadło i miejsce na wszystko.",
                    wymaga=("obrobka_drewna",), odblokowuje={"warsztat": 1}, bonus={"rzemieslnik": 1.10}),
    "hodowla": _p("Hodowla koni", 2, 50, "Silne konie niosą ludzi i prowiant.",
                  wymaga=("plodozmian", "handel"), odblokowuje={"stajnie": 1}),
    "szlaki": _p("Szlaki handlowe", 2, 70, "Wytyczone trakty i umowy z sąsiadami.",
                 wymaga=("handel",), odblokowuje={"karawanseraj": 1, "targ": 2, "sklep": 2}),
    "murowanie": _p("Murowanie", 2, 70, "Wapno i zaprawa: ściany stoją dziesiątki lat.",
                    wymaga=("kamieniarstwo",), odblokowuje={"dom": 2, "spichlerz": 2, "palisada": 3}),
    "plug_zelazny": _p("Pług żelazny", 2, 70, "Głęboka orka, większy plon, mocniejsze konie.",
                       wymaga=("kowalstwo",), odblokowuje={"farma": 3, "stajnie": 2}, bonus={"rolnik": 1.15}),
    "medycyna": _p("Medycyna", 2, 60, "Nastawianie kości i płukanie ran.",
                   wymaga=("zielarstwo",), odblokowuje={"lecznica": 2}),
    "archiwum": _p("Archiwum", 2, 80, "Katalogi, półki i klucz w ręku skryby. Największe skryptorium.",
                   wymaga=("zwoje",), odblokowuje={"skryptorium": 3}),
    "festyny": _p("Święta i festyny", 2, 90, "Dożynki i kiermasze — tawerna rośnie w rozmach.",
                  wymaga=("wspolne_ognisko",), odblokowuje={"tawerna": 2}),
    "wielki_trakt": _p("Wielki trakt", 2, 70, "Droga, którą przychodzą nowi: do dwunastu chat.",
                       wymaga=("osadnictwo",), chaty=12),
    "wiek_mistrzow": _p("Wiek mistrzów", 2, 140, "Przełom: cechy, gildie i prawdziwe mistrzostwo.",
                        wymaga=("kowalstwo", "murowanie", "archiwum"), era_po=3),
    # ---- Epoka III: mistrzostwo --------------------------------------------------
    "alchemia": _p("Alchemia", 3, 110, "Retorty, tygle i cierpliwość: eliksiry.",
                   wymaga=("medycyna",), odblokowuje={"laboratorium": 1}),
    "fortyfikacje": _p("Fortyfikacje", 3, 120, "Blanki, machikuły i wysoka strażnica.",
                       wymaga=("murowanie",), odblokowuje={"wieza": 2}, bonus={"straznik": 1.10}),
    "spichlerze": _p("Spichlerze mistrzowskie", 3, 100, "Wentylowane, suche i bezpieczne przed gryzoniami.",
                     wymaga=("murowanie",), odblokowuje={"spichlerz": 3}),
    "gildie": _p("Gildie", 3, 120, "Cechy dbają o jakość i szkolą uczniów.",
                 wymaga=("szlaki", "rzemioslo"),
                 odblokowuje={"warsztat": 2, "kuznia": 2, "targ": 3, "karawanseraj": 2}),
    "wielka_architektura": _p("Wielka architektura", 3, 160, "Sklepienia, filary i dachy na wieki.",
                              wymaga=("gildie", "fortyfikacje"),
                              odblokowuje={"dom": 3, "kuznia": 3, "tartak": 3, "huta": 3,
                                           "laboratorium": 2, "karawanseraj": 3}),
}


# ------------------------------------------------------------------ #
#  Stan                                                                #
# ------------------------------------------------------------------ #

def stan(gracz: "Gracz") -> dict:
    """Stan badań gracza; dopisuje brakujące pola (stary zapis, świeża postać)."""
    b = getattr(gracz, "badania", None)
    if not isinstance(b, dict):
        b = {}
        gracz.badania = b
    b.setdefault("zrobione", [])
    b.setdefault("aktualny", None)
    b.setdefault("postep", {})
    b.setdefault("zapas", 0.0)
    return b


def migruj_stary_zapis(gracz: "Gracz") -> None:
    """Zapis sprzed badań: wszystko jest już poznane, żeby nic się nie zablokowało."""
    b = stan(gracz)
    b["zrobione"] = list(PROJEKTY)
    b["aktualny"] = None
    b["postep"] = {}
    b["zapas"] = 0.0


def zrobione(gracz: "Gracz") -> set[str]:
    return set(stan(gracz)["zrobione"]) & set(PROJEKTY)


def era(gracz: "Gracz") -> int:
    """Bieżąca epoka: 1 plus największy przełom, który już się dokonał."""
    wynik = 1
    for pid in zrobione(gracz):
        po = PROJEKTY[pid]["era_po"]
        if po:
            wynik = max(wynik, po)
    return wynik


def maks_poziom(gracz: "Gracz", klucz: str) -> int:
    """Najwyższy poziom budynku, który osada potrafi postawić (0 = jeszcze nie umie)."""
    najwyzszy = START_POZIOMY.get(klucz, 0)
    for pid in zrobione(gracz):
        najwyzszy = max(najwyzszy, PROJEKTY[pid]["odblokowuje"].get(klucz, 0))
    return najwyzszy


def limit_chat(gracz: "Gracz") -> int:
    limit = START_CHATY
    for pid in zrobione(gracz):
        if PROJEKTY[pid]["chaty"]:
            limit = max(limit, PROJEKTY[pid]["chaty"])
    return limit


def projekt_dla(klucz: str, poziom: int) -> str | None:
    """Najtańszy projekt, który pozwala postawić budynek ``klucz`` na poziomie ``poziom``."""
    kandydaci = [pid for pid, p in PROJEKTY.items() if p["odblokowuje"].get(klucz, 0) >= poziom]
    return min(kandydaci, key=lambda pid: (PROJEKTY[pid]["era"], PROJEKTY[pid]["koszt"]), default=None)


def projekt_dla_chat(gracz: "Gracz") -> str | None:
    """Najbliższy niepoznany projekt podnoszący limit chat."""
    kandydaci = [pid for pid, p in PROJEKTY.items()
                 if p["chaty"] and p["chaty"] > limit_chat(gracz) and pid not in zrobione(gracz)]
    return min(kandydaci, key=lambda pid: PROJEKTY[pid]["chaty"], default=None)


def bonus_zajecia(gracz: "Gracz", zajecie: str) -> float:
    """Iloczyn premii z poznanych projektów dla danego zajęcia (np. płodozmian dla rolnika)."""
    m = 1.0
    poznane = zrobione(gracz)
    for pid, p in PROJEKTY.items():          # w kolejności tabeli: iloczyn nie zależy od kolejności w zbiorze
        if pid in poznane:
            m *= p["bonus"].get(zajecie, 1.0)
    return m


def powod_blokady(gracz: "Gracz", pid: str) -> str | None:
    """Czemu projektu nie można teraz zacząć albo None, gdy można."""
    p = PROJEKTY[pid]
    if pid in zrobione(gracz):
        return "już poznane"
    brakujace = [PROJEKTY[w]["nazwa"] for w in p["wymaga"] if w not in zrobione(gracz)]
    if brakujace:
        return "najpierw: " + ", ".join(brakujace)
    if era(gracz) < p["era"]:
        return f"wymaga epoki {p['era']} ({ERY[p['era']]})"
    poziom_skryptorium = poziom_budynku(gracz, "skryptorium")
    if poziom_skryptorium < p["era"]:
        return f"wymaga skryptorium na poziomie {p['era']}"
    return None


def dostepne(gracz: "Gracz") -> list[str]:
    return [pid for pid in PROJEKTY if powod_blokady(gracz, pid) is None]


def opis_efektow(pid: str) -> str:
    """Co daje projekt: nowe budowle, poziomy, chaty, epoka i premie."""
    p = PROJEKTY[pid]
    czesci = []
    for klucz, poz in p["odblokowuje"].items():
        nazwa = BUDYNKI[klucz]["nazwa"]
        czesci.append(f"{nazwa}" if poz == 1 else f"{nazwa} do poz. {poz}")
    if p["chaty"]:
        czesci.append(f"do {p['chaty']} chat")
    if p["era_po"]:
        czesci.append(f"EPOKA {p['era_po']}: {ERY[p['era_po']]}")
    for z, m in p["bonus"].items():
        czesci.append(f"{z} +{round((m - 1) * 100)}%")
    return ", ".join(czesci) if czesci else "—"


# ------------------------------------------------------------------ #
#  Praca badaczy                                                       #
# ------------------------------------------------------------------ #

def badacze(gracz: "Gracz") -> list[dict]:
    from game.osada import osadnicy
    return [o for o in osadnicy(gracz) if o["zajecie"] == "badacz" and not o.get("chory")]


def mnoznik_skryptorium(gracz: "Gracz") -> float:
    poziom = poziom_budynku(gracz, "skryptorium")
    return 0.0 if poziom <= 0 else 1.0 + PREMIA_SKRYPTORIUM * (poziom - 1)


def punkty_dzienne(gracz: "Gracz") -> float:
    """Ile punktów wiedzy osada wypracowuje dziennie (bez studiów bohatera)."""
    from game.osada import mnoznik_pracy
    return sum(BAZA_PUNKTOW * mnoznik_pracy(gracz, o) for o in badacze(gracz)) * mnoznik_skryptorium(gracz)


def punkty_studiow_bohatera(gracz: "Gracz") -> int:
    return max(1, PUNKTY_BOHATERA + atrybuty.modyfikator(gracz, "inteligencja"))


def postep(gracz: "Gracz", pid: str) -> float:
    return float(stan(gracz)["postep"].get(pid, 0.0))


def dni_do_konca(gracz: "Gracz") -> int | None:
    """Szacunek dni do końca bieżącego projektu albo None, gdy badania stoją."""
    b = stan(gracz)
    if not b["aktualny"]:
        return None
    tempo = punkty_dzienne(gracz)
    if tempo <= 0:
        return None
    zostalo = PROJEKTY[b["aktualny"]]["koszt"] - postep(gracz, b["aktualny"]) - b["zapas"]
    return max(1, math.ceil(max(0.0, zostalo) / tempo))   # dzielenie, nie //: ten sam wzór daje to samo w C#


def wybierz_projekt(gracz: "Gracz", pid: str) -> str:
    if pid not in PROJEKTY:
        return "  Nie ma takiego badania."
    powod = powod_blokady(gracz, pid)
    if powod:
        return f"  Nie można teraz badać: {PROJEKTY[pid]['nazwa']} — {powod}."
    b = stan(gracz)
    b["aktualny"] = pid
    wiadomosc = f"  📜  Badacze zajmują się: {PROJEKTY[pid]['nazwa']}."
    return wiadomosc + _wchlon_zapas(gracz)


def _wchlon_zapas(gracz: "Gracz") -> str:
    """Punkty wypracowane bez projektu trafiają do świeżo wybranego."""
    b = stan(gracz)
    if b["zapas"] <= 0 or not b["aktualny"]:
        return ""
    ile = b["zapas"]
    b["zapas"] = 0.0
    koniec = _dodaj(gracz, ile)
    return f"  (+{ile:.0f} pkt z zapasu)" + (" " + koniec if koniec else "")


def _dodaj(gracz: "Gracz", ile: float) -> str:
    """Dolicza punkty do bieżącego projektu; zwraca komunikat, gdy się skończył."""
    b = stan(gracz)
    pid = b["aktualny"]
    if not pid:
        b["zapas"] = min(LIMIT_ZAPASU, float(b["zapas"]) + ile)
        return ""
    b["postep"][pid] = float(b["postep"].get(pid, 0.0)) + ile
    if b["postep"][pid] < PROJEKTY[pid]["koszt"]:
        return ""
    nadwyzka = b["postep"][pid] - PROJEKTY[pid]["koszt"]
    b["postep"].pop(pid, None)
    b["zrobione"].append(pid)
    b["aktualny"] = None
    b["zapas"] = min(LIMIT_ZAPASU, float(b["zapas"]) + nadwyzka)
    gracz.statystyki["badania"] = len(zrobione(gracz))
    p = PROJEKTY[pid]
    tekst = f"  📜  BADANIA UKOŃCZONE: {p['nazwa']}!  Odblokowano: {opis_efektow(pid)}."
    if p["era_po"]:
        tekst += f"\n  🌅  Wchodzicie w epokę: {ERY[p['era_po']]}."
    return tekst


def dzien_badan(gracz: "Gracz") -> tuple[list[str], list[str]]:
    """Dzień pracy skryptorium. Zwraca (wieści do kroniki, wieści pilne)."""
    kronika: list[str] = []
    pilne: list[str] = []
    ile = punkty_dzienne(gracz)
    if ile <= 0:
        return kronika, pilne
    for o in badacze(gracz):
        kronika += kompetencje.cwicz(o, "badacz")
    wiesc = _dodaj(gracz, ile)
    if wiesc:
        pilne.append(wiesc)
    return kronika, pilne


def studiuj(gracz: "Gracz") -> str:
    """Bohater spędza dzień w skryptorium: punkty wiedzy do bieżącego projektu i mija dzień."""
    if poziom_budynku(gracz, "skryptorium") <= 0:
        return "  Najpierw potrzebne jest skryptorium."
    if not stan(gracz)["aktualny"]:
        return "  Wybierz najpierw projekt badań."
    ile = punkty_studiow_bohatera(gracz)
    tekst = f"  📖  Spędzasz dzień nad zwojami: +{ile} pkt wiedzy."
    koniec = _dodaj(gracz, float(ile))
    if koniec:
        tekst += chr(10) + koniec
    from game.osada import dodaj_czas
    dodaj_czas(gracz, 1)
    return tekst


def etykieta_menu(gracz: "Gracz") -> str:
    """Linia w menu obozu: co się bada albo co trzeba zrobić."""
    if poziom_budynku(gracz, "skryptorium") <= 0:
        return "Skryptorium (badania) — najpierw zbuduj je w [11]"
    b = stan(gracz)
    if b["aktualny"]:
        p = PROJEKTY[b["aktualny"]]
        return f"Skryptorium — {p['nazwa']} {postep(gracz, b['aktualny']):.0f}/{p['koszt']}"
    return "Skryptorium — wybierz badania" + ("  ⭐" if dostepne(gracz) else "")


# ------------------------------------------------------------------ #
#  Menu                                                                #
# ------------------------------------------------------------------ #

def _pasek(czesc: float, szer: int = 20) -> str:
    wypelnione = max(0, min(szer, int(round(czesc * szer))))
    return "█" * wypelnione + "░" * (szer - wypelnione)


def menu_badan(gracz: "Gracz") -> None:
    """Skryptorium: bieżący projekt, lista nauk, studia bohatera."""
    from game.osada import ilu_w_zawodzie, miejsca

    while True:
        wyczysc()
        wyswietl_linie("═")
        print(f"  SKRYPTORIUM  —  {kalendarz.opis_daty(gracz)}")
        wyswietl_linie("═")
        poziom = poziom_budynku(gracz, "skryptorium")
        b = stan(gracz)
        if poziom <= 0:
            print("\n  Bez skryptorium nikt nie spisuje wiedzy. Zbuduj je w rozbudowie obozu [11].")
            nacisnij_enter()
            return
        e = era(gracz)
        wolne = miejsca(gracz, "badacz") or 0
        print(f"\n  Epoka {e}: {ERY[e]}   ·   skryptorium poz. {poziom}   ·   "
              f"badaczy {ilu_w_zawodzie(gracz, 'badacz')}/{wolne}   ·   wiedza: {punkty_dzienne(gracz):.1f} pkt/dzień")
        if b["aktualny"]:
            p = PROJEKTY[b["aktualny"]]
            zrobione_pkt = postep(gracz, b["aktualny"])
            dni = dni_do_konca(gracz)
            eta = f"~{dni} dni" if dni else "stoi (brak badacza — [16] w osadzie)"
            print(f"\n  W TOKU: {p['nazwa']}  [{_pasek(zrobione_pkt / p['koszt'])}] "
                  f"{zrobione_pkt:.0f}/{p['koszt']}  {eta}")
            print(f"          {p['opis']}")
            print(f"          Da: {opis_efektow(b['aktualny'])}")
        else:
            print("\n  Badacze czekają na polecenie — wybierz projekt." +
                  (f"  (zapas {b['zapas']:.0f} pkt)" if b["zapas"] else ""))
        dost = dostepne(gracz)
        print("\n  DO POZNANIA:")
        if not dost:
            print("   — nic nowego; rozbuduj skryptorium albo dokończ przełom epoki.")
        for i, pid in enumerate(dost, 1):
            p = PROJEKTY[pid]
            znak = "▶" if pid == b["aktualny"] else " "
            print(f"  {znak}[{i:>2}] {p['nazwa']:<26} {p['koszt']:>4} pkt — {opis_efektow(pid)}")
        zablokowane = [pid for pid in PROJEKTY if powod_blokady(gracz, pid) not in (None, "już poznane")]
        if zablokowane:
            print("\n  ZAMKNIĘTE (pierwsze trzy):")
            for pid in zablokowane[:3]:
                print(f"   🔒 {PROJEKTY[pid]['nazwa']} — {powod_blokady(gracz, pid)}")
        print(f"\n  Poznane: {len(zrobione(gracz))}/{len(PROJEKTY)}")
        print(f"\n  [S] 📖 Studiuj sam (1 dzień, +{punkty_studiow_bohatera(gracz)} pkt)")
        print("  [0] ↩ Wróć\n")
        wybor = input("  Twój wybór: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "s":
            print(studiuj(gracz))
            nacisnij_enter()
            continue
        if wybor.isdigit() and 1 <= int(wybor) <= len(dost):
            print(wybierz_projekt(gracz, dost[int(wybor) - 1]))
            nacisnij_enter()
            continue
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
