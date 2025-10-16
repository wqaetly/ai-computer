using System;
using System.IO;

namespace AiComputer.Services
{
    /// <summary>
    /// OCR引擎配置 - 包含模型路径和识别参数
    /// </summary>
    public class OcrConfiguration
    {
        /// <summary>
        /// DBNet检测模型ONNX文件路径
        /// </summary>
        public string DbNetModelPath { get; set; } = "";

        /// <summary>
        /// AngleNet角度检测模型ONNX文件路径
        /// </summary>
        public string AngleNetModelPath { get; set; } = "";

        /// <summary>
        /// CrnnNet文本识别模型ONNX文件路径
        /// </summary>
        public string CrnnNetModelPath { get; set; } = "";

        /// <summary>
        /// 字典文件(keys.txt)路径
        /// </summary>
        public string KeysFilePath { get; set; } = "";

        /// <summary>
        /// 线程数，推荐设置为CPU核心数的一半
        /// </summary>
        public int NumThreads { get; set; } = 4;

        /// <summary>
        /// 图像填充像素，增加边缘检测准确性
        /// </summary>
        public int Padding { get; set; } = 50;

        /// <summary>
        /// 启用角度检测
        /// </summary>
        public bool EnableAngleDetection { get; set; } = true;

        /// <summary>
        /// 使用统一角度
        /// </summary>
        public bool UseUnifiedAngle { get; set; } = true;

        /// <summary>
        /// 文本框置信度阈值
        /// </summary>
        public float BoxScoreThreshold { get; set; } = 0.6f;

        /// <summary>
        /// 二值化阈值
        /// </summary>
        public float BoxThreshold { get; set; } = 0.3f;

        /// <summary>
        /// 文本框扩展比例
        /// </summary>
        public float UnClipRatio { get; set; } = 2.0f;

        public OcrConfiguration()
        {
            // 设置默认值
            NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            Padding = 50;
            EnableAngleDetection = true;
            UseUnifiedAngle = true;
            BoxScoreThreshold = 0.6f;
            BoxThreshold = 0.3f;
            UnClipRatio = 2.0f;
        }

        /// <summary>
        /// 创建默认中文OCR配置
        /// </summary>
        public static OcrConfiguration CreateDefaultChinese()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var modelsDir = Path.Combine(baseDir, "Assets", "OCRModels", "chinese");

            return new OcrConfiguration
            {
                DbNetModelPath = Path.Combine(modelsDir, "ch_PP-OCRv4_det_infer.onnx"),
                AngleNetModelPath = Path.Combine(modelsDir, "ch_ppocr_mobile_v2.0_cls_infer.onnx"),
                CrnnNetModelPath = Path.Combine(modelsDir, "ch_PP-OCRv4_rec_infer.onnx"),
                KeysFilePath = Path.Combine(modelsDir, "ppocr_keys_v1.txt")
            };
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DbNetModelPath) &&
                   !string.IsNullOrEmpty(AngleNetModelPath) &&
                   !string.IsNullOrEmpty(CrnnNetModelPath) &&
                   !string.IsNullOrEmpty(KeysFilePath) &&
                   File.Exists(DbNetModelPath) &&
                   File.Exists(AngleNetModelPath) &&
                   File.Exists(CrnnNetModelPath) &&
                   File.Exists(KeysFilePath);
        }

        /// <summary>
        /// 获取配置验证错误信息
        /// </summary>
        public string GetValidationError()
        {
            if (string.IsNullOrEmpty(DbNetModelPath))
                return "DBNet模型路径未设置";
            if (!File.Exists(DbNetModelPath))
                return $"DBNet模型文件不存在: {DbNetModelPath}";

            if (string.IsNullOrEmpty(AngleNetModelPath))
                return "AngleNet模型路径未设置";
            if (!File.Exists(AngleNetModelPath))
                return $"AngleNet模型文件不存在: {AngleNetModelPath}";

            if (string.IsNullOrEmpty(CrnnNetModelPath))
                return "CrnnNet模型路径未设置";
            if (!File.Exists(CrnnNetModelPath))
                return $"CrnnNet模型文件不存在: {CrnnNetModelPath}";

            if (string.IsNullOrEmpty(KeysFilePath))
                return "字典文件路径未设置";
            if (!File.Exists(KeysFilePath))
                return $"字典文件不存在: {KeysFilePath}";

            return "配置有效";
        }

        /// <summary>
        /// 根据图像分辨率自动优化参数
        /// </summary>
        public void OptimizeForResolution(int width, int height)
        {
            int imageMaxSide = Math.Max(width, height);

            // 根据图像尺寸自动调整参数
            if (imageMaxSide <= 512)
            {
                // 小图像 - 提高精度参数
                BoxScoreThreshold = 0.5f;
                BoxThreshold = 0.2f;
                UnClipRatio = 1.8f;
                Padding = 30;
            }
            else if (imageMaxSide <= 1024)
            {
                // 中等图像 - 平衡参数
                BoxScoreThreshold = 0.6f;
                BoxThreshold = 0.3f;
                UnClipRatio = 2.0f;
                Padding = 50;
            }
            else if (imageMaxSide <= 2048)
            {
                // 大图像 - 提高速度参数
                BoxScoreThreshold = 0.7f;
                BoxThreshold = 0.4f;
                UnClipRatio = 2.2f;
                Padding = 60;
            }
            else
            {
                // 超大图像 - 最大速度参数
                BoxScoreThreshold = 0.8f;
                BoxThreshold = 0.5f;
                UnClipRatio = 2.5f;
                Padding = 80;
            }
        }

        /// <summary>
        /// 获取最大边长限制 (根据图像分辨率自动计算)
        /// </summary>
        public int GetOptimalMaxSideLength(int width, int height)
        {
            int imageMaxSide = Math.Max(width, height);

            // 根据图像尺寸设置最大边长，兼顾速度和精度
            if (imageMaxSide <= 512)
                return 512;
            else if (imageMaxSide <= 1024)
                return 1024;
            else if (imageMaxSide <= 2048)
                return 1536;  // 不直接使用原尺寸，略微压缩提升速度
            else
                return 2048;  // 大图像限制最大尺寸
        }

        public override string ToString()
        {
            return $"OCR配置 - 线程数: {NumThreads}, 填充: {Padding}, 角度检测: {EnableAngleDetection}";
        }
    }
}
