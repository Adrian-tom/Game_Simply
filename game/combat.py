"""Moduł obsługujący system walki turowej."""

import random

from game.player import Gracz
from game.enemy import Przeciwnik, losuj_przeciwnika, losuj_bossa
from game.skills import (
    UMIEJETNOSCI,
    ranga_skilla,
    skaluj_wartosc,
    cd_skilla,
    czas_trwania,
)
from game.quests import sprawdz_questy
from game.items import EKWIPUNEK, dodaj_do_plecaka
from game.utils import wyczysc, nacisnij_enter, wyswietl_linie
from game.rekruci import tura_towarzysza, etykieta_towarzysza


# ------------------------------------------------------------------ #
#  Obliczenia obrażeń                                                  #
# ------------------------------------------------------------------ #

def _oblicz_obrazenia(atak: int, obrona: int) -> int:
    """Oblicza zadane obrażenia z losową wariancją ±20%."""
    bazowe = max(1, atak - obrona)
    wariancja = max(1, int(bazowe * 0.2))
    return random.randint(max(1, bazowe - wariancja), bazowe + wariancja)


# Szanse na krytyczne trafienie per klasa
_SZANSA_KRYT: dict[str, float] = {
    "Wojownik": 0.10,
    "Mag": 0.08,
    "Lotrzyk": 0.22,
    "Druid": 0.06,
    "Nekromanta": 0.12,
}


def _atak_z_krytem(
    atak: int, obrona: int, klasa: str, stan: dict, gracz=None
) -> tuple[int, bool]:
    """
    Oblicza obrażenia z szansą na krytyczne trafienie.
    Zwraca (obrazenia, czy_krit).
    """
    bazowe = _oblicz_obrazenia(atak, obrona)
    szansa = _SZANSA_KRYT.get(klasa, 0.10)
    # Lotrzyk dostaje +10% szansy na krit gdy buff ataku aktywny
    if klasa == "Lotrzyk" and stan["buff_atak_tury"] > 0:
        szansa += 0.10
    szansa += float(stan.get("forma_kryt") or 0)
    if gracz is not None:
        from game.atrybuty import szansa_kryta_zrecznosc
        szansa += szansa_kryta_zrecznosc(gracz)
    if random.random() < szansa:
        return int(bazowe * 2), True
    return bazowe, False


# ------------------------------------------------------------------ #
#  Stan walki (buffy, debuffs, efekty)                                #
# ------------------------------------------------------------------ #

def _nowy_stan_walki() -> dict:
    """Zwraca zainicjalizowany słownik stanu walki."""
    return {
        "tura": 1,
        # Buffs gracza
        "buff_atak_mnoznik": 1.0,
        "buff_atak_tury": 0,
        "buff_obrona_mnoznik": 1.0,
        "buff_obrona_tury": 0,
        "brak_obrony_tura": False,
        "leczenie_zablokowane": False,
        "tarcza_runowa": 0,
        "unik_aktywny": False,
        "unik_szansa": 0.75,
        "przyspieszenie": False,
        "regeneracja_hp": 0,
        "regeneracja_tury": 0,
        "nastepny_atak_mnoznik": 1.0,
        "lich_ochrona": False,
        "lich_ochrona_hp": 40,
        # Debuffs wroga
        "wrog_ogluszone_tury": 0,
        "wrog_trucizna_tury": 0,
        "wrog_trucizna_obrazenia": 10,
        "wrog_oslabienie_tury": 0,
        "wrog_rozpad": False,
        # Statusy gracza
        "gracz_trucizna_tury": 0,
        "gracz_trucizna_obrazenia": 8,
        "gracz_krwawienie_tury": 0,
        "gracz_krwawienie_obrazenia": 6,
        "gracz_ogluszone_tury": 0,
        # Flaga bossa
        "jest_boss": False,
        "cd": {},
        "przyzwanie": None,
        "forma": None,
        "forma_tury": 0,
        "forma_atak": 1.0,
        "forma_obrona": 1.0,
        "forma_kryt": 0.0,
        "forma_unik": 0.0,
        "forma_regen": 0,
        "forma_mana": 0,
        "tarcza_losu_uzyta": False,
    }


def _wyczysc_forme(stan: dict) -> None:
    stan["forma"] = None
    stan["forma_tury"] = 0
    stan["forma_atak"] = 1.0
    stan["forma_obrona"] = 1.0
    stan["forma_kryt"] = 0.0
    stan["forma_unik"] = 0.0
    stan["forma_regen"] = 0
    stan["forma_mana"] = 0


def _ustaw_forme(stan: dict, nazwa: str, tury: int, **efekty) -> None:
    _wyczysc_forme(stan)
    stan["forma"] = nazwa
    stan["forma_tury"] = tury
    for k, v in efekty.items():
        stan[k] = v


def _ustaw_przyzwanie(
    stan: dict,
    nazwa: str,
    ikona: str,
    hp: int,
    atak: int,
    przejecie: float = 0.5,
    **extra,
) -> None:
    poprzednie = stan.get("przyzwanie")
    if poprzednie:
        print(f"  Poprzednie przyzwanie ({poprzednie['nazwa']}) ustępuje nowemu.")
    stan["przyzwanie"] = {
        "nazwa": nazwa,
        "ikona": ikona,
        "hp": hp,
        "max_hp": hp,
        "atak": atak,
        "przejecie": przejecie,
        **extra,
    }


def _tura_przyzwania(stan: dict, przeciwnik: Przeciwnik) -> str | None:
    sluga = stan.get("przyzwanie")
    if not sluga or sluga["hp"] <= 0 or not przeciwnik.zyje():
        return None
    atak = sluga["atak"]
    obrona = przeciwnik.obrona
    przebicie = float(sluga.get("przebicie") or 0)
    if przebicie:
        obrona = max(0, int(obrona * (1.0 - przebicie)))
    obrazenia = _oblicz_obrazenia(atak, obrona)
    przeciwnik.hp -= obrazenia
    print(
        f"  {sluga['ikona']}  {sluga['nazwa']} atakuje {przeciwnik.nazwa}"
        f" za {obrazenia} obrażeń!"
    )
    if not przeciwnik.zyje():
        return "wygrana"
    return None


def _przejmij_obrazenia_sluga(stan: dict, obrazenia: int, nazwa_wroga: str) -> int:
    """Sługa może przejąć część ciosu. Zwraca obrażenia, które idą w gracza."""
    sluga = stan.get("przyzwanie")
    if not sluga or sluga["hp"] <= 0 or obrazenia <= 0:
        return obrazenia
    if random.random() > float(sluga.get("przejecie", 0.5)):
        return obrazenia
    absorb = min(sluga["hp"], obrazenia)
    sluga["hp"] -= absorb
    print(
        f"  {sluga['ikona']}  {sluga['nazwa']} przejmuje {absorb} obrażeń"
        f" zamiast ciebie!"
    )
    if sluga["hp"] <= 0:
        print(f"  {sluga['nazwa']} rozpada się w pył!")
        stan["przyzwanie"] = None
    return max(0, obrazenia - absorb)


def _sprobuj_ocalic(gracz: Gracz, stan: dict) -> bool:
    """Ochrona Licha albo Tarcza losu. True jeśli śmierć została anulowana."""
    if gracz.zyje():
        return False
    if stan.get("lich_ochrona"):
        stan["lich_ochrona"] = False
        gracz.hp = stan.get("lich_ochrona_hp", 40)
        print(
            f"  💀  Ochrona Licha zadziałała! Zamiast umrzeć,"
            f" odnawiasz {gracz.hp} HP!"
        )
        return True
    from game.pochodzenie import ma_tarczę_losu
    if ma_tarczę_losu(gracz) and not stan.get("tarcza_losu_uzyta"):
        stan["tarcza_losu_uzyta"] = True
        gracz.hp = 1
        print("  🛡  Tarcza losu! Zamiast umrzeć, zostajesz z 1 HP.")
        return True
    return False


