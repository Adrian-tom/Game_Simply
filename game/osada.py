"""Praca w obozie, chaty, osadnicy, targ i rzemiosło."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.oboz import (
    SUROWCE,
    dodaj_surowiec,
    ma_budynek,
    linia_surowcow,
)
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

MAX_CHATY = 6
KOSZT_CHATY = {"drewno": 8, "kamien": 4, "zloto": 20}
CENA_OSADNIKA = 28

_IMIONA_OSADNIKOW = (
    "Nessa", "Torin", "Elka", "Bram", "Sira", "Olek",
    "Mila", "Gareth", "Iva", "Piotr", "Ruta", "Kael",
)

PRACE: list[dict] = [
    {
        "nazwa": "Rąbanie drewna",
        "ikona": "🪓",
        "opis": "Kilka godzin przy siekierze.",
        "czas": 2,
        "nagrody": {"drewno": (2, 4), "zloto": (1, 3)},
    },
    {
        "nazwa": "Łamanie kamienia",
        "ikona": "⛏",
        "opis": "Żwir i głazy na fundamenty chat.",
        "czas": 2,
        "nagrody": {"kamien": (2, 3), "zloto": (1, 2)},
    },
    {
        "nazwa": "Zbieranie ziół",
        "ikona": "🌿",
        "opis": "Skrawek łąki za obozem.",
        "czas": 2,
        "nagrody": {"ziola": (2, 4), "zloto": (1, 3)},
    },
    {
        "nazwa": "Polowanie",
        "ikona": "🏹",
        "opis": "Sidła i cisza w lesie.",
        "czas": 3,
        "nagrody": {"skora": (1, 3), "zloto": (2, 5)},
    },
    {
        "nazwa": "Praca na targu",
        "ikona": "🛒",
        "opis": "Liczenie monet i przekonywanie chłopów.",
        "czas": 2,
        "wymaga": "targ",
        "nagrody": {"zloto": (8, 14)},
    },
    {
        "nazwa": "Dzień w warsztacie",
        "ikona": "🔧",
        "opis": "Mieszanie wywarów albo naprawa narzędzi.",
        "czas": 2,
        "wymaga": "warsztat",
        "nagrody": {"zloto": (4, 8), "ziola": (0, 1)},
        "rzemioslo": True,
    },
]

PRZEPISY: list[dict] = [
    {
        "nazwa": "Mikstura leczenia",
        "wynik": "mikstura",
        "ile": 1,
        "koszt": {"ziola": 3},
    },
    {
        "nazwa": "Antidotum",
        "wynik": "antidotum",
        "ile": 1,
        "koszt": {"ziola": 2, "skora": 1},
    },
    {
        "nazwa": "Mikstura many",
        "wynik": "mana",
        "ile": 1,
        "koszt": {"ziola": 2, "ruda": 1},
    },
]

CENY_SUROWCOW = {
    "drewno": 2,
    "kamien": 2,
    "ziola": 4,
    "skora": 5,
    "ruda": 8,
}


def dodaj_czas(gracz: Gracz, ile: int = 1) -> None:
    gracz.czas = getattr(gracz, "czas", 0) + max(0, ile)


def oznacz_wyjscie(gracz: Gracz) -> None:
    gracz.czas_wyjscia = getattr(gracz, "czas", 0)


def _magazyn(gracz: Gracz) -> dict[str, int]:
    from game.oboz import SUROWCE as _S
    if getattr(gracz, "surowce", None) is None:
        gracz.surowce = {k: 0 for k in _S}
    return gracz.surowce


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


def liczba_chat(gracz: Gracz) -> int:
    return int(getattr(gracz, "chaty", 0) or 0)


def osadnicy(gracz: Gracz) -> list[dict]:
    if getattr(gracz, "osadnicy", None) is None:
        gracz.osadnicy = []
    return gracz.osadnicy


def wolne_chaty(gracz: Gracz) -> int:
    return max(0, liczba_chat(gracz) - len(osadnicy(gracz)))


def zbuduj_chate(gracz: Gracz) -> str:
    ile = liczba_chat(gracz)
    if ile >= MAX_CHATY:
        return f"  Osada nie pomieści więcej chat (maks. {MAX_CHATY})."
    if not _moze_zaplacic(gracz, KOSZT_CHATY):
        return f"  Brakuje materiałów na chatę. Potrzeba: {_format_kosztu(KOSZT_CHATY)}."
    _pobierz_koszt(gracz, KOSZT_CHATY)
    gracz.chaty = ile + 1
    gracz.statystyki["zbudowane_chaty"] = gracz.chaty
    return (
        f"  🛖  Wznosisz chatę ({gracz.chaty}/{MAX_CHATY})."
        " Osadnik może tu zamieszkać i pracować, gdy ciebie nie ma."
    )


def zatrudnij_osadnika(gracz: Gracz, zajecie: str = "zbiory") -> str:
    if wolne_chaty(gracz) <= 0:
        return "  Brak wolnej chaty. Zbuduj domostwo w rozbudowie obozu."
    if gracz.zloto < CENA_OSADNIKA:
        return f"  Osadnik chce {CENA_OSADNIKA} złota za przeprowadzkę."
    zajete = {o.get("imie") for o in osadnicy(gracz)}
    pula = [n for n in _IMIONA_OSADNIKOW if n not in zajete] or list(_IMIONA_OSADNIKOW)
    imie = random.choice(pula)
    ikony = {"zbiory": "🌾", "handel": "💰", "rzemioslo": "🔧"}
    gracz.zloto -= CENA_OSADNIKA
    osadnicy(gracz).append({
        "imie": imie,
        "ikona": ikony.get(zajecie, "👤"),
        "zajecie": zajecie,
    })
    gracz.statystyki["zatrudnieni_osadnicy"] = len(osadnicy(gracz))
    return (
        f"  {ikony.get(zajecie, '👤')}  {imie} wprowadza się do chaty"
        f" i zajmuje się: {zajecie}.  (-{CENA_OSADNIKA} złota)"
    )


def _licz_zajecie(gracz: Gracz, zajecie: str) -> int:
    n = sum(1 for o in osadnicy(gracz) if o.get("zajecie") == zajecie)
    from game.rekruci import _lista, info_rekruta
    for r in _lista(gracz):
        if r.get("zajecie") != zajecie:
            continue
        if info_rekruta(r):
            n += 1
    return n


def _dodaj_wynik_rzemiosla(gracz: Gracz, wynik: str, ile: int) -> str:
    if ile <= 0:
        return ""
    if wynik == "mikstura":
        gracz.mikstury += ile
        return f"+{ile} miksturę"
    if wynik == "antidotum":
        gracz.antidota = getattr(gracz, "antidota", 0) + ile
        return f"+{ile} antidotum"
    if wynik == "mana":
        gracz.mikstury_many = getattr(gracz, "mikstury_many", 0) + ile
        return f"+{ile} miksturę many"
    return ""


def rozlicz_powrot_do_obozu(gracz: Gracz) -> list[str]:
    """Targ, handel i praca osadników za czas nieobecności."""
    teraz = getattr(gracz, "czas", 0)
    wyjscie = getattr(gracz, "czas_wyjscia", teraz)
    delta = max(0, teraz - wyjscie)
    gracz.czas_wyjscia = teraz
    if delta <= 0:
        return []

    msgs: list[str] = []
    handlowcy = _licz_zajecie(gracz, "handel")
    rzemieslnicy = _licz_zajecie(gracz, "rzemioslo")
    zbieracze_osady = sum(1 for o in osadnicy(gracz) if o.get("zajecie") == "zbiory")

    if ma_budynek(gracz, "targ"):
        mnoznik = 3 + 4 * handlowcy
        zloto = delta * mnoznik
        gracz.zloto += zloto
        gracz.statystyki["zloto_z_targu"] = gracz.statystyki.get("zloto_z_targu", 0) + zloto
        msgs.append(
            f"  🛒  Targ pracował {delta} dni twojej nieobecności:"
            f" +{zloto} złota"
            + (f" (handlarze: {handlowcy})" if handlowcy else "")
            + "."
        )
    elif handlowcy:
        zloto = delta * 2 * handlowcy
        gracz.zloto += zloto
        gracz.statystyki["zloto_z_targu"] = gracz.statystyki.get("zloto_z_targu", 0) + zloto
        msgs.append(
            f"  💰  Handlarze bez stoiska targowego uzbierali +{zloto} złota."
            " Zbuduj targ, by zarabiać więcej."
        )

    if zbieracze_osady:
        for _ in range(zbieracze_osady):
            klucz = random.choice(list(SUROWCE))
            ile = max(1, delta // 2 + random.randint(0, 1))
            dodaj_surowiec(gracz, klucz, ile)
            info = SUROWCE[klucz]
            msgs.append(
                f"  🌾  Osadnicy-zbieracze: +{ile} {info['nazwa']}."
            )

    if rzemieslnicy and ma_budynek(gracz, "warsztat"):
        partie = max(1, delta // 3) * rzemieslnicy
        partie = min(partie, 6)
        zrobione = 0
        mag = _magazyn(gracz)
        for _ in range(partie):
            if mag.get("ziola", 0) < 2:
                break
            mag["ziola"] -= 2
            gracz.mikstury += 1
            zrobione += 1
        if zrobione:
            msgs.append(
                f"  🔧  Warsztat podczas twojej nieobecności: +{zrobione} mikstur"
                f" (zużyto {zrobione * 2} ziół)."
            )
        else:
            msgs.append("  🔧  Rzemieślnicy czekali — brakło ziół na wywary.")
    elif rzemieslnicy:
        msgs.append("  🔧  Rzemieślnicy nie mają warsztatu — nic nie powstaje.")

    return msgs


def _wykonaj_prace(gracz: Gracz, praca: dict) -> None:
    dodaj_czas(gracz, int(praca.get("czas", 1)))
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
    if praca.get("rzemioslo") and random.random() < 0.45:
        mag = _magazyn(gracz)
        if mag.get("ziola", 0) >= 2:
            mag["ziola"] -= 2
            gracz.mikstury += 1
            print("  🧪  Udało ci się uwarzyć dodatkową miksturę (−2 zioła).")
    print(f"  {linia_surowcow(gracz)}")
    print(f"  Złoto: {gracz.zloto} szt.")


def menu_pracy(gracz: Gracz) -> None:
    """Praca fizyczna w obozie — złoto i surowce, zużywa czas."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  PRACA W OBOZIE")
        wyswietl_linie("═")
        print(f"\n  Dni podróży (czas świata): {getattr(gracz, 'czas', 0)}")
        print(f"  {linia_surowcow(gracz)}")
        print(f"  Złoto: {gracz.zloto} szt.\n")
        print("  Praca nie zastępuje wyprawy. Targ liczy dopiero nieobecność.\n")
        dostepne = []
        for praca in PRACE:
            wymaga = praca.get("wymaga")
            if wymaga and not ma_budynek(gracz, wymaga):
                continue
            dostepne.append(praca)
        for i, praca in enumerate(dostepne, 1):
            print(f"  [{i}] {praca.get('ikona', '⚒')} {praca['nazwa']}  ({praca['czas']} dni) — {praca['opis']}")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(dostepne):
                _wykonaj_prace(gracz, dostepne[idx])
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def _wypij_przepis(gracz: Gracz, przepis: dict) -> str:
    if not _moze_zaplacic(gracz, przepis["koszt"]):
        return f"  Brakuje składników ({_format_kosztu(przepis['koszt'])})."
    _pobierz_koszt(gracz, przepis["koszt"])
    txt = _dodaj_wynik_rzemiosla(gracz, przepis["wynik"], przepis["ile"])
    dodaj_czas(gracz, 1)
    return f"  🔧  Wytwarzasz: {przepis['nazwa']} ({txt})."


