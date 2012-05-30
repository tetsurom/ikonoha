using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace IronKonoha
{

	[System.Diagnostics.DebuggerDisplay("{Filename}, {LineNumber}")]
	public class LineInfo
	{
		public LineInfo(int line, string file)
		{
			this.LineNumber = line;
			this.Filename = file;
		}

		public int LineNumber { get; set; }

		public string Filename { get; set; }
	}

	public class Konoha
	{

		Context ctx;
		KonohaSpace space;

		public static readonly int FN_NONAME = -1;

		public Konoha()
		{
			ctx = new Context();
			space = new KonohaSpace(ctx);
		}

		/// <summary>
		/// １つの文を実行する。
		/// </summary>
		/// <param name="exprStr">実行する文</param>
		/// <param name="module">グローバル変数等を管理するオブジェクト</param>
		/// <returns>実行結果</returns>
		public object ExecuteExpr(string exprStr, ExpandoObject module)
		{
			return Eval(exprStr);
		}

		public static ExpandoObject CreateScope()
		{
			return new ExpandoObject();
		}

		public dynamic Eval(string code)
		{
			space.Eval(code);
			return null;
		}

	}
}
