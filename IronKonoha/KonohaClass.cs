using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Diagnostics;

namespace IronKonoha
{
	public abstract class KonohaType : IDynamicMetaObjectProvider
	{
		public static readonly KonohaType Int = new TypeWrapper(typeof(System.Int64));
		public static readonly KonohaType Float = new TypeWrapper(typeof(System.Double));
		public static readonly KonohaType Boolean = new TypeWrapper(typeof(bool));
		public static readonly KonohaType Void = new TypeWrapper(typeof(void));
		public static readonly KonohaType System = new TypeWrapper(typeof(Runtime.System));
		public static readonly KonohaType Var = new TypeWrapper(typeof(Variant));
		public static readonly KonohaType Func = new TypeWrapper(typeof(Delegate));
		public static readonly KonohaType Object = new TypeWrapper(typeof(object));

		public Type Type { get; protected set; }
		public string Name { get; protected set; }

		public bool IsGenericType
		{
			get
			{
				return this is TypeWrapper ? ((TypeWrapper)this).Type.IsGenericType : false;
			}
		}

		public KonohaType MakeArrayType()
		{
			return new TypeWrapper((this is TypeWrapper ? ((TypeWrapper)this).Type : typeof(object)).MakeArrayType());
		}

		public abstract DynamicMetaObject GetMetaObject(Expression parameter);

		public abstract MethodInfo GetMethod(string name);
		public abstract int GetFieldSize();

		internal abstract object GetDefault();
	}


