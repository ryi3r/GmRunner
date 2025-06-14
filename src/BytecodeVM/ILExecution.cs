using UndertaleModLib.Models;

namespace BytecodeVM;

public class ILExecution
{
    // scope constants
    public const int Self = -1;
    public const int Other = -2;
    public const int All = -3;
    public const int NoOne = -4;
    public const int Global = -5;
    public const int Local = -7;

    public static bool ShowOutput = false;

    public static VMError? GetScopedVari(States.GameState.GameState state, FunctionStack stack, UndertaleInstruction.InstanceType scope, string name, out dynamic? value)
    {
        value = null;
        switch (scope)
        {
            case UndertaleInstruction.InstanceType.Undefined:
                if (!state.GlobalVariables.TryGetValue(name, out value))
                    return new(stack, VMErrorType.Runtime, $"[GetScopedVari] global variable is not defined ({name})");
                break;
            case UndertaleInstruction.InstanceType.Self:
                {
                    if (stack.InstancesScope.Count <= 0)
                    {
                        value = null;
                        return new(stack, VMErrorType.RunnerInternal, "[GetScopedVari] [InstanceType.Self] instance scope is empty");
                    }
                    var inss = stack.InstancesScope[^1];
                    if (!inss.Variables.TryGetValue(name, out value))
                        return new(stack, VMErrorType.Runtime, $"[GetScopedVari] {inss.Object.Name.Content} variable is not defined ({name})");
                }
                break;
            case UndertaleInstruction.InstanceType.Other:
                {
                    if (stack.InstancesScope.Count <= 1)
                    {
                        value = null;
                        return new(stack, VMErrorType.RunnerInternal, "[GetScopedVari] [InstanceType.Other] instance scope is too small");
                    }
                    var inss = stack.InstancesScope[^2];
                    if (!inss.Variables.TryGetValue(name, out value))
                        return new(stack, VMErrorType.Runtime, $"[GetScopedVari] {inss.Object.Name.Content} variable is not defined ({name})");
                }
                break;
            case UndertaleInstruction.InstanceType.All:
                value = null;
                return new(stack, VMErrorType.Runtime, "[GetScopedVari] scope should not be InstanceType.All");
            case UndertaleInstruction.InstanceType.Noone:
                value = null;
                return new(stack, VMErrorType.Runtime, "[GetScopedVari] scope should not be InstanceType.Noone");
            case UndertaleInstruction.InstanceType.Global:
                if (!state.GlobalVariables.TryGetValue(name, out value))
                    return new(stack, VMErrorType.Runtime, $"[GetScopedVari] global variable is not defined ({name})");
                break;
            case UndertaleInstruction.InstanceType.Builtin or UndertaleInstruction.InstanceType.Arg:
                if (!stack.LocalBuiltin.TryGetValue(name, out value))
                    if (!state.BuiltinVariables.TryGetValue(name, out value))
                        return new(stack, VMErrorType.Runtime, $"[GetScopedVari] builtin variable is not defined ({name})");
                break;
            case UndertaleInstruction.InstanceType.Local:
                if (!stack.LocalVariables.TryGetValue(name, out value))
                    return new(stack, VMErrorType.Runtime, $"[GetScopedVari] temporal variable is not defined ({name})");
                break;
            case UndertaleInstruction.InstanceType.Stacktop:
                {
                    if (stack.InstancesScope.Count <= 0)
                    {
                        value = null;
                        return new(stack, VMErrorType.RunnerInternal, "[GetScopedVari] [InstanceType.Stacktop] instance scope is too small");
                    }
                    var inss = stack.InstancesScope[0];
                    if (!inss.Variables.TryGetValue(name, out value))
                        return new(stack, VMErrorType.Runtime, $"[GetScopedVari] {inss.Object.Name.Content}: variable is not defined ({name})");
                }
                break;
            case UndertaleInstruction.InstanceType.Static:
                if (!state.StaticVariables.TryGetValue(name, out value))
                    return new(stack, VMErrorType.Runtime, $"[GetScopedVari] static variable is not defined ({name})");
                break;
        }
        return null;
    }

    public static VMError? SetScopedVari(States.GameState.GameState state, FunctionStack stack, UndertaleInstruction.InstanceType scope, string name, dynamic? value)
    {
        switch (scope)
        {
            case UndertaleInstruction.InstanceType.Undefined:
                state.GlobalVariables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.Self:
                if (stack.InstancesScope.Count <= 0)
                    return new(stack, VMErrorType.RunnerInternal, "[SetScopedVari] [InstanceType.Self] instance scope is empty");
                stack.InstancesScope[^1].Variables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.Other:
                if (stack.InstancesScope.Count <= 1)
                    return new(stack, VMErrorType.RunnerInternal, "[SetScopedVari] [InstanceType.Other] instance scope is too small");
                stack.InstancesScope[^2].Variables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.All:
                {
                    foreach (var inst in state.Instances.Values)
                        inst.Variables[name] = value;
                }
                break;
            case UndertaleInstruction.InstanceType.Noone:
                return new(stack, VMErrorType.Runtime, "[SetScopedVari] scope should not be InstanceType.Noone");
            case UndertaleInstruction.InstanceType.Global:
                state.GlobalVariables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.Builtin:
                //return new(stack, VMErrorType.Runtime, "[SetScopedVari] scope should not be InstanceType.Builtin");
                state.BuiltinVariables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.Local:
                stack.LocalVariables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.Stacktop:
                if (stack.InstancesScope.Count <= 0)
                    return new(stack, VMErrorType.RunnerInternal, "[SetScopedVari] [InstanceType.Stacktop] instance scope is too small");
                stack.InstancesScope[0].Variables[name] = value;
                break;
            case UndertaleInstruction.InstanceType.Arg:
                return new(stack, VMErrorType.Runtime, "[SetScopedVari] scope should not be InstanceType.Arg");
            case UndertaleInstruction.InstanceType.Static:
                state.StaticVariables[name] = value;
                break;
            default:
                {
                    if (!state.Instances.ContainsKey((uint)scope))
                        return new(stack, VMErrorType.Runtime, $"[SetScopedVari] expected instance id but found nothing? {(uint)scope}");
                    state.Instances[(uint)scope].Variables[name] = value;
                }
                break;
        }
        return null;
    }

    public static dynamic? EnsureNativeValue(States.GameState.GameState state, FunctionStack stack, out VMError? err, dynamic? value)
    {
        err = null;
        while (value is VariableReference rValue)
        {
            if (rValue.HasValue)
                value = rValue.Value;
            else
            {
                err = GetScopedVari(state, stack, rValue.Scope, rValue.VariableName!, out var nValue);
                if (err != null)
                    return null;
                if (rValue.ReferenceType == UndertaleInstruction.VariableType.Array)
                {
                    if (nValue == null)
                    {
                        err = new(stack, VMErrorType.Runtime, "array is null");
                        return null;
                    }
                    if (nValue.GetType().IsArray)
                    {
                        if (rValue.Value >= nValue.Length)
                            nValue = null;
                        else
                            nValue = nValue[rValue.Value];
                    }
                    else
                    {
                        if (rValue.Value >= nValue.Count)
                            nValue = null;
                        else
                            nValue = nValue[rValue.Value];
                    }
                }
                value = nValue;
            }
        }
        return value;
    }

    public static bool IsValueTruthy(FunctionStack stack, dynamic? value, out VMError? error)
    {
        error = null;
        switch (value)
        {
            case double:
                return value >= 0.5;
            case float:
                return value >= 0.5f;
            case int or long or uint or short:
                return value >= 1;
            case bool:
                return value;
            case VariableReference:
                error = new(stack, VMErrorType.RunnerInternal, $"[IsValueTruthy] [DataType.Variable] unable to convert variable type VariableReference (this should never happen!!)");
                break;
            case string:
                return ((string)value).Length > 0;
            case null:
                return false;
            default: // todo: handle arrays, dictionaries & other complex datatypes
                error = new(stack, VMErrorType.RunnerInternal, $"[IsValueTruthy] [DataType.Variable] unable to check if value is truthy {value}");
                break;
        }
        return false;
    }

