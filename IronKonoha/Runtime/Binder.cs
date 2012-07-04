using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Linq.Expressions;

namespace IronKonoha.Runtime
{
	// SymplGetMemberBinder is used for general dotted expressions for fetching
	// members.
	//
	public class KonohaGetMemberBinder : GetMemberBinder
	{
		public KonohaGetMemberBinder(string name)
			: base(name, true)
		{
		}
		public override DynamicMetaObject FallbackGetMember(
				DynamicMetaObject targetMO, DynamicMetaObject errorSuggestion)
		{
			// ComBinder moved to Codeplex only, removing usage from this project
			// to keep it dependent only functionality in .NET 4.0.
			// Requires Microsoft.Dynamic reference.
			// Defer if any object has no value so that we evaulate their
			// Expressions and nest a CallSite for the InvokeMember.
			if (!targetMO.HasValue) return Defer(targetMO);
			// Find our own binding.
			var flags = BindingFlags.IgnoreCase | BindingFlags.Static |
						BindingFlags.Instance | BindingFlags.Public;
			var members = targetMO.LimitType.GetMember(this.Name, flags);
			if (members.Length == 1)
			{
				return new DynamicMetaObject(
					Runtime.Utilities.EnsureObjectResult(
					  Expression.MakeMemberAccess(
						Expression.Convert(targetMO.Expression,
										   members[0].DeclaringType),
						members[0])),
					// Don't need restriction test for name since this
					// rule is only used where binder is used, which is
					// only used in sites with this binder.Name.
					BindingRestrictions.GetTypeRestriction(targetMO.Expression,
														   targetMO.LimitType));
			}
			else
			{
				return errorSuggestion ??
					Runtime.Utilities.CreateThrow(
						targetMO, null,
						BindingRestrictions.GetTypeRestriction(targetMO.Expression,
															   targetMO.LimitType),
						typeof(MissingMemberException),
						"cannot bind member, " + this.Name +
							", on object " + targetMO.Value.ToString());
			}
		}
	}

	public class KonohaInvokeMemberBinder : InvokeMemberBinder
	{
		public KonohaInvokeMemberBinder(string name, CallInfo callinfo)
			: base(name, true, callinfo)
		{ // true = ignoreCase
		}

		public override DynamicMetaObject FallbackInvokeMember(
				DynamicMetaObject targetMO, DynamicMetaObject[] args,
				DynamicMetaObject errorSuggestion)
		{
			if (!targetMO.HasValue || args.Any((a) => !a.HasValue))
			{
				var deferArgs = new DynamicMetaObject[args.Length + 1];
				for (int i = 0; i < args.Length; i++)
				{
					deferArgs[i + 1] = args[i];
				}
				deferArgs[0] = targetMO;
				return Defer(deferArgs);
			}
			// Find our own binding.
			// Could consider allowing invoking static members from an instance.
			var flags = BindingFlags.IgnoreCase | BindingFlags.Instance |
						BindingFlags.Public;
			var members = targetMO.LimitType.GetMember(this.Name, flags);
			if ((members.Length == 1) && (members[0] is PropertyInfo ||
										  members[0] is FieldInfo))
			{
				// NEED TO TEST, should check for delegate value too
				var mem = members[0];
				throw new NotImplementedException();
				//return new DynamicMetaObject(
				//    Expression.Dynamic(
				//        new SymplInvokeBinder(new CallInfo(args.Length)),
				//        typeof(object),
				//        args.Select(a => a.Expression).AddFirst(
				//               Expression.MakeMemberAccess(this.Expression, mem)));

				// Don't test for eventinfos since we do nothing with them now.
			}
			else
			{
				// Get MethodInfos with right arg counts.
				var mi_mems = members.
					Select(m => m as MethodInfo).
					Where(m => m is MethodInfo &&
							   ((MethodInfo)m).GetParameters().Length ==
								   args.Length);
				// Get MethodInfos with param types that work for args.  This works
				// except for value args that need to pass to reftype params. 
				// We could detect that to be smarter and then explicitly StrongBox
				// the args.
				List<MethodInfo> res = new List<MethodInfo>();
				foreach (var mem in mi_mems)
				{
					if (Runtime.Utilities.ParametersMatchArguments(
										   mem.GetParameters(), args))
					{
						res.Add(mem);
					}
				}
				// False below means generate a type restriction on the MO.
				// We are looking at the members targetMO's Type.
				var restrictions = Runtime.Utilities.GetTargetArgsRestrictions(
													  targetMO, args, false);
				if (res.Count == 0)
				{
					return errorSuggestion ??
						Runtime.Utilities.CreateThrow(
							targetMO, args, restrictions,
							typeof(MissingMemberException),
							"Can't bind member invoke -- " + args.ToString());
				}
				// restrictions and conversion must be done consistently.
				var callArgs = Runtime.Utilities.ConvertArguments(
												 args, res[0].GetParameters());
				return new DynamicMetaObject(
				   Runtime.Utilities.EnsureObjectResult(
					 Expression.Call(
						Expression.Convert(targetMO.Expression,
										   targetMO.LimitType),
						res[0], callArgs)),
				   restrictions);
				// Could hve tried just letting Expr.Call factory do the work,
				// but if there is more than one applicable method using just
				// assignablefrom, Expr.Call throws.  It does not pick a "most
				// applicable" method or any method.
			}
		}

