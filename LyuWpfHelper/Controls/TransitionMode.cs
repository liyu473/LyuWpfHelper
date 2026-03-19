namespace LyuWpfHelper.Controls;

/// <summary>
/// 过渡动画模式
/// </summary>
public enum TransitionMode
{
    /// <summary>从右到左</summary>
    Right2Left,
    /// <summary>从左到右</summary>
    Left2Right,
    /// <summary>从下到上</summary>
    Bottom2Top,
    /// <summary>从上到下</summary>
    Top2Bottom,
    /// <summary>从右到左 + 淡入</summary>
    Right2LeftWithFade,
    /// <summary>从左到右 + 淡入</summary>
    Left2RightWithFade,
    /// <summary>从下到上 + 淡入</summary>
    Bottom2TopWithFade,
    /// <summary>从上到下 + 淡入</summary>
    Top2BottomWithFade,
    /// <summary>仅淡入</summary>
    Fade,
    /// <summary>自定义</summary>
    Custom
}
