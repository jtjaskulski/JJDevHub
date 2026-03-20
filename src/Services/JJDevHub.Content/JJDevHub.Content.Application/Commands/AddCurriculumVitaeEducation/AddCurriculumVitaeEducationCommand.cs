using JJDevHub.Content.Core.Enums;
using JJDevHub.Shared.Kernel.CQRS;
using MediatR;

namespace JJDevHub.Content.Application.Commands.AddCurriculumVitaeEducation;

public record AddCurriculumVitaeEducationCommand(
    Guid CurriculumVitaeId,
    long ExpectedVersion,
    string Institution,
    string FieldOfStudy,
    EducationDegree Degree,
    DateTime PeriodStart,
    DateTime? PeriodEnd) : ICommand<Unit>;
