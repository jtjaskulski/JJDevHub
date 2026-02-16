using MediatR;

namespace JJDevHub.Shared.Kernel.CQRS;

public interface ICommand<out TResponse> : IRequest<TResponse> { }
