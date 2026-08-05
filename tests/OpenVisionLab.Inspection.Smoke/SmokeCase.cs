using System;

namespace OpenVisionLab.Inspection.Smoke
{
    internal sealed class SmokeCase
    {
        internal SmokeCase(string name, Action execute)
        {
            Name = name;
            Execute = execute;
        }

        internal string Name { get; }

        internal Action Execute { get; }
    }
}
