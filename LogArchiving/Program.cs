using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace LogArchiving
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                List<String[]> list = ReadArchiveConfig();

                Parallel.ForEach(list, rec =>
                 {
                     string Folder = rec[0];
                     string Filetype = rec[1];
                     int Daterange = Convert.ToInt32(rec[2]);
                     string Destination = rec[3];
                     string zipFilename = rec[4];
                     string finalpath = rec[5];
                     int frequency = (Convert.ToInt32(rec[6]) == 0) ? 1 :Convert.ToInt32(rec[6]);
                     DateTime startdate = Convert.ToDateTime(rec[7]);
                     int datediff = (DateTime.Now - startdate).Days;

                     log.Info($"Configured to zip/move all files of type({Filetype}) older than {Daterange-1} day(s) from [{Folder}] to [{Destination}] every {frequency} day(s) starting from {startdate} ");

                     if (datediff % frequency == 0)
                     {
                         FileInfo[] files = (new DirectoryInfo(Folder)).GetFiles(Filetype).Where(x => x.LastWriteTime.Date <= DateTime.Today.AddDays(Daterange) && x.LastWriteTime < DateTime.Now.AddMinutes(-5)).ToArray();
                         
                         log.Info($"Got {files.Length} file(s) from {Folder}");
                         bool resp;
                         if (files.Length > 20)
                             resp = ZipFolder(files, zipFilename, Destination, finalpath);
                         else
                             resp = ZipFiles(files, zipFilename, Destination, finalpath);
                     }
                     else
                     {
                         log.Error($"Check your settings for {Folder} \n The next run date is {DateTime.Now.AddDays(frequency - (datediff % frequency)).Date}");
                     }
                 });
            }
            catch(IOException ex)
            {
                log.Error($"Oops! We skipped a file because it's being used by another process. {ex.Source}");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private static List<String[]> ReadArchiveConfig()
        {
            List<String[]> list = new List<String[]>();

            try
            {
                string[] filelines = File.ReadAllLines("archivedata.txt");
                foreach (string line in filelines)
                {
                    if (!line.StartsWith("*") && !(line is null) && !(line == "")) list.Add(line.Split("|"));
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            return list;
        }

        static bool ZipFiles(FileInfo[] files, string zipname, string destinationpath, string finalpath)
        {
            try
            {
                if (files.Length == 0)
                {
                    return false;
                }
                if (!Directory.Exists(destinationpath))
                    Directory.CreateDirectory(destinationpath);

                string destfile = Path.Combine(destinationpath, zipname + DateTime.Now.ToString("yyyyMMddHHmmss")) + ".zip";
                FileStream fs = new FileStream(destfile, FileMode.Create, FileAccess.Write);

                log.Info("Creating archivefile to " + destfile);
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.WriteTo(fs);
                    fs.Close();
                    ms.Close();
                }

                foreach (var file in files)
                {
                    using (var zipArchive = ZipFile.Open(destfile, ZipArchiveMode.Update))
                    {
                        zipArchive.CreateEntryFromFile(file.FullName, file.Name, CompressionLevel.Optimal);
                        log.Info($"Zipped file successfully- {file.FullName}");
                    }
                }

                log.Info("Deleting files...");

                for (int i = 0; i < files.Length; i++)
                {
                    files[i].Delete();
                }

                log.Info("Files deleted");

                FileInfo zipfil = new FileInfo(destfile);
                if (finalpath != "")
                    zipfil.MoveTo(Path.Combine(finalpath, zipfil.Name.Substring(-4) + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip"));
                //test
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return false;
            }



            return true;
        }

        static bool ZipFolder(FileInfo[] files, string zipname, string destinationpath, string finalpath)
        {
            try
            {
                if (files.Length == 0)
                {
                    return false;
                }
                if (!Directory.Exists(destinationpath))
                    Directory.CreateDirectory(destinationpath);

                string destfolder = Path.Combine(destinationpath, zipname + DateTime.Now.ToString("yyyyMMddHHmmss"));
                if (!Directory.Exists(destfolder))
                    Directory.CreateDirectory(destfolder);

                string destfile = Path.Combine(destinationpath, zipname + DateTime.Now.ToString("yyyyMMddHHmmss")) + ".zip";

                foreach (var file in files)
                {
                    try
                    {
                        file.MoveTo(Path.Combine(destfolder, file.Name));

                    }
                    catch (System.IO.IOException ex)
                    {
                        log.Error(ex);
                    }
                }

                ZipFile.CreateFromDirectory(destfolder, destfile, CompressionLevel.Optimal, false);
                log.Info($"Zipped folder successfully- {destfolder}");

                log.Info("Deleting folder");

                Directory.Delete(destfolder, true);

                log.Info("Folder deleted");

                FileInfo zipfil = new FileInfo(destfile);
                if (finalpath != "")
                    zipfil.MoveTo(Path.Combine(finalpath, zipfil.Name.Substring(-4) + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip"));
                //test
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return false;
            }

            return true;
        }

    }


}
