using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumConfg
{

  static internal class JsonReader
    {
       static private string ReadFromFile(string path)
        {
            StringBuilder sb = new StringBuilder(512);
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {                
                while (!sr.EndOfStream)
                {
                    string lineStr=sr.ReadLine()?.Trim()??string.Empty;
                    if(!lineStr.StartsWith("#") && !lineStr.StartsWith("//") && !string.IsNullOrWhiteSpace(lineStr))
                    {
                        sb.AppendLine(lineStr);
                    }
                }
                sr.Close();
            }
            return sb.ToString();
        }


        static public Dictionary<string, object> CreateFromPath(string path)
        {
            return CreateFromText(ReadFromFile(path));
        }

       static public Dictionary<string, object> CreateFromText(string jsonStr)
        {
            Dictionary<string, object> jsonDict = new Dictionary<string, object>();
            try
            {

            jsonDict = LumJson.Deserialize(jsonStr) as Dictionary<string, object>;


            }catch (Exception ex)
            {
                throw new Exception("Json读取异常:" + ex);
            }
            finally
            {
                if (jsonDict == null)
                {
                    throw new Exception("Json读取异常:输入字符为空或未知错误");
                }
            }            
            return jsonDict;            
        }       
    }
}
