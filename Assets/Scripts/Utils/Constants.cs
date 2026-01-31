using UnityEngine;

// 滤镜颜色枚举
public enum FilterColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple
}

// 技能类型枚举
public enum SkillType
{
    FilterSystem,  // 全局滤镜系统
    MaskSystem     // 跟随蒙版系统
}

// 游戏常量
public static class GameConstants
{
    // 标签定义
    public const string TAG_RED_OBJECT = "Red";
    public const string TAG_BLUE_OBJECT = "Blue";
    public const string TAG_GREEN_OBJECT = "Green";
    public const string TAG_YELLOW_OBJECT = "Yellow";
    public const string TAG_PURPLE_OBJECT = "Purple";
    public const string TAG_BLACK_OBJECT = "Black";
    public const string TAG_HIDDEN_OBJECT = "HiddenObject";
    public const string TAG_DEATH_ZONE = "DeathZone";
    public const string TAG_TRAP = "Trap";
    public const string TAG_RESPAWN_POINT = "RespawnPoint";

    // 层级名称
    public const string LAYER_BACKGROUND = "Background";
    public const string LAYER_FILTER = "Filter";
    public const string LAYER_INTERACTION = "Interaction";
    public const string LAYER_MASK = "Mask";

    // 颜色定义
    public static readonly Color RED_COLOR = new Color(1f, 0.27f, 0.27f, 1f);      // #FF4444
    public static readonly Color BLUE_COLOR = new Color(0.27f, 0.27f, 1f, 1f);     // #4444FF
    public static readonly Color GREEN_COLOR = new Color(0.27f, 1f, 0.27f, 1f);    // #44FF44
    public static readonly Color YELLOW_COLOR = new Color(1f, 1f, 0.27f, 1f);      // #FFFF44
    public static readonly Color PURPLE_COLOR = new Color(1f, 0.27f, 1f, 1f);      // #FF44FF

    // 获取颜色对应的标签
    public static string GetColorTag(FilterColor color)
    {
        return color switch
        {
            FilterColor.Red => TAG_RED_OBJECT,
            FilterColor.Blue => TAG_BLUE_OBJECT,
            FilterColor.Green => TAG_GREEN_OBJECT,
            FilterColor.Yellow => TAG_YELLOW_OBJECT,
            FilterColor.Purple => TAG_PURPLE_OBJECT,
            _ => TAG_BLACK_OBJECT
        };
    }

    // 获取颜色值
    public static Color GetColor(FilterColor color)
    {
        return color switch
        {
            FilterColor.Red => RED_COLOR,
            FilterColor.Blue => BLUE_COLOR,
            FilterColor.Green => GREEN_COLOR,
            FilterColor.Yellow => YELLOW_COLOR,
            FilterColor.Purple => PURPLE_COLOR,
            _ => Color.black
        };
    }
}