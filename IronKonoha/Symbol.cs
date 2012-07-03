using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
	public enum SymPol
	{
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

		protected Symbol()
		{
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
		private static readonly Dictionary<KeywordType, string> nameTable = new Dictionary<KeywordType, string>()
		{
			{KeywordType.Expr, "expr"},
			{KeywordType.Block, "block"},
			{KeywordType.If, "if"},
			{KeywordType.Symbol, "SYMBOL"},
			{KeywordType.Params, "params"},
			{KeywordType.Type, "type"},
			{KeywordType.Else, "else"},
		};
		public static Symbol Get(Context ctx, string name)
		{
			if (!ctx.share.SymbolMap.ContainsKey(name))
			{
				ctx.share.SymbolMap.Add(name, new Symbol() { Name = name });
			}
			return ctx.share.SymbolMap[name];
		}
		public static Symbol Get(Context ctx, KeywordType kw)
		{
			return Get(ctx, nameTable[kw]);
		}

		public override string ToString()
		{
			return Name ?? "";
		}
	}

	public class SymbolConst
	{
		public readonly Symbol Expr;
		public readonly Symbol Block;
		public readonly Symbol If;
		public readonly Symbol Else;
		public readonly Symbol SYMBOL;
		public readonly Symbol Params;
		public readonly Symbol Type;

		internal SymbolConst(Context ctx)
		{
			Expr = Symbol.Get(ctx, "expr");
			Block = Symbol.Get(ctx, "block");
			If = Symbol.Get(ctx, "if");
			Else = Symbol.Get(ctx, "else");
			SYMBOL = Symbol.Get(ctx, "SYMBOL");
			Params = Symbol.Get(ctx, "params");
			Type = Symbol.Get(ctx, "type");
		}
	}
}
