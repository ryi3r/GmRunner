using BytecodeVM;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Variables
{
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) method(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? _)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null)
            args[0] = (int)UndertaleInstruction.InstanceType.Self;
        if (args[1] is not UndertaleFunction && args[1] is not RuntimeFunction && args[0] as int? == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        if (args[1] is UndertaleFunction uFunc)
        {
            var code = state.Data.Code.First(entry => entry.Name.Content == uFunc.Name.Content);
            var rFunc = new RuntimeFunction()
            {
                Name = uFunc.Name.Content,
                Function = (state, stack, inst, ins) =>
                {
                    var customArgs = state.HandleFunctionArguments(stack, inst, code.ArgumentsCount, out var err);
                    if (err != null)
                        return (err, null);
                    if (customArgs == null)
                        return (new(stack, VMErrorType.Runner, "arguments are null"), null);
                    var lastLocBuiltin = stack.LocalBuiltin;
                    stack.LocalBuiltin = [];
                    stack.LocalBuiltin.Add("argument", customArgs);
                    stack.LocalBuiltin.Add("argument_count", customArgs.Length);
                    for (var i = 0; i < customArgs.Length; i++)
                        stack.LocalBuiltin.Add($"argument{i}", customArgs[i]);
                    var lastLocVari = stack.LocalVariables;
                    stack.LocalVariables = [];
                    var ind = -1;
                    if (ins != null)
                    {
                        ind = stack.InstancesScope.Count;
                        stack.InstancesScope.Add(ins);
                    }
                    err = state.Run(code, out var value, stack);
                    if (ind != -1)
                        stack.InstancesScope.RemoveAt(ind);
                    stack.LocalBuiltin = lastLocBuiltin;
                    stack.LocalVariables = lastLocVari;
                    return (err, value);
                },
            };
            if (stack.InstancesScope.Count <= 0 && (int)args[0] == (int)UndertaleInstruction.InstanceType.Self)
            {
                state.Functions[uFunc.Name.Content] = rFunc.Function;
                return (null, rFunc);
            }
            else
            {
                err = ILExecution.SetScopedVari(state, stack, (UndertaleInstruction.InstanceType)(int)args[0], uFunc.Name.Content, rFunc);
                if (err != null)
                    return (err, null);
                return (null, rFunc);
            }
        }
        else if (args[1] is RuntimeFunction rFunc)
        {
            if (stack.InstancesScope.Count <= 0 && args[0] == (int)UndertaleInstruction.InstanceType.Self)
            {
                state.Functions[rFunc.Name] = rFunc.Function;
                return (null, rFunc);
            }
            else
            {
                err = ILExecution.SetScopedVari(state, stack, (UndertaleInstruction.InstanceType)(int)args[0], rFunc.Name, rFunc);
                if (err != null)
                    return (err, null);
                return (null, rFunc);
            }
        }
        else
            return (new VMError(stack, VMErrorType.RunnerInternal, "expected undertalefunction or runtimefunction"), null);
    }
}