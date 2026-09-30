"""Rozmowy w stylu Disco Elysium, rzemiosło i nowe zasady walki."""

import builtins
import contextlib
import io
import random
import unittest

from game import rozmowy, rzemioslo
from game.combat import (
    _nowy_stan_walki,
    _plomienie_i_regeneracja,
    _po_turze_wroga,
    _tura_przeciwnika,
    szansa_ucieczki,
)
from game.enemy import Przeciwnik, losuj_bossa, losuj_przeciwnika, mnoznik_typu
from game.player import Gracz


@contextlib.contextmanager
def odpowiedzi(*lista):
    kolejka = list(lista)
    stary = builtins.input
    def _input(*_):
        if not kolejka:
            raise EOFError("test: skończyły się odpowiedzi")
        return kolejka.pop(0)

    builtins.input = _input
    try:
        with contextlib.redirect_stdout(io.StringIO()) as bufor:
            yield bufor
    finally:
        builtins.input = stary


class TestRozmow(unittest.TestCase):
    def test_szansa_testu(self):
        self.assertEqual(rozmowy.szansa(0, 11), 0.5)
        self.assertEqual(rozmowy.szansa(100, 30), 0.95)  # nat 1 zawsze porażka
        self.assertEqual(rozmowy.szansa(-100, 5), 0.05)  # nat 20 zawsze sukces

    def test_kazdy_cel_istnieje(self):
        for nazwa, rozmowa in rozmowy.ROZMOWY.items():
            wezly = rozmowa["wezly"]
            self.assertIn(rozmowa["start"], wezly, nazwa)
            for wid, wezel in wezly.items():
                for opcja in wezel["opcje"]:
                    for pole in ("cel", "sukces", "porazka"):
                        cel = opcja.get(pole)
                        if cel is not None:
                            self.assertIn(cel, wezly, f"{nazwa}:{wid} → {cel}")

    def test_czerwony_test_przepada_na_zawsze(self):
        g = Gracz("T", "Mag")
        s = rozmowy._Sesja(g, "grimbold", {})
        opcja = {"test": ("spostrzegawczosc", 16), "bialy": False}
        g.testy_rozmow = {"klucz": 99}
        self.assertEqual(rozmowy._stan_testu(s, opcja, "klucz"), "spalony")

    def test_bialy_test_wraca_po_rozwoju(self):
        g = Gracz("T", "Mag")
        s = rozmowy._Sesja(g, "grimbold", {})
        opcja = {"test": ("perswazja", 12)}
        g.testy_rozmow = {"k": s.premia_testu("perswazja")}
        self.assertEqual(rozmowy._stan_testu(s, opcja, "k"), "zamkniety")
        g.atrybuty["charyzma"] += 4
        self.assertEqual(rozmowy._stan_testu(s, opcja, "k"), "otwarty")

    def test_rozmowa_z_grimboldem_uczy_i_daje_mysl(self):
        g = Gracz("T", "Wojownik")
        random.seed(0)
        with odpowiedzi("3", "4", "5"):  # „czemu kowadło milczy?” → „wróć” → „odejść”
            rozmowy.prowadz(g, "grimbold")
        self.assertIn("stal_pamieta", g.mysli)

    def test_herszt_bierze_okup(self):
        g = Gracz("T", "Wojownik")
        g.zloto = 1000
        with odpowiedzi("1"):
            wynik = rozmowy.rozmowa_z_hersztem(g, 50, "Banda")
        self.assertEqual(wynik, "odwolany")
        self.assertEqual(g.zloto, 900)


