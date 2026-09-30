"""Obrona osady: zagrożenie, zapowiedzi najazdów, fale i pojedynek z hersztem.

Zagrożenie rośnie co dzień (szybciej zimą i im bogatsza osada). Po przekroczeniu
progu zwiadowcy zapowiadają najazd — wieża podaje dokładną datę i siłę.
Jeśli bohater jest w obozie, prowadzi obronę sam: rozmowa z hersztem
(testy w stylu Disco Elysium), trzy fale z wyborem taktyki i pojedynek.
Pod jego nieobecność osada broni się sama — palisadą, wieżą i strażnikami.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import kalendarz, talenty
from game.oboz import SUROWCE, BUDYNKI, _magazyn, poziom_budynku
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

PROG_NAJAZDU = 100.0

_BANDY = (
    ("Czerwone Kaptury", "rozbójnicy z traktu"),
    ("Szarzy Wilcy", "najemnicy bez pana"),
    ("Gnijący Legion", "nieumarli z bagien"),
    ("Kły Gór", "orkowie zza przełęczy"),
    ("Bractwo Popiołu", "fanatycy spalonej świątyni"),
)


def sila_obrony(gracz: "Gracz") -> int:
    from game.osada import CECHY_OSADNIKOW, mnoznik_pracy, osadnicy
    from game.rekruci import REKRUCI

    sila = 18 * poziom_budynku(gracz, "palisada") + 10 * poziom_budynku(gracz, "wieza")
    for o in osadnicy(gracz):
        if o["zajecie"] == "straznik" and not o.get("chory"):
            sila += 6 * mnoznik_pracy(gracz, o) * CECHY_OSADNIKOW.get(o["cecha"], {}).get("straz", 1.0)
    for r in getattr(gracz, "rekruci", None) or []:
        info = REKRUCI.get(r.get("klucz"))
        if info and r.get("zajecie") == "obrona":
            sila += 6 + info.get("atak", 8) // 2
    if talenty.ma(gracz, "strateg"):
        sila *= 1.25
    return int(sila)


def _wzrost_zagrozenia(gracz: "Gracz") -> float:
    wartosc_skarbca = gracz.zloto + sum(_magazyn(gracz).values()) * 2
    return (1.4 + gracz.czas / 70) * kalendarz.pora(gracz)["zagrozenie"] * (1 + min(0.6, wartosc_skarbca / 1500))


def sila_najazdu(gracz: "Gracz") -> int:
    return int((16 + gracz.czas * 0.35 + 6 * int(getattr(gracz, "najazdy", 0) or 0)) * random.uniform(0.85, 1.15))


def opis_zagrozenia(gracz: "Gracz") -> str:
    if getattr(gracz, "najazd_za", None) is not None:
        if poziom_budynku(gracz, "wieza"):
            return f"⚔ NAJAZD za {gracz.najazd_za} dni (siła {gracz.flagi.get('sila_najazdu', '?')})"
        return "⚔ najazd nadciąga!"
    return f"zagrożenie {int(getattr(gracz, 'zagrozenie', 0))}%"


def dzien_obrony(gracz: "Gracz") -> list[str]:
    """Wywoływane raz dziennie przez game.swiat."""
    if getattr(gracz, "flagi", None) is None:
        gracz.flagi = {}
    if getattr(gracz, "najazd_za", None) is not None:
        gracz.najazd_za -= 1
        if gracz.najazd_za <= 0:
            return najazd(gracz)
        if gracz.najazd_za == 1:
            return ["  🔔  Dym na horyzoncie. Najazd JUTRO."]
        return []
    gracz.zagrozenie = float(getattr(gracz, "zagrozenie", 0) or 0) + _wzrost_zagrozenia(gracz)
    if gracz.zagrozenie < PROG_NAJAZDU:
        return []
    banda, kto = random.choice(_BANDY)
    gracz.flagi["banda"] = banda
    gracz.flagi["sila_najazdu"] = sila_najazdu(gracz)
    wieza = poziom_budynku(gracz, "wieza")
    gracz.najazd_za = random.randint(3, 4) + 2 * wieza
    if wieza:
        return [f"  🗼  Wieża: {banda} ({kto}) szykują najazd — za {gracz.najazd_za} dni, siła ok. "
                f"{gracz.flagi['sila_najazdu']} (twoja obrona: {sila_obrony(gracz)})."]
    return [f"  👣  Zwiadowcy donoszą: {banda} ({kto}) krążą wokół osady. Najazd lada dzień."]


# ------------------------------------------------------------------ #
#  Najazd                                                              #
# ------------------------------------------------------------------ #

def _koniec_najazdu(gracz: "Gracz") -> None:
    gracz.najazd_za = None
    gracz.zagrozenie = random.uniform(0, 15)
    gracz.najazdy = int(getattr(gracz, "najazdy", 0) or 0) + 1
    gracz.flagi.pop("sila_najazdu", None)


def _straty(gracz: "Gracz", przewaga: float) -> list[str]:
    """przewaga 0..1: jak bardzo banda przełamała obronę."""
    from game.osada import osadnicy, zmien_morale

    msgs = []
    if przewaga <= 0:
        return msgs
    mag = _magazyn(gracz)
    zrabowane = []
    for k in SUROWCE:
        ile = int(mag.get(k, 0) * 0.4 * przewaga)
        if ile:
            mag[k] -= ile
            zrabowane.append(f"{SUROWCE[k]['ikona']}{ile}")
    zloto = int(gracz.zloto * 0.2 * przewaga) if not getattr(gracz, "w_obozie", True) else 0
    if zloto:
        gracz.zloto -= zloto
        zrabowane.append(f"💰{zloto}")
    if zrabowane:
        msgs.append("  🔥  Zrabowano: " + "  ".join(zrabowane))
    if random.random() < przewaga:
        zbudowane = [k for k in BUDYNKI if poziom_budynku(gracz, k)]
        cel = "palisada" if poziom_budynku(gracz, "palisada") else (random.choice(zbudowane) if zbudowane else None)
        if cel:
            nowy = poziom_budynku(gracz, cel) - 1
            gracz.poziomy_budynkow[cel] = nowy
            if nowy <= 0:
                gracz.poziomy_budynkow.pop(cel, None)
                gracz.budynki.discard(cel)
                msgs.append(f"  🔥  {BUDYNKI[cel]['nazwa']} spłonęła doszczętnie.")
            else:
                msgs.append(f"  🔥  {BUDYNKI[cel]['nazwa']} uszkodzona (poziom {nowy}).")
    for o in list(osadnicy(gracz)):
        szansa = (0.3 if o["zajecie"] == "straznik" else 0.08) * przewaga
        if random.random() < szansa:
            osadnicy(gracz).remove(o)
            msgs.append(f"  ⚰  {o['imie']} ginie w obronie osady.")
    zmien_morale(gracz, -20 * przewaga)
    return msgs


def najazd(gracz: "Gracz") -> list[str]:
    sila = int(gracz.flagi.get("sila_najazdu") or sila_najazdu(gracz))
    banda = gracz.flagi.get("banda", "Banda")
    if getattr(gracz, "w_obozie", True) and gracz.zyje():
        wynik = obrona_osady(gracz, sila, banda)
        _koniec_najazdu(gracz)
        return [] if wynik != "przegrana" else ["  ☠  Poległeś, broniąc osady."]
    from game.osada import zmien_morale
    obrona = sila_obrony(gracz) * random.uniform(0.8, 1.2)
    atak = sila * random.uniform(0.8, 1.2)
    msgs = [f"  ⚔  POSŁANIEC Z OSADY: {banda} napadli na obóz pod twoją nieobecność! (siła {sila} vs obrona {int(obrona)})"]
    if obrona >= atak:
        msgs.append("  🛡  Obrońcy odparli atak. Ludzie są z siebie dumni.")
        zmien_morale(gracz, 6)
        gracz.statystyki["odparte_najazdy"] = gracz.statystyki.get("odparte_najazdy", 0) + 1
    else:
        msgs += _straty(gracz, min(1.0, (atak - obrona) / atak + 0.2))
    _koniec_najazdu(gracz)
    return msgs


_TAKTYKI = [
    ("palisada", "🏹", "Łucznicy i włócznie na palisadę", "obrona ×1,3 z palisadą (×0,9 bez)"),
    ("wypad", "⚔", "Wypad za bramę — walczysz z przodownikiem fali", "wygrana łamie falę o połowę"),
    ("smola", "🔥", "Smoła i ogień (4 drewna)", "obrona ×1,6 w tej fali"),
    ("ukryj", "🛖", "Ukryj ludzi w chatach", "obrona ×0,6, ale nikt nie zginie"),
]


def obrona_osady(gracz: "Gracz", sila: int, banda: str) -> str:
    """Najazd z bohaterem na miejscu. Zwraca 'wygrana', 'czesciowo' albo 'przegrana'."""
    from game.combat import przeprowadz_walke
    from game.enemy import herszt_najazdu, losuj_przeciwnika
    from game.osada import zmien_morale
    from game.rozmowy import rozmowa_z_hersztem

    wyczysc()
    wyswietl_linie("═")
    print(f"  🔔  NAJAZD NA OSADĘ — {banda}")
    wyswietl_linie("═")
    print(f"\n  Siła bandy: ok. {sila}.  Twoja obrona: {sila_obrony(gracz)}.")
    print("  Zanim polecą strzały, herszt podjeżdża pod bramę. Chce gadać.")
    nacisnij_enter()

    wynik_rozmowy = rozmowa_z_hersztem(gracz, sila, banda)
    if wynik_rozmowy == "odwolany":
        print("\n  Banda zawraca. Nikt dziś nie zginie.")
        zmien_morale(gracz, 8)
        nacisnij_enter()
        return "wygrana"
    if wynik_rozmowy == "oslabiony":
        sila = int(sila * 0.6)
        print(f"\n  Część bandy odjeżdża. Zostaje ok. {sila} najbardziej upartych.")
        nacisnij_enter()

    przelamanie = 0.0
    for fala in range(1, 4):
        sila_fali = sila / 3
        wyczysc()
        wyswietl_linie()
        print(f"  ⚔  FALA {fala}/3 — napiera ok. {int(sila_fali)} siły.  Obrona: {sila_obrony(gracz)}.  HP: {gracz.hp}/{gracz.max_hp}")
        wyswietl_linie()
        for i, (_, ikona, nazwa, opis) in enumerate(_TAKTYKI, 1):
            print(f"  [{i}] {ikona}  {nazwa} — {opis}")
        wybor = input("\n  Rozkaz: ").strip()
        taktyka = _TAKTYKI[int(wybor) - 1][0] if wybor.isdigit() and 1 <= int(wybor) <= 4 else "palisada"
        mnoznik = 1.0
        bez_ofiar = False
        if taktyka == "palisada":
            mnoznik = 1.3 if poziom_budynku(gracz, "palisada") else 0.9
        elif taktyka == "smola":
            mag = _magazyn(gracz)
            if mag.get("drewno", 0) >= 4:
                mag["drewno"] -= 4
                mnoznik = 1.6
                print("  🔥  Kadzie smoły lecą z palisady!")
            else:
                print("  Brakuje drewna na smołę — ludzie walczą czym mają.")
        elif taktyka == "ukryj":
            mnoznik, bez_ofiar = 0.6, True
        elif taktyka == "wypad":
            print("\n  Otwierasz bramę i ruszasz na przodownika fali.")
            nacisnij_enter()
            wrog = losuj_przeciwnika(biom=None, poziom=max(1, min(14, sila // 22)))
            wrog.nazwa = f"{wrog.nazwa} z bandy"
            wynik = przeprowadz_walke(gracz, przeciwnik=wrog)
            if wynik == "przegrana":
                return "przegrana"
            if wynik == "wygrana":
                sila_fali /= 2
                print("  Przodownik padł — fala się łamie!")
        obrona = sila_obrony(gracz) * mnoznik * random.uniform(0.85, 1.15) + 5 * gracz.poziom
        if obrona >= sila_fali:
            print(f"\n  🛡  Fala odparta ({int(obrona)} vs {int(sila_fali)}).")
        else:
            przewaga = (sila_fali - obrona) / sila_fali
            przelamanie += przewaga / 3
            print(f"\n  💥  Fala wdziera się za palisadę ({int(obrona)} vs {int(sila_fali)})!")
            if bez_ofiar:
                print("  Ludzie przeczekali w chatach — ale bandyci plądrują.")
        nacisnij_enter()

    print(f"\n  Herszt bandy {banda} wychodzi na plac sam. Tylko ty i on.")
    nacisnij_enter()
    wynik = przeprowadz_walke(gracz, przeciwnik=herszt_najazdu(sila, getattr(gracz, "tryb_trudnosci", "normalny")))
    if wynik == "przegrana":
        return "przegrana"
    if wynik == "wygrana":
        print("\n  🏆  Herszt pada. Banda się rozpierzcha, porzucając łupy.")
        gracz.zloto += 10 + sila // 3
        gracz.karma = int(getattr(gracz, "karma", 0) or 0) + 1
        zmien_morale(gracz, 12)
        przelamanie *= 0.5
        gracz.statystyki["odparte_najazdy"] = gracz.statystyki.get("odparte_najazdy", 0) + 1
    else:
        print("\n  Wycofujesz się. Banda plądruje, zanim odjedzie.")
        przelamanie = min(1.0, przelamanie + 0.3)
    for msg in _straty(gracz, przelamanie):
        print(msg)
    nacisnij_enter()
    return "wygrana" if przelamanie < 0.1 else "czesciowo"


def menu_obrony(gracz: "Gracz") -> None:
    from game.osada import CECHY_OSADNIKOW, osadnicy

    wyczysc()
    wyswietl_linie("═")
    print("  🛡  OBRONA OSADY")
    wyswietl_linie("═")
    print(f"\n  Stan: {opis_zagrozenia(gracz)}")
    print(f"  Siła obrony: {sila_obrony(gracz)}")
    print(f"    🪵 palisada poz. {poziom_budynku(gracz, 'palisada')} (+18/poziom)")
    print(f"    🗼 wieża poz. {poziom_budynku(gracz, 'wieza')} (+10/poziom, wcześniejsze ostrzeżenie)")
    straz = [o for o in osadnicy(gracz) if o["zajecie"] == "straznik"]
    print(f"    🛡 strażnicy: {len(straz)}" + (" — " + ", ".join(f"{o['imie']} ({o['cecha']})" for o in straz) if straz else ""))
    obroncy = [r for r in (gracz.rekruci or []) if r.get("zajecie") == "obrona"]
    print(f"    ⚔ drużyna na murach: {len(obroncy)}")
    print(f"\n  Odparte najazdy: {gracz.statystyki.get('odparte_najazdy', 0)} / {getattr(gracz, 'najazdy', 0)}")
    print("  Najazdy rosną z czasem. Zima i bogaty skarbiec przyciągają bandy.")
    print("  Gdy jesteś w obozie, bronisz osady osobiście (negocjacje, fale, pojedynek).")
    nacisnij_enter()
