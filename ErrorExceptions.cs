using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErrorExceptions : MonoBehaviour
{
    public static Exception Error(ErrorType type, string error, int line, int column)
    {
        throw new Exception($"{type.ToString()} ERROR: {error} in line {line} at column {column}.");
    }

    public enum ErrorType
    {
        LEXICAL,
        SINTACTIC,
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
