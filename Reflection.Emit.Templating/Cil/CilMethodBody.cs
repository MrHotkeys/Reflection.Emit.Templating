using System;
using System.Collections.Generic;

namespace MrHotkeys.Reflection.Emit.Templating.Cil
{
    public sealed class CilMethodBody : ICilMethodBody
    {
        private IList<ICilLocalVariable> _locals;
        public IList<ICilLocalVariable> Locals
        {
            get => _locals;
            set => _locals = value ?? throw new ArgumentNullException(nameof(Locals));
        }

        private IList<ICilToken> _tokens;
        public IList<ICilToken> Tokens
        {
            get => _tokens;
            set => _tokens = value ?? throw new ArgumentNullException(nameof(Tokens));
        }

        public CilMethodBody(List<ICilLocalVariable> locals, List<ICilToken> tokens)
        {
            _locals = locals ?? throw new ArgumentNullException(nameof(locals));
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }
    }
}