using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Template
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) __sample_function__(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, null);
    }
}
