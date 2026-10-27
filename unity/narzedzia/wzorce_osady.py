"""Wzorce osady, budowy, badań i umiejętności dla testów zgodności portu C#.

Trzy rodzaje wzorców:

* tabele (budynki, projekty badań, zajęcia, cechy) — port przepisuje je ręcznie,
  więc test porównuje każdy wiersz z Pythonem;
* wyniki funkcji na stałych danych (umiejętności z imienia, koszty, bramki badań);
* scenariusze: stan początkowy + polecenia gracza + N dni ``osada.dzien_osady``,
  po każdym dniu migawka stanu. Ten sam seed i te same polecenia muszą dać w C#
  te same liczby i te same teksty — co do losowania.

Haczyki na moduły, których port jeszcze nie ma (więzi, myśli), są w scenariuszach
zastąpione wersją neutralną, tak samo jak po stronie C#.
"""
from __future__ import annotations

import os
import random
import sys

KORZEN = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, KORZEN)

IMIONA_PROBNE = ("Nessa", "Olek", "Jagna", "Torin", "Zażółć", "Mila", "")


def _lista(slownik) -> list:
    return [[k, v] for k, v in slownik.items()]


def wzorce_losowania_napisu() -> dict:
    """Zasiew napisem, rozkład trójkątny i próbka — to, z czego powstają umiejętności osadnika."""
    wynik = {}
    for tekst in ("kompetencje|Nessa", "kompetencje|Olek", "abc", "zażółć gęślą jaźń", ""):
        rng = random.Random(tekst)
        losowe = [rng.random() for _ in range(6)]
        rng = random.Random(tekst)
        trojkatne = [rng.triangular(0, 6, 1.5) for _ in range(6)]
        rng = random.Random(tekst)
        probka = rng.sample(list(range(10)), 2)
        calkowite = [rng.randint(2, 5) for _ in range(5)]
        wynik[tekst] = {"random": losowe, "triangular": trojkatne, "sample": probka, "randint": calkowite}
    return wynik


def wzorce_tabel() -> dict:
    from game import badania, budowa, kompetencje, osada
    from game.oboz import BUDYNKI

    budynki = [{"klucz": k, "nazwa": b["nazwa"], "ikona": b["ikona"], "max": b["max"],
                "utrzymanie": b["utrzymanie"], "opis": b["opis"], "koszt": _lista(b["koszt"]),
                "zelazo_od": b.get("zelazo_od", 2)} for k, b in BUDYNKI.items()]
    projekty = [{"id": k, "nazwa": p["nazwa"], "era": p["era"], "koszt": p["koszt"], "opis": p["opis"],
                 "wymaga": list(p["wymaga"]), "odblokowuje": _lista(p["odblokowuje"]),
                 "chaty": p["chaty"] or 0, "era_po": p["era_po"] or 0, "bonus": _lista(p["bonus"])}
                for k, p in badania.PROJEKTY.items()]
    zajecia = [{"klucz": k, "nazwa": z["nazwa"], "ikona": z["ikona"], "opis": z["opis"],
                "wymaga": z.get("wymaga"), "miejsca": z.get("miejsca", 1)}
               for k, z in osada.ZAJECIA.items()]
    cechy = [{"klucz": k, "opis": c["opis"], "praca": c.get("praca", 1.0), "je": c.get("je", 1),
              "morale": c.get("morale", 0), "choroby": c.get("choroby", 1.0)}
             for k, c in osada.CECHY_OSADNIKOW.items()]
    return {
        "budynki": budynki, "projekty": projekty, "zajecia": zajecia, "cechy": cechy,
        "praca": _lista(budowa.PRACA), "ery": _lista(badania.ERY),
        "start_poziomy": _lista(badania.START_POZIOMY),
        "umiejetnosci": [[k, v["nazwa"], v["ikona"]] for k, v in kompetencje.UMIEJETNOSCI.items()],
        "umiejetnosc_zajecia": _lista(kompetencje.UMIEJETNOSC_ZAJECIA),
        "stale": {"skala_kosztu": badania.SKALA_KOSZTU, "baza_punktow": badania.BAZA_PUNKTOW,
                  "limit_planu": budowa.LIMIT_PLANU, "start_chaty": badania.START_CHATY,
                  "max_chaty": osada.MAX_CHATY, "koszt_chaty": _lista(osada.KOSZT_CHATY)},
    }


def _gracz_pusty():
    from game.player import Gracz
    g = Gracz("Wzorzec", "Wojownik")
    g.w_obozie = True
    g.osadnicy = []
    g.talenty = []
    return g


