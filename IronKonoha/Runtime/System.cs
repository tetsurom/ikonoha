using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha.Runtime
{
	[Obsolete("now System is not .net class, but KonohaClass.")]
	public class System
	{
		public static void p(object obj)
		{
			Console.WriteLine(obj);
		}
		private class AssertFailedException : Exception
		{
			public AssertFailedException() { }
			public AssertFailedException(string message) : base(message) { }
			public AssertFailedException(string message, Exception inner) : base(message) { }
		}
		public static void assert(bool cond)
		{
			if(!cond)
				throw new AssertFailedException();
		}

		public static void hello()
		{
			Console.WriteLine("hello");
		}

		public static string test = "test";
	}
}
