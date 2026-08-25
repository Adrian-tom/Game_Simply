"""Moduł zawierający klasę gracza."""

from game.skills import UMIEJETNOSCI

# Progi EXP potrzebne do awansu na kolejny poziom
EXP_PROGI = [0, 100, 250, 450, 700, 1000, 1400, 1900, 2500, 3200, 4000]

# Startowe statystyki i wzrosty per klasa
_STATYSTYKI_KLAS: dict[str, dict] = {
    "Wojownik": {
        "max_hp": 120, "atak": 18, "obrona": 8, "mikstury": 2, "max_mana": 0,
        "hp_na_poziom": 25, "atak_na_poziom": 6, "obrona_na_poziom": 3,
    },
    "Mag": {
        "max_hp": 70, "atak": 10, "obrona": 2, "mikstury": 3, "max_mana": 50,
        "hp_na_poziom": 10, "atak_na_poziom": 3, "obrona_na_poziom": 1,
    },
    "Lotrzyk": {
        "max_hp": 90, "atak": 14, "obrona": 5, "mikstury": 2, "max_mana": 0,
        "hp_na_poziom": 15, "atak_na_poziom": 5, "obrona_na_poziom": 2,
    },
    "Druid": {
        "max_hp": 85, "atak": 11, "obrona": 4, "mikstury": 3, "max_mana": 55,
        "hp_na_poziom": 12, "atak_na_poziom": 3, "obrona_na_poziom": 2,
    },
    "Nekromanta": {
        "max_hp": 75, "atak": 12, "obrona": 3, "mikstury": 2, "max_mana": 60,
        "hp_na_poziom": 11, "atak_na_poziom": 4, "obrona_na_poziom": 1,
    },
}


