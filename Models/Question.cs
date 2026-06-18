using System.Text.Json.Serialization;

namespace CodeDeLaRoute.Models;

/// <summary>
/// 题目模型：包含题目文本、选项、正确答案索引、解释、难度、图片URL 及预定义变体。
/// </summary>
public class Question
{
    /// <summary>题目唯一标识</summary>
    public int Id { get; set; }
    /// <summary>题目文本</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>关联图片 URL（可选）</summary>
    public string? ImageUrl { get; set; }
    /// <summary>题目分类（如 Signalisation、Priorité 等）</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>官方 ETG 主题代码（L, C, R, M, D, P, N, E, S, U），从 Category 自动映射</summary>
    public string ThemeCode => MapToThemeCode(Category);
    /// <summary>选项列表（通常 2-4 个）</summary>
    public List<string> Options { get; set; } = new();
    /// <summary>
    /// 正确答案的索引列表（0-based）。
    /// 若 Count == 1 则为单选题；大于 1 则为多选题。
    /// </summary>
    public List<int> CorrectAnswerIndices { get; set; } = new();
    /// <summary>答案解析/说明</summary>
    public string Explanation { get; set; } = string.Empty;
    /// <summary>难度等级</summary>
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Moyen;
    /// <summary>是否为多选题（根据 CorrectAnswerIndices 数量自动计算）</summary>
    public bool IsMultipleChoice => CorrectAnswerIndices.Count > 1;
    /// <summary>
    /// 预定义变体题列表（从 JSON 加载）。
    /// 若为空，则运行时由 QuestionVariantService 自动生成算法变体。
    /// </summary>
    public List<Question> Variants { get; set; } = new();

    /// <summary>从题目文本中自动提取的法语驾考术语代码列表</summary>
    [JsonIgnore]
    public List<string> Termes => TermesGlossaire.ExtractTermes(this);

    /// <summary>
    /// 将项目自定义分类映射到 ETG 官方 10 个主题代码。
    /// </summary>
    private static string MapToThemeCode(string category) => category switch
    {
        // L - Le conducteur (The Driver): vigilance, alcohol, drugs, fatigue, responsibility
        "Alcool et Stupéfiants" => "L",
        "Distractions et Fatigue" => "L",
        "Permis à Points" => "L",
        "Permis" => "L",
        "Conduite" => "L",

        // C - Les autres usagers (Other Road Users): sharing the road, vulnerable users
        "Piétons et Cyclistes" => "C",
        "Croisement et Dépassement" => "C",
        "Priorité" => "C",
        "Circulation interfile" => "C",

        // R - La circulation routière (Traffic Rules): signals, speed, intersections
        "Signalisation" => "R",
        "Signalisation temporaire" => "R",
        "Vitesse" => "R",
        "Passages à niveau" => "R",

        // M - La mécanique et les équipements (Mechanics & Equipment)
        "Mécanique" => "M",
        "Éclairage" => "M",

        // D - La route (The Road): road types, tunnels, weather, night driving
        "Conditions Météo" => "D",
        "Conditions météo moto" => "D",
        "Tunnels et Autoroutes" => "D",

        // P - Prendre et quitter son véhicule (Entering/Exiting Vehicle): parking, stopping, loading
        "Stationnement" => "P",
        "Passager" => "P",
        "Chargement et bagages" => "P",

        // N - Les notions diverses (Miscellaneous): documents, insurance, infractions
        "Maîtrise" => "N",
        "Assurance et documents" => "N",
        "Documents et Assurance" => "N",

        // E - L'environnement (Environment): eco-driving, pollution, noise
        "Environnement" => "E",

        // S - La sécurité du passager et du véhicule (Safety): seatbelts, airbags, child seats
        "Sécurité" => "S",
        "Équipement" => "S",

        // U - Les premiers secours (First Aid)
        "Premiers Secours" => "U",

        _ => "R" // default to rules of the road
    };
}

/// <summary>
/// 难度等级枚举，使用字符串序列化（"Facile"/"Moyen"/"Difficile"）。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyLevel
{
    Facile,
    Moyen,
    Difficile
}

