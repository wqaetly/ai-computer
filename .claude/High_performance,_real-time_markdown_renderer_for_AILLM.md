Title: High performance, real-time markdown renderer for AI/LLM

URL: https://github.com/DearVa/LiveMarkdown.Avalonia

Summary: undefined

Content:
**Table of Contents**

- 1. [👋 Introduction  👋 简介](#toc-0)
- 2. [⭐ Features  ⭐ 功能](#toc-1)
- 3. [✈️ Roadmap   ✈️ 路线图](#toc-2)
- 4. [🚀 Getting Started  🚀 入门](#toc-3)
  - 4.1. [1. Install the NuGet package1.安装 NuGet 包](#toc-4)
  - 4.2. [2. Register the Markdown styles in your Avalonia application2. 在 Avalonia 应用程序中注册 Markdown 样式](#toc-5)
  - 4.3. [3. Use the MarkdownRenderer control in your XAML3. 在 XAML 中使用 MarkdownRenderer 控件](#toc-6)
- 5. [🪄 Style Customization  🪄 风格定制](#toc-7)
- 6. [🤔 FAQ](#toc-8)
- 7. [🤝 Contributing](#toc-9)
- 8. [📄 License](#toc-10)
  - 8.1. [Third-Party Licenses](#toc-11)

[![netstandard2.0](https://camo.githubusercontent.com/e753947669e06a391703fa3626b8a8d3b600d4fc20c6978d180728d69a51f7e4/68747470733a2f2f696d672e736869656c64732e696f2f62616467652f6e65747374616e646172642d322e302d626c75652e737667)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) [![Avalonia](https://camo.githubusercontent.com/7b9b899d3c7b097e0a41042c16f4b83af8c12689c8ed15bc47ab1229f260d822/68747470733a2f2f696d672e736869656c64732e696f2f62616467652f4176616c6f6e69612d31312d626c75652e737667)](https://avaloniaui.net/) [![License](https://camo.githubusercontent.com/859a1a0bc85ce8bbd7a730a274fec5c9e77c4726ffdf6aa762a78685e26033a4/68747470733a2f2f696d672e736869656c64732e696f2f62616467652f4c6963656e73652d417061636865253230322e302d626c75652e737667)](https://github.com/DearVa/LiveMarkdown.Avalonia/blob/main/LICENSE) [![GitHub issues](https://camo.githubusercontent.com/ef50d89f7f9d44175f7d62fe8d9c2a2d16020b3639bd4d228dfebd3e78fa389c/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f6973737565732f4465617256612f4c6976654d61726b646f776e2e4176616c6f6e69612e737667)](https://github.com/DearVa/LiveMarkdown.Avalonia/issues) [![NuGet](https://camo.githubusercontent.com/98a790062fa9347c958209e347d8699b9fd60a6f68babf5f30bc15ca897b6841/68747470733a2f2f696d672e736869656c64732e696f2f6e756765742f762f4c6976654d61726b646f776e2e4176616c6f6e69612e737667)](https://www.nuget.org/packages/LiveMarkdown.Avalonia/)

[![demo.gif](https://raw.githubusercontent.com/DearVa/LiveMarkdown.Avalonia/main/img/demo.gif)](https://raw.githubusercontent.com/DearVa/LiveMarkdown.Avalonia/main/img/demo.gif)

## 👋 Introduction  👋 简介

[](#-introduction)

`LiveMarkdown.Avalonia` is a High-performance Markdown viewer for Avalonia applications. It supports **real-time rendering** of Markdown content, so it's ideal for applications that require dynamic text updating, **especially when streaming large model outputs**.  
`LiveMarkdown.Avalonia` 是一款适用于 Avalonia 应用程序的高性能 Markdown 查看器。它支持 Markdown 内容的**实时渲染** ，因此非常适合需要动态文本更新的应用程序， **尤其是在流式传输大型模型输出时** 。

## ⭐ Features  ⭐ 功能

[](#-features)

- 🚀 **High-performance rendering powered by [Markdig](https://github.com/xoofx/markdig)**  
  🚀 **由 [Markdig](https://github.com/xoofx/markdig) 提供支持的高性能渲染**
- 🔄 **Real-time updates**: Automatically re-renders changes in Markdown content  
  🔄 **实时更新** ：自动重新渲染 Markdown 内容中的更改
- 🎨 **Customizable styles**: Easily style Markdown elements using Avalonia's powerful styling system  
  🎨 **可定制的样式** ：使用 Avalonia 强大的样式系统轻松设置 Markdown 元素的样式
- 🔗 **Hyperlink support**: Clickable links with customizable behavior  
  🔗 **超链接支持** ：可点击的链接，具有可自定义的行为
- 📊 **Table support**: Render tables with proper formatting  
  📊 **表格支持** ：以适当的格式呈现表格
- 📜 **Code block syntax highlighting**: Supports multiple languages with [ColorCode](https://github.com/CommunityToolkit/ColorCode-Universal)  
  📜 **代码块语法高亮** ：使用 [ColorCode](https://github.com/CommunityToolkit/ColorCode-Universal) 支持多种语言
- 🖼️ **Image support**: Load online, local even `avares` images asynchronously  
  🖼️ **图片支持** ：在线加载，本地甚至异步 `avares` 图片
- ✍️ **Selectable text**: Text can be selected across different Markdown elements  
  ✍️ **可选文本** ：可以在不同的 Markdown 元素中选择文本

Note  笔记

This library currently only supports `Append` and `Clear` operations on the Markdown content, which is enough for LLM streaming scenarios.  
该库目前仅支持对 Markdown 内容进行 `Append` 和 `Clear` 操作，对于 LLM 流式场景来说已经足够了。

Warning  警告

Known issue: Avalonia 11.3.5 and above changed text layout behavior, which may cause some text offset issues in certain scenarios. e.g. code inline has extra bottom margin, wried italic font rendering, etc.  
已知问题：Avalonia 11.3.5 及更高版本改变了文本布局行为，这可能会在某些情况下导致一些文本偏移问题。例如，内联代码有额外的底部边距、扭曲的斜体字体渲染等。

## ✈️ Roadmap   ✈️ 路线图

[](#️-roadmap)

- Basic Markdown rendering  
  基本 Markdown 渲染
- Real-time updates  实时更新
- Hyperlink support  超链接支持
- Table support  表支持
- Code block syntax highlighting  
  代码块语法高亮
- Image support   图像支持
  - Bitmap  位图
  - SVG
  - Online images  在线图片
  - Local images  本地图像
  - `avares` images  
    `avares` 图片
- Selectable text across elements  
  可跨元素选择文本
- LaTeX support  LaTeX 支持
- HTML rendering  HTML 渲染

## 🚀 Getting Started  🚀 入门

[](#-getting-started)

### 1\. Install the NuGet package

1.安装 NuGet 包

[](#1-install-the-nuget-package)

You can install the latest version from NuGet CLI:  
您可以从 NuGet CLI 安装最新版本：

```bash
dotnet add package LiveMarkdown.Avalonia
```

or use the NuGet Package Manager in your IDE.  
或者使用 IDE 中的 NuGet 包管理器。

### 2\. Register the Markdown styles in your Avalonia application

2\. 在 Avalonia 应用程序中注册 Markdown 样式

[](#2-register-the-markdown-styles-in-your-avalonia-application)

```text
<Application
  x:Class="YourAppClass" xmlns="https://github.com/avaloniaui"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" RequestedThemeVariant="Default">

  <Application.Styles>
    <!-- Your other styles here -->
    <StyleInclude Source="avares://LiveMarkdown.Avalonia/Styles.axaml"/>
  </Application.Styles>

  <Application.Resources>
    <!-- Your other resources here -->
    <Color x:Key="BorderColor">#3DFFFFFF</Color>
    <Color x:Key="ForegroundColor">#FFFFFF</Color>
    <Color x:Key="CardBackgroundColor">#15000000</Color>
    <Color x:Key="SecondaryCardBackgroundColor">#99000000</Color>
  </Application.Resources>
</Application>
```

### 3\. Use the `MarkdownRenderer` control in your XAML

3\. 在 XAML 中使用 `MarkdownRenderer` 控件

[](#3-use-the-markdownrenderer-control-in-your-xaml)

Add the `MarkdownRenderer` control to your `.axaml` file:  
将 `MarkdownRenderer` 控件添加到你的 `.axaml` 文件：

```text
<YourControl
  xmlns:md="clr-namespace:LiveMarkdown.Avalonia;assembly=LiveMarkdown.Avalonia">
  <md:MarkdownRenderer x:Name="MarkdownRenderer"/>
</YourControl>
```

Then you can manage the Markdown content in your code-behind:  
然后，您可以在代码隐藏中管理 Markdown 内容：

```c
// ObservableStringBuilder is used for efficient string updates
var markdownBuilder = new ObservableStringBuilder();
MarkdownRenderer.MarkdownBuilder = markdownBuilder;

// Append Markdown content, this will trigger re-rendering
markdownBuilder.Append("# Hello, Markdown!");
markdownBuilder.Append("\n\nThis is a **live** Markdown viewer for Avalonia applications.");

// Clear the content
markdownBuilder.Clear();
```

If you want to load local images with relative paths, you can set the `MarkdownRenderer.ImageBasePath` property.  
如果要加载具有相对路径的本地图像，可以设置 `MarkdownRenderer.ImageBasePath` 属性。

## 🪄 Style Customization  🪄 风格定制

[](#-style-customization)

Markdown elements can be styled using Avalonia's powerful styling system. You can override the [default styles](https://github.com/DearVa/LiveMarkdown.Avalonia/blob/main/src/LiveMarkdown.Avalonia/Styles.axaml) by defining your own styles in your application styles.  
Markdown 元素可以使用 Avalonia 强大的样式系统进行样式设置。您可以在应用程序样式中定义自己的样式来覆盖[默认样式](https://github.com/DearVa/LiveMarkdown.Avalonia/blob/main/src/LiveMarkdown.Avalonia/Styles.axaml) 。

Avalonia Styling Docs:

- [Avalonia Styles](https://docs.avaloniaui.net/docs/styling)
- [Style selector syntax](https://docs.avaloniaui.net/docs/reference/styles/style-selector-syntax)

## 🤔 FAQ

[](#-faq)

- Q: Why some emojis not rendered correctly (rendered in single color)?
- A: This is a known issue caused by Skia (the render backend of Avalonia). You can upgrade SkiaSharp version (e.g. >= 3.117.0) to fix this. [Related issue](https://github.com/AvaloniaUI/Avalonia/issues/18677)

## 🤝 Contributing

[](#-contributing)

We welcome issues, feature ideas, and PRs! See [CONTRIBUTING.md](https://github.com/DearVa/LiveMarkdown.Avalonia/blob/main/CONTRIBUTING.md) for guidelines.

## 📄 License

[](#-license)

Distributed under the Apache 2.0 License. See [LICENSE](https://github.com/DearVa/LiveMarkdown.Avalonia/blob/main/LICENSE) for more information.

### Third-Party Licenses

[](#third-party-licenses)

- **markdig** - [BSD-2-Clause License](https://github.com/xoofx/markdig/blob/master/license.txt)
  - Markdown parser for Everywhere.Markdown rendering
  - Source repo: [https://github.com/xoofx/markdig](https://github.com/xoofx/markdig)
- **Svg.Skia** - [MIT License](https://github.com/wieslawsoltes/Svg.Skia/blob/master/LICENSE.TXT)
  - Svg rendering for images
  - Source repo: [https://github.com/wieslawsoltes/Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia)
- **TextMateSharp** - [MIT License](https://github.com/danipen/TextMateSharp/blob/master/LICENSE.md)
  - Syntax highlighting for code blocks
  - Source repo: [https://github.com/danipen/TextMateSharp](https://github.com/danipen/TextMateSharp)

