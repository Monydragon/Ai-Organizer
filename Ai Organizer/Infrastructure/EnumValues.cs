using System;
using System.Collections.Generic;
using System.Linq;

namespace Ai_Organizer.Infrastructure;

public static class EnumValues
{
    public static IReadOnlyList<TEnum> GetValues<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().ToList();
}

