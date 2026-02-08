using Xunit;
using AttoML.Interpreter.Runtime;

namespace AttoML.Tests.Modules
{
	public class MathModuleTests : AttoMLTestBase
	{
		[Fact]
		public void MathModule_Pi_ReturnsConstant()
		{
			var src = @"Math.pi";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.PI, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Pi_InCalculation()
		{
			var src = @"Math.sin Math.pi";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(0.0, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Asin_ValidInput()
		{
			var src = @"Math.asin 0.5";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.Asin(0.5), fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Asin_EdgeCase()
		{
			var src = @"Math.asin 1.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.PI / 2, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Asin_RaisesDomainForInvalidInput()
		{
			var src = @"Math.asin 2.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
		}

		[Fact]
		public void MathModule_Asin_RaisesDomainForNegativeOutOfRange()
		{
			var src = @"Math.asin (0.0 - 1.5)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
		}

		[Fact]
		public void MathModule_Acos_ValidInput()
		{
			var src = @"Math.acos 0.5";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.Acos(0.5), fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Acos_EdgeCase()
		{
			var src = @"Math.acos 0.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.PI / 2, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Acos_RaisesDomainForInvalidInput()
		{
			var src = @"Math.acos 1.5";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			Assert.Throws<AttoException>(() => ev.Eval(expr!, ev.GlobalEnv));
		}

		[Fact]
		public void MathModule_Sinh_Positive()
		{
			var src = @"Math.sinh 1.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.Sinh(1.0), fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Sinh_Zero()
		{
			var src = @"Math.sinh 0.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(0.0, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Cosh_Positive()
		{
			var src = @"Math.cosh 1.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.Cosh(1.0), fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Cosh_Zero()
		{
			var src = @"Math.cosh 0.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(1.0, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Tanh_Positive()
		{
			var src = @"Math.tanh 1.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(System.Math.Tanh(1.0), fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Tanh_Zero()
		{
			var src = @"Math.tanh 0.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(0.0, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_Tanh_ApproachesOne()
		{
			var src = @"Math.tanh 10.0";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.True(fv.Value > 0.99);
		}

		[Fact]
		public void MathModule_TrigIdentity_SinAsin()
		{
			var src = @"Math.sin (Math.asin 0.5)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(0.5, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_TrigIdentity_CosAcos()
		{
			var src = @"Math.cos (Math.acos 0.5)";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(0.5, fv.Value, precision: 10);
		}

		[Fact]
		public void MathModule_HyperbolicIdentity()
		{
			// cosh^2(x) - sinh^2(x) = 1
			var src = @"
				let x = 2.0 in
				let c = Math.cosh x in
				let s = Math.sinh x in
				(c * c) - (s * s)
			";
			var (_, ev, expr, _) = CompileAndInitialize(src);
			var v = ev.Eval(expr!, ev.GlobalEnv);
			Assert.IsType<FloatVal>(v);
			var fv = (FloatVal)v;
			Assert.Equal(1.0, fv.Value, precision: 10);
		}
	}
}
