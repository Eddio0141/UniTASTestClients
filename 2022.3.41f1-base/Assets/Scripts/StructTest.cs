using UnityEngine;

public readonly struct StructTest
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private static bool _constrainedTestSuccess;
#pragma warning restore CS0414 // Field is assigned but its value is never used

    private readonly string _dummyMsg;

    static StructTest()
    {
        // test opcode `constrained` and `callvirt` being together, this should not throw
        var instance = new StructTest("foo").ToString();
        Debug.Log(instance);

        _constrainedTestSuccess = true;
    }

    public StructTest(string dummyMsg)
    {
        _dummyMsg = dummyMsg;
    }

    public override string ToString()
    {
        return _dummyMsg;
    }
}