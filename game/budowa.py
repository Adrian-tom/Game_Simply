"""Budowa w czasie: plany, budowniczowie i rusztowania — jak blueprint w RimWorldzie.

Zlecenie budowy **od razu pobiera materiały** i zakłada plan; potem dzień po
dniu przybywa pracy, aż budynek stanie. Pracują:

* **budowniczowie** (zajęcie „budowniczy”) — pełna moc, rośnie ich umiejętność Budowa;
* **bezczynni** osadnicy — 35% mocy (też się uczą, połowę wolniej);
* **pozostali** — 10% mocy „po godzinach”, żeby osada z jednym drwalem i myśliwym
  nie utknęła na zawsze (bohater przyspiesza budowę osobiście: ``pomoz``).

Jeden plan przyjmuje dziennie co najwyżej ``LIMIT_PLANU`` pracy, więc dziesięciu
budowniczych nie postawi huty w jeden dzień, tylko kilku plany naraz.

Czego **wolno** budować, mówi ``game.badania`` (nauka w skryptorium); ile **trwa**,
mówi tabela ``PRACA``. Plan trzyma kopię kosztu — anulowanie zwraca część materiałów.
"""
from __future__ import annotations

from typing import TYPE_CHECKING

from game import atrybuty, badania, kalendarz, kompetencje
from game.oboz import (
    BUDYNKI,
    _format_kosztu,
    _magazyn,
    _moze_zaplacic,
    _pobierz_koszt,
    koszt_budowy,
    linia_surowcow,
    poziom_budynku,
    ustaw_poziom,
    utrzymanie_dzienne,
)
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

MAKS_PLANOW = 4
LIMIT_PLANU = 2.5            # tyle pracy dziennie przyjmie jeden plan
WAGA_BEZCZYNNEGO = 0.35
WAGA_POZOSTALYCH = 0.10
PRACA_BOHATERA = 1.5         # dzień bohatera przy budowie (plus premia z siły)
ZWROT_MIN = 0.5              # anulowanie prawie skończonej budowy zwraca połowę materiałów
KOSZT_CHATY_PRACA = 2.0

# Dni pracy budowniczego (mnożnik 1,0) na poziom 1; poziom n wymaga baza × n.
PRACA: dict[str, float] = {
    "sklep": 2.0, "dom": 3.0, "farma": 2.0, "spichlerz": 3.0, "tartak": 3.0, "huta": 4.0,
    "kuznia": 4.0, "warsztat": 3.0, "laboratorium": 5.0, "lecznica": 3.0, "targ": 3.0,
    "karawanseraj": 5.0, "tawerna": 3.0, "palisada": 2.0, "wieza": 4.0, "stajnie": 3.0,
    "skryptorium": 3.0,
}


def praca_potrzebna(klucz: str, poziom: int) -> float:
    if klucz == "chata":
        return KOSZT_CHATY_PRACA
    return PRACA.get(klucz, 3.0) * poziom


def plany(gracz: "Gracz") -> list[dict]:
    """Kolejka budów (kolejność zlecenia). Brakujące pole = świeża lista."""
    if not isinstance(getattr(gracz, "budowy", None), list):
        gracz.budowy = []
    return gracz.budowy


def plan_dla(gracz: "Gracz", klucz: str) -> dict | None:
    for p in plany(gracz):
        if p["klucz"] == klucz:
            return p
    return None


def nazwa_planu(plan: dict) -> str:
    if plan["klucz"] == "chata":
        return "Chata"
    info = BUDYNKI[plan["klucz"]]
    return f"{info['nazwa']} (poz. {plan['poziom']})"


def ikona_planu(plan: dict) -> str:
    return "🛖" if plan["klucz"] == "chata" else BUDYNKI[plan["klucz"]]["ikona"]


def postep_planu(plan: dict) -> float:
    """Ułamek ukończenia 0–1."""
    return max(0.0, min(1.0, plan["zrobiona"] / plan["wymagana"])) if plan["wymagana"] else 1.0


# ------------------------------------------------------------------ #
#  Ludzie                                                              #
# ------------------------------------------------------------------ #

