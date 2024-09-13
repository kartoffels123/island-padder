using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class UVPadder
{
    public static void ApplyDilation(Bitmap inputBitmap, Bitmap outputBitmap, int maxSteps)
    {
        int width = inputBitmap.Width;
        int height = inputBitmap.Height;
        bool pixelsChanged;

        BitmapData inputData = inputBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData outputData = outputBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        int bytesPerPixel = Image.GetPixelFormatSize(inputBitmap.PixelFormat) / 8;
        int stride = inputData.Stride;
        IntPtr inputPtr = inputData.Scan0;
        IntPtr outputPtr = outputData.Scan0;

        byte[] inputPixels = new byte[stride * height];
        byte[] outputPixels = new byte[stride * height];
        Marshal.Copy(inputPtr, inputPixels, 0, inputPixels.Length);
        Marshal.Copy(outputPtr, outputPixels, 0, outputPixels.Length);

        for (int step = 0; step < maxSteps; step++)
        {
            pixelsChanged = false;
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * stride) + (x * bytesPerPixel);
                    if (inputPixels[pixelIndex + 3] == 0) // Transparent pixel
                    {
                        Color averageColor = GetAverageNeighborColor(inputPixels, width, height, x, y, stride, bytesPerPixel);

                        if (averageColor.A > 0)
                        {
                            outputPixels[pixelIndex] = averageColor.B;
                            outputPixels[pixelIndex + 1] = averageColor.G;
                            outputPixels[pixelIndex + 2] = averageColor.R;
                            outputPixels[pixelIndex + 3] = averageColor.A;
                            pixelsChanged = true;
                        }
                    }
                    else
                    {
                        outputPixels[pixelIndex] = inputPixels[pixelIndex];
                        outputPixels[pixelIndex + 1] = inputPixels[pixelIndex + 1];
                        outputPixels[pixelIndex + 2] = inputPixels[pixelIndex + 2];
                        outputPixels[pixelIndex + 3] = inputPixels[pixelIndex + 3];
                    }
                }
            });

            if (!pixelsChanged)
            {
                break;
            }

            // Swap input and output for next iteration
            byte[] temp = inputPixels;
            inputPixels = outputPixels;
            outputPixels = temp;
        }

        Marshal.Copy(outputPixels, 0, outputPtr, outputPixels.Length);

        inputBitmap.UnlockBits(inputData);
        outputBitmap.UnlockBits(outputData);
    }

    public static Color GetAverageNeighborColor(byte[] pixels, int width, int height, int x, int y, int stride, int bytesPerPixel)
    {
        int totalR = 0;
        int totalG = 0;
        int totalB = 0;
        int totalA = 0;
        int count = 0;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int neighborX = x + dx;
                int neighborY = y + dy;

                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    int pixelIndex = (neighborY * stride) + (neighborX * bytesPerPixel);
                    byte neighborA = pixels[pixelIndex + 3];

                    if (neighborA > 0)
                    {
                        totalR += pixels[pixelIndex + 2];
                        totalG += pixels[pixelIndex + 1];
                        totalB += pixels[pixelIndex];
                        totalA += neighborA;
                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            int averageR = totalR / count;
            int averageG = totalG / count;
            int averageB = totalB / count;
            int averageA = totalA / count;

            return Color.FromArgb(averageA, averageR, averageG, averageB);
        }

        return Color.Transparent;
    }
}
