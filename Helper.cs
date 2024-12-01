using System;
using System.Collections.Generic;
using System.IO;

namespace FactorioUpdater
{
    public class Helper
    {

        public static Dictionary<string, string> GetModNamesFromDir(string dir)
        {
            string searchPattern = "*_*.*.*.zip";
            Dictionary<string, string> modInfoList = new Dictionary<string, string>();
            string[] files = Directory.GetFiles(dir, searchPattern);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string[] parts = fileName.Split('_');
                if (parts.Length == 2)
                {
                    if (modInfoList.ContainsKey(parts[0]) && IsVersionSmaller(parts[1], modInfoList[parts[0]]))
                    {
                        continue;
                    }

                    modInfoList[parts[0]] = parts[1];
                }
            }
            return modInfoList;
        }

        public static void MoveMods(List<string> names, string from, string to)
        {
            try
            {
                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(to))
                {
                    Directory.CreateDirectory(to);
                }

                foreach (string modNameWithoutVersion in names)
                {
                    // Filter the list of files in the source directory based on the mod name
                    string[] matchingFiles = Directory.GetFiles(from, $"{modNameWithoutVersion}_*.zip");

                    foreach (string sourcePath in matchingFiles)
                    {
                        string modName = Path.GetFileName(sourcePath);

                        string destinationPath = Path.Combine(to, modName);

                        // Move the mod file from the source directory to the destination directory
                        File.Move(sourcePath, destinationPath);
                        Console.WriteLine($"Moved '{modName}' from '{from}' to '{to}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving mods: {ex.Message}");
            }
        }

        public static void PrintProgressBar(long current, long total)
        {
            int progressBarWidth = 50;
            double progressPercentage = (double)current / total;
            int progressChars = (int)(progressBarWidth * progressPercentage);

            Console.Write("\r[");

            for (int i = 0; i < progressBarWidth; i++)
            {
                if (i < progressChars)
                    Console.Write("#");
                else
                    Console.Write(" ");
            }

            string sizeString = FormatSize(current);
            string totalSizeString = FormatSize(total);

            Console.Write($"] {progressPercentage:P0} ({sizeString} / {totalSizeString})           ");
        }
        public static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;

            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }

            return $"{bytes:0.##} {sizes[order]}";
        }

        public static bool IsVersionSmaller(string version1, string version2)
        {
            string[] parts1 = version1.Split('.');
            string[] parts2 = version2.Split('.');

            for (int i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
            {
                int part1 = int.Parse(parts1[i]);
                int part2 = int.Parse(parts2[i]);

                if (part1 < part2)
                {
                    return true; // version1 is smaller
                }
                else if (part1 > part2)
                {
                    return false; // version1 is greater
                }
            }

            // If all parts are equal, consider version1 not smaller
            return false;
        }
    }
}
