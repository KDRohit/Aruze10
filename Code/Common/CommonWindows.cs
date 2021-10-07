using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public static class NetCoreExtensions
{
#if UNITY_WSA_10_0 && NETFX_CORE
    public static void Stop(this System.Threading.Timer t)
    {
    }

    public static string ToShortDateString(this System.DateTime d)
    {
        var shortDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate");
        return shortDateFormat.Format(d);
    }

    public static string ToShortTimeString(this System.DateTime d)
    {
        var shortDateFormat = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shorttime");
        return shortDateFormat.Format(d);
    }

    public static void Sleep(this System.Threading.Thread t, int millisecondsTimeout)
    {
        System.Threading.Tasks.Task.Delay(millisecondsTimeout).Wait(0);
    }

    public static void ThreadSleep(int millisecondsTimeout)
    {
        System.Threading.Tasks.Task.Delay(millisecondsTimeout).Wait(0);
    }
#endif
}


