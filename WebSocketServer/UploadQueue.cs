using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WSS
{
	public class UploadQueue
	{
		public class Ticket : IDisposable
		{
			internal readonly TaskCompletionSource<UploadWorkerContext> CompletionSource;
			private readonly Task<UploadWorkerContext> ContextTask;
			private readonly Action<Ticket> OnDispose;
			internal UploadWorkerContext Context;

			private Ticket(Action<Ticket> onDispose)
			{
				OnDispose = onDispose;
			}

			public Ticket(Action<Ticket> onDispose, TaskCompletionSource<UploadWorkerContext> compl)
				: this(onDispose)
			{
				CompletionSource = compl;
				ContextTask = compl.Task;
			}

			public Ticket(Action<Ticket> onDispose, UploadWorkerContext task)
				: this(onDispose)
			{
				ContextTask = Task.FromResult(Context = task);
			}

			public async Task<UploadWorkerContext> GetContext()
			{
				return Context = await ContextTask;
			}

			void IDisposable.Dispose()
			{
				OnDispose(this);
			}
		}

		private readonly LinkedList<Ticket> Queue = new LinkedList<Ticket>();

		private Queue<UploadWorkerContext> AvailableContexts;

		public UploadQueue(IEnumerable<UploadWorkerContext> contexts)
		{
			AvailableContexts = new Queue<UploadWorkerContext>(contexts);
		}

		public Ticket EnterQueue()
		{
			lock (this) {
				if (AvailableContexts.Count > 0)
					return new Ticket(DisposeTicket, AvailableContexts.Dequeue());
				else {
					var ticket = new Ticket(DisposeTicket, new TaskCompletionSource<UploadWorkerContext>());
					Queue.AddLast(ticket);
					return ticket;
				}
			}
		}

		private void DisposeTicket(Ticket ticket)
		{
			lock (this) {
				if (ticket.Context == null) {
					Queue.Remove(ticket);
					ticket.CompletionSource.SetCanceled();
				} else {
					GiveBackContext(ticket.Context);
				}
			}
		}

		private void GiveBackContext(UploadWorkerContext context)
		{
			lock (this) {
				Ticket ticket;
				do {
					if (Queue.Count == 0) {
						AvailableContexts.Enqueue(context);
						return;
					} else {
						ticket = Queue.First.Value;
						Queue.RemoveFirst();
					}
				} while (!ticket.CompletionSource.TrySetResult(context));
				ticket.Context = context; // just in case they're not waiting RIGHT NOW
			}
		}
	}
}

