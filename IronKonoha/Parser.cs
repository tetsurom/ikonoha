using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace IronKonoha
{
	public enum SymPol{
		RAW,
		NAME,
		MsETHOD,
	}

	[System.Diagnostics.DebuggerDisplay("{Name,nq}")]
	public class Symbol
	{
		public static readonly Symbol NewID = new Symbol();
		public static readonly Symbol NONAME = new Symbol();
		public string Name { get; private set; }

		public Symbol(){
		}
		public static Symbol Get(Context ctx, string name, Symbol def, SymPol pol)
		{
			/*if (pol == SYMPOL_RAW)
			{
				return ctx.share.SymbolMap[name];
			}
			else
			{
				ksymbol_t sym, mask = 0;
				name = ksymbol_norm(buf, name, &len, &hcode, &mask, pol);
				sym = Kmap_getcode(_ctx, _ctx->share->symbolMapNN, _ctx->share->symbolList, name, len, hcode, SPOL_ASCII, def);
				if(def == sym) return def;
				return sym | mask;
			}*/
			if (!ctx.share.SymbolMap.ContainsKey(name))
			{
				ctx.share.SymbolMap.Add(name, new Symbol() { Name = name });
			}
			return ctx.share.SymbolMap[name];
		}
	}

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
		public BlockExpr CreateBlock(KonohaExpr parent, IList<Token> token, int start, int end, char delim)
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
				if (tk.TopChar == '@' && (tk1.Type == TokenType.SYMBOL || tk1.Type == TokenType.USYMBOL))
				{
					tk1.Type = TokenType.METANAME;
					tk1.Keyword = 0;
					tokensDst.Add(tk1);
					i++;
					if (i + 1 < end && tokens[i + 1].TopChar == '(')
					{
						i = makeTree(TokenType.AST_PARENTHESIS, tokens, i + 1, end, ')', tokensDst, out errorToken);
					}
					continue;
				}
				if (tk.Type == TokenType.METANAME)
				{  // already parsed
					tokensDst.Add(tk);
					if (tk1.Type == TokenType.AST_PARENTHESIS)
					{
						tokensDst.Add(tk1);
						i++;
					}
					continue;
				}
				if (tk.Type != TokenType.INDENT)
					break;
				if (indent == 0)
					indent = tk.Text.Length;
			}
			for (; i < end; i++)
			{
				var tk = tokens[i];
				if (tk.TopChar == delim && tk.Type == TokenType.OPERATOR)
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
					continue;
				}
				else if (tk.TopChar == '[')
				{
					i = makeTree(TokenType.AST_BRANCET, tokens, i, end, ']', tokensDst, out errorToken);
					continue;
				}
				else if (tk.Type == TokenType.ERR)
				{
					errorToken = tk;
				}
				if (tk.Type == TokenType.INDENT)
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
				if (tk.Type == TokenType.ERR)
					break;  // ERR
				Debug.Assert(tk.TopChar != '{');
				if (tk.TopChar == '(')
				{
					i = makeTree(TokenType.AST_PARENTHESIS, tokens, i, end, ')', tkP.Sub, out errorToken);
					continue;
				}
				else if (tk.TopChar == '[')
				{
					i = makeTree(TokenType.AST_BRANCET, tokens, i, end, ']', tkP.Sub, out errorToken);
					continue;
				}
				else if (tk.TopChar == closeChar)
				{
					errorToken = null;
					return i;
				}
				if ((closeChar == ')' || closeChar == ']') && tk.Type == TokenType.CODE)
					probablyCloseBefore = i;
				if (tk.Type == TokenType.INDENT && closeChar != '}')
					continue;  // remove INDENT;
				i = appendKeyword(tokens, i, end, tkP.Sub, out errorToken);
			}
			if (tk.Type != TokenType.ERR)
			{
				uint errref = ctx.SUGAR_P(ReportLevel.ERR, tk.ULine, 0, "'{0}' is expected (probably before {1})", closeChar.ToString(), tokens[probablyCloseBefore].Text);
				tkP.toERR(this.ctx, errref);
			}
			else
			{
				tkP.Type = TokenType.ERR;
			}
			errorToken = tkP;
			return end;
		}

		//static int appendKeyword(CTX, kKonohaSpace *ks, kArray *tls, int s, int e, kArray *dst, kToken **tkERR)
		private int appendKeyword(IList<Token> tokens, int start, int end, IList<Token> tokensDst, out Token errorToken)
		{
			int next = start; // don't add
			Token tk = tokens[start];
			if (tk.Type < TokenType.OPERATOR)
			{
				tk.Keyword = (KeywordType)tk.Type;
			}
			if (tk.Type == TokenType.SYMBOL)
			{
				tk.IsResolved(ctx, ks);
			}
			else if (tk.Type == TokenType.USYMBOL)
			{
				if (!tk.IsResolved(ctx, ks))
				{
					throw new NotImplementedException();
					//KonohaClass ct = kKonohaSpace_getCT(ks, null/*FIXME*/, tk.Text, tk.Text.Length, TY_unknown);
					object ct = null;
					if (ct != null)
					{
						tk.Keyword = KeywordType.Type;
						//tk.Type = ct->cid;
					}
				}
			}
			else if (tk.Type == TokenType.OPERATOR)
			{
				if (!tk.IsResolved(ctx, ks))
				{
					uint errref = ctx.SUGAR_P(ReportLevel.ERR, tk.ULine, 0, "undefined token: {0}", tk.Text);
					tk.toERR(this.ctx, errref);
					errorToken = tk;
					return end;
				}
			}
			else if (tk.Type == TokenType.CODE)
			{
				tk.Keyword = KeywordType.Brace;
			}
			if (tk.IsType)
			{
				while (next + 1 < end)
				{
					Token tkN = tokens[next + 1];
					if (tkN.TopChar != '[')
						break;
					List<Token> abuf = new List<Token>();
					int atop = abuf.Count;
					next = makeTree(TokenType.AST_BRANCET, tokens, next + 1, end, ']', abuf, out errorToken);
					if (abuf.Count > atop)
					{
						tk.ResolveType(this.ctx, abuf[atop]);
					}
				}
			}
			if (tk.Keyword > KeywordType.Expr)
			{
				tokensDst.Add(tk);
			}
			errorToken = null;
			return next;
		}

	}

	public class KonohaParam
	{
		public static readonly KonohaParam NULL;

		public TokenType Type { get; set; }
	}

	public class KonohaClass
	{
		public int cid { get; set; }

		public KonohaParam cparam { get; set; }
	}


}
