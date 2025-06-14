using UndertaleModLib.Models;

namespace BytecodeVM;

public class FunctionBacktrace
{
    public required string Name;
    public int InstructionIndex;
    public required ILContainer Code;

    public FunctionBacktrace Freeze()
    {
        return new()
        {
            Name = Name,
            InstructionIndex = InstructionIndex,
            Code = Code,
        };
    }

    public override string ToString()
    {
        return $"{Name}:{InstructionIndex}";
    }
}
