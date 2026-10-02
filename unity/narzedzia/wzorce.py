#!/usr/bin/env python3
"""Zrzuca wzorce z wersji pythonowej, do których porównuje się port na C#.

Port do Unity ma dawać ten sam świat co wersja w Pythonie — ten sam seed musi
dać te same biomy, te same punkty na mapie i te same piksele kafli. Zamiast
wierzyć na słowo, zapisujemy tu wzorce z Pythona, a testy w ``unity/Testy``
czytają je i porównują z wynikiem C#.

Uruchomienie (z katalogu głównego repozytorium):

    python unity/narzedzia/wzorce.py

Pliki lądują w ``unity/Testy/wzorce/``. Po zmianie logiki świata albo rysowania
kafli trzeba je wygenerować ponownie i sprawdzić, czy port nadal się zgadza.
"""
from __future__ import annotations

import base64
import json
import os
import random
import sys

KATALOG = os.path.dirname(os.path.abspath(__file__))
KORZEN = os.path.dirname(os.path.dirname(KATALOG))
WYJSCIE = os.path.join(os.path.dirname(KATALOG), "Testy", "wzorce")

sys.path.insert(0, KORZEN)


def zapisz(nazwa: str, dane) -> None:
    os.makedirs(WYJSCIE, exist_ok=True)
    sciezka = os.path.join(WYJSCIE, nazwa)
    with open(sciezka, "w", encoding="utf-8") as f:
        json.dump(dane, f, ensure_ascii=False, indent=1)
    print(f"  zapisano {os.path.relpath(sciezka, KORZEN)}")


# ------------------------------------------------------------------ #
#  Generator losowy                                                    #
# ------------------------------------------------------------------ #

def wzorce_losowania() -> dict:
    """Surowe wyniki random.Random dla kilku seedów.

    Sprawdzamy każdą metodę, której używa generowanie świata, bo każda zużywa
    inną liczbę słów generatora — pomyłka w jednej rozjechałaby całą dalszą
    sekwencję, nawet gdyby pierwsze liczby się zgadzały.
    """
    wynik = {}
    for seed in (0, 1, 42, 12345, 2_147_483_646, 988_061):
        rng = random.Random(seed)
        losowe = [rng.random() for _ in range(8)]
        rng = random.Random(seed)
        bity = [rng.getrandbits(k) for k in (1, 3, 7, 8, 16, 31, 32)]
        rng = random.Random(seed)
        calkowite = [rng.randint(0, 8) for _ in range(12)]
        rng = random.Random(seed)
        zakresy = [rng.randrange(1, 2 ** 31) for _ in range(4)]
        rng = random.Random(seed)
        biomy = ("równiny", "ruiny", "las", "bagna", "wzgórza", "kanion")
        probki = [rng.sample(list(biomy), k=k) for k in (4, 5, 6)]
        rng = random.Random(seed)
        wybory = [rng.choice(list(biomy)) for _ in range(10)]
        wynik[str(seed)] = {
            "random": losowe,
            "getrandbits": bity,
            "randint_0_8": calkowite,
            "randrange_1_2p31": zakresy,
            "sample": probki,
            "choice": wybory,
        }
    return wynik


# ------------------------------------------------------------------ #
#  Mapa świata                                                         #
# ------------------------------------------------------------------ #

def _splaszcz_region(pola) -> dict:
    return {
        "biomy": [[p["biom"] for p in wiersz] for wiersz in pola],
        "punkty": [[p["punkt"] or "" for p in wiersz] for wiersz in pola],
    }


def wzorce_mapy() -> dict:
    """Pełne regiony dla różnych seedów i współrzędnych.

    Region startowy, sąsiedzi, regiony dalekie (gdzie wchodzą mityczne punkty
    i miasta) oraz współrzędne ujemne — czyli wszystkie gałęzie generatora.
    """
    from game.mapa import generuj_mape, poziom_regionu

    wynik = {}
    przypadki = [
        (0, 0, 0), (1, 0, 0), (42, 0, 0), (988_061, 0, 0),
        (42, 1, 0), (42, 0, 1), (42, -1, 0), (42, 0, -1), (42, -3, 2),
        (42, 4, 4), (42, -5, -5), (12345, 2, -3), (2_147_483_646, 7, 0),
    ]
    for seed, rx, ry in przypadki:
        poziom = poziom_regionu(rx, ry)
        pola = generuj_mape(poziom, seed, rx, ry)
        wynik[f"{seed}|{rx}|{ry}"] = dict(_splaszcz_region(pola), poziom=poziom)
    return wynik