		public override DynamicMetaObject FallbackInvoke(
				DynamicMetaObject targetMO, DynamicMetaObject[] args,
				DynamicMetaObject errorSuggestion)
		{
			var argexprs = new Expression[args.Length + 1];
			for (int i = 0; i < args.Length; i++)
			{
				argexprs[i + 1] = args[i].Expression;
			}
			argexprs[0] = targetMO.Expression;
			// Just "defer" since we have code in SymplInvokeBinder that knows
			// what to do, and typically this fallback is from a language like
			// Python that passes a DynamicMetaObject with HasValue == false.
			return new DynamicMetaObject(
						   Expression.Dynamic(
				// This call site doesn't share any L2 caching
				// since we don't call GetInvokeBinder from Sympl.
				// We aren't plumbed to get the runtime instance here.
							   new KonohaInvokeBinder(new CallInfo(args.Length)),
							   typeof(object), // ret type
							   argexprs),
				// No new restrictions since SymplInvokeBinder will handle it.
						   targetMO.Restrictions.Merge(
							   BindingRestrictions.Combine(args)));
		}
	}

	public class KonohaInvokeBinder : InvokeBinder
	{
		public KonohaInvokeBinder(CallInfo callinfo)
			: base(callinfo)
		{
		}

		public override DynamicMetaObject FallbackInvoke(
				DynamicMetaObject targetMO, DynamicMetaObject[] argMOs,
				DynamicMetaObject errorSuggestion)
		{
			if (!targetMO.HasValue || argMOs.Any((a) => !a.HasValue))
			{
				var deferArgs = new DynamicMetaObject[argMOs.Length + 1];
				for (int i = 0; i < argMOs.Length; i++)
				{
					deferArgs[i + 1] = argMOs[i];
				}
				deferArgs[0] = targetMO;
				return Defer(deferArgs);
			}
			// Find our own binding.
			if (targetMO.LimitType.IsSubclassOf(typeof(Delegate)))
			{
				var parms = targetMO.LimitType.GetMethod("Invoke").GetParameters();
				if (parms.Length == argMOs.Length)
				{
					// Don't need to check if argument types match parameters.
					// If they don't, users get an argument conversion error.
					var callArgs = Runtime.Utilities.ConvertArguments(argMOs, parms);
					var expression = Expression.Invoke(
						Expression.Convert(targetMO.Expression, targetMO.LimitType),
						callArgs);
					return new DynamicMetaObject(
						Runtime.Utilities.EnsureObjectResult(expression),
						BindingRestrictions.GetTypeRestriction(targetMO.Expression,
															   targetMO.LimitType));
				}
			}
			return errorSuggestion ??
				Runtime.Utilities.CreateThrow(
					targetMO, argMOs,
					BindingRestrictions.GetTypeRestriction(targetMO.Expression,
														   targetMO.LimitType),
					typeof(InvalidOperationException),
					"Wrong number of arguments for function -- " +
					targetMO.LimitType.ToString() + " got " + argMOs.ToString());

		}
	}
}
