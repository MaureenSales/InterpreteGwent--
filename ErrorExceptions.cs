namespace Interprete
{
    public class ErrorExceptions
    {
        public static Exception Error( ErrorType type, string error, int line, int column )
        {
            throw new Exception($"{type.ToString()} ERROR: {error} in line {line} at column {column}.");
        }

        public enum ErrorType
        {
            LEXICAL,
            SINTACTIC,
            SEMANTIC
        }
    }
}