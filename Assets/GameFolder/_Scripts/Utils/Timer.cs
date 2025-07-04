using System;
using UnityEngine;

namespace SKC.Helpers
{
	// TODO: Add Time.deltaTime to here instead of every other classes
	public class Timer : MonoBehaviour
	{
		public struct EventArgs
		{
			public float Value;
			public float MaxValue;
			public bool IsComplete;

			public EventArgs(float value, float maxValue, bool isComplete)
			{
				Value = value;
				MaxValue = maxValue;
				IsComplete = isComplete;
			}
		}

		public float _duration = 0.25f;

		float _value;
		
		public event Action<EventArgs> ValueChanged;

		public bool IsCompleted => _value >= _duration;
		public bool IsStop = false;

		public void Add(float amount)
		{
			if (_value >= _duration)
			{
				ValueChanged?.Invoke(new EventArgs(_value, _duration,true));
				return;
			}
			
			_value += amount;
			ValueChanged?.Invoke(new EventArgs(_value, _duration,false));
		}
		
		public void Tick()
		{
			if(!IsStop)
				Add(Time.deltaTime);
		}

		public void SetZero()
		{
			_value = 0f;
			ValueChanged?.Invoke(new EventArgs(_value, _duration,false));
		}
	}
}
