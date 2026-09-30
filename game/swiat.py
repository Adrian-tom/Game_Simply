"""Dzienny cykl świata — jedno miejsce, w którym mija czas.

Każdy dzień, niezależnie od tego, gdzie jest bohater:
  1. zmiana pory roku,
  2. bohater je (w obozie z magazynu, na wyprawie z prowiantu) i goi rany,
  3. osada pracuje, je, pali opał, psuje zapasy, płaci utrzymanie,
  4. rośnie zagrożenie najazdem (i może dojść do najazdu),
  5. ruszają karawany i ceny w innych osadach,
  6. dojrzewają myśli w gabinecie,
  7. czasem ktoś z osady przychodzi ze sprawą do rozstrzygnięcia,
  8. czasem los: powódź, pożar, zaraza, wilki albo urodzaj.

Wieści zwykłe trafiają do ``gracz.kronika`` (pokazywanej w obozie),
pilne są zwracane i drukowane od razu.
"""
from __future__ import annotations

import random
from typing import TYPE_CHECKING

from game import kalendarz, przetrwanie

if TYPE_CHECKING:
    from game.player import Gracz

SZANSA_SPRAWY = 0.07


def _zapewnij(gracz: "Gracz") -> None:
    for pole, domyslne in (("kronika", list), ("flagi", dict), ("sprawy", list)):
        if getattr(gracz, pole, None) is None:
            setattr(gracz, pole, domyslne())


def minij_dni(gracz: "Gracz", dni: int = 1, *, odpoczynek: bool = False) -> list[str]:
    """Upływ ``dni`` dni. Zwraca pilne wieści (reszta czeka w kronice)."""
    _zapewnij(gracz)
    pilne: list[str] = []
    for _ in range(max(0, int(dni))):
        pilne += _jeden_dzien(gracz, odpoczynek)
        if not gracz.zyje():
            break
    return pilne


def _jeden_dzien(gracz: "Gracz", odpoczynek: bool) -> list[str]:
    from game import handel, mysli, obrona, osada

    pilne: list[str] = []
    stara_pora = kalendarz.pora(gracz)["klucz"]
    gracz.czas = int(getattr(gracz, "czas", 0) or 0) + 1
    pora = kalendarz.pora(gracz)
    if pora["klucz"] != stara_pora:
        if stara_pora == "zima":
            gracz.statystyki["przetrwane_zimy"] = gracz.statystyki.get("przetrwane_zimy", 0) + 1
        pilne.append(f"  {pora['ikona']}  NADCHODZI {pora['nazwa'].upper()}. {pora['opis']}")
        if pora["klucz"] == "jesien":
            pilne.append("  🍂  Za 30 dni zima: zgromadź żywność i drewno na opał.")

    # bohater
    if getattr(gracz, "w_obozie", True):
        msg = przetrwanie.zjedz_z_magazynu(gracz)
        if msg:
            pilne.append(msg)
        from game.oboz import poziom_budynku
        tempo = 1.0 + (1.0 if odpoczynek else 0.0) + 0.5 * poziom_budynku(gracz, "lecznica")
        if osada.ilu_w_zawodzie(gracz, "uzdrowiciel"):
            tempo += 0.5
        gracz.kronika += przetrwanie.lecz_rany(gracz, tempo)
    else:
        pilne += przetrwanie.dzien_wyprawy(gracz)

    # osada
    kronika, pilne_osady = osada.dzien_osady(gracz)
    gracz.kronika += kronika
    pilne += pilne_osady

    # handel, myśli
    gracz.kronika += handel.dzien_handlu(gracz)
    pilne += mysli.dzien_mysli(gracz)

    # sprawy osady — tylko gdy jest kto przyjść
    if osada.osadnicy(gracz) and random.random() < SZANSA_SPRAWY:
        from game.rozmowy import losuj_sprawe
        sprawa = losuj_sprawe(gracz)
        if sprawa:
            gracz.sprawy.append(sprawa)
            gracz.kronika.append("  📜  Ktoś z osady czeka na ciebie ze sprawą ([22] w obozie).")

    # wydarzenia sezonowe (powódź, pożar, zaraza, wilki, urodzaj)
    from game.wydarzenia import dzien_wydarzen
    pilne += dzien_wydarzen(gracz)

    # zagrożenie i najazd (na końcu dnia — po nocy przychodzą bandy)
    pilne += obrona.dzien_obrony(gracz)
    return pilne
