using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loom.Parser.ASTGenerator.AST.Expressions
{
    /// <summary>
    /// Represents member access or method reference (a.b or a:b)
    /// </summary>
    public class MemberExpression : Expression
    {
        public Expression Left { get; set; }
        public string Name { get; set; }
        public bool IsMethod { get; set; }

        public MemberExpression()
        {
            Left = new Expression();
            Name = string.Empty;
            IsMethod = false;
        }
    }
}
