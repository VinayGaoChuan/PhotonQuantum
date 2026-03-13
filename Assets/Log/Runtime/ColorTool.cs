using System;
using System.Text;
#if !BATTLE_SERVER
using UnityEngine;
#endif

/// <summary>
/// 颜色枚举（仅含“颜色.pdf”中列出的 138 种颜色）
/// 枚举值从 0 起按文档顺序排列
/// </summary>
public enum ColorEnum
{
    /// <summary> 爱丽丝蓝 </summary>
    AliceBlue = 0,

    /// <summary> 古董白 </summary>
    AntiqueWhite = 1,

    /// <summary> 碧绿 </summary>
    AquaMarine = 2,

    /// <summary> 青白色 </summary>
    Azure = 3,

    /// <summary> 米色 </summary>
    Beige = 4,

    /// <summary> 陶坯黄 </summary>
    Bisque = 5,

    /// <summary> 黑色 </summary>
    Black = 6,

    /// <summary> 杏仁白 </summary>
    BlanchedAlmond = 7,

    /// <summary> 蓝色 </summary>
    Blue = 8,

    /// <summary> 蓝紫色 </summary>
    BlueViolet = 9,

    /// <summary> 褐色 </summary>
    Brown = 10,

    /// <summary> 硬木褐 </summary>
    BurlyWood = 11,

    /// <summary> 军服蓝 </summary>
    CadetBlue = 12,

    /// <summary> 查特酒绿 </summary>
    Chartreuse = 13,

    /// <summary> 巧克力色 </summary>
    Chocolate = 14,

    /// <summary> 珊瑚红 </summary>
    Coral = 15,

    /// <summary> 矢车菊蓝 </summary>
    CornFlowerBlue = 16,

    /// <summary> 玉米穗黄 </summary>
    CornSilk = 17,

    /// <summary> 绯红 </summary>
    Crimson = 18,

    /// <summary> 青色 </summary>
    Cyan = 19,

    /// <summary> 深蓝 </summary>
    DarkBlue = 20,

    /// <summary> 深青 </summary>
    DarkCyan = 21,

    /// <summary> 深金菊黄 </summary>
    DarkGoldenRod = 22,

    /// <summary> 暗灰 </summary>
    DarkGray = 23,

    /// <summary> 深绿 </summary>
    DarkGreen = 24,

    /// <summary> 深卡其色 </summary>
    DarkKhaki = 25,

    /// <summary> 深品红 </summary>
    DarkMagenta = 26,

    /// <summary> 深橄榄绿 </summary>
    DarkOliveGreen = 27,

    /// <summary> 深橙 </summary>
    DarkOrange = 28,

    /// <summary> 深洋兰紫 </summary>
    DarkOrchid = 29,

    /// <summary> 深红 </summary>
    DarkRed = 30,

    /// <summary> 深鲑红 </summary>
    DarkSalmon = 31,

    /// <summary> 深海藻绿 </summary>
    DarkSeaGreen = 32,

    /// <summary> 深岩蓝 </summary>
    DarkSlateBlue = 33,

    /// <summary> 深岩灰 </summary>
    DarkSlateGray = 34,

    /// <summary> 深松石绿 </summary>
    DarkTurquoise = 35,

    /// <summary> 深紫 </summary>
    DarkViolet = 36,

    /// <summary> 深粉 </summary>
    DeepPink = 37,

    /// <summary> 深天蓝 </summary>
    DeepSkyBlue = 38,

    /// <summary> 昏灰 </summary>
    DimGray = 39,

    /// <summary> 湖蓝 </summary>
    DodgerBlue = 40,

    /// <summary> 火砖红 </summary>
    FireBrick = 41,

    /// <summary> 花卉白 </summary>
    FloralWhite = 42,

    /// <summary> 森林绿 </summary>
    ForestGreen = 43,

    /// <summary> 庚氏灰 </summary>
    GainsBoro = 44,

    /// <summary> 幽灵白 </summary>
    GhostWhite = 45,

    /// <summary> 金色 </summary>
    Gold = 46,

    /// <summary> 金菊黄 </summary>
    GoldenRod = 47,

    /// <summary> 灰色 </summary>
    Gray = 48,

    /// <summary> 调和绿 </summary>
    Green = 49,

    /// <summary> 黄绿色 </summary>
    GreenYellow = 50,

    /// <summary> 蜜瓜绿 </summary>
    HoneyDew = 51,

