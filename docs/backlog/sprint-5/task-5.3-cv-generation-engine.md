# Task 5.3: Silnik Generowania CV - Eksport do PDF

| Pole | Wartosc |
|------|---------|
| Sprint | 5 - The Secret Feature |
| Status | TODO |
| Priorytet | Medium |
| Estymacja | 13 story points |
| Powiazane pliki | Nowe pliki do stworzenia |

## Opis

Silnik generowania CV to kluczowa funkcja ukrytego modulu. Laczy dane z MongoDB read store (doswiadczenie zawodowe, umiejetnosci, wyksztalcenie) z wymaganiami konkretnej firmy (z ApplicationTracker) i generuje spersonalizowany dokument CV w formacie PDF. Kazde CV jest dostosowane do wymagan firmy - podkreslane sa umiejetnosci i doswiadczenie pasujace do oferty.

### Co trzeba zrobic

1. Integracja danych z roznych agregatow (WorkExperience, CurriculumVitae, JobApplication)
2. Silnik laczenia danych z szablonami CV
3. Generowanie PDF z QuestPDF
4. API endpoint do generowania i pobierania CV
5. Angular UI do konfiguracji i podgladu CV

## Kryteria akceptacji

- [ ] Endpoint `POST /api/content/cv/generate` przyjmujacy `JobApplicationId` i generujacy PDF
- [ ] Endpoint `GET /api/content/cv/download/{id}` zwracajacy PDF do pobrania
- [ ] CV zawiera: dane osobowe, doswiadczenie zawodowe, umiejetnosci, wyksztalcenie, projekty
- [ ] Doswiadczenie i umiejetnosci sa priorytetyzowane na bazie wymagan firmy (dopasowane na gorze)
- [ ] "Match score" widoczny wewnetrznie (np. "85% wymagan spelnionych")
- [ ] Profesjonalny layout PDF (czytelny, estetyczny, ATS-friendly)
- [ ] Mozliwosc wyboru szablonu (np. Classic, Modern, Minimal)
- [ ] Angular podglad CV przed generowaniem
- [ ] Historia wygenerowanych CV (lista z datami i firmami)

## Wymagane pakiety NuGet

| Pakiet | Wersja | Projekt docelowy | Uzasadnienie |
|--------|--------|-----------------|--------------|
| `QuestPDF` | latest | JJDevHub.Content.Application lub Infrastructure | Generowanie PDF - fluent API, MIT license, darmowy, natywny .NET |

### Alternatywy rozwazone

| Pakiet | Licencja | Zalety | Wady |
|--------|----------|--------|------|
| `QuestPDF` | MIT (Community) | Fluent API, hot reload, darmowy | Community edition - limit na przychody |
| `itext7` | AGPL | Bardzo dojrzaly, pelna specyfikacja PDF | AGPL wymaga open source lub licencje komercyjna |
| `PdfSharpCore` | MIT | Darmowy, prosty | Niski poziom API, brak fluent, mniej funkcji |

**Rekomendacja: QuestPDF** - najlepszy stosunek jakosc/wygoda, fluent API idealnie pasuje do dynamicznego generowania CV.

## Kroki implementacji

1. **Zainstaluj QuestPDF:**
   ```
   dotnet add src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure package QuestPDF
   ```

2. **Stworz serwis `ICvGenerationService` w Application:**
   ```csharp
   public interface ICvGenerationService
   {
       Task<byte[]> GenerateAsync(Guid jobApplicationId, CvTemplate template);
       Task<CvPreviewDto> PreviewAsync(Guid jobApplicationId);
   }
   ```

3. **Zaimplementuj `CvGenerationService` w Infrastructure:**
   - Pobierz dane z MongoDB read store:
     - `WorkExperienceReadModel[]` - doswiadczenie zawodowe
     - `CurriculumVitaeReadModel` - umiejetnosci, wyksztalcenie, dane osobowe
     - `JobApplicationReadModel` - wymagania firmy (do priorytetyzacji)
   - Oblicz "match score" (ile wymagan `IsMet = true`)
   - Posortuj doswiadczenie i umiejetnosci: dopasowane do wymagan na gorze
   - Wygeneruj PDF z QuestPDF