def robotnicy(gracz: "Gracz") -> list[tuple[dict, float]]:
    """Kto buduje i z jaką mocą dzienną: (osadnik, moc). Chorzy odpoczywają."""
    from game.osada import mnoznik_pracy, osadnicy

    wynik = []
    for o in osadnicy(gracz):
        if o.get("chory", 0) > 0:
            continue
        waga = (1.0 if o["zajecie"] == "budowniczy"
                else WAGA_BEZCZYNNEGO if o["zajecie"] == "bezczynny" else WAGA_POZOSTALYCH)
        wynik.append((o, waga * mnoznik_pracy(gracz, o, "budowniczy")))
    return wynik


def moc_budowy(gracz: "Gracz") -> float:
    """Ile pracy osada wykonuje dziennie, gdyby miała co budować."""
    return sum(m for _, m in robotnicy(gracz))


def praca_bohatera(gracz: "Gracz") -> float:
    return PRACA_BOHATERA + 0.25 * max(0, atrybuty.modyfikator(gracz, "sila"))


# ------------------------------------------------------------------ #
#  Zlecanie i anulowanie                                               #
# ------------------------------------------------------------------ #

def nastepny_poziom(gracz: "Gracz", klucz: str) -> int | None:
    n = poziom_budynku(gracz, klucz) + 1
    return n if n <= BUDYNKI[klucz]["max"] else None


def powod_blokady(gracz: "Gracz", klucz: str) -> str | None:
    """Czemu nie da się teraz zlecić budowy (bez sprawdzania portfela) albo None."""
    if klucz == "chata":
        from game.osada import MAX_CHATY, liczba_chat
        razem = liczba_chat(gracz) + sum(1 for p in plany(gracz) if p["klucz"] == "chata")
        if razem >= MAX_CHATY:
            return f"maks. {MAX_CHATY} chat"
        if razem >= badania.limit_chat(gracz):
            projekt = badania.projekt_dla_chat(gracz)
            return "limit chat — poznaj: " + (badania.PROJEKTY[projekt]["nazwa"] if projekt else "—")
        return None
    nast = nastepny_poziom(gracz, klucz)
    if nast is None:
        return "najwyższy poziom"
    if plan_dla(gracz, klucz):
        return "w budowie"
    if nast > badania.maks_poziom(gracz, klucz):
        projekt = badania.projekt_dla(klucz, nast)
        return "nieznane — poznaj: " + (badania.PROJEKTY[projekt]["nazwa"] if projekt else "—")
    return None


def _koszt_planu(gracz: "Gracz", klucz: str) -> dict:
    if klucz == "chata":
        from game.osada import KOSZT_CHATY
        return dict(KOSZT_CHATY)
    return koszt_budowy(gracz, klucz)


def zlec(gracz: "Gracz", klucz: str) -> str:
    """Zakłada plan budowy (materiały schodzą od razu). Zwraca komunikat."""
    if klucz != "chata" and klucz not in BUDYNKI:
        return "  Nie ma takiej budowli."
    nazwa = "chatę" if klucz == "chata" else BUDYNKI[klucz]["nazwa"].lower()
    powod = powod_blokady(gracz, klucz)
    if powod == "w budowie":
        return f"  {nazwa.capitalize()} już stoi na rusztowaniach."
    if powod:
        return f"  Nie można teraz zlecić ({nazwa}): {powod}."
    if len(plany(gracz)) >= MAKS_PLANOW:
        return f"  Kolejka budów jest pełna ({MAKS_PLANOW}) — dokończ coś albo anuluj plan."
    koszt = _koszt_planu(gracz, klucz)
    if not _moze_zaplacic(gracz, koszt):
        return f"  Brakuje materiałów na {nazwa}.  Potrzeba: {_format_kosztu(koszt)}."
    _pobierz_koszt(gracz, koszt)
    poziom = 1 if klucz == "chata" else poziom_budynku(gracz, klucz) + 1
    ostatni = max((p.get("id", 0) for p in plany(gracz)), default=0)
    plan = {"id": ostatni + 1, "klucz": klucz, "poziom": poziom, "zrobiona": 0.0,
            "wymagana": praca_potrzebna(klucz, poziom), "koszt": koszt}
    plany(gracz).append(plan)
    dni = dni_do_konca(gracz, plan)
    eta = f"ok. {dni} dni" if dni else "ale nikt nie ma czasu budować — przydziel budowniczego [16]"
    return (f"  📐  Zlecono budowę: {ikona_planu(plan)} {nazwa_planu(plan)}.  "
            f"Praca: {plan['wymagana']:.0f} dni roboczych, {eta}.")


