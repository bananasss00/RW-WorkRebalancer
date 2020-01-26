using System.IO;
using System.Text;

namespace WorkRebalancer
{
    public static class Loger
    {
        private static StringBuilder sb = new StringBuilder();
        [System.Diagnostics.Conditional("DEBUG")]
        public static void AppendLine(string s) => sb.AppendLine(s);
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Append(string s) => sb.Append(s);
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Clear() => sb = new StringBuilder();
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Save(string fn) => File.WriteAllText(fn, sb.ToString());
    }
}