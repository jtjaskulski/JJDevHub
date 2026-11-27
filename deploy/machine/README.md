# Maszyna jjdevhub

Gość wydania. Parametry z tej strony później przepisuje Terraform. Adres panelu hypervisora i hasła tu nie stoją.

## Parametry

| Parametr | Wartość |
| --- | --- |
| Nazwa | `jjdevhub` |
| System | Ubuntu 24.04.5 LTS |
| vCPU | 4, jeden socket |
| Typ CPU | `host` |
| Pamięć | 16384 MiB, balonowanie wyłączone |
| Dysk wirtualny | 120 GiB |
| Maszyna | `q35` |
| BIOS | OVMF (UEFI), dysk EFI 4 MiB, klucze pre-enrolled |
| vTPM | 2.0 |
| Sieć | VirtIO, gość wychodzi do internetu |
| Port 80 i 443 | brak przekierowania z routera na gościa |
| Agent QEMU | włączony |

Dysk `sda` ma 120 GiB. Instalator założył partycję EFI 1 GiB, `/boot` 2 GiB i grupę LVM na pozostałych 116,9 GiB. Wolumin root `ubuntu-vg/ubuntu-lv` ma 58,5 GiB. Reszta grupy nie jest przypisana do tego woluminu.

## Konto i env

Użytkownik `jjdeploy` jest w grupie `docker` i nie ma hasła logowania. Katalog `/etc/jjdevhub` ma właściciela root, grupę `jjdeploy` i tryb `750`. Plik `/etc/jjdevhub/compose.env` ma właściciela `jjdeploy` i tryb `600`. Wartości zostają na maszynie. W git jest tylko `deploy/compose/compose.env.example`.

## Docker

Pakiety z repozytorium Ubuntu: `docker.io` i `docker-compose-v2`. Demon wstaje razem z gościem.

## Runner

Jedna instancja. Nazwa i etykieta to `jjdevhub`. Proces chodzi jako `jjdeploy`. Jednostka na hoście nazywa się `actions.runner.jtjaskulski-JJDevHub.jjdevhub.service`. Wzór bez tokenu jest w `deploy/machine/runner.service`. Token rejestracji był jednorazowy i nie wchodzi do git.

## Polecenia

Po instalacji systemu, jako konto administracyjne:

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y qemu-guest-agent
sudo apt install -y docker.io docker-compose-v2
sudo systemctl enable --now docker
sudo adduser --disabled-password --gecos "" jjdeploy
sudo usermod -aG docker jjdeploy
sudo install -d -o root -g jjdeploy -m 750 /etc/jjdevhub
sudo install -o jjdeploy -g jjdeploy -m 600 /dev/null /etc/jjdevhub/compose.env
```

Archiwum runnera dla Linux x64 leży w `/home/jjdeploy/actions-runner`. Rejestracja szła jako `jjdeploy`: adres `https://github.com/jtjaskulski/JJDevHub`, nazwa `jjdevhub`, etykieta `jjdevhub`. Usługa, z tego katalogu:

```bash
sudo ./svc.sh install jjdeploy
sudo ./svc.sh start
```

## Sprawdzenie

```bash
nproc
free -h
sudo -iu jjdeploy docker info
systemctl is-active 'actions.runner.*'
sudo stat -c '%a %U:%G %n' /etc/jjdevhub /etc/jjdevhub/compose.env
test -d /sys/firmware/efi && echo uefi
cat /sys/class/tpm/tpm0/tpm_version_major
```

`nproc` pokazuje 4. Pamięć jest rzędu 16 GiB. `docker info` odpowiada jako `jjdeploy`. Runner jest aktywny. Katalog env ma tryb `750` i właściciela `root:jjdeploy`, plik ma tryb `600` i właściciela `jjdeploy:jjdeploy`. Gość bootuje przez UEFI, a TPM ma wersję 2.
