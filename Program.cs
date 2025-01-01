using System;

namespace FactorioUpdater
{


    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine(">>> 1. Update factorio mods.\n>>> 2. Download modpack.");
                var key = Console.ReadKey();
                if (key.KeyChar == '1')
                {
                    var updater = new Updater();
                    var _ = updater.Main();
                    _.Wait();
                }
                if (key.KeyChar == '2')
                    Console.WriteLine("   ...   ");
                Console.Clear();
            }

        }





    }
}
