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

		public Context ctx { get; private set; }
		public KNameSpace space { get; private set; }

		public static readonly int FN_NONAME = -1;

		public Konoha()
		{
			ctx = new Context();
			space = new KNameSpace(ctx);
		}

		public dynamic Eval(string code)
		{
			return space.Eval(code);
		}

	}
}