def wzorce_kompetencji() -> dict:
    from game import kompetencje, osada

    osadnicy = []
    for imie in IMIONA_PROBNE:
        o = osada._zapewnij_osadnika({"imie": imie, "zajecie": "rolnik", "morale": 60.0, "cecha": "skromny"})
        osadnicy.append({
            "imie": imie,
            "umiejetnosci": [[k, w["p"], w["xp"], w["pasja"]] for k, w in o["kompetencje"].items()],
            "krotki": kompetencje.opis_krotki(o, 3),
            "pelny": kompetencje.opis_pelny(o, ile=5, zajecie="rolnik"),
            "pelny_wszystko": kompetencje.opis_pelny(o),
        })

    # Nauka przez pracę: 140 dni w jednym zawodzie, dla trzech pasji.
    nauka = []
    for pasja in (0, 1, 2):
        o = osada._zapewnij_osadnika({"imie": "Nessa", "zajecie": "rolnik", "morale": 60.0, "cecha": "skromny"})
        o["kompetencje"]["uprawa"].update({"p": 0, "xp": 0.0, "pasja": pasja})
        wiesci = []
        probki = []
        for dzien in range(140):
            wiesci += kompetencje.cwicz(o, "rolnik", 1.0 if dzien % 3 else 0.5)
            w = o["kompetencje"]["uprawa"]
            probki.append([w["p"], w["xp"]])
        nauka.append({"pasja": pasja, "wiesci": wiesci, "probki": probki})

    migracje = []
    for dosw in ({"rolnik": 65, "drwal": 12}, {"handlarz": 30}, {"badacz": 9, "tracz": 61}, {}):
        o = {"imie": "Stary", "zajecie": "rolnik", "dosw": dict(dosw)}
        k = kompetencje.zapewnij(o)
        migracje.append({"dosw": _lista(dosw), "umiejetnosci": [[u, w["p"]] for u, w in k.items()]})

    czynniki = []
    for z in ("drwal", "rolnik", "bezczynny", "badacz", "straznik"):
        for p in (0, 4, 12, 20):
            o = osada._zapewnij_osadnika({"imie": "Nessa", "zajecie": z, "morale": 60.0, "cecha": "skromny"})
            u = kompetencje.umiejetnosc_zajecia(z)
            if u:
                o["kompetencje"][u]["p"] = p
            czynniki.append([z, p, kompetencje.czynnik(o)])
    return {"osadnicy": osadnicy, "nauka": nauka, "migracje": migracje, "czynniki": czynniki,
            "xp_krzywa": [kompetencje.xp_do_nastepnego(p) for p in range(-1, 22)]}


def wzorce_kosztow() -> list:
    from game import oboz
    wynik = []
    for talent in ((), ("architekt",)):
        for klucz, b in oboz.BUDYNKI.items():
            for poziom in range(1, b["max"] + 2):
                g = _gracz_pusty()
                g.talenty = list(talent)
                oboz.ustaw_poziom(g, klucz, poziom - 1) if poziom > 1 else None
                koszt = oboz.koszt_budowy(g, klucz)
                wynik.append({"talent": list(talent), "klucz": klucz, "poziom": poziom,
                              "koszt": None if koszt is None else _lista(koszt),
                              "tekst": None if koszt is None else oboz._format_kosztu(koszt)})
    return wynik


def wzorce_bramek() -> list:
    """Bramki badań w kilku stanach: co wolno badać, jaka epoka, jakie poziomy i limit chat."""
    from game import badania, oboz
    stany = [
        ([], 0), ([], 1), (["obrobka_drewna"], 1), (["obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje"], 2),
        (["obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje", "wiek_zelaza"], 1),
        (["obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje", "wiek_zelaza"], 2),
        (["obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje", "wiek_zelaza", "osadnictwo", "wytop",
          "kowalstwo", "murowanie", "archiwum", "wiek_mistrzow"], 3),
        (list(badania.PROJEKTY), 3),
    ]
    wynik = []
    for zrobione, skr in stany:
        g = _gracz_pusty()
        if skr:
            oboz.ustaw_poziom(g, "skryptorium", skr)
        badania.stan(g)["zrobione"] = list(zrobione)
        wynik.append({
            "zrobione": zrobione, "skryptorium": skr, "era": badania.era(g),
            "limit_chat": badania.limit_chat(g),
            "blokady": [[pid, badania.powod_blokady(g, pid)] for pid in badania.PROJEKTY],
            "dostepne": badania.dostepne(g),
            "maks": [[k, badania.maks_poziom(g, k)] for k in oboz.BUDYNKI],
            "bonus": [[z, badania.bonus_zajecia(g, z)] for z in
                      ("rolnik", "straznik", "hutnik", "gornik", "tracz", "drwal", "rzemieslnik")],
            "opisy": [[pid, badania.opis_efektow(pid)] for pid in badania.PROJEKTY],
            "projekt_dla": [[k, p, badania.projekt_dla(k, p)] for k in oboz.BUDYNKI for p in (1, 2, 3)],
            "projekt_dla_chat": badania.projekt_dla_chat(g),
        })
    return wynik


