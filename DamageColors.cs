using System.Collections.Generic;
using UnityEngine;
using XRL;

namespace Gokudera_ElPsyCongroo_ICTooltips.Utilities
{
    public class DamageColors
    {
        public static IDictionary<string, Color> Colors = new Dictionary<string, Color>()
        {
            { "Heat", The.Color.DarkOrange },
            { "Fire", The.Color.Orange },
            { "Vaporized", The.Color.Brown },
            { "Cold", The.Color.Cyan },
            { "Electric", The.Color.Yellow },
            { "Acid", The.Color.DarkGreen },
            { "Disintegration", The.Color.Black },
            { "Plasma", The.Color.Green },
            { "Light", The.Color.Yellow },
            { "Poison", The.Color.Green },
            { "Bleeding", The.Color.DarkRed },
            { "Asphyxiation", The.Color.Brown },
            { "Metabolic", The.Color.Brown },
            { "Drain", The.Color.DarkMagenta },
            { "Psionic", The.Color.DarkMagenta },
            { "Mental", The.Color.Magenta },
            { "Physical", The.Color.Red },
            { "Illusion", The.Color.DarkBlue },
            { "Neutron", The.Color.Blue },
        };
    }
}
