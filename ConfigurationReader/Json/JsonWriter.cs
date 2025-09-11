using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumConfg
{
    static internal class JsonWriter
    {
        /// <summary>
        /// Json写入文件模型控制
        /// 后续改内部变量，用不同方法实现调用控制
        /// </summary>
        public static int JsonWriterMode = 0;
        
        static public bool WriteFile_NormalString(object obj, string path, bool append = false)
        {
            JsonWriterMode = 1;
            return WriteFile(obj, path, append);
        }

        static public bool WriteFile(object obj, string path, bool append = false)
        {
            try
            {
                var str = Json.Serialize(obj);
                using (StreamWriter sw = new StreamWriter(path, append, Encoding.UTF8))
                {
                    sw.Write(str);
                    sw.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