def _migawka(g, kronika, pilne, wyniki) -> dict:
    from game import badania, budowa, oboz, osada
    b = badania.stan(g)
    return {
        "czas": g.czas, "zloto": g.zloto,
        "surowce": [[k, g.surowce.get(k, 0)] for k in oboz.SUROWCE],
        "narzedzia": g.narzedzia, "chaty": g.chaty,
        "poziomy": sorted([k, v] for k, v in g.poziomy_budynkow.items()),
        "osadnicy": [[o["imie"], o["zajecie"], o["morale"], o.get("chory", 0),
                      [[k, w["p"], w["xp"]] for k, w in o["kompetencje"].items()]]
                     for o in osada.osadnicy(g)],
        "plany": [[p["id"], p["klucz"], p["poziom"], p["zrobiona"]] for p in budowa.plany(g)],
        "badania": {"zrobione": list(b["zrobione"]), "aktualny": b["aktualny"],
                    "postep": sorted([k, v] for k, v in b["postep"].items()), "zapas": b["zapas"]},
        "flagi": {k: g.flagi.get(k, 0) for k in ("dni_glodu_osady", "morale_wydarzenia", "zepsute_razem")},
        "zimno": bool(g.flagi.get("zimno")),
        "bilans": sorted([k, v] for k, v in g.flagi.get("bilans_dnia", {}).items()),
        "kronika": list(kronika), "pilne": list(pilne), "wyniki": list(wyniki),
        "punkty_dzienne": badania.punkty_dzienne(g), "moc_budowy": budowa.moc_budowy(g),
    }


def _wykonaj(g, akcja) -> str:
    from game import badania, budowa, osada
    typ = akcja["typ"]
    if typ == "zlec":
        return budowa.zlec(g, akcja["arg"])
    if typ == "projekt":
        return badania.wybierz_projekt(g, akcja["arg"])
    if typ == "anuluj":
        return budowa.anuluj(g, akcja["arg"])
    if typ == "zajecie":
        o = next(o for o in osada.osadnicy(g) if o["imie"] == akcja["imie"])
        return osada.ustaw_zajecie(g, o, akcja["arg"])
    if typ == "auto_projekt":
        if badania.stan(g)["aktualny"] is None:
            for pid in badania.dostepne(g):
                return badania.wybierz_projekt(g, pid)
        return ""
    raise ValueError(typ)


def _uruchom_scenariusz(scen: dict) -> list:
    from game import mysli, oboz, osada, wiezi
    g = _gracz_pusty()          # konstruktor graczą losuje (seed świata) — dopiero potem zasiewamy
    s = scen["stan"]
    g.czas, g.zloto, g.chaty, g.narzedzia = s["czas"], s["zloto"], s["chaty"], s.get("narzedzia", 0)
    g.talenty = list(s.get("talenty", []))
    g.surowce = {k: s["surowce"].get(k, 0) for k in oboz.SUROWCE}
    for k, v in s["poziomy"].items():
        oboz.ustaw_poziom(g, k, v)
    g.osadnicy = [dict(o, chory=o.get("chory", 0)) for o in s["osadnicy"]]
    g.badania["zrobione"] = list(s.get("zrobione", []))
    # Moduły, których port jeszcze nie ma, zastępujemy wersją neutralną (jak w C#).
    stare = (wiezi.przelicz_dzien, wiezi.premia_morale, wiezi.po_stracie, mysli.premia_morale)
    wiezi.przelicz_dzien = lambda *a, **k: []
    wiezi.premia_morale = lambda *a, **k: 0.0
    wiezi.po_stracie = lambda *a, **k: []
    mysli.premia_morale = lambda *a, **k: 0.0
    random.seed(scen["seed"])
    probki = []
    try:
        for dzien in range(scen["dni"]):
            wyniki = [_wykonaj(g, a) for a in scen["akcje"] if a["dzien"] == dzien or a.get("codziennie")]
            g.czas += 1
            kronika, pilne = osada.dzien_osady(g)
            probki.append(_migawka(g, kronika, pilne, wyniki))
    finally:
        wiezi.przelicz_dzien, wiezi.premia_morale, wiezi.po_stracie, mysli.premia_morale = stare
    return probki


