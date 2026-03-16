using Loom.Parser.ASTGenerator;
using Loom.Parser.ASTGenerator.AST.Statements;
using Loom.Parser.Lexer;
using Loom.Parser.Lexer.Objects;
using Loom.Parser.Preprocessor;
using Loom.Parser.PrettyPrint;
using GeneralTK.Extensions.Console;
using GeneralTK.Extensions.Logging;
using System.Text.Json;

namespace Loom.App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string script = File.ReadAllText("tests\\Sample.lua");

            Console.WriteLine("Lexing...");
            LexicalAnalyser codeLexer = new LexicalAnalyser(script);
            LexTokenList lexTokens = codeLexer.Analyze();

            Console.WriteLine("Preprocessing...");
            Preprocessor preProcessor = new Preprocessor(lexTokens);
            lexTokens = preProcessor.Process();

            Console.WriteLine("Generating AST...");
            ASTGenerator astGenerator = new ASTGenerator(lexTokens);
            StatementList statements = astGenerator.ParseStatements();

            PrettyPrinter prettyPrinter = new PrettyPrinter(PrettyPrinterSettings.Beautify);

            Console.WriteLine("Amount of statements; " + statements.Count.ToString());
            statements.ForEach(t => t.Log());
            ((AssignmentStatement)statements.ElementAt(1)).Variables.ForEach(t => t.Log("---- "));
            Console.WriteLine("Original;");
            Console.WriteLine(prettyPrinter.Print(statements));

            Console.ReadLine();
        }
    }
}
