# 04. Maszyna i runner

Poprzedni numer to [Dockerfile](03-dockerfile.md). Następny to [Cloudflare](05-cloudflare.md).

Ten numer stawia jedną maszynę wirtualną i runner `jjdevhub`. Maszyna ma Dockera. Nie ma jeszcze k3s: klaster jest fazą późniejszą. Nie ma jeszcze tunelu. Jest katalog na env i użytkownik, którym runner wdraża. Publiczna chmura nie jest celem tego numeru.

## Po co

Bez maszyny tag nie ma dokąd wjechać, a compose zostaje ćwiczeniem na laptopie. Bez osobnego runnera job deployu musiałby iść z `ubuntu-latest` prosto do sieci domowej, czyli otworzyć port albo trzymać klucz w sekrecie GitHuba. Runner stoi na maszynie i wychodzi do GitHuba sam. GitHub nie łączy się do domu.

Runner nie wykonuje pull requestów. Pull request zostaje na `ubuntu-latest`. Inaczej kod z gałęzi roboczej działałby na maszynie, na której leży env.

## Dlaczego ta technologia

### Jedna VM

4 vCPU, 16 GiB RAM bez balonowania, dysk 120 GiB, UEFI, vTPM. Problem, który zamyka, to „wdrożę na laptopie, który chodzi”. Odpada kontener tylko na laptopie jako wydanie. Koszt: jedna maszyna jest jedną awarią. Ten produkt świadomie nie ma drugiej lokalizacji. Kopia dysku jest sprawą hypervisora i nie jest opisana jako drugi region.

Balonowanie jest wyłączone, bo proces, który traci pamięć w środku requestu, wygląda jak wyciek aplikacji. vTPM zostaje od razu, choć OpenBao użyje go dopiero w fazie klastra. Dokładanie TPM później oznacza przebudowę VM.

Na tym numerze hypervisor może być kliknięty. Opis kodem dochodzi w [Terraformie](../13-kubernetes/03-terraform-maszyna.md). Ten numer zostawia listę parametrów, których Terraform nie będzie później „poprawiał” po cichu.

### Docker, nie k3s

Na maszynie jest Docker Engine i plugin compose. Problem, który zamyka, to wejście w klaster, zanim pierwsza brama w ogóle wstała. Odpada k3s w tym numerze. Koszt: dwa razy opisujemy uruchomienie, raz compose, raz manifesty. To jest cel kolejności, nie pomyłka. Compose jest wydaniem do czasu fazy klastra.

### Runner `jjdevhub`

GitHub Actions self-hosted, etykieta `jjdevhub`, jedna instancja, usługa systemowa użytkownika `jjdeploy`. Użytkownik jest w grupie `docker`. Nie jest rootem hosta poza tym, co daje grupa docker: grupa docker jest równoważna rootowi wobec demona i to zostaje zapisane wprost, żeby nie udawać izolacji, której nie ma. Osobny użytkownik i tak ogranicza to, co widać w katalogu domowym.

Problem, który zamyka, to klucz SSH do maszyny w sekrecie repozytorium. Odpada deploy z hosted runnera po SSH. Koszt: program runnera trzeba zaktualizować razem z wydaniem GitHuba. Harmonogram aktualizacji jest ręczny i jest w checkpointcie: `systemctl status` pokazuje działającą usługę.

Token rejestracji runnera jest jednorazowy i nie wchodzi do git.

### Env poza repozytorium

`/etc/jjdevhub/compose.env` ma uprawnienie `600` i właściciela `jjdeploy`. Na tym numerze plik może być prawie pusty. Ma istnieć i nie być w git. Problem, który zamyka, to hasło dopisane do repozytorium „na chwilę, żeby wstało”. Odpada `.env` commitowany z przykładowym hasłem produkcyjnym. Przykład kluczy, bez wartości, jest w `deploy/compose/compose.env.example` i zawiera wyłącznie nazwy zmiennych.

### Sonar do oglądania