4. **Zdefiniuj szablony CV (QuestPDF Documents):**
   ```csharp
   public class ClassicCvDocument : IDocument
   {
       public void Compose(IDocumentContainer container)
       {
           container.Page(page =>
           {
               page.Header().Element(ComposeHeader);
               page.Content().Element(ComposeContent);
               page.Footer().Element(ComposeFooter);
           });
       }
   }
   ```
   - `ClassicCvDocument` - tradycyjny layout (chronologiczny)
   - `ModernCvDocument` - dwukolumnowy z sidebar
   - `MinimalCvDocument` - minimalistyczny, duzo bialej przestrzeni

5. **Dodaj API endpointy:**
   ```csharp
   group.MapPost("/cv/generate", async (GenerateCvCommand command, ISender sender) => ...)
       .RequireAuthorization("OwnerOnly")
       .Produces<FileResult>(200, "application/pdf");

   group.MapGet("/cv/download/{id}", async (Guid id, ISender sender) => ...)
       .RequireAuthorization("OwnerOnly");

   group.MapPost("/cv/preview", async (PreviewCvQuery query, ISender sender) => ...)
       .RequireAuthorization("OwnerOnly");
   ```

6. **Zbuduj Angular UI:**
   - `CvGeneratorComponent`:
     - Wybor firmy z JobApplication (dropdown)
     - Wybor szablonu CV (preview thumbnails)
     - Podglad danych ktore beda w CV (edytowalny)
     - Przycisk "Generuj PDF" -> download
   - `CvHistoryComponent`:
     - Lista wygenerowanych CV z datami
     - Ponowne pobranie / regeneracja

7. **Dodaj przechowywanie wygenerowanych PDF:**
   - Opcja A: MongoDB GridFS (przechowywanie binarne w MongoDB)
   - Opcja B: Filesystem z referencja w PostgreSQL
   - Rekomendacja: MongoDB GridFS (spojne z istniejaca infrastruktura)

## Model danych

```
GenerateCvCommand
├── JobApplicationId: Guid
├── Template: CvTemplate (Classic | Modern | Minimal)
├── IncludeSections: List<CvSection>
│   └── PersonalInfo | WorkExperience | Skills | Education | Projects
└── CustomOrder: bool (true = priorytetyzuj wg wymagan firmy)

CvPreviewDto
├── PersonalInfo: PersonalInfoDto
├── WorkExperiences: List<WorkExperienceDto> (posortowane wg relevance)
├── Skills: List<SkillDto> (posortowane wg relevance)
├── Education: List<EducationDto>
├── Projects: List<ProjectDto>
├── MatchScore: decimal (0.0 - 1.0)
└── MatchedRequirements: List<string>
```

## Zaleznosci

- **Wymaga:** Task 2.1 (CurriculumVitae agregat), Task 5.1 (Angular RBAC), Task 5.2 (ApplicationTracker z wymaganiami firm)
- **Blokuje:** Nic (to jest koncowy produkt)

## Notatki techniczne

- QuestPDF fluent API pozwala na composable layout - kazda sekcja CV (header, doswiadczenie, umiejetnosci) jest oddzielnym komponentem ktory mozna laczyc w rozne szablony.
- QuestPDF ma hot reload - mozna edytowac layout i widziec zmiany natychmiast w QuestPDF Companion app.
- ATS-friendly format: unikaj grafik, uzyj standardowych czcionek, hierarchia naglowkow, brak tabel zagniezdoznych. Wiele firm uzywa ATS (Applicant Tracking System) do automatycznego parsowania CV.
- "Match score" jest wewnetrzna metryka (nie widoczna na CV) - pomaga w ocenie dopasowania do oferty.
- Wygenerowane PDF powinny miec metadata (tytul, autor, data) ustawione przez QuestPDF.
- Na przyszlosc: integracja z AI (Task AI.Gateway) do sugerowania lepszych opisow doswiadczenia pod wymagania firmy.
