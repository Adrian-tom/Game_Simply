"""Moduł obsługujący system walki turowej."""

import random
from dataclasses import dataclass
from typing import Callable

from game.player import Gracz
from game import przetrwanie, talenty
from game.enemy import NAZWY_TYPOW, Przeciwnik, losuj_bossa, losuj_przeciwnika, mnoznik_typu
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
        # Zapowiedzi, garda i nowe efekty
        "zapowiedz": None,
        "hp_przy_zapowiedzi": 0,
        "garda": False,
        "wrog_podpalony": 0,
        "olej_tury": 0,
        "eliksir_sily_tury": 0,
        "odpornosc_ognia": False,
        "nieustepliwy_uzyty": False,
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
    if talenty.ma(gracz, "nieustepliwy") and not stan.get("nieustepliwy_uzyty"):
        stan["nieustepliwy_uzyty"] = True
        gracz.hp = 1
        print("  ⚔  Nieustępliwy! Chwiejesz się, ale stoisz — 1 HP.")
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

    for licznik, koniec in (
        ("olej_tury", "  🛢  Olej na ostrzu wypalił się."),
        ("eliksir_sily_tury", "  💪  Eliksir siły przestaje działać."),
    ):
        if stan.get(licznik, 0) > 0:
            stan[licznik] -= 1
            if stan[licznik] == 0:
                print(koniec)

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
    if stan.get("wrog_podpalony", 0) > 0:
        efekty.append(f"🔥 {przeciwnik.nazwa} płonie ({stan['wrog_podpalony']} tur)")
    if stan.get("olej_tury", 0) > 0:
        efekty.append(f"🛢 Płonące ostrze ({stan['olej_tury']} tur)")
    if stan.get("eliksir_sily_tury", 0) > 0:
        efekty.append(f"💪 Eliksir siły ({stan['eliksir_sily_tury']} tur)")
    if stan.get("odpornosc_ognia"):
        efekty.append("🧯 Odporność na ogień")
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
    stan_zdrowia = przetrwanie.opis_stanu(gracz)
    if stan_zdrowia != "zdrowy":
        print(f"  Twój stan: {stan_zdrowia}")
    _pokaz_wiedze_o_wrogu(gracz, przeciwnik, stan)

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

@dataclass
class Kontekst:
    """Wszystko, czego handler umiejętności potrzebuje do wykonania efektu.

    Skalowanie (S/T/mag) zależy od rangi i poziomu, więc jest przygotowane raz
    w _uzyj_umiejetnosci i przekazane dalej, zamiast liczone w każdym handlerze.
    """

    gracz: Gracz
    przeciwnik: Przeciwnik
    stan: dict
    ranga: int
    efektywny_atak: int
    _klucz: str
    _wzmocnienie: float = 1.0

    def S(self, baza: int) -> int:
        """Skaluje wartość efektu rangą i poziomem postaci."""
        return skaluj_wartosc(self.gracz, self._klucz, baza)

    def T(self, baza: int) -> int:
        """Skaluje czas trwania efektu."""
        return czas_trwania(self.gracz, self._klucz, baza)

    def mag(self, lo: int, hi: int) -> int:
        """Losowe obrażenia magiczne z przedziału, po skalowaniu i wzmocnieniu."""
        a, b = self.S(lo), self.S(hi)
        return int(random.randint(min(a, b), max(a, b)) * self._wzmocnienie)


# ------------------------------------------------------------------ #
#  Handlery umiejętności — jeden efekt = jedna funkcja                 #
# ------------------------------------------------------------------ #

# ---- WOJOWNIK – klasa główna ----


def _sk_potezny_cios(k: Kontekst) -> str | None:
    mnoznik = 2.0 + 0.15 * (k.ranga - 1)
    obrazenia = int(_oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona) * mnoznik)
    k.przeciwnik.hp -= obrazenia
    k.stan["brak_obrony_tura"] = True
    print(f"  Zadajesz {obrazenia} obrażeń! (Tracisz obronę przy odwecie)")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_tarcza_wiary(k: Kontekst) -> str | None:
    k.stan["buff_obrona_mnoznik"] = 2.0
    k.stan["buff_obrona_tury"] = k.T(1)
    print(f"  Twoja obrona jest podwojona przez {k.stan['buff_obrona_tury']} tur(y) wroga!")
    return None


def _sk_okrzyk_bojowy(k: Kontekst) -> str | None:
    bonus = 0.30 + 0.05 * (k.ranga - 1)
    k.stan["buff_atak_mnoznik"] = 1.0 + bonus
    k.stan["buff_atak_tury"] = k.T(2)
    print(
        f"  Okrzyk bojowy! Atak +{int(bonus * 100)}%"
        f" przez {k.stan['buff_atak_tury']} tury!"
    )
    return None


def _sk_szal_berserka(k: Kontekst) -> str | None:
    bonus = 0.50 + 0.05 * (k.ranga - 1)
    k.stan["buff_atak_mnoznik"] = 1.0 + bonus
    k.stan["buff_atak_tury"] = k.T(3)
    k.stan["leczenie_zablokowane"] = True
    print(
        f"  Szał berserka! Atak +{int(bonus * 100)}%"
        f" przez {k.stan['buff_atak_tury']} tury — leczenie zablokowane!"
    )
    return None


# ---- WOJOWNIK – Paladyn ----


def _sk_boskie_swiatlo(k: Kontekst) -> str | None:
    lecz = k.S(50)
    wyleczone = min(lecz, k.gracz.max_hp - k.gracz.hp)
    k.gracz.hp += wyleczone
    print(f"  Boskie światło! Przywróciłeś {wyleczone} HP!")
    return None


def _sk_swiety_cios(k: Kontekst) -> str | None:
    mnoznik = 2.5 + 0.10 * (k.ranga - 1)
    bazowe = int(_oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona) * mnoznik)
    swiete = int(k.S(20) * mnoznik_typu(k.przeciwnik, "swiete"))
    obrazenia = bazowe + swiete
    k.przeciwnik.hp -= obrazenia
    print(
        f"  Zadajesz {obrazenia} obrażeń"
        f" ({bazowe} fizycznych + {swiete} świętych)!"
    )
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- WOJOWNIK – Barbarzyńca ----


