using System;
using System.Threading.Tasks;

namespace WSS
{
	public static class TaskTimeout
	{
		public static async Task WithTimeout(this Task task, TimeSpan timeout)
		{
			if (task != await Task.WhenAny(task, Task.Delay(timeout)))
				throw new TimeoutException("async operation timed out");
		}

		public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
		{
			if (task != await Task.WhenAny(task, Task.Delay(timeout)))
				throw new TimeoutException("async operation timed out");
			else
				return await task;
		}
	}
}

