using System;

namespace Funny.Runtime
{
    public struct Var {

        public static Var New(string name, object value)
        {
            if (value is int i)
                return New(name, i);
            if (value is double d)
                return New(name, d);
            if (value is bool b)
                return New(name, b);
            if (value is string s)
                return New(name, s);
            throw new ArgumentException($"Type {value.GetType()} is not supported");
        }
        public static Var New(string name, bool value) 
            => new Var(name, value, VarType.BoolType);
        public static Var New(string name, int value) 
            => new Var(name, value, VarType.IntType);
        public static Var New(string name, double value) 
            => new Var(name, value, VarType.RealType);
        public static Var New(string name, string value) 
            => new Var(name, value, VarType.TextType);

        public readonly string Name;
        public readonly object Value;
        public readonly VarType Type;
        public Var(string name, object value, VarType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Name}: {Value} of type {Type}";
        }
    }
}