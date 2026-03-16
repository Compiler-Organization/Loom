using Loom.Parser.ASTGenerator.AST.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Loom.Parser.ASTGenerator.AST.Statements
{
    public class LocalDeclarationStatement : Statement
    {
        public Statement Statement { get; set; }
    }
}
