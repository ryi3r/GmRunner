using System.Text;
using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public class TextFile
{
    static Mutex Mutex = new();
    static Dictionary<int, TextFile> InnerOpenFiles = [];
    static int InnerLastFileId = 1_000_000;
    public static Dictionary<int, TextFile> OpenFiles
    {
        get
        {
            Mutex.WaitOne();
            try
            {
                return InnerOpenFiles;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }

        set
        {
            Mutex.WaitOne();
            try
            {
                InnerOpenFiles = value;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }
    }
    public static int LastFileId
    {
        get
        {
            Mutex.WaitOne();
            try
            {
                return InnerLastFileId;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }

        set
        {
            Mutex.WaitOne();
            try
            {
                InnerLastFileId = value;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }
    }
    public bool IsWriting = false;
    public List<dynamic>? ReadData;
    public StringBuilder? WriteData;
    public string? FilePath;
    public int ReadLine = 0;
    public int LastReadLine = 0;

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_open_read(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        var fpath = state.GetFilePath((string)args[0]!);
        if (File.Exists(fpath))
        {
            var data = File.ReadAllText(fpath).Replace("\r\n", "\n").Split("\n");
            var file = new TextFile()
            {
                ReadData = [],
                FilePath = fpath,
            };
            foreach (var line in data)
            {
                if (line.EndsWith(' ') && double.TryParse(line.Trim(), out var num))
                    file.ReadData.Add(num);
                else
                    file.ReadData.Add(line);
            }
            var fid = LastFileId++;
            OpenFiles[fid] = file;
            return (null, fid);
        }
        return (null, -1);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_open_write(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        var fpath = state.GetFilePath((string)args[0]!);
        if (File.Exists(fpath))
        {
            var file = new TextFile()
            {
                WriteData = new(),
                IsWriting = true,
                FilePath = fpath,
            };
            var fid = LastFileId++;
            OpenFiles[fid] = file;
            return (null, fid);
        }
        return (null, -1);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_open_append(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        var fpath = state.GetFilePath((string)args[0]!)!;
        var file = new TextFile()
        {
            WriteData = new(),
            IsWriting = true,
            FilePath = fpath,
        };
        if (File.Exists(fpath))
            file.WriteData.Append(File.ReadAllText(fpath).Replace("\r\n", "\n"));
        var fid = LastFileId++;
        OpenFiles[fid] = file;
        return (null, fid);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_open_from_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        var data = ((string)args[0]!).Replace("\r\n", "\n").Split("\n");
        var file = new TextFile()
        {
            ReadData = [],
        };
        foreach (var line in data)
        {
            if (line.EndsWith(' ') && double.TryParse(line.Trim(), out var num))
                file.ReadData.Add(num);
            else
                file.ReadData.Add(line);
        }
        var fid = LastFileId++;
        OpenFiles[fid] = file;
        return (null, fid);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_read_real(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.ReadData == null || f.IsWriting)
            return (new(stack, VMErrorType.Runtime, "file is not open for reading"), null);
        if (f.ReadLine >= f.ReadData.Count)
            return (new(stack, VMErrorType.Runtime, "reached eof"), null);
        if (f.ReadData[f.ReadLine] is double num)
        {
            f.LastReadLine = f.ReadLine;
            return (null, num);
        }
        else
            return (new(stack, VMErrorType.Runtime, "unable to parse number"), null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_read_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.ReadData == null || f.IsWriting)
            return (new(stack, VMErrorType.Runtime, "file is not open for reading"), null);
        if (f.ReadLine >= f.ReadData.Count)
            return (new(stack, VMErrorType.Runtime, "reached eof"), null);
        f.LastReadLine = f.ReadLine;
        if (f.ReadData[f.ReadLine] is string str)
        {
            f.LastReadLine = f.ReadLine;
            return (null, str);
        }
        else
            return (new(stack, VMErrorType.Runtime, "unable to parse string"), null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_readln(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.ReadData == null || f.IsWriting)
            return (new(stack, VMErrorType.Runtime, "file is not open for reading"), null);
        if (f.ReadLine >= f.ReadData.Count)
            return (new(stack, VMErrorType.Runtime, "reached eof"), null);
        f.LastReadLine = f.ReadLine;
        return (null, f.ReadData[f.ReadLine++]);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_write_real(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.WriteData == null || !f.IsWriting)
            return (new(stack, VMErrorType.Runtime, "file is not open for writing"), null);
        f.WriteData.Append($"{(double)args[1]} ");
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_write_string(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.WriteData == null || !f.IsWriting)
            return (new(stack, VMErrorType.Runtime, "file is not open for writing"), null);
        f.WriteData.Append($"{args[1]}");
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_writeln(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.WriteData == null || !f.IsWriting)
            return (new(stack, VMErrorType.Runtime, "file is not open for writing"), null);
        f.WriteData.Append($"\n");
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_eoln(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        return (null, f.LastReadLine >= f.ReadLine);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_eof(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        return (null, f.ReadLine >= (f.ReadData?.Count ?? 0));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) file_text_close(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        if (!OpenFiles.ContainsKey((int)args[0]))
            return (new(stack, VMErrorType.Runtime, "file is not open"), null);
        var f = OpenFiles[(int)args[0]];
        if (f.WriteData != null && f.FilePath != null && f.IsWriting)
            File.WriteAllText(f.FilePath, f.WriteData.ToString());
        return (null, true);
    }
}
