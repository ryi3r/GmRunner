using UndertaleModLib.Models;

namespace BytecodeVM;

public class FunctionStack
{
    public List<FunctionBacktrace> Stacktrace = [];
    public FunctionStack? Parent;
    public List<FunctionStack> Children = [];
    public int Depth = 0;
    public Dictionary<string, dynamic?> LocalVariables = [];
    public Dictionary<string, dynamic?> LocalBuiltin = [];
    public List<dynamic?> Stack = [];
    public List<dynamic> InstancesScope = [];
    public List<UndertaleInstruction.InstanceType> Scope = [UndertaleInstruction.InstanceType.Self];
    public bool IsStaticOk = false; // checks if its ok to define an static function (todo: check if this is true)

    public FunctionStack()
    {
        LocalBuiltin.Add("argument_count", 0);
        LocalBuiltin.Add("async_load", null);
    }

    public void Clean()
    {
        Stacktrace.Clear();
        Parent = null;
        Children.Clear();
        Depth = 0;
        LocalVariables.Clear();
        Stack.Clear();
        InstancesScope.Clear();
    }

    public FunctionStack Freeze(bool recursive)
    {
        var children = Children;
        if (recursive)
        {
            children = new List<FunctionStack>(Children.Capacity);
            foreach (var child in Children)
                children.Add(child.Freeze(true));
        }
        var sTrace = new List<FunctionBacktrace>(Stacktrace.Capacity);
        foreach (var value in Stacktrace)
            sTrace.Add(value.Freeze());
        return new()
        {
            Stacktrace = sTrace,
            Parent = recursive ? Parent?.Freeze(true) : Parent,
            Children = children,
            Depth = Depth,
            LocalVariables = new Dictionary<string, dynamic?>(LocalVariables),
            Stack = [.. Stack],
            InstancesScope = [.. InstancesScope],
            Scope = [.. Scope],
            IsStaticOk = IsStaticOk,
        };
    }

    public dynamic? PopStack(out VMError? error)
    {
        error = null;
        if (Stack.Count <= 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        var value = Stack[^1];
        Stack.RemoveAt(Stack.Count - 1);
        return value;
    }

    public dynamic? PopStack(int offset, out VMError? error)
    {
        error = null;
        if (Stack.Count + offset <= 0 || offset > 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        var value = Stack[Stack.Count - 1 + offset];
        Stack.RemoveAt(Stack.Count - 1 + offset);
        return value;
    }

    /*public dynamic? PopTopStack(out VMError? error)
    {
        error = null;
        if (Stack.Count <= 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        var value = Stack[0];
        Stack.RemoveAt(0);
        return value;
    }

    public dynamic? PopTopStack(int offset, out VMError? error)
    {
        error = null;
        if (Stack.Count - offset <= 0 || offset < 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        var value = Stack[offset];
        Stack.RemoveAt(offset);
        return value;
    }*/

    public void PushStack(dynamic? value)
    {
        Stack.Add(value);
    }

    /*public void PushTopStack(dynamic? value)
    {
        Stack.Insert(0, value);
    }*/

    public dynamic? PeekStack(out VMError? error)
    {
        error = null;
        if (Stack.Count <= 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        return Stack[^1];
    }

    /*public dynamic? PeekTopStack(out VMError? error)
    {
        error = null;
        if (Stack.Count <= 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        return Stack[0];
    }*/

    public dynamic? PeekStack(int offset, out VMError? error)
    {
        error = null;
        if (Stack.Count + offset <= 0 || offset > 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        return Stack[Stack.Count - 1 + offset];
    }

    /*public dynamic? PeekTopStack(int offset, out VMError? error)
    {
        error = null;
        if (Stack.Count - offset <= 0 || offset < 0)
        {
            error = new(this, VMErrorType.Runner, "unbalanced stack");
            return null;
        }
        return Stack[offset];
    }*/

    public VMError? PokeStack(dynamic? value)
    {
        if (Stack.Count <= 0)
            return new(this, VMErrorType.Runner, "unbalanced stack");
        Stack[^1] = value;
        return null;
    }

    /*public VMError? PokeTopStack(dynamic? value)
    {
        if (Stack.Count <= 0)
            return new(this, VMErrorType.Runner, "unbalanced stack");
        Stack[0] = value;
        return null;
    }*/

    public override string ToString()
    {
        return $"Stacktrace (Depth {Depth}): {string.Join(", ", Stacktrace.Select(s => s.Name))}";
    }
}