    public static int DataTypeToSize(FunctionStack stack, UndertaleInstruction.DataType dataType, out VMError? error)
    {
        error = null;
        switch (dataType)
        {
            case UndertaleInstruction.DataType.Double or UndertaleInstruction.DataType.Int64:
                return 8;
            case UndertaleInstruction.DataType.Int32 or UndertaleInstruction.DataType.Boolean or UndertaleInstruction.DataType.String or UndertaleInstruction.DataType.Int16:
                return 4;
            case UndertaleInstruction.DataType.Variable:
                return 16;
            default:
                error = new(stack, VMErrorType.RunnerInternal, $"unknown datatype DataType.{dataType}");
                return 0;
        }
    }

    public static UndertaleInstruction.DataType VariToDataType(FunctionStack stack, dynamic? vari, out VMError? error)
    {
        error = null;
        switch (vari)
        {
            case double:
                return UndertaleInstruction.DataType.Double;
            case float:
                return UndertaleInstruction.DataType.Float;
            case int:
                return UndertaleInstruction.DataType.Int32;
            case long:
                return UndertaleInstruction.DataType.Int64;
            case bool:
                return UndertaleInstruction.DataType.Boolean;
            case VariableReference:
                return UndertaleInstruction.DataType.Variable;
            case string:
                return UndertaleInstruction.DataType.String;
            case uint:
                return UndertaleInstruction.DataType.UnsignedInt;
            case short:
                return UndertaleInstruction.DataType.Int16;
            case null:
                return UndertaleInstruction.DataType.Undefined;
            default: // todo: handle arrays, dictionaries & other complex datatypes
                error = new(stack, VMErrorType.RunnerInternal, $"[Opcode.Conv] [DataType.Variable] unable to convert variable {vari}");
                return UndertaleInstruction.DataType.Undefined;
        }
    }

    public static bool IsValueNumber(dynamic? val)
    {
        return val is double || val is float || val is short || val is int || val is long || val is uint;
    }

    public static int ConvertNumberToInt(dynamic val)
    {
        return val switch
        {
            bool => val ? 1 : 0,
            _ => (int)val,
        };
    }

    public static float ConvertNumberToFloat(dynamic val)
    {
        return val switch
        {
            bool => val ? 1.0f : 0.0f,
            _ => (float)val,
        };
    }

    public static double ConvertNumberToDouble(dynamic val)
    {
        return val switch
        {
            bool => val ? 1.0 : 0.0,
            _ => (double)val,
        };
    }

    public static VMError? Run(States.GameState.GameState state, UndertaleFunction? func, out dynamic? returnValue, FunctionStack? _stack = null)
    {
        if (func == null) // assuming empty
        {
            returnValue = null;
            return null;
        }
        return Run(state, state.Data.Code.First(entry => entry.Name.Content == func.Name.Content), out returnValue, _stack);
    }

    public static VMError? Run(States.GameState.GameState state, UndertaleCode? code, out dynamic? returnValue, FunctionStack? _stack = null)
    {
        if (code == null) // assuming empty
        {
            returnValue = null;
            return null;
        }
        var ilcont = new ILContainer()
        {
            Name = code.Name.Content,
            Instructions = code.ParentEntry != null ? [.. code.ParentEntry.Instructions] : [.. code.Instructions],
        };
        //var length = code.Instructions.Count;
        if (code.ParentEntry != null)
        {
            var instOffset = (int)(code.Offset / 4); // todo: i hope these are in instruction size and not index size
            code = code.ParentEntry;
            while (instOffset > 0)
                instOffset -= (int)code.Instructions[ilcont.StartOffset++].CalculateInstructionSize();
            // seems like each child function has an `exit` op at the end of it so not necessary
            /*length = ilcont.StartOffset;
            var instLength = code.Instructions[ilcont.StartOffset - 1].JumpOffset;
            while (instLength > 0)
                instLength -= (int)code.Instructions[length++].CalculateInstructionSize();*/
        }
        return Run(state, ilcont, out returnValue, _stack);
    }

