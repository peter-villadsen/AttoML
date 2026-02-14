using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Modules
{
    public class HttpTests : AttoMLTestBase
    {
        // NOTE: These tests are integration tests that require network access
        // They may fail if httpbin.org is unavailable or network is down
        // Consider marking them with [Trait("Category", "Integration")] for selective execution

        [Fact(Skip = "Integration test - requires network access to httpbin.org")]
        public void HttpGet_ReturnsString()
        {
            var code = @"Http.get ""https://httpbin.org/get""";
            var (_, ev, expr, _) = CompileAndInitialize(code);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            var sv = Assert.IsType<StringVal>(v);
            Assert.Contains("httpbin", sv.Value);
        }

        [Fact(Skip = "Integration test - requires network access to httpbin.org")]
        public void HttpPost_WithBody_ReturnsString()
        {
            var code = @"Http.post (""https://httpbin.org/post"", ""test data"")";
            var (_, ev, expr, _) = CompileAndInitialize(code);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            var sv = Assert.IsType<StringVal>(v);
            Assert.Contains("test data", sv.Value);
        }

        [Fact(Skip = "Integration test - requires network access to httpbin.org")]
        public void HttpPostJson_ReturnsString()
        {
            var code = @"Http.postJson (""https://httpbin.org/post"", ""{\""key\"": \""value\""}"")";
            var (_, ev, expr, _) = CompileAndInitialize(code);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            var sv = Assert.IsType<StringVal>(v);
            Assert.Contains("json", sv.Value);
        }

        [Fact(Skip = "Integration test - requires network access to httpbin.org")]
        public void HttpGetWithHeaders_ReturnsString()
        {
            var code = @"
                let headers = [(""User-Agent"", ""AttoML/1.0""), (""Accept"", ""application/json"")] in
                Http.getWithHeaders (""https://httpbin.org/headers"", headers)
            ";
            var (_, ev, expr, _) = CompileAndInitialize(code);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            var sv = Assert.IsType<StringVal>(v);
            Assert.Contains("AttoML/1.0", sv.Value);
        }

        [Fact]
        public void HttpGet_InvalidUrl_ThrowsHttpException()
        {
            var code = @"Http.get ""http://invalid-domain-that-does-not-exist-12345.com""";
            var (_, ev, expr, _) = CompileAndInitialize(code);

            var ex = Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exVal = Assert.IsType<AdtVal>(ex.Exn);
            Assert.Equal("Http", exVal.Ctor);
        }

        [Fact]
        public void HttpModuleExists()
        {
            // Just verify the module is loaded and has expected functions
            var code = @"Http.get";
            var (_, ev, expr, _) = CompileAndInitialize(code);
            var v = ev.Eval(expr!, ev.GlobalEnv);

            Assert.IsType<ClosureVal>(v);
        }
    }
}
