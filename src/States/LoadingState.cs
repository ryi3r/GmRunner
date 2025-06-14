namespace States;

public class LoadingState : State
{
    public enum MessageType
    {
        Info,
        Warn,
        ImportantWarn,
        Error,
    }

    public List<(MessageType, string)> Messages = [];
    public int Scroll = 0;
}
