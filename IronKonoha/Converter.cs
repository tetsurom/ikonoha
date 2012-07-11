using System;
using System.Dynamic;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using System.Reflection;
using System.Diagnostics;

namespace IronKonoha
{

	public class FunctionEnvironment
	{
		public ParameterExpression[] Params { get; set; }
		public LabelTarget ReturnLabel { get; set; }
		public KFunc Method { get; set; }
	}

	public class FuncCache<T, RT>
	{
		protected string BlockBody;
		protected IEnumerable<ParameterExpression> Params;
		protected Converter Converter;

		private T _lambda;
		public T Lambda
		{
			get
			{
				if (_lambda == null)
				{
                    _lambda = Converter.ConvertFunc<T, RT>(BlockBody, Params, Method).Compile();
                    this.Invoke = _lambda;
                    //Scope[key] = _lambda;
                    Scope = null;
                    key = null;
				}
				return _lambda;
			}
		}

		public T Invoke { get; private set; }

		public KFunc Method;

		public FuncCache(Converter converter, string body, IEnumerable<FuncParam> param, KFunc mtd){

			this.Converter = converter;
			this.BlockBody = body;
			this.Method = mtd;
			var paramexprs = param.Select(p => Expression.Parameter(p.Type.Type, p.Name));
			Params = paramexprs.ToArray();
			var bodyexpr = Expression.Block(
				Params,
				Expression.Invoke(
					Expression.MakeMemberAccess(
						Expression.Constant(this),
						this.GetType().GetProperty("Lambda")),
					Params));
			var lmd = Expression.Lambda<T>(bodyexpr, Params);
			Invoke = lmd.Compile();
		}

        public IDictionary<string, object> Scope { private get; set; }
        public string key { private get; set; }
	}

	/// <summary>
	/// Generate DLR Expression Tree from konoha AST.
	/// </summary>
	public class Converter
	{
		private Context ctx;
		private KNameSpace ks;
		internal static readonly Expression KNull = Expression.Constant(null);
		internal static readonly Expression KTrue = Expression.Constant(true);
		internal static readonly Expression KFalse = Expression.Constant(false);
		internal static readonly Dictionary<KeywordType,ExpressionType> BinaryOperationType
			 = new Dictionary<KeywordType, ExpressionType>()
			{
				{KeywordType.ADD ,ExpressionType.Add},
				{KeywordType.SUB ,ExpressionType.Subtract},
				{KeywordType.MUL ,ExpressionType.Multiply},
				{KeywordType.DIV ,ExpressionType.Divide},
				{KeywordType.EQ  ,ExpressionType.Equal},
				{KeywordType.NEQ ,ExpressionType.NotEqual},
				{KeywordType.LT  ,ExpressionType.LessThan},
				{KeywordType.LTE ,ExpressionType.LessThanOrEqual},
				{KeywordType.GT  ,ExpressionType.GreaterThan},
				{KeywordType.GTE ,ExpressionType.GreaterThanOrEqual},
				{KeywordType.AND ,ExpressionType.And},
				{KeywordType.OR  ,ExpressionType.Or},
				{KeywordType.MOD ,ExpressionType.Modulo}
			};
		//internal static readonly Expression KEmptyString = Expression.Constant(null,typeof(KString));

		readonly SymbolConst Symbols;

		private IDictionary<string, object> Scope { get { return ks.Scope as IDictionary<string, object>; } }

		public Converter(Context ctx, KNameSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
			Symbols = new SymbolConst(ctx);
		}

		public Expression Convert (BlockExpr block)
		{
			IEnumerable<Expression> list = ConvertToExprList(block,null);
			if(list.Count() == 0){
				return Expression.Lambda(Expression.Block(Expression.Constant(null)));
			}
			return Expression.Lambda(Expression.Block(list));
		}

