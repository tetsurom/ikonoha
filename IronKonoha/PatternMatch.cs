using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
	internal class PatternMatch
	{
		internal static int Expr(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			int r = -1;
			var expr = stmt.newExpr2(ctx, tls, s, e);
			if (expr != null)
			{
				//dumpExpr(_ctx, 0, 0, expr);
				//kObject_setObject(stmt, name, expr);
				stmt.map.Add(name, expr);
				r = e;
			}
			return r;
		}

		internal static int Type(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			int r = -1;
			Token tk = tls[s];
			if (tk.IsType)
			{
				//kObject_setObject(stmt, name, tk);
				stmt.map.Add(name, new SingleTokenExpr(tk));
				r = s + 1;
			}
			return r;
		}

		internal static int Usymbol(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			int r = -1;
			Token tk = tls[s];
			if (tk.TokenType == TokenType.USYMBOL)
			{
				stmt.map.Add(name, new SingleTokenExpr(tk));
				r = s + 1;
			}
			return r;
		}

		internal static int Symbol(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			int r = -1;
			Token tk = tls[s];
			if (tk.TokenType == TokenType.SYMBOL)
			{
				stmt.map.Add(name, new SingleTokenExpr(tk));
				r = s + 1;
			}
			return r;
		}

		// static KMETHOD PatternMatch_Params(CTX, ksfp_t *sfp _RIX)
		internal static int Params(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tokens, int s, int e)
		{
			int r = -1;
			Token tk = tokens[s];
			if (tk.TokenType == TokenType.AST_PARENTHESIS)
			{
				var tls = tk.Sub;
				int ss = 0;
				int ee = tls.Count;
				if (0 < ee && tls[0].Keyword == KeywordType.Void) ss = 1;  //  f(void) = > f()
				BlockExpr bk = new Parser(ctx, stmt.ks).CreateBlock(stmt, tls, ss, ee, ',');
				stmt.map.Add(name, bk);
				r = s + 1;
			}
			return r;
		}


		// static KMETHOD PatternMatch_Block(CTX, ksfp_t *sfp _RIX)
		internal static int Block(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			//Console.WriteLine("PatternMatch_Block name:" + name.Name);
			Token tk = tls[s];
			if (tk.TokenType == TokenType.CODE)
			{
				stmt.map.Add(name, new CodeExpr(tk));
				return s + 1;
			}
			var parser = new Parser(ctx, stmt.ks);
			if (tk.TokenType == TokenType.AST_BRACE)
			{
				BlockExpr bk = parser.CreateBlock(stmt, tk.Sub, 0, tk.Sub.Count, ';');
				stmt.map.Add(name, bk);
				return s + 1;
			}
			else
			{
				BlockExpr bk = parser.CreateBlock(stmt, tls, s, e, ';');
				stmt.map.Add(name, bk);
				return e;
			}
		}

		// static KMETHOD PatternMatch_Toks(CTX, ksfp_t *sfp _RIX)
		internal static int Toks(Context ctx, KStatement stmt, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
		{
			if (s < e)
			{
				var a = new List<Token>();
				while (s < e)
				{
					a.Add(tls[s]);
					s++;
				}
				//kObject_setObject(stmt, name, a);
				//stmt.map.Add(name, a);
				throw new NotImplementedException();
				return e;
			}
			return -1;
		}
	}
}
