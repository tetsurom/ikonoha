using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace KFibo
{
	class Program
	{

		static IronKonoha.Konoha konoha;

		static long csfibo(long n)
		{
			if (n < 3) { return 1; } else { return csfibo(n - 1) + csfibo(n - 2); }
		}

		static void Assert(bool val)
		{
			if (val)
			{
				Console.WriteLine("PASSED");
			}
			else
			{
				Console.WriteLine("FAILED");
			}
		}

		static void Assert<T>(string program, T request)
		{
			var ret = konoha.Eval(program);
			if (ret == request)
			{
				Console.WriteLine("PASSED {0}", program);
			}
			else
			{
				Console.WriteLine("FAILED {0} Request: {1}, Return: {2}", program, request, ret);
			}
		}

		static void Main(string[] args)
		{
			konoha = new IronKonoha.Konoha();
			//dynamic global = konoha.space.Scope;
			//global.csfibo = new Func<long, long>(csfibo);
			Assert(@"1", 1);
			Assert(@"1234567890", 1234567890);
			Assert(@"6+4", 10);
			Assert(@"6-4", 2);
			Assert(@"6*4", 24);
			Assert(@"6/4", 1);
			Assert(@"6%4", 2);
			Assert (true == konoha.Eval (@"5==5"));
			Assert (false == konoha.Eval (@"2==7"));
			Assert (false == konoha.Eval (@"5!=5"));
			Assert (true == konoha.Eval (@"2!=7"));
			Assert (false == konoha.Eval (@"15 <   5"));
			Assert (false == konoha.Eval (@"11 <  11"));
			Assert (true == konoha.Eval (@"10 <  13"));
			Assert (false == konoha.Eval (@"15 <=  5"));
			Assert (true == konoha.Eval (@"11 <= 11"));
			Assert (true == konoha.Eval (@"10 <= 13"));
			Assert (false == konoha.Eval (@"15 >   5"));
			Assert (false == konoha.Eval (@"11 >  11"));
			Assert (true == konoha.Eval (@"10 >  13"));
			Assert (false == konoha.Eval (@"15 >=  5"));
			Assert (true == konoha.Eval (@"11 >= 11"));
			Assert (true == konoha.Eval (@"10 >= 13"));
			Assert(@"int f(){ int a = 1; return a; }; f();", 1);
			Assert(@"int fibo(int n){ if(n < 3){ return 1; }else{ return fibo(n-1) + fibo(n-2); }}; fibo(10);", 55);
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
