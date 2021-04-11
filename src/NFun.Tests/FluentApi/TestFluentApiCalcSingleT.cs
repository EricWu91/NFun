using NFun.FluentApi;
using NUnit.Framework;

namespace NFun.Tests.FluentApi
{
    public class TestFluentApiCalcSingleT
    {
        [Test]
        public void Smoke()
        {
            var input = new UserInputModel("vasa", 13);
            var result = Funny.Calc("(age == 13) and (name == 'vasa')", input);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void ArrayTransforms()
        {
            var result = Funny.Calc(
                "ids.count{it>2}", new UserInputModel("vasa", 13, size: 21, iq: 12, 1, 2, 3, 4));
            Assert.AreEqual(2, result);
        }

        [Test]
        public void ReturnsIntArray()
        {
            var result = Funny.Calc(
                "ids.filter{it>age}.map{it**2}", new UserInputModel("vasa", 1, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[] {4, 9, 16}, result);
        }

        [Test]
        public void ReturnsText()
        {
            var result = Funny.Calc(
                "ids.reverse().join(',')", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual("4,3,2,1", result);
        }

        [Test]
        public void ReturnsConstantText()
        {
            var result = Funny.Calc(
                "'Hello world'", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual("Hello world", result);
        }

        [Test]
        public void ReturnsConstantArrayOfTexts()
        {
            var result = Funny.Calc(
                "['Hello','world']", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[] {"Hello", "world"}, result);
        }

        [Test]
        public void ReturnsArrayOfTexts()
        {
            var result = Funny.Calc(
                "ids.map{it.toText()} ", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[] {"1", "2", "3", "4"}, result);
        }

        [Test]
        public void ReturnsComplexIntArrayConstant()
        {
            var result = Funny.Calc(
                "[[[1,2],[]],[[3,4]],[[]]]", new UserInputModel("vasa", 13, size: 21, iq: 1, 1, 2, 3, 4));
            Assert.AreEqual(new[]
            {
                new[] {new[] {1, 2}, new int[0]},
                new[] {new[] {3, 4}},
                new[] {new int[0]}
            }, result);
        }
        [TestCase("")]
        [TestCase("x:int;")]
        [TestCase("x:int = 2")]
        [TestCase("a = 12; b = 32; x = a*b")]
        [TestCase("y = name")]
        public void NoOutputSpecified_throws(string expr) 
            => Assert.Throws<FunInvalidUsageTODOException>(() => Funny.Calc(expr,new UserInputModel("vasa")));
        
        [Test]
        public void OutputTypeContainsNoEmptyConstructor_throws() =>
            Assert.Throws<FunInvalidUsageTODOException>(() => Funny.Calc(
                "@{name = name}"
                , new UserInputModel("vasa")));

        [TestCase("age>someUnknownvariable")]
        public void UseUnknownInput_throws(string expression) =>
            Assert.Throws<FunInvalidUsageTODOException>(() =>
                Funny.Calc(expression, new UserInputModel(age: 22)));
    }
}