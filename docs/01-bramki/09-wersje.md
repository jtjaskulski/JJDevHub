# 09. Wersje komponentów

Poprzedni numer to [obrazy](08-obrazy.md). Następny folder zaczyna się od [Aspire](../02-szkielet-i-deploy/01-aspire.md).

Ten numer dodaje osobny numer wersji każdego komponentu i check `release-tags`. Tag powstaje na pushu do `main`, tylko dla komponentu, którego plik wersji się zmienił. Obrazu ten numer nie buduje. Zostawia pliki, z których późniejszy deploy wyczyta `learning-v0.3.0`.

## Po co

Jeden tag `v1.2.3` na całe repozytorium przebudowuje Identity, gdy zmieniła się notatka. Nie da się cofnąć samego News do poprzedniego wydania. Osobny tag na komponent to umożliwia. Check na pull requeście pilnuje, że plik wersji w ogóle jest spójny, zanim `main` dostanie push, z którego tag się wycina.

Bez tego numeru [deploy](../02-szkielet-i-deploy/07-deploy.md) nie ma czego wgrać.

## Dlaczego ta technologia

### release-please

release-please czyta commity Conventional Commits na `main` i otwiera pull request z podbiciem `versions/<komponent>.json`. Problem, który zamyka, to ręczne edytowanie trzech plików wersji i zapominanie o changelogu. Odpada jeden `version.json` w korzeniu. Koszt: manifest w `.github/release-please-config.json` musi znać listę komponentów. Nowy komponent dopisuje się tu i w słowniku w tym samym scaleniu.

Wtyczka `linked-versions` nie wchodzi. Powiązanie „wszystko na ten sam numer” znosi sens osobnych tagów.

### SemVer i seria `0.y.z`

Przed `1.0.0` zmiana łamiąca, oznaczona `!` w tytule, podbija minor. Od `1.0.0` podbija major. `feat` podbija minor. `fix` podbija patch. Problem, który zamyka, to `1.0.0` wystawione na pierwszej usłudze, która jeszcze nie ma klientów poza tym repozytorium. Odpada brak reguły i wersje `1.0.0` oraz `1.0.1` tego samego dnia bez różnicy znaczenia. Koszt: trzeba pamiętać, że `0.2.0` może złamać kontrakt. Klienci web i telefon są w tym samym repozytorium i czytają `GET /versions`, więc widzą numer, który właśnie wszedł.

Wersja `0.0.0` nie dostaje tagu. Plik startowy komponentu ma `0.0.0`, a pierwszy tag to pierwsze realne podbicie, zwykle `0.1.0`, gdy komponent pierwszy raz jest wydawany.

### Jedna wersja, kilka obrazów

Komponent `learning` wydaje obrazy `jjdevhub-learning-api`, `jjdevhub-learning-relay` i `jjdevhub-learning-projector`. Wszystkie trzy noszą ten sam tag `learning-vX.Y.Z`. Problem, który zamyka, to relay w wersji `0.4.0` czytający outbox, którego api `0.3.0` jeszcze nie pisze. Odpada osobny numer wersji na każdy proces. Koszt: poprawka samego relay podbija też api. To jest akceptowane, bo procesy jednej usługi wdraża się razem.

`web` i `mobile` mają po jednym artefakcie. `gateway` ma jeden obraz.

### Jądro podbija usługi

