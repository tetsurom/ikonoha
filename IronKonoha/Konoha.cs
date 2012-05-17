using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace IronKonoha
{
    public class LineInfo
    {
        public LineInfo(int line, string file)
        {
            this.LineNumber = line;
            this.Filename = file;
        }
        public int LineNumber { get; set; }
        public string Filename { get; set; }
    }

    public class Konoha{

        public Konoha()
        {
        }

        /// <summary>
        /// １つの文を実行する。
        /// </summary>
        /// <param name="exprStr">実行する文</param>
        /// <param name="module">グローバル変数等を管理するオブジェクト</param>
        /// <returns>実行結果</returns>
        public object ExecuteExpr(string exprStr, ExpandoObject module)
        {
            throw new NotImplementedException();
        }

        public static ExpandoObject CreateScope()
        {
            return new ExpandoObject();
        }
    }
}
