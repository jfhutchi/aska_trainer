using HutchASKA.Core.Player;
using SandSailorStudio.Attributes;

namespace HutchASKA.Plugin.Player;

internal static class VariableAttributeController
{
    internal static void Fill(VariableAttribute attribute)
    {
        var maximum = AttributeMath.ValueAtFraction(attribute.min, attribute.max, 1);
        attribute.SetValue(maximum);
    }
}
