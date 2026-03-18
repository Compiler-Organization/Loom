using Loom.Parser.ASTGenerator.AST.Expressions;
using Loom.Parser.ASTGenerator.AST.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.Parser.ASTGenerator.AST.Statements
{
    public class CompoundAssignmentStatement : Statement
    {
        public Expression Variable { get; set; }

        public CompoundOperators Operator { get; set; }

        public Expression Value { get; set; }
    }

    public enum CompoundOperators
    {
        /// <summary>
        /// E.g '+='
        /// </summary>
        Addition,

        /// <summary>
        /// E.g '-='
        /// </summary>
        Subtraction,

        /// <summary>
        /// E.g '*='
        /// </summary>
        Multiplication,

        /// <summary>
        /// E.g '/='
        /// </summary>
        Division,

        /// <summary>
        /// E.g '//='
        /// </summary>
        FloorDivision,

        /// <summary>
        /// E.g '%='
        /// </summary>
        Modulus,

        /// <summary>
        /// E.g '^='
        /// </summary>
        Exponentiation,

        /// <summary>
        /// E.g '..='
        /// </summary>
        Concat
    }
}
