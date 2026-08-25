"""Dialogi NPC — wątki postaci, testy k20 i rekrutacja."""

import random

from game.utils import wyswietl_linie, nacisnij_enter
from game.atrybuty import SKILLE, przeprowadz_test, trudnosc


_DIALOGI: dict[str, dict] = {
    "karczmarz": {
        "imie": "Karczmarz Boldan",
        "ikona": "🍺",
        "rekrut": "boldan",
        "powitania": [
            "Hej, podróżniku. Kufel jest czysty, ale sen — nie. Siadaj, jeśli umiesz słuchać.",
            "Dobrze, że zawitałeś. W tej izbie więcej prawdy wylewa się z piwa niż z kazania.",
            "Hola. Jeśli szukasz tylko strawy, masz. Jeśli szukasz człowieka — to już drożej.",
        ],
        "tematy": [
            ("O okolicy", [
                "Na wschód gobliny liczą wozy, nie trupy. To gorsze — znaczy, że ktoś nimi kieruje.",
                "Drogi w lesie milkną po zmierzchu. Kto wraca, wraca bez sakiewki albo bez imienia.",
                "Za wzgórzami ktoś znowu pali ogniska w ruinach. Nie chłopi. Chłopi boją się dymu.",
            ]),
            ("O karczmie", [
                "Ta izba stała, gdy królowie jeszcze mieli zęby. Ja tylko zmywam krew z blatów.",
                "Goście płacą za ciszę równie chętnie jak za piwo. Czasem drożej.",
                "Nie lubię rycerzy, co przysięgają na pusty kufel. Przysięga powinna śmierdzieć potem.",
            ]),
        ],
        "watek": {
            "tytul": "Córka karczmarza",
            "etapy": [
                {
                    "etykieta": "Zapytaj, czemu nie śpi",
                    "tekst": (
                        "Lira. Córka. Tydzień temu wsiadła na wóz kupca Vasca i uśmiechnęła się tak, "
                        "jakbym ja był już wspomnieniem. Mówiła, że obmywanie kufli to nie życie. "
                        "Może i ma rację. Ale nocami słyszę skrzypienie tej ławy, na której odrabiała rachunki."
                    ),
                },
                {
                    "etykieta": "Co wie o karawanie?",
                    "tekst": (
                        "Vasco wozi sukno i kłamstwa. Dostałem list — nie jej charakter, ale jej złość. "
                        "Żyje. Nie wróci. Chce murów, targu, ludzi, co nie pachną słodem. "
                        "Jeśli kiedyś staniesz w mieście, nie krzywdź jej. I nie mów, że ją „odnalazłem”. "
                        "Ona nie jest zgubą. Jest wyborem, którego nie potrafię przełknąć."
                    ),
                },
                {
                    "etykieta": "Zaproponuj inną drogę",
                    "tekst": (
                        "Osada. Twoja. Jeśli tam trafią ludzie z imionami, a nie tylko z mieczami… "
                        "może Lira zrozumie, że palenisko też bywa królestwem. "
                        "A ja? Ja umiem warzyć, liczyć i milczeć. Jeśli zapłacisz jak za całe życie, "
                        "albo jeśli twoje słowa ugną we mnie kręgosłup — pójdę. Nie po chwałę. Po sen."
                    ),
                    "nagrody": [("karma", 1)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Wyciągnij plotkę o skrytce",
                "skill": "perswazja",
                "st": 12,
                "sukces": "Dobrze… za wzgórzem ktoś zakopał sakiewkę. Nie mów, że odemnie.",
                "porazka": "Plotki kosztują. A ty nie brzmisz jak ktoś, komu ufam.",
                "nagrody": [("zloto", 18)],
            },
        ],
    },
    "kupiec": {
        "imie": "Kupiec Aldric",
        "ikona": "💰",
        "rekrut": "aldric",
        "powitania": [
            "Witaj, witaj. Uśmiech mam na sprzedaż, resztę — na kredyt, którego nie chcesz.",
            "Bohaterze. Towar nie kłamie. Ludzie tak. Dlatego wolę towar.",
            "Dobry dzień. Jeśli masz złoto, jesteś przyjacielem. Jeśli nie — jesteś opowieścią.",
        ],
        "tematy": [
            ("O towarach", [
                "Gwarancja autentyczności? Owszem. Gwarancja, że przeżyjesz — extra.",
                "Kolczuga z ostatniej dostawy pamięta poprzedniego właściciela. Nie pytaj jak.",
                "Sztylet jest tani, bo prawda bywa krótka. Miecz jest drogi, bo kłamstwo trwa.",
            ]),
            ("O gildii", [
                "Gildia kocha porządek: twój dług, ich nóż, wspólny uśmiech na rynku.",
                "Wiatrak cen kręci się w mieście. Tu, na trakcie, jeszcze da się oddychać.",
                "Kupiec, który nie boi się gildii, albo kłamie, albo już nie żyje i o tym nie wie.",
            ]),
        ],
        "watek": {
            "tytul": "Dług, który gryzie palce",
            "etapy": [
                {
                    "etykieta": "Czemu drży ci prawa dłoń?",
                    "tekst": (
                        "Czterysta sztuk. Nie towar — kara za to, że sprzedałem zboże poza rejestrem, "
                        "gdy w mieście dzieci jadły korę. Windykatorzy zaczynają od palców wskazujących. "
                        "Żebym „wiedział, czym pokazywać ceny”."
                    ),
                },
                {
                    "etykieta": "Gdzie księga długów?",
                    "tekst": (
                        "Zgubiłem ją w zaułku. Albo mi ją wyjęto. Kto ją ma, ma mnie. "
                        "Gildia nie potrzebuje sądu. Potrzebuje papieru i świadka, który kiwnie głową. "
                        "Jeśli kiedyś będziesz w mieście — nie czytaj jej głośno. Spal. Albo sprzedaj drożej niż życie."
                    ),
                },
                {
                    "etykieta": "Osada poza ich ręką",
                    "tekst": (
                        "Zaplecze. Targ, którego nie ma na mapie gildii. Jeśli mi je dasz, zerwę z nimi, "
                        "zanim zerwą mnie. Nie jestem rycerzem. Jestem człowiekiem, który umie liczyć "
                        "tak, żebyś ty miał zysk, a oni — tylko plotkę. Cena za mnie jest obleśna. "
                        "Albo twoja charyzma musi być jak pożar na rynku."
                    ),
                    "nagrody": [("zloto", 20)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Wytarguj napiwek",
                "skill": "perswazja",
                "st": 13,
                "sukces": "Lubię twój styl. Weź to — na znak, że jeszcze umiem być człowiekiem.",
                "porazka": "Ceny są ceny. Nie jestem przytułkiem.",
                "nagrody": [("zloto", 15)],
            },
        ],
    },
    "kowal": {
        "imie": "Kowal Grimbold",
        "ikona": "⚒",
        "rekrut": "grimbold",
        "powitania": [
            "Stuk, stuk. Nowy klient? Witam. Jeśli chcesz miecz, powiedz po co. Kłamać i tak usłyszę w stali.",
            "Wejdź. Tu nie gryziemy. Tylko hartujemy — i pamięć, i żelazo.",
            "Widzę ręce. Pracowały. To dobrze. Gładkie dłonie zamawiają rzeczy, których potem żałują.",
        ],
        "tematy": [
            ("O broni", [
                "Miecz to nie żelazo. To decyzja, którą ktoś podejmie, gdy ciebie już nie będzie przy kowadle.",
                "Topór nie udaje. Łuk udaje, że przemoc jest z daleka. Oba kłamią inaczej.",
                "Sztylet to broń ludzi, co myślą. I ludzi, co nie chcą, żeby myślano o nich.",
            ]),
            ("O rzemiośle", [
                "Trzydzieści lat. Mistrz mawiał: lepsza stal, lepszy wojownik. Nie powiedział, co z gorszym człowiekiem.",
                "Stal krasnoludzka pamięta górę. Ludzka — kłamstwo zamawiającego.",
                "Wiele mieczy wróciło. Żaden nie wrócił czysty. To nie wada kucia. To wada świata.",
            ]),
        ],
        "watek": {
            "tytul": "Przeklęte ostrze",
            "etapy": [
                {
                    "etykieta": "Czemu kowadło milczy dłużej niż trzeba?",
                    "tekst": (
                        "Wykowałem miecz dla rycerza z czystym herbem. Wykonał nim wieś. "
                        "Dzieci, studnię, psa. Stal to pamięta. W nocy słyszę hart, którego nie dawałem. "
                        "Nie jestem magiem. Jestem winny, bo umiałem za dobrze."
                    ),
                },
                {
                    "etykieta": "Da się to odkuć?",
                    "tekst": (
                        "Żeby przekuć klątwę, trzeba rudy, która widziała smoka. Nie metafory — ognia, "
                        "co nie pyta o herby. Leże. Wiesz, które. Jeśli doniesiesz, może przestanie szeptać. "
                        "Jeśli nie — będę kłuł pługi do śmierci i udawał, że to wystarczy."
                    ),
                },
                {
                    "etykieta": "Kuźnia w twoim obozie",
                    "tekst": (
                        "Chcę kłuć dla kogoś, kto nie każe mi zabijać niewinnych. "
                        "Twoja osada. Warsztat. Ludzie, co noszą motyki, nie wyroki. "
                        "Wezmę za to majątek albo dam się złamać słowem, jakiego nie słyszałem od mistrza."
                    ),
                    "nagrody": [("karma", 1)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Poproś o radę kowalską",
                "skill": "perswazja",
                "st": 12,
                "sukces": "Lubię ludzi, co słuchają. Weź zapas na ostrzenie. I nie rób ze stali alibi.",
                "porazka": "Nie mam czasu na gadki. Albo kujesz, albo wychodzisz.",
                "nagrody": [("zloto", 12)],
            },
        ],
    },
    "kaplan": {
        "imie": "Kapłan Eremiel",
        "ikona": "🙏",
        "rekrut": "eremiel",
        "powitania": [
            "Oby bogowie strzegli twojej ścieżki. Ja strzegę resztek, które im zostawiliśmy.",
            "Wejdź. Modlitwa nie boli. Milczenie czasem tak.",
            "Przybywasz w porę. Albo za późno. W świątyni te dwa słowa często znaczą to samo.",
        ],
        "tematy": [
            ("O błogosławieństwach", [
                "Ochrona raz na wyprawę. Bogowie lubią rachunek. Nie lubią, gdy ktoś prosi w kółko o to samo.",
                "Wiara nie zastąpi zbroi. Zbroja nie zastąpi tego, po co wracasz z pola.",
                "Znak na czole to nie tarcza. To obietnica, że jeśli padniesz, ktoś powie twoje imię.",
            ]),
            ("O złu", [
                "Gobliny są głośne. Prawdziwy mrok uczy się szeptać liturgią.",
                "Nieumarli nie nienawidzą. Tęsknią źle. To gorsze.",
                "W ruinach coś śpi. Modlę się, żeby sen był głębszy niż nasza ciekawość.",
            ]),
        ],
        "watek": {
            "tytul": "Rozłam w zakonie",
            "etapy": [
                {
                    "etykieta": "Czemu kadzidło pachnie strachem?",
                    "tekst": (
                        "Relikwiarz skradziono. Bracia oskarżają się jak pijani sędziowie. "
                        "Każdy chce być czysty. Nikt nie chce być odpowiedzialny. "
                        "W zakonie to ten sam grzech, tylko w ładniejszym szacie."
                    ),
                },
                {
                    "etykieta": "Cień z północy",
                    "tekst": (
                        "Szeptał imiona. W tym moje. Boję się, że to ja otworzyłem drzwi, "
                        "modląc się o znak, gdy wiara była cienka jak opłatek. "
                        "Jeśli znak przyszedł — przyszedł zębami."
                    ),
                },
                {
                    "etykieta": "Świeckie ramię",
                    "tekst": (
                        "Potrzebuję obozu, który nie jest ołtarzem. Miejsca, gdzie herezja "
                        "nie kończy się stosem, tylko rozmową i pracą. Pójdę z tobą, jeśli "
                        "zapłacisz jak za odpust króla — albo jeśli twoja charyzma uniesie winę, "
                        "której ja nie umiem unieść sam."
                    ),
                    "nagrody": [("mikstura", 1)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Wyznaj zmartwienie (Spostrzegawczość)",
                "skill": "spostrzegawczosc",
                "st": 13,
                "sukces": "Widzę więcej, niż mówisz. Przyjmij dar — przyda się, gdy liturgia zmilknie.",
                "porazka": "Twoje serce jest zamknięte. Wróć, gdy będziesz gotów.",
                "nagrody": [("mikstura", 1)],
            },
        ],
    },
    "stary_rycerz": {
        "imie": "Stary Rycerz Alderon",
        "ikona": "🗡",
        "rekrut": "alderon",
        "powitania": [
            "Hm. Młody. Dobre oczy. Siadaj. Stojąc, ludzie kłamią szybciej.",
            "Za moich czasów przysięga pachniała krwią i sianem. Dziś pachnie pieczęcią.",
            "Usiądź. Nie lubię gadać z posągami. Posągi nie żałują.",
        ],
        "tematy": [
            ("O walce", [
                "Siła to nagrobek z ładnym napisem. Głowa to to, co zostaje, gdy napis zblednie.",
                "Nie stój jak słup. Słup jest od tego, żeby go obalili i zrobili z niego stos.",
                "Kto ostatni stoi, wygrywa. Kto pierwszy myśli — czasem nie musi stać do końca.",
            ]),
            ("O świecie", [
                "Królowie gniją w tym samym tempie co chłopi. Smoki mają więcej czasu.",
                "Za wzgórzami miasto. Tam sprawiedliwość nosi perukę i liczy podatki od głodu.",
                "Gobliny irytują. Orkowie bolą. Zdrajca, którego karmiłeś — ten zostaje.",
            ]),
        ],
        "watek": {
            "tytul": "Ostatnia przysięga",
            "etapy": [
                {
                    "etykieta": "Jaką przysięgę jeszcze nosisz?",
                    "tekst": (
                        "Martwemu królowi obiecałem zdrajcę. Giermek. Jadł z mojej miski, "
                        "spał pod moim płaszczem, a potem otworzył bramę nocą. "
                        "Król umarł z pytaniem na ustach. Ja zostałem z odpowiedzią, której nie mam."
                    ),
                },
                {
                    "etykieta": "Gdzie jest giermek?",
                    "tekst": (
                        "W mieście. Poborca. Liczy głowy jak bydło i uśmiecha się do burmistrzyni. "
                        "Jestem za stary na samosąd w bramie. A za młody na to, by powiedzieć, "
                        "że przysięga wygasła, bo ja zmęczyłem się jej ciężarem."
                    ),
                },
                {
                    "etykieta": "Sprawiedliwość, nie rzeź",
                    "tekst": (
                        "Pójdę z tobą, jeśli obiecasz, że gdy go znajdziemy, nie zrobisz z tego widowiska. "
                        "Sąd. Albo wygnanie. Nie stos dla tłumu. Zapłacisz jak za cały regiment — "
                        "bo regimentem już nie jestem. Albo powiesz to tak, że stary pies znowu uwierzy w rozkaz."
                    ),
                    "nagrody": [("karma", 1)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Zastrasz, by zdradził słabość wrogów",
                "skill": "zastraszanie",
                "st": 14,
                "sukces": "Hah. Masz jaja. Gobliny padają, gdy celujesz w kolana. Ludzie — gdy w dumę.",
                "porazka": "Młody, na mnie takie numery nie działają. Widziałem gorszych.",
                "nagrody": [("zloto", 10)],
            },
        ],
    },
    "tajemniczy_wedrowiec": {
        "imie": "Ashen Wędrowiec",
        "ikona": "🌑",
        "rekrut": "ashen",
        "powitania": [
            "Psst. Nie tak blisko. Cień lubi dystans. Ja też.",
            "Widziałem cię w miejscu, którego jeszcze nie ma. Nie dziękuj. To nie komplement.",
            "Szukasz. Dobrze. Ci, co znaleźli, zwykle żałują, że przestali szukać.",
        ],
        "tematy": [
            ("O przeznaczeniu", [
                "Przeznaczenie to alibi dla tchórzy i marketing dla proroków.",
                "Byłem tam, gdzie ty dojdziesz. Powiem ci tylko: zabierz wodę. I kogoś, kto umie kłamać.",
                "Twoja przyszłość jest niezapisana, bo ktoś spalił księgę. Nie pytaj kto.",
            ]),
            ("O niebezpieczeństwie", [
                "Jesteś śledzony. Nie odwracaj się. Śledzący lubi, gdy się odwracasz — wtedy jesteś grzeczny.",
                "Coś zbiera siły w mroku. Gdy przyjdzie, będzie miało twoje pytania, nie twoje odpowiedzi.",
                "Bezpieczna droga kończy się w urzędzie. Niebezpieczna — w prawdzie albo w rowie.",
            ]),
        ],
        "watek": {
            "tytul": "Kamienne Serce",
            "etapy": [
                {
                    "etykieta": "Czemu nie masz cienia w południe?",
                    "tekst": (
                        "Bo nie jestem do końca tu. To nie poezja. W południe świat wymaga pełnego kształtu. "
                        "Ja mam tylko odłamek. Reszta leży pod pieczęcią, którą ludzie nazywają ruinami, "
                        "żeby nie musieli nazywać jej grobem."
                    ),
                },
                {
                    "etykieta": "Kamienne Serce",
                    "tekst": (
                        "Kto zbierze odłamki, budzi to, co śpi. Ja jestem jednym z nich, który wolał chodzić "
                        "niż być ołtarzem. Dlatego szeptam, dlatego kłamię, dlatego nie wchodzę do świątyń "
                        "w południe. Kapłani czują dziurę. Myślą, że to grzech. To geometria."
                    ),
                },
                {
                    "etykieta": "Schowaj się w obozie",
                    "tekst": (
                        "Chcę zniknąć między zwykłymi ludźmi. Palenisko zagłusza sny lepiej niż runy. "
                        "Weźmiesz mnie za fortunę — albo za słowo, które ugnie nawet pieczęć. "
                        "Jeśli to drugie, pamiętaj: charyzma to też zaklęcie. Tylko tańsze w krwi."
                    ),
                    "nagrody": [("zloto", 25)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Blefuj, że znasz jego sekret",
                "skill": "oszustwo",
                "st": 15,
                "sukces": "Więc jednak wiesz. Weź to i znikaj, zanim ktoś nas usłyszy naprawdę.",
                "porazka": "Nie znasz nic. Odchodź, zanim się zdenerwuję. A ja zdenerwowany nie bywam — bywam.",
                "nagrody": [("zloto", 25)],
            },
        ],
    },
    "burmistrz": {
        "imie": "Burmistrz Mirena",
        "ikona": "🏛",
        "rekrut": "mirena",
        "powitania": [
            "Mów krótko. Miasto nie ma czasu na eposy, a ja — na ludzi, co je opowiadają zamiast działać.",
            "Jeśli niesiesz zboże, jesteś sojusznikiem. Jeśli niesiesz radę — stań w kolejce za głodem.",
            "Witaj za murami. Tu każdy uśmiech ma stawkę podatkową.",
        ],
        "tematy": [
            ("O mieście", [
                "Mury chronią przed wilkami. Przed radą miasta chroni tylko bezsenność.",
                "Straż służy temu, kto płaci obiad. Dziś obiad płacą spichlerze, nie ja.",
                "Na rynku prawda kosztuje więcej niż jedwab, bo jedwab można podrobić ładniej.",
            ]),
            ("O prawie", [
                "Prawo jest mostem. Bogaci idą po nim. Biedni — pod nim, w ścieku, i też dochodzą.",
                "Mogę wieszać złodziei chleba. Kupców zboża — nie. Siedzą w radzie i głosują moje wyroki.",
                "Sprawiedliwość bez spichlerza to kazanie na czczo. Słychać je lepiej, działa gorzej.",
            ]),
        ],
        "watek": {
            "tytul": "Głód za murami",
            "etapy": [
                {
                    "etykieta": "Ile kłamie spichlerz?",
                    "tekst": (
                        "Liczymy gęby. Ziarna jest na papierze. W workach — na plotkę. "
                        "Ktoś przesuwa cyfry w nocy. Nie goblin. Goblin nie umie pisać tak równo."
                    ),
                },
                {
                    "etykieta": "Kupcy czy zdrajcy?",
                    "tekst": (
                        "Trzymają zboże, aż cena będzie jak nóż. Nie mogę ich powiesić. Są radą. "
                        "Mogę tylko patrzeć, jak miasto uczy się jeść wolniej. To też jest polityka. "
                        "Najbrzydsza."
                    ),
                },
                {
                    "etykieta": "Sojusz z twoją osadą",
                    "tekst": (
                        "Potrzebuję targu na odludziu. Szlaku, którego nie ma w księgach gildii. "
                        "Jeśli dasz mi miejsce przy twoim palenisku, zostawię łańcuch burmistrza "
                        "temu, kto lubi go bardziej niż ludzi. Cena za mnie jest skandaliczna — "
                        "jak za całą radę. Albo przekonaj mnie tak, że zapomnę, czym jest urząd."
                    ),
                    "nagrody": [("karma", 1)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Wyjednaj zniżkę celną",
                "skill": "perswazja",
                "st": 16,
                "sukces": "Na jeden wóz. I nie mów radzie, że mam serce. To szkodzi notowaniom.",
                "porazka": "Cła są kręgosłupem miasta. Nie złamię go dla ładnej mowy.",
                "nagrody": [("zloto", 30)],
            },
        ],
    },
    "kupiec_miejski": {
        "imie": "Kupiec Vasco",
        "ikona": "🐪",
        "rekrut": "vasco",
        "powitania": [
            "Sukno, sól, milczenie. W tej kolejności. Czwartego nie sprzedaję — to już polityka.",
            "Witaj na rynku. Jeśli nie kupujesz, i tak słucham. Informacja też ma taryfę.",
            "Aldric cię przysłał? Nie. Aldric nie przysyła ludzi. Aldric przysyła długi.",
        ],
        "tematy": [
            ("O karawanach", [
                "Wóz, który jedzie nocą, nie zawsze wiezie sukno. Czasem wiezie decyzje rady.",
                "Lira? Znam imię. Nie sprzedaję ludzi. Sprzedaję przejazd. Różnica jest cienka i mokra.",
                "Gildia lubi, gdy ktoś inny brudzi ręce. Ja mam rękawice. Droższe niż twoja zbroja.",
            ]),
            ("O zysku", [
                "Zysk bez ryzyka to podatek. Ryzyko bez zysku to bohaterstwo. Nie handluję drugim.",
                "Twoja osada na mapie to dziura. Dziury są cenne: nikt nie cłuje dziur.",
                "Partner milczący żyje dłużej. Partner głośny żyje ciekawiej. Wybieraj żołądkiem.",
            ]),
        ],
        "watek": {
            "tytul": "Cichy udział",
            "etapy": [
                {
                    "etykieta": "Co naprawdę wieziesz?",
                    "tekst": (
                        "Więcej niż sukno. Straż tego nie widzi albo nie chce. "
                        "Nie pytaj o worki. Pytaj o to, kto płacze, gdy worki dojeżdżają. "
                        "Czasem nikt. To najlepsze kursy."
                    ),
                },
                {
                    "etykieta": "Lira Boldanówna",
                    "tekst": (
                        "Żyje. Pracuje przy wadze. Nie chcę, żeby ojciec tu wlazł z nożem i kazaniem. "
                        "Ona wybrała miasto jak ktoś wybiera ogień zamiast dymu. "
                        "Mogę przekazać list. Nie mogę przekazać jej z powrotem. Nie jest towarem."
                    ),
                    "nagrody": [("karma", 1)],
                },
                {
                    "etykieta": "Udział w osadzie",
                    "tekst": (
                        "Twoje chaty, mój wóz. Targ, którego gildia nie ma w księgach. "
                        "Jeśli mnie weźmiesz, uznają cię za konkurencję. To komplement i wyrok w jednym. "
                        "Zapłacisz jak za karawanę królewską — albo zagadasz mnie tak, że sam uwierzę, "
                        "iż uczciwość jest opłacalna. To byłby nowy towar na tym rynku."
                    ),
                    "nagrody": [("zloto", 18)],
                },
            ],
        },
        "testy": [
            {
                "etykieta": "Wytarguj ciszę o twojej osadzie",
                "skill": "oszustwo",
                "st": 15,
                "sukces": "Nie słyszałem o żadnym obozie. Słyszałem o wilkach. Wilki nie płacą cła.",
                "porazka": "Za głośno handlujesz tajemnicą. To amatorszczyzna.",
                "nagrody": [("zloto", 20)],
            },
        ],
    },
}


def _nagrody_dialogu(gracz, nagrody: list) -> None:
    for typ, wartosc in nagrody:
        if typ == "zloto":
            gracz.zloto += wartosc
            print(f"  💰  Zyskujesz {wartosc} złota.")
        elif typ == "mikstura":
            gracz.mikstury += wartosc
            print(f"  🧪  Dostajesz {wartosc} miksturę.")
        elif typ == "karma":
            gracz.karma = getattr(gracz, "karma", 0) + wartosc
            print(f"  ✨  Karma {wartosc:+d}.")


def _etap_watku(gracz, klucz: str) -> int:
    if getattr(gracz, "watki_npc", None) is None:
        gracz.watki_npc = {}
    return int(gracz.watki_npc.get(klucz, 0))


def _ustaw_etap_watku(gracz, klucz: str, etap: int) -> None:
    if getattr(gracz, "watki_npc", None) is None:
        gracz.watki_npc = {}
    gracz.watki_npc[klucz] = etap


def _pokaz_watek(postac: dict, klucz: str, gracz) -> None:
    watek = postac.get("watek") or {}
    etapy = watek.get("etapy") or []
    if not etapy:
        return
    idx = _etap_watku(gracz, klucz)
    wyswietl_linie()
    print(f"  {postac['ikona']}  {postac['imie']}  —  {watek.get('tytul', 'Wątek')}")
    if idx >= len(etapy):
        print(
            f'  „To już opowiedziałem. Reszta dzieje się, gdy nie gadasz, tylko żyjesz.”'
        )
        print()
        nacisnij_enter()
        return
    etap = etapy[idx]
    print(f'  „{etap["tekst"]}"\n')
    _nagrody_dialogu(gracz, etap.get("nagrody") or [])
    _ustaw_etap_watku(gracz, klucz, idx + 1)
    if idx + 1 >= len(etapy):
        print("  (Wątek tej postaci dobiegł końca — ale decyzje jeszcze nie.)")
    else:
        print("  (Wątek posunął się naprzód. Wróć, by usłyszeć więcej.)")
    print()
    nacisnij_enter()


def _pokaz_dialog(klucz: str, gracz=None) -> None:
    """Wyświetla interaktywny dialog z NPC (wątek, testy, rekrutacja)."""
    postac = _DIALOGI[klucz]
    wyswietl_linie()
    powitanie = random.choice(postac["powitania"])
    print(f"  {postac['ikona']}  {postac['imie']}:")
    print(f'  „{powitanie}"\n')
    testy = list(postac.get("testy") or [])
    uzyte: set[int] = set()
    watek = postac.get("watek") or {}
    etapy = watek.get("etapy") or []

    while True:
        print("  O czym porozmawiać?\n")
        tematy = postac["tematy"]
        opcje: list[tuple[str, str]] = []
        for temat, _ in tematy:
            opcje.append(("temat", temat))
        if gracz is not None and etapy:
            idx_w = _etap_watku(gracz, klucz)
            if idx_w < len(etapy):
                opcje.append(("watek", f"📜 {etapy[idx_w]['etykieta']}"))
            else:
                opcje.append(("watek", f"📜 {watek.get('tytul', 'Wątek')} (zakończony)"))
        if gracz is not None:
            for test in testy:
                opcje.append(("test", test["etykieta"]))
        if gracz is not None and postac.get("rekrut"):
            opcje.append(("rekrut", "🤝 Zaproponuj dołączenie do osady"))

        for i, (_, etykieta) in enumerate(opcje, 1):
            if opcje[i - 1][0] == "test":
                test_nr = sum(1 for t, _ in opcje[: i - 1] if t == "test")
                if test_nr in uzyte:
                    print(f"  [{i}] 🎲 {etykieta}  (już próbowałeś)")
                    continue
                test = testy[test_nr]
                st = trudnosc(gracz, test["st"])
                nazwa = SKILLE[test["skill"]]["nazwa"]
                print(f"  [{i}] 🎲 {etykieta}  ({nazwa} ST {st})")
            else:
                print(f"  [{i}] {etykieta}")
        print("  [0] 🚶 Zakończ rozmowę\n")

        wybor = input("  Twój wybór: ").strip()
        if wybor == "0":
            print(f"  {postac['imie']}: „Do zobaczenia. Nie wszystko da się dogadać przy stole.”")
            nacisnij_enter()
            return

        try:
            idx = int(wybor) - 1
        except ValueError:
            print("  Nieprawidłowy wybór.")
            nacisnij_enter()
            continue

        if not 0 <= idx < len(opcje):
            print("  Nieprawidłowy wybór.")
            nacisnij_enter()
            continue

        rodzaj, _ = opcje[idx]
        if rodzaj == "temat":
            temat_idx = sum(1 for t, _ in opcje[:idx] if t == "temat")
            _, kwestie = tematy[temat_idx]
            kwestia = random.choice(kwestie)
            wyswietl_linie()
            print(f"  {postac['ikona']}  {postac['imie']}:")
            print(f'  „{kwestia}"\n')
            nacisnij_enter()
            continue

        if rodzaj == "watek" and gracz is not None:
            _pokaz_watek(postac, klucz, gracz)
            continue

        if rodzaj == "test" and gracz is not None:
            test_idx = sum(1 for t, _ in opcje[:idx] if t == "test")
            if test_idx in uzyte:
                print("  Już to próbowałeś w tej rozmowie.")
                nacisnij_enter()
                continue
            test = testy[test_idx]
            uzyte.add(test_idx)
            st = trudnosc(gracz, test["st"])
            wynik = przeprowadz_test(gracz, test["skill"], st)
            wyswietl_linie()
            print(f"  {postac['ikona']}  {postac['imie']}:")
            if wynik.sukces:
                print(f'  „{test["sukces"]}"')
                _nagrody_dialogu(gracz, test.get("nagrody") or [])
            else:
                print(f'  „{test["porazka"]}"')
            print()
            nacisnij_enter()
            continue

        if rodzaj == "rekrut" and gracz is not None:
            from game.rekruci import proponuj_rekrutacje_npc
            proponuj_rekrutacje_npc(gracz, postac["rekrut"])
            continue

        print("  Nieprawidłowy wybór.")
        nacisnij_enter()


def dialog_karczmarz(gracz=None) -> None:
    _pokaz_dialog("karczmarz", gracz)


def dialog_kupiec(gracz=None) -> None:
    _pokaz_dialog("kupiec", gracz)


def dialog_kowal(gracz=None) -> None:
    _pokaz_dialog("kowal", gracz)


def dialog_kaplan(gracz=None) -> None:
    _pokaz_dialog("kaplan", gracz)


def dialog_stary_rycerz(gracz=None) -> None:
    _pokaz_dialog("stary_rycerz", gracz)


def dialog_tajemniczy(gracz=None) -> None:
    _pokaz_dialog("tajemniczy_wedrowiec", gracz)


def dialog_burmistrz(gracz=None) -> None:
    _pokaz_dialog("burmistrz", gracz)


def dialog_kupiec_miejski(gracz=None) -> None:
    _pokaz_dialog("kupiec_miejski", gracz)


def losowy_npc(gracz=None) -> None:
    pula = (
        "karczmarz",
        "kupiec",
        "kowal",
        "kaplan",
        "stary_rycerz",
        "tajemniczy_wedrowiec",
    )
    klucz = random.choice(pula)
    _pokaz_dialog(klucz, gracz)
