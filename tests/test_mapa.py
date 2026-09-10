"""Testy trwałego świata: regiony, seed, poziom trudności."""

import unittest

from game.mapa import (
    ROZMIAR,
    SRODEK,
    czy_region_znany,
    generuj_mape,
    liczba_regionow,
    poziom_regionu,
    przesun_gracza,
    zapewnij_mape,
)
from game.player import Gracz


class TestPoziomRegionu(unittest.TestCase):
    def test_obóz_jest_poziomem_pierwszym(self):
        self.assertEqual(poziom_regionu(0, 0), 1)

    def test_poziom_rosnie_z_odlegloscia(self):
        self.assertEqual(poziom_regionu(1, 0), 2)
        self.assertEqual(poziom_regionu(0, -3), 4)
        self.assertEqual(poziom_regionu(-2, 2), 3)

    def test_poziom_liczy_dalsza_os(self):
        # Chebyshev: decyduje większa współrzędna, nie ich suma.
        self.assertEqual(poziom_regionu(3, 1), 4)


class TestGenerowanie(unittest.TestCase):
    def test_ten_sam_seed_daje_ten_sam_region(self):
        a = generuj_mape(2, seed=777, rx=1, ry=0)
        b = generuj_mape(2, seed=777, rx=1, ry=0)
        self.assertEqual(a, b)

    def test_rozne_seedy_daja_rozne_swiaty(self):
        a = generuj_mape(1, seed=111, rx=0, ry=0)
        b = generuj_mape(1, seed=222, rx=0, ry=0)
        self.assertNotEqual(a, b)

    def test_obóz_tylko_w_regionie_startowym(self):
        start = generuj_mape(1, seed=5, rx=0, ry=0)
        self.assertEqual(start[SRODEK][SRODEK]["punkt"], "obóz")
        obok = generuj_mape(2, seed=5, rx=1, ry=0)
        punkty = {p["punkt"] for wiersz in obok for p in wiersz}
        self.assertNotIn("obóz", punkty)

    def test_rozmiar_siatki(self):
        pola = generuj_mape(1, seed=9, rx=0, ry=0)
        self.assertEqual(len(pola), ROZMIAR)
        self.assertTrue(all(len(w) == ROZMIAR for w in pola))


class TestTrwalySwiat(unittest.TestCase):
    def setUp(self):
        self.gracz = Gracz("Testowy")
        zapewnij_mape(self.gracz)

    def test_powrot_przez_krawedz_wraca_do_tego_samego_regionu(self):
        g = self.gracz
        g.mapa_x = ROZMIAR - 1
        przesun_gracza(g, 1, 0)
        self.assertEqual((g.region_x, g.region_y), (1, 0))
        przesun_gracza(g, -1, 0)
        self.assertEqual((g.region_x, g.region_y), (0, 0))
        self.assertEqual(g.mapa_gen, 1)

    def test_powrot_zachowuje_stan_pol(self):
        g = self.gracz
        g.mapa_x = ROZMIAR - 1
        pole = g.mapa_pola[g.mapa_y][g.mapa_x]
        pole["zbierania"] = 2
        biom = pole["biom"]

        przesun_gracza(g, 1, 0)   # do sąsiedniego regionu
        przesun_gracza(g, -1, 0)  # i z powrotem

        wrocone = g.mapa_pola[g.mapa_y][g.mapa_x]
        self.assertEqual(wrocone["zbierania"], 2)
        self.assertEqual(wrocone["biom"], biom)

    def test_wejscie_od_strony_przeciwnej(self):
        g = self.gracz
        g.mapa_x, g.mapa_y = ROZMIAR - 1, 3
        przesun_gracza(g, 1, 0)
        self.assertEqual((g.mapa_x, g.mapa_y), (0, 3))

    def test_licznik_regionow_rosnie_tylko_dla_nowych(self):
        g = self.gracz
        self.assertEqual(liczba_regionow(g), 1)
        g.mapa_x = ROZMIAR - 1
        przesun_gracza(g, 1, 0)
        self.assertEqual(liczba_regionow(g), 2)
        przesun_gracza(g, -1, 0)
        self.assertEqual(liczba_regionow(g), 2)

    def test_czy_region_znany(self):
        g = self.gracz
        self.assertTrue(czy_region_znany(g, 0, 0))
        self.assertFalse(czy_region_znany(g, 5, 5))

    def test_kazda_postac_ma_wlasny_seed(self):
        a, b = Gracz("A"), Gracz("B")
        zapewnij_mape(a)
        zapewnij_mape(b)
        self.assertNotEqual(a.seed, b.seed)


class TestMigracjaStarychZapisow(unittest.TestCase):
    def test_stary_zapis_bez_wspolrzednych_dostaje_region(self):
        g = Gracz("Stary")
        # Symulacja zapisu sprzed trwałego świata.
        del g.region_x, g.region_y
        g.regiony = {}
        g.mapa_gen = 3
        g.mapa_pola = generuj_mape(3, seed=1, rx=2, ry=0)

        zapewnij_mape(g)

        self.assertEqual(poziom_regionu(g.region_x, g.region_y), 3)
        self.assertEqual(g.mapa_gen, 3)
        self.assertEqual(liczba_regionow(g), 1)


if __name__ == "__main__":
    unittest.main()
