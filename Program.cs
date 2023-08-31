using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace FacatorioUpdater
{

    
    internal class Program
    {


        const string Mirror = "https://mods-storage.re146.dev/";
        static async Task Main(string[] args)
        {
            string[] modNames = {"Aircraft","aai-zones"};

            Console.WriteLine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                client.DefaultRequestHeaders.Add("Pragma", "no-cache");

                Mod a = await GetModInfo(client, modNames[1]);
                await DownloadMod(client, a, Mirror);
                try
                {
                    //string response = await client.GetStringAsync(apiUrl);
                    
                    //JObject modInfo = JObject.Parse(response);

                    //Mod a = new Mod(modInfo);

                    //PrintJObject(modInfo);
                    //Console.ReadKey();

                    Console.WriteLine($"Mod Title: {a.title}");
                    Console.WriteLine($"Mod Name: {a.name}");
                    Console.WriteLine($"Description: {a.description}");
                    Console.WriteLine($"Author: {a.author}");
                    Console.WriteLine($"Downloads: {a.downloadCounter}");
                    Console.WriteLine($"Most recent version is {a.RecentVersion.version}");
                    Console.WriteLine($"For factorio version {a.RecentVersion.factorioVer} or higher");
                    Console.WriteLine($"Download url for last version{a.versions.Last().downURL}");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching mod information: {ex.Message}");
                }
            }
            Console.ReadKey();
            Console.WriteLine();
            
        }
        static void PrintJObject(JObject obj, string indent = "")
        {
            foreach (var property in obj.Properties())
            {
                Console.WriteLine($"{indent}{property.Name}: {property.Value}");

                if (property.Value.Type == JTokenType.Object)
                {
                    PrintJObject((JObject)property.Value, indent + "  ");
                }
            }
        }

        async static Task<Mod> GetModInfo(HttpClient client,string name)
        {
            string apiUrl = $"https://mods.factorio.com/api/mods/{name}";

            try
            {
                string response = await client.GetStringAsync(apiUrl);

                JObject modInfo = JObject.Parse(response);

                return new Mod(modInfo);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching mod information: {ex.Message}");
            }
            return null;
        }
        async static Task<bool> DownloadMod(HttpClient client,Mod mod,string mirror)
        {
            const int updateInterval = 10;
            DateTime lastUpdate = DateTime.MinValue;

            Mod.Version ver = mod.WantedVersion != null ? mod.WantedVersion : mod.RecentVersion;
            string fileName = $"{mod.name}_{ver.version}.zip";
            string url = GetDownloadLink(Mirror, mod, ver);
            string downloadPatch = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            try
            {
                
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = File.Create(Path.Combine(downloadPatch, fileName)))
                    {
                        const int bufferSize = 8192;
                        byte[] buffer = new byte[bufferSize];
                        long totalBytesRead = 0;
                        int bytesRead;

                        long fileSize = response.Content.Headers.ContentLength ?? -1;
                        Console.CursorVisible = false;

                        Console.WriteLine($"Downloading mod {mod.title}");

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            DateTime currentTime = DateTime.Now;
                            if ((currentTime - lastUpdate).TotalMilliseconds >= updateInterval)
                            {
                                PrintProgressBar(totalBytesRead, fileSize);
                                lastUpdate = currentTime;
                            }
                            
                        }
                        PrintProgressBar(totalBytesRead, fileSize);
                        Console.WriteLine();
                    }
                }
                else
                    return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                Console.CursorVisible = true;
            }
            Console.CursorVisible = true;
            return true;
        }
        async static Task<List<Mod>> DownloadMods(HttpClient client, string mirror, List<Mod> mods)
        {
            List<Mod> failed = new List<Mod>();
            for (int i = 0; i < mods.Count; i++)
            {
                Console.Clear();
                Console.WriteLine($"Downloading mod ({i + 1}/{mods.Count})");
                if(!await DownloadMod(client, mods[i], mirror))
        {
                    failed.Add(mods[i]);
                }
            }
            return failed;
        }
        static string GetDownloadLink(string mirror,Mod mod,Mod.Version ver )
        {
            if(!mod.IsInitialized)
                throw new Exception("Mod is not initialized!!");
            return $"{mirror}{mod.name}/{ver.version}.zip";
        }



        static void PrintProgressBar(long current, long total)
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

        static string FormatSize(long bytes)
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
        //static DownloadMods(List<Mod> mods)
        //{

        //}
    }
}
