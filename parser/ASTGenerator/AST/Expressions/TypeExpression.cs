using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loom.Parser.ASTGenerator.AST.Expressions
{
    public class TypeExpression : Expression
    {
        public TypeAnnotations Type { get; set; }
    }

    public enum TypeAnnotations
    {
        Any,
        Nil,
        String,
        Number,
        Boolean,
        Thread
    }
}
