using System;
using System.Collections.Generic;
using NLog;

namespace LibDmd.Common
{
	public class Profiler
	{
		private static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>();
		private static List<Profile> RootProfiles { get; } = new List<Profile>();
		private static Profile _parent;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static void Start(string key)
		{
			if (Profiles.ContainsKey(key)) {
				Profiles[key].Start();
			} else { 
				_parent = new Profile(key, _parent);
				Profiles.Add(key, _parent);
				if (_parent.Parent == null) {
					RootProfiles.Add(_parent);
				}
			}
		}

		public static void Stop(string key)
		{
			Profiles[key].Stop();
			_parent = Profiles[key].Parent;
		}

		public static void Print()
		{
			Logger.Debug("Profiling data:");
			RootProfiles.ForEach(p => p.Print());
		}

		public static void Reset()
		{
			Profiles.Clear();
			RootProfiles.Clear();
			_parent = null;
		}
	}

	class Profile
	{
		public string Name { get; }
		public Profile Parent { get; }
		public List<Profile> Children { get; } = new List<Profile>();
		public bool IsRunning;

		private readonly int _level;
		private long _started;
		private long _totalTicks;
		private long _count;
		private long _minTicks;
		private long _maxTicks;
		private int _drops;

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public Profile(string name, Profile parent)
		{
			Name = name;
			Parent = parent;
			_level = parent?._level + 1 ?? 0;
			parent?.Children.Add(this);
			Start();
		}

		public void Start()
		{
			if (IsRunning) {
				_drops++;
				// don't measure if caused drop
				IsRunning = false;
				return;
			}
			_started = DateTime.Now.Ticks;
			IsRunning = true;
		}

		public void Stop()
		{
			if (!IsRunning) {
				return;
			}
			Children.ForEach(c => c.Stop());

			var ticks = DateTime.Now.Ticks - _started;
			_totalTicks += ticks;
			_count++;
			_maxTicks = Math.Max(_maxTicks, ticks);
			_minTicks = _minTicks == 0 ? ticks : Math.Min(_minTicks, ticks);
			IsRunning = false;
		}

		public void Print()
		{
			Logger.Debug(this);
			Children.ForEach(c => c.Print());
		}

		public override string ToString()
		{ 
			const int precision = 1000;
			var avgMs = Math.Round((double)_totalTicks / _count / TimeSpan.TicksPerMillisecond * precision) / precision;
			var minMs = Math.Round((double)_minTicks / TimeSpan.TicksPerMillisecond * precision) / precision;
			var maxMs = Math.Round((double)_maxTicks / TimeSpan.TicksPerMillisecond * precision) / precision;
			var str = $"{Name}: {avgMs}ms ({minMs}/{maxMs}), {_count}x, {_drops} drop(s)";
			return str.PadLeft(str.Length + _level * 3);
		}
	}
}