def menu_warsztatu(gracz: Gracz) -> None:
    if not ma_budynek(gracz, "warsztat"):
        print("\n  Nie masz warsztatu. Wzniesiesz go w rozbudowie obozu.")
        nacisnij_enter()
        return
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  WARSZTAT")
        wyswietl_linie("═")
        print(f"\n  {linia_surowcow(gracz)}\n")
        for i, p in enumerate(PRZEPISY, 1):
            print(f"  [{i}] {p['nazwa']}  — {_format_kosztu(p['koszt'])}")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(PRZEPISY):
                print(_wypij_przepis(gracz, PRZEPISY[idx]))
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def menu_sprzedazy_surowcow(gracz: Gracz) -> None:
    """Handel surowcami (miasto / targ)."""
    while True:
        mag = _magazyn(gracz)
        wyczysc()
        wyswietl_linie("═")
        print("  SPRZEDAŻ SUROWCÓW")
        wyswietl_linie("═")
        print(f"\n  Złoto: {gracz.zloto} szt.\n")
        klucze = list(SUROWCE)
        for i, k in enumerate(klucze, 1):
            info = SUROWCE[k]
            print(
                f"  [{i}] {info['ikona']} {info['nazwa']}: {mag.get(k, 0)}"
                f"  (cena {CENY_SUROWCOW[k]} zł / szt.)"
            )
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
                ile_txt = input(f"  Ile sztuk {SUROWCE[k]['nazwa']}? ").strip()
                ile = int(ile_txt)
                if ile <= 0 or mag.get(k, 0) < ile:
                    print("  Tyle nie masz.")
                    nacisnij_enter()
                    continue
                mag[k] -= ile
                zysk = ile * CENY_SUROWCOW[k]
                gracz.zloto += zysk
                print(f"  Sprzedano {ile} × {SUROWCE[k]['nazwa']} za {zysk} złota.")
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def _zmien_zajecie_osadnika(gracz: Gracz, wpis: dict) -> None:
    print(f"\n  {wpis.get('ikona', '👤')}  {wpis.get('imie')}")
    print("  [1] 🌾 Zbieractwo")
    print("  [2] 💰 Handel")
    print("  [3] 🔧 Rzemiosło")
    print("  [4] 🚪 Wypędź z osady")
    print("  [0] ↩ Wróć\n")
    wybor = input("  Twój wybór: ").strip()
    ikony = {"zbiory": "🌾", "handel": "💰", "rzemioslo": "🔧"}
    if wybor == "1":
        wpis["zajecie"] = "zbiory"
        wpis["ikona"] = ikony["zbiory"]
        print("  Zajęcie: zbieractwo.")
    elif wybor == "2":
        wpis["zajecie"] = "handel"
        wpis["ikona"] = ikony["handel"]
        print("  Zajęcie: handel.")
    elif wybor == "3":
        wpis["zajecie"] = "rzemioslo"
        wpis["ikona"] = ikony["rzemioslo"]
        print("  Zajęcie: rzemiosło.")
    elif wybor == "4":
        osadnicy(gracz).remove(wpis)
        gracz.statystyki["zatrudnieni_osadnicy"] = len(osadnicy(gracz))
        print(f"  {wpis.get('imie')} opuszcza chatę.")
    nacisnij_enter()


