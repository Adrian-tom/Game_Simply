"""Przetrwanie, osada, obrona, handel, dziedzictwo — reguły, które łatwo zepsuć zmianą liczb."""

import builtins
import contextlib
import io
import random
import unittest

from game import dziedzictwo, handel, kalendarz, mysli, obrona, oboz, osada, przetrwanie, swiat, talenty
from game.player import Gracz


@contextlib.contextmanager
def cicho(odpowiedzi=None):
    """Tłumi print i podaje odpowiedzi do input (domyślnie Enter)."""
    kolejka = list(odpowiedzi or [])
    stary_input = builtins.input
    builtins.input = lambda *_: kolejka.pop(0) if kolejka else ""
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            yield
    finally:
        builtins.input = stary_input


def nowy(klasa="Wojownik"):
    random.seed(1)
    g = Gracz("Test", klasa)
    g.w_obozie = True
    return g


class TestKalendarza(unittest.TestCase):
    def test_pory_roku_po_30_dni(self):
        self.assertEqual(kalendarz.pora(0)["klucz"], "wiosna")
        self.assertEqual(kalendarz.pora(30)["klucz"], "lato")
        self.assertEqual(kalendarz.pora(90)["klucz"], "zima")
        self.assertEqual(kalendarz.pora(120)["klucz"], "wiosna")
        self.assertEqual(kalendarz.rok(120), 2)

    def test_zima_nie_daje_plonow(self):
        self.assertEqual(kalendarz.pora(95)["rolnictwo"], 0.0)
        self.assertTrue(kalendarz.pora(95)["opal"])


class TestPrzetrwania(unittest.TestCase):
    def test_wyprawa_bez_prowiantu_to_glod(self):
        g = nowy()
        g.w_obozie = False
        g.prowiant = 0
        with cicho():
            przetrwanie.dzien_wyprawy(g)
        self.assertEqual(g.glod, 1)
        self.assertLess(przetrwanie.mnoznik_ataku(g), 1.0)

    def test_glod_nie_zabija_poza_walka(self):
        g = nowy()
        g.w_obozie = False
        g.hp = 3
        with cicho():
            for _ in range(10):
                przetrwanie.dzien_wyprawy(g)
        self.assertGreaterEqual(g.hp, 1)

    def test_prowiant_bierze_z_magazynu(self):
        g = nowy()
        g.surowce["zywnosc"] = 20
        przetrwanie.spakuj_prowiant(g)
        self.assertEqual(g.prowiant, przetrwanie.pojemnosc_prowiantu(g))
        self.assertEqual(g.surowce["zywnosc"], 20 - g.prowiant)
        przetrwanie.rozpakuj_prowiant(g)
        self.assertEqual(g.surowce["zywnosc"], 20)

    def test_rana_oslabia_i_sie_goi(self):
        g = nowy()
        przetrwanie.dodaj_rane(g, "zlamana_reka")
        self.assertAlmostEqual(przetrwanie.mnoznik_ataku(g), 0.8)
        przetrwanie.lecz_rany(g, 10)
        self.assertEqual(przetrwanie.rany(g), [])

    def test_rana_oslabia_mikstury(self):
        g = nowy()
        g.hp, g.mikstury = 1, 2
        with cicho():
            g.uzyj_miksture()
        zdrowy = g.hp - 1
        g.hp = 1
        przetrwanie.dodaj_rane(g, "gleboka_rana")
        with cicho():
            g.uzyj_miksture()
        self.assertLess(g.hp - 1, zdrowy)


