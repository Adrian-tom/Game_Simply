"""Śmierć z konsekwencjami i dziedziczenie osady.

* Tryb łatwy i normalny: bohater nie ginie na zawsze — osadnicy znajdują go
  nieprzytomnego. Traci prowiant, część złota i łupów, budzi się w obozie
  po kilku dniach z ciężką raną, a osada przeżyła te dni bez niego.
  (Wcześniej zapis zostawał z 0 HP i śmierć nic nie kosztowała.)
* Tryb hardcore: bohater ginie naprawdę — ale osada żyje dalej.
  Dziedzicem zostaje ktoś z drużyny albo osadników (połowa poziomu,
  połowa złota i reputacji, cały dorobek osady). Kto nie zostawił nikogo,
  ten przegrywa na dobre.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

DNI_NIEPRZYTOMNOSCI = 3

# Pola, które należą do osady i świata, a nie do bohatera — przechodzą na dziedzica.
POLA_OSADY = (
    "seed", "regiony", "budynki", "poziomy_budynkow", "chaty", "osadnicy", "rekruci", "surowce",
    "skladniki", "przepisy", "zapasy_cel", "narzedzia", "karawany", "ceny", "czas", "czas_wyjscia",
    "zagrozenie", "najazd_za", "najazdy", "kronika", "sprawy", "flagi", "watki_npc", "statystyki",
    "osiagniecia", "ukonczone_questy", "tryb_trudnosci", "rod", "pokolenie",
)

_KLASY_REKRUTOW = {
    "boris": "Wojownik", "mira": "Druid", "durin": "Wojownik", "kora": "Lotrzyk", "ashen": "Nekromanta",
    "boldan": "Wojownik", "aldric": "Lotrzyk", "grimbold": "Wojownik", "eremiel": "Mag",
    "alderon": "Wojownik", "mirena": "Mag", "vasco": "Lotrzyk",
}
_KLASY_ZAWODOW = {
    "straznik": "Wojownik", "drwal": "Wojownik", "kamieniarz": "Wojownik", "gornik": "Wojownik",
    "mysliwy": "Lotrzyk", "handlarz": "Lotrzyk", "zielarz": "Druid", "rolnik": "Druid",
    "uzdrowiciel": "Druid", "rzemieslnik": "Mag", "tracz": "Wojownik", "hutnik": "Wojownik",
}


def _do_obozu(gracz: "Gracz") -> None:
    from game.mapa import SRODEK, region_pola

    gracz.region_x = gracz.region_y = 0
    gracz.mapa_x = gracz.mapa_y = SRODEK
    gracz.mapa_gen = 1
    gracz.mapa_pola = region_pola(gracz, 0, 0)
    gracz.w_obozie = True


def omdlenie(gracz: "Gracz") -> None:
    """Upadek w trybie normalnym: kara, ale gra toczy się dalej."""
    from game import przetrwanie
    from game.osada import zmien_morale
    from game.swiat import minij_dni

    wyczysc()
    wyswietl_linie("═")
    print("  ☠  PADASZ")
    wyswietl_linie("═")
    stracone_zloto = int(gracz.zloto * 0.3)
    gracz.zloto -= stracone_zloto
    plecak = list(getattr(gracz, "plecak", None) or [])
    stracone = [p for p in plecak if random.random() < 0.5]
    for p in stracone:
        gracz.plecak.remove(p)
    for k in list((gracz.przedmioty or {}).keys()):
        if k != "cieple_odzienie":
            gracz.przedmioty[k] //= 2
    gracz.prowiant = 0
    gracz.glod = 0
    print("\n  Ciemność. Potem głosy, kołysanie wozu, zapach dymu.")
    print("  Osadnicy znaleźli cię w błocie, obdartego do koszuli.")
    print(f"\n  Stracone: {stracone_zloto} zł, prowiant" + (f", {len(stracone)} rzeczy z plecaka" if stracone else "")
          + ", połowa eliksirów i bomb.")
    print(przetrwanie.dodaj_rane(gracz, "ciezka_rana"))
    _do_obozu(gracz)
    gracz.hp = max(1, int(gracz.max_hp * 0.3))
    gracz.mana = 0
    zmien_morale(gracz, -10)
    gracz.statystyki["omdlenia"] = gracz.statystyki.get("omdlenia", 0) + 1
    print(f"\n  Leżysz w gorączce przez {DNI_NIEPRZYTOMNOSCI} dni. Osada radzi sobie bez ciebie.")
    for msg in minij_dni(gracz, DNI_NIEPRZYTOMNOSCI, odpoczynek=True):
        print(msg)
    if not gracz.zyje():
        gracz.hp = 1
    nacisnij_enter()


def _kandydaci(gracz: "Gracz") -> list[dict]:
    from game.osada import osadnicy
    from game.rekruci import REKRUCI

    wynik = []
    for r in getattr(gracz, "rekruci", None) or []:
        info = REKRUCI.get(r.get("klucz"))
        if info:
            wynik.append({"imie": info["imie"], "klasa": _KLASY_REKRUTOW.get(r["klucz"], "Wojownik"),
                          "zrodlo": ("rekrut", r), "opis": info["opis"]})
    for o in osadnicy(gracz):
        wynik.append({"imie": o["imie"], "klasa": _KLASY_ZAWODOW.get(o["zajecie"], "Wojownik"),
                      "zrodlo": ("osadnik", o), "opis": f"{o['zajecie']}, {o['cecha']}"})
    return wynik


def sukcesja(gracz: "Gracz") -> "Gracz | None":
    """Hardcore: wybór dziedzica. None = nie ma nikogo, koniec rodu."""
    from game import mysli
    from game.player import Gracz, _prog_exp
    from game.talenty import zapewnij_talenty

    kandydaci = _kandydaci(gracz)
    wyczysc()
    wyswietl_linie("═")
    print(f"  ⚰  {gracz.imie.upper()} NIE ŻYJE")
    wyswietl_linie("═")
    print(f"\n  {gracz.klasa}, poziom {gracz.poziom}. Pokolenie {getattr(gracz, 'pokolenie', 1)}. Dzień {gracz.czas}.")
    if not kandydaci:
        print("\n  Nie zostawiłeś nikogo, kto mógłby poprowadzić osadę. Chaty pustoszeją.")
        print("  Ród wygasa.")
        nacisnij_enter()
        return None
    print("\n  Osada zbiera się przy ognisku. Ktoś musi przejąć ster.\n")
    for i, k in enumerate(kandydaci, 1):
        print(f"  [{i}] {k['imie']} — przyszły {k['klasa']} ({k['opis']})")
    wybor = input("\n  Kto zostanie dziedzicem? ").strip()
    idx = int(wybor) - 1 if wybor.isdigit() and 1 <= int(wybor) <= len(kandydaci) else 0
    wybrany = kandydaci[idx]

    nowy = Gracz(wybrany["imie"].split()[0], wybrany["klasa"])
    for pole in POLA_OSADY:
        if hasattr(gracz, pole):
            setattr(nowy, pole, getattr(gracz, pole))
    typ, wpis = wybrany["zrodlo"]
    if typ == "rekrut":
        nowy.rekruci = [r for r in nowy.rekruci if r is not wpis]
    else:
        nowy.osadnicy = [o for o in nowy.osadnicy if o is not wpis]
    nowy.rod = list(getattr(gracz, "rod", None) or []) + [
        {"imie": gracz.imie, "klasa": gracz.klasa, "poziom": gracz.poziom, "dzien": gracz.czas}
    ]
    nowy.pokolenie = int(getattr(gracz, "pokolenie", 1) or 1) + 1
    nowy.zloto = gracz.zloto // 2
    nowy.karma = int(getattr(gracz, "karma", 0) or 0) // 2
    poziom = max(1, gracz.poziom // 2)
    nowy.punkty_talentow = 0
    zapewnij_talenty(nowy)
    if poziom > 1:
        with _cicho():
            nowy.zdobadz_exp(_prog_exp(poziom - 1) - nowy.exp)
    nowy.punkty_talentow += nowy.pokolenie - 1
    nowy.mysli = {}
    mysli.odkryj(nowy, "cien_przodka")
    _do_obozu(nowy)
    nowy.hp, nowy.mana = nowy.max_hp, nowy.max_mana
    from game.mapa import zapewnij_mape
    zapewnij_mape(nowy)
    print(f"\n  {nowy.imie} ({nowy.klasa}, poziom {nowy.poziom}) przejmuje osadę. Pokolenie {nowy.pokolenie}.")
    print(f"  Dziedzictwo: +{nowy.pokolenie - 1} punkt(y) talentów, myśl „Cień przodka” do przyswojenia.")
    nacisnij_enter()
    return nowy


class _cicho:
    """Tłumi komunikaty awansów przy odtwarzaniu poziomu dziedzica."""

    def __enter__(self):
        import builtins
        self._print = builtins.print
        builtins.print = lambda *a, **k: None
        return self

    def __exit__(self, *exc):
        import builtins
        builtins.print = self._print
        return False


def po_smierci(gracz: "Gracz") -> "Gracz | None":
    """Zwraca postać, którą gra się dalej (ta sama, dziedzic) albo None (koniec gry)."""
    if getattr(gracz, "tryb_trudnosci", "normalny") == "hardcore":
        return sukcesja(gracz)
    omdlenie(gracz)
    return gracz
