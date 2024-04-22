using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordServerStorage
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Bot DiscordBot = new Bot();
            _ = Task.Run(async () => { await DiscordBot.Initialize; });

            if (DiscordBot.Initiated)
            {
                Console.WriteLine("Bot has been started and we can steal a reference");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(DiscordBot));
            
        }
    }
}
