using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Window
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) window_set_caption(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        Raylib.SetWindowTitle(args[0]);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) window_set_size(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        Raylib.SetWindowSize((int)args[0], (int)args[1]);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) window_get_fullscreen(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, (bool)Raylib.IsWindowFullscreen());
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) window_center(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        Raylib.SetWindowPosition(
            (Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor()) - Raylib.GetScreenWidth()) / 2,
            (Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor()) - Raylib.GetScreenHeight()) / 2
        );
        return (null, null);
    }
}
