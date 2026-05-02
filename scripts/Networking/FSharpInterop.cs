using Microsoft.FSharp.Core;

namespace AirBrawl2.Networking.FSharpInterop;

public static class FSharpOptionExtension
{
    public static T? ToNullable<T>(this FSharpOption<T> opt) =>
        FSharpOption<T>.GetTag(opt) switch
        {
            FSharpOption<T>.Tags.Some => opt.Value,
            _ => default
        };
}

public static class FSharpResultExtension
{
    public static bool HasError<T, TE>(this FSharpResult<T, TE> result, out TE error)
    {
        error = result.ErrorValue;
        return result.IsError;
    }
}
