"""Relacje między osadnikami: dryf więzi, morale, pary, żałoba po stracie.

Testy sprawdzają nie tylko „czy funkcja zwraca liczbę", ale czy system robi
to, po co istnieje: czy zgodne charaktery się zbliżają, czy głód kłóci ludzi,
czy śmierć bliskiego kogoś łamie i czy relacje przechodzą przez zapis gry.
"""
import unittest

from game import osada, savegame, wiezi
from game.mapa import zapewnij_mape
from game.player import Gracz


def _gracz_z_osada(ilu: int = 2, cechy=None, zajecie: str = "drwal") -> Gracz:
    g = Gracz("Wódz", "Wojownik")
    zapewnij_mape(g)
    g.chaty = max(ilu, 4)
    g.zloto = 9999
    # Podmieniamy startowych osadników na własnych, więc startowa więź
    # Olka i Jagny musi zniknąć razem z nimi.
    g.osadnicy = []
    g.wiezi = {}
    for i in range(ilu):
        o = osada._zapewnij_osadnika({
            "imie": f"Osada{i}", "zajecie": zajecie, "morale": 60.0,
            "cecha": (cechy[i] if cechy else "skromny"),
        })
        g.osadnicy.append(o)
    return g


class TestWiezi(unittest.TestCase):
    def test_wiez_jest_symetryczna(self):
        g = _gracz_z_osada()
        wiezi.zmien(g, "Ala", "Bok", 30)
        self.assertEqual(wiezi.wiez(g, "Ala", "Bok"), wiezi.wiez(g, "Bok", "Ala"))
        # Jeden klucz na związek — nie dwa wpisy, które mogą się rozjechać.
        self.assertEqual(len(g.wiezi), 1)

    def test_wiez_nie_wychodzi_poza_zakres(self):
        g = _gracz_z_osada()
        wiezi.zmien(g, "Ala", "Bok", 500)
        self.assertEqual(wiezi.wiez(g, "Ala", "Bok"), wiezi.MAX_WIEZ)
        wiezi.zmien(g, "Ala", "Bok", -500)
        self.assertEqual(wiezi.wiez(g, "Ala", "Bok"), wiezi.MIN_WIEZ)

    def test_sam_ze_soba_nie_ma_wiezi(self):
        g = _gracz_z_osada()
        wiezi.zmien(g, "Ala", "Ala", 50)
        self.assertEqual(wiezi.wiez(g, "Ala", "Ala"), 0.0)
        self.assertEqual(g.wiezi, {})

    def test_nazwy_progow(self):
        self.assertEqual(wiezi.nazwa_wiezi(90), "para")
        self.assertEqual(wiezi.nazwa_wiezi(50), "przyjaciele")
        self.assertEqual(wiezi.nazwa_wiezi(0), "obojętni")
        self.assertEqual(wiezi.nazwa_wiezi(-50), "waśń")
        self.assertEqual(wiezi.nazwa_wiezi(-90), "wrogowie")


class TestDryfu(unittest.TestCase):
    def test_wspolna_praca_i_zgodne_cechy_zblizaja(self):
        g = _gracz_z_osada(2, ["pracowity", "pracowity"])
        for _ in range(40):
            wiezi.przelicz_dzien(g)
        self.assertGreater(wiezi.wiez(g, "Osada0", "Osada1"), wiezi.PROG_PRZYJAZN)

    def test_sprzeczne_cechy_dziela(self):
        g = _gracz_z_osada(2, ["pracowity", "leniwy"])
        for _ in range(60):
            wiezi.przelicz_dzien(g)
        self.assertLess(wiezi.wiez(g, "Osada0", "Osada1"), 0)

    def test_glod_i_zimno_kloca_ludzi(self):
        zgodni = _gracz_z_osada(2, ["pracowity", "pracowity"])
        glodni = _gracz_z_osada(2, ["pracowity", "pracowity"])
        for _ in range(30):
            wiezi.przelicz_dzien(zgodni)
            wiezi.przelicz_dzien(glodni, glodni=True, zimno=True)
        self.assertGreater(wiezi.wiez(zgodni, "Osada0", "Osada1"),
                           wiezi.wiez(glodni, "Osada0", "Osada1"))

    def test_jeden_osadnik_nie_tworzy_wiezi(self):
        g = _gracz_z_osada(1)
        self.assertEqual(wiezi.przelicz_dzien(g), [])
        self.assertEqual(g.wiezi, {})

    def test_wiezi_po_nieobecnych_sa_czyszczone(self):
        g = _gracz_z_osada(2, ["pracowity", "pracowity"])
        wiezi.zmien(g, "Osada0", "Duch", 60)
        wiezi.przelicz_dzien(g)
        # „Duch" nie mieszka w osadzie — jego więzi nie mogą rosnąć w nieskończoność.
        self.assertNotIn("Duch|Osada0", g.wiezi)

    def test_wiesc_o_przyjazni_pojawia_sie_raz(self):
        g = _gracz_z_osada(2, ["pracowity", "pracowity"])
        wszystkie = []
        for _ in range(60):
            wszystkie += wiezi.przelicz_dzien(g)
        przyjaznie = [w for w in wszystkie if "zaprzyjaźnili" in w]
        self.assertEqual(len(przyjaznie), 1, przyjaznie)


