using Loom.Parser.ASTGenerator.AST;
using Loom.Parser.ASTGenerator.AST.Expressions;
using Loom.Parser.ASTGenerator.AST.Statements;
using System.Text;

namespace Loom.Parser.PrettyPrint
{
    /// <summary>
    /// Convert code from AST to its code representation
    /// </summary>
    public class PrettyPrinter
    {
        PrettyPrinterSettings PrinterSettings { get; set; }
        readonly StringBuilder builder = new StringBuilder();

        public PrettyPrinter(PrettyPrinterSettings settings)
        {
            PrinterSettings = settings ?? PrettyPrinterSettings.Beautify;
        }

        string NL(string newLine) => newLine ?? PrinterSettings.NewLine;
        string Indent(string baseIndent) => baseIndent + PrinterSettings.Indentation;

        public string Print(StatementList statements)
        {
            builder.Clear();
            WriteStatements(statements, "", PrinterSettings.NewLine);
            return builder.ToString();
        }

        void WriteStatements(StatementList statements, string indent, string newLine)
        {
            var nl = NL(newLine);
            foreach (var stmt in statements)
            {
                if (WriteStatement(stmt, indent))
                {
                    builder.Append(nl);
                }
            }
        }

        bool WriteStatement(Statement statement, string indent)
        {
            switch (statement)
            {
                case CallStatement cs:
                    builder.Append(indent);
                    WriteExpression(cs.Function, indent, PrinterSettings.NewLine);
                    builder.Append("(");
                    WriteExpressions(cs.Arguments, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(")");
                    return true;

                case FunctionDeclarationStatement fds:
                    builder.Append(indent);
                    if (fds.IsLocal) builder.Append("local ");
                    builder.Append("function ");
                    builder.Append(fds.Name.Identifier);
                    builder.Append("(");
                    WriteExpressions(fds.Parameters, indent, PrinterSettings.NewLine);
                    builder.Append(")");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(fds.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("end");
                    return true;

                case AssignmentStatement assign:
                    builder.Append(indent);
                    WriteExpressions(assign.Variables, indent, PrinterSettings.NewLine);
                    if(assign.Values.Count > 0)
                    {
                        builder.Append(" = ");
                        WriteExpressions(assign.Values, Indent(indent), PrinterSettings.NewLine);
                    }
                    return true;

                case IfStatement ifs:
                    builder.Append(indent);
                    builder.Append("if ");
                    WriteExpression(ifs.Condition, indent, PrinterSettings.NewLine);
                    builder.Append(" then");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(ifs.Body, Indent(indent), PrinterSettings.NewLine);

                    foreach (var elif in ifs.ElseIfStatements)
                    {
                        builder.Append(indent);
                        builder.Append("elseif ");
                        WriteExpression(elif.Condition, indent, PrinterSettings.NewLine);
                        builder.Append(" then");
                        builder.Append(PrinterSettings.NewLine);
                        WriteStatements(elif.Body, Indent(indent), PrinterSettings.NewLine);
                    }

                    if (ifs.ElseStatements.Count > 0)
                    {
                        builder.Append(indent);
                        builder.Append("else");
                        builder.Append(PrinterSettings.NewLine);
                        WriteStatements(ifs.ElseStatements, Indent(indent), PrinterSettings.NewLine);
                    }

                    builder.Append(indent);
                    builder.Append("end");
                    return true;

                case GenericForStatement gfs:
                    builder.Append(indent);
                    builder.Append("for ");
                    for (int i = 0; i < gfs.VariableArray.Array.Count; i++)
                    {
                        WriteExpression(gfs.VariableArray.Array[i]);
                        if (i < gfs.VariableArray.Array.Count - 1) builder.Append(", ");
                    }
                    builder.Append(" in ");
                    WriteExpression(gfs.Iterator);
                    builder.Append(" do");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(gfs.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("end");
                    return true;

                case ForStatement fs:
                    builder.Append(indent);
                    builder.Append("for ");
                    WriteExpression(fs.ControlVariable);
                    builder.Append(", ");
                    WriteExpression(fs.EndValue);
                    if (fs.Increment != null)
                    {
                        builder.Append(", ");
                        WriteExpression(fs.Increment);
                    }
                    builder.Append(" do");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(fs.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("end");
                    return true;

                case RepeatStatement rs:
                    builder.Append(indent);
                    builder.Append("repeat");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(rs.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("until ");
                    WriteExpression(rs.Condition, indent, PrinterSettings.NewLine);
                    return true;

                case DoStatement ds:
                    builder.Append(indent);
                    builder.Append("do");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(ds.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("end");
                    return true;

                case BreakStatement:
                    builder.Append(indent);
                    builder.Append("break");
                    return true;

                case ContinueStatement:
                    builder.Append(indent);
                    builder.Append("continue");
                    return true;

                case WhileStatement ws:
                    builder.Append(indent);
                    builder.Append("while ");
                    WriteExpression(ws.Condition, indent, PrinterSettings.NewLine);
                    builder.Append(" do");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(ws.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("end");
                    return true;

                case ReturnStatement ret:
                    builder.Append(indent);
                    builder.Append("return ");
                    if (ret.ReturnValue != null) WriteExpression(ret.ReturnValue, indent, PrinterSettings.NewLine);
                    return true;

                case LocalDeclarationStatement lds:
                    builder.Append(indent);
                    builder.Append("local ");
                    WriteStatement(lds.Statement, "");
                    return true;

                default:
                    return false;
            }
        }

        void WriteExpressions(ExpressionList expressions, string indent = "", string newLine = null)
        {
            var nl = NL(newLine);
            for (int i = 0; i < expressions.Count; i++)
            {
                WriteExpression(expressions[i], indent, nl);
                if (i < expressions.Count - 1) builder.Append(", ");
            }
        }

        void WriteExpression(Expression expression, string indent = "", string newLine = null)
        {
            if (expression == null) return;
            var nl = NL(newLine);

            switch (expression)
            {
                case ConstantExpression c:
                    if (c.Type == DataTypes.String)
                        builder.Append("'" + c.Value + "'");
                    else
                        builder.Append(c.Value);
                    break;

                case IdentifierExpression id:
                    builder.Append(id.Identifier);
                    break;

                case VarargExpression:
                    builder.Append("...");
                    break;

                case RelationalExpression re:
                    WriteExpression(re.Left, indent, nl);
                    builder.Append(re.Operator switch
                    {
                        RelationalOperators.EqualTo => " == ",
                        RelationalOperators.NotEqualTo => " ~= ",
                        RelationalOperators.BiggerThan => " > ",
                        RelationalOperators.SmallerThan => " < ",
                        RelationalOperators.BiggerOrEqual => " >= ",
                        RelationalOperators.SmallerOrEqual => " <= ",
                        _ => " "
                    });
                    WriteExpression(re.Right, indent, nl);
                    break;

                case ArithmeticExpression ae:
                    WriteExpression(ae.Left, indent, nl);
                    builder.Append(ae.Operator switch
                    {
                        ArithmeticOperators.Addition => " + ",
                        ArithmeticOperators.Subtraction => " - ",
                        ArithmeticOperators.Multiplication => " * ",
                        ArithmeticOperators.Division => " / ",
                        ArithmeticOperators.Modulus => " % ",
                        ArithmeticOperators.Exponentiation => " ^ ",
                        _ => " "
                    });
                    WriteExpression(ae.Right, indent, nl);
                    break;

                case ConcatExpression ce:
                    WriteExpression(ce.Left, indent, nl);
                    builder.Append(" .. ");
                    WriteExpression(ce.Right, indent, nl);
                    break;

                case ArrayExpression arr:
                    builder.Append("{");
                    builder.Append(nl);
                    for (int i = 0; i < arr.Array.Count; i++)
                    {
                        builder.Append(Indent(indent));
                        WriteExpression(arr.Array[i], Indent(indent), nl);
                        if (i < arr.Array.Count - 1) builder.Append(",");
                        builder.Append(nl);
                    }
                    builder.Append(indent);
                    builder.Append("}");
                    break;

                case GroupedExpression ge:
                    builder.Append("(");
                    WriteExpression(ge.Expression, indent, nl);
                    builder.Append(")");
                    break;

                case CallExpression call:
                    if (call.Operand is MemberExpression mem && call.Arguments.Count > 0 && ReferenceEquals(call.Arguments[0], mem.Left))
                    {
                        WriteExpression(mem.Left, indent, nl);
                        builder.Append(":" + mem.Name + "(");
                        for (int i = 1; i < call.Arguments.Count; i++)
                        {
                            WriteExpression(call.Arguments[i], indent, nl);
                            if (i < call.Arguments.Count - 1) builder.Append(", ");
                        }
                        builder.Append(")");
                        break;
                    }

                    WriteExpression(call.Operand, indent, nl);
                    builder.Append("(");
                    WriteExpressions(call.Arguments, indent, nl);
                    builder.Append(")");
                    break;

                case MemberExpression member:
                    WriteExpression(member.Left, indent, nl);
                    builder.Append(member.IsMethod ? ":" : ".");
                    builder.Append(member.Name);
                    break;

                case FunctionDeclarationExpression fn:
                    builder.Append("function");
                    if (!fn.IsAnonymous) builder.Append(" " + fn.Name.Identifier);
                    builder.Append("(");
                    WriteExpressions(fn.Parameters, indent, nl);
                    builder.Append(")");
                    builder.Append(PrinterSettings.NewLine);
                    WriteStatements(fn.Body, Indent(indent), PrinterSettings.NewLine);
                    builder.Append(indent);
                    builder.Append("end");
                    break;

                case AssignmentExpression aexp:
                    WriteExpressions(aexp.Variables, indent, nl);
                    builder.Append(" = ");
                    WriteExpressions(aexp.Values, Indent(indent), nl);
                    break;

                case IfExpression ie:
                    builder.Append("if ");
                    WriteExpression(ie.Condition, indent, nl);
                    builder.Append(" then ");
                    WriteExpression(ie.Body, Indent(indent), nl);
                    foreach (var elif in ie.ElseIfExpressions)
                    {
                        builder.Append(" elseif ");
                        WriteExpression(elif.Condition, indent, nl);
                        builder.Append(" then ");
                        WriteExpression(elif.Body, Indent(indent), nl);
                    }
                    if (ie.ElseExpression != null)
                    {
                        builder.Append(" else ");
                        WriteExpression(ie.ElseExpression, indent, nl);
                    }
                    break;

                case LengthExpression le:
                    builder.Append("#");
                    WriteExpression(le.Expression, indent, nl);
                    break;

                case NegativeExpression ne:
                    builder.Append("-");
                    WriteExpression(ne.Expression, indent, nl);
                    break;

                case NotExpression no:
                    builder.Append("not ");
                    WriteExpression(no.Expression, indent, nl);
                    break;

                case LogicalExpression lo:
                    WriteExpression(lo.Left, indent, nl);
                    builder.Append(lo.Operator switch
                    {
                         LogicalOperators.And => " and ",
                         LogicalOperators.Or => " or ",
                         LogicalOperators.Not => " not ",
                        _ => " "
                    });
                    WriteExpression(lo.Right, indent, nl);
                    break;

                case NilExpression _:
                    builder.Append("nil");
                    break;

                case IndexExpression idx:
                    WriteExpression(idx.Array, indent, nl);
                    builder.Append("[");
                    WriteExpression(idx.Index, indent, nl);
                    builder.Append("]");
                    break;

                default:
                    break;
            }
        }
    }
}
