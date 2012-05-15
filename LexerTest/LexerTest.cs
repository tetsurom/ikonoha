using IronKonoha;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LexerTest
{
    
    
    /// <summary>
    ///LexerTest のテスト クラスです。すべての
    ///LexerTest 単体テストをここに含めます
    ///</summary>
    [TestClass()]
    public class LexerTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///現在のテストの実行についての情報および機能を
        ///提供するテスト コンテキストを取得または設定します。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 追加のテスト属性
        // 
        //テストを作成するときに、次の追加属性を使用することができます:
        //
        //クラスの最初のテストを実行する前にコードを実行するには、ClassInitialize を使用
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //クラスのすべてのテストを実行した後にコードを実行するには、ClassCleanup を使用
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //各テストを実行する前にコードを実行するには、TestInitialize を使用
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //各テストを実行した後にコードを実行するには、TestCleanup を使用
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///TokenizeNumber のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeNumberTest()
        {
            Context_Accessor ctx = new Context_Accessor();
            Lexer_Accessor.Token token;
            Lexer_Accessor.IntegerToken tokenExpected = new Lexer_Accessor.IntegerToken(1234567890);
            Lexer_Accessor.TokenizerEnvironment tenv = new Lexer_Accessor.TokenizerEnvironment();
            tenv.bol = 0;
            tenv.Line = 0;
            tenv.TabWidth = 4;
            tenv.Source = "1234567890";
            int tokStart = 0;
            Lexer_Accessor.Method thunk = null;
            int expected = 10;
            int actual;
            actual = Lexer_Accessor.TokenizeNumber(ctx, out token, tenv, tokStart, thunk);
            Assert.IsNotNull(token);
            Assert.IsInstanceOfType(token, typeof(Lexer_Accessor.IntegerToken));
            Assert.AreEqual(tokenExpected.Value, (token as Lexer_Accessor.IntegerToken).Value);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///Tokenize のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeTest()
        {
            PrivateObject param0 = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor target = new Lexer_Accessor(param0); // TODO: 適切な値に初期化してください
            target.Tokenize();
            Assert.Inconclusive("値を返さないメソッドは確認できません。");
        }

        /// <summary>
        ///TokenizeBlock のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeBlockTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeBlock(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeComment のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeCommentTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeComment(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeDoubleQuote のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeDoubleQuoteTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeDoubleQuote(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeIndent のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeIndentTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeIndent(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeLine のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeLineTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeLine(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeNextline のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeNextlineTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeNextline(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeOneCharOperator のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeOneCharOperatorTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeOneCharOperator(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeOperator のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeOperatorTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeOperator(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeSkip のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeSkipTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeSkip(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeSlash のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeSlashTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeSlash(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeSymbol のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeSymbolTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeSymbol(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }

        /// <summary>
        ///TokenizeUndefined のテスト
        ///</summary>
        [TestMethod()]
        [DeploymentItem("IronKonoha.dll")]
        public void TokenizeUndefinedTest()
        {
            Context_Accessor ctx = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token token = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Token tokenExpected = null; // TODO: 適切な値に初期化してください
            Lexer_Accessor.TokenizerEnvironment tenv = null; // TODO: 適切な値に初期化してください
            int tokStart = 0; // TODO: 適切な値に初期化してください
            Lexer_Accessor.Method thunk = null; // TODO: 適切な値に初期化してください
            int expected = 0; // TODO: 適切な値に初期化してください
            int actual;
            actual = Lexer_Accessor.TokenizeUndefined(ctx, out token, tenv, tokStart, thunk);
            Assert.AreEqual(tokenExpected, token);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("このテストメソッドの正確性を確認します。");
        }
    }
}
