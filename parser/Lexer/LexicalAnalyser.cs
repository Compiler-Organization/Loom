using Loom.Parser.Lexer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loom.Parser.Lexer
{
    public class LexicalAnalyser
    {
        readonly List<string> Keywords = new List<string>
        {
            "and", 
            "break", 
            "do", 
            "else", 
            "elseif", 
            "end", 
            "false", 
            "for", 
            "function", 
            "if", 
            "in", 
            "local", 
            "nil", 
            "not", 
            "or", 
            "repeat", 
            "return", 
            "then", 
            "true", 
            "until", 
            "while"
        };

        string Input { get; set; }

        public LexicalAnalyser(string Input)
        {
            this.Input = Input;
        }

        LexKind Identify(string Value)
        {
            if (double.TryParse(Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
                return LexKind.Number;

            if (Value == "false"
                || Value == "true")
                return LexKind.Boolean;

            if (Keywords.Contains(Value))
                return LexKind.Keyword;

            return LexKind.Identifier;
        }

        public LexTokenList Analyze()
        {
            LexTokenList LexTokens = new LexTokenList();
            StringBuilder sb = new StringBuilder();
            int Line = 1;

            for (int i = 0; i < Input.Length; i++)
            {
                LexKind kind = LexKind.Terminal;
                string value = "";
                switch (Input[i])
                {
                    case '(': kind = LexKind.ParentheseOpen; break;
                    case ')': kind = LexKind.ParentheseClose; break;

                    case '[':
                        {
                            int j = i + 1;
                            if (j < Input.Length && (Input[j] == '[' || Input[j] == '='))
                            {
                                int eq = 0;

                                while (j < Input.Length && Input[j] == '=')
                                {
                                    eq++;
                                    j++;
                                }
                                if (j < Input.Length && Input[j] == '[')
                                {
                                    i = j + 1;
                                    while (i < Input.Length)
                                    {
                                        if (Input[i] == '\n')
                                        {
                                            Line++;
                                            sb.Append(Input[i]);
                                            i++;
                                            continue;
                                        }

                                        if (Input[i] == ']')
                                        {
                                            int k = i + 1;
                                            int eq2 = 0;
                                            while (k < Input.Length && Input[k] == '=')
                                            {
                                                eq2++;
                                                k++;
                                            }
                                            if (k < Input.Length && Input[k] == ']' && eq2 == eq)
                                            {
                                                i = k;
                                                break;
                                            }
                                        }

                                        sb.Append(Input[i]);
                                        i++;
                                    }

                                    value = sb.ToString();
                                    kind = LexKind.String;
                                    sb.Clear();
                                    break;
                                }
                            }

                            kind = LexKind.BracketOpen;
                            break;
                        }
                    case ']': kind = LexKind.BracketClose; break;

                    case '{': kind = LexKind.BraceOpen; break;
                    case '}': kind = LexKind.BraceClose; break;

                    case '<':
                        {
                            if (i + 1 < Input.Length && Input[i + 1] == '=')
                            {
                                kind = LexKind.SmallerOrEqual;
                                i++;
                            }
                            else
                            {
                                kind = LexKind.ChevronOpen;
                            }
                            break;
                        }
                    case '>':
                        {
                            if (i + 1 < Input.Length && Input[i + 1] == '=')
                            {
                                kind = LexKind.BiggerOrEqual;
                                i++;
                            }
                            else
                            {
                                kind = LexKind.ChevronClose;
                            }
                            break;
                        }

                    case '#': kind = LexKind.Hashtag; break;

                    case ';': kind = LexKind.Semicolon; break;

                    case ',': kind = LexKind.Comma; break;

                    case '.':
                        {
                            if (i + 1 < Input.Length && Input[i + 1] == '.')
                            {
                                if (i + 2 < Input.Length && Input[i + 2] == '.')
                                {
                                    kind = LexKind.Vararg;
                                    i += 2;
                                }
                                else
                                {
                                    kind = LexKind.Concat;
                                    i += 1;
                                }
                            }
                            else
                            {
                                kind = LexKind.Dot;
                            }
                            break;
                        }

                    case '"':
                        {
                            i++;
                            while (i < Input.Length && Input[i] != '"')
                            {
                                if (Input[i] == '\\' && i + 1 < Input.Length)
                                {
                                    sb.Append(Input[i]);
                                    sb.Append(Input[i + 1]);
                                    i += 2;
                                    continue;
                                }
                                sb.Append(Input[i]);
                                i++;
                            }
                            value = sb.ToString();
                            kind = LexKind.String;
                            sb.Clear();

                            break;
                        }

                    case '\'':
                        {
                            i++;
                            while (i < Input.Length && Input[i] != '\'')
                            {
                                if (Input[i] == '\\' && i + 1 < Input.Length)
                                {
                                    sb.Append(Input[i]);
                                    sb.Append(Input[i + 1]);
                                    i += 2;
                                    continue;
                                }
                                sb.Append(Input[i]);
                                i++;
                            }
                            value = sb.ToString();
                            kind = LexKind.String;
                            sb.Clear();

                            break;
                        }

                    case '=':
                        {
                            if (i + 1 < Input.Length && Input[i + 1] == '=')
                            {
                                kind = LexKind.EqualTo;
                                i++;
                            }
                            else
                            {
                                kind = LexKind.Equals;
                            }
                            break;
                        }

                    case '~':
                        {
                            if (i + 1 < Input.Length && Input[i + 1] == '=')
                            {
                                kind = LexKind.NotEqualTo;
                                i++;
                            }
                            break;
                        }


                    case '\n':
                        Line++;
                        kind = LexKind.NewLine;
                        break;

                    case '?':
                        {
                            kind = LexKind.Question;
                            break;
                        }

                    case '!':
                        {
                            kind = LexKind.Exclamation;
                            break;
                        }

                    case '+':
                        {
                            kind = LexKind.Add;
                            break;
                        }
                    case '*':
                        {
                            kind = LexKind.Mul;
                            break;
                        }
                    case '/':
                        {
                            kind = LexKind.Div;
                            break;
                        }
                    case '%':
                        {
                            kind = LexKind.Mod;
                            break;
                        }
                    case '^':
                        {

                            kind = LexKind.Exp;
                            break;
                        }

                    case ' ':
                    case '\r':
                    case '\t':
                        break;

                    case ':':
                        {
                            kind = LexKind.Colon;
                            break;
                        }

                    case '-':
                        {
                            if (i + 1 < Input.Length && Input[i + 1] == '-')
                            {
                                i += 2;
                                if (i < Input.Length && Input[i] == '[')
                                {
                                    int j = i + 1;
                                    int eq = 0;
                                    while (j < Input.Length && Input[j] == '=') { eq++; j++; }
                                    if (j < Input.Length && Input[j] == '[')
                                    {
                                        i = j + 1;
                                        while (i < Input.Length)
                                        {
                                            if (Input[i] == '\n')
                                            {
                                                Line++;
                                                sb.Append(Input[i]);
                                                i++;
                                                continue;
                                            }

                                            if (Input[i] == ']')
                                            {
                                                int k = i + 1;
                                                int eq2 = 0;
                                                while (k < Input.Length && Input[k] == '=') { eq2++; k++; }
                                                if (k < Input.Length && Input[k] == ']' && eq2 == eq)
                                                {
                                                    i = k;
                                                    break;
                                                }
                                            }

                                            sb.Append(Input[i]);
                                            i++;
                                        }

                                        value = sb.ToString();
                                        sb.Clear();
                                        kind = LexKind.Comment;
                                        break;
                                    }
                                }

                                while (i < Input.Length && Input[i] != '\n')
                                {
                                    sb.Append(Input[i]);
                                    i++;
                                }
                                value = sb.ToString();
                                sb.Clear();
                                kind = LexKind.Comment;
                                break;
                            }
                            kind = LexKind.Sub;
                            break;
                        }

                    default:
                        {
                            if (Char.IsLetter(Input[i]) || Input[i] == '_')
                            {
                                while (i < Input.Length && (Char.IsLetterOrDigit(Input[i]) || Input[i] == '_'))
                                {
                                    sb.Append(Input[i++]);
                                }
                                i--;
                                value = sb.ToString();
                                kind = Identify(value);
                                sb.Clear();
                                break;
                            }

                            if (Char.IsDigit(Input[i]))
                            {
                                int start = i;
                                while (i < Input.Length && Char.IsDigit(Input[i]))
                                    i++;
                                if (i < Input.Length && Input[i] == '.')
                                {
                                    i++;
                                    while (i < Input.Length && Char.IsDigit(Input[i]))
                                        i++;
                                }
                                if (i < Input.Length && (Input[i] == 'e' || Input[i] == 'E'))
                                {
                                    i++;
                                    if (i < Input.Length && (Input[i] == '+' || Input[i] == '-'))
                                        i++;
                                    while (i < Input.Length && Char.IsDigit(Input[i]))
                                        i++;
                                }
                                value = Input.Substring(start, i - start);
                                kind = LexKind.Number;
                                i--;
                                break;
                            }

                            break;
                        }
                }


                if (kind != LexKind.Terminal)
                {
                    LexTokens.Add(new LexToken()
                    {
                        Kind = kind,
                        Value = value,
                        Line = Line
                    });
                }
            }

            LexTokens.Add(new LexToken()
            {
                Kind = LexKind.EOF
            });

            return LexTokens;
        }
    }
}
