using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Array
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) array_length_1d(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        // todo: check if it's actually an array/List
        return (null, args[0]!.GetType().IsArray ? (args[0]!.Length + 1) : args[0]!.Count);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) array_length(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        // todo: check if it's actually an array/List
        return (null, args[0]!.GetType().IsArray ? (args[0]!.Length + 1) : args[0]!.Count);
    }
}