class TestMorale(unittest.TestCase):
    def test_przyjaciel_podnosi_wrog_obniza(self):
        g = _gracz_z_osada(2)
        o = g.osadnicy[0]
        self.assertEqual(wiezi.premia_morale(g, o), 0.0)

        wiezi.zmien(g, "Osada0", "Osada1", 60)
        self.assertGreater(wiezi.premia_morale(g, o), 0)

        wiezi.zmien(g, "Osada0", "Osada1", -140)
        self.assertLess(wiezi.premia_morale(g, o), 0)

    def test_premia_jest_ograniczona(self):
        g = _gracz_z_osada(6)
        for i in range(1, 6):
            wiezi.zmien(g, "Osada0", f"Osada{i}", 95)
        # Limit, żeby relacje nie przykryły głodu i zimna.
        self.assertLessEqual(wiezi.premia_morale(g, g.osadnicy[0]), 18.0)

    def test_morale_w_symulacji_rosnie_od_przyjazni(self):
        g = _gracz_z_osada(2, ["pracowity", "pracowity"])
        wiezi.zmien(g, "Osada0", "Osada1", 95)
        cel_z = osada._cel_morale(g, g.osadnicy[0], False, False, False)
        g.wiezi = {}
        cel_bez = osada._cel_morale(g, g.osadnicy[0], False, False, False)
        self.assertGreater(cel_z, cel_bez)


class TestStraty(unittest.TestCase):
    def test_smierc_bliskiego_lamie_partnera(self):
        g = _gracz_z_osada(2)
        wiezi.zmien(g, "Osada0", "Osada1", 95)
        g.osadnicy[1]["morale"] = 80.0

        wiesci = wiezi.po_stracie(g, "Osada0", "zmarl")
        self.assertLess(g.osadnicy[1]["morale"], 80.0)
        self.assertTrue(any("Osada1" in w for w in wiesci), wiesci)
        # Więzi zmarłego znikają — inaczej liczyłyby się dalej do morale.
        self.assertEqual(g.wiezi, {})

    def test_smierc_boli_bardziej_niz_odejscie(self):
        strata, zgon = _gracz_z_osada(2), _gracz_z_osada(2)
        for g in (strata, zgon):
            wiezi.zmien(g, "Osada0", "Osada1", 95)
            g.osadnicy[1]["morale"] = 80.0
        wiezi.po_stracie(strata, "Osada0", "odeszl")
        wiezi.po_stracie(zgon, "Osada0", "zmarl")
        self.assertLess(zgon.osadnicy[1]["morale"], strata.osadnicy[1]["morale"])

    def test_wrog_nie_zaluje_odejscia(self):
        g = _gracz_z_osada(2)
        wiezi.zmien(g, "Osada0", "Osada1", -90)
        g.osadnicy[1]["morale"] = 50.0
        wiezi.po_stracie(g, "Osada0", "odeszl")
        self.assertGreater(g.osadnicy[1]["morale"], 50.0)

    def test_strata_obojetnego_nikogo_nie_rusza(self):
        g = _gracz_z_osada(2)
        g.osadnicy[1]["morale"] = 60.0
        self.assertEqual(wiezi.po_stracie(g, "Osada0", "zmarl"), [])
        self.assertEqual(g.osadnicy[1]["morale"], 60.0)


