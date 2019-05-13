using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace TysoTake2.TypeSolvingNodes.Tests
{
    public class SolveAdapterAnonymFunctionsTest
    {
        private NsHumanizerSolver solver;

        [SetUp]
        public void Init()
        {
            solver = new NsHumanizerSolver();
        }
        [Test]
        public void FilterFunction_Resolved()
        {
            //6      0    5     4 132
            //y = [0,2].filter(x=>x>0)
            solver.SetConst(0, FType.ArrayOf(FType.Int32));
            
            var xGeneric =  solver.SetNewVar("4:x");
            Assert.IsTrue(solver.InitLambda(4, 3, new[] {xGeneric}));
            
            solver.SetVar(1, "4:x");
            solver.SetConst(2, FType.Int32);
            solver.SetCall(new CallDef(new[] {FType.Bool, FType.Int32, FType.Int32}, new []{3,1,2}));
            

            solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(0)),
                FType.ArrayOf(FType.Generic(0)),
                FType.Fun(FType.Bool,FType.Generic(0)),
            }, new[] {5, 0, 4}));
            
            solver.SetDefenition("y", 6, 5);
            
            var res = solver.Solve();
            Assert.IsTrue(res.IsSolved);
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32),         res.GetVarType("y"));
            Assert.AreEqual(FType.Fun(FType.Bool,FType.Int32), res.GetNodeType(4));
            Assert.AreEqual(FType.Int32, res.GetVarType("4:x"));
            
        }
    }
}