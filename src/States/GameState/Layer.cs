using UndertaleModLib.Models;

namespace BytecodeVM;

public class Layer
{
    public required UndertaleRoom.Layer? Data;
    public long Depth = 0;
    public float XOffset = 0.0f;
    public float YOffset = 0.0f;
    public float XSpeed = 0.0f;
    public float YSpeed = 0.0f;
    public bool Visible = true;
    public bool AutoManage = false;
}
