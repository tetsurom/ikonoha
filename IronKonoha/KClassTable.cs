using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		/// <param name="rtype">?</param>
		/// <param name="psize">?</param>
		/// <param name="p">型引数</param>
		/// <returns></returns>
		/// 
		// datatype.h
		// static kclass_t *CT_Generics(CTX, kclass_t *ct, ktype_t rtype, int psize, kparam_t *p)
		public static KClass Generics(Context ctx, KClass ct, KType rtype, IList<KParam> p)
		{
			int psize = p.Count;
			KParamID paramdom = Kparamdom(ctx, p);
			KClass ct0 = ct;
			var isNotFuncClass = (ct.bcid != BCID.CLASS_Func);
			do {
				if(ct.paramdom == paramdom && (isNotFuncClass || ct.p0 == rtype)) {
					return ct;
				}
				if(ct.searchSimilarClassNULL == null) break;
				ct = ct.searchSimilarClassNULL;
			} while(ct != null);
			KClass newct = new KClass(ctx, ct0, null, null){
				paramdom = paramdom,
				p0 = isNotFuncClass ? p[0].ty : rtype
			};
			//newct.methods, K_EMPTYARRAY);
			if(newct.searchSuperMethodClassNULL == null) {
				newct.searchSuperMethodClassNULL = ct0;
			}
			ct.searchSimilarClassNULL = newct;
			return ct.searchSimilarClassNULL;
		}

	}
}
