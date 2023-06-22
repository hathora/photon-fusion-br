// Created by dylan@hathora.dev

using System;
using System.IO;
using System.Text;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Hathora.Core.Scripts.Runtime.Common.Utils
{
    public static class HathoraUtils
    {
        /// <summary>
        /// (!) Hathora SDK Enums starts at index 1; not 0: Care of indexes
        /// TODO: If this ever becomes 0, delete this const and update all refs.
        /// </summary>
        public const int SDK_ENUM_STARTING_INDEX = 1;

        /// <summary>
        /// eg: "E1HKfn68Pkms5zsZsvKONw=="
        /// https://stackoverflow.com/a/9279005 
        /// </summary>
        /// <returns></returns>
        public static string GenerateShortUid(bool omitEndDblEquals)
        {
            string shortId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            
            if (omitEndDblEquals && shortId.EndsWith("=="))
                shortId = shortId[..^2]; // Exclude the last 2 chars

            Debug.Log($"[HathoraUtils] ShortId Generated: {shortId}");

            return shortId;
        }

        public static string NormalizePath(string _path) =>
            Path.GetFullPath(_path);

        /// <summary>
        /// Gets path to Unity proj root, then normalizes the/path/slashes.
        /// </summary>
        /// <returns></returns>
        public static string GetNormalizedPathToProjRoot()
        {
            string dirtyPathToUnityProjRoot = Directory.GetParent(Application.dataPath)?.ToString();
            return dirtyPathToUnityProjRoot == null 
                ? null 
                : NormalizePath(dirtyPathToUnityProjRoot);
        }

        /// <summary>Returns null on null || MinValue</summary>
        public static string GetFriendlyDateTimeShortStr(DateTime? _dateTime)
        {
            if (_dateTime == null || _dateTime == DateTime.MinValue)
                return null;

            return $"{_dateTime.Value.ToShortDateString()} {_dateTime.Value.ToShortTimeString()}";
        }

        /// <summary>Returns null on null || MinValue</summary>
        public static string GetFriendlyDateTimeDiff(
            TimeSpan _duration, 
            bool _exclude0)
        {
            int totalHours = (int)_duration.TotalHours;
            int totalMinutes = (int)_duration.TotalMinutes % 60;
            int totalSeconds = (int)_duration.TotalSeconds % 60;
            
            if (totalHours > 0 || !_exclude0)
                return $"{totalHours}h:{totalMinutes}m:{totalSeconds}s";
            
            return totalMinutes > 0 
                ? $"{totalMinutes}m:{totalSeconds}s" 
                : $"{totalSeconds}s";
        }


        /// <summary>
        /// </summary>
        /// <param name="_startTime"></param>
        /// <param name="_endTime"></param>
        /// <param name="exclude0">If 0, </param>
        /// <returns>hh:mm:ss</returns>
        public static string GetFriendlyDateTimeDiff(
            DateTime _startTime, 
            DateTime _endTime,
            bool exclude0)
        {
            TimeSpan duration = _endTime - _startTime;

            return GetFriendlyDateTimeDiff(duration, exclude0);
        }
    }
}