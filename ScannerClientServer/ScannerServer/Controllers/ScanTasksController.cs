using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
using ScannerServer.Models;
using ScannerServer.Repositories;

namespace ScannerServer.Controllers
{
	[ApiController]

	[Route("ScanTasks")]
	public class ScanTasksController : ControllerBase
	{
		private readonly IScanTasksRepository scans;

		/// <summary>
		/// Конструктор контроллера, в который мы инжектим зависимость от интерфейса
		/// управляющего списком сканов  
		/// </summary>
		/// <param name="scans">список созданных тасков сканирования</param>
		public ScanTasksController(IScanTasksRepository scans)
		{
			this.scans = scans;
		}

		//GET /ScanTasks/
		/// <summary>
		/// Возвращает список всех созданных тасков в виде
		/// массива строк описывающих их статус 
		/// </summary>
		/// <param name="Id">Идентификатор таска сканирования</param>
		/// <returns>Статус таска</returns>
		[HttpGet]
		public ActionResult<string> GetAllTaskStatus(long Id)
		{
			IEnumerable<ScanTask> scanTasks = scans.GetScanTasks();
			
			if(scanTasks is null){
				return "No tasks created yet.";
			}

			StringBuilder response = new();
			foreach(var task in scanTasks){
				response.AppendLine(task.ToString());
			}

			return Ok(response.ToString());
		}



		//GET /ScanTasks/{id}
		/// <summary>
		/// Проверяем наличие таска с указанным идентификатором.
		/// Если он есть - возвращаем статус, если нет - ошибку. 
		/// </summary>
		/// <param name="Id">Идентификатор таска сканирования</param>
		/// <returns>Статус таска</returns>
		[HttpGet("{Id}")]
		public ActionResult<string> GetScanTaskStatus(long Id)
		{
			ScanTask scanTask = scans.GetScanTask(Id);

			if(scanTask is null){
				return NotFound();
			}

			return Ok(scanTask.ToString());
		}


		//POST /ScanTasks/
		/// <summary>
		///	Создание нового таска на сканирование 
		/// рекурсивно внутрь указанной директории
		/// </summary>
		/// <param name="path">Путь к директории</param>
		/// <returns>Статус создания</returns>
		[HttpPost]
		public ActionResult<string> CreateScanTask(string path)
		{
			
			if (path is null){
				return BadRequest();
			}

			var scanTask = new ScanTask(path);

			scans.CreateScanTask(scanTask);

			if (scanTask.Completed.Task.Exception is not null)
			return $"Task {scanTask.Id} was not created due to error : {scanTask.Completed.Task.Exception.Message} ";

			return Ok($"Task {scanTask.Id} successfully created.");
		}
	}
}