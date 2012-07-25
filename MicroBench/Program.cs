﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroBench
{
	class Program
	{
		static IronKonoha.Konoha konoha;

		static int TotalCount;
		static int PassedCount;

		enum Mode { Full, TimeOnly };
		static Mode mode = Mode.Full;

		static void Assert(bool val)
		{
			TotalCount++;
			if (val)
			{
				Console.WriteLine("PASSED");
				PassedCount++;
			}
			else
			{
				Console.WriteLine("FAILED");
			}
		}

		static void AssertNoError(string program)
		{
			if (program == String.Empty) return;
			TotalCount++;
			PassedCount++;
			var beforeTime = DateTime.Now;
			var ret = string.Format("PASSED {0}", program);
			try
			{
				konoha.Eval(program);
			}
			catch (Exception e)
			{
				ret = string.Format("FAILED {0} {1}", program, e.GetType().Name);
				//ret = string.Format("FAILED {0} {1}", program, e.ToString());
				PassedCount--;
			}
			finally { }
			var afterTime = DateTime.Now;
			var diffTime = afterTime - beforeTime;
			if(mode == Mode.Full) Console.WriteLine(ret);
			Console.WriteLine(diffTime.TotalMilliseconds);
		}

		static void Assert<T>(string program, T request)
		{
			TotalCount++;
			var ret = konoha.Eval(program);
			if (ret == request)
			{
				Console.WriteLine("PASSED {0}", program);
				PassedCount++;
			}
			else
			{
				Console.WriteLine("FAILED {0} Request: {1}, Return: {2}", program, request, ret);
			}
		}

		static void Main(string[] args)
		{
			konoha = new IronKonoha.Konoha();
			int N = 1000000;
			mode = Mode.TimeOnly;
			AssertNoError(string.Format(@"int N={0};", N));
			AssertNoError(@"int testSimpleLoop() { int i = 0; while (i < N) { i = i + 1; }; return i; }");
			AssertNoError(@"int testLocalVariable() { int y = 0; int i = 0; while (i < N) { y = i; i = i + 1; } return y; }");
			//AssertNoError(@"int global_x = 0;");
			//AssertNoError(@"int global_y = 0;");
			//AssertNoError(@"int global_z = 0;");
			//AssertNoError(@"int testGlobalVariable() { int i = 0; while (i < N) { global_y = i; i = i + 1; } return global_y; }");
			AssertNoError("String testStringAssignment() { String s = \"\"; int i = 0; while (i < N) { i = i + 1; s = \"A\"; } return s; }");
			AssertNoError(@"int testIntegerOperation() { int y = 0; int i = 0; while (i < N) { i = i + 1; y = y + 1; } return y; }");
			AssertNoError(@"float testFloatOperation() { float f = 0.0; int i = 0; while (i < N) { i = i + 1; f = f + 0.1; } return f; }");
			AssertNoError(@"void func0() {}");
			AssertNoError(@"int testFunctionCall() { int i = 0; while (i < N) { i = i + 1; func0(); } return i; }");
			AssertNoError(@"int func1() { return 1; }");
			AssertNoError(@"int testFunctionReturn() { int res = 0; int i = 0; while (i < N) { i = i + 1; res = func1(); } return res; }");
			AssertNoError(@"int testMathFabs() { int i = 0; while (i < N) { i = i + 1; Math.fabs(0-1.0); } return i; }");
			//AssertNoError(@"int testCallFunctionObject() { Func[void] f = func0; int i = 0; while (i < N) { i = i + 1; f(); } return i; }");
			AssertNoError(@"class Dim { int x; int y; int z; void f() { } }");
			AssertNoError(@"int testObjectCreation() { int i = 0; while (i < N) { i = i + 1; Dim d = new Dim(); } return i; }");
			AssertNoError(@"Dim testFieldVariable() { Dim d = new Dim(); int i = 0; while (i < N) { i = i + 1; d.y = 1; } return d; }");
			AssertNoError(@"int testMethodCall() { Dim d = new Dim(); int i = 0; while (i < N) { i = i + 1; d.f(); } return i; }");
			AssertNoError(@"int testArraySetter() { int i = 0; Array[int] a = new Int[N]; while (i < N) { a[i] = i; i = i + 1; } return i; }");
			AssertNoError(@"int testArrayGetter() { int i = 0; int y = 0; Array[int] a = new Int[N]; while (i < N) { y = y + a[i]; i = i + 1; } return y; }");
			AssertNoError(@"testSimpleLoop();");
			AssertNoError(@"testLocalVariable();");
			//AssertNoError(@"testGlobalVariable();");
			AssertNoError(@"testStringAssignment();");
			AssertNoError(@"testIntegerOperation();");
			AssertNoError(@"testFloatOperation();");
			AssertNoError(@"testFunctionCall();");
			AssertNoError(@"testFunctionReturn();");
			AssertNoError(@"testMathFabs();");
			//AssertNoError(@"testCallFunctionObject();");
			AssertNoError(@"testObjectCreation();");
			AssertNoError(@"testFieldVariable();");
			AssertNoError(@"testMethodCall();");
			AssertNoError(@"testArraySetter();");
			AssertNoError(@"testArrayGetter();");
			for (int i = 0; i < 20; ++i)
			{
				Console.Clear();
				AssertNoError(@"testSimpleLoop();");
				AssertNoError(@"testLocalVariable();");
				//AssertNoError(@"testGlobalVariable();");
				AssertNoError(@"testStringAssignment();");
				AssertNoError(@"testIntegerOperation();");
				AssertNoError(@"testFloatOperation();");
				AssertNoError(@"testFunctionCall();");
				AssertNoError(@"testFunctionReturn();");
				AssertNoError(@"testMathFabs();");
				//AssertNoError(@"testCallFunctionObject();");
				AssertNoError(@"testObjectCreation();");
				AssertNoError(@"testFieldVariable();");
				AssertNoError(@"testMethodCall();");
				AssertNoError(@"testArraySetter();");
				AssertNoError(@"testArrayGetter();");
			}
			if (mode == Mode.Full) Console.WriteLine("Total: {0}  Passed: {1}  {2} % Passed.", TotalCount, PassedCount, (int)((double)PassedCount / TotalCount * 100));
		}
	}
}
