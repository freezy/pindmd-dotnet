using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace LibDmd.Input.FileSystem
{
	public class RawSource : AbstractSource, IRawSource
	{
		public override string Name { get; } = "Binary File";

		private readonly string _filename;

		public RawSource(string filename)
		{
			_filename = filename;
		}

		public IObservable<byte[]> GetRawdata()
		{
			return ObservableRead(new FileStream(_filename, FileMode.Open, FileAccess.Read), 4096);
		}

		public static IObservable<byte[]> ObservableRead(Stream stream, int bufferSize)
		{
			return Observable.Create<byte[]>(o =>
			{
				var buffer = new byte[bufferSize];
				try {
					while (true) {
						var read = stream.Read(buffer, 0, buffer.Length);
						if (read == 0) {
							break;
						}
						var results = buffer.Take(read).ToArray();
						// always return a copy, never the buffer for concurrency's sake.
						o.OnNext(results);
					}
				} catch (Exception ex) {
					o.OnError(ex);
				} finally {
					o.OnCompleted();
				}
				return Disposable.Empty;
			});
		}
	}
}
