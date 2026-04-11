using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.UpdateJobApplication;

public record UpdateJobApplicationCommand(
    Guid Id,
    long ExpectedVersion,
    string CompanyName,
    string? Location,
    string? WebsiteUrl,
    string? Industry,
    string Position,
    ApplicationStatus Status,
    DateOnly AppliedDate,
    Guid? LinkedCurriculumVitaeId) : ICommand<Unit>;
