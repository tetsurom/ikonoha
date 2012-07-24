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
		public List<ParameterExpression> Locals { get; set; }
		public LabelTarget ReturnLabel { get; set; }
		public KFunc Method { get; set; }
		public KGamma Gamma { get; set; }
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
				{KeywordType.MOD ,ExpressionType.Modulo},
				{KeywordType.LET ,ExpressionType.Assign},
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

			block.TyCheckAll(ctx, environment.Gamma);
			Debug.WriteLine("### Konoha AST Dump (tychecked) ###");
			Debug.WriteLine(block.GetDebugView());

			var exprs = ConvertToExprList(block, environment);

			return exprs;
		}

		public Expression<T> ConvertFunc<T, RT>(string body, IEnumerable<ParameterExpression> param, KFunc mtd)
		{
			var environment = new FunctionEnvironment()
			{
				Params = param.ToArray(),
				ReturnLabel = Expression.Label(typeof(RT)),
				Method = mtd
			};
			environment.Gamma = new KGamma() { ks = this.ks, cid = KonohaType.System, mtd = mtd };

			int outerVariableSize = 0;// environment.Gamma.vars.Count;

			var tokenizer = new Tokenizer(ctx, ks);
			var parser = new Parser(ctx, ks);
			var tokens = tokenizer.Tokenize(body);
			var kblock = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');

			Debug.WriteLine("### Konoha AST Dump ###");
			Debug.WriteLine(kblock.GetDebugView());

			kblock.TyCheckAll(ctx, environment.Gamma);
			Debug.WriteLine("### Konoha AST Dump (tychecked) ###");
			Debug.WriteLine(kblock.GetDebugView());

			var localVarExprs = environment.Gamma.lvar
				.Skip(outerVariableSize)
				.Select(v => Expression.Parameter(v.Type.Type ?? typeof(object), v.Name));
			if (environment.Locals == null)
			{
				environment.Locals = localVarExprs.ToList();
			}
			else
			{
				environment.Locals.AddRange(localVarExprs);
			}

			var list = ConvertToExprList(kblock, environment).ToList();
			list.Add(Expression.Label(environment.ReturnLabel, Expression.Constant(default(RT), typeof(RT))));

			var block = Expression.Block(typeof(RT), environment.Locals, list);

			string dbv = (string)typeof(Expression).InvokeMember("DebugView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, block, null);
			Debug.WriteLine("### DLR AST Dump ###");
			Debug.WriteLine(dbv);

			var lmd = Expression.Lambda<T>(block, environment.Params);
			var d = lmd.Compile();

			return lmd;
		}

		public Expression KStatementToExpr(KStatement st, FunctionEnvironment environment)
		{
			if (st.syn == null || st.syn.KeyWord == KeyWordTable.Expr)
			{
				return MakeExpression(st.map.Values.First(), environment);
			}
			if (st.syn.KeyWord == KeyWordTable.Err)
			{
				throw new ArgumentException("invalid statement");
			}
			if (st.syn.KeyWord == KeyWordTable.If || st.build == StmtType.IF)
			{
				return MakeIfExpression(st.map, environment);
			}
			if (st.syn.KeyWord == KeyWordTable.While || st.build == StmtType.LOOP)
			{
				return MakeWhileExpression(st.map, environment);
			}
			if (st.syn.KeyWord == KeyWordTable.StmtMethodDecl)
			{
				return Expression.Empty();//MakeFuncDeclExpression(st.map);
			}
			if (st.syn.KeyWord == KeyWordTable.Class)
			{
				return Expression.Empty();//MakeFuncDeclExpression(st.map);
			}
			if (st.syn.KeyWord == KeyWordTable.Type)
			{
				var cons = st.Expr(ctx, ctx.Symbols.Expr);
				try
				{
					// local variable decl;
					var variable = MakeExpression(cons.GetConsAt<KonohaExpr>(1), environment);
					var value = MakeExpression(cons.GetConsAt<KonohaExpr>(2), environment);
					Debug.Assert(variable is ParameterExpression);
					return Expression.Assign(variable, value);
				}
				catch (InvalidOperationException)
				{
					// grobal variable;
					var name = cons.GetConsAt<ParamExpr>(1).Name;
					var value = MakeExpression(cons.GetConsAt<KonohaExpr>(2), environment);
					return Expression.Dynamic(
						new Runtime.KonohaSetMemberBinder(name),
						typeof(object),
						Expression.Constant(ks.Classes["System"]),
						value);
				}
			}
			if (st.syn.KeyWord == KeyWordTable.Return || st.build == StmtType.RETURN)
			{
				var exp = MakeExpression(st.map.Values.First(), environment);
				if (exp.Type != environment.ReturnLabel.Type)
				{
					exp = Expression.Convert(exp, environment.ReturnLabel.Type);
				}
				return Expression.Return(environment.ReturnLabel, exp);
			}
			throw new InvalidOperationException(string.Format("unknown statement: {0}", st.GetSourceView()));
		}

        public IEnumerable<Expression> ConvertToExprList(BlockExpr block, FunctionEnvironment environment)
		{
            return block.blocks.Select(s => KStatementToExpr(s, environment));
		}

		public Expression MakeBlockExpression(KonohaExpr expr, FunctionEnvironment environment)
		{
			/*
			int outerVariableSize = environment.Gamma.vars.Count;

			var localVarExprs = environment.Gamma.vars
				.Skip(outerVariableSize)
				.Select(v => Expression.Parameter(v.Type.Type, v.Name));
			if (environment.Locals == null)
			{
				environment.Locals = localVarExprs.ToList();
			}
			else
			{
				environment.Locals.AddRange(localVarExprs);
			}
			*/
			IEnumerable<Expression> blockbody;

			if (expr is CodeExpr)
			{
				blockbody = ConvertTextBlock(expr.tk.Text, environment);
			}
			else
			{
				blockbody = ConvertToExprList(expr as BlockExpr, environment);
			}
			var block = Expression.Block(blockbody);
			/*
			environment.Gamma.vars = environment.Gamma.vars.Take(outerVariableSize).ToList();
			environment.Locals = environment.Locals.Take(outerVariableSize).ToList();
			*/
			return block;
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

		public Expression MakeExpression<T>(ConstExpr<T> kexpr, FunctionEnvironment environment)
		{
			return Expression.Constant(kexpr.TypedData);
		}

		public Expression MakeExpression(CreateInstanceExpr kexpr, FunctionEnvironment environment)
		{
			return Expression.Dynamic(
				new Runtime.KonohaCreateInstanceBinder(new CallInfo(0/*argsize + 1*/)), // FIXME: must impl ctor with args.
 				typeof(object),
				Expression.Constant(kexpr.ty));

		}

		public Expression MakeExpression(ParamExpr kexpr, FunctionEnvironment environment)
		{
			// search local variables
			if (environment != null && environment.Locals != null)
			{
				foreach (var p in environment.Locals.Reverse<ParameterExpression>())
				{
					if (p.Name == kexpr.Name)
					{
						return p;
					}
				}
			}
			// check it is parameter or not.
			if (kexpr.Order >= 0)
			{
				// add 1 because params[0] is 'this' object.
				return environment.Params[kexpr.Order + 1];
			}
			// search grobal variables.
			if (((KonohaClass)ks.Classes["System"]).Fields.ContainsKey(kexpr.Name))
			{
				return Expression.Dynamic(
					new Runtime.KonohaSetMemberBinder(kexpr.Name),
					typeof(object),
					Expression.Constant(ks.Classes["System"]));
			}

			throw new InvalidOperationException(string.Format("undefined field, grobal/local variable or parameter: {0}", kexpr.Name));
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

		public LoopExpression MakeWhileExpression (Dictionary<dynamic, KonohaExpr> map, FunctionEnvironment environment)
		{
			Expression cond = MakeExpression(map[Symbols.Expr], environment);
			if(cond.Type != typeof(bool)){
				cond = Expression.Convert(cond, typeof(bool));
			}
			var label = Expression.Label();
			var block = MakeBlockExpression(map[Symbols.Block], environment);
			return Expression.Loop(Expression.IfThenElse(cond,block,Expression.Break(label)),label);
		}

		public Expression MakeConsExpression(ConsExpr expr, FunctionEnvironment environment)
		{
			if (expr.syn.KeyWord == KeyWordTable.DOT)
			{
				Token tk = expr.Cons[0] as Token;
				var typeexpr = (expr.Cons[1] as ConstExpr<KonohaType>);
				if (typeexpr != null)
				{
					// static field access
					var klass = typeexpr.TypedData.Type;
					return Expression.Dynamic(
						new Runtime.KonohaGetMemberBinder(tk.Text),
						typeof(object),
						Expression.Constant(klass)
					);
				}
				else
				{
					// instance field access
					var obj = MakeExpression((KonohaExpr)expr.Cons[1], environment);
					return Expression.Dynamic(
						new Runtime.KonohaGetMemberBinder(tk.Text),
						typeof(object),
						obj);
				}

			}
			else if (expr.syn.KeyWord == KeyWordTable.LET && expr.Cons[1] is ConsExpr)
			{
				ConsExpr cons = (ConsExpr)expr.Cons[1];
				string fieldName = (cons.GetConsAt<Token>(0)).Text;
				Expression obj = MakeExpression(cons.GetConsAt<KonohaExpr>(1), environment);
				Expression val = MakeExpression(expr.GetConsAt<KonohaExpr>(2), environment);
				return Expression.Dynamic(
					new Runtime.KonohaSetMemberBinder(fieldName),
					typeof(object),
					obj,
					val
				);
			}
			else if (expr.syn.KeyWord == KeyWordTable.Params || expr.syn.KeyWord == KeyWordTable.Parenthesis)
			{
				Token tk = expr.Cons[0] as Token ?? ((KonohaExpr)expr.Cons[0]).tk;
				var argsize = expr.Cons.Count - 2;

				Expression[] paramThis = null;
				if (expr.Cons[1] is ConstExpr<KonohaType>)
				{
					paramThis = new[] { Expression.Constant(((ConstExpr<KonohaType>)expr.Cons[1]).TypedData) };
				}
				else
				{
					paramThis = new[] { MakeExpression((ParamExpr)expr.Cons[1], environment) };
				}
				var param = expr.Cons.Skip(2).Select(c => MakeExpression((KonohaExpr)c, environment));

				return Expression.Dynamic(
					new Runtime.KonohaInvokeMemberBinder(
						tk.Text,
						new CallInfo(argsize + 1)),
					typeof(object),
					paramThis.Concat(param));
			}
			else if (expr.Cons[0] is Token)
			{
				Token tk = (Token)expr.Cons[0];
				var param = expr.Cons.Skip(1).Select(p => MakeExpression(p as KonohaExpr, environment)).ToArray();
				if (BinaryOperationType[tk.Keyword.Type] == ExpressionType.Assign)
				{
					// Expression.Assign cannot be created by Expression.MakeBinary
					return Expression.Assign(param[0], param[1]);
				}
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
