using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
	[Flags]
	public enum SynFlag
	{
		ExprTerm = 1,
		ExprOp = 1 << 1,
		ExprLeftJoinOp2 = 1 << 2,
		ExprPostfixOp2 = 1 << 3,
		StmtBreakExec = 1 << 8,
		StmtJumpAhead = 1 << 9,
		StmtJumpSkip = 1 << 10,
	}
	/*
		typedef const struct _ksyntax ksyntax_t;
		struct _ksyntax {
			keyword_t kw;  kflag_t flag;
			kArray   *syntaxRulenull;
			kMethod  *ParseStmtnull;
			kMethod  *ParseExpr;
			kMethod  *TopStmtTyCheck;
			kMethod  *StmtTyCheck;
			kMethod  *ExprTyCheck;
			// binary
			ktype_t    ty;   kshort_t priority;
			kmethodn_t op2;  kmethodn_t op1;      // & a
			//kshort_t dummy;
		};
		*/
	[System.Diagnostics.DebuggerDisplay("{KeyWord}")]
	public class Syntax
	{
		public IList<Token> SyntaxRule { get; set; }
		public KKeyWord KeyWord { get; set; }
		public SynFlag Flag { get; set; }
		/// <summary>
		/// 文法の優先度？ 
		/// </summary>
		public int priority { get; set; }
		public KonohaType Type { get; set; }
		public StmtParser PatternMatch { get; set; }
		public ExprParser ParseExpr { get; set; }
		public StmtTyChecker TopStmtTyCheck { get; set; }
		public StmtTyChecker StmtTyCheck { get; set; }
		public ExprTyChecker ExprTyCheck { get; set; }
		public KFunc Op1 { get; set; }
		public KFunc Op2 { get; set; }
		public Syntax Parent { get; set; }
	}
}