def _sk_wscieklosc(k: Kontekst) -> str | None:
    bonus = 0.80 + 0.05 * (k.ranga - 1)
    tury = k.T(4)
    k.stan["buff_atak_mnoznik"] = 1.0 + bonus
    k.stan["buff_atak_tury"] = tury
    k.stan["buff_obrona_mnoznik"] = 0.5
    k.stan["buff_obrona_tury"] = tury
    print(
        f"  Wściekłość! Atak +{int(bonus * 100)}% przez {tury} tury"
        f" — obrona -50%!"
    )
    return None


def _sk_niszczace_uderzenie(k: Kontekst) -> str | None:
    pct = 0.30 + 0.04 * (k.ranga - 1)
    obrazenia = max(1, int(k.przeciwnik.hp * pct))
    k.przeciwnik.hp -= obrazenia
    print(
        f"  Niszczące uderzenie! Zadajesz {obrazenia} obrażeń"
        f" ({int(pct * 100)}% HP wroga)!"
    )
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- MAG – klasa główna ----


def _sk_kula_ognia(k: Kontekst) -> str | None:
    obrazenia = int(k.mag(35, 55) * mnoznik_typu(k.przeciwnik, "ogien"))
    k.przeciwnik.hp -= obrazenia
    print(f"  Kula ognia trafia za {obrazenia} obrażeń magicznych!{_dopisek_typu(k.przeciwnik, 'ogien')}")
    _podpal(k.przeciwnik, k.stan, 2)
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_lodowe_wiezy(k: Kontekst) -> str | None:
    k.stan["wrog_ogluszone_tury"] = 1 + (1 if k.ranga >= 4 else 0)
    print(
        f"  {k.przeciwnik.nazwa} jest zamrożony i pomija"
        f" {k.stan['wrog_ogluszone_tury']} tur(y)!"
    )
    return None


def _sk_tarcza_runowa(k: Kontekst) -> str | None:
    k.stan["tarcza_runowa"] = k.S(40)
    print(f"  Tarcza runowa aktywna! Absorbuje do {k.stan['tarcza_runowa']} obrażeń.")
    return None


def _sk_meteor(k: Kontekst) -> str | None:
    obrazenia = int(k.mag(80, 120) * mnoznik_typu(k.przeciwnik, "ogien"))
    k.przeciwnik.hp -= obrazenia
    print(f"  Meteor uderza za {obrazenia} obrażeń magicznych!{_dopisek_typu(k.przeciwnik, 'ogien')}")
    _podpal(k.przeciwnik, k.stan, 3)
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- MAG – Arcymag ----


def _sk_przyspieszenie_magiczne(k: Kontekst) -> str | None:
    k.stan["przyspieszenie"] = True
    print("  Przyspieszenie magiczne! Następny czar będzie darmowy i ×2 silniejszy.")
    return None


def _sk_kula_pioruna(k: Kontekst) -> str | None:
    obrazenia = k.mag(100, 150)
    k.przeciwnik.hp -= obrazenia
    print(f"  Kula pioruna uderza za {obrazenia} obrażeń magicznych!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- ŁOTRZYK – klasa główna ----


def _sk_cios_w_plecy(k: Kontekst) -> str | None:
    bazowe = _oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona)
    szansa = 0.40 + 0.05 * (k.ranga - 1)
    aktywuje = k.stan["tura"] == 1 or random.random() < szansa
    if aktywuje:
        obrazenia = bazowe * 2
        print(f"  Cios w plecy! Zadajesz {obrazenia} obrażeń (podwójne)!")
    else:
        obrazenia = bazowe
        print(f"  Zadajesz {obrazenia} obrażeń.")
    k.przeciwnik.hp -= obrazenia
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_trucizna(k: Kontekst) -> str | None:
    tury = k.T(3)
    dps = k.S(10)
    k.stan["wrog_trucizna_tury"] = tury
    k.stan["wrog_trucizna_obrazenia"] = dps
    print(
        f"  {k.przeciwnik.nazwa} jest zatruty!"
        f" Traci {dps} HP na turę przez {tury} tury."
    )
    return None


def _sk_dymna_bomba(k: Kontekst) -> str | None:
    print("  Rzucasz bombę dymną! Znikasz w chmurze dymu...")
    return "ucieczka"
    return None


def _sk_smiertelne_uderzenie(k: Kontekst) -> str | None:
    prog = 0.25 + 0.02 * (k.ranga - 1)
    if k.przeciwnik.hp < k.przeciwnik.max_hp * prog:
        mnoznik = 3.0 + 0.2 * (k.ranga - 1)
        obrazenia = int(_oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona) * mnoznik)
        print(f"  Śmiertelne uderzenie! Zadajesz {obrazenia} obrażeń!")
    else:
        obrazenia = _oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona)
        print(f"  Zadajesz {obrazenia} obrażeń (wróg zbyt silny na egzekucję).")
    k.przeciwnik.hp -= obrazenia
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- ŁOTRZYK – Zabójca ----


def _sk_cien_smierci(k: Kontekst) -> str | None:
    bazowe = _oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona)
    szansa = min(0.90, 0.60 + 0.05 * (k.ranga - 1))
    if random.random() < szansa:
        obrazenia = bazowe * 4
        print(f"  KRYTYCZNE TRAFIENIE! Cień śmierci zadaje {obrazenia} obrażeń!")
    else:
        obrazenia = bazowe
        print(f"  Cios chybił — zadajesz {obrazenia} obrażeń.")
    k.przeciwnik.hp -= obrazenia
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_egzekucja(k: Kontekst) -> str | None:
    prog = 0.15 + 0.02 * (k.ranga - 1)
    if k.przeciwnik.hp < k.przeciwnik.max_hp * prog:
        print(f"  Egzekucja! Kończysz {k.przeciwnik.nazwa} jednym ciosem!")
        k.przeciwnik.hp = 0
        return "wygrana"
    obrazenia = _oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona)
    k.przeciwnik.hp -= obrazenia
    print(f"  Zadajesz {obrazenia} obrażeń (wróg zbyt silny na egzekucję).")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- ŁOTRZYK – Zwiadowca ----


def _sk_unik(k: Kontekst) -> str | None:
    szansa = min(0.95, 0.75 + 0.05 * (k.ranga - 1))
    k.stan["unik_aktywny"] = True
    k.stan["unik_szansa"] = szansa
    print(
        f"  Przygotowujesz się do uniku!"
        f" ({int(szansa * 100)}% szans na ominięcie ataku wroga)"
    )
    return None


