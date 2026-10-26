"""Budowa w czasie: plany, budowniczowie, pomoc bohatera, zwrot przy anulowaniu, bramki badań."""
import builtins
import contextlib
import io
import random
import unittest

from game import badania, budowa, kompetencje, oboz, osada, swiat
from game.player import Gracz


@contextlib.contextmanager
def cicho(odpowiedzi=None):
    kolejka = list(odpowiedzi or [])
    stary = builtins.input
    builtins.input = lambda *_: kolejka.pop(0) if kolejka else ""
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            yield
    finally:
        builtins.input = stary


def nowy():
    random.seed(7)
    g = Gracz("Test", "Wojownik")
    g.w_obozie = True
    g.zloto = 500
    g.surowce.update({k: 200 for k in oboz.SUROWCE})
    return g


def budowniczy(g, imie="Bud", poziom=4, morale=60.0):
    """Dodaje budowniczego o znanym mnożniku: umiejętność 4 (×1,0), cecha bez wpływu na pracę."""
    o = osada._zapewnij_osadnika({"imie": imie, "zajecie": "budowniczy", "morale": morale, "cecha": "skromny"})
    o["kompetencje"]["budowa"].update({"p": poziom, "xp": 0.0, "pasja": 0})
    osada.osadnicy(g).append(o)
    return o


def tylko_budowniczy(g, ile=1):
    g.osadnicy = []
    return [budowniczy(g, f"B{i}") for i in range(ile)]


class TestZlecania(unittest.TestCase):
    def test_bez_badan_nie_zlecisz_tartaku(self):
        g = nowy()
        zloto = g.zloto
        tekst = budowa.zlec(g, "tartak")
        self.assertIn("poznaj: Obróbka drewna", tekst)
        self.assertEqual(budowa.plany(g), [])
        self.assertEqual(g.zloto, zloto)

    def test_zlecenie_pobiera_material_od_razu(self):
        g = nowy()
        koszt = oboz.koszt_budowy(g, "dom")
        drewno, zloto = g.surowce["drewno"], g.zloto
        self.assertIn("Zlecono budowę", budowa.zlec(g, "dom"))
        self.assertEqual(g.surowce["drewno"], drewno - koszt["drewno"])
        self.assertEqual(g.zloto, zloto - koszt["zloto"])
        self.assertEqual(oboz.poziom_budynku(g, "dom"), 0)       # jeszcze nie stoi
        self.assertEqual(len(budowa.plany(g)), 1)
        self.assertEqual(budowa.plany(g)[0]["wymagana"], 3.0)

    def test_brak_materialow_nie_zaklada_planu(self):
        g = nowy()
        g.zloto = 0
        self.assertIn("Brakuje materiałów", budowa.zlec(g, "dom"))
        self.assertEqual(budowa.plany(g), [])

    def test_ten_sam_budynek_nie_moze_byc_w_budowie_dwa_razy(self):
        g = nowy()
        budowa.zlec(g, "dom")
        self.assertIn("rusztowaniach", budowa.zlec(g, "dom"))
        self.assertEqual(len(budowa.plany(g)), 1)

    def test_kolejka_ma_limit(self):
        g = nowy()
        badania.migruj_stary_zapis(g)
        for klucz in ("dom", "farma", "palisada", "tartak"):
            self.assertIn("Zlecono", budowa.zlec(g, klucz))
        self.assertIn("Kolejka budów jest pełna", budowa.zlec(g, "huta"))

    def test_wyzszy_poziom_wymaga_badan(self):
        g = nowy()
        oboz.ustaw_poziom(g, "farma", 1)
        self.assertIn("poznaj: Płodozmian", budowa.zlec(g, "farma"))
        badania.stan(g)["zrobione"].append("plodozmian")
        self.assertIn("Zlecono", budowa.zlec(g, "farma"))
        self.assertEqual(budowa.plany(g)[0]["poziom"], 2)
        self.assertEqual(budowa.plany(g)[0]["wymagana"], 4.0)    # 2 dni × poziom 2

    def test_chaty_ogranicza_badanie_osadnictwa(self):
        g = nowy()
        g.chaty = 4
        self.assertIn("limit chat", budowa.zlec(g, "chata"))
        badania.stan(g)["zrobione"].append("osadnictwo")          # limit 8
        g.chaty = 7
        self.assertIn("Zlecono", budowa.zlec(g, "chata"))
        self.assertIn("limit chat", budowa.zlec(g, "chata"))      # siódma stoi, ósma w planie
        self.assertEqual(sum(1 for p in budowa.plany(g) if p["klucz"] == "chata"), 1)

    def test_chata_w_planie_liczy_sie_do_limitu(self):
        g = nowy()
        g.chaty = 3
        budowa.zlec(g, "chata")
        self.assertIn("limit chat", budowa.zlec(g, "chata"))


