using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiComputer.Services.Tools;

/// <summary>
/// 工具执行器
/// 负责解析工具调用、执行工具、格式化结果
/// </summary>
public class ToolExecutor
{
    private readonly Dictionary<string, ITool> _tools = new();

    /// <summary>
    /// 注册工具
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// 获取所有已注册的工具
    /// </summary>
    public IReadOnlyDictionary<string, ITool> Tools => _tools;

    /// <summary>
    /// 构建系统提示词（包含工具使用说明）
    /// </summary>
    public string BuildSystemPrompt(string userSystemPrompt)
    {
        if (_tools.Count == 0)
        {
            return userSystemPrompt;
        }

        var toolsDescription = BuildToolsDescription();
        var toolUsePrompt = $@"# Available Tools

{toolsDescription}

## 🔧 Tool Usage Rules (MANDATORY)

**When to Call Tools:**
You MUST call tools when encountering ANY of the following:
- Latest prices, availability, or market data
- Product specifications released after 2024
- Performance benchmarks or reviews
- Any product, technology, or concept you're uncertain about
- Real-time or recent information (within last 6 months)

**Response Format (STRICTLY FOLLOW):**
1. First, provide a brief analysis based on general knowledge (2-3 sentences max)
2. Then, output ALL needed tool calls at the END of your response
3. Think concisely - avoid verbose explanations

**Tool Call Syntax:**
<tool_use>
  <name>tool_name</name>
  <arguments>{{json_parameters}}</arguments>
</tool_use>

**Multiple Tool Calls Example:**
For a question about ""RTX 4090 vs RX 7900 XTX"", you should output:

Based on general knowledge, both are high-end GPUs from 2023. Let me search for current pricing and benchmarks.

<tool_use>
  <name>web_search</name>
  <arguments>{{""query"": ""RTX 4090 price 2025""}}</arguments>
</tool_use>
<tool_use>
  <name>web_search</name>
  <arguments>{{""query"": ""RX 7900 XTX price 2025""}}</arguments>
</tool_use>
<tool_use>
  <name>web_search</name>
  <arguments>{{""query"": ""RTX 4090 vs RX 7900 XTX benchmark 2025""}}</arguments>
</tool_use>

**Critical Rules:**
- Place ALL tool calls at the END of response (not beginning!)
- Output multiple tool calls together (one per unknown item)
- Use valid JSON in <arguments>
- Keep thinking process minimal and focused
- Tool results will be automatically executed and provided back to you

Tool results format:
<tool_use_result>
  <name>tool_name</name>
  <result>result_data</result>
</tool_use_result>

# User Instructions
{userSystemPrompt}";

        return toolUsePrompt;
    }

    /// <summary>
    /// 构建工具描述（XML格式）
    /// </summary>
    private string BuildToolsDescription()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<tools>");

        foreach (var tool in _tools.Values)
        {
            sb.AppendLine("  <tool>");
            sb.AppendLine($"    <name>{tool.Name}</name>");
            sb.AppendLine($"    <description>{tool.Description}</description>");
            sb.AppendLine("    <arguments>");
            sb.AppendLine($"      {tool.InputSchema.RootElement.GetRawText()}");
            sb.AppendLine("    </arguments>");
            sb.AppendLine("  </tool>");
        }

        sb.AppendLine("</tools>");
        return sb.ToString();
    }

    /// <summary>
    /// 从文本中解析工具调用
    /// </summary>
    public List<ToolCall> ParseToolCalls(string text)
    {
        var results = new List<ToolCall>();

        if (string.IsNullOrWhiteSpace(text))
            return results;

        // 匹配 <tool_use>...</tool_use> 标签
        var pattern = @"<tool_use>([\s\S]*?)<name>([\s\S]*?)</name>([\s\S]*?)<arguments>([\s\S]*?)</arguments>([\s\S]*?)</tool_use>";
        var matches = Regex.Matches(text, pattern);

        var index = 0;
        foreach (Match match in matches)
        {
            var toolName = match.Groups[2].Value.Trim();
            var argumentsJson = match.Groups[4].Value.Trim();

            // 检查工具是否存在
            if (!_tools.ContainsKey(toolName))
            {
                Console.WriteLine($"[ToolExecutor] Warning: Tool '{toolName}' not found in registered tools");
                continue;
            }

            // 解析参数
            JsonElement arguments;
            try
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                arguments = doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ToolExecutor] Failed to parse arguments for tool '{toolName}': {ex.Message}");
                continue;
            }

            results.Add(new ToolCall
            {
                Id = $"{toolName}-{index++}",
                ToolName = toolName,
                Arguments = arguments,
                Status = ToolCallStatus.Pending
            });
        }

        return results;
    }

    /// <summary>
    /// 执行工具调用（并行执行以提升性能）
    /// </summary>
    public async Task<List<ToolExecutionResult>> ExecuteToolsAsync(
        List<ToolCall> toolCalls,
        CancellationToken cancellationToken = default)
    {
        if (toolCalls.Count == 0)
            return new List<ToolExecutionResult>();

        // 并行执行所有工具调用
        var executionTasks = toolCalls.Select(async toolCall =>
        {
            try
            {
                var tool = _tools[toolCall.ToolName];
                toolCall.Status = ToolCallStatus.Executing;

                var result = await tool.ExecuteAsync(toolCall.Arguments, cancellationToken);

                toolCall.Status = ToolCallStatus.Success;

                return new ToolExecutionResult
                {
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.ToolName,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ToolExecutor] Tool execution failed: {toolCall.ToolName}, Error: {ex.Message}");

                toolCall.Status = ToolCallStatus.Failed;

                return new ToolExecutionResult
                {
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.ToolName,
                    Error = ex.Message
                };
            }
        }).ToList();

        // 等待所有工具执行完成
        var results = await Task.WhenAll(executionTasks);

        return results.ToList();
    }

    /// <summary>
    /// 格式化工具执行结果为XML格式
    /// </summary>
    public string FormatToolResults(List<ToolExecutionResult> results)
    {
        var sb = new StringBuilder();

        foreach (var result in results)
        {
            if (!result.IsError)
            {
                sb.AppendLine("<tool_use_result>");
                sb.AppendLine($"  <name>{result.ToolName}</name>");
                sb.AppendLine($"  <result>{result.Result}</result>");
                sb.AppendLine("</tool_use_result>");
            }
            else
            {
                sb.AppendLine("<tool_use_result>");
                sb.AppendLine($"  <name>{result.ToolName}</name>");
                sb.AppendLine($"  <error>{result.Error}</error>");
                sb.AppendLine("</tool_use_result>");
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 从文本中移除工具调用标签（用于显示）
    /// </summary>
    public string RemoveToolUseTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // 移除 <tool_use>...</tool_use> 标签
        var pattern = @"<tool_use>[\s\S]*?</tool_use>";
        return Regex.Replace(text, pattern, string.Empty).Trim();
    }
}
