using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerMath
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) lerp(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 3)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null || args[2] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        return (null, double.Lerp((double)args[0], (double)args[1], (double)args[2]));
    }
}
