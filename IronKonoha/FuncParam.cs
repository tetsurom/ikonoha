using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
	public class FuncParam
	{
		public string Name { get; set; }
		public KonohaType Type { get; set; }
		public FuncParam(string name, KonohaType type)
		{
			Name = name;
			Type = type;
		}
	}
}
