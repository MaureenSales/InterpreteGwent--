using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErrorExceptions : MonoBehaviour
{
    public static System.Exception Error(ErrorType type, string error, int line, int column)
    {
        throw new System.Exception($"{type.ToString()} ERROR: {error} in line {line} at column {column}.");
    }

    public static System.Exception Error(ErrorType type, string error)
    {
        throw new System.Exception($"{type.ToString()} ERROR: {error}.");
    }

    public enum ErrorType
    {
        LEXICAL,
        SYNTACTIC,
        SEMANTIC
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
