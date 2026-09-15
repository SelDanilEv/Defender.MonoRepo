using Defender.Common.Exceptions;

namespace Defender.CarService.Application.Common.Exceptions;

public sealed class CarApplicationException : ServiceException
{
    public CarApplicationException(string code)
        : base(code)
    {
        Code = code;
    }

    public CarApplicationException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
