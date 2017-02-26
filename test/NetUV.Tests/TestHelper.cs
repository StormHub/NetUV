// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.IO;

    static class TestHelper
    {
        public static string RootSystemDirectory() => Path.GetPathRoot(Path.GetTempPath());

        public static string CreateTempDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static string CreateRandomDirectory(string path)
        {
            string directory = Path.Combine(path, Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string CreateTempFile(string directory)
        {
            string fileName = Path.Combine(directory, Path.GetRandomFileName());
            using (File.Create(fileName))
            {
                // NOP
            }
            
            return fileName;
        }

        public static FileStream OpenTempFile()
        {
            string directory = CreateTempDirectory();
            string fileName = Path.Combine(directory, Path.GetRandomFileName());
            FileStream file = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            return file;
        }

        public static void CreateFile(string fullName)
        {
            if (File.Exists(fullName))
            {
                return;
            }
            using (File.Create(fullName))
            {
                // NOP
            }
        }

        public static string[] GetFiles(string directory) => 
            Directory.Exists(directory) ? Directory.GetFiles(directory) : new string[0];

        public static void DeleteFile(string fullName)
        {
            if (File.Exists(fullName))
            {
                File.Delete(fullName);
            }
        }

        public static void TouchFile(string fullName)
        {
            using (File.Open(fullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // NOP
            }

            File.SetLastWriteTimeUtc(fullName, DateTime.UtcNow);
        }
    }
}
