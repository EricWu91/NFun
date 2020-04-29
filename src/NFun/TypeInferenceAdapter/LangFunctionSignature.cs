using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class LangFunctionSignature{
               public readonly VarType ReturnType;
               public readonly VarType[] ArgTypes;
               public LangFunctionSignature(VarType output, VarType[] inputs)
               {
                   ReturnType = output;
                   ArgTypes = inputs;
               }
       }
}