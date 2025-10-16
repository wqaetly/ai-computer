using Emgu.CV;
using System;

namespace AiComputer.Services.OCR
{
    /// <summary>
    /// 图像缩放参数
    /// </summary>
    internal class ScaleParam
    {
        public int SrcWidth { get; set; }
        public int SrcHeight { get; set; }
        public int DstWidth { get; set; }
        public int DstHeight { get; set; }
        public float ScaleWidth { get; set; }
        public float ScaleHeight { get; set; }

        public ScaleParam(int srcWidth, int srcHeight, int dstWidth, int dstHeight, float scaleWidth, float scaleHeight)
        {
            SrcWidth = srcWidth;
            SrcHeight = srcHeight;
            DstWidth = dstWidth;
            DstHeight = dstHeight;
            ScaleWidth = scaleWidth;
            ScaleHeight = scaleHeight;
        }

        public override string ToString()
        {
            return $"sw:{SrcWidth},sh:{SrcHeight},dw:{DstWidth},dh:{DstHeight},{ScaleWidth},{ScaleHeight}";
        }

        /// <summary>
        /// 根据源图像和目标尺寸计算缩放参数
        /// 确保尺寸是32的倍数（ONNX模型要求）
        /// </summary>
        public static ScaleParam GetScaleParam(Mat src, int dstSize)
        {
            int srcWidth = src.Cols;
            int srcHeight = src.Rows;
            int dstWidth = srcWidth;
            int dstHeight = srcHeight;

            float scale = 1.0F;
            if (dstWidth > dstHeight)
            {
                scale = (float)dstSize / (float)dstWidth;
                dstWidth = dstSize;
                dstHeight = (int)((float)dstHeight * scale);
            }
            else
            {
                scale = (float)dstSize / (float)dstHeight;
                dstHeight = dstSize;
                dstWidth = (int)((float)dstWidth * scale);
            }

            // 确保尺寸是32的倍数
            if (dstWidth % 32 != 0)
            {
                dstWidth = (dstWidth / 32 - 1) * 32;
                dstWidth = Math.Max(dstWidth, 32);
            }
            if (dstHeight % 32 != 0)
            {
                dstHeight = (dstHeight / 32 - 1) * 32;
                dstHeight = Math.Max(dstHeight, 32);
            }

            float scaleWidth = (float)dstWidth / (float)srcWidth;
            float scaleHeight = (float)dstHeight / (float)srcHeight;

            return new ScaleParam(srcWidth, srcHeight, dstWidth, dstHeight, scaleWidth, scaleHeight);
        }
    }
}
