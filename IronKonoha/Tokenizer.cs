using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace IronKonoha
{

	[Flags]
	public enum KFunkFlag
	{

	}
	/// <summary>
	/// temporaly
	/// </summary>
	public class KFunk
	{
		public static readonly KFunk NoName = new KFunk();
		public KFunk(KFunkFlag flag, KType cid, string name, IList<KStatement> param)
		{

		}
		public KFunk()
		{

		}
	}

	public class KFunk<D> : KFunk
	{
		public D Delegate { get; set; }

	}

	public enum KeywordType
	{
		Err,
		Expr,
		Symbol,
		Usymbol,
		Text,
		TKInt,
		TKFloat,
		Type,
		Parenthesis,
		Bracket,
		Brace,
		StmtTypeDecl,
		Block,
		Params,
		ExprMethodCall,
		Toks,
		DOT,
		DIV,
		MOD,
		MUL,
		ADD,
		SUB,
		LT,
		LTE,
		GT,
		GTE,
		EQ,
		NEQ,
		AND,
		OR,
		NOT,
		COLON,
		LET,
		COMMA,
		DOLLAR,
		Void,
		StmtMethodDecl,
		Boolean,
		Int,
		Null,
		True,
		False,
		If,
		Else,
		Return
	}

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
		MN,
		AST_OPTIONAL      // for syntax sugar
	}

	public class KType
	{
		private static readonly Dictionary<BCID, KType> bcidMap = new Dictionary<BCID, KType>();
		public static readonly KType Unknown = new KType();
		public static readonly KType Void = KType.FromBCID(BCID.CLASS_Tvoid);
		public static readonly KType Int = KType.FromBCID(BCID.CLASS_Int);
		public static readonly KType Boolean = KType.FromBCID(BCID.CLASS_Boolean);
		public static readonly KType System = KType.FromBCID(BCID.CLASS_System);

		public static KType FromBCID(BCID bcid){
			if (!bcidMap.ContainsKey(bcid))
			{
				bcidMap.Add(bcid, new KType());
			}
			return bcidMap[bcid];
		}
	}

	[System.Diagnostics.DebuggerDisplay("{Text,nq} [{Type}]")]
	public class Token
	{

		public TokenType Type { get; set; }
		public string Text { get; private set; }
		public IList<Token> Sub { get; set; }
		public char TopChar { get { return Text.Length == 0 ? '\0' : this.Text[0]; } }
		public KeywordType Keyword { get; set; }
		public KType KType { get; set; }
		public LineInfo ULine { get; set; }

		public Token(TokenType type, string text, int lpos)
		{
			this.Type = type;
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
			this.Type = TokenType.ERR;
			this.Text = ctx.ctxsugar.errors.strings[(int)errorcode];
		}

		public bool IsType { get { return Keyword == KeywordType.Type; } }


		// static kbool_t Token_resolved(CTX, kKonohaSpace *ks, struct _kToken *tk)
		public bool IsResolved(Context ctx, KonohaSpace ks)
		{
			KKeyWord kw = ctx.kmodsugar.keyword_(this.Text, null);
			if (kw != null && kw != Symbol.NONAME)
			{
				Syntax syn = ks.GetSyntax(kw.Type);
				if (syn != null)
				{
					if (syn.Type != KType.Unknown)
					{
						this.Keyword = KeywordType.Type;
						this.Type = TokenType.TYPE;
						this.KType = syn.Type;
					}
					else
					{
						this.Keyword = kw.Type;
					}
					return true;
				}
			}
			return false;
		}

		// Token_resolveType(CTX, ...) // old
		// static struct _kToken* TokenType_resolveGenerics(CTX, kKonohaSpace *ks, struct _kToken *tk, kToken *tkP)
		internal bool ResolveGenerics(Context ctx, Token tkP)
		{
			if(tkP.Type == TokenType.AST_BRACKET) {
				int i;
				
				int size = tkP.Sub.Count;
				List<KParam> p = new List<KParam>(size);
				for(i = 0; i < size; i++) {
					Token tkT = (tkP.Sub[i]);
					if(tkT.IsType) {
						p.Add(new KParam() { ty = tkT.KType });
						continue;
					}
					if(tkT.TopChar == ',') continue;
					//return NULL; // new int[10];  // not generics
					return false;
				}
				int psize = p.Count;
				KClass ct = null;
				if(psize > 0) {
					ct = ctx.CT_(this.KType);
					if (ct.bcid == BCID.CLASS_Func)
					{
						ct = KClassTable.Generics(ctx, ct, p[0].ty, p.Skip(1).ToList());
					}
					else if(ct.p0 == KType.Void) {
						ctx.SUGAR_P(ReportLevel.ERR, this.ULine, 0, "not generic type: {0}", this.KType);
						//return tk;
						return true;
					}
					else {
						ct = KClassTable.Generics(ctx, ct, KType.Void, p);
					}
 				}
				else {
					var p0 = new List<KParam>();
					p0.Add(new KParam() { ty = this.KType, fn = null });
					ct = KClassTable.Generics(ctx, ctx.CT_(BCID.CLASS_Array), this.KType, p0);
 				}
				this.KType = ct.cid;
				//return tk;
				return true;
 			}
			//return NULL;
			return false;
		}

		public void Print(Context ctx, ReportLevel pe, string fmt, params object[] ap)
		{
			ctx.SUGAR_P(pe, this.ULine, 0, fmt, ap);
		}

		public Symbol nameid { get; set; }
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
		public delegate int FTokenizer(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk);

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

		static int TokenizeSkip(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeIndent(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeNextline(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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
			return TokenizeIndent(ctx, out token, tenv, pos, thunk);
		}

		static int TokenizeNumber(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeSymbol(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
		{
			int pos = tokStart;
			string ts = tenv.Source;

			while (pos < ts.Length && IsSymbolic(ts[pos]))
				++pos;

			token = new Token(TokenType.SYMBOL, ts.Substring(tokStart, pos - tokStart), tokStart);
			return pos;
		}

		static int TokenizeOneCharOperator(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
		{
			token = new Token(TokenType.OPERATOR, tenv.Source.Substring(tokStart, 1), tokStart);
			return ++tokStart;
		}

		static int TokenizeOperator(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeLine(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
		{
			string ts = tenv.Source;
			int pos = tokStart;
			while (ts[pos] != '\n' || ts[pos] != '\r')
				++pos;
			token = null;
			return pos;
		}

		static int TokenizeComment(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeSlash(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeDoubleQuote(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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

		static int TokenizeUndefined(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
		{
			token = null;
			return tokStart;
		}

		static int TokenizeBlock(Context ctx, out Token token, TokenizerEnvironment tenv, int tokStart, KFunk thunk)
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
		private KonohaSpace ks;

		public Tokenizer(Context ctx, KonohaSpace ks)
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
			Token token;

			for (int pos = TokenizeIndent(this.ctx, out token, env, 0, null); pos < env.Source.Length; )
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

			foreach (var tk in tokens)
			{
				Console.WriteLine("{0} [{1}]", tk.Text, tk.Type);
			}

			return tokens;
		}

	}
}
