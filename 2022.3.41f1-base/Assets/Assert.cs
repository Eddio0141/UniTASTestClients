using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public static class Assert
{
    public static void Log(string name, LogType expectedType, string expectedLog, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        _logHookStore = (name, expectedType, expectedLog, message, file, line);
        Application.logMessageReceived += LogHook;
    }

    private static (string name, LogType expectedType, string expectedLog, string message, string file, int line)
        _logHookStore;

    private static void LogHook(string condition, string _, LogType type)
    {
        Application.logMessageReceived -= LogHook;

        Result result;
        if (_logHookStore.expectedType == type && _logHookStore.expectedLog == condition)
        {
            result = new(_logHookStore.name, null, true);
        }
        else
        {
            var fullMsg = new StringBuilder();
            fullMsg.AppendLine("assertion failed `expected_log` == `actual_log` && `expected_msg` == `actual_msg`{0}");
            if (_logHookStore.expectedType != type)
            {
                fullMsg.AppendLine($" expected_log: {_logHookStore.expectedType}");
                fullMsg.AppendLine($"   actual_log: {type}");
            }

            if (_logHookStore.expectedLog != condition)
            {
                fullMsg.AppendLine($" expected_msg: {ShowHiddenChars(_logHookStore.expectedLog)}");
                fullMsg.AppendLine($"   actual_msg: {ShowHiddenChars(condition)}");
            }

            var fullMsgStr = AssertMsg(_logHookStore.name, fullMsg.ToString(), _logHookStore.message,
                _logHookStore.file, _logHookStore.line);
            result = new(_logHookStore.name, fullMsgStr, false);
        }

        TestResults.Add(result);
    }

    private static string ShowHiddenChars(string str)
    {
        return str?.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    public static void Null<T>(string name, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (actual == null)
            result = new(name, null, true);
        else
        {
            var fullMsg = AssertMsg(name, $"assertion failed `actual` == null{{0}}\n actual: {actual}", message, file,
                line);
            result = new(name, fullMsg, false);
        }

        TestResults.Add(result);
    }

    public static void True(string name, bool success, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (success)
            result = new(name, null, true);
        else
        {
            var fullMsg = AssertMsg(name, "assertion failed{0}", message, file, line);
            result = new(name, fullMsg, false);
        }

        TestResults.Add(result);
    }

    public static void False(string name, bool success, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (success)
        {
            var fullMsg = AssertMsg(name, "assertion failed{0}", message, file, line);
            result = new(name, fullMsg, false);
        }
        else
            result = new(name, null, true);

        TestResults.Add(result);
    }

    public static void NotThrows(string name, Action action, string message = null,
        [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        Result result;
        try
        {
            action();
            result = new(name, null, true);
        }
        catch (Exception e)
        {
            var fullMsg = AssertMsg(name,
                $"assertion failed `expected` no throw{{0}}\n actual: {e.GetType().FullName}: {e.Message}", message,
                file, line);
            result = new(name, fullMsg, false);
        }

        TestResults.Add(result);
    }

    public static void Throws<T>(string name, T expected, Action action, string message = null,
        [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        where
        T : Exception
    {
        Result result;
        try
        {
            action();
            var fullMsg = AssertMsg(name,
                $"assertion failed `expected` throws{{0}}\n expected: {expected.GetType().FullName}: {expected.Message}",
                message, file, line);
            result = new(name, fullMsg, false);
        }
        catch (Exception e)
        {
            if (e.GetType() == expected.GetType() && e.Message == expected.Message)
                result = new(name, null, true);
            else
            {
                var fullMsg = AssertMsg(name,
                    $"assertion failed `expected` throws{{0}}\n expected: {expected.GetType().FullName}: {expected.Message}\n   actual: {e.GetType().FullName}: {e.Message}",
                    message, file, line);
                result = new(name, fullMsg, false);
            }
        }

        TestResults.Add(result);
    }

    public static void NotEqual<T>(string name, T expected, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        NotEqualBase(name, expected, actual, Equals(expected, actual), file, line, message);
    }

    public static void Equal<T>(string name, T expected, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        EqualBase(name, expected, actual, Equals(expected, actual), file, line, message);
    }

    public static void Equal(string name, float expected, float actual, float tolerance,
        string message = null,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        EqualBase(name, expected, actual, Math.Abs(expected - actual) < tolerance, file, line, message);
    }

    private static void NotEqualBase<T>(string name, T expected, T actual, bool success, string file, int line,
        string message = null)
    {
        Result result;
        if (success)
        {
            var fullMsg = AssertMsg(name,
                $"assertion failed `expected` != `actual`{{0}}\n expected: {expected}\n   actual: {actual}", message,
                file,
                line);
            result = new(name, fullMsg, false);
        }
        else
        {
            result = new(name, null, true);
        }

        TestResults.Add(result);
    }

    private static void EqualBase<T>(string name, T expected, T actual, bool success, string file, int line,
        string message = null)
    {
        Result result;
        if (success)
        {
            result = new(name, null, true);
        }
        else
        {
            var assertMsg = new StringBuilder();
            assertMsg.AppendLine("assertion failed `expected` == `actual`{0}");
            if (expected is string sExpected)
            {
                var sActual = actual as string;
                sExpected = ShowHiddenChars(sExpected);
                sActual = ShowHiddenChars(sActual);
                assertMsg.AppendLine($" expected: {sExpected}");
                assertMsg.AppendLine($"   actual: {sActual}");
            }
            else
            {
                assertMsg.AppendLine($" expected: {expected}");
                assertMsg.AppendLine($"   actual: {actual}");
            }

            var fullMsg = AssertMsg(name, assertMsg.ToString(), message, file, line);
            result = new(name, fullMsg, false);
        }

        TestResults.Add(result);
    }

    private static string AssertMsg(string name, string assertMsg, string userMsg, string file, int line)
    {
        userMsg = userMsg == null ? string.Empty : $": {userMsg}";
        return $"test {name} failed at {file}:{line}:\n" + string.Format(assertMsg, userMsg);
    }

    private static readonly List<Result> TestResults = new();
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

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private readonly struct Result
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