﻿using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IronKonoha
{
	/// <summary>
	/// Generate DLR Expression Tree from konoha AST.
	/// </summary>
	class Converter
	{
		private Context ctx;
		private KonohaSpace ks;
		internal static readonly Expression KNull = Expression.Constant(null);
		internal static readonly Expression KTrue = Expression.Constant(true);
		internal static readonly Expression KFalse = Expression.Constant(false);
		//internal static readonly Expression KEmptyString = Expression.Constant(null,typeof(KString));
		
		public Converter(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
		}
<<<<<<< HEAD
		
		public Expression<Func<object>> Convert (BlockExpr block)
		{
			Expression<Func<object>> b = null;
//			try{
				List<Expression> list = new List<Expression> ();
				foreach(KStatement st in block.blocks) {
					foreach(KonohaExpr kexpr in st.map.Values) {
						list.Add(MakeExpression(kexpr));
					}
				}

				var root = Expression.Convert(Expression.Block(list), typeof(object));
				b = Expression.Lambda<Func<object>>(root);
//			}catch(Exception e){
				//TODO : static error check
//			}
			return b;
		}

		public Expression MakeExpression(KonohaExpr kexpr)
		{
			if(kexpr is ConsExpr) {
				return MakeConsExpression((ConsExpr)kexpr);
			} else if(kexpr is TermExpr) {
				switch(kexpr.tk.Type) {
				case TokenType.INT:
					return Expression.Constant(long.Parse(kexpr.tk.Text));
				case TokenType.FLOAT:
					return Expression.Constant(double.Parse(kexpr.tk.Text));
				case TokenType.TEXT:
					return Expression.Constant(kexpr.tk.Text);
				}
			}
			return null;
		}

		public Expression MakeConsExpression (ConsExpr expr)
		{
			Token tk = expr.Cons[0] as Token;
			var param = expr.Cons.Skip(1).Select(p => MakeExpression (p as KonohaExpr));
			switch(tk.Type) {
			case TokenType.OPERATOR:
				return OperatorASM(tk.Keyword,param.ElementAt(0),param.ElementAt(1));
			case TokenType.SYMBOL:
				return SymbolASM(tk.Keyword, param);
			case TokenType.CODE:
				return Expression.Call(typeof(Converter).GetMethod("RunEval"),param.ElementAt(0));
			}
			return null;
		}

		public Expression OperatorASM (KeywordType keyword, Expression left, Expression right)
		{
			switch (keyword) {
			case KeywordType.ADD:
				return Expression.Call(typeof(Converter).GetMethod("Add"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.SUB:
				return Expression.Call(typeof(Converter).GetMethod("Sub"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.MUL:
				return Expression.Call(typeof(Converter).GetMethod("Mul"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.DIV:
				return Expression.Call(typeof(Converter).GetMethod("Div"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.EQ:
				return Expression.Call(typeof(Converter).GetMethod("Eq"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.NEQ:
				return Expression.Call(typeof(Converter).GetMethod("Neq"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.LT:
				return Expression.Call(typeof(Converter).GetMethod("Lt"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.LTE:
				return Expression.Call(typeof(Converter).GetMethod("Lte"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.GT:
				return Expression.Call(typeof(Converter).GetMethod("Gt"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.GTE:
				return Expression.Call(typeof(Converter).GetMethod("Gte"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.AND:
				return Expression.Call(typeof(Converter).GetMethod("And"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.OR:
				return Expression.Call(typeof(Converter).GetMethod("Or"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.MOD:
				return Expression.Call(typeof(Converter).GetMethod("Mod"),Expression.Convert(left,typeof(object)),Expression.Convert(right,typeof(object)));
			case KeywordType.Parenthesis:
				return null; // It will not use in here.
			}
		return null;
		}

		public Expression SymbolASM (KeywordType keyword, IEnumerable<Expression> param)
		{
			switch(keyword) {
			case KeywordType.If:
				if(param.Count() == 3){
					return Expression.Condition(param.ElementAt(0), param.ElementAt(1), param.ElementAt(2));
				}
				return Expression.Condition(param.ElementAt(0), param.ElementAt(1),KNull);
			case KeywordType.Null:
				return KNull;
			}
			return null;
		}
		
		#region Operator
		public static object Add(dynamic a, dynamic b){
			return a + b;
		}
		public static object Sub(dynamic a, dynamic b){
			return a - b;
		}
		public static object Mul(dynamic a, dynamic b){
			return a * b;
		}
		public static object Div(dynamic a, dynamic b){
			return a / b;
		}
		public static object Eq(dynamic a, dynamic b){
			return a == b;
		}
		public static object Neq(dynamic a, dynamic b){
			return a != b;
		}
		public static object Lt(dynamic a, dynamic b){
			return a < b;
		}
		public static object Lte(dynamic a, dynamic b){
			return a <= b;
		}
		public static object Gt(dynamic a, dynamic b){
			return a > b;
		}
		public static object Gte(dynamic a, dynamic b){
			return a >= b;
		}
		public static object And(dynamic a, dynamic b){
			return a && b; //TODO
		}
		public static object Or(dynamic a, dynamic b){
			return a || b; //TODO
		}
		public static object Mod(dynamic a, dynamic b){
			return a % b;
		}
		#endregion
		public static object RunEval(string script,Context ctx, KonohaSpace ks)
		{
			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var converter = new Converter(ctx,ks);
			var tokens = tokenizer.Tokenize(script);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			var ast = converter.Convert(block);
			var f = ast.Compile();
			return f();
		}

	}
}
