using GzipTool.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GzipTool;

internal static class Program
{
    private const int SUCCESS = 0;
    private const int FAILURE = 1;

    private const string Usage = "Use executable as below:\n" +
                                 "gziptool.exe compress inputFilePath outputFilePath\n" +
                                 "gziptool.exe decompress inputFilePath outputFilePath";

    public static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine($"Invalid number of arguments... Expecting 3 arguments.{Environment.NewLine}{Usage}");
            return FAILURE;
        }

        CompressionMode compressionMode;

        if (args[0].Equals("compress", StringComparison.OrdinalIgnoreCase))
            compressionMode = CompressionMode.Compress;
        else if (args[0].Equals("decompress", StringComparison.OrdinalIgnoreCase))
            compressionMode = CompressionMode.Decompress;
        else
        {
            Console.WriteLine($"Couldn't parse program operation mode...{Environment.NewLine}{Usage}");
            return FAILURE;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
            Console.CancelKeyPress += OnConsoleOnCancelKeyPress;

            var result = Run(compressionMode, inputPath, outputPath, cts.Token);

            Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;
                
            return result;

            void OnConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                cts.Cancel();
            }
        }
    }

    private static int Run(CompressionMode compressionMode, string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            ParallelFileIO parallelWorker;
            if (compressionMode == CompressionMode.Compress)
                parallelWorker = new ParallelFileCompress(inputPath, outputPath);
            else if (compressionMode == CompressionMode.Decompress)
            {
                parallelWorker = new ParallelFileDecompress2(inputPath, outputPath);
                //parallelWorker = new ParallelFileDecompress(inputPath, outputPath);
            }
            else
                throw new ApplicationException("Uknown compression mode.");

            parallelWorker.Run(cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            // FileNotFoundException should provide nice user-readable exception message.
            Console.WriteLine(ex.Message);
            return FAILURE;
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine("Directory not found... " + ex.Message);
            return FAILURE;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Program does not have required permissions. {ex.Message}");
            return FAILURE;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation has been canceled.");
            return FAILURE;
        }
        catch (PathTooLongException ex)
        {
            Console.WriteLine($"One of the given paths is too long..." + ex.Message);
            return FAILURE;
        }
        catch (IOException ex)
        {
            if (!string.IsNullOrEmpty(ex.Message))
                Console.WriteLine(ex.Message);
            else
                Console.WriteLine($"IOException occured. HResult: {ex.HResult}");
            return FAILURE;
        }
        catch (Exception ex)
        {
            // the exception message is localized, so the user should work out what went wrong
            Console.WriteLine("Application has encountered unhandled exception: " + ex.Message);
            return FAILURE;
        }

        return SUCCESS;
    }
}