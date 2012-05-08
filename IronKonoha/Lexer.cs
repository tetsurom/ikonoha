using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IronKonoha
{
    class Lexer
    {
        private StringReader reader;

        public Lexer(StringReader reader)
        {
            this.reader = reader;
        }
    }
}
