using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Keyboard
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) keyboard_check(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        return (null, (bool)Raylib.IsKeyDown((KeyboardKey)(int)args[0]));
    }
}
