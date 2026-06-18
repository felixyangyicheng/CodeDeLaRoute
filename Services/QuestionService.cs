using System.Net.Http.Json;
using System.Text.Json;
using CodeDeLaRoute.Models;

namespace CodeDeLaRoute.Services;

/// <summary>
/// 题库服务：负责从 JSON 文件加载 Auto/Moto 题目，缓存到内存，
/// 并通过 QuestionVariantService 将每道基础题扩展为最多 5 道变体题。
/// </summary>
public class QuestionService
{
    private readonly HttpClient _http;
    private readonly QuestionVariantService _variantService;
    private readonly Dictionary<string, List<Question>> _cache = new();
    private readonly Random _random = new();

    /// <summary>JSON 反序列化选项：属性名不区分大小写</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 构造函数，由 DI 容器注入 HttpClient 和变体服务。
    /// </summary>
    /// <param name="http">用于加载 JSON 的 HttpClient</param>
    /// <param name="variantService">题目变体生成服务</param>
    public QuestionService(HttpClient http, QuestionVariantService variantService)
    {
        _http = http;
        _variantService = variantService;
    }

    /// <summary>
    /// 从 JSON 加载指定车辆类型的基础题目并缓存。
    /// 首次调用时同时加载基础题库和补充题库。
    /// </summary>
    /// <param name="vehicle">车辆类型："auto" 或 "moto"</param>
    /// <returns>该车辆类型的所有基础题目列表</returns>
    private async Task<List<Question>> GetQuestionsAsync(string vehicle = "auto")
    {
        if (_cache.TryGetValue(vehicle, out var cached))
            return cached;

        var baseFile = vehicle == "moto" ? "data/questions_moto.json" : "data/questions.json";
        var questions = await _http.GetFromJsonAsync<List<Question>>(baseFile, JsonOptions)
                        ?? new List<Question>();

        var suppFile = vehicle == "moto" ? "data/questions_moto_supplement.json" : "data/questions_auto_supplement.json";
        try
        {
            var supplement = await _http.GetFromJsonAsync<List<Question>>(suppFile, JsonOptions);
            if (supplement != null)
                questions.AddRange(supplement);
        }
        catch
        {
            // 补充题库文件可能不存在，忽略即可
        }

        _cache[vehicle] = questions;
        return questions;
    }

    /// <summary>
    /// 异步获取指定车辆类型的所有分类名称（去重排序）。
    /// </summary>
    /// <param name="vehicle">车辆类型："auto" 或 "moto"</param>
    public async Task<List<string>> GetCategoriesAsync(string vehicle = "auto")
    {
        var questions = await GetQuestionsAsync(vehicle);
        return questions.Select(q => q.Category).Distinct().OrderBy(c => c).ToList();
    }

    /// <summary>
    /// 同步获取分类（仅在缓存已加载后使用，否则返回空列表）。
    /// </summary>
    /// <param name="vehicle">车辆类型："auto" 或 "moto"</param>
    public List<string> GetCategories(string vehicle = "auto")
    {
        return _cache.TryGetValue(vehicle, out var questions)
            ? questions.Select(q => q.Category).Distinct().OrderBy(c => c).ToList()
            : new List<string>();
    }

    /// <summary>
    /// 异步生成随机测验：从基础题库中筛选，经变体服务扩展后随机抽取指定数量。
    /// </summary>
    /// <param name="count">题目数量</param>
    /// <param name="category">可选的分类筛选</param>
    /// <param name="vehicle">车辆类型："auto" 或 "moto"</param>
    public async Task<List<Question>> GetRandomQuizAsync(int count = 10, string? category = null, string vehicle = "auto")
    {
        var baseQuestions = await GetQuestionsAsync(vehicle);
        var source = category != null
            ? baseQuestions.Where(q => q.Category == category).ToList()
            : baseQuestions;

        var expanded = source.SelectMany(q => _variantService.Expand(q, 5)).ToList();
        return expanded.OrderBy(_ => _random.Next()).Take(Math.Min(count, expanded.Count)).ToList();
    }

    /// <summary>
    /// 生成术语专项练习：筛选包含指定术语的题目。
    /// </summary>
    /// <param name="count">题目数量</param>
    /// <param name="termeCode">术语代码（如 "vitesse"、"priorite"）</param>
    /// <param name="vehicle">车辆类型："auto" 或 "moto"</param>
    public async Task<List<Question>> GetTermeQuizAsync(int count, string termeCode, string vehicle = "auto")
    {
        var baseQuestions = await GetQuestionsAsync(vehicle);
        var expanded = baseQuestions
            .SelectMany(q => _variantService.Expand(q, 5))
            .Where(q => q.Termes.Contains(termeCode))
            .ToList();

        return expanded.Count == 0
            ? new List<Question>()
            : expanded.OrderBy(_ => _random.Next()).Take(Math.Min(count, expanded.Count)).ToList();
    }

