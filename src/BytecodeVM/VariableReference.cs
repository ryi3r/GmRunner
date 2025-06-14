using UndertaleModLib.Models;

namespace BytecodeVM;

public class VariableReference
{
    public string? VariableName;
    public UndertaleInstruction.InstanceType Scope = UndertaleInstruction.InstanceType.Undefined;
    public UndertaleInstruction.VariableType ReferenceType = UndertaleInstruction.VariableType.Normal;
    public bool HasValue;
    public dynamic? Value;
}