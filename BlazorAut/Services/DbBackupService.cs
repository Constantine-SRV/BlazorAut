using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BlazorAut.Services
{
    public class DbBackupService
    {
        private readonly string _connectionString;
        private readonly string _accountName;
        private readonly string _accountKey;

        public DbBackupService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _accountName = "constantine2zu";
            _accountKey = Environment.GetEnvironmentVariable("AZURE_STORAGE_KEY");
        }

        public async Task RunBackup(Action<string> onOutputReceived, Action<string> onErrorReceived, CancellationToken cancellationToken)
        {
            var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(_connectionString);
            string command = $"PGPASSWORD={connectionStringBuilder.Password} pg_dump -U {connectionStringBuilder.Username} -d {connectionStringBuilder.Database} -h {connectionStringBuilder.Host} -F c -b -v -f ./dbwebaws_backup.dump";

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = null;

            try
            {
                process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onOutputReceived?.Invoke(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onErrorReceived?.Invoke(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Here we wait for the process to exit, while observing the cancellation token
                await Task.Run(() => process.WaitForExit(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();  // Ensure the process is killed if the operation is cancelled
                }
                throw;  // Re-throw the exception to handle it in the calling method
            }
            finally
            {
                process?.Dispose(); // Ensure process resources are cleaned up
            }
        }

        public async Task UploadBackup(Action<string> onOutputReceived, Action<string> onErrorReceived, CancellationToken cancellationToken)
        {
            string command = $"sudo -E az storage blob upload --account-name {_accountName} --account-key {_accountKey} --container-name web --file ./dbwebaws_backup.dump --name dbwebaws_backup.dump --overwrite";

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = null;

            try
            {
                process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onOutputReceived?.Invoke(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        onErrorReceived?.Invoke(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Here we wait for the process to exit, while observing the cancellation token
                await Task.Run(() => process.WaitForExit(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();  // Ensure the process is killed if the operation is cancelled
                }
                throw;  // Re-throw the exception to handle it in the calling method
            }
            finally
            {
                process?.Dispose(); // Ensure process resources are cleaned up
            }
        }
    }
}
