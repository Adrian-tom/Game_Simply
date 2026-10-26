"""Umiejętności 0–20 i pasje osadników: losowanie, nauka przez pracę, wpływ na wydajność."""
import random
import unittest

from game import kompetencje, osada
from game.player import Gracz


def _osadnik(imie="Test", zajecie="rolnik", poziom=4, pasja=0, morale=60.0, cecha="skromny"):
    """Osadnik z ustawioną umiejętnością zajęcia — reszta z losowania po imieniu."""
    o = osada._zapewnij_osadnika({"imie": imie, "zajecie": zajecie, "morale": morale, "cecha": cecha})
    u = kompetencje.umiejetnosc_zajecia(zajecie)
    if u:
        o["kompetencje"][u].update({"p": poziom, "xp": 0.0, "pasja": pasja})
    return o


class TestLosowania(unittest.TestCase):
    def test_to_samo_imie_daje_te_same_predyspozycje(self):
        self.assertEqual(kompetencje.generuj("Nessa"), kompetencje.generuj("Nessa"))
        self.assertNotEqual(kompetencje.generuj("Nessa"), kompetencje.generuj("Torin"))

    def test_zakresy_i_pasje(self):
        imiona = ("Nessa", "Torin", "Elka", "Bram", "Sira", "Olek", "Mila", "Gareth", "Iva", "Piotr")
        for imie in imiona:
            k = kompetencje.generuj(imie)
            self.assertEqual(set(k), set(kompetencje.UMIEJETNOSCI))
            for w in k.values():
                self.assertTrue(0 <= w["p"] <= 12, (imie, w))
                self.assertIn(w["pasja"], (0, 1, 2))
            self.assertGreaterEqual(sum(1 for w in k.values() if w["pasja"]), 1, imie)

    def test_losowanie_nie_zjada_globalnej_losowosci(self):
        random.seed(5)
        oczekiwane = random.random()
        random.seed(5)
        kompetencje.generuj("Nessa")
        self.assertEqual(random.random(), oczekiwane)


class TestKrzywejXp(unittest.TestCase):
    def test_krzywa_ma_zalozone_punkty(self):
        self.assertEqual(kompetencje.xp_do_nastepnego(0), 40)
        self.assertEqual(kompetencje.xp_do_nastepnego(9), 400)
        self.assertEqual(kompetencje.xp_do_nastepnego(19), 1200)
        kolejne = [kompetencje.xp_do_nastepnego(p) for p in range(20)]
        self.assertEqual(kolejne, sorted(kolejne))

    def test_tytuly(self):
        self.assertEqual(kompetencje.tytul(0), "nowicjusz")
        self.assertEqual(kompetencje.tytul(8), "czeladnik")
        self.assertEqual(kompetencje.tytul(20), "arcymistrz")


class TestNauki(unittest.TestCase):
    def test_dzien_pracy_daje_xp_zalezne_od_pasji(self):
        xp = []
        for pasja in (0, 1, 2):
            o = _osadnik(pasja=pasja)
            kompetencje.cwicz(o, "rolnik")
            xp.append(o["kompetencje"]["uprawa"]["xp"] + sum(
                kompetencje.xp_do_nastepnego(p) for p in range(4, o["kompetencje"]["uprawa"]["p"])))
        self.assertAlmostEqual(xp[0], 21.0)
        self.assertAlmostEqual(xp[1], 60.0)
        self.assertAlmostEqual(xp[2], 90.0)

    def test_awans_zglasza_nowy_poziom(self):
        o = _osadnik(poziom=0, pasja=2)
        wiesci = []
        for _ in range(3):
            wiesci += kompetencje.cwicz(o, "rolnik")
        self.assertGreaterEqual(o["kompetencje"]["uprawa"]["p"], 1)
        self.assertTrue(any("uprawa 1" in w for w in wiesci), wiesci)

    def test_pasja_przyspiesza_dojscie_do_poziomu(self):
        poziomy = []
        for pasja in (0, 2):
            o = _osadnik(poziom=0, pasja=pasja)
            for _ in range(60):
                kompetencje.cwicz(o, "rolnik")
            poziomy.append(o["kompetencje"]["uprawa"]["p"])
        self.assertGreater(poziomy[1], poziomy[0] + 2)

    def test_maksimum_i_brak_nauki_bez_zajecia(self):
        o = _osadnik(poziom=kompetencje.MAX_POZIOM)
        self.assertEqual(kompetencje.cwicz(o, "rolnik"), [])
        self.assertEqual(o["kompetencje"]["uprawa"]["p"], kompetencje.MAX_POZIOM)
        bezczynny = _osadnik(zajecie="bezczynny")
        self.assertEqual(kompetencje.cwicz(bezczynny), [])