class TestOsady(unittest.TestCase):
    def test_start_z_dwojgiem_osadnikow(self):
        g = nowy()
        self.assertEqual(len(osada.osadnicy(g)), 2)
        self.assertEqual(osada.wolne_chaty(g), 0)

    def test_osadnicy_jedza_codziennie(self):
        g = nowy()
        for o in osada.osadnicy(g):
            o["zajecie"] = "bezczynny"
        g.surowce["zywnosc"] = 10
        with cicho():
            swiat.minij_dni(g, 1)
        # 2 osadników + bohater w obozie
        self.assertEqual(g.surowce["zywnosc"], 10 - 3 - int(osada.bilans_zywnosci(g) != -2) * 0)

    def test_glod_obniza_morale(self):
        g = nowy()
        for o in osada.osadnicy(g):
            o["zajecie"] = "bezczynny"
        g.surowce["zywnosc"] = 0
        przed = osada.srednie_morale(g)
        with cicho():
            swiat.minij_dni(g, 5)
        self.assertLess(osada.srednie_morale(g), przed)

    def test_bezczynny_odpoczynek_nie_przynosi_zlota(self):
        """Dawny exploit: targ płacił za siedzenie w obozie więcej, niż kosztował odpoczynek."""
        g = nowy()
        g.budynki = {"targ", "dom"}
        g.osadnicy = []
        g.zloto = 100
        with cicho():
            swiat.minij_dni(g, 30)
        self.assertLessEqual(g.zloto, 100 + 30 * 2)  # najwyżej stragany, bez handlarzy nie ma cudów

    def test_tartak_zamienia_drewno_w_deski(self):
        g = nowy()
        g.budynki.add("tartak")
        o = osada.osadnicy(g)[0]
        self.assertIn("zostaje", osada.ustaw_zajecie(g, o, "tracz"))
        g.surowce.update({"drewno": 20, "deski": 0, "zywnosc": 50})
        with cicho():
            swiat.minij_dni(g, 3)
        self.assertGreater(g.surowce["deski"], 0)

    def test_zawod_wymaga_budynku(self):
        g = nowy()
        o = osada.osadnicy(g)[0]
        self.assertIn("Potrzebny", osada.ustaw_zajecie(g, o, "rolnik"))

    def test_zima_bez_drewna_to_zimno(self):
        g = nowy()
        for o in osada.osadnicy(g):
            o["zajecie"] = "bezczynny"
        g.czas = 95
        g.surowce.update({"drewno": 0, "zywnosc": 50})
        with cicho():
            pilne = swiat.minij_dni(g, 1)
        self.assertTrue(any("opał" in m for m in pilne))

    def test_spichlerz_hamuje_psucie(self):
        wyniki = []
        for spichlerz in (False, True):
            g = nowy()
            g.osadnicy, g.w_obozie = [], False
            g.prowiant = 50
            if spichlerz:
                g.poziomy_budynkow = {"spichlerz": 3}
                g.budynki.add("spichlerz")
            g.surowce["zywnosc"] = 200
            with cicho():
                swiat.minij_dni(g, 10)
            wyniki.append(g.surowce["zywnosc"])
        self.assertGreater(wyniki[1], wyniki[0])

    def test_rozbudowa_podnosi_poziom_i_koszt(self):
        g = nowy()
        g.zloto = 10_000
        g.surowce.update({k: 500 for k in oboz.SUROWCE})
        oboz.zbuduj(g, "farma")
        koszt2 = oboz.koszt_budowy(g, "farma")
        oboz.zbuduj(g, "farma")
        self.assertEqual(oboz.poziom_budynku(g, "farma"), 2)
        self.assertIn("deski", koszt2)
        self.assertIsNotNone(oboz.koszt_budowy(g, "farma"))
        oboz.zbuduj(g, "farma")
        self.assertIsNone(oboz.koszt_budowy(g, "farma"))


class TestObrony(unittest.TestCase):
    def test_palisada_i_straz_dodaja_obrony(self):
        g = nowy()
        zero = obrona.sila_obrony(g)
        g.budynki.add("palisada")
        osada.osadnicy(g)[0]["zajecie"] = "straznik"
        self.assertGreater(obrona.sila_obrony(g), zero + 18)

    def test_zagrozenie_prowadzi_do_zapowiedzi_i_najazdu(self):
        g = nowy()
        g.w_obozie = False
        g.surowce["zywnosc"] = 500
        with cicho():
            swiat.minij_dni(g, 120)
        self.assertGreaterEqual(g.najazdy, 1)

    def test_najazd_pod_nieobecnosc_rozstrzyga_sie_sam(self):
        g = nowy()
        g.w_obozie = False
        g.flagi["sila_najazdu"] = 500
        g.surowce["drewno"] = 100
        with cicho():
            obrona.najazd(g)
        self.assertLess(g.surowce["drewno"], 100)
        self.assertIsNone(g.najazd_za)


