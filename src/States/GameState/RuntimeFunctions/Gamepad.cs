using BytecodeVM;
using Raylib_cs;
using UndertaleModLib.Models;

namespace States.GameState;

public static class Gamepad
{
    public static GamepadButton? GameMakerConstToGamepadButton(int num)
    {
        return num switch
        {
            32769 => GamepadButton.RightFaceDown, // gp_face1
            32770 => GamepadButton.RightFaceRight, // gp_face2
            32771 => GamepadButton.RightFaceLeft, // gp_face3
            32772 => GamepadButton.RightFaceUp, // gp_face4
            32773 => GamepadButton.LeftTrigger1, // gp_shoulderl
            32774 => GamepadButton.LeftTrigger2, // gp_shoulderlb
            32775 => GamepadButton.RightTrigger1, // gp_shoulderr
            32776 => GamepadButton.RightTrigger2, // gp_shoulderrb
            32777 => GamepadButton.MiddleLeft, // gp_select
            32778 => GamepadButton.MiddleRight, // gp_start
            32779 => GamepadButton.LeftThumb, // gp_stickl
            32780 => GamepadButton.RightThumb, // gp_stickr
            32781 => GamepadButton.LeftFaceUp, // gp_padu
            32782 => GamepadButton.LeftFaceDown, // gp_padd
            32783 => GamepadButton.LeftFaceLeft, // gp_padl
            32784 => GamepadButton.LeftFaceRight, // gp_padr
            32785 => null, // gp_axislh
            32786 => null, // gp_axislv
            32787 => null, // gp_axisrh
            32788 => null, // gp_axisrv
            32789 => null, // gp_axis_acceleration_x
            32790 => null, // gp_axis_acceleration_y
            32791 => null, // gp_axis_acceleration_z
            32792 => null, // gp_axis_angular_velocity_x
            32793 => null, // gp_axis_angular_velocity_y
            32794 => null, // gp_axis_angular_velocity_z
            32795 => null, // gp_axis_orientation_x
            32796 => null, // gp_axis_orientation_y
            32797 => null, // gp_axis_orientation_z
            32798 => null, // gp_axis_orientation_w
            32799 => GamepadButton.Middle, // gp_home
            32800 => null, // gp_extra1
            32801 => null, // gp_extra2
            32802 => null, // gp_extra3
            32803 => null, // gp_extra4
            32804 => null, // gp_paddler
            32805 => null, // gp_paddlel
            32806 => null, // gp_paddlerb
            32807 => null, // gp_paddlelb
            32808 => null, // gp_touchpadbutton
            32809 => null, // gp_extra5
            32810 => null, // gp_extra6
            _ => GamepadButton.Unknown,
        };
    }

    public static GamepadAxis? GameMakerConstToGamepadAxis(int num)
    {
        return num switch
        {
            32785 => GamepadAxis.LeftX, // gp_axislh
            32786 => GamepadAxis.LeftY, // gp_axislv
            32787 => GamepadAxis.RightX, // gp_axisrh
            32788 => GamepadAxis.RightY, // gp_axisrv
            _ => null,
        };
    }

    // todo: do this
    // gm docs: https://manual.gamemaker.io/monthly/en/#t=GameMaker_Language%2FGML_Reference%2FGame_Input%2FGamePad_Input%2FGamepad_Input.htm
    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) gamepad_get_device_count(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 0)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        //return (null, 0);
        var count = 0;
        while (Raylib.IsGamepadAvailable(count))
            count++;
        return (null, count);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) gamepad_button_check(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        var button = GameMakerConstToGamepadButton((int)args[0]);
        if (button != null)
            return (null, (bool)Raylib.IsGamepadButtonDown((int)args[1], (GamepadButton)button));
        return (null, false);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) gamepad_button_check_pressed(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        var button = GameMakerConstToGamepadButton((int)args[0]);
        if (button != null)
            return (null, (bool)Raylib.IsGamepadButtonPressed((int)args[1], (GamepadButton)button));
        return (null, false);
    }

    [GameState.DefineFunciton]
    public static (VMError?, dynamic?) gamepad_button_check_released(GameState state, FunctionStack stack, UndertaleInstruction inst, Instance? ins)
    {
        var args = state.HandleFunctionArguments(stack, inst, out var err);
        if (err != null)
            return (err, null);
        if (args == null)
            return (new(stack, VMErrorType.Runner, "arguments are null"), null);
        if (args.Length != 2)
            return (new VMError(stack, VMErrorType.Runtime, "unexpected argument count"), null);
        if (args[0] == null || args[1] == null)
            return (new VMError(stack, VMErrorType.Runtime, "wrong argument types"), null);
        var button = GameMakerConstToGamepadButton((int)args[0]);
        if (button != null)
            return (null, (bool)Raylib.IsGamepadButtonReleased((int)args[1], (GamepadButton)button));
        return (null, false);
    }
}
