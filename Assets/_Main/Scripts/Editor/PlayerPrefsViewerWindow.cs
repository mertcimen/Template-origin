#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

public class PlayerPrefsViewerWindow : EditorWindow
{
	private class Row
	{
		public string Key;
		public string Value;
		public string Edited;
	}

	private readonly List<Row> rows = new();
	private Vector2 scroll;
	private string sourceInfo = "";

	private bool requestReload;

	[MenuItem("Tools/PlayerPrefs Viewer")]
	public static void Open() => GetWindow<PlayerPrefsViewerWindow>("PlayerPrefs");

	private void OnEnable() => Reload();

	private void OnGUI()
	{
		requestReload = false;

		using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
		{
			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
				requestReload = true;

			if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(55)))
			{
				if (EditorUtility.DisplayDialog("Clear PlayerPrefs", "Bu projeye ait PlayerPrefs temizlenecek. Devam?", "Yes", "No"))
				{
					ClearAll();
					requestReload = true;
				}
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Apply All", EditorStyles.toolbarButton, GUILayout.Width(75)))
			{
				ApplyAll();
				requestReload = true;
			}
		}

		EditorGUILayout.Space(6);

		if (!string.IsNullOrEmpty(sourceInfo))
			EditorGUILayout.LabelField(sourceInfo, EditorStyles.miniLabel);

		EditorGUILayout.Space(4);

		using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
		{
			GUILayout.Label("Key", GUILayout.Width(360));
			GUILayout.Label("Value");
			GUILayout.Label("", GUILayout.Width(70));
		}

		scroll = EditorGUILayout.BeginScrollView(scroll);

