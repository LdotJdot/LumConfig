using LumConfg;
using System.Collections;
using System.IO;

namespace ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LumConfigManager con=new LumConfigManager();

            // the value type should be array and basic json type (int, long, double, string, bool), others will be stringfy.
            con.Set("findmax", "xx");
            con.Set("HotKey", 46);
            con.Set("Now", DateTime.Now);
            con.Set("TheHotKeys", new int[] { 46, 33, 21 });
            con.Set("HotKeys:Mainkey", 426);

            con.Save("d:\\aa.json");


            LumConfigManager conRead = new LumConfigManager("d:\\aa.json");

            Console.WriteLine(conRead.GetInt("HotKeys:Mainkey"));
            Console.WriteLine(conRead.Get("Now"));

            var res = conRead.Get("TheHotKeys") as IList;

            foreach (var data in res)
            {
                Console.WriteLine(data);
            }

        }
    }
}
