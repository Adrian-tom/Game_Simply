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


class TestBiomowZSzumu(unittest.TestCase):
    """Biomy z szumu na współrzędnych świata (wzorzec z Project Zomboid): ciągłe i zróżnicowane."""

    def test_wszystkie_biomy_wystepuja_i_zaden_nie_dominuje(self):
        from collections import Counter
        from game.mapa import BIOMY_NAZWY, biom_swiata
        for seed in (1, 42, 988_061):
            licznik = Counter(biom_swiata(seed, x, y) for x in range(-60, 60) for y in range(-60, 60))
            self.assertEqual(set(licznik), set(BIOMY_NAZWY), seed)
            self.assertLess(max(licznik.values()) / sum(licznik.values()), 0.45, seed)
            self.assertGreater(min(licznik.values()) / sum(licznik.values()), 0.02, seed)

    def test_biom_przechodzi_przez_granice_regionu(self):
        from game.mapa import ROZMIAR, generuj_mape
        zgodne = razem = 0
        for seed in range(1, 9):
            for rx, ry in ((1, 0), (2, 1), (-1, 3), (3, -2)):
                lewy = generuj_mape(3, seed, rx, ry)
                prawy = generuj_mape(3, seed, rx + 1, ry)
                for y in range(ROZMIAR):
                    razem += 1
                    zgodne += lewy[y][ROZMIAR - 1]["biom"] == prawy[y][0]["biom"]
        # niezależne klastry dawały tu ok. 1/4 zgodności; szum świata — wyraźnie ponad połowę
        self.assertGreater(zgodne / razem, 0.6)

    def test_start_na_lace_i_powtarzalnosc(self):
        from game.mapa import SRODEK, biom_swiata, generuj_mape
        for seed in (3, 77, 5_000_001):
            self.assertEqual(biom_swiata(seed, SRODEK + 2, SRODEK - 2), "równiny")
            self.assertEqual(generuj_mape(1, seed, 0, 0)[SRODEK][SRODEK]["biom"], "równiny")
        self.assertEqual(generuj_mape(2, 9, -4, 7), generuj_mape(2, 9, -4, 7))

    def test_szum_jest_gladki_i_w_zakresie(self):
        from game.szum import fbm, szum2
        wartosci = [szum2(x * 0.37, y * 0.41, 5) for x in range(-30, 30) for y in range(-30, 30)]
        self.assertTrue(all(0.0 <= v < 1.0 for v in wartosci))
        skok = max(abs(szum2(x * 0.05, 0.3, 9) - szum2(x * 0.05 + 0.05, 0.3, 9)) for x in range(200))
        self.assertLess(skok, 0.2)
        self.assertTrue(all(0.0 <= fbm(x * 0.9, -x * 0.7, 3) < 1.0 for x in range(-50, 50)))
