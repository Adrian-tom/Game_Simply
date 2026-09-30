"""Handel i karawany: osady z własnymi cenami, dzienne wahania, napady na trakcie.

Każda osada handlowa ma specjalności (tanio kupuje to, czego ma w bród,
drogo to, czego jej brakuje). Ceny co dzień lekko dryfują i wracają do
swojej normy, a żywność drożeje zimą. Karawanseraj wysyła karawany:
towar jedzie kilka dni, może wpaść w zasadzkę (eskorta i talent Konwój
zmniejszają ryzyko) i wraca ze złotem — albo z zamówionym towarem.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import kalendarz, talenty
from game.oboz import SUROWCE, _magazyn, poziom_budynku
from game.osada import CENY_SUROWCOW
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

STALI_PARTNERZY: dict[str, dict] = {
    "brzezie": {
        "nazwa": "Wioska Brzezie", "ikona": "🏘", "odleglosc": 1,
        "opis": "Rolnicza wieś za lasem. Żywności ma w bród, żelaza jak na lekarstwo.",
        "normy": {"zywnosc": 0.7, "drewno": 0.8, "zelazo": 1.6, "deski": 1.3, "ruda": 1.2},
    },
    "kamienny_brod": {
        "nazwa": "Kamienny Bród", "ikona": "⛰", "odleglosc": 2,
        "opis": "Gródek górników. Ruda i kamień tanie, jedzenie i zioła na wagę srebra.",
        "normy": {"ruda": 0.6, "kamien": 0.6, "zywnosc": 1.6, "ziola": 1.4, "skora": 1.2},
    },
    "port_veldmar": {
        "nazwa": "Port Veldmar", "ikona": "⚓", "odleglosc": 3,
        "opis": "Portowe miasto kupców. Płaci najwięcej za skóry, deski i żelazo.",
        "normy": {"skora": 1.6, "deski": 1.5, "zelazo": 1.4, "ziola": 1.2, "kamien": 0.8},
    },
}

KOSZT_ESKORTY_DZIENNIE = 6
PORCJA_POJEMNOSCI = 30


def partnerzy(gracz: "Gracz") -> dict[str, dict]:
    """Stali partnerzy + miasta odkryte w regionach świata."""
    wynik = dict(STALI_PARTNERZY)
    for klucz, pola in (getattr(gracz, "regiony", None) or {}).items():
        for wiersz in pola:
            for pole in wiersz:
                if pole.get("punkt") == "miasto" and pole.get("odkryte"):
                    rx, ry = (int(v) for v in klucz.split(","))
                    r = random.Random(f"{gracz.seed}-{klucz}")
                    towary = r.sample(list(SUROWCE), 4)
                    wynik[f"miasto_{klucz}"] = {
                        "nazwa": f"Miasto w regionie [{rx}, {ry}]", "ikona": "🏙",
                        "odleglosc": max(abs(rx), abs(ry)) + 1,
                        "opis": "Duże miasto — dobre ceny, długa droga.",
                        "normy": {towary[0]: 1.5, towary[1]: 1.35, towary[2]: 0.75, towary[3]: 0.7},
                        "premia": 1.1,
                    }
    return wynik


def _mnozniki(gracz: "Gracz", osada: str) -> dict[str, float]:
    if getattr(gracz, "ceny", None) is None:
        gracz.ceny = {}
    if osada not in gracz.ceny:
        normy = partnerzy(gracz)[osada]["normy"]
        gracz.ceny[osada] = {k: normy.get(k, 1.0) * random.uniform(0.9, 1.1) for k in SUROWCE}
    return gracz.ceny[osada]


def cena(gracz: "Gracz", osada: str, surowiec: str, *, kupno: bool = False) -> int:
    info = partnerzy(gracz)[osada]
    wartosc = CENY_SUROWCOW[surowiec] * _mnozniki(gracz, osada)[surowiec] * info.get("premia", 1.0)
    if surowiec == "zywnosc":
        wartosc *= kalendarz.pora(gracz)["ceny_zywnosci"]
    if kupno:
        wartosc *= 1.25 * (0.9 if talenty.ma(gracz, "targowanie") else 1.0)
    elif talenty.ma(gracz, "targowanie"):
        wartosc *= 1.1
    from game.mysli import przyswojona
    if not kupno and przyswojona(gracz, "wszystko_na_sprzedaz"):
        wartosc *= 1.05
    return max(1, int(round(wartosc)))


def dzien_handlu(gracz: "Gracz") -> list[str]:
    """Dryf cen i ruch karawan. Zwraca wieści do kroniki."""
    msgs: list[str] = []
    for osada, mn in (getattr(gracz, "ceny", None) or {}).items():
        normy = partnerzy(gracz).get(osada, {}).get("normy", {})
        for k in mn:
            norma = normy.get(k, 1.0)
            mn[k] = max(0.4, min(2.5, mn[k] + (norma - mn[k]) * 0.1 + random.uniform(-0.04, 0.04)))
    for karawana in list(getattr(gracz, "karawany", None) or []):
        karawana["zostalo"] -= 1
        if not karawana.get("po_zasadzce") and karawana["zostalo"] <= karawana["dni"] // 2:
            karawana["po_zasadzce"] = True
            if random.random() < karawana["ryzyko"]:
                strata = random.uniform(0.3, 1.0)
                karawana["strata"] = strata
                msgs.append(f"  🗡  Karawanę do {karawana['nazwa']} napadnięto na trakcie! Stracono ok. {int(strata * 100)}% towaru.")
                if karawana.get("eskorta") and random.random() < 0.3:
                    msgs.append("  ⚰  Jeden z eskorty nie wrócił.")
        if karawana["zostalo"] <= 0:
            gracz.karawany.remove(karawana)
            msgs += _powrot_karawany(gracz, karawana)
    return msgs


def _powrot_karawany(gracz: "Gracz", k: dict) -> list[str]:
    zachowane = 1.0 - k.get("strata", 0.0)
    zysk = int(k["wartosc"] * zachowane * (1.25 if talenty.ma(gracz, "siec_kupcow") else 1.0))
    msgs = [f"  🐪  Karawana wraca z {k['nazwa']}: +{zysk} złota."]
    zakup = k.get("zakup")
    if zakup and zysk > 0:
        surowiec, cena_szt = zakup
        ile = zysk // cena_szt
        if ile:
            zysk -= ile * cena_szt
            _magazyn(gracz)[surowiec] = _magazyn(gracz).get(surowiec, 0) + ile
            msgs.append(f"  📦  Przywieziono zamówienie: +{ile} {SUROWCE[surowiec]['nazwa']} (reszta: {zysk} zł).")
    gracz.zloto += zysk
    gracz.statystyki["karawany"] = gracz.statystyki.get("karawany", 0) + 1
    return msgs


def pojemnosc(gracz: "Gracz") -> int:
    p = PORCJA_POJEMNOSCI * max(1, poziom_budynku(gracz, "karawanseraj"))
    return p * (2 if talenty.ma(gracz, "ksiaze_kupiecki") else 1)


def ryzyko(gracz: "Gracz", odleglosc: int, eskorta: int) -> float:
    r = 0.06 + 0.07 * odleglosc - 0.05 * eskorta
    if talenty.ma(gracz, "konwoj"):
        r *= 0.6
    return max(0.02, min(0.6, r))


def _wybierz_liczbe(tekst: str, maks: int) -> int:
    odp = input(tekst).strip()
    if not odp.isdigit():
        return 0
    return max(0, min(maks, int(odp)))


def _wyslij(gracz: "Gracz", osada: str) -> None:
    info = partnerzy(gracz)[osada]
    mag = _magazyn(gracz)
    towary: dict[str, int] = {}
    miejsce = pojemnosc(gracz)
    while True:
        wyczysc()
        wyswietl_linie()
        print(f"  🐪  Karawana do: {info['ikona']} {info['nazwa']}  (droga: {info['odleglosc'] * 2 + 1} dni w obie strony)")
        wyswietl_linie()
        zajete = sum(towary.values())
        wartosc = sum(n * cena(gracz, osada, k) for k, n in towary.items())
        print(f"  Ładunek: {zajete}/{miejsce}   wartość na miejscu: {wartosc} zł\n")
        klucze = list(SUROWCE)
        for i, k in enumerate(klucze, 1):
            print(f"  [{i}] {SUROWCE[k]['ikona']} {SUROWCE[k]['nazwa']:9}  masz {mag.get(k, 0) - towary.get(k, 0):>4}"
                  f"   ładujesz {towary.get(k, 0):>3}   cena tam: {cena(gracz, osada, k)} zł")
        print("\n  [W] Wyślij karawanę   [0] Anuluj\n")
        wybor = input("  Co załadować: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "w":
            break
        if wybor.isdigit() and 1 <= int(wybor) <= len(klucze):
            k = klucze[int(wybor) - 1]
            maks = min(mag.get(k, 0) - towary.get(k, 0), miejsce - zajete)
            ile = _wybierz_liczbe(f"  Ile {SUROWCE[k]['nazwa']} (maks. {maks})? ", maks)
            if ile:
                towary[k] = towary.get(k, 0) + ile
    if not towary:
        print("  Pusta karawana nigdzie nie jedzie.")
        nacisnij_enter()
        return
    dni = info["odleglosc"] * 2 + 1
    eskorta = _wybierz_liczbe(f"  Eskorta (0–3 najemników, {KOSZT_ESKORTY_DZIENNIE * dni} zł/os. za drogę): ", 3)
    koszt = eskorta * KOSZT_ESKORTY_DZIENNIE * dni
    if koszt > gracz.zloto:
        eskorta, koszt = 0, 0
        print("  Nie stać cię na eskortę — karawana jedzie bez niej.")
    zakup = None
    print("\n  Zamówienie powrotne: za utarg karawana może kupić jeden towar.")
    klucze = list(SUROWCE)
    for i, k in enumerate(klucze, 1):
        print(f"  [{i}] {SUROWCE[k]['nazwa']} ({cena(gracz, osada, k, kupno=True)} zł/szt.)", end="   ")
        if i % 4 == 0:
            print()
    wybor = input("\n  Numer towaru albo Enter = tylko złoto: ").strip()
    if wybor.isdigit() and 1 <= int(wybor) <= len(klucze):
        k = klucze[int(wybor) - 1]
        zakup = (k, cena(gracz, osada, k, kupno=True))
    for k, n in towary.items():
        mag[k] -= n
    gracz.zloto -= koszt
    r = ryzyko(gracz, info["odleglosc"], eskorta)
    gracz.karawany.append({
        "cel": osada, "nazwa": info["nazwa"], "dni": dni, "zostalo": dni, "eskorta": eskorta,
        "wartosc": sum(n * cena(gracz, osada, k) for k, n in towary.items()),
        "ryzyko": r, "zakup": zakup,
    })
    print(f"\n  🐪  Karawana rusza. Powrót za {dni} dni. Ryzyko napadu: {int(r * 100)}%.")
    nacisnij_enter()


def menu_handlu(gracz: "Gracz") -> None:
    if getattr(gracz, "karawany", None) is None:
        gracz.karawany = []
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  💰  HANDEL I KARAWANY")
        wyswietl_linie("═")
        poziom = poziom_budynku(gracz, "karawanseraj")
        print(f"\n  Karawanseraj: {'poz. ' + str(poziom) if poziom else 'brak (zbuduj w [11])'}"
              f"   Karawany w drodze: {len(gracz.karawany)}/{poziom}   Ładowność: {pojemnosc(gracz)}")
        for k in gracz.karawany:
            print(f"    🐪 → {k['nazwa']}: wraca za {k['zostalo']} dni (wartość {k['wartosc']} zł)")
        print("\n  Ceny skupu (co najlepiej sprzedać gdzie):")
        klucze = list(partnerzy(gracz))
        for i, osada in enumerate(klucze, 1):
            info = partnerzy(gracz)[osada]
            ceny = sorted(SUROWCE, key=lambda s: -cena(gracz, osada, s) / CENY_SUROWCOW[s])
            najlepsze = ", ".join(f"{SUROWCE[s]['ikona']}{cena(gracz, osada, s)}" for s in ceny[:3])
            print(f"  [{i}] {info['ikona']} {info['nazwa']} ({info['odleglosc']} dni drogi) — drogo kupują: {najlepsze}")
            print(f"      {info['opis']}")
        print("\n  [0] Wróć\n")
        wybor = input("  Wyślij karawanę do: ").strip()
        if wybor == "0":
            return
        if not wybor.isdigit() or not 1 <= int(wybor) <= len(klucze):
            continue
        if not poziom:
            print("  Najpierw zbuduj karawanseraj.")
            nacisnij_enter()
            continue
        if len(gracz.karawany) >= poziom:
            print("  Wszystkie wozy są w drodze. Rozbuduj karawanseraj albo poczekaj.")
            nacisnij_enter()
            continue
        _wyslij(gracz, klucze[int(wybor) - 1])
