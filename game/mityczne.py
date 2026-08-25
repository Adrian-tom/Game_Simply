"""Rzadkie mityczne lokacje: portal, leże smoka, latająca wyspa."""

from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game.combat import przeprowadz_walke
from game.enemy import losuj_mitycznego
from game.items import EKWIPUNEK, dodaj_do_plecaka
from game.oboz import dodaj_surowiec
from game.utils import wyczysc, wyswietl_linie, nacisnij_enter

if TYPE_CHECKING:
    from game.player import Gracz


def _nagroda_przedmiotu(gracz: Gracz, klucz: str) -> None:
    dodaj_do_plecaka(gracz, klucz)
    item = EKWIPUNEK[klucz]
    print(f"  {item['ikona']}  Zdobywasz: {item['nazwa']} (plecak).")


def _walka_mityczna(gracz: Gracz, typ: str) -> str:
    mapa_gen = getattr(gracz, "mapa_gen", 1)
    tryb = getattr(gracz, "tryb_trudnosci", "normalny")
    przeciwnik = losuj_mitycznego(typ, gracz.poziom, mapa_gen, tryb)
    return przeprowadz_walke(gracz, jest_boss=True, przeciwnik=przeciwnik)


def _odwiedz(gracz: Gracz) -> None:
    gracz.statystyki["odwiedzone_mityczne"] = (
        gracz.statystyki.get("odwiedzone_mityczne", 0) + 1
    )


def _zamknij_lokacje(pole: dict) -> None:
    pole["punkt"] = None


def zdarzenie_mityczne(gracz: Gracz, pole: dict) -> str | None:
    """Wejście na mityczny punkt. Zwraca 'przegrana' albo None."""
    punkt = pole.get("punkt")
    if punkt == "portal":
        return _portal(gracz, pole)
    if punkt == "leze_smoka":
        return _leze_smoka(gracz, pole)
    if punkt == "latajaca_wyspa":
        return _latajaca_wyspa(gracz, pole)
    return None


def _portal(gracz: Gracz, pole: dict) -> str | None:
    wyczysc()
    wyswietl_linie("═")
    print("  🌀  PORTAL DO INNEGO WYMIARU")
    wyswietl_linie("═")
    print("\n  W powietrzu wisi pierścień czarnego światła.")
    print("  Z drugiej strony słychać szept, który nie należy do tego świata.")
    print("\n  [1]  🌀  Wejdź do portalu")
    print("  [0]  🚶  Cofnij się\n")
    if input("  Twój wybór: ").strip() != "1":
        print("  Zostawiasz szczelinę w spokoju. Na razie.")
        nacisnij_enter()
        return None

    _odwiedz(gracz)
    print("\n  Świat wywraca się na nice. Stoisz na obsidianowej równinie.")
    nacisnij_enter()
    wynik = _walka_mityczna(gracz, "portal")
    if wynik == "przegrana":
        return "przegrana"
    if wynik != "wygrana":
        print("  Uciekasz z otchłani. Portal jeszcze pulsuje.")
        nacisnij_enter()
        return None

    _zamknij_lokacje(pole)
    zloto = random.randint(40, 80)
    gracz.zloto += zloto
    dodaj_surowiec(gracz, "ruda", random.randint(2, 4))
    print(f"\n  Portal zamyka się za tobą. W dłoni zostaje chłód innego świata.")
    print(f"  💰  +{zloto} złota   ⛏  ruda")
    _nagroda_przedmiotu(gracz, "klinga_otchlani")
    nacisnij_enter()
    return None


