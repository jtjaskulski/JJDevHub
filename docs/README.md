# JJDevHub

Ten folder jest specyfikacją JJDevHub. Kod usług, workflow i ekrany powstają stąd, jeden numer naraz. Ścieżki w numerach są względem korzenia repozytorium, w którym ta specyfikacja leży obok `src/`, `deploy/` i `.github/`.

Każdy plik jest po polsku. Numer opisuje system i kroki wdrożenia. Nie opisuje narzędzi, które ten plik napisały.

Słownik nazw jest w [00-fundamenty/slownik.md](00-fundamenty/slownik.md). Decyzje technologiczne są w [00-fundamenty/technologie.md](00-fundamenty/technologie.md). Obraz działającego systemu jest w [00-fundamenty/architektura.md](00-fundamenty/architektura.md). Sposób pracy jest w [00-fundamenty/zasady-pracy.md](00-fundamenty/zasady-pracy.md). Szablon sekcji numeru jest w [00-fundamenty/szablon-numeru.md](00-fundamenty/szablon-numeru.md). Odhaczenie zamkniętego numeru jest w [00-fundamenty/postep.md](00-fundamenty/postep.md).

## Jak czytać

Najpierw cały folder `00-fundamenty`. Potem foldery po kolei, od `01-bramki` do `13-kubernetes`. W folderze pliki czyta się po numerze. Jeden numer na jedno wdrożenie. Nie łączy się dwóch numerów w jednym pull requeście.

Foldery są ułożone w kolejności pracy. Nie trzeba przeskakiwać między nimi, żeby złożyć kolejkę.

| Folder | Numery | Co zamyka |
| --- | --- | --- |
| [01-bramki](01-bramki/01-solucja.md) | `01`–`09` | Repozytorium odrzuca złą gałąź, czerwony test, sekret i znaną podatność, zanim powstanie usługa |
| [02-szkielet-i-deploy](02-szkielet-i-deploy/01-aspire.md) | `01`–`08` | Pusta brama stoi na maszynie, a ten sam job wgra później każdy tag |
| [03-learning-zapis](03-learning-zapis/01-kernel-i-domena.md) | `01`–`07` | Learning zapisuje kurs i lekcję w swojej bazie |
| [04-zdarzenia](04-zdarzenia/01-kontrakty.md) | `01`–`09` | Ta sama usługa publikuje fakt i składa z niego bazę odczytu |
| [05-tozsamosc](05-tozsamosc/01-konta-i-zaproszenia.md) | `01`–`06` | Konta, token, audyt i saga rejestracji |
| [06-news](06-news/01-agregat-i-zdarzenie.md) | `01`–`04` | Notatka ze strumieniem zdarzeń |
| [07-cv](07-cv/01-sekcje.md) | `01`–`03` | CV i kopia technologii |
| [08-contact](08-contact/01-dwa-zapytania.md) | `01`–`03` | Dwa zapytania, skrzynki i push |
| [09-rozszerzenia-api](09-rozszerzenia-api/01-graphql.md) | `01`–`02` | GraphQL katalogu i asystent kursu |
| [10-testy](10-testy/01-integracyjne.md) | `01`–`03` | Domknięcie piramidy |
| [11-web](11-web/01-motyw.md) | `01`–`07` | Przeglądarka |
| [12-mobile](12-mobile/01-szkielet-expo.md) | `01`–`04` | Telefon |
| [13-kubernetes](13-kubernetes/01-kind.md) | `01`–`08` | k3s, Terraform, sekrety, cache, drugi pod, wariant Azure |

Numer jest gotowy, gdy jego sekcja `## Gotowe gdy` jest spełniona, checkpoint przechodzi i w [postep.md](00-fundamenty/postep.md) jest data. Następny numer nie startuje wcześniej.

## Kolejność

