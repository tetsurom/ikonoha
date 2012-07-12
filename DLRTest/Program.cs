using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Dynamic;

namespace DLRTest
{
	class Program
	{
		static void Main(string[] args)
		{
			dynamic Program = new IronKonoha.TypeWrapper(typeof(Program));

			Console.WriteLine("Dynamic Object - Call From C# code");
			Program.TestStaticMethod(1, 2, 3, 4, 5, 6, 7);

			var Params = new ParameterExpression[] {
				Expression.Parameter(typeof(int)),
				Expression.Parameter(typeof(int)),
				Expression.Parameter(typeof(int)),
				Expression.Parameter(typeof(int)),
				Expression.Parameter(typeof(int)),
				Expression.Parameter(typeof(int)),
				Expression.Parameter(typeof(int)),
			};

			var bodyexpr = Expression.Block(
				Expression.Dynamic(
					new IronKonoha.Runtime.KonohaInvokeMemberBinder("TestStaticMethod", new CallInfo(Params.Length)),
					typeof(object),
					new Expression[] { Expression.Constant(Program) }.Concat(Params)));

			var lmd = Expression.Lambda<Action<int, int, int, int, int, int, int>>(bodyexpr, Params);
			var delg = lmd.Compile();
			Console.WriteLine("Dynamic Object - Call From Dynamic code");
			delg(1,2,3,4,5,6,7);
		}

		public static object TestStaticMethod(int a, int b, int c, int d, int e, int f, int g)
		{
			Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}", a, b, c, d, e, f, g);
			return a + b + c + d + e + f + g;
		}

	}
}
