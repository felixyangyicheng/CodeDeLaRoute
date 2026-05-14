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
