﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TeleCore.Network.IO;
using TMPro;
using Verse;

namespace TeleCore.Network.Utility;

public static class IOUtils
{
    //  \\\\\\\\
    //  \\\XX\\\
    //  \\#++#\\
    //  \I++++O\
    //  \I++++O\
    //  \\#++#\\
    //  \\\XX\\\
    //  \\\\\\\\

    internal const char Input = 'I';
    internal const char Output = 'O';
    internal const char TwoWay = 'X';
    internal const char Empty = '#';
    internal const char Visual = '+';

    internal const string RegexPattern = @"\[[^\]]*\]|.";

    public static NetworkIOMode ParseIOMode(char c)
    {
        return c switch
        {
            Input => NetworkIOMode.Input,
            Output => NetworkIOMode.Output,
            TwoWay => NetworkIOMode.TwoWay,
            Empty => NetworkIOMode.None,
            Visual => NetworkIOMode.Visual,
            _ => throw new ArgumentException($"Invalid IO mode character: {c}")
        };
    }

    public static Rot4 RelativeDir(IntVec3 center, IntVec3 pos, IntVec2 size)
    {
        var top = center.z - size.z / 2;
        var bottom = center.z + size.z / 2;
        var left = center.x - size.x / 2;
        var right = center.x + size.x / 2;

        if (pos.z < top)
        {
            if (pos.x < left) //NorthWest
                return Rot4.Invalid;
            if (pos.x > right) //NorthEast
                return Rot4.Invalid;
            return Rot4.North;
        }

        if (pos.z > bottom)
        {
            if (pos.x < left) //SouthWest
                return Rot4.Invalid;
            if (pos.x > right) //SouthEast
                return Rot4.Invalid;
            return Rot4.South;
        }

        if (pos.x < left) //West
            return Rot4.West;

        if (pos.x > right) //East
            return Rot4.East;

        //throw new ArgumentException("The position cannot be inside the bounding box.");
        return Rot4.Invalid;
    }

    public static bool Matches(this NetworkIOMode innerMode, NetworkIOMode outerMode)
    {
        var innerInput = (innerMode & NetworkIOMode.Input) == NetworkIOMode.Input;
        var outerInput = (outerMode & NetworkIOMode.Input) == NetworkIOMode.Input;

        var innerOutput = (innerMode & NetworkIOMode.Output) == NetworkIOMode.Output;
        var outerOutput = (outerMode & NetworkIOMode.Output) == NetworkIOMode.Output;

        return (innerInput && outerOutput) || (outerInput && innerOutput);
    }

    public static List<IOCellPrototype> GenerateFromPattern(string ioPattern, IntVec2 thingSize)
    {
        var size = thingSize;
        var width = size.x;
        var height = size.z;

        var rect = new CellRect(0 - (width - 1) / 2, 0 - (height - 1) / 2, width, height);
        var rectList = rect.ToArray();

        ioPattern = DefaultFallbackIfNecessary(ioPattern, size);
        var modeGrid = GetIOModeArrey(ioPattern);

        var result = new List<IOCellPrototype>();

        for (var y = 0; y < rect.Height; y++)
        for (var x = 0; x < rect.Width; x++)
        {
            var actualIndex = y * rect.Width + x;
            var ioMode = modeGrid[actualIndex];
            var cell = rectList[actualIndex];

            if (ioMode != NetworkIOMode.None)
                result.Add(new IOCellPrototype
                {
                    offset = cell,
                    direction = RelativeDir(IntVec3.Zero, cell, size),
                    mode = ioMode
                });
        }

        return result;
    }

    /// <summary>
    ///     Rotates the pattern array to match the rotation of the thing.
    /// </summary>
    internal static IOCell[] RotateIOCells(IOCell[] arr, Rot4 rotation, IntVec2 size)
    {
        var xWidth = size.x;
        var yHeight = size.z;
        if (rotation == Rot4.East) arr = arr.RotateLeft(xWidth, yHeight);
        if (rotation == Rot4.South) arr = arr.FLipHorizontal(xWidth, yHeight);
        if (rotation == Rot4.West) arr = arr.RotateRight(xWidth, yHeight);

        return arr;
    }

    internal static NetworkIOMode[] GetIOModeArrey(string input)
    {
        input = input.Replace("|", "");
        var matches = Regex.Matches(input, RegexPattern);
        var modeGrid = new NetworkIOMode[matches.Count];
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.Value.Length == 1)
                modeGrid[i] = ParseIOMode(match.Value[0]);
        }

        return modeGrid;
    }

    internal static string DefaultFallbackIfNecessary(string pattern, IntVec2 size)
    {
        if (pattern != null) return pattern;

        var widthx = size.x;
        var heighty = size.z;

        var charArr = new char[widthx * heighty];

        for (var x = 0; x < widthx; x++)
        for (var y = 0; y < heighty; y++)
            charArr[x + y * widthx] = TwoWay;
        return charArr.ArrayToString();
    }
}