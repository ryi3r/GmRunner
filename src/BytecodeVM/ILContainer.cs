using UndertaleModLib.Models;

namespace BytecodeVM;

public class ILContainer
{
    public required string Name;
    public int StartOffset = 0;
    public required UndertaleInstruction[] Instructions;
}