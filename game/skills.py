"""Definicje umiejętności i podklas postaci."""

# Słownik umiejętności: klucz -> metadane
# klasa:    klasa główna (Wojownik / Mag / Lotrzyk)
# podklasa: None = skill klasy głównej, inaczej nazwa podklasy
# poziom:   poziom odblokowania
# koszt_many: 0 jeśli umiejętność nie używa many
UMIEJETNOSCI: dict[str, dict] = {

    # ------------------------------------------------------------------ #
    #  WOJOWNIK – klasa główna                                            #
    # ------------------------------------------------------------------ #
    "potezny_cios": {
        "nazwa": "Potężny cios",
        "opis": "2× obrażenia, ale obrona = 0 przy odwecie wroga",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 0,
        "ikona": "⚔",
    },
    "tarcza_wiary": {
        "nazwa": "Tarcza wiary",
        "opis": "Obrona ×2 przez jedną turę wroga",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 0,
        "ikona": "🛡",
    },
    "okrzyk_bojowy": {
        "nazwa": "Okrzyk bojowy",
        "opis": "+30% do ataku przez 2 tury",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 0,
        "ikona": "📣",
    },
    "szal_berserka": {
        "nazwa": "Szał berserka",
        "opis": "+50% do ataku przez 3 tury, ale HP nie może być leczone",
        "klasa": "Wojownik",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 0,
        "ikona": "🔥",
    },

    # Wojownik → Paladyn
    "boskie_swiatlo": {
        "nazwa": "Boskie światło",
        "opis": "Leczy 50 HP (działa nawet podczas szału berserka)",
        "klasa": "Wojownik",
        "podklasa": "Paladyn",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "✨",
    },
    "swiety_cios": {
        "nazwa": "Święty cios",
        "opis": "2.5× obrażenia + 20 obrażeń świętych",
        "klasa": "Wojownik",
        "podklasa": "Paladyn",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "🌟",
    },

    # Wojownik → Barbarzyńca
    "wscieklosc": {
        "nazwa": "Wściekłość",
        "opis": "+80% do ataku przez 4 tury, ale obrona -50%",
        "klasa": "Wojownik",
        "podklasa": "Barbarzynca",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "😡",
    },
    "niszczace_uderzenie": {
        "nazwa": "Niszczące uderzenie",
        "opis": "Zadaje 30% obecnego HP wroga jako obrażenia",
        "klasa": "Wojownik",
        "podklasa": "Barbarzynca",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "💥",
    },

    # ------------------------------------------------------------------ #
    #  MAG – klasa główna                                                  #
    # ------------------------------------------------------------------ #
    "kula_ognia": {
        "nazwa": "Kula ognia",
        "opis": "35–55 obrażeń magicznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 10,
        "ikona": "🔥",
    },
    "lodowe_wiezy": {
        "nazwa": "Lodowe więzy",
        "opis": "Wróg traci następną turę (ogłuszenie)",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 15,
        "ikona": "❄",
    },
    "tarcza_runowa": {
        "nazwa": "Tarcza runowa",
        "opis": "Absorbuje do 40 obrażeń w następnej turze wroga",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 15,
        "ikona": "🔮",
    },
    "meteor": {
        "nazwa": "Meteor",
        "opis": "80–120 obrażeń magicznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 25,
        "ikona": "☄",
    },

    # Mag → Nekromanta
    "wysysanie_zycia": {
        "nazwa": "Wysysanie życia",
        "opis": "30–50 obrażeń magicznych, gracz leczy się o tę samą ilość",
        "klasa": "Mag",
        "podklasa": "Nekromanta",
        "poziom": 6,
        "koszt_many": 20,
        "ikona": "🩸",
    },
    "klatwa_smierci": {
        "nazwa": "Klątwa śmierci",
        "opis": "Wróg zadaje 50% mniej obrażeń przez 3 tury",
        "klasa": "Mag",
        "podklasa": "Nekromanta",
        "poziom": 9,
        "koszt_many": 20,
        "ikona": "💀",
    },

    # Mag → Arcymag
    "przyspieszenie_magiczne": {
        "nazwa": "Przyspieszenie magiczne",
        "opis": "Następny czar jest darmowy i zadaje podwójne obrażenia",
        "klasa": "Mag",
        "podklasa": "Arcymag",
        "poziom": 6,
        "koszt_many": 10,
        "ikona": "⚡",
    },
    "kula_pioruna": {
        "nazwa": "Kula pioruna",
        "opis": "100–150 obrażeń magicznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": "Arcymag",
        "poziom": 9,
        "koszt_many": 30,
        "ikona": "⚡",
    },

    # ------------------------------------------------------------------ #
    #  ŁOTRZYK – klasa główna                                             #
    # ------------------------------------------------------------------ #
    "cios_w_plecy": {
        "nazwa": "Cios w plecy",
        "opis": "40% szans na 2× obrażenia (gwarantowany w 1. turze walki)",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 0,
        "ikona": "🗡",
    },
    "trucizna": {
        "nazwa": "Trucizna",
        "opis": "Wróg traci 10 HP na turę przez 3 tury",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 0,
        "ikona": "☠",
    },
    "dymna_bomba": {
        "nazwa": "Dymna bomba",
        "opis": "Natychmiastowa ucieczka (100% skuteczność)",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 0,
        "ikona": "💨",
    },
    "smiertelne_uderzenie": {
        "nazwa": "Śmiertelne uderzenie",
        "opis": "3× obrażenia jeśli HP wroga < 25%, inaczej normalny atak",
        "klasa": "Lotrzyk",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 0,
        "ikona": "☠",
    },

    # Łotrzyk → Zabójca
    "cien_smierci": {
        "nazwa": "Cień śmierci",
        "opis": "60% szans na 4× obrażenia (krytyczne trafienie)",
        "klasa": "Lotrzyk",
        "podklasa": "Zabojca",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "🌑",
    },
    "egzekucja": {
        "nazwa": "Egzekucja",
        "opis": "Natychmiast zabija wroga jeśli ma < 15% HP",
        "klasa": "Lotrzyk",
        "podklasa": "Zabojca",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "💀",
    },

    # Łotrzyk → Zwiadowca
    "unik": {
        "nazwa": "Unik",
        "opis": "75% szans na uniknięcie następnego ataku wroga",
        "klasa": "Lotrzyk",
        "podklasa": "Zwiadowca",
        "poziom": 6,
        "koszt_many": 0,
        "ikona": "💨",
    },
    "grad_strzal": {
        "nazwa": "Grad strzał",
        "opis": "Wykonuje 3 szybkie ataki, każdy za normalne obrażenia",
        "klasa": "Lotrzyk",
        "podklasa": "Zwiadowca",
        "poziom": 9,
        "koszt_many": 0,
        "ikona": "🏹",
    },
}

