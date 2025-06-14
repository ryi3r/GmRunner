using System.Globalization;
using System.Reflection;
using BytecodeVM;
using Raylib_cs;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace States.GameState;

public class GameState(UndertaleData data) : State
{
    public UndertaleData Data = data;
    public required string FilePath;

    public bool HasInit = false;

    public int RoomId = -1;
    public UndertaleRoom? Room;

    public Dictionary<int, Image> TextureImages = [];
    public Dictionary<int, Audio> AudioPool = [];

    public Dictionary<int, Texture2D> TextureTextures = [];
    public Dictionary<string, dynamic?> GlobalVariables = [];
    public Dictionary<string, dynamic?> BuiltinVariables = [];
    public Dictionary<string, dynamic?> StaticVariables = [];
    #region Instance Data
    public Dictionary<uint, Instance> Instances = [];
    public List<uint> InstancesList = [];
    public uint LastInstanceId = data.GeneralInfo.LastObj + 1;
    #endregion
    #region Room Data
    public Dictionary<string, Layer> Layers = [];
    public List<string> OrderedLayers = [];
    #endregion
    public Dictionary<string, Func<GameState, FunctionStack, UndertaleInstruction, Instance?, (VMError?, dynamic?)>> Functions = [];
    public List<View> Views = [];
    public int ArrayOwnerId = 0; // Copy-on-write GameMaker behavior
    public Ini? IniFile;
    public Random Random = new();
    public Dictionary<string, UndertaleData> AudioGroups = [];
    public RenderTexture2D? RenderTexture;
    #region Text Align
    public enum TextHAlign
    {
        Left = 0,
        Center = 1,
        Right = 2,
    }
    public enum TextVAlign
    {
        Top = 0,
        Middle = 1,
        Bottom = 2,
    }
    public (TextHAlign, TextVAlign) TextAlign = (TextHAlign.Left, TextVAlign.Top);
    #endregion
    public int DrawColor = 0xffffff;
    public float DrawAlpha = 1.0f;
    public int DrawFont = -1; // todo: make this shit work

    public void Free()
    {

    }

    public void RegisterAll()
    {
        RegisterBuiltinVariables();
        //RegisterFunctions();
        DefineFunciton.Initialize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code"></param>
    /// <param name="highlightInstruction"></param>
    /// <returns>(the string line where the highlighted instruction is, the decompiled code)</returns>
    public (int?, string) DecompileCode(UndertaleCode? code, int highlightInstruction)
    {
        if (code == null)
            return (null, $"<??? at {highlightInstruction}>");
        var dcont = new DecompileContext(code);
        var ast = dcont.DecompileToAST();
        var dec = new Underanalyzer.Decompiler.AST.ASTPrinter(dcont);
        ast.Print(dec);
        //dec.OutputString;
        return (0, "");
    }

    public void HandleError(VMError? error)
    {
        if (error != null)
        {
            Console.WriteLine(error);
            #region Decompilated stacktrace
            /*Console.WriteLine("============== Decompilated stacktrace ==============");
            Console.WriteLine("============== Decompilated stacktrace ==============");*/
            #endregion
            // todo
        }
    }

    public void HandleErrorDynamic(dynamic? error)
    {
        if (error is VMError)
            HandleError(error);
    }

    public dynamic?[]? HandleFunctionArguments(FunctionStack stack, UndertaleInstruction inst, out VMError? error)
    {
        error = null;
        var args = new dynamic?[inst.ArgumentsCount];
        for (var i = 0; i < inst.ArgumentsCount; i++)
        {
            var value = stack.PopStack(out var err);
            if (err != null)
            {
                error = err;
                return null;
            }
            args[i] = ILExecution.EnsureNativeValue(this, stack, out err, value);
            if (err != null)
            {
                error = err;
                return null;
            }
        }
        return args;
        /*error = null;
        var args = new dynamic?[inst.ArgumentsCount];
        for (var i = inst.ArgumentsCount - 1; i >= 0; i--)
        {
            var value = stack.PopStack(out var err);
            if (err != null)
            {
                error = err;
                return null;
            }
            args[i] = ILExecution.EnsureNativeValue(this, stack, out err, value);
            if (err != null)
            {
                error = err;
                return null;
            }
        }
        return args;*/
    }

    public dynamic?[]? HandleFunctionArguments(FunctionStack stack, UndertaleInstruction inst, int atLeast, out VMError? error)
    {
        error = null;
        var args = new dynamic?[Math.Max(inst.ArgumentsCount, atLeast)];
        for (var i = 0; i < inst.ArgumentsCount; i++)
        {
            var value = stack.PopStack(out var err);
            if (err != null)
            {
                error = err;
                return null;
            }
            args[i] = ILExecution.EnsureNativeValue(this, stack, out err, value);
            if (err != null)
            {
                error = err;
                return null;
            }
        }
        return args;
    }

    public string? GetFilePath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            if ((Data.Options.Info & UndertaleOptions.OptionsFlags.DisableSandbox) == UndertaleOptions.OptionsFlags.DisableSandbox)
                return path;
        }
        else // todo: verify if this path ends up in the config folder or not
            return $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/{Data.GeneralInfo.FileName.Content}/{path}";
        return null;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DefineFunciton(string? name = null) : Attribute
    {
        public string? Name = name;

        public static void Initialize(GameState gState)
        {
            var funcs = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(f => f.GetTypes())
                .SelectMany(m => m.GetMethods(BindingFlags.Static | BindingFlags.Public))
                .Where(m => m.GetCustomAttributes(typeof(DefineFunciton), false).Length > 0);
            foreach (var func in funcs)
            {
                if (func != null)
                {
                    gState.Functions.Add(func.GetCustomAttribute<DefineFunciton>()!.Name ?? func.Name, (state, stack, inst, ins) =>
                    {
                        return ((VMError?, dynamic?))func.Invoke(null, [state, stack, inst, ins])!;
                    });
                    ;
                    Console.WriteLine($"Registered function: {func.Name}");
                }
            }
        }
    }

