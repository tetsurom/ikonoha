using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace IronKonoha
{
    class Lexer
    {
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

        public delegate Token FTokenizer(TextReader reader);

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
            /* UALPHA */ TokenizeSkip,
            /* LALPHA */ TokenizeSkip,
            /* MULTI */ TokenizeSkip,
            /* NL */ TokenizeSkip,
            /* TAB */ TokenizeSkip,
            /* SP */ TokenizeSkip,
            /* LPAR */ TokenizeSkip,
            /* RPAR */ TokenizeSkip,
            /* LSQ */ TokenizeSkip,
            /* RSQ */ TokenizeSkip,
            /* LBR */ TokenizeSkip,
            /* RBR */ TokenizeSkip,
            /* LT */ TokenizeSkip,
            /* GT */ TokenizeSkip,
            /* QUOTE */ TokenizeSkip,
            /* DQUOTE */ TokenizeSkip,
            /* BKQUOTE */ TokenizeSkip,
            /* OKIDOKI */ TokenizeSkip,
            /* SHARP */ TokenizeSkip,
            /* DOLLAR */ TokenizeSkip,
            /* PER */ TokenizeSkip,
            /* AND */ TokenizeSkip,
            /* STAR */ TokenizeSkip,
            /* PLUS */ TokenizeSkip,
            /* COMMA */ TokenizeSkip,
            /* MINUS */ TokenizeSkip,
            /* DOT */ TokenizeSkip,
            /* SLASH */ TokenizeSkip,
            /* COLON */ TokenizeSkip,
            /* SEMICOLON */ TokenizeSkip,
            /* EQ */ TokenizeSkip,
            /* QUESTION */ TokenizeSkip,
            /* AT */ TokenizeSkip,
            /* VAR */ TokenizeSkip,
            /* CHILDER */ TokenizeSkip,
            /* BKSLASH */ TokenizeSkip,
            /* HAT */ TokenizeSkip,
            /* UNDER */ TokenizeSkip,
        };

        static Token TokenizeSkip(TextReader reader)
        {
            reader.Read();
            return null;
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

        static Token TokenizeNumber(TextReader reader)
        {
            bool isFloat = false;
            var sbuild = new StringBuilder();
            for (int ch = reader.Read(); ch >= 0; ch = reader.Read()){
		        if (ch == '_') continue; // nothing
                sbuild.Append((char)ch);

		        if(ch == '.') {
                    if (!IsNumChar(reader.Peek()))
                    {
                        //reader.
				        break;
			        }
			        isFloat = true;
			        continue;
		        }
                if ((ch == 'e' || ch == 'E') && (reader.Peek() == '+' || reader.Peek() == '-'))
                {
                    sbuild.Append((char)reader.Read());
			        continue;
		        }

                if (!IsAlphaOrNum(ch)) break;
	        }
            string str = sbuild.ToString();
            if (isFloat)
            {
                return new FloatToken(Convert.ToDouble(str));
            }
            else if(str.StartsWith("0x") || str.StartsWith("0X"))
            {
                return new IntegerToken(Convert.ToInt64(str, 16));
            }
            else if(str.StartsWith("0"))
            {
                return new IntegerToken(Convert.ToInt64(str, 8));
            }
            else
            {
                return new IntegerToken(Convert.ToInt64(str, 10));
            }
            return null;
        }

        private TextReader reader;

        public Lexer(TextReader reader)
        {
            this.reader = reader;
            Tokenize();
        }

        private List<Token> tokens = new List<Token>();

        private void Tokenize()
        {
            for (int ch = reader.Peek(); ch >= 0; ch = reader.Peek())
            {
                Token token = tokenizerMatrix[(int)charTypeMatrix[ch]](reader);
                if (token != null)
                {
                    tokens.Add(token);
                }
            }
        }

    }
}
