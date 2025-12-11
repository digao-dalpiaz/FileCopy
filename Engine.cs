using System.Text.Json;

namespace FileCopy
{
    internal class Engine
    {

        class Ctrl
        {
            public DateTime Timestamp { get; set; }
        }

        public static async Task CopyWithResumeAsync(string sourcePath, string destinationFolder, int bufferSize = 1024 * 1024)
        {
            if (!File.Exists(sourcePath)) throw new Wrong("Source file not found");
            if (!Directory.Exists(destinationFolder)) throw new Wrong("Destination folder not found");

            long sourceLength = new FileInfo(sourcePath).Length;
            long destLength = 0;

            string destPath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
            string destPathTmp = destPath + ".copying";
            string ctrlFile = destPath + ".ctrl";

            if (File.Exists(destPath)) throw new Wrong("Destination file already exists");

            var sourceTimestamp = File.GetLastWriteTime(sourcePath);

            Ctrl ctrl;
            if (File.Exists(destPathTmp))
            {
                Console.WriteLine("Continuing previous copy....");

                destLength = new FileInfo(destPathTmp).Length;

                if (!File.Exists(ctrlFile)) throw new Wrong("Control file not found");
                ctrl = JsonSerializer.Deserialize<Ctrl>(File.ReadAllText(ctrlFile)) ?? throw new Exception("Json nulo");
                if (ctrl.Timestamp != sourceTimestamp) throw new Wrong("Source file changed since last copy");
            }
            else
            {
                ctrl = new Ctrl { Timestamp = sourceTimestamp };
                File.WriteAllText(ctrlFile, JsonSerializer.Serialize(ctrl));
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

                    if (File.GetLastWriteTime(sourcePath) != sourceTimestamp) throw new Wrong("Source file change during copy");
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
            File.SetLastWriteTime(destPath, sourceTimestamp);
        }

        private static void DrawProgressBar(long current, long total, DateTime startTime, long bytesRead)
        {
            double percent = (double)current / total;
            int barWidth = 50; //bar length

            int filled = (int)(percent * barWidth);
            int empty = barWidth - filled;

            int baseLine = Console.CursorTop;

            Console.SetCursorPosition(0, baseLine);

            Console.Write("[");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('█', filled));

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(new string('░', empty));

            Console.ResetColor();
            Console.Write("]");

            var elapsedTime = DateTime.Now - startTime;
            var remainingBytes = total - current;
            var remainingTime = TimeSpan.FromSeconds(elapsedTime.TotalSeconds * remainingBytes / bytesRead);

            Console.SetCursorPosition(0, baseLine + 1);
            Console.Write($"{ToMB(current)} of {ToMB(total)} ({percent * 100:0.00}%)".PadRight(Console.WindowWidth));

            Console.SetCursorPosition(0, baseLine + 2);
            Console.Write($"{ToTime(elapsedTime)} elapsed | {ToTime(remainingTime)} remaining".PadRight(Console.WindowWidth));
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
