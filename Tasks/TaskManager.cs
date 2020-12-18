using System.Collections.Generic;
using UnityEngine;

namespace exoLib.Tasks
{
	/// <summary>
	/// When to perform provided task.
	/// </summary>
	public enum TaskPerformWhen
	{
		Update,
		FixedUpdate,
		LateUpdate,
	}

	/// <summary>
	/// Allows the user to attach tasks that will be performed during the next pass specified by TaskPerformWhen.
	/// 
	/// Example usage:
	///		TaskManager.Run(() =>
	///		{
	///			Debug.Log("I will be performed during LateUpdate");
	///		}, TaskPerformWhen.LateUpdate);
	/// </summary>
	public class TaskManager : MonoBehaviour
	{
		/// <summary>
		/// Singleton pattern
		/// </summary>
		private static TaskManager _instance;
		/// <summary>
		/// Returns (or creates and returns) an instance of the task manager.
		/// Not exposed directly for ease of usage. (TaskManager.Instance.Run != TaskManager.Run)
		/// </summary>
		private static TaskManager Instance
		{
			get
			{
				if (_instance == null)
					_instance = new GameObject("TaskManager").AddComponent<TaskManager>();

				return _instance;
			}
		}
		/// <summary>
		/// Any task that can be created by the user
		/// </summary>
		public delegate void Task();
		/// <summary>
		/// Initial queue sizes.
		/// </summary>
		private const int INITIAL_CAPACITY = 8;
		/// <summary>
		/// Tasks to be performed during Update
		/// </summary>
		private Queue<Task> _updateTasks = new Queue<Task>(INITIAL_CAPACITY);
		/// <summary>
		/// Tasks to be performed during FixedUpdate
		/// </summary>
		private Queue<Task> _fixedUpdateTasks = new Queue<Task>(INITIAL_CAPACITY);
		/// <summary>
		/// Tasks to be performed during LateUpdate
		/// </summary>
		private Queue<Task> _lateUpdateTasks = new Queue<Task>(INITIAL_CAPACITY);

		/// <summary>
		/// Returns queue of tasks for given pass.
		/// </summary>
		private Queue<Task> GetTaskQueue(TaskPerformWhen when)
		{
			switch (when)
			{
				case TaskPerformWhen.Update:
					return _updateTasks;
				case TaskPerformWhen.FixedUpdate:
					return _fixedUpdateTasks;
				case TaskPerformWhen.LateUpdate:
					return _lateUpdateTasks;
				default:
					break;
			}

			throw new System.Exception("Couldn't retrieve task queue!");
		}
		/// <summary>
		/// Performs all queued tasks.
		/// </summary>
		private void PerformTasks(TaskPerformWhen when)
		{
			var queue = GetTaskQueue(when);
			while (queue.Count > 0)
			{
				var task = queue.Dequeue();
				task.Invoke();
			}
		}

		/// <summary>
		/// Process all update tasks.
		/// </summary>
		private void Update()
		{
			PerformTasks(TaskPerformWhen.Update);
		}

		/// <summary>
		/// Process all fixedUpdate tasks.
		/// </summary>
		private void FixedUpdate()
		{
			PerformTasks(TaskPerformWhen.FixedUpdate);
		}

		/// <summary>
		/// Process all lateUpdate tasks.
		/// </summary>
		private void LateUpdate()
		{
			PerformTasks(TaskPerformWhen.LateUpdate);
		}

		/// <summary>
		/// Enqueue task to be executed in the next pass of performWhen.
		/// </summary>
		public static void Run(Task task, TaskPerformWhen when = TaskPerformWhen.FixedUpdate)
		{
			Instance.GetTaskQueue(when).Enqueue(task);
		}
	}
}