using Microsoft.Extensions.Configuration;
using PublishCli.Models;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublishCli
{
    class Program
    {
        static PublishOptions pbOptions;

        static void Main(string[] args)
        {
            try
            {
                ConfigurationBuilder builder = new ConfigurationBuilder();

                if(args.Length > 0)
                {
                   builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), $"PublishCli-{args[0]}.json"), optional: true, reloadOnChange: true);
                   Console.WriteLine($"当前环境 {args[0]}");
                }
                else
                {
                    builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "PublishCli.json"), optional: true, reloadOnChange: true);
                }

                AppConfig.ConfigurationRoot =builder.Build();

                pbOptions = AppConfig.PublishOptions;
                if (pbOptions == null) throw new ArgumentNullException("PublishOptions");
                pbOptions.ChkValid();

                // 从源文件夹中读取列表
                DirectoryInfo sourceDirectory = new DirectoryInfo(pbOptions.SourcePath);
                if (!sourceDirectory.Exists) throw new Exception("找不到 SourcePath 文件夹");


                if (pbOptions.ClearDist)
                {
                    if (Directory.Exists(pbOptions.DistPath)) DeleteFolder(new DirectoryInfo(pbOptions.DistPath));
                }

                if (!Directory.Exists(pbOptions.DistPath)) Directory.CreateDirectory(pbOptions.DistPath);
                CopyFiles(sourceDirectory, pbOptions.PublishPath, pbOptions.DistPath);

                Console.WriteLine("Copy 完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("异常：" + ex.Message);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// 删除文件夹及其内容
        /// </summary>
        /// <param name="dir"></param>
        public static void DeleteFolder(DirectoryInfo dirInfo)
        {
            foreach (var fi in dirInfo.GetFiles())
            {
                if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                    fi.Attributes = FileAttributes.Normal;
                fi.Delete();//直接删除其中的文件 
            }

            foreach (var di in dirInfo.GetDirectories())
            {
                DeleteFolder(di);
            }

            dirInfo.Delete();
        }

        public static void CopyFiles(DirectoryInfo sourceInfo, string distDir, string publishDir)
        {
            if (pbOptions.ExcludeFolders.Contains(sourceInfo.Name)) return;
            Parallel.ForEach(sourceInfo.GetFiles(), new ParallelOptions()
            {
                MaxDegreeOfParallelism = 5
            }, fi =>
          {
              // 排除文件
              if (pbOptions.ExcludeNames.Contains(fi.Name)) return;

              var destFi = new FileInfo(Path.Combine(distDir, fi.Name));
              bool isCopy = !destFi.Exists;
              string outputTxt = string.Empty;
              if (isCopy)
              {
                  outputTxt = $"---------新文件---------\n{destFi.FullName}";
              }
              else
              {
                  string destFileMd5 = GetMD5HashFromFile(destFi.FullName);
                  string fiFileMd5 = GetMD5HashFromFile(fi.FullName);
                  isCopy = !(destFileMd5.Equals(fiFileMd5));
                  if (isCopy)
                  {
                      outputTxt = $"---------文件有改动---------\n{destFi.FullName}\n旧md5:{destFileMd5}\n新md5:{fiFileMd5}";
                  }
              }
              if (isCopy)
              {
                  string publishFileName = Path.Combine(publishDir, fi.Name);
                  //Console.WriteLine($"正在复制:{fi.FullName} -> {publishFileName}");
                  var distFileName = Path.Combine(distDir, fi.Name);
                  if (pbOptions.ReplacePublishFile)
                  {
                      if (!Directory.Exists(distDir)) Directory.CreateDirectory(distDir);
                      fi.CopyTo(distFileName, true);
                  }
                  if (!Directory.Exists(publishDir)) Directory.CreateDirectory(publishDir);
                  fi.CopyTo(publishFileName, true);
                  outputTxt += $"\n完成复制：{fi.FullName} -> {publishFileName}";
                  Console.WriteLine(outputTxt);
              }
          });
            //foreach (var fi in sourceInfo.GetFiles())
            //{
            //    var destFi = new FileInfo(Path.Combine(distDir, fi.Name));
            //    bool isCopy = !destFi.Exists;   //destFi.LastWriteTime < fi.LastWriteTime;
            //    if (!isCopy)
            //    {
            //        string destFileMd5 = GetMD5HashFromFile(destFi.FullName);
            //        string fiFileMd5 = GetMD5HashFromFile(fi.FullName);
            //        isCopy = !(destFileMd5.Equals(fiFileMd5));
            //        if (isCopy)
            //        {
            //            Console.WriteLine("---------文件有改动---------");
            //            Console.WriteLine($"{destFi.FullName} md5:{destFileMd5}");
            //            Console.WriteLine($"{fi.FullName} md5:{fiFileMd5}");
            //        }

            //    }
            //    if (isCopy && !pbOptions.ExcludeNames.Contains(destFi.Name))
            //    {
            //        string publishFileName = Path.Combine(publishDir, fi.Name);
            //        Console.WriteLine($"正在复制:{fi.FullName} -> {publishFileName}");
            //        var distFileName = Path.Combine(distDir, fi.Name);
            //        if (pbOptions.ReplacePublishFile)
            //        {
            //            if (!Directory.Exists(distDir)) Directory.CreateDirectory(distDir);
            //            fi.CopyTo(distFileName, true);
            //        }
            //        if (!Directory.Exists(publishDir)) Directory.CreateDirectory(publishDir);
            //        fi.CopyTo(publishFileName, true);
            //    }
            //}

            foreach (var di in sourceInfo.GetDirectories())
            {
                CopyFiles(di, Path.Combine(distDir, di.Name), Path.Combine(publishDir, di.Name));
            }
        }

        /// <summary>
        /// 获取文件MD5值
        /// </summary>
        /// <param name="fileName">文件绝对路径</param>
        /// <returns>MD5值</returns>
        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString().ToLowerInvariant(); ;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetMD5HashFromFile() fail,error:" + ex.Message);
                return string.Empty;
            }
        }
    }
}
