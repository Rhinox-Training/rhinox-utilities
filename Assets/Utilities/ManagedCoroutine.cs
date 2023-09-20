// ReSharper disable All
/// TaskManager.cs -> ManagedCoroutine.cs
/// Copyright (c) 2011, Ken Rockot  <k-e-n-@-REMOVE-CAPS-AND-HYPHENS-oz.gs>.  All rights reserved.
/// Everyone is granted non-exclusive license to do anything at all with this code.
///
/// Copyright (c) 2019, Rhinox NV https://www.rhinox.be/
/// Changes were made to enable better event support and renamed to ManagedCoroutine
///
/// This is a new coroutine interface for Unity.
///
/// The motivation for this is twofold:
///
/// 1. The existing coroutine API provides no means of stopping specific
///    coroutines; StopCoroutine only takes a string argument, and it stops
///    all coroutines started with that same string; there is no way to stop
///    coroutines which were started directly from an enumerator.  This is
///    not robust enough and is also probably pretty inefficient.
///
/// 2. StartCoroutine and friends are MonoBehaviour methods.  This means
///    that in order to start a coroutine, a user typically must have some
///    component reference handy.  There are legitimate cases where such a
///    constraint is inconvenient.  This implementation hides that
///    constraint from the user.
///
/// Example usage:
///
/// ----------------------------------------------------------------------------
/// IEnumerator MyAwesomeTask()
/// {
///     while(true) {
///         Debug.Log("Logcat iz in ur consolez, spammin u wif messagez.");
///         yield return null;
////    }
/// }
///
/// IEnumerator TaskKiller(float delay, Task t)
/// {
///     yield return new WaitForSeconds(delay);
///     t.Stop();
/// }
///
/// void SomeCodeThatCouldBeAnywhereInTheUniverse()
/// {
///     Task spam = new Task(MyAwesomeTask());
///     new Task(TaskKiller(5, spam));
/// }
/// ----------------------------------------------------------------------------
///
/// When SomeCodeThatCouldBeAnywhereInTheUniverse is called, the debug console
/// will be spammed with annoying messages for 5 seconds.
///
/// Simple, really.  There is no need to initialize or even refer to TaskManager.
/// When the first Task is created in an application, a "TaskManager" GameObject
/// will automatically be added to the scene root with the TaskManager component
/// attached.  This component will be responsible for dispatching all coroutines
/// behind the scenes.
///
/// Task also provides an event that is triggered when the coroutine exits.

using System;
using UnityEngine;
using System.Collections;
using Rhinox.Perceptor;

namespace Rhinox.Utilities
{
	/// A Task object represents a coroutine.  Tasks can be started, paused, and stopped.
	/// It is an error to attempt to start a task that has been stopped or which has
	/// naturally terminated.
	public class ManagedCoroutine
	{
		public class WaitForManagedCoroutine : CustomYieldInstruction
		{
			private ManagedCoroutine _coroutine;
			public WaitForManagedCoroutine(ManagedCoroutine c)
			{
				_coroutine = c;
			}

			public override bool keepWaiting => !_coroutine.Finished;
		}

		public static ManagedCoroutine Begin(IEnumerator c, bool autoStart = true)
		{
			var coroutine = new ManagedCoroutine(c);
			if (autoStart)
				coroutine.Start();
			return coroutine;
		}
		
		public static ManagedCoroutine Begin(IEnumerator c, FinishedHandler callback)
		{
			var coroutine = new ManagedCoroutine(c);
			coroutine.OnFinished += callback;
			coroutine.Start();
			return coroutine;
		}
		
		/// Paused tasks are considered to be running.
		public bool Running => _coroutine.Running;

		public bool Paused => _coroutine.Paused;

		public bool Finished => _coroutine.Finished;

		public bool LogErrors = true;

