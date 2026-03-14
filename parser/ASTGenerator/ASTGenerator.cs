using Loom.Parser.ASTGenerator.AST.Statements;
using Loom.Parser.ASTGenerator.AST.Expressions;
using Loom.Parser.Lexer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Loom.Parser.Lexer;
using Loom.Parser.ASTGenerator.AST;

namespace Loom.Parser.ASTGenerator
{
    /// <summary>
    /// Parse Lua code into an abstract syntax tree
    /// </summary>
    public class ASTGenerator
    {
        LexTokenReader TokenReader { get; set; }

        public ASTGenerator(LexTokenList lexTokens)
        {
            this.TokenReader = new LexTokenReader(lexTokens);
        }

        enum Precedence
        {
            Lowest = 0,
            Or = 1,
            And = 2,
            Relational = 3,
            Concat = 4,
            AddSub = 5,
            MulDiv = 6,
            Exponent = 7,
            Unary = 8,
            Postfix = 9,
            Call = 10
        }

        public StatementList ParseStatements()
        {
            StatementList statementList = new StatementList();

            while (!TokenReader.Expect(LexKind.EOF) && !IsBlockTerminator())
            {
                Statement statement = ParseStatement();
                if (statement != null)
                {
                    statementList.Add(statement);
                }

                while (TokenReader.Expect(LexKind.Semicolon) || TokenReader.Expect(LexKind.NewLine))
                {
                    TokenReader.Consume();
                }
            }

            return statementList;
        }

        bool IsBlockTerminator()
        {
            if (!TokenReader.Expect(LexKind.Keyword))
            {
                return false;
            }

            string value = TokenReader.Peek().Value;
            return value == "end" 
                || value == "else" 
                || value == "elseif" 
                || value == "until";
        }

        Statement ParseStatement()
        {
            if (TokenReader.Expect(LexKind.Keyword))
            {
                switch (TokenReader.Peek().Value)
                {
                    case "local": return ParseLocalDeclaration();
                    case "function": return ParseFunctionDeclarationStatement(false);
                    case "if": return ParseIfStatement();
                    case "while": return ParseWhileStatement();
                    case "for": return ParseForStatement();
                    case "repeat": return ParseRepeatStatement();
                    case "do": return ParseDoStatement();
                    case "break": TokenReader.Skip(1); return new BreakStatement();
                    case "continue": TokenReader.Skip(1); return new ContinueStatement();
                    case "return": return ParseReturnStatement();
                }
            }

            ExpressionList expressions = ParseExpressionList();
            if (expressions == null || expressions.Count == 0)
            {
                return null;
            }

            if (expressions.Count == 1 && expressions[0] is CallExpression callExpr)
            {
                return new CallStatement 
                { 
                    Function = callExpr.Operand, 
                    Arguments = callExpr.Arguments 
                };
            }

            if (TokenReader.Expect(LexKind.Equals))
            {
                TokenReader.Skip(1);

                ExpressionList valueExpressions = ParseExpressionList() ?? new ExpressionList();
                if (valueExpressions.Count == 0)
                {
                    throw new Exception("Assignment without values");
                }

                return new AssignmentStatement 
                { 
                    Variables = expressions, 
                    Values = valueExpressions 
                };
            }

            if (expressions.Count == 1 && expressions[0] is AssignmentExpression assignmentExpression)
            {
                return new AssignmentStatement 
                { 
                    Variables = assignmentExpression.Variables, 
                    Values = assignmentExpression.Values 
                };
            }

            throw new Exception("Unsupported statement starting with token: " + TokenReader.Peek().Kind + " " + TokenReader.Peek().Value);
        }

