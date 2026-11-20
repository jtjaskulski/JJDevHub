# 08. Wariant Azure

Poprzedni numer to [skalowanie](07-skalowanie.md). To jest ostatni numer.

Ten numer opisuje drugi katalog Terraform: AKS, ACR, PostgreSQL Flexible Server, Key Vault i Event Hubs z punktem końcowym Kafka. Nie jest celem domyślnym. Nie staje obok maszyny na tym samym tagu, dopóki ktoś świadomie nie zastosuje planu. Wejście z GitHub Actions do Azure jest przez OIDC, bez hasła aplikacji.

## Po co

Maszyna w domu ma sufit. Katalog chmury zamyka pytanie, jak ten sam obraz i ten sam manifest wyglądają, gdy węzeł nie jest VM w Proxmoxie. Osobny katalog zamyka to bez mieszania providera Azure do pliku, który opisuje dysk w domu. OIDC zamyka sekret chmury w GitHubie.

## Dlaczego ta technologia

### Ten sam Terraform

`deploy/terraform/azure`. Problem, który zamyka, to Bicep obok Terraforma. Odpada Bicep i odpada CloudFormation. Koszt: moduły Azure są inne niż Proxmox, język jest ten sam. Plan i stan tak samo nie wchodzą do git.

### Usługi

AKS do Podów. ACR do obrazów, obok GHCR albo zamiast, gdy ten wariant jest włączony. Jedno wydanie nie pcha do dwóch rejestrów bez powodu. Gdy stosujemy ten katalog, publish dostaje rejestr z zmiennej, nie na sztywno. Domyślny rejestr zostaje GHCR, dopóki zmienna nie wskaże ACR. PostgreSQL Flexible Server zamiast kontenera, jedna instancja, bazy te same nazwy. Event Hubs w warstwie, która ma punkt końcowy Kafka. Key Vault zamiast OpenBao tylko w tym wariancie. Problem, który zamyka, to przenoszenie OpenBao do Azure „bo tak”, gdy Key Vault jest usługą zarządzaną tego wariantu. Odpada Key Vault na maszynie w domu. Tam zostaje OpenBao.

Compaction: Event Hubs nie jest tym samym brokerem co `apache/kafka`. Temat kompaktowany `learning.integration` wymaga warstwy, która to umie, albo osobnej decyzji, że w tym wariancie kopia technologii jedzie inaczej. Numer nie udaje, że każdy SKU Event Hubs kompaktuje. W pliku jest zmienna warstwy i komentarz, że bez kompaktowania ten wariant nie jest zgodny z CV. `terraform plan` nie jest „gotowe”, jeśli warstwa nie spełnia tego warunku. Nie wybieramy SKU z pamięci, jeśli cennik się zmienił. Wybór jest w `terraform.tfvars.example` jako nazwa, którą trzeba sprawdzić w dokumentacji w dniu apply.

### Tożsamość

Aplikacja na AKS używa tożsamości zarządzanej do Key Vault i do ACR. Nie ma hasła rejestru w GitHubie. Job, jeśli ma wgrać do AKS, używa OIDC: federacja podmiotu `repo:ORG/REPO:environment:azure`. Problem, który zamyka, to client secret w sekretach repozytorium. Odpada klucz JSON konta usługi w git. Koszt: trzeba założyć federację. Jest w tym katalogu.

Środowisko GitHub `azure` jest wymagane do tego joba. Zwykły deploy na k3s go nie używa. Problem, który zamyka, to jeden job, który przez pomyłkę wgra tag i do domu, i do Azure. Osobny workflow `deploy-azure.yml` uruchamiany `workflow_dispatch`, nie przy każdym tagu. Domyślny `deploy.yml` nie dostaje kubeconfig AKS.

### Skala

HPA zostaje 1–2, dopóki ktoś nie zmieni liczb świadomie. Pula węzłów startuje od jednej. Problem, który zamyka, to katalog, który od razu stawia trzy strefy i drogi SKU „bo produkcja”. Odpada cicha produkcja. Ten katalog jest wariantem do apply, nie dowodem, że ruch tam idzie.

### Czego nie ma

