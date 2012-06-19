using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace IronKonoha
{
	/// <summary>
	/// Create Konoha AST from sourcecode.
	/// </summary>
	public class Parser
	{

		private Context ctx;
		private KonohaSpace ks;

		public Parser(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}
		/*
		public KonohaExpr ParseExpr(String str)
		{
			if (str == null)
			{
				throw new ArgumentException("str must not be null.");
			}
			return null;
			//return ParseExprAux(new Tokenizer(str));
		}
		
		private KonohaExpr ParseExprAux(Tokenizer lexer)
		{
			return null;
			//throw new NotImplementedException();
		}
		*/

		/// <summary>
		/// トークン列をパースしてブロックを得る
		/// </summary>
		/// <param name="parent">親のステートメント</param>
		/// <param name="token">トークン列</param>
		/// <param name="start">開始トークン位置</param>
		/// <param name="end">終了トークンの次の位置</param>
		/// <param name="delim">デリミタ</param>
		/// <returns></returns>
		// static kBlock *new_Block(CTX, kKonohaSpace *ks, kStmt *parent, kArray *tls, int s, int e, int delim)
		public BlockExpr CreateBlock(ExprOrStmt parent, IList<Token> token, int start, int end, char delim)
		{
			BlockExpr block = new BlockExpr();
			block.parent = parent;
			int indent = 0;
			int atop = token.Count;
			for (int i = start; i < end; )
			{
				Token error;
				Debug.Assert(atop == token.Count);
				i = SelectStatementLine(ref indent, token, i, end, delim, token, out error);
				int asize = token.Count;
				if (asize > atop)
				{
					block.AddStatementLine(ctx, ks, token, atop, asize, out error);
				}
			}
			return block;
		}

		// static int selectStmtLine(CTX, kKonohaSpace *ks, int *indent, kArray *tls, int s, int e, int delim, kArray *tlsdst, kToken **tkERRRef)
		private int SelectStatementLine(ref int indent, IList<Token> tokens, int start, int end, char delim, IList<Token> tokensDst, out Token errorToken)
		{
			int i = start;
			Debug.Assert(end <= tokens.Count);
			for (; i < end - 1; i++)
			{
				Token tk = tokens[i];
				Token tk1 = tokens[i + 1];
				if (tk.Keyword != 0)
					break;  // already parsed
				if (tk.TopChar == '@' && (tk1.TokenType == TokenType.SYMBOL || tk1.TokenType == TokenType.USYMBOL))
				{
					tk1.TokenType = TokenType.METANAME;
					tk1.Keyword = 0;
					tokensDst.Add(tk1);
					i++;
					if (i + 1 < end && tokens[i + 1].TopChar == '(')
					{
						i = makeTree(TokenType.AST_PARENTHESIS, tokens, i + 1, end, ')', tokensDst, out errorToken);
					}
					continue;
				}
				if (tk.TokenType == TokenType.METANAME)
				{  // already parsed
					tokensDst.Add(tk);
					if (tk1.TokenType == TokenType.AST_PARENTHESIS)
					{
						tokensDst.Add(tk1);
						i++;
					}
					continue;
				}
				if (tk.TokenType != TokenType.INDENT)
					break;
				if (indent == 0)
					indent = tk.Text.Length;
			}
			for (; i < end; i++)
			{
				var tk = tokens[i];
				if (tk.TopChar == delim && tk.TokenType == TokenType.OPERATOR)
				{
					errorToken = null;
					return i + 1;
				}
				if (tk.Keyword != 0)
				{
					tokensDst.Add(tk);
					continue;
				}
				else if (tk.TopChar == '(')
				{
					i = makeTree(TokenType.AST_PARENTHESIS, tokens, i, end, ')', tokensDst, out errorToken);
					tk.Keyword = KeywordType.Parenthesis;
					continue;
				}
				else if (tk.TopChar == '[')
				{
					i = makeTree(TokenType.AST_BRACKET, tokens, i, end, ']', tokensDst, out errorToken);
					tk.Keyword = KeywordType.Bracket;
					continue;
				}
				else if (tk.TokenType == TokenType.ERR)
				{
					errorToken = tk;
				}
				if (tk.TokenType == TokenType.INDENT)
				{
					if (tk.Text.Length <= indent)
					{
						Debug.WriteLine(string.Format("tk.Lpos={0}, indent={1}", tk.Text.Length, indent));
						errorToken = null;
						return i + 1;
					}
					continue;
				}
				i = appendKeyword(tokens, i, end, tokensDst, out errorToken);
			}
			errorToken = null;
			return i;
		}

		// static int makeTree(CTX, kKonohaSpace *ks, ktoken_t tt, kArray *tls, int s, int e, int closech, kArray *tlsdst, kToken **tkERRRef)
		private int makeTree(TokenType tokentype, IList<Token> tokens, int start, int end, char closeChar, IList<Token> tokensDst, out Token errorToken)
		{
			int i, probablyCloseBefore = end - 1;
			Token tk = tokens[start];
			Debug.Assert(tk.Keyword == 0);

			Token tkP = new Token(tokentype, tk.Text, closeChar) { Keyword = (KeywordType)tokentype };
			tokensDst.Add(tkP);
			tkP.Sub = new List<Token>();
			for (i = start + 1; i < end; i++)
			{
				tk = tokens[i];
				Debug.Assert(tk.Keyword == 0);
				if (tk.TokenType == TokenType.ERR)
					break;  // ERR
				Debug.Assert(tk.TopChar != '{');
				if (tk.TopChar == '(')
				{
					i = makeTree(TokenType.AST_PARENTHESIS, tokens, i, end, ')', tkP.Sub, out errorToken);
					tk.Keyword = KeywordType.Parenthesis;
					continue;
				}
				else if (tk.TopChar == '[')
				{
					i = makeTree(TokenType.AST_BRACKET, tokens, i, end, ']', tkP.Sub, out errorToken);
					tk.Keyword = KeywordType.Bracket;
					continue;
				}
				else if (tk.TopChar == closeChar)
				{
					errorToken = null;
					return i;
				}
				if ((closeChar == ')' || closeChar == ']') && tk.TokenType == TokenType.CODE)
					probablyCloseBefore = i;
				if (tk.TokenType == TokenType.INDENT && closeChar != '}')
					continue;  // remove INDENT;
				i = appendKeyword(tokens, i, end, tkP.Sub, out errorToken);
			}
			if (tk.TokenType != TokenType.ERR)
			{
				uint errref = ctx.SUGAR_P(ReportLevel.ERR, tk.ULine, 0, "'{0}' is expected (probably before {1})", closeChar.ToString(), tokens[probablyCloseBefore].Text);
				tkP.toERR(this.ctx, errref);
			}
			else
			{
				tkP.TokenType = TokenType.ERR;
			}
			errorToken = tkP;
			return end;
		}

		//static int appendKeyword(CTX, kKonohaSpace *ks, kArray *tls, int s, int e, kArray *dst, kToken **tkERR)
		private int appendKeyword(IList<Token> tls, int start, int end, IList<Token> dst, out Token errorToken)
		{
			int next = start; // don't add
			Token tk = tls[start];
			if (tk.TokenType < TokenType.OPERATOR)
			{
				tk.Keyword = (KeywordType)tk.TokenType;
			}
			if (tk.TokenType == TokenType.SYMBOL)
			{
				tk.IsResolved(ctx, ks);
			}
			else if (tk.TokenType == TokenType.USYMBOL)
			{
				if (!tk.IsResolved(ctx, ks))
				{
					throw new NotImplementedException();
					//KonohaClass ct = kKonohaSpace_getCT(ks, null/*FIXME*/, tk.Text, tk.Text.Length, TY_unknown);
					//object ct = null;
					//if (ct != null)
					//{
					//	tk.Keyword = KeywordType.Type;
						//tk.Type = ct->cid;
					//}
				}
			}
			else if (tk.TokenType == TokenType.OPERATOR)
			{
				if (!tk.IsResolved(ctx, ks))
				{
					uint errref = ctx.SUGAR_P(ReportLevel.ERR, tk.ULine, 0, "undefined token: {0}", tk.Text);
					tk.toERR(this.ctx, errref);
					errorToken = tk;
					return end;
				}
			}
			else if (tk.TokenType == TokenType.CODE)
			{
				tk.Keyword = KeywordType.Brace;
			}
			if (tk.IsType)
			{
				dst.Add(tk);
				while (next + 1 < end)
				{
					Token tkB = tls[next + 1];
					if (tkB.TopChar != '[')
					{
						break;
					}
					List<Token> abuf = new List<Token>();
					int atop = abuf.Count;
					next = makeTree(TokenType.AST_BRACKET, tls, next + 1, end, ']', abuf, out errorToken);
					if (abuf.Count <= atop)
					{
						return next;
					}
					else
					{
						tkB = abuf[atop];
						tk.ResolveGenerics(this.ctx, tkB);
					}
				}
			}
			else if (tk.Keyword > KeywordType.Expr)
			{
				dst.Add(tk);
			}
			errorToken = null;
			return next;
		}

	}

	public class KParam
	{
		public static readonly KParam NULL;
		public KType ty { get; set; }
		public Symbol fn { get; set; }
		public TokenType Type { get; set; }
	}
	[Obsolete]
	public class BCID{
		public static readonly Type CLASS_Tvoid = typeof(void);
		public static readonly Type CLASS_Tvar = typeof(Variant);
		public static readonly Type CLASS_Object = typeof(object);
		public static readonly Type CLASS_Boolean = typeof(bool);
		public static readonly Type CLASS_Int = typeof(int);
		public static readonly Type CLASS_String = typeof(string);
		public static readonly Type CLASS_Array = typeof(Array);
		public static readonly Type CLASS_Param = typeof(KParam);
		public static readonly Type CLASS_Method = typeof(Delegate);
		public static readonly Type CLASS_Func = typeof(Delegate);
		public static readonly Type CLASS_System = typeof(object);
		public static readonly Type CLASS_T0 = typeof(object);
	}

	public class KParamID
	{

	}
	[Obsolete]
	public class KDEFINE_CLASS{

	}

	public class KLine
	{

	}

	[Obsolete]
	[Flags]
	public enum KClassFlag
	{
		Ref             = (1<<0),
		Prototype       = (1<<1),
		Immutable       = (1<<2),
		Private         = (1<<4),
		Final           = (1<<5),
		Singleton       = (1<<6),
		UnboxType       = (1<<7),
		Interface       = (1<<8),
		TypeVar         = (1<<9),
	}

	// konoha2.h
	// _kclass_t
	[Obsolete]
	public class KClass
	{
		public KType cid { get; set; }
		public BCID bcid { get; set; }
		public KType p0 { get; set; }
		public KParam cparam { get; set; }
		public KParamID paramdom { get; set; }
		public KClassFlag flag { get; set; }
		public KClass searchSimilarClassNULL { get; set; }

		public KClass(){

		}
		// datatype.h
		// static struct _kclass* new_CT(CTX, kclass_t *bct, KDEFINE_CLASS *s, kline_t pline)
		public KClass(Context ctx, KClass bct, KDEFINE_CLASS s, KLine kline)
		{
			throw new NotImplementedException();
		}

		public KClass searchSuperMethodClassNULL { get; set; }

		public object packdom { get; set; }

		public bool isUnbox { get { return (flag & KClassFlag.UnboxType) != 0; } }
	}


}
