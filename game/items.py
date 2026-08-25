"""Definicje ekwipunku — broń i zbroja z bonusami statystyk."""

from game.utils import wyswietl_linie, nacisnij_enter

# Słownik: klucz → dane przedmiotu ekwipunku
EKWIPUNEK: dict[str, dict] = {
    # ── Broń ──────────────────────────────────────────────────────────
    "sztylet": {
        "nazwa": "Sztylet",
        "typ": "bron",
        "bonus_atak": 3,
        "bonus_obrona": 0,
        "cena": 40,
        "ikona": "🗡",
        "opis": "Lekki i szybki nóż. Idealny dla łotrzyków.",
    },
    "miecz": {
        "nazwa": "Miecz",
        "typ": "bron",
        "bonus_atak": 7,
        "bonus_obrona": 0,
        "cena": 90,
        "ikona": "⚔",
        "opis": "Solidna broń bojowa. Pasuje do wojowników.",
    },
    "topor": {
        "nazwa": "Topór Wojenny",
        "typ": "bron",
        "bonus_atak": 11,
        "bonus_obrona": -2,
        "cena": 130,
        "ikona": "🪓",
        "opis": "Potężny, lecz ciężki. Zmniejsza obronę o 2.",
    },
    "laska_maga": {
        "nazwa": "Laska Maga",
        "typ": "bron",
        "bonus_atak": 5,
        "bonus_obrona": 1,
        "cena": 75,
        "ikona": "🔮",
        "opis": "Kryształ wzmacnia zaklęcia i zapewnia lekką ochronę.",
    },
    "luk_elfi": {
        "nazwa": "Łuk Elficki",
        "typ": "bron",
        "bonus_atak": 9,
        "bonus_obrona": 0,
        "cena": 115,
        "ikona": "🏹",
        "opis": "Precyzyjnie wykonany łuk elfich mistrzów.",
    },
    # ── Zbroja ────────────────────────────────────────────────────────
    "skorzana_zbroja": {
        "nazwa": "Skórzana Zbroja",
        "typ": "zbroja",
        "bonus_atak": 0,
        "bonus_obrona": 3,
        "cena": 50,
        "ikona": "🥋",
        "opis": "Lekka i wygodna. Pozwala swobodnie się poruszać.",
    },
    "plaszcz_lotrzyka": {
        "nazwa": "Płaszcz Łotrzyka",
        "typ": "zbroja",
        "bonus_atak": 1,
        "bonus_obrona": 4,
        "cena": 70,
        "ikona": "🥷",
        "opis": "Ciemny płaszcz ułatwiający skradanie się.",
    },
    "szata_maga": {
        "nazwa": "Szata Maga",
        "typ": "zbroja",
        "bonus_atak": 2,
        "bonus_obrona": 2,
        "cena": 85,
        "ikona": "👘",
        "opis": "Magicznie wzmocniona szata. Daje atak i obronę.",
    },
    "kolczuga": {
        "nazwa": "Kolczuga",
        "typ": "zbroja",
        "bonus_atak": 0,
        "bonus_obrona": 6,
        "cena": 100,
        "ikona": "🛡",
        "opis": "Solidna ochrona przed cięciem i kłuciem.",
    },
    "plytowa_zbroja": {
        "nazwa": "Zbroja Płytowa",
        "typ": "zbroja",
        "bonus_atak": 0,
        "bonus_obrona": 11,
        "cena": 190,
        "ikona": "🛡",
        "opis": "Ciężka, lecz niezawodna. Wymaga siły.",
    },
    "klinga_otchlani": {
        "nazwa": "Klinga Otchłani",
        "typ": "bron",
        "bonus_atak": 14,
        "bonus_obrona": 0,
        "cena": 0,
        "ikona": "🗡",
        "opis": "Ostrze z wymiaru cienia. Nie sprzedasz tego w zwykłym sklepie.",
    },
    "ostrze_smoka": {
        "nazwa": "Ostrze Smoka",
        "typ": "bron",
        "bonus_atak": 16,
        "bonus_obrona": -1,
        "cena": 0,
        "ikona": "⚔",
        "opis": "Wykuwane w żarze leża. Cięcie pali jak ogień.",
    },
    "zbroja_lusek": {
        "nazwa": "Zbroja z Łusek",
        "typ": "zbroja",
        "bonus_atak": 0,
        "bonus_obrona": 14,
        "cena": 0,
        "ikona": "🐉",
        "opis": "Pancerz ze smoczych łusek. Żar i kły odbijają się od niego.",
    },
    "plaszcz_niebios": {
        "nazwa": "Płaszcz Niebios",
        "typ": "zbroja",
        "bonus_atak": 4,
        "bonus_obrona": 8,
        "cena": 0,
        "ikona": "☁",
        "opis": "Tkanina z latającej wyspy. Lekka jak powietrze.",
    },
}

