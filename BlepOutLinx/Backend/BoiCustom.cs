using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Blep.Backend
{
    public static class BoiCustom
    {
        /// <summary>
        /// Compares two byte arrays.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns><c>true</c> if arrays are identical; <c>false</c> otherwise.</returns>
        public static bool BOIC_Bytearr_Compare(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length == 0 || b.Length == 0) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        /// <summary>
        /// Recursively moves a directory and all its contents into a new location.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>Number of IO errors encountered during the operation. </returns>
        public static int BOIC_RecursiveDirectoryCopy(string from, string to)
        {
            int errc = 0;
            DirectoryInfo din = new DirectoryInfo(from);
            DirectoryInfo dout = new DirectoryInfo(to);
            if (!din.Exists) { throw new IOException($"An attempt to copy a nonexistent directory ({from}) to {to} has occured."); }
            if (!dout.Exists) Directory.CreateDirectory(to);
            foreach (FileInfo fi in din.GetFiles())
            {
                try { File.Copy(fi.FullName, Path.Combine(to, fi.Name)); }
                catch (IOException ioe)
                {
                    Wood.Write("Could not copy a file during recursive copy process");
                    Wood.WriteLine(ioe, 1);
                    errc++;
                }

            }
            foreach (DirectoryInfo di in din.GetDirectories())
            {
                try { errc += BOIC_RecursiveDirectoryCopy(di.FullName, Path.Combine(to, di.Name)); }
                catch (IOException ioe)
                {
                    Wood.Write("Could not copy a subfolder during recursive copy process");
                    Wood.WriteLine(ioe, 1);

                }

            }
            return errc;
        }
        public static List<Task> EnqueueRecursiveCopy(this DirectoryInfo from, string to)
        {
            if (from == null || to == null) throw new ArgumentNullException();
            return EnqueueRecursiveCopy(from, new DirectoryInfo(to));
        }
        public static List<Task> EnqueueRecursiveCopy(this DirectoryInfo from, DirectoryInfo to)
        {
            if (from == null || to == null) throw new ArgumentNullException();
            if (!from.Exists) throw new ArgumentException("Can not copy from a nonexistent directory!");
            var res = new List<Task>();
            if (!to.Exists) to.Create();
            foreach (var sdir in from.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                res.AddRange(sdir.EnqueueRecursiveCopy(Path.Combine(to.FullName, sdir.Name)));
            }
            foreach (var file in from.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var nt = new Task(() => file.CopyTo(Path.Combine(to.FullName, file.Name)));
                nt.Start();
                res.Add(nt);
            }

            return res;
        }
        public static string ContentsAsStringOrNothing(string uri)
        {
            try
            {
                return File.Exists(uri) ? File.ReadAllText(uri) : null;
            }
            catch
            {
                return null;
            }
        }
        public static bool TryWriteText(string uri, string contents)
        {
            try
            {
                File.WriteAllText(uri, contents);
                return true;
            }
            catch
            {
                return false;
            }
        }
        //TODO: what the fuck is wrong with this
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "AllocConsole")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachConsole([MarshalAs(UnmanagedType.I4)]int dwProcessId);


        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Error)]
        public static extern int GetLastError();
    }
}
