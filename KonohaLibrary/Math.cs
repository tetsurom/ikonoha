using System;
using IronKonoha;

namespace KonohaLibrary
{
	public class Math : IKonohaPackageLoadable {
		public bool import(KNameSpace ns)
		{
			ns.Classes.Add("Math", new TypeWrapper(typeof(System.Math)));
			return true;
		}
	}
}

