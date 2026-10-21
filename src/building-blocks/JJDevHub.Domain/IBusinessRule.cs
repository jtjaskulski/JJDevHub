namespace JJDevHub.Domain
{
    public interface IBusinessRule
    {
        ResultError Error { get; }

        bool IsBroken();
    }
}