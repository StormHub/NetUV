// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.InteropServices;

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

        public static IntPtr GetHandle(Socket socket)
        {
            // https://github.com/dotnet/corefx/issues/6807
            // Until ths handle instance is exposed as scheduled to be .NET standard 2.0
            // we have no choice but reflection.
            FieldInfo fieldInfo =
                typeof(Socket).GetField("m_Handle", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? typeof(Socket).GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic);

            var safeHandle = (SafeHandle)fieldInfo.GetValue(socket);
            return safeHandle.DangerousGetHandle();
        }

    }
}