    /// <summary> 艳粉 </summary>
    HotPink = 52,

    /// <summary> 印度红 </summary>
    IndianRed = 53,

    /// <summary> 靛蓝 </summary>
    Indigo = 54,

    /// <summary> 象牙白 </summary>
    Ivory = 55,

    /// <summary> 卡其色 </summary>
    Khaki = 56,

    /// <summary> 薰衣草紫 </summary>
    Lavender = 57,

    /// <summary> 薰衣草红 </summary>
    LavenderBlush = 58,

    /// <summary> 草坪绿 </summary>
    LawnGreen = 59,

    /// <summary> 柠檬绸黄 </summary>
    LemonChiffon = 60,

    /// <summary> 浅蓝 </summary>
    LightBlue = 61,

    /// <summary> 浅珊瑚红 </summary>
    LightCoral = 62,

    /// <summary> 浅青 </summary>
    LightCyan = 63,

    /// <summary> 浅金菊黄 </summary>
    LightGoldenRodYellow = 64,

    /// <summary> 亮灰 </summary>
    LightGray = 65,

    /// <summary> 浅绿 </summary>
    LightGreen = 66,

    /// <summary> 浅粉 </summary>
    LightPink = 67,

    /// <summary> 浅鲑红 </summary>
    LightSalmon = 68,

    /// <summary> 浅海藻绿 </summary>
    LightSeaGreen = 69,

    /// <summary> 浅天蓝 </summary>
    LightSkyBlue = 70,

    /// <summary> 浅岩灰 </summary>
    LightSlateGray = 71,

    /// <summary> 亮钢蓝 </summary>
    LightSteelBlue = 72,

    /// <summary> 浅黄 </summary>
    LightYellow = 73,

    /// <summary> 绿色 </summary>
    Lime = 74,

    /// <summary> 青柠绿 </summary>
    LimeGreen = 75,

    /// <summary> 亚麻色 </summary>
    Linen = 76,

    /// <summary> 洋红 </summary>
    Magenta = 77,

    /// <summary> 栗色 </summary>
    Maroon = 78,

    /// <summary> 中碧绿 </summary>
    MediumAquaMarine = 79,

    /// <summary> 中蓝 </summary>
    MediumBlue = 80,

    /// <summary> 中洋兰紫 </summary>
    MediumOrchid = 81,

    /// <summary> 中紫 </summary>
    MediumPurple = 82,

    /// <summary> 中海藻绿 </summary>
    MediumSeaGreen = 83,

    /// <summary> 中岩蓝 </summary>
    MediumSlateBlue = 84,

    /// <summary> 中嫩绿 </summary>
    MediumSpringGreen = 85,

    /// <summary> 中松石绿 </summary>
    MediumTurquoise = 86,

    /// <summary> 中紫红 </summary>
    MediumVioletRed = 87,

    /// <summary> 午夜蓝 </summary>
    MidnightBlue = 88,

    /// <summary> 薄荷乳白 </summary>
    MintCream = 89,

    /// <summary> 雾玫瑰红 </summary>
    MistyRose = 90,

    /// <summary> 鹿皮色 </summary>
    Moccasin = 91,

    /// <summary> 土著白 </summary>
    NavajoWhite = 92,

    /// <summary> 藏青 </summary>
    Navy = 93,

    /// <summary> 旧蕾丝白 </summary>
    OldLace = 94,

    /// <summary> 橄榄色 </summary>
    Olive = 95,

    /// <summary> 橄榄绿 </summary>
    OliveDrab = 96,

    /// <summary> 橙色 </summary>
    Orange = 97,

    /// <summary> 橘红 </summary>
    OrangeRed = 98,

    /// <summary> 洋兰紫 </summary>
    Orchid = 99,

    /// <summary> 白金菊黄 </summary>
    PaleGoldenRod = 100,

    /// <summary> 白绿色 </summary>
    PaleGreen = 101,

    /// <summary> 白松石绿 </summary>
    PaleTurquoise = 102,

    /// <summary> 弱紫罗兰红 </summary>
    PaleVioletRed = 103,

    /// <summary> 番木瓜橙 </summary>
    PapayaWhip = 104,

    /// <summary> 粉扑桃色 </summary>
    PeachPuff = 105,

    /// <summary> 秘鲁红 </summary>
    Peru = 106,

    /// <summary> 粉色 </summary>
    Pink = 107,

