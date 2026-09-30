"""Testy zapisu: round-trip, atomowość, uszkodzony plik."""

import json
import tempfile
import unittest
from pathlib import Path

from game import savegame
from game.mapa import ROZMIAR, liczba_regionow, przesun_gracza, zapewnij_mape
from game.player import Gracz
from game.quests import przyjmij_questa


class TestZapisu(unittest.TestCase):
    def setUp(self):
        self.katalog = tempfile.TemporaryDirectory()
        self.oryginalny = savegame._PLIK_ZAPISU
        savegame._PLIK_ZAPISU = Path(self.katalog.name) / "savegame.json"

    def tearDown(self):
        savegame._PLIK_ZAPISU = self.oryginalny
        self.katalog.cleanup()

    def _gracz_po_wyprawie(self) -> Gracz:
        g = Gracz("Bohater", "Mag")
        zapewnij_mape(g)
        g.mapa_x = ROZMIAR - 1
        przesun_gracza(g, 1, 0)          # drugi region
        g.mapa_y = ROZMIAR - 1
        przesun_gracza(g, 0, 1)          # trzeci region
        g.zloto = 421
        g.karma = -7
        przyjmij_questa(g, "lowca_potworow")
        return g

    def test_round_trip_zachowuje_osade_i_nowe_systemy(self):
        from game import mysli, przetrwanie, rzemioslo, swiat, talenty
        g = Gracz("Wódz", "Druid")
        zapewnij_mape(g)
        g.poziomy_budynkow = {"farma": 2}
        g.budynki.add("farma")
        g.osadnicy[0]["zajecie"] = "rolnik"
        przetrwanie.dodaj_rane(g, "wstrzas")
        g.punkty_talentow = 1
        talenty.wykup(g, "oszczedny")
        mysli.odkryj(g, "cena_krwi")
        rzemioslo.naucz(g, "bomba")
        g.skladniki = {"grzyb": 2}
        g.karawany = [{"cel": "brzezie", "nazwa": "Brzezie", "dni": 12, "zostalo": 10, "eskorta": 1,
                       "wartosc": 50, "ryzyko": 0.1, "zakup": ("ruda", 10)}]
        g.testy_rozmow = {"grimbold:powitanie:3": 2}
        swiat.minij_dni(g, 3)
        self.assertTrue(savegame.zapisz_gre(g))

        w = savegame.wczytaj_gre()
        self.assertEqual(w.czas, g.czas)
        self.assertEqual(w.poziomy_budynkow, {"farma": 2})
        self.assertEqual([o["zajecie"] for o in w.osadnicy], [o["zajecie"] for o in g.osadnicy])
        self.assertEqual(w.rany, g.rany)
        self.assertEqual(w.talenty, ["oszczedny"])
        self.assertEqual(w.mysli, g.mysli)
        self.assertIn("bomba", rzemioslo.znane(w))
        self.assertEqual(w.skladniki, {"grzyb": 2})
        self.assertEqual(w.karawany[0]["zakup"], ["ruda", 10])  # krotka → lista w JSON, kod to znosi
        self.assertEqual(w.testy_rozmow, g.testy_rozmow)
        swiat.minij_dni(w, 5)  # wczytany stan musi dać się dalej symulować

    def test_stary_zapis_z_martwa_postacia_budzi_sie_ranny(self):
        g = Gracz("Stary", "Wojownik")
        zapewnij_mape(g)
        g.hp = 0
        savegame.zapisz_gre(g)
        w = savegame.wczytaj_gre()
        self.assertGreater(w.hp, 0)

    def test_round_trip_zachowuje_swiat(self):
        g = self._gracz_po_wyprawie()
        self.assertTrue(savegame.zapisz_gre(g))

        w = savegame.wczytaj_gre()
        zapewnij_mape(w)

        self.assertEqual(w.seed, g.seed)
        self.assertEqual((w.region_x, w.region_y), (g.region_x, g.region_y))
        self.assertEqual(liczba_regionow(w), liczba_regionow(g))
        self.assertEqual(w.regiony, g.regiony)
        self.assertEqual(w.zloto, 421)
        self.assertEqual(w.karma, -7)
        self.assertEqual(w.questy_start, g.questy_start)

    def test_brak_pliku_zwraca_none(self):
        self.assertIsNone(savegame.wczytaj_gre())

    def test_uszkodzony_zapis_podnosi_wyjatek(self):
        savegame._PLIK_ZAPISU.write_text("{ to nie jest JSON", encoding="utf-8")
        with self.assertRaises(savegame.ZapisUszkodzony):
            savegame.wczytaj_gre()

    def test_zapis_nie_zostawia_pliku_tymczasowego(self):
        g = Gracz("A")
        zapewnij_mape(g)
        savegame.zapisz_gre(g)
        tymczasowe = list(Path(self.katalog.name).glob("*.tmp"))
        self.assertEqual(tymczasowe, [])

    def test_zapis_jest_poprawnym_jsonem(self):
        g = self._gracz_po_wyprawie()
        savegame.zapisz_gre(g)
        dane = json.loads(savegame._PLIK_ZAPISU.read_text(encoding="utf-8"))
        self.assertIn("regiony", dane)
        self.assertIn("seed", dane)

    def test_nadpisanie_nie_niszczy_poprzedniego_przy_bledzie(self):
        g = Gracz("A")
        zapewnij_mape(g)
        g.zloto = 100
        savegame.zapisz_gre(g)

        # Stan, którego nie da się zserializować — zapis musi się nie udać,
        # ale poprzedni, działający plik ma zostać nietknięty.
        g.zloto = 999
        g.statystyki["zly_typ"] = {1, 2, 3}  # set nie przechodzi przez JSON
        self.assertFalse(savegame.zapisz_gre(g))

        odczytany = savegame.wczytaj_gre()
        self.assertEqual(odczytany.zloto, 100)


class TestSciezkiZapisu(unittest.TestCase):
    def test_zapis_lezy_obok_gry_a_nie_w_cwd(self):
        self.assertTrue(savegame._PLIK_ZAPISU.is_absolute())
        self.assertEqual(savegame._PLIK_ZAPISU.parent, savegame._KATALOG_GRY)


if __name__ == "__main__":
    unittest.main()
