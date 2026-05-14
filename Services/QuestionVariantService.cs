using CodeDeLaRoute.Models;

namespace CodeDeLaRoute.Services;

/// <summary>
/// 题目变体生成服务：通过算法策略将一道基础题扩展为多道变体题。
/// 优先使用 JSON 中预定义的手工变体，否则使用 5 种算法策略自动生成。
/// </summary>
public class QuestionVariantService
{
    private readonly Random _rng = new();

    /// <summary>
    /// 将一道题目扩展为最多 totalVariants 道（含原题）。
    /// 如果有预定义变体则优先使用，否则调用算法生成。
    /// </summary>
    /// <param name="baseQuestion">基础题目</param>
    /// <param name="totalVariants">期望的变体总数（含原题）</param>
    /// <returns>原题 + 变体的列表</returns>
    public List<Question> Expand(Question baseQuestion, int totalVariants = 5)
    {
        if (baseQuestion.Variants.Count > 0)
        {
            var result = new List<Question> { baseQuestion };
            result.AddRange(baseQuestion.Variants.Take(totalVariants - 1));
            return result;
        }

        return GenerateVariants(baseQuestion, totalVariants);
    }

    /// <summary>
    /// 执行算法变体生成：依次尝试 5 种策略，每种策略最多产生一道变体。
    /// </summary>
    /// <param name="baseQ">基础题目</param>
    /// <param name="count">目标变体总数</param>
    private List<Question> GenerateVariants(Question baseQ, int count)
    {
        var pool = new List<Question> { baseQ };
        var strategies = new List<Func<Question, Question?>>
        {
            ShuffleOptions,
            NegateQuestion,
            ChangeDistractors,
            SwapRole,
            ChangeNumerics
        };

        var shuffledStrategies = strategies.OrderBy(_ => _rng.Next()).ToList();

        foreach (var strategy in shuffledStrategies)
        {
            if (pool.Count >= count) break;
            var variant = strategy(baseQ);
            if (variant != null && !pool.Any(q => q.Text == variant.Text))
            {
                variant.Id = baseQ.Id * 1000 + pool.Count;
                variant.Category = baseQ.Category;
                variant.Difficulty = baseQ.Difficulty;
                variant.ImageUrl = baseQ.ImageUrl;
                variant.Explanation = baseQ.Explanation;
                pool.Add(variant);
            }
        }

        return pool;
    }

    /// <summary>
    /// 策略1：随机打乱选项顺序，并重新映射正确答案的索引。
    /// </summary>
    private Question? ShuffleOptions(Question q)
    {
        if (q.Options.Count < 2) return null;

        var indices = Enumerable.Range(0, q.Options.Count).OrderBy(_ => _rng.Next()).ToArray();
        var newOptions = indices.Select(i => q.Options[i]).ToList();
        var newCorrect = q.CorrectAnswerIndices.Select(i => Array.IndexOf(indices, i)).OrderBy(x => x).ToList();

        return new Question
        {
            Text = q.Text,
            Options = newOptions,
            CorrectAnswerIndices = newCorrect
        };
    }

    /// <summary>
    /// 策略2：否定式变体——将问题改为"哪项是错误的？"，正确答案变为一个错误选项。
    /// 仅适用于单选题。
    /// </summary>
    private Question? NegateQuestion(Question q)
    {
        if (q.CorrectAnswerIndices.Count == q.Options.Count) return null;
        if (q.IsMultipleChoice) return null;

        var negatedText = q.Text
            .Replace("Que signifie", "Laquelle n'est PAS")
            .Replace("Quelle est", "Laquelle n'est PAS")
            .Replace("Quel est", "Lequel n'est PAS")
            .Replace("vous devez", "vous ne devez PAS")
            .Replace("il faut", "il ne faut PAS");

        if (negatedText == q.Text)
            negatedText = "Laquelle de ces propositions est INCORRECTE ? " + q.Text;

        var wrongIndices = Enumerable.Range(0, q.Options.Count)
            .Where(i => !q.CorrectAnswerIndices.Contains(i)).ToList();
        if (wrongIndices.Count == 0) return null;

        var newCorrect = new List<int> { wrongIndices[_rng.Next(wrongIndices.Count)] };

        return new Question
        {
            Text = negatedText,
            Options = q.Options.ToList(),
            CorrectAnswerIndices = newCorrect
        };
    }

    /// <summary>
    /// 策略3：替换干扰项——将其中一个错误选项替换为似是而非的备选文本。
    /// </summary>
    private Question? ChangeDistractors(Question q)
    {
        if (q.Options.Count < 3) return null;

        var wrongIndices = Enumerable.Range(0, q.Options.Count)
            .Where(i => !q.CorrectAnswerIndices.Contains(i)).ToList();
        if (wrongIndices.Count == 0) return null;

        var newOptions = q.Options.ToList();
        var targetIdx = wrongIndices[_rng.Next(wrongIndices.Count)];

        var replacements = new[] { "Aucune de ces réponses", "Toutes ces réponses", "Cela dépend de la situation", "Uniquement de nuit", "Sur autoroute uniquement", "En agglomération uniquement", "Sous la pluie", "Par temps sec" };
        newOptions[targetIdx] = replacements[_rng.Next(replacements.Length)];

        return new Question
        {
            Text = q.Text,
            Options = newOptions,
            CorrectAnswerIndices = q.CorrectAnswerIndices.ToList()
        };
    }

    /// <summary>
    /// 策略4：角色互换——将"你应该"改为"对方应该"，从另一方视角提问。
    /// 仅适用于优先权/安全/超车类题目。
    /// </summary>
    private Question? SwapRole(Question q)
    {
        if (!q.Category.Contains("Priorité") && !q.Category.Contains("Sécurité") && !q.Category.Contains("Dépassement"))
            return null;

        var newText = q.Text
            .Replace("vous devez", "l'autre conducteur doit")
            .Replace("vous êtes", "un autre véhicule est")
            .Replace("Vous", "L'autre usager");

        if (newText == q.Text) return null;

        return new Question
        {
            Text = newText + " ?",
            Options = q.Options.ToList(),
            CorrectAnswerIndices = q.CorrectAnswerIndices.ToList()
        };
    }

    /// <summary>
    /// 策略5：数值扰动——对包含数字的选项在 ±20 范围内随机调整。
    /// 仅适用于包含"km/h"等单位的数值型题目。
    /// </summary>
    private Question? ChangeNumerics(Question q)
    {
        var hasNumbers = q.Options.Any(o => o.Any(char.IsDigit));
        if (!hasNumbers) return null;

        var newOptions = q.Options.Select(opt =>
        {
            if (opt.Contains("km/h"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(opt, @"\d+");
                if (match.Success)
                {
                    var val = int.Parse(match.Value);
                    var adjust = _rng.Next(-20, 21);
                    var newVal = Math.Max(10, val + adjust);
                    return opt.Replace(match.Value, newVal.ToString());
                }
            }
            return opt;
        }).ToList();

        if (newOptions.SequenceEqual(q.Options)) return null;

        return new Question
        {
            Text = q.Text + " (valeurs modifiées)",
            Options = newOptions,
            CorrectAnswerIndices = q.CorrectAnswerIndices.ToList()
        };
    }
}
