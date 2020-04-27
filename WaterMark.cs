﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watermarker
{
    /// <summary>
    /// 水印设置
    /// </summary>
    public class WatermarkSettings
    {
        /// <summary>
        /// 是否启用文本水印
        /// </summary>
        public bool WatermarkTextEnable { get; set; } = false;

        /// <summary>
        /// 水印文本
        /// </summary>
        public string WatermarkText { get; set; } = "";

        /// <summary>
        /// 文字水印字体
        /// </summary>
        public string WatermarkFont { get; set; } = "Arial";

        /// <summary>
        /// 文本水印颜色
        /// </summary>
        public Color TextColor { get; set; } = Color.White;

        /// <summary>
        /// 水印文本旋转角度
        /// </summary>
        public int TextRotatedDegree { get; set; } = 0;

        /// <summary>
        /// 文本水印的公共设置
        /// </summary>
        public CommonSettings TextSettings { get; set; } = new CommonSettings();

        /// <summary>
        /// 图片水印是否启用
        /// </summary>
        public bool WatermarkPictureEnable { get; set; } = false;

        /// <summary>
        /// 图片水印的公共设置
        /// </summary>
        public CommonSettings PictureSettings { get; set; } = new CommonSettings();

        /// <summary>
        /// 加水印最小图片宽度
        /// </summary>
        public int MinimumImageWidthForWatermark { get; set; } = 150;
        /// <summary>
        /// 加水印最小图片高度
        /// </summary>
        public int MinimumImageHeightForWatermark { get; set; } = 150;
    }

    /// <summary>
    /// 水印位置
    /// </summary>
    public enum WatermarkPosition
    {
        /// <summary>
        /// 左上角
        /// </summary>
        TopLeftCorner,
        /// <summary>
        /// 中上
        /// </summary>
        TopCenter,
        /// <summary>
        /// 右上角
        /// </summary>
        TopRightCorner,
        /// <summary>
        /// 左中
        /// </summary>
        CenterLeft,
        /// <summary>
        /// 居中
        /// </summary>
        Center,
        /// <summary>
        /// 右中
        /// </summary>
        CenterRight,
        /// <summary>
        /// 左下角
        /// </summary>
        BottomLeftCorner,
        /// <summary>
        /// 中下
        /// </summary>
        BottomCenter,
        /// <summary>
        /// 右下角
        /// </summary>
        BottomRightCorner,
    }

    /// <summary>
    /// 公共设置
    /// </summary>
    public class CommonSettings
    {
        /// <summary>
        /// 相对于原始图像的水印大小仅水印文字有效(%)
        /// </summary>
        public int Size { get; set; } = 20;
        /// <summary>
        /// 水印位置
        /// </summary>
        public List<WatermarkPosition> PositionList { get; set; } = new List<WatermarkPosition>(new WatermarkPosition[] { WatermarkPosition.BottomRightCorner });
        /// <summary>
        /// 透明度,0透明、1不透明(0-1)
        /// </summary>
        public double Opacity { get; set; } = 1;
    }

}


namespace Watermarker
{
    public class WatermarkProcess : IDisposable
    {
        private readonly Lazy<Bitmap> watermarkBitmap;
        public WatermarkProcess()
        {

        }
        public WatermarkProcess(string imageWatermarkFilename)
        {
            watermarkBitmap = new Lazy<Bitmap>(() =>
            {
                if (!string.IsNullOrEmpty(imageWatermarkFilename) && File.Exists(imageWatermarkFilename))
                {
                    using (FileStream fs = new FileStream(imageWatermarkFilename, FileMode.Open, FileAccess.Read))
                    {
                        BinaryReader br = new BinaryReader(fs);
                        byte[] pictureBinary = br.ReadBytes((int)fs.Length);
                        br?.Close();
                        br?.Dispose();
                        using (MemoryStream ms = new MemoryStream(pictureBinary))
                        {
                            return new Bitmap(ms);
                        }
                    }
                }
                return null;
            });
        }

        public Image MakeImageWatermark(Image sourceImage, WatermarkSettings currentSettings)
        {
            if ((sourceImage.Height > currentSettings.MinimumImageHeightForWatermark) ||
                 (sourceImage.Width > currentSettings.MinimumImageWidthForWatermark))
            {
                Bitmap destBitmap = CreateBitmap(sourceImage);

                if (currentSettings.WatermarkTextEnable && !String.IsNullOrEmpty(currentSettings.WatermarkText))
                {
                    PlaceTextWatermark(destBitmap, currentSettings);
                }

                if (currentSettings.WatermarkPictureEnable && watermarkBitmap.Value != null)
                {
                    PlaceImageWatermark(destBitmap, watermarkBitmap.Value, currentSettings);
                }

                sourceImage.Dispose();
                return destBitmap;
            }
            return sourceImage;
        }

