# 07. Skalowanie

Poprzedni numer to [Redis](06-redis.md). Następny to [Azure](08-azure.md).

Ten numer stawia HPA na wdrożeniach aplikacji: minimum 1, maksimum 2. Podnosi drugą replikę, gdy CPU przekroczy 70 procent requestu albo gdy liczba żądań HTTP w toku przekroczy 20. Postgres, Kafka, Redis, OpenBao, telemetria i tunel nie dostają HPA. Inbox sprawia, że drugi konsument nie zastosuje tej samej wiadomości drugi raz.

## Po co

Jedna replika leży razem z węzłem albo nie wyrabia przy dwóch ciężkich żądaniach. Druga replika zamyka to w granicach jednej maszyny: nie udajemy farmy. Sufit 2 zamyka rachunek, którego nikt nie pilnuje, i zamyka przypadek, w którym zła metryka podnosi dziesięć Podów. Inbox, który już jest, zamyka podwójny push i podwójną projekcję.

## Dlaczego ta technologia

### HPA, nie ręczna replika

`HorizontalPodAutoscaler` patrzy na CPU względem requestu i na metrykę zewnętrzną albo na metrykę poda `http_in_flight`, jeśli metrics-server i adapter, który mamy, umieją ją podać. Jeśli na jednym k3s adapter metryki HTTP jest za drogi, zostaje CPU 70 procent i jawny zapis, że próg 20 żądań dojdzie, gdy metryka jest zbierana. Problem, który zamyka, to replika 2 na sztywno, nawet gdy nic się nie dzieje, oraz replika 1 na zawsze. Odpada HPA bez requestu: procent nie ma mianownika. Request jest w manifeście z numeru manifestów.

Na jednej maszynie druga replika nie ratuje padu węzła. Ratuje pad procesu i daje drugą kolejkę. To mówimy wprost. Nie obiecujemy drugiej strefy.

### Kogo nie skalujemy

Baza i Kafka mają jeden wolumen i jednego lidera w tym układzie. Dwa Pody Postgresa bez replikacji zepsują dane. Redis jako cache mógłby mieć drugą replikę bez sensu na jednym węźle. OpenBao z jednym unseal nie jest parą. Tunel ma jedno połączenie. Telemetria jest jeden kontener. Problem, który zamyka, to HPA skopiowane na wszystko. Odpada „skalujemy cały namespace”.

### PDB

`PodDisruptionBudget` z `minAvailable: 1` na bramie i na API, które ma replikę. Przy maksimum 2 i minimum 1 PDB nie blokuje dobrowolnego wysiedlenia jednego Poda, gdy drugi żyje. Gdy jest tylko jeden, wysiedlenie jest świadome. Problem, który zamyka, to `kubectl drain`, który kładzie jedyną bramę bez zastanowienia, gdy są dwie. Odpada PDB `minAvailable: 2` przy suficie 2: wtedy nie da się ruszyć nic.

### Konsumenci

Relay i projektor mogą mieć HPA do 2 tylko dlatego, że inbox i `push_delivery` oraz `SKIP LOCKED` już są. Bez nich ten numer nie podnosi repliki konsumenta. Sprawdzenie jest w teście, który już był: drugi raz ten sam `message-id` nie wysyła pusha. Ten numer nie wyłącza tego testu. Deployment projektora dostaje HPA tylko po tym przypomnieniu w opisie obiektu.

### Aspire

