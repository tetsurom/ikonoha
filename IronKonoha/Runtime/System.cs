using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha.Runtime
{
	[Obsolete("now System is not .net class, but KonohaClass.")]
	public class System
	{
		public static void p(object obj)
		{
			Console.WriteLine(obj);
		}

		public static void hello()
		{
			Console.WriteLine("hello");
		}

		public static string test = "test";
	}
}
