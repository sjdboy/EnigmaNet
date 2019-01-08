using System;
using System.IO;

namespace EnigmaNet.Utils
{
    public static class IOUtils
    {
        public static void CreateDirectoryIfNot(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
