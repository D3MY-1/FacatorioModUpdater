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
using static System.Net.WebRequestMethods;

namespace FacatorioUpdater
{

    
    internal class Program
    {


        const string Mirror = "https://mods-storage.re146.dev/";
        static async Task Main(string[] args)
        {
            string[] modNames = {"Aircraft","aai-zones", "fart-mod", "IndustrialRevolution3MirroredRecipes" };

            Console.WriteLine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            string downloadPatch = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                client.DefaultRequestHeaders.Add("Pragma", "no-cache");

                List<Mod> toUpdate = new List<Mod>();
                var inf = Helper.GetModNamesFromDir(downloadPatch);
                Console.WriteLine("Mods to update :");
                foreach (var i in inf)
                {
                    Mod info = await GetModInfo(client,i.Name);
                    if(Helper.IsVersionSmaller(i.Version, info.RecentVersion.version))
                    {
                        toUpdate.Add(info);
                        Console.WriteLine(i.Name);
                    }
                    
                }

                var failed = await DownloadMods(client, Mirror, toUpdate);

                if (failed.Count > 0)
                {
                    Console.WriteLine($"{failed.Count} of download mods failed there are a list of them!");
                    foreach (Mod mod in failed)
                    {
                        Console.WriteLine($"Name : {mod.name} Download link {GetDownloadLink(Mirror, mod, mod.WantedVersion)}");
                    }
                }
                else
                {
                    Console.WriteLine("Downloaded sucesfully!");
                }
            }
            Console.ReadKey();
            //Console.WriteLine();
            
            
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
        static void PrintModInfo(Mod mod)
        {
            if(mod.IsInitialized)
            {
                Console.WriteLine($"Mod Title: {mod.title}");
                Console.WriteLine($"Mod Name: {mod.name}");
                Console.WriteLine($"Description: {mod.description}");
                Console.WriteLine($"Author: {mod.author}");
                Console.WriteLine($"Downloads: {mod.downloadCounter}");
                Console.WriteLine($"Most recent mod version is {mod.RecentVersion.version}");
                Console.WriteLine($"For factorio version {mod.RecentVersion.factorioVer} or higher");
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
        async static Task<List<Mod>> GetModsInfo(HttpClient client,List<string> names)
        {
            List<Mod> modList = new List<Mod>();
            foreach(string name in names)
            {
                modList.Add(await GetModInfo(client, name));
            }
            return modList;
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
                    using (FileStream fileStream = System.IO.File.Create(Path.Combine(downloadPatch, fileName)))
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
                                Helper.PrintProgressBar(totalBytesRead, fileSize);
                                lastUpdate = currentTime;
                            }
                            
                        }
                        Helper.PrintProgressBar(totalBytesRead, fileSize);
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

    }
}
