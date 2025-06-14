namespace BytecodeVM;

public enum VMErrorType
{
    RunnerInternal,
    Runner,
    Runtime,
}

public class VMError(FunctionStack stack, VMErrorType type, string message)
{
    public FunctionStack Stack = stack.Freeze(true);
    public VMErrorType Type = type;
    public string Message = message;
    public Exception? InternalException;

    public override string ToString()
    {
        return $"{Type} Error: {Message}\nStack: {Stack}";
    }
}