using System.Collections.Generic;

namespace IM;

public class ImError
{
    public const int InvalidInput = 400;
    public static readonly Dictionary<int, string> Message = new()
    {
        { InvalidInput, "Invalid input params." },
    };
}