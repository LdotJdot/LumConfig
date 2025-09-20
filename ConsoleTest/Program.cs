using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LumConfg;
using System.Collections;
using System.IO;
using System.Text.Json;

namespace ConsoleTest
{
    [MemoryDiagnoser]
    public class JsonConvert
    {
        public const string simpleJson = @"{""name"":""\""老虎\""凯恩\u5927\u6492\u65e6""}";
        public const string simpleJson2 = @"{""name"":32233}";
        public const string jsonNoComment = @"
        {
            ""version"": ""0.2.0"",
            ""configurations"": [
                {
                    ""name"": "".NET Core Launch (console)"",
                    ""type"": ""coreclr"",
                    ""request"": ""launch"",
                    ""preLaunchTask"": ""build"",
                    ""program"": ""${workspaceFolder}/bin/Debug/net6.0/c#.dll"",
                    ""args"": [],
                    ""cwd"": ""${workspaceFolder}"",
                    ""console"": ""internalConsole"",
                    ""stopAtEntry"": false,
                    ""val"":1E-3
                },
                {
/*comment
   test
*/
                    ""name"": "".NET Core Attach"",
                    ""type"":/*type*/ ""coreclr"",
                    ""request"": ""attach""
                }
            ]
        }
        ";

        public const string jsonWithComment = @"
        {
            ""version"": ""0.2.0"",
            ""configurations"": [
                {
                    // Use IntelliSense to find out which attributes exist for C# debugging
                    // Use hover for the description of the existing attributes
                    // For further information visit https://github.com/OmniSharp/omnisharp-vscode/blob/master/debugger-launchjson.md
                    ""name"": "".NET Core Launch (console)"",
                    ""type"": ""coreclr"",
                    ""request"": ""launch"",
                    ""preLaunchTask"": ""build"",
                    // If you have changed target frameworks, make sure to update the program path.
                    ""program"": ""${workspaceFolder}/bin/Debug/net6.0/c#.dll"",
                    ""args"": [],
                    ""cwd"": ""${workspaceFolder}"",
                    // For more information about the 'console' field, see https://aka.ms/VSCode-CS-LaunchJson-Console
                    ""console"": ""internalConsole"",
                    ""stopAtEntry"": false
                },
                {
                    ""name"": "".NET Core Attach"",
                    ""type"": ""coreclr"",
                    ""request"": ""attach""
                }
            ]
        }
        ";
        const int cycle = 1_0000;
        [Benchmark]
        public void JsonOdd()
        {
            for (int i = 0; i < cycle; i++)
            {
                var obj = LumConfg.Json.Deserialize(jsonNoComment);
            }
        }
        [Benchmark]
        public void JsonNew()
        {
            for (int i = 0; i < cycle; i++)
            {
                var obj = LumJson.Serialize(jsonNoComment);
            }
        }
        [Benchmark]
        public void JsonNewWithComment()
        {
            for (int i = 0; i < cycle; i++)
            {
                var obj = LumJson.Deserialize(jsonWithComment);
            }
        }
       
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(JsonConvert.simpleJson);
            // var summary = BenchmarkRunner.Run<JsonConvert>();

            var obj = LumJson.Deserialize(JsonConvert.simpleJson2);
            Console.WriteLine((obj as Dictionary<string, object>)["name"]);
            Console.WriteLine(LumJson.Serialize(obj));


        }
    }
}