    public void RegisterBuiltinVariables()
    {
        if (OperatingSystem.IsWindows())
            BuiltinVariables.Add("os_type", RunnerOs.OsWindows);
        else if (OperatingSystem.IsLinux())
            BuiltinVariables.Add("os_type", RunnerOs.OsLinux);
        else if (OperatingSystem.IsMacOS())
            BuiltinVariables.Add("os_type", RunnerOs.OsMacOsX);
        else if (OperatingSystem.IsAndroid())
            BuiltinVariables.Add("os_type", RunnerOs.OsAndroid);
        else if (OperatingSystem.IsIOS())
            BuiltinVariables.Add("os_type", RunnerOs.OsIos);
        else if (OperatingSystem.IsTvOS())
            BuiltinVariables.Add("os_type", RunnerOs.OsTvOs);
        else
            BuiltinVariables.Add("os_type", RunnerOs.OsUnknown);
        BuiltinVariables.Add("undefined", null);
        BuiltinVariables.Add("application_surface", Surface.ApplicationSurface);
        BuiltinVariables.Add("room_width", 0);
        BuiltinVariables.Add("room_height", 0);
        BuiltinVariables.Add("room_speed", 0);
        BuiltinVariables.Add("room", 0);
        var wPort = new List<dynamic?>();
        var hPort = new List<dynamic?>();
        var xPort = new List<dynamic?>();
        var yPort = new List<dynamic?>();
        for (var i = 0; i < 8; i++)
        {
            Views.Add(new View()
            {
                Camera = new(),
                XBorder = 32,
                YBorder = 32,
            });
            wPort.Add(640.0f);
            hPort.Add(480.0f);
            xPort.Add(0.0f);
            yPort.Add(0.0f);
        }
        BuiltinVariables["view_wport"] = wPort.ToArray();
        BuiltinVariables["view_hport"] = hPort.ToArray();
        BuiltinVariables["view_xport"] = xPort.ToArray();
        BuiltinVariables["view_yport"] = yPort.ToArray();
        // todo
    }

    public void EnsureTextureIsLoaded(UndertaleSprite sprite)
    {
        foreach (var tex in sprite.Textures)
        {
            var pagItem = tex.Texture;
            var pagIndex = Data.EmbeddedTextures.IndexOf(pagItem.TexturePage);
            if (pagIndex != -1 && TextureImages.TryGetValue(pagIndex, out var img) && !TextureTextures.ContainsKey(pagIndex))
                TextureTextures.Add(pagIndex, Raylib.LoadTextureFromImage(img));
        }
    }

    public VMError? Run(ILContainer code, out dynamic? returnValue, FunctionStack? stack = null)
    {
        var err = ILExecution.Run(this, code, out returnValue, stack);
        HandleError(err);
        return err;
    }

    public VMError? Run(UndertaleFunction? code, out dynamic? returnValue, FunctionStack? stack = null)
    {
        var err = ILExecution.Run(this, code, out returnValue, stack);
        HandleError(err);
        return err;
    }

    public VMError? Run(UndertaleCode? code, out dynamic? returnValue, FunctionStack? stack = null)
    {
        var err = ILExecution.Run(this, code, out returnValue, stack);
        HandleError(err);
        return err;
    }

    public void UpdateCameraVariables()
    {
        // todo: support runner modifying these variables
        {
            var wPort = (dynamic?[])BuiltinVariables["view_wport"]!;
            for (var i = 0; i < wPort.Length; i++)
                wPort[i] = Views[i].XSize;
        }
        {
            var hPort = (dynamic?[])BuiltinVariables["view_hport"]!;
            for (var i = 0; i < hPort.Length; i++)
                hPort[i] = Views[i].YSize;
        }

        {
            var xPort = (dynamic?[])BuiltinVariables["view_xport"]!;
            for (var i = 0; i < xPort.Length; i++)
                xPort[i] = Views[i].X;
        }
        {
            var yPort = (dynamic?[])BuiltinVariables["view_yport"]!;
            for (var i = 0; i < yPort.Length; i++)
                yPort[i] = Views[i].Y;
        }
    }
}
