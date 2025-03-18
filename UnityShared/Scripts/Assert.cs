using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[SuppressMessage("ReSharper", "UseStringInterpolation")]
public static class Assert
{
    public static void Log(string name, LogType expectedType, string expectedLog, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        _logHookStore = new LogHookStore(name, expectedType, expectedLog, message, file, line);
        Application.logMessageReceived += LogHook;
    }

    private static LogHookStore _logHookStore;

    private static void LogHook(string condition, string _, LogType type)
    {
        Application.logMessageReceived -= LogHook;

        var name = _logHookStore.Name;
        var file = _logHookStore.File;
        var line = _logHookStore.Line;
        Result result;
        if (_logHookStore.ExpectedType == type && _logHookStore.ExpectedLog == condition)
        {
            result = new Result(name, null, true);
        }
        else
        {
            var fullMsg = new StringBuilder();
            fullMsg.AppendLine("assertion failed `expected_log` == `actual_log` && `expected_msg` == `actual_msg`{0}");
            if (_logHookStore.ExpectedType != type)
            {
                fullMsg.AppendLine(string.Format(" expected_log: {0}", _logHookStore.ExpectedType));
                fullMsg.AppendLine(string.Format("   actual_log: {0}", type));
            }

            if (_logHookStore.ExpectedLog != condition)
            {
                fullMsg.AppendLine(string.Format(" expected_msg: {0}", ShowHiddenChars(_logHookStore.ExpectedLog)));
                fullMsg.AppendLine(string.Format("   actual_msg: {0}", ShowHiddenChars(condition)));
            }

            var fullMsgStr = AssertMsg(name, fullMsg.ToString(), _logHookStore.Message, file, line);
            result = new Result(name, fullMsgStr, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    private static string ShowHiddenChars(string str)
    {
        if (str == null) return null;
        return str.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    public static void Null<T>(string name, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (actual == null)
            result = new Result(name, null, true);
        else
        {
            var fullMsg = AssertMsg(name, string.Format("assertion failed `actual` == null{{0}}\n actual: {0}", actual),
                message, file,
                line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void NotNull<T>(string name, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (actual == null)
        {
            var fullMsg = AssertMsg(name, "assertion failed `actual` != null{0}", message, file, line);
            result = new Result(name, fullMsg, false);
        }
        else
            result = new Result(name, null, true);

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void True(string name, bool success, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (success)
            result = new Result(name, null, true);
        else
        {
            var fullMsg = AssertMsg(name, "assertion failed{0}", message, file, line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
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
            result = new Result(name, fullMsg, false);
        }
        else
            result = new Result(name, null, true);

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void NotThrows(string name, Action action, string message = null,
        [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        Result result;
        try
        {
            action();
            result = new Result(name, null, true);
        }
        catch (Exception e)
        {
            var fullMsg = AssertMsg(name,
                string.Format("assertion failed `expected` no throw{{0}}\n actual: {0}: {1}", e.GetType().FullName,
                    e.Message), message,
                file, line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
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
                string.Format("assertion failed `expected` throws{{0}}\n expected: {0}: {1}",
                    expected.GetType().FullName, expected.Message),
                message, file, line);
            result = new Result(name, fullMsg, false);
        }
        catch (Exception e)
        {
            if (e.GetType() == expected.GetType() && e.Message == expected.Message)
                result = new Result(name, null, true);
            else
            {
                var fullMsg = AssertMsg(name,
                    string.Format("assertion failed `expected` throws{{0}}\n expected: {0}: {1}\n   actual: {2}: {3}",
                        expected.GetType().FullName, expected.Message, e.GetType().FullName, e.Message),
                    message, file, line);
                result = new Result(name, fullMsg, false);
            }
        }

        LogAssert(name, file, line, result);
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

    public static void Equal(string name, double expected, double actual, double tolerance, string message = null,
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
                string.Format("assertion failed `expected` != `actual`{{0}}\n expected: {0}\n   actual: {1}", expected,
                    actual), message,
                file,
                line);
            result = new Result(name, fullMsg, false);
        }
        else
        {
            result = new Result(name, null, true);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    private static void EqualBase<T>(string name, T expected, T actual, bool success, string file, int line,
        string message = null)
    {
        Result result;
        if (success)
        {
            result = new Result(name, null, true);
        }
        else
        {
            var assertMsg = new StringBuilder();
            assertMsg.AppendLine("assertion failed `expected` == `actual`{0}");
            if (typeof(T) == typeof(string) && expected != null && actual != null)
            {
                var sExpected = (string)(object)expected;
                var sActual = (string)(object)actual;
                sExpected = ShowHiddenChars(sExpected);
                sActual = ShowHiddenChars(sActual);
                assertMsg.AppendLine(string.Format(" expected: {0}", sExpected));
                assertMsg.AppendLine(string.Format("   actual: {0}", sActual));
            }
            else
            {
                assertMsg.AppendLine(string.Format(" expected: {0}", expected));
                assertMsg.AppendLine(string.Format("   actual: {0}", actual));
            }

            var fullMsg = AssertMsg(name, assertMsg.ToString(), message, file, line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    private static string AssertMsg(string name, string assertMsg, string userMsg, string file, int line)
    {
        userMsg = userMsg == null ? string.Empty : string.Format(": {0}", userMsg);
        return string.Format("test {0} failed at {1}:{2}:\n", name, file, line) + string.Format(assertMsg, userMsg);
    }

    private static void LogAssert(string name, string file, int line, Result result)
    {
        Debug.Log(string.Format("Assertion `{0}` at {1}:{2}, {3}", name, file, line,
            result.Success ? "success" : "failure"));
    }

    private static readonly List<Result> TestResults = new List<Result>();
#pragma warning disable CS1691 CS1692 CS0414 // Field is assigned but its value is never used
    private static bool _testsDone;
#pragma warning restore CS1691 CS1692 CS0414 // Field is assigned but its value is never used

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static void Reset()
    {
        _testsDone = false;
        TestResults.Clear();
    }

    public static void Finish()
    {
        _testsDone = true;
        Debug.Log("tests finished");
        foreach (var result in TestResults)
        {
            Debug.Log(result);
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    private struct Result
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
            return Success ? string.Format("success: {0}", Name) : string.Format("failure: {0}: {1}", Name, Message);
        }
    }

    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    private struct LogHookStore
    {
        public readonly string Name;
        public readonly LogType ExpectedType;
        public readonly string ExpectedLog;
        public readonly string Message;
        public readonly string File;
        public readonly int Line;

        public LogHookStore(string name, LogType expectedType, string expectedLog, string message, string file,
            int line)
        {
            Name = name;
            ExpectedType = expectedType;
            ExpectedLog = expectedLog;
            Message = message;
            File = file;
            Line = line;
        }
    }
}