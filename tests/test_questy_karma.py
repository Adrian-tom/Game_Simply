"""Testy questów (postęp od przyjęcia) i karmy (realne skutki)."""

import unittest

from game.karma import (
    bonus_swiatyni,
    etykieta,
    mnoznik_cen,
    modyfikator_rekrutacji,
    modyfikator_zastraszania,
    poziom,
)
from game.player import Gracz
from game.pochodzenie import cena_dla
from game.quests import QUESTY, przyjmij_questa, sprawdz_questy


class TestQuestyNieWstecz(unittest.TestCase):
    def test_quest_nie_zalicza_sie_za_stary_dorobek(self):
        g = Gracz("A")
        g.statystyki["zabite_potwory"] = 20
        przyjmij_questa(g, "lowca_potworow")
        self.assertEqual(sprawdz_questy(g), [])
        self.assertNotIn("lowca_potworow", g.ukonczone_questy)

    def test_quest_zalicza_sie_po_nowym_postepie(self):
        g = Gracz("A")
        g.statystyki["zabite_potwory"] = 20
        przyjmij_questa(g, "lowca_potworow")
        g.statystyki["zabite_potwory"] += QUESTY["lowca_potworow"]["cel_ilosc"]
        self.assertNotEqual(sprawdz_questy(g), [])
        self.assertIn("lowca_potworow", g.ukonczone_questy)

    def test_postep_liczony_od_zera_dla_swiezej_postaci(self):
        g = Gracz("A")
        przyjmij_questa(g, "weteran")
        g.statystyki["wygrane_walki"] = QUESTY["weteran"]["cel_ilosc"]
        self.assertNotEqual(sprawdz_questy(g), [])

    def test_nagroda_przyznana_raz(self):
        g = Gracz("A")
        przyjmij_questa(g, "lowca_potworow")
        g.statystyki["zabite_potwory"] = 100
        zloto_przed = g.zloto
        sprawdz_questy(g)
        po_pierwszym = g.zloto
        sprawdz_questy(g)
        self.assertGreater(po_pierwszym, zloto_przed)
        self.assertEqual(g.zloto, po_pierwszym)


class TestKarmaMaSkutki(unittest.TestCase):
    def _gracz(self, karma: int) -> Gracz:
        g = Gracz("A")
        g.karma = karma
        return g

    def test_dobra_karma_obniza_ceny(self):
        self.assertLess(cena_dla(self._gracz(20), 100), 100)

    def test_zla_karma_podnosi_ceny(self):
        self.assertGreater(cena_dla(self._gracz(-20), 100), 100)

    def test_neutralna_karma_nie_zmienia_cen(self):
        self.assertEqual(cena_dla(self._gracz(0), 100), 100)

    def test_mnoznik_cen_jest_ograniczony(self):
        for karma in (-1000, -20, 0, 20, 1000):
            with self.subTest(karma=karma):
                self.assertGreaterEqual(mnoznik_cen(self._gracz(karma)), 0.85)
                self.assertLessEqual(mnoznik_cen(self._gracz(karma)), 1.15)

    def test_dobra_karma_ulatwia_rekrutacje(self):
        self.assertLess(modyfikator_rekrutacji(self._gracz(20)), 0)

    def test_zla_karma_utrudnia_rekrutacje_ale_ulatwia_zastraszanie(self):
        zly = self._gracz(-20)
        self.assertGreater(modyfikator_rekrutacji(zly), 0)
        self.assertLess(modyfikator_zastraszania(zly), 0)

    def test_swiatynia_nagradza_dobrych_i_karze_zlych(self):
        self.assertGreater(bonus_swiatyni(self._gracz(20)), 0)
        self.assertEqual(bonus_swiatyni(self._gracz(0)), 0)
        self.assertLess(bonus_swiatyni(self._gracz(-20)), 0)

    def test_kazdy_poziom_karmy_ma_nazwe(self):
        for karma in (100, 12, 5, 0, -5, -12, -100):
            with self.subTest(karma=karma):
                klucz, nazwa, ikona = poziom(self._gracz(karma))
                self.assertTrue(klucz and nazwa and ikona)
                self.assertIn(nazwa, etykieta(self._gracz(karma)))


if __name__ == "__main__":
    unittest.main()
