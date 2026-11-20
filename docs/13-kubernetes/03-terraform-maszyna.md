# 03. Terraform maszyny i DNS

Poprzedni numer to [manifesty](02-manifesty.md). Następny to [deploy na k3s](04-deploy-k3s.md).

Ten numer opisuje VM i rekord tunelu Terraformem. Parametry są te same co przy ręcznej maszynie: 4 vCPU, 16 GiB, 120 GiB, UEFI, vTPM. Klikany panel przestaje być źródłem. Drugi katalog, Azure, nie powstaje w tym numerze.

## Po co

Maszyna opisana w README rozjedzie się z tym, co stoi, gdy ktoś dołoży dysk w panelu. Terraform zamyka opis i pokazuje zmianę w planie, zanim cokolwiek ruszy. Tunel opisany w panelu Cloudflare ma ten sam problem: origin da się przepiąć kliknięciem na inną usługę.

## Dlaczego ta technologia

### Terraform, nie Bicep

Jeden język na maszynę w domu i na wariant Azure. Problem, który zamyka, to Bicep dla Azure i osobny opis Proxmoxa. Odpada Bicep jako drugi język, nawet jeśli wariant chmury jest w Azure. Koszt: provider Proxmoxa jest osobny od providera Azure. Oba są Terraformem.

### Provider Proxmox

Provider `bpg/proxmox` opisuje VM. Stan Terraform jest poza gitem, w miejscu wskazanym w README, nie w repozytorium: lokalny plik stanu zawiera identyfikatory i bywa zawierał sekrety. Problem, który zamyka, to stan w git. Odpada commit `terraform.tfstate`. Koszt: kopia stanu jest obowiązkiem, tak jak kopia dysku. README mówi, gdzie stan leży, bez hasła.

Hasło API Proxmoxa jest w zmiennej środowiskowej `TF_VAR_proxmox_password`, nie w `*.tf`. Token Cloudflare jest `TF_VAR_cloudflare_api_token`. Plik `terraform.tfvars` jest w `.gitignore`. Przykład `terraform.tfvars.example` ma puste wartości.

### Parametry, których nie ruszamy po cichu

`cores` 4, `memory` 16384, dysk 120, `bios` ovmf, TPM włączone, balonowanie wyłączone. Problem, który zamyka, to „mała zmiana” pamięci w planie, która restartuje gościa. Plan, który niszczy VM, nie jest stosowany bez świadomej decyzji. Ten numer nie każe niszczyć istniejącej maszyny, jeśli już działa: import istniejącego gościa albo opis, który odtwarza te same parametry. `terraform plan` ma pokazać brak zmian albo zmianę, którą da się nazwać. Nie stosujemy planu z `forces replacement` na dysku danych bez osobnego zdania w commicie.

### Cloudflare

Rekord CNAME na tunel, proxied, bez rekordu A na adres domu. Ingress tunelu: hostname aplikacji na `http://127.0.0.1:8080` dopóki k3s nie przejmie Service; po k3s origin zostaje tym, co nasłuchuje na tym porcie na hoście, czyli nadal brama, przez `hostPort` albo przez forward z węzła. Ten numer nie zmienia originu na panel. Origin to brama.

### k3s w cloud-init

Cloud-init instaluje k3s bez Traefika i bez ServiceLB: flagi `--disable=traefik` i `--disable=servicelb`. Problem, który zamyka, to k3s, który sam wystawia LoadBalancer obok YARP. Odpada domyślna instalacja. Koszt: trzeba podać flagi. Są w pliku `deploy/terraform/proxmox/cloud-init.yaml` bez sekretów. Do czasu przełączenia deployu k3s może stać obok Dockera. Compose nadal obsługuje ruch, dopóki numer deployu nie przełączy joba. Ten numer instaluje k3s. Nie wyłącza compose. Wyłączenie compose jest w następnym numerze, gdy `kubectl` przejmie wydanie.

