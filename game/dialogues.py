"""Dialogi NPC — gotowe kwestie dla różnych postaci."""

import random

from game.utils import wyswietl_linie, nacisnij_enter


_DIALOGI: dict[str, dict] = {
    "karczmarz": {
        "imie": "Karczmarz Boldan",
        "ikona": "🍺",
        "powitania": [
            "Hej, podróżniku! Dobrze, że zawitałeś. Czym mogę służyć?",
            "Siadaj, gościu! Mam świeże piwo i gorącą potrawkę!",
            "Hola, wędrowcze! Dawno tu nie było takiego wojownika jak ty.",
            "Ach, nowy gość! Wejdź, ogrzej kości i posłuchaj, co mam do powiedzenia.",
        ],
        "tematy": [
            ("O okolicy", [
                "Na wschód od nas ponoć grasują goblińscy zwiadowcy. Bądź ostrożny!",
                "Mówią, że w starych ruinach za wzgórzami ktoś znowu rozpalił ognie...",
                "Poprzedni gość wspominał o skarbach ukrytych w bagnach. Ale wróćmy do tematu piwa!",
                "Drogi w lesie są ostatnio niebezpieczne. Wielu podróżnych nie wróciło.",
            ]),
            ("O potworach", [
                "Gobliny? Pfa, to małe dranie. Ale trolle pod mostem — to co innego!",
                "Ostatnio widziałem coś dużego między drzewami. Nie wiem co to było, ale uciekłem.",
                "Szkielety wychodzą z ruin o północy. Radzę omijać je szerokim łukiem.",
                "Słyszałem, że w bagnach żyje coś wielkiego i śmierdzącego. Troll bagiennych, pono.",
            ]),
            ("O handlu", [
                "Kowal Grimbold ma nowe towary. Mówi, że dostał stal od krasnoludzkich górników.",
                "Wędrowni kupcy bywają tu raz na tydzień. Mają niezłe mikstury.",
                "Za dodatkową monetę zdradzę ci, gdzie kupiec trzyma swój klucz do skrzynki!",
                "Niedaleko stąd jest kuźnia. Stary Grimbold robi najlepsze miecze w całej okolicy.",
            ]),
        ],
    },
    "kupiec": {
        "imie": "Kupiec Aldric",
        "ikona": "💰",
        "powitania": [
            "Witaj, witaj! Mam wszystko, czego potrzebujesz — po odpowiedniej cenie!",
            "Och, podróżnik! Interesy robię z każdym. Co cię interesuje?",
            "Dobry dzień, bohaterze. Moje towary są najlepsze w tej części królestwa!",
            "Ach, klient! Doskonałe wyczucie czasu — właśnie dostałem nową dostawę.",
        ],
        "tematy": [
            ("O towarach", [
                "Mam dzisiaj świeże mikstury i trochę sprzętu. Obejrzysz?",
                "Mój dostawca przywiózł ostatnio niesamowitą kolczugę. Piękna robota!",
                "Uważaj na podróbki. Moje towary mają gwarancję autentyczności!",
                "Sztylet — lekki, zwinny i przystępny cenowo. Polecam dla każdego.",
            ]),
            ("O przygodach", [
                "Widziałem niejednego bohatera. Ci najlepsi zawsze dobrze się ekwipowali przed walką.",
                "Jeden mój klient kupił sztylet i wrócił z dwoma workami złota. Niezła inwestycja!",
                "Sprzedaję, bo lubię pomagać bohaterom. No i złoto też lubię, czego tu ukrywać.",
                "Dobry ekwipunek to różnica między życiem a śmiercią. Naprawdę.",
            ]),
            ("O questach", [
                "Słyszałem o tablicy questów w obozie. Ponoć dobrze płacą za gobliny.",
                "Ktoś szuka śmiałka do oczyszczenia starych ruin. Duże pieniądze!",
                "Questy to ryzyko, ale też zysk. Zawsze miej miksturę przy sobie.",
                "Znam pewnego starego rycerza, który płaci za informacje o potworach. Ciekawe?",
            ]),
        ],
    },
    "kowal": {
        "imie": "Kowal Grimbold",
        "ikona": "⚒",
        "powitania": [
            "Stuk, stuk! Nowy klient? Witam, witam. Co chcesz wykuć?",
            "Ah, podróżnik! Moje miecze nie mają sobie równych. Przekonaj się!",
            "Hej! Wejdź, tu nie gryziemy — tylko hartujemy stal!",
            "Widzę, że szukasz dobrej broni. Trafiłeś w dobre miejsce, przyjacielu.",
        ],
        "tematy": [
            ("O broni", [
                "Miecz to nie tylko żelazo. To precyzja, balans i pasja. Moje są doskonałe.",
                "Topór wojenny — prosta, brutalna siła. Idealny dla tych, co nie lubią subtelności.",
                "Łuk elficki? Skąd go mam? Elfy go zostawiły, ja naprawiłem. Teraz jest mój.",
                "Sztylet to broń tchórzy? Nie! To broń inteligentnych. Szybki cios i po wszystkim.",
            ]),
            ("O zbroi", [
                "Płytowa zbroja to inwestycja na całe życie. Twoje — jeśli przeżyjesz.",
                "Kolczuga jest lżejsza, ale solidna. Połowa moich klientów ją nosi.",
                "Skórzana zbroja dla zwinnych — dobry wybór. Lekka i funkcjonalna.",
                "Szata maga? Trudna w kuciu, ale efekty są niesamowite. Magia spleciona ze stalą.",
            ]),
            ("O rzemiośle", [
                "Trzydzieści lat przy kowadle — tyle zajmuje nauka dobrego kucia.",
                "Mój mistrz mawiał: lepsza stal, lepszy wojownik. Brałem to dosłownie.",
                "Wiele mieczy wyszło z tej kuźni. Kilka wróciło — w trochę gorszym stanie.",
                "Stal krasnoludzka to najlepsza na świecie. I droga. Bardzo droga.",
            ]),
        ],
    },
    "kaplan": {
        "imie": "Kapłan Eremiel",
        "ikona": "🙏",
        "powitania": [
            "Oby bogowie strzegli twojej ścieżki, podróżniku. Czym mogę ci służyć?",
            "Pokój niech będzie z tobą. Wejdź i pomodl się za pomyślność wyprawy.",
            "Przybywasz w odpowiednim czasie. Coś mówi mi, że bogowie cię tu wołali.",
            "Witaj, wędrowcze. Każdy, kto przekracza te progi, jest tu mile widziany.",
        ],
        "tematy": [
            ("O błogosławieństwach", [
                "Mogę odmówić modlitwę ochrony. Twoja obrona wzrośnie, lecz tylko raz na wyprawę.",
                "Bogowie dają siłę tym, którzy w nich wierzą. I tym, którzy dużo walczą.",
                "Błogosławieństwo nie zastąpi dobrej zbroi. Ale w połączeniu — niemożliwe staje się możliwe.",
                "Przyjmij znak ochrony. Może uratować ci życie, gdy będziesz tego najbardziej potrzebował.",
            ]),
            ("O złu", [
                "Mroczna magia pełza z północy. Czuję to w kościach. Uważaj na nekromantów.",
                "Gobliny to tylko pionki w grze sił ciemności. Prawdziwy wróg kryje się głębiej.",
                "Widziałem już armie nieumarłych. Najlepiej nie budzić ich do życia.",
                "Coś potężnego śpi w starych ruinach. Modlę się, żeby nie zostało przebudzone.",
            ]),
            ("O świątyni", [
                "Ta świątynia stoi tu od wieków. Przetrwała wojny i kataklizmy. Stoi nadal.",
                "Ofiary złożone na ołtarzu wracają do potrzebujących. Taki jest porządek świata.",
                "Każdy wojownik powinien raz odwiedzić świątynię. Dla spokoju ducha.",
                "Modlitwa nie zaszkodzi. Nawet jeśli nie wierzysz, bogowie słyszą każde słowo.",
            ]),
        ],
    },
    "stary_rycerz": {
        "imie": "Stary Rycerz Alderon",
        "ikona": "🗡",
        "powitania": [
            "Hm. Młody. Dobre oczy masz, widzę. Przysiadaj, powiem ci coś ważnego.",
            "Za moich czasów wojownicy chodzili boso i walczyli gołymi rękami. Teraz co? Sklepy! Bah.",
            "Pamiętam cię — nie, przepraszam, to było sto lat temu. Witaj, nieznajomy.",
            "Usiądź. Nie lubię gadać z ludźmi, co stoją. To niekulturalne.",
        ],
        "tematy": [
            ("O walce", [
                "Siła to nie wszystko. Widziałem mnóstwo silnych facetów w grobach. Liczy się głowa.",
                "Kiedy wróg atakuje, nie stój jak słup. Poruszaj się, myśl, nie daj się złapać.",
                "Najlepszą taktyką jest nie dać się trafić. Potem można pomyśleć o ataku.",
                "Kto ostatni stoi — ten wygrywa. Proste jak mój miecz.",
            ]),
            ("O doświadczeniu", [
                "Pięćdziesiąt lat walczyłem. Rany, głupiec ze mnie był. Ale i doświadczony.",
                "Każda blizna opowiada historię. Moje ciało to biblioteka klęsk i zwycięstw.",
                "Mądrość przychodzi z wiekiem — lub z bolesną nauczką. Lepiej wybrać wiek.",
                "Nie ma lepszego nauczyciela niż przeżyta walka. I lekarza po niej.",
            ]),
            ("O świecie", [
                "Królowie przychodzą i odchodzą. Smoki pozostają. Zawsze były i będą.",
                "Za wzgórzami na wschodzie jest stare miasto. Tam, ponoć, czeka coś potężnego.",
                "Dziś bohaterowie chodzą po sklepach. Za moich czasów chodziliśmy po lodowatych górach!",
                "Gobliny to irytacja. Orkowie — problem. Smok — to jest problem.",
            ]),
        ],
    },
    "tajemniczy_wedrowiec": {
        "imie": "Tajemnicza Postać",
        "ikona": "🌑",
        "powitania": [
            "Psst. Ty. Tak, ty. Podejdź bliżej... nie, nie tak blisko. Zatrzymaj się.",
            "Widziałem cię już wcześniej. Nie tutaj — gdzie indziej. W wizji. Zaciekawiające.",
            "Nie wszystko jest takim, jakim się wydaje. Ani ty, ani ja, ani ta droga.",
            "Szukasz czegoś. Czuję to. Może ja mam to, czego szukasz. A może nie.",
        ],
        "tematy": [
            ("O przeznaczeniu", [
                "Każdy krok, który stawiasz, wiedzie ku czemuś. Pytanie — ku czemu?",
                "Byłem tam, gdzie ty jeszcze nie dotarłeś. I powiem ci: warto tam dojść.",
                "Przeznaczenie to nie ścieżka — to wybór. Pamiętaj o tym, gdy przyjdzie czas.",
                "Twoja przyszłość jest jeszcze niezapisana. Dbaj o to.",
            ]),
            ("O tajemnicach", [
                "Słyszałeś o Kamiennym Sercu? Nie? Dobrze. Lepiej nie słyszeć.",
                "W ruinach za wschodnim lasem coś śpi. Niech dalej śpi. Na razie.",
                "Trzy prawa i lewo przy rozwidleniu. Zapamiętaj, gdybyś czegoś szukał.",
                "Mapa, którą nosisz w głowie, jest dokładniejsza niż ta na pergaminie.",
            ]),
            ("O niebezpieczeństwie", [
                "Jesteś śledzony. Nie odwracaj się. Idź wolno. Poczekaj... teraz uciekaj.",
                "Coś zbiera siły w mrokach. Nie wiem co. Wiem tylko, że gdy przyjdzie — będzie za późno.",
                "Jeden z twoich wrogów jest bliżej, niż myślisz. Zaufaj instynktom.",
                "Bezpieczna droga nie zawsze jest dobra. Niebezpieczna bywa lepsza.",
            ]),
        ],
    },
}


