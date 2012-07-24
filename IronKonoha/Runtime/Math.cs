using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha.Runtime
{
	public class Math
	{
		public static double fabs(double n)
		{
			return n > 0 ? n : -n;
		}
	}
}