class TestWplywu(unittest.TestCase):
    def test_czynnik_od_poziomu(self):
        self.assertAlmostEqual(kompetencje.czynnik(_osadnik(poziom=0)), 0.8)
        self.assertAlmostEqual(kompetencje.czynnik(_osadnik(poziom=4)), 1.0)
        self.assertAlmostEqual(kompetencje.czynnik(_osadnik(poziom=20)), 1.8)
        self.assertEqual(kompetencje.czynnik(_osadnik(zajecie="bezczynny")), 1.0)

    def test_mistrz_pracuje_szybciej_niz_nowicjusz(self):
        g = Gracz("Test", "Wojownik")
        slaby, mistrz = _osadnik(poziom=0), _osadnik(poziom=16)
        self.assertGreater(osada.mnoznik_pracy(g, mistrz), osada.mnoznik_pracy(g, slaby) * 1.8)

    def test_pasja_podnosi_morale_tylko_przy_swojej_pracy(self):
        self.assertEqual(kompetencje.premia_morale(_osadnik(pasja=2)), 8.0)
        self.assertEqual(kompetencje.premia_morale(_osadnik(pasja=1)), 4.0)
        self.assertEqual(kompetencje.premia_morale(_osadnik(pasja=0)), 0.0)
        chory = _osadnik(pasja=2)
        chory["chory"] = 3
        self.assertEqual(kompetencje.premia_morale(chory), 0.0)
        o = _osadnik(pasja=2)
        o["zajecie"] = "bezczynny"
        self.assertEqual(kompetencje.premia_morale(o), 0.0)


class TestMigracji(unittest.TestCase):
    def test_stare_gwiazdki_przechodza_w_poziomy(self):
        o = {"imie": "Stary", "zajecie": "rolnik", "dosw": {"rolnik": 65, "drwal": 12}}
        k = kompetencje.zapewnij(o)
        self.assertGreaterEqual(k["uprawa"]["p"], 9)        # ★★★
        self.assertGreaterEqual(k["zbieractwo"]["p"], 3)    # ★
        self.assertNotIn("dosw", o)

    def test_zapewnij_nie_nadpisuje_istniejacych(self):
        o = _osadnik(poziom=7, pasja=2)
        kompetencje.zapewnij(o)
        self.assertEqual(o["kompetencje"]["uprawa"]["p"], 7)

    def test_nowa_umiejetnosc_w_starym_zapisie(self):
        o = {"imie": "X", "zajecie": "drwal", "kompetencje": {"uprawa": {"p": 3, "xp": 0.0, "pasja": 0}}}
        k = kompetencje.zapewnij(o)
        self.assertEqual(set(k), set(kompetencje.UMIEJETNOSCI))
        self.assertEqual(k["uprawa"]["p"], 3)

    def test_opis_pokazuje_umiejetnosc_zajecia_nawet_gdy_slaba(self):
        o = _osadnik(zajecie="handlarz", poziom=0)
        linie = kompetencje.opis_pelny(o, ile=2, zajecie="handlarz")
        self.assertTrue(any("Handel" in l and "▶" in l for l in linie), linie)


if __name__ == "__main__":
    unittest.main()
