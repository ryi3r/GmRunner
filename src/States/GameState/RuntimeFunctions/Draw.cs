using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerDraw
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_sprite_ext(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 9)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        foreach (var arg in args)
        {
            if (arg == null)
                return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        }
        if (args[0] < 0 || args[0] >= state.Data.Sprites.Count)
            return (new VMError(stack, VMErrorType.Runtime, $"sprite index {args[0]} out-of-bounds"), null);
        var spr = state.Data.Sprites[(int)args[0]];
        var imgIndex = (int)float.Floor(ILExecution.ConvertNumberToFloat(args[1]));
        var tex = spr.Textures[imgIndex % spr.Textures.Count];
        var texIndex = state.Data.EmbeddedTextures.IndexOf(tex.Texture.TexturePage);
        if (texIndex != -1)
        {
            state.EnsureTextureIsLoaded(spr);
            Raylib.DrawTexturePro(
                state.TextureTextures[texIndex],
                new(tex.Texture.SourceX, tex.Texture.SourceY, tex.Texture.SourceWidth, tex.Texture.SourceHeight),
                new(tex.Texture.TargetX, tex.Texture.TargetY, tex.Texture.TargetWidth * (float)args[4], tex.Texture.TargetHeight * (float)args[5]),
                new(spr.OriginX * (float)args[4], spr.OriginY * (float)args[5]),
                (float)args[6],
                new((((int)args[7]) >> 16) & 0xff, (((int)args[7]) >> 8) & 0xff, ((int)args[7]) & 0xff, (int)(((float)args[8]) * 255.0f))
            );
        }
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_set_halign(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        state.TextAlign.Item1 = (GameState.TextHAlign)Math.Clamp((int)args[0], 0, 2);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_set_valign(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        state.TextAlign.Item2 = (GameState.TextVAlign)Math.Clamp((int)args[0], 0, 2);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_set_colour(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        return draw_set_color(state, stack, inst, ins);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_set_color(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        state.DrawColor = (int)args[0];
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_set_alpha(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        state.DrawAlpha = (float)args[0];
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_set_font(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        // also count the default font
        if (args[0] < -1 || args[0] >= state.Data.Fonts.Count)
            return (new VMError(stack, VMErrorType.Runtime, $"font {args[0]} out-of-bounds"), null);
        state.DrawFont = (int)args[0];
        // todo: make sure the font is loaded into memory at this point
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) draw_text_transformed(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 6)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        foreach (var arg in args)
        {
            if (arg == null)
                return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        }
        // todo: count the set font
        // todo: optimize this shit
        var font = Raylib.GetFontDefault();
        var fSize = Raylib.MeasureTextEx(font, (string)args[2]!, font.BaseSize, font.GlyphPadding);
        var w = MathF.Ceiling(fSize.X);
        var h = MathF.Ceiling(fSize.Y);
        var tex = Raylib.LoadRenderTexture((int)w, (int)h);
        Raylib.BeginTextureMode(tex);
        Raylib.DrawText((string)args[2]!, 0, 0, font.BaseSize, new((state.DrawColor >> 16) & 0xff, (state.DrawColor >> 8) & 0xff, state.DrawColor & 0xff, (int)(state.DrawAlpha * 255.0f)));
        Raylib.EndTextureMode();
        Raylib.DrawTexturePro(tex.Texture, new(0, 0, w, h), new(0, 0, w * (float)args[3], h * (float)args[4]), new(
            state.TextAlign.Item1 switch
            {
                GameState.TextHAlign.Center => w * (float)args[3] / 2.0f,
                GameState.TextHAlign.Right => w * (float)args[3],
                _ => 0, // left
            },
            state.TextAlign.Item2 switch
            {
                GameState.TextVAlign.Middle => h * (float)args[4] / 2.0f,
                GameState.TextVAlign.Bottom => h * (float)args[4],
                _ => 0, // top
            }
        ), (float)args[5], Color.White);
        Raylib.UnloadRenderTexture(tex);
        return (null, null);
    }

    /*
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) __sample_function__(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, null);
    }
    */
}