        Statement ParseLocalDeclaration()
        {
            TokenReader.Skip(1);

            if (TokenReader.Expect(LexKind.Keyword) && TokenReader.Peek().Value == "function")
            {
                return ParseFunctionDeclarationStatement(true);
            }

            ExpressionList expressions = new ExpressionList();
            while (true)
            {
                if (!TokenReader.Expect(LexKind.Identifier) && !TokenReader.Expect(LexKind.Vararg))
                {
                    throw new Exception("Expected identifier in local declaration");
                }

                if (TokenReader.Expect(LexKind.Identifier))
                {
                    expressions.Add(new IdentifierExpression 
                    { 
                        Identifier = TokenReader.Consume().Value 
                    });
                }
                else
                {
                    expressions.Add(new VarargExpression());
                }

                if (TokenReader.Expect(LexKind.Comma))
                {
                    TokenReader.Skip(1);
                }
                else
                {
                    break;
                }
            }

            ExpressionList vals = new ExpressionList();
            if (TokenReader.Expect(LexKind.Equals))
            {
                TokenReader.Skip(1);
                vals = ParseExpressionList() ?? new ExpressionList();
            }

            LocalDeclarationStatement local = new LocalDeclarationStatement();
            local.Statement = new AssignmentStatement 
            { 
                Variables = expressions, 
                Values = vals 
            };

            return local;
        }

        Statement ParseFunctionDeclarationStatement(bool isLocal)
        {
            if (TokenReader.Expect(LexKind.Keyword) && TokenReader.Peek().Value == "function")
            {
                TokenReader.Skip(1);
            }

            IdentifierExpression name = null;
            if (TokenReader.Expect(LexKind.Identifier))
            {
                name = new IdentifierExpression 
                { 
                    Identifier = TokenReader.Consume().Value 
                };
            }

            if (!TokenReader.Expect(LexKind.ParentheseOpen))
            {
                throw new Exception("Expected '(' after function name");
            }

            TokenReader.Skip(1);
            ExpressionList parameters = new ExpressionList();
            while (!TokenReader.Expect(LexKind.ParentheseClose))
            {
                if (TokenReader.Expect(LexKind.Identifier))
                {
                    parameters.Add(new IdentifierExpression 
                    { 
                        Identifier = TokenReader.Consume().Value 
                    });
                }
                else if (TokenReader.Expect(LexKind.Vararg))
                {
                    parameters.Add(new VarargExpression());
                    TokenReader.Skip(1);
                }
                else
                {
                    break;
                }

                if (TokenReader.Expect(LexKind.Comma))
                {
                    TokenReader.Skip(1);
                }
            }
            if (TokenReader.Expect(LexKind.ParentheseClose))
            {
                TokenReader.Skip(1);
            }

            StatementList body = ParseStatements();
            if (TokenReader.Expect(LexKind.Keyword, "end"))
            {
                TokenReader.Skip(1);
            }

            FunctionDeclarationStatement funcStmt = new FunctionDeclarationStatement
            {
                IsLocal = isLocal,
                Name = name,
                Parameters = parameters,
                Body = body
            };
            return funcStmt;
        }

        Statement ParseIfStatement()
        {
            TokenReader.Skip(1);
            Expression cond = ParseExpression();
            if (!TokenReader.Expect(LexKind.Keyword, "then"))
            {
                throw new Exception("Expected 'then' after if condition");
            }
            TokenReader.Skip(1);
            StatementList body = ParseStatements();

            IfStatement ifStmt = new IfStatement
            {
                Condition = cond,
                Body = body
            };

            while (TokenReader.Expect(LexKind.Keyword) && TokenReader.Peek().Value == "elseif")
            {
                TokenReader.Skip(1);
                Expression elifCond = ParseExpression();

                if (!TokenReader.Expect(LexKind.Keyword, "then"))
                {
                    throw new Exception("Expected 'then' after elseif condition");
                }
                TokenReader.Skip(1);
                StatementList elifBody = ParseStatements();
                IfStatement elseIfStmt = new IfStatement
                {
                    Condition = elifCond,
                    Body = elifBody
                };
                ifStmt.ElseIfStatements.Add(elseIfStmt);
            }

            if (TokenReader.Expect(LexKind.Keyword, "else"))
            {
                TokenReader.Skip(1);
                ifStmt.ElseStatements = ParseStatements();
            }

            if (TokenReader.Expect(LexKind.Keyword, "end"))
            {
                TokenReader.Skip(1);
            }

            return ifStmt;
        }

        Statement ParseWhileStatement()
        {
            TokenReader.Skip(1);
            Expression cond = ParseExpression();
            if (!TokenReader.Expect(LexKind.Keyword, "do"))
            {
                throw new Exception("Expected 'do' after while condition");
            }
            TokenReader.Skip(1);
            StatementList body = ParseStatements();
            if (TokenReader.Expect(LexKind.Keyword, "end"))
            {
                TokenReader.Skip(1);
            }

            return new WhileStatement
            {
                Condition = cond,
                Body = body
            };
        }

