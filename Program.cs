using System.Drawing;
using System.Runtime.InteropServices;
using Interprete;

internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine();
        //Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        System.Console.WriteLine("Please enter the root of the .txt: ");
        Console.ForegroundColor = ConsoleColor.White;
        string Root = "/home/maureensb/Documentos/InterpreteGwent++/Source.txt";
        System.Console.WriteLine(Root);
        string input = "";

        input = File.ReadAllText(Root);

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write(">>> ");

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        System.Console.WriteLine("Loading document " + Root + " .....");
        Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine(input);
        try
        {
            Lexer lexer = new Lexer(input);

            foreach (Token token in lexer.Tokens)
            {
                token.ToString();
                Console.WriteLine(" ");
            }
            System.Console.WriteLine("Successfully completed " + "🎊");

            Parser parser = new Parser(lexer);
            PrintASTnode printer = new PrintASTnode();

            foreach (var stm in parser.Statements)
            {
                System.Console.WriteLine(printer.Print(stm));
                System.Console.WriteLine();
            }
            Evaluador evaluador = new Evaluador();
            foreach (var stm in parser.Statements)
            {
                System.Console.WriteLine(evaluador.evaluate(stm));
                System.Console.WriteLine();
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(e.Message);
            //System.Console.WriteLine(e.StackTrace);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }


}