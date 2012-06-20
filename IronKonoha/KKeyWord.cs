using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha
{
	public enum KeywordType
	{
		Err,
		Expr,
		Symbol,
		Usymbol,
		Text,
		TKInt,
		TKFloat,
		Type,
		Parenthesis,
		Bracket,
		Brace,
		StmtTypeDecl,
		Block,
		Params,
		ExprMethodCall,
		Toks,
		DOT,
		DIV,
		MOD,
		MUL,
		ADD,
		SUB,
		LT,
		LTE,
		GT,
		GTE,
		EQ,
		NEQ,
		AND,
		OR,
		NOT,
		COLON,
		LET,
		COMMA,
		DOLLAR,
		Void,
		StmtMethodDecl,
		Boolean,
		Int,
		Null,
		True,
		False,
		If,
		Else,
		Return
	}

	[System.Diagnostics.DebuggerDisplay("{Type} {Name}")]
	public class KKeyWord
	{
		public string Name { get; private set; }
		public KeywordType Type { get; private set; }
		public KKeyWord(string name, KeywordType kw)
		{
			Debug.Assert(name != null);
			Name = name;
			Type = kw;
		}
		public KKeyWord(KeywordType kw)
		{
			Name = kw.ToString();
			Type = kw;
		}
		public static bool operator ==(KKeyWord a, KKeyWord b)
		{
			if (System.Object.ReferenceEquals(a, b))
			{
				return true;
			}
			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}
			return a.Name == b.Name && a.Type == b.Type;
		}

		public static bool operator !=(KKeyWord a, KKeyWord b)
		{
			return !(a == b);
		}
	}
	public class KeyWordTable
	{
		public static readonly KKeyWord Err = new KKeyWord("$ERR", KeywordType.Err);
		public static readonly KKeyWord Expr = new KKeyWord("$expr", KeywordType.Expr);
		public static readonly KKeyWord Symbol = new KKeyWord("$SYMBOL", KeywordType.Symbol);
		public static readonly KKeyWord Usymbol = new KKeyWord("$USYMBOL", KeywordType.Usymbol);
		public static readonly KKeyWord Text = new KKeyWord("$TEXT", KeywordType.Text);
		public static readonly KKeyWord TKInt = new KKeyWord("$INT", KeywordType.TKInt);
		public static readonly KKeyWord TKFloat = new KKeyWord("$FLOAT", KeywordType.TKFloat);
		public static readonly KKeyWord Type = new KKeyWord("$type", KeywordType.Type);
		public static readonly KKeyWord Parenthesis = new KKeyWord("()", KeywordType.Parenthesis);
		public static readonly KKeyWord Bracket = new KKeyWord("[]", KeywordType.Bracket);
		public static readonly KKeyWord Brace = new KKeyWord("{}", KeywordType.Brace);
		public static readonly KKeyWord StmtTypeDecl = new KKeyWord("$type", KeywordType.StmtTypeDecl);
		public static readonly KKeyWord Block = new KKeyWord("$block", KeywordType.Block);
		public static readonly KKeyWord Params = new KKeyWord("$param", KeywordType.Params);
		public static readonly KKeyWord ExprMethodCall = new KKeyWord("", KeywordType.ExprMethodCall);
		public static readonly KKeyWord Toks = new KKeyWord("$toks", KeywordType.Toks);
		public static readonly KKeyWord DOT = new KKeyWord(".", KeywordType.DOT);
		public static readonly KKeyWord DIV = new KKeyWord("/", KeywordType.DIV);
		public static readonly KKeyWord MOD = new KKeyWord("%", KeywordType.MOD);
		public static readonly KKeyWord MUL = new KKeyWord("*", KeywordType.MUL);
		public static readonly KKeyWord ADD = new KKeyWord("+", KeywordType.ADD);
		public static readonly KKeyWord SUB = new KKeyWord("-", KeywordType.SUB);
		public static readonly KKeyWord LT = new KKeyWord("<", KeywordType.LT);
		public static readonly KKeyWord LTE = new KKeyWord("<=", KeywordType.LTE);
		public static readonly KKeyWord GT = new KKeyWord(">", KeywordType.GT);
		public static readonly KKeyWord GTE = new KKeyWord(">=", KeywordType.GTE);
		public static readonly KKeyWord EQ = new KKeyWord("==", KeywordType.EQ);
		public static readonly KKeyWord NEQ = new KKeyWord("!=", KeywordType.NEQ);
		public static readonly KKeyWord AND = new KKeyWord("&&", KeywordType.AND);
		public static readonly KKeyWord OR = new KKeyWord("||", KeywordType.OR);
		public static readonly KKeyWord NOT = new KKeyWord("!", KeywordType.NOT);
		public static readonly KKeyWord COLON = new KKeyWord(":", KeywordType.COLON);
		public static readonly KKeyWord LET = new KKeyWord("", KeywordType.LET);
		public static readonly KKeyWord COMMA = new KKeyWord(",", KeywordType.COMMA);
		public static readonly KKeyWord DOLLAR = new KKeyWord("", KeywordType.DOLLAR);
		public static readonly KKeyWord Void = new KKeyWord("", KeywordType.Void);
		public static readonly KKeyWord StmtMethodDecl = new KKeyWord("void", KeywordType.StmtMethodDecl);
		public static readonly KKeyWord Boolean = new KKeyWord("boolean", KeywordType.Boolean);
		public static readonly KKeyWord Int = new KKeyWord("int", KeywordType.Int);
		public static readonly KKeyWord Null = new KKeyWord("null", KeywordType.Null);
		public static readonly KKeyWord True = new KKeyWord("true", KeywordType.True);
		public static readonly KKeyWord False = new KKeyWord("false", KeywordType.False);
		public static readonly KKeyWord If = new KKeyWord("if", KeywordType.If);
		public static readonly KKeyWord Else = new KKeyWord("else", KeywordType.Else);
		public static readonly KKeyWord Return = new KKeyWord("return", KeywordType.Return);

		public static List<KKeyWord> Map = new List<KKeyWord>(){
			Err,
			Expr,
			Symbol,
			Usymbol,
			Text,
			TKInt,
			TKFloat,
			Type,
			Parenthesis,
			Bracket,
			Brace,
			StmtTypeDecl,
			Block,
			Params,
			ExprMethodCall,
			Toks,
			DOT,
			DIV,
			MOD,
			MUL,
			ADD,
			SUB,
			LT,
			LTE,
			GT,
			GTE,
			EQ,
			NEQ,
			AND,
			OR,
			NOT,
			COLON,
			LET,
			COMMA,
			DOLLAR,
			Void,
			StmtMethodDecl,
			Boolean,
			Int,
			Null,
			True,
			False,
			If,
			Else,
			Return
		};
	}
}
