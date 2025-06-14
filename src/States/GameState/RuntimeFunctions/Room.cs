using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Room
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) room_goto(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 1)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] < 0 || args[0] > state.Data.GeneralInfo.RoomOrder.Count)
            return (new VMError(stack, VMErrorType.Runtime, $"room {args[0]} is out-of-bounds"), null);
        for (var i = state.Instances.Count - 1; i >= 0; i--)
        {
            var instId = state.InstancesList[i];
            var tIns = state.Instances[instId];
            tIns.RunEvent(EventType.Other, EventSubtypeOther.RoomEnd);
            if (!tIns.Object.Persistent)
            {
                tIns.RunEvent(EventType.Destroy, 0);
                state.Instances.Remove(instId);
                state.InstancesList.RemoveAt(i);
                tIns.RunEvent(EventType.CleanUp, 0);
            }
        }
        // todo: free up resources (textures or smth) used by layers
        state.Layers.Clear();
        state.OrderedLayers.Clear();
        state.RoomId = (int)args[0];
        state.Room = state.Data.GeneralInfo.RoomOrder[state.RoomId].Resource;
        state.BuiltinVariables["room"] = state.RoomId;
        state.BuiltinVariables["room_width"] = state.Room.Width;
        state.BuiltinVariables["room_height"] = state.Room.Height;
        if (!state.Data.IsGameMaker2())
        {
            Raylib.SetTargetFPS((int)state.Room.Speed);
            state.BuiltinVariables["room_speed"] = state.Room.Speed;
        }
        foreach (var tIns in state.Instances.Values)
            tIns.RunEvent(EventType.Other, EventSubtypeOther.RoomStart);
        foreach (var rLayer in state.Room.Layers)
        {
            Layer? layer = null;
            switch (rLayer.LayerType)
            {
                /*case UndertaleRoom.LayerType.Path:
                    break;*/
                case UndertaleRoom.LayerType.Background:
                    layer = new()
                    {
                        Data = rLayer,
                        Depth = rLayer.LayerDepth,
                        XOffset = rLayer.XOffset,
                        YOffset = rLayer.YOffset,
                        XSpeed = rLayer.HSpeed,
                        YSpeed = rLayer.VSpeed,
                        Visible = rLayer.IsVisible,
                    };
                    break;
                case UndertaleRoom.LayerType.Instances:
                    {
                        layer = new InstanceLayer()
                        {
                            Data = rLayer,
                            Depth = rLayer.LayerDepth,
                            XOffset = rLayer.XOffset,
                            YOffset = rLayer.YOffset,
                            XSpeed = rLayer.HSpeed,
                            YSpeed = rLayer.VSpeed,
                            Visible = rLayer.IsVisible,
                        };
                        var iLayerCast = (InstanceLayer)layer;
                        var iLayer = rLayer.InstancesData;
                        foreach (var tIns in iLayer.Instances)
                        {
                            if (!tIns.Nonexistent && !(tIns.ObjectDefinition.Persistent && state.Instances.ContainsKey(tIns.InstanceID)))
                            {
                                var gInst = new Instance(tIns.ObjectDefinition)
                                {
                                    State = state,
                                    Id = tIns.InstanceID,
                                    ObjectIndex = state.Data.GameObjects.IndexOf(tIns.ObjectDefinition),
                                    XScale = tIns.ScaleX,
                                    YScale = tIns.ScaleY,
                                    Color = (int)((tIns.Color & 0xffffff) >> 8),
                                    Alpha = (tIns.Color & 0xff) / 255.0f,
                                    Rotation = tIns.Rotation,
                                    ImageSpeed = tIns.ImageSpeed,
                                    ImageIndex = tIns.ImageIndex,
                                    X = tIns.X,
                                    Y = tIns.Y,
                                    XStart = tIns.X,
                                    YStart = tIns.Y,
                                    Layer = iLayerCast,
                                };
                                if (gInst.ObjectIndex == -1)
                                    return (new VMError(stack, VMErrorType.Runner, $"{tIns.ObjectDefinition.Name.Content} object index not found"), null);
                                iLayerCast.Instances.Add(gInst);
                                state.Instances.Add(gInst.Id, gInst);
                                state.InstancesList.Add(gInst.Id);
                                gInst.RunEvent(EventType.PreCreate, 0);
                            }
                        }
                    }
                    break;
                /*case UndertaleRoom.LayerType.Assets:
                    break;
                case UndertaleRoom.LayerType.Tiles:
                    break;
                case UndertaleRoom.LayerType.Effect:
                    break;
                case UndertaleRoom.LayerType.Path2:
                    break;*/
                default:
                    return (new VMError(stack, VMErrorType.Runtime, $"layertype {rLayer.LayerType} not supported"), null);
            }
            state.Layers.Add(rLayer.LayerName.Content, layer!);
            state.OrderedLayers.Add(rLayer.LayerName.Content);
        }
        state.OrderedLayers.Sort((a, b) => state.Layers[a].Depth.CompareTo(state.Layers[b].Depth));
        state.InstancesList.Sort();
        foreach (var instIndex in state.InstancesList.ToArray())
            state.Instances[instIndex].RunEvent(EventType.Create, 0);
        state.Run(state.Room.CreationCodeId, out _);
        return (null, null);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) room_restart(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        stack.PushStack(state.RoomId);
        inst.ArgumentsCount = 1;
        return room_goto(state, stack, inst, ins);
    }
}
