using System;
using System.Collections.Generic;

namespace OpenVisionLab.Inspection.Smoke
{
    internal sealed class SmokeRunner
    {
        internal int Passed { get; private set; }

        internal int Total { get; private set; }

        internal void Run(IEnumerable<SmokeCase> cases)
        {
            foreach (SmokeCase smokeCase in cases)
            {
                Total++;
                smokeCase.Execute();
                Passed++;
                Console.WriteLine("PASS | " + smokeCase.Name);
            }
        }
    }
}