# Podklasy dostępne dla każdej klasy głównej
PODKLASY: dict[str, list[dict]] = {
    "Wojownik": [
        {
            "klucz": "Paladyn",
            "nazwa": "Paladyn",
            "opis": "Wojownik wspierany boską mocą — leczy i zadaje święte obrażenia.",
        },
        {
            "klucz": "Barbarzynca",
            "nazwa": "Barbarzyńca",
            "opis": "Dziki wojownik — maksymalne obrażenia kosztem obrony.",
        },
    ],
    "Mag": [
        {
            "klucz": "Nekromanta",
            "nazwa": "Nekromanta",
            "opis": "Mag ciemności — wysysa życie i rzuca klątwy osłabiające.",
        },
        {
            "klucz": "Arcymag",
            "nazwa": "Arcymag",
            "opis": "Mistrz magii — skupia się na niszczycielskich zaklęciach.",
        },
    ],
    "Lotrzyk": [
        {
            "klucz": "Zabojca",
            "nazwa": "Zabójca",
            "opis": "Precyzyjny morderca — krytyczne uderzenia i egzekucje.",
        },
        {
            "klucz": "Zwiadowca",
            "nazwa": "Zwiadowca",
            "opis": "Zwinny łowca — uniki i grad strzał.",
        },
    ],
}
