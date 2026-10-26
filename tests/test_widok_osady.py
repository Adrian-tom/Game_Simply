"""Scena osady z bliska: przełącznik widoku, rozkład placu i rysowanie.

Przełącznik (`game.ekran`) musi działać bez pygame, bo tryb tekstowy też przez
niego przechodzi. Reszta sprawdza rzeczy, które da się stwierdzić bez oglądania
obrazka: czy budynek trafia na swoje pole, czy chata nie staje na kuźni, czy
każdy osadnik dostaje podpis i czy scena w ogóle coś narysowała. Wygląd ocenia
się okiem — tego testy nie zastąpią.
"""
import os
import unittest

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")

from game import ekran, kalendarz, osada
from game.mapa import zapewnij_mape
from game.player import Gracz

try:
    import pygame
except ImportError:  # pragma: no cover - zależy od środowiska
    pygame = None


def _gracz(chaty=3, budynki=None, ludzie=(), czas=5) -> Gracz:
    g = Gracz("Wódz", "Wojownik")
    zapewnij_mape(g)
    g.czas = czas
    g.chaty = chaty
    # Gra trzyma nazwy w zbiorze, a poziomy osobno — scena czyta poziom_budynku().
    g.budynki = set(budynki or ())
    g.poziomy_budynkow = dict(budynki or {})
    g.osadnicy = []
    g.wiezi = {}
    for imie, zajecie in ludzie:
        g.osadnicy.append(osada._zapewnij_osadnika(
            {"imie": imie, "zajecie": zajecie, "morale": 60.0, "cecha": "pracowity"}))
    return g


class TestPrzelacznika(unittest.TestCase):
    """Działa w trybie tekstowym, więc nie wymaga pygame."""

    def tearDown(self):
        ekran.widok = None

    def test_widok_wraca_po_wyjsciu(self):
        self.assertIsNone(ekran.widok)
        with ekran.widok_osady("osada"):
            self.assertEqual(ekran.widok, "osada")
        self.assertIsNone(ekran.widok)

    def test_widoki_zagniezdzaja_sie(self):
        with ekran.widok_osady("osada"):
            with ekran.widok_osady("oboz"):
                self.assertEqual(ekran.widok, "oboz")
            # Menu wchodzą jedno w drugie, więc wyjście z wnętrza musi oddać
            # widok zewnętrznego menu, a nie zgasić go całkiem.
            self.assertEqual(ekran.widok, "osada")

    def test_wyjatek_nie_zostawia_widoku(self):
        with self.assertRaises(RuntimeError):
            with ekran.widok_osady("osada"):
                raise RuntimeError("przerwane menu")
        self.assertIsNone(ekran.widok)