class TestPracy(unittest.TestCase):
    def test_budowniczy_konczy_palisade_w_dwa_dni(self):
        g = nowy()
        tylko_budowniczy(g)
        budowa.zlec(g, "palisada")                    # 2 dni pracy, mnożnik ok. 1,08
        k, pilne = budowa.dzien_budowy(g)
        self.assertEqual(pilne, [])
        self.assertEqual(oboz.poziom_budynku(g, "palisada"), 0)
        k, pilne = budowa.dzien_budowy(g)
        self.assertTrue(any("UKOŃCZONO" in m for m in pilne), pilne)
        self.assertEqual(oboz.poziom_budynku(g, "palisada"), 1)
        self.assertEqual(budowa.plany(g), [])
        self.assertEqual(g.statystyki["zbudowane_budynki"], 1)

    def test_plan_przyjmuje_dziennie_nie_wiecej_niz_limit(self):
        g = nowy()
        tylko_budowniczy(g, 10)
        budowa.zlec(g, "dom")
        budowa.dzien_budowy(g)
        self.assertLessEqual(budowa.plany(g)[0]["zrobiona"], budowa.LIMIT_PLANU + 1e-9)

    def test_nadmiar_mocy_przechodzi_na_kolejny_plan(self):
        g = nowy()
        tylko_budowniczy(g, 4)
        budowa.zlec(g, "dom")
        budowa.zlec(g, "farma")
        budowa.dzien_budowy(g)
        pierwszy, drugi = budowa.plany(g)
        self.assertAlmostEqual(pierwszy["zrobiona"], budowa.LIMIT_PLANU)
        self.assertGreater(drugi["zrobiona"], 0)

    def test_wagi_pracownikow(self):
        g = nowy()
        g.osadnicy = []
        b = budowniczy(g, "Bud")
        bezczynny = budowniczy(g, "Leń")
        bezczynny["zajecie"] = "bezczynny"
        inny = budowniczy(g, "Rolnik")
        inny["zajecie"] = "drwal"
        moce = {o["imie"]: m for o, m in budowa.robotnicy(g)}
        self.assertAlmostEqual(moce["Leń"] / moce["Bud"], budowa.WAGA_BEZCZYNNEGO)
        self.assertAlmostEqual(moce["Rolnik"] / moce["Bud"], budowa.WAGA_POZOSTALYCH, places=2)

    def test_chory_nie_buduje(self):
        g = nowy()
        b, = tylko_budowniczy(g)
        b["chory"] = 3
        self.assertEqual(budowa.moc_budowy(g), 0)
        budowa.zlec(g, "dom")
        budowa.dzien_budowy(g)
        self.assertEqual(budowa.plany(g)[0]["zrobiona"], 0)

    def test_umiejetnosc_przyspiesza_budowe(self):
        moce = []
        for poziom in (0, 16):
            g = nowy()
            g.osadnicy = []
            budowniczy(g, poziom=poziom)
            moce.append(budowa.moc_budowy(g))
        self.assertGreater(moce[1], moce[0] * 1.9)

    def test_budowniczy_i_pomocnik_ucza_sie_budowy(self):
        g = nowy()
        b, = tylko_budowniczy(g)
        leń = budowniczy(g, "Leń")
        leń["zajecie"] = "bezczynny"
        budowa.zlec(g, "dom")
        for _ in range(3):
            budowa.dzien_budowy(g)
        xp = lambda o: o["kompetencje"]["budowa"]["xp"]
        self.assertGreater(xp(b), 0)
        self.assertGreater(xp(leń), 0)
        self.assertGreater(xp(b), xp(leń))

    def test_bez_planow_nikt_sie_nie_uczy(self):
        g = nowy()
        b, = tylko_budowniczy(g)
        budowa.dzien_budowy(g)
        self.assertEqual(b["kompetencje"]["budowa"]["xp"], 0.0)

    def test_eta_zgodna_z_symulacja(self):
        g = nowy()
        tylko_budowniczy(g, 2)
        budowa.zlec(g, "dom")
        oczekiwane = budowa.dni_do_konca(g, budowa.plany(g)[0])
        dni = 0
        while budowa.plany(g) and dni < 30:
            budowa.dzien_budowy(g)
            dni += 1
        self.assertEqual(dni, oczekiwane)

    def test_brak_ludzi_to_brak_eta(self):
        g = nowy()
        g.osadnicy = []
        budowa.zlec(g, "dom")
        self.assertIsNone(budowa.dni_do_konca(g, budowa.plany(g)[0]))