        private IEnumerable<Expression> ConvertTextBlock(string body, FunctionEnvironment environment)
		{
			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var tokens = tokenizer.Tokenize(body);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			Debug.WriteLine("### Konoha AST Dump ###");
			Debug.WriteLine(block.GetDebugView());
			block.TyCheckAll(ctx, new KGamma() { ks = this.ks, cid = KonohaType.System, mtd = environment.Method });
			Debug.WriteLine("### Konoha AST Dump (tychecked) ###");
			Debug.WriteLine(block.GetDebugView());
			return ConvertToExprList(block, environment);
		}

		public Expression<T> ConvertFunc<T, RT>(string body, IEnumerable<ParameterExpression> param, KFunc mtd)
		{
			var env = new FunctionEnvironment()
			{
				Params = param.ToArray(),
				ReturnLabel = Expression.Label(typeof(RT)),
				Method = mtd
			};
			var list = ConvertTextBlock(body, env).ToList();
			list.Add(Expression.Label(env.ReturnLabel, Expression.Constant(default(RT), typeof(RT))));
			var block = Expression.Block(typeof(RT), list);
			string dbv = (string)typeof(Expression).InvokeMember("DebugView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, block, null);
			Debug.WriteLine("### DLR AST Dump ###");
			Debug.WriteLine(dbv);
			return Expression.Lambda<T>(block, env.Params);
		}

		public Expression KStatementToExpr(KStatement st, FunctionEnvironment environment)
		{
			if (st.syn != null && st.syn.KeyWord == KeyWordTable.Err)
			{
				throw new ArgumentException("invalid statement");
			}
			if (st.syn != null && st.syn.KeyWord == KeyWordTable.If || st.build == StmtType.IF)
			{
				return MakeIfExpression(st.map, environment);
			}
			if (st.syn != null && st.syn.KeyWord == KeyWordTable.StmtMethodDecl)
			{
				return Expression.Empty();//MakeFuncDeclExpression(st.map);
			}
			if (st.syn != null && st.syn.KeyWord == KeyWordTable.Type)
			{
				string name = ((st.map[Symbol.Get(this.ctx,KeywordType.Expr)] as ConsExpr).Cons[1] as KonohaExpr).tk.Text;
				ParameterExpression tmp = Expression.Parameter(typeof(long),name);
				this.Scope.Add(name,(Expression)tmp);

				return Expression.Block(
					new ParameterExpression[] { tmp },
					Expression.Assign(
						tmp,
						MakeExpression((st.map[Symbol.Get(this.ctx,KeywordType.Expr)] as ConsExpr).Cons[2] as KonohaExpr,
							environment))
				);
			}
			if (st.syn != null && st.syn.KeyWord == KeyWordTable.Return || st.build == StmtType.RETURN)
			{
				var exp = MakeExpression(st.map.Values.First(), environment);
				if (exp.Type != environment.ReturnLabel.Type)
				{
					exp = Expression.Convert(exp, environment.ReturnLabel.Type);
				}
				return Expression.Return(environment.ReturnLabel, exp);
			}
			return MakeExpression(st.map.Values.First(), environment);
		}

        public IEnumerable<Expression> ConvertToExprList(BlockExpr block, FunctionEnvironment environment)
		{
            return block.blocks.Select(s => KStatementToExpr(s, environment));
		}

		public Expression MakeBlockExpression(KonohaExpr expr, FunctionEnvironment environment)
		{
			return Expression.Block(ConvertTextBlock(expr.tk.Text, environment));
		}

		public IEnumerable<string> GetParamList(BlockExpr args)
		{
			return from stmt in args.blocks
				   select stmt.map[Symbols.Expr].tk.Text;
		}

		public Expression MakeExpression(ConsExpr kexpr, FunctionEnvironment environment)
		{
			return MakeConsExpression(kexpr, environment);
		}

		public Expression MakeExpression(TermExpr kexpr, FunctionEnvironment environment)
		{
			var text = kexpr.tk.Text;
			switch (kexpr.tk.TokenType)
			{
				case TokenType.INT:
					return Expression.Constant(long.Parse(text));
				case TokenType.FLOAT:
					return Expression.Constant(double.Parse(text));
				case TokenType.TEXT:
					return Expression.Constant(text);
				case TokenType.SYMBOL:
					if (environment != null)
					{
						for (int i = 0; i < environment.Params.Length; ++i)
						{
							if (environment.Params[i].Name == text)
							{
								return environment.Params[i];
							}
						}
					}
					return KNull;
			}
			return KNull;
		}

		public Expression MakeExpression<T>(ConstExpr<T> kexpr, FunctionEnvironment environment)
		{
			return Expression.Constant(kexpr.Data);
		}

		public Expression MakeExpression(ParamExpr kexpr, FunctionEnvironment environment)
		{
			// add 1 because params[0] is 'this' object.
			return environment.Params[kexpr.Order + 1];
		}

		public Expression MakeExpression(KonohaExpr kexpr, FunctionEnvironment environment)
		{
			var expression = MakeExpression((dynamic)kexpr, environment);
			if (expression == null)
			{
				throw new ArgumentException("invalid KonohaExpr.", "kexpr");
			}
			return expression;
		}

		public ConditionalExpression MakeIfExpression(Dictionary<dynamic, KonohaExpr> map, FunctionEnvironment environment)
		{
			Expression cond = MakeExpression(map[Symbols.Expr], environment);
			if(cond.Type != typeof(bool)){
				cond = Expression.Convert(cond, typeof(bool));
			}
			var onTrue = MakeBlockExpression(map[Symbols.Block], environment);

			if(map.Count() < 3){
				return Expression.IfThen(cond, onTrue);
			}
			return Expression.IfThenElse(cond, onTrue, MakeBlockExpression(map[Symbols.Else], environment));
		}

		public Expression MakeConsExpression(ConsExpr expr, FunctionEnvironment environment)
		{
			if (expr.syn.KeyWord == KeyWordTable.DOT)
			{
				Token tk = expr.Cons[0] as Token;
				return Expression.Dynamic(
					new Runtime.KonohaGetMemberBinder(tk.Text),
					typeof(object),
					Expression.Constant((expr.Cons[1] as ConstExpr<KonohaType>).Data)
				);
			}
			else if (expr.syn.KeyWord == KeyWordTable.Params || expr.syn.KeyWord == KeyWordTable.Parenthesis)
			{
				Token tk = expr.Cons[0] as Token ?? ((KonohaExpr)expr.Cons[0]).tk;
				return Expression.Dynamic(
					new Runtime.KonohaInvokeMemberBinder(tk.Text, new CallInfo(1)),
					typeof(object),
					new[] { Expression.Constant((expr.Cons[1] as ConstExpr<KonohaType>).Data) }.Concat(
						expr.Cons.Skip(2).Select(c => MakeExpression((KonohaExpr)c, environment))));
			}
			else if (expr.Cons[0] is Token)
			{
				Token tk = (Token)expr.Cons[0];
				var param = expr.Cons.Skip(1).Select(p => MakeExpression(p as KonohaExpr, environment)).ToArray();
				switch (tk.TokenType)
				{
					case TokenType.OPERATOR:
						if (param[0].Type.IsPrimitive && param[0].Type == param[1].Type)
						{
							return Expression.MakeBinary(
								BinaryOperationType[tk.Keyword.Type],
								param[0],
								param[1]
							);
						}
						return Expression.Dynamic(GetBinaryBinder(BinaryOperationType[tk.Keyword.Type]),
							typeof(object),
							param[0],
							param[1]
						);
				}
			}
			return null;
		}

		public dynamic GetBinaryBinder(ExpressionType et)
		{
			return Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(
				CSharpBinderFlags.None, et, typeof(Converter),
				new CSharpArgumentInfo[] {
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
		}

	}
}
