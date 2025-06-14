using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerFile
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_exists(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        return (null, File.Exists(state.GetFilePath(args[0])));
    }
}
