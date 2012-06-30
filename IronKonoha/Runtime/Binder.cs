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
				throw new NotImplementedException();
				//return errorSuggestion ??
				//    Runtime.Utilities.CreateThrow(
				//        targetMO, null,
				//        BindingRestrictions.GetTypeRestriction(targetMO.Expression,
				//                                               targetMO.LimitType),
				//        typeof(MissingMemberException),
				//        "cannot bind member, " + this.Name +
				//            ", on object " + targetMO.Value.ToString());
			}
		}
	}

}