def menu_osady(gracz: Gracz) -> None:
    """Chaty, osadnicy i warsztat."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  OSADA")
        wyswietl_linie("═")
        print(f"\n  Chaty: {liczba_chat(gracz)}/{MAX_CHATY}   wolne: {wolne_chaty(gracz)}")
        print(f"  Targ: {'✔' if ma_budynek(gracz, 'targ') else '—'}"
              f"   Warsztat: {'✔' if ma_budynek(gracz, 'warsztat') else '—'}")
        print(f"  {linia_surowcow(gracz)}")
        print(f"  Złoto: {gracz.zloto} szt.\n")
        lista = osadnicy(gracz)
        if not lista:
            print("  Nikt jeszcze nie mieszka w chatach.")
            print("  Zbuduj chatę w [11], potem zatrudnij osadnika w mieście albo tutaj.\n")
        else:
            for i, o in enumerate(lista, 1):
                print(
                    f"  [{i}] {o.get('ikona', '👤')} {o.get('imie')}"
                    f"  — {o.get('zajecie', 'zbiory')}"
                )
            print()
        print("  [N]  🤝  Zatrudnij osadnika"
              f"  ({CENA_OSADNIKA} zł, wolna chata)")
        if ma_budynek(gracz, "warsztat"):
            print("  [W]  🔧  Warsztat (wytwarzanie)")
        if ma_budynek(gracz, "targ"):
            print("  [T]  🛒  Sprzedaj surowce na targu")
        print("  [0]  ↩  Wróć\n")
        wybor = input("  Twój wybór: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "n":
            print("\n  Czym ma się zająć?")
            print("  [1] 🌾 Zbieractwo  [2] 💰 Handel  [3] 🔧 Rzemiosło")
            z = input("  Zajęcie: ").strip()
            mapa = {"1": "zbiory", "2": "handel", "3": "rzemioslo"}
            print(zatrudnij_osadnika(gracz, mapa.get(z, "zbiory")))
            nacisnij_enter()
            continue
        if wybor == "w" and ma_budynek(gracz, "warsztat"):
            menu_warsztatu(gracz)
            continue
        if wybor == "t" and ma_budynek(gracz, "targ"):
            menu_sprzedazy_surowcow(gracz)
            continue
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(lista):
                _zmien_zajecie_osadnika(gracz, lista[idx])
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