/// <summary>
/// ETG 官方 10 个主题定义（2016 年改革后）。
/// </summary>
public static class EtgThemes
{
    /// <summary>主题列表：代码、名称、每题数量、图标</summary>
    public static readonly List<(string Code, string Name, int Count, string Icon)> All = new()
    {
        ("L", "Le conducteur", 10, "🧑"),
        ("C", "Les autres usagers", 5, "🚶"),
        ("R", "La circulation routière", 4, "🚦"),
        ("M", "La mécanique et équipements", 4, "🔧"),
        ("D", "La route", 4, "🛣️"),
        ("P", "Prendre et quitter son véhicule", 3, "🅿️"),
        ("N", "Les notions diverses", 3, "📋"),
        ("E", "L'environnement", 3, "🌿"),
        ("S", "Sécurité passager et véhicule", 3, "🛡️"),
        ("U", "Les premiers secours", 1, "🏥"),
    };

    /// <summary>总题数</summary>
    public const int TotalQuestions = 40;

    /// <summary>通过分数线</summary>
    public const int PassThreshold = 35;

    /// <summary>考试总时间（分钟）</summary>
    public const int ExamMinutes = 30;

    /// <summary>每题答题时间（秒）</summary>
    public const int QuestionSeconds = 20;

    /// <summary>获取主题代码对应的名称</summary>
    public static string GetName(string code) =>
        All.FirstOrDefault(t => t.Code == code).Name ?? code;
}

/// <summary>
/// 法语驾考术语及其关键词匹配规则。
/// </summary>
public static class TermesGlossaire
{
    /// <summary>
    /// 术语定义：代码、显示名称、图标、匹配关键词列表。
    /// 问题的 Text + Options + Explanation 中包含任一关键词即可匹配。
    /// </summary>
    public static readonly List<TermeDefinition> All = new()
    {
        new("vitesse",      "Vitesse",         "⏱️",  new[]{"vitesse","limitation","km/h","ralentir","accélérer","limiteur","régulateur"}),
        new("permis",       "Permis / Points", "📝",  new[]{"permis","points","probatoire","suspension","retrait","stage","annulation","invalidation"}),
        new("depassement",  "Dépassement",     "⬅️",  new[]{"dépasser","dépassement","doubler","rabattre","ligne continue","croisement"}),
        new("freinage",     "Freinage / ABS",  "🛑",  new[]{"freiner","freinage","ABS","distance","temps de réaction","disque","plaquette"}),
        new("autoroute",    "Autoroute",       "🛣️",  new[]{"autoroute","bande urgence","BAU","péage","voie rapide","demi-tour"}),
        new("secours",      "Premiers secours","🏥",  new[]{"secours","urgence","accident","PLS","massage","DAE","défibrillateur","alerter","protéger","SAMU","pompier"}),
        new("panneaux",     "Panneaux",        "🚸",  new[]{"panneau","panneaux","triangulaire","octogonal","carré","rond","rectangulaire","balise"}),
        new("pluie",        "Pluie / Météo",   "🌧️",  new[]{"pluie","aquaplaning","chaussée mouillée","pluvieux","intempérie"}),
        new("priorite",     "Priorité",        "⚠️",   new[]{"priorité","cédez","STOP","prioritaire","croisement","refuser","laisser passer"}),
        new("sanctions",    "Sanctions",       "⚖️",   new[]{"amende","sanction","prison","contravention","délit","€","135","750","1500","forfaitaire"}),
        new("clignotant",   "Clignotant",      "🔄",  new[]{"clignotant","warning","feu détresse","indicateur","signaler"}),
        new("pietons",      "Piétons",         "🚶",  new[]{"piéton","piétons","passage piéton","trottoir","traverser"}),
        new("eclairage",    "Éclairage",       "💡",  new[]{"phare","feux de croisement","feux de route","pleins phares","feux de brouillard","DRL","diurne","éclairage"}),
        new("brouillard",   "Brouillard",      "🌫️",  new[]{"brouillard","visibilité réduite","brume","visibilité inférieure","50 m"}),
        new("parking",      "Stationnement",   "🅿️",  new[]{"stationnement","stationner","garer","parking","arrêt","zone bleue","disque"}),
        new("alcool",       "Alcool",          "🍷",  new[]{"alcool","alcoolémie","ivresse","éthylotest","sobre","0,5","0,2","éliminer"}),
        new("neige",        "Neige / Verglas", "❄️",  new[]{"neige","verglas","chaînes","pneus neige","hiver","enneigée"}),
        new("assurance",    "Assurance",       "📄",  new[]{"assurance","carte grise","contrôle technique","document","attestation","certificat","vignette","immatriculation"}),
        new("stupefiants",  "Stupéfiants",     "💊",  new[]{"stupéfiant","drogue","cannabis","dépistage","salivaire","toxique"}),
        new("ecoconduite",  "Éco-conduite",    "🌿",  new[]{"éco","CO₂","environnement","pollution","consommation","Crit'Air","ZFE","recyclage"}),
        new("pneus",        "Pneus",           "🛞",  new[]{"pneu","pneus","pression","usure","crevaison","gonflage","sous-gonflage","adhérence"}),
        new("agglo",        "Agglomération",   "🏙️",  new[]{"agglomération","ville","urbain","zone 30","hors agglomération","traverse"}),
        new("feux-tricol",  "Feux tricolores", "🚦",  new[]{"feu tricolore","feu rouge","feu vert","orange","feu jaune","M12","carrefour"}),
        new("cyclistes",    "Cyclistes",       "🚲",  new[]{"cycliste","vélo","piste cyclable","bande cyclable","sas vélo","cyclomoteur"}),
        new("gilet",        "Gilet / Triangle","🦺",  new[]{"gilet","triangle","présignalisation","sécurité","fluorescent","réfléchissant"}),
        new("tunnel",       "Tunnel",          "🚇",  new[]{"tunnel","souterrain","issue secours","galerie"}),
        new("telephone",    "Téléphone",       "📱",  new[]{"téléphone","appel","kit mains-libres","distraction","somnolence","fatigue","pause"}),
        new("interfile",    "Interfile",       "🏍️",  new[]{"interfile","inter-files","remonter file","circulation inter-files"}),
        new("angle-mort",   "Angle mort",      "👁️",  new[]{"angle mort","rétroviseur","visibilité directe","contrôle visuel","tourner la tête"}),
        new("ceinture",     "Ceinture",        "💺",  new[]{"ceinture","airbag","siège enfant","retenue","appui-tête","dossier"}),
    };