        /// <summary>
        /// 图片水印
        /// </summary>
        /// <param name="destBitmap"></param>
        /// <param name="watermarkBitmap"></param>
        /// <param name="currentSettings"></param>
        private void PlaceImageWatermark(Bitmap destBitmap, Bitmap watermarkBitmap, WatermarkSettings currentSettings)
        {
            currentSettings = currentSettings ?? new WatermarkSettings();
            using (Graphics g = Graphics.FromImage(destBitmap))
            {
                double watermarkSizeInPercent = (double)currentSettings.PictureSettings.Size / 100;

                Size boundingBoxSize = new Size((int)(destBitmap.Width * watermarkSizeInPercent),
                    (int)(destBitmap.Height * watermarkSizeInPercent));
                Size calculatedWatermarkSize = ScaleRectangleToFitBounds(boundingBoxSize, watermarkBitmap.Size);

                if (calculatedWatermarkSize.Width == 0 || calculatedWatermarkSize.Height == 0)
                {
                    return;
                }

                Bitmap scaledWatermarkBitmap =
                    new Bitmap(calculatedWatermarkSize.Width, calculatedWatermarkSize.Height);
                using (Graphics watermarkGraphics = Graphics.FromImage(scaledWatermarkBitmap))
                {
                    ColorMatrix opacityMatrix = new ColorMatrix
                    {
                        Matrix33 = (float)currentSettings.PictureSettings.Opacity
                    };
                    ImageAttributes attrs = new ImageAttributes();
                    attrs.SetColorMatrix(opacityMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    watermarkGraphics.DrawImage(watermarkBitmap,
                        new Rectangle(0, 0, scaledWatermarkBitmap.Width, scaledWatermarkBitmap.Height),
                        0, 0, watermarkBitmap.Width, watermarkBitmap.Height,
                        GraphicsUnit.Pixel, attrs);
                    attrs.Dispose();
                }

                foreach (var position in currentSettings.PictureSettings.PositionList)
                {
                    Point watermarkPosition = CalculateWatermarkPosition(position,
                        destBitmap.Size, calculatedWatermarkSize);

                    g.DrawImage(scaledWatermarkBitmap,
                        new Rectangle(watermarkPosition, calculatedWatermarkSize),
                        0, 0, calculatedWatermarkSize.Width, calculatedWatermarkSize.Height, GraphicsUnit.Pixel);
                }
                scaledWatermarkBitmap.Dispose();
            }
        }

        /// <summary>
        /// 文字水印
        /// </summary>
        /// <param name="sourceBitmap"></param>
        /// <param name="currentSettings"></param>
        private void PlaceTextWatermark(Bitmap sourceBitmap, WatermarkSettings currentSettings)
        {
            using (Graphics g = Graphics.FromImage(sourceBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                string text = currentSettings.WatermarkText;

                int textAngle = currentSettings.TextRotatedDegree;
                double sizeFactor = (double)currentSettings.TextSettings.Size / 100;
                Size maxTextSize = new Size(
                    (int)(sourceBitmap.Width * sizeFactor),
                    (int)(sourceBitmap.Height * sizeFactor));

                int fontSize = ComputeMaxFontSize(text, textAngle, currentSettings.WatermarkFont, maxTextSize, g);

                Font font = new Font(currentSettings.WatermarkFont, (float)fontSize, FontStyle.Bold);
                SizeF originalTextSize = g.MeasureString(text, font);
                SizeF rotatedTextSize = CalculateRotatedRectSize(originalTextSize, textAngle);

                Bitmap textBitmap = new Bitmap((int)rotatedTextSize.Width, (int)rotatedTextSize.Height,
                    PixelFormat.Format32bppArgb);
                using (Graphics textG = Graphics.FromImage(textBitmap))
                {
                    Color color = Color.FromArgb((int)(currentSettings.TextSettings.Opacity * 255), currentSettings.TextColor);
                    SolidBrush brush = new SolidBrush(color);

                    textG.TranslateTransform(rotatedTextSize.Width / 2, rotatedTextSize.Height / 2);
                    textG.RotateTransform((float)textAngle);
                    textG.DrawString(text, font, brush, -originalTextSize.Width / 2,
                        -originalTextSize.Height / 2);
                    textG.ResetTransform();

                    brush.Dispose();
                }

                foreach (var position in currentSettings.TextSettings.PositionList)
                {
                    Point textPosition = CalculateWatermarkPosition(position,
                        sourceBitmap.Size, rotatedTextSize.ToSize());
                    g.DrawImage(textBitmap, textPosition);
                }
                textBitmap.Dispose();
                font.Dispose();
            }
        }


        /// <summary>
        /// 创建图像
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <returns></returns>
        private Bitmap CreateBitmap(Image sourceImage)
        {
            Bitmap destBitmap = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);
            destBitmap.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            using (Graphics g = Graphics.FromImage(destBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(sourceImage, 0, 0);
            }
            return destBitmap;
        }

        /// <summary>
        /// 矩形边界
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        private Size ScaleRectangleToFitBounds(Size bounds, Size rect)
        {
            if (rect.Width < bounds.Width && rect.Height < bounds.Height)
            {
                return rect;
            }

            if (bounds.Width == 0 || bounds.Height == 0)
            {
                return new Size(0, 0);
            }

            double scaleFactorWidth = (double)rect.Width / bounds.Width;
            double scaleFactorHeight = (double)rect.Height / bounds.Height;

            double scaleFactor = Math.Max(scaleFactorWidth, scaleFactorHeight);
            return new Size()
            {
                Width = (int)(rect.Width / scaleFactor),
                Height = (int)(rect.Height / scaleFactor)
            };
        }

        /// <summary>
        /// 计算最大字体大小
        /// </summary>
        /// <param name="text"></param>
        /// <param name="angle"></param>
        /// <param name="fontName"></param>
        /// <param name="maxTextSize"></param>
        /// <param name="g"></param>
        /// <returns></returns>
        private int ComputeMaxFontSize(string text, int angle, string fontName, Size maxTextSize, Graphics g)
        {
            for (int fontSize = 2; ; fontSize++)
            {
                using (Font tmpFont = new Font(fontName, fontSize, FontStyle.Bold))
                {
                    SizeF textSize = g.MeasureString(text, tmpFont);
                    SizeF rotatedTextSize = CalculateRotatedRectSize(textSize, angle);
                    if (((int)rotatedTextSize.Width > maxTextSize.Width) ||
                        ((int)rotatedTextSize.Height > maxTextSize.Height))
                    {
                        return fontSize - 1;
                    }
                }
            }
        }

        /// <summary>
        /// 计算旋转矩形大小
        /// </summary>
        /// <param name="rectSize"></param>
        /// <param name="angleDeg"></param>
        /// <returns></returns>
        private SizeF CalculateRotatedRectSize(SizeF rectSize, double angleDeg)
        {
            double angleRad = angleDeg * Math.PI / 180;
            double width = rectSize.Height * Math.Abs(Math.Sin(angleRad)) +
                rectSize.Width * Math.Abs(Math.Cos(angleRad));
            double height = rectSize.Height * Math.Abs(Math.Cos(angleRad)) +
                rectSize.Width * Math.Abs(Math.Sin(angleRad));
            return new SizeF((float)width, (float)height);
        }

        /// <summary>
        /// 计算水印位置
        /// </summary>
        /// <param name="watermarkPosition"></param>
        /// <param name="imageSize"></param>
        /// <param name="watermarkSize"></param>
        /// <returns></returns>
        private Point CalculateWatermarkPosition(WatermarkPosition watermarkPosition, Size imageSize, Size watermarkSize)
        {
            Point position = new Point();
            switch (watermarkPosition)
            {
                case WatermarkPosition.TopLeftCorner:
                    position.X = 0;
                    position.Y = 0;
                    break;
                case WatermarkPosition.TopCenter:
                    position.X = (imageSize.Width / 2) - (watermarkSize.Width / 2);
                    position.Y = 0;
                    break;
                case WatermarkPosition.TopRightCorner:
                    position.X = imageSize.Width - watermarkSize.Width;
                    position.Y = 0;
                    break;
                case WatermarkPosition.CenterLeft:
                    position.X = 0;
                    position.Y = (imageSize.Height / 2) - (watermarkSize.Height / 2);
                    break;
                case WatermarkPosition.Center:
                    position.X = (imageSize.Width / 2) - (watermarkSize.Width / 2);
                    position.Y = (imageSize.Height / 2) - (watermarkSize.Height / 2);
                    break;
                case WatermarkPosition.CenterRight:
                    position.X = imageSize.Width - watermarkSize.Width;
                    position.Y = (imageSize.Height / 2) - (watermarkSize.Height / 2);
                    break;
                case WatermarkPosition.BottomLeftCorner:
                    position.X = 0;
                    position.Y = imageSize.Height - watermarkSize.Height;
                    break;
                case WatermarkPosition.BottomCenter:
                    position.X = (imageSize.Width / 2) - (watermarkSize.Width / 2);
                    position.Y = imageSize.Height - watermarkSize.Height;
                    break;
                case WatermarkPosition.BottomRightCorner:
                    position.X = imageSize.Width - watermarkSize.Width;
                    position.Y = imageSize.Height - watermarkSize.Height;
                    break;
            }
            return position;
        }

        public void Dispose()
        {
            watermarkBitmap?.Value?.Dispose();
        }
    }
}
