using UnityEngine;

public struct StructTest
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private static bool _constrainedTestSuccess;
#pragma warning restore CS0414 // Field is assigned but its value is never used
    
    static StructTest()
    {
        // test opcode `constrained` and `callvirt` being together, this should not throw
        var instance = new StructTest();
        Debug.Log(instance);

        _constrainedTestSuccess = true;
    }

    public override string ToString()
    {
        return "foo!";
    }
}
