"""Moduł zawierający klasę gracza."""

from game.skills import UMIEJETNOSCI, nastepne_umiejetnosci
from game.atrybuty import startowe_atrybuty, biegle_skille_klasy, linia_atrybutow
from game.mapa import SRODEK, liczba_odkrytych, liczba_pol

# Progi EXP potrzebne do awansu na kolejny poziom (łączny EXP)
EXP_PROGI = [0, 100, 250, 450, 700, 1000, 1400, 1900, 2500, 3200, 4000]


def _prog_exp(poziom: int) -> int:
    """Łączny EXP wymagany, aby opuścić dany poziom."""
    if poziom < len(EXP_PROGI):
        return EXP_PROGI[poziom]
    return EXP_PROGI[-1] + (poziom - len(EXP_PROGI) + 1) * 850

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
        self.mapa_x = SRODEK
        self.mapa_y = SRODEK
        self.aktualny_biom = "równiny"
        self.mapa_gen = 1  # numer regionu — rośnie po przekroczeniu krawędzi
        self.mapa_pola = None  # siatka regionu (generowana leniwie)
        self.punkty_atrybutow = 2  # start jak w BG3 — kilka punktów do rozdania
        self.osiagniecia: set[str] = set()  # odblokowane osiągnięcia
        self.tryb_trudnosci = "normalny"
        self.karma = 0
        self.blogoslawienstwo_wyprawy = False

        stat = _STATYSTYKI_KLAS[klasa]
        self.poziom = 1
        self.exp = 0
        self.zloto = 30
        self.max_hp = stat["max_hp"]
        self.hp = stat["max_hp"]
        self.atak = stat["atak"]
        self.obrona = stat["obrona"]
        self.mikstury = stat["mikstury"]
        self.mikstury_duze = 0
        self.mikstury_many = 1 if stat["max_mana"] > 0 else 0
        self.antidota = 0
        self.max_mana = stat["max_mana"]
        self.mana = stat["max_mana"]

        self._hp_na_poziom = stat["hp_na_poziom"]
        self._atak_na_poziom = stat["atak_na_poziom"]
        self._obrona_na_poziom = stat["obrona_na_poziom"]

        # Ekwipunek — założona broń i zbroja oraz plecak (klucze z game/items.py)
        self.wyposazenie: dict[str, str | None] = {"bron": None, "zbroja": None}
        self.plecak: list[str] = []

        # Questy
        self.aktywne_questy: set[str] = set()
        self.ukonczone_questy: set[str] = set()

        # Statystyki na potrzeby questów
        self.statystyki: dict[str, int] = {
            "zabite_potwory": 0,
            "wygrane_walki": 0,
            "zakupy": 0,
            "odwiedzone_swiatynie": 0,
            "zebrane_surowce": 0,
            "zbudowane_budynki": 0,
        }

        # Umiejętności odblokowane na starcie (poziom 1 dla klasy głównej)
        self.umiejetnosci: list[str] = [
            k for k, v in UMIEJETNOSCI.items()
            if v["klasa"] == self.klasa and v["podklasa"] is None and v["poziom"] == 1
        ]
        self.rangi_umiejetnosci: dict[str, int] = {k: 1 for k in self.umiejetnosci}
        self.punkty_umiejetnosci = 0
        self.surowce: dict[str, int] = {
            "drewno": 3, "kamien": 2, "ziola": 1, "skora": 0, "ruda": 0,
        }
        self.budynki: set[str] = set()
        self.rekruci: list[dict] = []
        self.zbieracze_w_pracy = False
        self.czas: int = 0
        self.czas_wyjscia: int = 0
        self.chaty: int = 0
        self.osadnicy: list[dict] = []
        self.watki_npc: dict[str, int] = {}
        self.atrybuty: dict[str, int] = startowe_atrybuty(klasa)
        self.biegle_skille: list[str] = biegle_skille_klasy(klasa)
        self.pochodzenie: str | None = None
        self.cechy: list[str] = []

    # ------------------------------------------------------------------ #
    #  Stan                                                                #
    # ------------------------------------------------------------------ #

    def zyje(self) -> bool:
        return self.hp > 0

    def exp_do_awansu(self) -> int:
        """Zwraca łączny próg EXP potrzebny do następnego poziomu."""
        return _prog_exp(self.poziom)

    def exp_w_poziomie(self) -> tuple[int, int]:
        """Postęp w bieżącym poziomie: (zdobyte w poziomie, potrzeba na awans)."""
        poprzedni = 0 if self.poziom <= 1 else _prog_exp(self.poziom - 1)
        potrzeba = self.exp_do_awansu() - poprzedni
        zdobyte = max(0, self.exp - poprzedni)
        return zdobyte, potrzeba

    # ------------------------------------------------------------------ #
    #  Akcje                                                               #
    # ------------------------------------------------------------------ #

    def uzyj_miksture(self) -> str:
        """Leczy gracza i zmniejsza liczbę mikstur. Zwraca komunikat."""
        if self.mikstury <= 0:
            return "Nie masz żadnych mikstur!"
        lecz = 40
        from game.pochodzenie import bonus_leczenia_mikstury
        lecz += bonus_leczenia_mikstury(self)
        self.mikstury -= 1
        poprzednie_hp = self.hp
        self.hp = min(self.hp + lecz, self.max_hp)
        faktyczne = self.hp - poprzednie_hp
        return f"Użyłeś mikstury leczenia! Przywróciłeś {faktyczne} HP. (Mikstury: {self.mikstury})"

    def uzyj_miksture_duza(self) -> str:
        """Leczy 80 HP. Zwraca komunikat."""
        if getattr(self, "mikstury_duze", 0) <= 0:
            return "Nie masz większych mikstur!"
        self.mikstury_duze -= 1
        poprzednie_hp = self.hp
        from game.pochodzenie import bonus_leczenia_mikstury
        lecz = 80 + bonus_leczenia_mikstury(self)
        self.hp = min(self.hp + lecz, self.max_hp)
        faktyczne = self.hp - poprzednie_hp
        return (
            f"Użyłeś większej mikstury! Przywróciłeś {faktyczne} HP. "
            f"(Większe mikstury: {self.mikstury_duze})"
        )

    def uzyj_miksture_many(self) -> str:
        """Przywraca 30 many. Zwraca komunikat."""
        if self.max_mana <= 0:
            return "Twoja klasa nie korzysta z many."
        if getattr(self, "mikstury_many", 0) <= 0:
            return "Nie masz mikstur many!"
        self.mikstury_many -= 1
        poprzednia = self.mana
        self.mana = min(self.mana + 30, self.max_mana)
        faktyczne = self.mana - poprzednia
        return (
            f"Użyłeś mikstury many! Przywróciłeś {faktyczne} many. "
            f"(Mikstury many: {self.mikstury_many})"
        )

    def uzyj_antidotum(self) -> str | None:
        """Zużywa antidotum. Zwraca None gdy brak, w przeciwnym razie komunikat."""
        if getattr(self, "antidota", 0) <= 0:
            return None
        self.antidota -= 1
        return f"Wypijasz antidotum. (Antidota: {self.antidota})"

    def zdobadz_exp(self, ilosc: int) -> list[str]:
        """Dodaje EXP i sprawdza awans. Zwraca listę komunikatów."""
        from game.pochodzenie import mnoznik_exp
        mno = mnoznik_exp(self)
        ilosc = max(1, int(ilosc * mno))
        extra = " (premia weterana)" if mno > 1.0 else ""
        komunikaty = [f"Zdobyłeś {ilosc} EXP!{extra}"]
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

        pkt_atr = 2 if self.poziom % 5 == 0 else 1
        pkt_skilli = 2 if self.poziom in (5, 10, 15) else 1
        self.punkty_atrybutow = getattr(self, "punkty_atrybutow", 0) + pkt_atr
        self.punkty_umiejetnosci = getattr(self, "punkty_umiejetnosci", 0) + pkt_skilli

        komunikaty = [
            f"*** AWANS NA POZIOM {self.poziom}! ***",
            f"  Max HP: {self.max_hp}  Atak: {self.atak}  Obrona: {self.obrona}",
        ]
        if self.max_mana > 0:
            komunikaty.append(f"  Max Mana: {self.max_mana}")
        komunikaty.append("  HP i mana zostały w pełni uzupełnione!")
        komunikaty.append(
            f"  +{pkt_atr} pkt. atrybutów, +{pkt_skilli} pkt. umiejętności — rozdaj w obozie."
        )

        komunikaty.extend(self._odblokuj_umiejetnosci(self.poziom))

        if self.poziom == 5 and self.podklasa is None:
            self.podklasa_dostepna = True
            komunikaty.append("  ⭐  Osiągnąłeś poziom 5! Możesz wybrać podklasę w obozie.")

        for info in nastepne_umiejetnosci(self, 1):
            komunikaty.append(
                f"  Następna umiejętność (poz. {info['poziom']}): "
                f"{info['ikona']} {info['nazwa']}"
            )

        return komunikaty

    def _odblokuj_umiejetnosci(self, poziom: int) -> list[str]:
        """Odblokowuje umiejętności przypisane do danego poziomu. Zwraca komunikaty."""
        komunikaty = []
        rangi = getattr(self, "rangi_umiejetnosci", None)
        if rangi is None:
            self.rangi_umiejetnosci = {k: 1 for k in self.umiejetnosci}
            rangi = self.rangi_umiejetnosci
        for klucz, info in UMIEJETNOSCI.items():
            if klucz in self.umiejetnosci:
                continue
            if info["klasa"] != self.klasa:
                continue
            if info["podklasa"] is not None and info["podklasa"] != self.podklasa:
                continue
            if info["poziom"] <= poziom:
                self.umiejetnosci.append(klucz)
                rangi[klucz] = 1
                komunikaty.append(
                    f"  *** NOWA UMIEJĘTNOŚĆ: {info['ikona']} {info['nazwa']}! ***"
                )
        return komunikaty

    def wybierz_podklase(self, podklasa: str) -> list[str]:
        """Ustawia podklasę gracza i natychmiast odblokowuje kwalifikujące się skille."""
        self.podklasa = podklasa
        self.podklasa_dostepna = False
        rangi = getattr(self, "rangi_umiejetnosci", None)
        if rangi is None:
            self.rangi_umiejetnosci = {k: 1 for k in self.umiejetnosci}
            rangi = self.rangi_umiejetnosci
        komunikaty = [f"  Wybrałeś podklasę: {podklasa}!"]
        for klucz, info in UMIEJETNOSCI.items():
            if klucz in self.umiejetnosci:
                continue
            if info["klasa"] != self.klasa or info["podklasa"] != podklasa:
                continue
            if info["poziom"] <= self.poziom:
                self.umiejetnosci.append(klucz)
                rangi[klucz] = 1
                komunikaty.append(
                    f"  *** NOWA UMIEJĘTNOŚĆ: {info['ikona']} {info['nazwa']}! ***"
                )
        self.punkty_umiejetnosci = getattr(self, "punkty_umiejetnosci", 0) + 1
        komunikaty.append("  +1 punkt umiejętności za wybór specjalizacji.")
        return komunikaty

    def rejestruj_walke(self, nazwa_potwora: str) -> None:
        """Rejestruje wygraną walkę i aktualizuje statystyki dla questów."""
        self.statystyki["wygrane_walki"] = self.statystyki.get("wygrane_walki", 0) + 1
        self.statystyki["zabite_potwory"] = self.statystyki.get("zabite_potwory", 0) + 1
        klucz = f"zabite_{nazwa_potwora.lower()}"
        self.statystyki[klucz] = self.statystyki.get(klucz, 0) + 1

    # Definicje osiągnięć: (klucz, nazwa, warunek_fn)
    _OSIAGNIECIA = [
        ("pierwsze_kroki", "🥇 Pierwsze kroki", lambda g: g.statystyki.get("zabite_potwory", 0) >= 1),
        ("rzeźnik", "🗡 Rzeźnik", lambda g: g.statystyki.get("zabite_potwory", 0) >= 50),
        ("wojownik_mroku", "⚔ Wojownik Mroku", lambda g: g.statystyki.get("zabite_potwory", 0) >= 100),
        ("odkrywca", "🗺 Odkrywca", lambda g: g.mapa_gen >= 10),
        ("podroznik", "🌍 Podróżnik", lambda g: g.mapa_gen >= 5),
        ("kartograf", "🗺 Kartograf", lambda g: liczba_odkrytych(g) >= liczba_pol()),
        ("bogacz", "💰 Bogacz", lambda g: g.zloto >= 500),
        ("kolekcjoner", "🧪 Kolekcjoner", lambda g: g.mikstury >= 10),
        ("legenda", "👑 Legenda", lambda g: g.poziom >= 10),
        ("kapitan", "🎖 Kapitan", lambda g: g.poziom >= 5),
        ("badacz_swiatyn", "🛕 Badacz Świątyń", lambda g: g.statystyki.get("odwiedzone_swiatynie", 0) >= 5),
        ("osadnik", "🏕 Osadnik", lambda g: len(getattr(g, "budynki", set()) or set()) >= 1),
        ("starosta", "🏘 Starosta", lambda g: len(getattr(g, "budynki", set()) or set()) >= 4),
        ("druzynowy", "🤝 Drużynowy", lambda g: len(getattr(g, "rekruci", []) or []) >= 1),
        ("mitolog", "🌌 Mitolog", lambda g: g.statystyki.get("odwiedzone_mityczne", 0) >= 1),
        ("pogromca_mitow", "🐉 Pogromca mitów", lambda g: g.statystyki.get("odwiedzone_mityczne", 0) >= 3),
        ("obywatel", "🏙 Obywatel", lambda g: g.statystyki.get("odwiedzone_miasta", 0) >= 1),
        ("gospodarz", "🛖 Gospodarz", lambda g: int(getattr(g, "chaty", 0) or 0) >= 2
         and len(getattr(g, "osadnicy", []) or []) >= 1),
        ("hurtownik", "🛒 Hurtownik", lambda g: g.statystyki.get("zloto_z_targu", 0) >= 80),
    ]

    def sprawdz_osiagniecia(self) -> list[str]:
        """Sprawdza i odblokowuje nowe osiągnięcia. Zwraca listę nowych."""
        nowe = []
        for klucz, nazwa, warunek in self._OSIAGNIECIA:
            if klucz not in self.osiagniecia and warunek(self):
                self.osiagniecia.add(klucz)
                nowe.append(f"  🏆 OSIĄGNIĘCIE: {nazwa}")
        return nowe

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
                f"\n  🔮  Mana: {self.mana}/{self.max_mana} {self.pasek_many()}"
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

        exp_teraz, exp_potrzeba = self.exp_w_poziomie()
        pkt_u = getattr(self, "punkty_umiejetnosci", 0)
        pkt_a = getattr(self, "punkty_atrybutow", 0)
        pkt_linia = ""
        if pkt_a or pkt_u:
            pkt_linia = (
                f"\n  Do rozdania: {pkt_a} atr.  {pkt_u} um."
            )

        from game.pochodzenie import nazwa_pochodzenia, nazwy_cech
        poch = nazwa_pochodzenia(self)
        cechy_str = nazwy_cech(self)

        return (
            f"\n{linia}\n"
            f"  🧙  Bohater: {self.imie} [{klasa_str}]  (Poz. {self.poziom})\n"
            f"  📜  Pochodzenie: {poch}\n"
            f"  ✨  Cechy: {cechy_str}\n"
            f"  ❤️  HP: {self.hp}/{self.max_hp} {self.pasek_hp()}{mana_linia}\n"
            f"  ⚔  Atak: {self.atak}   🛡  Obrona: {self.obrona}\n"
            f"{linia_atrybutow(self)}\n"
            f"  ⭐  EXP: {exp_teraz}/{exp_potrzeba}"
            f"   (łącznie {self.exp})   💰 Złoto: {self.zloto} szt.{pkt_linia}\n"
            f"  🧪 Mikstury: {self.mikstury}   💚 Większe: {getattr(self, 'mikstury_duze', 0)}"
            f"   🔮 Many: {getattr(self, 'mikstury_many', 0)}"
            f"   🧴 Antidota: {getattr(self, 'antidota', 0)}\n"
            f"  ⚔  Broń: {bron_str}\n"
            f"  🛡  Zbroja: {zbroja_str}\n"
            f"{linia}"
        )
