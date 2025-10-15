using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Dialogs;

namespace AiComputer.ViewModels;

/// <summary>
/// 重命名会话对话框的 ViewModel
/// </summary>
public partial class RenameSessionDialogViewModel : ObservableObject
{
    private readonly ISukiDialog _dialog;
    private readonly Action<string> _onRename;
    
    /// <summary>
    /// 会话标题
    /// </summary>
    [ObservableProperty]
    private string _sessionTitle;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dialog">对话框实例</param>
    /// <param name="currentTitle">当前标题</param>
    /// <param name="onRename">重命名回调</param>
    public RenameSessionDialogViewModel(ISukiDialog dialog, string currentTitle, Action<string> onRename)
    {
        _dialog = dialog;
        _onRename = onRename;
        SessionTitle = currentTitle;
    }
    
    /// <summary>
    /// 确认重命名命令
    /// </summary>
    [RelayCommand]
    private void ConfirmRename()
    {
        if (!string.IsNullOrWhiteSpace(SessionTitle))
        {
            _onRename(SessionTitle.Trim());
            _dialog.Dismiss();
        }
    }
    
    /// <summary>
    /// 取消命令
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _dialog.Dismiss();
    }
}