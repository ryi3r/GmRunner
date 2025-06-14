using States.GameState;
using UndertaleModLib.Models;

namespace BytecodeVM;

public class Instance
{
    public required GameState State;
    public UndertaleGameObject Object;
    public Dictionary<string, dynamic?> Variables = [];
    public required int ObjectIndex = -1;
    public uint Id
    {
        get
        {
            return (uint)Variables["id"];
        }
        set
        {
            Variables["id"] = value;
        }
    }
    public float XScale
    {
        get
        {
            return (float)Variables["image_xscale"];
        }
        set
        {
            Variables["image_xscale"] = value;
        }
    }
    public float YScale
    {
        get
        {
            return (float)Variables["image_yscale"];
        }
        set
        {
            Variables["image_yscale"] = value;
        }
    }
    public int Color
    {
        get
        {
            return (int)Variables["image_blend"];
        }
        set
        {
            Variables["image_blend"] = value;
        }
    }
    public float Alpha
    {
        get
        {
            return (float)Variables["image_alpha"];
        }
        set
        {
            Variables["image_alpha"] = value;
        }
    }
    public float Rotation
    {
        get
        {
            return (float)Variables["image_angle"];
        }
        set
        {
            Variables["image_angle"] = value;
        }
    }
    public float ImageSpeed
    {
        get
        {
            return (float)Variables["image_speed"];
        }
        set
        {
            Variables["image_speed"] = value;
        }
    }
    public float ImageIndex
    {
        get
        {
            return (float)Variables["image_index"];
        }
        set
        {
            Variables["image_index"] = value;
        }
    }
    public bool Visible
    {
        get
        {
            return true;//ILExecution.IsValueTruthy(new FunctionStack(), Variables["visible"], out VMError? _);
        }
        set
        {
            Variables["visible"] = value;
        }
    }
    public float X
    {
        get
        {
            return (float)Variables["x"];
        }
        set
        {
            Variables["x"] = value;
        }
    }
    public float Y
    {
        get
        {
            return (float)Variables["y"];
        }
        set
        {
            Variables["y"] = value;
        }
    }
    public UndertaleSprite? SpriteIndex
    {
        get
        {
            var v = Variables["sprite_index"];
            if (v == null || v < 0 || v >= State.Data.Sprites.Count)
                return null;
            return State.Data.Sprites[(int)v];
        }
        set
        {
            Variables["sprite_index"] = value == null ? null : (value is UndertaleSprite ? value : State.Data.Sprites.IndexOf(value));
        }
    }
    public UndertaleSprite? BboxIndex
    {
        get
        {
            var v = Variables["bbox_index"];
            if (v == null || v < 0 || v >= State.Data.Sprites.Count)
                return null;
            return State.Data.Sprites[(int)v];
        }
        set
        {
            Variables["bbox_index"] = value == null ? null : (value is UndertaleSprite ? value : State.Data.Sprites.IndexOf(value));
        }
    }
    public dynamic?[] Alarm
    {
        get
        {
            return Variables["alarm"]!;
        }
        set
        {
            Variables["alarm"] = value;
        }
    }
    public float XStart
    {
        get
        {
            return (float)Variables["xstart"];
        }
        set
        {
            Variables["xstart"] = value;
        }
    }
    public float YStart
    {
        get
        {
            return (float)Variables["ystart"];
        }
        set
        {
            Variables["ystart"] = value;
        }
    }

    public required InstanceLayer Layer;
    //public bool Initialized = false;

    public Instance(UndertaleGameObject obj)
    {
        Variables["id"] = 100000;
        Variables["image_xscale"] = 1.0f;
        Variables["image_yscale"] = 1.0f;
        Variables["image_blend"] = 0xffffff;
        Variables["image_alpha"] = 1.0f;
        Variables["image_angle"] = 0.0f;
        Variables["image_speed"] = 1.0f;
        Variables["image_index"] = 0.0f;
        Variables["visible"] = true;
        Variables["x"] = 0.0f;
        Variables["y"] = 0.0f;
        Variables["object_index"] = ObjectIndex;
        Variables["sprite_index"] = null;
        Variables["bbox_index"] = null;
        Variables["alarm"] = new dynamic?[12] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        Variables["xstart"] = 0.0f;
        Variables["ystart"] = 0.0f;

        Object = obj;
        SpriteIndex = obj.Sprite;
        BboxIndex = obj.TextureMaskId;
    }

    public VMError? RunEvent(EventType eventType, dynamic? subEvent, FunctionStack? _stack = null)
    {
        var stack = _stack ?? new();
        stack.InstancesScope = [this];
        var ev = Object.Events[(int)eventType].FirstOrDefault(ev => (int)ev.EventSubtype == (int)subEvent);
        if (ev != null)
        {
            foreach (var action in ev.Actions)
            {
                var err = State.Run(action.CodeId, out _, stack); ;
                if (err != null)
                    return err;
            }
        }
        return null;
    }

    public bool EventContainsCode(EventType eventType, dynamic? subEvent)
    {
        var ev = Object.Events[(int)eventType].FirstOrDefault(ev => (int)ev.EventSubtype == (int)subEvent);
        if (ev != null)
        {
            if (ev.Actions.Count > 0)
                return true;
        }
        return false;
    }
}
