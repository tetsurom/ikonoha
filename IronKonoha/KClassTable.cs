using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace IronKonoha
{
	public class KClassTable
	{

		//static KClass p0

		// static kparamid_t Kparamdom(CTX, int psize, kparam_t *p)
		static KParamID Kparamdom(Context ctx, IList<KParam> p)
		{
			return ctx.share.ParamDomMap[p];
		}

		/// <summary>
		/// ジェネリック型に型引数を当てはめる
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="ct">ジェネリック型</param>
		/// <param name="rtype">戻り値の型</param>
		/// <param name="p">型引数</param>
		/// <returns></returns>
		/// 
		// datatype.h
		// static kclass_t *CT_Generics(CTX, kclass_t *ct, ktype_t rtype, int psize, kparam_t *p)
		public static Type Generics(Type ct, Type rtype, IList<Type> p)
		{
			if (ct == typeof(Delegate))
			{
				if (rtype == null)
				{
					rtype = typeof(void);
				}
				p.Add(rtype);
				return Expression.GetDelegateType(p.ToArray());
			}
			return ct.MakeGenericType(p.ToArray());
		}

	}
}
