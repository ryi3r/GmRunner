using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerInstance
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) instance_exists(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] < 0 || args[0] > state.Data.GameObjects.Count)
            return (new VMError(stack, VMErrorType.Runtime, $"object {args[0]} is out-of-bounds"), null);
        return (null, state.Instances.Values.Any((obj) => obj.ObjectIndex == args[0]));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) instance_number(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] < 0 || args[0] > state.Data.GameObjects.Count)
            return (new VMError(stack, VMErrorType.Runtime, $"object {args[0]} is out-of-bounds"), null);
        return (null, state.Instances.Values.Count((obj) => obj.ObjectIndex == args[0] || args[0] == (int)UndertaleInstruction.InstanceType.All));
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) instance_destroy(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length > 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        foreach (var (id, tIns) in state.Instances)
        {
            if (args.Length > 0 ? id == args[0] || tIns.ObjectIndex == args[0] : tIns == stack.InstancesScope[^1])
            {
                tIns.RunEvent(EventType.Destroy, 0);
                state.Instances.Remove(id);
                state.InstancesList.Remove(id);
                tIns.RunEvent(EventType.CleanUp, 0);
                if (tIns.Layer.AutoManage)
                {
                    foreach (var (key, value) in state.Layers)
                    {
                        if (value == tIns.Layer)
                        {
                            state.Layers.Remove(key);
                            state.OrderedLayers.Remove(key);
                            break;
                        }
                    }
                }
            }
        }
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) instance_create_depth(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 4 && args.Length != 5)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null || args[2] == null || args[3] == null || (args.Length == 5 ? args[4] == null : false))
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        if (args[3] < 0 || args[3] >= state.Data.GameObjects.Count)
            return (new(stack, VMErrorType.Runtime, "game object out-of-bounds"), null);
        var layer = new InstanceLayer()
        {
            Data = null,
            Depth = args[2],
            XOffset = 0.0f,
            YOffset = 0.0f,
            XSpeed = 0.0f,
            YSpeed = 0.0f,
            Visible = true,
        };
        var obj = state.Data.GameObjects[args[3]];
        var gInst = new Instance(obj)
        {
            State = state,
            Id = state.LastInstanceId++,
            ObjectIndex = (int)args[3],
            X = (float)args[0],
            Y = (float)args[1],
            XStart = (float)args[0],
            YStart = (float)args[1],
            Layer = layer,
        };
        layer.Instances.Add(gInst);
        state.Instances.Add(gInst.Id, gInst);
        state.InstancesList.Add(gInst.Id); // will always be last!
        {
            string? key = null;
            while (key == null || state.Layers.ContainsKey(key))
                key = $"__runner_layer__{state.Random.NextInt64()}";
            state.Layers.Add(key, layer);
            state.OrderedLayers.Add(key);
        }
        state.OrderedLayers.Sort((a, b) => state.Layers[a].Depth.CompareTo(state.Layers[b].Depth));
        gInst.RunEvent(EventType.PreCreate, 0);
        if (args.Length == 5)
        {
            foreach (var (key, value) in (Dictionary<string, dynamic?>)args[4]!)
                gInst.Variables[key] = value;
        }
        // todo: probably not GameMaker behaviour, it's probably supposed to be once the objects are processed instead
        gInst.RunEvent(EventType.Create, 0);
        return (null, gInst);
    }
}