@unittest.skipIf(pygame is None, "brak pygame-ce")
class TestRozkladu(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        pygame.init()
        pygame.display.set_mode((64, 64))

    def setUp(self):
        from grafika.widok_osady import WidokOsady
        self.widok = WidokOsady()

    def test_budynek_staje_na_swoim_polu(self):
        from grafika.widok_osady import MIEJSCA
        plan = self.widok._rozklad(_gracz(0, {"kuznia": 2}))
        self.assertEqual(plan[MIEJSCA["kuznia"]], ("budynek", "kuznia", 2))

    def test_niezbudowanego_budynku_nie_ma_na_placu(self):
        plan = self.widok._rozklad(_gracz(0, {}))
        self.assertNotIn("kuznia", [wpis[1] for wpis in plan.values()])

    def test_chata_nie_staje_na_budynku(self):
        from grafika.widok_osady import MIEJSCA
        # Kuźnia zajmuje pole, które chaty biorą chętnie — ma mieć pierwszeństwo,
        # bo chata jest wymienna, a budynek narysowany jest w jednym miejscu.
        g = _gracz(12, {"kuznia": 1})
        plan = self.widok._rozklad(g)
        self.assertEqual(plan[MIEJSCA["kuznia"]][1], "kuznia")
        self.assertEqual(sum(1 for w in plan.values() if w[0] == "chata"), 12)

    def test_namiot_ustepuje_domowi(self):
        rodzaje = lambda g: [w[0] for w in self.widok._rozklad(g).values()]
        self.assertIn("namiot", rodzaje(_gracz(1, {})))
        self.assertNotIn("namiot", rodzaje(_gracz(1, {"dom": 1})))

    def test_osadnik_stoi_przy_swoim_warsztacie(self):
        from grafika.widok_osady import MIEJSCA
        g = _gracz(0, {"tartak": 1}, [("Olek", "tracz")])
        plan = self.widok._rozklad(g)
        self.assertEqual(self.widok._stanowisko(g.osadnicy[0], plan), MIEJSCA["tartak"])

    def test_bez_warsztatu_osadnik_wraca_pod_ognisko(self):
        g = _gracz(0, {}, [("Olek", "tracz")])
        plan = self.widok._rozklad(g)
        self.assertEqual(self.widok._stanowisko(g.osadnicy[0], plan), (3, 3))


@unittest.skipIf(pygame is None, "brak pygame-ce")
class TestRysowania(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        pygame.init()
        pygame.display.set_mode((64, 64))

    def setUp(self):
        from grafika.scena import SZER, WYS
        from grafika.widok_osady import WidokOsady
        self.widok = WidokOsady()
        self.pow = pygame.Surface((SZER, WYS))
        self.rozmiar = (SZER, WYS)

    def test_pusty_oboz_sie_rysuje(self):
        wynik = self.widok.rysuj(self.pow, _gracz(0, {}), "oboz", 0.0)
        self.assertEqual(wynik["podpisy"], [])

    def test_kazdy_osadnik_dostaje_podpis(self):
        g = _gracz(4, {"farma": 1}, [("Jagna", "rolnik"), ("Olek", "drwal"), ("Wit", "straznik")])
        podpisy = self.widok.rysuj(self.pow, g, "osada", 0.4)["podpisy"]
        self.assertEqual(len(podpisy), 3)
        for imie in ("Jagna", "Olek", "Wit"):
            self.assertTrue(any(imie in p[2] for p in podpisy), imie)

    def test_podpisy_mieszcza_sie_w_kadrze(self):
        szer, wys = self.rozmiar
        g = _gracz(8, {"palisada": 2, "farma": 1, "targ": 1},
                   [("Jagna", "rolnik"), ("Hela", "handlarz"), ("Torin", "gornik"),
                    ("Wit", "straznik"), ("Mira", "zielarz")])
        for x, y, _, _ in self.widok.rysuj(self.pow, g, "osada", 1.3)["podpisy"]:
            self.assertTrue(0 <= x <= szer, x)
            self.assertTrue(-20 <= y <= wys, y)

    def test_scena_cos_rysuje(self):
        self.pow.fill((0, 0, 0))
        self.widok.rysuj(self.pow, _gracz(5, {"palisada": 1, "kuznia": 1}), "osada", 0.2)
        # Czarne płótno zostałoby czarne, gdyby rysowanie wypadło w całości.
        self.assertNotEqual(self.pow.get_at((5, 5))[:3], (0, 0, 0))

    def test_zima_wyglada_inaczej_niz_lato(self):
        lato = pygame.Surface(self.rozmiar)
        zima = pygame.Surface(self.rozmiar)
        self.widok.rysuj(lato, _gracz(3, {}, czas=kalendarz.DNI_PORY + 2), "osada", 0.0)
        self.widok.rysuj(zima, _gracz(3, {}, czas=3 * kalendarz.DNI_PORY + 2), "osada", 0.0)
        self.assertNotEqual(pygame.image.tobytes(lato, "RGB"),
                            pygame.image.tobytes(zima, "RGB"))

    def test_rozbudowa_zmienia_obraz(self):
        przed = pygame.Surface(self.rozmiar)
        po = pygame.Surface(self.rozmiar)
        self.widok.rysuj(przed, _gracz(2, {}), "osada", 0.0)
        self.widok.rysuj(po, _gracz(2, {"kuznia": 1, "palisada": 1}), "osada", 0.0)
        # Scena czyta stan gry na żywo: postawiony budynek musi być widać od razu.
        self.assertNotEqual(pygame.image.tobytes(przed, "RGB"),
                            pygame.image.tobytes(po, "RGB"))

    def test_maksymalna_osada_sie_miesci(self):
        from game.oboz import BUDYNKI
        from game.osada import MAX_CHATY
        g = _gracz(MAX_CHATY, {k: w["max"] for k, w in BUDYNKI.items()},
                   [(f"Osada{i}", "drwal") for i in range(MAX_CHATY)])
        podpisy = self.widok.rysuj(self.pow, g, "osada", 0.7)["podpisy"]
        self.assertEqual(len(podpisy), MAX_CHATY)


@unittest.skipIf(pygame is None, "brak pygame-ce")
class TestRuchuWScenie(unittest.TestCase):
    """Osadnicy chodzą po placu, a scena ma dzień i noc."""

    @classmethod
    def setUpClass(cls):
        pygame.init()
        pygame.display.set_mode((64, 64))

    def setUp(self):
        from grafika.scena import SZER, WYS
        from grafika.widok_osady import WidokOsady
        self.widok = WidokOsady()
        self.pow = pygame.Surface((SZER, WYS))
        self.gracz = _gracz(4, {"kuznia": 1, "tawerna": 1, "farma": 1, "tartak": 1, "palisada": 1},
                            [("Jagna", "rolnik"), ("Olek", "tracz"), ("Wit", "straznik"),
                             ("Hela", "drwal")])

    def _pozycje(self):
        return {o["imie"]: self.widok._ruch.pozycja(o["imie"]) for o in self.gracz.osadnicy}

    def _przewin(self, od: float, do: float, krok: float = 0.05):
        t = od
        while t < do:
            self.widok.rysuj(self.pow, self.gracz, "osada", t)
            t += krok
        return t

    def test_osadnik_nie_teleportuje_sie_miedzy_klatkami(self):
        from game.ruch import PREDKOSC
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.0)
        przed = self._pozycje()
        t = 0.05
        for _ in range(200):
            self.widok.rysuj(self.pow, self.gracz, "osada", t)
            po = self._pozycje()
            for imie, (x, y) in po.items():
                px, py = przed[imie]
                # Klatka trwa 0,05 s, więc przesunięcie nie może przekroczyć drogi z prędkości.
                self.assertLessEqual(((x - px) ** 2 + (y - py) ** 2) ** 0.5,
                                     PREDKOSC * 0.05 + 1e-6, imie)
            przed = po
            t += 0.05

    def test_nikt_nie_wchodzi_w_budynek_ani_w_ognisko(self):
        zablok = self.widok._zablokowane(self.widok._rozklad(self.gracz), 1)
        t = 0.0
        for _ in range(900):
            self.widok.rysuj(self.pow, self.gracz, "osada", t)
            for x, y in self._pozycje().values():
                for bx, by in zablok:
                    self.assertFalse(abs(x - bx) < 0.45 and abs(y - by) < 0.45, (t, x, y))
            t += 0.1

    def test_osadnicy_zmieniaja_miejsce_w_ciagu_doby(self):
        from game.ruch import DLUGOSC_DNIA
        miejsca = set()
        t = 0.0
        while t < DLUGOSC_DNIA:
            self.widok.rysuj(self.pow, self.gracz, "osada", t)
            miejsca.add(tuple(round(c) for c in self.widok._ruch.pozycja("Olek")))
            t += 0.5
        # Praca, ognisko albo tawerna i chata to co najmniej trzy różne miejsca.
        self.assertGreaterEqual(len(miejsca), 3, miejsca)

    def test_chory_nie_idzie_do_pracy(self):
        self.gracz.osadnicy[1]["chory"] = 5     # Olek
        domy = self.widok._domy(self.widok._rozklad(self.gracz))
        t = 0.0
        for _ in range(300):
            self.widok.rysuj(self.pow, self.gracz, "osada", t)
            t += 0.1
        px, py = self.widok._ruch.postacie["Olek"].pole
        # Stoi przy domu (sąsiednie pole), a nie przy tartaku.
        blisko = min(max(abs(px - d[0]), abs(py - d[1])) for d in domy)
        self.assertLessEqual(blisko, 1)

    def test_przybysz_wchodzi_brama(self):
        from grafika.widok_osady import BRAMA
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.0)
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.05)
        self.gracz.osadnicy.append(osada._zapewnij_osadnika(
            {"imie": "Nowy", "zajecie": "drwal", "morale": 60.0, "cecha": "pracowity"}))
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.1)
        x, y = self.widok._ruch.pozycja("Nowy")
        # Brama bywa zajęta (strażnik), więc przybysz staje na sąsiednim polu.
        self.assertLess(((x - BRAMA[0]) ** 2 + (y - BRAMA[1]) ** 2) ** 0.5, 2.0)
        self.assertGreater(y, 4.5)          # a nie w środku placu

    def test_odejscie_osadnika_znika_ze_sceny(self):
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.0)
        self.gracz.osadnicy = [o for o in self.gracz.osadnicy if o["imie"] != "Hela"]
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.1)
        self.assertNotIn("Hela", self.widok._ruch.postacie)

    def test_po_dlugiej_przerwie_scena_nadrabia_ruch(self):
        # Gracz siedział w innym widoku: po powrocie jest już inna pora doby
        # (t = 70 to noc, a zaczynaliśmy w pracy), więc ludzie nie mogą stać
        # w starych miejscach — muszą być w drodze do chat albo już przy nich.
        self.widok.rysuj(self.pow, self.gracz, "osada", 0.0)
        przed = self._pozycje()
        self.widok.rysuj(self.pow, self.gracz, "osada", 70.0)
        po = self._pozycje()
        self.assertNotEqual(przed, po)

    def test_cofniecie_zegara_nie_psuje_sceny(self):
        self.widok.rysuj(self.pow, self.gracz, "osada", 10.0)
        przed = self._pozycje()
        self.widok.rysuj(self.pow, self.gracz, "osada", 2.0)    # zegar okna wystartował od nowa
        self.assertEqual(przed, self._pozycje())

    def test_zastawiony_plac_nie_zbija_ludzi_na_jednym_polu(self):
        # Komplet budynków i chat zostawia na placu jedno wolne pole. Reszta
        # osadników musi czekać na dziedzińcu przed bramą, nie we wnętrzu budynków.
        from game.oboz import BUDYNKI
        from game.osada import MAX_CHATY
        g = _gracz(MAX_CHATY, {k: w["max"] for k, w in BUDYNKI.items()},
                   [(f"Osada{i}", "drwal") for i in range(MAX_CHATY)])
        zablokowane = self.widok._zablokowane(self.widok._rozklad(g), 2)
        t = 0.0
        for _ in range(900):
            self.widok.rysuj(self.pow, g, "osada", t)
            pola = [self.widok._ruch.postacie[o["imie"]].pole for o in g.osadnicy]
            self.assertEqual(len(set(pola)), len(pola), f"t={t}")
            for pole in pola:
                self.assertNotIn(pole, zablokowane, f"t={t}")
            t += 0.1

    def test_noc_jest_ciemniejsza_od_dnia(self):
        from game.ruch import DLUGOSC_DNIA
        from grafika.widok_osady import PRZESUNIECIE_DNIA

        def srednia(czas_dnia):
            t = czas_dnia - PRZESUNIECIE_DNIA
            powierzchnia = pygame.Surface(self.pow.get_size())
            widok = type(self.widok)()
            widok.rysuj(powierzchnia, self.gracz, "osada", t)
            return pygame.transform.average_color(powierzchnia)[:3]

        poludnie = sum(srednia(DLUGOSC_DNIA * 0.4))
        polnoc = sum(srednia(DLUGOSC_DNIA * 0.92))
        self.assertGreater(poludnie, polnoc * 1.5)

    def test_ognisko_swieci_w_nocy_jasniej_niz_otoczenie(self):
        from game.ruch import DLUGOSC_DNIA
        from grafika.widok_osady import OGNISKO, PRZESUNIECIE_DNIA, _srodek
        t = DLUGOSC_DNIA * 0.92 - PRZESUNIECIE_DNIA
        self.widok.rysuj(self.pow, self.gracz, "osada", t)
        sx, sy = _srodek(*OGNISKO)
        przy_ogniu = sum(self.pow.get_at((sx, sy - 3))[:3])
        daleko = sum(self.pow.get_at((sx + 120, sy + 60))[:3])
        self.assertGreater(przy_ogniu, daleko + 60)


if __name__ == "__main__":
    unittest.main()