`aspire run` zostaje przy jednej replice. HPA nie dotyczy laptopa.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| HPA | zmiana liczby Podów w widełkach |
| request | mianownik procentu CPU |
| PDB | ile Podów ma zostać przy dobrowolnym wysiedleniu |
| in-flight | żądania obsłużone, jeszcze nie zakończone |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/k8s/base/hpa/` | HPA bramy, api, relay, projektor |
| `deploy/k8s/base/pdb/` | PDB bramy i api |
| `tests/` | istniejące testy inboxu, bez nowego podwajania pusha |

Lista Deploymentów z HPA: `gateway`, `learning-api`, `learning-relay`, `learning-projector` i odpowiedniki usług, które już są na klastrze: identity, news, cv, contact. Nie: postgres, kafka, redis, openbao, otel, cloudflared.

## Plan

### 1. Obiekt

`minReplicas: 1`, `maxReplicas: 2`, metryka CPU 70. Druga metryka tylko wtedy, gdy jest zbierana. Jeśli jej nie ma, w pliku jest komentarz, że próg 20 wchodzi z metryką, i obiekt ma jedną metrykę. Nie wpisujemy metryki, której klaster nie ma, bo HPA zostanie `Unknown` i nie skaluje wcale.

### 2. Obciążenie

Na kind albo na maszynie `kubectl scale` nie jest testem. Test: narzędzie z numeru wydajności puszczone mocniej, ręcznie, albo pętla `hey` jeśli licencja na to pozwala. `hey` jest na Apache-2.0. Wolno go użyć lokalnie. Nie jest paczką aplikacji. Obserwacja: `kubectl get hpa` pokazuje replikę 2, potem po ciszy wraca do 1. Jeśli na jednej maszynie CPU nie dojdzie do 70, checkpoint opisuje replikę 2 przez tymczasowe obniżenie progu w nakładce kind, nie na vm. Nakładka kind może mieć 1 procent, żeby zobaczyć ruch HPA. Overlay vm zostaje przy 70. To jest ważne: kind udowadnia, że obiekt działa, vm nie dostaje progu z ćwiczenia.

### 3. PDB

Osobne pliki przy HPA. `kubectl describe pdb` pokazuje `minAvailable` 1.

### Co jest do czego

| Obiekt | min | max |
| --- | --- | --- |
| HPA aplikacji | 1 | 2 |
| PDB bramy | 1 dostępny | nie dotyczy sufitu replik |
| baza | brak HPA | brak |

## Testy

Brak nowego faktu xUnit. Istniejący test drugiego dostarczenia pusha zostaje zielony. Checkpoint jest kubectl.

| Arrange | Act | Assert |
| --- | --- | --- |
| overlay kind z niskim progiem | obciążenie | `replicas` 2, po ciszy 1 |
| overlay vm | odczyt HPA | próg CPU 70, max 2 |

## Checkpoint

```bash
kubectl -n jjdevhub get hpa
kubectl -n jjdevhub get pdb
kubectl -n jjdevhub get hpa postgres 2>/dev/null && exit 1 || true
```

Nie ma HPA o nazwie postgres, kafka, redis, openbao. HPA bramy ma max 2. Po ćwiczeniu na kind replika wraca do 1.

## Umiesz wyjaśnić

Dlaczego maksimum to 2?

Bo jest jedna maszyna. Trzeci Pod nie daje drugiej strefy, a zjada CPU bazy. Sufit jest decyzją, nie brakiem wiedzy o większym HPA.

Dlaczego baza nie ma HPA?

Bo drugi Pod Postgresa bez replikacji i bez osobnego dysku na replikę zepsuje dane albo nie wstanie. Skalowanie bazy to inny projekt. Tu go nie ma.

Co daje PDB przy max 2?

Przy dwóch Podach wolno wysiedlić jeden. Nie wolno wysiedlić obu naraz dobrowolnie. Przy jednym Podzie i tak nie ma zapasu. PDB nie tworzy zapasu. HPA go tworzy, gdy jest powód.

Dlaczego drugi projektor nie podwaja skutków?

Bo inbox i `SKIP LOCKED` są wcześniej. HPA bez nich byłoby błędem. Z nimi druga replika jest kolejnym konsumentem grupy.

Czemu kind ma inny próg niż vm?

Bo na laptopie 70 procent może nie nastąpić i ćwiczenie nic nie pokaże. Próg ćwiczenia nie jedzie na maszynę. Overlay trzyma różnicę.

## Zasady

- Max 2, min 1 dla aplikacji.
- Brak HPA na bazie, Kafce, Redisie, OpenBao, telemetrii i tunelu.
- PDB `minAvailable` 1, nie 2.
- Próg kind nie jest progiem vm.
- Aspire zostaje przy jednej replice.
- Skalowanie konsumenta zakłada inbox.

## Poza zakresem

- Wiele węzłów i autoskalowanie węzłów nie jest tym numerem.
- Azure ma własny pulę węzłów w [08-azure.md](08-azure.md). HPA 1–2 zostaje, dopóki świadomie go nie zmienimy. Na Azure też startujemy od 2, nie od 20.
- Druga maszyna w domu nie powstaje.

## Źródła

- [Horizontal Pod Autoscaler](https://kubernetes.io/docs/concepts/workloads/autoscaling/horizontal-pod-autoscale/). Wiążące są min, max i metryka względem requestu. Dotyczy widełek 1–2 i 70 procent. Ze źródła nie bierzemy braku maksimum.
- [PodDisruptionBudget](https://kubernetes.io/docs/concepts/workloads/pods/disruptions/). Wiążące jest `minAvailable`. Dotyczy bramy. Ze źródła nie bierzemy `minAvailable` równego maksimum replik.
- Numer inboxu i numer pusha. Wiążące jest jedno zastosowanie wiadomości. Dotyczy zgody na drugą replikę konsumenta. Ze źródła nie bierzemy drugiej repliki bez tej tabeli.

## Gotowe gdy

- HPA aplikacji ma min 1 i max 2.
- Na kind widać przejście na 2 i powrót na 1 przy progu ćwiczeniowym.
- Overlay vm ma 70 procent, nie próg ćwiczenia.
- Nie ma HPA Postgresa, Kafki, Redisa, OpenBao i tunelu.
- PDB bramy ma `minAvailable` 1.
