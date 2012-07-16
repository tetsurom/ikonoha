using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace IronKonoha
{
	public enum TokenType
	{
		NONE,          // KW_Err
		INDENT,        // KW_Expr
		SYMBOL,        // KW_Symbol
		USYMBOL,       // KW_Usymbol
		TEXT,          // KW_Text
		INT,           // KW_Int
		FLOAT,         // KW_Float
		TYPE,          // KW_Type
		AST_PARENTHESIS,  // KW_Parenthesis
		AST_BRACKET,      // KW_Brancet
		AST_BRACE,        // KW_Brace

		OPERATOR,
		MSYMBOL,       //
		ERR,           //
		CODE,          //
		WHITESPACE,    //
		METANAME,
		MethodName,
		AST_OPTIONAL      // for syntax sugar
	}

	class Variant
	{

	}

	[Obsolete]
	[System.Diagnostics.DebuggerDisplay("{bcid}")]
	public class KType
	{
		private static readonly Dictionary<BCID, KType> bcidMap = new Dictionary<BCID, KType>();
		public static readonly Type Unknown = null;
		public static readonly Type Void = typeof(void);
		public static readonly Type Int = typeof(int);
		public static readonly Type Boolean = typeof(bool);
		public static readonly Type System = typeof(IronKonoha.Runtime.System);
		public static readonly Type TVar = typeof(Variant);

		public static KType FromBCID(BCID bcid){
			if (!bcidMap.ContainsKey(bcid))
			{
				bcidMap.Add(bcid, new KType() { bcid = bcid });
			}
			return bcidMap[bcid];
		}

		public BCID bcid { get; private set; }

		public bool isUnbox
		{
			get
			{
				return true;
			}
		}
	}

	[System.Diagnostics.DebuggerDisplay("{Text,nq} [{TokenType}, {Type}]")]
	public class Token
	{

		/// <summary>
		/// トークンの種類
		/// </summary>
		[Obsolete]
		public TokenType TokenType { get; set; }
		public string Text { get; private set; }
		public IList<Token> Sub { get; set; }
		[Obsolete]
		public char TopChar { get { return Text.Length == 0 ? '\0' : this.Text[0]; } }
		public int Indent {get; set;}

		private KKeyWord _keyword;
		public KKeyWord Keyword
		{
			get
			{
				return _keyword ?? KeyWordTable.Map[0];
			}
			set
			{
				Debug.Assert(value != null);
				_keyword = value ?? KeyWordTable.Map[0];
			}
		}
		/// <summary>
		/// トークンが表す型
		/// </summary>
		public KonohaType Type { get; set; }
		public LineInfo ULine { get; set; }

		public Token(TokenType type, string text, int lpos)
		{
			this.TokenType = type;
			this.Text = text;
		}

		public override string ToString()
		{
			return Text;
		}

		// static void Token_toERR(CTX, struct _kToken *tk, size_t errref)
		[Obsolete]
		public void toERR(Context ctx, uint errorcode)
		{
			this.TokenType = TokenType.ERR;
			this.Text = ctx.ctxsugar.errors.strings[(int)errorcode];
		}

		public bool IsType { get { return Keyword.Type == KeywordType.Type; } }


		// static kbool_t Token_resolved(CTX, kKonohaSpace *ks, struct _kToken *tk)
		public bool Resolve(Context ctx, KNameSpace ks)
		{
			KKeyWord kw = ctx.kmodsugar.keyword_(this.Text);
			if (kw != null && kw.Name != null && kw.Name != string.Empty)
			{
				Syntax syn = ks.GetSyntax(kw);
				if (syn != null)
				{
					if (syn.Type != null)
					{
						this.Keyword = KeyWordTable.Type;
						this.TokenType = TokenType.TYPE;
						this.Type = syn.Type;
					}
					else
					{
						Debug.Assert(kw != null);
						if (ks.Classes.ContainsKey(this.Text))
						{
							//throw new InvalidOperationException(string.Format("undefined Type: {0}", this.Text));
							this.Type = ks.Classes[this.Text];
						}
						this.Keyword = kw;
					}
					return true;
				}
			}
			// FIXME: CKonohaにはないif文
			if (ks.Classes.ContainsKey(this.Text))
			{
				//throw new InvalidOperationException(string.Format("undefined Type: {0}", this.Text));
				this.Type = ks.Classes[this.Text];
				this.Keyword = KeyWordTable.Type;
				return true;
			}
			return false;
		}

		// Token_resolveType(CTX, ...) // old
		// static struct _kToken* TokenType_resolveGenerics(CTX, kKonohaSpace *ks, struct _kToken *tk, kToken *tkP)
		internal bool ResolveGenerics(Context ctx, Token tkP)
		{
			if(tkP.TokenType == TokenType.AST_BRACKET) {
				int i;
				
				int size = tkP.Sub.Count;
				var p = new List<KonohaType>(size);
				for(i = 0; i < size; i++) {
					Token tkT = (tkP.Sub[i]);
					if(tkT.IsType) {
						p.Add(tkT.Type);
						continue;
					}
					if(tkT.TopChar == ',') continue;
					return false;
				}
				int psize = p.Count;
				KonohaType ct = this.Type;
				if(psize > 0) {
					if (ct is TypeWrapper && ((TypeWrapper)ct).Type == typeof(Delegate))
					{
						ct = KClassTable.Generics(ct, p[0], p.Skip(1).ToList());
					}
					else if(!ct.IsGenericType) {
						ctx.SUGAR_P(ReportLevel.ERR, this.ULine, 0, "not generic type: {0}", this.Type);
						//return tk;
						return true;
					}
					else {
						ct = KClassTable.Generics(ct, null, p);
					}
 				}
				else {
					ct = this.Type.MakeArrayType();
 				}
				this.Type = ct;
				return true;
 			}
			return false;
		}

		public void Print(Context ctx, ReportLevel pe, string fmt, params object[] ap)
		{
			ctx.SUGAR_P(pe, this.ULine, 0, fmt, ap);
		}

		public Symbol nameid { get; set; }

		public bool toBrace(Context ctx, KNameSpace ks)
		{
			if (TokenType == TokenType.CODE)
			{
				this.Sub = new Tokenizer(ctx, ks).Tokenize(this.Text);
				this.TokenType = TokenType.AST_BRACE;
				return true;
			}
			return false;
		}

	};

	class Tokenizer
	{
		/// <summary>
		/// temporaly
		/// </summary>
		internal class TokenizerEnvironment
		{
			public string Source { get; set; }

			public FTokenizer[] TokenizerMatrix { get; set; }

			public int TabWidth { get; set; }
			/// <summary>
			/// 現在の行
			/// </summary>
			public LineInfo Line { get; set; }
			/// <summary>
			/// 現在の行が始まる位置
			/// </summary>
			public int Bol { get; set; }
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
		public delegate int FTokenizer(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk);

		#region 定数

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

		#endregion

		#region トークナイズ関数郡

		static int TokenizeSkip(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
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

		static int TokenizeIndent(Context ctx, ref Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
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
			//token = null;
			if(token != null) {
				token.Indent = 0;
			}
			return pos;
		}

		static int TokenizeNextline(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			int pos = tokStart;
			string ts = tenv.Source;

			if (pos < ts.Length)
			{
				if (ts[pos] == '\r')
				{
                    ++pos;
				}
				if (ts[pos] == '\n')
				{
					++pos;
				}
			}

			tenv.Line.LineNumber += 1;
			tenv.Bol = pos;
			token = new Token(TokenType.INDENT,"",0);
			return TokenizeIndent(ctx, ref token, tenv, pos, thunk);
		}

		static int TokenizeNumber(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			int pos = tokStart;
			bool dotAppeared = false;
			string ts = tenv.Source;

			while (pos < ts.Length)
			{
				char ch = ts[pos++];
				if (ch == '_')
					continue; // nothing
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
				token = new Token(TokenType.FLOAT, str, tokStart);
			}
			else
			{
				token = new Token(TokenType.INT, str, tokStart);
			}
			return pos;  // next
		}

		static int TokenizeSymbol(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			int pos = tokStart;
			string ts = tenv.Source;

			while (pos < ts.Length && IsSymbolic(ts[pos]))
				++pos;

			token = new Token(TokenType.SYMBOL, ts.Substring(tokStart, pos - tokStart), tokStart);
			return pos;
		}

		static int TokenizeOneCharOperator(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			token = new Token(TokenType.OPERATOR, tenv.Source.Substring(tokStart, 1), tokStart);
			return ++tokStart;
		}

		static int TokenizeOperator(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
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
			token = new Token(TokenType.OPERATOR, ts.Substring(tokStart, pos - tokStart), tokStart);
			return pos;
		}

		static int TokenizeLine(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			string ts = tenv.Source;
			int pos = tokStart;
			while (ts[pos] != '\n' && ts[pos] != '\r')
				++pos;
			token = null;
			return pos;
		}

		static int TokenizeComment(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
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
					tenv.Line.LineNumber += 1;
					if (ts[pos] == '\n')
					{
						++pos;
					}
				}
				else if (ch == '\n')
				{
					tenv.Line.LineNumber += 1;
				}
				if (prev == '*' && ch == '/')
				{
					level--;
					if (level == 0)
					{
						return pos;
					}
				}
				else if (prev == '/' && ch == '*')
				{
					level++;
				}
				prev = ch;
			}

			return pos;
		}

		static int TokenizeSlash(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
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

		static int TokenizeDoubleQuote(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
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
					token = new Token(TokenType.TEXT, ts.Substring(tokStart + 1, (pos - 1) - (tokStart + 1)), tokStart + 1);
					return pos;
				}
				if (ch == '\\' && pos < ts.Length)
				{
					switch (ts[pos])
					{
						case 'n': ch = '\n'; pos++; break;
						case 't': ch = '\t'; pos++; break;
						case 'r': ch = '\r'; pos++; break;
					}
				}
				prev = ch;
			}
			return pos - 1;
		}

		static int TokenizeUndefined(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			token = null;
			return tokStart;
		}

		static int TokenizeBlock(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunc thunk)
		{
			string ts = tenv.Source;
			char ch = '\0';
			int pos = tokStart + 1;
			int level = 1;
			FTokenizer[] fmat = tenv.TokenizerMatrix;

			token = null;

			while (pos < ts.Length)
			{
				ch = ts[pos];
				if (ch == '}')
				{
					level--;
					if (level == 0)
					{
						token = new Token(TokenType.CODE, ts.Substring(tokStart + 1, pos - 1 - tokStart), tokStart + 1);
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
					var f = fmat[(int)charTypeMatrix[ch]];
					pos = f(ctx, out token, tenv, pos, null);
				}
			}
			return pos;
		}

		#endregion

		private Context ctx;
		private KNameSpace ks;

		public Tokenizer(Context ctx, KNameSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}

		public IList<Token> Tokenize(String script)
		{
			var env = new TokenizerEnvironment()
			{
				TokenizerMatrix = tokenizerMatrix,//ks == null ? tokenizerMatrix : ks.TokenizerMatrix,
				Source = script,
				Line = new LineInfo(0, "")
			};

			FTokenizer[] fmat = env.TokenizerMatrix;
			var tokens = new List<Token>();
			Token token = null;

			for (int pos = TokenizeIndent(this.ctx, ref token, env, 0, null); pos < env.Source.Length; )
			{
				CharType ct = charTypeMatrix[env.Source[pos]];
				int pos2 = fmat[(int)ct](this.ctx, out token, env, pos, null);
				Debug.Assert(pos2 > pos);
				pos = pos2;
				if (token != null)
				{
					token.ULine = new LineInfo(env.Line.LineNumber, env.Line.Filename);
					tokens.Add(token);
				}
			}

			//foreach (var tk in tokens)
			//{
			//    Console.WriteLine("{0} [{1}]", tk.Text, tk.TokenType);
			//}

			return tokens;
		}

	}
}