def _sk_grad_strzal(k: Kontekst) -> str | None:
    strzaly = 3 + (k.ranga - 1) // 2
    total = 0
    trafienia = 0
    for _ in range(strzaly):
        if not k.przeciwnik.zyje():
            break
        dam = _oblicz_obrazenia(k.efektywny_atak, k.przeciwnik.obrona)
        k.przeciwnik.hp = max(0, k.przeciwnik.hp - dam)
        total += dam
        trafienia += 1
    print(f"  Grad strzał: {trafienia} trafień za łącznie {total} obrażeń!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- MAG – Mroczny mag ----


def _sk_mroczna_strzala(k: Kontekst) -> str | None:
    obrazenia = k.mag(45, 70)
    k.przeciwnik.hp -= obrazenia
    print(f"  Mroczna strzała trafia za {obrazenia} obrażeń mrocznych!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_klatwa_mroku(k: Kontekst) -> str | None:
    tury = k.T(3)
    dps = k.S(15)
    k.stan["wrog_oslabienie_tury"] = tury
    k.stan["wrog_trucizna_tury"] = tury
    k.stan["wrog_trucizna_obrazenia"] = dps
    print(
        f"  Klątwa mroku! {k.przeciwnik.nazwa} zadaje 50% mniej obrażeń"
        f" i traci {dps} HP/turę przez {tury} tury."
    )
    return None


# ---- DRUID – klasa główna ----


def _sk_splot_korzeni(k: Kontekst) -> str | None:
    k.stan["wrog_ogluszone_tury"] = 1 + (1 if k.ranga >= 4 else 0)
    print(
        f"  🌿  Sploty korzeni oplatają {k.przeciwnik.nazwa}!"
        f" Pomija {k.stan['wrog_ogluszone_tury']} tur(y)."
    )
    return None


def _sk_forma_niedzwiedzia(k: Kontekst) -> str | None:
    tury = k.T(4)
    _ustaw_forme(
        k.stan,
        "niedźwiedź",
        tury,
        forma_atak=1.20 + 0.04 * (k.ranga - 1),
        forma_obrona=1.40 + 0.06 * (k.ranga - 1),
        forma_regen=k.S(8),
    )
    print(
        f"  🐻  Przemieniasz się w niedźwiedzia na {tury} tury!"
        f" Atak ×{k.stan['forma_atak']:.2f}, obrona ×{k.stan['forma_obrona']:.2f},"
        f" +{k.stan['forma_regen']} HP/turę."
    )
    return None


def _sk_uzdrowienie(k: Kontekst) -> str | None:
    lecz = k.S(50)
    wyleczone = min(lecz, k.gracz.max_hp - k.gracz.hp)
    k.gracz.hp += wyleczone
    print(f"  💚  Uzdrowienie! Przywróciłeś {wyleczone} HP!")
    return None


def _sk_burza_natury(k: Kontekst) -> str | None:
    obrazenia = k.mag(40, 60)
    k.przeciwnik.hp -= obrazenia
    print(f"  ⛈  Burza natury uderza za {obrazenia} obrażeń żywiołowych!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_forma_wilka(k: Kontekst) -> str | None:
    tury = k.T(4)
    _ustaw_forme(
        k.stan,
        "wilk",
        tury,
        forma_atak=1.45 + 0.06 * (k.ranga - 1),
        forma_obrona=0.75,
        forma_kryt=0.15 + 0.03 * (k.ranga - 1),
    )
    print(
        f"  🐺  Przemieniasz się w wilka na {tury} tury!"
        f" Atak ×{k.stan['forma_atak']:.2f}, słabsza obrona,"
        f" +{int(k.stan['forma_kryt'] * 100)}% szansy na krytyk."
    )
    return None


def _sk_regeneracja(k: Kontekst) -> str | None:
    k.stan["regeneracja_hp"] = k.S(15)
    k.stan["regeneracja_tury"] = k.T(4)
    print(
        f"  🌱  Regeneracja! Będziesz odnawiać {k.stan['regeneracja_hp']} HP"
        f" na turę przez {k.stan['regeneracja_tury']} tury."
    )
    return None


def _sk_forma_kruka(k: Kontekst) -> str | None:
    tury = k.T(4)
    _ustaw_forme(
        k.stan,
        "kruk",
        tury,
        forma_atak=1.05,
        forma_unik=min(0.70, 0.40 + 0.05 * (k.ranga - 1)),
    )
    print(
        f"  🐦  Przemieniasz się w kruka na {tury} tury!"
        f" {int(k.stan['forma_unik'] * 100)}% szansy na unik ciosów."
    )
    return None


# ---- DRUID – Szaman ----


def _sk_totem_zycia(k: Kontekst) -> str | None:
    k.stan["regeneracja_hp"] = k.S(30)
    k.stan["regeneracja_tury"] = k.T(3)
    print(
        f"  🔺  Totem życia! Będziesz odnawiać {k.stan['regeneracja_hp']} HP"
        f" na turę przez {k.stan['regeneracja_tury']} tury."
    )
    return None


def _sk_forma_ducha(k: Kontekst) -> str | None:
    tury = k.T(4)
    _ustaw_forme(
        k.stan,
        "duch",
        tury,
        forma_unik=min(0.60, 0.30 + 0.04 * (k.ranga - 1)),
        forma_mana=k.S(8),
    )
    print(
        f"  👻  Przemieniasz się w ducha na {tury} tury!"
        f" {int(k.stan['forma_unik'] * 100)}% uniku,"
        f" +{k.stan['forma_mana']} many na turę."
    )
    return None


def _sk_piorun_szamana(k: Kontekst) -> str | None:
    obrazenia = k.mag(70, 100)
    k.przeciwnik.hp -= obrazenia
    print(f"  ⚡  Piorun szamana uderza za {obrazenia} obrażeń błyskawicznych!")
    szansa_stun = min(0.90, 0.50 + 0.08 * (k.ranga - 1))
    if random.random() < szansa_stun:
        k.stan["wrog_ogluszone_tury"] = 1
        print(f"  {k.przeciwnik.nazwa} jest ogłuszony i pomija następną turę!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- DRUID – Strażnik Lasu ----


def _sk_kolce_natury(k: Kontekst) -> str | None:
    tury = k.T(4)
    dps = k.S(20)
    k.stan["wrog_trucizna_tury"] = tury
    k.stan["wrog_trucizna_obrazenia"] = dps
    print(
        f"  🌵  Kolce natury! {k.przeciwnik.nazwa} traci {dps} HP"
        f" na turę przez {tury} tury."
    )
    return None


def _sk_gniew_puszczy(k: Kontekst) -> str | None:
    aktywne = sum([
        k.stan["wrog_trucizna_tury"] > 0,
        k.stan["wrog_ogluszone_tury"] > 0,
        k.stan["wrog_oslabienie_tury"] > 0,
        k.stan["wrog_rozpad"],
    ])
    mnoznik = max(1, aktywne)
    lo, hi = k.S(50), k.S(80)
    obrazenia = int(random.randint(min(lo, hi), max(lo, hi)) * mnoznik * k._wzmocnienie)
    k.przeciwnik.hp -= obrazenia
    print(f"  🌲  Gniew puszczy! ×{mnoznik} efektów — zadajesz {obrazenia} obrażeń!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# ---- NEKROMANTA – klasa główna ----


def _sk_wysysanie_zycia(k: Kontekst) -> str | None:
    obrazenia = k.mag(30, 50)
    k.przeciwnik.hp -= obrazenia
    wyleczone = min(obrazenia, k.gracz.max_hp - k.gracz.hp)
    k.gracz.hp += wyleczone
    print(f"  🩸  Wysysasz {obrazenia} HP od {k.przeciwnik.nazwa}!")
    print(f"  Leczysz się o {wyleczone} HP!")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_przywolaj_szkielet(k: Kontekst) -> str | None:
    hp = k.S(28)
    atak = k.S(10)
    _ustaw_przyzwanie(k.stan, "Szkielet", "💀", hp, atak, przejecie=0.45)
    print(
        f"  💀  Przywołujesz szkielet! HP {hp}, atak {atak}."
        f" Może przejąć ciosy wroga."
    )
    return None


def _sk_klatwa_smierci(k: Kontekst) -> str | None:
    tury = k.T(3)
    k.stan["wrog_oslabienie_tury"] = tury
    print(
        f"  💀  Klątwa śmierci! {k.przeciwnik.nazwa} zadaje 50% mniej obrażeń"
        f" przez {tury} tury."
    )
    return None


def _sk_rozpad(k: Kontekst) -> str | None:
    if not k.stan["wrog_rozpad"]:
        k.stan["wrog_rozpad"] = True
        pct = 0.20 + 0.03 * (k.ranga - 1)
        utracone = max(1, int(k.przeciwnik.max_hp * pct))
        k.przeciwnik.max_hp -= utracone
        k.przeciwnik.hp = min(k.przeciwnik.hp, k.przeciwnik.max_hp)
        print(
            f"  🦴  Rozpad! {k.przeciwnik.nazwa} traci {utracone}"
            f" maksymalnego HP ({int(pct * 100)}% — teraz {k.przeciwnik.max_hp})."
        )
    else:
        print(f"  Rozpad już działa na {k.przeciwnik.nazwa}.")
    return None


def _sk_przywolaj_ghul(k: Kontekst) -> str | None:
    hp = k.S(50)
    atak = k.S(12)
    _ustaw_przyzwanie(k.stan, "Ghul", "🧟", hp, atak, przejecie=0.70)
    print(
        f"  🧟  Przywołujesz ghula! HP {hp}, atak {atak}."
        f" Chętnie przejmuje ciosy."
    )
    return None


def _sk_dotyk_smierci(k: Kontekst) -> str | None:
    tury = k.T(3)
    dps = k.S(25)
    k.stan["wrog_trucizna_tury"] = tury
    k.stan["wrog_trucizna_obrazenia"] = dps
    print(
        f"  ☠  Dotyk śmierci! {k.przeciwnik.nazwa} traci {dps} HP"
        f" na turę przez {tury} tury."
    )
    return None


# ---- NEKROMANTA – Lich ----


def _sk_fala_smierci(k: Kontekst) -> str | None:
    obrazenia = k.mag(60, 90)
    k.przeciwnik.hp -= obrazenia
    pct_lecz = 0.30 + 0.05 * (k.ranga - 1)
    wyleczone = min(int(obrazenia * pct_lecz), k.gracz.max_hp - k.gracz.hp)
    k.gracz.hp += wyleczone
    print(f"  💀  Fala śmierci uderza za {obrazenia} obrażeń!")
    print(f"  Leczysz się o {wyleczone} HP ({int(pct_lecz * 100)}% obrażeń).")
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


def _sk_przywolaj_widmo(k: Kontekst) -> str | None:
    hp = k.S(32)
    atak = k.S(16)
    _ustaw_przyzwanie(
        k.stan, "Widmo", "👻", hp, atak, przejecie=0.40, przebicie=0.5
    )
    print(
        f"  👻  Przywołujesz widmo z Otchłani! HP {hp}, atak {atak}."
        f" Ataki ignorują połowę obrony wroga."
    )
    return None


def _sk_wiecznie_zywi(k: Kontekst) -> str | None:
    hp = k.S(40)
    k.stan["lich_ochrona"] = True
    k.stan["lich_ochrona_hp"] = hp
    print(
        f"  💀  Ochrona Licha aktywna! Jeśli miałbyś umrzeć,"
        f" zamiast tego odzyskasz {hp} HP (raz)."
    )
    return None


# ---- NEKROMANTA – Kapłan Mroku ----


def _sk_pakt_krwi(k: Kontekst) -> str | None:
    koszt_hp = max(8, 20 - 2 * (k.ranga - 1))
    k.gracz.hp = max(1, k.gracz.hp - koszt_hp)
    mnoznik = 3.0 + 0.25 * (k.ranga - 1)
    k.stan["nastepny_atak_mnoznik"] = mnoznik
    print(
        f"  🗡  Pakt krwi! Tracisz {koszt_hp} HP."
        f" Następny atak zadaje ×{mnoznik:.2f} obrażeń!"
    )
    return None


def _sk_krwawy_sluga(k: Kontekst) -> str | None:
    koszt_hp = max(10, k.S(18))
    zaplacone = min(koszt_hp, max(0, k.gracz.hp - 1))
    k.gracz.hp -= zaplacone
    hp = k.S(45) + zaplacone // 2
    atak = k.S(18)
    _ustaw_przyzwanie(k.stan, "Krwawy sługa", "🩸", hp, atak, przejecie=0.55)
    print(
        f"  🩸  Poświęcasz {zaplacone} HP i przywołujesz krwawego sługę!"
        f" HP {hp}, atak {atak}."
    )
    return None


def _sk_ofiarny_rytual(k: Kontekst) -> str | None:
    pct = 0.30
    mnoznik = 3.0 + 0.25 * (k.ranga - 1)
    poswiecenie = max(1, int(k.gracz.hp * pct))
    k.gracz.hp = max(1, k.gracz.hp - poswiecenie)
    obrazenia = int(poswiecenie * mnoznik)
    k.przeciwnik.hp -= obrazenia
    print(
        f"  🩸  Ofiarny rytuał! Poświęcasz {poswiecenie} HP —"
        f" {k.przeciwnik.nazwa} traci {obrazenia} HP!"
    )
    if not k.przeciwnik.zyje():
        return "wygrana"
    return None


# Rejestr umiejętności: klucz z UMIEJETNOSCI → funkcja obsługi.
# Dodanie nowego skilla to nowa funkcja i jeden wpis, a nie kolejny elif.
HANDLERY_UMIEJETNOSCI: dict[str, Callable[["Kontekst"], str | None]] = {
    "potezny_cios": _sk_potezny_cios,
    "tarcza_wiary": _sk_tarcza_wiary,
    "okrzyk_bojowy": _sk_okrzyk_bojowy,
    "szal_berserka": _sk_szal_berserka,
    "boskie_swiatlo": _sk_boskie_swiatlo,
    "swiety_cios": _sk_swiety_cios,
    "wscieklosc": _sk_wscieklosc,
    "niszczace_uderzenie": _sk_niszczace_uderzenie,
    "kula_ognia": _sk_kula_ognia,
    "lodowe_wiezy": _sk_lodowe_wiezy,
    "tarcza_runowa": _sk_tarcza_runowa,
    "meteor": _sk_meteor,
    "przyspieszenie_magiczne": _sk_przyspieszenie_magiczne,
    "kula_pioruna": _sk_kula_pioruna,
    "cios_w_plecy": _sk_cios_w_plecy,
    "trucizna": _sk_trucizna,
    "dymna_bomba": _sk_dymna_bomba,
    "smiertelne_uderzenie": _sk_smiertelne_uderzenie,
    "cien_smierci": _sk_cien_smierci,
    "egzekucja": _sk_egzekucja,
    "unik": _sk_unik,
    "grad_strzal": _sk_grad_strzal,
    "mroczna_strzala": _sk_mroczna_strzala,
    "klatwa_mroku": _sk_klatwa_mroku,
    "splot_korzeni": _sk_splot_korzeni,
    "forma_niedzwiedzia": _sk_forma_niedzwiedzia,
    "uzdrowienie": _sk_uzdrowienie,
    "burza_natury": _sk_burza_natury,
    "forma_wilka": _sk_forma_wilka,
    "regeneracja": _sk_regeneracja,
    "forma_kruka": _sk_forma_kruka,
    "totem_zycia": _sk_totem_zycia,
    "forma_ducha": _sk_forma_ducha,
    "piorun_szamana": _sk_piorun_szamana,
    "kolce_natury": _sk_kolce_natury,
    "gniew_puszczy": _sk_gniew_puszczy,
    "wysysanie_zycia": _sk_wysysanie_zycia,
    "przywolaj_szkielet": _sk_przywolaj_szkielet,
    "klatwa_smierci": _sk_klatwa_smierci,
    "rozpad": _sk_rozpad,
    "przywolaj_ghul": _sk_przywolaj_ghul,
    "dotyk_smierci": _sk_dotyk_smierci,
    "fala_smierci": _sk_fala_smierci,
    "przywolaj_widmo": _sk_przywolaj_widmo,
    "wiecznie_zywi": _sk_wiecznie_zywi,
    "pakt_krwi": _sk_pakt_krwi,
    "krwawy_sluga": _sk_krwawy_sluga,
    "ofiarny_rytual": _sk_ofiarny_rytual,
}


# ------------------------------------------------------------------ #
#  Dyspozytor                                                          #
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

    # Arcymag: przyspieszenie — następny czar darmowy i 2× silniejszy
    wzmocnienie = 1.0
    if stan["przyspieszenie"] and koszt > 0:
        wzmocnienie = 2.0
        koszt = 0
        stan["przyspieszenie"] = False
        print("\n  ⚡ Przyspieszenie magiczne! Obrażenia ×2, mana darmowa!")

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
            * _mnoznik_gracza(gracz, przeciwnik, stan)
        ),
    )
    stan["nastepny_atak_mnoznik"] = 1.0

    handler = HANDLERY_UMIEJETNOSCI.get(klucz)
    if handler is None:
        print("  Ta umiejętność jeszcze nic nie robi.")
        return None

    return handler(
        Kontekst(
            gracz=gracz,
            przeciwnik=przeciwnik,
            stan=stan,
            ranga=ranga,
            efektywny_atak=efektywny_atak,
            _klucz=klucz,
            _wzmocnienie=wzmocnienie,
        )
    )

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
        zapas = getattr(gracz, "przedmioty", None) or {}
        for nr, (klucz, ikona, nazwa, opis) in enumerate(_PRZEDMIOTY_BOJOWE, 5):
            if zapas.get(klucz, 0) > 0:
                print(f"  [{nr}] {ikona}  {nazwa} ({zapas[klucz]}) — {opis}")
        print("  [0] ↩  Wróć\n")

        wybor = input("  Wybierz przedmiot: ").strip()
        if wybor == "0":
            return False
        if wybor.isdigit() and 5 <= int(wybor) < 5 + len(_PRZEDMIOTY_BOJOWE):
            if _uzyj_przedmiotu_bojowego(gracz, stan, _PRZEDMIOTY_BOJOWE[int(wybor) - 5][0]):
                return True
            continue

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
        _pokaz_zapowiedz(przeciwnik, stan)
        print("\n  Co robisz?")
        print("  [1] ⚔  Atakuj")
        if stan["leczenie_zablokowane"]:
            print("  [2] 🧪  Przedmioty — leczenie ZABLOKOWANE (szał berserka)")
        else:
            print(f"  [2] 🧪  Przedmioty (mikstury: {gracz.mikstury})")
        print("  [3] ✨  Umiejętności")
        print(f"  [4] 🏃  Uciekaj (szansa {int(szansa_ucieczki(gracz, stan) * 100)}%)")
        print("  [5] 🛡  Garda — połowa obrażeń w tej turze, potem kontra")

        wybor = input("\n  Twój wybór: ").strip()

        if wybor == "1":
            efektywny_atak = max(
                1,
                int(
                    gracz.atak
                    * stan["buff_atak_mnoznik"]
                    * stan["nastepny_atak_mnoznik"]
                    * float(stan.get("forma_atak") or 1.0)
                    * _mnoznik_gracza(gracz, przeciwnik, stan)
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
            if stan.get("olej_tury", 0) > 0 and przeciwnik.zyje():
                ogien = int((8 + 3 * gracz.poziom) * mnoznik_typu(przeciwnik, "ogien"))
                przeciwnik.hp -= ogien
                print(f"  🛢  Płonące ostrze dokłada {ogien} obrażeń od ognia!{_dopisek_typu(przeciwnik, 'ogien')}")
                _podpal(przeciwnik, stan, 1)
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
            if random.random() < szansa_ucieczki(gracz, stan):
                print("\n  🏃  Udało ci się uciec!")
                return "ucieczka"
            print("\n  🏃  Nie udało się uciec — przeciwnik blokuje drogę!")
            return None

        elif wybor == "5":
            stan["garda"] = True
            print("\n  🛡  Przyjmujesz gardę. Czekasz na jego ruch.")
            return None

        else:
            print("  Nieprawidłowy wybór. Wpisz 1–5.")


# ------------------------------------------------------------------ #
#  Tura przeciwnika                                                   #
# ------------------------------------------------------------------ #

def szansa_ucieczki(gracz: Gracz, stan: dict) -> float:
    """Ucieczka zależy od Zręczności, klasy, ran i talentów — nie jest rzutem monetą."""
    from game.atrybuty import modyfikator

    szansa = 0.35 + 0.06 * modyfikator(gracz, "zrecznosc")
    if gracz.klasa == "Lotrzyk":
        szansa += 0.15
    if talenty.ma(gracz, "szosty_zmysl"):
        szansa += 0.15
    szansa += przetrwanie.premia_ucieczki(gracz)
    if stan.get("jest_boss"):
        szansa -= 0.25
    if getattr(gracz, "tryb_trudnosci", "normalny") == "hardcore":
        szansa -= 0.10
    return max(0.05, min(0.90, szansa))


def _mnoznik_gracza(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> float:
    """Wszystko, co zmienia siłę ciosu poza buffami umiejętności."""
    m = przetrwanie.mnoznik_ataku(gracz)
    if stan.get("eliksir_sily_tury", 0) > 0:
        m *= 1.4
    if stan.get("jest_boss") and talenty.ma(gracz, "zabojca_potworow"):
        m *= 1.2
    return m


def _dopisek_typu(przeciwnik: Przeciwnik, typ: str) -> str:
    m = mnoznik_typu(przeciwnik, typ)
    if m > 1:
        return f"  ({NAZWY_TYPOW[typ]}: SŁABOŚĆ!)"
    if m < 1:
        return f"  ({NAZWY_TYPOW[typ]}: odporny)"
    return ""


def _podpal(przeciwnik: Przeciwnik, stan: dict, tury: int) -> None:
    if "ogien" in przeciwnik.odpornosci:
        return
    if stan.get("wrog_podpalony", 0) == 0 and przeciwnik.regeneracja:
        print(f"  🔥  {przeciwnik.nazwa} płonie — ogień nie pozwala ranom się zasklepić!")
    stan["wrog_podpalony"] = max(stan.get("wrog_podpalony", 0), tury)


def _pokaz_wiedze_o_wrogu(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Bierny test wiedzy: kto ma głowę (INT/MDR), ten widzi słabości wroga."""
    if stan.get("wiedza_pokazana") is not None:
        if stan["wiedza_pokazana"]:
            print(stan["wiedza_pokazana"])
        return
    from game.atrybuty import modyfikator

    wiedza = 10 + max(modyfikator(gracz, "inteligencja"), modyfikator(gracz, "madrosc"))
    if talenty.ma(gracz, "wewnetrzny_glos"):
        wiedza += 2
    czesci = []
    if przeciwnik.slabosci:
        czesci.append("słaby na: " + ", ".join(NAZWY_TYPOW[t] for t in sorted(przeciwnik.slabosci)))
    if przeciwnik.odpornosci:
        czesci.append("odporny na: " + ", ".join(NAZWY_TYPOW[t] for t in sorted(przeciwnik.odpornosci)))
    if przeciwnik.regeneracja:
        czesci.append("regeneruje się (ogień to przerywa)")
    if czesci and wiedza >= 12:
        stan["wiedza_pokazana"] = f"  🧠  WIEDZA [bierny test ST 12: sukces] — {przeciwnik.nazwa}: {'; '.join(czesci)}."
        print(stan["wiedza_pokazana"])
    else:
        stan["wiedza_pokazana"] = ""


_OPISY_ZAPOWIEDZI = {
    "ciezki_cios": "bierze potężny zamach — następny cios będzie podwójny. Garda zatrzyma 70%.",
    "ogien": "nabiera powietrza — w następnej turze zieje ogniem, obrona nie pomoże. Garda −50%, eliksir ognioodporności −50%.",
    "klatwa": "zaczyna inkantację klątwy. Zadaj mocny cios (20% jego życia) albo go ogłusz, by ją przerwać.",
}


def _pokaz_zapowiedz(przeciwnik: Przeciwnik, stan: dict) -> None:
    typ = stan.get("zapowiedz")
    if typ:
        print(f"\n  ⚠  ZAPOWIEDŹ: {przeciwnik.nazwa} {_OPISY_ZAPOWIEDZI[typ]}")


def _plomienie_i_regeneracja(przeciwnik: Przeciwnik, stan: dict) -> bool:
    """Ogień pali, regeneracja leczy — chyba że ogień ją blokuje. True = wróg padł."""
    if stan.get("wrog_podpalony", 0) > 0:
        dmg = max(3, int(przeciwnik.max_hp * 0.04 * mnoznik_typu(przeciwnik, "ogien")))
        przeciwnik.hp = max(0, przeciwnik.hp - dmg)
        stan["wrog_podpalony"] -= 1
        print(f"  🔥  {przeciwnik.nazwa} płonie: −{dmg} HP.")
        if not przeciwnik.zyje():
            return True
    elif przeciwnik.regeneracja and przeciwnik.hp < przeciwnik.max_hp:
        regen = max(3, int(przeciwnik.max_hp * przeciwnik.regeneracja))
        faktyczne = min(regen, przeciwnik.max_hp - przeciwnik.hp)
        przeciwnik.hp += faktyczne
        print(f"  💚  {przeciwnik.nazwa} regeneruje {faktyczne} HP! (ogień by to przerwał)")
    return False


def _szal_bossa(przeciwnik: Przeciwnik, stan: dict) -> None:
    if stan.get("jest_boss") and not przeciwnik.szal_uzyty and przeciwnik.hp <= przeciwnik.max_hp * 0.5:
        przeciwnik.szal_uzyty = True
        przeciwnik.atak = int(przeciwnik.atak * 1.25)
        print(f"  🔥  {przeciwnik.nazwa} wpada w SZAŁ! Atak +25% do końca walki.")


def _zadaj_graczowi(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict, obrazenia: int, opis: str) -> None:
    obrazenia = _przejmij_obrazenia_sluga(stan, obrazenia, przeciwnik.nazwa)
    if obrazenia <= 0:
        return
    gracz.hp = max(0, gracz.hp - obrazenia)
    print(f"  {opis} Otrzymujesz {obrazenia} obrażeń!")
    if _sprobuj_ocalic(gracz, stan):
        return
    rana = przetrwanie.moze_zranic(gracz, obrazenia, stan["jest_boss"])
    if rana:
        print(rana)


def _wykonaj_zapowiedz(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    typ = stan.pop("zapowiedz")
    stan["zapowiedz"] = None
    garda = stan.get("garda")
    if typ == "ciezki_cios":
        obr = _oblicz_obrazenia(przeciwnik.atak, gracz.obrona) * 2
        if garda:
            obr = max(1, int(obr * 0.3))
        _zadaj_graczowi(gracz, przeciwnik, stan, obr,
                        f"💥  {przeciwnik.nazwa} spuszcza potężny cios{' na twoją gardę' if garda else ''}!")
    elif typ == "ogien":
        from game.atrybuty import szansa_uniku_zrecznosc
        if random.random() < float(stan.get("forma_unik") or 0) or (
            not przetrwanie.bez_uniku(gracz) and random.random() < szansa_uniku_zrecznosc(gracz)
        ):
            print(f"  💨  Uskakujesz przed ogniem {przeciwnik.nazwa}!")
            return
        baza = random.randint(16, 28) * (1 + 0.10 * (przeciwnik.poziom - 1))
        if stan.get("jest_boss"):
            baza *= 1.4
        if garda:
            baza *= 0.5
        if stan.get("odpornosc_ognia"):
            baza *= 0.5
        obr = int(baza)
        if stan["tarcza_runowa"] > 0:
            absorbcja = min(stan["tarcza_runowa"], obr)
            stan["tarcza_runowa"] -= absorbcja
            obr -= absorbcja
            print(f"  🔮  Tarcza runowa absorbuje {absorbcja} obrażeń ognia!")
        _zadaj_graczowi(gracz, przeciwnik, stan, obr, f"🔥  {przeciwnik.nazwa} zieje ogniem!")
    elif typ == "klatwa":
        strata = stan.get("hp_przy_zapowiedzi", przeciwnik.hp) - przeciwnik.hp
        if strata >= przeciwnik.max_hp * 0.2:
            print(f"  ✋  Twój cios przerywa inkantację {przeciwnik.nazwa}! Klątwa się rozpada.")
            return
        stan["gracz_trucizna_tury"] = max(stan["gracz_trucizna_tury"], 3)
        stan["gracz_trucizna_obrazenia"] = int((12 if stan["jest_boss"] else 7) * (1 + 0.1 * (przeciwnik.poziom - 1)))
        print(f"  ⚗  {przeciwnik.nazwa} kończy klątwę! Trucizna: −{stan['gracz_trucizna_obrazenia']} HP na turę przez 3 tury (antidotum pomoże).")


def _po_turze_wroga(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Koniec tury wroga: garda przechodzi w kontrę, wróg może zapowiedzieć kolejny ruch."""
    if stan.get("garda"):
        stan["garda"] = False
        kontra = 1.6 if talenty.ma(gracz, "kontra") else 1.3
        stan["nastepny_atak_mnoznik"] = max(stan["nastepny_atak_mnoznik"], kontra)
        print(f"  🛡  Garda otwiera kontrę: następny cios ×{kontra}.")
    if not gracz.zyje() or not przeciwnik.zyje() or stan.get("zapowiedz"):
        return
    for typ, szansa in przeciwnik.zapowiedzi.items():
        if random.random() < szansa:
            stan["zapowiedz"] = typ
            stan["hp_przy_zapowiedzi"] = przeciwnik.hp
            print(f"  ⚠  {przeciwnik.nazwa} {_OPISY_ZAPOWIEDZI[typ]}")
            return


_PRZEDMIOTY_BOJOWE = [
    ("bandaz", "🩹", "Bandaż", "zatrzymuje krwawienie i opatruje ranę"),
    ("eliksir_sily", "💪", "Eliksir siły", "atak +40% przez 3 tury"),
    ("eliksir_ognia", "🧯", "Eliksir ognioodporności", "ogień −50% do końca walki"),
    ("olej_ognisty", "🛢", "Olej ognisty", "płonące ostrze na 3 tury, blokuje regenerację"),
    ("bomba", "💣", "Bomba", "50+ obrażeń, ignoruje obronę"),
]


def _uzyj_przedmiotu_bojowego(gracz: Gracz, stan: dict, klucz: str) -> bool:
    zapas = getattr(gracz, "przedmioty", None) or {}
    if zapas.get(klucz, 0) <= 0:
        print("  Nie masz tego przedmiotu.")
        return False
    zapas[klucz] -= 1
    dlugo = 2 if talenty.ma(gracz, "kamien_filozoficzny") else 1
    if klucz == "bandaz":
        stan["gracz_krwawienie_tury"] = 0
        msg = przetrwanie.wylecz_jedna(gracz, 3 if talenty.ma(gracz, "polowy_medyk") else 2)
        print("  🩹  Opatrujesz się. Krwawienie ustaje." + (f"\n{msg}" if msg else ""))
    elif klucz == "eliksir_sily":
        stan["eliksir_sily_tury"] = 3 * dlugo
        print(f"  💪  Mięśnie płoną siłą: atak +40% przez {3 * dlugo} tury.")
    elif klucz == "eliksir_ognia":
        stan["odpornosc_ognia"] = True
        print("  🧯  Skóra chłodnieje. Ogień zrani cię o połowę słabiej.")
    elif klucz == "olej_ognisty":
        stan["olej_tury"] = 3 * dlugo
        print(f"  🛢  Nacierasz ostrze olejem i podpalasz. Płonie przez {3 * dlugo} tury.")
    elif klucz == "bomba":
        wrog = stan.get("_wrog")
        if wrog is not None:
            obr = int(50 * (1 + 0.10 * (gracz.poziom - 1)))
            wrog.hp -= obr
            print(f"  💣  Bomba wybucha! {wrog.nazwa} traci {obr} HP.")
            _podpal(wrog, stan, 1)
    return True


def _tura_przeciwnika(gracz: Gracz, przeciwnik: Przeciwnik, stan: dict) -> None:
    """Obsługuje turę przeciwnika z uwzględnieniem aktywnych efektów."""

    # Trucizna na wroga (odporność ×0.5, słabość ×1.5)
    if stan["wrog_trucizna_tury"] > 0:
        dam = max(1, int(stan["wrog_trucizna_obrazenia"] * mnoznik_typu(przeciwnik, "trucizna")))
        przeciwnik.hp = max(0, przeciwnik.hp - dam)
        stan["wrog_trucizna_tury"] -= 1
        print(
            f"  ☠  Trucizna! {przeciwnik.nazwa} traci {dam} HP."
            f" (Pozostało tur: {stan['wrog_trucizna_tury']})"
        )
        if not przeciwnik.zyje():
            return

    if _plomienie_i_regeneracja(przeciwnik, stan):
        return

    # Ogłuszenie wroga — przerywa też zapowiedziany ruch
    if stan["wrog_ogluszone_tury"] > 0:
        stan["wrog_ogluszone_tury"] -= 1
        if stan.get("zapowiedz"):
            print(f"  ❄  Ogłuszenie przerywa to, co {przeciwnik.nazwa} szykował!")
            stan["zapowiedz"] = None
        print(f"  ❄  {przeciwnik.nazwa} jest ogłuszony i pomija turę!")
        _po_turze_wroga(gracz, przeciwnik, stan)
        return

    _szal_bossa(przeciwnik, stan)
    if stan.get("zapowiedz"):
        _wykonaj_zapowiedz(gracz, przeciwnik, stan)
        _po_turze_wroga(gracz, przeciwnik, stan)
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
    if stan.get("garda"):
        obrazenia = max(1, obrazenia // 2)

    # Osłabienie (Klątwa śmierci)
    if stan["wrog_oslabienie_tury"] > 0:
        obrazenia = max(1, int(obrazenia * 0.5))
        stan["wrog_oslabienie_tury"] -= 1

    # Unik (Zwiadowca)
    if stan["unik_aktywny"]:
        stan["unik_aktywny"] = False
        if random.random() < stan.get("unik_szansa", 0.75):
            print(f"  💨  Uniknąłeś ataku {przeciwnik.nazwa}!")
            _po_turze_wroga(gracz, przeciwnik, stan)
            return
        print(f"  💨  Próbowałeś uniknąć, ale {przeciwnik.nazwa} trafił!")

    form_unik = float(stan.get("forma_unik") or 0)
    if form_unik > 0 and random.random() < form_unik:
        print(
            f"  Unikasz ataku {przeciwnik.nazwa}"
            f" w postaci {stan.get('forma')}!"
        )
        _po_turze_wroga(gracz, przeciwnik, stan)
        return

    from game.atrybuty import szansa_uniku_zrecznosc
    pasywny_unik = 0.0 if przetrwanie.bez_uniku(gracz) else szansa_uniku_zrecznosc(gracz)
    if pasywny_unik > 0 and random.random() < pasywny_unik:
        print(f"  💨  Zręczność! Unikasz ciosu {przeciwnik.nazwa}!")
        _po_turze_wroga(gracz, przeciwnik, stan)
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
        _po_turze_wroga(gracz, przeciwnik, stan)
        return

    gracz.hp = max(0, gracz.hp - obrazenia)

    if obrazenia > 0:
        garda = "  (garda)" if stan.get("garda") else ""
        print(f"  💀  {przeciwnik.nazwa} atakuje cię! Otrzymujesz {obrazenia} obrażeń.{garda}")
    else:
        print(f"  🔮  Tarcza runowa całkowicie zablokowała atak {przeciwnik.nazwa}!")

    if _sprobuj_ocalic(gracz, stan):
        _po_turze_wroga(gracz, przeciwnik, stan)
        return
    rana = przetrwanie.moze_zranic(gracz, obrazenia, stan["jest_boss"])
    if rana:
        print(rana)

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
    _po_turze_wroga(gracz, przeciwnik, stan)


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
    from game.rzemioslo import lup_skladnikow
    for msg in lup_skladnikow(gracz, przeciwnik, jest_boss):
        print(msg)
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
    """Główna pętla walki. Zwraca: 'wygrana', 'przegrana' lub 'ucieczka'.

    Na czas walki okno graficzne (jeśli jest) rysuje arenę zamiast mapy.
    """
    from game import ekran
    try:
        return _walka(gracz, biom, jest_boss, przeciwnik)
    finally:
        ekran.koniec_walki()


def _walka(gracz: Gracz, biom: str | None, jest_boss: bool, przeciwnik: Przeciwnik | None) -> str:
    mapa_gen = getattr(gracz, "mapa_gen", 1)
    tryb = getattr(gracz, "tryb_trudnosci", "normalny")
    if przeciwnik is None:
        if jest_boss:
            przeciwnik = losuj_bossa(gracz.poziom, mapa_gen, tryb)
        else:
            przeciwnik = losuj_przeciwnika(gracz.poziom, biom, mapa_gen, tryb)
    stan = _nowy_stan_walki()
    stan["jest_boss"] = jest_boss
    stan["_wrog"] = przeciwnik
    from game import ekran
    ekran.ustaw_walke(gracz, przeciwnik, stan)

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
