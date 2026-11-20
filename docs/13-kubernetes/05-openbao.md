# 05. OpenBao

Poprzedni numer to [deploy na k3s](04-deploy-k3s.md). Następny to [Redis](06-redis.md).

Ten numer stawia OpenBao na klastrze. Pod aplikacji dostaje poświadczenie bazy na godzinę przez metodę Kubernetes. Plik env z hasłem bazy przestaje być jedynym źródłem. Klucz rozszczelnienia jest w vTPM przez `systemd-creds` na hoście, nie w git. Identity nadal wydaje tokeny ludzi. OpenBao nie jest katalogiem użytkowników.

## Po co

Hasło w `/etc/jjdevhub/compose.env` żyje tak długo jak plik i widzi je każdy, kto czyta env procesu. Krótki lease zamyka okno. Osobna rola na bazę zamyka to, że News przeczyta sekret Learning. vTPM zamyka klucz, którym OpenBao wstaje po restarcie, bez trzymania go w repozytorium.

## Dlaczego ta technologia

### OpenBao, nie Vault

OpenBao jest na MPL-2.0. Vault jest na BSL. Problem, który zamyka, to magazyn sekretów spoza listy licencji. Odpada Keycloak jako magazyn haseł bazy: Keycloak jest katalogiem ludzi, a ludzi wydaje Identity. Koszt: trzeba postawić serwer i politykę. To jest ten numer.

### Metoda Kubernetes

Pod ma ServiceAccount. OpenBao wydaje sekret tylko tej roli, na czas godziny. `learning` nie czyta ścieżki `identity`. Problem, który zamyka, to jeden token w Secret klastra na wszystkie bazy. Odpada statyczny Secret z hasłem jako docelowy stan. Na czas przełączenia Secret może jeszcze być, aż Pod dostanie env z kontenera init. Koniec numeru: Deployment nie ma hasła bazy w Secretcie klastra. Ma rolę. Koszt: awaria OpenBao blokuje start nowego Poda. Stary Pod działa do końca lease. To jest do przyjęcia. Live nie zależy od OpenBao w trakcie żądania, jeśli hasło jest już w procesie. Ready może sprawdzać, że da się odnowić, albo nie. Ten numer nie wiesza live na OpenBao.

### Init albo proces odnawiający

Kontener init woła OpenBao i zapisuje plik, który proces czyta, albo osobny proces w Podzie odnawia lease. Problem, który zamyka, to biblioteka OpenBao w każdej domenie. Odpada `ISecretReader` w `Article`. Port `ISecretReader` wolno mieć w infrastrukturze, jeśli proces sam czyta. Prostsze: init wstrzykuje env, aplikacja nadal czyta connection string ze zmiennej, tylko zmienna nie jest w manifeście. Kod aplikacji się nie zmienia. Koszt: mniej „portu” w kodzie. Zysk: domena nie wie o OpenBao. Wybieramy init albo proces odnawiający w tym samym Podzie. Nie dokładamy klienta OpenBao do Learning.Domain.

### Rozszczelnienie

Klucz unseal jest w `systemd-creds` powiązanym z vTPM. Procedura startu OpenBao na hoście albo w Podzie, który ma dostęp do tego mechanizmu, jest w `deploy/openbao/README.md` bez wartości klucza. Problem, który zamyka, to klucz unseal w Secretcie Kubernetesa, który jest tylko base64. Odpada klucz w git. Koszt: utrata TPM jest utratą możliwości unseal. Kopia procedury odzyskiwania jest poza repozytorium, obok kopii stanu Terraform. Numer o tym mówi i nie wkleja shardów.

### Dev

