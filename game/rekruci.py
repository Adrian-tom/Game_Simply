"""Rekrutacja NPC: walka, zbiory, handel albo rzemiosło."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.oboz import dodaj_surowiec, SUROWCE, ma_budynek
from game.quests import sprawdz_questy
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

MAX_WALKA = 1
MAX_REKRUTOW = 12
CHA_MINIMUM = 16
CHA_EKSTREMALNA = 18

REKRUCI: dict[str, dict] = {
    "boris": {
        "imie": "Boris Topór",
        "ikona": "🪓",
        "opis": "Były najemnik. Ciosy ciężkie jak kowadło.",
        "cena": 40,
        "atak": 14,
        "zbior": "drewno",
        "zbior_ile": (2, 4),
    },
    "mira": {
        "imie": "Mira Zielarka",
        "ikona": "🌿",
        "opis": "Uzdrowicielka z bagien. W walce czasem leczy sojusznika.",
        "cena": 45,
        "atak": 7,
        "leczenie": 18,
        "zbior": "ziola",
        "zbior_ile": (2, 4),
    },
    "durin": {
        "imie": "Durin Kamienny",
        "ikona": "⛏",
        "opis": "Krasnoludzki górnik. Szuka rudy, a młotem też umie.",
        "cena": 50,
        "atak": 11,
        "zbior": "ruda",
        "zbior_ile": (1, 3),
    },
    "kora": {
        "imie": "Kora Łowczyni",
        "ikona": "🏹",
        "opis": "Cicha tropicielka. Skóry z lasu i strzały w plecy wroga.",
        "cena": 48,
        "atak": 12,
        "zbior": "skora",
        "zbior_ile": (2, 3),
    },
    "ashen": {
        "imie": "Ashen Wędrowiec",
        "ikona": "🌑",
        "opis": "Odłamek pieczęci w ludzkim płaszczu. Cięcia, których nie widać w południe.",
        "cena": 160,
        "atak": 16,
        "zbior": "kamien",
        "zbior_ile": (2, 4),
        "rekrut_st": 18,
        "historia": True,
    },
    "boldan": {
        "imie": "Karczmarz Boldan",
        "ikona": "🍺",
        "opis": "Zna każdy kufel i każdą plotkę. W walce broni się jak ktoś, kto zamykał izbę na noc.",
        "cena": 180,
        "atak": 10,
        "zbior": "drewno",
        "zbior_ile": (2, 3),
        "rekrut_st": 18,
        "historia": True,
    },
    "aldric": {
        "imie": "Kupiec Aldric",
        "ikona": "💰",
        "opis": "Liczy szybciej niż tnie. Handel to jego miecz, sakiewka — tarcza.",
        "cena": 220,
        "atak": 8,
        "zbior": "skora",
        "zbior_ile": (1, 3),
        "rekrut_st": 19,
        "historia": True,
    },
    "grimbold": {
        "imie": "Kowal Grimbold",
        "ikona": "⚒",
        "opis": "Trzydzieści lat przy kowadle. W walce młot, w osadzie — warsztat.",
        "cena": 200,
        "atak": 13,
        "zbior": "ruda",
        "zbior_ile": (1, 3),
        "rekrut_st": 18,
        "historia": True,
    },
    "eremiel": {
        "imie": "Kapłan Eremiel",
        "ikona": "🙏",
        "opis": "Modlitwa i wątpliwość w jednym człowieku. W walce leczy, w polu zbiera zioła.",
        "cena": 240,
        "atak": 7,
        "leczenie": 22,
        "zbior": "ziola",
        "zbior_ile": (2, 4),
        "rekrut_st": 19,
        "historia": True,
    },
    "alderon": {
        "imie": "Stary Rycerz Alderon",
        "ikona": "🗡",
        "opis": "Przysięga cięższa niż zbroja. W walce wciąż tnie jak za młodu — tylko wolniej wstaje.",
        "cena": 260,
        "atak": 18,
        "zbior": "kamien",
        "zbior_ile": (1, 2),
        "rekrut_st": 20,
        "historia": True,
    },
    "mirena": {
        "imie": "Burmistrz Mirena",
        "ikona": "🏛",
        "opis": "Zostawiła łańcuch urzędu. Umie liczyć głody i ludzi. Handel to jej wojna.",
        "cena": 280,
        "atak": 9,
        "zbior": "ziola",
        "zbior_ile": (1, 3),
        "rekrut_st": 20,
        "historia": True,
    },
    "vasco": {
        "imie": "Kupiec Vasco",
        "ikona": "🐪",
        "opis": "Karawany, cła i rzeczy, których straż nie widzi. Idealny do targu w dziczy.",
        "cena": 250,
        "atak": 11,
        "zbior": "skora",
        "zbior_ile": (2, 3),
        "rekrut_st": 19,
        "historia": True,
    },
}

_NAJEMNICY_KARCZMY = ("boris", "mira", "durin", "kora")
_ETYKIETY_ZAJEC = {
    "walka": "⚔ walka",
    "zbiory": "🌲 zbiory",
    "handel": "💰 handel",
    "rzemioslo": "🔧 rzemiosło",
}


def _lista(gracz: Gracz) -> list[dict]:
    if getattr(gracz, "rekruci", None) is None:
        gracz.rekruci = []
    return gracz.rekruci


def _limit(gracz: Gracz) -> int:
    extra = 2 if ma_budynek(gracz, "dom") else 0
    return min(MAX_REKRUTOW, 8 + extra)


def klucze_zrekrutowanych(gracz: Gracz) -> set[str]:
    return {r["klucz"] for r in _lista(gracz) if r.get("klucz") in REKRUCI}


def info_rekruta(wpis: dict) -> dict | None:
    return REKRUCI.get(wpis.get("klucz"))


def towarzysz_walki(gracz: Gracz) -> dict | None:
    for r in _lista(gracz):
        if r.get("zajecie") == "walka" and r.get("klucz") in REKRUCI:
            return r
    return None


def etykieta_towarzysza(gracz: Gracz) -> str | None:
    wpis = towarzysz_walki(gracz)
    info = info_rekruta(wpis) if wpis else None
    if not info:
        return None
    return f"{info['ikona']} {info['imie']}"


def _kandydaci(gracz: Gracz) -> list[str]:
    zajete = klucze_zrekrutowanych(gracz)
    return [k for k in _NAJEMNICY_KARCZMY if k not in zajete]


def zatrudnij(gracz: Gracz, klucz: str, zajecie: str, *, darmo: bool = False) -> str:
    info = REKRUCI.get(klucz)
    if not info:
        return "  Nikogo takiego nie ma."
    if klucz in klucze_zrekrutowanych(gracz):
        return f"  {info['imie']} już należy do obozu."
    if len(_lista(gracz)) >= _limit(gracz):
        extra = ""
        if not ma_budynek(gracz, "dom"):
            extra = " Dom w obozie daje dodatkowe miejsce."
        return f"  Brak miejsc w obozie (maks. {_limit(gracz)}).{extra}"
    cena = 0 if darmo else info["cena"]
    if gracz.zloto < cena:
        return f"  Za mało złota (potrzeba {cena} szt.)."
    if zajecie == "walka":
        aktualny = towarzysz_walki(gracz)
        if aktualny is not None:
            aktualny["zajecie"] = "zbiory"
    gracz.zloto -= cena
    _lista(gracz).append({"klucz": klucz, "zajecie": zajecie})
    gracz.statystyki["zrekrutowani"] = len(_lista(gracz))
    rola = _ETYKIETY_ZAJEC.get(zajecie, zajecie)
    platnosc = "za słowo" if darmo else f"-{cena} złota"
    return (
        f"  {info['ikona']}  {info['imie']} dołącza ({rola})!  ({platnosc})"
    )


def _ustaw_zajecie(gracz: Gracz, wpis: dict, zajecie: str) -> str:
    info = info_rekruta(wpis)
    if not info:
        return "  Nieznany rekrut."
    if zajecie == "walka":
        aktualny = towarzysz_walki(gracz)
        if aktualny is not None and aktualny is not wpis:
            aktualny["zajecie"] = "zbiory"
            stary = info_rekruta(aktualny)
            if stary:
                print(f"  {stary['imie']} wraca do zbiorów — w walce może iść tylko jedna osoba.")
    wpis["zajecie"] = zajecie
    rola = _ETYKIETY_ZAJEC.get(zajecie, zajecie)
    return f"  {info['ikona']}  {info['imie']} zajmuje się teraz: {rola}."


def oferta_rekrutacji(gracz: Gracz, klucz: str | None = None) -> None:
    """Propozycja najmu konkretnego albo losowego kandydata."""
    dostepni = _kandydaci(gracz)
    if klucz and klucz not in dostepni:
        if klucz in klucze_zrekrutowanych(gracz):
            print("\n  Ta osoba już z tobą wędrowała.")
        else:
            print("\n  Nikogo nie ma.")
        nacisnij_enter()
        return
    if not dostepni:
        print("\n  Wszyscy znani najemnicy już pracują w twoim obozie.")
        nacisnij_enter()
        return
    if klucz is None:
        klucz = random.choice(dostepni)
    info = REKRUCI[klucz]
    wyczysc()
    wyswietl_linie("═")
    print("  NAJEM")
    wyswietl_linie("═")
    print(f"\n  {info['ikona']}  {info['imie']}")
    print(f"  {info['opis']}")
    zbior = SUROWCE.get(info["zbior"], {})
    print(
        f"  Walka: atak {info['atak']}"
        + (f"  · leczenie {info['leczenie']}" if info.get("leczenie") else "")
    )
    print(
        f"  Zbiory: {zbior.get('ikona', '')} {zbior.get('nazwa', info['zbior'])}"
        f"  {info['zbior_ile'][0]}–{info['zbior_ile'][1]} / wyprawa"
    )
    print(f"  Cena: {info['cena']} złota   (masz {gracz.zloto})")
    print(f"  Miejsca w obozie: {len(_lista(gracz))}/{_limit(gracz)}\n")
    print("  [1]  ⚔  Zatrudnij do walki" + ("  (slot wolny)" if not towarzysz_walki(gracz) else "  — zdejmie obecnego towarzysza"))
    print("  [2]  🌲  Zatrudnij do zbiorów")
    print("  [3]  💰  Zatrudnij do handlu")
    print("  [4]  🔧  Zatrudnij do rzemiosła")
    print("  [0]  🚶  Odmów\n")
    wybor = input("  Twój wybór: ").strip()
    mapa = {"1": "walka", "2": "zbiory", "3": "handel", "4": "rzemioslo"}
    if wybor in mapa:
        print(zatrudnij(gracz, klucz, mapa[wybor]))
        for msg in sprawdz_questy(gracz):
            print(msg)
    else:
        print(f"  {info['imie']} kiwa głową. „Innym razem.”")
    nacisnij_enter()


def _wybierz_zajecie_najmu() -> str | None:
    print("  Czym ma się zająć?")
    print("  [1] ⚔ Walka   [2] 🌲 Zbiory   [3] 💰 Handel   [4] 🔧 Rzemiosło   [0] Anuluj")
    wybor = input("  Twój wybór: ").strip()
    return {"1": "walka", "2": "zbiory", "3": "handel", "4": "rzemioslo"}.get(wybor)


def proponuj_rekrutacje_npc(gracz: Gracz, klucz: str) -> None:
    """Najem postaci fabularnej: fortuną albo ekstremalną charyzmą."""
    info = REKRUCI.get(klucz)
    if not info:
        print("  Nikogo takiego nie ma.")
        nacisnij_enter()
        return
    if klucz in klucze_zrekrutowanych(gracz):
        print(f"\n  {info['imie']} już należy do twojej osady.")
        nacisnij_enter()
        return

    from game.atrybuty import wartosc, przeprowadz_test, trudnosc

    cha = wartosc(gracz, "charyzma")
    st_baza = int(info.get("rekrut_st", 18))
    st = trudnosc(gracz, st_baza)

    wyczysc()
    wyswietl_linie("═")
    print("  REKRUTACJA")
    wyswietl_linie("═")
    print(f"\n  {info['ikona']}  {info['imie']}")
    print(f"  {info['opis']}")
    print(f"  Cena wykupu życia: {info['cena']} złota   (masz {gracz.zloto})")
    print(f"  Twoja charyzma: {cha}   (próg próby {CHA_MINIMUM}, ekstremum {CHA_EKSTREMALNA})")
    print(f"  Miejsca w obozie: {len(_lista(gracz))}/{_limit(gracz)}\n")
    print("  Ta osoba nie jest najemnikiem z ogłoszenia. Albo płacisz fortunę,")
    print("  albo łamiesz jej opór słowem — i to słowem, które boli.\n")
    print(f"  [1]  💰  Zapłać {info['cena']} złota")
    print(f"  [2]  ✨  Przekonaj (charyzma / perswazja, ST {st})")
    print("  [0]  🚶  Odpuść\n")
    wybor = input("  Twój wybór: ").strip()

    if wybor == "1":
        zajecie = _wybierz_zajecie_najmu()
        if not zajecie:
            print("  Nic nie ustaliliście.")
            nacisnij_enter()
            return
        print(zatrudnij(gracz, klucz, zajecie))
        for msg in sprawdz_questy(gracz):
            print(msg)
        nacisnij_enter()
        return

    if wybor != "2":
        print(f"  {info['imie']} odwraca wzrok. „Nie każdy musi iść.”")
        nacisnij_enter()
        return

    if cha < CHA_MINIMUM:
        print(
            f"  Twoja charyzma ({cha}) nie uniesie tego człowieka."
            f" Potrzeba co najmniej {CHA_MINIMUM} — albo złota."
        )
        nacisnij_enter()
        return

    darmo = False
    if cha >= CHA_EKSTREMALNA:
        print(
            f"  Charyzma {cha} zgina opór jak suche drewno."
            " Nie musisz rzucać kości — słowo wystarczy."
        )
        darmo = True
    else:
        wynik = przeprowadz_test(gracz, "perswazja", st)
        if not wynik.sukces:
            print(f"  {info['imie']}: „Ładnie mówisz. Za mało, bym spalił za tobą mosty.”")
            nacisnij_enter()
            return
        darmo = True

    zajecie = _wybierz_zajecie_najmu()
    if not zajecie:
        print("  Słowo było, decyzji nie było.")
        nacisnij_enter()
        return
    print(zatrudnij(gracz, klucz, zajecie, darmo=darmo))
    for msg in sprawdz_questy(gracz):
        print(msg)
    nacisnij_enter()


def menu_druzyny(gracz: Gracz) -> None:
    """Obozowe zarządzanie rekrutami."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  DRUŻYNA")
        wyswietl_linie("═")
        print(f"\n  Miejsca: {len(_lista(gracz))}/{_limit(gracz)}"
              f"   (w walce maks. {MAX_WALKA} osoba)")
        if ma_budynek(gracz, "dom"):
            print("  Dom powiększa liczbę miejsc na nazwanych towarzyszy.")
        else:
            print("  Zbuduj dom, aby przyjąć więcej osób.")
        print("  Osadnicy z chat nie zajmują tych miejsc — zarządzasz nimi w [16].")
        print()
        rekruci = [r for r in _lista(gracz) if r.get("klucz") in REKRUCI]
        if not rekruci:
            print("  Nikogo jeszcze nie zatrudniłeś.")
            print("  Najemnicy: karczma [6]. Postacie: rozmowa → dołączenie do osady.\n")
        else:
            for i, r in enumerate(rekruci, 1):
                info = REKRUCI[r["klucz"]]
                rola = _ETYKIETY_ZAJEC.get(r.get("zajecie"), r.get("zajecie"))
                print(f"  [{i}] {info['ikona']} {info['imie']}  — {rola}")
                print(f"      {info['opis']}")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(rekruci):
                _menu_rekruta(gracz, rekruci[idx])
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def _menu_rekruta(gracz: Gracz, wpis: dict) -> None:
    info = info_rekruta(wpis)
    if not info:
        return
    print(f"\n  {info['ikona']}  {info['imie']}")
    print("  [1]  ⚔  Przydziel do walki")
    print("  [2]  🌲  Przydziel do zbiorów")
    print("  [3]  💰  Przydziel do handlu")
    print("  [4]  🔧  Przydziel do rzemiosła")
    print("  [5]  🚪  Zwolnij (nie wróci)")
    print("  [0]  ↩  Wróć\n")
    wybor = input("  Twój wybór: ").strip()
    if wybor == "1":
        print(_ustaw_zajecie(gracz, wpis, "walka"))
    elif wybor == "2":
        print(_ustaw_zajecie(gracz, wpis, "zbiory"))
    elif wybor == "3":
        print(_ustaw_zajecie(gracz, wpis, "handel"))
    elif wybor == "4":
        print(_ustaw_zajecie(gracz, wpis, "rzemioslo"))
    elif wybor == "5":
        _lista(gracz).remove(wpis)
        gracz.statystyki["zrekrutowani"] = len(_lista(gracz))
        print(f"  {info['imie']} opuszcza obóz.")
    nacisnij_enter()


