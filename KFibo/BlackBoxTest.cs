using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace KFibo
{
	public class BlackBoxTest
	{
		private int testCount;
		private int passCount;
		public IronKonoha.Konoha konoha;

		public BlackBoxTest()
		{
			konoha = new IronKonoha.Konoha();
			testCount = 0;
			passCount = 0;
		}

		public void AssertFunctionTest(string eval,string testcase ,dynamic ret)
		{
			dynamic func = konoha.Eval(eval);
			dynamic ftest = konoha.Eval(testcase);
			if(ftest != ret) {
				Console.WriteLine("Error in " + testcase + ": return = " + ftest);
			}else{
				Console.WriteLine(testcase + " passed.");
				passCount++;
			}
			testCount++;
		}

		public void AssertStmtTest(string eval, dynamic ret)
		{
			dynamic t = konoha.Eval(eval);
			if(ret != t) {
				Console.WriteLine("Error in " + eval + ": return = " + t);
			}else {
				Console.WriteLine(eval + " passed.");
				passCount++;
			}
			testCount++;
		}

		public void EndOfTest()
		{
			Console.WriteLine((1.0 * passCount / testCount * 100)  + "% tests passed, " + (testCount - passCount) + " tests failed out of " + testCount);
		}
	}
}