def anuluj(gracz: "Gracz", id_planu: int) -> str:
    """Zdejmuje plan. Zwrot materiałów: 100% bez prac, do 50% przy prawie skończonym."""
    plan = next((p for p in plany(gracz) if p["id"] == id_planu), None)
    if plan is None:
        return "  Nie ma takiego planu."
    ulamek = 1.0 - (1.0 - ZWROT_MIN) * postep_planu(plan)
    mag = _magazyn(gracz)
    zwrot = {}
    for k, ile in plan["koszt"].items():
        oddaj = int(ile * ulamek)
        zwrot[k] = oddaj
        if k == "zloto":
            gracz.zloto += oddaj
        else:
            mag[k] = mag.get(k, 0) + oddaj
    plany(gracz).remove(plan)
    return (f"  ✖  Anulowano: {nazwa_planu(plan)}.  Zwrot {round(ulamek * 100)}%: "
            f"{_format_kosztu({k: v for k, v in zwrot.items() if v})}.")


# ------------------------------------------------------------------ #
#  Czas                                                                #
# ------------------------------------------------------------------ #

def dni_do_konca(gracz: "Gracz", plan: dict) -> int | None:
    """Szacunek dni do ukończenia planu przy obecnej mocy albo None, gdy nikt nie buduje."""
    moc = moc_budowy(gracz)
    if moc <= 0:
        return None
    zostalo = {p["id"]: max(0.0, p["wymagana"] - p["zrobiona"]) for p in plany(gracz)}
    for dzien in range(1, 400):
        pula = moc
        for p in plany(gracz):
            if zostalo[p["id"]] <= 0:
                continue
            bierze = min(pula, LIMIT_PLANU, zostalo[p["id"]])
            zostalo[p["id"]] -= bierze
            pula -= bierze
        if zostalo[plan["id"]] <= 1e-9:
            return dzien
    return None


def _ukoncz(gracz: "Gracz", plan: dict) -> str:
    plany(gracz).remove(plan)
    if plan["klucz"] == "chata":
        gracz.chaty = int(getattr(gracz, "chaty", 0) or 0) + 1
        gracz.statystyki["zbudowane_chaty"] = gracz.chaty
        from game.osada import MAX_CHATY
        return f"  🛖  Chata stoi ({gracz.chaty}/{MAX_CHATY})! Ktoś może się tu wprowadzić."
    info = BUDYNKI[plan["klucz"]]
    ustaw_poziom(gracz, plan["klucz"], max(poziom_budynku(gracz, plan["klucz"]), plan["poziom"]))
    gracz.statystyki["ukonczone_budowy"] = gracz.statystyki.get("ukonczone_budowy", 0) + 1
    if plan["poziom"] == 1:
        return f"  {info['ikona']}  UKOŃCZONO: {info['nazwa']}!  ({info['opis']})"
    return f"  {info['ikona']}  UKOŃCZONO: {info['nazwa']} rozbudowana do poziomu {plan['poziom']}!"


def _z_questami(gracz: "Gracz", wiesci: list[str]) -> list[str]:
    """Ukończona budowa mogła domknąć zadanie — dopisz jego komunikaty."""
    if not wiesci:
        return wiesci
    from game.quests import sprawdz_questy
    return wiesci + sprawdz_questy(gracz)


def dzien_budowy(gracz: "Gracz") -> tuple[list[str], list[str]]:
    """Jeden dzień pracy na budowach. Zwraca (wieści do kroniki, wieści pilne)."""
    kronika: list[str] = []
    pilne: list[str] = []
    kolejka = plany(gracz)
    if not kolejka:
        return kronika, pilne
    ludzie = robotnicy(gracz)
    moc = sum(m for _, m in ludzie)
    pula = moc
    wykorzystano = 0.0
    for plan in list(kolejka):
        bierze = min(pula, LIMIT_PLANU, plan["wymagana"] - plan["zrobiona"])
        if bierze <= 0:
            continue
        plan["zrobiona"] += bierze
        pula -= bierze
        wykorzystano += bierze
        if plan["zrobiona"] >= plan["wymagana"] - 1e-9:
            pilne.append(_ukoncz(gracz, plan))
    if wykorzystano > 0:
        czesc = wykorzystano / moc
        for o, _ in ludzie:
            if o["zajecie"] == "budowniczy":
                kronika += kompetencje.cwicz(o, "budowniczy", czesc)
            elif o["zajecie"] == "bezczynny":
                kronika += kompetencje.cwicz(o, "budowniczy", 0.5 * czesc)
    return kronika, _z_questami(gracz, pilne)