    public static VMError? Run(States.GameState.GameState state, ILContainer code, out dynamic? returnValue, FunctionStack? _stack = null)
    {
        returnValue = null;
        var stack = (_stack ?? new())!;
        var backtrace = new FunctionBacktrace()
        {
            Name = code.Name,
            InstructionIndex = 0,
            Code = code,
        };
        stack.Stacktrace.Add(backtrace);
        var instIndex = code.StartOffset;
        var skipAddrSize = 0;
        if (ShowOutput)
            Console.WriteLine($"Running code: {code.Name}");
        while (instIndex < code.Instructions.Length)
        {
            backtrace.InstructionIndex = instIndex;
            var inst = code.Instructions[instIndex];
            if (skipAddrSize > 0)
            {
                skipAddrSize -= (int)inst.CalculateInstructionSize();
                if (skipAddrSize < 0)
                    return new(stack, VMErrorType.RunnerInternal, "unbalanced address");
                instIndex++;
                continue;
            }
            else if (skipAddrSize < 0) // this shit's horrible god damn
            {
                skipAddrSize += (int)inst.CalculateInstructionSize();
                if (skipAddrSize > 0)
                {
                    //return new(stack, VMErrorType.RunnerInternal, "unbalanced address");
                    skipAddrSize = 0; // i have no idea how to fix this, so this works as a workaround
                }
                else
                {
                    instIndex--;
                    continue;
                }
            }
            if (ShowOutput)
                Console.WriteLine($"\t{inst} (t1:{inst.Type1},t2:{inst.Type2},scope:{inst.TypeInst}) ({instIndex})");
            switch (inst.Kind)
            {
                case UndertaleInstruction.Opcode.Conv:
                    {
                        var vari = stack.PeekStack(out var err);
                        if (err != null)
                            return err;
                        var type1 = inst.Type1;
                        if (inst.Type1 == UndertaleInstruction.DataType.Variable)
                        {
                            if (vari is VariableReference vVari)
                            {
                                if (vVari.HasValue)
                                    vari = vVari.Value;
                                else
                                {
                                    err = GetScopedVari(state, stack, vVari.Scope, inst.ValueVariable?.Name.Content ?? vVari.VariableName!, out vari);
                                    if (err != null)
                                        return err;
                                    if (vari != null && vVari.Value != null && vVari.ReferenceType == UndertaleInstruction.VariableType.Array)
                                    {
                                        if ((int)vVari.Value >= 0 && (int)vVari.Value < (vari!.GetType().IsArray ? vari!.Length : vari!.Count))
                                            vari = vari![(int)vVari.Value];
                                    }
                                }
                            }
                            // if it's not a vari ref then we assume it's already the value
                            type1 = VariToDataType(stack, vari, out err);
                            if (err != null)
                                return err;
                        }
                        switch (type1)
                        {
                            case UndertaleInstruction.DataType.Variable:
                                return new(stack, VMErrorType.RunnerInternal, "[Opcode.Conv] [DataType.Variable] unexpected data type");
                            case UndertaleInstruction.DataType.String:
                                switch (inst.Type2)
                                {
                                    case UndertaleInstruction.DataType.Double:
                                        {
                                            if (double.TryParse(vari, out double output))
                                                vari = output;
                                            else
                                                return new(stack, VMErrorType.Runtime, "[Opcode.Conv] [DataType.String] unable to convert to DataType.Double (invalid number)");
                                        }
                                        break;
                                    case UndertaleInstruction.DataType.Float:
                                        {
                                            if (float.TryParse(vari, out float output))
                                                vari = output;
                                            else
                                                return new(stack, VMErrorType.Runtime, "[Opcode.Conv] [DataType.String] unable to convert to DataType.Float (invalid number)");
                                        }
                                        break;
                                    case UndertaleInstruction.DataType.Int32:
                                        {
                                            if (int.TryParse(vari, out int output))
                                                vari = output;
                                            else
                                                return new(stack, VMErrorType.Runtime, "[Opcode.Conv] [DataType.String] unable to convert to DataType.Int32 (invalid number)");
                                        }
                                        break;
                                    case UndertaleInstruction.DataType.Int64:
                                        {
                                            if (long.TryParse(vari, out long output))
                                                vari = output;
                                            else
                                                return new(stack, VMErrorType.Runtime, "[Opcode.Conv] [DataType.String] unable to convert to DataType.Int64 (invalid number)");
                                        }
                                        break;
                                    case UndertaleInstruction.DataType.Boolean:
                                        vari = ((string)vari!).Length > 0;
                                        break;
                                    case UndertaleInstruction.DataType.Variable:
                                        if (vari is not VariableReference)
                                            vari = new VariableReference()
                                            {
                                                HasValue = true,
                                                Value = vari,
                                            };
                                        break;
                                    case UndertaleInstruction.DataType.Delete:
                                        return new(stack, VMErrorType.RunnerInternal, "[Opcode.Conv] [DataType.Delete] unable to convert to type");
                                    case UndertaleInstruction.DataType.Undefined:
                                        vari = null;
                                        break;
                                    case UndertaleInstruction.DataType.UnsignedInt:
                                        vari = (uint)vari;
                                        break;
                                    case UndertaleInstruction.DataType.Int16:
                                        vari = (short)vari;
                                        break;
                                }
                                break;
                            case UndertaleInstruction.DataType.Delete:
                                if (inst.Type2 != UndertaleInstruction.DataType.Delete)
                                    return new(stack, VMErrorType.RunnerInternal, "[Opcode.Conv] [DataType.Delete] unable to convert to a type that is not DataType.Delete");
                                break;
                            case UndertaleInstruction.DataType.Undefined:
                                vari = null;
                                break;
                            case UndertaleInstruction.DataType.Boolean:
                                switch (inst.Type2)
                                {
                                    case UndertaleInstruction.DataType.Double:
                                        vari = vari >= 0.5;
                                        break;
                                    case UndertaleInstruction.DataType.Float:
                                        vari = vari >= 0.5f;
                                        break;
                                    case UndertaleInstruction.DataType.Int32 or UndertaleInstruction.DataType.Int64 or UndertaleInstruction.DataType.Int16 or UndertaleInstruction.DataType.UnsignedInt:
                                        vari = vari >= 1;
                                        break;
                                    case UndertaleInstruction.DataType.Variable:
                                        {
                                            vari = IsValueTruthy(stack, vari, out err);
                                            if (err != null)
                                                return err;
                                        }
                                        break;
                                    case UndertaleInstruction.DataType.String:
                                        vari = vari != null ? ((string)vari)?.Length > 0 : false;
                                        break;
                                    case UndertaleInstruction.DataType.Delete:
                                        return new(stack, VMErrorType.RunnerInternal, "[Opcode.Conv] [DataType.Delete] unable to convert to type");
                                    case UndertaleInstruction.DataType.Undefined:
                                        vari = null;
                                        break;
                                }
                                break;
                            default:
                                switch (inst.Type2)
                                {
                                    case UndertaleInstruction.DataType.Double:
                                        vari = (double)vari;
                                        break;
                                    case UndertaleInstruction.DataType.Float:
                                        vari = (float)vari;
                                        break;
                                    case UndertaleInstruction.DataType.Int32:
                                        vari = (int)vari;
                                        break;
                                    case UndertaleInstruction.DataType.Int64:
                                        vari = (long)vari;
                                        break;
                                    case UndertaleInstruction.DataType.Variable:
                                        if (vari is not VariableReference)
                                            vari = new VariableReference()
                                            {
                                                HasValue = true,
                                                Value = vari,
                                            };
                                        break;
                                    case UndertaleInstruction.DataType.String:
                                        vari = string.Format("{}", vari);
                                        break;
                                    case UndertaleInstruction.DataType.Delete:
                                        return new(stack, VMErrorType.RunnerInternal, "[Opcode.Conv] [DataType.Delete] unable to convert to type");
                                    case UndertaleInstruction.DataType.Undefined:
                                        vari = null;
                                        break;
                                    case UndertaleInstruction.DataType.UnsignedInt:
                                        vari = (uint)vari;
                                        break;
                                    case UndertaleInstruction.DataType.Int16:
                                        vari = (short)vari;
                                        break;
                                }
                                break;
                        }
                        err = stack.PokeStack(vari);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Mul:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Mul] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Mul] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, nval1 * nval2);
                        else*/
                        err = stack.PokeStack(nval1 * nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Div:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Div] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Div] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, nval1 / nval2);
                        else*/
                        err = stack.PokeStack(nval1 / nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Rem:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Rem] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Rem] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, Math.DivRem((long)nval1, (long)nval2).Remainder);
                        else*/
                        err = stack.PokeStack(Math.DivRem((long)nval1, (long)nval2).Remainder);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Mod:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Mod] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Mod] unable to perform operation with values {val1} and {val2}");
                        if ((float)nval2 == 0.0)
                            return new(stack, VMErrorType.Runtime, $"[Opcode.Mod] value2 must not be 0");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName, nval1 % nval2);
                        else*/
                        err = stack.PokeStack(nval1 % nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Add:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Add] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!(IsValueNumber(nval1) || nval1 is string) || !(IsValueNumber(nval2) || nval2 is string))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Add] unable to perform operation with values {nval1} and {nval2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName, nval1 + nval2);
                        else*/
                        err = stack.PokeStack(nval1 + nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Sub:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Sub] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Sub] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName, nval1 - nval2);
                        else*/
                        err = stack.PokeStack(nval1 - nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.And:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.And] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.And] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, (long)nval1 & (long)nval2);
                        else*/
                        err = stack.PokeStack((long)nval1 & (long)nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Or:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Or] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Or] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, (long)nval1 | (long)nval2);
                        else*/
                        err = stack.PokeStack((long)nval1 | (long)nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Xor:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Xor] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Xor] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, (long)nval1 ^ (long)nval2);
                        else*/
                        err = stack.PokeStack((long)nval1 ^ (long)nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Neg:
                    {
                        if (stack.Stack.Count <= 0)
                            return new(stack, VMErrorType.Runner, "[Opcode.Neg] expected the stack to contain at least 1 element but it's empty");
                        var value = stack.PeekStack(out var err);
                        if (err != null)
                            return err;
                        var val1 = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(val1))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Neg] unable to perform operation with values {val1}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, -val1);
                        else*/
                        err = stack.PokeStack(-val1);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Not:
                    {
                        if (stack.Stack.Count <= 0)
                            return new(stack, VMErrorType.Runner, "[Opcode.Not] expected the stack to contain at least 1 element but it's empty");
                        var sValue = stack.PeekStack(out VMError? err);
                        if (err != null)
                            return err;
                        var value = EnsureNativeValue(state, stack, out err, sValue);
                        if (err != null)
                            return err;
                        switch (value)
                        {
                            case double:
                                value = BitConverter.UInt64BitsToDouble(~BitConverter.DoubleToUInt64Bits(value));
                                break;
                            case float:
                                value = BitConverter.UInt32BitsToSingle(~BitConverter.SingleToUInt32Bits(value));
                                break;
                            case bool:
                                value = !value;
                                break;
                            case int or long or short or uint:
                                value = ~value;
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Not] unable to bitwise invert value {value}");
                        }
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && sValue is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, value);
                        else*/
                        err = stack.PokeStack(value);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Shl:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Shl] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Shl] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, (long)nval1 << (int)nval2);
                        else*/
                        err = stack.PokeStack((long)nval1 << (int)nval2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Shr:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.Shr] expected the stack to contain at least 2 elements but it's empty");
                        var val2 = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nval2 = EnsureNativeValue(state, stack, out err, val2);
                        if (err != null)
                            return err;
                        var val1 = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var nval1 = EnsureNativeValue(state, stack, out err, val1);
                        if (err != null)
                            return err;
                        if (!IsValueNumber(nval1) || !IsValueNumber(nval2))
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Shr] unable to perform operation with values {val1} and {val2}");
                        /*if (inst.Type2 == UndertaleInstruction.DataType.Variable && val1 is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, (long)nval1 >> (int)nval2);
                        else*/
                        err = stack.PokeStack((long)val1 >> (int)val2);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Cmp:
                    {
                        if (stack.Scope.Count <= 0)
                            return new(stack, VMErrorType.Runner, "[Opcode.Cmp] expected the scope stack to contain at least 1 element but it's empty");
                        var value = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var cmp2 = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        value = stack.PeekStack(out err);
                        if (err != null)
                            return err;
                        var cmp1 = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        // ensure that bool values always end up as numbers
                        {
                            var swap = false;
                            if (cmp2 is bool && IsValueNumber(cmp1))
                            {
                                swap = true;
                                (cmp1, cmp2) = (cmp2, cmp1);
                            }
                            if (cmp1 is bool && IsValueNumber(cmp2))
                            {
                                switch (cmp2)
                                {
                                    case double:
                                        cmp1 = cmp1 ? 1.0 : 0.0;
                                        break;
                                    case float:
                                        cmp1 = cmp1 ? 1.0f : 0.0f;
                                        break;
                                    case short or int or long or uint:
                                        cmp1 = cmp1 ? 1 : 0;
                                        break;
                                    default:
                                        return new(stack, VMErrorType.RunnerInternal, $"invalid conversion type bool->number !? {cmp1}, {cmp2}");
                                }
                            }
                            if (swap)
                                (cmp1, cmp2) = (cmp2, cmp1);
                        }
                        // ensure that instances are their ids instead
                        {
                            if (cmp1 is Instance inst1)
                                cmp1 = inst1.Id;
                            if (cmp2 is Instance inst2)
                                cmp2 = inst2.Id;
                        }
                        switch (inst.ComparisonKind)
                        {
                            case UndertaleInstruction.ComparisonType.LT:
                                err = stack.PokeStack(cmp1 < cmp2);
                                break;
                            case UndertaleInstruction.ComparisonType.LTE:
                                err = stack.PokeStack(cmp1 <= cmp2);
                                break;
                            case UndertaleInstruction.ComparisonType.EQ:
                                err = stack.PokeStack(cmp1 == cmp2);
                                break;
                            case UndertaleInstruction.ComparisonType.NEQ:
                                err = stack.PokeStack(cmp1 != cmp2);
                                break;
                            case UndertaleInstruction.ComparisonType.GTE:
                                err = stack.PokeStack(cmp1 >= cmp2);
                                break;
                            case UndertaleInstruction.ComparisonType.GT:
                                err = stack.PokeStack(cmp1 > cmp2);
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"unknown comparisonkind ComparisonKind.{inst.ComparisonKind}");
                        }
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Pop:
                    {
                        /*if (inst.Type1 != UndertaleInstruction.DataType.Variable)
                            Console.WriteLine("TRIGGER 2");*/
                        VMError? err = null;
                        dynamic? value = null;
                        var fetchStackInst = false;
                        switch (inst.ReferenceType)
                        {
                            case UndertaleInstruction.VariableType.Normal:
                                value = stack.PopStack(out err);
                                if (inst.Type1 == UndertaleInstruction.DataType.Variable && inst.TypeInst == UndertaleInstruction.InstanceType.Stacktop)
                                    fetchStackInst = true;
                                break;
                            case UndertaleInstruction.VariableType.Array:
                                {
                                    var index = stack.PopStack(out err);
                                    if (err != null)
                                        return err;
                                    index = EnsureNativeValue(state, stack, out err, index);
                                    if (err != null)
                                        return err;
                                    var scope = stack.PopStack(out err);
                                    if (err != null)
                                        return err;
                                    scope = (UndertaleInstruction.InstanceType)(int)EnsureNativeValue(state, stack, out err, scope);
                                    if (err != null)
                                        return err;
                                    value = stack.PopStack(out err);
                                    if (err != null)
                                        return err;
                                    value = EnsureNativeValue(state, stack, out err, value);
                                    if (err != null)
                                        return err;
                                    err = GetScopedVari(state, stack, scope, inst.ValueVariable.Name.Content, out dynamic? arr);
                                    if (err != null) // if this is reached the array is not yet defined, so lets define a dummy one
                                    {
                                        //return err;
                                        arr = new List<dynamic?>();
                                        err = SetScopedVari(state, stack, scope, inst.ValueVariable.Name.Content, arr);
                                        if (err != null)
                                            return err;
                                    }
                                    if (arr == null)
                                        return new(stack, VMErrorType.Runtime, $"array is null! {inst.ValueVariable.Name.Content}");
                                    if (arr.GetType().IsArray)
                                    {
                                        if (arr.Length <= (int)index)
                                            return new(stack, VMErrorType.Runtime, $"invalid index {index} with fixed size array {arr} (with length {arr.Length})");
                                        // todo: check array copy-on-write id
                                        if (state.Data.ArrayCopyOnWrite)
                                        {
                                            {
                                                var nArr = new dynamic?[arr.Length];
                                                Array.Copy(arr, nArr, arr.Length);
                                                arr = nArr;
                                            }
                                            err = SetScopedVari(state, stack, scope, inst.ValueVariable.Name.Content, arr);
                                            if (err != null)
                                                return err;
                                        }
                                    }
                                    else
                                    {
                                        var lArr = (List<dynamic?>)arr!;
                                        // todo: check array copy-on-write id
                                        if (state.Data.ArrayCopyOnWrite)
                                        {
                                            lArr = [.. (List<dynamic?>)arr!];
                                            arr = lArr;
                                            err = SetScopedVari(state, stack, scope, inst.ValueVariable.Name.Content, arr);
                                            if (err != null)
                                                return err;
                                        }
                                        while (lArr.Count <= (int)index)
                                            lArr.Add(null);
                                    }
                                    arr![(int)index] = value;
                                    value = arr;
                                }
                                break;
                            case UndertaleInstruction.VariableType.StackTop:
                                value = stack.PopStack(out err);
                                fetchStackInst = true;
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Pop] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                        }
                        if (err != null)
                            return err;
                        // todo: verify if convering the values is ok
                        var sVari = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        if (fetchStackInst)
                        {
                            dynamic? scope = -999;
                            if (IsValueNumber(value) && inst.ReferenceType != UndertaleInstruction.VariableType.Array && inst.Type1 == UndertaleInstruction.DataType.Variable)
                                scope = (int)value;
                            else
                            {
                                scope = stack.PopStack(out err);
                                if (err != null)
                                    return err;
                                scope = EnsureNativeValue(state, stack, out err, scope);
                                if (err != null)
                                    return err;
                            }
                            switch (scope)
                            {
                                case int or long or short or uint:
                                    {
                                        var iScope = (int)scope;
                                        if (iScope != -999 && iScope <= -1)
                                        {
                                            dynamic? sVal = null;
                                            dynamic? ins = null;
                                            if (inst.Type1 == UndertaleInstruction.DataType.Variable)
                                            {
                                                sVal = stack.PopStack(out err);
                                                if (err != null)
                                                    return err;
                                                // todo: ensure that it's a native value/VariableReference?
                                                sVal = EnsureNativeValue(state, stack, out err, sVal);
                                                if (err != null)
                                                    return err;
                                                if (sVal is Instance)
                                                {
                                                    ins = sVal;
                                                    sVal = stack.PopStack(out err);
                                                    if (err != null)
                                                        return err;
                                                    // todo: ensure that it's a native value/VariableReference?
                                                    sVal = EnsureNativeValue(state, stack, out err, sVal);
                                                    if (err != null)
                                                        return err;
                                                }
                                            }
                                            else
                                            {
                                                sVal = value;
                                                // todo: ensure that it's a native value/VariableReference?
                                                sVal = EnsureNativeValue(state, stack, out err, sVal);
                                                if (err != null)
                                                    return err;
                                                ins = stack.PopStack(out err);
                                                if (err != null)
                                                    return err;
                                                ins = EnsureNativeValue(state, stack, out err, ins);
                                                if (err != null)
                                                    return err;
                                            }
                                            var ind = -1;
                                            if (ins != null)
                                            {
                                                ind = stack.InstancesScope.Count;
                                                stack.InstancesScope.Add(ins);
                                            }
                                            //err = SetScopedVari(state, stack, inst.TypeInst, inst.ValueVariable.Name.Content, sVal);
                                            err = SetScopedVari(state, stack, iScope == -1 && stack.InstancesScope.Count == 0 ? UndertaleInstruction.InstanceType.Undefined : (UndertaleInstruction.InstanceType)iScope, inst.ValueVariable.Name.Content, sVal);
                                            if (err != null)
                                                return err;
                                            if (ind != -1)
                                                stack.InstancesScope.RemoveAt(ind);
                                            /*else
                                                return new(stack, VMErrorType.RunnerInternal, $"{sVal} is not vari ref!?");*/
                                        }
                                        /*else if (state.Instances.TryGetValue((uint)scope, out var fIns))
                                        {
                                            stack.InstancesScope.Add(fIns);
                                            err = SetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                            stack.InstancesScope.Remove(fIns);
                                            if (err != null)
                                                return err;
                                        }*/
                                        else
                                            return new(stack, VMErrorType.RunnerInternal, $"unknown scope {scope}!?");
                                    }
                                    break;
                                /*case Instance ins: // praying this shit is correct, seems only to be used in callv.v?
                                    vRef.Value = ins;
                                    //stack.InstancesScope.Add(ins);
                                    //vRef.HasValue = true;
                                    //err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                    //stack.InstancesScope.Remove(ins);
                                    //if (err != null)
                                        //return err;
                                    break;*/
                                default:
                                    return new(stack, VMErrorType.RunnerInternal, $"unknown scope {scope}!?");
                            }
                        }
                        else if (inst.Type1 != UndertaleInstruction.DataType.Variable && value is VariableReference vRef)
                            err = SetScopedVari(state, stack, vRef.Scope, inst.ValueVariable.Name.Content, sVari);
                        else
                            err = SetScopedVari(state, stack, inst.TypeInst, inst.ValueVariable.Name.Content, sVari);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Dup:
                    {
                        // todo: figure out a way to make this faster
                        var typeSize = DataTypeToSize(stack, inst.Type1, out var err);
                        if (err != null)
                            return err;
                        if ((int)inst.ComparisonKind == 0)
                        {
                            var dupStack = new List<dynamic?>();
                            var size = typeSize * (inst.Extra + 1);
                            while (size > 0)
                            {
                                var elem = stack.PopStack(out err);
                                if (err != null)
                                    return err;
                                dupStack.Add(elem);
                                var dtype = VariToDataType(stack, elem, out err);
                                if (err != null)
                                    return err;
                                size -= DataTypeToSize(stack, dtype, out err);
                                if (err != null)
                                    return err;
                            }
                            for (var i = 0; i < 2; i++)
                            {
                                for (var ii = dupStack.Count - 1; ii >= 0; ii--)
                                    stack.PushStack(dupStack[ii]);
                            }
                            /*for (var i = 0; i <= inst.Extra; i++)
                            {
                                var value = stack.PeekStack(i * -2, out var err);
                                if (err != null)
                                    return err;
                                stack.PushStack(value);
                            }*/
                        }
                        else
                        {
                            var topStack = new List<dynamic?>();
                            var bottomStack = new List<dynamic?>();
                            {
                                var size = typeSize * inst.Extra;
                                while (size > 0)
                                {
                                    var elem = stack.PopStack(out err);
                                    if (err != null)
                                        return err;
                                    topStack.Add(elem);
                                    var dtype = VariToDataType(stack, elem, out err);
                                    if (err != null)
                                        return err;
                                    size -= DataTypeToSize(stack, dtype, out err);
                                    if (err != null)
                                        return err;
                                }
                            }
                            {
                                var size = typeSize * (((byte)inst.ComparisonKind & ~0x80) >> 3);
                                while (size > 0)
                                {
                                    var elem = stack.PopStack(out err);
                                    if (err != null)
                                        return err;
                                    bottomStack.Add(elem);
                                    var dtype = VariToDataType(stack, elem, out err);
                                    if (err != null)
                                        return err;
                                    size -= DataTypeToSize(stack, dtype, out err);
                                    if (err != null)
                                        return err;
                                }
                            }
                            for (var i = topStack.Count - 1; i >= 0; i--)
                                stack.PushStack(topStack[i]);
                            for (var i = bottomStack.Count - 1; i >= 0; i--)
                                stack.PushStack(bottomStack[i]);
                        }
                    }
                    break;
                case UndertaleInstruction.Opcode.Ret:
                    {
                        var value = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        returnValue = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        instIndex = code.Instructions.Length;
                        continue;
                    }
                case UndertaleInstruction.Opcode.Exit:
                    instIndex = code.Instructions.Length;
                    continue;
                case UndertaleInstruction.Opcode.Popz:
                    {
                        stack.PopStack(out var err);
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.B:
                    skipAddrSize = inst.JumpOffset;
                    if (skipAddrSize < 0)
                        skipAddrSize -= inst.JumpOffset % 2;
                    continue;
                case UndertaleInstruction.Opcode.Bt:
                    {
                        if (stack.Scope.Count <= 0)
                            return new(stack, VMErrorType.Runner, "[Opcode.Bf] expected the scope stack to contain at least 1 element but it's empty");
                        var value = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nativValue = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        if (IsValueTruthy(stack, nativValue, out err))
                        {
                            skipAddrSize = inst.JumpOffset;
                            if (skipAddrSize < 0)
                                skipAddrSize -= inst.JumpOffset % 2;
                            continue;
                        }
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Bf:
                    {
                        if (stack.Scope.Count <= 0)
                            return new(stack, VMErrorType.Runner, "[Opcode.Bf] expected the scope stack to contain at least 1 element but it's empty");
                        var value = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var nativeValue = EnsureNativeValue(state, stack, out err, value);
                        if (err != null)
                            return err;
                        if (!IsValueTruthy(stack, nativeValue, out err))
                        {
                            skipAddrSize = inst.JumpOffset;
                            if (skipAddrSize < 0)
                                skipAddrSize -= inst.JumpOffset % 2;
                            continue;
                        }
                        if (err != null)
                            return err;
                    }
                    break;
                case UndertaleInstruction.Opcode.Push:
                    {
                        dynamic? value;
                        switch (inst.Type1)
                        {
                            case UndertaleInstruction.DataType.Double:
                                value = inst.ValueDouble;
                                break;
                            case UndertaleInstruction.DataType.Float:
                                value = (float)inst.ValueDouble;
                                break;
                            case UndertaleInstruction.DataType.Int32:
                                value = inst.ValueFunction != null ? inst.ValueFunction : inst.ValueInt;
                                break;
                            case UndertaleInstruction.DataType.Int64:
                                value = inst.ValueLong;
                                break;
                            case UndertaleInstruction.DataType.UnsignedInt:
                                value = (uint)inst.ValueLong;
                                break;
                            case UndertaleInstruction.DataType.Int16:
                                value = inst.ValueShort;
                                break;
                            case UndertaleInstruction.DataType.String:
                                value = inst.ValueString.Resource.Content;
                                break;
                            case UndertaleInstruction.DataType.Variable:
                                {
                                    var vari = inst.ValueVariable;
                                    var vRef = new VariableReference()
                                    {
                                        VariableName = vari.Name.Content,
                                        Scope = vari.InstanceType,
                                        ReferenceType = inst.ReferenceType,
                                    };
                                    // todo: figure out a better way to do this
                                    if (inst.Type2 != UndertaleInstruction.DataType.Variable && vari.VarID >= 0 && (int)inst.TypeInst >= 0 && state.Data.GameObjects.Count > (int)inst.TypeInst)
                                    {
                                        var ins = state.Instances.FirstOrDefault(ins => ins.Value.Object == state.Data.GameObjects[(int)inst.TypeInst]).Value;
                                        if (ins != null)
                                        {
                                            var ind = stack.InstancesScope.Count;
                                            stack.InstancesScope.Add(ins);
                                            var err = GetScopedVari(state, stack, UndertaleInstruction.InstanceType.Self, vRef.VariableName, out var val);
                                            stack.InstancesScope.RemoveAt(ind);
                                            if (err == null)
                                            {
                                                vRef.Value = EnsureNativeValue(state, stack, out err, val);
                                                if (err != null)
                                                    return err;
                                                vRef.HasValue = true;
                                            }
                                        }
                                    }
                                    var fetchStackInst = false;
                                    switch (inst.ReferenceType)
                                    {
                                        case UndertaleInstruction.VariableType.Array:
                                            {
                                                var sVal = stack.PopStack(out var err);
                                                if (err != null)
                                                    return err;
                                                vRef.Value = EnsureNativeValue(state, stack, out err, sVal);
                                                if (err != null)
                                                    return err;
                                                sVal = stack.PopStack(out err);
                                                if (err != null)
                                                    return err;
                                                vRef.Scope = (UndertaleInstruction.InstanceType)(int)EnsureNativeValue(state, stack, out err, sVal);
                                            }
                                            break;
                                        case UndertaleInstruction.VariableType.StackTop:
                                            fetchStackInst = true;
                                            break;
                                        case UndertaleInstruction.VariableType.Normal:
                                            if (inst.TypeInst == UndertaleInstruction.InstanceType.Stacktop)
                                                fetchStackInst = true;
                                            break;
                                        default:
                                            return new(stack, VMErrorType.RunnerInternal, $"unknown reftype {inst.ReferenceType}!?");
                                    }
                                    if (fetchStackInst)
                                    {
                                        var scope = stack.PopStack(out var err);
                                        if (err != null)
                                            return err;
                                        scope = EnsureNativeValue(state, stack, out err, scope);
                                        if (err != null)
                                            return err;
                                        switch (scope)
                                        {
                                            case int or long or short or uint:
                                                {
                                                    if (((int)scope) == -9)
                                                    {
                                                        var sVal = stack.PopStack(out err);
                                                        if (err != null)
                                                            return err;
                                                        sVal = EnsureNativeValue(state, stack, out err, sVal);
                                                        if (err != null)
                                                            return err;
                                                        {
                                                            Instance? fIns = null;
                                                            if (sVal is not Instance && IsValueNumber(sVal) && state.Instances.TryGetValue((uint)sVal, out fIns))
                                                                sVal = fIns;
                                                        }
                                                        if (sVal is Instance || sVal is FunctionConstructor)
                                                        {
                                                            var ind = stack.InstancesScope.Count;
                                                            stack.InstancesScope.Add(sVal);
                                                            vRef.HasValue = true;
                                                            err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                                            stack.InstancesScope.RemoveAt(ind);
                                                            if (err != null)
                                                                return err;
                                                        }
                                                        else
                                                            return new(stack, VMErrorType.RunnerInternal, $"{sVal} is not instance!?");
                                                    }
                                                    else if (state.Instances.TryGetValue((uint)scope, out var fIns))
                                                    {
                                                        var ind = stack.InstancesScope.Count;
                                                        stack.InstancesScope.Add(fIns);
                                                        vRef.HasValue = true;
                                                        err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                                        stack.InstancesScope.RemoveAt(ind);
                                                        if (err != null)
                                                            return err;
                                                    }
                                                    else
                                                        return new(stack, VMErrorType.RunnerInternal, $"unknown scope {scope}!?");
                                                }
                                                break;
                                            case Instance ins: // praying this shit is correct, seems only to be used in callv.v?
                                                vRef.Value = ins;
                                                vRef.HasValue = true;
                                                /*stack.InstancesScope.Add(ins);
                                                vRef.HasValue = true;
                                                err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                                stack.InstancesScope.Remove(ins);
                                                if (err != null)
                                                    return err;*/
                                                break;
                                            case FunctionConstructor cons:
                                                vRef.Value = cons; // same as last
                                                vRef.HasValue = true;
                                                break;
                                            default:
                                                return new(stack, VMErrorType.RunnerInternal, $"unknown scope {scope}!?");
                                        }
                                    }
                                    value = vRef;
                                }
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Push] datatype not supported: DataType.{inst.Type1}");
                        }
                        switch (inst.ReferenceType)
                        {
                            case UndertaleInstruction.VariableType.Normal or UndertaleInstruction.VariableType.Array or UndertaleInstruction.VariableType.StackTop:
                                stack.PushStack(value);
                                break;
                            /*case UndertaleInstruction.VariableType.StackTop:
                                stack.PushTopStack(value);
                                break;*/
                            default:
                                //return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Push] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                                stack.PushStack(value);
                                break;
                        }
                    }
                    break;
                case UndertaleInstruction.Opcode.PushEnv:
                    {
                        dynamic? value;
                        switch (inst.Type1)
                        {
                            case UndertaleInstruction.DataType.Double or UndertaleInstruction.DataType.Float:
                                {
                                    value = (int)inst.ValueDouble;
                                    if (state.Instances.TryGetValue((uint)value, out var fIns))
                                        value = fIns;
                                    else
                                        value = state.Instances.FirstOrDefault(ins => ins.Value.ObjectIndex == (uint)value).Value;
                                }
                                break;
                            case UndertaleInstruction.DataType.Int32 or UndertaleInstruction.DataType.Int64 or UndertaleInstruction.DataType.UnsignedInt or UndertaleInstruction.DataType.Int16:
                                {
                                    value = inst.ValueInt;
                                    if (state.Instances.TryGetValue((uint)value, out var fIns))
                                        value = fIns;
                                    else
                                        value = state.Instances.FirstOrDefault(ins => ins.Value.ObjectIndex == (uint)value).Value;
                                }
                                break;
                            case UndertaleInstruction.DataType.String:
                                return new(stack, VMErrorType.Runner, "[Opcode.Pushenv] unexpected pushenv type string");
                            case UndertaleInstruction.DataType.Variable:
                                {
                                    var vari = inst.ValueVariable;
                                    var vRef = new VariableReference()
                                    {
                                        VariableName = vari.Name.Content,
                                        Scope = vari.InstanceType,
                                        ReferenceType = inst.ReferenceType,
                                    };
                                    var fetchStackInst = false;
                                    switch (inst.ReferenceType)
                                    {
                                        case UndertaleInstruction.VariableType.Array:
                                            {
                                                var sVal = stack.PopStack(out var err);
                                                if (err != null)
                                                    return err;
                                                vRef.Value = EnsureNativeValue(state, stack, out err, sVal);
                                                if (err != null)
                                                    return err;
                                                sVal = stack.PopStack(out err);
                                                if (err != null)
                                                    return err;
                                                vRef.Scope = (UndertaleInstruction.InstanceType)(int)EnsureNativeValue(state, stack, out err, sVal);
                                            }
                                            break;
                                        case UndertaleInstruction.VariableType.StackTop:
                                            fetchStackInst = true;
                                            break;
                                        case UndertaleInstruction.VariableType.Normal:
                                            if (inst.Type1 == UndertaleInstruction.DataType.Variable && inst.TypeInst == UndertaleInstruction.InstanceType.Stacktop)
                                                fetchStackInst = true;
                                            break;
                                    }
                                    if (fetchStackInst)
                                    {
                                        var scope = stack.PopStack(out var err);
                                        if (err != null)
                                            return err;
                                        scope = EnsureNativeValue(state, stack, out err, scope);
                                        if (err != null)
                                            return err;
                                        switch (scope)
                                        {
                                            case int or long or short or uint:
                                                {
                                                    if (((int)scope) == -9)
                                                    {
                                                        var sVal = stack.PopStack(out err);
                                                        if (err != null)
                                                            return err;
                                                        sVal = EnsureNativeValue(state, stack, out err, sVal);
                                                        if (err != null)
                                                            return err;
                                                        {
                                                            Instance? fIns = null;
                                                            if (sVal is not Instance && IsValueNumber(sVal) && state.Instances.TryGetValue((uint)sVal, out fIns))
                                                                sVal = fIns;
                                                        }
                                                        if (sVal is Instance || sVal is FunctionConstructor)
                                                        {
                                                            stack.InstancesScope.Add(sVal);
                                                            vRef.HasValue = true;
                                                            err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                                            stack.InstancesScope.Remove(sVal);
                                                            if (err != null)
                                                                return err;
                                                        }
                                                        else
                                                            return new(stack, VMErrorType.RunnerInternal, $"{sVal} is not instance!?");
                                                    }
                                                    else if (state.Instances.TryGetValue((uint)scope, out var fIns))
                                                    {
                                                        stack.InstancesScope.Add(fIns);
                                                        vRef.HasValue = true;
                                                        err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                                        stack.InstancesScope.Remove(fIns);
                                                        if (err != null)
                                                            return err;
                                                    }
                                                    else
                                                        return new(stack, VMErrorType.RunnerInternal, $"unknown scope {scope}!?");
                                                }
                                                break;
                                            case Instance ins:
                                                stack.InstancesScope.Add(ins);
                                                vRef.HasValue = true;
                                                err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName, out vRef.Value);
                                                stack.InstancesScope.Remove(ins);
                                                if (err != null)
                                                    return err;
                                                break;
                                            default:
                                                return new(stack, VMErrorType.RunnerInternal, $"unknown scope {scope}!?");
                                        }
                                    }
                                    value = vRef;
                                }
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Push] datatype not supported: DataType.{inst.Type1}");
                        }
                        switch (inst.ReferenceType)
                        {
                            case UndertaleInstruction.VariableType.Normal or UndertaleInstruction.VariableType.Array or UndertaleInstruction.VariableType.StackTop:
                                stack.InstancesScope.Add(value);
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Push] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                        }
                        if (value == null)
                        {
                            skipAddrSize = inst.JumpOffset;
                            if (skipAddrSize < 0)
                                skipAddrSize -= inst.JumpOffset % 4;
                            continue;
                        }
                    }
                    break;
                case UndertaleInstruction.Opcode.PopEnv:
                    if (stack.InstancesScope.Count <= 0)
                        return new(stack, VMErrorType.Runner, "tried to popenv but instancesscope is empty");
                    stack.InstancesScope.RemoveAt(stack.InstancesScope.Count - 1);
                    break;
                case UndertaleInstruction.Opcode.PushLoc:
                    {
                        if (!stack.LocalVariables.TryGetValue(inst.ValueVariable.Name.Content, out var value))
                            return new(stack, VMErrorType.Runtime, $"[Opcode.PushLoc] variable {inst.ValueVariable.Name.Content} doesn't exist");
                        var vRef = value is VariableReference ? value : new VariableReference()
                        {
                            HasValue = true,
                            Value = value,
                            VariableName = inst.ValueVariable.Name.Content,
                            Scope = UndertaleInstruction.InstanceType.Local,
                        };
                        switch (inst.ReferenceType)
                        {
                            case UndertaleInstruction.VariableType.Normal or UndertaleInstruction.VariableType.Array:
                                stack.PushStack(vRef);
                                break;
                            case UndertaleInstruction.VariableType.StackTop:
                                //stack.PushTopStack(vRef);
                                Console.WriteLine("A");
                                break;
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.PushLoc] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                        }
                    }
                    break;
                case UndertaleInstruction.Opcode.PushGlb:
                    {
                        if (!state.GlobalVariables.TryGetValue(inst.ValueVariable.Name.Content, out var value))
                            return new(stack, VMErrorType.Runtime, $"[Opcode.PushGlb] variable {inst.ValueVariable.Name.Content} doesn't exist");
                        var vRef = value is VariableReference ? value : new VariableReference()
                        {
                            HasValue = true,
                            Value = value,
                            VariableName = inst.ValueVariable.Name.Content,
                            Scope = UndertaleInstruction.InstanceType.Global,
                        };
                        switch (inst.ReferenceType)
                        {
                            case UndertaleInstruction.VariableType.Normal or UndertaleInstruction.VariableType.Array or UndertaleInstruction.VariableType.StackTop or (UndertaleInstruction.VariableType)72:
                                stack.PushStack(value);
                                break;
                            /*case UndertaleInstruction.VariableType.StackTop:
                                stack.PushTopStack(value);
                                break;*/
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.PushGlb] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                        }
                    }
                    break;
                case UndertaleInstruction.Opcode.PushBltn:
                    {
                        if (!stack.LocalBuiltin.TryGetValue(inst.ValueVariable.Name.Content, out var value))
                            if (!state.BuiltinVariables.TryGetValue(inst.ValueVariable.Name.Content, out value))
                                return new(stack, VMErrorType.Runtime, $"[Opcode.PushBltn] variable {inst.ValueVariable.Name.Content} doesn't exist");
                        var vRef = value is VariableReference ? value : new VariableReference()
                        {
                            HasValue = true,
                            Value = value,
                            VariableName = inst.ValueVariable.Name.Content,
                            Scope = UndertaleInstruction.InstanceType.Builtin,
                        };
                        switch (inst.ReferenceType)
                        {
                            case UndertaleInstruction.VariableType.Normal or UndertaleInstruction.VariableType.Array or UndertaleInstruction.VariableType.StackTop or (UndertaleInstruction.VariableType)72:
                                stack.PushStack(value);
                                break;
                            /*case UndertaleInstruction.VariableType.StackTop:
                                stack.PushTopStack(value);
                                break;*/
                            default:
                                return new(stack, VMErrorType.RunnerInternal, $"[Opcode.PushBltn] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                        }
                    }
                    break;
                case UndertaleInstruction.Opcode.PushI:
                    /*if ((inst.Type1 != UndertaleInstruction.DataType.Int32 && inst.Type1 != UndertaleInstruction.DataType.Int16) || inst.Type2 != UndertaleInstruction.DataType.Double)
                        Console.WriteLine("TRIGGER 1");*/
                    switch (inst.ReferenceType)
                    {
                        case UndertaleInstruction.VariableType.Normal or UndertaleInstruction.VariableType.Array or UndertaleInstruction.VariableType.StackTop or (UndertaleInstruction.VariableType)72:
                            stack.PushStack(inst.ValueShort);
                            break;
                        /*case UndertaleInstruction.VariableType.StackTop:
                            stack.PushTopStack(inst.ValueShort);
                            break;*/
                        default:
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Push] ReferenceType not supported: VariableType.{inst.ReferenceType}");
                    }
                    break;
                case UndertaleInstruction.Opcode.Call:
                    {
                        if (!state.Functions.TryGetValue(inst.ValueFunction.Name.Content, out var func))
                            return new(stack, VMErrorType.Runtime, $"invalid function with name {inst.ValueFunction.Name.Content}");
                        if (stack.Scope.Count <= 0)
                            return new(stack, VMErrorType.Runner, "[Opcode.Call] expected the scope stack to contain at least 1 element but it's empty");
#if !DEBUG
                        try
                        {
#endif
                        var (err, value) = func(state, stack, inst, null);
                        if (err != null)
                            return err;
                        stack.PushStack(value is VariableReference ? value : new VariableReference()
                        {
                            HasValue = true,
                            Value = value,
                        });
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            return new(stack, VMErrorType.RunnerInternal, ex.ToString())
                            {
                                InternalException = ex,
                            };
                        }
#endif
                    }
                    break;
                case UndertaleInstruction.Opcode.CallV:
                    {
                        if (stack.Stack.Count <= 1)
                            return new(stack, VMErrorType.Runner, "[Opcode.CallV] expected the stack to contain at least 2 elements");
                        var sTop = stack.PopStack(out var err);
                        if (err != null)
                            return err;
                        var vari = (VariableReference?)stack.PopStack(out err);
                        if (err != null)
                            return err;
                        if (vari == null)
                            return new(stack, VMErrorType.RunnerInternal, "expected vari to be VariableReference");
                        /*if (vari.HasValue)
                            return new(stack, VMErrorType.RunnerInternal, "expected to have a value reference not the value itself");*/
                        dynamic? func = null;
                        Instance? ins = null;
                        var containsFunc = false;
                        var foundFunc = false;
                        if (sTop is VariableReference vRef)
                        {
                            containsFunc = vRef.HasValue;
                            if (vRef.Value is Instance)
                            {
                                ins = vRef.Value;
                                var ind = stack.InstancesScope.Count;
                                stack.InstancesScope.Add(ins);
                                err = GetScopedVari(state, stack, vRef.Scope, vRef.VariableName!, out func);
                                if (err != null)
                                    return err;
                                func = EnsureNativeValue(state, stack, out err, func);
                                stack.InstancesScope.RemoveAt(ind);
                                if (err != null)
                                    return err;
                                foundFunc = true;
                            }
                        }
                        if (!foundFunc)
                        {
                            dynamic? vIns = null;
                            {
                                var nVari = vari.HasValue ? vari.Value : EnsureNativeValue(state, stack, out err, vari);
                                if (err != null)
                                    return err;
                                {
                                    Instance? fIns = null;
                                    if (nVari is not Instance && IsValueNumber(nVari) && state.Instances.TryGetValue((uint)nVari, out fIns))
                                        nVari = fIns;
                                }
                                vIns = nVari is Instance ? nVari : EnsureNativeValue(state, stack, out err, sTop);
                                if (err != null)
                                    return err;
                                {
                                    if (vIns is not Instance && IsValueNumber(ins) && state.Instances.TryGetValue((uint)vIns, out Instance? fIns))
                                        vIns = fIns;
                                }
                                {
                                    if (vIns is Instance nIns)
                                        ins = nIns;
                                }
                            }
                            if (!containsFunc)
                            {
                                var ind = -1;
                                {
                                    if (vIns is Instance nIns)
                                    {
                                        ind = stack.InstancesScope.Count;
                                        stack.InstancesScope.Add(nIns);
                                    }
                                }
                                if (vari.Value is Instance)
                                    func = EnsureNativeValue(state, stack, out err, sTop);
                                else
                                    err = GetScopedVari(state, stack, vari.Scope, vari.VariableName!, out func);
                                if (err != null)
                                    return err;
                                func = EnsureNativeValue(state, stack, out err, func);
                                if (ind != -1)
                                    stack.InstancesScope.RemoveAt(ind);
                                if (err != null)
                                    return err;
                            }
                            else
                            {
                                var nrRef = (VariableReference)sTop!;
                                if (nrRef.Value is FunctionConstructor fCons)
                                {
                                    var ind = stack.InstancesScope.Count;
                                    stack.InstancesScope.Add(fCons);
                                    err = GetScopedVari(state, stack, nrRef.Scope, nrRef.VariableName!, out func);
                                    stack.InstancesScope.RemoveAt(ind);
                                    if (err != null)
                                        return err;
                                }
                                else
                                    func = nrRef.Value;
                            }
                        }
#if !DEBUG
                        try
                        {
#endif
                        dynamic? value;
                        if (func is RuntimeFunction rFunc)
                            (err, value) = rFunc.Function(state, stack, inst, ins);
                        else if (func is UndertaleFunction uFunc)
                        {
                            var ind = -1;
                            if (ins != null)
                            {
                                ind = stack.InstancesScope.Count;
                                stack.InstancesScope.Add(ins);
                            }
                            err = state.Run(uFunc, out value, stack);
                            if (ind != -1)
                                stack.InstancesScope.RemoveAt(ind);
                        }
                        else
                            return new(stack, VMErrorType.RunnerInternal, "expected function to be UndertaleFunction or RuntimeFunction but it's not??");
                        if (err != null)
                            return err;
                        stack.PushStack(value is VariableReference ? value : new VariableReference()
                        {
                            HasValue = true,
                            Value = value,
                        });
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            return new(stack, VMErrorType.RunnerInternal, ex.ToString())
                            {
                                InternalException = ex,
                            };
                        }
#endif
                    }
                    break;
                case UndertaleInstruction.Opcode.Break:
                    switch (inst.ExtendedKind)
                    {
                        /*case -1: // chkindex
                            break;
                        case -2: // pushaf
                            break;
                        case -3: // popaf
                            break;
                        case -4: // pushac
                            break;*/
                        case -5: // setowner
                            {
                                var value = stack.PopStack(out var err);
                                if (err != null)
                                    return err;
                                state.ArrayOwnerId = value;
                            }
                            break;
                        case -6: // isstaticok
                            stack.PushStack(stack.IsStaticOk);
                            break;
                        case -7: // setstatic
                            stack.IsStaticOk = true;
                            break;
                        /*case -8: // savearef
                            break;
                        case -9: // restorearef
                            break;
                        case -10: // chknullish
                            break;
                        case -11: // pushref
                            break;*/
                        default:
                            return new(stack, VMErrorType.RunnerInternal, $"[Opcode.Break] unknown extended kind {inst.ExtendedKind}");
                    }
                    break;
                default:
                    return new(stack, VMErrorType.RunnerInternal, $"unknown opcode Opcode.{inst.Kind}");
            }
            /*if (stack.InstancesScope.Count > 0 && inst.ToString().Contains("self.y") && stack.InstancesScope[^1].Variables["y"] is Instance)
                Console.WriteLine("A");*/
            instIndex++;
        }
        if (stack.IsStaticOk)
            stack.IsStaticOk = false;
        stack.Stacktrace.Remove(backtrace);
        if (ShowOutput)
            Console.WriteLine($"Exit code: {code.Name} --> {(stack.Stacktrace.Count > 0 ? $"{stack.Stacktrace[^1].Name}:{stack.Stacktrace[^1].InstructionIndex}" : "<null>")}");
        return null;
    }
}