class TestAnulowania(unittest.TestCase):
    def test_zwrot_calosci_przed_rozpoczeciem(self):
        g = nowy()
        przed = (g.zloto, dict(g.surowce))
        budowa.zlec(g, "dom")
        self.assertIn("Zwrot 100%", budowa.anuluj(g, budowa.plany(g)[0]["id"]))
        self.assertEqual((g.zloto, g.surowce), przed)
        self.assertEqual(budowa.plany(g), [])

    def test_zwrot_maleje_z_postepem_ale_nie_ponizej_polowy(self):
        g = nowy()
        budowa.zlec(g, "dom")
        plan = budowa.plany(g)[0]
        plan["zrobiona"] = plan["wymagana"]              # w praktyce gotowy plan znika, to granica
        zloto = g.zloto
        tekst = budowa.anuluj(g, plan["id"])
        self.assertIn("Zwrot 50%", tekst)
        self.assertEqual(g.zloto - zloto, plan["koszt"]["zloto"] // 2)

    def test_anulowanie_nieistniejacego_planu(self):
        self.assertIn("Nie ma takiego planu", budowa.anuluj(nowy(), 99))


class TestBohatera(unittest.TestCase):
    def test_bohater_przyspiesza_budowe_i_mija_dzien(self):
        g = nowy()
        g.osadnicy = []
        budowa.zlec(g, "dom")
        czas = g.czas
        with cicho():
            tekst = budowa.pomoz(g, budowa.plany(g)[0]["id"])
        self.assertIn("Cały dzień przy budowie", tekst)
        self.assertEqual(g.czas, czas + 1)
        self.assertGreaterEqual(budowa.plany(g)[0]["zrobiona"], budowa.PRACA_BOHATERA)
        self.assertEqual(g.statystyki["dni_budowy"], 1)

    def test_sam_bohater_stawia_dom_w_dwa_dni(self):
        g = nowy()
        g.osadnicy = []
        budowa.zlec(g, "dom")                       # 3 dni pracy, bohater daje mniej niż 3 na dzień
        self.assertLess(budowa.praca_bohatera(g), 3.0)
        id_planu = budowa.plany(g)[0]["id"]
        with cicho():
            budowa.pomoz(g, id_planu)
            self.assertEqual(oboz.poziom_budynku(g, "dom"), 0)
            budowa.pomoz(g, id_planu)
        self.assertEqual(oboz.poziom_budynku(g, "dom"), 1)

    def test_poza_obozem_nie_pomaga(self):
        g = nowy()
        budowa.zlec(g, "dom")
        g.w_obozie = False
        czas = g.czas
        self.assertIn("tylko w obozie", budowa.pomoz(g, budowa.plany(g)[0]["id"]))
        self.assertEqual(g.czas, czas)


class TestIntegracjiZDniemOsady(unittest.TestCase):
    def test_budowa_konczy_sie_w_cyklu_swiata_i_trafia_do_pilnych(self):
        g = nowy()
        tylko_budowniczy(g, 2)
        g.surowce["zywnosc"] = 200
        budowa.zlec(g, "dom")
        with cicho():
            pilne = swiat.minij_dni(g, 4)
        self.assertEqual(oboz.poziom_budynku(g, "dom"), 1)
        self.assertTrue(any("UKOŃCZONO" in m for m in pilne), pilne)

    def test_ukonczona_chata_zwieksza_liczbe_chat(self):
        g = nowy()
        tylko_budowniczy(g, 2)
        g.surowce["zywnosc"] = 200
        przed = g.chaty
        budowa.zlec(g, "chata")
        with cicho():
            swiat.minij_dni(g, 3)
        self.assertEqual(g.chaty, przed + 1)

    def test_budowniczy_nie_produkuje_surowcow_ale_je(self):
        g = nowy()
        tylko_budowniczy(g)
        g.surowce["zywnosc"] = 10
        drewno = g.surowce["drewno"]
        with cicho():
            swiat.minij_dni(g, 1)
        self.assertEqual(g.surowce["drewno"], drewno)
        self.assertLess(g.surowce["zywnosc"], 10)

    def test_praca_uczy_umiejetnosci_zawodu_i_pasja_poprawia_humor(self):
        g = nowy()
        rolnik = osada.osadnicy(g)[0]
        rolnik["zajecie"] = "drwal"
        u = kompetencje.umiejetnosc_zajecia("drwal")
        rolnik["kompetencje"][u].update({"p": 2, "xp": 0.0, "pasja": 0})
        with cicho():
            swiat.minij_dni(g, 3)
        w = rolnik["kompetencje"][u]
        self.assertGreater(w["xp"] + 100 * w["p"], 0)
        bez = osada._cel_morale(g, rolnik, False, False, False)
        w["pasja"] = 2
        self.assertAlmostEqual(osada._cel_morale(g, rolnik, False, False, False) - bez, 8.0)

    def test_stary_osadnik_z_gwiazdkami_dziala_w_petli(self):
        g = nowy()
        g.osadnicy = [{"imie": "Stary", "zajecie": "drwal", "morale": 60.0, "cecha": "pracowity",
                       "dosw": {"drwal": 70}, "chory": 0}]
        with cicho():
            swiat.minij_dni(g, 2)
        self.assertNotIn("dosw", g.osadnicy[0])
        self.assertGreaterEqual(g.osadnicy[0]["kompetencje"]["zbieractwo"]["p"], 9)


class TestMenuIWidoku(unittest.TestCase):
    def test_menu_zlecenie_i_wyjscie(self):
        g = nowy()
        with cicho(["1", "", "0"]):
            budowa.menu_budowy(g)
        self.assertEqual(len(budowa.plany(g)), 1)

    def test_menu_anuluje_i_pomaga(self):
        g = nowy()
        tylko_budowniczy(g)
        budowa.zlec(g, "dom")
        with cicho(["p", "", "a", "", "0"]):
            budowa.menu_budowy(g)
        self.assertEqual(budowa.plany(g), [])

    def test_menu_pokazuje_zamkniete_budowle(self):
        g = nowy()
        buf = io.StringIO()
        stary = builtins.input
        builtins.input = lambda *_: "0"
        try:
            with contextlib.redirect_stdout(buf):
                budowa.menu_budowy(g)
        finally:
            builtins.input = stary
        self.assertIn("NIEZNANE", buf.getvalue())
        self.assertIn("Tartak ← Obróbka drewna", buf.getvalue())

    def test_menu_obozu_wola_nowe_menu(self):
        g = nowy()
        with cicho(["0"]):
            oboz.menu_rozbudowy(g)

    def test_rusztowania_dla_widoku(self):
        g = nowy()
        budowa.zlec(g, "dom")
        budowa.plany(g)[0]["zrobiona"] = 1.5
        self.assertEqual(budowa.rusztowania(g), [("dom", 1, 0.5)])

    def test_doradca_zauwaza_stojaca_budowe(self):
        from game import doradca
        g = nowy()
        g.osadnicy = []
        budowa.zlec(g, "dom")
        self.assertTrue(any("Budowa stoi" in p for p in doradca.porady(g, 10)))


if __name__ == "__main__":
    unittest.main()
