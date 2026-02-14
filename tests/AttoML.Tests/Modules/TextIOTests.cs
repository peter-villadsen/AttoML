using System;
using System.IO;
using Xunit;
using AttoML.Core;
using AttoML.Interpreter;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Modules
{
    public class TextIOTests : AttoMLTestBase
    {
        [Fact]
        public void Print_Works()
        {
            // Redirect stdout to capture output
            var originalOut = Console.Out;
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                var (_, ev, expr, _) = CompileAndInitialize("TextIO.print \"Hello, World!\\n\"");
                var v = ev.Eval(expr!, ev.GlobalEnv);

                Assert.IsType<UnitVal>(v);
                Assert.Equal("Hello, World!\n", sw.ToString());

                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void FileWrite_Works()
        {
            var testFile = Path.GetTempFileName();
            try
            {
                var code = $@"
                    let out = TextIO.openOut ""{testFile.Replace("\\", "\\\\")}"" in
                    let _ = TextIO.output out ""Line 1\n"" in
                    let _ = TextIO.output out ""Line 2\n"" in
                    TextIO.closeOut out
                ";
                var (_, ev, expr, _) = CompileAndInitialize(code);
                var v = ev.Eval(expr!, ev.GlobalEnv);

                Assert.IsType<UnitVal>(v);
                Assert.True(File.Exists(testFile));
                var content = File.ReadAllText(testFile);
                Assert.Equal("Line 1\nLine 2\n", content);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void FileRead_Works()
        {
            var testFile = Path.GetTempFileName();
            try
            {
                // Create test file
                File.WriteAllText(testFile, "Test content\n");

                var code = $@"
                    let inStream = TextIO.openIn ""{testFile.Replace("\\", "\\\\")}"" in
                    let content = TextIO.input inStream in
                    let _ = TextIO.closeIn inStream in
                    content
                ";
                var (_, ev, expr, _) = CompileAndInitialize(code);
                var v = ev.Eval(expr!, ev.GlobalEnv);

                var sv = Assert.IsType<StringVal>(v);
                Assert.Equal("Test content\n", sv.Value);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void InputLine_ReturnsOption()
        {
            var testFile = Path.GetTempFileName();
            try
            {
                // Create test file with two lines
                File.WriteAllText(testFile, "Line 1\nLine 2\n");

                var code = $@"
                    let inStream = TextIO.openIn ""{testFile.Replace("\\", "\\\\")}"" in
                    let line1 = TextIO.inputLine inStream in
                    let _ = TextIO.closeIn inStream in
                    line1
                ";
                var (_, ev, expr, _) = CompileAndInitialize(code);
                var v = ev.Eval(expr!, ev.GlobalEnv);

                var adt = Assert.IsType<AdtVal>(v);
                Assert.Equal("Some", adt.Ctor);
                var line = Assert.IsType<StringVal>(adt.Payload);
                Assert.Equal("Line 1\n", line.Value);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void InputLine_ReturnsNone_AtEOF()
        {
            var testFile = Path.GetTempFileName();
            try
            {
                // Create empty file
                File.WriteAllText(testFile, "");

                var code = $@"
                    let inStream = TextIO.openIn ""{testFile.Replace("\\", "\\\\")}"" in
                    let result = TextIO.inputLine inStream in
                    let _ = TextIO.closeIn inStream in
                    result
                ";
                var (_, ev, expr, _) = CompileAndInitialize(code);
                var v = ev.Eval(expr!, ev.GlobalEnv);

                var adt = Assert.IsType<AdtVal>(v);
                Assert.Equal("None", adt.Ctor);
                Assert.Null(adt.Payload);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void OpenAppend_Works()
        {
            var testFile = Path.GetTempFileName();
            try
            {
                // Write initial content
                File.WriteAllText(testFile, "Line 1\n");

                var code = $@"
                    let out = TextIO.openAppend ""{testFile.Replace("\\", "\\\\")}"" in
                    let _ = TextIO.output out ""Line 2\n"" in
                    TextIO.closeOut out
                ";
                var (_, ev, expr, _) = CompileAndInitialize(code);
                var v = ev.Eval(expr!, ev.GlobalEnv);

                Assert.IsType<UnitVal>(v);
                var content = File.ReadAllText(testFile);
                Assert.Equal("Line 1\nLine 2\n", content);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void OpenOut_Truncates()
        {
            var testFile = Path.GetTempFileName();
            try
            {
                // Write initial content
                File.WriteAllText(testFile, "Old content that should be replaced\n");

                var code = $@"
                    let out = TextIO.openOut ""{testFile.Replace("\\", "\\\\")}"" in
                    let _ = TextIO.output out ""New content\n"" in
                    TextIO.closeOut out
                ";
                var (_, ev, expr, _) = CompileAndInitialize(code);
                var v = ev.Eval(expr!, ev.GlobalEnv);

                Assert.IsType<UnitVal>(v);
                var content = File.ReadAllText(testFile);
                Assert.Equal("New content\n", content);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void OpenIn_NonExistentFile_ThrowsIoException()
        {
            var code = @"
                TextIO.openIn ""/this/file/does/not/exist.txt""
            ";
            var (_, ev, expr, _) = CompileAndInitialize(code);

            var ex = Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
            var exVal = Assert.IsType<AdtVal>(ex.Exn);
            Assert.Equal("Io", exVal.Ctor);
        }

        [Fact]
        public void StandardStreams_Exist()
        {
            var (_, ev, expr1, _) = CompileAndInitialize("TextIO.stdIn");
            var v1 = ev.Eval(expr1!, ev.GlobalEnv);
            var stdin = Assert.IsType<StreamVal>(v1);
            Assert.Equal("in", stdin.Type);

            var (_, ev2, expr2, _) = CompileAndInitialize("TextIO.stdOut");
            var v2 = ev2.Eval(expr2!, ev2.GlobalEnv);
            var stdout = Assert.IsType<StreamVal>(v2);
            Assert.Equal("out", stdout.Type);

            var (_, ev3, expr3, _) = CompileAndInitialize("TextIO.stdErr");
            var v3 = ev3.Eval(expr3!, ev3.GlobalEnv);
            var stderr = Assert.IsType<StreamVal>(v3);
            Assert.Equal("out", stderr.Type);
        }
    }
}