# ------------------------------------------------------------------ #
#  Wyświetlanie dialogów (UI)                                          #
# ------------------------------------------------------------------ #

def _pokaz_dialog(postac: dict) -> None:
    """Wyświetla interaktywny dialog z NPC."""
    wyswietl_linie()
    powitanie = random.choice(postac["powitania"])
    print(f"  {postac['ikona']}  {postac['imie']}:")
    print(f'  „{powitanie}"\n')

    while True:
        print("  O czym porozmawiać?\n")
        for i, (temat, _) in enumerate(postac["tematy"], 1):
            print(f"  [{i}] {temat}")
        print("  [0] Zakończ rozmowę\n")

        wybor = input("  Twój wybór: ").strip()

        if wybor == "0":
            print(f"  {postac['imie']}: „Do zobaczenia, wędrowcze!"")
            nacisnij_enter()
            return

        try:
            idx = int(wybor) - 1
            if 0 <= idx < len(postac["tematy"]):
                _, kwestie = postac["tematy"][idx]
                kwestia = random.choice(kwestie)
                wyswietl_linie()
                print(f"  {postac['ikona']}  {postac['imie']}:")
                print(f'  „{kwestia}"\n')
                nacisnij_enter()
                continue
        except ValueError:
            pass

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def dialog_karczmarz() -> None:
    """Dialog z karczmarzem."""
    _pokaz_dialog(_DIALOGI["karczmarz"])


def dialog_kupiec() -> None:
    """Dialog z kupcem."""
    _pokaz_dialog(_DIALOGI["kupiec"])


def dialog_kowal() -> None:
    """Dialog z kowalem."""
    _pokaz_dialog(_DIALOGI["kowal"])


def dialog_kaplan() -> None:
    """Dialog z kapłanem."""
    _pokaz_dialog(_DIALOGI["kaplan"])


def dialog_stary_rycerz() -> None:
    """Dialog ze starym rycerzem."""
    _pokaz_dialog(_DIALOGI["stary_rycerz"])


def dialog_tajemniczy() -> None:
    """Dialog z tajemniczą postacią."""
    _pokaz_dialog(_DIALOGI["tajemniczy_wedrowiec"])


def losowy_npc() -> None:
    """Losuje NPC i wyświetla jego dialog."""
    klucz = random.choice(list(_DIALOGI.keys()))
    _pokaz_dialog(_DIALOGI[klucz])
