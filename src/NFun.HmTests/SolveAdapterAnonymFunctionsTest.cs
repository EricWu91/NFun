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
            //3      0    2       1
            //y = [0,2].filter(x=>x>0)
            solver.SetConst(0, FType.ArrayOf(FType.Int32));
            solver.SetStrict(1, FType.Fun(FType.Generic(0), FType.Bool));
            solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(0)),
                FType.ArrayOf(FType.Generic(0)),
                FType.Fun(FType.Generic(0), FType.Bool),
            }, new[] {2, 0, 1}));
            
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32),         res.GetVarType("y"));
            Assert.AreEqual(FType.Fun(FType.Int32, FType.Bool), res.GetNodeType(1));
        }
        [Test]
        public void MultiSolvingWithMap_Resolved()
        {
            //3      0    2   1
            //y = [0,2].Map(x=>x>0)
            var anonymFunSolver = new NsHumanizerSolver();
            
            //######### SOLVING ANONYMOUS ################
            // 3    021
            //out = x>0
            anonymFunSolver.SetVarType("x", FType.Generic(0));
            anonymFunSolver.SetVar(0, "x");
            anonymFunSolver.SetConst(1, FType.Int32);
            anonymFunSolver.SetCall(new CallDef(new[] {FType.Bool, FType.Int32, FType.Int32}, new []{2, 0, 1}));
            var anonymSolvation =  anonymFunSolver.Solve();
            var anonymFunDef =  anonymSolvation.MakeFunDefenition();
            //###############################################
            
            solver.SetConst(0, FType.ArrayOf(FType.Int32));
            solver.SetStrict(1, anonymFunDef);
            solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(0)),
                FType.ArrayOf(FType.Generic(0)),
                anonymFunDef
            }, new[] {2, 0, 1}));
            
            solver.SetDefenition("y", 3, 2);
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32),         res.GetVarType("y"));
        }
        
        [Test]
        public void MultiSolvingWithMapAndClosure_Resolved()
        {
            //3   0  2    1
            //y = a.Map(x=>x*input)

            solver.SetVar(0, "a");
            //######### SOLVING ANONYMOUS ################
            var anonymFunSolver = new NsHumanizerSolver();
            // 3    021
            //out = x>input
            anonymFunSolver.SetVar(0, "x");
            var closured = solver.GetByVar("input");

            anonymFunSolver.SetNode(1,closured);
            anonymFunSolver.SetArithmeticalOp(2, 0, 1);

            var anonymSolve = anonymFunSolver.Solve();
            var anonymFunDef =  anonymSolve.MakeFunDefenition();
            //###############################################
            solver.SetStrict(1, anonymFunDef);
            Assert.IsTrue(solver.SetCall(new CallDef(new[]
            {
                FType.ArrayOf(FType.Generic(0)),
                FType.ArrayOf(FType.Generic(1)),
                FType.Fun(FType.Generic(0),FType.Generic(1)), 
            }, new[] {2, 0, 1})));
            
            Assert.IsTrue(solver.SetDefenition("y", 3, 2));
            
            var res = solver.Solve();
            Assert.AreEqual(0,res.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Real),         res.GetVarType("y"));
            Assert.AreEqual(FType.ArrayOf(FType.Real),         res.GetVarType("a"));
            Assert.AreEqual(FType.Real,         res.GetVarType("input"));
        }
    }
}