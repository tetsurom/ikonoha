using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronKonoha;
using System.IO;
using System.Dynamic;

namespace KConsole
{
	class Program
	{
		static Dictionary<string, string> parseOption(string[] args)
		{
			var optionsDef = new HashSet<string> { "-i", "-hoge", "-huga", "-o" };

			string key = null;
			return args
					.GroupBy(s => optionsDef.Contains(s) ? key = s : key)
					.Where(g=>g.Key != null)
					.ToDictionary(g => g.Key, g => g.Skip(1).FirstOrDefault());
		}

		static void Main(string[] args)
		{
			var options = parseOption(args);
			int optionsCount = options.Select(pair => pair.Value != null ? 2 : 1).Sum();
			string scriptName = args.Length > optionsCount ? args[optionsCount] : null;
			if (scriptName != null)
			{
				string[] scriptArgs = args.Skip(optionsCount).ToArray();
			}

			var konoha = new IronKonoha.Konoha();

			if (scriptName != null)
			{
				var sr = new StreamReader(scriptName);
				konoha.EvalScript(sr.ReadToEnd());
				return;
			}
			
			//var grobalScope = IronKonoha.Konoha.CreateScope();
			string prompt = ">>> ";
			string input = null;
			string exprstr = "";
			while (true)
			{
				Console.Write(prompt);
				input = Console.ReadLine();
				if (input == "")
				{
					exprstr = "";
					prompt = ">>> ";
					continue;
				}
				else if (exprstr != "")
				{
					exprstr = exprstr + " " + input;
				}
				else
				{
					exprstr = input;
				}
				{
					Console.WriteLine(" => " + konoha.Eval(exprstr));
				}
				//try
				//{
				//var ast = new Parser().ParseExpr(exprstr);
				//}
				//catch (Exception e)
				//{
				//	prompt = "... ";
				//	continue;
				//}
				try
				{
					//dynamic res = konoha.ExecuteExpr (exprstr, grobalScope);
				}
				catch (Exception)
				{

				}
				finally
				{
					exprstr = "";
					prompt = ">>> ";
				}

			}
		}
	}
}
