namespace BSU.CLI
{
    public static class Utils
    {
        public static string BytesToHuman(long byteCount)
        {
            string[] sizes = {"B", "KB", "MB", "GB", "TB"};
            var order = 0;
            while (byteCount >= 10000 && order < sizes.Length - 1)
            {
                order++;
                byteCount /= 1024;
            }

            return $"{byteCount:0.##} {sizes[order]}";
        }
    }
}