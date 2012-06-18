using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public KonohaSpace ks { get; private set; }
		public static readonly KFunk NoName = new KFunk();
		public string Name { get; private set; }
		public string Body { get; private set; }
		public KType cid { get; set; }
		public IList<KStatement> param { get; set; }
		public KFunkFlag flag { get; set; }
		public IEnumerable<string> paramNames
		{
			get
			{
				return param.Select(stmt => stmt.map[ks.Symbols.Expr].tk.Text);
			}
		}
		public IEnumerable<KType> paramTypes
		{
			get
			{
				return param.Select(stmt => stmt.map[ks.Symbols.Type].tk.KType);
			}
		}
		public bool isPublic { get { return true; } }

		public KFunk(KonohaSpace ks, KFunkFlag flag, KType cid, string name, IList<KStatement> param, string body)
		{
			this.ks = ks;
			this.Name = name;
			this.flag = flag;
			this.cid = cid;
			this.param = param;
			this.Body = body;
		}
		public KFunk()
		{

		}

		public int packid { get; set; }
	}

	public class KFunk<D> : KFunk
	{
		public D Delegate { get; set; }

	}
}
