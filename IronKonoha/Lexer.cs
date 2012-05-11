using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace IronKonoha
{
    class Lexer
    {
        /// <summary>
        /// temporaly
        /// </summary>
        internal class TokenizerEnvironment
        {
            public string Source;

            public FTokenizer[] tokenizerMatrix { get; set; }

            public int TabWidth = 4;

            public int Line { get; set; }

            public int bol { get; set; }
        }

        /// <summary>
        /// temporaly
        /// </summary>
        internal class Method
        {

        }

        public abstract class Token { };

        internal class LiteralToken<T> : Token
        {
            public T Value { get; private set; }
            public LiteralToken(T val)
            {
                Value = val;
            }
        }
        internal class IntegerToken : LiteralToken<long>
        {
            public IntegerToken(long val)
                : base(val)
            {
            }
        }
        internal class FloatToken : LiteralToken<double>
        {
            public FloatToken(double val)
                : base(val)
            {
            }
        }
        internal class StringToken : LiteralToken<string>
        {
            public StringToken(string val)
                : base(val)
            {
            }
        }
        internal class SymbolToken : StringToken
        {
            public SymbolToken(string sym)
                : base(sym)
            {
            }
        }
        internal class OperatorToken : StringToken
        {
            public OperatorToken(string op)
                : base(op)
            {
            }
        }
        internal class CodeToken : StringToken
        {
            public CodeToken(string code)
                : base(code)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token">トークン</param>
        /// <param name="tenv"></param>
        /// <param name="tokStart">トークンの開始位置</param>
        /// <param name="thunk"></param>
        /// <returns>次のトークンの開始位置</returns>
        public delegate int FTokenizer(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk);

        enum CharType
        {
            NULL,
            UNDEF,
            DIGIT,
            UALPHA,
            LALPHA,
            MULTI,
            NL,
            TAB,
            SP,
            LPAR,
            RPAR,
            LSQ,
            RSQ,
            LBR,
            RBR,
            LT,
            GT,
            QUOTE,
            DQUOTE,
            BKQUOTE,
            OKIDOKI,
            SHARP,
            DOLLAR,
            PER,
            AND,
            STAR,
            PLUS,
            COMMA,
            MINUS,
            DOT,
            SLASH,
            COLON,
            SEMICOLON,
            EQ,
            QUESTION,
            AT,
            VAR,
            CHILDER,
            BKSLASH,
            HAT,
            UNDER
        }

        private static readonly CharType[] charTypeMatrix = {
	        CharType.NULL/*nul*/, CharType.UNDEF/*soh*/, CharType.UNDEF/*stx*/, CharType.UNDEF/*etx*/, CharType.UNDEF/*eot*/, CharType.UNDEF/*enq*/, CharType.UNDEF/*ack*/, CharType.UNDEF/*bel*/,
	        CharType.UNDEF/*bs*/,  CharType.TAB/*ht*/, CharType.NL/*nl*/, CharType.UNDEF/*vt*/, CharType.UNDEF/*np*/, CharType.UNDEF/*cr*/, CharType.UNDEF/*so*/, CharType.UNDEF/*si*/,
	        /*	020 dle  02CharType.UNDEF dcCharType.UNDEF  022 dc2  023 dc3  024 dc4  025 nak  026 syn  027 etb*/
	        CharType.UNDEF, CharType.UNDEF, CharType.UNDEF, CharType.UNDEF,     CharType.UNDEF, CharType.UNDEF, CharType.UNDEF, CharType.UNDEF,
	        /*	030 can  03CharType.UNDEF em   032 sub  033 esc  034 fs   035 gs   036 rs   037 us*/
	        CharType.UNDEF, CharType.UNDEF, CharType.UNDEF, CharType.UNDEF,     CharType.UNDEF, CharType.UNDEF, CharType.UNDEF, CharType.UNDEF,
	        /*040 sp   041  !   042  "   043  #   044  $   045  %   046  &   047  '*/
	        CharType.SP, CharType.OKIDOKI, CharType.DQUOTE, CharType.SHARP, CharType.DOLLAR, CharType.PER, CharType.AND, CharType.QUOTE,
	        /*050  (   051  )   052  *   053  +   054  ,   055  -   056  .   057  /*/
	        CharType.LPAR, CharType.RPAR, CharType.STAR, CharType.PLUS, CharType.COMMA, CharType.MINUS, CharType.DOT, CharType.SLASH,
	        /*060  0   061  1   062  2   063  3   064  4   065  5   066  6   067  7 */
	        CharType.DIGIT, CharType.DIGIT, CharType.DIGIT, CharType.DIGIT,  CharType.DIGIT, CharType.DIGIT, CharType.DIGIT, CharType.DIGIT,
	        /*	070  8   071  9   072  :   073  ;   074  <   075  =   076  >   077  ? */
	        CharType.DIGIT, CharType.DIGIT, CharType.COLON, CharType.SEMICOLON, CharType.LT, CharType.EQ, CharType.GT, CharType.QUESTION,
	        /*100  @   101  A   102  B   103  C   104  D   105  E   106  F   107  G */
	        CharType.AT, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA,
	        /*110  H   111  I   112  J   113  K   114  L   115  M   116  N   117  O */
	        CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA,
	        /*120  P   121  Q   122  R   123  S   124  T   125  U   126  V   127  W*/
	        CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.UALPHA,
	        /*130  X   131  Y   132  Z   133  [   134  \   135  ]   136  ^   137  CharType.*/
	        CharType.UALPHA, CharType.UALPHA, CharType.UALPHA, CharType.LSQ, CharType.BKSLASH, CharType.RSQ, CharType.HAT, CharType.UNDER,
	        /*140  `   141  a   142  b   143  c   144  d   145  e   146  f   147  g*/
	        CharType.BKQUOTE, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA,
	        /*150  h   151  i   152  j   153  k   154  l   155  m   156  n   157  o*/
	        CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA,
	        /*160  p   161  q   162  r   163  s   164  t   165  u   166  v   167  w*/
	        CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LALPHA,
	        /*170  x   171  y   172  z   173  {   174  |   175  }   176  ~   177 del*/
	        CharType.LALPHA, CharType.LALPHA, CharType.LALPHA, CharType.LBR, CharType.VAR, CharType.RBR, CharType.CHILDER, CharType.UNDEF,
        };

        private static FTokenizer[] tokenizerMatrix = {
            /* NULL */ TokenizeSkip,
            /* UNDEF */ TokenizeSkip,
            /* DIGIT */ TokenizeNumber,
            /* UALPHA */ TokenizeSymbol,
            /* LALPHA */ TokenizeSymbol,
            /* MULTI */ TokenizeSymbol,
            /* NL */ TokenizeNextline,
            /* TAB */ TokenizeSkip,
            /* SP */ TokenizeSkip,
            /* LPAR */ TokenizeOneCharOperator,
            /* RPAR */ TokenizeOneCharOperator,
            /* LSQ */ TokenizeOneCharOperator,
            /* RSQ */ TokenizeOneCharOperator,
            /* LBR */ TokenizeBlock,
            /* RBR */ TokenizeOneCharOperator,
            /* LT */ TokenizeOperator,
            /* GT */ TokenizeOperator,
            /* QUOTE */ TokenizeUndefined,
            /* DQUOTE */ TokenizeDoubleQuote,
            /* BKQUOTE */ TokenizeUndefined,
            /* OKIDOKI */ TokenizeOperator,
            /* SHARP */ TokenizeOperator,
            /* DOLLAR */ TokenizeOperator,
            /* PER */ TokenizeOperator,
            /* AND */ TokenizeOperator,
            /* STAR */ TokenizeOperator,
            /* PLUS */ TokenizeOperator,
            /* COMMA */ TokenizeOneCharOperator,
            /* MINUS */ TokenizeOperator,
            /* DOT */ TokenizeOperator,
            /* SLASH */ TokenizeSlash,
            /* COLON */ TokenizeOperator,
            /* SEMICOLON */ TokenizeOneCharOperator,
            /* EQ */ TokenizeOperator,
            /* QUESTION */ TokenizeOperator,
            /* AT */ TokenizeOneCharOperator,
            /* VAR */ TokenizeOperator,
            /* CHILDER */ TokenizeOperator,
            /* BKSLASH */ TokenizeUndefined,
            /* HAT */ TokenizeOperator,
            /* UNDER */ TokenizeSymbol,
        };

        static int TokenizeSkip(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            token = null;
            return ++tokStart;
        }

        static private bool IsNumChar(int c)
        {
            return '0' <= c && c <= '9';
        }
        static private bool IsHexNumChar(int c)
        {
            return ('0' <= c && c <= '9') | ('A' <= c && c <= 'F') | ('a' <= c && c <= 'f');
        }
        static private bool IsAlphaOrNum(int c)
        {
            return ('0' <= c && c <= '9') | ('A' <= c && c <= 'Z') | ('a' <= c && c <= 'z');
        }

        static private bool IsSymbolic(int c)
        {
            return IsAlphaOrNum(c) || c == '_';
        }

        static int TokenizeIndent(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            int pos = tokStart;
            string ts = tenv.Source;
            int indent = 0;

            if (pos < ts.Length)
            {
                char ch = ts[pos++];
                if (ch == '\t')
                {
                    indent += tenv.TabWidth;
                }
                else if (ch == ' ')
                {
                    indent += 1;
                }
                else
                {
                    --pos;
                }
            }
            token = null;

            return pos;
        }

        static int TokenizeNextline(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            int pos = tokStart;
            string ts = tenv.Source;

            while (pos < ts.Length)
            {
                if (ts[pos] == '\r')
                {
                    if (ts[pos + 1] == '\n')
                    {
                        ++pos;
                    }
                    ++pos;
                }
                else if (ts[pos] == '\n')
                {
                    ++pos;
                }
            }

            tenv.Line += 1;
            tenv.bol = pos;
            return TokenizeIndent(ctx, out token, tenv, pos, thunk);
        }

        static int TokenizeNumber(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            int pos = tokStart;
            bool dotAppeared = false;
            string ts = tenv.Source;

            while (pos < ts.Length)
            {
                char ch = ts[pos++];
                if (ch == '_') continue; // nothing
                if (ch == '.')
                {
                    if (!IsNumChar(ts[pos]))
                    {
                        --pos;
                        break;
                    }
                    dotAppeared = true;
                    continue;
                }
                if ((ch == 'e' || ch == 'E') && (ts[pos] == '+' || ts[pos] == '-'))
                {
                    pos++;
                    continue;
                }
                if (!IsAlphaOrNum(ch))
                {
                    --pos;
                    break;
                }
            }

            string str = ts.Substring(tokStart, pos - tokStart).Replace("_", "");
            if (dotAppeared)
            {
                token = new FloatToken(Convert.ToDouble(str));
            }
            else if(str.StartsWith("0x") || str.StartsWith("0X"))
            {
                token = new IntegerToken(Convert.ToInt64(str, 16));
            }
            else if(str.StartsWith("0"))
            {
                token = new IntegerToken(Convert.ToInt64(str, 8));
            }
            else
            {
                token = new IntegerToken(Convert.ToInt64(str, 10));
            }
            return pos;  // next
        }

        static int TokenizeSymbol(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            int pos = tokStart;
            string ts = tenv.Source;

            while (pos < ts.Length && IsSymbolic(ts[pos])) ++pos;

            token = new SymbolToken(ts.Substring(tokStart, pos - tokStart));
            return pos;
        }

        static int TokenizeOneCharOperator(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            token = new OperatorToken(tenv.Source.Substring(tokStart, 1));
            return ++tokStart;
        }

        static int TokenizeOperator(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            int pos = tokStart;
            string ts = tenv.Source;

            while (pos < ts.Length)
            {
                switch (ts[pos])
                {
                    case '<':
                    case '>':
                    case '@':
                    case '$':
                    case '#':
                    case '+':
                    case '-':
                    case '*':
                    case '%':
                    case '/':
                    case '=':
                    case '&':
                    case '?':
                    case ':':
                    case '.':
                    case '^':
                    case '!':
                    case '~':
                    case '|':
                        ++pos;
                        continue;
                }
                break;
            }
            token = new OperatorToken(ts.Substring(tokStart, pos - tokStart));
            return pos;
        }

        static int TokenizeLine(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            string ts = tenv.Source;
            int pos = tokStart;
            while (ts[pos] != '\n' || ts[pos] != '\r') ++pos;
            token = null;
            return pos;
        }

        static int TokenizeComment(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            string ts = tenv.Source;
            int pos = tokStart + 2;
            char ch = '\0';
            char prev = '\0';
            int level = 1;
            token = null;

            while (pos < ts.Length)
            {
                ch = ts[pos++];
                if (ch == '\r')
                {
                    tenv.Line += 1;
                    if(ts[pos] == '\n') ++pos;
                }else if (ch == '\n')
                {
                    tenv.Line += 1;
                }
                if (prev == '*' && ch == '/')
                {
                    level--;
                    if (level == 0) return pos;
                }
                else if (prev == '/' && ch == '*')
                {
                    level++;
                }
                prev = ch;
            }

            return pos;
        }

        static int TokenizeSlash(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            string ts = tenv.Source;
            if (ts[tokStart + 1] == '/')
            {
                return TokenizeLine(ctx, out token, tenv, tokStart, thunk);
            }
            else if (ts[tokStart + 1] == '*')
            {
                return TokenizeComment(ctx, out token, tenv, tokStart, thunk);
            }
            return TokenizeOperator(ctx, out token, tenv, tokStart, thunk);
        }

        static int TokenizeDoubleQuote(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            string ts = tenv.Source;
            char ch = '\0';
            char prev = '"';
            int pos = tokStart + 1;

            token = null;

            while (pos < ts.Length)
            {
                ch = ts[pos++];
                if (ch == '\n' || ch == '\r')
                {
                    break;
                }
                if (ch == '"' && prev != '\\')
                {
                    token = new StringToken(ts.Substring(tokStart + 1, (pos - 1) - (tokStart + 1)));
                    return pos;
                }
                prev = ch;
            }
            return pos - 1;
        }

        static int TokenizeUndefined(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            token = null;
            return tokStart;
        }

        static int TokenizeBlock(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, Method thunk)
        {
            string ts = tenv.Source;
            char ch = '\0';
            int pos = tokStart + 1;
            int level = 1;
            FTokenizer[] fmat = tenv.tokenizerMatrix;

            token = null;

            while (pos < ts.Length)
            {
                ch = ts[pos++];
                if (ch == '}')
                {
                    level--;
                    if (level == 0)
                    {
                        token = new CodeToken(ts.Substring(tokStart + 1, ((pos - 2) - (tokStart) + 1)));
                        return pos + 1;
                    }
                    pos++;
                }
                else if (ch == '{')
                {
                    level++;
                    pos++;
                }
                else
                {
                    pos = fmat[ch](ctx, out token, tenv, pos, null);
                }
            }
            return pos;
        }

        public Lexer(String str)
        {
            this.env = new TokenizerEnvironment();
            env.tokenizerMatrix = tokenizerMatrix;
            env.Source = str;
            Tokenize();
        }

        private List<Token> tokens = new List<Token>();

        private void Tokenize()
        {
            FTokenizer[] fmat = env.tokenizerMatrix;
            Token token;

            for (int pos = TokenizeIndent(null, out token, this.env, 0, null); pos < env.Source.Length; )
            {
                CharType ct = charTypeMatrix[env.Source[pos]];
                int pos2 = fmat[(int)ct](null, out token, this.env, pos, null);
                Debug.Assert(pos2 > pos);
                pos = pos2;
                if (token != null)
                {
                    tokens.Add(token);
                }
            }
        }

        private TokenizerEnvironment env;
    }
}
