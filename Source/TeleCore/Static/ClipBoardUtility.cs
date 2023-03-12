﻿using System.Collections.Generic;

namespace TeleCore
{
    /// <summary>
    /// Dynamic Clipboard utility, allows you to save any type via a string tag, and retrieve it the same way.
    /// </summary>
    
    
    //TODO: FIX CLIPBOARD - NOT SAVING
    public static class ClipBoardUtility
    {
        private static readonly Dictionary<string, object> _clipboard = new Dictionary<string, object>();

        public static bool IsActive(string clipBoardKey)
        {
            return _clipboard.TryGetValue(clipBoardKey, out var value) && value != null;
        }
        
        public static T TryGetClipBoard<T>(string tag)
        {
            if (_clipboard.TryGetValue(tag, out var value))
            {
                return (T)value;
            }
            return (T)(object)null;
        }

        public static void TrySetClipBoard<T>(string tag, T value)
        {
            if (_clipboard.ContainsKey(tag))
            {
                _clipboard.Add(tag, value);
            }
        }
    }
}
