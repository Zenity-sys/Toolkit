using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace KavrosClueHelper
{
    /// <summary>Names for Kavros clients: Companion's client-names.json if present,
    /// otherwise the process <c>JX_DISPLAY_NAME</c>, otherwise "Client N".</summary>
    internal static class ClientIdentity
    {
        public static string Label(uint pid, int index, string title)
        {
            string fromShare = NameFromShare(pid);
            if (!string.IsNullOrEmpty(fromShare)) return fromShare;
            string fromEnv = DisplayName((int)pid);
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;
            return "Client " + (index + 1).ToString();
        }

        static string sharePath;
        static DateTime shareTime;
        static Dictionary<int, string> shareNames = new Dictionary<int, string>();

        static string NameFromShare(uint pid)
        {
            if (pid == 0) return null;
            try
            {
                if (sharePath == null)
                    sharePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KavrosOverlay", "client-names.json");
                if (File.Exists(sharePath))
                {
                    DateTime written = File.GetLastWriteTimeUtc(sharePath);
                    if (written != shareTime)
                    {
                        shareTime = written;
                        shareNames = ParseShare(File.ReadAllText(sharePath, Encoding.UTF8));
                    }
                    string name;
                    if (shareNames.TryGetValue((int)pid, out name) && !string.IsNullOrEmpty(name)) return name;
                }
            }
            catch (Exception) { }
            return null;
        }

        static Dictionary<int, string> ParseShare(string json)
        {
            var map = new Dictionary<int, string>();
            if (string.IsNullOrEmpty(json)) return map;
            foreach (Match obj in Regex.Matches(json, "\\{[^}]+\\}"))
            {
                Match pid = Regex.Match(obj.Value, "\"pid\"\\s*:\\s*(-?\\d+)");
                Match name = Regex.Match(obj.Value, "\"name\"\\s*:\\s*\"([^\"]*)\"");
                int id;
                if (pid.Success && name.Success && int.TryParse(pid.Groups[1].Value, out id) && name.Groups[1].Value.Length > 0)
                    map[id] = name.Groups[1].Value;
            }
            return map;
        }

        const string DisplayNameVariable = "JX_DISPLAY_NAME";

        public static string DisplayName(int pid)
        {
            try
            {
                Dictionary<string, string> map = ReadBlock(pid);
                if (map == null) return null;
                string value;
                if (!map.TryGetValue(DisplayNameVariable, out value)) return null;
                return Sanitize(value);
            }
            catch (Exception) { return null; }
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            StringBuilder sb = new StringBuilder(value.Length);
            bool space = false;
            int i;
            for (i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < 32) continue;
                if (char.IsWhiteSpace(c))
                {
                    if (sb.Length == 0 || space) continue;
                    space = true;
                    sb.Append(' ');
                    continue;
                }
                space = false;
                sb.Append(c);
                if (sb.Length >= 24) break;
            }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? null : s;
        }

        const int ProcessBasicInfoClass = 0;
        const int ProcessQueryInformation = 0x0400;
        const int ProcessVmRead = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(int access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr process, IntPtr address, byte[] buffer, int size, out IntPtr read);
        [DllImport("ntdll.dll")]
        static extern int NtQueryInformationProcess(IntPtr process, int clazz, ref ProcessBasicInformation info, int size, IntPtr retLen);

        [StructLayout(LayoutKind.Sequential)]
        struct ProcessBasicInformation
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }

        static Dictionary<string, string> ReadBlock(int pid)
        {
            if (pid <= 0 || IntPtr.Size != 8) return null;
            IntPtr proc = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, pid);
            if (proc == IntPtr.Zero) return null;
            try
            {
                ProcessBasicInformation pbi = new ProcessBasicInformation();
                int st = NtQueryInformationProcess(proc, ProcessBasicInfoClass, ref pbi, Marshal.SizeOf(typeof(ProcessBasicInformation)), IntPtr.Zero);
                if (st != 0 || pbi.PebBaseAddress == IntPtr.Zero) return null;
                IntPtr parameters = ReadPtr(proc, new IntPtr(pbi.PebBaseAddress.ToInt64() + 0x20));
                if (parameters == IntPtr.Zero) return null;
                IntPtr env = ReadPtr(proc, new IntPtr(parameters.ToInt64() + 0x80));
                if (env == IntPtr.Zero) return null;
                byte[] block = ReadUntilDoubleNull(proc, env, 128 * 1024);
                return block == null ? null : ParseBlock(block);
            }
            catch (Exception) { return null; }
            finally { CloseHandle(proc); }
        }

        static IntPtr ReadPtr(IntPtr proc, IntPtr address)
        {
            byte[] buf = new byte[8];
            IntPtr n;
            if (!ReadProcessMemory(proc, address, buf, 8, out n) || n.ToInt64() != 8) return IntPtr.Zero;
            return new IntPtr(BitConverter.ToInt64(buf, 0));
        }

        static byte[] ReadUntilDoubleNull(IntPtr proc, IntPtr address, int maxBytes)
        {
            List<byte> chunks = new List<byte>(4096);
            byte[] buf = new byte[4096];
            long offset = 0;
            int zeros = 0;
            while (offset < maxBytes)
            {
                IntPtr n;
                int want = (int)Math.Min(buf.Length, maxBytes - offset);
                if (!ReadProcessMemory(proc, new IntPtr(address.ToInt64() + offset), buf, want, out n) || n.ToInt64() <= 0) break;
                int got = (int)n.ToInt64();
                int i;
                for (i = 0; i < got; i++)
                {
                    chunks.Add(buf[i]);
                    if (buf[i] == 0)
                    {
                        zeros++;
                        if (zeros >= 4)
                        {
                            byte[] arr = new byte[chunks.Count];
                            chunks.CopyTo(arr);
                            return arr;
                        }
                    }
                    else zeros = 0;
                }
                offset += got;
                if (got < want) break;
            }
            if (chunks.Count < 4) return null;
            byte[] all = new byte[chunks.Count];
            chunks.CopyTo(all);
            return all;
        }

        static Dictionary<string, string> ParseBlock(byte[] block)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (block == null || block.Length < 2) return map;
            int start = 0;
            while (start + 1 < block.Length)
            {
                int end = start;
                while (end + 1 < block.Length && !(block[end] == 0 && block[end + 1] == 0)) end += 2;
                int bytes = end - start;
                if (bytes <= 0) break;
                string entry = Encoding.Unicode.GetString(block, start, bytes);
                int eq = entry.IndexOf('=');
                if (eq > 0)
                {
                    string key = entry.Substring(0, eq);
                    if (!map.ContainsKey(key)) map[key] = entry.Substring(eq + 1);
                }
                start = end + 2;
            }
            return map;
        }
    }
}
