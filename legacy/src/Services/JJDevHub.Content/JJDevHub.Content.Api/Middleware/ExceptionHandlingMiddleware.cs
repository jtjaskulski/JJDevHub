using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using JJDevHub.Content.Core.Exceptions;
using JJDevHub.Shared.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.Content.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, body) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(
                    "VALIDATION.FAILED",
                    null,
                    validationEx.Errors
                        .Select(e => new ValidationErrorItem(
                            string.IsNullOrEmpty(e.PropertyName) ? null : ToCamelCase(e.PropertyName),
                            string.IsNullOrEmpty(e.ErrorCode) ? "VALIDATION.GENERIC" : e.ErrorCode,
                            e.ErrorMessage))
                        .ToArray())),

            WorkExperienceConcurrencyException => (
                HttpStatusCode.Conflict,
                new ErrorResponse("CONTENT.WORK_EXPERIENCE.CONCURRENCY_MISMATCH", exception.Message)),

            CurriculumVitaeConcurrencyException => (
                HttpStatusCode.Conflict,
                new ErrorResponse("CONTENT.CURRICULUM_VITAE.CONCURRENCY_MISMATCH", exception.Message)),

            JobApplicationConcurrencyException => (
                HttpStatusCode.Conflict,
                new ErrorResponse("CONTENT.JOB_APPLICATION.CONCURRENCY_MISMATCH", exception.Message)),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                new ErrorResponse(
                    "CONTENT.CONCURRENCY_CONFLICT",
                    "The record was modified by another process.")),

            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                new ErrorResponse(domainEx.Code, domainEx.Message)),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new ErrorResponse("CONTENT.NOT_FOUND", exception.Message)),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse(
                    "SERVER.INTERNAL_ERROR",
                    _environment.IsDevelopment()
                        ? $"{exception.Message} | {exception.InnerException?.Message}"
                        : "An unexpected error occurred."))
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }

    private static string ToCamelCase(string pascal)
    {
        if (string.IsNullOrEmpty(pascal) || char.IsLower(pascal[0]))
            return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}

public record ErrorResponse(
    string Code,
    string? Message = null,
    IReadOnlyList<ValidationErrorItem>? Errors = null);

public record ValidationErrorItem(string? Field, string Code, string? Message);
