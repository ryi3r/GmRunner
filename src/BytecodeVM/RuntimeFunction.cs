using States.GameState;
using UndertaleModLib.Models;

namespace BytecodeVM;

public class RuntimeFunction
{
    public required string Name;
    public required Func<GameState, FunctionStack, UndertaleInstruction, Instance?, (VMError?, dynamic?)> Function;
}
