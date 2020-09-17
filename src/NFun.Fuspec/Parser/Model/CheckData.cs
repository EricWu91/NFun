﻿using NFun.Fuspec.Parser.Interfaces;
using NFun.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nfun.Fuspec.Parser.Model
{
    public class CheckData: ISetCheckData
    {
        readonly List<VarVal> _check;
        public VarVal[] ValuesKit => _check.ToArray();

        public CheckData() => _check = new List<VarVal>();

        public void AddValue(IEnumerable<VarVal> value) => _check.AddRange(value);
    }
}