using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IronKonoha
{
    public class KonohaStatement : KonohaExpr{
        public Syntax syn { get; set; }
        public LineInfo ULine { get; set; }
        public KonohaSpace ks { get; set; }

        public KonohaStatement(LineInfo line)
        {
            this.ULine = line;
        }

        public bool parseSyntaxRule(Context ctx, IList<Token> tls, int s, int e)
        {
	        bool ret = false;
	        Syntax syn = this.ks.GetSyntaxRule(tls, s, e);
	        Debug.Assert(syn != null);
	        if(syn.syntaxRuleNULL != null) {
		        this.syn = syn;
		        ret = (matchSyntaxRule(ctx, syn.syntaxRuleNULL, this.ULine, tls, s, e, false) != -1);
	        }
	        else {
		        ctx.SUGAR_P(ReportLevel.ERR, this.ULine, 0, "undefined syntax rule for '%s'", syn.KeyWord.ToString());
	        }
	        return ret;
        }

        public int matchSyntaxRule(Context ctx, IList<Token> rules, LineInfo /*parent*/uline, IList<Token> tls, int s, int e, bool optional)
        {
	        int ri, ti, rule_size = rules.Count;
	        ti = s;
	        for(ri = 0; ri < rule_size && ti < e; ri++) {
		        Token rule = rules[ri];
		        Token tk = tls[ti];
		        uline = tk.ULine;
		        //DBG_P("matching rule=%d,%s,%s token=%d,%s,%s", ri, T_tt(rule.Type), T_kw(rule.KeyWord), ti-s, T_tt(tk.Type), kToken_s(tk));
		        if(rule.Type == TokenType.CODE) {
			        if(rule.KeyWord != tk.KeyWord) {
				        if(optional) return s;
				        tk.Print(ctx, ReportLevel.ERR, "%s needs '%s'", this.syn.KeyWord, rule.KeyWord);
				        return -1;
			        }
			        ti++;
			        continue;
		        }
		        else if(rule.Type == TokenType.METANAME) {
			        Syntax syn = this.ks.GetSyntax(rule.KeyWord);
			        if(syn == null || syn.ParseStmtnull == null) {
                        tk.Print(ctx, ReportLevel.ERR, "unknown syntax pattern: %s", rule.KeyWord);
				        return -1;
			        }
			        int c = e;
			        if(ri + 1 < rule_size && rules[ri+1].Type == TokenType.CODE) {
				        c = lookAheadKeyword(tls, ti+1, e, rules[ri+1]);
				        if(c == -1) {
					        if(optional) return s;
                            tk.Print(ctx, ReportLevel.ERR, "%s needs '%s'", this.syn.KeyWord, rule.KeyWord);
					        return -1;
				        }
				        ri++;
			        }
			        int err_count = ctx.sugar.err_count;
			        int next = ParseStmt(ctx, syn, rule.nameid, tls, ti, c);
        //			DBG_P("matched '%s' nameid='%s', next=%d=>%d", Pkeyword(rule.KeyWord), Pkeyword(rule->nameid), ti, next);
			        if(next == -1) {
				        if(optional) return s;
                        if (err_count == ctx.sugarerr_count)
                        {
                            tk.Print(ctx, ReportLevel.ERR, "unknown syntax pattern: %s", this.syn.KeyWord, rule.KeyWord, tk.Text);
				        }
				        return -1;
			        }
			        ////XXX Why???
			        //optional = 0;
			        ti = (c == e) ? next : c + 1;
			        continue;
		        }
		        else if(rule.Type == TokenType.AST_OPTIONAL) {
			        int next = matchSyntaxRule(ctx, rule.Sub, uline, tls, ti, e, true);
			        if(next == -1) return -1;
			        ti = next;
			        continue;
		        }
		        else if(rule.Type == TokenType.AST_PARENTHESIS || rule.Type == TokenType.AST_BRACE || rule.Type == TokenType.AST_BRANCET) {
			        if(tk.Type == rule.Type && rule.TopChar == tk.TopChar) {
				        int next = matchSyntaxRule(ctx, rule.Sub, uline, tk.Sub, 0, tk.Sub.Count, false);
				        if(next == -1) return -1;
				        ti++;
			        }
			        else {
				        if(optional) return s;
				        //kToken_p(tk, ERR_, "%s needs '%c'", T_statement(this.syn.KeyWord), rule.TopChar);
				        return -1;
			        }
		        }
	        }
	        if(!optional) {
		        for(; ri < rules.Count; ri++) {
			        Token rule = rules[ri];
			        if(rule.Type != TokenType.AST_OPTIONAL) {
				        //SUGAR_P(ERR_, uline, -1, "%s needs syntax pattern: %s", T_statement(this.syn.KeyWord), T_kw(rule.KeyWord));
				        return -1;
			        }
		        }
		        //WARN_Ignored(_ctx, tls, ti, e);
	        }
	        return ti;
        }

        public int ParseStmt(Context ctx, Syntax syn, Symbol name, IList<Token> tls, int s, int e)
        {
            throw new NotImplementedException();
	        /*
            INIT_GCSTACK();
	        BEGIN_LOCAL(lsfp, 8);
	        KSETv(lsfp[K_CALLDELTA+0].o, (kObject*)stmt);
	        lsfp[K_CALLDELTA+0].ndata = (uintptr_t)syn;
	        lsfp[K_CALLDELTA+1].ivalue = name;
	        KSETv(lsfp[K_CALLDELTA+2].a, tls);
	        lsfp[K_CALLDELTA+3].ivalue = s;
	        lsfp[K_CALLDELTA+4].ivalue = e;
	        KCALL(lsfp, 0, syn->ParseStmtNULL, 4, knull(CT_Int));
	        END_LOCAL();
	        RESET_GCSTACK();
	        return (int)lsfp[0].ivalue;
             * */
        }

        public int lookAheadKeyword(IList<Token> tls, int s, int e, Token rule)
        {
            int i;
            for (i = s; i < e; i++)
            {
                Token tk = tls[i];
                if (rule.KeyWord == tk.KeyWord) return i;
            }
            return -1;
        }

        public int addAnnotation(Context ctx, IList<Token> tls, int s, int e)
        {
	        int i;
	        for(i = s; i < e; i++) {
		        Token tk = tls[i];
		        if(tk.Type != TokenType.METANAME) break;
		        if(i+1 < e) {
			        string buf = string.Format("@%s", tk.Text);
			        // what is FN_NEWID?
                    //KeywordType kw = keyword(ctx, buf, tk.Text.Length + 1, FN_NEWID);
			        Token tk1 = tls[i+1];
			        object value = true;//UPCAST(K_TRUE);
			        if(tk1.Type == TokenType.AST_PARENTHESIS) {
				        //value = (kObject*)Stmt_newExpr2(_ctx, stmt, tk1.Sub, 0, tk1.Sub.Count);
				        i++;
			        }
			        if(value != null) {
				        //kObject_setObject(stmt, kw, value);
			        }
		        }
	        }
	        return i;
        }

        public void ConvertToErrorToken(Context ctx, uint estart)
        {
            throw new NotImplementedException();
            //this.Type = TokenType.ERR;
            //this.Text = ctx.modlocal[MOD_suger].errors.strings[errorcode];
        }
    }

}
