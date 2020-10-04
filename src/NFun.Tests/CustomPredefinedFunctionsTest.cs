using System;
using System.Collections.Generic;
using System.Linq;
using NFun;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class CustomPredefinedFunctionsTest
    {
        [Test]
        public void CustomNonGenericFunction_CallsWell()
        {
            string customName = "lenofstr";
            string arg = "some very good string";
            var runtime = FunBuilder
                .With($"y = {customName}('{arg}')")
                .WithFunctions(
                new FunctionMock(
                    args => ((IFunArray)args[0]).Count, 
                    customName, 
                    VarType.Int32, 
                    VarType.Text)).Build();
           
            runtime.Calculate().AssertReturns(VarVal.New("y", arg.Length));
        }

        [TestCase("[0x1,2,3,4]", new[] { 1, 3 })]
        [TestCase("[0x0,1,2,3,4]", new[] { 0, 2, 4 })]
        [TestCase("['0','1','2','3','4']", new[] { "0", "2", "4" })]
        [TestCase("[0.0]", new[] { 0.0 })]
        public void CustomGenericFunction_EachSecond_WorksFine(string arg, object expected)
        {
            string customName = "each_second";
            var runtime = FunBuilder
                .With($"y = {customName}({arg})")
                .WithFunctions(
                    new GenericFunctionMock(
                        args => new EnumerableFunArray(((IEnumerable<object>)args[0])
                            .Where((_, i) => i % 2 == 0), VarType.Anything),
                        customName,
                        VarType.ArrayOf(VarType.Generic(0)),
                        VarType.ArrayOf(VarType.Generic(0))))
                .Build();
            runtime.Calculate().AssertReturns(VarVal.New("y", expected));
        }

    }
    public class GenericFunctionMock: GenericFunctionBase
    {
        private readonly Func<object[], object> _calc;

        public GenericFunctionMock(Func<object[], object> calc,string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {
            _calc = calc;
        }

        protected override object Calc(object[] args) => _calc(args);
    }

    public class  FunctionMock: FunctionWithManyArguments
    {
        private readonly Func<object[], object> _calc;

        public FunctionMock(Func<object[], object> calc, string name, VarType returnType, params VarType[] argTypes) 
            : base(name, returnType, argTypes)
        {
            _calc = calc;
        }

        public override object Calc(object[] args) => _calc(args);
    }
}