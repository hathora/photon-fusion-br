// Created by dylan@hathora.dev

using System;
using System.IO;
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
    }
}