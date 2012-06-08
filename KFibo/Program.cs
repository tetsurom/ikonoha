using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFibo
{
	class Program
	{
		static void Main(string[] args)
		{
			var konoha = new IronKonoha.Konoha();
			konoha.Eval("int fibo(int n){ if(n < 3){ return 1; } else { return fibo(n - 1) + fibo(n - 2); } }");
			dynamic grobal = konoha.space.scope;
			Console.ReadLine();
			Console.WriteLine(grobal.fibo(10));
			Console.ReadLine();
		}
	}
}
