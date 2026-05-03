namespace GestionCo.Api.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} avec l'identifiant '{key}' n'a pas été trouvé(e).") { }
    public NotFoundException(string message) : base(message) { }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("Une ou plusieurs erreurs de validation ont eu lieu.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Action non autorisée") : base(message) { }
}
