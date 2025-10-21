using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiComputer.Models;

namespace AiComputer.Services;

/// <summary>
/// 聊天存档服务 - 负责保存和加载对话记录
/// </summary>
public class ChatArchiveService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true, // 格式化输出，方便阅读
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 支持中文字符
    };

    /// <summary>
    /// 将会话列表保存到JSON文件
    /// </summary>
    /// <param name="sessions">会话列表</param>
    /// <param name="filePath">文件路径</param>
    public async Task SaveToFileAsync(IEnumerable<ChatSession> sessions, string filePath)
    {
        try
        {
            // 转换为可序列化的数据模型
            var archive = new ChatArchive
            {
                Version = "1.0",
                CreatedAt = DateTime.Now,
                Sessions = sessions.Select(ConvertToSessionData).ToList()
            };

            // 序列化为JSON
            var json = JsonSerializer.Serialize(archive, _jsonOptions);

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 写入文件
            await File.WriteAllTextAsync(filePath, json);

            Console.WriteLine($"[ChatArchive] 存档已保存到: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatArchive] 保存失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从JSON文件加载会话列表
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>会话列表</returns>
    public async Task<List<ChatSession>> LoadFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"存档文件不存在: {filePath}");
            }

            // 读取文件
            var json = await File.ReadAllTextAsync(filePath);

            // 反序列化
            var archive = JsonSerializer.Deserialize<ChatArchive>(json, _jsonOptions);

            if (archive == null)
            {
                throw new InvalidDataException("存档文件格式错误");
            }

            Console.WriteLine($"[ChatArchive] 已加载存档（版本: {archive.Version}，会话数: {archive.Sessions.Count}）");

            // 转换为会话对象
            return archive.Sessions.Select(ConvertToSession).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatArchive] 加载失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 将会话转换为可序列化的数据模型
    /// </summary>
    private ChatSessionData ConvertToSessionData(ChatSession session)
    {
        return new ChatSessionData
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            LastUpdated = session.LastUpdated,
            Messages = session.Messages.Select(ConvertToMessageData).ToList()
        };
    }

    /// <summary>
    /// 将消息转换为可序列化的数据模型
    /// </summary>
    private ChatMessageData ConvertToMessageData(ChatMessage message)
    {
        return new ChatMessageData
        {
            Content = message.Content,
            ReasoningContent = message.ReasoningContent,
            Role = message.Role,
            Timestamp = message.Timestamp,
            ToolCallId = message.ToolCallId,
            ToolName = message.ToolName,
            ToolArguments = message.ToolArguments,
            Status = message.Status,
            IsReasoningExpanded = message.IsReasoningExpanded,
            IsSearchResultExpanded = message.IsSearchResultExpanded
        };
    }

    /// <summary>
    /// 将数据模型转换为会话对象
    /// </summary>
    private ChatSession ConvertToSession(ChatSessionData data)
    {
        var session = new ChatSession(data.Title)
        {
            // 使用反射设置只读属性 Id 和 CreatedAt
        };

        // 设置 Id（使用反射绕过 init-only 限制）
        var idProperty = typeof(ChatSession).GetProperty(nameof(ChatSession.Id));
        idProperty?.SetValue(session, data.Id);

        // 设置 CreatedAt（使用反射绕过 init-only 限制）
        var createdAtProperty = typeof(ChatSession).GetProperty(nameof(ChatSession.CreatedAt));
        createdAtProperty?.SetValue(session, data.CreatedAt);

        // 设置 LastUpdated（这个是可写的）
        session.LastUpdated = data.LastUpdated;

        // 添加消息（先禁用集合监听，避免触发自动标题生成）
        foreach (var messageData in data.Messages)
        {
            var message = ConvertToMessage(messageData);
            session.Messages.Add(message);
        }

        return session;
    }

    /// <summary>
    /// 将数据模型转换为消息对象
    /// </summary>
    private ChatMessage ConvertToMessage(ChatMessageData data)
    {
        var message = new ChatMessage
        {
            Content = data.Content,
            ReasoningContent = data.ReasoningContent,
            Role = data.Role,
            Timestamp = data.Timestamp,
            ToolCallId = data.ToolCallId,
            ToolName = data.ToolName,
            ToolArguments = data.ToolArguments,
            Status = data.Status,
            IsStreaming = false, // 存档的消息不应处于流式传输状态
            IsReasoningExpanded = data.IsReasoningExpanded,
            IsSearchResultExpanded = data.IsSearchResultExpanded
        };

        // 将内容同步到 ContentBuilder（用于 UI 渲染）
        if (!string.IsNullOrEmpty(data.Content))
        {
            message.ContentBuilder.Append(data.Content);
        }

        if (!string.IsNullOrEmpty(data.ReasoningContent))
        {
            message.ReasoningContentBuilder.Append(data.ReasoningContent);
        }

        return message;
    }
}
