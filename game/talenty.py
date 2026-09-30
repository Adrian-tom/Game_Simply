"""Drzewko talentów: sześć gałęzi, po pięć węzłów w każdej.

Talenty są niezależne od klasy — spinają systemy, które klasa nie obejmuje:
przetrwanie, osadę, rzemiosło, handel i „umysł” (rozmowy w stylu Disco Elysium).
Węzeł wymaga poprzedniego w tej samej gałęzi, a węzły 4–5 także poziomu postaci.
Punkt talentu przychodzi z każdym awansem (i jeden za każde pokolenie rodu).
"""
from __future__ import annotations

from typing import TYPE_CHECKING

from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

# Minimalny poziom postaci dla węzła n-tego w gałęzi (1-5).
PROG_POZIOMU = {1: 1, 2: 1, 3: 4, 4: 7, 5: 10}

GALEZIE: dict[str, dict] = {
    "wojaczka": {
        "nazwa": "Wojaczka",
        "ikona": "⚔",
        "wezly": [
            ("twarda_skora", "Twarda skóra", "+3 do obrony na stałe."),
            ("kontra", "Kontra", "Po gardzie następny cios ×1.6 zamiast ×1.3."),
            ("zabojca_potworow", "Zabójca potworów", "+20% obrażeń przeciw bossom i mitycznym bestiom."),
            ("zahartowany_w_boju", "Zahartowany w boju", "Szansa na ranę w walce o połowę mniejsza."),
            ("nieustepliwy", "Nieustępliwy", "Raz na walkę zamiast paść zostajesz z 1 HP."),
        ],
    },
    "przetrwanie": {
        "nazwa": "Przetrwanie",
        "ikona": "🐾",
        "wezly": [
            ("oszczedny", "Oszczędny", "25% szans, że dzień wyprawy nie zje racji."),
            ("tropiciel", "Tropiciel", "+50% surowców i żywności ze zbieractwa na mapie."),
            ("hartowany", "Hartowany", "Zima nie rani bez ciepłego odzienia; głód działa o połowę słabiej."),
            ("polowy_medyk", "Polowy medyk", "Rany goją się dwa razy szybciej, bandaż leczy mocniej."),
            ("szosty_zmysl", "Szósty zmysł", "Zasadzki o połowę rzadsze, ucieczka +15%."),
        ],
    },
    "przywodztwo": {
        "nazwa": "Przywództwo",
        "ikona": "🏰",
        "wezly": [
            ("gospodarz", "Gospodarz", "+10% produkcji całej osady."),
            ("charyzmatyczny_wodz", "Charyzmatyczny wódz", "+10 do docelowego morale osadników."),
            ("strateg", "Strateg", "+25% siły obrony osady."),
            ("architekt", "Architekt", "Budowa i rozbudowa tańsza o 20%."),
            ("legenda_osady", "Legenda osady", "Wieść niesie się sama: osadnicy czasem przychodzą za darmo."),
        ],
    },
    "rzemioslo": {
        "nazwa": "Rzemiosło i alchemia",
        "ikona": "⚗",
        "wezly": [
            ("zielarz", "Zielarz", "Mikstury i maści leczą o 30% mocniej."),
            ("eksperymentator", "Eksperymentator", "Eksperymenty w laboratorium częściej odkrywają przepisy."),
            ("mistrz_kuzni", "Mistrz kuźni", "Ulepszenia sprzętu tańsze o 30% i o jeden poziom wyżej."),
            ("alchemik", "Alchemik", "30% szans na dodatkową sztukę przy każdym wytworzeniu."),
            ("kamien_filozoficzny", "Kamień filozoficzny", "Eliksiry bojowe działają dwa razy dłużej."),
        ],
    },
    "handel": {
        "nazwa": "Handel",
        "ikona": "💰",
        "wezly": [
            ("targowanie", "Targowanie", "Ceny o 10% korzystniejsze przy każdym handlu."),
            ("konwoj", "Konwój", "Ryzyko napadu na karawanę mniejsze o 40%."),
            ("siec_kupcow", "Sieć kupców", "Karawany przywożą o 25% więcej złota."),
            ("lichwiarz", "Lichwiarz", "Dochód z targu +50%."),
            ("ksiaze_kupiecki", "Książę kupiecki", "Karawany zabierają dwa razy więcej towaru."),
        ],
    },
    "umysl": {
        "nazwa": "Umysł",
        "ikona": "🧠",
        "wezly": [
            ("wewnetrzny_glos", "Wewnętrzny głos", "Głosy umiejętności odzywają się częściej (bierne testy +2)."),
            ("drugie_podejscie", "Drugie podejście", "+1 do wszystkich testów w rozmowach."),
            ("czytanie_ludzi", "Czytanie ludzi", "+2 do perswazji, oszustwa i zastraszania."),
            ("gleboka_mysl", "Głęboka myśl", "+1 miejsce w gabinecie myśli, przyswajanie szybsze o połowę."),
            ("epifania", "Epifania", "Raz na rozmowę możesz powtórzyć nieudany test."),
        ],
    },
}

