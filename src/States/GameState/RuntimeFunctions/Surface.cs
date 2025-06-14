using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Surface
{
    public const int ApplicationSurface = -500;

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) surface_resize(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        // is there a better way to do this LMAO
        if (args[0] != ApplicationSurface)
            return (new(stack, VMErrorType.Runner, "not application surface!?"), null);
        var img = Raylib.LoadImageFromTexture(state.RenderTexture!.Value.Texture);
        Raylib.ImageResize(ref img, (int)args[1], (int)args[2]);
        Raylib.UnloadTexture(state.RenderTexture.Value.Texture);
        state.RenderTexture = state.RenderTexture.Value with { Texture = Raylib.LoadTextureFromImage(img) };
        Raylib.UnloadImage(img);
        return (null, null);
    }
}
