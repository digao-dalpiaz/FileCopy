namespace FileCopy
{
    internal class Engine
    {

        public async Task CopyWithResumeAsync(string sourcePath, string destinationFolder, int bufferSize = 1024 * 1024)
        {
            if (!File.Exists(sourcePath)) throw new Wrong("Source file not found");
            if (!Directory.Exists(destinationFolder)) throw new Wrong("Destination folder not found");

            long sourceLength = new FileInfo(sourcePath).Length;
            long destLength = 0;

            string destPath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
            string destPathTmp = destPath + ".copying";

            if (File.Exists(destPath)) throw new Wrong("Destination file already exists");

            if (File.Exists(destPathTmp))
            {
                Console.WriteLine("Continuing previous copy....");

                destLength = new FileInfo(destPathTmp).Length;
            }

            using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var dest = new FileStream(destPathTmp, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                source.Seek(destLength, SeekOrigin.Begin);

                var buffer = new byte[bufferSize];
                int bytesRead;

                var startTime = DateTime.Now;
                long totalBytesRead = 0;

                void UpdateBar()
                {
                    DrawProgressBar(destLength, sourceLength, startTime, totalBytesRead);
                }

                var lastUpdate = DateTime.MinValue;

                while ((bytesRead = await source.ReadAsync(buffer)) > 0)
                {
                    await dest.WriteAsync(buffer);
                    destLength += bytesRead;
                    totalBytesRead += bytesRead;

                    if ((DateTime.Now - lastUpdate).TotalMilliseconds > 1000)
                    {
                        UpdateBar();
                        lastUpdate = DateTime.Now;
                    }
                }

                UpdateBar();
            }

            File.Move(destPathTmp, destPath);
            File.SetLastWriteTime(destPath, File.GetLastWriteTime(sourcePath));
        }

        private void DrawProgressBar(long current, long total, DateTime startTime, long bytesRead)
        {
            double percent = (double)current / total;
            int barWidth = 50; //bar length

            int filled = (int)(percent * barWidth);
            int empty = barWidth - filled;

            Console.SetCursorPosition(0, Console.CursorTop);

            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('█', filled));

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(new string('░', empty));

            Console.ResetColor();

            var elapsedTime = DateTime.Now - startTime;
            var remainingBytes = total - current;
            var remainingTime = elapsedTime * remainingBytes / bytesRead;
            Console.Write($"] {ToMB(current)} of {ToMB(total)} ({percent * 100:0.00}%) {ToTime(elapsedTime)} {ToTime(remainingTime)}");
        }

        private static string ToMB(long size)
        {
            return ((double)size / 1024 / 1024).ToString("0.00") + " MB";
        }

        private static string ToTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        }

    }
}
