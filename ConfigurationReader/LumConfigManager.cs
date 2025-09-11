// Aurthor: LDotJDot

namespace LumConfg
{
    public class LumConfigManager
    {
        public const string Version = "1.0.1";
        public string path;

        private Dictionary<string, object> config;

        public LumConfigManager()
        {
            config = new Dictionary<string, object>();
            path = string.Empty;
        }

        public LumConfigManager(string path)
        {
            this.path = path;
            config = Initialize(this.path);
            if (config == null)
            {
                throw new Exception("配置文件创建失败:" + path);
            }
        }

        public void Save(string path)
        {
            try
            {
                if (JsonWriter.WriteFile_NormalString(config, path))
                {
                    return;
                }
                throw new Exception("Save failed.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Origin path is empty");
            }
            Save(path);
        }

        private Dictionary<string, object> Initialize(string path)
        {
            if (!File.Exists(path))
            {
                string text = AppDomain.CurrentDomain.BaseDirectory + "\\" + path;
                if (!File.Exists(text))
                {
                    throw new Exception("文件不存在:" + path);
                }
                path = text;
            }
            try
            {
                return JsonReader.CreateFromPath(path);
            }
            catch
            {
                throw new Exception("配置文件格式无效:" + path);
            }
        }

        public bool Set(string path, object value)
        {
            string[] nodes = path.Split(':');
            return SetValueOnPath(nodes, value);
        }

        public string? GetString(string path)
        {
            string[] nodes = path.Split(':');
            return GetValueOnPath(nodes)?.ToString();
        }

        public bool? GetBool(string path)
        {
            string[] nodes = path.Split(':');
            if (bool.TryParse(GetValueOnPath(nodes)?.ToString() ?? "", out bool result))
            {
                return result;
            }
            return null;
        }

        public int? GetInt(string path)
        {
            string[] nodes = path.Split(':');
            if (int.TryParse(GetValueOnPath(nodes)?.ToString() ?? "", out int result))
            {
                return result;
            }
            return null;
        }

        public object? Get(string path)
        {
            string[] nodes = path.Split(':');
            return GetValueOnPath(nodes);
        }

        public double? GetDouble(string path)
        {
            string[] nodes = path.Split(':');
            if (double.TryParse(GetValueOnPath(nodes)?.ToString() ?? "", out double result))
            {
                return result;
            }
            return null;
        }

        private bool SetValueOnPath(string[] nodes, object value)
        {
            if (nodes == null || nodes.Length == 0)
            {
                return false;
            }
            Dictionary<string, object> dictionary = config;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (string.IsNullOrEmpty(nodes[i]))
                {
                    return false;
                }
                if (!dictionary.ContainsKey(nodes[i]))
                {
                    dictionary[nodes[i]] = new Dictionary<string, object>();
                }
                Dictionary<string, object> dictionary2 = dictionary[nodes[i]] as Dictionary<string, object>;
                if (dictionary2 != null)
                {
                    dictionary = dictionary2;
                    continue;
                }
                return false;
            }
            string text = nodes[^1];
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            dictionary[text] = value;
            return true;
        }

        private object? GetValueOnPath(string[] nodes)
        {
            if (nodes == null || nodes.Length == 0)
            {
                return null;
            }
            Dictionary<string, object> dictionary = config;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (string.IsNullOrEmpty(nodes[i]))
                {
                    return null;
                }
                if (!dictionary.TryGetValue(nodes[i], out object value))
                {
                    return null;
                }
                Dictionary<string, object> dictionary2 = value as Dictionary<string, object>;
                if (dictionary2 != null)
                {
                    dictionary = dictionary2;
                    continue;
                }
                return null;
            }
            string text = nodes[^1];
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (dictionary.TryGetValue(text, out object value2))
            {
                return value2;
            }
            return null;
        }


    }

}
