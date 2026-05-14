using Microsoft.AspNetCore.Components;

namespace CodeDeLaRoute;

/// <summary>
/// NavigationManager 扩展方法：提供从 URL 查询字符串中读取参数值的功能。
/// </summary>
public static class NavigationManagerExtensions
{
    /// <summary>
    /// 从当前 URL 的查询字符串中获取指定 key 的值。
    /// 如果 key 不存在，返回 null。
    /// </summary>
    /// <param name="nav">NavigationManager 实例</param>
    /// <param name="key">查询参数名</param>
    /// <returns>参数值（URL 解码后），或 null</returns>
    public static string? GetQueryParam(this NavigationManager nav, string key)
    {
        var uri = new Uri(nav.Uri);
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query)) return null;

        foreach (var pair in query.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]) == key)
                return Uri.UnescapeDataString(parts[1]);
        }
        return null;
    }
}
