using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;

[SuppressMessage("ReSharper", "NotAccessedField.Global")]
public static class Results
{
    [UsedImplicitly] private static bool _generalTestsDone;

    public static void Finish()
    {
        _generalTestsDone = true;
        Debug.Log("tests finished");
        foreach (var result in TestResults)
        {
            Debug.Log(result);
        }
    }

    public static readonly List<Result> TestResults = new();

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct Result
    {
        public Result(string name, string message, bool success)
        {
            Name = name;
            Message = message;
            Success = success;
        }

        public readonly string Name;
        public readonly string Message;
        public readonly bool Success;

        public override string ToString()
        {
            return Success ? $"success: {Name}" : $"failure: {Name}: {Message}";
        }
    }
}