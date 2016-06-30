﻿using System;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Mono.Cecil;

/// <summary>
/// Cross reflection framework.
/// </summary>
public static class Cross {

    private readonly static Type[] a_Type_0 = new Type[0];
    private readonly static object[] a_object_0 = new object[0];

    static Cross() {
        string os;
        //for mono, get from
        //static extern PlatformID Platform
        PropertyInfo property_platform = typeof(Environment).GetProperty("Platform", BindingFlags.NonPublic | BindingFlags.Static);
        if (property_platform != null) {
            os = property_platform.GetValue(null, a_object_0).ToString().ToLower();
        } else {
            //for .net, use default value
            os = Environment.OSVersion.Platform.ToString().ToLower();
        }

        if (os.Contains("win")) {
            CurrentPlatform = Platform.Windows;
        } else if (os.Contains("mac") || os.Contains("osx")) {
            CurrentPlatform = Platform.MacOS;
        } else if (os.Contains("lin") || os.Contains("unix")) {
            CurrentPlatform = Platform.Linux;
        }

    }

    public static Platform CurrentPlatform { get; private set; } 

    public static ICrossConfig Config = new CrossGungeonConfig();

    public static Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

    public static Type XType(this string name_, Platform from) {
        return name_.XType((int) from, (int) CurrentPlatform);
    }

    public static Type XType(this string name_, int from, int to) {
        Type type;
        if (TypeMap.TryGetValue(name_, out type)) {
            return type;
        }
        string name = Config.TypeName(name_, from, to);

        foreach (Assembly assembly in Config.Assemblies) {
            if ((type = assembly.GetType(name, false)) != null) {
                break;
            }
        }

        return TypeMap[name_] = type;
    }

}

/// <summary>
/// Cross reflection configuration interface.
/// </summary>
public interface ICrossConfig {
    IEnumerable<Assembly> Assemblies { get; }
    string TypeName(string name, int from, int to);
}

/// <summary>
/// Cross reflection platform enum that can be used as "from" and "to".
/// </summary>
public enum Platform : int {
    Unknown = 0,
    Windows = 1,
    MacOS = 2,
    Linux = 3
}