class Gracz:
    """Klasa reprezentująca postać gracza."""

    def __init__(self, imie: str, klasa: str = "Wojownik") -> None:
        self.imie = imie
        self.klasa = klasa
        self.podklasa: str | None = None
        self.podklasa_dostepna: bool = False
        self.mapa_x = 2
        self.mapa_y = 2
        self.aktualny_biom = "Obóz"
        self.mapa_gen = 1  # numer "mapy" – rośnie za każdym razem gdy gracz przekroczy krawędź

        stat = _STATYSTYKI_KLAS[klasa]
        self.poziom = 1
        self.exp = 0
        self.zloto = 30
        self.max_hp = stat["max_hp"]
        self.hp = stat["max_hp"]
        self.atak = stat["atak"]
        self.obrona = stat["obrona"]
        self.mikstury = stat["mikstury"]
        self.max_mana = stat["max_mana"]
        self.mana = stat["max_mana"]

        self._hp_na_poziom = stat["hp_na_poziom"]
        self._atak_na_poziom = stat["atak_na_poziom"]
        self._obrona_na_poziom = stat["obrona_na_poziom"]

        # Ekwipunek — założona broń i zbroja (klucze z game/items.py)
        self.wyposazenie: dict[str, str | None] = {"bron": None, "zbroja": None}

        # Questy
        self.aktywne_questy: set[str] = set()
        self.ukonczone_questy: set[str] = set()

        # Statystyki na potrzeby questów
        self.statystyki: dict[str, int] = {
            "zabite_potwory": 0,
            "wygrane_walki": 0,
            "zakupy": 0,
            "odwiedzone_swiatynie": 0,
        }

        # Umiejętności odblokowane na starcie (poziom 1 dla klasy głównej)
        self.umiejetnosci: list[str] = [
            k for k, v in UMIEJETNOSCI.items()
            if v["klasa"] == self.klasa and v["podklasa"] is None and v["poziom"] == 1
        ]

    # ------------------------------------------------------------------ #
    #  Stan                                                                #
    # ------------------------------------------------------------------ #

    def zyje(self) -> bool:
        return self.hp > 0

    def exp_do_awansu(self) -> int:
        """Zwraca próg EXP potrzebny do następnego poziomu."""
        if self.poziom < len(EXP_PROGI):
            return EXP_PROGI[self.poziom]
        return EXP_PROGI[-1] + (self.poziom - len(EXP_PROGI) + 1) * 1000

    # ------------------------------------------------------------------ #
    #  Akcje                                                               #
    # ------------------------------------------------------------------ #

    def uzyj_miksture(self) -> str:
        """Leczy gracza i zmniejsza liczbę mikstur. Zwraca komunikat."""
        if self.mikstury <= 0:
            return "Nie masz żadnych mikstur!"
        lecz = 40
        self.mikstury -= 1
        poprzednie_hp = self.hp
        self.hp = min(self.hp + lecz, self.max_hp)
        faktyczne = self.hp - poprzednie_hp
        return f"Użyłeś mikstury leczenia! Przywróciłeś {faktyczne} HP. (Mikstury: {self.mikstury})"

    def zdobadz_exp(self, ilosc: int) -> list[str]:
        """Dodaje EXP i sprawdza awans. Zwraca listę komunikatów."""
        komunikaty = [f"Zdobyłeś {ilosc} EXP!"]
        self.exp += ilosc
        while self.exp >= self.exp_do_awansu():
            komunikaty += self._awansuj()
        return komunikaty

    def _awansuj(self) -> list[str]:
        """Awansuje gracza o jeden poziom."""
        self.poziom += 1
        self.max_hp += self._hp_na_poziom
        self.hp = self.max_hp
        self.atak += self._atak_na_poziom
        self.obrona += self._obrona_na_poziom
        if self.max_mana > 0:
            self.max_mana += 10
            self.mana = self.max_mana

        komunikaty = [
            f"*** AWANS NA POZIOM {self.poziom}! ***",
            f"  Max HP: {self.max_hp}  Atak: {self.atak}  Obrona: {self.obrona}",
        ]
        if self.max_mana > 0:
            komunikaty.append(f"  Max Mana: {self.max_mana}")
        komunikaty.append("  HP zostało w pełni uzupełnione!")

        komunikaty.extend(self._odblokuj_umiejetnosci(self.poziom))

        if self.poziom == 5 and self.podklasa is None:
            self.podklasa_dostepna = True
            komunikaty.append("  ⭐  Osiągnąłeś poziom 5! Możesz wybrać podklasę w obozie.")

        return komunikaty

    def _odblokuj_umiejetnosci(self, poziom: int) -> list[str]:
        """Odblokowuje umiejętności przypisane do danego poziomu. Zwraca komunikaty."""
        komunikaty = []
        for klucz, info in UMIEJETNOSCI.items():
            if klucz in self.umiejetnosci:
                continue
            if info["klasa"] != self.klasa:
                continue
            # Pomiń skill podklasy innej niż wybrana (lub gdy brak podklasy)
            if info["podklasa"] is not None and info["podklasa"] != self.podklasa:
                continue
            if info["poziom"] == poziom:
                self.umiejetnosci.append(klucz)
                komunikaty.append(
                    f"  *** NOWA UMIEJĘTNOŚĆ: {info['ikona']} {info['nazwa']}! ***"
                )
        return komunikaty

    def wybierz_podklase(self, podklasa: str) -> list[str]:
        """Ustawia podklasę gracza i natychmiast odblokowuje kwalifikujące się skille."""
        self.podklasa = podklasa
        self.podklasa_dostepna = False
        komunikaty = [f"  Wybrałeś podklasę: {podklasa}!"]
        for klucz, info in UMIEJETNOSCI.items():
            if klucz in self.umiejetnosci:
                continue
            if info["klasa"] != self.klasa or info["podklasa"] != podklasa:
                continue
            if info["poziom"] <= self.poziom:
                self.umiejetnosci.append(klucz)
                komunikaty.append(
                    f"  *** NOWA UMIEJĘTNOŚĆ: {info['ikona']} {info['nazwa']}! ***"
                )
        return komunikaty

    def rejestruj_walke(self, nazwa_potwora: str) -> None:
        """Rejestruje wygraną walkę i aktualizuje statystyki dla questów."""
        self.statystyki["wygrane_walki"] = self.statystyki.get("wygrane_walki", 0) + 1
        self.statystyki["zabite_potwory"] = self.statystyki.get("zabite_potwory", 0) + 1
        klucz = f"zabite_{nazwa_potwora.lower()}"
        self.statystyki[klucz] = self.statystyki.get(klucz, 0) + 1

    # ------------------------------------------------------------------ #
    #  Wyświetlanie                                                        #
    # ------------------------------------------------------------------ #

    def pasek_hp(self, szerokosc: int = 20) -> str:
        """Zwraca tekstowy pasek HP."""
        wypelniony = int((self.hp / self.max_hp) * szerokosc)
        return "[" + "█" * wypelniony + "░" * (szerokosc - wypelniony) + "]"

    def pasek_many(self, szerokosc: int = 20) -> str:
        """Zwraca tekstowy pasek many (pusty string gdy brak many)."""
        if self.max_mana == 0:
            return ""
        wypelniony = int((self.mana / self.max_mana) * szerokosc)
        return "[" + "▓" * wypelniony + "░" * (szerokosc - wypelniony) + "]"

    def __str__(self) -> str:
        linia = "─" * 40
        klasa_str = self.klasa
        if self.podklasa:
            klasa_str += f" / {self.podklasa}"
        elif self.podklasa_dostepna:
            klasa_str += " ⭐ (wybierz podklasę!)"
        mana_linia = ""
        if self.max_mana > 0:
            mana_linia = (
                f"\n  Mana: {self.mana}/{self.max_mana} {self.pasek_many()}"
            )

        # Linie ekwipunku
        from game.items import EKWIPUNEK  # import lokalny, aby uniknąć cyklu
        bron_klucz = self.wyposazenie.get("bron")
        zbroja_klucz = self.wyposazenie.get("zbroja")
        bron_str = "— brak —"
        zbroja_str = "— brak —"
        if bron_klucz and bron_klucz in EKWIPUNEK:
            it = EKWIPUNEK[bron_klucz]
            bron_str = f"{it['ikona']} {it['nazwa']} (+{it['bonus_atak']} Atak)"
        if zbroja_klucz and zbroja_klucz in EKWIPUNEK:
            it = EKWIPUNEK[zbroja_klucz]
            zbroja_str = f"{it['ikona']} {it['nazwa']} (+{it['bonus_obrona']} Obrona)"

        return (
            f"\n{linia}\n"
            f"  Bohater: {self.imie} [{klasa_str}]  (Poz. {self.poziom})\n"
            f"  HP: {self.hp}/{self.max_hp} {self.pasek_hp()}{mana_linia}\n"
            f"  Atak: {self.atak}   Obrona: {self.obrona}\n"
            f"  EXP: {self.exp}/{self.exp_do_awansu()}   Złoto: {self.zloto} szt.\n"
            f"  Mikstury: {self.mikstury}\n"
            f"  Broń: {bron_str}\n"
            f"  Zbroja: {zbroja_str}\n"
            f"{linia}"
        )