1. [01-bramki/01-solucja.md](01-bramki/01-solucja.md) — pusta solucja, `global.json`, centralne wersje pakietów.
2. [01-bramki/02-galezie.md](01-bramki/02-galezie.md) — gałęzie, tytuł, limit rozmiaru pull requestu.
3. [01-bramki/03-build-testy-format.md](01-bramki/03-build-testy-format.md) — build, testy, format.
4. [01-bramki/04-codeql.md](01-bramki/04-codeql.md) — skan kodu.
5. [01-bramki/05-zaleznosci.md](01-bramki/05-zaleznosci.md) — podatności pakietów i Dependabot.
6. [01-bramki/06-sonarqube.md](01-bramki/06-sonarqube.md) — bramka jakości.
7. [01-bramki/07-lancuch-dostaw.md](01-bramki/07-lancuch-dostaw.md) — sekrety, akcje, Scorecard.
8. [01-bramki/08-obrazy.md](01-bramki/08-obrazy.md) — skan obrazu, SBOM, atestacja.
9. [01-bramki/09-wersje.md](01-bramki/09-wersje.md) — osobny numer każdego komponentu.
10. [02-szkielet-i-deploy/01-aspire.md](02-szkielet-i-deploy/01-aspire.md) — AppHost i `ServiceDefaults`.
11. [02-szkielet-i-deploy/02-gateway.md](02-szkielet-i-deploy/02-gateway.md) — YARP i `GET /versions`.
12. [02-szkielet-i-deploy/03-dockerfile.md](02-szkielet-i-deploy/03-dockerfile.md) — jeden Dockerfile.
13. [02-szkielet-i-deploy/04-maszyna.md](02-szkielet-i-deploy/04-maszyna.md) — VM i runner.
14. [02-szkielet-i-deploy/05-cloudflare.md](02-szkielet-i-deploy/05-cloudflare.md) — DNS i tunel.
15. [02-szkielet-i-deploy/06-compose.md](02-szkielet-i-deploy/06-compose.md) — compose z AppHosta.
16. [02-szkielet-i-deploy/07-deploy.md](02-szkielet-i-deploy/07-deploy.md) — pierwszy rollout i rollback.
17. [02-szkielet-i-deploy/08-telemetria.md](02-szkielet-i-deploy/08-telemetria.md) — `otel-lgtm` na maszynie.
18. [03-learning-zapis/01-kernel-i-domena.md](03-learning-zapis/01-kernel-i-domena.md) — `Result` i model Learning.
19. [03-learning-zapis/02-use-case-i-porty.md](03-learning-zapis/02-use-case-i-porty.md) — handlery i porty.
20. [03-learning-zapis/03-postgres.md](03-learning-zapis/03-postgres.md) — serwer i dziesięć baz.
21. [03-learning-zapis/04-ef-core.md](03-learning-zapis/04-ef-core.md) — zapis Learning.
22. [03-learning-zapis/05-rest.md](03-learning-zapis/05-rest.md) — HTTP, błędy, `ETag`, idempotencja.
23. [03-learning-zapis/06-kurs-i-lekcja.md](03-learning-zapis/06-kurs-i-lekcja.md) — kurs, lekcja, postęp.
24. [03-learning-zapis/07-ef-zaawansowany.md](03-learning-zapis/07-ef-zaawansowany.md) — interceptory, konwertery, dziedziczenie.
25. [04-zdarzenia/01-kontrakty.md](04-zdarzenia/01-kontrakty.md) — rekordy i nazwy faktów.
26. [04-zdarzenia/02-outbox.md](04-zdarzenia/02-outbox.md) — outbox w transakcji zapisu.
27. [04-zdarzenia/03-kafka.md](04-zdarzenia/03-kafka.md) — broker KRaft i topiki.
28. [04-zdarzenia/04-relay.md](04-zdarzenia/04-relay.md) — publikacja z outboxa.
29. [04-zdarzenia/05-projektor-i-inbox.md](04-zdarzenia/05-projektor-i-inbox.md) — konsument i inbox.
30. [04-zdarzenia/06-odczyt-dapper.md](04-zdarzenia/06-odczyt-dapper.md) — zapytania do bazy odczytu.
31. [04-zdarzenia/07-ewolucja-schematu.md](04-zdarzenia/07-ewolucja-schematu.md) — `v2` i upcaster.
32. [04-zdarzenia/08-slady-i-metryki.md](04-zdarzenia/08-slady-i-metryki.md) — ślad przez Kafkę.
33. [04-zdarzenia/09-health-i-odpornosc.md](04-zdarzenia/09-health-i-odpornosc.md) — probe i circuit breaker.
34. [05-tozsamosc/01-konta-i-zaproszenia.md](05-tozsamosc/01-konta-i-zaproszenia.md) — `Account` i `Invite`.
35. [05-tozsamosc/02-openiddict.md](05-tozsamosc/02-openiddict.md) — authorization code i PKCE.
36. [05-tozsamosc/03-jwt-i-policies.md](05-tozsamosc/03-jwt-i-policies.md) — walidacja w usługach i w bramie.
37. [05-tozsamosc/04-audyt.md](05-tozsamosc/04-audyt.md) — hash chain.
38. [05-tozsamosc/05-saga-rejestracji.md](05-tozsamosc/05-saga-rejestracji.md) — rejestracja z zaproszenia.
39. [05-tozsamosc/06-twardnienie-api.md](05-tozsamosc/06-twardnienie-api.md) — limit, CORS, nagłówki.
40. [06-news/01-agregat-i-zdarzenie.md](06-news/01-agregat-i-zdarzenie.md) — agregat `Article`.
41. [06-news/02-strumien.md](06-news/02-strumien.md) — magazyn strumienia.
42. [06-news/03-zapis.md](06-news/03-zapis.md) — komendy notatki.
43. [06-news/04-odczyt-markdown.md](06-news/04-odczyt-markdown.md) — projekcja i sanitacja.
44. [07-cv/01-sekcje.md](07-cv/01-sekcje.md) — sekcje CV jako strumienie.
45. [07-cv/02-kopia-technologii.md](07-cv/02-kopia-technologii.md) — temat kompaktowany.
46. [07-cv/03-strona-publiczna.md](07-cv/03-strona-publiczna.md) — strona z jednego odczytu.
47. [08-contact/01-dwa-zapytania.md](08-contact/01-dwa-zapytania.md) — student i rekruter.
48. [08-contact/02-skrzynki-sse.md](08-contact/02-skrzynki-sse.md) — listy i strumień zdarzeń.
49. [08-contact/03-push.md](08-contact/03-push.md) — dwa kanały na telefon właściciela.
50. [09-rozszerzenia-api/01-graphql.md](09-rozszerzenia-api/01-graphql.md) — katalog przez Hot Chocolate.
51. [09-rozszerzenia-api/02-asystent.md](09-rozszerzenia-api/02-asystent.md) — pytania o kurs.
52. [10-testy/01-integracyjne.md](10-testy/01-integracyjne.md) — Testcontainers i AppHost.
53. [10-testy/02-kontraktowe.md](10-testy/02-kontraktowe.md) — Pact między Cv a Learning.
54. [10-testy/03-wydajnosciowe.md](10-testy/03-wydajnosciowe.md) — próg opóźnienia.
55. [11-web/01-motyw.md](11-web/01-motyw.md) — liczby wyglądu.
56. [11-web/02-szkielet.md](11-web/02-szkielet.md) — trasy i stopka wersji.
57. [11-web/03-klient-openapi.md](11-web/03-klient-openapi.md) — klient o typie ścieżki.
58. [11-web/04-logowanie-pkce.md](11-web/04-logowanie-pkce.md) — przeglądarka jako klient publiczny.
59. [11-web/05-tresc-publiczna.md](11-web/05-tresc-publiczna.md) — CV, kursy, notatki, formularz.
60. [11-web/06-intranet.md](11-web/06-intranet.md) — edycja właściciela.
61. [11-web/07-playwright.md](11-web/07-playwright.md) — ścieżka w przeglądarce.
62. [12-mobile/01-szkielet-expo.md](12-mobile/01-szkielet-expo.md) — Expo i te same liczby.
63. [12-mobile/02-logowanie.md](12-mobile/02-logowanie.md) — PKCE na telefonie.
64. [12-mobile/03-ekrany.md](12-mobile/03-ekrany.md) — ekrany i wersja.
65. [12-mobile/04-powiadomienia.md](12-mobile/04-powiadomienia.md) — token urządzenia.
66. [13-kubernetes/01-kind.md](13-kubernetes/01-kind.md) — lokalny klaster.
67. [13-kubernetes/02-manifesty.md](13-kubernetes/02-manifesty.md) — Deployment, StatefulSet, Kustomize.
68. [13-kubernetes/03-terraform-maszyna.md](13-kubernetes/03-terraform-maszyna.md) — VM i DNS opisane kodem.
69. [13-kubernetes/04-deploy-k3s.md](13-kubernetes/04-deploy-k3s.md) — ten sam tag na k3s.
70. [13-kubernetes/05-openbao.md](13-kubernetes/05-openbao.md) — sekrety poza plikiem env.
71. [13-kubernetes/06-redis.md](13-kubernetes/06-redis.md) — cache odczytu.
72. [13-kubernetes/07-skalowanie.md](13-kubernetes/07-skalowanie.md) — drugi pod.
73. [13-kubernetes/08-azure.md](13-kubernetes/08-azure.md) — drugi wariant Terraforma.

## Czego w tych plikach nie ma

- Wartości sekretu, tokenu, hasła i connection stringa z hasłem.
- Drugiej nazwy na byt, który jest już w słowniku.
- Opisu chmury publicznej jako jedynego celu. Cel domyślny to jedna maszyna. Azure jest wariantem w ostatnim numerze.
- Osobnej usługi powiadomień. Push jest adapterem Contact.

Gdy dwa pliki mówią co innego o tej samej nazwie, wygrywa słownik. Oba pliki poprawia się w tym samym numerze, który rozjazd znalazł.
