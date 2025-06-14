using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public class Audio
{
    static Mutex Mutex = new();
    static Dictionary<dynamic, HashSet<Audio>> InnerPool = [];
    static Dictionary<int, Audio> InnerPlaying = [];
    static int InnerLastPlayingId = 100000;
    public static Dictionary<dynamic, HashSet<Audio>> Pool
    {
        get
        {
            Mutex.WaitOne();
            try
            {
                return InnerPool;
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
                InnerPool = value;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }
    }
    public static Dictionary<int, Audio> Playing
    {
        get
        {
            Mutex.WaitOne();
            try
            {
                return InnerPlaying;
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
                InnerPlaying = value;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }
    }
    public static int LastPlayingId
    {
        get
        {
            Mutex.WaitOne();
            try
            {
                return InnerLastPlayingId;
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
                InnerLastPlayingId = value;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }
    }

    public Music? Music;
    public Sound? Sound;
    public UndertaleSound? SoundOrigin;
    public bool IsAlias = false;
    public bool Looping = false;
    public bool SetPlaying = false;

    public Audio Load()
    {
        var aud = new Audio
        {
            SoundOrigin = SoundOrigin,
        };
        if (Sound != null)
            aud.Sound = Raylib.LoadSoundAlias((Sound)Sound);
        var snd = SoundOrigin!.AudioFile.Data;
        if (snd[0..4] == "OggS"u8.ToArray())
            aud.Music = Raylib.LoadMusicStreamFromMemory(".ogg", snd);
        return aud;
    }

    public static Audio Load(GameState state, UndertaleSound sound)
    {
        var aud = new Audio
        {
            SoundOrigin = sound
        };
        byte[] snd;

        if (sound.AudioID != -1 && sound.AudioGroup != null && state.AudioGroups.TryGetValue(sound.AudioGroup.Name.Content, out var aGroup))
            snd = aGroup.EmbeddedAudio[sound.AudioID].Data;
        else if (sound.AudioFile != null)
            snd = sound.AudioFile.Data;
        else
            snd = File.ReadAllBytes($"{state.FilePath}/../{sound.File.Content}");

        if (snd[0..4] == "OggS"u8.ToArray())
            aud.Music = Raylib.LoadMusicStreamFromMemory(".ogg", snd);
        else
            aud.Sound = Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(".wav", snd));
        if (!Pool.ContainsKey(sound))
            Pool[sound] = [];
        return aud;
    }

    public dynamic GetSoundOrigin()
    {
        return SoundOrigin!;
    }

    public int Play()
    {
        var snd = GetSoundOrigin();
        var pool = (HashSet<Audio>)Pool[snd];
        if (pool.Contains(this))
        {
            if (Music != null)
                Raylib.PlayMusicStream((Music)Music);
            if (Sound != null)
                Raylib.PlaySound((Sound)Sound);
            pool.Remove(this);
            SetPlaying = true;
            var id = LastPlayingId++;
            Playing.Add(id, this);
            return id;
        }
        else
        {
            var aud = Load();
            if (aud.Music != null)
                Raylib.PlayMusicStream((Music)aud.Music);
            if (aud.Sound != null)
                Raylib.PlaySound((Sound)aud.Sound);
            aud.SetPlaying = true;
            var id = LastPlayingId++;
            Playing.Add(id, aud);
            return id;
        }
    }

    public int ForcePlay()
    {
        var snd = GetSoundOrigin();
        var pool = (HashSet<Audio>)Pool[snd];
        if (!pool.Contains(this))
        {
            if (Music != null)
                Raylib.PlayMusicStream((Music)Music);
            if (Sound != null)
                Raylib.PlaySound((Sound)Sound);
            SetPlaying = true;
            pool.Remove(this);
            var id = LastPlayingId++;
            Playing.Add(id, this);
            return id;
        }
        else
            return Play();
    }

    public static void Stop(int index)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            aud.SetPlaying = false;
            if (aud.Music != null)
                Raylib.StopMusicStream((Music)aud.Music);
            if (aud.Sound != null)
                Raylib.StopSound((Sound)aud.Sound);
            Playing.Remove(index);
            ((HashSet<Audio>)Pool[aud.GetSoundOrigin()]).Add(aud);
        }
    }

    public static void Pause(int index)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                Raylib.PauseMusicStream((Music)aud.Music);
            if (aud.Sound != null)
                Raylib.PauseSound((Sound)aud.Sound);
        }
    }

    public static void Resume(int index)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                Raylib.ResumeMusicStream((Music)aud.Music);
            if (aud.Sound != null)
                Raylib.ResumeSound((Sound)aud.Sound);
        }
    }

    public static bool IsPlaying(int index)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null && Raylib.IsMusicStreamPlaying((Music)aud.Music))
                return true;
            if (aud.Sound != null && Raylib.IsSoundPlaying((Sound)aud.Sound))
                return true;
        }
        return false;
    }

    public static bool IsPlayingGeneral(GameState state, int index)
    {
        {
            if (Playing.TryGetValue(index, out var aud))
            {
                if (aud.Music != null && Raylib.IsMusicStreamPlaying((Music)aud.Music))
                    return true;
                if (aud.Sound != null && Raylib.IsSoundPlaying((Sound)aud.Sound))
                    return true;
            }
        }
        if (index >= 0 && index < state.Data.EmbeddedAudio.Count)
        {
            var snd = state.Data.EmbeddedAudio[index];
            foreach (var aud in new List<Audio>(Playing.Values))
            {
                if (aud.GetSoundOrigin() == snd)
                {
                    if (aud.Music != null && Raylib.IsMusicStreamPlaying((Music)aud.Music))
                        return true;
                    if (aud.Sound != null && Raylib.IsSoundPlaying((Sound)aud.Sound))
                        return true;
                }
            }
        }
        return false;
    }

    public static void SetVolume(int index, float volume)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                Raylib.SetMusicVolume((Music)aud.Music, volume);
            if (aud.Sound != null)
                Raylib.SetSoundVolume((Sound)aud.Sound, volume);
        }
    }

    public static void SetVolumeGeneral(GameState state, int index, float volume)
    {
        {
            if (Playing.TryGetValue(index, out var aud))
            {
                if (aud.Music != null)
                    Raylib.SetMusicVolume((Music)aud.Music, volume);
                if (aud.Sound != null)
                    Raylib.SetSoundVolume((Sound)aud.Sound, volume);
            }
        }
        if (index >= 0 && index < state.Data.EmbeddedAudio.Count)
        {
            var snd = state.Data.EmbeddedAudio[index];
            foreach (var aud in new List<Audio>(Playing.Values))
            {
                if (aud.GetSoundOrigin() == snd)
                {
                    if (aud.Music != null)
                        Raylib.SetMusicVolume((Music)aud.Music, volume);
                    if (aud.Sound != null)
                        Raylib.SetSoundVolume((Sound)aud.Sound, volume);
                }
            }
        }
    }

    public static void SetPitch(int index, float pitch)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                Raylib.SetMusicPitch((Music)aud.Music, pitch);
            if (aud.Sound != null)
                Raylib.SetSoundPitch((Sound)aud.Sound, pitch);
        }
    }

    public static void Seek(int index, float time)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                Raylib.SeekMusicStream((Music)aud.Music, time);
        }
    }

    public static float GetLength(int index)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                return Raylib.GetMusicTimeLength((Music)aud.Music);
        }
        return -1.0f;
    }

    public static float GetPlayedTime(int index)
    {
        if (Playing.TryGetValue(index, out var aud))
        {
            if (aud.Music != null)
                return Raylib.GetMusicTimePlayed((Music)aud.Music);
        }
        return 0.0f;
    }

    public static void StopAll()
    {
        foreach (var key in new List<int>(Playing.Keys))
            Stop(key);
    }

    ~Audio()
    {
        if (Music != null)
            Raylib.UnloadMusicStream((Music)Music);
        if (Sound != null)
        {
            if (IsAlias)
                Raylib.UnloadSoundAlias((Sound)Sound);
            else
                Raylib.UnloadSound((Sound)Sound);
        }
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) audio_play_sound(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length < 3 || args.Length > 7)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        foreach (var arg in args)
        {
            if (arg == null)
                return (new(stack, VMErrorType.Runtime, "invalid argument types"), null);
        }
        if (state.AudioPool.TryGetValue((int)args[0], out var aud))
        {
            var id = aud.Play();
            // todo: priority (args[1])
            aud.Looping = ILExecution.IsValueTruthy(stack, args[2], out err);
            if (err != null)
            {
                Stop(id);
                return (err, -1);
            }
            if (args.Length >= 4) SetVolume(id, (float)args[3]);
            if (args.Length >= 5) Seek(id, (float)args[4]);
            if (args.Length >= 6) SetPitch(id, (float)args[5]);
            // todo: listener mask
            return (null, id);
        }
        return (null, -1);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) audio_exists(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        return (null, (int)args[0] >= 0 && state.Data.EmbeddedAudio.Count < (int)args[0]);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) audio_is_playing(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        return (null, IsPlayingGeneral(state, (int)args[0]));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) audio_sound_gain(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        // todo: arg2 == time to set the gain
        SetVolumeGeneral(state, (int)args[0], (float)args[1]);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) audio_sound_length(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
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
        // todo: arg2 == time to set the gain
        GetLength((int)args[0]);
        return (null, null);
    }
}
