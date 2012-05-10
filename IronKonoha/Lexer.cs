using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IronKonoha
{
    class Lexer
    {
        private TextReader reader;

        public Lexer(TextReader reader)
        {
            this.reader = reader;
            Tokenize();
        }

        abstract class Token { };

        delegate Token FTokenizer(TextReader reader);

        private Dictionary<char, FTokenizer> tokenizerMatrix;
        private List<Token> tokens;

        private void Tokenize()
        {
            while (reader.Peek() >= 0)
            {
                char ch = (char)reader.Read();
                tokens.Add(tokenizerMatrix[ch](reader));
            }
        }

    }
}