def _koniec_rundy(stan: dict, gracz: Gracz) -> None:
    """Dekrementuje tury aktywnych buffów gracza po każdej pełnej rundzie."""
    if stan["buff_atak_tury"] > 0:
        stan["buff_atak_tury"] -= 1
        if stan["buff_atak_tury"] == 0:
            stan["buff_atak_mnoznik"] = 1.0
            if stan["leczenie_zablokowane"]:
                stan["leczenie_zablokowane"] = False
                print("  Szał berserka minął. Możesz znów się leczyć.")

    # Druid: regeneracja HP
    if stan["regeneracja_tury"] > 0:
        wyleczone = min(stan["regeneracja_hp"], gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        stan["regeneracja_tury"] -= 1
        print(
            f"  🌱  Regeneracja! Odnawiasz {wyleczone} HP."
            f" (Pozostało tur: {stan['regeneracja_tury']})"
        )

    if stan.get("forma") and stan.get("forma_tury", 0) > 0:
        if stan.get("forma_regen", 0) > 0:
            wyleczone = min(stan["forma_regen"], gracz.max_hp - gracz.hp)
            if wyleczone:
                gracz.hp += wyleczone
                print(f"  Forma regeneruje {wyleczone} HP.")
        if stan.get("forma_mana", 0) > 0 and gracz.max_mana > 0:
            odzysk = min(stan["forma_mana"], gracz.max_mana - gracz.mana)
            if odzysk:
                gracz.mana += odzysk
                print(f"  Forma przywraca {odzysk} many.")
        stan["forma_tury"] -= 1
        if stan["forma_tury"] <= 0:
            print(f"  Przemiana ({stan.get('forma')}) mija. Znów jesteś sobą.")
            _wyczysc_forme(stan)

    from game.pochodzenie import suma_flagi
    regen = int(suma_flagi(gracz, "regen_hp"))
    if regen > 0:
        wyleczone = min(regen, gracz.max_hp - gracz.hp)
        if wyleczone:
            gracz.hp += wyleczone
            print(f"  Druga skóra regeneruje {wyleczone} HP.")
    rmana = int(suma_flagi(gracz, "regen_mana"))
    if rmana > 0 and gracz.max_mana > 0:
        odzysk = min(rmana, gracz.max_mana - gracz.mana)
        if odzysk:
            gracz.mana += odzysk
            print(f"  Spokojny umysł przywraca {odzysk} many.")

    odpor = int(suma_flagi(gracz, "odpornosc_dot"))

    # Trucizna gracza
    if stan["gracz_trucizna_tury"] > 0:
        dam = max(1, stan["gracz_trucizna_obrazenia"] - odpor)
        gracz.hp = max(0, gracz.hp - dam)
        stan["gracz_trucizna_tury"] -= 1
        print(
            f"  ⚗  Trucizna działa! Tracisz {dam} HP."
            f" (Pozostało tur: {stan['gracz_trucizna_tury']})"
        )

    # Krwawienie gracza
    if stan["gracz_krwawienie_tury"] > 0:
        dam = max(1, stan["gracz_krwawienie_obrazenia"] - odpor)
        gracz.hp = max(0, gracz.hp - dam)
        stan["gracz_krwawienie_tury"] -= 1
        print(
            f"  🩸  Krwawisz! Tracisz {dam} HP."
            f" (Pozostało tur: {stan['gracz_krwawienie_tury']})"
        )

    cd = stan.setdefault("cd", {})
    for klucz in list(cd):
        if cd[klucz] > 0:
            cd[klucz] -= 1
            if cd[klucz] <= 0:
                del cd[klucz]

    stan["tura"] += 1


# ------------------------------------------------------------------ #
#  Wyświetlanie stanu walki                                           #
# ------------------------------------------------------------------ #

def _wyswietl_stan_walki(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Wyświetla aktualny stan walki wraz z aktywnymi efektami."""
    wyswietl_linie()
    print(f"  {przeciwnik}")
    mana_str = ""
    if gracz.max_mana > 0:
        mana_str = f"   🔮 {gracz.mana}/{gracz.max_mana} {gracz.pasek_many()}"
    print(f"  🧙 {gracz.imie}  ❤️ {gracz.hp}/{gracz.max_hp} {gracz.pasek_hp()}{mana_str}")
    towar = etykieta_towarzysza(gracz)
    if towar:
        print(f"  🤝 Towarzysz: {towar}")
    sluga = stan.get("przyzwanie")
    if sluga:
        print(
            f"  Przyzwanie: {sluga['ikona']} {sluga['nazwa']}"
            f"  HP {sluga['hp']}/{sluga['max_hp']}"
        )

    efekty: list[str] = []
    if stan["buff_atak_tury"] > 0:
        efekty.append(f"Atak ×{stan['buff_atak_mnoznik']:.1f} ({stan['buff_atak_tury']} tur)")
    if stan["buff_obrona_tury"] > 0:
        efekty.append(f"Obrona ×{stan['buff_obrona_mnoznik']:.1f} ({stan['buff_obrona_tury']} tur)")
    if stan["tarcza_runowa"] > 0:
        efekty.append(f"Tarcza runowa ({stan['tarcza_runowa']} HP)")
    if stan["leczenie_zablokowane"]:
        efekty.append("Leczenie zablokowane")
    if stan["regeneracja_tury"] > 0:
        efekty.append(f"Regeneracja +{stan['regeneracja_hp']} HP/tur ({stan['regeneracja_tury']} tur)")
    if stan["lich_ochrona"]:
        efekty.append("Ochrona Licha (aktywna)")
    if stan["nastepny_atak_mnoznik"] > 1.0:
        efekty.append(f"Następny atak ×{stan['nastepny_atak_mnoznik']:.0f}")
    # Statusy gracza
    if stan["gracz_trucizna_tury"] > 0:
        efekty.append(f"⚗ Zatruty ({stan['gracz_trucizna_tury']} tur, -{stan['gracz_trucizna_obrazenia']} HP)")
    if stan["gracz_krwawienie_tury"] > 0:
        efekty.append(f"🩸 Krwawienie ({stan['gracz_krwawienie_tury']} tur, -{stan['gracz_krwawienie_obrazenia']} HP)")
    if stan["gracz_ogluszone_tury"] > 0:
        efekty.append(f"❄ Ogłuszony ({stan['gracz_ogluszone_tury']} tur)")
    if stan["wrog_trucizna_tury"] > 0:
        efekty.append(f"{przeciwnik.nazwa} zatruty ({stan['wrog_trucizna_tury']} tur)")
    if stan["wrog_ogluszone_tury"] > 0:
        efekty.append(f"{przeciwnik.nazwa} ogłuszony ({stan['wrog_ogluszone_tury']} tur)")
    if stan["wrog_oslabienie_tury"] > 0:
        efekty.append(f"{przeciwnik.nazwa} osłabiony ({stan['wrog_oslabienie_tury']} tur)")
    if stan["wrog_rozpad"]:
        efekty.append(f"{przeciwnik.nazwa} w rozpadzie (-20% max HP)")
    if stan["jest_boss"]:
        efekty.append("⚠ BOSS!")
    if stan.get("forma"):
        efekty.append(
            f"Forma: {stan['forma']} ({stan.get('forma_tury', 0)} tur)"
        )
    cd_map = stan.get("cd") or {}
    cd_txt = [
        f"{UMIEJETNOSCI[k]['nazwa']} {v}"
        for k, v in cd_map.items()
        if v > 0 and k in UMIEJETNOSCI
    ]
    if cd_txt:
        efekty.append("CD: " + ", ".join(cd_txt))
    if efekty:
        print(f"  Efekty: {', '.join(efekty)}")

    wyswietl_linie()


# ------------------------------------------------------------------ #
#  Submenu umiejętności                                               #
# ------------------------------------------------------------------ #

def _menu_umiejetnosci(gracz: Gracz, stan: dict) -> str | None:
    """
    Wyświetla submenu umiejętności. Zwraca klucz wybranego skilla lub None
    (gdy gracz wraca do głównego menu walki).
    """
    while True:
        print("\n  === UMIEJĘTNOŚCI ===")
        cd_map = stan.setdefault("cd", {})
        for i, klucz in enumerate(gracz.umiejetnosci, 1):
            info = UMIEJETNOSCI[klucz]
            ranga = ranga_skilla(gracz, klucz)
            cd_zost = cd_map.get(klucz, 0)
            koszt_str = f"  [{info['koszt_many']} many]" if info["koszt_many"] > 0 else ""
            ranga_str = f" r.{ranga}"
            if cd_zost > 0:
                blokada = f"  CD {cd_zost}"
            elif gracz.mana < info["koszt_many"]:
                blokada = "  ✗ mana"
            else:
                blokada = ""
            print(
                f"  [{i}] {info['ikona']} {info['nazwa']}{ranga_str}{koszt_str}"
                f"  — {info['opis']}{blokada}"
            )
        print("  [0] Wróć\n")

        wybor = input("  Wybierz umiejętność: ").strip()
        if wybor == "0":
            return None
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(gracz.umiejetnosci):
                klucz = gracz.umiejetnosci[idx]
                info = UMIEJETNOSCI[klucz]
                if cd_map.get(klucz, 0) > 0:
                    print(f"  Umiejętność odnowi się za {cd_map[klucz]} tur(y).")
                    continue
                if gracz.mana < info["koszt_many"]:
                    print(
                        f"  Niewystarczająca mana!"
                        f" (Masz {gracz.mana}, potrzebujesz {info['koszt_many']})"
                    )
                    continue
                return klucz
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")


# ------------------------------------------------------------------ #
#  Obsługa umiejętności                                               #
# ------------------------------------------------------------------ #

def _uzyj_umiejetnosci(
    klucz: str, gracz: Gracz, przeciwnik: Przeciwnik, stan: dict
) -> str | None:
    """
    Wykonuje wybraną umiejętność. Zwraca 'wygrana', 'ucieczka' lub None.
    """
    info = UMIEJETNOSCI[klucz]
    koszt = info["koszt_many"]
    ranga = ranga_skilla(gracz, klucz)

    def S(baza: int) -> int:
        return skaluj_wartosc(gracz, klucz, baza)

    def T(baza: int) -> int:
        return czas_trwania(gracz, klucz, baza)

    def mag(lo: int, hi: int) -> int:
        a, b = S(lo), S(hi)
        return int(random.randint(min(a, b), max(a, b)) * dmg_wzmocnienie)

    # Arcymag: przyspieszenie — następny czar darmowy i 2× silniejszy
    dmg_wzmocnienie = 1.0
    if stan["przyspieszenie"] and koszt > 0:
        dmg_wzmocnienie = 2.0
        koszt = 0
        stan["przyspieszenie"] = False
        print(f"\n  ⚡ Przyspieszenie magiczne! Obrażenia ×2, mana darmowa!")

    gracz.mana -= koszt
    print(f"\n  {info['ikona']}  Używasz: {info['nazwa']} (r.{ranga})!")

    cd = cd_skilla(klucz)
    if cd > 0:
        stan.setdefault("cd", {})[klucz] = cd

    efektywny_atak = max(
        1,
        int(
            gracz.atak
            * stan["buff_atak_mnoznik"]
            * stan["nastepny_atak_mnoznik"]
            * float(stan.get("forma_atak") or 1.0)
        ),
    )
    stan["nastepny_atak_mnoznik"] = 1.0

    # ---- WOJOWNIK – klasa główna ----

    if klucz == "potezny_cios":
        mnoznik = 2.0 + 0.15 * (ranga - 1)
        obrazenia = int(_oblicz_obrazenia(efektywny_atak, przeciwnik.obrona) * mnoznik)
        przeciwnik.hp -= obrazenia
        stan["brak_obrony_tura"] = True
        print(f"  Zadajesz {obrazenia} obrażeń! (Tracisz obronę przy odwecie)")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "tarcza_wiary":
        stan["buff_obrona_mnoznik"] = 2.0
        stan["buff_obrona_tury"] = T(1)
        print(f"  Twoja obrona jest podwojona przez {stan['buff_obrona_tury']} tur(y) wroga!")

    elif klucz == "okrzyk_bojowy":
        bonus = 0.30 + 0.05 * (ranga - 1)
        stan["buff_atak_mnoznik"] = 1.0 + bonus
        stan["buff_atak_tury"] = T(2)
        print(
            f"  Okrzyk bojowy! Atak +{int(bonus * 100)}%"
            f" przez {stan['buff_atak_tury']} tury!"
        )

    elif klucz == "szal_berserka":
        bonus = 0.50 + 0.05 * (ranga - 1)
        stan["buff_atak_mnoznik"] = 1.0 + bonus
        stan["buff_atak_tury"] = T(3)
        stan["leczenie_zablokowane"] = True
        print(
            f"  Szał berserka! Atak +{int(bonus * 100)}%"
            f" przez {stan['buff_atak_tury']} tury — leczenie zablokowane!"
        )

    # ---- WOJOWNIK – Paladyn ----

    elif klucz == "boskie_swiatlo":
        lecz = S(50)
        wyleczone = min(lecz, gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        print(f"  Boskie światło! Przywróciłeś {wyleczone} HP!")

    elif klucz == "swiety_cios":
        mnoznik = 2.5 + 0.10 * (ranga - 1)
        bazowe = int(_oblicz_obrazenia(efektywny_atak, przeciwnik.obrona) * mnoznik)
        swiete = S(20)
        obrazenia = bazowe + swiete
        przeciwnik.hp -= obrazenia
        print(
            f"  Zadajesz {obrazenia} obrażeń"
            f" ({bazowe} fizycznych + {swiete} świętych)!"
        )
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- WOJOWNIK – Barbarzyńca ----

    elif klucz == "wscieklosc":
        bonus = 0.80 + 0.05 * (ranga - 1)
        tury = T(4)
        stan["buff_atak_mnoznik"] = 1.0 + bonus
        stan["buff_atak_tury"] = tury
        stan["buff_obrona_mnoznik"] = 0.5
        stan["buff_obrona_tury"] = tury
        print(
            f"  Wściekłość! Atak +{int(bonus * 100)}% przez {tury} tury"
            f" — obrona -50%!"
        )

    elif klucz == "niszczace_uderzenie":
        pct = 0.30 + 0.04 * (ranga - 1)
        obrazenia = max(1, int(przeciwnik.hp * pct))
        przeciwnik.hp -= obrazenia
        print(
            f"  Niszczące uderzenie! Zadajesz {obrazenia} obrażeń"
            f" ({int(pct * 100)}% HP wroga)!"
        )
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- MAG – klasa główna ----

    elif klucz == "kula_ognia":
        obrazenia = mag(35, 55)
        przeciwnik.hp -= obrazenia
        print(f"  Kula ognia trafia za {obrazenia} obrażeń magicznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "lodowe_wiezy":
        stan["wrog_ogluszone_tury"] = 1 + (1 if ranga >= 4 else 0)
        print(
            f"  {przeciwnik.nazwa} jest zamrożony i pomija"
            f" {stan['wrog_ogluszone_tury']} tur(y)!"
        )

    elif klucz == "tarcza_runowa":
        stan["tarcza_runowa"] = S(40)
        print(f"  Tarcza runowa aktywna! Absorbuje do {stan['tarcza_runowa']} obrażeń.")

    elif klucz == "meteor":
        obrazenia = mag(80, 120)
        przeciwnik.hp -= obrazenia
        print(f"  Meteor uderza za {obrazenia} obrażeń magicznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- MAG – Arcymag ----

    elif klucz == "przyspieszenie_magiczne":
        stan["przyspieszenie"] = True
        print("  Przyspieszenie magiczne! Następny czar będzie darmowy i ×2 silniejszy.")

    elif klucz == "kula_pioruna":
        obrazenia = mag(100, 150)
        przeciwnik.hp -= obrazenia
        print(f"  Kula pioruna uderza za {obrazenia} obrażeń magicznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- ŁOTRZYK – klasa główna ----

    elif klucz == "cios_w_plecy":
        bazowe = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
        szansa = 0.40 + 0.05 * (ranga - 1)
        aktywuje = stan["tura"] == 1 or random.random() < szansa
        if aktywuje:
            obrazenia = bazowe * 2
            print(f"  Cios w plecy! Zadajesz {obrazenia} obrażeń (podwójne)!")
        else:
            obrazenia = bazowe
            print(f"  Zadajesz {obrazenia} obrażeń.")
        przeciwnik.hp -= obrazenia
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "trucizna":
        tury = T(3)
        dps = S(10)
        stan["wrog_trucizna_tury"] = tury
        stan["wrog_trucizna_obrazenia"] = dps
        print(
            f"  {przeciwnik.nazwa} jest zatruty!"
            f" Traci {dps} HP na turę przez {tury} tury."
        )

    elif klucz == "dymna_bomba":
        print("  Rzucasz bombę dymną! Znikasz w chmurze dymu...")
        return "ucieczka"

    elif klucz == "smiertelne_uderzenie":
        prog = 0.25 + 0.02 * (ranga - 1)
        if przeciwnik.hp < przeciwnik.max_hp * prog:
            mnoznik = 3.0 + 0.2 * (ranga - 1)
            obrazenia = int(_oblicz_obrazenia(efektywny_atak, przeciwnik.obrona) * mnoznik)
            print(f"  Śmiertelne uderzenie! Zadajesz {obrazenia} obrażeń!")
        else:
            obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
            print(f"  Zadajesz {obrazenia} obrażeń (wróg zbyt silny na egzekucję).")
        przeciwnik.hp -= obrazenia
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- ŁOTRZYK – Zabójca ----

    elif klucz == "cien_smierci":
        bazowe = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
        szansa = min(0.90, 0.60 + 0.05 * (ranga - 1))
        if random.random() < szansa:
            obrazenia = bazowe * 4
            print(f"  KRYTYCZNE TRAFIENIE! Cień śmierci zadaje {obrazenia} obrażeń!")
        else:
            obrazenia = bazowe
            print(f"  Cios chybił — zadajesz {obrazenia} obrażeń.")
        przeciwnik.hp -= obrazenia
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "egzekucja":
        prog = 0.15 + 0.02 * (ranga - 1)
        if przeciwnik.hp < przeciwnik.max_hp * prog:
            print(f"  Egzekucja! Kończysz {przeciwnik.nazwa} jednym ciosem!")
            przeciwnik.hp = 0
            return "wygrana"
        obrazenia = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
        przeciwnik.hp -= obrazenia
        print(f"  Zadajesz {obrazenia} obrażeń (wróg zbyt silny na egzekucję).")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- ŁOTRZYK – Zwiadowca ----

    elif klucz == "unik":
        szansa = min(0.95, 0.75 + 0.05 * (ranga - 1))
        stan["unik_aktywny"] = True
        stan["unik_szansa"] = szansa
        print(
            f"  Przygotowujesz się do uniku!"
            f" ({int(szansa * 100)}% szans na ominięcie ataku wroga)"
        )

    elif klucz == "grad_strzal":
        strzaly = 3 + (ranga - 1) // 2
        total = 0
        trafienia = 0
        for _ in range(strzaly):
            if not przeciwnik.zyje():
                break
            dam = _oblicz_obrazenia(efektywny_atak, przeciwnik.obrona)
            przeciwnik.hp = max(0, przeciwnik.hp - dam)
            total += dam
            trafienia += 1
        print(f"  Grad strzał: {trafienia} trafień za łącznie {total} obrażeń!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- MAG – Mroczny mag ----

    elif klucz == "mroczna_strzala":
        obrazenia = mag(45, 70)
        przeciwnik.hp -= obrazenia
        print(f"  Mroczna strzała trafia za {obrazenia} obrażeń mrocznych!")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "klatwa_mroku":
        tury = T(3)
        dps = S(15)
        stan["wrog_oslabienie_tury"] = tury
        stan["wrog_trucizna_tury"] = tury
        stan["wrog_trucizna_obrazenia"] = dps
        print(
            f"  Klątwa mroku! {przeciwnik.nazwa} zadaje 50% mniej obrażeń"
            f" i traci {dps} HP/turę przez {tury} tury."
        )

    # ---- DRUID – klasa główna ----

    elif klucz == "splot_korzeni":
        stan["wrog_ogluszone_tury"] = 1 + (1 if ranga >= 4 else 0)
        print(
            f"  🌿  Sploty korzeni oplatają {przeciwnik.nazwa}!"
            f" Pomija {stan['wrog_ogluszone_tury']} tur(y)."
        )

    elif klucz == "forma_niedzwiedzia":
        tury = T(4)
        _ustaw_forme(
            stan,
            "niedźwiedź",
            tury,
            forma_atak=1.20 + 0.04 * (ranga - 1),
            forma_obrona=1.40 + 0.06 * (ranga - 1),
            forma_regen=S(8),
        )
        print(
            f"  🐻  Przemieniasz się w niedźwiedzia na {tury} tury!"
            f" Atak ×{stan['forma_atak']:.2f}, obrona ×{stan['forma_obrona']:.2f},"
            f" +{stan['forma_regen']} HP/turę."
        )

    elif klucz == "uzdrowienie":
        lecz = S(50)
        wyleczone = min(lecz, gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        print(f"  💚  Uzdrowienie! Przywróciłeś {wyleczone} HP!")

    elif klucz == "burza_natury":
        obrazenia = mag(40, 60)
        przeciwnik.hp -= obrazenia
        print(f"  ⛈  Burza natury uderza za {obrazenia} obrażeń żywiołowych!")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "forma_wilka":
        tury = T(4)
        _ustaw_forme(
            stan,
            "wilk",
            tury,
            forma_atak=1.45 + 0.06 * (ranga - 1),
            forma_obrona=0.75,
            forma_kryt=0.15 + 0.03 * (ranga - 1),
        )
        print(
            f"  🐺  Przemieniasz się w wilka na {tury} tury!"
            f" Atak ×{stan['forma_atak']:.2f}, słabsza obrona,"
            f" +{int(stan['forma_kryt'] * 100)}% szansy na krytyk."
        )

    elif klucz == "regeneracja":
        stan["regeneracja_hp"] = S(15)
        stan["regeneracja_tury"] = T(4)
        print(
            f"  🌱  Regeneracja! Będziesz odnawiać {stan['regeneracja_hp']} HP"
            f" na turę przez {stan['regeneracja_tury']} tury."
        )

    elif klucz == "forma_kruka":
        tury = T(4)
        _ustaw_forme(
            stan,
            "kruk",
            tury,
            forma_atak=1.05,
            forma_unik=min(0.70, 0.40 + 0.05 * (ranga - 1)),
        )
        print(
            f"  🐦  Przemieniasz się w kruka na {tury} tury!"
            f" {int(stan['forma_unik'] * 100)}% szansy na unik ciosów."
        )

    # ---- DRUID – Szaman ----

    elif klucz == "totem_zycia":
        stan["regeneracja_hp"] = S(30)
        stan["regeneracja_tury"] = T(3)
        print(
            f"  🔺  Totem życia! Będziesz odnawiać {stan['regeneracja_hp']} HP"
            f" na turę przez {stan['regeneracja_tury']} tury."
        )

    elif klucz == "forma_ducha":
        tury = T(4)
        _ustaw_forme(
            stan,
            "duch",
            tury,
            forma_unik=min(0.60, 0.30 + 0.04 * (ranga - 1)),
            forma_mana=S(8),
        )
        print(
            f"  👻  Przemieniasz się w ducha na {tury} tury!"
            f" {int(stan['forma_unik'] * 100)}% uniku,"
            f" +{stan['forma_mana']} many na turę."
        )

    elif klucz == "piorun_szamana":
        obrazenia = mag(70, 100)
        przeciwnik.hp -= obrazenia
        print(f"  ⚡  Piorun szamana uderza za {obrazenia} obrażeń błyskawicznych!")
        szansa_stun = min(0.90, 0.50 + 0.08 * (ranga - 1))
        if random.random() < szansa_stun:
            stan["wrog_ogluszone_tury"] = 1
            print(f"  {przeciwnik.nazwa} jest ogłuszony i pomija następną turę!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- DRUID – Strażnik Lasu ----

    elif klucz == "kolce_natury":
        tury = T(4)
        dps = S(20)
        stan["wrog_trucizna_tury"] = tury
        stan["wrog_trucizna_obrazenia"] = dps
        print(
            f"  🌵  Kolce natury! {przeciwnik.nazwa} traci {dps} HP"
            f" na turę przez {tury} tury."
        )

    elif klucz == "gniew_puszczy":
        aktywne = sum([
            stan["wrog_trucizna_tury"] > 0,
            stan["wrog_ogluszone_tury"] > 0,
            stan["wrog_oslabienie_tury"] > 0,
            stan["wrog_rozpad"],
        ])
        mnoznik = max(1, aktywne)
        lo, hi = S(50), S(80)
        obrazenia = int(random.randint(min(lo, hi), max(lo, hi)) * mnoznik * dmg_wzmocnienie)
        przeciwnik.hp -= obrazenia
        print(f"  🌲  Gniew puszczy! ×{mnoznik} efektów — zadajesz {obrazenia} obrażeń!")
        if not przeciwnik.zyje():
            return "wygrana"

    # ---- NEKROMANTA – klasa główna ----

    elif klucz == "wysysanie_zycia":
        obrazenia = mag(30, 50)
        przeciwnik.hp -= obrazenia
        wyleczone = min(obrazenia, gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        print(f"  🩸  Wysysasz {obrazenia} HP od {przeciwnik.nazwa}!")
        print(f"  Leczysz się o {wyleczone} HP!")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "przywolaj_szkielet":
        hp = S(28)
        atak = S(10)
        _ustaw_przyzwanie(stan, "Szkielet", "💀", hp, atak, przejecie=0.45)
        print(
            f"  💀  Przywołujesz szkielet! HP {hp}, atak {atak}."
            f" Może przejąć ciosy wroga."
        )

    elif klucz == "klatwa_smierci":
        tury = T(3)
        stan["wrog_oslabienie_tury"] = tury
        print(
            f"  💀  Klątwa śmierci! {przeciwnik.nazwa} zadaje 50% mniej obrażeń"
            f" przez {tury} tury."
        )

    elif klucz == "rozpad":
        if not stan["wrog_rozpad"]:
            stan["wrog_rozpad"] = True
            pct = 0.20 + 0.03 * (ranga - 1)
            utracone = max(1, int(przeciwnik.max_hp * pct))
            przeciwnik.max_hp -= utracone
            przeciwnik.hp = min(przeciwnik.hp, przeciwnik.max_hp)
            print(
                f"  🦴  Rozpad! {przeciwnik.nazwa} traci {utracone}"
                f" maksymalnego HP ({int(pct * 100)}% — teraz {przeciwnik.max_hp})."
            )
        else:
            print(f"  Rozpad już działa na {przeciwnik.nazwa}.")

    elif klucz == "przywolaj_ghul":
        hp = S(50)
        atak = S(12)
        _ustaw_przyzwanie(stan, "Ghul", "🧟", hp, atak, przejecie=0.70)
        print(
            f"  🧟  Przywołujesz ghula! HP {hp}, atak {atak}."
            f" Chętnie przejmuje ciosy."
        )

    elif klucz == "dotyk_smierci":
        tury = T(3)
        dps = S(25)
        stan["wrog_trucizna_tury"] = tury
        stan["wrog_trucizna_obrazenia"] = dps
        print(
            f"  ☠  Dotyk śmierci! {przeciwnik.nazwa} traci {dps} HP"
            f" na turę przez {tury} tury."
        )

    # ---- NEKROMANTA – Lich ----

    elif klucz == "fala_smierci":
        obrazenia = mag(60, 90)
        przeciwnik.hp -= obrazenia
        pct_lecz = 0.30 + 0.05 * (ranga - 1)
        wyleczone = min(int(obrazenia * pct_lecz), gracz.max_hp - gracz.hp)
        gracz.hp += wyleczone
        print(f"  💀  Fala śmierci uderza za {obrazenia} obrażeń!")
        print(f"  Leczysz się o {wyleczone} HP ({int(pct_lecz * 100)}% obrażeń).")
        if not przeciwnik.zyje():
            return "wygrana"

    elif klucz == "przywolaj_widmo":
        hp = S(32)
        atak = S(16)
        _ustaw_przyzwanie(
            stan, "Widmo", "👻", hp, atak, przejecie=0.40, przebicie=0.5
        )
        print(
            f"  👻  Przywołujesz widmo z Otchłani! HP {hp}, atak {atak}."
            f" Ataki ignorują połowę obrony wroga."
        )

    elif klucz == "wiecznie_zywi":
        hp = S(40)
        stan["lich_ochrona"] = True
        stan["lich_ochrona_hp"] = hp
        print(
            f"  💀  Ochrona Licha aktywna! Jeśli miałbyś umrzeć,"
            f" zamiast tego odzyskasz {hp} HP (raz)."
        )

    # ---- NEKROMANTA – Kapłan Mroku ----

    elif klucz == "pakt_krwi":
        koszt_hp = max(8, 20 - 2 * (ranga - 1))
        gracz.hp = max(1, gracz.hp - koszt_hp)
        mnoznik = 3.0 + 0.25 * (ranga - 1)
        stan["nastepny_atak_mnoznik"] = mnoznik
        print(
            f"  🗡  Pakt krwi! Tracisz {koszt_hp} HP."
            f" Następny atak zadaje ×{mnoznik:.2f} obrażeń!"
        )

    elif klucz == "krwawy_sluga":
        koszt_hp = max(10, S(18))
        zaplacone = min(koszt_hp, max(0, gracz.hp - 1))
        gracz.hp -= zaplacone
        hp = S(45) + zaplacone // 2
        atak = S(18)
        _ustaw_przyzwanie(stan, "Krwawy sługa", "🩸", hp, atak, przejecie=0.55)
        print(
            f"  🩸  Poświęcasz {zaplacone} HP i przywołujesz krwawego sługę!"
            f" HP {hp}, atak {atak}."
        )

    elif klucz == "ofiarny_rytual":
        pct = 0.30
        mnoznik = 3.0 + 0.25 * (ranga - 1)
        poswiecenie = max(1, int(gracz.hp * pct))
        gracz.hp = max(1, gracz.hp - poswiecenie)
        obrazenia = int(poswiecenie * mnoznik)
        przeciwnik.hp -= obrazenia
        print(
            f"  🩸  Ofiarny rytuał! Poświęcasz {poswiecenie} HP —"
            f" {przeciwnik.nazwa} traci {obrazenia} HP!"
        )
        if not przeciwnik.zyje():
            return "wygrana"

    return None


def _menu_przedmiotow(gracz: Gracz, stan: dict) -> bool:
    """
    Submenu przedmiotów. Zwraca True, jeśli gracz zużył turę.
    """
    while True:
        print("\n  === 🧪 PRZEDMIOTY ===")
        print(f"  [1] 🧪  Mikstura leczenia ({gracz.mikstury}) — +40 HP")
        print(
            f"  [2] 💚  Mikstura większa ({getattr(gracz, 'mikstury_duze', 0)}) — +80 HP"
        )
        if gracz.max_mana > 0:
            print(
                f"  [3] 🔮  Mikstura many ({getattr(gracz, 'mikstury_many', 0)}) — +30 many"
            )
        print(
            f"  [4] 🧴  Antidotum ({getattr(gracz, 'antidota', 0)})"
            f" — zdejmuje truciznę i krwawienie"
        )
        print("  [0] ↩  Wróć\n")

        wybor = input("  Wybierz przedmiot: ").strip()
        if wybor == "0":
            return False

        if wybor in ("1", "2"):
            if stan["leczenie_zablokowane"]:
                print("  Nie możesz się leczyć podczas szału berserka!")
                continue
            if wybor == "1":
                if gracz.mikstury <= 0:
                    print("  Nie masz mikstur leczenia!")
                    continue
                print(f"\n  🧪  {gracz.uzyj_miksture()}")
            else:
                if getattr(gracz, "mikstury_duze", 0) <= 0:
                    print("  Nie masz większych mikstur!")
                    continue
                print(f"\n  🧪  {gracz.uzyj_miksture_duza()}")
            return True

        if wybor == "3":
            if gracz.max_mana <= 0:
                print("  Twoja klasa nie korzysta z many.")
                continue
            if getattr(gracz, "mikstury_many", 0) <= 0:
                print("  Nie masz mikstur many!")
                continue
            print(f"\n  🔮  {gracz.uzyj_miksture_many()}")
            return True

        if wybor == "4":
            msg = gracz.uzyj_antidotum()
            if msg is None:
                print("  Nie masz antidotum!")
                continue
            stan["gracz_trucizna_tury"] = 0
            stan["gracz_krwawienie_tury"] = 0
            print(f"\n  ⚗  {msg}")
            print("  Trucizna i krwawienie ustępują.")
            return True

        print("  Nieprawidłowy wybór.")


def _drop_po_walce(gracz: Gracz, jest_boss: bool) -> None:
    """Losowy łup po wygranej walce."""
    szansa = 0.40 if jest_boss else 0.18
    if random.random() > szansa:
        return

    roll = random.random()
    if roll < 0.40:
        gracz.mikstury += 1
        print("  🧪  Łup: mikstura leczenia!")
    elif roll < 0.58:
        gracz.antidota = getattr(gracz, "antidota", 0) + 1
        print("  ⚗  Łup: antidotum!")
    elif roll < 0.72 and gracz.max_mana > 0:
        gracz.mikstury_many = getattr(gracz, "mikstury_many", 0) + 1
        print("  🔮  Łup: mikstura many!")
    else:
        tanie = ["sztylet", "skorzana_zbroja", "plaszcz_lotrzyka"]
        mocne = ["miecz", "kolczuga", "luk_elfi", "szata_maga"]
        klucz = random.choice(mocne if jest_boss else tanie)
        dodaj_do_plecaka(gracz, klucz)
        item = EKWIPUNEK[klucz]
        print(f"  {item['ikona']}  Łup: {item['nazwa']} (trafia do plecaka)")


def _drop_surowcow(gracz: Gracz, jest_boss: bool) -> None:
    """Skóra i ruda z pokonanych wrogów — na rozbudowę obozu."""
    from game.oboz import dodaj_surowiec, SUROWCE

    szansa = 0.55 if jest_boss else 0.28
    if random.random() > szansa:
        return
    klucz = "skora" if random.random() < 0.55 else "ruda"
    ile = random.randint(2, 4) if jest_boss else random.randint(1, 2)
    dodaj_surowiec(gracz, klucz, ile)
    info = SUROWCE[klucz]
    print(f"  {info['ikona']}  Łup: +{ile} {info['nazwa']}")

def _tura_gracza(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> str | None:
    """
    Obsługuje turę gracza. Zwraca 'wygrana', 'ucieczka' lub None
    (walka trwa dalej).
    """
    while True:
        print("\n  Co robisz?")
        print("  [1] ⚔  Atakuj")
        if stan["leczenie_zablokowane"]:
            print("  [2] 🧪  Przedmioty — leczenie ZABLOKOWANE (szał berserka)")
        else:
            print(f"  [2] 🧪  Przedmioty (mikstury: {gracz.mikstury})")
        print("  [3] ✨  Umiejętności")
        print("  [4] 🏃  Uciekaj")

        wybor = input("\n  Twój wybór: ").strip()

        if wybor == "1":
            efektywny_atak = max(
                1,
                int(
                    gracz.atak
                    * stan["buff_atak_mnoznik"]
                    * stan["nastepny_atak_mnoznik"]
                    * float(stan.get("forma_atak") or 1.0)
                ),
            )
            stan["nastepny_atak_mnoznik"] = 1.0
            from game.pochodzenie import suma_flagi
            if suma_flagi(gracz, "berserk") and gracz.hp < gracz.max_hp * 0.4:
                efektywny_atak = max(
                    1, int(efektywny_atak * (1.0 + suma_flagi(gracz, "berserk")))
                )
            if stan["tura"] == 1 and suma_flagi(gracz, "pierwszy_cios"):
                efektywny_atak = max(
                    1, int(efektywny_atak * (1.0 + suma_flagi(gracz, "pierwszy_cios")))
                )
            obrazenia, krit = _atak_z_krytem(
                efektywny_atak, przeciwnik.obrona, gracz.klasa, stan, gracz
            )
            przeciwnik.hp -= obrazenia
            if krit:
                print(f"\n  ⚡ KRYTYCZNE TRAFIENIE! Atakujesz {przeciwnik.nazwa}! Zadajesz {obrazenia} obrażeń!")
            else:
                print(f"\n  ⚔  Atakujesz {przeciwnik.nazwa}! Zadajesz {obrazenia} obrażeń.")
            wamp = suma_flagi(gracz, "wampir")
            if wamp > 0 and obrazenia > 0:
                heal = min(int(obrazenia * wamp), gracz.max_hp - gracz.hp)
                if heal:
                    gracz.hp += heal
                    print(f"  Krwawy cios: odzyskujesz {heal} HP.")
            if not przeciwnik.zyje():
                return "wygrana"
            return None

        elif wybor == "2":
            if _menu_przedmiotow(gracz, stan):
                return None
            continue

        elif wybor == "3":
            klucz = _menu_umiejetnosci(gracz, stan)
            if klucz is None:
                continue  # gracz wrócił — pokaż menu ponownie
            return _uzyj_umiejetnosci(klucz, gracz, przeciwnik, stan)

        elif wybor == "4":
            limit = 0.25 if getattr(gracz, "tryb_trudnosci", "normalny") == "hardcore" else 0.5
            szansa = random.random()
            if szansa < limit:
                print("\n  🏃  Udało ci się uciec!")
                return "ucieczka"
            print("\n  🏃  Nie udało się uciec — przeciwnik blokuje drogę!")
            return None

        else:
            print("  Nieprawidłowy wybór. Wpisz 1, 2, 3 lub 4.")


# ------------------------------------------------------------------ #
#  Tura przeciwnika                                                   #
# ------------------------------------------------------------------ #

def _specjal_wroga(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> bool:
    """
    Unikalne zachowania wrogów. Zwraca True, jeśli wróg zużył turę
    na umiejętność zamiast zwykłego ataku.
    """
    nazwa = przeciwnik.nazwa.lower()

    if "troll" in nazwa:
        regen = max(4, int(przeciwnik.max_hp * 0.08))
        faktyczne = min(regen, przeciwnik.max_hp - przeciwnik.hp)
        if faktyczne > 0:
            przeciwnik.hp += faktyczne
            print(f"  💚  {przeciwnik.nazwa} regeneruje {faktyczne} HP!")

    if "wiedźma" in nazwa or "wiedzma" in nazwa:
        if random.random() < 0.35:
            stan["gracz_trucizna_tury"] = max(stan["gracz_trucizna_tury"], 3)
            stan["gracz_trucizna_obrazenia"] = 12 if stan["jest_boss"] else 8
            print(f"  ⚗  {przeciwnik.nazwa} rzuca klątwę trucizny zamiast ataku!")
            return True

    if "smok" in nazwa and stan["tura"] % 3 == 0:
        dmg = random.randint(28, 48) if stan["jest_boss"] else random.randint(16, 28)
        form_unik = float(stan.get("forma_unik") or 0)
        if form_unik > 0 and random.random() < form_unik:
            print(
                f"  Unikasz zionięcia {przeciwnik.nazwa}"
                f" w postaci {stan.get('forma')}!"
            )
            return True
        from game.atrybuty import szansa_uniku_zrecznosc
        if random.random() < szansa_uniku_zrecznosc(gracz):
            print(f"  💨  Zręczność! Unikasz zionięcia {przeciwnik.nazwa}!")
            return True
        if stan["tarcza_runowa"] > 0:
            absorbcja = min(stan["tarcza_runowa"], dmg)
            stan["tarcza_runowa"] -= absorbcja
            dmg -= absorbcja
            print(f"  🔮  Tarcza runowa absorbuje {absorbcja} obrażeń ognia!")
        dmg = _przejmij_obrazenia_sluga(stan, dmg, przeciwnik.nazwa)
        if dmg > 0:
            gracz.hp = max(0, gracz.hp - dmg)
            print(f"  🔥  {przeciwnik.nazwa} zieje ogniem! Otrzymujesz {dmg} obrażeń (ignoruje obronę)!")
        _sprobuj_ocalic(gracz, stan)
        return True

    return False


def _tura_przeciwnika(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Obsługuje turę przeciwnika z uwzględnieniem aktywnych efektów."""

    # Trucizna na wroga
    if stan["wrog_trucizna_tury"] > 0:
        dam = stan["wrog_trucizna_obrazenia"]
        przeciwnik.hp = max(0, przeciwnik.hp - dam)
        stan["wrog_trucizna_tury"] -= 1
        print(
            f"  ☠  Trucizna! {przeciwnik.nazwa} traci {dam} HP."
            f" (Pozostało tur: {stan['wrog_trucizna_tury']})"
        )
        if not przeciwnik.zyje():
            return

    # Ogłuszenie wroga
    if stan["wrog_ogluszone_tury"] > 0:
        stan["wrog_ogluszone_tury"] -= 1
        print(f"  ❄  {przeciwnik.nazwa} jest ogłuszony i pomija turę!")
        return

    if _specjal_wroga(gracz, przeciwnik, stan):
        return
    if stan["brak_obrony_tura"]:
        efektywna_obrona = 0
        stan["brak_obrony_tura"] = False
    elif stan["buff_obrona_tury"] > 0:
        efektywna_obrona = max(0, int(gracz.obrona * stan["buff_obrona_mnoznik"]))
        stan["buff_obrona_tury"] -= 1
        if stan["buff_obrona_tury"] == 0:
            stan["buff_obrona_mnoznik"] = 1.0
    else:
        efektywna_obrona = gracz.obrona

    efektywna_obrona = max(
        0, int(efektywna_obrona * float(stan.get("forma_obrona") or 1.0))
    )

    obrazenia = _oblicz_obrazenia(przeciwnik.atak, efektywna_obrona)

    # Osłabienie (Klątwa śmierci)
    if stan["wrog_oslabienie_tury"] > 0:
        obrazenia = max(1, int(obrazenia * 0.5))
        stan["wrog_oslabienie_tury"] -= 1

    # Unik (Zwiadowca)
    if stan["unik_aktywny"]:
        stan["unik_aktywny"] = False
        if random.random() < stan.get("unik_szansa", 0.75):
            print(f"  💨  Uniknąłeś ataku {przeciwnik.nazwa}!")
            return
        print(f"  💨  Próbowałeś uniknąć, ale {przeciwnik.nazwa} trafił!")

    form_unik = float(stan.get("forma_unik") or 0)
    if form_unik > 0 and random.random() < form_unik:
        print(
            f"  Unikasz ataku {przeciwnik.nazwa}"
            f" w postaci {stan.get('forma')}!"
        )
        return

    from game.atrybuty import szansa_uniku_zrecznosc
    pasywny_unik = szansa_uniku_zrecznosc(gracz)
    if pasywny_unik > 0 and random.random() < pasywny_unik:
        print(f"  💨  Zręczność! Unikasz ciosu {przeciwnik.nazwa}!")
        return

    # Tarcza runowa
    if stan["tarcza_runowa"] > 0:
        absorbcja = min(stan["tarcza_runowa"], obrazenia)
        stan["tarcza_runowa"] -= absorbcja
        obrazenia -= absorbcja
        if absorbcja > 0:
            print(
                f"  🔮  Tarcza runowa absorbuje {absorbcja} obrażeń!"
                f" (Pozostało: {stan['tarcza_runowa']})"
            )

    obrazenia = _przejmij_obrazenia_sluga(stan, obrazenia, przeciwnik.nazwa)
    if obrazenia <= 0:
        return

    gracz.hp = max(0, gracz.hp - obrazenia)

    if obrazenia > 0:
        print(f"  💀  {przeciwnik.nazwa} atakuje cię! Otrzymujesz {obrazenia} obrażeń.")
    else:
        print(f"  🔮  Tarcza runowa całkowicie zablokowała atak {przeciwnik.nazwa}!")

    if _sprobuj_ocalic(gracz, stan):
        return

    # Szansa wroga na nałożenie statusu gracza (bossowie 2× szansa)
    szansa_status = 0.20 if stan["jest_boss"] else 0.10
    if gracz.zyje() and random.random() < szansa_status:
        status = random.choice(["trucizna", "krwawienie", "ogluszone"])
        if status == "trucizna" and stan["gracz_trucizna_tury"] == 0:
            stan["gracz_trucizna_tury"] = 3
            stan["gracz_trucizna_obrazenia"] = 8 if not stan["jest_boss"] else 14
            print(f"  ⚗  {przeciwnik.nazwa} cię zatruł! Tracisz {stan['gracz_trucizna_obrazenia']} HP na turę przez 3 tury.")
        elif status == "krwawienie" and stan["gracz_krwawienie_tury"] == 0:
            stan["gracz_krwawienie_tury"] = 3
            stan["gracz_krwawienie_obrazenia"] = 6 if not stan["jest_boss"] else 12
            print(f"  🩸  {przeciwnik.nazwa} spowodował krwawienie! Tracisz {stan['gracz_krwawienie_obrazenia']} HP na turę przez 3 tury.")
        elif status == "ogluszone" and stan["gracz_ogluszone_tury"] == 0:
            from game.pochodzenie import suma_flagi
            if random.random() < suma_flagi(gracz, "odporny_stun"):
                print("  Żelazna wola! Opierasz się ogłuszeniu.")
            else:
                stan["gracz_ogluszone_tury"] = 1
                print(f"  ❄  {przeciwnik.nazwa} ogłuszył cię! Pomijasz następną turę!")


# ------------------------------------------------------------------ #
#  Nagrody i mana po walce                                           #
# ------------------------------------------------------------------ #

def _odnow_mane_po_walce(gracz: Gracz) -> None:
    """Odnawia 20 many Magowi po zakończeniu walki."""
    if gracz.max_mana > 0:
        gracz.mana = min(gracz.mana + 20, gracz.max_mana)


def _zakonczenie_wygrana(gracz: Gracz, przeciwnik: Przeciwnik, jest_boss: bool = False) -> None:
    """Przetwarza nagrody po wygranej walce."""
    zloto = przeciwnik.losowe_zloto()
    from game.pochodzenie import mnoznik_zlota_walka, suma_flagi
    zloto = max(1, int(zloto * mnoznik_zlota_walka(gracz)))
    if jest_boss:
        # Bossowie dają bonus mikstury
        gracz.mikstury += 1
    if suma_flagi(gracz, "lup_mikstura"):
        gracz.mikstury += 1
    gracz.zloto += zloto
    gracz.rejestruj_walke(przeciwnik.nazwa)
    komunikaty = gracz.zdobadz_exp(przeciwnik.exp_nagroda)
    komunikaty.extend(sprawdz_questy(gracz))
    _odnow_mane_po_walce(gracz)

    wyswietl_linie()
    if jest_boss:
        print(f"\n  🏆🏆  BOSS POKONANY: {przeciwnik.nazwa}!")
        print(f"  Legendarny łup:")
        print(f"  💰  Zdobyłeś {zloto} złota!")
        print(f"  🧪  Bonus: 1 mikstura leczenia!")
    else:
        print(f"\n  🏆  Pokonałeś {przeciwnik.nazwa}!")
        print(f"  💰  Zdobyłeś {zloto} złota!")
    if suma_flagi(gracz, "lup_mikstura"):
        print("  🧪  Szczęśliwy łup: +1 mikstura!")
    _drop_po_walce(gracz, jest_boss)
    _drop_surowcow(gracz, jest_boss)
    for msg in komunikaty:
        print(f"  {msg}")
    nacisnij_enter()


# ------------------------------------------------------------------ #
#  Główna pętla walki                                                 #
# ------------------------------------------------------------------ #

def przeprowadz_walke(
    gracz: Gracz,
    biom: str | None = None,
    jest_boss: bool = False,
    przeciwnik: Przeciwnik | None = None,
) -> str:
    """
    Główna pętla walki. Zwraca: 'wygrana', 'przegrana' lub 'ucieczka'.
    """
    mapa_gen = getattr(gracz, "mapa_gen", 1)
    tryb = getattr(gracz, "tryb_trudnosci", "normalny")
    if przeciwnik is None:
        if jest_boss:
            przeciwnik = losuj_bossa(gracz.poziom, mapa_gen, tryb)
        else:
            przeciwnik = losuj_przeciwnika(gracz.poziom, biom, mapa_gen, tryb)
    stan = _nowy_stan_walki()
    stan["jest_boss"] = jest_boss

    from game.ikony import etykieta_biomu, wrog

    wyczysc()
    if jest_boss:
        print(f"\n  *** ⚠ BOSS! *** ")
        print(f"  {przeciwnik.opis}")
        print(f"  Stoisz przed: {wrog(przeciwnik.nazwa)} {przeciwnik.nazwa}!\n")
    else:
        print(f"\n  *** ⚔ STARCIE! ***")
        if biom:
            print(f"  Biom: {etykieta_biomu(biom)}")
        print(f"  {przeciwnik.opis}")
        print(f"  Napotkałeś: {wrog(przeciwnik.nazwa)} {przeciwnik.nazwa}!\n")
    nacisnij_enter()

    while gracz.zyje() and przeciwnik.zyje():
        _wyswietl_stan_walki(gracz, przeciwnik, stan)

        # Ogłuszenie gracza — pomija jego turę
        if stan["gracz_ogluszone_tury"] > 0:
            stan["gracz_ogluszone_tury"] -= 1
            print(f"  ❄  Jesteś ogłuszony! Pomijasz turę.")
            wynik = None
        else:
            wynik = _tura_gracza(gracz, przeciwnik, stan)

        if wynik == "ucieczka":
            _odnow_mane_po_walce(gracz)
            return "ucieczka"

        if wynik != "wygrana" and przeciwnik.zyje():
            wynik = tura_towarzysza(gracz, przeciwnik) or wynik

        if wynik != "wygrana" and przeciwnik.zyje():
            wynik = _tura_przyzwania(stan, przeciwnik) or wynik

        if wynik == "wygrana":
            _zakonczenie_wygrana(gracz, przeciwnik, jest_boss)
            return "wygrana"

        # Tura przeciwnika (sprawdź czy żyje po ataku gracza)
        if przeciwnik.zyje():
            _tura_przeciwnika(gracz, przeciwnik, stan)

        # Wróg mógł umrzeć od trucizny w turze przeciwnika
        if not przeciwnik.zyje():
            _zakonczenie_wygrana(gracz, przeciwnik, jest_boss)
            return "wygrana"

        if not gracz.zyje():
            print(f"\n  💀  Zostałeś pokonany przez {przeciwnik.nazwa}...")
            nacisnij_enter()
            return "przegrana"

        _koniec_rundy(stan, gracz)

        # Sprawdź czy gracz żyje po statusach końca rundy
        if not gracz.zyje():
            print(f"\n  💀  Padłeś od zatrucia lub krwawienia...")
            nacisnij_enter()
            return "przegrana"

        nacisnij_enter()

    return "wygrana"
