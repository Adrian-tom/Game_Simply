"""Kompetencje osadników: umiejętności 0–20, pasje i awans przez pracę.

Wcześniej osadnik zbierał „doświadczenie w zawodzie” w trzech progach
(★ ★★ ★★★), z czego każdy dawał +10% pracy i na tym kończyła się różnica
między ludźmi. Teraz każdy ma dziesięć umiejętności na poziomach 0–20,
a niektóre z nich kocha: **pasja** przyspiesza naukę i poprawia humor, gdy
osadnik robi to, co lubi. Dwóch rolników to już nie to samo — jeden ma uprawę
na 12 i ogień w oczach, drugi 3 i myśli o czymś innym.

Liczby wzorowane na RimWorldzie, przeskalowane do dziennego rytmu Simply:

* XP do następnego poziomu rośnie liniowo od 40 (poziom 0) do 400 (9),
  a potem do 1200 (19) — RimWorld ma 1000 → 10000 → 30000 na tych samych
  progach, tu wszystko podzielone przez 25;
* dzień pracy daje 60 XP razy mnożnik pasji: brak ×0,35, mała ×1, duża ×1,5.
  Bez pasji poziom 10 to ~100 dni pracy, z dużą pasją ~25.

Moduł nie zna osady (tylko dane osadnika), więc nic tu się nie importuje cyklicznie.
"""
from __future__ import annotations

import random

MAX_POZIOM = 20
XP_NA_DZIEN = 60.0
MNOZNIK_PASJI = {0: 0.35, 1: 1.0, 2: 1.5}
PREMIA_MORALE_PASJI = {0: 0.0, 1: 4.0, 2: 8.0}   # cel morale, gdy osadnik robi to, co lubi

UMIEJETNOSCI: dict[str, dict] = {
    "budowa": {"nazwa": "Budowa", "ikona": "🏗"},
    "gornictwo": {"nazwa": "Górnictwo", "ikona": "⛏"},
    "uprawa": {"nazwa": "Uprawa", "ikona": "🌾"},
    "zbieractwo": {"nazwa": "Zbieractwo", "ikona": "🌿"},
    "lowiectwo": {"nazwa": "Łowiectwo", "ikona": "🏹"},
    "rzemioslo": {"nazwa": "Rzemiosło", "ikona": "🔧"},
    "handel": {"nazwa": "Handel", "ikona": "💰"},
    "medycyna": {"nazwa": "Medycyna", "ikona": "⚕"},
    "walka": {"nazwa": "Walka", "ikona": "🛡"},
    "wiedza": {"nazwa": "Wiedza", "ikona": "📜"},
}

# Która umiejętność rośnie od danego zajęcia. Bezczynny nie ćwiczy niczego.
UMIEJETNOSC_ZAJECIA: dict[str, str] = {
    "drwal": "zbieractwo", "zielarz": "zbieractwo", "kamieniarz": "gornictwo", "gornik": "gornictwo",
    "mysliwy": "lowiectwo", "rolnik": "uprawa", "tracz": "rzemioslo", "hutnik": "rzemioslo",
    "rzemieslnik": "rzemioslo", "handlarz": "handel", "straznik": "walka", "uzdrowiciel": "medycyna",
    "budowniczy": "budowa", "badacz": "wiedza",
}

_TYTULY = ((16, "arcymistrz"), (12, "mistrz"), (8, "czeladnik"), (4, "uczeń"), (0, "nowicjusz"))


def xp_do_nastepnego(poziom: int) -> float:
    """XP potrzebne, by z ``poziom`` wejść na kolejny (krzywa kawałkami liniowa)."""
    p = max(0, min(MAX_POZIOM - 1, poziom))
    if p <= 9:
        return 40.0 + (400.0 - 40.0) * p / 9
    return 400.0 + (1200.0 - 400.0) * (p - 9) / 10


def tytul(poziom: int) -> str:
    for prog, nazwa in _TYTULY:
        if poziom >= prog:
            return nazwa
    return _TYTULY[-1][1]


def opis_pasji(pasja: int) -> str:
    return "🔥" * max(0, min(2, pasja))


# ------------------------------------------------------------------ #
#  Tworzenie                                                           #
# ------------------------------------------------------------------ #

def generuj(imie: str) -> dict[str, dict]:
    """Umiejętności startowe osadnika o danym imieniu.

    Losowanie jest zasiane imieniem, więc ta sama osoba ma te same predyspozycje
    w każdej grze i w każdym zapisie — Nessa jest „tą od ziół” zawsze, a stary
    zapis bez umiejętności nie dostaje przy wczytaniu innego człowieka.
    """
    rng = random.Random(f"kompetencje|{imie}")
    klucze = list(UMIEJETNOSCI)
    wynik = {k: {"p": min(6, int(rng.triangular(0, 6, 1.5))), "xp": 0.0, "pasja": 0} for k in klucze}
    for k in rng.sample(klucze, 2):                      # dwa talenty: ktoś jest w czymś dobry
        wynik[k]["p"] = min(12, wynik[k]["p"] + rng.randint(2, 5))
    pierwszy, drugi = rng.sample(klucze, 2)
    wynik[pierwszy]["pasja"] = 2 if rng.random() < 0.3 else 1
    if rng.random() < 0.55:
        wynik[drugi]["pasja"] = 1
    return wynik


