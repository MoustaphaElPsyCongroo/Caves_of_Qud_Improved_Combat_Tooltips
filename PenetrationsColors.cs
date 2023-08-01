using System.Collections.Generic;
using UnityEngine;
using XRL;

namespace Improved_Damage_Tooltips.Utilities
{
    public class PenetrationsColors
    {
        public static IDictionary<int, Color> Colors = new Dictionary<int, Color>()
        {
            { 1, The.Color.Brown },
            { 2, The.Color.Yellow },
            { 3, The.Color.DarkRed },
            { 4, The.Color.Red },
            { 5, The.Color.DarkMagenta },
            { 10, The.Color.Magenta },
            { 20, The.Color.Orange }
        };
    }
}
