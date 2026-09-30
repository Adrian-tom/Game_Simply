"""Rozmowy w stylu Disco Elysium: węzły, głosy umiejętności, białe i czerwone testy.

* **Głos** (bierny test) — umiejętność wtrąca się sama, jeśli premia + 10 ≥ ST.
  Może też odsłonić nową opcję (efekt ``("wiem", klucz)``).
* **Biały test** ⚪ — po porażce zamknięty, dopóki premia do umiejętności nie wzrośnie
  (awans, atrybut, myśl, talent). **Czerwony** 🔴 — jedna szansa na zawsze.
* Przy teście widać trudność i szansę w procentach — decyzja należy do gracza.
* Epifania (talent) pozwala raz na rozmowę powtórzyć nieudany test,
  Napar jasności daje +2 do wszystkich testów w jednej rozmowie.

Rozmowa to słownik węzłów; opcje prowadzą do kolejnych węzłów albo kończą
rozmowę (``cel: None``), a efekty zmieniają świat (karma, złoto, myśli,
przepisy, morale osady, rekrutacja…). ``prowadz`` zwraca wynik rozmowy
(np. 'odwolany' dla herszta), jeśli któryś efekt go ustawił.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import talenty
from game.atrybuty import SKILLE, premia_skilla, rzuc_test
from game.utils import nacisnij_enter, wyczysc, wyswietl_linie

if TYPE_CHECKING:
    from game.player import Gracz

_POZIOMY_TRUDNOSCI = ((8, "Trywialny"), (10, "Łatwy"), (12, "Średni"), (14, "Wymagający"),
                      (16, "Trudny"), (18, "Heroiczny"), (99, "Legendarny"))


def nazwa_trudnosci(st: int) -> str:
    for prog, nazwa in _POZIOMY_TRUDNOSCI:
        if st <= prog:
            return nazwa
    return "Legendarny"


def szansa(premia: int, st: int) -> float:
    """P(k20 + premia ≥ ST), nat 20 zawsze sukces, nat 1 zawsze porażka."""
    udane = sum(1 for r in range(1, 21) if r == 20 or (r != 1 and r + premia >= st))
    return udane / 20


class _Sesja:
    def __init__(self, gracz: "Gracz", rozmowa_id: str, kontekst: dict):
        self.gracz = gracz
        self.id = rozmowa_id
        self.kontekst = kontekst
        self.premia = 1 if talenty.ma(gracz, "drugie_podejscie") else 0
        self.epifania = talenty.ma(gracz, "epifania")
        self.wiem: set[str] = set()
        self.wynik: str | None = None
        self.uzyte: set[str] = set()

    def premia_testu(self, skill: str) -> int:
        return premia_skilla(self.gracz, skill) + self.premia


def _tekst(szablon: str, kontekst: dict) -> str:
    try:
        return szablon.format(**kontekst)
    except (KeyError, IndexError):
        return szablon


def _warunek_spelniony(s: _Sesja, warunek) -> bool:
    if not warunek:
        return True
    typ = warunek[0]
    g = s.gracz
    if typ == "wiem":
        return warunek[1] in s.wiem
    if typ == "zloto":
        return g.zloto >= _liczba(s, warunek[1])
    if typ == "surowiec":
        return g.surowce.get(warunek[1], 0) >= warunek[2]
    if typ == "skladnik":
        return (g.skladniki or {}).get(warunek[1], 0) >= warunek[2]
    if typ == "flaga":
        return bool(g.flagi.get(warunek[1]))
    if typ == "brak_flagi":
        return not g.flagi.get(warunek[1])
    if typ == "osadnik":
        return s.kontekst.get("osadnik") is not None
    return True


def _liczba(s: _Sesja, wartosc) -> int:
    """Liczba wprost albo nazwa z kontekstu ('okup', '-okup')."""
    if isinstance(wartosc, str):
        if wartosc.startswith("-"):
            return -int(s.kontekst.get(wartosc[1:], 0))
        return int(s.kontekst.get(wartosc, 0))
    return int(wartosc)


def _efekty(s: _Sesja, efekty: list) -> None:
    from game import mysli, rzemioslo
    from game.osada import osadnicy, zatrudnij_osadnika, zmien_morale

    g = s.gracz
    for e in efekty or []:
        typ = e[0]
        if typ == "karma":
            g.karma = int(getattr(g, "karma", 0) or 0) + e[1]
            print(f"  ✨  Karma {e[1]:+d}.")
        elif typ == "zloto":
            ile = _liczba(s, e[1])
            g.zloto = max(0, g.zloto + ile)
            print(f"  💰  Złoto {ile:+d}.")
        elif typ == "surowiec":
            g.surowce[e[1]] = max(0, g.surowce.get(e[1], 0) + e[2])
            print(f"  📦  {e[1]} {e[2]:+d}.")
        elif typ == "skladnik":
            if e[2] < 0:
                rzemioslo.skladniki(g)[e[1]] = max(0, rzemioslo.skladniki(g).get(e[1], 0) + e[2])
            else:
                print(rzemioslo.dodaj_skladnik(g, e[1], e[2]))
        elif typ == "przedmiot":
            rzemioslo.przedmioty(g)[e[1]] = rzemioslo.przedmioty(g).get(e[1], 0) + e[2]
            print(f"  🎁  {rzemioslo.NAZWY_PRZEDMIOTOW.get(e[1], e[1])} +{e[2]}.")
        elif typ == "przepis":
            klucz = e[1] if e[1] != "losowy" else next(
                (k for k in random.sample(list(rzemioslo.PRZEPISY), len(rzemioslo.PRZEPISY))
                 if k not in rzemioslo.znane(g)), None)
            if klucz:
                msg = rzemioslo.naucz(g, klucz)
                if msg:
                    print(msg)
        elif typ == "mysl":
            msg = mysli.odkryj(g, e[1])
            if msg:
                print(msg)
        elif typ == "morale":
            zmien_morale(g, e[1])
            print(f"  {'😊' if e[1] > 0 else '😠'}  Morale osady {e[1]:+d}.")
        elif typ == "flaga":
            g.flagi[e[1]] = e[2] if len(e) > 2 else True
        elif typ == "wiem":
            s.wiem.add(e[1])
        elif typ == "wynik":
            s.wynik = e[1]
        elif typ == "rekrut":
            from game.rekruci import proponuj_rekrutacje_npc
            proponuj_rekrutacje_npc(g, e[1])
        elif typ == "osadnik":
            print(zatrudnij_osadnika(g, e[1], darmo=True))
        elif typ == "wylecz_osadnika" and s.kontekst.get("osadnik"):
            s.kontekst["osadnik"]["chory"] = 0
            s.kontekst["osadnik"]["morale"] = min(100, s.kontekst["osadnik"]["morale"] + 15)
            print(f"  💚  {s.kontekst['osadnik']['imie']} wraca do sił.")
        elif typ == "usun_osadnika" and s.kontekst.get("osadnik") in osadnicy(g):
            osadnicy(g).remove(s.kontekst["osadnik"])
            print(f"  🚪  {s.kontekst['osadnik']['imie']} opuszcza osadę.")
        elif typ == "morale_osadnika" and s.kontekst.get("osadnik"):
            o = s.kontekst["osadnik"]
            o["morale"] = max(0, min(100, o["morale"] + e[1]))


def _klucz_testu(s: _Sesja, wezel_id: str, nr: int) -> str:
    return f"{s.id}:{wezel_id}:{nr}"


def _stan_testu(s: _Sesja, opcja: dict, klucz: str) -> str:
    """'otwarty', 'zamkniety' (biały — czeka na wzrost umiejętności), 'spalony' (czerwony)."""
    zapis = (s.gracz.testy_rozmow or {}).get(klucz)
    if zapis is None:
        return "otwarty"
    if not opcja.get("bialy", True):
        return "spalony"
    return "otwarty" if s.premia_testu(opcja["test"][0]) > zapis else "zamkniety"


def _pokaz_glosy(s: _Sesja, wezel: dict) -> None:
    bonus = 2 if talenty.ma(s.gracz, "wewnetrzny_glos") else 0
    for glos in wezel.get("glosy", []):
        skill, st, tekst = glos[0], glos[1], glos[2]
        if s.premia_testu(skill) + 10 + bonus >= st:
            nazwa = SKILLE[skill]["nazwa"].upper()
            print(f"  {SKILLE[skill].get('ikona', '🎲')}  {nazwa} [{nazwa_trudnosci(st)}: sukces] — {_tekst(tekst, s.kontekst)}")
            if len(glos) > 3:
                _efekty(s, glos[3])


def _rzut(s: _Sesja, skill: str, st: int) -> bool:
    """Test z wypisaniem w stylu DE. Epifania może raz dać drugą szansę."""
    wynik = rzuc_test(s.gracz, skill, st)
    premia = s.premia_testu(skill)
    suma = wynik.rzut + premia
    sukces = wynik.krytyczny or (not wynik.wpadka and suma >= st)
    nazwa = SKILLE[skill]["nazwa"].upper()
    status = "KRYTYCZNY SUKCES" if wynik.krytyczny else "KRYTYCZNA PORAŻKA" if wynik.wpadka else (
        "SUKCES" if sukces else "PORAŻKA")
    print(f"\n  🎲  {nazwa} [{nazwa_trudnosci(st)}: {status}]   k20 {wynik.rzut} {premia:+d} = {suma} vs {st}")
    if not sukces and s.epifania:
        s.epifania = False
        print("  💡  EPIFANIA — nagle widzisz to z innej strony. Rzucasz jeszcze raz.")
        return _rzut(s, skill, st)
    return sukces


def prowadz(gracz: "Gracz", rozmowa_id: str, kontekst: dict | None = None) -> str | None:
    """Prowadzi rozmowę do końca. Zwraca wynik ustawiony efektem ('wynik', …) albo None."""
    if getattr(gracz, "testy_rozmow", None) is None:
        gracz.testy_rozmow = {}
    if getattr(gracz, "flagi", None) is None:
        gracz.flagi = {}
    rozmowa = ROZMOWY[rozmowa_id]
    s = _Sesja(gracz, rozmowa_id, dict(kontekst or {}))
    wezel_id = rozmowa["start"]
    naparu = (gracz.przedmioty or {}).get("napar_jasnosci", 0)
    from game import ekran
    try:
        return _petla_rozmowy(gracz, rozmowa, s, wezel_id, naparu, ekran)
    finally:
        ekran.ustaw_rozmowe(None)


def _petla_rozmowy(gracz, rozmowa, s, wezel_id, naparu, ekran):
    while wezel_id is not None:
        wezel = rozmowa["wezly"][wezel_id]
        ekran.ustaw_rozmowe(_tekst(wezel.get("mowi", ""), s.kontekst).split(" — ")[0])
        wyczysc()
        wyswietl_linie()
        print(f"  {_tekst(wezel.get('mowi', ''), s.kontekst)}")
        wyswietl_linie()
        print(f"  „{_tekst(wezel['tekst'], s.kontekst)}”\n")
        if wezel_id not in s.uzyte:
            s.uzyte.add(wezel_id)
            _efekty(s, wezel.get("efekty", []))
        _pokaz_glosy(s, wezel)
        print()
        widoczne: list[tuple[int, dict]] = []
        for nr, opcja in enumerate(wezel["opcje"]):
            if not _warunek_spelniony(s, opcja.get("warunek")):
                continue
            if opcja.get("raz") and f"{wezel_id}:{nr}" in s.uzyte:
                continue
            widoczne.append((nr, opcja))
        for i, (nr, opcja) in enumerate(widoczne, 1):
            tekst = _tekst(opcja["tekst"], s.kontekst)
            if "test" in opcja:
                skill, st = opcja["test"]
                st = _liczba(s, st)
                stan = _stan_testu(s, opcja, _klucz_testu(s, wezel_id, nr))
                kolor = "⚪" if opcja.get("bialy", True) else "🔴"
                nazwa = SKILLE[skill]["nazwa"]
                if stan == "otwarty":
                    proc = int(szansa(s.premia_testu(skill), st) * 100)
                    print(f"  [{i}] {kolor} [{nazwa} · {nazwa_trudnosci(st)} {st} · {proc}%] {tekst}")
                elif stan == "zamkniety":
                    print(f"  [{i}] {kolor} [{nazwa}: zamknięty — wróć, gdy się rozwiniesz] {tekst}")
                else:
                    print(f"  [{i}] 🔴 [{nazwa}: szansa przepadła] {tekst}")
            else:
                print(f"  [{i}] {tekst}")
        if naparu:
            print("  [N] 🍵 Wypij napar jasności (+2 do testów w tej rozmowie)")
        wybor = input("\n  Twój wybór: ").strip().lower()
        if wybor == "n" and naparu:
            gracz.przedmioty["napar_jasnosci"] -= 1
            naparu = 0
            s.premia += 2
            print("  🍵  Myśli się wyostrzają. (+2 do testów)")
            nacisnij_enter()
            continue
        if not (wybor.isdigit() and 1 <= int(wybor) <= len(widoczne)):
            continue
        nr, opcja = widoczne[int(wybor) - 1]
        s.uzyte.add(f"{wezel_id}:{nr}")
        _efekty(s, opcja.get("efekty", []))
        if "test" in opcja:
            skill, st = opcja["test"]
            st = _liczba(s, st)
            klucz = _klucz_testu(s, wezel_id, nr)
            if _stan_testu(s, opcja, klucz) != "otwarty":
                print("  Ta droga jest teraz zamknięta.")
                nacisnij_enter()
                continue
            if _rzut(s, skill, st):
                gracz.testy_rozmow.pop(klucz, None)
                _efekty(s, opcja.get("efekty_sukces", []))
                wezel_id = opcja.get("sukces")
            else:
                gracz.testy_rozmow[klucz] = s.premia_testu(skill)
                _efekty(s, opcja.get("efekty_porazka", []))
                wezel_id = opcja.get("porazka")
            nacisnij_enter()
        else:
            wezel_id = opcja.get("cel")
    return s.wynik


# ------------------------------------------------------------------ #
#  Treść                                                               #
# ------------------------------------------------------------------ #

GRIMBOLD = "GRIMBOLD, KOWAL"

ROZMOWY: dict[str, dict] = {}

ROZMOWY["grimbold"] = {"start": "powitanie", "wezly": {
    "powitanie": {
        "mowi": GRIMBOLD,
        "tekst": "Stuk, stuk. Nowy klient? Witam. Jeśli chcesz miecz, powiedz po co. Kłamać i tak usłyszę w stali.",
        "glosy": [("spostrzegawczosc", 12, "Kowadło milczy. Od dawna — na klepisku nie ma świeżych opiłków.")],
        "opcje": [
            {"tekst": "O broni.", "cel": "bron"},
            {"tekst": "O rzemiośle.", "cel": "rzemioslo"},
            {"tekst": "Czemu kowadło milczy dłużej niż trzeba?", "cel": "watek1"},
            {"tekst": "Poproś o radę kowalską.", "test": ("perswazja", 12), "sukces": "rada_sukces", "porazka": "rada_porazka"},
            {"tekst": "Przyniosłem łuskę smoka.", "warunek": ("skladnik", "luska", 1), "cel": "luska"},
            {"tekst": "Odejść.", "cel": None},
        ],
    },
    "bron": {
        "mowi": GRIMBOLD,
        "tekst": "Miecz to nie żelazo. To decyzja, którą ktoś podejmie, gdy ciebie już nie będzie przy kowadle. "
                 "Topór nie udaje. Łuk udaje, że przemoc jest z daleka. Oba kłamią inaczej.",
        "opcje": [{"tekst": "A sztylet?", "cel": "sztylet"}, {"tekst": "Wróć.", "cel": "powitanie"}],
    },
    "sztylet": {
        "mowi": GRIMBOLD,
        "tekst": "Sztylet to broń ludzi, co myślą. I ludzi, co nie chcą, żeby myślano o nich.",
        "opcje": [{"tekst": "Wróć.", "cel": "powitanie"}],
    },
    "rzemioslo": {
        "mowi": GRIMBOLD,
        "tekst": "Trzydzieści lat. Mistrz mawiał: lepsza stal, lepszy wojownik. Nie powiedział, co z gorszym człowiekiem. "
                 "Wiele mieczy wróciło. Żaden nie wrócił czysty. To nie wada kucia. To wada świata.",
        "opcje": [
            {"tekst": "Nauczysz mnie czegoś o ogniu?", "test": ("perswazja", 13), "sukces": "olej", "porazka": "rada_porazka"},
            {"tekst": "Wróć.", "cel": "powitanie"},
        ],
    },
    "olej": {
        "mowi": GRIMBOLD,
        "tekst": "Drewno, smoła z rudą, i nie żałuj ognia. Ostrze w oleju pali to, co się goi — troll to poczuje.",
        "efekty": [("przepis", "olej_ognisty")],
        "opcje": [{"tekst": "Dziękuję.", "cel": "powitanie"}],
    },
    "watek1": {
        "mowi": GRIMBOLD,
        "tekst": "Wykowałem miecz dla rycerza z czystym herbem. Wykonał nim wieś. Dzieci, studnię, psa. Stal to pamięta. "
                 "W nocy słyszę hart, którego nie dawałem. Nie jestem magiem. Jestem winny, bo umiałem za dobrze.",
        "glosy": [("spostrzegawczosc", 14, "Nie kłamie. Ale nie mówi wszystkiego — wciąż kuje. Komu?")],
        "efekty": [("mysl", "stal_pamieta")],
        "opcje": [
            {"tekst": "Da się to odkuć?", "cel": "watek2"},
            {"tekst": "Trzydzieści lat przy kowadle, a winisz siebie za czyjąś rękę?", "test": ("perswazja", 14),
             "sukces": "wina_sukces", "porazka": "wina_porazka"},
            {"tekst": "Zajrzeć pod płótno za kowadłem.", "test": ("spostrzegawczosc", 16), "bialy": False,
             "sukces": "plotno_sukces", "porazka": "plotno_porazka"},
            {"tekst": "Wróć.", "cel": "powitanie"},
        ],
    },
    "wina_sukces": {
        "mowi": GRIMBOLD,
        "tekst": "…Może. Może ręka była jego. Ale kto mu dał czym? Siadaj. Rzadko ktoś mówi do mnie, jakby wiedział, o czym mówi.",
        "efekty": [("karma", 1)],
        "opcje": [{"tekst": "Da się to odkuć?", "cel": "watek2"}],
    },
    "wina_porazka": {
        "mowi": GRIMBOLD,
        "tekst": "Łatwo mówić komuś, kto nie stał przy ogniu. Idź. Albo zostań i milcz.",
        "opcje": [{"tekst": "Milczeć.", "cel": "watek2"}, {"tekst": "Odejść.", "cel": None}],
    },
    "plotno_sukces": {
        "mowi": "TY",
        "tekst": "Pod płótnem leży ostrze. Nieostrzone, bez rękojeści — ale hart ma taki, jakiego nie daje się zwykłej stali. Ktoś na nie czeka.",
        "glosy": [("spostrzegawczosc", 10, "To nie jest miecz na sprzedaż. To jest miecz, którego się boi.")],
        "opcje": [{"tekst": "— To dla kogo?", "cel": "plotno_konfrontacja"}],
    },
    "plotno_porazka": {
        "mowi": "TY",
        "tekst": "Sięgasz po róg płótna. Grimbold nie rusza się — ale młot w jego dłoni przestaje się kołysać. Cofasz rękę.",
        "opcje": [{"tekst": "Da się to odkuć?", "cel": "watek2"}],
    },
    "plotno_konfrontacja": {
        "mowi": GRIMBOLD,
        "tekst": "Dla nikogo. Dlatego leży. Wykułem je, zanim usłyszałem szept. Nie umiem go zniszczyć i nie umiem go oddać. "
                 "Rozumiesz teraz, o co pytasz?",
        "opcje": [{"tekst": "Da się to odkuć?", "cel": "watek2"}],
    },
    "watek2": {
        "mowi": GRIMBOLD,
        "tekst": "Żeby przekuć klątwę, trzeba łuski, która widziała smoczy ogień. Nie metafory — ognia, co nie pyta o herby. "
                 "Jeśli doniesiesz, może przestanie szeptać.",
        "opcje": [
            {"tekst": "Chodź do mojej osady. Kuźnia czeka.", "cel": "watek3"},
            {"tekst": "Wrócę z łuską.", "cel": None},
        ],
    },
    "watek3": {
        "mowi": GRIMBOLD,
        "tekst": "Chcę kuć dla kogoś, kto nie każe mi zabijać niewinnych. Twoja osada. Ludzie, co noszą motyki, nie wyroki. "
                 "Wezmę za to majątek albo dam się złamać słowem, jakiego nie słyszałem od mistrza.",
        "opcje": [
            {"tekst": "Rozmawiajmy o warunkach.", "efekty": [("rekrut", "grimbold")], "cel": None},
            {"tekst": "Jeszcze nie.", "cel": None},
        ],
    },
    "luska": {
        "mowi": GRIMBOLD,
        "tekst": "…Ciepła. Wciąż ciepła. Daj. Wrzucę ją w hart i posłucham, co powie stal. Twoja broń też skorzysta.",
        "efekty": [("skladnik", "luska", -1), ("karma", 1), ("flaga", "grimbold_luska")],
        "opcje": [{"tekst": "Kuj.", "efekty": [("zloto", 0)], "cel": "luska_koniec"}],
    },
    "luska_koniec": {
        "mowi": GRIMBOLD,
        "tekst": "Cisza. Pierwszy raz od lat — cisza. Weź to. I nie rób ze stali alibi.",
        "efekty": [("przedmiot", "olej_ognisty", 2), ("przepis", "olej_ognisty"), ("zloto", 40)],
        "opcje": [{"tekst": "Odejść.", "cel": None}],
    },
    "rada_sukces": {
        "mowi": GRIMBOLD,
        "tekst": "Lubię ludzi, co słuchają. Weź zapas na ostrzenie. I nie rób ze stali alibi.",
        "efekty": [("zloto", 12)],
        "opcje": [{"tekst": "Wróć.", "cel": "powitanie"}],
    },
    "rada_porazka": {
        "mowi": GRIMBOLD,
        "tekst": "Nie mam czasu na gadki. Albo kujesz, albo wychodzisz.",
        "opcje": [{"tekst": "Wróć.", "cel": "powitanie"}],
    },
}}

ROZMOWY["herszt"] = {"start": "brama", "wezly": {
    "brama": {
        "mowi": "HERSZT — {banda}",
        "tekst": "Ładne chaty. Szkoda by było, gdyby spłonęły. {okup} złota i odjeżdżamy. Albo zostajemy — na dłużej.",
        "glosy": [
            ("spostrzegawczosc", 12, "Lewy bok herszta krwawi przez opatrunek. Nie ma sił na długie oblężenie.", [("wiem", "rana")]),
            ("przetrwanie", 13, "Ich konie są wychudzone. Ta banda też głoduje — przyszła po jedzenie, nie po krew.", [("wiem", "glod")]),
            ("zastraszanie", 14, "Patrzy na twoją palisadę dłużej, niż powinien. Liczy.", [("wiem", "liczy")]),
        ],
        "opcje": [
            {"tekst": "Zapłać okup ({okup} zł).", "warunek": ("zloto", "okup"),
             "efekty": [("zloto", "-okup"), ("morale", -4), ("wynik", "odwolany")], "cel": None},
            {"tekst": "Ta osada nie jest warta krwi twoich ludzi.", "test": ("perswazja", "st_perswazji"),
             "sukces": "przekonany", "porazka": "wsciekly"},
            {"tekst": "Twoja rana nie wytrzyma oblężenia. Wiesz o tym.", "warunek": ("wiem", "rana"),
             "test": ("perswazja", 10), "sukces": "przekonany", "porazka": "wsciekly"},
            {"tekst": "Dam wam 15 racji żywności. Odejdźcie w pokoju.", "warunek": ("wiem", "glod"),
             "efekty": [("surowiec", "zywnosc", -15), ("karma", 1), ("mysl", "cena_krwi"), ("wynik", "odwolany")],
             "cel": "odjazd"},
            {"tekst": "Spójrz na palisadę. Policz jeszcze raz.", "test": ("zastraszanie", "st_zastraszania"), "bialy": False,
             "sukces": "przestraszony", "porazka": "wsciekly"},
            {"tekst": "Królewski oddział jest pół dnia drogi stąd.", "test": ("oszustwo", 15), "bialy": False,
             "sukces": "przestraszony", "porazka": "wsciekly"},
            {"tekst": "Nie dostaniesz nic. Do broni!", "efekty": [("wynik", "walka")], "cel": None},
        ],
    },
    "przekonany": {
        "mowi": "HERSZT — {banda}",
        "tekst": "…Może i racja. Połowa moich i tak nie chce ginąć za garnki. Kto chce, zostaje. Reszta — do koni.",
        "efekty": [("mysl", "cena_krwi"), ("wynik", "oslabiony")],
        "glosy": [("perswazja", 12, "Nie przekonałeś go. Przekonałeś jego ludzi. On został z tymi, którzy chcą krwi.")],
        "opcje": [{"tekst": "Niech tak będzie.", "cel": None}],
    },
    "przestraszony": {
        "mowi": "HERSZT — {banda}",
        "tekst": "Tfu. Nie warto. Zbierać się! Jeszcze się spotkamy, osadniku.",
        "efekty": [("wynik", "odwolany"), ("morale", 5)],
        "opcje": [{"tekst": "Patrzeć, jak odjeżdżają.", "cel": None}],
    },
    "odjazd": {
        "mowi": "HERSZT — {banda}",
        "tekst": "Nie myślałem, że ktoś jeszcze dzieli się chlebem z takimi jak my. …Dziękuję. Nie wrócimy.",
        "opcje": [{"tekst": "Idźcie.", "cel": None}],
    },
    "wsciekly": {
        "mowi": "HERSZT — {banda}",
        "tekst": "Dość gadania! Chłopcy — ogień na dachy!",
        "efekty": [("wynik", "walka")],
        "opcje": [{"tekst": "Do broni!", "cel": None}],
    },
}}

# --- sprawy osady: krótkie rozmowy z decyzjami, wywoływane przez dzienny cykl ---

ROZMOWY["klotnia_o_racje"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "PLAC PRZY SPICHLERZU",
        "tekst": "{imie} i {imie2} skaczą sobie do oczu. — Ona dostaje więcej, bo stoi bliżej kotła! — A on śpi do południa!",
        "glosy": [("spostrzegawczosc", 12, "{imie2} ma świeże otarcia na dłoniach. Naprawdę pracuje najciężej.", [("wiem", "otarcia")])],
        "opcje": [
            {"tekst": "Od dziś wszyscy dostają równo. Koniec dyskusji.", "efekty": [("morale", 3)], "cel": None},
            {"tekst": "Kto pracuje ciężej, je więcej. Tak jest sprawiedliwie.", "test": ("perswazja", 12),
             "sukces": "praca_ok", "porazka": "praca_zle"},
            {"tekst": "Spójrz na jej dłonie, {imie}. Kto tu śpi do południa?", "warunek": ("wiem", "otarcia"),
             "efekty": [("morale", 6), ("mysl", "palenisko_krolestwem")], "cel": "otarcia"},
            {"tekst": "Obydwoje bez kolacji. Następnym razem — pręgierz.", "test": ("zastraszanie", 11),
             "sukces": "surowo", "porazka": "praca_zle"},
        ],
    },
    "praca_ok": {"mowi": "PLAC", "tekst": "Kilka głów kiwa z uznaniem. {imie} burczy, ale bierze łopatę.",
                 "efekty": [("morale", 5)], "opcje": [{"tekst": "Wracajcie do pracy.", "cel": None}]},
    "praca_zle": {"mowi": "PLAC", "tekst": "Szmer niezadowolenia. Ktoś spluwa pod nogi. Będzie o tym głośno przy ognisku.",
                  "efekty": [("morale", -5)], "opcje": [{"tekst": "Odejść.", "cel": None}]},
    "surowo": {"mowi": "PLAC", "tekst": "Cisza. Nikt nie patrzy ci w oczy. Kłótni już nie będzie — ale śmiechu też nie.",
               "efekty": [("morale", -2), ("mysl", "twarda_reka")], "opcje": [{"tekst": "Odejść.", "cel": None}]},
    "otarcia": {"mowi": "PLAC", "tekst": "{imie} czerwienieje. — …Przepraszam. — Ktoś klaszcze. Ludzie widzą, że patrzysz uważnie.",
                "opcje": [{"tekst": "Wracajcie do pracy.", "cel": None}]},
}}

ROZMOWY["chory_osadnik"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "CHATA — {imie}",
        "tekst": "{imie} leży pod kocem i drży. — To tylko gorączka… przejdzie… — mówi, ale głos się łamie.",
        "glosy": [("przetrwanie", 12, "Sine usta, skurcze. To nie gorączka — to woda. Studnia przy bagnach.", [("wiem", "studnia")])],
        "opcje": [
            {"tekst": "Daj mu 3 zioła na wywar.", "warunek": ("surowiec", "ziola", 3),
             "efekty": [("surowiec", "ziola", -3), ("wylecz_osadnika",), ("karma", 1), ("morale", 3)], "cel": None},
            {"tekst": "Studnia przy bagnach! Zasypać ją, kopać nową.", "warunek": ("wiem", "studnia"),
             "efekty": [("surowiec", "kamien", -3), ("wylecz_osadnika",), ("morale", 7), ("flaga", "czysta_studnia")], "cel": "studnia"},
            {"tekst": "Przetrzymaj to. Jutro do pracy.", "test": ("perswazja", 13),
             "sukces": "twardziel", "porazka": "zle"},
            {"tekst": "Nie mam na to czasu.", "efekty": [("morale", -4), ("morale_osadnika", -20)], "cel": None},
        ],
    },
    "studnia": {"mowi": "OSADA", "tekst": "Kopiecie nową studnię. Po dwóch dniach gorączki ustępują. Ludzie mówią, że wiesz, co robisz.",
                "opcje": [{"tekst": "Dobrze.", "cel": None}]},
    "twardziel": {"mowi": "CHATA — {imie}", "tekst": "— Masz rację. Nie dam się. — Wstaje chwiejnie, ale wstaje.",
                  "efekty": [("wylecz_osadnika",)], "opcje": [{"tekst": "Odejść.", "cel": None}]},
    "zle": {"mowi": "CHATA — {imie}", "tekst": "Patrzy na ciebie tak, jakbyś go już pochował.",
            "efekty": [("morale_osadnika", -25), ("morale", -3)], "opcje": [{"tekst": "Odejść.", "cel": None}]},
}}

ROZMOWY["zlodziej"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "SPICHLERZ",
        "tekst": "Strażnicy przyprowadzają {imie} — z kieszeniami pełnymi suszonego mięsa. Ludzie czekają na wyrok.",
        "glosy": [("spostrzegawczosc", 11, "Dłonie chude jak patyki. Nie kradł dla siebie.", [("wiem", "rodzina")])],
        "opcje": [
            {"tekst": "Czemu kradłeś?", "warunek": ("wiem", "rodzina"), "cel": "rodzina"},
            {"tekst": "Wynoś się z osady.", "efekty": [("usun_osadnika",), ("morale", -2)], "cel": None},
            {"tekst": "Chłosta na placu. Niech wszyscy widzą.", "efekty": [("karma", -1), ("morale", 2), ("mysl", "twarda_reka"),
                                                                           ("morale_osadnika", -40)], "cel": None},
            {"tekst": "Odpracujesz to. Dwa razy tyle, ile wziąłeś.", "test": ("perswazja", 12),
             "sukces": "odpracuje", "porazka": "znowu"},
        ],
    },
    "rodzina": {
        "mowi": "{imie}",
        "tekst": "— Siostra… w Brzeziu. Z dziećmi. Nie mieli nic od jesieni. Wiem, że to kradzież. Zrobiłbym to jeszcze raz.",
        "opcje": [
            {"tekst": "Weź 5 racji. Zanieś im. Oficjalnie.", "warunek": ("surowiec", "zywnosc", 5),
             "efekty": [("surowiec", "zywnosc", -5), ("karma", 2), ("morale", 5), ("morale_osadnika", 40),
                        ("mysl", "palenisko_krolestwem")], "cel": None},
            {"tekst": "Rozumiem. Ale prawo jest prawem — odpracujesz.", "efekty": [("morale", 2)], "cel": None},
        ],
    },
    "odpracuje": {"mowi": "{imie}", "tekst": "— Dziękuję. Nie zawiodę. — I rzeczywiście, od świtu pierwszy przy pracy.",
                  "efekty": [("karma", 1), ("morale_osadnika", 20)], "opcje": [{"tekst": "Odejść.", "cel": None}]},
    "znowu": {"mowi": "SPICHLERZ", "tekst": "Kilka dni później znika kolejny worek. Ktoś się z ciebie śmieje.",
              "efekty": [("surowiec", "zywnosc", -8), ("morale", -3)], "opcje": [{"tekst": "…", "cel": None}]},
}}

ROZMOWY["przybysz"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "BRAMA OSADY",
        "tekst": "Obcy z tobołkiem. — Słyszałem, że tu się da żyć. Umiem ciąć drewno, strzelać, liczyć. Przyjmiecie?",
        "glosy": [("spostrzegawczosc", 13, "Buty za dobre jak na włóczęgę. I pierścień na sznurku pod koszulą.", [("wiem", "szpieg")]),
                  ("perswazja", 12, "Mówi prawdę o tym, co umie. Czy mówi o tym, skąd idzie — tego nie wiesz.")],
        "opcje": [
            {"tekst": "Witaj w osadzie.", "warunek": ("osadnik",), "efekty": [("osadnik", "drwal")], "cel": "przyjety"},
            {"tekst": "Pokaż pierścień. Kto cię przysłał?", "warunek": ("wiem", "szpieg"),
             "test": ("zastraszanie", 12), "sukces": "szpieg_zdemaskowany", "porazka": "szpieg_ucieka"},
            {"tekst": "Nie przyjmujemy obcych.", "cel": None},
        ],
    },
    "przyjety": {"mowi": "BRAMA", "tekst": "Kłania się i niesie tobołek do wolnej chaty.", "opcje": [{"tekst": "Dobrze.", "cel": None}]},
    "szpieg_zdemaskowany": {
        "mowi": "OBCY",
        "tekst": "— Dobra, dobra! Herszt z Czerwonych Kapturów płaci za wieści o osadach. Powiem wszystko, tylko nie wieszajcie.",
        "efekty": [("flaga", "znany_szpieg"), ("zloto", 15)],
        "glosy": [("przetrwanie", 10, "Jeśli banda wie, że ją przejrzałeś, nie przyjdzie tak szybko.")],
        "opcje": [{"tekst": "Wynoś się. I powiedz im, że czekamy.", "efekty": [("morale", 4)], "cel": None}],
    },
    "szpieg_ucieka": {"mowi": "BRAMA", "tekst": "Obcy rzuca się do ucieczki i znika w lesie. Wie teraz dużo o twojej palisadzie.",
                      "opcje": [{"tekst": "Cholera.", "cel": None}]},
}}

ROZMOWY["kupiec_u_bram"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "VASCO, WĘDROWNY KUPIEC",
        "tekst": "Ach, osada z dymem z komina! Mam rzeczy, których nie znajdziesz w żadnym Brzeziu. Zwoje, korzenie, pióra. Dla ciebie — uczciwie.",
        "glosy": [("oszustwo", 12, "„Uczciwie” mówi się wtedy, gdy nie jest uczciwie. Cena jest do zbicia.", [("wiem", "zbij")])],
        "opcje": [
            {"tekst": "Zwój z przepisem (60 zł).", "warunek": ("zloto", 60), "efekty": [("zloto", -60), ("przepis", "losowy")], "cel": "start"},
            {"tekst": "Rzadki składnik — kwiat pustyni (30 zł).", "warunek": ("zloto", 30),
             "efekty": [("zloto", -30), ("skladnik", "kwiat_pustyni", 1)], "cel": "start"},
            {"tekst": "Grzyb jaskiniowy (25 zł).", "warunek": ("zloto", 25),
             "efekty": [("zloto", -25), ("skladnik", "grzyb", 1)], "cel": "start"},
            {"tekst": "Te ceny to rozbój. Zejdźmy o połowę.", "warunek": ("wiem", "zbij"), "test": ("perswazja", 13),
             "raz": True, "sukces": "rabat", "porazka": "start"},
            {"tekst": "Odprowadzić go do bramy.", "cel": None},
        ],
    },
    "rabat": {
        "mowi": "VASCO",
        "tekst": "Ha! Kto cię uczył targować? Dobrze, dobrze. Weź ten zwój za trzydzieści. Tylko nikomu ani słowa.",
        "opcje": [
            {"tekst": "Biorę (30 zł).", "warunek": ("zloto", 30), "efekty": [("zloto", -30), ("przepis", "losowy"),
                                                                             ("mysl", "wszystko_na_sprzedaz")], "cel": None},
            {"tekst": "Jednak nie.", "cel": None},
        ],
    },
}}

ROZMOWY["swieto_plonow"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "STARSZYZNA OSADY",
        "tekst": "Żniwa za nami. Ludzie pytają, czy będzie święto plonów. Jedno ognisko, trochę mięsa, trochę pieśni.",
        "glosy": [("przetrwanie", 12, "Za trzydzieści dni zima. Każda racja zjedzona dziś to racja, której zabraknie w mrozie.")],
        "opcje": [
            {"tekst": "Będzie święto! (15 racji)", "warunek": ("surowiec", "zywnosc", 15),
             "efekty": [("surowiec", "zywnosc", -15), ("morale", 14)], "cel": "swieto"},
            {"tekst": "Skromne święto — pieśni tak, uczta nie.", "test": ("perswazja", 12),
             "sukces": "skromne", "porazka": "zawod"},
            {"tekst": "Nie w tym roku. Zima idzie.", "efekty": [("morale", -4), ("mysl", "zimowy_glod")], "cel": None},
        ],
    },
    "swieto": {"mowi": "OSADA", "tekst": "Śpiewy do świtu. Ktoś po raz pierwszy od miesięcy mówi o „naszej” osadzie.",
               "opcje": [{"tekst": "…", "cel": None}]},
    "skromne": {"mowi": "OSADA", "tekst": "Nie ma mięsa, ale jest ogień i jest śmiech. Wystarczy.",
                "efekty": [("morale", 7)], "opcje": [{"tekst": "…", "cel": None}]},
    "zawod": {"mowi": "OSADA", "tekst": "Pieśni milkną szybko. Brzuchy pamiętają dłużej niż uszy.",
              "efekty": [("morale", -2)], "opcje": [{"tekst": "…", "cel": None}]},
}}

ROZMOWY["pijany_straznik"] = {"start": "start", "wezly": {
    "start": {
        "mowi": "NOCNA WARTA",
        "tekst": "{imie} śpi na posterunku, obejmując dzban. Brama stoi otworem.",
        "glosy": [("perswazja", 12, "To trzecia noc z rzędu. Coś go gryzie — ludzie nie piją tak z nudów.", [("wiem", "powod")])],
        "opcje": [
            {"tekst": "Obudzić i zapytać, co się dzieje.", "warunek": ("wiem", "powod"), "cel": "powod"},
            {"tekst": "Wiadro wody i tydzień bez żołdu.", "test": ("zastraszanie", 11), "sukces": "dyscyplina", "porazka": "bunt"},
            {"tekst": "Zostawić. Samemu stanąć na warcie do rana.", "efekty": [("morale", 3), ("karma", 1)], "cel": None},
        ],
    },
    "powod": {"mowi": "{imie}", "tekst": "— Ostatni najazd… Widziałem, jak płonie chata Hely. Nie mogłem nic zrobić. Nie mogę spać.",
              "opcje": [{"tekst": "Nie jesteś sam. Będziemy stać razem.", "efekty": [("morale_osadnika", 30), ("morale", 4),
                                                                                     ("mysl", "cena_krwi")], "cel": None}]},
    "dyscyplina": {"mowi": "NOCNA WARTA", "tekst": "{imie} zrywa się na równe nogi. Od tej nocy warta jest czujna.",
                   "efekty": [("mysl", "twarda_reka")], "opcje": [{"tekst": "…", "cel": None}]},
    "bunt": {"mowi": "NOCNA WARTA", "tekst": "— A kto ty jesteś, żeby mnie sądzić?! — Kilku strażników stoi po jego stronie.",
             "efekty": [("morale", -6)], "opcje": [{"tekst": "…", "cel": None}]},
}}


# ------------------------------------------------------------------ #
#  Sprawy osady                                                        #
# ------------------------------------------------------------------ #

def _warunki_sprawy(gracz: "Gracz") -> dict[str, bool]:
    from game import kalendarz
    from game.osada import ilu_w_zawodzie, osadnicy, wolne_chaty

    lista = osadnicy(gracz)
    return {
        "klotnia_o_racje": len(lista) >= 2,
        "chory_osadnik": any(o.get("chory") for o in lista),
        "zlodziej": len(lista) >= 2,
        "przybysz": wolne_chaty(gracz) > 0,
        "kupiec_u_bram": True,
        "swieto_plonow": kalendarz.pora(gracz)["klucz"] == "jesien" and not gracz.flagi.get(f"swieto_{kalendarz.rok(gracz)}"),
        "pijany_straznik": ilu_w_zawodzie(gracz, "straznik") > 0,
    }


def losuj_sprawe(gracz: "Gracz") -> str | None:
    if len(getattr(gracz, "sprawy", None) or []) >= 3:
        return None
    mozliwe = [k for k, ok in _warunki_sprawy(gracz).items() if ok and k not in gracz.sprawy]
    return random.choice(mozliwe) if mozliwe else None


def _kontekst_sprawy(gracz: "Gracz", klucz: str) -> dict:
    from game import kalendarz
    from game.osada import osadnicy

    lista = osadnicy(gracz)
    kontekst: dict = {}
    if klucz == "chory_osadnik":
        kandydaci = [o for o in lista if o.get("chory")] or lista
    elif klucz == "pijany_straznik":
        kandydaci = [o for o in lista if o["zajecie"] == "straznik"] or lista
    else:
        kandydaci = lista
    if kandydaci:
        o = random.choice(kandydaci)
        kontekst["osadnik"] = o
        kontekst["imie"] = o["imie"]
        inni = [x for x in lista if x is not o]
        kontekst["imie2"] = random.choice(inni)["imie"] if inni else "sąsiad"
    if klucz == "swieto_plonow":
        gracz.flagi[f"swieto_{kalendarz.rok(gracz)}"] = True
    if klucz == "przybysz" and random.random() < 0.5:
        kontekst["osadnik"] = {}  # potrzebny tylko warunek 'osadnik'
    kontekst.setdefault("osadnik", {} if klucz == "przybysz" else None)
    return kontekst


def menu_spraw(gracz: "Gracz") -> None:
    if not getattr(gracz, "sprawy", None):
        print("\n  Nikt nie czeka ze sprawą. Osada żyje swoim rytmem.")
        nacisnij_enter()
        return
    while gracz.sprawy:
        klucz = gracz.sprawy.pop(0)
        if klucz not in ROZMOWY:
            continue
        prowadz(gracz, klucz, _kontekst_sprawy(gracz, klucz))
        if gracz.sprawy:
            if input("\n  Kolejna sprawa czeka. Rozpatrzyć teraz? [T/n] ").strip().lower() == "n":
                return


def rozmowa_z_hersztem(gracz: "Gracz", sila: int, banda: str) -> str:
    from game.oboz import poziom_budynku
    from game.obrona import sila_obrony

    kontekst = {
        "banda": banda,
        "okup": max(20, sila * 2),
        "st_perswazji": min(18, 11 + sila // 40),
        "st_zastraszania": max(8, min(20, 16 + sila // 30 - sila_obrony(gracz) // 12 - poziom_budynku(gracz, "palisada"))),
    }
    return prowadz(gracz, "herszt", kontekst) or "walka"


# ------------------------------------------------------------------ #
#  Dawni NPC w silniku rozmów                                          #
# ------------------------------------------------------------------ #

# Głos, który odzywa się przy każdej z postaci (bierny test), i myśl na koniec wątku.
_GLOSY_NPC: dict[str, tuple] = {
    "karczmarz": (("spostrzegawczosc", 11, "Wyciera ten sam kufel trzeci raz. Nie myśli o kuflu — myśli o córce."), "palenisko_krolestwem"),
    "kupiec": (("oszustwo", 12, "Uśmiecha się oczami, nie ustami. Liczy cię jak towar."), "wszystko_na_sprzedaz"),
    "kaplan": (("perswazja", 12, "Modli się ciszej, gdy mówi o zakonie. Wstydzi się — nie Boga, braci."), "cena_krwi"),
    "stary_rycerz": (("zastraszanie", 12, "Stary, ale stoi jak ktoś, kto wciąż czeka na cios. Nie groź mu — zrozumie to jako zaproszenie."), "twarda_reka"),
    "tajemniczy_wedrowiec": (("spostrzegawczosc", 14, "Jego cień pada nie tam, gdzie powinien. Ktoś — albo coś — idzie za nim."), "cien_przodka"),
    "burmistrz": (("przetrwanie", 12, "Spieczone usta, zapadnięte policzki. Burmistrz też nie je, żeby starczyło dla miasta."), "zimowy_glod"),
    "kupiec_miejski": (("oszustwo", 13, "Mówi „przyjacielu” za często. Przyjaciół się tak nie nazywa — przyjaciół się ma."), "wszystko_na_sprzedaz"),
}


def _graf_npc(gracz: "Gracz", klucz: str) -> dict:
    """Buduje graf rozmowy z danych starego dialogu (tematy, wątek, testy, rekrutacja)."""
    from game.dialogues import _DIALOGI, _etap_watku

    npc = _DIALOGI[klucz]
    mowi = npc["imie"].upper()
    glos, mysl = _GLOSY_NPC.get(klucz, (None, None))
    wezly: dict = {}
    opcje: list = []
    for i, (temat, kwestie) in enumerate(npc["tematy"]):
        wezly[f"temat{i}"] = {"mowi": mowi, "tekst": random.choice(kwestie),
                              "opcje": [{"tekst": "Wróć.", "cel": "start"}]}
        opcje.append({"tekst": temat + ".", "cel": f"temat{i}"})

    watek = npc.get("watek") or {}
    etapy = watek.get("etapy") or []
    etap = _etap_watku(gracz, klucz)
    if etap < len(etapy):
        e = etapy[etap]
        efekty = [("flaga", f"watek_{klucz}", etap + 1)]
        efekty += [(typ, ile) for typ, ile in e.get("nagrody", []) if typ in ("karma", "zloto")]
        if etap + 1 == len(etapy) and mysl:
            efekty.append(("mysl", mysl))
        wezly["watek"] = {"mowi": f"{mowi} — {watek.get('tytul', '')}".upper(), "tekst": e["tekst"],
                          "efekty": efekty, "opcje": [{"tekst": "Wróć.", "cel": "start"}]}
        opcje.append({"tekst": f"📜 {e['etykieta']}", "cel": "watek", "raz": True})

    for i, t in enumerate(npc.get("testy") or []):
        wezly[f"test{i}_s"] = {"mowi": mowi, "tekst": t["sukces"],
                               "efekty": [(typ, ile) for typ, ile in t.get("nagrody", []) if typ in ("karma", "zloto")],
                               "opcje": [{"tekst": "Wróć.", "cel": "start"}]}
        wezly[f"test{i}_p"] = {"mowi": mowi, "tekst": t["porazka"], "opcje": [{"tekst": "Wróć.", "cel": "start"}]}
        opcje.append({"tekst": t["etykieta"], "test": (t["skill"], t["st"]),
                      "sukces": f"test{i}_s", "porazka": f"test{i}_p"})

    if npc.get("rekrut"):
        opcje.append({"tekst": "🤝 Zaproponuj dołączenie do osady.", "efekty": [("rekrut", npc["rekrut"])], "cel": None})
    opcje.append({"tekst": "Odejść.", "cel": None})
    wezly["start"] = {"mowi": mowi, "tekst": random.choice(npc["powitania"]),
                      "glosy": [glos] if glos else [], "opcje": opcje}
    return {"start": "start", "wezly": wezly}


def rozmowa_z_npc(gracz: "Gracz", klucz: str) -> None:
    """Rozmowa z postacią z dialogues.py — w silniku z głosami i białymi/czerwonymi testami."""
    from game.dialogues import _ustaw_etap_watku

    ROZMOWY[f"npc_{klucz}"] = _graf_npc(gracz, klucz)
    try:
        prowadz(gracz, f"npc_{klucz}")
    finally:
        del ROZMOWY[f"npc_{klucz}"]
    etap = gracz.flagi.pop(f"watek_{klucz}", None)
    if etap is not None:
        _ustaw_etap_watku(gracz, klucz, etap)