    /// <summary> 李紫 </summary>
    Plum = 108,

    /// <summary> 粉末蓝 </summary>
    PowderBlue = 109,

    /// <summary> 紫色 </summary>
    Purple = 110,

    /// <summary> 红色 </summary>
    Red = 111,

    /// <summary> 玫瑰褐 </summary>
    RosyBrown = 112,

    /// <summary> 品蓝 </summary>
    RoyalBlue = 113,

    /// <summary> 鞍褐 </summary>
    SaddleBrown = 114,

    /// <summary> 鲑红 </summary>
    Salmon = 115,

    /// <summary> 沙褐 </summary>
    SandyBrown = 116,

    /// <summary> 海藻绿 </summary>
    SeaGreen = 117,

    /// <summary> 蚌壳白 </summary>
    SeaShell = 118,

    /// <summary> 土黄赭 </summary>
    Sienna = 119,

    /// <summary> 银色 </summary>
    Silver = 120,

    /// <summary> 天蓝 </summary>
    SkyBlue = 121,

    /// <summary> 岩蓝 </summary>
    SlateBlue = 122,

    /// <summary> 岩灰 </summary>
    SlateGray = 123,

    /// <summary> 雪白 </summary>
    Snow = 124,

    /// <summary> 春绿 </summary>
    SpringGreen = 125,

    /// <summary> 钢青 </summary>
    SteelBlue = 126,

    /// <summary> 日晒褐 </summary>
    Tan = 127,

    /// <summary> 鸭翅绿 </summary>
    Teal = 128,

    /// <summary> 蓟紫 </summary>
    Thistle = 129,

    /// <summary> 番茄红 </summary>
    Tomato = 130,

    /// <summary> 松石绿 </summary>
    Turquoise = 131,

    /// <summary> 紫罗兰色 </summary>
    Violet = 132,

    /// <summary> 麦色 </summary>
    Wheat = 133,

    /// <summary> 白色 </summary>
    White = 134,

    /// <summary> 烟雾白 </summary>
    WhiteSmoke = 135,

    /// <summary> 黄色 </summary>
    Yellow = 136,

    /// <summary> 暗黄绿色 </summary>
    YellowGreen = 137
}

public static class ColorTool
{
#if !WX_MINIGAME
    [ThreadStatic]
#endif
    // 每个线程都会有自己独立的 StringBuilder 实例
    private static StringBuilder _threadLocalStringBuilder;

    public static StringBuilder GetStringBuilder()
    {
        if (_threadLocalStringBuilder == null)
        {
            _threadLocalStringBuilder = new StringBuilder();
        }
        else
        {
            _threadLocalStringBuilder.Clear();
        }

        return _threadLocalStringBuilder;
    }

#if !BATTLE_SERVER

    #region ToColor

