using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFibo
{
	class Program
	{
		static int csfibo(int n)
		{
			if (n < 3) { return 1; } else { return csfibo(n - 1) + csfibo(n - 2); }
		}

		static void Main(string[] args)
		{
			var konoha = new IronKonoha.Konoha();
			dynamic global = konoha.space.scope;
			global.csfibo = new Func<int, int>(csfibo);
			konoha.Eval("int fibo(int n){ if(n < 3){ return 1; } else { return fibo(n - 1) + fibo(n - 2); } }");
			Console.ReadLine(); // fibo is not compiled yet.
			Console.WriteLine(global.fibo(36)); // here fibo is compiled first and calc fibo(10).
			Console.ReadLine();
			konoha.Eval("csfibo(36)"); // call fibo defined in C# code.
			Console.ReadLine();
		}
	}
}
