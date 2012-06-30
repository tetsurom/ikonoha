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

namespace IronKonoha
{
	//public delegate object FuncLambda(params object[] args);

	public abstract class Cache
	{
		protected string BlockBody;
		protected IList<FuncParam> Params;
		protected Converter converter;

		public Cache(Converter converter, string body, IList<FuncParam> param)
		{
			this.converter = converter;
			this.BlockBody = body;
			this.Params = param;
		}
	}

	public class FunctionEnvironment
	{
		public ParameterExpression[] Params { get; set; }
		public LabelTarget ReturnLabel { get; set; }
	}

	public class FuncCache<T, RT> : Cache
	{
		private T _lambda;
		public T Lambda
		{
			get
			{
				if (_lambda == null)
				{
                    _lambda = converter.ConvertFunc<T, RT>(BlockBody, Params).Compile();
                    this.Invoke = _lambda;
                    Scope[key] = _lambda;
                    Scope = null;
                    key = null;
				}
				return _lambda;
			}
		}

		public T Invoke { get; private set; }

		public FuncCache(Converter converter, string body, IList<FuncParam> param)
			:base(converter, body, param){
			var paramexprs = Params.Select(p => Expression.Parameter(p.Type, p.Name)).ToArray();
			var bodyexpr = Expression.Block(
				Expression.Invoke(
					Expression.MakeMemberAccess(Expression.Constant(this), this.GetType().GetProperty("Lambda")),
					paramexprs));
			var lmd = Expression.Lambda<T>(bodyexpr, paramexprs);
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
		private KonohaSpace ks;
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

		public Converter(Context ctx, KonohaSpace ks)
		{
			this.ctx = ctx;
			this.ks = ks;
			Symbols = new SymbolConst(ctx);
		}
		
		public Expression Convert (BlockExpr block)
		{
			return Expression.Lambda(Expression.Block(ConvertToExprList(block, null)));
		}

        private IEnumerable<Expression> ConvertTextBlock(string body, FunctionEnvironment environment)
		{

			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var tokens = tokenizer.Tokenize(body);
			var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
			block.TyCheckAll(ctx, new KGamma() { ks = this.ks, cid = KType.System, flag = KGammaFlag.TOPLEVEL });
			return ConvertToExprList(block, environment);
		}

		public Expression<T> ConvertFunc<T, RT>(string body, IList<FuncParam> param)
		{
			var env = new FunctionEnvironment()
			{
				Params = param.Select(p=>Expression.Parameter(p.Type, p.Name)).ToArray(),
				ReturnLabel = Expression.Label(typeof(RT))
			};
			var list = ConvertTextBlock(body, env).ToList();
			list.Add(Expression.Label(env.ReturnLabel, Expression.Constant(default(RT), typeof(RT))));
			var block = Expression.Block(typeof(RT), list);
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
					typeof(object)
				);
			}
			else if (expr.Cons[0] is Token)
			{
				Token tk = expr.Cons[0] as Token;
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
			else
			{
				Token tk = ((KonohaExpr)expr.Cons[0]).tk;
				var f = Scope[tk.Text];

				if (expr.Cons.Count > 2)
				{
					dynamic df = f;
					var t = df.GetType().GetGenericArguments()[0];
					Expression p = MakeExpression((KonohaExpr)expr.Cons[2], environment);
					if (p.Type == t)
					{
						return Expression.Invoke(Expression.Constant(f), new[] { p });
					}

					var bind = Microsoft.CSharp.RuntimeBinder.Binder.Invoke(CSharpBinderFlags.InvokeSimpleName,typeof(Converter),
					new CSharpArgumentInfo[] {
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					});
					return Expression.Dynamic(bind,typeof(object),Expression.Constant(f),p);
				}
				else
				{
					return Expression.Invoke(Expression.Constant(f));
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
