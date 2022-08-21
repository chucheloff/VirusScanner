using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ScannerServer.Models
{
    /// <summary>
    /// Данная модель представляет из себя обхект задачи по сканированию директории.
    /// Для корневой директории создается массив всех вложенных папок и файлов и по каждому файлу
    /// создается задача асинхронного сканирования. По окончанию всех асинхронных тасков задача имеет статус Completed. 
    /// </summary>
    public class ScanTask
        {
            //Список файлов и директорий которые мы будем сканировать
            private readonly string[] _files;
            
            //храним искомые строки, в теории можно читать откуда-либо
            //используем поточно-безопасную версию словаря
            static ConcurrentDictionary<string, string> virusStrings;

            //Следующий доступный идентификатор задачи
            private static long IdPool;

            //набор полей в которых будем вести статистику по ходу выполенения сканирования
            public long Id { get; }
            private long _errors;
            private long _jsErrors;
            private long _rmErrors;
            private long _runDllErrors;
            private long _totalProcessed;
            private TimeSpan _execTime;
            private DateTime _startTime;

            //Объект, определяющий статус исполения таска
            public TaskCompletionSource Completed { get; set; }

            /// <summary>
            /// Возвращает следующий доступный индетификатор.
            /// Изменяет значение пула на следующий доступный.
            /// </summary>
            /// <returns>Код идентификатора</returns>
            private static long GetNextId() => IdPool++;

            /// <summary>
            /// Статический конструктор, инициализирующий словарь с вирусами и пул индентификаторов
            /// </summary>
            static ScanTask()
            {
                virusStrings = new ConcurrentDictionary<string, string>();
                virusStrings.TryAdd(@"<script>evil_script()</script>", "js");
                virusStrings.TryAdd(@"rm -rf %userprofile%\Documents", "rmrf");
                virusStrings.TryAdd(@"Rundll32 sus.dll SusEntry", "runDll");
                
                //значение первого выданного ID для процесса - не важное какое, главное инициализировать пул
                IdPool = 1234;
            }

            /// <summary>
            /// Конструктор экземпляра класса.
            /// Инициализирует объект новым идентификатором, 
            /// создает список файлов и директорий для сканирования
            /// и запускает таск если был передан корректный путь 
            /// </summary>
            /// <param name="source">Путь к исходной директории</param>
            public ScanTask(string source)
            {
                
                Completed = new();
                
                Id = GetNextId();
                if (source is null){
                    Completed.SetException(new ArgumentException("Can't scan empty path"));
                }
                source = source.Replace("%userprofile%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                
                if (File.Exists(source))
                {
                    _files = new[] { source };
                }
                else if (Directory.Exists(source))
                {   
                    try{
                        _files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                    }catch(Exception ex){
                        Completed.SetException(ex);
                        return;
                    }
                }
                else {
                    Completed.SetException(new ArgumentException("invalid args"));
                    return;
                }
                
                Console.WriteLine($"Task #{Id} created and started execution.\n");
                _startTime = DateTime.Now;
                Task.Run(RunAsync);
            }


            /// <summary>
            /// Возвращает информацию о таске в зависимости от статуса
            /// Если таск завершен - возвращает полную статистику
            /// Если таск еще выполняется - информационное сообщение  
            /// </summary>
            /// <returns>Статус таска</returns>
            public override string ToString()
            {
                // проверяем состояние таска
                if (Completed.Task.IsCompletedSuccessfully)
                {
                    // такс выполнен и можно формировать строку статистики выполнения
                    var sb = new StringBuilder();
                    sb.Append($"====== Scan #{Id} result ======\n");
                    sb.Append($"Processed files: {_totalProcessed}\n");
                    sb.Append($"JS detects: {_jsErrors}\n");
                    sb.Append($"rm -rf detects: {_rmErrors}\n");
                    sb.Append($"Rundll detects: {_runDllErrors}\n");
                    sb.Append($"Errors: {_errors}\n");
                    sb.Append($"Execution time: {_execTime}\n");
                    sb.Append("==============================\n");
                    return sb.ToString();
                }
                if(Completed.Task.Exception is not null){
                    return $"Task #{Id} was haulted due to exception :\n{Completed.Task.Exception.Message}";
                }
                //таск не выполнен и нам нужно просто об этом сообщить, не отдавая пока что дополнительных статистик
                return $"Scan #{Id} is not yet completed\n";
            }


            /// <summary>
            /// Создаем по каждому файлу набор тасков которые запускаем асинхронно
            /// Затум по окончанию выполнения всех тасков по сканированию начиная с заданной директории
            /// Переводим статус задачи по сканированию в Completed  
            /// </summary>
            /// <returns>Событие окончания сканирования</returns>
            private async Task RunAsync()
            {
                var tasks = new Task[_files.Length];

                for (var i = 0; i < tasks.Length; i++)
                {
                    // так как таски будут выполняться асинхронно к каждому 
                    //в тело метода передаем копию итератора, так как в момент выполнения
                    //итератор i может иметь уже другое значение
                    var local = i;
                    
                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            //если файл возможно открыть - открываем и пытаемся прочитать
                            await using var fs = File.OpenRead(_files[local]);
                            //считаем общее количество обработанных файлов
                            Interlocked.Increment(ref _totalProcessed);

                            //создаем объект ридера который позволит асинхронно читать файл 
                            using var sr = new StreamReader(fs);

                            //читаем строку 
                            var line = await sr.ReadLineAsync();
                            var jsFile = Regex.Match(_files[local], @"^.+\.(js)$").Success;
                            var found = false;
                            //проверяем что файл не закончился
                            while (line != null)
                            {
                                //errType вернет значение в виде описания типа ошибки
                                ScanTask.virusStrings.TryGetValue(line, out string errType);
                                //смотри на тип, если он найден, реализуем логику ведения статистики сканирования
                                switch (errType)
                                {
                                    case "rmrf":
                                        //увеличиваем количество соотвествующих вирусов
                                        Interlocked.Increment(ref _rmErrors);
                                        //сигнализириуем что нашли первы вирус
                                        found = true;
                                        break;
                                    case "runDll":
                                        Interlocked.Increment(ref _runDllErrors);
                                        found = true;
                                        break;
                                    case "js":
                                        //дополнительная проверка на то, что файл - .js 
                                        //и только в таком случае мы считаем что нашли опасный скрипт
                                        if (jsFile)
                                        {
                                            Interlocked.Increment(ref _jsErrors);
                                            found = true;
                                        }
                                        break;
                                    default:
                                        break;
                                }

                                if (found)
                                {
                                    break;
                                }
                                //смотрим следующую линию
                                line = await sr.ReadLineAsync();
                            }
                            sr.Close();
                            sr.Dispose();
                        }
                        catch (Exception)
                        {
                            //при возникновении ошибки при открытии или чтения файла безопасно увеличиваем количество ошибок
                            Interlocked.Increment(ref _errors);
                        }
                    });
                }

                try
                {
                    // теперь нам необходимо дождаться выполнения всех созданных потоков рекурсивного сканирования
                    await Task.WhenAll(tasks);
                    // засекаем сколько исполнялся данный таск 
                    _execTime = DateTime.Now.Subtract(_startTime);
                    // вызываем ивент окончания исполнения таска который вернется в await в мейне
                    Completed.SetResult();
                }
                catch (Exception e)
                {
                    //что-то пошло не там в многпооточной части - фиксируем ошибку в объекте статуса исполенения таска
                    Interlocked.Increment(ref _errors);
                    Completed.SetException(e);
                }
            }
        }
}