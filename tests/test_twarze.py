"""Portrety rozmówców: rysy z imienia, twarze bez twarzy dla miejsc, mruganie.

Testy rysunku sprawdzają rzeczy, które da się stwierdzić bez oglądania obrazka:
czy portret w ogóle coś narysował, czy mieści się w płótnie i czy ta sama osoba
wygląda zawsze tak samo. Wygląd ocenia się okiem — tego testy nie zastąpią.
"""
import os
import unittest

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")

try:
    import pygame
except ImportError:  # pragma: no cover - zależy od środowiska
    pygame = None


@unittest.skipIf(pygame is None, "brak pygame-ce")
class TestRysow(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        pygame.init()

    def test_te_same_imie_daje_te_same_rysy(self):
        from grafika.portret import rysy_rozmowcy
        a, b = rysy_rozmowcy("KARCZMARZ BOLDAN"), rysy_rozmowcy("KARCZMARZ BOLDAN")
        for pole in ("skora", "wlosy", "fryzura", "zarost", "nos", "usta"):
            self.assertEqual(getattr(a, pole), getattr(b, pole), pole)

    def test_rozni_rozmowcy_roznia_sie(self):
        from grafika.portret import rysy_rozmowcy
        klucze = set()
        for m in ("GRIMBOLD", "BOLDAN", "ALDRIC", "EREMIEL", "ALDERON", "VASCO"):
            r = rysy_rozmowcy(m)
            klucze.add((r.skora, r.wlosy, r.fryzura, r.zarost, r.nos, r.brwi))
        # Sześć imion nie może dać jednej twarzy — inaczej ziarno nie działa.
        self.assertGreaterEqual(len(klucze), 4)

    def test_tabela_rozmowcow_wymusza_wyglad(self):
        from grafika.portret import rysy_rozmowcy
        self.assertEqual(rysy_rozmowcy("KAPŁAN EREMIEL").naglowie, "kaptur")
        self.assertEqual(rysy_rozmowcy("STARY RYCERZ ALDERON").naglowie, "helm")
        self.assertEqual(rysy_rozmowcy("GRIMBOLD, KOWAL").zarost, "broda")

    def test_kobiety_bez_zarostu(self):
        from grafika.portret import rysy_rozmowcy
        for imie in ("BURMISTRZ MIRENA", "KORA ŁOWCZYNI", "MIRA ZIELARKA"):
            self.assertIsNone(rysy_rozmowcy(imie).zarost, imie)

    def test_bohater_ma_wyglad_swojej_klasy(self):
        from game import ekran
        from game.player import Gracz
        from grafika.portret import rysy_rozmowcy

        poprzedni = ekran.gracz
        try:
            ekran.ustaw_gracza(Gracz("Testowy", "Nekromanta"))
            self.assertEqual(rysy_rozmowcy("TY").naglowie, "kaptur_kosc")
            ekran.ustaw_gracza(Gracz("Testowy", "Wojownik"))
            self.assertEqual(rysy_rozmowcy("TY").naglowie, "helm")
        finally:
            ekran.ustaw_gracza(poprzedni)


@unittest.skipIf(pygame is None, "brak pygame-ce")
class TestPortretu(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        pygame.init()

    def test_portret_rysuje_i_miesci_sie_w_plotnie(self):
        from grafika.portret import rysy_rozmowcy
        from grafika.twarze import SZER_P, WYS_P, portret

        p = portret(rysy_rozmowcy("GRIMBOLD, KOWAL"))
        self.assertEqual(p.get_size(), (SZER_P, WYS_P))

        ramka = p.get_bounding_rect(min_alpha=1)
        self.assertGreater(ramka.width * ramka.height, 400)   # coś narysowano
        self.assertGreaterEqual(ramka.top, 0)                 # nic nie wystaje poza płótno
        self.assertLessEqual(ramka.bottom, WYS_P)

    def test_kapelusz_nie_jest_sciety_u_gory(self):
        """Regresja: przy cy=20 rondo kapelusza wychodziło poza górną krawędź."""
        from grafika.portret import rysy_rozmowcy
        from grafika.twarze import portret

        p = portret(rysy_rozmowcy("BURMISTRZ"))
        self.assertGreater(p.get_bounding_rect(min_alpha=1).top, 0)

    def test_mruganie_zamyka_i_otwiera_oko(self):
        """
        Mrugnięcie trwa ~0,18 s w cyklu 4 s, więc próbkować trzeba gęsto —
        co 0,25 s siatka po prostu je przeskakuje.
        """
        from grafika.twarze import mruganie

        wartosci = [mruganie(t / 100.0) for t in range(500)]   # 5 s co 10 ms
        self.assertIn(1.0, wartosci)                            # bywa otwarte
        self.assertTrue(any(w == 0.0 for w in wartosci))        # i bywa zamknięte

        # Oko jest otwarte przez zdecydowaną większość czasu — mrugająca
        # bez przerwy postać wygląda na chorą, nie na żywą.
        self.assertGreater(sum(1 for w in wartosci if w == 1.0) / len(wartosci), 0.9)

    def test_rozmowcy_nie_mrugaja_rownoczesnie(self):
        from grafika.twarze import mruganie
        a = [mruganie(t / 100.0, 0.0) for t in range(500)]
        b = [mruganie(t / 100.0, 0.5) for t in range(500)]
        self.assertNotEqual(a, b)

    def test_miejsce_nie_dostaje_twarzy(self):
        """„PLAC PRZY SPICHLERZU" to narracja miejsca, nie rozmówca."""
        from grafika.portret import _czy_miejsce
        for m in ("PLAC PRZY SPICHLERZU", "BRAMA OSADY", "SPICHLERZ", "OSADA"):
            self.assertTrue(_czy_miejsce(m), m)
        for m in ("GRIMBOLD, KOWAL", "HERSZT — Wilcza Banda", "TY", "NOCNA WARTA"):
            self.assertFalse(_czy_miejsce(m), m)

    def test_widok_rozmowy_rysuje_osobe_inaczej_niz_miejsce(self):
        from grafika.portret import WidokRozmowy
        from grafika.scena import SZER, WYS

        widok = WidokRozmowy()
        osoba = pygame.Surface((SZER, WYS))
        miejsce = pygame.Surface((SZER, WYS))
        widok.rysuj(osoba, "GRIMBOLD, KOWAL", 0.4)
        widok.rysuj(miejsce, "GRIMBOLD, KOWAL — PLAC", 0.4)

        # Ten sam klucz tła, ale jeden kadr ma portret, drugi nie.
        srodek_osoba = {tuple(osoba.get_at((SZER // 2, y)))[:3] for y in range(40, 120)}
        srodek_miejsce = {tuple(miejsce.get_at((SZER // 2, y)))[:3] for y in range(40, 120)}
        self.assertNotEqual(srodek_osoba, srodek_miejsce)


if __name__ == "__main__":
    unittest.main()
