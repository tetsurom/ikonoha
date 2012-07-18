using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace KFibo {
	class Program {
		static IronKonoha.Konoha konoha;
		static long csfibo(long n)
		{
			if (n < 3) {
				return 1;
			}
			else
			{
				return csfibo(n - 1) + csfibo(n - 2);
			}
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
			Assert(@"(2-1+3)*3/6%4", 2);
			Assert(@"5==5", true);
			Assert(@"2==7", false);
			Assert(@"5!=5", false);
			Assert(@"2!=7", true);
			Assert(@"15 <   5", false);
			Assert(@"11 <  11", false);
			Assert(@"10 <  13", true);
			Assert(@"15 <=  5", false);
			Assert(@"11 <= 11", true);
			Assert(@"10 <= 13", true);
			Assert(@"15 >   5", true);
			Assert(@"11 >  11", false);
			Assert(@"10 >  13", false);
			Assert(@"15 >=  5", true);
			Assert(@"11 >= 11", true);
			Assert(@"10 >= 13", false);
			Assert(@"int f(){ int a = 1; return a; }; f();", 1);
			Assert(@"int fibo(int n){ if(n < 3){ return 1; }else{ return fibo(n-1) + fibo(n-2); }}; fibo(10);", 55);
			Assert("int h(){ System s = new System(); s.p(\"a\"); return 0; }; h();", 0);
			Assert("int whiletest(int n){ while( n > 0 ){  n = n - 1; }; return n; }; whiletest(10);", 0);
			Assert(konoha.Eval(@"class EmptyClass{}; new EmptyClass();") is IronKonoha.KonohaInstance);
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
