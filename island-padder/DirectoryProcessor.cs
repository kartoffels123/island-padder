using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

public class DirectoryProcessor
{
    public static void ProcessDirectory(string inputDir, string outputDir, int maxSteps = 32)
    {
        // Ensure the output directory exists
        Directory.CreateDirectory(outputDir);

        // Process each file and directory recursively
        foreach (string filePath in Directory.GetFiles(inputDir, "*.png", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(inputDir, filePath);
            string outputPath = Path.Combine(outputDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (Bitmap inputBitmap = new Bitmap(filePath))
            {
                using (Bitmap outputBitmap = new Bitmap(inputBitmap.Width, inputBitmap.Height))
                {
                    // Copy the input bitmap to the output bitmap
                    using (Graphics g = Graphics.FromImage(outputBitmap))
                    {
                        g.DrawImage(inputBitmap, 0, 0, inputBitmap.Width, inputBitmap.Height);
                    }

                    // Apply edge padding
                    UVPadder.ApplyDilation(inputBitmap, outputBitmap, maxSteps);

                    // Save the result
                    outputBitmap.Save(outputPath, ImageFormat.Png);
                }
            }

            Console.WriteLine($"Processed {relativePath}");
        }
    }

    static void Main()
    {
        string inputDir = "input";
        string outputDir = "output";

        ProcessDirectory(inputDir, outputDir);

        Console.WriteLine("All images processed successfully.");
    }
}
