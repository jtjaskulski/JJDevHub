# Task 5.3: Silnik Generowania CV - Eksport do PDF

| Pole | Wartosc |
|------|---------|
| Sprint | 5 - The Secret Feature |
| Status | DONE |
| Priorytet | Medium |
| Estymacja | 13 story points |
| Powiazane pliki | `CurriculumVitaePdfComposer.cs`, `ICvPdfBlobStore`, `CurriculumVitaeEndpoints` (`POST .../cv/{id}/pdf`, `GET .../cv/pdf-download/{fileId}`), `admin-cv.page.*`, pakiet `QuestPDF` |

## Opis

Generowanie CV w PDF z danych read modelu (`CurriculumVitae` + powiazane sekcje). Wygenerowany plik zapisywany w MongoDB (`cv_pdf_blobs`), pobranie po `fileId`. UI admin (`admin/cv`) — generowanie i podglad/lista powiazana z istniejacym CV.

## Kryteria akceptacji (MVP wdrozone)

- [x] Generowanie PDF (QuestPDF) na podstawie danych CV z MongoDB
- [x] Endpoint `POST /api/v1/content/cv/{id}/pdf` (Owner) — tworzy blob, zwraca metadane
- [x] Endpoint `GET /api/v1/content/cv/pdf-download/{fileId}` (Owner) — `application/pdf`
- [x] CV zawiera: dane osobowe, doswiadczenie, umiejetnosci, wyksztalcenie, projekty (zgodnie z kompozytorem)
- [x] Angular: strona admin-cv — operacje na CV i generowanie/pobieranie PDF

## Kolejne iteracje

- [ ] Priorytetyzacja sekcji wzgledem wymagan z `JobApplication` (match score w PDF/UI)
- [ ] Wiele szablonow wizualnych (Classic / Modern / Minimal) jako osobne layouty QuestPDF
- [ ] Historia generacji z filtrowaniem po firmie/dacie (rozszerzenie listy w UI)
- [ ] Metryki biznesowe (OpenTelemetry): licznik wygenerowanych PDF

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `QuestPDF` | (projekt) | JJDevHub.Content.Infrastructure | Generowanie PDF — fluent API, MIT |

## Zaleznosci

- **Wymaga:** Task 2.1 (CurriculumVitae), Task 5.1 (Angular RBAC), Task 5.2 (opcjonalnie powiazanie aplikacji z CV przez `LinkedCurriculumVitaeId`)

## Notatki techniczne

- ATS-friendly layout: proste sekcje, czytelna hierarchia — dalsze dopracowanie w kolejnych szablonach.
- Przechowywanie binariow w MongoDB jako dedykowana kolekcja blobow (spojne z reszta read store).