    private static readonly (string rgb16Color, string htmlColor, Color color)[] ColorPrefixes = new (string rgb16Color, string htmlColor, Color color)[]
    {
        /*   0 */ ("#F0F8FF", "<color=#F0F8FF>", new Color(0.94f, 0.97f, 1f)), // AliceBlue    爱丽丝蓝
        /*   1 */ ("#FAEBD7", "<color=#FAEBD7>", new Color(0.98f, 0.92f, 0.84f)), // AntiqueWhite 古董白
        /*   2 */ ("#7FFFD4", "<color=#7FFFD4>", new Color(0.50f, 1f, 0.83f)), // AquaMarine   碧绿
        /*   3 */ ("#F0FFFF", "<color=#F0FFFF>", new Color(0.94f, 1f, 1f)), // Azure        青白色
        /*   4 */ ("#F5F5DC", "<color=#F5F5DC>", new Color(0.96f, 0.96f, 0.86f)), // Beige        米色
        /*   5 */ ("#FFE4C4", "<color=#FFE4C4>", new Color(1f, 0.89f, 0.77f)), // Bisque       陶坯黄
        /*   6 */ ("#000000", "<color=#000000>", new Color(0f, 0f, 0f)), // Black        黑色
        /*   7 */ ("#FFEBCD", "<color=#FFEBCD>", new Color(1f, 0.92f, 0.80f)), // BlanchedAlmond 杏仁白
        /*   8 */ ("#0000FF", "<color=#0000FF>", new Color(0f, 0f, 1f)), // Blue         蓝色
        /*   9 */ ("#8A2BE2", "<color=#8A2BE2>", new Color(0.54f, 0.17f, 0.89f)), // BlueViolet   蓝紫色
        /*  10 */ ("#A52A2A", "<color=#A52A2A>", new Color(0.65f, 0.16f, 0.16f)), // Brown        褐色
        /*  11 */ ("#DEB887", "<color=#DEB887>", new Color(0.87f, 0.72f, 0.53f)), // BurlyWood    硬木褐
        /*  12 */ ("#5F9EA0", "<color=#5F9EA0>", new Color(0.37f, 0.62f, 0.63f)), // CadetBlue    军服蓝
        /*  13 */ ("#7FFF00", "<color=#7FFF00>", new Color(0.50f, 1f, 0f)), // Chartreuse   查特酒绿
        /*  14 */ ("#D2691E", "<color=#D2691E>", new Color(0.82f, 0.41f, 0.12f)), // Chocolate    巧克力色
        /*  15 */ ("#FF7F50", "<color=#FF7F50>", new Color(1f, 0.50f, 0.31f)), // Coral        珊瑚红
        /*  16 */ ("#6495ED", "<color=#6495ED>", new Color(0.39f, 0.58f, 0.93f)), // CornFlowerBlue 矢车菊蓝
        /*  17 */ ("#FFF8DC", "<color=#FFF8DC>", new Color(1f, 0.97f, 0.86f)), // CornSilk     玉米穗黄
        /*  18 */ ("#DC143C", "<color=#DC143C>", new Color(0.86f, 0.08f, 0.24f)), // Crimson      绯红
        /*  19 */ ("#00FFFF", "<color=#00FFFF>", new Color(0f, 1f, 1f)), // Cyan         青色
        /*  20 */ ("#00008B", "<color=#00008B>", new Color(0f, 0f, 0.55f)), // DarkBlue     深蓝
        /*  21 */ ("#008B8B", "<color=#008B8B>", new Color(0f, 0.55f, 0.55f)), // DarkCyan     深青
        /*  22 */ ("#B8860B", "<color=#B8860B>", new Color(0.72f, 0.53f, 0.04f)), // DarkGoldenRod 深金菊黄
        /*  23 */ ("#A9A9A9", "<color=#A9A9A9>", new Color(0.66f, 0.66f, 0.66f)), // DarkGray     暗灰
        /*  24 */ ("#006400", "<color=#006400>", new Color(0f, 0.39f, 0f)), // DarkGreen    深绿
        /*  25 */ ("#BDB76B", "<color=#BDB76B>", new Color(0.74f, 0.72f, 0.42f)), // DarkKhaki    深卡其色
        /*  26 */ ("#8B008B", "<color=#8B008B>", new Color(0.55f, 0f, 0.55f)), // DarkMagenta  深品红
        /*  27 */ ("#556B2F", "<color=#556B2F>", new Color(0.33f, 0.42f, 0.18f)), // DarkOliveGreen 深橄榄绿
        /*  28 */ ("#FF8C00", "<color=#FF8C00>", new Color(1f, 0.55f, 0f)), // DarkOrange   深橙
        /*  29 */ ("#9932CC", "<color=#9932CC>", new Color(0.60f, 0.20f, 0.80f)), // DarkOrchid   深洋兰紫
        /*  30 */ ("#8B0000", "<color=#8B0000>", new Color(0.55f, 0f, 0f)), // DarkRed      深红
        /*  31 */ ("#E9967A", "<color=#E9967A>", new Color(0.91f, 0.59f, 0.48f)), // DarkSalmon   深鲑红
        /*  32 */ ("#8FBC8F", "<color=#8FBC8F>", new Color(0.56f, 0.74f, 0.56f)), // DarkSeaGreen 深海藻绿
        /*  33 */ ("#483D8B", "<color=#483D8B>", new Color(0.28f, 0.24f, 0.55f)), // DarkSlateBlue 深岩蓝
        /*  34 */ ("#2F4F4F", "<color=#2F4F4F>", new Color(0.18f, 0.31f, 0.31f)), // DarkSlateGray 深岩灰
        /*  35 */ ("#00CED1", "<color=#00CED1>", new Color(0f, 0.81f, 0.82f)), // DarkTurquoise 深松石绿
        /*  36 */ ("#9400D3", "<color=#9400D3>", new Color(0.58f, 0f, 0.83f)), // DarkViolet   深紫
        /*  37 */ ("#FF1493", "<color=#FF1493>", new Color(1f, 0.08f, 0.58f)), // DeepPink     深粉
        /*  38 */ ("#00BFFF", "<color=#00BFFF>", new Color(0f, 0.75f, 1f)), // DeepSkyBlue  深天蓝
        /*  39 */ ("#696969", "<color=#696969>", new Color(0.41f, 0.41f, 0.41f)), // DimGray      昏灰
        /*  40 */ ("#1E90FF", "<color=#1E90FF>", new Color(0.12f, 0.56f, 1f)), // DodgerBlue   湖蓝
        /*  41 */ ("#B22222", "<color=#B22222>", new Color(0.70f, 0.13f, 0.13f)), // FireBrick    火砖红
        /*  42 */ ("#FFFAF0", "<color=#FFFAF0>", new Color(1f, 0.98f, 0.94f)), // FloralWhite  花卉白
        /*  43 */ ("#228B22", "<color=#228B22>", new Color(0.13f, 0.55f, 0.13f)), // ForestGreen  森林绿
        /*  44 */ ("#DCDCDC", "<color=#DCDCDC>", new Color(0.86f, 0.86f, 0.86f)), // GainsBoro    庚氏灰
        /*  45 */ ("#F8F8FF", "<color=#F8F8FF>", new Color(0.97f, 0.97f, 1f)), // GhostWhite   幽灵白
        /*  46 */ ("#FFD700", "<color=#FFD700>", new Color(1f, 0.84f, 0f)), // Gold         金色
        /*  47 */ ("#DAA520", "<color=#DAA520>", new Color(0.85f, 0.65f, 0.13f)), // GoldenRod    金菊黄
        /*  48 */ ("#808080", "<color=#808080>", new Color(0.50f, 0.50f, 0.50f)), // Gray         灰色
        /*  49 */ ("#008000", "<color=#008000>", new Color(0f, 0.50f, 0f)), // Green        调和绿
        /*  50 */ ("#ADFF2F", "<color=#ADFF2F>", new Color(0.68f, 1f, 0.18f)), // GreenYellow  黄绿色
        /*  51 */ ("#F0FFF0", "<color=#F0FFF0>", new Color(0.94f, 1f, 0.94f)), // HoneyDew     蜜瓜绿
        /*  52 */ ("#FF69B4", "<color=#FF69B4>", new Color(1f, 0.41f, 0.71f)), // HotPink      艳粉
        /*  53 */ ("#CD5C5C", "<color=#CD5C5C>", new Color(0.80f, 0.36f, 0.36f)), // IndianRed    印度红
        /*  54 */ ("#4B0082", "<color=#4B0082>", new Color(0.29f, 0f, 0.51f)), // Indigo       靛蓝
        /*  55 */ ("#FFFFF0", "<color=#FFFFF0>", new Color(1f, 1f, 0.94f)), // Ivory        象牙白
        /*  56 */ ("#F0E68C", "<color=#F0E68C>", new Color(0.94f, 0.90f, 0.55f)), // Khaki        卡其色
        /*  57 */ ("#E6E6FA", "<color=#E6E6FA>", new Color(0.90f, 0.90f, 0.98f)), // Lavender     薰衣草紫
        /*  58 */ ("#FFF0F5", "<color=#FFF0F5>", new Color(1f, 0.94f, 0.96f)), // LavenderBlush 薰衣草红
        /*  59 */ ("#7CFC00", "<color=#7CFC00>", new Color(0.49f, 0.99f, 0f)), // LawnGreen    草坪绿
        /*  60 */ ("#FFFACD", "<color=#FFFACD>", new Color(1f, 0.98f, 0.80f)), // LemonChiffon 柠檬绸黄
        /*  61 */ ("#ADD8E6", "<color=#ADD8E6>", new Color(0.68f, 0.85f, 0.90f)), // LightBlue    浅蓝
        /*  62 */ ("#F08080", "<color=#F08080>", new Color(0.94f, 0.50f, 0.50f)), // LightCoral   浅珊瑚红
        /*  63 */ ("#E0FFFF", "<color=#E0FFFF>", new Color(0.88f, 1f, 1f)), // LightCyan    浅青
        /*  64 */ ("#FAFAD2", "<color=#FAFAD2>", new Color(0.98f, 0.98f, 0.82f)), // LightGoldenRodYellow 浅金菊黄
        /*  65 */ ("#D3D3D3", "<color=#D3D3D3>", new Color(0.83f, 0.83f, 0.83f)), // LightGray    亮灰
        /*  66 */ ("#90EE90", "<color=#90EE90>", new Color(0.56f, 0.93f, 0.56f)), // LightGreen   浅绿
        /*  67 */ ("#FFB6C1", "<color=#FFB6C1>", new Color(1f, 0.71f, 0.76f)), // LightPink    浅粉
        /*  68 */ ("#FFA07A", "<color=#FFA07A>", new Color(1f, 0.63f, 0.48f)), // LightSalmon  浅鲑红
        /*  69 */ ("#20B2AA", "<color=#20B2AA>", new Color(0.13f, 0.70f, 0.67f)), // LightSeaGreen 浅海藻绿
        /*  70 */ ("#87CEFA", "<color=#87CEFA>", new Color(0.53f, 0.81f, 0.98f)), // LightSkyBlue 浅天蓝
        /*  71 */ ("#778899", "<color=#778899>", new Color(0.47f, 0.53f, 0.60f)), // LightSlateGray 浅岩灰
        /*  72 */ ("#B0C4DE", "<color=#B0C4DE>", new Color(0.69f, 0.77f, 0.87f)), // LightSteelBlue 亮钢蓝
        /*  73 */ ("#FFFFE0", "<color=#FFFFE0>", new Color(1f, 1f, 0.88f)), // LightYellow  浅黄
        /*  74 */ ("#00FF00", "<color=#00FF00>", new Color(0f, 1f, 0f)), // Lime         绿色
        /*  75 */ ("#32CD32", "<color=#32CD32>", new Color(0.20f, 0.80f, 0.20f)), // LimeGreen    青柠绿
        /*  76 */ ("#FAF0E6", "<color=#FAF0E6>", new Color(0.98f, 0.94f, 0.90f)), // Linen        亚麻色
        /*  77 */ ("#FF00FF", "<color=#FF00FF>", new Color(1f, 0f, 1f)), // Magenta      洋红
        /*  78 */ ("#800000", "<color=#800000>", new Color(0.50f, 0f, 0f)), // Maroon       栗色
        /*  79 */ ("#66CDAA", "<color=#66CDAA>", new Color(0.40f, 0.80f, 0.67f)), // MediumAquaMarine 中碧绿
        /*  80 */ ("#0000CD", "<color=#0000CD>", new Color(0f, 0f, 0.80f)), // MediumBlue   中蓝
        /*  81 */ ("#BA55D3", "<color=#BA55D3>", new Color(0.73f, 0.33f, 0.83f)), // MediumOrchid 中洋兰紫
        /*  82 */ ("#9370DB", "<color=#9370DB>", new Color(0.58f, 0.44f, 0.86f)), // MediumPurple 中紫
        /*  83 */ ("#3CB371", "<color=#3CB371>", new Color(0.24f, 0.70f, 0.44f)), // MediumSeaGreen 中海藻绿
        /*  84 */ ("#7B68EE", "<color=#7B68EE>", new Color(0.48f, 0.41f, 0.93f)), // MediumSlateBlue 中岩蓝
        /*  85 */ ("#00FA9A", "<color=#00FA9A>", new Color(0f, 0.98f, 0.60f)), // MediumSpringGreen 中嫩绿
        /*  86 */ ("#48D1CC", "<color=#48D1CC>", new Color(0.28f, 0.82f, 0.80f)), // MediumTurquoise 中松石绿
        /*  87 */ ("#C71585", "<color=#C71585>", new Color(0.78f, 0.08f, 0.52f)), // MediumVioletRed 中紫红
        /*  88 */ ("#191970", "<color=#191970>", new Color(0.10f, 0.10f, 0.44f)), // MidnightBlue 午夜蓝
        /*  89 */ ("#F5FFFA", "<color=#F5FFFA>", new Color(0.96f, 1f, 0.98f)), // MintCream    薄荷乳白
        /*  90 */ ("#FFE4E1", "<color=#FFE4E1>", new Color(1f, 0.89f, 0.88f)), // MistyRose    雾玫瑰红
        /*  91 */ ("#FFE4B5", "<color=#FFE4B5>", new Color(1f, 0.89f, 0.71f)), // Moccasin     鹿皮色
        /*  92 */ ("#FFDEAD", "<color=#FFDEAD>", new Color(1f, 0.87f, 0.68f)), // NavajoWhite  土著白
        /*  93 */ ("#000080", "<color=#000080>", new Color(0f, 0f, 0.50f)), // Navy         藏青
        /*  94 */ ("#FDF5E6", "<color=#FDF5E6>", new Color(0.99f, 0.96f, 0.90f)), // OldLace      旧蕾丝白
        /*  95 */ ("#808000", "<color=#808000>", new Color(0.50f, 0.50f, 0f)), // Olive        橄榄色
        /*  96 */ ("#6B8E23", "<color=#6B8E23>", new Color(0.42f, 0.56f, 0.14f)), // OliveDrab    橄榄绿
        /*  97 */ ("#FFA500", "<color=#FFA500>", new Color(1f, 0.65f, 0f)), // Orange       橙色
        /*  98 */ ("#FF4500", "<color=#FF4500>", new Color(1f, 0.27f, 0f)), // OrangeRed    橘红
        /*  99 */ ("#DA70D6", "<color=#DA70D6>", new Color(0.85f, 0.44f, 0.84f)), // Orchid       洋兰紫
        /* 100 */ ("#EEE8AA", "<color=#EEE8AA>", new Color(0.93f, 0.91f, 0.67f)), // PaleGoldenRod 白金菊黄
        /* 101 */ ("#98FB98", "<color=#98FB98>", new Color(0.60f, 0.98f, 0.60f)), // PaleGreen    白绿色
        /* 102 */ ("#AFEEEE", "<color=#AFEEEE>", new Color(0.69f, 0.93f, 0.93f)), // PaleTurquoise 白松石绿
        /* 103 */ ("#DB7093", "<color=#DB7093>", new Color(0.86f, 0.44f, 0.58f)), // PaleVioletRed 弱紫罗兰红
        /* 104 */ ("#FFEFD5", "<color=#FFEFD5>", new Color(1f, 0.94f, 0.84f)), // PapayaWhip   番木瓜橙
        /* 105 */ ("#FFDAB9", "<color=#FFDAB9>", new Color(1f, 0.85f, 0.73f)), // PeachPuff    粉扑桃色
        /* 106 */ ("#CD853F", "<color=#CD853F>", new Color(0.80f, 0.52f, 0.25f)), // Peru         秘鲁红
        /* 107 */ ("#FFC0CB", "<color=#FFC0CB>", new Color(1f, 0.75f, 0.80f)), // Pink         粉色
        /* 108 */ ("#DDA0DD", "<color=#DDA0DD>", new Color(0.87f, 0.63f, 0.87f)), // Plum         李紫
        /* 109 */ ("#B0E0E6", "<color=#B0E0E6>", new Color(0.69f, 0.88f, 0.90f)), // PowderBlue   粉末蓝
        /* 110 */ ("#800080", "<color=#800080>", new Color(0.50f, 0f, 0.50f)), // Purple       紫色
        /* 111 */ ("#FF0000", "<color=#FF0000>", new Color(1f, 0f, 0f)), // Red          红色
        /* 112 */ ("#BC8F8F", "<color=#BC8F8F>", new Color(0.74f, 0.56f, 0.56f)), // RosyBrown    玫瑰褐
        /* 113 */ ("#4169E1", "<color=#4169E1>", new Color(0.25f, 0.41f, 0.88f)), // RoyalBlue    品蓝
        /* 114 */ ("#8B4513", "<color=#8B4513>", new Color(0.55f, 0.27f, 0.07f)), // SaddleBrown  鞍褐
        /* 115 */ ("#FA8072", "<color=#FA8072>", new Color(0.98f, 0.50f, 0.45f)), // Salmon       鲑红
        /* 116 */ ("#F4A460", "<color=#F4A460>", new Color(0.96f, 0.64f, 0.38f)), // SandyBrown   沙褐
        /* 117 */ ("#2E8B57", "<color=#2E8B57>", new Color(0.18f, 0.55f, 0.34f)), // SeaGreen     海藻绿
        /* 118 */ ("#FFF5EE", "<color=#FFF5EE>", new Color(1f, 0.96f, 0.93f)), // SeaShell     蚌壳白
        /* 119 */ ("#A0522D", "<color=#A0522D>", new Color(0.63f, 0.32f, 0.18f)), // Sienna       土黄赭
        /* 120 */ ("#C0C0C0", "<color=#C0C0C0>", new Color(0.75f, 0.75f, 0.75f)), // Silver       银色
        /* 121 */ ("#87CEEB", "<color=#87CEEB>", new Color(0.53f, 0.81f, 0.92f)), // SkyBlue      天蓝
        /* 122 */ ("#6A5ACD", "<color=#6A5ACD>", new Color(0.42f, 0.35f, 0.80f)), // SlateBlue    岩蓝
        /* 123 */ ("#708090", "<color=#708090>", new Color(0.44f, 0.50f, 0.56f)), // SlateGray    岩灰
        /* 124 */ ("#FFFAFA", "<color=#FFFAFA>", new Color(1f, 0.98f, 0.98f)), // Snow         雪白
        /* 125 */ ("#00FF7F", "<color=#00FF7F>", new Color(0f, 1f, 0.50f)), // SpringGreen  春绿
        /* 126 */ ("#4682B4", "<color=#4682B4>", new Color(0.27f, 0.51f, 0.71f)), // SteelBlue    钢青
        /* 127 */ ("#D2B48C", "<color=#D2B48C>", new Color(0.82f, 0.71f, 0.55f)), // Tan          日晒褐
        /* 128 */ ("#008080", "<color=#008080>", new Color(0f, 0.50f, 0.50f)), // Teal         鸭翅绿
        /* 129 */ ("#D8BFD8", "<color=#D8BFD8>", new Color(0.85f, 0.75f, 0.85f)), // Thistle      蓟紫
        /* 130 */ ("#FF6347", "<color=#FF6347>", new Color(1f, 0.39f, 0.28f)), // Tomato       番茄红
        /* 131 */ ("#40E0D0", "<color=#40E0D0>", new Color(0.25f, 0.88f, 0.82f)), // Turquoise    松石绿
        /* 132 */ ("#EE82EE", "<color=#EE82EE>", new Color(0.93f, 0.51f, 0.93f)), // Violet       紫罗兰色
        /* 133 */ ("#F5DEB3", "<color=#F5DEB3>", new Color(0.96f, 0.87f, 0.70f)), // Wheat        麦色
        /* 134 */ ("#FFFFFF", "<color=#FFFFFF>", new Color(1f, 1f, 1f)), // White        白色
        /* 135 */ ("#F5F5F5", "<color=#F5F5F5>", new Color(0.96f, 0.96f, 0.96f)), // WhiteSmoke   烟雾白
        /* 136 */ ("#FFFF00", "<color=#FFFF00>", new Color(1f, 1f, 0f)), // Yellow       黄色
        /* 137 */ ("#9ACD32", "<color=#9ACD32>", new Color(0.60f, 0.80f, 0.20f)) // YellowGreen  暗黄绿色
    };

