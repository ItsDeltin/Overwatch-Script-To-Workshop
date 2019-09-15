using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Deltin.Deltinteger.Assets.Images
{
    public class EffectImage
    {
        public EffectPixel[,] Pixels;

        public EffectImage(EffectPixel[,] pixels)
        {
            Pixels = pixels;
        }

        public static EffectImage FromFile(string path)
        {
            return FromImage(
                Image.FromFile(path)
            );
        }

        public static EffectImage FromImage(Image image)
        {
            Size size = GetClosestSize(image);
            EffectImage eff = new EffectImage(new EffectPixel[size.Width, size.Height]);
            Bitmap resizedImage = ResizeImage(image, size.Width, size.Height);

            for (int x = 0; x < eff.Pixels.GetLength(0); x++)
                for (int y = 0; y < eff.Pixels.GetLength(1); y++)
                    eff.Pixels[x, y] = EffectPixel.FromRGBColor(x, y, resizedImage.GetPixel(x, y));
            
            resizedImage.Dispose();

            return eff;
        }

        private static Size GetClosestSize(Image image)
        {
            if (image.Width * image.Height <= Constants.MAX_EFFECT_COUNT)
                return image.Size;

            double aspectRatio = Convert.ToDouble(image.Width) / Convert.ToDouble(image.Height);

            //finds the closest aspect ratio where there are less than 128 effects
            Size closestSize = new Size(11, 11);
            double closestAspectRatio = Constants.MAX_EFFECT_COUNT;
            for (int a = 1; a <= Constants.MAX_EFFECT_COUNT; a++)
            {
                for (int b = 1; b <= Constants.MAX_EFFECT_COUNT; b++)
                {
                    if (a * b > Constants.MAX_EFFECT_COUNT)
                        break;
                    double aRatio = Convert.ToDouble(a) / Convert.ToDouble(b);
                    if (Math.Abs(aRatio - aspectRatio) < Math.Abs(aRatio - closestAspectRatio))
                    {
                        closestSize = new Size(a, b);
                        closestAspectRatio = aRatio;
                    }
                }
            }

            return closestSize;
        }

        private static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            image.Dispose();

            return destImage;
        }
    }

    public class EffectPixel
    {
        public int PositionX;
        public int PositionY;
        public Elements.Color Color;

        public EffectPixel(int posX, int posY, Elements.Color color)
        {
            PositionX = posX;
            PositionY = posY;
            Color = color;
        }

        public static EffectPixel FromRGBColor(int X, int Y, Color RGBColor)
        {
            //finds the closest color in overwatch
            int shortestDistance = 255 * 3;
            int shortestIndex = 0;
            for (int i  = 0; i < Constants.COLORS.Length; i++)
            {
                Color rgb = Constants.COLORS[i].RGBColor;
                int distance = Math.Abs(RGBColor.R - rgb.R) + Math.Abs(RGBColor.G - rgb.G) + Math.Abs(RGBColor.B - rgb.B);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestIndex = i;
                }
            }

            return new EffectPixel(X, Y, Constants.COLORS[shortestIndex].EffectColor);
        }
    }
}
