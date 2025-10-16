using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace AiComputer.Services
{
    /// <summary>
    /// OCR识别服务 - 简化版本
    /// 提供图片文字识别功能
    /// </summary>
    public class OcrService
    {
        private bool _isInitialized = false;
        private OcrConfiguration? _configuration;

        /// <summary>
        /// 初始化OCR服务
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                // 创建默认中文OCR配置
                _configuration = OcrConfiguration.CreateDefaultChinese();

                // 验证配置
                if (!_configuration.IsValid())
                {
                    var error = _configuration.GetValidationError();
                    Console.WriteLine($"OCR配置验证失败: {error}");
                    return false;
                }

                Console.WriteLine("OCR配置验证成功:");
                Console.WriteLine($"  - 检测模型: {Path.GetFileName(_configuration.DbNetModelPath)}");
                Console.WriteLine($"  - 角度模型: {Path.GetFileName(_configuration.AngleNetModelPath)}");
                Console.WriteLine($"  - 识别模型: {Path.GetFileName(_configuration.CrnnNetModelPath)}");
                Console.WriteLine($"  - 字典文件: {Path.GetFileName(_configuration.KeysFilePath)}");

                // TODO: 初始化OCR引擎
                // 这里需要加载ONNX模型并初始化识别引擎
                // 由于完整的OCR引擎移植较复杂，暂时只验证模型文件存在

                _isInitialized = true;
                Console.WriteLine("OCR服务初始化成功（模型文件已就位，待完整OCR引擎集成）");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR服务初始化失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="bitmap">要识别的图片</param>
        /// <returns>识别出的文字</returns>
        public async Task<string> RecognizeTextAsync(Bitmap bitmap)
        {
            if (!_isInitialized || _configuration == null)
            {
                throw new InvalidOperationException("OCR服务未初始化");
            }

            try
            {
                Console.WriteLine($"开始OCR识别，图片尺寸: {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height}");

                // 将Avalonia Bitmap转换为临时文件
                var tempPath = await SaveBitmapToTempFileAsync(bitmap);

                try
                {
                    // 使用Emgu.CV加载图片
                    var mat = LoadImageWithEmgu(tempPath);
                    Console.WriteLine($"图片加载成功，尺寸: {mat.Width}x{mat.Height}");

                    // TODO: 实现完整的OCR识别流程
                    // 1. 文本检测 (DBNet)
                    // 2. 角度检测和矫正 (AngleNet)
                    // 3. 文本识别 (CrnnNet)

                    // 目前返回提示信息和图片信息
                    await Task.Delay(500); // 模拟处理时间

                    mat.Dispose();

                    return $"[OCR已初始化]\n" +
                           $"图片尺寸: {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height}\n" +
                           $"模型文件已就位，等待完整OCR引擎集成\n\n" +
                           $"提示：粘贴图片功能已可用，OCR识别功能开发中...";
                }
                finally
                {
                    // 清理临时文件
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR识别失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 将Bitmap保存为临时文件
        /// </summary>
        private async Task<string> SaveBitmapToTempFileAsync(Bitmap bitmap)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ocr_temp_{Guid.NewGuid()}.png");

            using (var stream = File.Create(tempPath))
            {
                bitmap.Save(stream);
                await stream.FlushAsync();
            }

            return tempPath;
        }

        /// <summary>
        /// 使用Emgu.CV加载图片
        /// </summary>
        private Mat LoadImageWithEmgu(string imagePath)
        {
            return CvInvoke.Imread(imagePath, ImreadModes.AnyColor);
        }
    }
}