Zmiana w `JJDevHub.Shared.Kernel` nie ma własnego tagu. Ma zakres `shared`. Skrypt wersji podbija każdy komponent, który ma `ProjectReference` do jądra. Problem, który zamyka, to ciche zostawienie News na starym `Result`. Odpada ręczna lista „pamiętaj podbić wszystkie”. Koszt: mała poprawka w jądrze jest pięcioma tagami, gdy pięć usług już istnieje. Zanim usługi istnieją, podbicie dotyczy pustej listy i check loguje, że nie ma kogo podbijać.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| komponent | jednostka tagu ze słownika |
| manifest release-please | który katalog należy do którego komponentu |
| `!` w tytule | zmiana łamiąca |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/release-please-config.json` | komponenty i ścieżki |
| `.github/workflows/versions.yml` | job `release-tags` |
| `versions/identity.json` i pozostałe siedem plików | `{"version":"0.0.0"}` na starcie |
| `.github/scripts/bump-shared.sh` | podbicie usług zależnych od jądra |

Osiem plików: `identity`, `news`, `learning`, `cv`, `contact`, `gateway`, `web`, `mobile`.

## Plan

### 1. Pliki wersji

Każdy plik to JSON z polem `version`. Start to `0.0.0`. Check `release-tags` pada, gdy plik komponentu ze słownika nie istnieje, albo gdy JSON jest zły, albo gdy wersja nie pasuje do `^[0-9]+\.[0-9]+\.[0-9]+$`.

### 2. Manifest

Manifest mapuje ścieżki:

| Komponent | Ścieżki, które go ruszają |
| --- | --- |
| `learning` | `src/services/Learning/` |
| `identity` | `src/services/Identity/` |
| `news` | `src/services/News/` |
| `cv` | `src/services/Cv/` |
| `contact` | `src/services/Contact/` |
| `gateway` | `src/gateway/` |
| `web` | `src/clients/web/` |
| `mobile` | `src/clients/mobile/` |

`src/shared/JJDevHub.Shared.Kernel/` nie jest w manifeście jako komponent. Obsługuje go `bump-shared.sh`.

### 3. Job na pull requeście

`release-tags` na pull requeście nie tworzy tagu. Sprawdza pliki i, gdy tytuł to `feat` albo `fix` z zakresem komponentu, sprawdza, że release-please miałby co podbić. Samo podbicie dzieje się na `main`. Na pull requeście do `develop` check jest zielony, gdy pliki są poprawne. Nie wymaga podbicia wersji w tym samym pull requeście co kod: podbiciem jest osobny pull request release-please po wejściu na `main`. Inaczej każdy numer edytowałby wersję ręcznie i manifest byłby zbędny.

Gdy w tytule jest zakres komponentu, którego pliku wersji nie ma, check pada.

### 4. Tag na `main`

Na `push` do `main` job woła release-please. Dla komponentu, którego commity od ostatniego tagu podbijają wersję, powstaje tag `<komponent>-vX.Y.Z` i wpis w `versions/<komponent>.json`. Wersja, która zostaje `0.0.0`, nie dostaje tagu. Tag nie uruchamia deployu sam. Deploy słucha tagu w swoim workflow, od numeru deployu.

Zmiana tylko w News nie tworzy tagu `identity-v…`.

### 5. Ruleset

`release-tags` wchodzi do trzech rulesetów. Jest wymagany tak samo jak build.

### Co jest do czego

| Plik | Kto go czyta |
| --- | --- |
| `versions/*.json` | release-please, stopka klienta przez numer wbity przy buildzie, `GET /versions` |
| tag | workflow deployu |
| `bump-shared.sh` | job, gdy diff ruszył jądro |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| osiem plików `0.0.0` | `release-tags` w trybie sprawdzenia | kod `0`, brak tagu |
| tytuł `feat(cicd): x` | skrypt tytułu wersji | kod `1`, bo `cicd` nie jest komponentem |
| diff tylko w `src/services/News/` | manifest | kandydatem jest wyłącznie `news` |

## Checkpoint

```bash
test -f .github/workflows/versions.yml
test -f .github/release-please-config.json
for c in identity news learning cv contact gateway web mobile; do test -f versions/$c.json; done
python3 -c "import json,pathlib; [json.load(open(p)) for p in pathlib.Path('versions').glob('*.json')]"
actionlint .github/workflows/versions.yml
```

Każdy plik ma `version` równe `0.0.0`.

## Umiesz wyjaśnić

Dlaczego `shared` nie ma tagu?

Bo jądro nie jest procesem. Wydaniem jest usługa, która jądro wciąga. Podbijamy te usługi, nie osobny obraz jądra.

Czemu przed `1.0.0` złamanie podbija minor?

Bo SemVer tak traktuje serię zerową: major zero znaczy, że API jeszcze nie jest stabilne. `!` i tak jest widoczne w tytule.

Dlaczego relay i api mają ten sam numer?

Bo czytają ten sam outbox. Rozjazd wersji między nimi jest gorszy niż wspólne wydanie przy małej poprawce.

Kto tworzy tag?

Job na `main`, nie człowiek z laptopa. Tag z laptopa nie ma przebiegu, do którego da się przywiązać atestację.

Czy pull request funkcji musi ruszyć `versions/*.json`?

Nie. Rusza go release-please po scaleniu na `main`. Pull request funkcji ma zielony `release-tags`, gdy pliki istnieją i zakres tytułu jest z listy.

## Zasady

- Osiem komponentów, osiem plików, żadnego tagu `vX.Y.Z` bez prefiksu komponentu.
- `linked-versions` nie wchodzi.
- `0.0.0` nie jest tagiem.
- Zmiana News nie taguje Identity.
- Tag nie jest tworzony z gałęzi roboczej.

## Poza zakresem

- Budowanie obrazu i wysłanie na maszynę są w [07-deploy.md](../02-szkielet-i-deploy/07-deploy.md).
- Stopka `web-vX.Y.Z` jest w [02-szkielet.md](../11-web/02-szkielet.md). Ekran telefonu jest w [03-ekrany.md](../12-mobile/03-ekrany.md).
- `GET /versions` jest w [02-gateway.md](../02-szkielet-i-deploy/02-gateway.md).

## Źródła

- [SemVer](https://semver.org/lang/pl/). Wiążące są major, minor, patch i seria `0.y.z`. Dotyczy reguły podbijania. Ze źródła nie bierzemy jednego tagu na całe repozytorium.
- [release-please](https://github.com/googleapis/release-please). Wiążący jest manifest komponentów i pull request z podbiciem. Dotyczy tagu na `main`. Ze źródła nie bierzemy `linked-versions`.
- [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/). Wiążące są `feat`, `fix` i `!` jako wejście do podbicia. Dotyczy tego, skąd release-please bierze numer. Ze źródła nie bierzemy zakresu spoza słownika.

## Gotowe gdy

- Istnieje osiem plików `versions/<komponent>.json` z `0.0.0`.
- Check `release-tags` jest w trzech rulesetach.
- Manifest nie zawiera `linked-versions`.
- Tytuł z zakresem spoza listy komponentów, `infra` i `shared` pada już na [pr-title](02-galezie.md), a ten check pada dodatkowo, gdy zakres wygląda jak komponent, ale pliku wersji nie ma.
- Job na pull requeście nie tworzy tagu. Tag opisany w workflow powstaje wyłącznie na `push` do `main`.
