"""Okno pixelart: scena, konsola i przejmowanie print/input.

Testy grafiki wymagają pygame-ce i działają bez monitora (SDL_VIDEODRIVER=dummy).
Bez pygame są pomijane — logika gry jest testowana osobno.
"""
import builtins
import os
import unittest

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")

try:
    import pygame
except ImportError:  # pragma: no cover - zależy od środowiska
    pygame = None

from game import ekran
from game.mapa import generuj_mape, kierunki
from game.world import _SKROTY_EKSPLORACJI


class TestSkrotow(unittest.TestCase):
    def test_skroty_eksploracji_trafiaja_w_opcje_menu(self):
        dozwolone = set(kierunki()) | {"5"}
        self.assertTrue(set(_SKROTY_EKSPLORACJI.values()) <= dozwolone)
        # strzałki odpowiadają kierunkom świata, nie kolejności klawiszy
        self.assertEqual(kierunki()[_SKROTY_EKSPLORACJI["gora"]][0], "północ")
        self.assertEqual(kierunki()[_SKROTY_EKSPLORACJI["dol"]][0], "południe")
        self.assertEqual(kierunki()[_SKROTY_EKSPLORACJI["lewo"]][0], "zachód")
        self.assertEqual(kierunki()[_SKROTY_EKSPLORACJI["prawo"]][0], "wschód")


@unittest.skipIf(pygame is None, "brak pygame-ce")
class TestGrafiki(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        pygame.init()

    def _kolory(self, pow_):
        w, h = pow_.get_size()
        return {tuple(pow_.get_at((x, y)))[:3] for y in range(0, h, 3) for x in range(0, w, 3)}

    def test_scena_rysuje_region_z_generatora_gry(self):
        from grafika.scena import SZER, WYS, ScenaMapy

        cel = pygame.Surface((SZER, WYS))
        pola = generuj_mape(1, seed=19)
        ScenaMapy().rysuj(cel, pola, 0.0, gracz_xy=(4, 4), klasa="Mag", wszystko_odkryte=True)
        self.assertGreater(len(self._kolory(cel)), 200)

    def test_mgla_zaslania_nieodkryte_pola(self):
        from grafika.scena import SZER, WYS, ScenaMapy

        scena = ScenaMapy()
        pola = generuj_mape(1, seed=19)
        odkryte = pygame.Surface((SZER, WYS))
        zakryte = pygame.Surface((SZER, WYS))
        scena.rysuj(odkryte, pola, 0.0, wszystko_odkryte=True)
        scena.rysuj(zakryte, pola, 0.0)  # generator zwraca pola nieodkryte
        self.assertGreater(len(self._kolory(odkryte)), len(self._kolory(zakryte)))

    def test_noc_jest_ciemniejsza(self):
        from grafika.scena import SZER, WYS, ScenaMapy

        scena = ScenaMapy()
        pola = generuj_mape(1, seed=19)
        dzien, noc = pygame.Surface((SZER, WYS)), pygame.Surface((SZER, WYS))
        scena.rysuj(dzien, pola, 0.0, wszystko_odkryte=True)
        scena.zmierzch = True
        scena.rysuj(noc, pola, 0.0, wszystko_odkryte=True)
        jasnosc = lambda p: sum(pygame.transform.average_color(p)[:3])  # noqa: E731
        self.assertLess(jasnosc(noc), jasnosc(dzien))

    def test_konsola_rozpoznaje_klikalne_opcje(self):
        from grafika.okno import Konsola, Pisarz

        konsola = Konsola(pygame.Rect(0, 0, 600, 400), Pisarz(16))
        konsola.pisz("  MENU\n  [1]  Nowa gra\n  [10] Księga\n  zwykły tekst\n  Twój wybór: ")
        konsola.rysuj(pygame.Surface((600, 400)), "", 0.0, (0, 0))
        self.assertEqual([k for _, k in konsola.opcje], ["1", "10"])

    def test_konsola_czysci_jak_cls(self):
        from grafika.okno import Konsola, Pisarz

        konsola = Konsola(pygame.Rect(0, 0, 600, 400), Pisarz(16))
        konsola.pisz("coś\nwięcej\n")
        konsola.wyczysc()
        self.assertEqual(konsola.linie, [""])

    def test_okno_oddaje_print_i_input_po_wyjsciu(self):
        import grafika.okno as okno
        import main

        stare = builtins.print, builtins.input, os.system
        oryginalny_input = okno.Okno.input
        widziane = []

        def udawany(self, zacheta=""):
            widziane.append(zacheta)
            self.rysuj("")
            return "3"  # menu główne → Wyjście

        okno.Okno.input = udawany
        try:
            okno.uruchom_w_oknie(main.main)
        finally:
            okno.Okno.input = oryginalny_input
        self.assertTrue(widziane)
        self.assertEqual((builtins.print, builtins.input, os.system), stare)
        self.assertFalse(ekran.graficzny)


if __name__ == "__main__":
    unittest.main()
