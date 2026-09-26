using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace BeterEnter
{
    internal static class TrayIcons
    {
        public static Icon Load(bool paused)
        {
            string name = paused ? "BeterEnter.PausedIcon" : "BeterEnter.ActiveIcon";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidOperationException("Missing embedded icon: " + name);
                using (Icon original = new Icon(stream, System.Windows.Forms.SystemInformation.SmallIconSize)) return (Icon)original.Clone();
            }
        }
    }
}
