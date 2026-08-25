"""Moduł zapisu i wczytywania stanu gry (format JSON)."""

import json
import os
from pathlib import Path

from game.player import Gracz
from game.skills import MAX_RANGA
from game.mapa import SRODEK

_PLIK_ZAPISU = Path("savegame.json")


def _gracz_do_dict(gracz: Gracz) -> dict:
    """Serializuje obiekt gracza do słownika."""
    return {
        "imie": gracz.imie,
        "klasa": gracz.klasa,
        "podklasa": gracz.podklasa,
        "podklasa_dostepna": gracz.podklasa_dostepna,
        "mapa_x": gracz.mapa_x,
        "mapa_y": gracz.mapa_y,
        "aktualny_biom": gracz.aktualny_biom,
        "mapa_gen": gracz.mapa_gen,
        "mapa_pola": getattr(gracz, "mapa_pola", None),
        "punkty_atrybutow": getattr(gracz, "punkty_atrybutow", 0),
        "poziom": gracz.poziom,
        "exp": gracz.exp,
        "zloto": gracz.zloto,
        "max_hp": gracz.max_hp,
        "hp": gracz.hp,
        "atak": gracz.atak,
        "obrona": gracz.obrona,
        "mikstury": gracz.mikstury,
        "max_mana": gracz.max_mana,
        "mana": gracz.mana,
        "_hp_na_poziom": gracz._hp_na_poziom,
        "_atak_na_poziom": gracz._atak_na_poziom,
        "_obrona_na_poziom": gracz._obrona_na_poziom,
        "wyposazenie": gracz.wyposazenie,
        "aktywne_questy": list(gracz.aktywne_questy),
        "ukonczone_questy": list(gracz.ukonczone_questy),
        "statystyki": gracz.statystyki,
        "umiejetnosci": gracz.umiejetnosci,
        "rangi_umiejetnosci": getattr(gracz, "rangi_umiejetnosci", {}),
        "punkty_umiejetnosci": getattr(gracz, "punkty_umiejetnosci", 0),
        "osiagniecia": list(getattr(gracz, "osiagniecia", set())),
        "tryb_trudnosci": getattr(gracz, "tryb_trudnosci", "normalny"),
        "plecak": list(getattr(gracz, "plecak", [])),
        "mikstury_duze": getattr(gracz, "mikstury_duze", 0),
        "mikstury_many": getattr(gracz, "mikstury_many", 0),
        "antidota": getattr(gracz, "antidota", 0),
        "karma": getattr(gracz, "karma", 0),
        "surowce": dict(getattr(gracz, "surowce", {})),
        "budynki": list(getattr(gracz, "budynki", set())),
        "rekruci": list(getattr(gracz, "rekruci", [])),
        "czas": int(getattr(gracz, "czas", 0) or 0),
        "czas_wyjscia": int(getattr(gracz, "czas_wyjscia", 0) or 0),
        "chaty": int(getattr(gracz, "chaty", 0) or 0),
        "osadnicy": list(getattr(gracz, "osadnicy", []) or []),
        "watki_npc": dict(getattr(gracz, "watki_npc", {}) or {}),
        "atrybuty": dict(getattr(gracz, "atrybuty", {}) or {}),
        "biegle_skille": list(getattr(gracz, "biegle_skille", []) or []),
        "pochodzenie": getattr(gracz, "pochodzenie", None),
        "cechy": list(getattr(gracz, "cechy", []) or []),
    }