def _leze_smoka(gracz: Gracz, pole: dict) -> str | None:
    wyczysc()
    wyswietl_linie("═")
    print("  🐉  LEŻE SMOKA")
    wyswietl_linie("═")
    print("\n  Żar bije ze szczeliny w skale. Kości poprzedników chrzęszczą pod stopą.")
    print("  W głębi coś ogromnego przesuwa się po złocie.")
    print("\n  [1]  🐉  Wejdź do leża")
    print("  [0]  🚶  Cofnij się, póki możesz\n")
    if input("  Twój wybór: ").strip() != "1":
        print("  Odchodzisz. Smok jeszcze śpi — albo udaje.")
        nacisnij_enter()
        return None

    _odwiedz(gracz)
    print("\n  Ashkaryx otwiera oczy. Powietrze smakuje siarką.")
    nacisnij_enter()
    wynik = _walka_mityczna(gracz, "leze_smoka")
    if wynik == "przegrana":
        return "przegrana"
    if wynik != "wygrana":
        print("  Wymykasz się z żaru. Leże nadal należy do smoka.")
        nacisnij_enter()
        return None

    _zamknij_lokacje(pole)
    zloto = random.randint(80, 140)
    gracz.zloto += zloto
    dodaj_surowiec(gracz, "ruda", random.randint(3, 6))
    dodaj_surowiec(gracz, "skora", random.randint(2, 4))
    print("\n  Smok milknie. Skarbiec jest twój.")
    print(f"  💰  +{zloto} złota   ⛏ ruda   🦌 skóra")
    _nagroda_przedmiotu(gracz, "ostrze_smoka")
    _nagroda_przedmiotu(gracz, "zbroja_lusek")
    nacisnij_enter()
    return None


def _latajaca_wyspa(gracz: Gracz, pole: dict) -> str | None:
    wyczysc()
    wyswietl_linie("═")
    print("  ☁  LATAJĄCA WYSPA")
    wyswietl_linie("═")
    print("\n  Nad równiną unosi się odłamek ziemi. Wiatr niesie śpiew, którego nie znasz.")
    print("  Korzenie wyspy zwisają jak liny — da się wspiąć.")
    print("\n  [1]  🏚  Przeszukaj ruiny na szczycie")
    print("  [2]  🦅  Wezwij strażnika niebios")
    print("  [0]  🚶  Zejdź na ziemię\n")
    wybor = input("  Twój wybór: ").strip()
    if wybor == "0" or wybor not in ("1", "2"):
        print("  Schodzisz. Wyspa zostaje na niebie.")
        nacisnij_enter()
        return None

    _odwiedz(gracz)

    if wybor == "1":
        if random.random() < 0.35:
            print("\n  Wśród obłoków czeka strażnik. Ruiny nie są puste.")
            nacisnij_enter()
            wynik = _walka_mityczna(gracz, "latajaca_wyspa")
            if wynik == "przegrana":
                return "przegrana"
            if wynik != "wygrana":
                print("  Spadasz z wiatrem z powrotem na ziemię.")
                nacisnij_enter()
                return None
        else:
            print("\n  Ruiny są ciche. Znajdujesz dary wiatru.")
            zloto = random.randint(35, 70)
            gracz.zloto += zloto
            dodaj_surowiec(gracz, "ziola", random.randint(3, 5))
            print(f"  💰  +{zloto} złota   🌿 zioła")
            _nagroda_przedmiotu(gracz, "plaszcz_niebios")
            _zamknij_lokacje(pole)
            nacisnij_enter()
            return None

        zloto = random.randint(50, 90)
        gracz.zloto += zloto
        dodaj_surowiec(gracz, "ziola", random.randint(2, 4))
        print(f"  💰  +{zloto} złota")
        _nagroda_przedmiotu(gracz, "plaszcz_niebios")
        _zamknij_lokacje(pole)
        nacisnij_enter()
        return None

    print("\n  Krzyczysz w chmury. Gryf spada jak piorun.")
    nacisnij_enter()
    wynik = _walka_mityczna(gracz, "latajaca_wyspa")
    if wynik == "przegrana":
        return "przegrana"
    if wynik != "wygrana":
        print("  Wyspa odpływa. Następnym razem może cię nie wpuści.")
        nacisnij_enter()
        return None

    _zamknij_lokacje(pole)
    zloto = random.randint(60, 110)
    gracz.zloto += zloto
    dodaj_surowiec(gracz, "ziola", random.randint(3, 5))
    print(f"\n  Wyspa opada o włos i zastyga. Niebiosa oddają ci dar.")
    print(f"  💰  +{zloto} złota")
    _nagroda_przedmiotu(gracz, "plaszcz_niebios")
    nacisnij_enter()
    return None
