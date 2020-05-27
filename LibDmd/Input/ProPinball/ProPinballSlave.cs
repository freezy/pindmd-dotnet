using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using LibDmd.Frame;
using NLog;

namespace LibDmd.Input.ProPinball
{

	public class ProPinballSlave : AbstractSource, IGray4Source
	{
		public override string Name { get; } = "Pro Pinball";

		public Color Color { get; set; } = Color.FromRgb(255, 191, 0);

		public IObservable<Unit> OnResume => _onResume;
		public IObservable<Unit> OnPause => _onPause;

		private readonly ISubject<Unit> _onResume = new Subject<Unit>();
		private readonly ISubject<Unit> _onPause = new Subject<Unit>();

		private readonly uint _messageBufferSize = 392;
		private ProPinballBridge.ProPinballDmd _bridge;
		private IObservable<DmdFrame> _framesGray4;
		private DmdFrame _dmdFrame = new DmdFrame();

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public ProPinballSlave(string args)
		{
			var match = Regex.Match(args, @"m(\d+)", RegexOptions.IgnoreCase);
			if (match.Success) {
				_messageBufferSize = uint.Parse(match.Groups[1].Value);
			}
		}

		public IObservable<DmdFrame> GetGray4Frames()
		{
			if (_framesGray4 != null) {
				return _framesGray4;
			}
			CreateBridge();

			Logger.Info("Subscribing to Pro Pinball's message queue...");
			_framesGray4 = Observable.Create<DmdFrame>(o => {

				var len = Dimensions.Value.Surface;

				// this is blocking, so use a new thread
				var thread = new Thread(() => {
					unsafe {
						_bridge.GetFrames(frame => {
							var arr = _dmdFrame.Update(Dimensions.Value, new byte[len]);
							Marshal.Copy((IntPtr)frame, arr.Data, 0, len);
							o.OnNext(arr);

						}, err => {
							throw new ProPinballSlaveException(new string(err));

						}, () => {
							Logger.Debug("Received exit signal from Pro Pinball, closing.");
							Process.GetCurrentProcess().Kill();
						});
					}
				});
				thread.Start();
				Logger.Debug("Subscribed to Pro Pinball's message queue.");

				return Disposable.Create(() => {
					thread.Abort();
					Logger.Debug("Disposing Pro Pinball's message queue...");
				});
			});
			return _framesGray4;
		}

		private void CreateBridge()
		{
			if (_bridge != null) {
				throw new ProPinballSlaveException("Can only have one active source at the time!");
			}
			_bridge = new ProPinballBridge.ProPinballDmd(_messageBufferSize);
			if (_bridge.Status != 0) {
				unsafe {
					throw new ProPinballSlaveException("Error connecting: " + new string(_bridge.Error));
				}
			}
		}
	}

	public class ProPinballSlaveException : Exception
	{
		public ProPinballSlaveException(string message) : base(message)
		{
		}
	}
}
