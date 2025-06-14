using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Display
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) display_get_width(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor()));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) display_get_height(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor()));
    }
}
