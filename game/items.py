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


def zaloz(gracz, klucz: str) -> str:
    """Zakłada przedmiot z inwentarza. Zwraca komunikat."""
    if klucz not in EKWIPUNEK:
        return "  Nieznany przedmiot."

    item = EKWIPUNEK[klucz]
    slot = item["typ"]  # "bron" lub "zbroja"

    # Zdejmij stary przedmiot z tego slotu, jeśli był
    stary_klucz = gracz.wyposazenie.get(slot)
    if stary_klucz and stary_klucz in EKWIPUNEK:
        stary = EKWIPUNEK[stary_klucz]
        gracz.atak -= stary["bonus_atak"]
        gracz.obrona -= stary["bonus_obrona"]

    gracz.wyposazenie[slot] = klucz
    gracz.atak += item["bonus_atak"]
    gracz.obrona += item["bonus_obrona"]

    bonus_str = _opis_bonusu(item)
    return f"  Zakładasz: {item['ikona']} {item['nazwa']}  ({bonus_str})"


def wyswietl_przedmiot(klucz: str, nr: int, gracz=None) -> None:
    """Wyświetla jeden przedmiot z jego statystykami."""
    item = EKWIPUNEK[klucz]
    bonus_str = _opis_bonusu(item)
    zalozony = ""
    if gracz and gracz.wyposazenie.get(item["typ"]) == klucz:
        zalozony = " [ZAŁOŻONY]"
    print(
        f"  [{nr}] {item['ikona']} {item['nazwa']}{zalozony}"
        f"  —  {bonus_str}  —  {item['cena']} złota"
    )
    print(f"       {item['opis']}")


def otworz_ekwipunek(gracz) -> None:
    """Wyświetla i zarządza założonym ekwipunkiem gracza."""
    while True:
        wyswietl_linie()
        print("  🎒  EKWIPUNEK\n")
        bron_klucz = gracz.wyposazenie.get("bron")
        zbroja_klucz = gracz.wyposazenie.get("zbroja")

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

        opcje = []
        if bron_klucz:
            opcje.append(("Zdejmij broń", "zdejmij_bron"))
        if zbroja_klucz:
            opcje.append(("Zdejmij zbroję", "zdejmij_zbroja"))

        for i, (tekst, _) in enumerate(opcje, 1):
            print(f"  [{i}] {tekst}")
        print("  [0] Wróć\n")

        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            return

        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(opcje):
                _, akcja = opcje[idx]
                if akcja == "zdejmij_bron" and bron_klucz and bron_klucz in EKWIPUNEK:
                    item = EKWIPUNEK[bron_klucz]
                    gracz.atak -= item["bonus_atak"]
                    gracz.obrona -= item["bonus_obrona"]
                    gracz.wyposazenie["bron"] = None
                    print(f"  Zdjąłeś: {item['ikona']} {item['nazwa']}")
                elif akcja == "zdejmij_zbroja" and zbroja_klucz and zbroja_klucz in EKWIPUNEK:
                    item = EKWIPUNEK[zbroja_klucz]
                    gracz.atak -= item["bonus_atak"]
                    gracz.obrona -= item["bonus_obrona"]
                    gracz.wyposazenie["zbroja"] = None
                    print(f"  Zdjąłeś: {item['ikona']} {item['nazwa']}")
                nacisnij_enter()
                continue
        except ValueError:
            pass

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
