using System;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class TestFluentApiAddConstant
    {
        [Test]
        public void Smoke()
        {
            var calculator = Funny
                .WithConstant("age", 100)
                .WithConstant("name", "vasa")
                .BuildForCalc<ModelWithInt, string>();

            Func<ModelWithInt, string> lambda = calculator.ToLambda("'{name}\\'s id is {id} and age is {age}'");
            var result1 = lambda(new ModelWithInt { id = 42 });
            var result2 = lambda(new ModelWithInt { id = 1 });
            Assert.AreEqual(result1, "vasa's id is 42 and age is 100");
            Assert.AreEqual(result2, "vasa's id is 1 and age is 100");
        }


        [TestCase("id", "id")]
        [TestCase("Id", "id")]
        [TestCase("Id", "Id")]
        [TestCase("id", "Id")]
        public void InputNameOverridesConstant(string constantName, string varName)
        {
            var calculator = Funny
                .WithConstant(constantName, 100)
                .BuildForCalc<ModelWithInt, string>();

            Func<ModelWithInt, string> lambda = calculator.ToLambda("'id= {" + varName + "}'");

            var result = lambda(new ModelWithInt { id = 42 });
            Assert.AreEqual(result, "id= 42");

            var result2 = lambda(new ModelWithInt { id = 1 });
            Assert.AreEqual(result2, "id= 1");
        }


        [TestCase("id", "id")]
        [TestCase("Id", "id")]
        [TestCase("Id", "Id")]
        [TestCase("id", "Id")]
        public void OutputNameOverridesConstant(string constantName, string varName)
        {
            var calculator = Funny
                .WithConstant(constantName, 100)
                .BuildForCalcMany<UserInputModel, ContractOutputModel>();

            var lambda = calculator.ToLambda($"{varName}= age");

            var result1 = lambda(new UserInputModel(age: 42));
            Assert.AreEqual(result1.Id, 42);
            var result2 = lambda(new UserInputModel(age: 11));
            Assert.AreEqual(result2.Id, 11);
        }
    }
}