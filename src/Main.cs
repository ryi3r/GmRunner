using System.Diagnostics;
using BytecodeVM;
using Raylib_cs;
using States;
using States.GameState;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace GmRunner;

class Runner
{
    public static readonly Mutex ThreadMutex = new();

    static State InternalState = new();
    public static State State
    {
        get
        {
            ThreadMutex.WaitOne();
            try
            {
                return InternalState;
            }
            finally
            {
                ThreadMutex.ReleaseMutex();
            }
        }

        set
        {
            ThreadMutex.WaitOne();
            try
            {
                InternalState = value;
            }
            finally
            {
                ThreadMutex.ReleaseMutex();
            }
        }
    }

    public static Task LoadGame(string filePath)
    {
        var state = new LoadingState()
        {
            Messages = [(LoadingState.MessageType.Info, "Loading data...")],
        };
        State = state;
        void WriteMessage(LoadingState.MessageType type, string message)
        {
            ThreadMutex.WaitOne();
            try
            {
                state.Messages.Add((type, message));
            }
            finally
            {
                ThreadMutex.ReleaseMutex();
            }
        }
        return Task.Run(() =>
        {
            /*try
            {*/
            GameSpecificResolver.BaseDirectory = Path.GetFullPath($"{filePath}/../");
            var bdata = new MemoryStream(File.ReadAllBytes(filePath));
            var reader = new UndertaleReader(bdata,
                    (warning, isImportant) => WriteMessage(isImportant ? LoadingState.MessageType.ImportantWarn : LoadingState.MessageType.Warn, warning.ToString()),
                    message => WriteMessage(LoadingState.MessageType.Info, message.ToString()),
                    false
                )
            {
                FilePath = Path.GetFullPath(filePath)
            };
            var data = reader.ReadUndertaleData();
            reader.ThrowIfUnreadObjects();
            if (data == null)
                WriteMessage(LoadingState.MessageType.Error, "Loaded data was null");
            else
            {
                WriteMessage(LoadingState.MessageType.Info, "Loaded data");
                var gState = new GameState(data)
                {
                    FilePath = filePath,
                };
                WriteMessage(LoadingState.MessageType.Info, "Initializing VM");
                gState.RegisterAll();
                WriteMessage(LoadingState.MessageType.Info, "Setting up images");
                for (var i = 0; i < data.EmbeddedTextures.Count; i++)
                {
                    WriteMessage(LoadingState.MessageType.Info, $"    Loading Texture {i + 1}");
                    var img = data.EmbeddedTextures[i].TextureData.Image.GetMagickImage();
                    img.Format = ImageMagick.MagickFormat.Bmp;
                    gState.TextureImages.Add(i, Raylib.LoadImageFromMemory(".bmp", img.ToByteArray()));
                }
                WriteMessage(LoadingState.MessageType.Info, "Loading audio groups");
                for (var i = 0; i < data.AudioGroups.Count; i++)
                {
                    var aGroup = data.AudioGroups[i];
                    var fPath = $"{filePath}/../audiogroup{i}.dat";
                    if (File.Exists(fPath))
                    {
                        var mData = new MemoryStream(File.ReadAllBytes(fPath));
                        var audioReader = new UndertaleReader(mData,
                                (warning, isImportant) => WriteMessage(isImportant ? LoadingState.MessageType.ImportantWarn : LoadingState.MessageType.Warn, warning.ToString()),
                                message => WriteMessage(LoadingState.MessageType.Info, message.ToString()),
                                false
                            )
                        {
                            FilePath = Path.GetFullPath(fPath)
                        };
                        var aData = audioReader.ReadUndertaleData();
                        audioReader.ThrowIfUnreadObjects();
                        gState.AudioGroups.Add(aGroup.Name.Content, aData);
                    }
                }
                WriteMessage(LoadingState.MessageType.Info, "Setting up audio");
                for (var i = 0; i < data.Sounds.Count; i++)
                {
                    var snd = data.Sounds[i];
                    WriteMessage(LoadingState.MessageType.Info, $"    Loading Audio: {snd.Name.Content}");
                    gState.AudioPool.Add(i, Audio.Load(gState, snd));
                }
                WriteMessage(LoadingState.MessageType.Info, "Caching data now");
                var erroredOut = false;
                foreach (var script in data.GlobalInitScripts)
                {
                    WriteMessage(LoadingState.MessageType.Info, $"    Running script: {script.Code.Name.Content}");
                    var st = Stopwatch.StartNew();
                    var err = gState.Run(script.Code, out _, new() { IsStaticOk = true });
                    st.Stop();
                    WriteMessage(LoadingState.MessageType.Info, $"    ...took {st.ElapsedMilliseconds}ms");
                    if (err != null)
                    {
                        erroredOut = true;
                        WriteMessage(LoadingState.MessageType.Error, err.ToString());
                        break;
                    }

                }
                if (!erroredOut)
                {
                    State = gState;
                    Console.WriteLine("ok");
                }
            }
            reader.Dispose();
            bdata.Dispose();
            /*}
            catch (Exception e)
            {
                Console.WriteLine(e);
                WriteMessage(LoadingState.MessageType.Error, e.ToString());
            }*/
        });
    }

    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    //[STAThread]
    public static void Main()
    {
        //ILExecution.ShowOutput = true;
        Raylib.SetTraceLogLevel(TraceLogLevel.None);
        Raylib.SetWindowState(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(640, 480, "GmRunner");
        Raylib.SetTargetFPS(60);
        Raylib.InitAudioDevice();

        // task that ensures audios are looped and stuff
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    foreach (var data in new Dictionary<int, Audio>(Audio.Playing))
                    {
                        if (!Audio.IsPlaying(data.Key))
                        {
                            if (data.Value.SetPlaying && data.Value.Looping)
                                data.Value.ForcePlay();
                            else
                                Audio.Stop(data.Key);
                        }
                    }
                    await Task.Delay(4);
                }
            });
        }

        //LoadGame("../../../DELTARUNE/chapter1_windows/data.win");
        LoadGame("../../../DELTARUNE/data.win");
        //LoadGame("/home/ryi3r/.local/share/Steam/steamapps/common/DELTARUNEdemo/data.win");
        //LoadGame("/home/ryi3r/.local/share/Steam/steamapps/common/Undertale/assets/game.unx");

        while (!Raylib.WindowShouldClose())
        {
            if (State is GameState gState)
            {
                var data = gState.Data;
                gState.BuiltinVariables["delta_time"] = (long)(Raylib.GetFrameTime() * 1_000_000);
                if (!gState.HasInit)
                {
                    gState.HasInit = true;
                    Raylib.SetWindowTitle(data.GeneralInfo.Name.Content);
                    Raylib.SetWindowSize((int)data.GeneralInfo.DefaultWindowWidth, (int)data.GeneralInfo.DefaultWindowHeight);
                    if ((data.Options.Info & UndertaleOptions.OptionsFlags.Sizeable) != UndertaleOptions.OptionsFlags.Sizeable)
                    {
                        Raylib.ClearWindowState(ConfigFlags.ResizableWindow);
                        Raylib.ClearWindowState(ConfigFlags.MaximizedWindow);
                    }
                    if ((data.Options.Info & UndertaleOptions.OptionsFlags.ShowCursor) != UndertaleOptions.OptionsFlags.ShowCursor)
                        Raylib.HideCursor();
                    if (data.IsGameMaker2())
                    {
                        Raylib.SetTargetFPS((int)data.GeneralInfo.GMS2FPS);
                        gState.BuiltinVariables["room_speed"] = (int)data.GeneralInfo.GMS2FPS;
                    }
                    /*var gdc = new GlobalDecompileContext(data);
                    var outp = new Underanalyzer.Decompiler.DecompileContext(gdc, data.Code[0], new Underanalyzer.Decompiler.DecompileSettings()).DecompileToAST();
                    {
                        var ind = data.Strings.Count;
                        data.Strings.Add(new("_chapter"));
                        data.Variables.Add(new()
                        {
                            Name = data.Strings[ind],
                            InstanceType = UndertaleInstruction.InstanceType.Local,
                            VarID = ind,
                            NameStringID = ind,
                        });
                        var cGrp = new CompileGroup(data);
                        var code = new UndertaleCode()
                        {
                            Name = new("sdfksdfjksdflj"),
                        };
                        cGrp.QueueCodeReplace(code, File.ReadAllText("../../../src/TestFiles/asm.txt"));
                        var r = cGrp.Compile();
                        if (!r.Successful)
                            Console.WriteLine("A");

                        gState.Run(new ILContainer()
                        {
                            Name = "sdfksdfjksdflj",
                            Instructions = [.. code.Instructions],
                        }, out _);
                    }*/
                    gState.RenderTexture = Raylib.LoadRenderTexture((int)data.GeneralInfo.DefaultWindowWidth, (int)data.GeneralInfo.DefaultWindowHeight);
                    gState.Functions["room_goto"](gState, new() { Stack = [0] }, new() { ArgumentsCount = 1 }, null);
                    Console.WriteLine("init ok");
                }
                //gState.UpdateCameraVariables();
                var lastInsts = new List<Instance>(gState.Instances.Values);
                /*foreach (var ins in lastInsts)
                {
                    if (!ins.Initialized)
                    {
                        ins.Initialized = true;
                        gState.HandleError(ins.RunEvent(EventType.Create, 0));
                    }
                }*/
                foreach (var ins in lastInsts)
                    gState.HandleError(ins.RunEvent(EventType.Step, EventSubtypeStep.BeginStep));
                foreach (var ins in gState.Instances.Values)
                {
                    for (var i = 0; i < ins.Alarm.Length; i++)
                    {
                        if (ins.Alarm[i] >= 0)
                        {
                            if (ins.Alarm[i] == 0)
                                gState.HandleError(ins.RunEvent(EventType.Alarm, i));
                            ins.Alarm[i]--;
                        }
                    }
                }
                foreach (var ins in lastInsts)
                    gState.HandleError(ins.RunEvent(EventType.Step, EventSubtypeStep.Step));
                foreach (var ins in lastInsts)
                    gState.HandleError(ins.RunEvent(EventType.Step, EventSubtypeStep.EndStep));
                Raylib.BeginDrawing();
                if (gState.Room?.DrawBackgroundColor ?? true)
                    Raylib.ClearBackground(new Color((byte)(data.Options.WindowColor & 0xff), (byte)((data.Options.WindowColor >> 8) & 0xff), (byte)((data.Options.WindowColor >> 16) & 0xff), (byte)((data.Options.WindowColor >> 24) & 0xff)));
                //Raylib.BeginTextureMode(gState.RenderTexture!.Value);
                foreach (var subev in new EventSubtypeDraw[] { EventSubtypeDraw.PreDraw, EventSubtypeDraw.DrawBegin, EventSubtypeDraw.Draw, EventSubtypeDraw.DrawEnd, EventSubtypeDraw.DrawGUIBegin, EventSubtypeDraw.DrawGUI, EventSubtypeDraw.DrawGUIEnd, EventSubtypeDraw.PostDraw, EventSubtypeDraw.Resize })
                {
                    foreach (var ins in lastInsts)
                    {
                        if (subev == EventSubtypeDraw.Draw && !ins.EventContainsCode(EventType.Draw, subev))
                        {
                            if (ins.SpriteIndex != null)
                            {
                                var imgIndex = (int)float.Floor(ins.ImageIndex);
                                var texs = ins.SpriteIndex.Textures;
                                var tex = texs[imgIndex % texs.Count];
                                var texIndex = data.EmbeddedTextures.IndexOf(tex.Texture.TexturePage);
                                if (texIndex != -1)
                                {
                                    gState.EnsureTextureIsLoaded(ins.SpriteIndex);
                                    Raylib.DrawTexturePro(
                                        gState.TextureTextures[texIndex],
                                        new(tex.Texture.SourceX, tex.Texture.SourceY, tex.Texture.SourceWidth, tex.Texture.SourceHeight),
                                        new(tex.Texture.TargetX, tex.Texture.TargetY, tex.Texture.TargetWidth, tex.Texture.TargetHeight),
                                        new(ins.SpriteIndex.OriginX, ins.SpriteIndex.OriginY),
                                        ins.Rotation,
                                        new((ins.Color >> 16) & 0xff, (ins.Color >> 8) & 0xff, ins.Color & 0xff, (int)(ins.Alpha * 255.0f))
                                    );
                                }
                            }
                        }
                        else
                            gState.HandleError(ins.RunEvent(EventType.Draw, subev));
                    }
                }
                //Raylib.EndTextureMode();
                //Raylib.DrawTexture(gState.RenderTexture!.Value.Texture, 0, 0, Color.White);
                // debug
                //Raylib.DrawFPS(5, 5);
                Raylib.EndDrawing();
            }
            else if (State is LoadingState lState)
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                var baseFont = Raylib.GetFontDefault();
                var y = Raylib.GetScreenHeight() - 10;
                for (var i = lState.Messages.Count - (1 + lState.Scroll); i >= 0; i--)
                {
                    var type = lState.Messages[i].Item1;
                    var message = lState.Messages[i].Item2;
                    y -= (int)Raylib.MeasureTextEx(baseFont, message, 20, baseFont.GlyphPadding).Y;
                    Raylib.DrawText(message, 10, y, 20, type switch
                    {
                        LoadingState.MessageType.Info => Color.White,
                        LoadingState.MessageType.Warn => Raylib.ColorBrightness(Color.Yellow, 0.6f),
                        LoadingState.MessageType.ImportantWarn => Color.Yellow,
                        LoadingState.MessageType.Error => Raylib.ColorBrightness(Color.Red, 0.6f),
                        _ => Color.Lime,
                    });
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Down))
                {
                    if (lState.Scroll + 1 < lState.Messages.Count - 1)
                        lState.Scroll++;
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Up))
                {
                    if (lState.Scroll > 0)
                        lState.Scroll--;
                }
                Raylib.EndDrawing();
            }
        }

        Raylib.CloseWindow();
        Raylib.CloseAudioDevice();
    }
}
