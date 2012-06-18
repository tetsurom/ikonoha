using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
	[Flags]
	public enum KFuncFlag
	{
		Public               = (1<<0),
		Virtual              = (1<<1),
		Hidden               = (1<<2),
		Const                = (1<<3),
		Static               = (1<<4),
		Immutable            = (1<<5),
		Restricted           = (1<<6),
		Overloaded           = (1<<7),
		CALLCC               = (1<<8),
		FASTCALL             = (1<<9),
		D                    = (1<<10),
		Abstract             = (1<<11),
		Coercion             = (1<<12),
		SmartReturn          = (1<<13),
	}

	public class KFunc
	{
		public KonohaSpace ks { get; private set; }
		public static readonly KFunc NoName = new KFunc();
		public string Name { get; private set; }
		public string Body { get; private set; }
		public KType cid { get; set; }
		public IList<KStatement> param { get; set; }
		public KFuncFlag flag { get; set; }
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

		public KFunc(KonohaSpace ks, KFuncFlag flag, KType cid, string name, IList<KStatement> param, string body)
		{
			this.ks = ks;
			this.Name = name;
			this.flag = flag;
			this.cid = cid;
			this.param = param;
			this.Body = body;
		}
		public KFunc()
		{

		}

		public int packid { get; set; }

		public bool isCoercion { get { return (flag & KFuncFlag.Coercion) != 0; } }
	}

	public class KFunk<D> : KFunc
	{
		public D Delegate { get; set; }

	}
}