def _dict_do_gracza(dane: dict) -> Gracz:
    """Odtwarza obiekt gracza ze słownika."""
    gracz = Gracz(dane["imie"], dane["klasa"])
    gracz.podklasa = dane.get("podklasa")
    gracz.podklasa_dostepna = dane.get("podklasa_dostepna", False)
    gracz.mapa_x = dane.get("mapa_x", SRODEK)
    gracz.mapa_y = dane.get("mapa_y", SRODEK)
    gracz.aktualny_biom = dane.get("aktualny_biom", "Obóz")
    gracz.mapa_gen = dane.get("mapa_gen", 1)
    gracz.mapa_pola = dane.get("mapa_pola")
    gracz.punkty_atrybutow = dane.get("punkty_atrybutow", 0)
    gracz.poziom = dane.get("poziom", 1)
    gracz.exp = dane.get("exp", 0)
    gracz.zloto = dane.get("zloto", 30)
    gracz.max_hp = dane.get("max_hp", gracz.max_hp)
    gracz.hp = dane.get("hp", gracz.hp)
    gracz.atak = dane.get("atak", gracz.atak)
    gracz.obrona = dane.get("obrona", gracz.obrona)
    gracz.mikstury = dane.get("mikstury", gracz.mikstury)
    gracz.max_mana = dane.get("max_mana", gracz.max_mana)
    gracz.mana = dane.get("mana", gracz.mana)
    gracz._hp_na_poziom = dane.get("_hp_na_poziom", gracz._hp_na_poziom)
    gracz._atak_na_poziom = dane.get("_atak_na_poziom", gracz._atak_na_poziom)
    gracz._obrona_na_poziom = dane.get("_obrona_na_poziom", gracz._obrona_na_poziom)
    gracz.wyposazenie = dane.get("wyposazenie", {"bron": None, "zbroja": None})
    gracz.aktywne_questy = set(dane.get("aktywne_questy", []))
    gracz.ukonczone_questy = set(dane.get("ukonczone_questy", []))
    gracz.statystyki = dane.get("statystyki", gracz.statystyki)
    gracz.umiejetnosci = dane.get("umiejetnosci", gracz.umiejetnosci)
    zapisane_rangi = dane.get("rangi_umiejetnosci") or {}
    gracz.rangi_umiejetnosci = {
        k: max(1, min(MAX_RANGA, int(zapisane_rangi.get(k, 1))))
        for k in gracz.umiejetnosci
    }
    gracz._odblokuj_umiejetnosci(gracz.poziom)
    if "punkty_umiejetnosci" in dane:
        gracz.punkty_umiejetnosci = dane.get("punkty_umiejetnosci", 0)
    else:
        oczekiwane = sum(
            2 if p in (5, 10, 15) else 1 for p in range(2, gracz.poziom + 1)
        )
        if gracz.podklasa:
            oczekiwane += 1
        wydane = sum(max(0, r - 1) for r in gracz.rangi_umiejetnosci.values())
        gracz.punkty_umiejetnosci = max(0, oczekiwane - wydane)
    gracz.osiagniecia = set(dane.get("osiagniecia", []))
    gracz.tryb_trudnosci = dane.get("tryb_trudnosci", "normalny")
    gracz.plecak = list(dane.get("plecak", []))
    gracz.mikstury_duze = dane.get("mikstury_duze", 0)
    gracz.mikstury_many = dane.get("mikstury_many", 0)
    gracz.antidota = dane.get("antidota", 0)
    gracz.karma = dane.get("karma", 0)
    if "surowce" in dane:
        gracz.surowce = {
            "drewno": 0, "kamien": 0, "ziola": 0, "skora": 0, "ruda": 0,
        }
        gracz.surowce.update(dane.get("surowce") or {})
    # brak klucza = stary zapis sprzed systemu obozu — daj zapas startowy
    gracz.budynki = set(dane.get("budynki") or [])
    gracz.rekruci = list(dane.get("rekruci") or [])
    gracz.czas = int(dane.get("czas", 0) or 0)
    gracz.czas_wyjscia = int(dane.get("czas_wyjscia", 0) or 0)
    gracz.chaty = int(dane.get("chaty", 0) or 0)
    gracz.osadnicy = list(dane.get("osadnicy") or [])
    gracz.watki_npc = dict(dane.get("watki_npc") or {})
    from game.atrybuty import zapewnij_atrybuty, startowe_atrybuty, biegle_skille_klasy

    if dane.get("atrybuty"):
        gracz.atrybuty = dict(dane["atrybuty"])
    else:
        gracz.atrybuty = startowe_atrybuty(gracz.klasa)
    if dane.get("biegle_skille"):
        gracz.biegle_skille = list(dane["biegle_skille"])
    else:
        gracz.biegle_skille = biegle_skille_klasy(gracz.klasa)
    zapewnij_atrybuty(gracz)
    gracz.pochodzenie = dane.get("pochodzenie")
    gracz.cechy = list(dane.get("cechy") or [])
    return gracz


def zapisz_gre(gracz: Gracz) -> bool:
    """Zapisuje stan gry do pliku JSON. Zwraca True przy sukcesie."""
    try:
        dane = _gracz_do_dict(gracz)
        with open(_PLIK_ZAPISU, "w", encoding="utf-8") as f:
            json.dump(dane, f, ensure_ascii=False, indent=2)
        return True
    except OSError:
        return False


def wczytaj_gre() -> Gracz | None:
    """Wczytuje zapis gry z pliku JSON. Zwraca Gracz lub None gdy brak zapisu."""
    if not _PLIK_ZAPISU.exists():
        return None
    try:
        with open(_PLIK_ZAPISU, "r", encoding="utf-8") as f:
            dane = json.load(f)
        return _dict_do_gracza(dane)
    except (OSError, json.JSONDecodeError, KeyError):
        return None


def zapis_istnieje() -> bool:
    """Sprawdza czy plik zapisu istnieje."""
    return _PLIK_ZAPISU.exists()


def usun_zapis() -> None:
    """Usuwa plik zapisu (np. po śmierci w hardcore)."""
    if _PLIK_ZAPISU.exists():
        try:
            os.remove(_PLIK_ZAPISU)
        except OSError:
            pass