        Statement ParseForStatement()
        {
            TokenReader.Skip(1);

            if (TokenReader.Expect(LexKind.Identifier) && TokenReader.Expect(LexKind.Equals, 1))
            {
                IdentifierExpression id = new IdentifierExpression
                {
                    Identifier = TokenReader.Consume().Value
                };
                TokenReader.Skip(1);

                Expression start = ParseExpression();
                if (!TokenReader.Expect(LexKind.Comma))
                {
                    throw new Exception("Expected ',' in numeric for");
                }
                TokenReader.Skip(1);

                Expression end = ParseExpression();
                Expression step = null;
                if (TokenReader.Expect(LexKind.Comma))
                { 
                    TokenReader.Skip(1); step = ParseExpression(); 
                }

                if (!TokenReader.Expect(LexKind.Keyword, "do"))
                {
                    throw new Exception("Expected 'do' in for");
                }
                TokenReader.Skip(1);

                StatementList body = ParseStatements();
                if (TokenReader.Expect(LexKind.Keyword, "end"))
                {
                    TokenReader.Skip(1);
                }

                AssignmentExpression assignmentExpression = new AssignmentExpression();
                assignmentExpression.Variables.Add(id);
                assignmentExpression.Values.Add(start);

                ForStatement forStatement = new ForStatement
                {
                    ControlVariable = assignmentExpression,
                    EndValue = end,
                    Increment = step,
                    Body = body
                };
                return forStatement;
            }

            ExpressionList vars = new ExpressionList();
            if (TokenReader.Expect(LexKind.Identifier))
            {
                vars.Add(new IdentifierExpression 
                { 
                    Identifier = TokenReader.Consume().Value
                });

                while (TokenReader.Expect(LexKind.Comma))
                {
                    TokenReader.Skip(1);
                    if (TokenReader.Expect(LexKind.Identifier))
                    {
                        vars.Add(new IdentifierExpression { Identifier = TokenReader.Consume().Value });
                    }
                }
            }

            if (!TokenReader.Expect(LexKind.Keyword, "in"))
            {
                throw new Exception("Expected 'in' in generic for");
            }

            TokenReader.Skip(1);
            Expression iterator = ParseExpression();
            if (!TokenReader.Expect(LexKind.Keyword, "do"))
            {
                throw new Exception("Expected 'do' in for");
            }
            TokenReader.Skip(1);
            StatementList body2 = ParseStatements();
            if (TokenReader.Expect(LexKind.Keyword, "end")) TokenReader.Skip(1);

            GenericForStatement genericForStatement = new GenericForStatement
            {
                Iterator = iterator,
                Body = body2
            };

            foreach (Expression v in vars)
            {
                genericForStatement.VariableArray.Array.Add((IdentifierExpression)v);
            }
            return genericForStatement;
        }

        Statement ParseRepeatStatement()
        {
            TokenReader.Skip(1);
            StatementList body = ParseStatements();
            if (!TokenReader.Expect(LexKind.Keyword, "until"))
            {
                throw new Exception("Expected 'until' in repeat");
            }

            TokenReader.Skip(1);
            Expression cond = ParseExpression();

            return new RepeatStatement 
            { 
                Body = body, 
                Condition = cond 
            };
        }

        Statement ParseDoStatement()
        {
            TokenReader.Skip(1);

            StatementList body = ParseStatements();
            if (TokenReader.Expect(LexKind.Keyword, "end"))
            {
                TokenReader.Skip(1);
            }

            return new DoStatement { Body = body };
        }

        Statement ParseReturnStatement()
        {
            TokenReader.Skip(1);

            ExpressionList exprs = ParseExpressionList();
            ReturnStatement returnStatement = new ReturnStatement();
            if (exprs != null && exprs.Count > 0)
            {
                returnStatement.ReturnValue = exprs.First();
            }

            return returnStatement;
        }

