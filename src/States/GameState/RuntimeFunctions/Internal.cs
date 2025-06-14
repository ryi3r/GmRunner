using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class RunnerInternal
{
    [GameState.DefineFunciton("@@NullObject@@")]
    public static (VMError?, dynamic?) null_object(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, null);
    }

    [GameState.DefineFunciton("@@NewGMLArray@@")]
    public static (VMError?, dynamic?) create_array(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        return (null, args.ToList());
    }

    [GameState.DefineFunciton("@@NewGMLObject@@")]
    public static (VMError?, dynamic?) create_object(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length <= 0)
            return (new(stack, VMErrorType.Runner, "too little arguments"), null);
        var fConst = new FunctionConstructor();
        var lastLocBuiltin = stack.LocalBuiltin;
        stack.LocalBuiltin = [];
        var customArgs = args[1..];
        stack.LocalBuiltin.Add("argument", customArgs);
        stack.LocalBuiltin.Add("argument_count", customArgs.Length);
        for (var i = 0; i < customArgs.Length; i++)
            stack.LocalBuiltin.Add($"argument{i}", customArgs[i]);
        stack.InstancesScope.Add(fConst);
        var lastLocVari = stack.LocalVariables;
        stack.LocalVariables = [];
        if (args[0] is RuntimeFunction rFunc)
            (err, _) = rFunc.Function(state, stack, new(), null);
        else
            err = state.Run(args[0], out dynamic? _, stack);
        stack.LocalVariables = lastLocVari;
        stack.InstancesScope.Remove(fConst);
        stack.LocalBuiltin = lastLocBuiltin;
        if (err != null)
            return (err, null);
        fConst.Initialized = true;
        return (null, fConst);
    }

    [GameState.DefineFunciton("@@This@@")]
    public static (VMError?, dynamic?) get_this(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        return (null, stack.InstancesScope.Count > 0 ? stack.InstancesScope[^1] : null);
    }
}