	/// <summary>
	/// .Netの静的型のラッパー
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("TypeWrapper: {Type}")]
	public class TypeWrapper : KonohaType
	{
		public override DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return new TypeWrapperMetaObject(parameter, this);
		}
		public TypeWrapper(Type type)
		{
			this.Type = type;
			this.Name = type.Name;
		}
		public override MethodInfo GetMethod(string name)
		{
			var flags = BindingFlags.IgnoreCase | BindingFlags.Static |
						BindingFlags.Public;
			return Type.GetMethod(name, flags);
		}

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var tw = obj as TypeWrapper;
			if (obj != null)
			{
				return this.Type == ((TypeWrapper)obj).Type;
			}
			return base.Equals(obj);
		}
		public static bool operator ==(TypeWrapper a, TypeWrapper b)
		{
			if (object.ReferenceEquals(a, b))
			{
				return true;
			}
			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}
			return a.Type == b.Type;
		}


		public static bool operator !=(TypeWrapper a, TypeWrapper b)
		{
			return !(a == b);
		}

		internal override object GetDefault()
		{
			return Type.IsValueType ? Activator.CreateInstance(Type) : null;
		}

		public override int GetFieldSize()
		{
			return Type.GetFields().Length;
		}
	}

	public class TypeWrapperMetaObject : DynamicMetaObject
	{
		public TypeWrapper TypeWrapper { get; private set; }
		public Type Type { get { return TypeWrapper.Type; } }

		public TypeWrapperMetaObject(Expression parameter, TypeWrapper typeWrapper)
			: base(parameter, BindingRestrictions.Empty, typeWrapper)
		{
			this.TypeWrapper = typeWrapper;
		}

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			var flags = BindingFlags.IgnoreCase | BindingFlags.Static |
						BindingFlags.Public;
			var members = Type.GetMember(binder.Name, flags);
			if (members.Length == 1)
			{
				if (members[0].MemberType == MemberTypes.Method)
				{
					var method = members[0] as MethodInfo;
					Type ftype = Expression.GetDelegateType(method.GetParameters().Select(p => p.ParameterType).Concat(new[] { method.ReturnType }).ToArray());
					var delg = Delegate.CreateDelegate(ftype, method);
					return new DynamicMetaObject(
						Expression.Constant(delg),
						this.Restrictions.Merge(
							BindingRestrictions.GetInstanceRestriction(
								this.Expression,
								this.Value)
						)
					);
				}
				// staticメンバーへのバインドを行うので第1引数はnull
				return new DynamicMetaObject(
					Expression.MakeMemberAccess(
						null,
						members[0]),
					this.Restrictions.Merge(
						BindingRestrictions.GetInstanceRestriction(
							this.Expression,
							this.Value)
					)
				);
			}
			return binder.FallbackGetMember(this);
		}

		public override DynamicMetaObject BindInvokeMember(
			InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			var flags = BindingFlags.IgnoreCase | BindingFlags.Static |
						BindingFlags.Public;
			var members = Type.GetMember(binder.Name, flags);
			if ((members.Length == 1) && (members[0] is PropertyInfo ||
										  members[0] is FieldInfo))
			{
				var mem = members[0];
				throw new NotImplementedException();
			}
			else
			{
				List<MethodInfo> res = members.
					Where(m => m is MethodInfo &&
							   ((MethodInfo)m).GetParameters().Length == args.Length).
					Select(m => (MethodInfo)m).
					Where(m => Runtime.Utilities.ParametersMatchArguments(
										   m.GetParameters(), args)).
					ToList();
				if (res.Count == 0)
				{
					var typeMo = Runtime.Utilities.GetRuntimeTypeMoFromWrapper(this);
					var result = binder.FallbackInvokeMember(typeMo, args, null);
					return result;
				}
				var restrictions = Runtime.Utilities.GetTargetArgsRestrictions(
					this, args, true);
				var callArgs =
					Runtime.Utilities.ConvertArguments(
					args, res[0].GetParameters());
				return new DynamicMetaObject(
				   Runtime.Utilities.EnsureObjectResult(
					   Expression.Call(res[0], callArgs)),
				   restrictions);
			}
		}

		public override DynamicMetaObject BindCreateInstance(
				   CreateInstanceBinder binder, DynamicMetaObject[] args)
		{
			var res = Type.GetConstructors().
				Where(c => c.GetParameters().Length == args.Length).
				Where(c => Runtime.Utilities.ParametersMatchArguments(c.GetParameters(), args)).
				ToList();
			if (res.Count == 0)
			{
				return binder.FallbackCreateInstance(
								  Runtime.Utilities.GetRuntimeTypeMoFromWrapper(this),
								  args);
			}

			var restrictions = Runtime.Utilities.GetTargetArgsRestrictions(
								this, args, true);
			var ctorArgs =
				Runtime.Utilities.ConvertArguments(
				args, res[0].GetParameters());
			return new DynamicMetaObject(
				Expression.New(res[0], ctorArgs),
			   restrictions);
		}
	}

	[Flags]
	enum KFieldFlag { Getter, Setter };

	[System.Diagnostics.DebuggerDisplay("KonohaClass: {Name}")]
	public class KonohaClass : KonohaType
	{
		public Dictionary<string, object> Methods { get; private set; }
		public Dictionary<string, object> StaticFields { get; private set; }
		public Dictionary<string, object> Fields { get; private set; }
		public KonohaClass Parent { get; private set; }

		public KonohaInstance Instanciate(){
			var ins = new KonohaInstance(this);
			if (Methods.ContainsKey(Name))
			{
				((dynamic)Methods[Name])(ins);
			}
			return ins;
		}

		public bool TryFindMember(string key, out object value)
		{
			value = null;
			if (StaticFields.ContainsKey(key))
			{
				value = StaticFields[key];
				return true;
			}
			if (Methods.ContainsKey(key))
			{
				value = Methods[key];
				return true;
			}
			if (Parent != null)
			{
				return Parent.TryFindMember(key, out value);
			}
			return false;
		}

		public KonohaClass(string name, KonohaClass parent)
		{
			Name = name;
			Parent = parent;
			Methods = new Dictionary<string, object>();
			StaticFields = new Dictionary<string, object>();
			Fields = new Dictionary<string, object>();
		}

		public override DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return new KonohaClassMetaObject(parameter, this);
		}

		public override MethodInfo GetMethod(string name)
		{
			return ((Delegate)Methods[name]).Method;
		}

		public override int GetFieldSize()
		{
			return Fields.Count();
		}

		internal void checkMethodDecl(Context ctx, Token tkC, BlockExpr bk, KStatement lastStmt)
		{
			foreach(KStatement stmt in bk.blocks) {
				if(stmt.syn.KeyWord == KeyWordTable.StmtTypeDecl) continue;
				if(stmt.syn.KeyWord == KeyWordTable.StmtMethodDecl) {
					lastStmt.parent.insertAfter(lastStmt, stmt);
					lastStmt = stmt;
				}
				else {
					//Stmt_p(_ctx, stmt, NULL, WARN_, "%s is not available within the class clause", KW_t(stmt->syn->kw));
				}
			}
		}

		[Obsolete("this method is not necessary in C#.", true)]
		internal void setField(Context ctx, KonohaType supct, int fctsize)
		{
			throw new NotImplementedException();
		}

		internal bool addClassFields(Context ctx, KGamma gma, BlockExpr bk, LineInfo pline)
		{
			foreach (KStatement stmt in bk.blocks)
			{
				if (stmt.syn.KeyWord == KeyWordTable.StmtTypeDecl)
				{
					Debug.Assert(stmt.map.ContainsKey(ctx.Symbols.Type));
					Debug.Assert(stmt.map.ContainsKey(ctx.Symbols.Expr));
					KFieldFlag flag = KFieldFlag.Getter | KFieldFlag.Setter;
					var tk = stmt.map[ctx.Symbols.Type].tk;
					var expr = stmt.map[ctx.Symbols.Expr];
					if (!this.declType(ctx, gma, stmt, expr, flag, tk.Type, pline))
					{
						return false;
					}
				}
			}
			//DBG_ASSERT(ct->fsize == ct->fallocsize);
			//DBG_P("all fields are set");
			//KLIB2_setGetterSetter(ctx, ct);
			return true;
		}

		private bool declType(Context ctx, KGamma gma, KStatement stmt, KonohaExpr expr, KFieldFlag flag, KonohaType ty, LineInfo pline)
		{
			if(expr is TermExpr) {
				var name = expr.tk.Text;
				this.defineField(ctx, flag, ty, name, ty.GetDefault(), 0);
				return true;
			}
			else if(expr.syn.KeyWord == KeyWordTable.LET) {
				var lexpr = expr.GetConsAt(1);
				if(lexpr is TermExpr) {
					var vexpr = expr.tyCheckAt(ctx, stmt, 2, gma, ty, 0);
					if(vexpr == null) {
						return false;
					}
					var name = expr.tk.Text;
					if(vexpr.build == ExprType.CONST) {
						this.defineField(ctx, flag, ty, name, vexpr.Data, 0);
					}
					else if(vexpr.build == ExprType.NCONST) {
						this.defineField(ctx, flag, ty, name, vexpr.Data, 0);
					}
					else if(vexpr.build == ExprType.NULL) {
						this.defineField(ctx, flag, ty, name, ty.GetDefault(), 0);
					}
					else {
						//SUGAR Stmt_p(_ctx, stmt, NULL, ERR_, "const value is expected as the field initial value: %s", S_text(name));
						return false;
					}
					return true;
				}
			}
			else if (expr.syn.KeyWord == KeyWordTable.COMMA)
			{
				ConsExpr cons = (ConsExpr)expr;
				foreach(var con in cons.Cons){
					if(!this.declType(ctx, gma, stmt, (KonohaExpr)con, flag, ty, pline)) return false;
				}
				return true;
			}
			//Stmt_p(_ctx, stmt, NULL, ERR_, "field name is expected");
			return false;
		}

		private void defineField(Context ctx, KFieldFlag flag, KonohaType ty, string name, object defaultValue, int p_2)
		{
			Fields[name] = defaultValue;
		}

		internal override object GetDefault(){
			return null;
		}
	}

	public class KonohaClassMetaObject : DynamicMetaObject
	{
		public KonohaClass Class { get; private set; }
		public KonohaClassMetaObject(Expression parameter, KonohaClass klass)
			: base(parameter, BindingRestrictions.Empty, klass)
		{
			this.Class = klass;
		}

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			object val;
			if(Class.TryFindMember(binder.Name, out val)){
				return new DynamicMetaObject(
					Expression.Constant(val),
					this.Restrictions.Merge(
						BindingRestrictions.GetInstanceRestriction(
							this.Expression,
							this.Value)
					)
				);
			}
			return binder.FallbackGetMember(this);
		}

		public override DynamicMetaObject BindInvokeMember(
			InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			if (Class.Methods.ContainsKey(binder.Name))
			{
				var paramInfos = ((Delegate)Class.Methods[binder.Name]).Method.GetParameters();
				if (paramInfos[0].ParameterType == typeof(System.Runtime.CompilerServices.Closure))
				{
					paramInfos = paramInfos.Skip(1).ToArray();
				}

				var @this = Expression.Constant(null);
				var param = args.Select(a => a.Expression).ToList();
				param.Insert(0, @this);

				return new DynamicMetaObject(
					Runtime.Utilities.EnsureObjectResult(
						Expression.Invoke(
							Expression.Constant(Class.Methods[binder.Name]),
							Runtime.Utilities.ConvertArguments(param.ToArray(), paramInfos))),
					this.Restrictions.Merge(
						BindingRestrictions.GetInstanceRestriction(
							this.Expression,
							this.Value)
					)
				);
			}
			return binder.FallbackInvokeMember(this, args);
		}

		public override DynamicMetaObject BindCreateInstance(
					CreateInstanceBinder binder, DynamicMetaObject[] args)
		{

			if (true/*Class.Methods.ContainsKey(Class.Name)*/)
			{
				return new DynamicMetaObject(
					Expression.Call(Expression.Constant(Class), typeof(KonohaClass).GetMethod("Instanciate")),
					this.Restrictions.Merge(
						BindingRestrictions.GetInstanceRestriction(
							this.Expression,
							this.Value)
					)
				);
			}
			return binder.FallbackCreateInstance(this, args);
		}
	}

	public class KonohaInstance : IDynamicMetaObjectProvider
	{
		public KonohaClass Klass{ get; private set; }
		public Dictionary<string, object> Fields { get; private set; }
		public KonohaInstance(KonohaClass klass)
		{
			this.Klass = klass;
			Fields = new Dictionary<string, object>();
			foreach(var pair in Klass.Fields){
				Fields.Add(pair.Key, pair.Value);
			}
		}

		public bool TryFindMember(string key, out object value)
		{
			value = null;
			if (Fields.ContainsKey(key))
			{
				value = Fields[key];
				return true;
			}
			return false;
		}

		public DynamicMetaObject GetMetaObject(Expression parameter)
		{
			return new KonohaInstanceMetaObject(parameter, this);
		}

		public object SetFieldsEntry(string key, object value)
		{
			if (Fields.ContainsKey(key))
			{
				Fields[key] = value;
			}
			else
			{
				Fields.Add(key, value);
			}
			return value;
		}

		public object GetFieldsEntry(string key)
		{
			object result = null;
			if (Fields.ContainsKey(key))
			{
				result = Fields[key];
			}
			return result;
		}
	}

	public class KonohaInstanceMetaObject : DynamicMetaObject
	{
		public KonohaClass Class { 
			get{
				return Instance.Klass;
			}
		}
		public KonohaInstance Instance { get; private set; }

		public KonohaInstanceMetaObject(Expression parameter, KonohaInstance instance)
			: base(parameter, BindingRestrictions.Empty, instance)
		{
			this.Instance = instance;
		}
		/*
		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			object val;
			if (Instance.TryFindMember(binder.Name, out val))
			{
				return new DynamicMetaObject(
					Expression.Constant(val),
					this.Restrictions.Merge(
						BindingRestrictions.GetInstanceRestriction(
							this.Expression,
							this.Value)
					)
				);
			}
			return binder.FallbackGetMember(this);
		}
		 * */

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			// Method call in the containing class:
			string methodName = "GetFieldsEntry";

			// One parameter
			Expression[] parameters = new Expression[]
            {
                Expression.Constant(binder.Name)
            };

			var methodCall = Expression.Call(
				Expression.Convert(Expression, LimitType),
				typeof(KonohaInstance).GetMethod(methodName),
				parameters);

			DynamicMetaObject getFieldEntry = new DynamicMetaObject(
				methodCall,
				BindingRestrictions.GetTypeRestriction(Expression, LimitType));
			return getFieldEntry;
		}

		public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
		{
			// Method to call in the containing class:
			string methodName = "SetFieldsEntry";

			// setup the binding restrictions.
			BindingRestrictions restrictions =
				BindingRestrictions.GetTypeRestriction(Expression, LimitType);

			// setup the parameters:
			Expression[] args = new Expression[2];
			// First parameter is the name of the property to Set
			args[0] = Expression.Constant(binder.Name);
			// Second parameter is the value
			args[1] = Expression.Convert(value.Expression, typeof(object));

			// Setup the 'this' reference
			Expression self = Expression.Convert(Expression, LimitType);

			// Setup the method call expression
			Expression methodCall = Expression.Call(self,
					typeof(KonohaInstance).GetMethod(methodName),
					args);

			// Create a meta object to invoke Set later:
			DynamicMetaObject setFieldsEntry = new DynamicMetaObject(
				methodCall,
				restrictions);
			// return that dynamic object
			return setFieldsEntry;
		}

		public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			if (Class.Methods.ContainsKey(binder.Name))
			{
				var paramInfos = ((Delegate)Class.Methods[binder.Name]).Method.GetParameters();
				if (paramInfos[0].ParameterType == typeof(System.Runtime.CompilerServices.Closure))
				{
					paramInfos = paramInfos.Skip(1).ToArray();
				}

				var @this = Expression.Constant(this);
				var param = args.Select(a => a.Expression).ToList();
				param.Insert(0, @this);

				return new DynamicMetaObject(
					Runtime.Utilities.EnsureObjectResult(
						Expression.Invoke(
							Expression.Constant(Class.Methods[binder.Name]),
							Runtime.Utilities.ConvertArguments(param.ToArray(), paramInfos))),
					this.Restrictions.Merge(
						BindingRestrictions.GetInstanceRestriction(
							this.Expression,
							this.Value)
					)
				);
			}
			return binder.FallbackInvokeMember(this, args);
		}
	}
}
