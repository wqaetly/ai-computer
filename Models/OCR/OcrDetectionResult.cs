using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace AiComputer.Models.OCR
{
    /// <summary>
    /// OCR检测结果 - 包含识别的文本内容和坐标信息
    /// </summary>
    public class OcrDetectionResult
    {
        /// <summary>
        /// 识别到的文本内容
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 文本区域的矩形框
        /// </summary>
        public RectangleF BoundingBox { get; set; }

        /// <summary>
        /// 识别置信度 (0-1)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 四角坐标点 (左上, 右上, 右下, 左下)
        /// </summary>
        public PointF[] CornerPoints { get; set; } = new PointF[4];

        /// <summary>
        /// 处理时间 (毫秒)
        /// </summary>
        public float ProcessingTime { get; set; }

        /// <summary>
        /// 获取文本区域的中心点
        /// </summary>
        public PointF GetCenter()
        {
            return new PointF(BoundingBox.X + BoundingBox.Width / 2, BoundingBox.Y + BoundingBox.Height / 2);
        }

        /// <summary>
        /// 检查文本是否为空
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(Text);
        }

        public override string ToString()
        {
            return $"OCR结果: \"{Text}\" | 位置: {BoundingBox} | 置信度: {Confidence:F3} | 处理时间: {ProcessingTime:F2}ms";
        }
    }

    /// <summary>
    /// OCR批量检测结果
    /// </summary>
    public class OcrBatchResult
    {
        /// <summary>
        /// 所有检测到的文本结果
        /// </summary>
        public List<OcrDetectionResult> Results { get; set; } = new List<OcrDetectionResult>();

        /// <summary>
        /// 总处理时间 (毫秒)
        /// </summary>
        public float TotalProcessingTime { get; set; }

        /// <summary>
        /// 检测到的文本块数量
        /// </summary>
        public int DetectedCount { get; set; }

        /// <summary>
        /// 图像分辨率信息
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// 使用的识别参数
        /// </summary>
        public string RecognitionParams { get; set; } = "";

        /// <summary>
        /// 获取所有识别到的文本内容 (换行分割)
        /// </summary>
        public string GetAllText()
        {
            if (Results.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (var result in Results)
            {
                sb.AppendLine(result.Text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 按置信度过滤结果
        /// </summary>
        public List<OcrDetectionResult> FilterByConfidence(float minConfidence)
        {
            var filtered = new List<OcrDetectionResult>();
            foreach (var result in Results)
            {
                if (result.Confidence >= minConfidence)
                {
                    filtered.Add(result);
                }
            }
            return filtered;
        }

        public override string ToString()
        {
            return $"OCR批量结果: {DetectedCount}个文本块 | 总时间: {TotalProcessingTime:F2}ms | 图像尺寸: {ImageSize}";
        }
    }
}
