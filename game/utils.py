"""Funkcje pomocnicze — wyświetlanie i obsługa wejścia."""

import os


def wyczysc() -> None:
    """Czyści ekran terminala."""
    os.system("cls" if os.name == "nt" else "clear")


def wyswietl_linie(znak: str = "─", szerokosc: int = 44) -> None:
    """Wyświetla poziomą linię dekoracyjną."""
    print("  " + znak * szerokosc)


def nacisnij_enter(komunikat: str = "  [Naciśnij Enter, aby kontynuować...]") -> None:
    """Czeka na naciśnięcie Enter przez gracza."""
    input(komunikat)


def baner_tytulowy() -> None:
    """Wyświetla baner tytułowy gry."""
    wyczysc()
    wyswietl_linie("═")
    print("  ██████╗ ██████╗  ██████╗      ██████╗ ██████╗  ██████╗ ")
    print("  ██╔══██╗██╔══██╗██╔═══██╗    ██╔══██╗██╔══██╗██╔════╝ ")
    print("  ██████╔╝██████╔╝██║   ██║    ██████╔╝██████╔╝██║  ███╗")
    print("  ██╔═══╝ ██╔══██╗██║   ██║    ██╔══██╗██╔═══╝ ██║   ██║")
    print("  ██║     ██║  ██║╚██████╔╝    ██║  ██║██║     ╚██████╔╝")
    print("  ╚═╝     ╚═╝  ╚═╝ ╚═════╝     ╚═╝  ╚═╝╚═╝      ╚═════╝ ")
    wyswietl_linie("═")
    print("         ⚔  Tekstowa gra RPG fantasy po polsku  🛡")
    wyswietl_linie("═")
    print()
