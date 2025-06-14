using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class CliParameters
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) parameter_count(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, Environment.GetCommandLineArgs().Length - 1);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) parameter_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] as int? == null)
            return (new VMError(stack, VMErrorType.Runtime, $"object {args[0]} is not a number"), null);
        var cargs = Environment.GetCommandLineArgs();
        if ((int)args[0] >= cargs.Length)
            return (new VMError(stack, VMErrorType.Runtime, $"index {args[0]} out-of-range"), null);
        return (null, cargs[(int)args[0]]);
    }
}
