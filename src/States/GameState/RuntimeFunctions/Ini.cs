using System.Text;
using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public class Ini
{
    public Dictionary<string, Dictionary<string, dynamic?>> Data = [];
    public string? FilePath;

    public static Ini Parse(string str)
    {
        var ini = new Ini();
        var currentHeader = "";
        // we shouldn't need to account for \r
        foreach (var line in str.Replace("\r\n", "\n").Split("\n"))
        {
            if (line.StartsWith('[') && line.EndsWith(']'))
                currentHeader = line[1..^1];
            else if (line.Contains('='))
            {
                if (!ini.Data.ContainsKey(currentHeader))
                    ini.Data.Add(currentHeader, []);
                var eqPos = line.IndexOf('=');
                var key = line[..eqPos];
                var value = line[(eqPos + 1)..];
                // we should probably ignore any errors if there's duplicated values
                if (value.StartsWith('"') && value.EndsWith('"')) // this implies a value stored in a string
                {
                    value = value[1..^1].Replace("\\\"", "\"");
                }
                if (double.TryParse(value, out var res))
                    ini.Data[currentHeader][key] = res;
                else
                    ini.Data[currentHeader][key] = value;
            }
        }
        return ini;
    }

    public string Write()
    {
        var strBuilder = new StringBuilder();
        foreach (var (headerKey, headerValue) in Data)
        {
            strBuilder.AppendFormat("[{0}]\n", headerKey);
            foreach (var (key, value) in headerValue)
            {
                strBuilder.AppendFormat("{0}=", key);
                if (value is string sValue)
                    strBuilder.AppendFormat("\"{0}\"\n", sValue.Replace("\"", "\\\""));
                /*else if (ILExecution.IsValueNumber(value))
                    strBuilder.AppendFormat("{0}\n", value);*/
            }
        }
        return strBuilder.ToString();
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_open(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        var fPath = state.GetFilePath(args[0]);
        state.IniFile = Ini.Parse(File.Exists(fPath) ? File.ReadAllText(fPath) : "");
        state.IniFile.FilePath = fPath;
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_open_from_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        state.IniFile = Ini.Parse(args[0]);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_close(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (state.IniFile != null && state.IniFile.FilePath != null)
            File.WriteAllText(state.IniFile.FilePath, state.IniFile.Write());
        state.IniFile = null;
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_write_real(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (state.IniFile != null)
        {
            if (!state.IniFile.Data.ContainsKey(args[0]))
                state.IniFile.Data.Add((string)args[0]!, []);
            state.IniFile.Data[args[0]][args[1]] = (double)args[2];
        }
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_write_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (state.IniFile != null)
        {
            if (!state.IniFile.Data.ContainsKey(args[0]))
                state.IniFile.Data.Add((string)args[0]!, []);
            state.IniFile.Data[args[0]][args[1]] = args[2]!.ToString();
        }
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_read_real(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 3)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        if (state.IniFile != null)
        {
            if (!state.IniFile.Data.ContainsKey(args[0]) || !state.IniFile.Data[(string)args[0]!].ContainsKey(args[0]))
                return (null, args[2]);
            return (null, (double)state.IniFile.Data[args[0]][args[1]]);
        }
        return (null, args[2]);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_read_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 3)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        if (state.IniFile != null)
        {
            if (!state.IniFile.Data.ContainsKey(args[0]) || !state.IniFile.Data[(string)args[0]!].ContainsKey(args[0]))
                return (null, args[2]);
            return (null, state.IniFile.Data[args[0]][args[1]].ToString());
        }
        return (null, args[2]);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_key_exists(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (state.IniFile != null)
        {
            if (!state.IniFile.Data.ContainsKey(args[0]))
                return (null, false);
            return (null, state.IniFile.Data[(string)args[0]!].ContainsKey(args[1]));
        }
        return (null, false);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_section_exists(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        if (state.IniFile != null)
            return (null, state.IniFile.Data.ContainsKey(args[0]));
        return (null, false);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_key_delete(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (state.IniFile != null && state.IniFile.Data.ContainsKey(args[0]))
            state.IniFile!.Data[(string)args[0]!].Remove(args[1]);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) ini_section_delete(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        state.IniFile?.Data.Remove(args[0]);
        return (null, null);
    }
}
