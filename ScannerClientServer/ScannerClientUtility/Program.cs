using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Web;
using System.IO;

namespace ScannerClientUtility
{
	class Program
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Печатаем список команд 
        /// </summary>
        private static void printHelp(){
            Console.WriteLine(@$"""scan <Directory>"" - create new scan task");
            Console.WriteLine(@$"""status <id>"" - get info on specific task by id""");
            Console.WriteLine(@$"""status all"" - get info on all tasks in this session""");
            Console.Write(@$"""exit"" or ""quit"" - stop the client." + "\n>");
        }
        static async Task Main()
        {
            string baseUrl = "https://localhost:5001/ScanTasks/";
            Console.WriteLine("Scanner client started....\n");
            printHelp();
            string query;
            query = Console.ReadLine();

            //продолжаем обработку входных строк с консоли пока не увидим exit или quit
            while(!query.Contains("quit") && !query.Contains("exit"))
            {

                string[] splitResult = query.Split(" ");
                if (splitResult[0] == "status"){
                        try{
                            
                            //GET метод в контроллере запросит статус таска с соотвутствующим Id
                            var result = await HttpGetRequestAsync(baseUrl + splitResult[1].Replace("all", ""));
                            Console.Write(result + "\n>");
                        }catch(Exception e){
                            Console.Write(e + "\n>");
                        }
                }else if(splitResult[0] == "scan"){
                        try{
                            //POST метод в контроллере запустит рутину по созданию нового таска по сканированию
                            var result = await HttpPostRequestAsync(baseUrl, 
                            new Dictionary<string, string> { {"path", splitResult[1]} });
                            Console.Write(result + "\n>");
                        }catch(Exception e){
                            Console.Write(e + "\n>");
                        }
                } else if(splitResult[0].Contains("help"))  { 
                    printHelp();
                }else{
                    Console.Write("Unrecognised command\n>");
                }
                query = Console.ReadLine();
            }
        }

        /// <summary>
        /// POST запрос по указанному url и всеми параметрами из словаря values 
        /// </summary>
        /// <param name="url">Адрес запроса</param>
        /// <param name="values">Набор параметров ключ: значение</param>
        /// <returns>Результат выполнения POST запроса</returns>
        private static async Task<string> HttpPostRequestAsync(string url, Dictionary<string ,string> values)
        {
            //добавляем параметры в запрос
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (KeyValuePair<string, string> kvp in values){
                query[kvp.Key] = kvp.Value;
            }
            uriBuilder.Query = query.ToString();
            url = uriBuilder.ToString();
            //PostAsync 
            var content = new FormUrlEncodedContent(values);
            
            var response = await client.PostAsync(url,content);
            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        /// <summary>
        /// GET запрос без параметров и заголовков по указанному url
        /// </summary>
        /// <param name="url">адрес запроса</param>
        /// <returns>Результат выполнения GET запроса</returns>
        private static async Task<string> HttpGetRequestAsync(string url){
            var responseString = await client.GetStringAsync(url);
            return responseString;
        }
    }
}