        Expression ParseExpression(int minPrec = (int)Precedence.Lowest)
        {
            Expression left = ParsePrefix();

            while (true)
            {
                LexToken token = TokenReader.Peek();
                int prec = GetBinaryPrecedence(token);
                if (prec < minPrec)
                {
                    break;
                }

                if (IsBinaryOperator(token))
                {
                    TokenReader.Skip(1);

                    Expression right = ParseExpression(IsRightAssociative(token) ? prec : prec + 1);

                    left = BuildBinaryExpression(token, left, right);
                    continue;
                }

                if (token.Kind == LexKind.Dot || token.Kind == LexKind.Colon)
                {
                    TokenReader.Skip(1);

                    if (!TokenReader.Expect(LexKind.Identifier))
                    {
                        throw new Exception("Expected identifier after '.' or ':'");
                    }

                    left = new MemberExpression
                    {
                        Left = left,
                        Name = TokenReader.Consume().Value,
                        IsMethod = token.Kind == LexKind.Colon
                    };

                    continue;
                }

                if (token.Kind == LexKind.BracketOpen)
                {
                    TokenReader.Skip(1);

                    Expression index = ParseExpression();
                    if (TokenReader.Expect(LexKind.BracketClose))
                    {
                        TokenReader.Skip(1);
                    }

                    left = new IndexExpression
                    {
                        Array = left,
                        Index = index
                    };

                    continue;
                }

                if (token.Kind == LexKind.ParentheseOpen)
                {
                    TokenReader.Skip(1);
                    ExpressionList args = ParseExpressionList() ?? new ExpressionList();
                    if (TokenReader.Expect(LexKind.ParentheseClose))
                    {
                        TokenReader.Skip(1);
                    }

                    if (left is MemberExpression memberExpr2 && memberExpr2.IsMethod)
                    {
                        MemberExpression transformed = new MemberExpression
                        {
                            Left = memberExpr2.Left,
                            Name = memberExpr2.Name,
                            IsMethod = false
                        };

                        ExpressionList newArgs =
                        [
                            memberExpr2.Left, .. args
                        ];

                        CallExpression callExpr = new CallExpression
                        {
                            Operand = transformed,
                            Arguments = newArgs
                        };

                        left = callExpr;
                    }
                    else
                    {
                        CallExpression callExpr2 = new CallExpression
                        {
                            Operand = left,
                            Arguments = args
                        };

                        left = callExpr2;
                    }

                    continue;
                }

                break;
            }

            return left;
        }

        ExpressionList ParseExpressionList()
        {
            ExpressionList list = new ExpressionList();

            if (TokenReader.Expect(LexKind.ParentheseClose) 
                || TokenReader.Expect(LexKind.BraceClose) 
                || TokenReader.Expect(LexKind.EOF) 
                || IsBlockTerminator())
            {
                return list;
            }

            while (true)
            {
                Expression expr = ParseExpression();
                if (expr != null)
                {
                    list.Add(expr);
                }
                else
                {
                    break;
                }

                if (TokenReader.Expect(LexKind.Comma))
                { 
                    TokenReader.Skip(1); 
                    continue; 
                }

                if (TokenReader.Expect(LexKind.Semicolon))
                { 
                    TokenReader.Skip(1); 
                    continue; 
                }

                break;
            }

            return list;
        }

