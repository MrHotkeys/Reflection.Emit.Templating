namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public enum CilInstructionType
    {
        LoadArgument,
        LoadArgumentAddress,
        StoreArgument,
        LoadLocal,
        LoadLocalAddress,
        StoreLocal,
        Branch,
        LoadField,
        LoadFieldAddress,
        LoadStaticField,
        StoreField,
        StoreStaticField,
        Call,
        Return,
        Raw,
    }
}