class TestNarodzin(unittest.TestCase):
    def _para(self):
        g = _gracz_z_osada(2, ["pracowity", "pracowity"])
        g.chaty = 6
        g.surowce["zywnosc"] = 200
        g.zywnosc = 200
        wiezi.zmien(g, "Osada0", "Osada1", 95)
        for o in g.osadnicy:
            o["morale"] = 90.0
        return g

    def test_para_moze_doczekac_sie_dziecka(self):
        g = self._para()
        for _ in range(900):
            if len(g.osadnicy) > 2:
                break
            wiezi.przelicz_dzien(g)
        self.assertGreater(len(g.osadnicy), 2, "para nigdy nie doczekała się dziecka")

        dziecko = g.osadnicy[-1]
        self.assertEqual(sorted(dziecko["rodzice"]), ["Osada0", "Osada1"])
        # Dziecko od początku kocha rodziców — rodzina ma być realnym węzłem.
        for rodzic in dziecko["rodzice"]:
            self.assertGreaterEqual(wiezi.wiez(g, dziecko["imie"], rodzic), wiezi.PROG_PRZYJAZN)

    def test_rodzina_odpoczywa_miedzy_narodzinami(self):
        g = self._para()
        g.chaty = 12
        g.czas = 0
        for _ in range(900):
            wiezi.przelicz_dzien(g)
        # Dzien stoi w miejscu, wiec przerwa nigdy nie mija: jedno dziecko i koniec.
        self.assertEqual(len(g.osadnicy), 3, [o["imie"] for o in g.osadnicy])

        g.czas = wiezi.PRZERWA_NARODZIN
        for _ in range(900):
            if len(g.osadnicy) > 3:
                break
            wiezi.przelicz_dzien(g)
        self.assertGreater(len(g.osadnicy), 3, "po przerwie para nie doczekala sie drugiego dziecka")

    def test_glod_wstrzymuje_narodziny(self):
        g = self._para()
        for _ in range(900):
            wiezi.przelicz_dzien(g, glodni=True)
        self.assertEqual(len(g.osadnicy), 2)

    def test_brak_chaty_wstrzymuje_narodziny(self):
        g = self._para()
        g.chaty = 2   # obie zajęte
        for _ in range(900):
            wiezi.przelicz_dzien(g)
        self.assertEqual(len(g.osadnicy), 2)


class TestZapisu(unittest.TestCase):
    def setUp(self):
        import pathlib
        import tempfile
        self.katalog = tempfile.TemporaryDirectory()
        self.oryginalny = savegame._PLIK_ZAPISU
        savegame._PLIK_ZAPISU = pathlib.Path(self.katalog.name) / "savegame.json"

    def tearDown(self):
        savegame._PLIK_ZAPISU = self.oryginalny
        self.katalog.cleanup()

    def test_wiezi_przechodza_przez_zapis(self):
        g = _gracz_z_osada(2, ["pracowity", "pracowity"])
        for _ in range(20):
            wiezi.przelicz_dzien(g)
        przed = dict(g.wiezi)
        self.assertTrue(przed)

        self.assertTrue(savegame.zapisz_gre(g))
        w = savegame.wczytaj_gre()
        self.assertEqual(w.wiezi, przed)
        # Wczytany stan musi dać się dalej symulować.
        wiezi.przelicz_dzien(w)

    def test_stary_zapis_bez_wiezi_dziala(self):
        g = _gracz_z_osada(2)
        savegame.zapisz_gre(g)

        import json
        dane = json.loads(savegame._PLIK_ZAPISU.read_text(encoding="utf-8"))
        del dane["wiezi"]
        savegame._PLIK_ZAPISU.write_text(json.dumps(dane, ensure_ascii=False), encoding="utf-8")

        w = savegame.wczytaj_gre()
        self.assertEqual(w.wiezi, {})
        wiezi.przelicz_dzien(w)


class TestOpisu(unittest.TestCase):
    def test_opis_relacji_wymienia_partnera_i_wroga(self):
        g = _gracz_z_osada(3)
        wiezi.zmien(g, "Osada0", "Osada1", 95)
        wiezi.zmien(g, "Osada0", "Osada2", -90)
        opis = wiezi.opis_relacji(g, "Osada0")
        self.assertIn("Osada1", opis)
        self.assertIn("Osada2", opis)

    def test_samotny_osadnik_ma_opis(self):
        g = _gracz_z_osada(2)
        self.assertEqual(wiezi.opis_relacji(g, "Osada0"), "trzyma się na uboczu")


if __name__ == "__main__":
    unittest.main()
