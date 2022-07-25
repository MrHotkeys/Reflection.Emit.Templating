using System;
using System.Reflection;

namespace MrHotkeys.Reflection.Emit.Templating.Extensions
{
    public static class AccessibilityExtensions
    {
        public static MethodAttributes ToMethodAttributes(this Accessibility a) => a switch
        {
            Accessibility.Public => MethodAttributes.Public,
            Accessibility.Private => MethodAttributes.Private,
            Accessibility.Protected => MethodAttributes.Family,
            Accessibility.Internal => MethodAttributes.Assembly,
            Accessibility.ProtectedInternal => MethodAttributes.FamORAssem,
            Accessibility.PrivateProtected => MethodAttributes.FamANDAssem,
            _ => throw new InvalidOperationException(),
        };
    }
}