# ------------------------------------------------------------------ #
#  Pixelart                                                            #
# ------------------------------------------------------------------ #

def wzorce_pikseli() -> dict:
    """Wartości funkcji szumu i rampy — fundament całego rysowania."""
    from grafika.piksele import ciemniej, jasniej, szum, szum_gladki, z_rampy
    from grafika.teren import KAMIEN, RAMPY

    szumy = []
    for x, y, s in ((0, 0, 0), (1, 2, 3), (31, 15, 7), (100, 200, 300),
                    (320, 240, 65535), (7, 7, 99)):
        szumy.append({"x": x, "y": y, "s": s, "v": szum(x, y, s)})

    gladkie = []
    for x, y, skala, s in ((0.0, 0.0, 3.0, 7), (6.2, 3.1, 2.2, 0), (31.0, 30.0, 4.0, 12),
                           (0.4, 11.0, 6.0, 5), (64.0, 48.0, 5.0, 10)):
        gladkie.append({"x": x, "y": y, "skala": skala, "s": s,
                        "v": szum_gladki(x, y, skala, s)})

    rampowe = []
    for v in (0.0, 0.17, 0.33, 0.5, 0.62, 0.78, 1.0, 1.4, -0.3):
        for x, y in ((0, 0), (1, 2), (3, 3), (2, 1)):
            rampowe.append({"v": v, "x": x, "y": y,
                            "kolor": list(z_rampy(RAMPY["las"], v, x, y))})

    return {
        "szum": szumy,
        "szum_gladki": gladkie,
        "z_rampy_las": rampowe,
        "ciemniej": [list(ciemniej(KAMIEN[2], f)) for f in (0.6, 0.7, 0.8, 1.0)],
        "jasniej": [list(jasniej(KAMIEN[1], f)) for f in (1.18, 1.0, 1.5)],
    }


def _zrzut_powierzchni(pow_) -> dict:
    """Powierzchnia pygame → piksele RGBA w base64, gotowe do porównania.

    Pierwotnie szedł tu zwykły spis liczb, ale sam plik z kaflami urósł
    wtedy do ćwierć megabajta — jeden piksel zajmował kilkanaście znaków.
    Base64 surowych bajtów daje siedem razy mniej i czytelny diff:
    zmiana w rysowaniu to jedna zmieniona linia, a nie tysiąc liczb.
    """
    w, h = pow_.get_size()
    bajty = bytearray()
    for y in range(h):
        for x in range(w):
            r, g, b, a = pow_.get_at((x, y))
            bajty.extend((r, g, b, a))
    return {"w": w, "h": h, "piksele_b64": base64.b64encode(bytes(bajty)).decode("ascii")}


def wzorce_kafli() -> dict:
    """Gotowe kafle, mgła i obwódka — piksel w piksel.

    Kafel to najgęstszy kawałek matematyki w całej grafice: szum gładki, szum
    punktowy, rampy z ditheringiem, warstwy skały i darń na krawędzi. Jeśli
    kafle zgadzają się co do piksela, reszta prymitywów też stoi na prawdzie.
    """
    from grafika import teren

    wynik = {}
    for biom in ("równiny", "las", "bagna", "wzgórza", "kanion", "ruiny"):
        s = {"równiny": 11, "las": 1234, "bagna": 77, "wzgórza": 5,
             "kanion": 404, "ruiny": 9}[biom]
        h = teren.wysokosc_pola(biom, 3, 4, s)
        # Uwaga: "h" w zrzucie to wysokość obrazka, więc wysokość pola idzie
        # pod osobnym kluczem — inaczej jedna nadpisałaby drugą.
        wynik[f"kafel|{biom}"] = dict(_zrzut_powierzchni(teren.kafel(biom, h, s)),
                                      wysokosc_pola=h, s=s)
    # Bagna mają trzy klatki animacji wody — sprawdzamy każdą.
    for klatka in (1, 2):
        wynik[f"kafel|bagna|k{klatka}"] = dict(
            _zrzut_powierzchni(teren.kafel("bagna", 1, 77, klatka)),
            wysokosc_pola=1, s=77)

    kaf = teren.kafel("las", 5, 1234)
    wynik["mgla|dzien"] = _zrzut_powierzchni(teren.zamglij(kaf, 2, 6, False))
    wynik["mgla|noc"] = _zrzut_powierzchni(teren.zamglij(kaf, 2, 6, True))
    wynik["obwodka"] = _zrzut_powierzchni(teren.obwodka())
    return wynik


