using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumConfg
{
    internal static class FileUtils
    {

        /// <summary>
        /// 默认UTF8
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
       public static string[] ReadFile(string path, Encoding encoding = null, params string[] trimStart)
        {
            
            if (!File.Exists(path))
            {
                throw new Exception("File not found: "+path);
            }
            
            if (encoding == null)
            {
                encoding= Encoding.UTF8;
            }

            List<string> sb =new List<string>(1000);


            try
            {

            using (StreamReader sr = new StreamReader(path, encoding))
            {
                while (!sr.EndOfStream)
                {
                    string lineStr = sr.ReadLine()??"";

                    if (trimStart != null)
                    {
                        bool banned=false;
                        foreach(string ts in trimStart)
                        {
                            if(string.IsNullOrWhiteSpace(lineStr) || (!string.IsNullOrWhiteSpace(ts) && lineStr.StartsWith(ts)))
                            {
                                banned=true;
                                break;
                            }
                        }
                        if (banned)
                        {
                            continue;
                        }
                    }
                    
                    sb.Add(lineStr);
                    
                }
                sr.Close();
            }
            }
            catch(Exception ex)
            {
                throw new Exception("File read failed: " + path+"\r\n"+ex.Message);
            }
            return sb.ToArray();
        }

        public static void WriterdFile(string path, string context, Encoding encoding = null, bool append=false)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
           
            try
            {
                var pathDir = Path.GetDirectoryName(path);
                
                if (!Directory.Exists(pathDir))
                {
                    Directory.CreateDirectory(pathDir);
                    
                    if (!Directory.Exists(pathDir))
                    {
                        throw new Exception("Can not create path: "+path);
                    }
                }

                

                using (StreamWriter sr = new StreamWriter(path, append, encoding))
                {

                    sr.Write(context);
                    sr.Close();
                }
            }
            catch(Exception ex)
            {
                throw new Exception("File write failed: "+path+"\r\n"+ex.Message);
            }
        }
    }
}