# Przedmioty dostępne w ogólnym sklepie (kupiec wędrowny)
SKLEP_ASORTYMENT = ["sztylet", "skorzana_zbroja", "plaszcz_lotrzyka", "laska_maga"]

# Przedmioty dostępne w kuźni (kowal)
KUZNIA_ASORTYMENT = ["miecz", "topor", "luk_elfi", "kolczuga", "plytowa_zbroja", "szata_maga"]


# ------------------------------------------------------------------ #
#  Pomocnicze operacje ekwipunku                                       #
# ------------------------------------------------------------------ #

def _opis_bonusu(item: dict) -> str:
    """Zwraca czytelny opis bonusów przedmiotu."""
    czesci = []
    if item["bonus_atak"] > 0:
        czesci.append(f"+{item['bonus_atak']} Atak")
    elif item["bonus_atak"] < 0:
        czesci.append(f"{item['bonus_atak']} Atak")
    if item["bonus_obrona"] > 0:
        czesci.append(f"+{item['bonus_obrona']} Obrona")
    elif item["bonus_obrona"] < 0:
        czesci.append(f"{item['bonus_obrona']} Obrona")
    return ", ".join(czesci) if czesci else "brak bonusów"


def _plecak(gracz) -> list:
    """Zwraca listę plecaka, tworząc ją gdy brakuje (stare zapisy)."""
    if not hasattr(gracz, "plecak") or gracz.plecak is None:
        gracz.plecak = []
    return gracz.plecak


def _zdejmij_slot(gracz, slot: str) -> str | None:
    """Zdejmuje przedmiot ze slotu do plecaka. Zwraca klucz lub None."""
    klucz = gracz.wyposazenie.get(slot)
    if not klucz or klucz not in EKWIPUNEK:
        return None
    item = EKWIPUNEK[klucz]
    gracz.atak -= item["bonus_atak"]
    gracz.obrona -= item["bonus_obrona"]
    gracz.wyposazenie[slot] = None
    _plecak(gracz).append(klucz)
    return klucz


def zaloz(gracz, klucz: str) -> str:
    """Zakłada przedmiot z plecaka. Zdjęty ekwipunek wraca do plecaka."""
    if klucz not in EKWIPUNEK:
        return "  Nieznany przedmiot."

    plecak = _plecak(gracz)
    if klucz not in plecak:
        return "  Nie masz tego przedmiotu w plecaku."

    item = EKWIPUNEK[klucz]
    slot = item["typ"]  # "bron" lub "zbroja"
    plecak.remove(klucz)
    _zdejmij_slot(gracz, slot)

    gracz.wyposazenie[slot] = klucz
    gracz.atak += item["bonus_atak"]
    gracz.obrona += item["bonus_obrona"]

    bonus_str = _opis_bonusu(item)
    return f"  Zakładasz: {item['ikona']} {item['nazwa']}  ({bonus_str})"


def dodaj_do_plecaka(gracz, klucz: str) -> None:
    """Dodaje przedmiot do plecaka (np. po zakupie lub dropie)."""
    if klucz in EKWIPUNEK:
        _plecak(gracz).append(klucz)


