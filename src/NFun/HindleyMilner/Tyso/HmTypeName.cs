namespace NFun.HindleyMilner.Tyso
{
    public class HmTypeName
    {
        public HmTypeName(string id, int start, int finish, int depth, HmTypeName parent)
        {
            Id = id;
            Start = start;
            Finish = finish;
            Depth = depth;
            Parent = parent;
        }

        public const string AnyId = "any";
        public const string RealId = "real";
        public const string SomeIntegerId = "[someInteger]";
        public const string Int64Id = "int64";
        public const string Int32Id = "int32";
        public const string Int16Id = "int16";
        public const string Int8Id = "int8";
        public const string UInt64Id = "uint64";
        public const string UInt32Id = "uint32";
        public const string UInt16Id = "uint16";
        public const string UInt8Id = "uint8";
        public const string BoolId = "bool";
        public const string TextId = "text";
        public const string ArrayId = "array";
        public const string FunId = "fun";
        public const string CharId = "char";
        
        public static HmTypeName Any => new HmTypeName(AnyId,0,35,0, null);
        public static HmTypeName Real => new HmTypeName(RealId,1,20,1, Any);
        public static HmTypeName SomeInteger => new HmTypeName(SomeIntegerId,2,19,2, Real);
        public static HmTypeName Int64 => new HmTypeName(Int64Id,3,16,3, SomeInteger);
        public static HmTypeName Int32 => new HmTypeName(Int32Id,4,13,4, Int64);
        public static HmTypeName Int16 => new HmTypeName(Int16Id,5,10,5, Int32);
        public static HmTypeName Int8 => new HmTypeName(Int8Id,6,7,6, Int16);
        public static HmTypeName Uint8 => new HmTypeName(UInt8Id,8,9,10, Uint16);
        public static HmTypeName Uint16 => new HmTypeName(UInt16Id,11,12,9, Uint32);
        public static HmTypeName Uint32 => new HmTypeName(UInt32Id,14,15,8, Uint64);
        public static HmTypeName Uint64 => new HmTypeName(UInt64Id,17,18,7, SomeInteger);
        public static HmTypeName Char => new HmTypeName(CharId,21,22,1, Any);
        public static HmTypeName Bool => new HmTypeName(BoolId,23,24,1, Any);
        public static HmTypeName Complex => new HmTypeName("[someComplex]",25,34,1, Any);
        public static HmTypeName Array => new HmTypeName(ArrayId,26,29,2, Any);
        public static HmTypeName Function => new HmTypeName(FunId,30,31,2, Any);
        public static HmTypeName Generic(int num) => new HmTypeName("T" + num, -2, -1, -1, null);
        public bool IsGeneric => Start == -2 && Finish == -1;
        public string Id { get; }
        public int Start { get; }
        public int Depth { get; }
        public HmTypeName Parent { get; }
        public int Finish { get; }
        public override string ToString() => Id;
        
        public bool CanBeConvertedTo(HmTypeName baseType)
        {
            if (Start >= baseType.Start && Finish <= baseType.Finish)
                return true;

            switch (Id)
            {
                //Special uint convertion
                case UInt8Id:
                    return baseType.Id == UInt16Id || baseType.Id == UInt32Id || baseType.Id == UInt64Id;
                case UInt16Id:
                    return  baseType.Id == UInt32Id || baseType.Id == UInt64Id;
                case UInt32Id:
                    return  baseType.Id == UInt64Id;
                default:
                    return false;
            }
        } 
        public override bool Equals(object obj)
        {
            return (obj is HmTypeName type) && type.Start == Start && type.Finish == Finish && type.Id== Id;
        }

        protected bool Equals(HmTypeName other)
        {
            return string.Equals(Id, other.Id) && Start == other.Start && Finish == other.Finish;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Start;
                hashCode = (hashCode * 397) ^ Finish;
                return hashCode;
            }
        }
    }
}