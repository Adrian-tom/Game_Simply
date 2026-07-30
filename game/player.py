"""Moduł zawierający klasę gracza."""

# Progi EXP potrzebne do awansu na kolejny poziom
EXP_PROGI = [0, 100, 250, 450, 700, 1000, 1400, 1900, 2500, 3200, 4000]


class Gracz:
    """Klasa reprezentująca postać gracza."""

    def __init__(self, imie: str) -> None:
        self.imie = imie
        self.poziom = 1
        self.exp = 0
        self.zloto = 30
        self.max_hp = 100
        self.hp = 100
        self.atak = 15
        self.obrona = 5
        self.mikstury = 3

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
        self.max_hp += 20
        self.hp = self.max_hp
        self.atak += 5
        self.obrona += 2
        return [
            f"*** AWANS NA POZIOM {self.poziom}! ***",
            f"  Max HP: {self.max_hp}  Atak: {self.atak}  Obrona: {self.obrona}",
            "  HP zostało w pełni uzupełnione!",
        ]

    # ------------------------------------------------------------------ #
    #  Wyświetlanie                                                        #
    # ------------------------------------------------------------------ #

    def pasek_hp(self, szerokosc: int = 20) -> str:
        """Zwraca tekstowy pasek HP."""
        wypelniony = int((self.hp / self.max_hp) * szerokosc)
        return "[" + "█" * wypelniony + "░" * (szerokosc - wypelniony) + "]"

    def __str__(self) -> str:
        linia = "─" * 40
        return (
            f"\n{linia}\n"
            f"  Bohater: {self.imie}  (Poz. {self.poziom})\n"
            f"  HP: {self.hp}/{self.max_hp} {self.pasek_hp()}\n"
            f"  Atak: {self.atak}   Obrona: {self.obrona}\n"
            f"  EXP: {self.exp}/{self.exp_do_awansu()}   Złoto: {self.zloto} szt.\n"
            f"  Mikstury: {self.mikstury}\n"
            f"{linia}"
        )
