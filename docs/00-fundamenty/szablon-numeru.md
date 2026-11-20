# Szablon numeru

Każdy plik numerowany trzyma sekcje w tej kolejności. Pliki fundamentu tego szablonu nie wypełniają, bo nie wdrażają kodu.

| Sekcja | Co w niej jest |
| --- | --- |
| wstęp | co powstaje, link do numeru poprzedniego i następnego, jeden akapit |
| `## Po co` | problem, który numer zamyka, i scenariusz, który bez niego się psuje |
| `## Dlaczego ta technologia` | podsekcja `###` na każdą decyzję: czym jest w tym repozytorium, jaki problem zamyka, co odpadło, jaki koszt zostaje |
| `## Pojęcia` | słowa, bez których reszta numeru jest niejasna; termin techniczny zostaje po angielsku, zdanie jest po polsku |
| `## Klasy i ścieżki` | tabela: plik albo workflow i jego odpowiedzialność |
| `## Plan` | kroki `### 1.` i na końcu `### Co jest do czego` |
| `## Testy` | jeden fakt, jeden powód padnięcia, arrange / act / assert, komenda |
| `## Checkpoint` | komenda i oczekiwany wynik; bez tego wyniku numer nie jest zamknięty |
| `## Umiesz wyjaśnić` | pytania, na które odpowiedź ma paść z pamięci, zanim powstanie następny numer; pod każdym jest krótka odpowiedź wzorcowa |
| `## Zasady` | reguły, których kod tego numeru nie łamie |
| `## Poza zakresem` | link do numeru, który temat domyka |
| `## Źródła` | lista: adres, potem dwa do czterech zdań — co jest wiążące, której decyzji dotyczy, czego ze źródła nie bierzemy |
| `## Gotowe gdy` | warunki końca; ostatni da się sprawdzić komendą albo plikiem |

`## Dlaczego ta technologia` tłumaczy wybór, zanim powstanie kod. `## Plan` mówi, co zrobić, i nie powtarza uzasadnień. `### Co jest do czego` odpowiada, po co klasa albo job istnieje i kto go woła.

`## Umiesz wyjaśnić` nie jest ozdobą. Jeśli odpowiedź wzorcowa nie daje się powiedzieć własnymi słowami, numer nie jest zrozumiany i nie wpisuje się go do [postep.md](postep.md). Następny numer buduje na tym, więc luka zostaje w kodzie.

## Objętość i kod

Numer mieści jeden pomysł. Kluczowy fragment kodu jest w `## Plan` i jest krótki: kontrakt, rejestracja, zapytanie albo kawałek workflow. Resztę plików opisuje tabela `## Klasy i ścieżki`, nie drugi pełny listing.

Fragment w numerze jest tym, który ma trafić do repozytorium. Późniejszy numer nie zmienia jego nazw. Gdy musi rozszerzyć typ, pisze, które pole doszło i dlaczego poprzedni kontrakt nadal działa.

## Test

Test ma jeden powód do padnięcia. Nazwa metody mówi, co jest prawdą, gdy test przechodzi, na przykład `Create_blank_title_returns_invalid`. Arrange, act i assert są rozdzielone. Test, który sprawdza kontener i regułę domeny naraz, sprawdza za dużo.

Runner to xUnit i `[Fact]`. Cecha `Category=Integration` i `Category=Performance` jest w słowniku. Test bez cechy leci w `unit-tests`.

## Źródła

Punkt w `## Źródła` zaczyna się od adresu. Dalej jest to, co w tym adresie obowiązuje ten numer, której decyzji to dotyczy i czego ze strony nie bierzemy. Adres bez tych zdań nie wystarcza, bo z jednej strony dokumentacji da się wziąć i narzędzie, i układ, którego ten produkt nie chce.

Książka bez stabilnego adresu rozdziału wchodzi tak samo: autor, tytuł, potem te same dwa do czterech zdań.

## Czego szablon zabrania

- Drugiej nazwy na byt ze słownika.
- Wartości sekretu, nawet przykładowego hasła produkcyjnego. Przykład lokalny ma jawnie napisane, że działa tylko na laptopie.
- Kroku, który należy do innego numeru. Link wystarcza.
- Łączenia dwóch numerów w jednym opisie wdrożenia.
- Uzasadnienia w `## Plan` i listy plików w `## Dlaczego ta technologia`.
