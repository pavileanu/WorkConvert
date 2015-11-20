public class CachableDictionary<Ta, Tb> : Dictionary<Ta, Tb>
{



	public void Add(Ta key, Tb value)
	{
		//Get Dictionary Value
		if (value.GetType().Name == "Dictionary`2") {
			Type a = value.GetType().GetGenericArguments()(0);
			Type b = value.GetType().GetGenericArguments()(1);
			Type[] typeArgs = {
				a,
				b
			};
			Type d = typeof(CachableDictionary<, >);
			//value = CTypeDynamic(value, d.MakeGenericType(typeArgs))
			value = Activator.CreateInstance(d.MakeGenericType(typeArgs));
		}
		if (!value.GetType().Name == "Dictionary`2")
			clsIQ.AuditTrail.Add(DateTime.Now.ToShortTimeString() + " : " + HttpContext.Current.Request("lid") + " : " + key.ToString() + " : " + value.GetType().GetProperty("ID") != null ? value.GetType().GetProperty("ID").GetValue(value) : string.Empty + " : " + value.ToString());
		base.Add(key, value);
	}

	public Tb this[Ta key] {
		get { return base.Item(key); }
		set {
			if (value.GetType().Name == "Dictionary`2") {
				Type a = value.GetType().GetGenericArguments()(0);
				Type b = value.GetType().GetGenericArguments()(1);
				Type[] typeArgs = {
					a,
					b
				};
				Type d = typeof(CachableDictionary<, >);
				//value = CTypeDynamic(value, d.MakeGenericType(typeArgs))
				value = Activator.CreateInstance(d.MakeGenericType(typeArgs));
			}

			if (!value.GetType().Name == "Dictionary`2")
				clsIQ.AuditTrail.Add(DateTime.Now.ToShortTimeString() + " : " + HttpContext.Current.Request("lid") + " : " + key.ToString() + " : " + value.GetType().GetProperty("ID") != null ? value.GetType().GetProperty("ID").GetValue(value).ToString() : string.Empty + " : " + value.ToString());
			base.Item(key) = value;
		}
	}





}