    /// <summary>
    /// 从问题文本中提取匹配的术语代码列表。
    /// </summary>
    public static List<string> ExtractTermes(Question q)
    {
        var text = $"{q.Text} {string.Join(" ", q.Options)} {q.Explanation} {q.Category}".ToLowerInvariant();
        return All
            .Where(t => t.Keywords.Any(k => text.Contains(k.ToLowerInvariant())))
            .Select(t => t.Code)
            .ToList();
    }

    /// <summary>
    /// 获取术语显示信息。
    /// </summary>
    public static TermeDefinition? Get(string code) => All.FirstOrDefault(t => t.Code == code);
}

/// <summary>
/// 术语定义：代码、显示名称、图标、匹配关键词。
/// </summary>
public record TermeDefinition(string Code, string Name, string Icon, string[] Keywords);

/// <summary>
/// 测验结果：包含总题数、正确/错误数、得分百分比、详细结果和耗时。
/// </summary>
public class QuizResult
{
    /// <summary>测验总题数</summary>
    public int TotalQuestions { get; set; }
    /// <summary>正确答题数</summary>
    public int CorrectAnswers { get; set; }
    /// <summary>错误答题数</summary>
    public int WrongAnswers { get; set; }
    /// <summary>得分百分比（自动计算）</summary>
    public double ScorePercentage => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions * 100 : 0;
    /// <summary>每道题的详细结果</summary>
    public List<QuestionResult> Details { get; set; } = new();
    /// <summary>测验耗时</summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// 单道题的答题结果：包含题目、用户选择的答案索引、是否正确。
/// </summary>
public class QuestionResult
{
    /// <summary>对应的题目</summary>
    public Question Question { get; set; } = null!;
    /// <summary>用户选择的答案索引列表</summary>
    public List<int> UserAnswerIndices { get; set; } = new();
    /// <summary>用户答案是否正确</summary>
    public bool IsCorrect { get; set; }
}
