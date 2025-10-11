using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;

namespace AiComputer.ViewModels;

/// <summary>
/// 页面基类 - 用于侧边栏导航
/// </summary>
public abstract partial class PageBase : ViewModelBase
{
    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    /// 图标
    /// </summary>
    [ObservableProperty]
    private MaterialIconKind _icon;

    /// <summary>
    /// 索引（用于排序）
    /// </summary>
    [ObservableProperty]
    private int _index;

    protected PageBase(string displayName, MaterialIconKind icon, int index = 0)
    {
        _displayName = displayName;
        _icon = icon;
        _index = index;
    }
}