def _o(imie, zajecie, cecha, morale=60.0, chory=0) -> dict:
    return {"imie": imie, "zajecie": zajecie, "cecha": cecha, "morale": morale, "chory": chory}


def wzorce_scenariuszy() -> list:
    bogaty = {"zywnosc": 80, "drewno": 120, "kamien": 80, "ziola": 20, "skora": 10, "ruda": 30,
              "deski": 20, "zelazo": 6}
    scenariusze = [
        {
            "nazwa": "wiosna: budowa i badania", "seed": 11, "dni": 60,
            "stan": {"czas": 0, "zloto": 400, "chaty": 4, "surowce": bogaty, "poziomy": {"skryptorium": 1},
                     "osadnicy": [_o("Olek", "drwal", "pracowity"), _o("Jagna", "mysliwy", "wesoly"),
                                  _o("Nessa", "badacz", "skromny"), _o("Torin", "budowniczy", "zarloczny"),
                                  _o("Mila", "bezczynny", "leniwy")]},
            "akcje": [
                {"dzien": 0, "typ": "projekt", "arg": "obrobka_drewna"},
                {"dzien": 0, "typ": "zlec", "arg": "dom"}, {"dzien": 0, "typ": "zlec", "arg": "farma"},
                {"dzien": 0, "typ": "zlec", "arg": "tartak"}, {"dzien": 1, "typ": "zlec", "arg": "chata"},
                {"dzien": 2, "typ": "zlec", "arg": "palisada"}, {"dzien": 2, "typ": "zlec", "arg": "sklep"},
                {"dzien": 3, "typ": "anuluj", "arg": 3},
                {"dzien": 10, "typ": "zajecie", "imie": "Mila", "arg": "budowniczy"},
                {"dzien": 12, "typ": "zlec", "arg": "tartak"},
                {"dzien": 0, "typ": "auto_projekt", "codziennie": True},
            ],
        },
        {
            "nazwa": "zima: głód, mróz, choroby", "seed": 5, "dni": 50,
            "stan": {"czas": 82, "zloto": 60, "chaty": 8, "narzedzia": 2, "talenty": ["gospodarz", "lichwiarz"],
                     "surowce": {"zywnosc": 14, "drewno": 9, "kamien": 5, "ruda": 8, "deski": 2},
                     "poziomy": {"farma": 1, "tartak": 1, "huta": 1, "targ": 2, "spichlerz": 1, "lecznica": 1,
                                 "tawerna": 1, "skryptorium": 2, "warsztat": 1},
                     "osadnicy": [_o("Olek", "rolnik", "pracowity"), _o("Jagna", "tracz", "zarloczny"),
                                  _o("Bram", "hutnik", "chorowity"), _o("Elka", "handlarz", "gadatliwy"),
                                  _o("Wit", "kamieniarz", "tchorzliwy", 30.0), _o("Hela", "uzdrowiciel", "wesoly"),
                                  _o("Lech", "zielarz", "leniwy", 25.0, 4), _o("Ruta", "gornik", "odwazny"),
                                  _o("Kael", "badacz", "skromny")]},
            "akcje": [{"dzien": 0, "typ": "projekt", "arg": "obrobka_drewna"}],
        },
        {
            "nazwa": "epoki: badania do wieku żelaza", "seed": 23, "dni": 130,
            "stan": {"czas": 40, "zloto": 600, "chaty": 6, "surowce": bogaty, "poziomy": {"skryptorium": 2, "dom": 1},
                     "zrobione": ["obrobka_drewna", "kamieniarstwo", "plodozmian", "zwoje"],
                     "osadnicy": [_o("Nessa", "badacz", "pracowity"), _o("Sira", "badacz", "skromny"),
                                  _o("Piotr", "budowniczy", "skromny"), _o("Iva", "rolnik", "wesoly")]},
            "akcje": [{"dzien": 0, "typ": "auto_projekt", "codziennie": True},
                      {"dzien": 5, "typ": "zlec", "arg": "dom"}, {"dzien": 5, "typ": "zlec", "arg": "skryptorium"}],
        },
    ]
    for s in scenariusze:
        s["probki"] = _uruchom_scenariusz(s)
    return scenariusze


def wzorce_osady() -> dict:
    return {
        "losowanie_napisu": wzorce_losowania_napisu(),
        "tabele": wzorce_tabel(),
        "kompetencje": wzorce_kompetencji(),
        "koszty": wzorce_kosztow(),
        "bramki": wzorce_bramek(),
        "scenariusze": wzorce_scenariuszy(),
    }
