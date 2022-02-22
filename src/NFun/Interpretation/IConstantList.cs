using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.Interpretation {

internal interface IConstantList {
    bool TryGetConstant(string id, out ConstantValueAndType constant);
}

internal class EmptyConstantList : IConstantList {
    public static readonly EmptyConstantList Instance = new();
    private EmptyConstantList() { }

    public bool TryGetConstant(string id, out ConstantValueAndType constant) {
        constant = default;
        return false;
    }
}

internal class ConstantList : IConstantList {
    private readonly TypeBehaviour _typeBehaviour;
    public ConstantList(TypeBehaviour typeBehaviour) {
        _typeBehaviour = typeBehaviour;
        _dictionary = new Dictionary<string, ConstantValueAndType>();
    }

    private ConstantList(TypeBehaviour typeBehaviour, Dictionary<string, ConstantValueAndType> dictionary) {
        _typeBehaviour = typeBehaviour;
        _dictionary = dictionary;
    }

    internal ConstantList(TypeBehaviour typeBehaviour, (string id, object value)[] items) {
        _typeBehaviour = typeBehaviour;
        _dictionary = new Dictionary<string, ConstantValueAndType>(items.Length);
        foreach (var (id, value) in items)
        {
            var converter = _typeBehaviour.GetInputConverterFor(value.GetType());
            _dictionary.Add(id, new ConstantValueAndType(converter.ToFunObject(value), converter.FunnyType));
        }
    }

    readonly Dictionary<string, ConstantValueAndType> _dictionary;

    public void AddConstant(string id, object val) {
        //constants are readonly so we need to use input converter
        var converter = _typeBehaviour.GetInputConverterFor(val.GetType());
        _dictionary.Add(id, new ConstantValueAndType(converter.ToFunObject(val), converter.FunnyType));
    }

    public bool TryGetConstant(string id, out ConstantValueAndType constant) =>
        _dictionary.TryGetValue(id, out constant);

}

}