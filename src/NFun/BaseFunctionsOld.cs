using NFun.BuiltInFunctions;
using NFun.Interpritation.Functions;

namespace NFun
{
    public static class BaseFunctions
    {
        public static GenericFunctionBase[] GenericFunctions { get; } =
        {
            new EqualFunction(),
            new NotEqualFunction(),
            new MoreFunction(),
            new MoreOrEqualFunction(),
            new LessFunction(),
            new LessOrEqualFunction(),

            new AddFunction(),
            new SubstractFunction(),
            new MultiplyFunction(),
            new IsInSingleGenericFunctionDefenition(), 
            //new IsInMultipleGenericFunctionDefenition(), 
            new UniqueGenericFunctionDefenition(),
            new UniteGenericFunctionDefenition(),
            new IntersectGenericFunctionDefenition(),
            new SubstractArraysGenericFunctionDefenition(), 
            
            // new ConcatArraysGenericFunctionDefenition(CoreFunNames.ArrConcat), 
            new ConcatArraysGenericFunctionDefenition("concat"),

            new SetGenericFunctionDefenition(),
            new GetGenericFunctionDefenition(),
            new SliceGenericFunctionDefenition(),
            new SliceWithStepGenericFunctionDefenition(),
            new FindGenericFunctionDefenition(),
            new ReduceWithDefaultsGenericFunctionDefenition(),
            new ReduceGenericFunctionDefenition(),
            new TakeGenericFunctionDefenition(),
            new SkipGenericFunctionDefenition(),
            new RepeatGenericFunctionDefenition(),
            new FilterGenericFunctionDefenition(),
            new FlatGenericFunctionDefenition(),
            new ChunkGenericFunctionDefenition(),
            new MapFunction(),
            new AllGenericFunctionDefenition(),
            new AnyGenericFunctionDefenition(),
            new ReverseGenericFunctionDefenition(),
        };

        public static FunctionBase[] ConcreteFunctions { get; } =
        {
            new NotFunction(),
            new AndFunction(),
            new OrFunction(),
            new XorFunction(),
            
            //new AbsOfRealFunction(),
            //new AbsOfIntFunction(),

            //new AddRealFunction(),
            //new AddIntFunction(),
            //new AddInt64Function(),
            //new AddUInt8Function(),
            //new AddUInt32Function(),
            //new AddUInt64Function(),

           

            //new NegateOfInt16Function(),
            //new NegateOfInt32Function(),
            //new NegateOfUInt8Function(),
            //new NegateOfUInt32Function(),
            //new NegateOfInt64Function(),

            //new NegateOfRealFunction(),

            //new BitShiftLeftInt32Function(),
            //new BitShiftLeftInt64Function(),
            //new BitShiftRightInt32Function(),
            //new BitShiftRightInt64Function(),
            //new BitAndIntFunction(),
            //new BitAndInt64Function(),
            //new BitAndUInt8Function(),
            //new BitAndUInt16Function(),
            //new BitAndUInt32Function(),
            //new BitAndUInt64Function(),
            //new BitOrInt32Function(),
            //new BitOrInt64Function(),
            //new BitOrUInt8Function(),
            //new BitOrUInt16Function(),
            //new BitOrUInt32Function(),
            //new BitOrUInt64Function(),
            //new BitXorIntFunction(),
            //new BitXorInt64Function(),
            //new BitXorUInt8Function(),
            //new BitXorUInt16Function(),
            //new BitXorUInt32Function(),
            //new BitXorUInt64Function(),
            //new BitInverseIntFunction(),
            //new BitInverseInt64Function(),
            //new BitInverseUInt8Function(),
            //new BitInverseUInt16Function(),
            //new BitInverseUInt32Function(),
            //new BitInverseUInt64Function(),

            new PowRealFunction(),

           

            //new RemainderRealFunction(),
            //new RemainderInt16Function(),
            //new RemainderInt32Function(),
            //new RemainderInt64Function(),

            //new RemainderUInt8Function(),
            //new RemainderUInt16Function(),
            //new RemainderUInt32Function(),
            //new RemainderUInt64Function(),

            new DivideRealFunction(),
            new SqrtFunction(),

            new SinFunction(),
            new CosFunction(),
            new TanFunction(),
            new AtanFunction(),
            new Atan2Function(),
            new AsinFunction(),
            new AcosFunction(),
            new ExpFunction(),
            new LogFunction(),
            new LogEFunction(),
            new Log10Function(),
            //new FloorFunction(),
            //new CeilFunction(),
            //new RoundToIntFunction(),
            //new RoundToRealFunction(),
            //new SignFunction(),

            //new ToTextFunction(),
            //new ToIntFromRealFunction("toInt"),
            //new ToIntFromRealFunction(),
            //new ToIntFromTextFunction("toInt"),
            //new ToIntFromTextFunction(),

            //new ToIntFromBytesFunction(),
            //new ToRealFromTextFunction(),
            //new ToUtf8Function(),
            //new ToUnicodeFunction(),
            //new ToBytesFromIntFunction(),
            //new ToBitsFromIntFunction(), 
                
            //We need these function to allow user convert numbers.
            //a = toInt16(b) #no matter what is b - it will be casted
                
            //Safe converters
            //new ToInt16FromInt16Function(),
            //new ToInt32FromInt32Function(),
            //new ToInt32FromInt32Function("toInt"),
            //new ToInt64FromInt64Function(),
            //new ToUint16FromUint16Function(),
            //new ToUint32FromUint32Function(),
            //new ToUint64FromUint64Function(),
            //new ToRealFromRealFunction(),

            //Unsafe converters
            //new ToInt16FromInt64Function(),
            //new ToInt32FromInt64Function(),
            //new ToInt32FromInt64Function("toInt"),
            //new ToInt64FromInt64Function(),
            //new ToInt16FromUInt64Function(),
            //new ToInt32FromUInt64Function(),
            //new ToInt32FromUInt64Function("toInt"),
            //new ToInt64FromUInt64Function(),
            //new ToUint8FromInt64Function("toByte"),
            //new ToUint8FromInt64Function(),
            //new ToUint16FromInt64Function(),
            //new ToUint32FromInt64Function(),
            //new ToUint64FromInt64Function(),
            //new ToUint8FromUint64Function("toByte"),
            //new ToUint8FromUint64Function(),
            //new ToUint16FromUint64Function(),
            //new ToUint32FromUint64Function(),
            //new ToUint64FromUint64Function(),

            //new EFunction(),
            //new PiFunction(),
            new CountFunction(),
            new AverageFunction(),
            //new MaxOfIntFunction(),
            //new MaxOfInt64Function(),
            //new MaxOfRealFunction(),
            //new MinOfIntFunction(),
            //new MinOfInt64Function(),
            //new MinOfRealFunction(),
            new MultiMaxIntFunction(),
            new MultiMaxRealFunction(),
            new MultiMinIntFunction(),
            new MultiMinRealFunction(),
            new MultiSumIntFunction(),
            new MultiSumRealFunction(),
            new MedianIntFunction(),
            new MedianRealFunction(),
            new AnyFunction(),
            new SortIntFunction(),
            new SortRealFunction(),
            new SortTextFunction(),
            new RangeIntFunction(),
            new RangeWithStepIntFunction(),
            new RangeWithStepRealFunction(),
            new TrimFunction(),
            new TrimStartFunction(),
            new TrimEndFunction(),
            new SplitFunction(),
            new JoinFunction(),
            new ConcatTextsFunction()
        };
    }
}