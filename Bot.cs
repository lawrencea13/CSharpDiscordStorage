using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace DiscordServerStorage
{
    public class Bot
    {
        #region Pre-Form Initialization
        public bool Initiated { get; protected set; }

        public Bot() 
        {
            Initialize = CreateInstanceAsync();
        }

        public Task Initialize { get; }


        private async Task CreateInstanceAsync()
        {
            await Task.Delay(5);
            Initiated = true;
        }

        #endregion

        private DiscordSocketClient _client;

        public async Task StartBot()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            string token/**/;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // wait about 1 second. By the time any operations are done we should already be ready
            await Task.Delay(1000);
            var Channel = await _client.GetChannelAsync(MetaDataChannel) as IMessageChannel;

            IUserMessage msg = await Channel.GetMessageAsync(StaticMetaDataID) as IUserMessage;
            if(msg == null)
            {
                await Channel.SendMessageAsync("new message, please update your static metadataID");
                Environment.Exit(0);
            }

            try
            {
                ulong result = Convert.ToUInt64(msg.Content);
                var metaDataMsg = await Channel.GetMessageAsync(result) as IUserMessage;
                if (metaDataMsg != null)
                {
                    MetaDataMsg = metaDataMsg;
                    HasStoredMetaData = true;
                }
            }
            catch
            {
                HasStoredMetaData = false;
            }

            if (HasStoredMetaData)
            {
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(new Uri(MetaDataMsg.Attachments.ElementAt(0).Url), "metadata.json");
                }

                MetaDataStructureObject = JsonConvert.DeserializeObject<MetaDataStructure>(File.ReadAllText("metadata.json"));
                foreach(CustomDirectory dir in MetaDataStructureObject.DirectoryListings)
                {
                    if(dir.Name == "root")
                    {
                        CurrentDirectory = dir;
                        break;
                    }
                }
                if(CurrentDirectory == null && MetaDataStructureObject.DirectoryListings.Count != 0)
                {
                    CurrentDirectory = MetaDataStructureObject.DirectoryListings[0];
                }
                else if(MetaDataStructureObject.DirectoryListings.Count == 0)
                {
                    MetaDataStructureObject.DirectoryListings.Add(new CustomDirectory
                    {
                        Name = "root"
                    });
                    CurrentDirectory = MetaDataStructureObject.DirectoryListings[0];



                    await UpdateServerMetaData();
                }
            }
            else
            {
                MetaDataStructureObject = new MetaDataStructure();

                MetaDataStructureObject.DirectoryListings.Add(new CustomDirectory
                {
                    Name = "root"
                });
                CurrentDirectory = MetaDataStructureObject.DirectoryListings[0];

                await AddNewDirectory("test1", false);
                await AddNewDirectory("test2", false);
                await AddNewDirectory("test3", false);




                await UpdateServerMetaData();
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        // change to multi channel system in future
        private ulong StorageChannel/* = */;
        private ulong MetaDataChannel/* = */;
        private ulong StaticMetaDataID/* = */;
        // This changes because the bot will upload new metadata details after making changes
        // afterwards, we will simply edit the staticMetaDataID's message with the updated message ID
        private IUserMessage MetaDataMsg;
        private MetaDataStructure MetaDataStructureObject;
        private bool HasStoredMetaData = false;
        internal CustomDirectory CurrentDirectory;
        //private List<ulong> TestFile = new List<ulong>();

        public async Task AddNewDirectory(string name, bool UpdateServer = true)
        {
            CustomDirectory temp = new CustomDirectory
            {
                Name = name,
                ParentDirectory = CurrentDirectory
            };
            CurrentDirectory.SubDirectories.Add(temp);

            if (UpdateServer)
            {
                await UpdateServerMetaData();
            }
            

        }

        internal List<CustomDirectory> GetDirsInCurrentDirectory()
        {
            List<CustomDirectory> items = new List<CustomDirectory>();

            foreach(CustomDirectory dir in CurrentDirectory.SubDirectories)
            {
                items.Add(dir);
                //Console.WriteLine(dir.Name);
            }

            return items;
        }

        internal  List<CustomFile> GetFilesInCurrentDirectory()
        {
            List<CustomFile> items = new List<CustomFile>();

            foreach (CustomFile file in CurrentDirectory.MyFiles)
            {
                items.Add(file);
            }

            return items;
        }


        private async Task UpdateServerMetaData()
        {
            // called when a change happens to the server and we need to update the metadata.
            // this should be called as soon as possible and as much as possible without hindering the experience
            // Keeping this updated is what keeps the file system accurate.

            var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(MetaDataStructureObject, Formatting.None, 
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }));

            using (var fs = new FileStream("tempmetadata.json", FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }

            var Channel = await _client.GetChannelAsync(MetaDataChannel) as IMessageChannel;
            IUserMessage serverMetaData = await Channel.SendFileAsync("tempmetadata.json");

            await Channel.ModifyMessageAsync(StaticMetaDataID, msg => { msg.Content = serverMetaData.Id.ToString(); });
        }

        public async Task UploadLargeFile(List<string> FileChunks, FileInfo originalFileInfo)
        {
            // Received at step 4. This handles upload and cleanup at the same time.
            var Channel = _client.GetChannel(StorageChannel) as IMessageChannel;
            List<string> ServerMsgRefs = new List<string>();
            for(var i = 0; i < FileChunks.Count; i++)
            {
                IUserMessage msg = await Channel.SendFileAsync(FileChunks[i]);
                // get the message ID for reference later.
                ulong msgID = msg.Id;
                ServerMsgRefs.Add(msgID.ToString());
                //TestFile.Add(msgID);
                File.Delete(FileChunks[i]);
            }

            CurrentDirectory.MyFiles.Add(
                new CustomFile
                {
                    FileName = originalFileInfo.Name,
                    ServerMessages = ServerMsgRefs
                });

            await UpdateServerMetaData();

            // END step 4 and 5.
            
        }

        public async Task UploadSmallFile(FileInfo info)
        {
            var Channel = _client.GetChannel(StorageChannel) as IMessageChannel;
            IUserMessage sentFileMsg = await Channel.SendFileAsync(info.FullName);
            try
            {


                CurrentDirectory.MyFiles.Add(
                    new CustomFile 
                    {
                        FileName = info.Name,
                        ServerMessages = new List<string> { sentFileMsg.Id.ToString() },
                    });
                
                await UpdateServerMetaData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        public async Task RetrieveFile(string outputFolder, string fileName)
        {
            /* I'm writing this after writing this function.
             * There wasn't much testing done. I just kinda sent it
             * 
             * 1. Identify the associated messages that have the file we need
             * 2. Download the files to a temp directory.
             * 3. Read the downloaded files into a stream 1 by 1
             * 4. Each message read gets slapped into the final file in the final directory
             */

            List<string> Downloads = new List<string>();
            List<string> NewPaths = new List<string>();
            List<ulong> DownloadChunks = GetDownloadChunksByName(fileName);
            var Channel = _client.GetChannel(StorageChannel) as IMessageChannel;
            
            for(var i = 0;i < DownloadChunks.Count;i++)
            {
                var msg = await Channel.GetMessageAsync(DownloadChunks[i]) as IUserMessage;
                Console.WriteLine(msg.Attachments.Count);
                IAttachment chunk = msg.Attachments.ElementAt(0);
                
                Downloads.Add(chunk.Url);
            }

            using (var client = new WebClient())
            {
                int i = 0;
                foreach (string URL in Downloads)
                {
                    string tempDownloadPath = "partition" + i.ToString();
                    await client.DownloadFileTaskAsync(new Uri(URL), tempDownloadPath);
                    NewPaths.Add(tempDownloadPath);
                    i++;
                }
            }
            // just for testing
            FileStream newFile = File.Create(outputFolder + "\\" + fileName);
            int pos = 0;
            foreach(string path in NewPaths)
            {
                byte[] data = File.ReadAllBytes(path);
                await newFile.WriteAsync(data, 0, data.Length);
                pos += data.Length;
                try
                {
                    File.Delete(path);
                }
                catch(DirectoryNotFoundException)
                {
                    //ignore
                }
            }
            newFile.Dispose();
        }

        public async Task DeleteFile(string fileName)
        {
            foreach(CustomFile file in GetFilesInCurrentDirectory())
            {
                if(file.FileName == fileName)
                {
                    var Channel = _client.GetChannel(StorageChannel) as IMessageChannel;
                    foreach(string msg in file.ServerMessages)
                    {
                        ulong msgId = Convert.ToUInt64(msg);
                        await Channel.DeleteMessageAsync(msgId);
                    }

                    CurrentDirectory.MyFiles.Remove(file);
                    await UpdateServerMetaData();
                    break;
                }
            }
        }

        public void SetCurrentDirectory(string currentDirectory)
        {
            foreach(CustomDirectory dir in GetDirsInCurrentDirectory())
            {
                if(dir.Name == currentDirectory)
                {
                    CurrentDirectory = dir;
                    break;
                }
            }
        }

        public async Task DeleteFolder(string folderName)
        {
            foreach(CustomDirectory dir in GetDirsInCurrentDirectory())
            {
                if(dir.Name == folderName)
                {
                    if(dir.MyFiles.Count != 0)
                    {
                        foreach(CustomFile file in dir.MyFiles)
                        {
                            await DeleteFile(file.FileName);
                        }
                    }
                    if(dir.MyFiles.Count != 0)
                    {
                        // recursive is spooky
                        await DeleteFolder(folderName);
                    }

                    // Don't need to update current dir since we aren't deleting the dir we are in
                    
                    CurrentDirectory.SubDirectories.Remove(dir);
                    break;
                }
            }
        }

        private List<ulong> GetDownloadChunksByName(string fileName)
        {
            // This will likely get updated in the future
            // The reason it takes this path is because the old system didn't use a "current directory"
            // When handling things in the UI we will likely be able to reference the specific object utilized
            List<ulong> returnList = new List<ulong>();
            foreach(CustomFile file in CurrentDirectory.MyFiles)
            {
                if(file.FileName == fileName)
                {
                    foreach(string messageID in file.ServerMessages)
                    {
                        returnList.Add(Convert.ToUInt64(messageID));
                    }
                    break;
                }
            }
            return returnList;
        } 

    }
}