    /// <summary>
    /// 颜色结束符 </summary>
    private const string ColorSymbol = "</color>";
    
    /// <summary>
    /// 将指定字符串包装成对应颜色的富文本（基于索引获取前缀）。
    /// </summary>
    /// <param name="format">原始文本</param>
    /// <param name="ct">要应用颜色的 DebugColor 枚举值</param>
    /// <returns>带 <color> 标签的富文本字符串</returns>
    public static string ToColor(this string format, ColorEnum ct)
    {
        if (string.IsNullOrEmpty(format))
        {
            return format;
        }

        int idx = (int)ct;
        if (idx >= 0 && idx < ColorPrefixes.Length)
        {
            string prefix = ColorPrefixes[idx].htmlColor;
            if (!string.IsNullOrEmpty(prefix))
            {
                return GetStringBuilder().Append(prefix).Append(format).Append(ColorSymbol).ToString();
            }
        }

        return format;
    }

    public static string ToColor(this object format, ColorEnum ct)
    {
        string i = format.ToString();
        i = ToColor(i, ct);
        return i;
    }

    public static Color ToColor(this ColorEnum color)
    {
        int idx = (int)color;
        if (idx >= 0 && idx < ColorPrefixes.Length)
        {
            return ColorPrefixes[idx].color;
        }

        return Color.white;
    }

    #endregion

#else
    public static string ToColor(this string format, ColorEnum ct)
    {
        string colorCode = ct switch
        {
            ColorEnum.Green => "\x1b[32m",
            ColorEnum.Yellow => "\x1b[33m",
            ColorEnum.Red => "\x1b[31m",
            _ => ""
        };
        string resetCode = "\x1b[0m";
        return GetStringBuilder().Append(colorCode).Append(format).Append(resetCode).ToString();
    }
#endif
}