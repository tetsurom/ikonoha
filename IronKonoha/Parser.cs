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
        public KonohaExpr ParseExpr(TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentException("Reader must not be null.");
            }
            return ParseExprAux(new Lexer(reader));
        }

        private KonohaExpr ParseExprAux(Lexer lexer)
        {
            throw new NotImplementedException();
        }


        public abstract class KonohaExpr{}
    }
}
