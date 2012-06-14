using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFibo
{
	class Program
	{
		static long csfibo(long n)
		{
			if (n < 3) { return 1; } else { return csfibo(n - 1) + csfibo(n - 2); }
		}

		static void Main(string[] args)
		{
			var konoha = new IronKonoha.Konoha();
			dynamic global = konoha.space.scope;
			global.csfibo = new Func<long, long>(csfibo);

			dynamic fibo = konoha.Eval(@"
                int fibo(int n){
                    if(n < 3){
                        return 1;
                    }else{
                        return fibo(n - 1) + fibo(n - 2);
                    }
                }
            ");
			Console.ReadLine(); // fibo is not compiled yet.
			Console.WriteLine(fibo(36)); // here fibo is compiled first and calc fibo.
			Console.ReadLine();
			Console.WriteLine(konoha.Eval("csfibo(36)")); // call fibo defined in C# code.
			Console.ReadLine();
		}
	}
}