AppHost w Development może zostać przy haśle z user secrets. Nie wymagamy OpenBao na laptopie, żeby `aspire run` wstało. Na kind wolno postawić OpenBao w trybie dev, bez TPM, żeby zobaczyć metodę, i nie jest to układ maszyny. Maszyna nie używa trybu dev.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| lease | sekret ważny godzinę |
| unseal | otwarcie OpenBao po restarcie |
| rola | która ścieżka sekretu dla którego ServiceAccount |
| init | kontener, który wstawia env, zanim wstanie proces |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/openbao/policy/` | HCL ról, bez wartości |
| `deploy/k8s/base/openbao/` | Deployment OpenBao, jeśli jedzie jako Pod |
| `deploy/openbao/README.md` | unseal i to, czego nie ma w git |
| `deploy/k8s/base/apps/learning/` | init bez hasła w YAML |

## Plan

### 1. Polityki

Rola `learning` czyta `database/learning`. Rola `identity` czyta `database/identity`. Test polityki, nawet ręczny: token Learning na ścieżce Identity dostaje odmowę.

### 2. Init

Manifest init nie zawiera `value` hasła. Zawiera adres OpenBao i rolę. Adres jest wewnętrzny. Nie ma Ingress.

### 3. Wycięcie hasła z Secretu aplikacji

Po przejściu Poda na init Secret `jjdevhub-env` nie ma klucza hasła bazy albo nie jest montowany do aplikacji. Sprawdzenie: `kubectl set env` nie pokazuje hasła w specyfikacji Deploymentu. `kubectl describe` nie jest miejscem, z którego kopiujemy hasło do git.

### Co jest do czego

| Element | Kto |
| --- | --- |
| polityka | OpenBao |
| init | Pod aplikacji |
| TPM | host, unseal |
| Identity | ludzie, nie hasła bazy |

## Testy

Na kind w trybie dev: zalogowany token roli Learning nie czyta ścieżki Identity. To może być skrypt `deploy/openbao/smoke.sh`, nie xUnit.

| Arrange | Act | Assert |
| --- | --- | --- |
| token roli Learning | odczyt ścieżki Identity | odmowa |

## Checkpoint

```bash
kubectl -n jjdevhub get deploy learning-api -o yaml | grep -n password && exit 1 || true
```

Polecenie nie znajduje hasła w manifeście Deploymentu. OpenBao jest unseal po restarcie Poda OpenBao, jeśli jedzie na klastrze, albo po restarcie usługi na hoście. Procedura w README nie zawiera wartości.

Nowy Pod Learning łączy się z bazą. Stary plik env nie jest już w `envFrom` tego Deploymentu.

## Umiesz wyjaśnić

Dlaczego nie Vault?

Bo licencja BSL nie jest na liście. OpenBao jest na MPL i starcza jako magazyn.

Dlaczego domena nie woła OpenBao?

Bo sekret połączenia jest sprawą procesu, nie reguły artykułu. Init wstrzykuje zmienną. Test domeny zostaje bez sieci.

Czym rola różni się od jednego tokenu w klastrze?

Jeden token czyta wszystko. Rola Learning nie przejdzie na ścieżkę Identity. Wyciek tokenu Poda Learning nie oddaje klucza podpisu Identity, jeśli ten klucz też jest w OpenBao pod inną ścieżką.

Dlaczego unseal nie leży w Secretcie Kubernetesa?

Bo Secret to base64. Kto czyta etcd albo manifest, ma klucz. TPM wiąże klucz z maszyną.

Co z laptopem?

Zostaje przy user secrets. Nie ciągniemy trybu dev OpenBao jako warunku `aspire run`.

## Zasady

- Wartość sekretu nie wchodzi do git.
- Domena nie referencjonuje klienta OpenBao.
- Rola jednej usługi nie czyta ścieżki drugiej.
- Live nie woła OpenBao.
- Tryb dev nie jest układem maszyny.
- Identity nie jest zastępowana przez OpenBao.

## Poza zakresem

- Redis jest w [06-redis.md](06-redis.md).
- Klucz FCM i klucz asystenta przenoszą się do OpenBao tym samym mechanizmem, gdy Deployment tych procesów dostaje init. Nie trzeba osobnego numeru, jeśli polityka jest ta sama. Ten numer obejmuje też te ścieżki, skoro sekret już jest na maszynie. Jedna rola `contact` czyta FCM. Jedna rola `learning` czyta też klucz asystenta, albo osobna ścieżka w tej samej roli. Nie czytają ich inne usługi.

## Źródła

- [OpenBao](https://openbao.org/docs/what-is-openbao/). Wiążące są polityki i czas życia tokenu. Dotyczy ról. Ze źródła nie bierzemy jednego roota w Podzie aplikacji.
- [Kubernetes auth](https://openbao.org/docs/auth/kubernetes/). Wiążące jest logowanie ServiceAccount. Dotyczy init. Ze źródła nie bierzemy tokenu w manifeście.
- [MPL 2.0](https://www.mozilla.org/en-US/MPL/2.0/). Wiążąca jest licencja serwera. Dotyczy wyboru zamiast Vault. Ze źródła nie bierzemy BSL jako wyjątku.

## Gotowe gdy

- Deployment aplikacji nie zawiera hasła bazy.
- Token Learning nie czyta sekretu Identity.
- W git nie ma klucza unseal.
- `aspire run` działa bez OpenBao.
- Nowy Pod łączy się z własną bazą.
