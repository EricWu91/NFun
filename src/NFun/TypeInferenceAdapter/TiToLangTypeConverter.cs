using System;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.Types;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.TypeInferenceAdapter
{
    public interface ITypeInferenceResultsInterpriter
    {
        VarType Convert(IState type);
    }

    public class TypeInferenceOnlyConcreteInterpriter:ITypeInferenceResultsInterpriter
    {
        public VarType Convert(IState type)
        {
            switch (type)
            {
                case RefTo refTo:
                    return Convert(refTo.Element);
                case Primitive primitive:
                    return ToConcrete(primitive.Name);
                case Constrains constrains when constrains.Prefered != null:
                    return ToConcrete(constrains.Prefered.Name);
                case Constrains constrains when !constrains.HasAncestor:
                {
                    if(constrains.IsComparable)
                        throw new NotImplementedException();
                    return VarType.Anything;
                }

                case Constrains constrains:
                {
                    if (constrains.Ancestor.Name.HasFlag(PrimitiveTypeName._isAbstract))
                    {
                        switch (constrains.Ancestor.Name)
                        {
                            case PrimitiveTypeName.I96: return VarType.Int64;
                            case PrimitiveTypeName.I48: return VarType.Int32;
                            case PrimitiveTypeName.U48: return VarType.UInt32;
                            case PrimitiveTypeName.U24: return VarType.UInt16;
                            case PrimitiveTypeName.U12: return VarType.UInt8;
                            default: throw new NotSupportedException();
                        }
                    }
                    return ToConcrete(constrains.Ancestor.Name);
                }

                case Array array:
                    return VarType.ArrayOf(Convert(array.Element));
                case Fun fun:
                    return VarType.Fun(Convert(fun.ReturnType), fun.Args.Select(Convert).ToArray());
                default:
                    throw new NotSupportedException();
            }
        }

        private static VarType ToConcrete(PrimitiveTypeName name)
        {
            switch (name)
            {
                case PrimitiveTypeName.Any: return VarType.Anything;
                case PrimitiveTypeName.Char: return VarType.Char;
                case PrimitiveTypeName.Bool: return VarType.Bool;
                case PrimitiveTypeName.Real: return VarType.Real;
                case PrimitiveTypeName.I64: return VarType.Int64;
                case PrimitiveTypeName.I32: return VarType.Int32;
                case PrimitiveTypeName.I24: return VarType.Int32;
                case PrimitiveTypeName.I16: return VarType.Int16;
                case PrimitiveTypeName.U64: return VarType.UInt64;
                case PrimitiveTypeName.U32: return VarType.UInt32;
                case PrimitiveTypeName.U16: return VarType.UInt16;
                case PrimitiveTypeName.U8: return VarType.UInt8;

                case PrimitiveTypeName.I96: return VarType.Int64; /*return VarType.Real;*/
                case PrimitiveTypeName.I48: return VarType.Int32;/*;*/
                case PrimitiveTypeName.U48: /*return VarType.Int64;*/
                case PrimitiveTypeName.U24: /*return VarType.Int32;*/
                case PrimitiveTypeName.U12: /*return VarType.Int16;*/
                    throw new InvalidOperationException("Cannot cast abstract type " + name);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    //public abstract class TiToLangTypeConverter 
    //{
    //    public static TiToLangTypeConverter SaveGenerics 
    //        => new SaveGenericsTiToLangTypeConverter();
        
    //    public static TiToLangTypeConverter SetGenericsToAny 
    //        => new SetGenericsToAnyTiToLangTypeConverter();

    //    protected abstract VarType ConvertGeneric(GenericType type);
    //    public VarType ToSimpleType(IState type)
    //    {

    //        throw new InvalidOperationException();

    //        //if (type.IsPrimitiveGeneric)
    //        //{
    //        //    if(!(type is GenericType))
    //        //        throw new InvalidOperationException($"type {type} is not instance of GenericType");
    //        //    return ConvertGeneric((GenericType) type);
    //        //}

    //        //switch (type.Name.Id)
    //        //{
    //        //    case TiTypeName.AnyId: return VarType.Anything;
    //        //    case TiTypeName.RealId: return VarType.Real;
    //        //    case TiTypeName.TextId: return VarType.Text;
    //        //    case TiTypeName.BoolId: return VarType.Bool;
    //        //    case TiTypeName.Int16Id: return VarType.Int16;
    //        //    case TiTypeName.Int64Id: return  VarType.Int64;
    //        //    case TiTypeName.Int32Id: return VarType.Int32;

    //        //    case TiTypeName.UInt8Id:  return VarType.UInt8;
    //        //    case TiTypeName.UInt16Id: return VarType.UInt16;
    //        //    case TiTypeName.UInt64Id: return VarType.UInt64;
    //        //    case TiTypeName.UInt32Id: return VarType.UInt32;

    //        //    case TiTypeName.SomeIntegerId: return VarType.Int32;
    //        //    case TiTypeName.ArrayId: return VarType.ArrayOf(ConvertToSimpleType(type.Arguments[0]));
    //        //    case TiTypeName.CharId: return VarType.Char;
    //        //    case TiTypeName.FunId :
    //        //        return VarType.Fun(ConvertToSimpleType(type.Arguments[0]), 
    //        //            type.Arguments.Skip(1).Select(ConvertToSimpleType).ToArray()
    //        //        );
    //        //}
    //        //throw new InvalidOperationException("Not supported type "+ type.ToSmartString(SolvingNode.MaxTypeDepth));

    //    }

    //    private  VarType ConvertToSimpleType(SolvingNode node) 
    //        => ToSimpleType(node.MakeType());
        
    //    /// <summary>
    //    /// Generic types from TI stays nfun generics
    //    /// </summary>
    //    class SaveGenericsTiToLangTypeConverter : TiToLangTypeConverter
    //    {
    //        protected override VarType ConvertGeneric(GenericType type)
    //        {
    //            return VarType.Generic(type.GenericId);

    //        }
    //    }
        
    //    /// <summary>
    //    /// Generic types from TI become any type
    //    /// </summary>
    //    class SetGenericsToAnyTiToLangTypeConverter: TiToLangTypeConverter
    //    {
    //        protected override VarType ConvertGeneric(GenericType type)
    //        {
    //            return VarType.Anything;
    //        }
    //    }
    //}
    
}