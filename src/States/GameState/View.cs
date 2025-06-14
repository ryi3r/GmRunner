using Raylib_cs;

namespace BytecodeVM;

public class View
{
    public required Camera2D Camera;
    public float XBorder = 0.0f;
    public float YBorder = 0.0f;
    public float XSpeed = 0.0f;
    public float YSpeed = 0.0f;
    public float X = 0.0f;
    public float Y = 0.0f;
    public float XSize = 256.0f;
    public float YSize = 256.0f;
    public float TargetX = 0.0f;
    public float TargetY = 0.0f;
    public float TargetXSize = 256.0f;
    public float TargetYSize = 256.0f;
}