def pomoz(gracz: "Gracz", id_planu: int) -> str:
    """Bohater spędza dzień przy budowie: dopisuje swoją pracę, a reszta osady pracuje jak zwykle."""
    plan = next((p for p in plany(gracz) if p["id"] == id_planu), None)
    if plan is None:
        return "  Nie ma takiego planu."
    if not getattr(gracz, "w_obozie", True):
        return "  Przy budowie możesz pracować tylko w obozie."
    dodane = min(praca_bohatera(gracz), plan["wymagana"] - plan["zrobiona"])
    plan["zrobiona"] += dodane
    gracz.statystyki["dni_budowy"] = gracz.statystyki.get("dni_budowy", 0) + 1
    tekst = [f"  🔨  Cały dzień przy budowie: {nazwa_planu(plan)} +{dodane:.1f} dnia pracy."]
    if plan["zrobiona"] >= plan["wymagana"] - 1e-9:
        tekst += _z_questami(gracz, [_ukoncz(gracz, plan)])
    from game.osada import dodaj_czas
    dodaj_czas(gracz, 1)
    return "\n".join(tekst)


def rusztowania(gracz: "Gracz") -> list[tuple[str, int, float]]:
    """Co stoi na rusztowaniach: (klucz, poziom, postęp 0–1) — dla widoku osady."""
    return [(p["klucz"], p["poziom"], postep_planu(p)) for p in plany(gracz)]


# ------------------------------------------------------------------ #
#  Menu                                                                #
# ------------------------------------------------------------------ #

def _pasek(czesc: float, szer: int = 12) -> str:
    n = max(0, min(szer, int(round(czesc * szer))))
    return "█" * n + "░" * (szer - n)


def _opis_robotnikow(gracz: "Gracz") -> str:
    ludzie = robotnicy(gracz)
    budowniczych = sum(1 for o, _ in ludzie if o["zajecie"] == "budowniczy")
    pomocnikow = sum(1 for o, _ in ludzie if o["zajecie"] == "bezczynny")
    reszta = len(ludzie) - budowniczych - pomocnikow
    return f"{budowniczych} budowniczych, {pomocnikow} bezczynnych, {reszta} po godzinach"


def _wybierz_plan(gracz: "Gracz", pytanie: str) -> dict | None:
    kolejka = plany(gracz)
    if not kolejka:
        print("  Nic się teraz nie buduje.")
        return None
    if len(kolejka) == 1:
        return kolejka[0]
    for i, p in enumerate(kolejka, 1):
        print(f"  [{i}] {ikona_planu(p)} {nazwa_planu(p)}")
    wybor = input(f"  {pytanie}: ").strip()
    if wybor.isdigit() and 1 <= int(wybor) <= len(kolejka):
        return kolejka[int(wybor) - 1]
    print("  Nieprawidłowy wybór.")
    return None


def _wypisz_naglowek(gracz: "Gracz") -> None:
    wyczysc()
    wyswietl_linie("═")
    print(f"  ROZBUDOWA OBOZU  —  {kalendarz.opis_daty(gracz)}")
    wyswietl_linie("═")
    print(f"\n  {linia_surowcow(gracz)}")
    print(f"  Złoto: {gracz.zloto}   Utrzymanie: {utrzymanie_dzienne(gracz)} zł/dz.   "
          f"Epoka {badania.era(gracz)}: {badania.ERY[badania.era(gracz)]}")
    print(f"  Moc budowy: {moc_budowy(gracz):.1f} pracy/dzień  ({_opis_robotnikow(gracz)})")
    kolejka = plany(gracz)
    print(f"\n  W BUDOWIE ({len(kolejka)}/{MAKS_PLANOW}):")
    if not kolejka:
        print("   — nic; wybierz budowlę poniżej.")
    for p in kolejka:
        dni = dni_do_konca(gracz, p)
        eta = f"~{dni} dni" if dni else "stoi — brak ludzi"
        print(f"   {ikona_planu(p)} {nazwa_planu(p):<24} [{_pasek(postep_planu(p))}] "
              f"{p['zrobiona']:.1f}/{p['wymagana']:.0f}  {eta}")


