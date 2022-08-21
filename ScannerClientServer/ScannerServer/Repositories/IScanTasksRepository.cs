using System.Collections.Generic;
using ScannerServer.Models;

namespace ScannerServer.Repositories
{
	/// <summary>
	///	Интерфейс который определяет необходимый набор методов для класса, 
	/// управляющего списком задач по сканированию директорий.
	/// </summary>
	public interface IScanTasksRepository
	{
		/// <summary>
		/// Получить объект задачи сканирования по индетификатору
		/// </summary>
		/// <param name="Id">Идентификатор задачи</param>
		/// <returns>Объект задачи по сканированию</returns>
		ScanTask GetScanTask(long Id);

		/// <summary>
		/// Получить список всех созданных задач по сканированию
		/// </summary>
		/// <returns>Перечисляеммый список всех задач по сканированию</returns>
		IEnumerable<ScanTask> GetScanTasks();

		/// <summary>
		///	Создать задачу по сканированию указанной директории 
		/// </summary>
		/// <param name="task">Задача по сканированию директории</param>
		void CreateScanTask(ScanTask task);

		/// <summary>
		///	Получить статус задачи по её идентификатору 
		/// </summary>
		/// <param name="Id"></param>
		/// <returns></returns>
		string GetScanStatus(long Id);
		
	}
}