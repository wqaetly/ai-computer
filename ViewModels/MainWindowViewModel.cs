namespace AiComputer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// AI 聊天 ViewModel
    /// </summary>
    public AiChatViewModel AiChatViewModel { get; } = new();
}
