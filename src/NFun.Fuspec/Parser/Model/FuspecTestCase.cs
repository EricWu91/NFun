using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class FuspecTestCase
    {
        public FuspecTestCase(  string name, 
                                string[] tags, 
                                string body,
                                VarInfo[] inputVarList, VarInfo[] outputVarList, 
                                SetCheckPair[] setChecks,
                                bool isTestExecuted,
                                int startLine)
        {
            Name = name;
            Tags = tags;
            Script = body;
            InputVarList = inputVarList;
            OutputVarList = outputVarList;
            SetChecks = setChecks;
            IsTestExecuted = isTestExecuted;
            StartLine = startLine;
        }
        public string Name { get; }
        public string[] Tags { get; }
        public string Script{ get; }
        
        public VarInfo[] InputVarList { get; }
        public VarInfo[] OutputVarList { get; }
        
        public SetCheckPair[] SetChecks { get; }
        
        public bool IsTestExecuted { get; }
        
        public int StartLine { get; }

    }
    

}