using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Arrays
{
    public class EnumerableFunArray : IFunArray
    {
        private readonly IEnumerable<object> _origin;

        public EnumerableFunArray(IEnumerable<object> origin, VarType elementType)
        {
            _origin = origin;
            ElementType = elementType;
        }
        public IEnumerator<object> GetEnumerator() => _origin.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public VarType ElementType { get; }
        public int Count => _origin.Count();
        public IFunArray Slice(int? startIndex, int? endIndex, int? step)
        {
            var array = _origin.ToArray();
            return ArrayTools.SliceToImmutable(array,ElementType, startIndex, endIndex, step);
        }
        
        
        public object GetElementOrNull(int index) => _origin.ElementAtOrDefault(index);

        public bool IsEquivalent(IFunArray array) => TypeHelper.AreEquivalent(this, array);

        public IEnumerable<T> As<T>() => _origin.Cast<T>();

        public Array ClrArray => _origin.ToArray();
        public string ToText()
        {
            if (ElementType == VarType.Char)
            {
                var array = _origin.OfType<char>().ToArray();
                 return new string(array);
            }
            return ArrayTools.JoinElementsToFunString(_origin);
        }
    }
}