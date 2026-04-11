using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;

namespace JJDevHub.Content.Application.Commands.CreateJobApplication;

public record CreateJobApplicationCommand(
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId) : ICommand<Guid>;
