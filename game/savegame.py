"""Moduł zapisu i wczytywania stanu gry (format JSON)."""

import json
import os
from pathlib import Path

from game.player import Gracz

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
        "osiagniecia": list(getattr(gracz, "osiagniecia", set())),
    }


def _dict_do_gracza(dane: dict) -> Gracz:
    """Odtwarza obiekt gracza ze słownika."""
    gracz = Gracz(dane["imie"], dane["klasa"])
    gracz.podklasa = dane.get("podklasa")
    gracz.podklasa_dostepna = dane.get("podklasa_dostepna", False)
    gracz.mapa_x = dane.get("mapa_x", 2)
    gracz.mapa_y = dane.get("mapa_y", 2)
    gracz.aktualny_biom = dane.get("aktualny_biom", "Obóz")
    gracz.mapa_gen = dane.get("mapa_gen", 1)
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
    gracz.osiagniecia = set(dane.get("osiagniecia", []))
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
