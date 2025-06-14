using States.GameState;

namespace BytecodeVM;

public class InstanceLayer : Layer
{
    public List<Instance> Instances = [];
    public bool RunnerManaged = false;
}