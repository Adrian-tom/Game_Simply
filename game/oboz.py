"""Surowce z mapy i rozbudowa obozu — budynki mają poziomy i koszt utrzymania."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import kalendarz, talenty
from game.mapa import pole_gracza, odkryj_pole, opis_punktu, PUNKTY_MITYCZNE
from game.quests import sprawdz_questy
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

MAX_ZBIOROW_NA_POLU = 2

SUROWCE: dict[str, dict] = {
    "zywnosc": {"nazwa": "żywność", "ikona": "🍖"},
    "drewno": {"nazwa": "drewno", "ikona": "🌲"},
    "kamien": {"nazwa": "kamień", "ikona": "🪨"},
    "ziola": {"nazwa": "zioła", "ikona": "🌿"},
    "skora": {"nazwa": "skóra", "ikona": "🦌"},
    "ruda": {"nazwa": "ruda", "ikona": "⛏"},
    "deski": {"nazwa": "deski", "ikona": "🪵"},
    "zelazo": {"nazwa": "żelazo", "ikona": "🔩"},
}

# biom → lista (klucz, szansa, min, max). Żywność = jagody, grzyby, zwierzyna.
_ZBIORY_BIOM: dict[str, list[tuple[str, float, int, int]]] = {
    "równiny": [
        ("ziola", 0.75, 1, 3),
        ("zywnosc", 0.60, 1, 3),
        ("drewno", 0.50, 1, 2),
        ("kamien", 0.20, 1, 1),
    ],
    "las": [
        ("drewno", 0.90, 2, 4),
        ("zywnosc", 0.55, 1, 3),
        ("ziola", 0.40, 1, 2),
        ("skora", 0.30, 1, 2),
    ],
    "bagna": [
        ("ziola", 0.90, 2, 4),
        ("zywnosc", 0.35, 1, 2),
        ("drewno", 0.35, 1, 2),
        ("skora", 0.15, 1, 1),
    ],
    "ruiny": [
        ("kamien", 0.85, 2, 4),
        ("ruda", 0.30, 1, 2),
        ("drewno", 0.20, 1, 1),
    ],
    "wzgórza": [
        ("kamien", 0.70, 2, 3),
        ("ruda", 0.50, 1, 2),
        ("drewno", 0.25, 1, 2),
        ("zywnosc", 0.20, 1, 2),
    ],
    "kanion": [
        ("ruda", 0.75, 2, 3),
        ("kamien", 0.55, 1, 3),
    ],
}

# Rzadkie składniki alchemiczne ze zbieractwa: biom → (składnik, szansa)
_SKLADNIKI_BIOM = {
    "bagna": ("grzyb", 0.18),
    "kanion": ("kwiat_pustyni", 0.20),
    "wzgórza": ("pioro", 0.08),
    "ruiny": ("esencja", 0.06),
}

# koszt = cena poziomu 1; poziom n kosztuje koszt × n, a od 2. poziomu dochodzą
# deski i żelazo (patrz koszt_budowy). utrzymanie = złoto dziennie za poziom.
BUDYNKI: dict[str, dict] = {
    "sklep": {
        "nazwa": "Sklep", "ikona": "🏪", "max": 2, "utrzymanie": 1,
        "opis": "Kupiec w obozie — mikstury i podstawowy ekwipunek.",
        "koszt": {"drewno": 6, "kamien": 3, "zloto": 15},
    },
    "dom": {
        "nazwa": "Dom", "ikona": "🏠", "max": 3, "utrzymanie": 0,
        "opis": "Twój dom: lepszy odpoczynek (+25 HP za poziom) i miejsca dla drużyny.",
        "koszt": {"drewno": 10, "kamien": 5, "ziola": 4, "zloto": 25},
    },
    "farma": {
        "nazwa": "Pola uprawne", "ikona": "🌾", "max": 3, "utrzymanie": 0,
        "opis": "Rolnicy (2 na poziom) dają żywność — dużo jesienią, nic zimą.",
        "koszt": {"drewno": 8, "kamien": 2, "zloto": 15},
    },
    "spichlerz": {
        "nazwa": "Spichlerz", "ikona": "🏚", "max": 3, "utrzymanie": 0,
        "opis": "Żywność psuje się wolniej: 3% → 1% / 0,5% / 0% dziennie.",
        "koszt": {"drewno": 10, "kamien": 6, "zloto": 20},
    },
    "tartak": {
        "nazwa": "Tartak", "ikona": "🪚", "max": 3, "utrzymanie": 1,
        "opis": "Tracze (2 na poziom) robią deski z drewna: 2 drewna → 1 deska.",
        "koszt": {"drewno": 12, "kamien": 4, "ruda": 2, "zloto": 25},
    },
    "huta": {
        "nazwa": "Huta", "ikona": "🔥", "max": 3, "utrzymanie": 2,
        "opis": "Hutnicy (2 na poziom) wytapiają żelazo: 2 rudy + 1 drewno → 1 żelazo.",
        "koszt": {"kamien": 12, "drewno": 6, "ruda": 4, "zloto": 40},
    },
    "kuznia": {
        "nazwa": "Kuźnia", "ikona": "🔨", "max": 3, "utrzymanie": 1,
        "opis": "Kowal: broń, zbroje, narzędzia i ulepszenia sprzętu z żelaza.",
        "koszt": {"drewno": 6, "kamien": 8, "ruda": 5, "zloto": 40},
    },
    "warsztat": {
        "nazwa": "Warsztat", "ikona": "🔧", "max": 2, "utrzymanie": 1,
        "opis": "Mikstury, bandaże, odzienie, olej. Rzemieślnicy pracują na zamówienia.",
        "koszt": {"drewno": 6, "kamien": 6, "ruda": 3, "ziola": 4, "zloto": 30},
    },
    "laboratorium": {
        "nazwa": "Laboratorium", "ikona": "⚗", "max": 2, "utrzymanie": 2,
        "opis": "Alchemia: eliksiry, maści, bomby. Eksperymenty odkrywają przepisy.",
        "koszt": {"kamien": 8, "deski": 4, "ruda": 3, "ziola": 6, "zloto": 50},
    },
    "lecznica": {
        "nazwa": "Lecznica", "ikona": "⚕", "max": 2, "utrzymanie": 1,
        "opis": "Rany goją się szybciej, osadnicy rzadziej chorują. Miejsce dla uzdrowiciela.",
        "koszt": {"drewno": 8, "kamien": 4, "ziola": 6, "zloto": 30},
    },
    "targ": {
        "nazwa": "Targ", "ikona": "🛒", "max": 3, "utrzymanie": 1,
        "opis": "Stoiska: 2 zł dziennie za poziom + zarobek handlarzy.",
        "koszt": {"drewno": 8, "kamien": 4, "skora": 3, "zloto": 35},
    },
    "karawanseraj": {
        "nazwa": "Karawanseraj", "ikona": "🐪", "max": 3, "utrzymanie": 1,
        "opis": "Karawany do innych osad — jedna na poziom.",
        "koszt": {"drewno": 10, "deski": 6, "skora": 6, "zloto": 60},
    },
    "tawerna": {
        "nazwa": "Tawerna", "ikona": "🍺", "max": 2, "utrzymanie": 1,
        "opis": "Wieczory przy kuflu: morale osadników +6 za poziom.",
        "koszt": {"drewno": 10, "deski": 4, "zloto": 35},
    },
    "palisada": {
        "nazwa": "Palisada", "ikona": "🪵", "max": 3, "utrzymanie": 0,
        "opis": "Obrona osady +15 za poziom.",
        "koszt": {"drewno": 15, "zloto": 10},
    },
    "wieza": {
        "nazwa": "Wieża strażnicza", "ikona": "🗼", "max": 2, "utrzymanie": 1,
        "opis": "Obrona +10 za poziom. Najazd widać wcześniej i dokładniej.",
        "koszt": {"kamien": 10, "deski": 4, "zloto": 30},
    },
    "stajnie": {
        "nazwa": "Stajnie", "ikona": "🐴", "max": 2, "utrzymanie": 1,
        "opis": "Szybka podróż i +4 racje prowiantu na poziom.",
        "koszt": {"drewno": 8, "kamien": 4, "skora": 5, "zloto": 30},
    },
}


def _magazyn(gracz: Gracz) -> dict[str, int]:
    if getattr(gracz, "surowce", None) is None:
        gracz.surowce = {k: 0 for k in SUROWCE}
    for k in SUROWCE:
        gracz.surowce.setdefault(k, 0)
    return gracz.surowce


def dodaj_surowiec(gracz: Gracz, klucz: str, ile: int) -> None:
    if ile <= 0 or klucz not in SUROWCE:
        return
    mag = _magazyn(gracz)
    mag[klucz] = mag.get(klucz, 0) + ile
    gracz.statystyki["zebrane_surowce"] = gracz.statystyki.get("zebrane_surowce", 0) + ile


def poziom_budynku(gracz: Gracz, klucz: str) -> int:
    poziomy = getattr(gracz, "poziomy_budynkow", None) or {}
    if klucz in poziomy:
        return int(poziomy[klucz])
    return 1 if klucz in (getattr(gracz, "budynki", None) or set()) else 0


def ma_budynek(gracz: Gracz, klucz: str) -> bool:
    return poziom_budynku(gracz, klucz) > 0


def utrzymanie_dzienne(gracz: Gracz) -> int:
    return sum(info["utrzymanie"] * poziom_budynku(gracz, k) for k, info in BUDYNKI.items())


def linia_surowcow(gracz: Gracz) -> str:
    mag = _magazyn(gracz)
    czesci = [
        f"{SUROWCE[k]['ikona']}{mag.get(k, 0)}"
        for k in SUROWCE
    ]
    return "Surowce: " + "  ".join(czesci)


def opis_obozu(gracz: Gracz) -> str:
    zbudowane = []
    for k, info in BUDYNKI.items():
        poz = poziom_budynku(gracz, k)
        if poz:
            zbudowane.append(f"{info['ikona']} {info['nazwa']}" + (f" {poz}" if poz > 1 else ""))
    chaty = int(getattr(gracz, "chaty", 0) or 0)
    if chaty:
        zbudowane.append(f"🛖 {chaty}× chata")
    if not zbudowane:
        return "namiot i palenisko"
    return ", ".join(zbudowane)


def _format_kosztu(koszt: dict) -> str:
    czesci = []
    for k, ile in koszt.items():
        if k == "zloto":
            czesci.append(f"{ile} zł")
        else:
            info = SUROWCE[k]
            czesci.append(f"{info['ikona']}{ile} {info['nazwa']}")
    return ", ".join(czesci)


def _moze_zaplacic(gracz: Gracz, koszt: dict) -> bool:
    mag = _magazyn(gracz)
    if gracz.zloto < koszt.get("zloto", 0):
        return False
    for k, ile in koszt.items():
        if k == "zloto":
            continue
        if mag.get(k, 0) < ile:
            return False
    return True


def _pobierz_koszt(gracz: Gracz, koszt: dict) -> None:
    mag = _magazyn(gracz)
    gracz.zloto -= koszt.get("zloto", 0)
    for k, ile in koszt.items():
        if k == "zloto":
            continue
        mag[k] = mag.get(k, 0) - ile


def koszt_budowy(gracz: Gracz, klucz: str) -> dict | None:
    """Koszt następnego poziomu albo None, gdy budynek jest na maksimum."""
    info = BUDYNKI[klucz]
    nastepny = poziom_budynku(gracz, klucz) + 1
    if nastepny > info["max"]:
        return None
    koszt = {k: v * nastepny for k, v in info["koszt"].items()}
    if nastepny >= 2:
        koszt["deski"] = koszt.get("deski", 0) + 4 * nastepny
        koszt["zelazo"] = koszt.get("zelazo", 0) + 2 * (nastepny - 1)
    if talenty.ma(gracz, "architekt"):
        koszt = {k: max(1, int(v * 0.8)) for k, v in koszt.items()}
    return koszt


def pozostale_zbiory(pole: dict) -> int:
    uzyte = int(pole.get("zbierania", 0))
    return max(0, MAX_ZBIOROW_NA_POLU - uzyte)


def zbierz_na_polu(gracz: Gracz) -> str:
    """
    Zbiera surowce z aktualnego pola.
    Zwraca: 'ok', 'blokada', 'wyczerpane' albo 'walka'.
    """
    pole = pole_gracza(gracz)
    punkt = pole.get("punkt")
    if punkt == "obóz":
        print("\n  Przy palenisku nie ma czego zbierać. Wyjdź w teren.")
        return "blokada"
    if punkt == "boss" or punkt in PUNKTY_MITYCZNE or punkt == "miasto":
        print("\n  Nie pora na zbieractwo — to miejsce jest zbyt niebezpieczne.")
        return "blokada"
    if pozostale_zbiory(pole) <= 0:
        print("\n  To pole jest już ogołocone. Spróbuj indziej albo w nowym regionie.")
        return "wyczerpane"

    biom = pole.get("biom", "równiny")
    tabela = _ZBIORY_BIOM.get(biom, _ZBIORY_BIOM["równiny"])
    pora = kalendarz.pora(gracz)
    mnoznik = pora["zbiory"] * (1.5 if talenty.ma(gracz, "tropiciel") else 1.0)
    zyski: list[tuple[str, int]] = []
    for klucz, szansa, mn, mx in tabela:
        if random.random() <= szansa:
            ile = random.randint(mn, mx) * mnoznik
            ile = int(ile) + (1 if random.random() < ile - int(ile) else 0)
            if ile > 0:
                zyski.append((klucz, ile))
    if not zyski:
        klucz, _, mn, mx = tabela[0]
        zyski.append((klucz, max(1, int(random.randint(mn, mx) * mnoznik))))

    pole["zbierania"] = int(pole.get("zbierania", 0)) + 1
    print(f"\n  Przeszukujesz {biom}...")
    if pora["klucz"] == "zima":
        print("  ❄  Zima: pod śniegiem niewiele da się znaleźć.")
    for klucz, ile in zyski:
        if klucz == "zywnosc" and not getattr(gracz, "w_obozie", True):
            gracz.prowiant = int(getattr(gracz, "prowiant", 0) or 0) + ile
            print(f"  🍖  +{ile} racji do plecaka (prowiant: {gracz.prowiant})")
            continue
        dodaj_surowiec(gracz, klucz, ile)
        info = SUROWCE[klucz]
        print(f"  {info['ikona']}  +{ile} {info['nazwa']}")
    rzadki = _SKLADNIKI_BIOM.get(biom)
    if rzadki and random.random() < rzadki[1]:
        from game.rzemioslo import dodaj_skladnik
        print(dodaj_skladnik(gracz, rzadki[0]) + "  (rzadki składnik!)")
    print(f"  {linia_surowcow(gracz)}")
    zost = pozostale_zbiory(pole)
    if zost:
        print(f"  (Na tym polu zostało zbiorów: {zost})")
    else:
        print("  (Pole wyczerpane.)")

    szansa_walki = 0.12 * (0.5 if talenty.ma(gracz, "szosty_zmysl") else 1.0)
    if random.random() < szansa_walki:
        return "walka"
    return "ok"


def zbuduj(gracz: Gracz, klucz: str) -> str:
    """Wznosi budynek albo podnosi jego poziom. Zwraca komunikat."""
    info = BUDYNKI[klucz]
    koszt = koszt_budowy(gracz, klucz)
    if koszt is None:
        return f"  {info['nazwa']} ma już najwyższy poziom ({info['max']})."
    if not _moze_zaplacic(gracz, koszt):
        return (
            f"  Brakuje materiałów na {info['nazwa'].lower()}."
            f"  Potrzeba: {_format_kosztu(koszt)}."
        )
    _pobierz_koszt(gracz, koszt)
    if getattr(gracz, "budynki", None) is None:
        gracz.budynki = set()
    if getattr(gracz, "poziomy_budynkow", None) is None:
        gracz.poziomy_budynkow = {}
    nowy = poziom_budynku(gracz, klucz) + 1
    gracz.budynki.add(klucz)
    gracz.poziomy_budynkow[klucz] = nowy
    gracz.statystyki["zbudowane_budynki"] = len(gracz.budynki)
    if nowy == 1:
        return f"  {info['ikona']}  Wznosisz: {info['nazwa']}!  ({info['opis']})"
    return f"  {info['ikona']}  {info['nazwa']} rozbudowana do poziomu {nowy}!"


def menu_rozbudowy(gracz: Gracz) -> None:
    """Obozowe menu budowy i rozbudowy."""
    from game.osada import MAX_CHATY, KOSZT_CHATY, liczba_chat, zbuduj_chate

    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  ROZBUDOWA OBOZU")
        wyswietl_linie("═")
        print(f"\n  {linia_surowcow(gracz)}")
        print(f"  Złoto: {gracz.zloto} szt.   Utrzymanie budynków: {utrzymanie_dzienne(gracz)} zł/dzień\n")
        klucze = list(BUDYNKI)
        for i, klucz in enumerate(klucze, 1):
            info = BUDYNKI[klucz]
            poz = poziom_budynku(gracz, klucz)
            koszt = koszt_budowy(gracz, klucz)
            znacznik = f"poz. {poz}/{info['max']}" if poz else "—"
            print(f"  [{i:>2}] {info['ikona']} {info['nazwa']} ({znacznik}) — {info['opis']}")
            if koszt is None:
                print("        ✔ najwyższy poziom")
            else:
                stac = "✔ " if _moze_zaplacic(gracz, koszt) else ""
                akcja = "rozbuduj" if poz else "zbuduj"
                utrz = f", utrzymanie +{info['utrzymanie']} zł/dz." if info["utrzymanie"] else ""
                print(f"        {stac}{akcja}: {_format_kosztu(koszt)}{utrz}")
        nr_chaty = len(klucze) + 1
        print(
            f"\n  [{nr_chaty}] 🛖 Chata osadnika  — dom dla jednej osoby"
            f"  ({liczba_chat(gracz)}/{MAX_CHATY})"
        )
        print(f"        {_format_kosztu(KOSZT_CHATY)}")
        print("\n  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(klucze):
                print(zbuduj(gracz, klucze[idx]))
                for msg in sprawdz_questy(gracz):
                    print(msg)
                nacisnij_enter()
                continue
            if idx == len(klucze):
                print(zbuduj_chate(gracz))
                for msg in sprawdz_questy(gracz):
                    print(msg)
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def odkryte_punkty(gracz: Gracz) -> list[tuple[int, int, dict]]:
    """Odkryte pola z punktem orientacyjnym (bez bossa)."""
    pola = getattr(gracz, "mapa_pola", None) or []
    wynik = []
    for y, wiersz in enumerate(pola):
        for x, pole in enumerate(wiersz):
            if not pole.get("odkryte"):
                continue
            punkt = pole.get("punkt")
            if punkt and punkt != "boss":
                wynik.append((x, y, pole))
    return wynik


def menu_stajni(gracz: Gracz) -> None:
    """Szybka podróż do odkrytego punktu w bieżącym regionie."""
    if not ma_budynek(gracz, "stajnie"):
        print("\n  Nie masz jeszcze stajni.")
        nacisnij_enter()
        return

    cele = odkryte_punkty(gracz)
    wyczysc()
    wyswietl_linie("═")
    print("  STAJNIE  —  szybka podróż")
    wyswietl_linie("═")
    print("\n  Koń zawiezie cię do znanego miejsca w tym regionie.")
    print("  Nowe regiony i bossowie wymagają zwykłej drogi.\n")
    if not cele:
        print("  Nie znasz jeszcze żadnego punktu na mapie.")
        print("  Odkryj karczmę, kuźnię, świątynię, jaskinię albo obóz.")
        nacisnij_enter()
        return

    for i, (x, y, pole) in enumerate(cele, 1):
        nazwa = opis_punktu(pole.get("punkt"))
        tu = "  ← tu jesteś" if x == gracz.mapa_x and y == gracz.mapa_y else ""
        print(f"  [{i}] {nazwa}  ({x}, {y})  {pole['biom']}{tu}")
    print("  [0] Wróć\n")
    wybor = input("  Cel podróży: ").strip()
    if wybor == "0":
        return
    try:
        idx = int(wybor) - 1
        if 0 <= idx < len(cele):
            x, y, pole = cele[idx]
            if x == gracz.mapa_x and y == gracz.mapa_y:
                print("  Już tu jesteś.")
                nacisnij_enter()
                return
            gracz.mapa_x = x
            gracz.mapa_y = y
            odkryj_pole(gracz)
            from game.osada import dodaj_czas
            dodaj_czas(gracz, 1)
            print(
                f"\n  🐴  Galopujesz do: {opis_punktu(pole.get('punkt'))}"
                f" ({pole['biom']})."
            )
            nacisnij_enter()
            return
    except ValueError:
        pass
    print("  Nieprawidłowy wybór.")
    nacisnij_enter()
