using System.Globalization;
using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerOs
{
    public const int OsUnknown = -1;
    public const int OsWindows = 0;
    public const int OsMacOsX = 1;
    public const int OsIos = 3;
    public const int OsAndroid = 4;
    public const int OsLinux = 6;
    public const int OsTvOs = 20;

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) os_get_language(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, CultureInfo.InstalledUICulture.TwoLetterISOLanguageName);
    }
}
