using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IronKonoha
{
    /// <summary>
    /// Create Konoha AST from sourcecode.
    /// </summary>
    public class Parser
    {
        public KonohaExpr ParseExpr(String str)
        {
            if (str == null)
            {
                throw new ArgumentException("str must not be null.");
            }
            return ParseExprAux(new Lexer(str));
        }

        private KonohaExpr ParseExprAux(Lexer lexer)
        {
            throw new NotImplementedException();
        }

        public abstract class KonohaExpr{}
    }
}