    /// <summary>
    /// 生成多术语练习：筛选包含任一指定术语的题目。
    /// </summary>
    public async Task<List<Question>> GetMultiTermeQuizAsync(int count, List<string> termeCodes, string vehicle = "auto")
    {
        var baseQuestions = await GetQuestionsAsync(vehicle);
        var codeSet = termeCodes.ToHashSet();
        var expanded = baseQuestions
            .SelectMany(q => _variantService.Expand(q, 5))
            .Where(q => q.Termes.Any(t => codeSet.Contains(t)))
            .ToList();

        return expanded.Count == 0
            ? new List<Question>()
            : expanded.OrderBy(_ => _random.Next()).Take(Math.Min(count, expanded.Count)).ToList();
    }

    /// <summary>
    /// 获取指定车辆类型可用的所有术语及其题目数量。
    /// </summary>
    public async Task<List<(string Code, string Name, string Icon, int Count)>> GetAvailableTermesAsync(string vehicle = "auto")
    {
        var baseQuestions = await GetQuestionsAsync(vehicle);
        var expanded = baseQuestions.SelectMany(q => _variantService.Expand(q, 5)).ToList();

        return TermesGlossaire.All
            .Select(t => (t.Code, t.Name, t.Icon, Count: expanded.Count(q => q.Termes.Contains(t.Code))))
            .Where(t => t.Count > 0)
            .OrderByDescending(t => t.Count)
            .ToList();
    }

    /// <summary>
    /// 生成符合 ETG 官方主题分布的模拟考试（40 题）。
    /// 按官方 10 个主题各自的数量配额，从对应主题中随机抽取。
    /// </summary>
    /// <param name="vehicle">车辆类型："auto" 或 "moto"</param>
    public async Task<List<Question>> GetExamQuizAsync(string vehicle = "auto")
    {
        var baseQuestions = await GetQuestionsAsync(vehicle);
        var expanded = baseQuestions
            .SelectMany(q => _variantService.Expand(q, 5))
            .GroupBy(q => q.ThemeCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<Question>();
        foreach (var (code, name, quota, icon) in EtgThemes.All)
        {
            if (expanded.TryGetValue(code, out var pool) && pool.Count > 0)
            {
                var taken = pool.OrderBy(_ => _random.Next()).Take(Math.Min(quota, pool.Count)).ToList();
                result.AddRange(taken);
            }
        }

        // Fill remaining up to 40 if some themes lacked questions
        if (result.Count < EtgThemes.TotalQuestions)
        {
            var remaining = EtgThemes.TotalQuestions - result.Count;
            var allExpanded = baseQuestions.SelectMany(q => _variantService.Expand(q, 5)).ToList();
            var usedIds = result.Select(q => q.Id).ToHashSet();
            var fillers = allExpanded
                .Where(q => !usedIds.Contains(q.Id))
                .OrderBy(_ => _random.Next())
                .Take(remaining)
                .ToList();
            result.AddRange(fillers);
        }

        return result.OrderBy(_ => _random.Next()).ToList();
    }

    /// <summary>
    /// 同步生成随机测验（缓存已加载时使用，否则返回空列表）。
    /// </summary>
    public List<Question> GetRandomQuiz(int count = 10, string? category = null, string vehicle = "auto")
    {
        if (!_cache.TryGetValue(vehicle, out var baseQuestions)) return new();
        var source = category != null
            ? baseQuestions.Where(q => q.Category == category).ToList()
            : baseQuestions;

        var expanded = source.SelectMany(q => _variantService.Expand(q, 5)).ToList();
        return expanded.OrderBy(_ => _random.Next()).Take(Math.Min(count, expanded.Count)).ToList();
    }

    /// <summary>
    /// 异步获取所有题目（含变体），用于学习模式。
    /// </summary>
    public async Task<List<Question>> GetAllQuestionsAsync(string vehicle = "auto")
    {
        var baseQuestions = await GetQuestionsAsync(vehicle);
        return baseQuestions.SelectMany(q => _variantService.Expand(q, 5)).ToList();
    }

    /// <summary>
    /// 同步获取所有题目（缓存已加载时使用）。
    /// </summary>
    public List<Question> GetAllQuestions(string vehicle = "auto")
    {
        if (!_cache.TryGetValue(vehicle, out var baseQuestions)) return new();
        return baseQuestions.SelectMany(q => _variantService.Expand(q, 5)).ToList();
    }
}
