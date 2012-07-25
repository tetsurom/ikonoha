using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Dynamic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace IronKonoha.Runtime
{
	class Utilities
	{
		public static bool ParametersMatchArguments(ParameterInfo[] parameters,
													DynamicMetaObject[] args)
		{
			Debug.Assert(args.Length == parameters.Length);
			for (int i = 0; i < args.Length; i++)
			{
				var paramType = parameters[i].ParameterType;
				if (paramType == typeof(Type) &&
					(args[i].LimitType == typeof(TypeWrapper)))
				{
					continue;
				}
				if (!paramType.IsAssignableFrom(args[i].LimitType))
				{
					return false;
				}
			}
			return true;
		}

		internal static DynamicMetaObject GetRuntimeTypeMoFromWrapper(DynamicMetaObject wrapperMo)
		{
			Debug.Assert((wrapperMo.LimitType == typeof(TypeWrapper)));
			var pi = typeof(TypeWrapper).GetProperty("Type");
			Debug.Assert(pi != null);
			return new DynamicMetaObject(
				Expression.Property(
					Expression.Convert(wrapperMo.Expression, typeof(TypeWrapper)),
					pi),
				wrapperMo.Restrictions.Merge(
					BindingRestrictions.GetTypeRestriction(
						wrapperMo.Expression, typeof(TypeWrapper)))
			);
		}

		public static BindingRestrictions GetTargetArgsRestrictions(
				DynamicMetaObject target, DynamicMetaObject[] args,
				bool instanceRestrictionOnTarget)
		{
			var restrictions = target.Restrictions.Merge(BindingRestrictions
															.Combine(args));
			if (instanceRestrictionOnTarget)
			{
				restrictions = restrictions.Merge(
					BindingRestrictions.GetInstanceRestriction(
						target.Expression,
						target.Value
					));
			}
			else
			{
				restrictions = restrictions.Merge(
					BindingRestrictions.GetTypeRestriction(
						target.Expression,
						target.LimitType
					));
			}
			for (int i = 0; i < args.Length; i++)
			{
				BindingRestrictions r;
				if (args[i].HasValue && args[i].Value == null)
				{
					r = BindingRestrictions.GetInstanceRestriction(
							args[i].Expression, null);
				}
				else
				{
					r = BindingRestrictions.GetTypeRestriction(
							args[i].Expression, args[i].LimitType);
				}
				restrictions = restrictions.Merge(r);
			}
			return restrictions;
		}

		public static Expression[] ConvertArguments(DynamicMetaObject[] args, ParameterInfo[] ps)
		{
			Debug.Assert(args.Length == ps.Length);
			Expression[] callArgs = new Expression[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				Expression argExpr = args[i].Expression;
				if (args[i].LimitType == typeof(TypeWrapper) &&
					ps[i].ParameterType == typeof(Type))
				{
					argExpr = GetRuntimeTypeMoFromWrapper(args[i]).Expression;
				}
				argExpr = Expression.Convert(argExpr, ps[i].ParameterType);
				callArgs[i] = argExpr;
			}
			return callArgs;
		}

		public static Expression[] ConvertArguments(Expression[] args, ParameterInfo[] ps)
		{
			Debug.Assert(args.Length == ps.Length);
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].Type != ps[i].ParameterType)
				{
					args[i] = Expression.Convert(args[i], ps[i].ParameterType);
				}
			}
			return args;
		}

		public static Expression EnsureObjectResult(Expression expr)
		{
			return EnsureObjectResult(expr, typeof(object));
		}

		public static Expression EnsureObjectResult(Expression expr, Type type)
		{
			if (!expr.Type.IsValueType)
			{
				return expr;
			}
			if (expr.Type == typeof(void))
			{
				return Expression.Block(expr, Expression.Default(type));
			}
			else
			{
				if (expr.Type != type)
				{
					expr = Expression.Convert(expr, type);
				}
				return expr;
			}
		}

		public static DynamicMetaObject CreateThrow
		(DynamicMetaObject target, DynamicMetaObject[] args,
		 BindingRestrictions moreTests,
		 Type exception, params object[] exceptionArgs)
		{
			Expression[] argExprs = null;
			Type[] argTypes = Type.EmptyTypes;
			int i;
			if (exceptionArgs != null)
			{
				i = exceptionArgs.Length;
				argExprs = new Expression[i];
				argTypes = new Type[i];
				i = 0;
				foreach (object o in exceptionArgs)
				{
					Expression e = Expression.Constant(o);
					argExprs[i] = e;
					argTypes[i] = e.Type;
					i += 1;
				}
			}
			ConstructorInfo constructor = exception.GetConstructor(argTypes);
			if (constructor == null)
			{
				throw new ArgumentException(
					"Type doesn't have constructor with a given signature");
			}
			return new DynamicMetaObject(
				Expression.Throw(
					Expression.New(constructor, argExprs),
				// Force expression to be type object so that DLR CallSite
				// code things only type object flows out of the CallSite.
					typeof(object)),
				target.Restrictions.Merge(BindingRestrictions.Combine(args))
								   .Merge(moreTests));
		}

	}
}
