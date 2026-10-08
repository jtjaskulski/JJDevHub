# Budżet stylów

Limity produkcyjne w [angular.json](../../../src/Clients/web/angular.json), konfiguracja `production` → `budgets`:

| Typ | Warning | Error |
| --- | --- | --- |
| `initial` | 500kB | 1MB |
| `anyComponentStyle` | **24kB** | **40kB** |

`anyComponentStyle` podniesiono względem domyślnego Angulara, żeby pomieścić SCSS stron (home z hero + kafle, CV, szczegóły kursów) bez sztucznego rozbijania arkuszy. Nadal pilnuje, żeby pojedynczy `styleUrl` komponentu nie urósł bez kontroli.

Przy `ng build` (produkcja) przekroczenie error zatrzyma build CI ([web.yml](../../../.github/workflows/web.yml)).
