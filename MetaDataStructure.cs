using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordServerStorage
{
    internal class MetaDataStructure
    {
        public List<CustomDirectory> DirectoryListings = new List<CustomDirectory>();

        //deprecated, only need DirectoryListings, will remove once fully out
        //public List<CustomFile> FileServerInfo = new List<CustomFile>();
    }

    internal class CustomDirectory
    {
        public string Name { get; set; }
        public List<CustomDirectory> SubDirectories = new List<CustomDirectory>();
        public List<CustomFile> MyFiles = new List<CustomFile>();
    }

    internal class CustomFile
    {
        //// Replaces the normal file handling since we have custom processes for moving/adding/deleting
        /// <summary>
        /// Placing thoughts on how this will work here, likely gonna change as I put this thing together.
        /// This type of object will only exist when dealing with the actual "file"
        /// Meaning when using the UI to navigate the file structure, the only way the UI knows what to load is based on server side directory info
        /// Once file properties are pulled up, we may load a full CustomFile object that holds all of the necessary info to identify the pieces of a file.
        /// Additionally, these objects may be stored in serialized format, also on the discord server. The bot technically doesn't need to have all this crap in memory
        /// </summary>
        /// 

        public string FileName { get; set; }
        public List<string> ServerMessages = new List<string>();
    }

    
}