def _wypisz_oferte(gracz: "Gracz") -> tuple[list[str], list[str]]:
    """Wypisuje budowle do zlecenia; zwraca (klucze w kolejności numeracji, zamknięte)."""
    pozycje: list[str] = []
    zamkniete: list[str] = []
    print("\n  MOŻNA ZLECIĆ:")
    for klucz, info in BUDYNKI.items():
        powod = powod_blokady(gracz, klucz)
        if powod and powod.startswith("nieznane"):
            zamkniete.append(f"{info['nazwa']} ← {powod.split(': ', 1)[1]}")
            continue
        if powod in ("najwyższy poziom", "w budowie"):
            continue
        pozycje.append(klucz)
        nast = nastepny_poziom(gracz, klucz)
        koszt = koszt_budowy(gracz, klucz)
        stac = "✔ " if _moze_zaplacic(gracz, koszt) else ""
        poz = poziom_budynku(gracz, klucz)
        stan = f"poz. {poz} → {nast}" if poz else "nowa"
        utrz = f", utrzymanie +{info['utrzymanie']} zł/dz." if info["utrzymanie"] else ""
        print(f"  [{len(pozycje):>2}] {info['ikona']} {info['nazwa']} ({stan}) — {info['opis']}")
        print(f"        {stac}{_format_kosztu(koszt)}  ·  praca {praca_potrzebna(klucz, nast):.0f} dni{utrz}")
    if not pozycje:
        print("   — wszystko, co znacie, jest już zbudowane albo w budowie.")
    return pozycje, zamkniete


def menu_budowy(gracz: "Gracz") -> None:
    """Rozbudowa obozu: rusztowania w toku, lista budowli do zlecenia, zamknięte do odkrycia."""
    from game.osada import KOSZT_CHATY, MAX_CHATY, liczba_chat

    while True:
        _wypisz_naglowek(gracz)
        pozycje, zamkniete = _wypisz_oferte(gracz)
        chata_powod = powod_blokady(gracz, "chata")
        nr_chaty = len(pozycje) + 1
        if chata_powod is None:
            stac = "✔ " if _moze_zaplacic(gracz, KOSZT_CHATY) else ""
            print(f"  [{nr_chaty:>2}] 🛖 Chata osadnika ({liczba_chat(gracz)}/{MAX_CHATY}) — dom dla jednej osoby")
            print(f"        {stac}{_format_kosztu(KOSZT_CHATY)}  ·  praca {KOSZT_CHATY_PRACA:.0f} dni")
        else:
            print(f"   🛖 Chata osadnika ({liczba_chat(gracz)}/{MAX_CHATY}): {chata_powod}")
        if zamkniete:
            print("\n  🔒 NIEZNANE (badania w skryptorium [23]):")
            for linia in zamkniete:
                print(f"     {linia}")
        print("\n  [P] 🔨 Pomagaj przy budowie (dzień)    [A] ✖ Anuluj plan    [0] ↩ Wróć\n")
        wybor = input("  Twój wybór: ").strip().lower()
        if wybor == "0":
            return
        if wybor == "p":
            plan = _wybierz_plan(gracz, "Przy której budowie")
            if plan:
                print(pomoz(gracz, plan["id"]))
        elif wybor == "a":
            plan = _wybierz_plan(gracz, "Który plan anulować")
            if plan:
                print(anuluj(gracz, plan["id"]))
        elif wybor.isdigit() and 1 <= int(wybor) <= len(pozycje):
            print(zlec(gracz, pozycje[int(wybor) - 1]))
        elif wybor.isdigit() and int(wybor) == nr_chaty and chata_powod is None:
            print(zlec(gracz, "chata"))
        else:
            print("  Nieprawidłowy wybór.")
        nacisnij_enter()
