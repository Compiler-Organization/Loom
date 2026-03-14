using Loom.Parser.ASTGenerator.AST.Statements;
using Loom.Parser.Lexer;
using Loom.Parser.Lexer.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loom.Parser.Preprocessor
{
    public class Preprocessor
    {
        LexTokenReader TokenReader { get; set; }

        public Preprocessor(LexTokenList lexTokens)
        {
            TokenReader = new LexTokenReader(lexTokens);
        }

        void RemoveComments()
        {
            TokenReader.LexTokens.RemoveAll(token => token.Kind == LexKind.Comment);
        }

        void RemoveNewLine()
        {
            TokenReader.LexTokens.RemoveAll(token => token.Kind == LexKind.NewLine);
        }

        public LexTokenList Process()
        {
            RemoveComments();
            RemoveNewLine();

            return TokenReader.LexTokens;
        }
    }
}