class TestRzemiosla(unittest.TestCase):
    def test_wytworzenie_zuzywa_skladniki(self):
        g = Gracz("T")
        g.budynki.add("warsztat")
        g.surowce["ziola"] = 3
        przed = g.mikstury
        rzemioslo.wytworz(g, "mikstura")
        self.assertEqual(g.mikstury, przed + 1)
        self.assertEqual(g.surowce["ziola"], 0)

    def test_nieznany_przepis_trzeba_odkryc(self):
        g = Gracz("T")
        g.budynki.add("laboratorium")
        g.surowce["ziola"] = 10
        g.skladniki = {"grzyb": 5}
        self.assertFalse(rzemioslo.mozna_wytworzyc(g, "mikstura_duza")[0])
        with odpowiedzi():
            rzemioslo.eksperyment(g, "ziola", "grzyb")
        self.assertIn("mikstura_duza", rzemioslo.znane(g))

    def test_rzemieslnicy_realizuja_zamowienia(self):
        g = Gracz("T")
        g.budynki.add("warsztat")
        g.surowce["ziola"] = 30
        g.zapasy_cel = {"mikstura": g.mikstury + 3}
        rzemioslo.praca_rzemieslnikow(g, 5.0)
        self.assertEqual(rzemioslo.stan_zapasu(g, "mikstura"), g.zapasy_cel["mikstura"])

    def test_kazdy_przepis_ma_poprawne_skladniki(self):
        from game.oboz import SUROWCE
        for k, p in rzemioslo.PRZEPISY.items():
            for skl in p["koszt"]:
                self.assertTrue(skl in SUROWCE or skl in rzemioslo.SKLADNIKI, f"{k}: {skl}")


class TestWalki(unittest.TestCase):
    def test_trudnosc_zalezy_od_regionu_nie_gracza(self):
        random.seed(3)
        slaby = sum(losuj_przeciwnika(1, mapa_gen=2).max_hp for _ in range(40))
        random.seed(3)
        mocny_gracz = sum(losuj_przeciwnika(15, mapa_gen=2).max_hp for _ in range(40))
        self.assertEqual(slaby, mocny_gracz)

    def test_boss_regionu_pierwszego_do_pokonania(self):
        boss = losuj_bossa(1, 1)
        self.assertLess(boss.max_hp, 250)

    def test_ogien_blokuje_regeneracje_trolla(self):
        troll = Przeciwnik("Trolle", 100, 20, 8, 10, (1, 2))
        troll.hp = 50
        stan = _nowy_stan_walki()
        with odpowiedzi():
            _plomienie_i_regeneracja(troll, stan)
        self.assertGreater(troll.hp, 50)
        troll.hp = 50
        stan["wrog_podpalony"] = 2
        with odpowiedzi():
            _plomienie_i_regeneracja(troll, stan)
        self.assertLess(troll.hp, 50)
        self.assertEqual(mnoznik_typu(troll, "ogien"), 1.5)

    def test_garda_zmniejsza_zapowiedziany_cios(self):
        wyniki = []
        for garda in (False, True):
            random.seed(5)
            g = Gracz("T", "Wojownik")
            g.hp = g.max_hp = 1000
            g.atrybuty["zrecznosc"] = 8
            ork = Przeciwnik("Ork", 100, 60, 5, 10, (1, 2))
            stan = _nowy_stan_walki()
            stan["zapowiedz"] = "ciezki_cios"
            stan["garda"] = garda
            with odpowiedzi():
                _tura_przeciwnika(g, ork, stan)
            wyniki.append(1000 - g.hp)
        self.assertLess(wyniki[1], wyniki[0] * 0.5)

    def test_garda_daje_kontre(self):
        g = Gracz("T")
        stan = _nowy_stan_walki()
        stan["garda"] = True
        with odpowiedzi():
            _po_turze_wroga(g, Przeciwnik("Goblin", 10, 1, 1, 1, (1, 1)), stan)
        self.assertGreater(stan["nastepny_atak_mnoznik"], 1.0)

    def test_ucieczka_zalezy_od_zrecznosci(self):
        g = Gracz("T", "Wojownik")
        stan = _nowy_stan_walki()
        g.atrybuty["zrecznosc"] = 8
        wolny = szansa_ucieczki(g, stan)
        g.atrybuty["zrecznosc"] = 18
        self.assertGreater(szansa_ucieczki(g, stan), wolny)
        stan["jest_boss"] = True
        self.assertLess(szansa_ucieczki(g, stan), szansa_ucieczki(g, _nowy_stan_walki()))


if __name__ == "__main__":
    unittest.main()
