using System;
using System.Reflection;
using System.Linq;

namespace IronKonoha
{
	public class Package
	{
		public static bool load (KNameSpace ns, string pkgname)
		{
			Assembly asm = Assembly.LoadFile (pkgname);
			if (pkgname.EndsWith (".*")) {
				pkgname = pkgname.Replace(".*", "");
			}
			string clsName = pkgname.Split('.').Last();
    	    Type class1 = asm.GetType("KonohaLibrary."+clsName);
	        MethodInfo myMethod = class1.GetMethod("import");
			return (bool) myMethod.Invoke(null, new object[] {ns});
		}
	}
	public interface IKonohaPackageLoadable {
		bool import(KNameSpace ns);
	}
}