using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace IronKonoha
{
    /// <summary>
    /// Create Konoha AST from sourcecode.
    /// </summary>
    public class Parser
    {

        private Context ctx;
        private KonohaSpace ks;

        public Parser(Context ctx, KonohaSpace ks)
        {
            this.ctx = ctx;
            this.ks = ks;
        }

        public KonohaExpr ParseExpr(String str)
        {
            if (str == null)
            {
                throw new ArgumentException("str must not be null.");
            }
            return null;
            //return ParseExprAux(new Tokenizer(str));
        }

        private KonohaExpr ParseExprAux(Tokenizer lexer)
        {
            return null;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// トークン列をパースしてブロックを得る
        /// </summary>
        /// <param name="parent">親のステートメント</param>
        /// <param name="tokens">トークン列</param>
        /// <param name="start">開始トークン位置</param>
        /// <param name="end">終了トークンの次の位置</param>
        /// <param name="delim">デリミタ</param>
        /// <returns></returns>
        public BlockExpr CreateBlock(KonohaExpr parent, IList<Token> tokens, int start, int end, char delim)
        {
            BlockExpr block = new BlockExpr();
            block.parent = parent;
            int indent = 0;
            for (int i = start; i < end; )
            {
                Token error;
                i = SelectStatementLine(ref indent, tokens, i, end, delim, tokens, out error);
            }
            return block;
        }

        private int SelectStatementLine(ref int indent, IList<Token> tokens, int start, int end, char delim, IList<Token>tokensDst, out Token error)
        {
            int i = start;
	        Debug.Assert(end <= tokens.Count);
	        for(; i < end - 1; i++) {
		        Token tk = tokens[i];
                Token tk1 = tokens[i + 1];
		        if(tk.KeyWord != null) break;  // already parsed
                if (tk.TopChar == '@' && (tk1.Type == TokenType.SYMBOL || tk1.Type == TokenType.USYMBOL))
                {
                    tk1.Type = TokenType.METANAME; tk1.KeyWord = 0;
                    tokensDst.Add(tk1); i++;
                    if (i + 1 < end && tokens[i + 1].TopChar == '(')
                    {
                        i = makeTree(TokenType.AST_PARENTHESIS, tokens, i + 1, end, ')', tokensDst, out error);
                    }
                    continue;
                }
                if (tk.Type == TokenType.METANAME)
                {  // already parsed
                    tokensDst.Add(tk);
                    if (tk1.Type == TokenType.AST_PARENTHESIS)
                    {
                        tokensDst.Add(tk1);
                        i++;
                    }
                    continue;
                }
		        if(tk.Type != TokenType.INDENT) break;
		        if(indent == 0) indent = tk.Lpos;
	        }
	        for(; i < end ; i++) {
		        var tk = tokens[i];
		        if(tk.TopChar == delim && tk.Type == TokenType.OPERATOR) {
                    error = null;
			        return i+1;
		        }
		        if(tk.KeyWord != null) {
			        tokensDst.Add(tk);
			        continue;
		        }
		        else if(tk.TopChar == '(') {
			        i = makeTree(TokenType.AST_PARENTHESIS, tokens,  i, end, ')', tokensDst, out error);
			        continue;
		        }
		        else if(tk.TopChar == '[') {
			        i = makeTree(TokenType.AST_BRANCET, tokens, i, end, ']', tokensDst, out error);
			        continue;
		        }
		        else if(tk.Type == TokenType.ERR) {
			        error = tk;
		        }
		        if(tk.Type == TokenType.INDENT) {
			        if(tk.Lpos <= indent) {
				        Debug.WriteLine(string.Format("tk.Lpos=%d, indent=%d", tk.Lpos, indent));
                        error = null;
				        return i+1;
			        }
			        continue;
		        }
		        i = appendKeyword(tokens, i, end, tokensDst, out error);
	        }
            //---
            //indent = 0;
            error = null;
            return i;
        }

        private int makeTree(TokenType tokentype, IList<Token> tokens, int start, int end, char closeChar, IList<Token> tokensDst, out Token error)
        {
	        int i, probablyCloseBefore = end - 1;
	        Token tk = tokens[start];
	        Debug.Assert(tk.KeyWord == null);
        //	if(TokenType.AST_PARENTHESIS <= tk.Type && tk.Type <= AST_BRACE) {  // already transformed
        //		tokensDst.Add(tk);
        //		return s;
        //	}
            Token tkP = new Token(tokentype, tk.Text, closeChar) { KeyWord = (KeywordType)tokentype };
	        tokensDst.Add(tkP);
            tkP.Sub = new List<Token>();
	        for(i = start + 1; i < end; i++) {
		        tk = tokens[i];
		        Debug.Assert(tk.KeyWord == null);
		        if(tk.Type == TokenType.ERR) break;  // ERR
		        Debug.Assert(tk.TopChar != '{');
		        if(tk.TopChar == '(') {
			        i = makeTree(TokenType.AST_PARENTHESIS, tokens, i, end, ')', tkP.Sub, out error);
			        continue;
		        }
		        else if(tk.TopChar == '[') {
			        i = makeTree(TokenType.AST_BRANCET, tokens, i, end, ']', tkP.Sub, out error);
			        continue;
		        }
		        else if(tk.TopChar == closeChar) {
                    error = null;
			        return i;
		        }
		        if((closeChar == ')' || closeChar == ']') && tk.Type == TokenType.CODE) probablyCloseBefore = i;
		        if(tk.Type == TokenType.INDENT && closeChar != '}') continue;  // remove INDENT;
		        i = appendKeyword(tokens, i, end, tkP.Sub, out error);
	        }
	        if(tk.Type != TokenType.ERR) {
		        //size_t errref = SUGAR_P(ERR_, tk.ULine, tk.Lpos, "'%c' is expected (probably before %s)", closech, kToken_s(tls[probablyCloseBefore]));
		        //Token_toERR(_ctx, tkP, errref);
	        }
	        else {
		        tkP.Type = TokenType.ERR;
		        //tkP.Text = tk.Text;
	        }
	        error = tkP;
	        return end;
        }

        private int appendKeyword(IList<Token> tls, int s, int e, IList<Token> dst, out Token tkERR)
        {
	        int next = s; // don't add
	        Token tk = tls[s];
	        if(tk.Type < TokenType.OPERATOR) {
		        tk.KeyWord = (KeywordType)tk.Type;
	        }
	        if(tk.Type == TokenType.SYMBOL) {
		        Token_resolved(tk);
	        }
	        else if(tk.Type == TokenType.USYMBOL) {
		        if(!Token_resolved(tk)) {
			        //KonohaClass ct = kKonohaSpace_getCT(ks, null/*FIXME*/, tk.Text, tk.Text.Length, TY_unknown);
                    object ct = null;
                    if(ct != null) {
				        tk.KeyWord = KeywordType.Type;
				        //tk.Type = ct->cid;
			        }
		        }
	        }
	        else if(tk.Type == TokenType.OPERATOR) {
		        if(!Token_resolved(tk)) {
			        //size_t errref = SUGAR_P(ERR_, tk.ULine, tk.Lpos, "undefined token: %s", kToken_s(tk));
			        //Token_toERR(_ctx, tk, errref);
			        tkERR = tk;
			        return e;
		        }
	        }
	        else if(tk.Type == TokenType.CODE) {
                tk.KeyWord = KeywordType.Brace;
	        }
	        if(tk.KeyWord == KeywordType.Type) {
		        while(next + 1 < e) {
			        Token tkN = tls[next + 1];
			        if(tkN.TopChar != '[') break;
                    List<Token> abuf = new List<Token>();
			        int atop = abuf.Count;
			        next = makeTree(TokenType.AST_BRANCET, tls,  next+1, e, ']', abuf, out tkERR);
			        if(abuf.Count > atop) {
				        tk = Token_resolveType(tk, abuf[atop]);
			        }
		        }
	        }
	        if(tk.KeyWord > KeywordType.Expr) {
		        dst.Add(tk);
	        }
            tkERR = null;
	        return next;
        }

        private Token Token_resolveType(Token tk, Token tkP)
        {
	        int i;
            int psize= 0;
            int size = tkP.Sub.Count;
	        KonohaParam[] p = new KonohaParam[size];
	        for(i = 0; i < size; i++) {
		        Token tkT = (tkP.Sub[i]);
                if (tkT.KeyWord == KeywordType.Type)
                {
			        p[psize].Type = tkT.Type;
			        psize++;
		        }
		        if(tkT.TopChar == ',') continue;
	        }
	        KonohaClass ct;
	        if(psize > 0) {
                ct = null;// this.ctx.share.ca.cts[(int)tk.Type];
		        if(ct.cparam == K_NULLPARAM) {
			        SUGAR_P(ERR_, tk.ULine, tk.Lpos, "not generic type: %s", T_ty(TK_type(tk)));
			        return tk;
		        }
		        ct = kClassTable_Generics(ct, TY_void, psize, p);
	        }
	        else {
		        ct = CT_P0(_ctx, CT_Array, TK_type(tk));
	        }
	        tk.Type = (TokenType)ct.cid;
	        return tk;
        }

        private bool Token_resolved(Token tk)
        {
            throw new NotImplementedException();
        }
    }

    public class KonohaParam
    {

        public TokenType Type { get; set; }
    }

    public class KonohaClass {
        public int cid { get; set; }
        public KonohaParam cparam { get; set; }
    }

    public abstract class KonohaExpr { }

    public class BlockExpr : KonohaExpr
    {
        public KonohaExpr parent { get; set; }
    }
}