		/// Delegate for termination subscribers. Manual is true if
		/// the coroutine was stopped with an explicit call to Stop().
		public delegate void FinishedHandler(bool manual);
		public event FinishedHandler OnFinished;
		
		/// Delegate for termination subscribers of failed state.
		/// The exception which caused the coroutine to fail is passed as an argument
		public delegate void FailedHandler(Exception e);
		public event FailedHandler OnFailed;

		/// If autoStart is true (default) the task is automatically started upon construction.
		[Obsolete("Use ManagedCoroutine.Begin instead")]
		public ManagedCoroutine(IEnumerator c, bool autoStart = true)
			: this(c)
		{
			if (autoStart) Start();
		}

		private ManagedCoroutine(IEnumerator c)
		{
			_coroutine = CoroutineManager.Create(c);
			_coroutine.OnFinished += CoroutineOnFinished;
			_coroutine.OnFailed += CoroutineOnFailed;
		}

		public void Start() => _coroutine.Start();
		public void Stop() => _coroutine.Stop();
		public void Pause() => _coroutine.Pause();
		public void Unpause() => _coroutine.Unpause();

		private CoroutineManager.CoroutineState _coroutine;

		void CoroutineOnFinished(bool manual)
		{
			OnFinished?.Invoke(manual);
		}
		
		void CoroutineOnFailed(Exception e)
		{
			if (LogErrors)
				Debug.LogError(e);
			OnFailed?.Invoke(e);
		}

		public CustomYieldInstruction WaitForComplete()
		{
			return new WaitForManagedCoroutine(this);
		}
	}

	class CoroutineManager : MonoBehaviour
	{
		private static CoroutineManager _singleton;

		public class CoroutineState
		{
			public bool Running
			{
				get { return _running; }
			}

			public bool Paused
			{
				get { return _paused; }
			}

			public bool Finished
			{
				get { return _finished; }
			}

			public delegate void FinishedHandler(bool manual);
			public event FinishedHandler OnFinished;

			public delegate void FailedHandler(Exception e);
			public event FailedHandler OnFailed;

			private IEnumerator _coroutine;
			private bool _running;
			private bool _paused;
			private bool _stopped;
			private bool _finished;

			public CoroutineState(IEnumerator c) => _coroutine = c;
			public void Pause() => _paused = true;
			public void Unpause() => _paused = false;

			public void Start()
			{
				_running = true;
				_singleton.StartCoroutine(CallWrapper());
			}

			public void Stop()
			{
				_stopped = true;
				_running = false;
			}

			IEnumerator CallWrapper()
			{
				yield return null;
				IEnumerator e = null;
				try
				{
					e = _coroutine;
				}
				catch (Exception exception)
				{
					TriggerFailed(exception);
					yield break;
				}

				while (_running)
				{
					if (_paused)
					{
						yield return null;
					}
					else
					{
						bool canYield = false;
						try
						{

							canYield = e != null && e.MoveNext();
						}
						catch (Exception exception)
						{
							TriggerFailed(exception);
							yield break;
						}

						if (canYield)
						{
							yield return e.Current;
						}
						else
						{
							_running = false;
						}
					}
				}

				if (OnFinished != null)
					OnFinished(_stopped);

				_finished = true;
			}

			private void TriggerFailed(Exception exception)
			{
				if (OnFailed != null)
					OnFailed(exception);
			}
		}


		public static CoroutineState Create(IEnumerator coroutine)
		{
			if (_singleton == null)
			{
				var go = new GameObject("[Generated] CoroutineManager");
				go.hideFlags = HideFlags.HideAndDontSave;
				DontDestroyOnLoad(go);
				_singleton = go.AddComponent<CoroutineManager>();
			}

			return new CoroutineState(coroutine);
		}
	}

	public static class ManagedCoroutineExtensions
	{
		public static bool IsActive(this ManagedCoroutine coroutine)
		{
			if (coroutine == null) return false;
			return coroutine.Running;
		}
	}
}
