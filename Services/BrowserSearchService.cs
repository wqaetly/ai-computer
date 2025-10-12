using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace AiComputer.Services;

/// <summary>
/// 基于浏览器的搜索服务（使用 PuppeteerSharp）
/// </summary>
public class BrowserSearchService : ISearchService, IDisposable
{
    private IBrowser? _browser;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _pageLock = new(3, 3); // 最多3个并发页面
    private bool _disposed;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly Random _random = new();

    public string ServiceName => "Browser Search (PuppeteerSharp)";

    /// <summary>
    /// 搜索引擎类型
    /// </summary>
    public enum SearchEngine
    {
        Bing,
        Baidu
    }

    /// <summary>
    /// 当前使用的搜索引擎
    /// </summary>
    public SearchEngine CurrentEngine { get; set; } = SearchEngine.Baidu;

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            return _browser != null && _browser.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 确保浏览器已初始化
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_initialized && _browser != null && _browser.IsConnected)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized && _browser != null && _browser.IsConnected)
                return;

            Console.WriteLine("[BrowserSearchService] 正在初始化 PuppeteerSharp...");

            // 获取本地 Chrome 可执行文件路径
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var chromeExePath = Path.Combine(baseDir, "Assets", "chrome-win64", "chrome.exe");

            // 检查 Chrome 是否存在
            if (!File.Exists(chromeExePath))
            {
                throw new FileNotFoundException(
                    $"未找到 Chrome 浏览器，请确保 Chrome 已放置在以下路径: {chromeExePath}");
            }

            Console.WriteLine($"[BrowserSearchService] 使用本地 Chrome: {chromeExePath}");

            // 启动浏览器（添加反检测参数）
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true, // 无头模式
                ExecutablePath = chromeExePath, // 使用本地 Chrome
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--disable-web-security", // 禁用 CORS 检查
                    "--lang=zh-CN", // 设置语言为中文
                    // 反检测参数
                    "--disable-blink-features=AutomationControlled", // 隐藏自动化控制标记
                    "--disable-features=IsolateOrigins,site-per-process", // 禁用站点隔离
                    "--disable-site-isolation-trials",
                    "--disable-web-security",
                    "--disable-features=VizDisplayCompositor"
                }
            });

            _initialized = true;
            Console.WriteLine("[BrowserSearchService] 浏览器初始化完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BrowserSearchService] 初始化失败: {ex.Message}");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResult>();

        await EnsureInitializedAsync();

        if (_browser == null || !_browser.IsConnected)
            throw new InvalidOperationException("浏览器未初始化或已断开连接");

        await _pageLock.WaitAsync(cancellationToken);
        IPage? page = null;

        try
        {
            // 创建新页面
            page = await _browser.NewPageAsync();

            // 启用控制台消息监听（捕获 JavaScript console.log 输出）
            page.Console += (sender, e) =>
            {
                Console.WriteLine($"[Browser Console] {e.Message.Type}: {e.Message.Text}");
            };

            // 【关键】注入反检测 JavaScript 代码（在页面加载任何内容之前执行）
            // PuppeteerSharp 使用 EvaluateExpressionOnNewDocumentAsync
            await page.EvaluateExpressionOnNewDocumentAsync(@"
                // 隐藏 webdriver 标记
                Object.defineProperty(navigator, 'webdriver', {
                    get: () => undefined
                });

                // 修改 plugins 长度，模拟真实浏览器
                Object.defineProperty(navigator, 'plugins', {
                    get: () => [1, 2, 3, 4, 5]
                });

                // 修改 languages，使其更真实
                Object.defineProperty(navigator, 'languages', {
                    get: () => ['zh-CN', 'zh', 'en-US', 'en']
                });

                // 添加 chrome 对象（Puppeteer 默认没有）
                window.chrome = {
                    runtime: {}
                };

                // 修改 permissions 查询
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({ state: Notification.permission }) :
                        originalQuery(parameters)
                );

                // 伪装 permissions
                Object.defineProperty(navigator, 'permissions', {
                    get: () => ({
                        query: originalQuery
                    })
                });
            ");

            // 设置视口大小（模拟桌面浏览器）
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            });

            // 设置用户代理
            await page.SetUserAgentAsync(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // 根据搜索引擎类型执行搜索
            return CurrentEngine switch
            {
                SearchEngine.Bing => await SearchBingAsync(page, query, maxResults, cancellationToken),
                SearchEngine.Baidu => await SearchBaiduAsync(page, query, maxResults, cancellationToken),
                _ => throw new NotSupportedException($"不支持的搜索引擎: {CurrentEngine}")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BrowserSearchService] 搜索失败: {ex.Message}");
            throw;
        }
        finally
        {
            // 关闭页面
            if (page != null)
            {
                await page.CloseAsync();
            }
            _pageLock.Release();
        }
    }


    /// <summary>
    /// Bing 搜索
    /// </summary>
    private async Task<List<SearchResult>> SearchBingAsync(
        IPage page,
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        // 【优化】添加请求间的随机延迟（模拟真实用户行为）
        var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
        if (timeSinceLastRequest.TotalSeconds < 2)
        {
            var delayMs = _random.Next(800, 2000); // 0.8-2秒随机延迟
            Console.WriteLine($"[BrowserSearchService] 等待 {delayMs}ms 后发起请求...");
            await Task.Delay(delayMs, cancellationToken);
        }
        _lastRequestTime = DateTime.UtcNow;

        var searchUrl = $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}&ensearch=1";
        Console.WriteLine($"[BrowserSearchService] 正在访问 Bing: {searchUrl}");

        await page.GoToAsync(searchUrl, new NavigationOptions
        {
            Timeout = 30000,
            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
        });

        // 【优化】增加等待时间，确保 JavaScript 完全执行
        await Task.Delay(300, cancellationToken);

        // 等待搜索结果
        try
        {
            await page.WaitForSelectorAsync("#b_results", new WaitForSelectorOptions { Timeout = 10000 });
        }
        catch
        {
            Console.WriteLine("[BrowserSearchService] 等待搜索结果超时");
        }

        // 调试：输出页面信息
        Console.WriteLine($"[BrowserSearchService] Bing 页面标题: {await page.GetTitleAsync()}");
        Console.WriteLine($"[BrowserSearchService] 当前 URL: {page.Url}");

        // 提取结果（改进版）
        // 直接返回对象数组，无需 JSON.stringify
        var results = await page.EvaluateFunctionAsync<SearchResult[]>($@"
            () => {{
                const results = [];
                let items = document.querySelectorAll('#b_results .b_algo');
                console.log('Found .b_algo items:', items.length);

                if (items.length === 0) {{
                    items = document.querySelectorAll('li.b_algo');
                    console.log('Found li.b_algo items:', items.length);
                }}

                for (let i = 0; i < Math.min(items.length, {maxResults}); i++) {{
                    const item = items[i];
                    let linkEl = item.querySelector('h2 a');
                    if (!linkEl) linkEl = item.querySelector('a[href]');

                    let snippetEl = item.querySelector('.b_caption p');
                    if (!snippetEl) snippetEl = item.querySelector('.b_algoSlug');
                    if (!snippetEl) snippetEl = item.querySelector('.b_paractl');

                    console.log('Bing Item', i, ':', {{
                        hasLink: !!linkEl,
                        href: linkEl?.href,
                        title: linkEl?.innerText
                    }});

                    if (linkEl && linkEl.href && linkEl.innerText) {{
                        try {{
                            const url = new URL(linkEl.href);
                            if (!url.hostname.includes('bing.com')) {{
                                results.push({{
                                    title: linkEl.innerText.trim(),
                                    url: linkEl.href,
                                    snippet: snippetEl ? snippetEl.innerText.trim() : '无摘要',
                                    source: url.hostname
                                }});
                            }}
                        }} catch (e) {{
                            console.error('URL parsing error:', e);
                        }}
                    }}
                }}

                console.log('Total Bing results:', results.length);
                return results;
            }}
        ");

        var resultList = results?.ToList() ?? new List<SearchResult>();
        Console.WriteLine($"[BrowserSearchService] Bing 返回 {resultList.Count} 个结果");
        return resultList;
    }

    /// <summary>
    /// Baidu 搜索
    /// </summary>
    private async Task<List<SearchResult>> SearchBaiduAsync(
        IPage page,
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var searchUrl = $"https://www.baidu.com/s?wd={Uri.EscapeDataString(query)}";
        Console.WriteLine($"[BrowserSearchService] 正在访问 Baidu: {searchUrl}");

        await page.GoToAsync(searchUrl, new NavigationOptions
        {
            Timeout = 30000,
            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
        });

        // 等待搜索结果
        try
        {
            await page.WaitForSelectorAsync("#content_left", new WaitForSelectorOptions { Timeout = 10000 });
        }
        catch
        {
            Console.WriteLine("[BrowserSearchService] 等待搜索结果超时");
        }

        // 调试：输出页面信息
        Console.WriteLine($"[BrowserSearchService] Baidu 页面标题: {await page.GetTitleAsync()}");
        Console.WriteLine($"[BrowserSearchService] 当前 URL: {page.Url}");

        // 提取结果（参考 cherry-studio 实现）
        // 百度搜索结果使用 .result 类，而不是 .c-container
        var results = await page.EvaluateFunctionAsync<SearchResult[]>($@"
            () => {{
                const results = [];

                // 使用 cherry-studio 的选择器：#content_left .result h3
                let h3Items = document.querySelectorAll('#content_left .result h3');
                console.log('Found .result h3 items:', h3Items.length);

                // 如果没找到，尝试备用选择器
                if (h3Items.length === 0) {{
                    h3Items = document.querySelectorAll('#content_left .c-container h3');
                    console.log('Fallback: Found .c-container h3 items:', h3Items.length);
                }}

                // 如果还没找到，尝试其他备用选择器
                if (h3Items.length === 0) {{
                    h3Items = document.querySelectorAll('#content_left h3');
                    console.log('Fallback: Found #content_left h3 items:', h3Items.length);
                }}

                for (let i = 0; i < Math.min(h3Items.length, {maxResults}); i++) {{
                    const h3 = h3Items[i];

                    // 从 h3 中提取 a 标签
                    const linkEl = h3.querySelector('a');

                    console.log('Baidu Item', i, ':', {{
                        hasLink: !!linkEl,
                        href: linkEl?.href,
                        title: linkEl?.textContent
                    }});

                    if (linkEl && linkEl.href && linkEl.textContent) {{
                        try {{
                            const url = new URL(linkEl.href);
                            const href = linkEl.href;

                            // 调试：打印链接信息
                            console.log('Link URL:', href, 'Hostname:', url.hostname);

                            // 过滤逻辑：只过滤明确的百度内部页面，保留跳转链接
                            // 百度跳转链接通常是 /link?url= 格式，应该被保留
                            const isBaiduInternalPage = url.hostname.includes('baidu.com') && (
                                url.pathname === '/' ||                    // 首页
                                url.pathname === '/s' ||                   // 搜索页
                                url.pathname.startsWith('/baike/') ||      // 百度百科
                                url.pathname.startsWith('/zhidao/') ||     // 百度知道
                                url.pathname.startsWith('/tieba/') ||      // 百度贴吧
                                url.pathname.startsWith('/map/') ||        // 百度地图
                                url.pathname.startsWith('/fanyi')          // 百度翻译
                            );

                            // 只有非内部页面才添加到结果
                            if (!isBaiduInternalPage) {{
                                // 尝试获取摘要 - 在 .result 容器内查找
                                let snippetEl = null;

                                // 向上查找 .result 容器
                                let resultContainer = h3.closest('.result');
                                if (!resultContainer) resultContainer = h3.closest('.c-container');
                                if (!resultContainer) resultContainer = h3.closest('[class*=""result""]');

                                // 调试：输出容器的 HTML（前500字符）
                                if (resultContainer) {{
                                    const containerHtml = resultContainer.outerHTML.substring(0, 500);
                                    console.log('Result container HTML:', containerHtml);
                                }}

                                // 在容器内查找摘要，尝试多种选择器
                                if (resultContainer) {{
                                    // 尝试常见的摘要选择器
                                    snippetEl = resultContainer.querySelector('.c-abstract');
                                    if (!snippetEl) snippetEl = resultContainer.querySelector('.c-span18');
                                    if (!snippetEl) snippetEl = resultContainer.querySelector('.content-right_8Zs40');
                                    if (!snippetEl) snippetEl = resultContainer.querySelector('[class*=""abstract""]');
                                    if (!snippetEl) snippetEl = resultContainer.querySelector('.c-font-normal');
                                    if (!snippetEl) snippetEl = resultContainer.querySelector('[class*=""content""]');

                                    // 如果还是没找到，尝试查找任何段落或 span
                                    if (!snippetEl) {{
                                        const allSpans = resultContainer.querySelectorAll('span');
                                        for (let span of allSpans) {{
                                            // 查找内容较长的 span（可能是摘要）
                                            if (span.textContent && span.textContent.length > 20) {{
                                                snippetEl = span;
                                                break;
                                            }}
                                        }}
                                    }}

                                    console.log('Snippet found:', !!snippetEl, snippetEl ? snippetEl.textContent.substring(0, 50) : 'none');
                                }}

                                results.push({{
                                    title: linkEl.textContent.trim(),
                                    url: linkEl.href,
                                    snippet: snippetEl ? snippetEl.textContent.trim() : '无摘要',
                                    source: url.hostname
                                }});
                            }} else {{
                                console.log('Filtered as internal page:', href);
                            }}
                        }} catch (e) {{
                            console.error('URL parsing error:', e);
                        }}
                    }}
                }}

                console.log('Total Baidu results:', results.length);
                return results;
            }}
        ");

        var resultList = results?.ToList() ?? new List<SearchResult>();
        Console.WriteLine($"[BrowserSearchService] Baidu 返回 {resultList.Count} 个结果");
        return resultList;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_browser != null)
        {
            _browser.CloseAsync().GetAwaiter().GetResult();
            _browser.Dispose();
        }

        _initLock.Dispose();
        _pageLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
