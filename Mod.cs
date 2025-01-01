using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

internal class Mod
{
    public class Version
    {

        public string version { get; set; }
        public string downURL { get; set; }
        public string fileName { get; set; }
        public string factorioVer { get; set; }
    }

    public string title { get; private set; }
    public string description { get; private set; }
    public string author { get; private set; }
    public string name { get; private set; }
    public int downloadCounter { get; private set; }
    public List<Version> versions = null;
    public Version RecentVersion = null;
    public Version WantedVersion = null;

    public bool IsInitialized { get; private set; }

    public Mod(JObject obj)
    {
        IsInitialized = false;
        try
        {
            title = obj["title"]?.ToString();
            description = obj["summary"]?.ToString();
            author = obj["owner"]?.ToString();
            downloadCounter = obj["downloads_count"]?.Value<int>() ?? 0;
            name = obj["name"]?.ToString();

            versions = new List<Version>();
            JArray releasesArray = obj["releases"] as JArray;
            if (releasesArray != null)
            {
                foreach (JObject release in releasesArray)
                {
                    Version ver = new Version();
                    ver.downURL = release["download_url"]?.ToString();
                    ver.fileName = release["download_url"]?.ToString();
                    ver.version = release["version"]?.ToString();
                    ver.factorioVer = release["info_json"]?["factorio_version"].ToString();
                    versions.Add(ver);
                }
            }
            else
            {
                throw new Exception($"Failed to get versions {name} , {title}");
            }
            IsInitialized = true;
            RecentVersion = versions.Last();
        }
        catch (JsonReaderException jsonEx)
        {
            // Handle JSON parsing related exceptions
            Console.WriteLine($"JSON parsing error: {jsonEx.Message}");
        }
        catch (NullReferenceException nullRefEx)
        {
            // Handle null reference exceptions (e.g., accessing a non-existent property)
            Console.WriteLine($"Null reference error: {nullRefEx.Message}");
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}