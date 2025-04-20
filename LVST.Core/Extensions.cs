using System;

namespace LVST.Core;

public static class Extensions
{
    public static string ToBase64(this string str)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        return Convert.ToBase64String(bytes);
    }
    
    public static string FromBase64(this string str)
    {
        var bytes = Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
    
}