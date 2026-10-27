using PhotoSauce.MagicScaler;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace NeeView.UnitTest
{
    public class ImageResizeFilterConfigTest
    {
        [Fact]
        public void CreateProcessImageSetting_UsesCompandedBlendingWhenUnsharpMaskIsEnabled()
        {
            var config = new ImageResizeFilterConfig
            {
                IsUnsharpMaskEnabled = true,
            };

            var setting = config.CreateProcessImageSetting();

            Assert.Equal(InterpolationSettings.Lanczos, setting.Interpolation);
            Assert.True(setting.Sharpen);
            Assert.Equal(GammaMode.Companded, setting.BlendingMode);
            Assert.Equal(config.UnsharpMask.CreateUnsharpMaskSetting().Amount, setting.UnsharpMask.Amount);
        }

        [Fact]
        public void CreateProcessImageSetting_PreservesLinearBlendingWhenUnsharpMaskIsDisabled()
        {
            var config = new ImageResizeFilterConfig
            {
                IsUnsharpMaskEnabled = false,
            };

            var setting = config.CreateProcessImageSetting();

            Assert.False(setting.Sharpen);
            Assert.Equal(GammaMode.Linear, setting.BlendingMode);
        }

        [Fact]
        public void ProcessImageWithUnsharpMask_DoesNotCreateWhiteDotsInBlackArea()
        {
            var source = CreateBlackImageWithWhiteSquare();
            var before = Process(source, GammaMode.Linear);
            var after = Process(source, GammaMode.Companded);

            var beforeBlackPixels = CountNonBlackPixels(before, 340, 340, 172, 172);
            var afterBlackPixels = CountNonBlackPixels(after, 340, 340, 172, 172);

            Assert.True(beforeBlackPixels > 0, "The fixture must expose dark-area speckles with linear blending.");
            Assert.True(afterBlackPixels < beforeBlackPixels, $"Expected fewer bright pixels in black area; Linear={beforeBlackPixels}, Companded={afterBlackPixels}.");
            Assert.True(HasSharpenedEdge(after), "Companded blending must retain unsharp edge enhancement.");
        }

        private static BitmapSource CreateBlackImageWithWhiteSquare()
        {
            var pixels = new byte[64 * 64 * 4];
            for (var y = 18; y < 46; y++)
            {
                for (var x = 18; x < 46; x++)
                {
                    var offset = (y * 64 + x) * 4;
                    pixels[offset] = 255;
                    pixels[offset + 1] = 255;
                    pixels[offset + 2] = 255;
                    pixels[offset + 3] = 255;
                }
            }

            var bitmap = BitmapSource.Create(64, 64, 96, 96, PixelFormats.Pbgra32, null, pixels, 64 * 4);
            bitmap.Freeze();
            return bitmap;
        }

        private static BitmapSource Process(BitmapSource source, GammaMode blendingMode)
        {
            var encoder = new JpegBitmapEncoder { QualityLevel = 95 };
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var input = new MemoryStream();
            encoder.Save(input);
            input.Position = 0;

            using var output = new MemoryStream();
            var setting = new ImageResizeFilterConfig { IsUnsharpMaskEnabled = true }.CreateProcessImageSetting();
            setting.Width = 512;
            setting.Height = 512;
            setting.ResizeMode = CropScaleMode.Stretch;
            setting.BlendingMode = blendingMode;
            MagicImageProcessor.ProcessImage(input, output, setting);

            output.Position = 0;
            var result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad;
            result.StreamSource = output;
            result.EndInit();
            result.Freeze();
            return result;
        }

        private static int CountNonBlackPixels(BitmapSource bitmap, int x, int y, int width, int height)
        {
            var stride = bitmap.PixelWidth * 4;
            var pixels = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(pixels, stride, 0);

            var count = 0;
            for (var row = y; row < y + height; row++)
            {
                for (var column = x; column < x + width; column++)
                {
                    var offset = row * stride + column * 4;
                    if (pixels[offset] != 0 || pixels[offset + 1] != 0 || pixels[offset + 2] != 0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool HasSharpenedEdge(BitmapSource bitmap)
        {
            var stride = bitmap.PixelWidth * 4;
            var pixels = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(pixels, stride, 0);
            var middle = bitmap.PixelHeight / 2;
            var darkestEdgePixel = 255;
            for (var x = 135; x < 170; x++)
            {
                darkestEdgePixel = Math.Min(darkestEdgePixel, pixels[middle * stride + x * 4]);
            }

            return darkestEdgePixel == 0;
        }
    }
}
