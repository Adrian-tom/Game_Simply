"""Testy progresji postaci, skalowania wrogów i rejestru umiejętności."""

import contextlib
import io
import unittest

from game.combat import HANDLERY_UMIEJETNOSCI, Kontekst, _nowy_stan_walki, _uzyj_umiejetnosci
from game.enemy import Przeciwnik, losuj_bossa, losuj_przeciwnika
from game.player import EXP_PROGI, Gracz
from game.skills import UMIEJETNOSCI


class TestProgresji(unittest.TestCase):
    def test_progi_exp_rosna(self):
        self.assertEqual(EXP_PROGI, sorted(EXP_PROGI))

    def test_awans_daje_punkty(self):
        g = Gracz("A")
        with contextlib.redirect_stdout(io.StringIO()):
            g.zdobadz_exp(EXP_PROGI[1])
        self.assertEqual(g.poziom, 2)
        self.assertGreater(g.punkty_atrybutow, 2)
        self.assertGreater(g.punkty_umiejetnosci, 0)

    def test_podklasa_dostepna_na_piatym_poziomie(self):
        g = Gracz("A")
        with contextlib.redirect_stdout(io.StringIO()):
            g.zdobadz_exp(EXP_PROGI[5])
        self.assertGreaterEqual(g.poziom, 5)
        self.assertTrue(g.podklasa_dostepna)

    def test_exp_w_poziomie_nie_przekracza_progu(self):
        g = Gracz("A")
        with contextlib.redirect_stdout(io.StringIO()):
            g.zdobadz_exp(150)
        zdobyte, potrzeba = g.exp_w_poziomie()
        self.assertLessEqual(zdobyte, potrzeba)


class TestSkalowaniaWrogow(unittest.TestCase):
    def test_wrogowie_rosna_z_poziomem_regionu(self):
        slaby = losuj_przeciwnika(poziom_gracza=1, mapa_gen=1)
        mocny = losuj_przeciwnika(poziom_gracza=1, mapa_gen=10)
        # Ten sam szablon może wylosować różnych wrogów, więc porównujemy skalę.
        self.assertGreater(
            sum(losuj_przeciwnika(1, mapa_gen=10).max_hp for _ in range(30)),
            sum(losuj_przeciwnika(1, mapa_gen=1).max_hp for _ in range(30)),
        )
        self.assertGreater(slaby.max_hp, 0)
        self.assertGreater(mocny.max_hp, 0)

    def test_hardcore_jest_trudniejszy_od_latwego(self):
        latwy = sum(losuj_przeciwnika(5, mapa_gen=3, tryb="latwy").max_hp for _ in range(30))
        hard = sum(losuj_przeciwnika(5, mapa_gen=3, tryb="hardcore").max_hp for _ in range(30))
        self.assertGreater(hard, latwy)

    def test_boss_ma_dodatnie_statystyki(self):
        boss = losuj_bossa(poziom_gracza=5, mapa_gen=3)
        self.assertGreater(boss.hp, 0)
        self.assertGreater(boss.atak, 0)
        self.assertGreater(boss.exp_nagroda, 0)


class TestRejestruUmiejetnosci(unittest.TestCase):
    def test_kazda_umiejetnosc_ma_handler(self):
        self.assertEqual(set(UMIEJETNOSCI), set(HANDLERY_UMIEJETNOSCI))

    def test_kazdy_handler_da_sie_wywolac(self):
        """Żaden handler nie może wysypać się na świeżym stanie walki."""
        for klucz in sorted(UMIEJETNOSCI):
            with self.subTest(umiejetnosc=klucz):
                gracz = Gracz("Test", UMIEJETNOSCI[klucz]["klasa"])
                gracz.poziom = 10
                gracz.max_hp = gracz.hp = 300
                gracz.max_mana = gracz.mana = 200
                gracz.rangi_umiejetnosci[klucz] = 3
                wrog = Przeciwnik("Ork", 500, 20, 6, 50, (5, 10))
                stan = _nowy_stan_walki()
                with contextlib.redirect_stdout(io.StringIO()):
                    wynik = _uzyj_umiejetnosci(klucz, gracz, wrog, stan)
                self.assertIn(wynik, (None, "wygrana", "ucieczka"))

    def test_kontekst_skaluje_wartosci(self):
        gracz = Gracz("Test", "Wojownik")
        wrog = Przeciwnik("Ork", 100, 10, 2, 10, (1, 2))
        k = Kontekst(
            gracz=gracz,
            przeciwnik=wrog,
            stan=_nowy_stan_walki(),
            ranga=1,
            efektywny_atak=20,
            _klucz="potezny_cios",
        )
        self.assertGreater(k.S(10), 0)
        self.assertGreater(k.T(2), 0)


if __name__ == "__main__":
    unittest.main()
