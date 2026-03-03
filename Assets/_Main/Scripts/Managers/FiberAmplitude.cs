using System.Collections.Generic;
using System.Threading.Tasks;
using Fiber.Utilities;

namespace _Main.Scripts.Analytics
{
	public class FiberAmplitude : Singleton<FiberAmplitude>
	{
		private global::Amplitude _amplitude;
		private bool IsDev => true;

		private readonly string _devApiKey = "development9999948452";
		private readonly string _prodApiKey = "production08247508274";

		public async Task<bool> Init()
		{
			// _analyticsType = AnalyticsType.Amplitude;
			_amplitude = global::Amplitude.getInstance();
			_amplitude.setServerUrl("https://api2.amplitude.com");
			_amplitude.logging = true;
			_amplitude.trackSessionEvents(true);
			_amplitude.useAdvertisingIdForDeviceId();
			_amplitude.setUseDynamicConfig(true);
			_amplitude.setServerZone(AmplitudeServerZone.US);

			if (IsDev)
				_amplitude.init(_devApiKey);
			else
				_amplitude.init(_prodApiKey);

			return true;
		}

		public void SendCustomEvent(EAnalyticsEvent eventType, Dictionary<string, object> parameters)
		{
			var eventName = AnalyticsReferences.EventKeyTable[eventType];
			_amplitude.logEvent(eventName, parameters);
			_amplitude.uploadEvents();
		}
	}
}