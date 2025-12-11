using FileCopy;

try
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("===============================================");
    Console.WriteLine("=============== DIGAO FILE COPY ===============");
    Console.WriteLine("===============================================");
    Console.ResetColor();

    Console.WriteLine();

    string? GetArg(int index)
    {
        if (args.Length < index+1) return null;
        return args[index];
    }

    var sourceFile = GetArg(0);
    var destinationFolder = GetArg(1);

    if (string.IsNullOrEmpty(sourceFile))
        throw new Wrong("Source file not specified");

    if (string.IsNullOrEmpty(destinationFolder))
        destinationFolder = Directory.GetCurrentDirectory();

    Console.WriteLine("Source file: " + sourceFile);
    Console.WriteLine("Destination folder: " + destinationFolder);

    Console.WriteLine();

    await new Engine().CopyWithResumeAsync(sourceFile, destinationFolder);
}
catch (Exception ex)
{
    Environment.ExitCode = 9;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(
        ex is Wrong ? 
        "ERROR: " + ex.Message 
        :
        "FATAL ERROR: " + ex);
    Console.ResetColor();
}
