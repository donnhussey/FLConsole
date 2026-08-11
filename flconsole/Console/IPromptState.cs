namespace flconsole;

public interface IPromptState
{
    string CurrentText { get; }
    int CurrentCursorIndex { get; }
}