def cena_sprzedazy(klucz: str) -> int:
    """Cena odkupu — połowa wartości sklepowej."""
    if klucz not in EKWIPUNEK:
        return 0
    return max(1, EKWIPUNEK[klucz]["cena"] // 2)


def sprzedaj_przedmiot(gracz, klucz: str) -> str:
    """Sprzedaje przedmiot z plecaka. Zwraca komunikat."""
    plecak = _plecak(gracz)
    if klucz not in plecak:
        return "  Nie masz tego przedmiotu w plecaku."
    plecak.remove(klucz)
    zloto = cena_sprzedazy(klucz)
    gracz.zloto += zloto
    item = EKWIPUNEK[klucz]
    return f"  Sprzedano {item['ikona']} {item['nazwa']} za {zloto} złota."


def wyswietl_przedmiot(klucz: str, nr: int, gracz=None) -> None:
    """Wyświetla jeden przedmiot z jego statystykami."""
    item = EKWIPUNEK[klucz]
    bonus_str = _opis_bonusu(item)
    zalozony = ""
    if gracz and gracz.wyposazenie.get(item["typ"]) == klucz:
        zalozony = " [ZAŁOŻONY]"
    cena = item["cena"]
    if gracz is not None:
        from game.pochodzenie import cena_dla
        cena = cena_dla(gracz, item["cena"])
    print(
        f"  [{nr}] {item['ikona']} {item['nazwa']}{zalozony}"
        f"  —  {bonus_str}  —  {cena} złota"
    )
    print(f"       {item['opis']}")


def otworz_ekwipunek(gracz) -> None:
    """Wyświetla i zarządza założonym ekwipunkiem oraz plecakiem."""
    while True:
        wyswietl_linie()
        print("  🎒  EKWIPUNEK\n")
        bron_klucz = gracz.wyposazenie.get("bron")
        zbroja_klucz = gracz.wyposazenie.get("zbroja")
        plecak = _plecak(gracz)

        if bron_klucz and bron_klucz in EKWIPUNEK:
            item = EKWIPUNEK[bron_klucz]
            print(f"  Broń:   {item['ikona']} {item['nazwa']}  ({_opis_bonusu(item)})")
        else:
            print("  Broń:   — brak —")

        if zbroja_klucz and zbroja_klucz in EKWIPUNEK:
            item = EKWIPUNEK[zbroja_klucz]
            print(f"  Zbroja: {item['ikona']} {item['nazwa']}  ({_opis_bonusu(item)})")
        else:
            print("  Zbroja: — brak —")

        print(f"\n  Łączny Atak: {gracz.atak}   Łączna Obrona: {gracz.obrona}\n")

        if plecak:
            print("  ─── Plecak ───")
            for i, klucz in enumerate(plecak, 1):
                if klucz not in EKWIPUNEK:
                    print(f"  [{i}]  (nieznany przedmiot)")
                    continue
                item = EKWIPUNEK[klucz]
                print(
                    f"  [{i}] {item['ikona']} {item['nazwa']}"
                    f"  ({_opis_bonusu(item)})  — załóż"
                )
            print()
        else:
            print("  Plecak jest pusty.\n")

        opcje_extra: list[tuple[str, str]] = []
        if bron_klucz:
            opcje_extra.append(("Zdejmij broń", "zdejmij_bron"))
        if zbroja_klucz:
            opcje_extra.append(("Zdejmij zbroję", "zdejmij_zbroja"))

        offset = len(plecak)
        for i, (tekst, _) in enumerate(opcje_extra, 1):
            print(f"  [{offset + i}] {tekst}")
        print("  [0] Wróć\n")

        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return

        try:
            idx = int(wybor) - 1
        except ValueError:
            print("  Nieprawidłowy wybór.")
            nacisnij_enter()
            continue

        if 0 <= idx < len(plecak):
            print(zaloz(gracz, plecak[idx]))
            nacisnij_enter()
            continue

        extra_idx = idx - len(plecak)
        if 0 <= extra_idx < len(opcje_extra):
            _, akcja = opcje_extra[extra_idx]
            if akcja == "zdejmij_bron":
                zdjety = _zdejmij_slot(gracz, "bron")
                if zdjety:
                    item = EKWIPUNEK[zdjety]
                    print(f"  Zdjąłeś: {item['ikona']} {item['nazwa']}  (wraca do plecaka)")
            elif akcja == "zdejmij_zbroja":
                zdjety = _zdejmij_slot(gracz, "zbroja")
                if zdjety:
                    item = EKWIPUNEK[zdjety]
                    print(f"  Zdjąłeś: {item['ikona']} {item['nazwa']}  (wraca do plecaka)")
            nacisnij_enter()
            continue

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def menu_sprzedazy(gracz) -> None:
    """Pozwala sprzedać przedmioty z plecaka za połowę ceny."""
    while True:
        plecak = _plecak(gracz)
        wyswietl_linie()
        print(f"  💰  SPRZEDAŻ  |  Twoje złoto: {gracz.zloto} szt.\n")
        if not plecak:
            print("  Plecak jest pusty. Zdejmij ekwipunek, jeśli chcesz go sprzedać.")
            print("  [0] Wróć\n")
            wybor = input("  Twój wybór: ").strip()
            if wybor == "0":
                return
            continue

        print("  Przedmioty w plecaku (odkup za 50% ceny):\n")
        for i, klucz in enumerate(plecak, 1):
            if klucz not in EKWIPUNEK:
                continue
            item = EKWIPUNEK[klucz]
            print(
                f"  [{i}] {item['ikona']} {item['nazwa']}"
                f"  —  {cena_sprzedazy(klucz)} złota"
            )
        print("\n  [0] Wróć\n")

        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return
        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(plecak):
                print(sprzedaj_przedmiot(gracz, plecak[idx]))
                nacisnij_enter()
                continue
        except ValueError:
            pass
        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
