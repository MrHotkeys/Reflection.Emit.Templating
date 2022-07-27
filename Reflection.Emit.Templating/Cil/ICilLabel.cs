namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public interface ICilLabel : ICilToken
    {
        CilTokenType ICilToken.TokenType => CilTokenType.Label;

        public string Name { get; }
    }
}