        Expression ParsePrefix()
        {
            LexToken tk = TokenReader.Peek();

            if (tk.Kind == LexKind.Sub)
            {
                TokenReader.Skip(1);
                Expression right = ParseExpression((int)Precedence.Unary);

                return new NegativeExpression 
                { 
                    Expression = right 
                };
            }

            if (tk.Kind == LexKind.Hashtag)
            {
                TokenReader.Skip(1);
                Expression expr = ParseExpression((int)Precedence.Unary);

                return new LengthExpression 
                { 
                    Expression = expr 
                };
            }

            if (tk.Kind == LexKind.Keyword && tk.Value == "not")
            {
                TokenReader.Skip(1);
                Expression expr = ParseExpression((int)Precedence.Unary);

                return new NotExpression
                {
                    Expression = expr
                };
            }

            if (tk.Kind == LexKind.ParentheseOpen)
            {
                TokenReader.Skip(1);

                if (TokenReader.Expect(LexKind.ParentheseClose))
                { 
                    TokenReader.Skip(1); 
                    return null; 
                }

                Expression inner = ParseExpression();
                if (TokenReader.Expect(LexKind.ParentheseClose))
                {
                    TokenReader.Skip(1);
                }

                return new GroupedExpression 
                { 
                    Expression = inner 
                };
            }

            if (tk.Kind == LexKind.String 
                || tk.Kind == LexKind.Number 
                || tk.Kind == LexKind.Boolean)
            {
                TokenReader.Skip(1);
                ConstantExpression c = new ConstantExpression
                {
                    Value = tk.Value
                };

                switch (tk.Kind)
                {
                    case LexKind.String: c.Type = DataTypes.String; break;
                    case LexKind.Number: c.Type = DataTypes.Number; break;
                    case LexKind.Boolean: c.Type = DataTypes.Bool; break;
                }
                return c;
            }

            if (tk.Kind == LexKind.Vararg)
            {
                TokenReader.Skip(1);

                return new VarargExpression();
            }

            if (tk.Kind == LexKind.Keyword && tk.Value == "nil")
            {
                TokenReader.Skip(1);

                return new NilExpression();
            }

            if (tk.Kind == LexKind.Keyword && tk.Value == "function")
            {
                TokenReader.Skip(1);
                IdentifierExpression name = null;
                if (TokenReader.Expect(LexKind.Identifier))
                { 
                    name = new IdentifierExpression 
                    { 
                        Identifier = TokenReader.Consume().Value 
                    }; 
                }

                if (TokenReader.Expect(LexKind.ParentheseOpen))
                {
                    TokenReader.Skip(1);
                }

                ExpressionList parameters = new ExpressionList();
                while (!TokenReader.Expect(LexKind.ParentheseClose))
                {
                    if (TokenReader.Expect(LexKind.Identifier))
                    {
                        parameters.Add(new IdentifierExpression { Identifier = TokenReader.Consume().Value });
                    }
                    else if (TokenReader.Expect(LexKind.Vararg)) 
                    { 
                        parameters.Add(new VarargExpression()); TokenReader.Skip(1); 
                    }
                    else
                    {
                        break;
                    }

                    if (TokenReader.Expect(LexKind.Comma))
                    {
                        TokenReader.Skip(1);
                    }
                }

                if (TokenReader.Expect(LexKind.ParentheseClose))
                {
                    TokenReader.Skip(1);
                }

                StatementList body = ParseStatements();
                if (TokenReader.Expect(LexKind.Keyword, "end"))
                {
                    TokenReader.Skip(1);
                }

                return new FunctionDeclarationExpression 
                { 
                    Name = name, 
                    Parameters = parameters, 
                    Body = body, 
                    IsAnonymous = name == null 
                };
            }

            if (tk.Kind == LexKind.BraceOpen)
            {
                TokenReader.Skip(1);
                ExpressionList arr = new ExpressionList();
                if (!TokenReader.Expect(LexKind.BraceClose))
                {
                    while (true)
                    {
                        Expression elementExpr = ParseExpression();
                        if (elementExpr != null)
                        {
                            arr.Add(elementExpr);
                        }

                        if (TokenReader.Expect(LexKind.Comma)
                            || TokenReader.Expect(LexKind.Semicolon))
                        {
                            TokenReader.Skip(1);
                        }
                        else break;
                    }
                }

                if (TokenReader.Expect(LexKind.BraceClose))
                {
                    TokenReader.Skip(1);
                }

                return new ArrayExpression { Array = arr };
            }

            if (tk.Kind == LexKind.Identifier)
            {
                TokenReader.Skip(1);
                return new IdentifierExpression { Identifier = tk.Value };
            }

            return null;
        }

        static bool IsBinaryOperator(LexToken tok)
        {
            if (tok.Kind == LexKind.Keyword)
            {
                return tok.Value == "and" || tok.Value == "or";
            }

            switch (tok.Kind)
            {
                case LexKind.Add:
                case LexKind.Sub:
                case LexKind.Mul:
                case LexKind.Div:
                case LexKind.Mod:
                case LexKind.Exp:
                case LexKind.Concat:
                case LexKind.EqualTo:
                case LexKind.NotEqualTo:
                case LexKind.BiggerOrEqual:
                case LexKind.SmallerOrEqual:
                case LexKind.ChevronOpen:
                case LexKind.ChevronClose:
                    return true;
                default: return false;
            }
        }

