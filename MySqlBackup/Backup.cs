using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using Mysqlx;
using System.Diagnostics;

namespace MySqlBackup
{
    public class Backup
    {
        private readonly string _server;
        private readonly string _db;
        private readonly string _username;
        private readonly string _password;
        private readonly string _mysqldumpPath;
        private readonly string _mysqlPath;

        public Backup(string server, string db, string username, string password, string mysqlPath, string mysqldumpPath)
        {
            _server = server;
            _db = db;
            _username = username;
            _password = password;
            _mysqldumpPath = mysqldumpPath;
            _mysqlPath = mysqlPath;
        }
        public async Task<Response> GenerateBackup(string backupPath)
        {
            Response response = new Response();
            try
            {


                var extension = Path.GetExtension(backupPath);
                if (extension != ".sql")
                {
                    response.Success = false;
                    response.Message = "Backup path must be a .sql file";
                    return response;
                }

                // Ensure the directory exists
                var getDirectory = System.IO.Path.GetDirectoryName(backupPath);
                if (getDirectory == null)
                {
                    response.Success = false;
                    response.Message = "Invalid file path";
                    return response;
                }

                if (!Directory.Exists(getDirectory))
                {
                    Directory.CreateDirectory(getDirectory);
                }

                // mysqldump arguments
                string arguments = $"--host={_server} -u {_username} {_db} > \"{backupPath}\"";
                // string mysqldumpPath = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe";

                // Configure the process to execute mysqldump
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    // Arguments = $"/C \"{mysqldumpPath}\" {arguments}",
                    Arguments = $"/C \"\"{_mysqldumpPath}\" {arguments}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };


                // Set environment variable for password
                processStartInfo.Environment["MYSQL_PWD"] = _password;


                // Execute the backup process
                using (Process process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        response.Success = false;
                        response.Message = "Process not found";
                        return response;
                    }

                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (process.ExitCode == 0 && string.IsNullOrEmpty(error))
                    {
                        response.Success = true;
                        response.Message = "Backup created";
                        return response;
                    }

                    response.Success = false;
                    response.Message = error;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }


        }

        public async Task<Response> RestoreDatabase(string filePath)
        {
            Response response = new Response();
            try
            {
                if (Path.GetExtension(filePath) != ".sql")
                {
                    response.Message = "File must be contain an extension .sql";
                    return response;
                }
                // Check if the database exists
                bool dbOk = await CreateDatabase();
                // Command to restore the database
                // string restoreCommand = $"mysql --host={server} -u {userName} {databaseName}";
                // Full path to MySQL executable
                //string mysqlPath = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe";

                // Command to restore the database
                string restoreCommand = $"\"{_mysqlPath}\" --host={_server} -u {_username} -p{_password} {_db} < \"{filePath}\"";
                // Use ProcessStartInfo to execute the command
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Set the password in the environment variable
                processStartInfo.Environment["MYSQL_PWD"] = _password;

                using (var process = Process.Start(processStartInfo))
                {
                    using (var streamWriter = process.StandardInput)
                    {
                        if (streamWriter.BaseStream.CanWrite)
                        {
                            // Write the restore command with the SQL file redirection
                            streamWriter.WriteLine($"{restoreCommand} < \"{filePath}\"");
                        }
                    }

                    process.WaitForExit();

                    string error = process.StandardError.ReadToEnd();
                    if (process.ExitCode == 0)
                    {
                        response.Success = true;
                        response.Message = "Database restored successfully";
                        return response;
                    }
                    else
                    {
                        response.Message = error;
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                return response;
            }

        }

        private async Task<bool> CreateDatabase()
        {
            try
            {
                string connectionString = $"Server={_server};Uid={_username};Pwd={_password};";
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();


                    string queryShowDb = $"SHOW DATABASES LIKE '{_db}';";
                    using (var commandShow = new MySqlCommand(queryShowDb, connection))
                    {
                        using (var reader = await commandShow.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                string query = $"CREATE DATABASE `{_db}`;";
                                using (var command = new MySqlCommand(query, connection))
                                {
                                    await command.ExecuteNonQueryAsync();
                                    return true;
                                }
                            }

                            return false;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
