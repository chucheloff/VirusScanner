using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ScannerServer.Models;

namespace ScannerServer.Repositories
{
	public class ScanTasksRepository : IScanTasksRepository
	{
		private readonly ConcurrentDictionary<long, ScanTask> scanTasks = new();

		public void CreateScanTask(ScanTask task)
		{
			scanTasks.TryAdd(task.Id, task);
		}
		public string GetScanStatus(long Id)
		{
			ScanTask task; 
			scanTasks.TryGetValue(Id, out task);
			if (task is null){
				return $"Could not find task with id={Id}";
			}
			return task.ToString();
		}

		public ScanTask GetScanTask(long Id){
			ScanTask task;
			scanTasks.TryGetValue(Id, out task);
			return task;
		}

		public IEnumerable<ScanTask> GetScanTasks()
		{
			//безопасно получаем список существующих тасков из словаря
			//при помощи TryGetValue
			var keys = scanTasks.Keys.ToList();
			var values = new List<ScanTask>();
			foreach (var key in keys)
			{
				ScanTask scanTask;
				if (scanTasks.TryGetValue(key, out scanTask))
				{
					values.Add(scanTask);
				}
			}
			return values;
		}
	}
}