using System;

namespace NocturneAnnex.Horror
{
    public enum ToyBoxContent { Empty, Key, SpringToy, FootstepBeat }

    /// <summary>
    /// Eight toy boxes hiding one key, one spring toy (a jump scare) and one footstep beat. The scares never block anything: every box can be
    /// opened in any order, each only once, so the key is always reachable. Layout comes from a seed, so a reload shows the same boxes.
    /// </summary>
    public sealed class ToyBoxModel
    {
        public const int BoxCount = 8;

        readonly ToyBoxContent[] _contents = new ToyBoxContent[BoxCount];
        readonly bool[] _opened = new bool[BoxCount];

        public int OpenedCount { get; private set; }
        public bool KeyFound { get; private set; }

        public ToyBoxModel(int seed)
        {
            var r = new Random(seed);
            var order = new int[BoxCount];
            for (int i = 0; i < BoxCount; i++) order[i] = i;
            for (int i = BoxCount - 1; i > 0; i--) { int j = r.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            _contents[order[0]] = ToyBoxContent.Key;
            _contents[order[1]] = ToyBoxContent.SpringToy;
            _contents[order[2]] = ToyBoxContent.FootstepBeat;
        }

        public bool IsOpened(int box) => box >= 0 && box < BoxCount && _opened[box];

        /// <summary>Opens a box and returns what was inside. A box already opened, or an unknown index, gives Empty.</summary>
        public ToyBoxContent Open(int box)
        {
            if (box < 0 || box >= BoxCount || _opened[box]) return ToyBoxContent.Empty;
            _opened[box] = true;
            OpenedCount++;
            var c = _contents[box];
            if (c == ToyBoxContent.Key) KeyFound = true;
            return c;
        }
    }
}