def wzorce_sprite() -> dict:
    """Roślinność, skały i sylwetka bohatera."""
    from grafika import teren
    from grafika.piksele import kula

    wynik = {
        "kula|7x6": _zrzut_powierzchni(kula(7, 6, teren.RAMPY["las"], 3, 0.3)),
        "sosna|22": _zrzut_powierzchni(teren.sosna(22, 5)),
        "drzewo_lisciaste": _zrzut_powierzchni(teren.drzewo_lisciaste(8)),
        "skala|4x3": _zrzut_powierzchni(teren.skala(4, 3, 12)),
        "trzcina": _zrzut_powierzchni(teren.trzcina(6)),
        "kolumna|9": _zrzut_powierzchni(teren.kolumna(4, 9)),
    }
    for klasa in ("wojownik", "mag", "łotrzyk", "druid", "nekromanta"):
        wynik[f"gracz|{klasa}"] = _zrzut_powierzchni(teren.sprite_gracza(klasa))
    return wynik


# ------------------------------------------------------------------ #
#  Logika postaci                                                      #
# ------------------------------------------------------------------ #

def wzorce_postaci() -> dict:
    """Progi EXP, awanse, atrybuty startowe, kalendarz i karma."""
    from game import kalendarz, karma
    from game.atrybuty import KOLEJNOSC_ATRYBUTOW, biegle_skille_klasy, startowe_atrybuty
    from game.player import Gracz, _prog_exp

    klasy = ("Wojownik", "Mag", "Lotrzyk", "Druid", "Nekromanta")
    start = {}
    for klasa in klasy:
        g = Gracz("Test", klasa)
        start[klasa] = {
            "max_hp": g.max_hp, "atak": g.atak, "obrona": g.obrona,
            "max_mana": g.max_mana, "mikstury": g.mikstury,
            "mikstury_many": g.mikstury_many, "zloto": g.zloto,
            "atrybuty": [startowe_atrybuty(klasa)[k] for k in KOLEJNOSC_ATRYBUTOW],
            "biegle": biegle_skille_klasy(klasa),
        }

    awanse = {}
    for klasa in klasy:
        g = Gracz("Test", klasa)
        slad = []
        for _ in range(12):
            g.exp = g.exp_do_awansu()
            g._awansuj()
            slad.append({"poziom": g.poziom, "max_hp": g.max_hp, "atak": g.atak,
                         "obrona": g.obrona, "max_mana": g.max_mana,
                         "pkt_atr": g.punkty_atrybutow,
                         "pkt_um": g.punkty_umiejetnosci,
                         "pkt_tal": g.punkty_talentow})
        awanse[klasa] = slad

    kalendarzowe = []
    for dzien in (0, 1, 29, 30, 59, 60, 89, 90, 119, 120, 121, 365, 480):
        g = Gracz("Test")
        g.czas = dzien
        kalendarzowe.append({
            "dzien": dzien, "pora": kalendarz.pora(g)["klucz"],
            "rok": kalendarz.rok(g), "dzien_pory": kalendarz.dzien_pory(g),
            "do_zimy": kalendarz.dni_do_zimy(g),
        })

    karmowe = []
    for k in (-30, -12, -11, -10, -5, -4, 0, 4, 5, 11, 12, 30):
        g = Gracz("Test")
        g.karma = k
        karmowe.append({
            "karma": k, "poziom": karma.poziom(g)[0],
            "mnoznik_cen": karma.mnoznik_cen(g),
            "rekrutacja": karma.modyfikator_rekrutacji(g),
            "swiatynia": karma.bonus_swiatyni(g),
        })

    return {
        "progi_exp": [_prog_exp(p) for p in range(0, 20)],
        "start": start,
        "awanse": awanse,
        "kalendarz": kalendarzowe,
        "karma": karmowe,
    }


def main() -> int:
    print("Zrzucam wzorce dla portu na C#…")
    zapisz("losowanie.json", wzorce_losowania())
    zapisz("mapa.json", wzorce_mapy())
    zapisz("postac.json", wzorce_postaci())
    try:
        import pygame  # noqa: F401
    except ImportError:
        print("  (pominięto wzorce grafiki — brak pygame)")
        return 0
    os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
    zapisz("piksele.json", wzorce_pikseli())
    zapisz("kafle.json", wzorce_kafli())
    zapisz("sprite.json", wzorce_sprite())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
