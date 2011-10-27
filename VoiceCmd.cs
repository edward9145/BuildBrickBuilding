using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace BuildBrickBuilding
{
    class VoiceCmd
    {
        public Dictionary<string, Color> colors = new Dictionary<string, Color>
        {
            {"Black",           Color.Black},
            {"Blue",            Color.Blue},
            {"Coral",           Color.Coral},
            {"Cyan",            Color.Cyan},
            {"Gold",            Color.Gold},
            {"Green",           Color.Green},
            {"HotPink",         Color.HotPink},
            {"Maroon",          Color.Maroon},
            {"Magenta",         Color.Magenta},
            {"Navy",            Color.Navy},
            {"Olive",           Color.Olive},
            {"Orange",          Color.Orange},
            {"OrangeRed",       Color.OrangeRed},
            {"Orchid",          Color.Orchid},
            {"Pink",            Color.Pink},
            {"PowderBlue",      Color.PowderBlue},
            {"Red",             Color.Red},
            {"Tomato",          Color.Tomato},
            {"Wheat",           Color.Wheat},
            {"White",           Color.White},
            {"Yellow",          Color.Yellow},
            {"YellowGreen",     Color.YellowGreen},
        };
        public List<string> addCmd = new List<string>()
        {
            "OK",
            "Oh,Yes",
            "Yes",
            "Insert",
            "Put down",
        };
        public List<string> removeCmd = new List<string>()
        {
            "No",
            "Delete",
            "Remove",
            "Get away",
        };
        public List<string> clearCmd = new List<string>()
        {
            "Clear",
            "Clear scene",
            "Reset scene",
        };
        public List<string> exitCmd = new List<string>()
        {
            "Exit program",
            "Close window",
            "Bye byeeeeeeee",
        };

        public List<string> helpCmd = new List<string>();

        public List<string> other = new List<string>();
    }
}
