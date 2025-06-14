using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class GlobalVariable
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) variable_global_exists(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] is not string)
            return (new VMError(stack, VMErrorType.Runtime, $"object {args[0]} is not a string"), null);
        return (null, state.GlobalVariables.ContainsKey(args[0]));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) variable_global_get(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] is not string)
            return (new VMError(stack, VMErrorType.Runtime, $"object {args[0]} is not a string"), null);
        if (!state.GlobalVariables.TryGetValue(args[0], out dynamic? value))
            return (new VMError(stack, VMErrorType.Runtime, $"global variable {args[0]} doesn't exist"), null);
        return (null, value);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) variable_global_set(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] is not string)
            return (new VMError(stack, VMErrorType.Runtime, $"object {args[0]} is not a string"), null);
        state.GlobalVariables[args[0]] = args[1];
        return (null, null);
    }
}
