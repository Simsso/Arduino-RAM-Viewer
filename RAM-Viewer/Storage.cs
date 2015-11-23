using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace RAM_Viewer
{
    public static class Storage
    {
        public const string ProgramPath = @"RAM-Viewer";
        public const string MemoryStampsPath = ProgramPath + @"\MemoryStamps\", WatchListsPath = ProgramPath + @"\WatchLists\", Extension = ".bin";


        public static string[] AvailableMemoryStamps()
        {
            CreateDirectories();
            try
            {
                return Directory.GetFiles(MemoryStampsPath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string[] AvailableWatchLists()
        {
            CreateDirectories();
            try
            {
                return Directory.GetFiles(WatchListsPath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SaveMemoryStamps(List<MemoryStamp> MemoryStamps)
        {
            CreateDirectories();
            SerializeObject(MemoryStamps, MemoryStampsPath + DateTime.Now.Ticks.ToString() + Extension);
        }

        public static void SaveWatchList(List<WatchListVariable> WatchList)
        {
            CreateDirectories();
            SerializeObject(WatchList, WatchListsPath + DateTime.Now.Ticks.ToString() + Extension);
        }

        public static List<MemoryStamp> LoadMemoryStamps(string Name)
        {
            return (List<MemoryStamp>)DeserializeObject(MemoryStampsPath + Name + Extension);
        }

        public static List<WatchListVariable> LoadWatchList(string Name)
        {
            return (List<WatchListVariable>)DeserializeObject(WatchListsPath + Name + Extension);
        }

        private static void CreateDirectories()
        {
            // program path
            if (!Directory.Exists(ProgramPath))
            {
                try
                {
                    Directory.CreateDirectory(ProgramPath);
                }
                catch (Exception)
                {
                }
            }

            // memory stamp path
            if (!Directory.Exists(MemoryStampsPath))
            {
                try
                {
                    Directory.CreateDirectory(MemoryStampsPath);
                }
                catch (Exception)
                {
                }
            }

            // watch list path
            if (!Directory.Exists(WatchListsPath))
            {
                try
                {
                    Directory.CreateDirectory(WatchListsPath);
                }
                catch (Exception)
                {
                }
            }
        }

        private static void SerializeObject(object Object, string Path)
        {
            CreateDirectories();
            try 
	        {	  
                IFormatter Formatter = new BinaryFormatter();
                Stream Stream = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.None);
                Formatter.Serialize(Stream, Object);
                Stream.Close();
	        }
	        catch (Exception e)
	        {
                MessageBox.Show(e.Message);
		        return;
	        }
        }

        private static object DeserializeObject(string Path)
        {
            try
            {
                IFormatter Formatter = new BinaryFormatter();
                Stream Stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                object Object = (object) Formatter.Deserialize(Stream);
                Stream.Close();
                return Object;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
        }
    }
}
