using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;

namespace Blep.Backend
{
    /// <summary>
    /// Replacement for <see cref="Debug"/> because <see cref="Debug"/> doesn't work on release build
    /// </summary>
    public static class Wood
    {
        public static void Indent()
        {
            IndentLevel++;
        }
        public static void Unindent()
        {
            IndentLevel--;
        }
        public static void WriteLineIf(bool cond, object o)
        {
            if (cond) WriteLine(o);
        }
        public static void WriteLine(object o, int AddedIndent)
        {
            IndentLevel += AddedIndent;
            WriteLine(o);
            IndentLevel -= AddedIndent;
        }
        public static void WriteLine(object o)
        {
            string result = string.Empty;
            for (int i = 0; i < IndentLevel; i++) { result += "\t"; }
            result += o?.ToString() ?? "null";
            result += "\n";
            Write(result);
        }
        public static void WriteLine()
        {
            WriteLine(string.Empty);
        }
        public static void Write(object o)
        {
            
            if (!BlepApp.UsingTUI) Console.Write(o);
            SpinUp();
            WriteQueue.Enqueue(o ?? "null");

//#error reimpl writing and io error handling, threaded
        }
        
        public static void SetNewPathAndErase(string tar)
        {
            LogPath = tar;
            File.CreateText(tar).Dispose();
        }
        public static ConcurrentQueue<object> WriteQueue { get { _wc ??= new ConcurrentQueue<object>(); return _wc; } set { _wc = value; } }
        private static ConcurrentQueue<Object> _wc = new ConcurrentQueue<object>();
        private static ConcurrentQueue<Tuple<Exception, DateTime>> _encEx = new ConcurrentQueue<Tuple<Exception, DateTime>>();
        public static string LogPath { get => LogTarget?.FullName; set { LogTarget = new FileInfo(value); } }
        public static FileInfo LogTarget;
        public static int IndentLevel { get { return _indl; } set { _indl = Math.Max(value, 0); } }
        private static int _indl = 0;

        public static void SpinUp()
        {
            Lifetime = 125;
            if (wrThr?.IsAlive ?? false) return;
            wrThr = new Thread(EternalWrite)
            {
                IsBackground = false,
                Priority = ThreadPriority.BelowNormal
            };
            wrThr.Start();
        }
        public static int Lifetime = 0;
        public static void EternalWrite()
        {
            string startMessage = $"WOOD writer thread {Thread.CurrentThread.ManagedThreadId} booted up: {DateTime.UtcNow}\n";
            Console.WriteLine(startMessage);
            WriteQueue.Enqueue(startMessage);
            while (Lifetime > 0)
            {
                Thread.Sleep(50);
                Lifetime--;
                if (LogTarget == null) continue;
                try
                {
                    using var wt = LogTarget.AppendText();
                    while (!WriteQueue.IsEmpty)
                    {
                        if (WriteQueue.TryDequeue(out var toWrite))
                        {
                            //var bytesTW = Encoding.UTF8.GetBytes(toWrite.ToString());
                            //wt.Seek(0, SeekOrigin.End);
                            wt.Write(toWrite.ToString());
                            wt.Flush();

                        }
                        //wt.Write(Encoding.UTF8.GetBytes(res.ToString()));
                    }

                    while (!_encEx.IsEmpty)
                    {
                        if (_encEx.TryDequeue(out var oldex))
                        {
                            //var bytesTW = Encoding.UTF8.GetBytes();
                            //wt.Seek(0, SeekOrigin.End);
                            wt.Write($"\nWrite exc encountered on {oldex.Item2}:\n{oldex.Item1}");
                            wt.Flush();
                        }
                    }
                }
                catch (Exception e)
                {
                    _encEx.Enqueue(new Tuple<Exception, DateTime>(e, DateTime.UtcNow));
                }
                
            }
            using (var wt = LogTarget.AppendText())
            {
                string endMessage = $"Logger thread {Thread.CurrentThread.ManagedThreadId} expired due to inactivity: {DateTime.UtcNow}\n";
                Console.WriteLine(endMessage);
                wt.Write(endMessage);
                wt.Flush();
            }
        }
        public static Thread wrThr;
    }
}
