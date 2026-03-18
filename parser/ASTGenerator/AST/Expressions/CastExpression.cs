using Loom.Parser.ASTGenerator.AST.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.parser.ASTGenerator.AST.Expressions
{
    public class CastExpression : Expression
    {
        public Expression Expression { get; set; }

        public Expression Type { get; set; }
    }
}
