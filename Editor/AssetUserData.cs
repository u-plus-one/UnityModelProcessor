﻿using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class AssetUserData
	{
		public Dictionary<string, object> data;

		public bool IsDirty { get; private set; }

		public AssetUserData()
		{
			data = new Dictionary<string, object>();
		}

		private AssetUserData(Dictionary<string, object> data)
		{
			this.data = data;
		}

		public bool ContainsKey(string key) => data.ContainsKey(key);

		public object GetValue(string key, object fallback = default)
		{
			if(data.TryGetValue(key, out object value)) return value;
			return fallback;
		}

		public bool GetBool(string key, bool fallback = default)
		{
			if(data.TryGetValue(key, out object value)) return System.Convert.ToBoolean(value);
			return fallback;
		}

		public string GetString(string key, string fallback = default)
		{
			if(data.TryGetValue(key, out object value)) return System.Convert.ToString(value);
			return fallback;
		}

		public int GetInt(string key, int fallback = default)
		{
			if(data.TryGetValue(key, out object value)) return System.Convert.ToInt32(value);
			return fallback;
		}

		public float GetFloat(string key, float fallback = default)
		{
			if(data.TryGetValue(key, out object value)) return System.Convert.ToSingle(value);
			return fallback;
		}

		public void SetValue(string key, object value)
		{
			if(data.ContainsKey(key) && data[key] == value)
			{
				return;
			}
			data[key] = value;
			IsDirty = true;
		}

		public string Serialize()
		{
			IsDirty = false;
			return JsonConvert.SerializeObject(data);
		}

		public bool ApplyModified(Object asset)
		{
			if(!IsDirty) return false;
			return ApplyModified(AssetDatabase.GetAssetPath(asset));
		}

		public bool ApplyModified(string assetPath)
		{
			if(!IsDirty) return false;
			AssetImporter.GetAtPath(assetPath).userData = Serialize();
			return true;
		}

		public static AssetUserData Get(Object asset)
		{
			return Get(AssetDatabase.GetAssetPath(asset));
		}

		public static AssetUserData Get(string assetPath)
		{
			return TryDeserialize(AssetImporter.GetAtPath(assetPath).userData);
		}

		public static AssetUserData TryDeserialize(string userDataString)
		{
			if(string.IsNullOrWhiteSpace(userDataString))
			{
				return new AssetUserData();
			}
			else
			{
				return new AssetUserData(JsonConvert.DeserializeObject<Dictionary<string, object>>(userDataString));
			}
		}
	}
}
