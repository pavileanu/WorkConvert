using System.IO;

class Profiling
{


	public Dictionary<string, clsProfile> Profile;

	public void Pmark(string key)
	{
		return;

		if (Profile == null) {
			Profile = new Dictionary<string, clsProfile>();
		}

		if (!Profile.ContainsKey(key)) {
			Profile.Add(key, new clsProfile());
		}

		Profile(key).PMark();

	}


	public void Pacc(key)
	{
		return;

		Profile(key).PAccumulate();

	}






}
