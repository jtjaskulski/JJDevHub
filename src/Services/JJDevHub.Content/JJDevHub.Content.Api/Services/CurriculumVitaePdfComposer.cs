using JJDevHub.Content.Application.DTOs;
using JJDevHub.Content.Application.ReadModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JJDevHub.Content.Api.Services;

public static class CurriculumVitaePdfComposer
{
    public static byte[] Compose(CurriculumVitaeDto cv, JobApplicationReadModel? job)
    {
        var pi = cv.PersonalInfo;
        var title = $"{pi.FirstName} {pi.LastName}".Trim();
        var jobLine = job is not null
            ? $"Target role: {job.Position} at {job.CompanyName}"
            : null;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(20).SemiBold();
                    col.Item().Text($"{pi.Email} · {pi.Phone ?? "—"} · {pi.Location ?? "—"}").FontSize(10);
                    if (!string.IsNullOrWhiteSpace(jobLine))
                        col.Item().PaddingTop(8).Text(jobLine!).Italic().FontColor(Colors.Blue.Medium);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(pi.Bio))
                    {
                        col.Item().Text("Profile").SemiBold().FontSize(13);
                        col.Item().PaddingTop(4).Text(pi.Bio!);
                        col.Item().PaddingTop(12);
                    }

                    col.Item().Text("Skills").SemiBold().FontSize(13);
                    foreach (var s in cv.Skills)
                        col.Item().PaddingTop(2).Text($"• {s.Name} ({s.Category}) — {s.Level}");

                    col.Item().PaddingTop(12).Text("Education").SemiBold().FontSize(13);
                    foreach (var e in cv.Educations)
                    {
                        col.Item().PaddingTop(4).Text($"{e.Institution} — {e.FieldOfStudy} ({e.Degree})");
                        col.Item()
                            .Text($"{e.PeriodStart:yyyy-MM} — {e.PeriodEnd?.ToString("yyyy-MM") ?? "present"}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Medium);
                    }

                    col.Item().PaddingTop(12).Text("Projects").SemiBold().FontSize(13);
                    foreach (var p in cv.Projects)
                    {
                        col.Item().PaddingTop(4).Text(p.Name).SemiBold();
                        col.Item().Text(p.Description);
                    }

                    if (job is not null && job.Requirements.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Role requirements (from tracker)").SemiBold().FontSize(13);
                        foreach (var r in job.Requirements)
                        {
                            var mark = r.IsMet ? "✓" : "○";
                            col.Item().PaddingTop(2).Text($"{mark} [{r.Category}] {r.Description}");
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("Generated ");
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).SemiBold();
                    text.Span(" UTC · JJDevHub");
                });
            });
        }).GeneratePdf();
    }
}
