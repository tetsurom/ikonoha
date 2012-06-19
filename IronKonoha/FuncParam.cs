using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
	public class FuncParam
	{
		public string Name { get; set; }
		public Type Type { get; set; }
		public FuncParam(string name, Type type)
		{
			Name = name;
			Type = type;
		}
	}
}
