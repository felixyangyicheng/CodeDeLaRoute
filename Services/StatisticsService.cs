using System.Text.Json;
using CodeDeLaRoute.Models;
using Microsoft.JSInterop;

namespace CodeDeLaRoute.Services;

/// <summary>
/// 统计服务：通过浏览器 localStorage 持久化保存测验历史和全局统计。
/// </summary>
public class StatisticsService
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "cdlr_history";

    /// <summary>
    /// 构造函数，由 DI 注入 IJSRuntime 以访问浏览器 localStorage。
    /// </summary>
    public StatisticsService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// 从 localStorage 读取所有历史测验记录。
    /// </summary>
    public async Task<List<QuizHistoryEntry>> GetHistoryAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json)) return new();
            return JsonSerializer.Deserialize<List<QuizHistoryEntry>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// 保存一次测验结果到历史记录（最多保留最近 100 条）。
    /// </summary>
    /// <param name="result">测验结果</param>
    /// <param name="category">测验分类</param>
    /// <param name="questionCount">题目数量</param>
    public async Task SaveResultAsync(QuizResult result, string? category, int questionCount)
    {
        var history = await GetHistoryAsync();
        history.Add(new QuizHistoryEntry
        {
            Date = DateTime.Now,
            Category = category ?? "Toutes",
            TotalQuestions = questionCount,
            CorrectAnswers = result.CorrectAnswers,
            Score = result.ScorePercentage,
            Duration = result.Duration
        });
        if (history.Count > 100)
            history = history.OrderByDescending(h => h.Date).Take(100).ToList();
        var json = JsonSerializer.Serialize(history);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    /// <summary>
    /// 清空所有历史记录。
    /// </summary>
    public async Task ClearHistoryAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    /// <summary>
    /// 从历史记录中计算全局统计数据（总测验数、平均分、最高/最低分、分类统计等）。
    /// </summary>
    public async Task<GlobalStats> GetGlobalStatsAsync()
    {
        var history = await GetHistoryAsync();
        if (history.Count == 0)
            return new GlobalStats();

        return new GlobalStats
        {
            TotalQuizzes = history.Count,
            TotalQuestionsAnswered = history.Sum(h => h.TotalQuestions),
            TotalCorrectAnswers = history.Sum(h => h.CorrectAnswers),
            AverageScore = history.Average(h => h.Score),
            BestScore = history.Max(h => h.Score),
            WorstScore = history.Min(h => h.Score),
            TotalTime = TimeSpan.FromTicks(history.Sum(h => h.Duration.Ticks)),
            LastQuizDate = history.Max(h => h.Date),
            BestCategory = history.GroupBy(h => h.Category)
                .OrderByDescending(g => g.Average(h => h.Score))
                .First().Key,
            CategoryStats = history.GroupBy(h => h.Category)
                .Select(g => new CategoryStat
                {
                    Category = g.Key,
                    QuizCount = g.Count(),
                    AverageScore = g.Average(h => h.Score),
                    BestScore = g.Max(h => h.Score)
                }).OrderByDescending(c => c.AverageScore).ToList()
        };
    }
}

/// <summary>
/// 历史记录条目：记录单次测验的日期、分类、题数、得分和耗时。
/// </summary>
public class QuizHistoryEntry
{
    /// <summary>测验日期时间</summary>
    public DateTime Date { get; set; }
    /// <summary>测验分类</summary>
    public string Category { get; set; } = "Toutes";
    /// <summary>测验总题数</summary>
    public int TotalQuestions { get; set; }
    /// <summary>正确答题数</summary>
    public int CorrectAnswers { get; set; }
    /// <summary>得分百分比</summary>
    public double Score { get; set; }
    /// <summary>测验耗时</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>格式化的耗时显示（MM:SS）</summary>
    public string FormattedDuration => $"{(int)Duration.TotalMinutes:D2}:{Duration.Seconds:D2}";
    /// <summary>格式化的日期显示（dd/MM/yyyy HH:mm）</summary>
    public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
}

/// <summary>
/// 全局统计数据：汇总所有历史测验的统计信息。
/// </summary>
public class GlobalStats
{
    /// <summary>测验总次数</summary>
    public int TotalQuizzes { get; set; }
    /// <summary>累计答题数</summary>
    public int TotalQuestionsAnswered { get; set; }
    /// <summary>累计正确答题数</summary>
    public int TotalCorrectAnswers { get; set; }
    /// <summary>平均得分</summary>
    public double AverageScore { get; set; }
    /// <summary>历史最高分</summary>
    public double BestScore { get; set; }
    /// <summary>历史最低分</summary>
    public double WorstScore { get; set; }
    /// <summary>累计练习时间</summary>
    public TimeSpan TotalTime { get; set; }
    /// <summary>最近一次测验日期</summary>
    public DateTime LastQuizDate { get; set; }
    /// <summary>得分最高的分类</summary>
    public string BestCategory { get; set; } = "—";
    /// <summary>按分类统计的详细数据</summary>
    public List<CategoryStat> CategoryStats { get; set; } = new();
    /// <summary>全局正确率（百分比，自动计算）</summary>
    public double GlobalAccuracy => TotalQuestionsAnswered > 0
        ? (double)TotalCorrectAnswers / TotalQuestionsAnswered * 100
        : 0;
    /// <summary>格式化的总练习时间（Hh MMm）</summary>
    public string FormattedTotalTime => $"{(int)TotalTime.TotalHours}h {TotalTime.Minutes:D2}m";
}

/// <summary>
/// 分类统计：单个分类的测验次数、平均分和最高分。
/// </summary>
public class CategoryStat
{
    /// <summary>分类名称</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>该分类的测验次数</summary>
    public int QuizCount { get; set; }
    /// <summary>该分类的平均得分</summary>
    public double AverageScore { get; set; }
    /// <summary>该分类的历史最高分</summary>
    public double BestScore { get; set; }
}
