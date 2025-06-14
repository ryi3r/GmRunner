using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerString
{
    [GameState.DefineFunciton("string")]
    public static (VMError?, dynamic?) runner_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length <= 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        return (null, string.Format(args[0]?.ToString() ?? "<null>", args.Length > 1 ? args[1..] : []));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) string_copy(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 3)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null || args[2] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        return (null, ((string)args[0]!)[Math.Clamp((int)args[1] - 1, 0, ((string)args[0]!).Length)..Math.Clamp((int)args[1] - 1 + (int)args[2], 0, ((string)args[0]!).Length)]);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) string_pos(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        return (null, ((string)args[1]!).IndexOf((string)args[0]!) + 1);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) show_debug_message(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length <= 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        Console.WriteLine(string.Format(args[0]?.ToString() ?? "<null>", args.Length > 1 ? args[1..] : []));
        return (null, null);
    }
}
