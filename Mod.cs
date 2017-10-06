using ICities;
using System;

namespace SingleTrackAI
{
	public class Mod: IUserMod {
		
		public static string modName = "SingleTrainTrackAI";
		public static String modID = "SingleTrainTrackAI";
		public static string version = "1.1.0";
		
		public string Name {
			get { return modName; }
		}
		public string Description {
			get { return "Basic AI for 1 lane 2 ways train tracks from One-Way Train Tracks mod by BloodyPenguin"; }
		}
	}
}

