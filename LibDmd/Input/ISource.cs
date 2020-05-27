using System;
using System.Reactive;
using System.Reactive.Subjects;
using LibDmd.Frame;

namespace LibDmd.Input
{
	/// <summary>
	/// Acts as source for any frames ending up on the DMD.
	/// </summary>
	///
	/// <remarks>
	/// Since we want a contineous flow of frames, the method to override
	/// returns an observable. Note that the producer decides on the frequency
	/// in which frames are delivered to the consumer.
	///
	/// When implementing a source, make sure to only implement the "native"
	/// bit lengths of the source. Conversion if necessary is done in the Render
	/// Graph directly.
	/// </remarks>
	public interface ISource
	{
		/// <summary>
		/// A display name for the source
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The size of the source. Can change any time.
		/// </summary>
		BehaviorSubject<Dimensions> Dimensions { get; set; }

		/// <summary>
		/// An observable that triggers when the source starts providing frames.
		/// </summary>
		IObservable<Unit> OnResume { get; }

		/// <summary>
		/// An observable that triggers when the source is interrupted, e.g. a game is stopped.
		/// </summary>
		IObservable<Unit> OnPause { get; }
	}

	public enum ResizeMode
	{
		/// <summary>
		/// Stretch to fit dimensions. Aspect ratio is not kept.
		/// </summary>
		Stretch,

		/// <summary>
		/// Smaller dimensions fits while larger dimension gets cropped.
		/// </summary>
		Fill,

		/// <summary>
		/// Larger dimensions fits and smaller dimension stays black.
		/// </summary>
		Fit
	}
}