TALENTY: dict[str, dict] = {}
for _g, _info in GALEZIE.items():
    for _i, (_k, _nazwa, _opis) in enumerate(_info["wezly"], 1):
        TALENTY[_k] = {"galaz": _g, "stopien": _i, "nazwa": _nazwa, "opis": _opis}


def ma(gracz: "Gracz", klucz: str) -> bool:
    return klucz in (getattr(gracz, "talenty", None) or [])


def punkty(gracz: "Gracz") -> int:
    return int(getattr(gracz, "punkty_talentow", 0) or 0)


def dostepny(gracz: "Gracz", klucz: str) -> tuple[bool, str]:
    """Czy talent da się teraz wykupić — i jeśli nie, dlaczego."""
    info = TALENTY[klucz]
    if ma(gracz, klucz):
        return False, "już masz"
    if info["stopien"] > 1:
        poprzedni = GALEZIE[info["galaz"]]["wezly"][info["stopien"] - 2][0]
        if not ma(gracz, poprzedni):
            return False, "wymaga poprzedniego"
    prog = PROG_POZIOMU[info["stopien"]]
    if gracz.poziom < prog:
        return False, f"od poziomu {prog}"
    if punkty(gracz) <= 0:
        return False, "brak punktów"
    return True, ""


def wykup(gracz: "Gracz", klucz: str) -> str:
    mozna, powod = dostepny(gracz, klucz)
    if not mozna:
        return f"  Nie można: {powod}."
    gracz.talenty = list(getattr(gracz, "talenty", None) or []) + [klucz]
    gracz.punkty_talentow = punkty(gracz) - 1
    if klucz == "twarda_skora":
        gracz.obrona += 3
    info = TALENTY[klucz]
    return f"  ✨  Nowy talent: {info['nazwa']} — {info['opis']}"


def zapewnij_talenty(gracz: "Gracz") -> None:
    """Stary zapis bez talentów dostaje punkty za dotychczasowe awanse."""
    if getattr(gracz, "talenty", None) is None:
        gracz.talenty = []
    if getattr(gracz, "punkty_talentow", None) is None:
        gracz.punkty_talentow = max(0, gracz.poziom - 1) - len(gracz.talenty)


def menu_talentow(gracz: "Gracz") -> None:
    zapewnij_talenty(gracz)
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  🌳  DRZEWKO TALENTÓW")
        wyswietl_linie("═")
        print(f"\n  Punkty talentów: {punkty(gracz)}   (1 za awans; węzły 3/4/5 od poziomu 4/7/10)\n")
        numeracja: list[str] = []
        for galaz, info in GALEZIE.items():
            print(f"  {info['ikona']}  {info['nazwa'].upper()}")
            for klucz, nazwa, opis in info["wezly"]:
                numeracja.append(klucz)
                nr = len(numeracja)
                if ma(gracz, klucz):
                    znak = "●"
                    dopisek = ""
                else:
                    mozna, powod = dostepny(gracz, klucz)
                    znak = "◐" if mozna else "○"
                    dopisek = "" if mozna else f"  ({powod})"
                print(f"  [{nr:>2}] {znak} {nazwa} — {opis}{dopisek}")
            print()
        print("  ● masz   ◐ do wykupienia   ○ zablokowany")
        print("  [0] Wróć\n")
        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
        except ValueError:
            idx = -1
        if 0 <= idx < len(numeracja):
            print(wykup(gracz, numeracja[idx]))
        else:
            print("  Nieprawidłowy wybór.")
        nacisnij_enter()