class TestHandlu(unittest.TestCase):
    def test_karawana_wraca_ze_zlotem(self):
        g = nowy()
        g.zloto = 0
        g.karawany = [{"cel": "brzezie", "nazwa": "Brzezie", "dni": 3, "zostalo": 1, "eskorta": 0,
                       "wartosc": 100, "ryzyko": 0.0, "zakup": None, "po_zasadzce": True}]
        handel.dzien_handlu(g)
        self.assertEqual(g.zloto, 100)
        self.assertEqual(g.karawany, [])

    def test_specjalnosci_osad(self):
        g = nowy()
        self.assertGreater(handel.cena(g, "kamienny_brod", "zywnosc"), handel.cena(g, "brzezie", "zywnosc"))

    def test_eskorta_zmniejsza_ryzyko(self):
        g = nowy()
        self.assertLess(handel.ryzyko(g, 3, 3), handel.ryzyko(g, 3, 0))


class TestTalentowIMysli(unittest.TestCase):
    def test_drzewko_wymaga_kolejnosci(self):
        g = nowy()
        g.punkty_talentow = 5
        self.assertFalse(talenty.dostepny(g, "kontra")[0])
        talenty.wykup(g, "twarda_skora")
        self.assertTrue(talenty.dostepny(g, "kontra")[0])

    def test_awans_daje_punkt_talentu(self):
        g = nowy()
        with cicho():
            g.zdobadz_exp(100)
        self.assertEqual(g.punkty_talentow, 1)

    def test_mysl_przyswaja_sie_z_czasem(self):
        g = nowy()
        mysli.odkryj(g, "cena_krwi")
        g.mysli["cena_krwi"].update(stan="w_toku", dni=2)
        self.assertEqual(mysli.premia_testu(g, "zastraszanie"), -1)
        with cicho():
            mysli.dzien_mysli(g)
            mysli.dzien_mysli(g)
        self.assertTrue(mysli.przyswojona(g, "cena_krwi"))
        self.assertEqual(mysli.premia_testu(g, "perswazja"), 2)


class TestDziedzictwa(unittest.TestCase):
    def test_omdlenie_zamiast_smierci_w_normalnym(self):
        g = nowy()
        g.hp, g.zloto = 0, 100
        with cicho():
            wynik = dziedzictwo.po_smierci(g)
        self.assertIs(wynik, g)
        self.assertGreater(g.hp, 0)
        self.assertLess(g.zloto, 100)
        self.assertTrue(przetrwanie.ma_rane(g, "ciezka_rana"))

    def test_hardcore_przekazuje_osade_dziedzicowi(self):
        g = nowy()
        g.tryb_trudnosci = "hardcore"
        g.budynki.add("farma")
        g.zloto = 200
        with cicho(["1"]):
            dziedzic = dziedzictwo.po_smierci(g)
        self.assertIsNotNone(dziedzic)
        self.assertIsNot(dziedzic, g)
        self.assertIn("farma", dziedzic.budynki)
        self.assertEqual(dziedzic.pokolenie, 2)
        self.assertEqual(len(dziedzic.osadnicy), 1)  # dziedzic odszedł z chaty do domu wodza
        self.assertEqual(dziedzic.zloto, 100)

    def test_bez_nikogo_rod_wygasa(self):
        g = nowy()
        g.tryb_trudnosci = "hardcore"
        g.osadnicy, g.rekruci = [], []
        with cicho():
            self.assertIsNone(dziedzictwo.po_smierci(g))


class TestWydarzen(unittest.TestCase):
    def test_czysta_studnia_chroni_przed_zaraza(self):
        from game.wydarzenia import _zaraza
        g = nowy()
        g.flagi["czysta_studnia"] = True
        _zaraza(g)
        self.assertFalse(any(o.get("chory") for o in osada.osadnicy(g)))

    def test_palisada_zatrzymuje_wilki(self):
        from game.wydarzenia import _wilki
        g = nowy()
        g.budynki.add("palisada")
        g.surowce["zywnosc"] = 20
        _wilki(g)
        self.assertEqual(g.surowce["zywnosc"], 20)

    def test_wydarzenie_raz_na_pore(self):
        from game import wydarzenia
        g = nowy()
        stara = wydarzenia.SZANSA_DZIENNA
        wydarzenia.SZANSA_DZIENNA = 1.0
        try:
            razem = sum(len(wydarzenia.dzien_wydarzen(g)) for _ in range(20))
        finally:
            wydarzenia.SZANSA_DZIENNA = stara
        self.assertLessEqual(razem, 2)


if __name__ == "__main__":
    unittest.main()