Nie ma drugiego produktu. Te same obrazy, te same migracje, ten sam YARP. Ingress AKS nie zastępuje YARP. Jest Service wewnętrzny i, jeśli potrzeba wejścia, jeden punkt do bramy, nie do każdej usługi.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| wariant | katalog, który nie jest celem domyślnym |
| OIDC | federacja GitHuba z Entra ID, bez sekretu klienta |
| tożsamość zarządzana | konto AKS do Key Vault bez hasła w env |
| SKU | warstwa usługi, od której zależy compaction |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/terraform/azure/` | AKS, ACR, Postgres, Key Vault, Event Hubs |
| `.github/workflows/deploy-azure.yml` | tylko `workflow_dispatch`, OIDC |
| `deploy/terraform/azure/terraform.tfvars.example` | puste identyfikatory, nazwa warstwy do sprawdzenia |

Stan w tym samym reżimie co Proxmox: poza gitem.

## Plan

### 1. Walidacja

`terraform fmt -check` i `validate`, jeśli da się bez subskrypcji. Bez apply w CI.

### 2. OIDC

W pliku jest `azure/login` albo oficjalny krok logowania z `client-id`, `tenant-id`, `subscription-id` jako zmienne, nie jako sekret hasła. Uprawnienie joba: `id-token: write`. Środowisko `azure`.

### 3. Zgodność z CV

W README katalogu jest zdanie: bez kompaktowania na magistrali ten wariant nie przenosi `learning.integration`. Apply bez tej warstwy nie jest zakończeniem numeru.

### Co jest do czego

| Usługa Azure | Odpowiednik w domu |
| --- | --- |
| AKS | k3s |
| ACR | GHCR, gdy wariant włączony |
| Flexible Server | Postgres na hoście lub w klastrze |
| Key Vault | OpenBao |
| Event Hubs | `apache/kafka` |

## Testy

`fmt -check`. Nie ma testu, który tworzy subskrypcję.

| Arrange | Act | Assert |
| --- | --- | --- |
| katalog azure | `terraform fmt -check` | kod 0 |
| `deploy.yml` | przegląd | brak referencji do AKS |

## Checkpoint

```bash
terraform -chdir=deploy/terraform/azure fmt -check
grep -n aks .github/workflows/deploy.yml && exit 1 || true
```

Domyślny deploy nie wspomina AKS. Workflow azure istnieje i ma `workflow_dispatch`. W example nie ma sekretu. `deploy.yml` maszyny nadal działa.

## Umiesz wyjaśnić

Dlaczego to nie jest cel domyślny?

Bo produkt działa na jednej maszynie. Katalog chmury jest opisem, jak przenieść ten sam kształt, nie drugim miejscem, do którego każdy tag jedzie.

Dlaczego OIDC, a nie hasło aplikacji?

Bo hasło w sekretach GitHuba da się skopiować i nie wygasa z przebiegiem. Federacja daje token na czas joba i tylko z tego środowiska.

Czemu Key Vault tylko tutaj?

Bo w domu licencja i układ są pod OpenBao. W Azure zarządzany magazyn jest usługą platformy. Nie ciągniemy OpenBao do AKS tylko po to, żeby był ten sam plik binarny, i nie ciągniemy Key Vault do Proxmoxa.

Dlaczego Event Hubs jest warunkiem, a nie ozdobą?

Bo CV czyta temat kompaktowany. Magistrala bez kompaktowania zmieni produkt. Dlatego warstwa jest warunkiem „gotowe”, a nie dowolnym SKU.

Czemu osobny workflow?

Bo dopisanie AKS do joba, który wgrywa na k3s, wgra tag w dwa miejsca. Domyślna ścieżka zostaje domem. Azure jest dispatch.

## Zasady

- Brak apply w pull requeście.
- Brak client secret w git i w `deploy.yml` domu.
- Domyślny deploy nie celuje w AKS.
- HPA w tym wariancie startuje od widełek 1–2.
- YARP zostaje jedyną bramą aplikacji.
- Bez kompaktowania na magistrali numer nie jest zakończony apply.
- Stan Terraform nie jest w git.

## Poza zakresem

- Nie ma kolejnego numeru po tym. Zmiana produktu wraca do numeru, który dany element wprowadził.
- Nie ma drugiej aplikacji „JJDevHub Azure”.
- Nie ma przeprowadzki danych z domu jako automatu. Przeniesienie zrzutu, jeśli kiedykolwiek, jest operacją poza tym plikiem i nie jest cichym krokiem apply.

## Źródła

- [Terraform AzureRM](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs). Wiążące są zasoby AKS, ACR, Flexible Server, Key Vault i Event Hubs. Dotyczy katalogu wariantu. Ze źródła nie bierzemy Bicep jako drugiego opisu.
- [OIDC z GitHub do Entra](https://learn.microsoft.com/entra/workload-id/workload-identity-federation). Wiążące jest federacyjne poświadczenie bez sekretu klienta. Dotyczy `deploy-azure.yml`. Ze źródła nie bierzemy hasła aplikacji w sekretach.
- [Event Hubs Kafka](https://learn.microsoft.com/azure/event-hubs/azure-event-hubs-kafka-overview). Wiążące jest to, że zgodność z Kafką zależy od warstwy i nie obejmuje każdego zachowania brokera. Dotyczy warunku kompaktowania. Ze źródła nie bierzemy założenia, że każdy namespace kompaktuje.
- Numer skalowania. Wiążące są widełki 1–2. Dotyczy HPA także tutaj, dopóki nie zostaną zmienione osobno. Ze źródła nie bierzemy puli dziesięciu węzłów w example.

## Gotowe gdy

- `terraform fmt -check` w katalogu azure zwraca zero.
- `deploy.yml` domu nie wskazuje AKS.
- `deploy-azure.yml` jest tylko `workflow_dispatch` i używa OIDC.
- W example nie ma sekretu.
- README wariantu mówi, że bez kompaktowania nie ma zgodności z CV.
- Domyślna maszyna nadal obsługuje `/versions`.
