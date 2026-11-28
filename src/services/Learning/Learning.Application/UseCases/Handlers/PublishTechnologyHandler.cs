using JJDevHub.Shared.Kernel;

using Learning.Application.Ports;
using Learning.Application.UseCases.Commands;
using Learning.Domain;

namespace Learning.Application.UseCases.Handlers
{
    public sealed class PublishTechnologyHandler(
        ITechnologyRepository technologies,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        public async Task<Result<Guid>> HandleAsync(PublishTechnologyCommand command, CancellationToken cancellationToken)
        {
            Category? category = await categories.GetBySlugAsync(command.CategorySlug, cancellationToken);
            if (category is null)
            {
                return Result<Guid>.Fail(ResultErrorKind.NotFound, "learning.category.missing");
            }

            Result<Technology> published = ToTechnology(command, category.Slug);
            if (!published.IsSuccess)
            {
                return Result<Guid>.Fail(published.Error);
            }

            await technologies.AddAsync(published.Value, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(published.Value.Id);
        }

        private Result<Technology> ToTechnology(PublishTechnologyCommand command, Slug categorySlug)
        {
            Result<Slug> slug = Slug.Create(command.Slug);
            if (!slug.IsSuccess)
            {
                return Result<Technology>.Fail(slug.Error);
            }

            Result<LocalizedText> title = LocalizedText.Create(command.TitlePl, command.TitleEn);
            if (!title.IsSuccess)
            {
                return Result<Technology>.Fail(title.Error);
            }

            return Technology.Publish(slug.Value, title.Value, categorySlug, clock);
        }
    }
}