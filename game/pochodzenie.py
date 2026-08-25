"""Pochodzenie i cechy postaci (kreacja: wybór tła + 3 losowane cechy)."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz

# ------------------------------------------------------------------ #
#  Pochodzenia                                                         #
# ------------------------------------------------------------------ #

POCHODZENIA: dict[str, dict] = {
    "zolnierz": {
        "nazwa": "Żołnierz",
        "ikona": "🪖",
        "opis": "Służyłeś w szeregach. Znasz rozkazy, marsz i zapach krwi.",
        "efekty": [
            ("atrybut", "sila", 1),
            ("atrybut", "kondycja", 1),
            ("biegosc", "zastraszanie"),
            ("hp", 12),
        ],
    },
    "szlachcic": {
        "nazwa": "Szlachcic",
        "ikona": "👑",
        "opis": "Dwór, intrygi i pełna sakiewka. Słowa bywają ostrzejsze od miecza.",
        "efekty": [
            ("atrybut", "charyzma", 2),
            ("biegosc", "perswazja"),
            ("zloto", 40),
        ],
    },
    "sierota": {
        "nazwa": "Sierota uliczna",
        "ikona": "🧒",
        "opis": "Uliczne zaułki nauczyły cię kraść, kłamać i znikać.",
        "efekty": [
            ("atrybut", "zrecznosc", 1),
            ("atrybut", "charyzma", 1),
            ("biegosc", "zwinne_palce"),
            ("zloto", 15),
        ],
    },
    "pustelnik": {
        "nazwa": "Pustelnik",
        "ikona": "🪵",
        "opis": "Lata z dala od ludzi. Słyszysz las lepiej niż plotki.",
        "efekty": [
            ("atrybut", "madrosc", 2),
            ("biegosc", "przetrwanie"),
            ("surowiec", "ziola", 3),
        ],
    },
    "uczony": {
        "nazwa": "Uczony",
        "ikona": "📚",
        "opis": "Księgi, pergaminy i bezsenne noce. Umysł jest twoją bronią.",
        "efekty": [
            ("atrybut", "inteligencja", 2),
            ("biegosc", "spostrzegawczosc"),
            ("mana", 12),
        ],
    },
    "przestepca": {
        "nazwa": "Przestępca",
        "ikona": "🗡",
        "opis": "Prawo zna cię z imienia — i niekoniecznie z dobrej strony.",
        "efekty": [
            ("atrybut", "zrecznosc", 1),
            ("biegosc", "oszustwo"),
            ("zloto", 25),
            ("karma", -1),
        ],
    },
    "rzemieslnik": {
        "nazwa": "Rzemieślnik",
        "ikona": "🔧",
        "opis": "Dłonie twarde od pracy. Znasz wartość surowca i uczciwej roboty.",
        "efekty": [
            ("atrybut", "kondycja", 1),
            ("surowiec", "drewno", 3),
            ("surowiec", "kamien", 2),
            ("surowiec", "ruda", 1),
        ],
    },
    "lowca": {
        "nazwa": "Łowca",
        "ikona": "🏹",
        "opis": "Tropy, sidła i cisza przed strzałem. Dzicz to twój dom.",
        "efekty": [
            ("atrybut", "madrosc", 1),
            ("atrybut", "zrecznosc", 1),
            ("biegosc", "przetrwanie"),
            ("surowiec", "skora", 2),
        ],
    },
    "akolita": {
        "nazwa": "Akolita",
        "ikona": "🙏",
        "opis": "Służyłeś w świątyni. Wiara — albo nawyk — wciąż cię prowadzi.",
        "efekty": [
            ("atrybut", "madrosc", 1),
            ("atrybut", "charyzma", 1),
            ("mikstura", 1),
            ("karma", 2),
        ],
    },
    "banita": {
        "nazwa": "Banita",
        "ikona": "⛓",
        "opis": "Wygnany, ale nie złamany. Świat poza prawem hartuje.",
        "efekty": [
            ("atrybut", "sila", 1),
            ("biegosc", "zastraszanie"),
            ("atak", 2),
            ("hp", 8),
        ],
    },
    "zeglarz": {
        "nazwa": "Żeglarz",
        "ikona": "⚓",
        "opis": "Liny, fale i równowaga na kołyszącym się pokładzie.",
        "efekty": [
            ("atrybut", "zrecznosc", 2),
            ("biegosc", "akrobatyka"),
            ("zloto", 20),
        ],
    },
    "artysta": {
        "nazwa": "Artysta",
        "ikona": "🎭",
        "opis": "Pieśń, pędzel albo scena. Ludzie otwierają się, gdy ich rozbawisz.",
        "efekty": [
            ("atrybut", "charyzma", 1),
            ("biegosc", "perswazja"),
            ("zloto", 20),
            ("punkty_atrybutow", 1),
        ],
    },
}

KOLEJNOSC_POCHODZEN: tuple[str, ...] = (
    "zolnierz", "szlachcic", "sierota", "pustelnik", "uczony", "przestepca",
    "rzemieslnik", "lowca", "akolita", "banita", "zeglarz", "artysta",
)

# ------------------------------------------------------------------ #
#  Cechy (pula ~50)                                                    #
# ------------------------------------------------------------------ #

CECHY: dict[str, dict] = {
    "twardziel": {
        "nazwa": "Twardziel", "opis": "+18 max HP",
        "efekty": [("hp", 18)],
    },
    "kolos": {
        "nazwa": "Kolos", "opis": "+28 max HP",
        "efekty": [("hp", 28)],
    },
    "zabojca": {
        "nazwa": "Zabójca", "opis": "+3 Atak",
        "efekty": [("atak", 3)],
    },
    "pancerny": {
        "nazwa": "Pancerny", "opis": "+3 Obrona",
        "efekty": [("obrona", 3)],
    },
    "magokrwisty": {
        "nazwa": "Magokrwisty", "opis": "+18 max many (albo +12 HP, jeśli nie używasz many)",
        "efekty": [("mana", 18)],
    },
    "zasobny": {
        "nazwa": "Zasobny", "opis": "+50 złota na start",
        "efekty": [("zloto", 50)],
    },
    "aptekarz": {
        "nazwa": "Aptekarz", "opis": "+2 mikstury leczenia, +1 antidotum",
        "efekty": [("mikstura", 2), ("antidotum", 1)],
    },
    "zbieracz": {
        "nazwa": "Zbieracz", "opis": "Zapas surowców: drewno, kamień, zioła",
        "efekty": [("surowiec", "drewno", 3), ("surowiec", "kamien", 2), ("surowiec", "ziola", 2)],
    },
    "atleta": {
        "nazwa": "Atleta", "opis": "+1 Siła",
        "efekty": [("atrybut", "sila", 1)],
    },
    "akrobata": {
        "nazwa": "Akrobata", "opis": "+1 Zręczność",
        "efekty": [("atrybut", "zrecznosc", 1)],
    },
    "zelazne_pluca": {
        "nazwa": "Żelazne płuca", "opis": "+1 Kondycja",
        "efekty": [("atrybut", "kondycja", 1)],
    },
    "bystry": {
        "nazwa": "Bystry", "opis": "+1 Inteligencja",
        "efekty": [("atrybut", "inteligencja", 1)],
    },
    "czujny": {
        "nazwa": "Czujny", "opis": "+1 Mądrość",
        "efekty": [("atrybut", "madrosc", 1)],
    },
    "urokliwy": {
        "nazwa": "Urokliwy", "opis": "+1 Charyzma",
        "efekty": [("atrybut", "charyzma", 1)],
    },
    "silny_duch": {
        "nazwa": "Silny duch", "opis": "+10 HP i +10 many (albo +16 HP)",
        "efekty": [("hp", 10), ("mana", 10)],
    },
    "zapasowy_mieszek": {
        "nazwa": "Zapasowy mieszek", "opis": "+25 złota i 1 mikstura",
        "efekty": [("zloto", 25), ("mikstura", 1)],
    },
    "kowalski_zapas": {
        "nazwa": "Kowalski zapas", "opis": "+2 rudy i +2 kamienia",
        "efekty": [("surowiec", "ruda", 2), ("surowiec", "kamien", 2)],
    },
    "mysliwy": {
        "nazwa": "Myśliwy", "opis": "+3 skóry",
        "efekty": [("surowiec", "skora", 3)],
    },
    "zielarz": {
        "nazwa": "Zielarz", "opis": "+4 zioła",
        "efekty": [("surowiec", "ziola", 4)],
    },
    "szczescie_nowicjusza": {
        "nazwa": "Szczęście nowicjusza", "opis": "+1 punkt atrybutów do rozdania",
        "efekty": [("punkty_atrybutow", 1)],
    },
    "ostre_oko": {
        "nazwa": "Ostre oko", "opis": "+8% szansy na trafienie krytyczne",
        "efekty": [("kryt", 0.08)],
    },
    "cienie": {
        "nazwa": "Cienie", "opis": "+8% szansy na unik w walce",
        "efekty": [("unik", 0.08)],
    },
    "krwawy_cios": {
        "nazwa": "Krwawy cios", "opis": "Atak leczy cię o 15% zadanych obrażeń",
        "efekty": [("wampir", 0.15)],
    },
    "druga_skora": {
        "nazwa": "Druga skóra", "opis": "Regenerujesz 3 HP na turę walki",
        "efekty": [("regen_hp", 3)],
    },
    "spokojny_umysl": {
        "nazwa": "Spokojny umysł", "opis": "+4 many na turę walki (klasy z maną)",
        "efekty": [("regen_mana", 4)],
    },
    "berserker": {
        "nazwa": "Berserker", "opis": "Gdy HP spadnie poniżej 40%, ataki zadają +35% obrażeń",
        "efekty": [("berserk", 0.35)],
    },
    "groza": {
        "nazwa": "Groza", "opis": "+2 do testów Zastraszania",
        "efekty": [("test", "zastraszanie", 2)],
    },
    "slodki_jezyk": {
        "nazwa": "Słodki język", "opis": "+2 do testów Perswazji",
        "efekty": [("test", "perswazja", 2)],
    },
    "zlodziejskie_rece": {
        "nazwa": "Złodziejskie ręce", "opis": "+2 do testów Zwinnych palców",
        "efekty": [("test", "zwinne_palce", 2)],
    },
    "wspinacz": {
        "nazwa": "Wspinacz", "opis": "+2 do testów Atletyki",
        "efekty": [("test", "atletyka", 2)],
    },
    "kot": {
        "nazwa": "Kot", "opis": "+2 do testów Akrobatyki",
        "efekty": [("test", "akrobatyka", 2)],
    },
    "tropiciel": {
        "nazwa": "Tropiciel", "opis": "+2 do testów Przetrwania",
        "efekty": [("test", "przetrwanie", 2)],
    },
    "sokoli_wzrok": {
        "nazwa": "Sokoli wzrok", "opis": "+2 do testów Spostrzegawczości",
        "efekty": [("test", "spostrzegawczosc", 2)],
    },
    "klamca": {
        "nazwa": "Kłamca", "opis": "+2 do testów Oszustwa",
        "efekty": [("test", "oszustwo", 2)],
    },
    "inspiracja": {
        "nazwa": "Inspiracja", "opis": "+1 do wszystkich testów k20",
        "efekty": [("test_all", 1)],
    },
    "nocny_mysliwy": {
        "nazwa": "Nocny myśliwy", "opis": "+20% złota z walk",
        "efekty": [("loot_zloto", 0.20)],
    },
    "tani_targ": {
        "nazwa": "Tani targ", "opis": "15% zniżki w sklepie i kuźni",
        "efekty": [("znizka", 0.15)],
    },
    "zbawca": {
        "nazwa": "Zbawca", "opis": "Startowa karma +3",
        "efekty": [("karma", 3)],
    },
    "mroczny_pakt": {
        "nazwa": "Mroczny pakt", "opis": "+20 many, −8 max HP (klasy bez many: +12 ataku zamiast many)",
        "efekty": [("pakt", 1)],
    },
    "zelazna_wola": {
        "nazwa": "Żelazna wola", "opis": "50% szansy na opór wobec ogłuszenia",
        "efekty": [("odporny_stun", 0.50)],
    },
    "weteran": {
        "nazwa": "Weteran", "opis": "+10% EXP z walk i questów",
        "efekty": [("exp", 0.10)],
    },
    "grabiezca": {
        "nazwa": "Grabieżca", "opis": "+25% złota z walk",
        "efekty": [("loot_zloto", 0.25)],
    },
    "alchemik": {
        "nazwa": "Alchemik", "opis": "Mikstury leczenia leczą dodatkowe 12 HP",
        "efekty": [("mikstura_lecz", 12)],
    },
    "tarcza_losu": {
        "nazwa": "Tarcza losu", "opis": "Raz na walkę: zamiast umrzeć, zostajesz z 1 HP",
        "efekty": [("tarcza_losu", 1)],
    },
    "pierwszy_cios": {
        "nazwa": "Pierwszy cios", "opis": "Pierwszy atak w walce zadaje +50% obrażeń",
        "efekty": [("pierwszy_cios", 0.50)],
    },
    "mentor": {
        "nazwa": "Mentor", "opis": "+1 punkt umiejętności (księga w obozie)",
        "efekty": [("punkty_umiejetnosci", 1)],
    },
    "szczesliwy_lup": {
        "nazwa": "Szczęśliwy łup", "opis": "Po walce zawsze 1 mikstura (oprócz zwykłego dropu)",
        "efekty": [("lup_mikstura", 1)],
    },
    "gruba_skora": {
        "nazwa": "Gruba skóra", "opis": "Trucizna i krwawienie zadają 4 HP mniej",
        "efekty": [("odpornosc_dot", 4)],
    },
    "rumiane_zdrowie": {
        "nazwa": "Rumiane zdrowie", "opis": "+12 HP i 1 większa mikstura",
        "efekty": [("hp", 12), ("mikstura_duza", 1)],
    },
    "dzikie_serce": {
        "nazwa": "Dzikie serce", "opis": "+2 Atak i +8 HP",
        "efekty": [("atak", 2), ("hp", 8)],
    },
}

assert len(CECHY) == 50, len(CECHY)


def _lista_flag(gracz: Gracz) -> list[tuple]:
    """Efekty pochodzenia + cech (do zapytań w walce/sklepie)."""
    out: list[tuple] = []
    klucz_p = getattr(gracz, "pochodzenie", None)
    if klucz_p and klucz_p in POCHODZENIA:
        out.extend(POCHODZENIA[klucz_p]["efekty"])
    for c in getattr(gracz, "cechy", None) or []:
        if c in CECHY:
            out.extend(CECHY[c]["efekty"])
    return out


def suma_flagi(gracz: Gracz, typ: str) -> float:
    total = 0.0
    for e in _lista_flag(gracz):
        if e[0] == typ:
            total += float(e[1])
    return total


def premia_testu_cech(gracz: Gracz, skill: str) -> int:
    premia = int(suma_flagi(gracz, "test_all"))
    for e in _lista_flag(gracz):
        if e[0] == "test" and e[1] == skill:
            premia += int(e[2])
    return premia


def znizka_sklepu(gracz: Gracz) -> float:
    return min(0.30, suma_flagi(gracz, "znizka"))


def cena_dla(gracz: Gracz, baza: int) -> int:
    return max(1, int(baza * (1.0 - znizka_sklepu(gracz))))


def bonus_leczenia_mikstury(gracz: Gracz) -> int:
    return int(suma_flagi(gracz, "mikstura_lecz"))


def mnoznik_exp(gracz: Gracz) -> float:
    return 1.0 + suma_flagi(gracz, "exp")


def mnoznik_zlota_walka(gracz: Gracz) -> float:
    return 1.0 + suma_flagi(gracz, "loot_zloto")


def ma_tarczę_losu(gracz: Gracz) -> bool:
    return suma_flagi(gracz, "tarcza_losu") > 0


def nazwa_pochodzenia(gracz: Gracz) -> str:
    k = getattr(gracz, "pochodzenie", None)
    if k and k in POCHODZENIA:
        info = POCHODZENIA[k]
        return f"{info.get('ikona', '📜')} {info['nazwa']}"
    return "—"


def nazwy_cech(gracz: Gracz) -> str:
    czesci = []
    for c in getattr(gracz, "cechy", None) or []:
        if c in CECHY:
            czesci.append(CECHY[c]["nazwa"])
    return ", ".join(czesci) if czesci else "—"


def _dodaj_atrybut(gracz: Gracz, klucz: str, ile: int) -> None:
    from game.atrybuty import ATRYBUTY, MAX_ATRYBUT, zapewnij_atrybuty, modyfikator

    zapewnij_atrybuty(gracz)
    if klucz not in ATRYBUTY:
        return
    for _ in range(max(0, ile)):
        if gracz.atrybuty[klucz] >= MAX_ATRYBUT:
            break
        poprzedni = modyfikator(gracz, klucz)
        gracz.atrybuty[klucz] += 1
        nowy = modyfikator(gracz, klucz)
        if klucz == "kondycja":
            gracz.max_hp += 8
            gracz.hp += 8
        elif klucz == "sila" and nowy > poprzedni:
            gracz.atak += 1
        elif klucz == "zrecznosc" and nowy > poprzedni:
            gracz.obrona += 1
        elif klucz == "inteligencja" and gracz.max_mana > 0 and nowy > poprzedni:
            gracz.max_mana += 8
            gracz.mana += 8


def zastosuj_efekty(gracz: Gracz, efekty: list) -> None:
    from game.oboz import dodaj_surowiec

    for e in efekty:
        typ = e[0]
        if typ == "hp":
            gracz.max_hp += int(e[1])
            gracz.hp += int(e[1])
        elif typ == "atak":
            gracz.atak += int(e[1])
        elif typ == "obrona":
            gracz.obrona += int(e[1])
        elif typ == "mana":
            if gracz.max_mana > 0:
                gracz.max_mana += int(e[1])
                gracz.mana += int(e[1])
            else:
                bonus = max(8, int(e[1]) // 2)
                gracz.max_hp += bonus
                gracz.hp += bonus
        elif typ == "zloto":
            gracz.zloto = max(0, gracz.zloto + int(e[1]))
        elif typ == "mikstura":
            gracz.mikstury += int(e[1])
        elif typ == "mikstura_duza":
            gracz.mikstury_duze = getattr(gracz, "mikstury_duza", 0) + int(e[1])
        elif typ == "antidotum":
            gracz.antidota = getattr(gracz, "antidota", 0) + int(e[1])
        elif typ == "surowiec":
            dodaj_surowiec(gracz, e[1], int(e[2]))
        elif typ == "atrybut":
            _dodaj_atrybut(gracz, e[1], int(e[2]))
        elif typ == "biegosc":
            skill = e[1]
            bie = list(getattr(gracz, "biegle_skille", None) or [])
            if skill not in bie:
                bie.append(skill)
                gracz.biegle_skille = bie
        elif typ == "punkty_atrybutow":
            gracz.punkty_atrybutow = getattr(gracz, "punkty_atrybutow", 0) + int(e[1])
        elif typ == "punkty_umiejetnosci":
            gracz.punkty_umiejetnosci = getattr(gracz, "punkty_umiejetnosci", 0) + int(e[1])
        elif typ == "karma":
            gracz.karma = getattr(gracz, "karma", 0) + int(e[1])
        elif typ == "pakt":
            if gracz.max_mana > 0:
                gracz.max_mana += 20
                gracz.mana += 20
                gracz.max_hp = max(20, gracz.max_hp - 8)
                gracz.hp = min(gracz.hp, gracz.max_hp)
            else:
                gracz.atak += 2
                gracz.max_hp = max(20, gracz.max_hp - 5)
                gracz.hp = min(gracz.hp, gracz.max_hp)


def zastosuj_pochodzenie(gracz: Gracz, klucz: str) -> None:
    gracz.pochodzenie = klucz
    if klucz in POCHODZENIA:
        zastosuj_efekty(gracz, POCHODZENIA[klucz]["efekty"])


def zastosuj_ceche(gracz: Gracz, klucz: str) -> None:
    cechy = list(getattr(gracz, "cechy", None) or [])
    if klucz in cechy:
        return
    cechy.append(klucz)
    gracz.cechy = cechy
    if klucz in CECHY:
        zastosuj_efekty(gracz, CECHY[klucz]["efekty"])


def _opis_pochodzenia(klucz: str) -> str:
    info = POCHODZENIA[klucz]
    linie = [f"{info['nazwa']}", f"    {info['opis']}"]
    return "\n  ".join(linie)


def wybierz_pochodzenie() -> str:
    """Menu wyboru pochodzenia. Zwraca klucz."""
    while True:
        wyczysc()
        wyswietl_linie("═")
        print("  POCHODZENIE")
        wyswietl_linie("═")
        print("  Skąd pochodzi twój bohater? To daje biegłości i bonusy startowe.\n")
        for i, klucz in enumerate(KOLEJNOSC_POCHODZEN, 1):
            info = POCHODZENIA[klucz]
            print(f"  [{i:2}]  {info.get('ikona', '📜')} {info['nazwa']}")
            print(f"        {info['opis']}")
        print()
        wybor = input("  Twój wybór: ").strip()
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(KOLEJNOSC_POCHODZEN):
                klucz = KOLEJNOSC_POCHODZEN[idx]
                print(f"\n  Pochodzenie: {POCHODZENIA[klucz]['nazwa']}.")
                nacisnij_enter()
                return klucz
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def wybierz_trzy_cechy() -> list[str]:
    """3 rundy: losuj 4 cechy z puli, wybierz 1. Zwraca 3 klucze."""
    pula = list(CECHY.keys())
    wybrane: list[str] = []
    pokazane: set[str] = set()

    for runda in range(1, 4):
        dostepne = [k for k in pula if k not in pokazane]
        if len(dostepne) < 4:
            dostepne = [k for k in pula if k not in wybrane]
        oferty = random.sample(dostepne, k=min(4, len(dostepne)))
        pokazane.update(oferty)

        while True:
            wyczysc()
            wyswietl_linie("═")
            print(f"  CECHY POSTACI  —  wybór {runda}/3")
            wyswietl_linie("═")
            print("  Wylosowano 4 cechy. Wybierz jedną (reszta wraca do puli).\n")
            if wybrane:
                nazwy = ", ".join(CECHY[k]["nazwa"] for k in wybrane)
                print(f"  Już wybrane: {nazwy}\n")
            for i, klucz in enumerate(oferty, 1):
                info = CECHY[klucz]
                print(f"  [{i}]  {info['nazwa']}")
                print(f"       {info['opis']}")
            print()
            wybor = input("  Twój wybór: ").strip()
            try:
                idx = int(wybor) - 1
                if 0 <= idx < len(oferty):
                    wybrane.append(oferty[idx])
                    print(f"\n  Wybrano: {CECHY[oferty[idx]]['nazwa']}.")
                    nacisnij_enter()
                    break
            except ValueError:
                pass
            print("  Wpisz numer 1–4.")
            nacisnij_enter()
    return wybrane
