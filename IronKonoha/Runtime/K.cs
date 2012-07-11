using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha.Runtime
{
	class K
	{
		public static bool import(string name)
		{
			Console.WriteLine("importing {0} ...", name);
			return false;
		}

		public static void multi(long a, long b, long c)
		{
			Console.WriteLine("{0}, {1}, {2}", a, b, c);
		}

	}
}
