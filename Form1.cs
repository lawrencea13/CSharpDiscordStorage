using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;

namespace DiscordServerStorage
{
    public partial class Form1 : Form
    {
        // this is the reference that allows the form to interact with the server
        public Bot Interface;

        public Form1(Bot DiscordInterface)
        {
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            Interface = DiscordInterface;
            InitializeComponent();
            
            vScrollBar1.Value = panel1.VerticalScroll.Value;
            vScrollBar1.Minimum = panel1.VerticalScroll.Minimum;
            vScrollBar1.Maximum = panel1.VerticalScroll.Maximum;
            vScrollBar1.Scroll += VScrollBar1_Scroll;
            panel1.ControlAdded += Panel1_ControlAdded;
            panel1.ControlRemoved += Panel1_ControlRemoved;
            
        }

        private void Panel1_ControlRemoved(object sender, ControlEventArgs e)
        {
            
        }

        private void Panel1_ControlAdded(object sender, ControlEventArgs e)
        {
            
        }

        private void VScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            panel1.VerticalScroll.Value = vScrollBar1.Value;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) 
            {
                // Don't want to do if I am reading large files lmao
                //byte[] FileData = File.ReadAllBytes(file);
                FileInfo fileInfo = new FileInfo(file);
                //Console.WriteLine((fileInfo.Length / 1024).ToString());
                //Console.WriteLine(FileData.Length / 1024);
                if(fileInfo.Length / 1000 > 25000)
                {
                    // we will handle the file completely within the particular method, only need to pass fileinfo.
                    ProcessLargeFile(fileInfo);
                }
                else
                {
                    ProcessSmallFile(fileInfo);
                }
                
            }

        }

        public void PopuplateFoldersAndFiles()
        {

            List<CustomDirectory> currentFolders = Interface.GetDirsInCurrentDirectory();
            List<CustomFile> currentFiles = Interface.GetFilesInCurrentDirectory();

            int i = 0;

            foreach(CustomDirectory dir in currentFolders)
            {

                Panel panel = new Panel
                {
                    Location = new Point(10, (i * 50) + 10),
                    Height = 40,
                    Width = 600,
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                };

                panel1.Controls.Add(panel);
                
                Label label = new Label
                {
                    Location = panel.Location,
                    Text = currentFolders[i].Name,
                    Dock = DockStyle.Fill,
                };
                panel.Controls.Add(label);
                label.BringToFront();

                //panel.Dispose();
                i++;
            }

            int j = 0;

            foreach(CustomFile file in currentFiles)
            {
                Panel panel = new Panel
                {
                    Location = new Point(10, (i * 50) + 10),
                    Height = 40,
                    Width = 600,
                    BackColor = Color.White,
                    ForeColor = Color.Black,
                };

                panel1.Controls.Add(panel);

                Label label = new Label
                {
                    Location = panel.Location,
                    Text = currentFiles[j].FileName,
                    Dock = DockStyle.Fill,
                };
                panel.Controls.Add(label);
                label.BringToFront();

                //panel.Dispose();
                i++;
                j++;
            }

            

        }

        private void ProcessLargeFile(FileInfo info)
        {
            /* Gonna write my thought process before doing this. 
             * I don't believe the bot needs to be privy to the fact that this occurs
             * Just that there are a bunch of pieces that get sent through that need to be associated and ordered correctly
             * 1. Break the file into 25 mb pieces with the last being the smallest if applicable
             * 2. Write all of these pieces as temporary files
             * 3. Store the file references as a list
             * 4. Send this list of files to the bot to upload along with original file info e.g. the name
             * 5. Have bot upload pieces, upon completion, clear out the temporary files
             */

            // Step 1: Break that file up into PIECES
            const int MAX_BUFFER = 26214400; //25mb
            byte[] buffer = new byte[MAX_BUFFER];
            List<string> TempFileReferences = new List<string>();

            using (Stream input = File.OpenRead(info.FullName))
            {
                int index = 0;
                while (input.Position < input.Length)
                {
                    // Step 3 is happening first I guess...Add to list of references.
                    // We'll need to change where these files are saved, but that won't change how the next part works
                    string tempFileName = "testFile" + index.ToString();
                    TempFileReferences.Add(tempFileName);
                    // Step 2: Write those pieces into individual temporary files
                    using (Stream output = File.Create(tempFileName))
                    {
                        int chunkBytesRead = 0;
                        while (chunkBytesRead < MAX_BUFFER)
                        {
                            int bytesRead = input.Read(buffer,
                                                       chunkBytesRead,
                                                       MAX_BUFFER - chunkBytesRead);

                            if (bytesRead == 0)
                            {
                                break;
                            }
                            chunkBytesRead += bytesRead;
                        }
                        output.Write(buffer, 0, chunkBytesRead);
                    }
                    index++;
                }
            }
            // END of Step 1, 2, and 3.
            // Send to bot for step 4 and 5
            _ = Task.Run(async () => { await Interface.UploadLargeFile(TempFileReferences, info); });
        }

        private void ProcessSmallFile(FileInfo info)
        {
            // small file doesn't really need processing, at least for now. It should simply be able to be uploaded
            // So far, this just simply works. We're gonna let the bot handle the meta data
            // We may also need to pass through more info for said meta data
            _ = Task.Run(async () => { 
                await Interface.UploadSmallFile(info);
                PopuplateFoldersAndFiles();
            });
        }


        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await Task.Run(async () => { await Interface.StartBot(); });
            // Used for testing.
            //await Task.Run(async () => { await Interface.UploadFile(new CustomFile()); });
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(async () => { await Interface.RetrieveFile(KnownFolders.Downloads.Path, "testvideo.mp4"); });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PopuplateFoldersAndFiles();
        }

        private void panel1_Scroll(object sender, ScrollEventArgs e)
        {
             vScrollBar1.Value = panel1.VerticalScroll.Value;
        }
    }
}
