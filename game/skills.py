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

    # Mag → Mroczny mag
    "mroczna_strzala": {
        "nazwa": "Mroczna strzała",
        "opis": "45–70 obrażeń mrocznych (ignoruje obronę wroga)",
        "klasa": "Mag",
        "podklasa": "MrocznyMag",
        "poziom": 6,
        "koszt_many": 20,
        "ikona": "🌑",
    },
    "klatwa_mroku": {
        "nazwa": "Klątwa mroku",
        "opis": "Wróg zadaje 50% mniej obrażeń i traci 15 HP/turę przez 3 tury",
        "klasa": "Mag",
        "podklasa": "MrocznyMag",
        "poziom": 9,
        "koszt_many": 25,
        "ikona": "🌑",
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

    # ------------------------------------------------------------------ #
    #  DRUID – klasa główna                                               #
    # ------------------------------------------------------------------ #
    "splot_korzeni": {
        "nazwa": "Splot korzeni",
        "opis": "Unieruchamia wroga — pomija następną turę",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 10,
        "ikona": "🌿",
    },
    "uzdrowienie": {
        "nazwa": "Uzdrowienie",
        "opis": "Leczy 50 HP",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 15,
        "ikona": "💚",
    },
    "burza_natury": {
        "nazwa": "Burza natury",
        "opis": "40–60 obrażeń żywiołowych (ignoruje obronę wroga)",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 20,
        "ikona": "⛈",
    },
    "regeneracja": {
        "nazwa": "Regeneracja",
        "opis": "Gracz leczy się o 15 HP na turę przez 4 tury",
        "klasa": "Druid",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 20,
        "ikona": "🌱",
    },

    # Druid → Szaman
    "totem_zycia": {
        "nazwa": "Totem życia",
        "opis": "Leczy 30 HP na turę przez 3 tury",
        "klasa": "Druid",
        "podklasa": "Szaman",
        "poziom": 6,
        "koszt_many": 20,
        "ikona": "🔺",
    },
    "piorun_szamana": {
        "nazwa": "Piorun szamana",
        "opis": "70–100 obrażeń błyskawicznych + 50% szans na ogłuszenie",
        "klasa": "Druid",
        "podklasa": "Szaman",
        "poziom": 9,
        "koszt_many": 30,
        "ikona": "⚡",
    },

    # Druid → Strażnik Lasu
    "kolce_natury": {
        "nazwa": "Kolce natury",
        "opis": "Wróg traci 20 HP na turę przez 4 tury (trucizna roślinna)",
        "klasa": "Druid",
        "podklasa": "StraznikLasu",
        "poziom": 6,
        "koszt_many": 15,
        "ikona": "🌵",
    },
    "gniew_puszczy": {
        "nazwa": "Gniew puszczy",
        "opis": "50–80 obrażeń × liczba aktywnych efektów na wrogu (min. 1×)",
        "klasa": "Druid",
        "podklasa": "StraznikLasu",
        "poziom": 9,
        "koszt_many": 25,
        "ikona": "🌲",
    },

    # ------------------------------------------------------------------ #
    #  NEKROMANTA – klasa główna                                          #
    # ------------------------------------------------------------------ #
    "wysysanie_zycia": {
        "nazwa": "Wysysanie życia",
        "opis": "30–50 obrażeń mrocznych, gracz leczy się o tę samą ilość",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 1,
        "koszt_many": 15,
        "ikona": "🩸",
    },
    "klatwa_smierci": {
        "nazwa": "Klątwa śmierci",
        "opis": "Wróg zadaje 50% mniej obrażeń przez 3 tury",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 3,
        "koszt_many": 15,
        "ikona": "💀",
    },
    "rozpad": {
        "nazwa": "Rozpad",
        "opis": "Zmniejsza maksymalne HP wroga o 20% (trwałe na czas walki)",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 5,
        "koszt_many": 20,
        "ikona": "🦴",
    },
    "dotyk_smierci": {
        "nazwa": "Dotyk śmierci",
        "opis": "Wróg traci 25 HP na turę przez 3 tury (trucizna nekrotyczna)",
        "klasa": "Nekromanta",
        "podklasa": None,
        "poziom": 8,
        "koszt_many": 20,
        "ikona": "☠",
    },

    # Nekromanta → Lich
    "fala_smierci": {
        "nazwa": "Fala śmierci",
        "opis": "60–90 obrażeń mrocznych (ignoruje obronę), gracz leczy się o 30%",
        "klasa": "Nekromanta",
        "podklasa": "Lich",
        "poziom": 6,
        "koszt_many": 25,
        "ikona": "💀",
    },
    "wiecznie_zywi": {
        "nazwa": "Wiecznie żywi",
        "opis": "Aktywuje ochronę — jeśli gracz miałby umrzeć, leczy go o 40 HP (raz na walkę)",
        "klasa": "Nekromanta",
        "podklasa": "Lich",
        "poziom": 9,
        "koszt_many": 30,
        "ikona": "💀",
    },

    # Nekromanta → Kapłan Mroku
    "pakt_krwi": {
        "nazwa": "Pakt krwi",
        "opis": "Gracz traci 20 HP, ale następny atak zadaje 3× obrażenia",
        "klasa": "Nekromanta",
        "podklasa": "KaplanMroku",
        "poziom": 6,
        "koszt_many": 10,
        "ikona": "🗡",
    },
    "ofiarny_rytual": {
        "nazwa": "Ofiarny rytuał",
        "opis": "Gracz traci 30% HP, wróg traci tę samą wartość × 3",
        "klasa": "Nekromanta",
        "podklasa": "KaplanMroku",
        "poziom": 9,
        "koszt_many": 15,
        "ikona": "🩸",
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
            "klucz": "MrocznyMag",
            "nazwa": "Mroczny mag",
            "opis": "Mag ciemności — mroczne strzały i klątwy łączące obrażenia z osłabieniem.",
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
    "Druid": [
        {
            "klucz": "Szaman",
            "nazwa": "Szaman",
            "opis": "Uzdrowiciel duchów — totemy leczące i pioruny ogłuszające.",
        },
        {
            "klucz": "StraznikLasu",
            "nazwa": "Strażnik Lasu",
            "opis": "Władca przyrody — trwałe trucizny roślinne i gniew natury.",
        },
    ],
    "Nekromanta": [
        {
            "klucz": "Lich",
            "nazwa": "Lich",
            "opis": "Nieśmiertelny czarnoksiężnik — fale śmierci i ochrona przed zagładą.",
        },
        {
            "klucz": "KaplanMroku",
            "nazwa": "Kapłan Mroku",
            "opis": "Rytualistyczny morderca — poświęca własne HP dla potężnych ataków.",
        },
    ],
}
