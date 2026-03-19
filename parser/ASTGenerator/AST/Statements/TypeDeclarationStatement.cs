using Loom.Parser.ASTGenerator.AST.Expressions;
using Loom.Parser.ASTGenerator.AST.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.parser.ASTGenerator.AST.Statements
{
    public class TypeDeclarationStatement : Statement
    {
        public IdentifierExpression Annotation { get; set; }

        public Expression Type { get; set; }
    }
}