Ten sam obraz `sonarqube:community` może stać na maszynie jako kontener uruchamiany ręcznie, bez publikacji portu na adres domu. Werdykt pull requestu i tak liczy job z [06-sonarqube.md](../01-bramki/06-sonarqube.md). Ten kontener jest po to, żeby dało się otworzyć wynik lokalnie. Nie jest bramką.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| self-hosted runner | program na maszynie, który sam łączy się z GitHubem |
| etykieta `jjdevhub` | jedyna etykieta, na której wolno uruchomić deploy |
| balonowanie | oddawanie pamięci gościa hostowi; tu wyłączone |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/machine/README.md` | parametry VM i polecenia, które na niej wykonano |
| `deploy/compose/compose.env.example` | nazwy zmiennych, puste wartości |
| `deploy/machine/runner.service` | jednostka systemd, bez tokenu |

README maszyny nie zawiera adresu management hypervisora ani hasła.

## Plan

### 1. VM

Założyć gościa o parametrach z tego numeru. Sieć: gość wychodzi do internetu. Nie ma przekierowania portu 80 ani 443 z routera na gościa. Wejście HTTP dojdzie tunelem, nie otwartym portem.

Zainstalować Docker Engine z oficjalnego repozytorium dystrybucji gościa. `docker compose version` działa.

### 2. Użytkownik i env

Użytkownik `jjdeploy`. Katalog `/etc/jjdevhub` właściciel root, katalog odczytywalny tylko przez root i `jjdeploy`. Plik env `600`.

### 3. Runner

Rejestracja z etykietą `jjdevhub` i z wyłączonym wykonywaniem jobów innych niż te, które w workflow mają `runs-on: [self-hosted, jjdevhub]`. W ustawieniach repozytorium runner nie jest przypisany do pull requestów z forka. Repozytorium jest własne. Pull request i tak idzie na `ubuntu-latest`, co już mówią workflow bramek.

### 4. Dokument parametrów

`deploy/machine/README.md` wymienia vCPU, RAM, dysk, UEFI, vTPM, nazwę gościa i to, że port 443 nie jest otwarty. Nie wymienia sekretu.

### Co jest do czego

| Element | Po co |
| --- | --- |
| VM | miejsce, w którym działa wydanie |
| Docker | uruchamia compose, zanim będzie k3s |
| runner | job deployu bez klucza SSH w GitHubie |
| `compose.env` | wartości, których nie ma w git |

## Testy

Ten numer nie ma testu xUnit. Sprawdzeniem jest checkpoint wykonany na maszynie i plik w git, który nie zawiera sekretu.

| Arrange | Act | Assert |
| --- | --- | --- |
| maszyna po krokach | `docker info` jako `jjdeploy` | demon odpowiada |
| repozytorium | `gitleaks` na `deploy/machine` | kod `0` |

## Checkpoint

Na maszynie:

```bash
nproc
free -h
docker info
systemctl is-active actions.runner.* 
test "$(stat -c %a /etc/jjdevhub/compose.env)" = "600"
```

`nproc` pokazuje 4. Pamięć jest rzędu 16 GiB. Runner jest aktywny. Z laptopa port 443 gościa nie przyjmuje połączenia z internetu. Sprawdzenie portu jest z zewnątrz, nie z gościa: na gościu nic nie słucha na 443 i to też jest dobry wynik.

W git:

```bash
test -f deploy/machine/README.md
test -f deploy/compose/compose.env.example
git grep -n . -- deploy/compose/compose.env.example
```

Przykład env nie zawiera wartości po znaku równości albo zawiera wyłącznie pusty cudzysłów.

## Umiesz wyjaśnić

Dlaczego runner nie buduje pull requestów?

Bo gałąź robocza jest kodem, który dopiero przechodzi checki. Uruchomienie jej na maszynie z env miesza nieprzejrzany kod z wydaniem.

Dlaczego nie ma otwartego 443?

Bo wejście ma iść tunelem wychodzącym. Otwarty port na routerze jest drugą bramą, której YARP już nie kontroluje.

Czemu grupa docker nie jest „bezpiecznym użytkownikiem”?

Bo kto może mówić do demona, może uruchomić kontener z montowaniem katalogu głównego. Osobny użytkownik porządkuje pliki. Nie zastępuje izolacji kernela. Dlatego na tym runnerze wolno uruchamiać tylko job deployu z `main` i tagu, co wymusza workflow, nie etykieta sama z siebie.

Po co vTPM już teraz?

Bo dodanie TPM do istniejącej VM jest nową maszyną. OpenBao użyje pieczęci później. Sprzęt jest od początku.

Dlaczego Sonar na maszynie nie jest bramką?

Bo jego próg da się kliknąć. Bramka jest w jobie, który ładuje plik z git do świeżego kontenera.

## Zasady

- Pull request nie ma `runs-on` z etykietą `jjdevhub`.
- W git nie ma tokenu runnera, hasła hypervisora ani zawartości `compose.env`.
- Port 80 i 443 gościa nie jest publikowany na routerze.
- K3s nie jest instalowany w tym numerze.
- Parametry VM w README są tymi, które później przepisze Terraform. Ręczna zmiana po Terraformie jest zabroniona; do czasu Terraforma README jest źródłem.

## Poza zakresem

- DNS i tunel są w [05-cloudflare.md](05-cloudflare.md).
- Plik compose jest w [06-compose.md](06-compose.md).
- Job, który użyje runnera, jest w [07-deploy.md](07-deploy.md).
- Terraform tej samej VM jest w [03-terraform-maszyna.md](../13-kubernetes/03-terraform-maszyna.md).
- k3s jest w [04-deploy-k3s.md](../13-kubernetes/04-deploy-k3s.md).

## Źródła

- [Self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners). Wiążące jest to, że runner sam nawiązuje połączenie wychodzące. Dotyczy braku klucza SSH w sekrecie. Ze źródła nie bierzemy runnera na pull requestach.
- [Docker Engine post-install](https://docs.docker.com/engine/install/linux-postinstall/). Wiążące jest uruchamianie demona i grupa `docker`. Dotyczy użytkownika `jjdeploy`. Ze źródła nie bierzemy twierdzenia, że grupa docker jest ograniczonym kontem.
- [GitHub Actions security](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions#hardening-for-self-hosted-runners). Wiążące jest niewpuszczanie nieprzejrzanego kodu na self-hosted. Dotyczy podziału `ubuntu-latest` i `jjdevhub`. Ze źródła nie bierzemy self-hosted jako przyspieszenia testów pull requestu.

## Gotowe gdy

- Gość ma 4 vCPU, 16 GiB, dysk 120 GiB, UEFI i vTPM.
- `jjdeploy` uruchamia `docker info`.
- Runner z etykietą `jjdevhub` jest aktywny.
- `/etc/jjdevhub/compose.env` ma uprawnienie `600` i nie jest w git.
- Z internetu port 443 gościa nie przyjmuje połączenia.
