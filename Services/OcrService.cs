using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using AiComputer.Services.OCR;
using AiComputer.Models.OCR;

namespace AiComputer.Services
{
    /// <summary>
    /// OCR识别服务 - 完整实现版本
    /// 提供图片文字识别功能
    /// </summary>
    public class OcrService : IDisposable
    {
        private bool _isInitialized = false;
        private OcrConfiguration? _configuration;
        private OcrLite? _ocrLite;

        public OcrService()
        {
            _ocrLite = new OcrLite();
        }

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

                // 初始化OCR引擎
                await Task.Run(() =>
                {
                    _ocrLite?.InitModels(
                        _configuration.DbNetModelPath,
                        _configuration.AngleNetModelPath,
                        _configuration.CrnnNetModelPath,
                        _configuration.KeysFilePath,
                        _configuration.NumThreads
                    );
                });

                _isInitialized = true;
                Console.WriteLine("OCR服务初始化成功！");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR服务初始化失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="bitmap">要识别的图片</param>
        /// <returns>识别出的文字</returns>
        public async Task<string> RecognizeTextAsync(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            if (!_isInitialized || _configuration == null || _ocrLite == null)
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
                    // 执行OCR识别
                    var result = await RecognizeImageAsync(tempPath, true);

                    // 返回识别结果
                    return result.GetAllText();
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
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 识别图像文件中的文本
        /// </summary>
        /// <param name="imagePath">图像文件路径</param>
        /// <param name="autoOptimize">是否根据图像分辨率自动优化参数</param>
        /// <returns>识别结果</returns>
        public async Task<OcrBatchResult> RecognizeImageAsync(string imagePath, bool autoOptimize = true)
        {
            if (!_isInitialized || _configuration == null || _ocrLite == null)
            {
                throw new InvalidOperationException("OCR服务未初始化");
            }

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException($"图像文件不存在: {imagePath}");
            }

            try
            {
                // 获取图像信息用于参数优化
                Size imageSize = GetImageSize(imagePath);

                // 创建配置副本进行参数优化
                var optimizedConfig = CreateOptimizedConfig(imageSize, autoOptimize);

                // 执行OCR识别
                var ocrResult = await Task.Run(() =>
                {
                    return _ocrLite.Detect(
                        imagePath,
                        optimizedConfig.Padding,
                        optimizedConfig.GetOptimalMaxSideLength(imageSize.Width, imageSize.Height),
                        optimizedConfig.BoxScoreThreshold,
                        optimizedConfig.BoxThreshold,
                        optimizedConfig.UnClipRatio,
                        optimizedConfig.EnableAngleDetection,
                        optimizedConfig.UseUnifiedAngle
                    );
                });

                // 转换结果格式
                return ConvertToApiResult(ocrResult, imageSize, optimizedConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR识别失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 获取图像尺寸
        /// </summary>
        private Size GetImageSize(string imagePath)
        {
            using (var mat = CvInvoke.Imread(imagePath, ImreadModes.AnyColor))
            {
                return new Size(mat.Width, mat.Height);
            }
        }

        /// <summary>
        /// 创建优化后的配置
        /// </summary>
        private OcrConfiguration CreateOptimizedConfig(Size imageSize, bool autoOptimize)
        {
            if (_configuration == null)
                throw new InvalidOperationException("配置未初始化");

            // 创建配置副本
            var optimizedConfig = new OcrConfiguration
            {
                DbNetModelPath = _configuration.DbNetModelPath,
                AngleNetModelPath = _configuration.AngleNetModelPath,
                CrnnNetModelPath = _configuration.CrnnNetModelPath,
                KeysFilePath = _configuration.KeysFilePath,
                NumThreads = _configuration.NumThreads,
                Padding = _configuration.Padding,
                EnableAngleDetection = _configuration.EnableAngleDetection,
                UseUnifiedAngle = _configuration.UseUnifiedAngle,
                BoxScoreThreshold = _configuration.BoxScoreThreshold,
                BoxThreshold = _configuration.BoxThreshold,
                UnClipRatio = _configuration.UnClipRatio
            };

            // 根据图像分辨率自动优化参数
            if (autoOptimize)
            {
                optimizedConfig.OptimizeForResolution(imageSize.Width, imageSize.Height);
            }

            return optimizedConfig;
        }

        /// <summary>
        /// 将内部结果转换为API结果
        /// </summary>
        private OcrBatchResult ConvertToApiResult(Models.OCR.OcrResult ocrResult, Size imageSize, OcrConfiguration usedConfig)
        {
            var batchResult = new OcrBatchResult
            {
                TotalProcessingTime = ocrResult.DetectTime,
                DetectedCount = ocrResult.TextBlocks?.Count ?? 0,
                ImageSize = imageSize,
                RecognitionParams = $"最大边长: {usedConfig.GetOptimalMaxSideLength(imageSize.Width, imageSize.Height)}, " +
                                  $"填充: {usedConfig.Padding}, " +
                                  $"置信度阈值: {usedConfig.BoxScoreThreshold:F2}"
            };

            if (ocrResult.TextBlocks != null && ocrResult.TextBlocks.Count > 0)
            {
                var results = new List<OcrDetectionResult>();

                foreach (var textBlock in ocrResult.TextBlocks)
                {
                    var detectionResult = ConvertTextBlockToResult(textBlock);
                    results.Add(detectionResult);
                }

                batchResult.Results = results;
            }

            return batchResult;
        }

        /// <summary>
        /// 将TextBlock转换为OcrDetectionResult
        /// </summary>
        private OcrDetectionResult ConvertTextBlockToResult(TextBlock textBlock)
        {
            // 转换坐标点
            var cornerPoints = new PointF[4];
            for (int i = 0; i < 4 && i < textBlock.BoxPoints.Count; i++)
            {
                cornerPoints[i] = new PointF(textBlock.BoxPoints[i].X, textBlock.BoxPoints[i].Y);
            }

            // 计算边界矩形
            var rect = CalculateBoundingRect(cornerPoints);

            return new OcrDetectionResult
            {
                Text = textBlock.Text ?? "",
                BoundingBox = rect,
                Confidence = textBlock.BoxScore,
                CornerPoints = cornerPoints,
                ProcessingTime = textBlock.BlockTime
            };
        }

        /// <summary>
        /// 计算边界矩形
        /// </summary>
        private RectangleF CalculateBoundingRect(PointF[] points)
        {
            if (points == null || points.Length == 0)
                return new RectangleF();

            float minX = points[0].X, maxX = points[0].X;
            float minY = points[0].Y, maxY = points[0].Y;

            foreach (var point in points)
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 将Bitmap保存为临时文件
        /// </summary>
        private async Task<string> SaveBitmapToTempFileAsync(Avalonia.Media.Imaging.Bitmap bitmap)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ocr_temp_{Guid.NewGuid()}.png");

            using (var stream = File.Create(tempPath))
            {
                bitmap.Save(stream);
                await stream.FlushAsync();
            }

            return tempPath;
        }

        public void Dispose()
        {
            _ocrLite?.Dispose();
            _isInitialized = false;
        }
    }
}
