using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
			//dynamic global = konoha.space.Scope;
			//global.csfibo = new Func<long, long>(csfibo);
			Debug.Assert(1     == konoha.Eval(@"1"));
			Debug.Assert (2 == konoha.Eval (@"2"));
			Debug.Assert (10 == konoha.Eval (@"6+4"));
			Debug.Assert (2 == konoha.Eval (@"6-4"));
			Debug.Assert (24 == konoha.Eval (@"6*4"));
			Debug.Assert (1 == konoha.Eval (@"6/4"));
			Debug.Assert (2 == konoha.Eval (@"6%4"));
			Debug.Assert (true == konoha.Eval (@"5==5"));
			Debug.Assert (false == konoha.Eval (@"2==7"));
			Debug.Assert (false == konoha.Eval (@"5!=5"));
			Debug.Assert (true == konoha.Eval (@"2!=7"));
			Debug.Assert (false == konoha.Eval (@"15 <   5"));
			Debug.Assert (false == konoha.Eval (@"11 <  11"));
			Debug.Assert (true == konoha.Eval (@"10 <  13"));
			Debug.Assert (false == konoha.Eval (@"15 <=  5"));
			Debug.Assert (true == konoha.Eval (@"11 <= 11"));
			Debug.Assert (true == konoha.Eval (@"10 <= 13"));
			Debug.Assert (false == konoha.Eval (@"15 >   5"));
			Debug.Assert (false == konoha.Eval (@"11 >  11"));
			Debug.Assert (true == konoha.Eval (@"10 >  13"));
			Debug.Assert (false == konoha.Eval (@"15 >=  5"));
			Debug.Assert (true == konoha.Eval (@"11 >= 11"));
			Debug.Assert (true == konoha.Eval (@"10 >= 13"));
			/*
			konoha.Eval(@"
                int fibo(int n){
                    if(n < 3){
                        return 1;
                    }else{
                        return fibo(n - 1) + fibo(n - 2);
                    }
                }
            ");
			 */
			//Console.ReadLine();
			//Console.WriteLine(konoha.Eval(@"fibo(10);"));
			//Console.ReadLine(); // fibo is not compiled yet.
			//Console.WriteLine(global.fibo((long)36)); // here fibo is compiled first and calc fibo.
			//Console.ReadLine();
			//Console.WriteLine(konoha.Eval("csfibo(36)")); // call fibo defined in C# code.
			//Console.ReadLine();
		}
	}
}