def zapewnij(o: dict) -> dict[str, dict]:
    """Stary zapis albo świeży osadnik: dopisz umiejętności, przelicz gwiazdki z ``dosw``."""
    if not isinstance(o.get("kompetencje"), dict) or not o["kompetencje"]:
        o["kompetencje"] = generuj(o.get("imie", ""))
        # Dawne ★★★ w zawodzie to ~dziewiąty poziom tej umiejętności.
        for zajecie, xp in (o.get("dosw") or {}).items():
            umiejetnosc = UMIEJETNOSC_ZAJECIA.get(zajecie)
            gwiazdki = sum(1 for prog in (10, 30, 60) if xp >= prog)
            if umiejetnosc and gwiazdki:
                wpis = o["kompetencje"][umiejetnosc]
                wpis["p"] = max(wpis["p"], 3 * gwiazdki)
    for k in UMIEJETNOSCI:                                # nowa umiejętność w starym zapisie
        o["kompetencje"].setdefault(k, {"p": 0, "xp": 0.0, "pasja": 0})
    o.pop("dosw", None)
    return o["kompetencje"]


# ------------------------------------------------------------------ #
#  Odczyt                                                              #
# ------------------------------------------------------------------ #

def poziom(o: dict, umiejetnosc: str) -> int:
    return int(zapewnij(o)[umiejetnosc]["p"])


def pasja(o: dict, umiejetnosc: str) -> int:
    return int(zapewnij(o)[umiejetnosc]["pasja"])


def umiejetnosc_zajecia(zajecie: str | None) -> str | None:
    return UMIEJETNOSC_ZAJECIA.get(zajecie or "")


def czynnik(o: dict, zajecie: str | None = None) -> float:
    """Mnożnik wydajności od umiejętności w zajęciu: 0,8 na poziomie 0, 1,0 na 4, 1,8 na 20."""
    u = umiejetnosc_zajecia(zajecie or o.get("zajecie"))
    if u is None:
        return 1.0
    return 0.8 + 0.05 * poziom(o, u)


def premia_morale(o: dict) -> float:
    """Bonus do celu morale, gdy osadnik zajmuje się tym, co lubi (i nie choruje)."""
    if o.get("chory", 0) > 0:
        return 0.0
    u = umiejetnosc_zajecia(o.get("zajecie"))
    return PREMIA_MORALE_PASJI[pasja(o, u)] if u else 0.0


def najlepsze(o: dict, ile: int = 3) -> list[tuple[str, int, int]]:
    """Najwyższe umiejętności: (klucz, poziom, pasja) — najpierw poziom, potem pasja."""
    k = zapewnij(o)
    lista = [(u, int(w["p"]), int(w["pasja"])) for u, w in k.items()]
    lista.sort(key=lambda t: (-t[1], -t[2], t[0]))
    return lista[:ile]


def opis_krotki(o: dict, ile: int = 3) -> str:
    czesci = []
    for u, p, pas in najlepsze(o, ile):
        if p == 0 and not pas:
            continue
        czesci.append(f"{UMIEJETNOSCI[u]['nazwa']} {p}{opis_pasji(pas)}")
    return ", ".join(czesci) if czesci else "bez wprawy"


def opis_pelny(o: dict, ile: int | None = None, zajecie: str | None = None) -> list[str]:
    """Umiejętności od najwyższej, jedna linia każda — do karty osadnika.

    ``ile`` skraca listę (mały ekran); umiejętność bieżącego ``zajecie`` zawsze
    się w niej znajdzie, nawet jeśli jest słaba.
    """
    wszystkie = najlepsze(o, len(UMIEJETNOSCI))
    wybrane = wszystkie if ile is None else wszystkie[:ile]
    wazna = umiejetnosc_zajecia(zajecie)
    if wazna and all(u != wazna for u, _, _ in wybrane):
        wybrane = wybrane + [t for t in wszystkie if t[0] == wazna]
    linie = []
    for u, p, pas in wybrane:
        w = zapewnij(o)[u]
        pasek = "█" * (p // 2) + "░" * (10 - p // 2)
        postep = "" if p >= MAX_POZIOM else f"  ({int(w['xp'])}/{int(xp_do_nastepnego(p))} xp)"
        znacznik = "▶" if u == wazna else " "
        linie.append(f" {znacznik}{UMIEJETNOSCI[u]['ikona']} {UMIEJETNOSCI[u]['nazwa']:<10} {pasek} {p:>2}"
                     f" {opis_pasji(pas):<2} {tytul(p)}{postep}")
    return linie


# ------------------------------------------------------------------ #
#  Nauka przez pracę                                                   #
# ------------------------------------------------------------------ #

def cwicz(o: dict, zajecie: str | None = None, dni: float = 1.0) -> list[str]:
    """Dzień pracy w zajęciu: XP do umiejętności, awans, komunikaty o nowych poziomach."""
    u = umiejetnosc_zajecia(zajecie or o.get("zajecie"))
    if u is None:
        return []
    w = zapewnij(o)[u]
    if int(w["p"]) >= MAX_POZIOM:
        return []
    w["xp"] = float(w["xp"]) + XP_NA_DZIEN * MNOZNIK_PASJI[int(w["pasja"])] * dni
    wiesci = []
    while int(w["p"]) < MAX_POZIOM and w["xp"] >= xp_do_nastepnego(int(w["p"])):
        w["xp"] -= xp_do_nastepnego(int(w["p"]))
        w["p"] = int(w["p"]) + 1
        wiesci.append(
            f"  📈  {o.get('imie', '?')}: {UMIEJETNOSCI[u]['nazwa'].lower()} {w['p']}"
            f"{opis_pasji(int(w['pasja']))}"
            + (f" — teraz {tytul(int(w['p']))}" if int(w["p"]) in (4, 8, 12, 16) else "")
        )
    if int(w["p"]) >= MAX_POZIOM:
        w["xp"] = 0.0
    return wiesci
