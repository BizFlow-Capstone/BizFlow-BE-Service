using System.Text.Json;

namespace BizFlow.Application.Services;

/// <summary>
/// Generates human-readable Vietnamese text from ExpressionJson AST.
/// </summary>
public static class FormulaExplainer
{
    public static string Explain(string? expressionJson)
    {
        if (string.IsNullOrWhiteSpace(expressionJson))
            return "(chưa có công thức)";

        try
        {
            using var doc = JsonDocument.Parse(expressionJson);
            return ExplainNode(doc.RootElement);
        }
        catch
        {
            return "(không thể phân tích công thức)";
        }
    }

    private static string ExplainNode(JsonElement node)
    {
        // 1. Literal
        if (node.TryGetProperty("literal", out var literal))
        {
            return FormatNumber(literal);
        }

        // 2. Ref
        if (node.TryGetProperty("ref", out var refNode))
        {
            var code = refNode.GetString() ?? "?";
            return $"= giá trị của [{code}]";
        }

        // 3. Aggregate
        if (node.TryGetProperty("aggregate", out var aggNode))
        {
            var aggType = aggNode.GetString() ?? "SUM";
            var source = node.TryGetProperty("source", out var s) ? s.GetString() : "?";
            var field = node.TryGetProperty("field", out var f) ? f.GetString() : "?";

            var aggLabel = aggType.ToUpper() switch
            {
                "SUM" => "Tổng cộng",
                "AVG" => "Trung bình",
                "COUNT" => "Đếm",
                _ => aggType
            };

            var explanation = $"{aggLabel} ({aggType}) cột {field} từ bảng {source}";

            if (node.TryGetProperty("filter", out var filterNode))
            {
                var filters = new List<string>();
                foreach (var prop in filterNode.EnumerateObject())
                {
                    var val = prop.Value.ValueKind == JsonValueKind.Array
                        ? string.Join(", ", prop.Value.EnumerateArray().Select(v => v.GetString()))
                        : prop.Value.GetString() ?? "";
                    filters.Add($"{prop.Name} = {val}");
                }
                if (filters.Count > 0)
                    explanation += $", lọc theo {string.Join(" & ", filters)}";
            }

            return explanation;
        }

        // 4. Lookup
        if (node.TryGetProperty("lookup", out var lookupNode))
        {
            var entity = lookupNode.TryGetProperty("entity", out var e) ? e.GetString() : "?";
            var field = lookupNode.TryGetProperty("field", out var f) ? f.GetString() : "?";

            var explanation = $"Tra cứu {field} từ bảng {entity}";

            if (lookupNode.TryGetProperty("filter", out var filterNode))
            {
                var filters = new List<string>();
                foreach (var prop in filterNode.EnumerateObject())
                    filters.Add($"{prop.Name} = {prop.Value.GetString() ?? "?"}");
                if (filters.Count > 0)
                    explanation += $" (điều kiện: {string.Join(", ", filters)})";
            }

            return explanation;
        }

        // 5. Op
        if (node.TryGetProperty("op", out var opNode))
        {
            var op = opNode.GetString()?.ToUpper() ?? "?";
            var left = node.TryGetProperty("left", out var l) ? ExplainNode(l) : "?";
            var right = node.TryGetProperty("right", out var r) ? ExplainNode(r) : "?";

            var opSymbol = op switch
            {
                "ADD" => "+",
                "SUBTRACT" => "−",
                "MULTIPLY" => "×",
                "DIVIDE" => "÷",
                _ => op
            };

            return $"({left}) {opSymbol} ({right})";
        }

        // 6. Function
        if (node.TryGetProperty("fn", out var fnNode))
        {
            var fn = fnNode.GetString()?.ToUpper() ?? "?";
            var args = new List<string>();
            if (node.TryGetProperty("args", out var argsNode))
            {
                foreach (var arg in argsNode.EnumerateArray())
                    args.Add(ExplainNode(arg));
            }

            return $"{fn}({string.Join(", ", args)})";
        }

        // 7. Context
        if (node.TryGetProperty("context", out var ctxNode))
        {
            var key = ctxNode.GetString() ?? "?";
            var label = key switch
            {
                "period_start" => "ngày bắt đầu kỳ",
                "period_end" => "ngày kết thúc kỳ",
                "business_type" => "loại ngành nghề",
                _ => key
            };
            return $"[{label}]";
        }

        return "(node không xác định)";
    }

    private static string FormatNumber(JsonElement el)
    {
        if (el.TryGetDecimal(out var d))
            return d.ToString("N0");
        return el.GetRawText();
    }
}