Jeśli cloud-init na istniejącej VM nie przebiegnie drugi raz, instalacja k3s jest też skryptem `deploy/machine/install-k3s.sh` odpalanym raz, a Terraform i tak opisuje VM. Skrypt jest w git, bez tokenu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| plan | diff infrastruktury przed zastosowaniem |
| stan | plik Terraform poza gitem |
| cloud-init | pierwszy start gościa |
| `forces replacement` | zmiana, która niszczy i tworzy zasób od nowa |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/terraform/proxmox/` | VM |
| `deploy/terraform/cloudflare/` | rekord i tunel |
| `deploy/terraform/proxmox/cloud-init.yaml` | k3s bez Traefika |
| `deploy/terraform/*.tfvars.example` | puste zmienne |
| `.gitignore` | `*.tfstate`, `*.tfvars`, `.terraform/` |

## Plan

### 1. Opis VM

Zasób VM z parametrami z numeru maszyny. `terraform plan` na istniejącym gościu jest przejrzany. Zastosowanie, które nie niszczy dysku, wolno zrobić. Zastosowanie, które niszczy, nie jest częścią „gotowe”.

### 2. DNS

Zasób rekordu i konfiguracja tunelu. `terraform plan` nie dodaje rekordu A.

### 3. Sprawdzenie k3s

Na gościu `k3s kubectl get node` pokazuje jeden węzeł Ready. `kubectl get svc -A` nie pokazuje Traefika jako bramy produktu. Compose bramy nadal odpowiada na `/versions`, dopóki następny numer nie przełączy ruchu.

### Co jest do czego

| Katalog | Czego nie robi |
| --- | --- |
| `proxmox` | nie tworzy AKS |
| `cloudflare` | nie publikuje Postgresa |
| cloud-init | nie wkłada hasła bazy |

## Testy

`terraform fmt -check` i `terraform validate` w obu katalogach. Nie ma apply w CI na pull requeście. Apply jest ręczne, z planu przeczytanego przez człowieka. Job CI może wołać `fmt` i `validate` bez tokenu, jeśli provider na to pozwala offline. Jeśli validate wymaga tokenu, job robi tylko `fmt` i nie woła API. Check nazywa się `terraform-fmt`, plik to `.github/workflows/terraform-fmt.yml`, i wchodzi do rulesetów. Gdy katalogu nie ma, job loguje `no material`. Od tego numeru katalog jest.

| Arrange | Act | Assert |
| --- | --- | --- |
| katalogi Terraform | `terraform fmt -check` | kod 0 |

## Checkpoint

```bash
terraform -chdir=deploy/terraform/proxmox fmt -check
terraform -chdir=deploy/terraform/cloudflare fmt -check
```

Na maszynie, po świadomym apply: węzeł k3s Ready, brak rekordu A, `/versions` przez tunel nadal działa na compose. `git status` nie pokazuje `tfstate` ani `tfvars`.

## Umiesz wyjaśnić

Dlaczego stan nie jest w git?

Bo trzyma identyfikatory i często sekrety providera w cache obok. Repozytorium ma kod. Stan jest kopią operacyjną poza git.

Czemu plan z wymianą dysku nie jest „gotowe”?

Bo wymiana dysku kasuje bazy. Plan ma być nudny: brak zmian albo zmiana, która nie jest `forces replacement` na dysku.

Dlaczego k3s bez Traefika?

Bo Traefik zostałby drugą bramą obok YARP. Flaga instalacji to zamyka. Dopisanie Traefika później wymaga świadomej zmiany, nie domyślnego instalatora.

Dlaczego compose jeszcze żyje?

Bo przełączenie ruchu i przełączenie joba deployu to następny numer. Dwa przełączenia naraz zostawiają okno bez działającej bramy. Najpierw klaster stoi, ruch zostaje.

Co z sekretem w cloud-init?

Nie ma go. Cloud-init instaluje k3s. Hasła zostają w Secret klastra i w pliku env, poza gitem.

## Zasady

- `tfstate` i `tfvars` nie są w git.
- Brak rekordu A na adres domu.
- k3s bez Traefika i bez ServiceLB.
- Apply nie jest krokiem pull requestu.
- Parametry VM są te same co w numerze maszyny.
- Plan niszczący dysk nie jest stosowany jako część tego numeru.

## Poza zakresem

- Deploy obrazów na k3s jest w [04-deploy-k3s.md](04-deploy-k3s.md).
- Azure jest w [08-azure.md](08-azure.md).
- OpenBao jest w [05-openbao.md](05-openbao.md).

## Źródła

- [Terraform](https://developer.hashicorp.com/terraform/language). Wiążące są plan i stan. Dotyczy obu katalogów. Ze źródła nie bierzemy stanu w git.
- [bpg/proxmox](https://registry.terraform.io/providers/bpg/proxmox/latest/docs). Wiążące są parametry VM. Dotyczy 4 vCPU, RAM, dysku, UEFI, TPM. Ze źródła nie bierzemy balonowania.
- [k3s install flags](https://docs.k3s.io/installation). Wiążące są `--disable=traefik` i `--disable=servicelb`. Dotyczy cloud-init. Ze źródła nie bierzemy domyślnej instalacji z Traefikiem.
- [Cloudflare Terraform](https://registry.terraform.io/providers/cloudflare/cloudflare/latest/docs). Wiążący jest rekord proxied i tunel. Dotyczy braku rekordu A. Ze źródła nie bierzemy originu na Postgresa.

## Gotowe gdy

- `fmt -check` przechodzi w obu katalogach.
- Stan i tfvars są ignorowane przez git.
- Na maszynie jest jeden węzeł k3s bez Traefika jako bramy produktu.
- Tunel nadal wskazuje bramę i `/versions` działa.
- W plikach Terraform nie ma hasła.
