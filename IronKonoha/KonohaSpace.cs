using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
    public class KonohaSpace
    {
        private Context ctx;

        public KonohaSpace(Context ctx)
        {
            this.ctx = ctx;
        }

        public void Eval(string script)
        {
            var tokenizer = new Tokenizer(ctx, this);
            var parser = new Parser(ctx, this);
            var tokens = tokenizer.Tokenize(script);
            var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
        }
    }
}
