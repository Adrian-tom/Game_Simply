"""System questów — definicje, śledzenie postępu i nagrody."""

from game.utils import wyswietl_linie, nacisnij_enter


# Definicje dostępnych questów
QUESTY: dict[str, dict] = {
    "goblin_slayer": {
        "nazwa": "Pogromca Goblinów",
        "opis": "Pokonaj 5 Goblinów terroryzujących okoliczne wioski.",
        "cel_ilosc": 5,
        "stat_klucz": "zabite_goblin",
        "nagroda_zloto": 60,
        "nagroda_exp": 100,
        "nagroda_opis": "60 złota + 100 EXP",
    },
    "weteran": {
        "nazwa": "Weteran Bojowy",
        "opis": "Wygraj 10 walk — udowodnij swoją wartość w boju.",
        "cel_ilosc": 10,
        "stat_klucz": "wygrane_walki",
        "nagroda_zloto": 0,
        "nagroda_exp": 150,
        "nagroda_atak": 2,
        "nagroda_opis": "150 EXP + permanentny +2 do Ataku",
    },
    "handlarz": {
        "nazwa": "Stały Klient",
        "opis": "Dokonaj 3 zakupów w sklepie lub kuźni.",
        "cel_ilosc": 3,
        "stat_klucz": "zakupy",
        "nagroda_zloto": 40,
        "nagroda_exp": 50,
        "nagroda_mikstura": 2,
        "nagroda_opis": "40 złota + 50 EXP + 2 mikstury",
    },
    "pielgrzym": {
        "nazwa": "Pielgrzym",
        "opis": "Odwiedź świątynię i przyjmij błogosławieństwo kapłanów.",
        "cel_ilosc": 1,
        "stat_klucz": "odwiedzone_swiatynie",
        "nagroda_zloto": 30,
        "nagroda_exp": 60,
        "nagroda_obrona": 1,
        "nagroda_opis": "30 złota + 60 EXP + permanentny +1 do Obrony",
    },
    "lowca_potworow": {
        "nazwa": "Łowca Potworów",
        "opis": "Zabij łącznie 15 potworów.",
        "cel_ilosc": 15,
        "stat_klucz": "zabite_potwory",
        "nagroda_zloto": 80,
        "nagroda_exp": 200,
        "nagroda_opis": "80 złota + 200 EXP",
    },
}


# ------------------------------------------------------------------ #
#  Logika questów                                                       #
# ------------------------------------------------------------------ #

def _postep(gracz, quest: dict) -> int:
    """Zwraca aktualny postęp w zadaniu questa."""
    return gracz.statystyki.get(quest["stat_klucz"], 0)


def _ukoncz_questa(gracz, klucz: str, quest: dict) -> list[str]:
    """Przyznaje nagrody za questa. Zwraca listę komunikatów."""
    gracz.ukonczone_questy.add(klucz)
    gracz.aktywne_questy.discard(klucz)
    komunikaty = [f"  🏆  QUEST UKOŃCZONY: {quest['nazwa']}!"]

    if quest.get("nagroda_zloto", 0):
        gracz.zloto += quest["nagroda_zloto"]
        komunikaty.append(f"  💰  Złoto: +{quest['nagroda_zloto']}")
    if quest.get("nagroda_exp", 0):
        msgs = gracz.zdobadz_exp(quest["nagroda_exp"])
        for m in msgs:
            komunikaty.append(f"  {m}")
    if quest.get("nagroda_atak", 0):
        gracz.atak += quest["nagroda_atak"]
        komunikaty.append(f"  ⚔  Atak permanentnie: +{quest['nagroda_atak']}")
    if quest.get("nagroda_obrona", 0):
        gracz.obrona += quest["nagroda_obrona"]
        komunikaty.append(f"  🛡  Obrona permanentnie: +{quest['nagroda_obrona']}")
    if quest.get("nagroda_mikstura", 0):
        gracz.mikstury += quest["nagroda_mikstura"]
        komunikaty.append(f"  🧪  Mikstury: +{quest['nagroda_mikstura']}")

    return komunikaty


def sprawdz_questy(gracz) -> list[str]:
    """Sprawdza wszystkie aktywne questy i przyznaje nagrody za ukończone.

    Zwraca listę komunikatów (pusta lista jeśli nic się nie zmieniło).
    """
    komunikaty = []
    for klucz in list(gracz.aktywne_questy):
        if klucz in gracz.ukonczone_questy:
            continue
        quest = QUESTY[klucz]
        postep = _postep(gracz, quest)
        if postep >= quest["cel_ilosc"]:
            komunikaty.extend(_ukoncz_questa(gracz, klucz, quest))
    return komunikaty


# ------------------------------------------------------------------ #
#  Tablica questów (UI)                                                #
# ------------------------------------------------------------------ #

def pokaz_tablice_questow(gracz) -> None:
    """Wyświetla interaktywną tablicę questów w obozie."""
    while True:
        wyswietl_linie("═")
        print("  📜  TABLICA QUESTÓW\n")

        aktywne = [k for k in gracz.aktywne_questy if k not in gracz.ukonczone_questy]
        dostepne = [
            k for k in QUESTY
            if k not in gracz.ukonczone_questy and k not in gracz.aktywne_questy
        ]

        numer = 1
        numeracja: list[str] = []  # mapowanie nr → klucz

        if aktywne:
            print("  ─── AKTYWNE ───")
            for klucz in aktywne:
                quest = QUESTY[klucz]
                postep = _postep(gracz, quest)
                cel = quest["cel_ilosc"]
                print(f"  [{numer}] ⏳ {quest['nazwa']}  ({postep}/{cel})")
                print(f"       {quest['opis']}")
                print(f"       Nagroda: {quest['nagroda_opis']}")
                print()
                numeracja.append(klucz)
                numer += 1

        if dostepne:
            print("  ─── DOSTĘPNE DO PRZYJĘCIA ───")
            for klucz in dostepne:
                quest = QUESTY[klucz]
                print(f"  [{numer}] {quest['nazwa']}")
                print(f"       {quest['opis']}")
                print(f"       Nagroda: {quest['nagroda_opis']}")
                print()
                numeracja.append(klucz)
                numer += 1

        if gracz.ukonczone_questy:
            ukonczone_nazwy = [QUESTY[k]["nazwa"] for k in gracz.ukonczone_questy if k in QUESTY]
            print(f"  ✅  Ukończone ({len(gracz.ukonczone_questy)}/{len(QUESTY)}): "
                  f"{', '.join(ukonczone_nazwy)}")
            print()

        if not aktywne and not dostepne:
            print("  Wszystkie questy zostały ukończone lub przyjęte. Gratulacje!")
            print()

        print("  Wpisz numer questa, aby go przyjąć (lub [0] aby wyjść):\n")
        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            return

        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(numeracja):
                klucz = numeracja[idx]
                if klucz in aktywne:
                    print(f"  Ten quest jest już aktywny!")
                elif klucz in gracz.ukonczone_questy:
                    print(f"  Ten quest został już ukończony!")
                else:
                    gracz.aktywne_questy.add(klucz)
                    print(f"  ✅  Przyjąłeś questa: {QUESTY[klucz]['nazwa']}!")
                nacisnij_enter()
                continue
        except ValueError:
            pass

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()
