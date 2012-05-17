using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronKonoha
{
    public class Syntax{
        /// <summary>
        /// 何のためのメンバーなのか不明
        /// </summary>
        public IList<Token> syntaxRuleNULL { get; set; }
        public KeywordType KeyWord { get; set; }
        /// <summary>
        /// 文法の優先度？ 
        /// </summary>
        public int priority { get; set; }
        public KMethod ParseStmtnull { get; set; }
        public KMethod ParseExpr { get; set; }
        public KMethod TopStmtTyCheck { get; set; }
        public KMethod StmtTyCheck { get; set; }
        public KMethod ExprTyCheck { get; set; }
    }

    public class KonohaSpace
    {
        private Context ctx;

        public KonohaSpace(Context ctx)
        {
            this.ctx = ctx;
        }

        public void Eval(string script)
        {
            var tokenizer = new Tokenizer(ctx, this);
            var parser = new Parser(ctx, this);
            var tokens = tokenizer.Tokenize(script);
            var block = parser.CreateBlock(null, tokens, 0, tokens.Count(), ';');
        }

        internal Syntax GetSyntaxRule(IList<Token> tls, int s, int e)
        {
            Token tk = tls[s];
            if (tk.IsType)
            {
                tk = (s + 1 < e) ? tls[s + 1] : null;
                if (tk.Type == TokenType.SYMBOL || tk.Type == TokenType.USYMBOL)
                {
                    tk = (s + 2 < e) ? tls[s + 2] : null;
                    if (tk.Type == TokenType.AST_PARENTHESIS || tk.KeyWord == KeywordType.DOT)
                    {
                        return GetSyntax(KeywordType.StmtMethodDecl); //
                    }
                    return GetSyntax(KeywordType.StmtTypeDecl);  //
                }
                return GetSyntax(KeywordType.Expr);  // expression
            }
            Syntax syn = GetSyntax(tk.KeyWord);
            if (syn.syntaxRuleNULL == null)
            {
                //wDBG_P("kw='%s', %d, %d", T_kw(syn.KeyWord), syn.ParseExpr == kmodsugar.UndefinedParseExpr, kmodsugar.UndefinedExprTyCheck == syn.ExprTyCheck);
                int i;
                for (i = s + 1; i < e; i++)
                {
                    tk = tls[i];
                    syn = GetSyntax(tk.KeyWord);
                    if (syn.syntaxRuleNULL != null && syn.priority > 0)
                    {
                        ctx.SUGAR_P(ReportLevel.DEBUG, tk.ULine, tk.Lpos, "binary operator syntax kw='%s'", syn.KeyWord.ToString());   // sugar $expr "=" $expr;
                        return syn;
                    }
                }
                return GetSyntax(KeywordType.Expr);
            }
            return syn;
        }

        internal Syntax GetSyntax(KeywordType keyword)
        {
            throw new NotImplementedException();
        }
    }
}