def tura_towarzysza(gracz: Gracz, przeciwnik) -> str | None:
    """Dodatkowa akcja towarzysza po turze gracza. Zwraca 'wygrana' albo None."""
    wpis = towarzysz_walki(gracz)
    info = info_rekruta(wpis) if wpis else None
    if not info or not przeciwnik.zyje():
        return None

    atak = info["atak"] + (gracz.poziom - 1)
    if info.get("leczenie") and random.random() < 0.40 and gracz.hp < gracz.max_hp:
        lecz = info["leczenie"] + gracz.poziom
        faktyczne = min(lecz, gracz.max_hp - gracz.hp)
        gracz.hp += faktyczne
        print(
            f"  {info['ikona']}  {info['imie']} leczy cię o {faktyczne} HP!"
        )
        return None

    bazowe = max(1, atak - max(0, przeciwnik.obrona // 2))
    wariancja = max(1, bazowe // 5)
    obrazenia = random.randint(max(1, bazowe - wariancja), bazowe + wariancja)
    przeciwnik.hp -= obrazenia
    print(
        f"  {info['ikona']}  {info['imie']} atakuje {przeciwnik.nazwa}"
        f" za {obrazenia} obrażeń!"
    )
    if not przeciwnik.zyje():
        return "wygrana"
    return None


def rozlicz_zbieraczy(gracz: Gracz) -> list[str]:
    """Surowce zebrane w obozie podczas wyprawy."""
    komunikaty = []
    for r in _lista(gracz):
        if r.get("zajecie") != "zbiory":
            continue
        info = info_rekruta(r)
        if not info:
            continue
        mn, mx = info["zbior_ile"]
        ile = random.randint(mn, mx)
        dodaj_surowiec(gracz, info["zbior"], ile)
        surowiec = SUROWCE.get(info["zbior"], {})
        komunikaty.append(
            f"  {info['ikona']}  {info['imie']} przynosi "
            f"+{ile} {surowiec.get('nazwa', info['zbior'])}."
        )
    return komunikaty
