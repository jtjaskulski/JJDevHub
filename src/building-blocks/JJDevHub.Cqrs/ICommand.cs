namespace JJDevHub.Cqrs
{

    public interface ICommand<TResponse> : IRequest<TResponse>;
}