        static bool IsBinaryOperatorToken(LexKind kind)
        {
            switch (kind)
            {
                case LexKind.Add: case LexKind.Sub: case LexKind.Mul: case LexKind.Div: case LexKind.Mod: case LexKind.Exp: case LexKind.Concat:
                case LexKind.EqualTo: case LexKind.NotEqualTo: case LexKind.BiggerOrEqual: case LexKind.SmallerOrEqual: case LexKind.ChevronOpen: case LexKind.ChevronClose:
                    return true;
                default: return false;
            }
        }

        int GetBinaryPrecedence(LexToken tok)
        {
            if (tok.Kind == LexKind.Keyword)
            {
                if (tok.Value == "or")
                {
                    return (int)Precedence.Or;
                }

                if (tok.Value == "and")
                {
                    return (int)Precedence.And;
                }
            }

            switch (tok.Kind)
            {
                case LexKind.EqualTo:
                case LexKind.NotEqualTo:
                case LexKind.BiggerOrEqual:
                case LexKind.SmallerOrEqual:
                case LexKind.ChevronOpen:
                case LexKind.ChevronClose:
                    return (int)Precedence.Relational;
                case LexKind.Concat: 
                    return (int)Precedence.Concat;
                case LexKind.Add: 
                case LexKind.Sub: 
                    return (int)Precedence.AddSub;
                case LexKind.Mul: 
                case LexKind.Div: 
                case LexKind.Mod: 
                    return (int)Precedence.MulDiv;
                case LexKind.Exp: 
                    return (int)Precedence.Exponent;
                default: return (int)Precedence.Lowest;
            }
        }

        bool IsRightAssociative(LexToken tok)
        {
            return tok.Kind == LexKind.Exp || tok.Kind == LexKind.Concat;
        }

        Expression BuildBinaryExpression(LexToken opTok, Expression left, Expression right)
        {
            if (opTok.Kind == LexKind.Keyword)
            {
                if (opTok.Value == "and" 
                    || opTok.Value == "or")
                {
                    return new LogicalExpression 
                    { 
                        Left = left, 
                        Right = right, 
                        Operator = opTok.Value == "and" ? LogicalOperators.And : LogicalOperators.Or 
                    };
                }
            }

            switch (opTok.Kind)
            {
                case LexKind.Add: return new ArithmeticExpression { Left = left, Right = right, Operator = ArithmeticOperators.Addition };
                case LexKind.Sub: return new ArithmeticExpression { Left = left, Right = right, Operator = ArithmeticOperators.Subtraction };
                case LexKind.Mul: return new ArithmeticExpression { Left = left, Right = right, Operator = ArithmeticOperators.Multiplication };
                case LexKind.Div: return new ArithmeticExpression { Left = left, Right = right, Operator = ArithmeticOperators.Division };
                case LexKind.Mod: return new ArithmeticExpression { Left = left, Right = right, Operator = ArithmeticOperators.Modulus };
                case LexKind.Exp: return new ArithmeticExpression { Left = left, Right = right, Operator = ArithmeticOperators.Exponentiation };
                case LexKind.Concat: return new ConcatExpression { Left = left, Right = right };
                case LexKind.EqualTo: return new RelationalExpression { Left = left, Right = right, Operator = RelationalOperators.EqualTo };
                case LexKind.NotEqualTo: return new RelationalExpression { Left = left, Right = right, Operator = RelationalOperators.NotEqualTo };
                case LexKind.BiggerOrEqual: return new RelationalExpression { Left = left, Right = right, Operator = RelationalOperators.BiggerOrEqual };
                case LexKind.SmallerOrEqual: return new RelationalExpression { Left = left, Right = right, Operator = RelationalOperators.SmallerOrEqual };
                case LexKind.ChevronOpen: return new RelationalExpression { Left = left, Right = right, Operator = RelationalOperators.SmallerThan };
                case LexKind.ChevronClose: return new RelationalExpression { Left = left, Right = right, Operator = RelationalOperators.BiggerThan };
                default: throw new Exception("Unhandled binary operator: " + opTok.Kind);
            }
        }
    }
}
