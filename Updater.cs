using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace FactorioUpdater
{
    public class Updater
    {
        const string Mirror = "https://mods-storage.re146.dev/";

        public async Task Main()
        {
            string ver = null;
            while (true)
            {

                Console.WriteLine("### 1.Update mods to most recent version.\n### 2.Update mods to specific factorio version.");
                var key = Console.ReadKey();
                if (key.KeyChar == '1')
                {

                    break;
                }
                else if (key.KeyChar == '2')
                {
                    Console.WriteLine("### >>> Write factorio version(you can write only first 2 numbers) : ");
                    ver = Console.ReadLine();
                    ver.Trim();
                    break;
                }
                Console.Clear();
            }





            Console.WriteLine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            string currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string downloadPatch = Path.Combine(currDir, "Downloaded_Mods");
            string BackupPath = Path.Combine(currDir, "Old_Mods");
            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                client.DefaultRequestHeaders.Add("Pragma", "no-cache");

                List<Mod> toUpdate = new List<Mod>();
                Dictionary<string, string> inf = Helper.GetModNamesFromDir(currDir);// Used dictionary to avoid duplicates
                Console.WriteLine("Mods to update :");
                List<string> move = new List<string>();

                foreach (var i in inf) // key represents mod name value represents mod version
                {
                    Mod info = await GetModInfo(client, i.Key);

                    if (info == null)
                    {
                        Console.WriteLine($"Something went wrong with fetching this mod versions : {i.Key}");
                        continue;
                    }



                    if (ver == null)
                    {
                        if (Helper.IsVersionSmaller(i.Value, info.RecentVersion.version))
                        {
                            info.WantedVersion = info.RecentVersion;
                            Console.WriteLine($"{i.Key} : {i.Value} >> {info.RecentVersion.version} >> for factorio {info.RecentVersion.factorioVer}");
                        }
                    }
                    else
                    {

                        Mod.Version newVersion = info.versions
                            .Where(v => v.factorioVer == ver)
                            .LastOrDefault();

                        if (newVersion == null)
                        {
                            Console.WriteLine($"{i.Key} : {i.Value} >> Failed to find needed factorio version for this mod!!!");
                            move.Add(i.Key);
                            continue;
                        }

                        if (i.Value != newVersion.version)
                        {
                            info.WantedVersion = newVersion;
                        }
                        else
                        {
                            Console.WriteLine($"{i.Key} is most recent version for factorio {newVersion.factorioVer}!");
                            continue;
                        }

                        Console.WriteLine($"{i.Key} : {i.Value} >> {info.WantedVersion.version} >> for factorio {info.WantedVersion.factorioVer}");
                    }
                    toUpdate.Add(info);
                    move.Add(i.Key);
                }

                Console.WriteLine("Press enter to start update process.");
                Console.ReadKey();

                Directory.CreateDirectory(BackupPath);
                Directory.CreateDirectory(downloadPatch);
                Helper.MoveMods(move, currDir, BackupPath);

                var failed = await DownloadMods(client, Mirror, toUpdate, downloadPatch);

                if (failed.Count > 0)
                {
                    Console.WriteLine($"{failed.Count} of download mods failed there are a list of them!");
                    foreach (Mod mod in failed)
                    {
                        Console.WriteLine($"Name : {mod.name} Download link {GetDownloadLink(Mirror, mod, mod.RecentVersion)}");
                    }
                }
                else
                {
                    Console.WriteLine("Downloaded successfully!");
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
            if (mod.IsInitialized)
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


        async static Task<Mod> GetModInfo(HttpClient client, string name)
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
        async static Task<List<Mod>> GetModsInfo(HttpClient client, List<string> names)
        {
            List<Mod> modList = new List<Mod>();
            foreach (string name in names)
            {
                modList.Add(await GetModInfo(client, name));
            }
            return modList;
        }


        async static Task<bool> DownloadMod(HttpClient client, Mod mod, string mirror, string downloadPatch)
        {
            const int updateInterval = 10;
            DateTime lastUpdate = DateTime.MinValue;

            Mod.Version ver = mod.WantedVersion != null ? mod.WantedVersion : mod.RecentVersion;
            string fileName = $"{mod.name}_{ver.version}.zip";
            string url = GetDownloadLink(Mirror, mod, ver);
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
        async static Task<List<Mod>> DownloadMods(HttpClient client, string mirror, List<Mod> mods, string downloadPatch)
        {
            List<Mod> failed = new List<Mod>();
            for (int i = 0; i < mods.Count; i++)
            {
                Console.Clear();
                Console.WriteLine($"Downloading mod ({i + 1}/{mods.Count})");
                if (!await DownloadMod(client, mods[i], mirror, downloadPatch))
                {
                    failed.Add(mods[i]);
                }
            }
            return failed;
        }
        static string GetDownloadLink(string mirror, Mod mod, Mod.Version ver)
        {
            if (!mod.IsInitialized)
                throw new Exception("Mod is not initialized!!");
            return $"{mirror}{mod.name}/{ver.version}.zip";
        }
    }
}
