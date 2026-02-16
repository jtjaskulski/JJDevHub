using MediatR;

namespace JJDevHub.Shared.Kernel.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse> { }