		for (int i = 0; i < rows.Count; i++)
		{
			var r = rows[i];

			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.SelectableLabel(r.Key, GUILayout.Width(360), GUILayout.Height(EditorGUIUtility.singleLineHeight));
				r.Edited = EditorGUILayout.TextField(r.Edited ?? "", GUILayout.Height(EditorGUIUtility.singleLineHeight));

				if (GUILayout.Button("Set", GUILayout.Width(70)))
				{
					SetOne(r.Key, r.Edited);
					requestReload = true;
				}
			}
		}

		EditorGUILayout.EndScrollView();

		if (requestReload)
		{
			EditorApplication.delayCall += Reload;
			GUIUtility.ExitGUI();
		}
	}

	private void Reload()
	{
		rows.Clear();
		sourceInfo = "";

		var dict = ReadAllPrefs(out var info);
		sourceInfo = info;

		foreach (var kv in dict.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
			rows.Add(new Row { Key = kv.Key, Value = kv.Value ?? "", Edited = kv.Value ?? "" });

		Repaint();
	}

	private void SetOne(string key, string value)
	{
		PlayerPrefs.SetString(key, value ?? "");
		PlayerPrefs.Save();
	}

	private void ApplyAll()
	{
		for (int i = 0; i < rows.Count; i++)
			PlayerPrefs.SetString(rows[i].Key, rows[i].Edited ?? "");
		PlayerPrefs.Save();
	}

	private void ClearAll()
	{
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();

#if UNITY_EDITOR_WIN
		TryClearWindowsStores();
#elif UNITY_EDITOR_OSX
		TryClearMacStores();
#elif UNITY_EDITOR_LINUX
		TryClearLinuxStores();
#endif
	}

	private static Dictionary<string, string> ReadAllPrefs(out string info)
	{
#if UNITY_EDITOR_WIN
		return ReadWindows(out info);
#elif UNITY_EDITOR_OSX
		return ReadMac(out info);
#elif UNITY_EDITOR_LINUX
		return ReadLinux(out info);
#else
		info = "Unsupported platform";
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#endif
	}

#if UNITY_EDITOR_WIN
	private static Dictionary<string, string> ReadWindows(out string info)
	{
		string company = GetCompanyName();
		string product = GetProductName();

		var candidates = new List<string>
		{
			$@"Software\Unity\UnityEditor\{company}\{product}",
			$@"Software\{company}\{product}"
		};

		string best = PickBestRegistryPath(candidates, product);
		if (string.IsNullOrEmpty(best))
		{
			info = "Windows: Pref bulunamadı (registry).";
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		info = @"Windows: HKCU\" + best;
		return ReadRegistryPrefs(best);
	}

	private static string PickBestRegistryPath(List<string> paths, string product)
	{
		string best = "";
		int bestCount = 0;

		foreach (var p in paths.Distinct())
		{
			int count = RegistryCountValues(p);
			if (count > bestCount)
			{
				bestCount = count;
				best = p;
			}
		}

		if (bestCount > 0) return best;

		string unityRoot = @"Software\Unity\UnityEditor";
		using var root = Registry.CurrentUser.OpenSubKey(unityRoot);
		if (root == null) return "";

		foreach (var company in root.GetSubKeyNames())
		{
			string path = $@"{unityRoot}\{company}\{product}";
			int count = RegistryCountValues(path);
			if (count > bestCount)
			{
				bestCount = count;
				best = path;
			}
		}

		return bestCount > 0 ? best : "";
	}

	private static int RegistryCountValues(string path)
	{
		using var key = Registry.CurrentUser.OpenSubKey(path);
		return key == null ? 0 : key.GetValueNames().Length;
	}

	private static Dictionary<string, string> ReadRegistryPrefs(string path)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		using var key = Registry.CurrentUser.OpenSubKey(path);
		if (key == null) return result;

		foreach (var name in key.GetValueNames())
		{
			object val = key.GetValue(name);
			if (val == null) continue;

			string k = name;
			if (k.EndsWith("_h", StringComparison.OrdinalIgnoreCase))
				k = k.Substring(0, k.Length - 2);

			result[k] = RegistryValueToString(val);
		}

		return result;
	}

	private static string RegistryValueToString(object val)
	{
		if (val is byte[] bytes)
		{
			try { return Encoding.UTF8.GetString(bytes).TrimEnd('\0'); }
			catch { return Convert.ToBase64String(bytes); }
		}
		return val.ToString() ?? "";
	}

	private static void TryClearWindowsStores()
	{
		string company = GetCompanyName();
		string product = GetProductName();

		var paths = new[]
		{
			$@"Software\Unity\UnityEditor\{company}\{product}",
			$@"Software\{company}\{product}"
		};

		foreach (var p in paths)
		{
			using var key = Registry.CurrentUser.OpenSubKey(p, writable: true);
			if (key == null) continue;

			foreach (var name in key.GetValueNames())
				key.DeleteValue(name, false);
		}
	}
#endif

#if UNITY_EDITOR_OSX
	private static Dictionary<string, string> ReadMac(out string info)
	{
		string company = GetCompanyName();
		string product = GetProductName();
		string prefsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Preferences");

		var candidateFiles = new List<string>
		{
			Path.Combine(prefsDir, $"unity.{company}.{product}.plist"),
			Path.Combine(prefsDir, $"com.unity3d.{company}.{product}.plist"),
			Path.Combine(prefsDir, $"unity3d.{company}.{product}.plist")
		};

		foreach (var f in Directory.EnumerateFiles(prefsDir, "*.plist"))
		{
			string name = Path.GetFileName(f);
			if (name.IndexOf(product, StringComparison.OrdinalIgnoreCase) >= 0 &&
			    name.IndexOf("unity", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				if (!candidateFiles.Contains(f))
					candidateFiles.Add(f);
			}
		}

		string bestFile = PickBestPlist(candidateFiles);
		if (string.IsNullOrEmpty(bestFile))
		{
			info = "macOS: Pref plist bulunamadı (Library/Preferences).";
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		string xml = PlutilToXml(bestFile);
		if (string.IsNullOrEmpty(xml))
		{
			info = "macOS: plutil ile plist okunamadı.";
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		var dict = ParsePlistXml(xml);
		info = $"macOS: {bestFile} (keys: {dict.Count})";
		return dict;
	}

	private static string PickBestPlist(List<string> files)
	{
		string best = "";
		int bestCount = 0;

		foreach (var f in files.Distinct())
		{
			if (!File.Exists(f)) continue;

			string xml = PlutilToXml(f);
			if (string.IsNullOrEmpty(xml)) continue;

			int count = CountPlistKeys(xml);
			if (count > bestCount)
			{
				bestCount = count;
				best = f;
			}
		}

		return bestCount > 0 ? best : "";
	}

	private static int CountPlistKeys(string xml)
	{
		try
		{
			var doc = XDocument.Parse(xml);
			return doc.Descendants("key").Count();
		}
		catch { return 0; }
	}

	private static string PlutilToXml(string plistPath)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "/usr/bin/plutil",
				Arguments = $"-convert xml1 -o - \"{plistPath}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var p = Process.Start(psi);
			if (p == null) return "";
			string output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			if (p.ExitCode != 0) return "";
			return output ?? "";
		}
		catch { return ""; }
	}

	private static Dictionary<string, string> ParsePlistXml(string xml)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		try
		{
			var doc = XDocument.Parse(xml);
			var dict = doc.Descendants("dict").FirstOrDefault();
			if (dict == null) return result;

			var nodes = dict.Elements().ToList();
			for (int i = 0; i < nodes.Count - 1; i++)
			{
				if (nodes[i].Name != "key") continue;

				string key = nodes[i].Value ?? "";
				var valNode = nodes[i + 1];

				string value = valNode.Name.LocalName switch
				{
					"string" => valNode.Value ?? "",
					"integer" => valNode.Value ?? "",
					"real" => valNode.Value ?? "",
					"true" => "true",
					"false" => "false",
					"data" => (valNode.Value ?? "").Trim(),
					_ => valNode.Value ?? ""
				};

				if (!string.IsNullOrEmpty(key))
					result[key] = value;
			}
		}
		catch { }

		return result;
	}

	private static void TryClearMacStores()
	{
		string company = GetCompanyName();
		string product = GetProductName();
		string prefsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Preferences");

		var files = new[]
		{
			Path.Combine(prefsDir, $"unity.{company}.{product}.plist"),
			Path.Combine(prefsDir, $"com.unity3d.{company}.{product}.plist"),
			Path.Combine(prefsDir, $"unity3d.{company}.{product}.plist")
		};

		foreach (var f in files)
		{
			try { if (File.Exists(f)) File.Delete(f); }
			catch { }
		}
	}
#endif

#if UNITY_EDITOR_LINUX
	private static Dictionary<string, string> ReadLinux(out string info)
	{
		string company = GetCompanyName();
		string product = GetProductName();

		string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d", company, product);
		string prefsFile = Path.Combine(basePath, "prefs");

		if (!File.Exists(prefsFile))
		{
			info = "Linux: prefs dosyası bulunamadı.";
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var line in File.ReadAllLines(prefsFile))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;
			int eq = line.IndexOf('=');
			if (eq <= 0) continue;
			string k = line.Substring(0, eq).Trim();
			string v = line.Substring(eq + 1).Trim();
			if (!string.IsNullOrEmpty(k))
				result[k] = v;
		}

		info = $"Linux: {prefsFile} (keys: {result.Count})";
		return result;
	}

	private static void TryClearLinuxStores()
	{
		string company = GetCompanyName();
		string product = GetProductName();

		string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d", company, product);
		string prefsFile = Path.Combine(basePath, "prefs");

		try { if (File.Exists(prefsFile)) File.Delete(prefsFile); }
		catch { }
	}
#endif

	private static string GetProductName()
	{
		string p = PlayerSettings.productName;
		if (!string.IsNullOrEmpty(p)) return p;

		p = Application.productName;
		if (!string.IsNullOrEmpty(p)) return p;

		string proj = Path.GetFileName(Path.GetDirectoryName(Application.dataPath));
		return string.IsNullOrEmpty(proj) ? "UnknownProduct" : proj;
	}

	private static string GetCompanyName()
	{
		string c = PlayerSettings.companyName;
		if (!string.IsNullOrEmpty(c)) return c;

		c = Application.companyName;
		if (!string.IsNullOrEmpty(c)) return c;

		return "DefaultCompany";
	}
}
#endif
