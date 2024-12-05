// See https://aka.ms/new-console-template for more information

using MySqlBackup;

string mysqlPath = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe";
string mysqldumpPath = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe";
Backup backup=new Backup("192.168.0.160", "Office_Erp_DocumentServiceNew","root", "Asad@123",mysqlPath,mysqldumpPath);
var response =await  backup.RestoreDatabase("F:\\TestMysqlbackup\\testback.sql");
Console.WriteLine("Ok");
Console.ReadLine();