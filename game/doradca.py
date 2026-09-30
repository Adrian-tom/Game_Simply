"""Doradca w obozie: jedna–dwie najpilniejsze podpowiedzi na podstawie stanu osady.

Survival city builder ma dużo liczb naraz; doradca wskazuje tę, która
za chwilę zaboli (głód, zima bez opału, najazd jutro, ludzie bez pracy).
"""
from __future__ import annotations

from typing import TYPE_CHECKING

from game import kalendarz, mysli, przetrwanie, talenty
from game.oboz import ma_budynek
from game.osada import CENA_OSADNIKA, bilans_zywnosci, ilu_w_zawodzie, osadnicy, wolne_chaty

if TYPE_CHECKING:
    from game.player import Gracz


def porady(gracz: "Gracz", ile: int = 2) -> list[str]:
    wynik: list[str] = []
    zywnosc = gracz.surowce.get("zywnosc", 0)
    bilans = bilans_zywnosci(gracz)

    if getattr(gracz, "najazd_za", None) is not None and gracz.najazd_za <= 3:
        wynik.append(f"Najazd za {gracz.najazd_za} dni. Zostań w obozie, by bronić osobiście — "
                     "strażnicy, palisada i drewno na smołę robią różnicę.")
    if bilans < 0 and zywnosc // max(1, -bilans) < 7:
        wynik.append(f"Jedzenia starczy na ~{zywnosc // max(1, -bilans)} dni. Myśliwy, rolnik (farma), "
                     "polowanie w [15] albo zbieranie na mapie.")
    dni_zimy = kalendarz.dni_do_zimy(gracz)
    potrzeba_opalu = 30 * (1 + len(osadnicy(gracz)) // 4)
    if 0 < dni_zimy <= 25 and gracz.surowce.get("drewno", 0) < potrzeba_opalu:
        wynik.append(f"Zima za {dni_zimy} dni. Na opał potrzeba ok. {potrzeba_opalu} drewna "
                     f"(masz {gracz.surowce.get('drewno', 0)}), do tego zapas jedzenia — pola zimą nie rodzą.")
    if dni_zimy <= 25 and not przetrwanie.ma_odzienie(gracz):
        wynik.append("Bez ciepłego odzienia zimowe wyprawy ranią co dzień (warsztat: 4 skóry + deska).")
    bezczynni = ilu_w_zawodzie(gracz, "bezczynny")
    if bezczynni:
        wynik.append(f"{bezczynni} osadnik(ów) bez zajęcia — przydziel im pracę w [16].")
    if not ma_budynek(gracz, "farma") and gracz.czas >= 3:
        wynik.append("Pola uprawne (farma) to stałe jedzenie — rolnik daje ~3 racje dziennie, jesienią więcej.")
    if wolne_chaty(gracz) and gracz.zloto >= CENA_OSADNIKA:
        wynik.append(f"Masz wolną chatę — zatrudnij osadnika w [16] ({CENA_OSADNIKA} zł).")
    if przetrwanie.rany(gracz):
        wynik.append("Rany goją się szybciej przy odpoczynku [3], w lecznicy i po maści z laboratorium.")
    if talenty.punkty(gracz):
        wynik.append("Niewydane punkty talentów — drzewko w [20].")
    odkryte = [k for k, w in (gracz.mysli or {}).items() if w.get("stan") == "odkryta" and k in mysli.MYSLI]
    if odkryte:
        wynik.append(f"Czeka myśl do przyswojenia: „{mysli.MYSLI[odkryte[0]]['nazwa']}” [21].")
    return wynik[:ile]
