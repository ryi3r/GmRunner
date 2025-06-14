namespace BytecodeVM;

public class FunctionConstructor
{
    Dictionary<string, dynamic?> InnerVariables = [];
    public Dictionary<string, dynamic?> Variables
    {
        get
        {
            return InnerVariables;
        }
        set
        {
            if (!Initialized)
                InnerVariables = value;
        }
    }
    public bool Initialized = false;
}
