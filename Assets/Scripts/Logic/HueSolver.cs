using System.Collections.Generic;

namespace HueGates.Logic
{
    public static class Solver
    {
        public const int DefaultNodeCap = 200000;

        public static int[] Solve(Level level, int nodeCap = DefaultNodeCap)
        {
            return Solve(new GameState(level), nodeCap);
        }

        public static int[] Solve(GameState state, int nodeCap = DefaultNodeCap)
        {
            int count = state.level.arrows.Length;
            var blockers = new int[count];
            var gateBase = new int[count];
            var colors = new int[count];
            for (int i = 0; i < count; i++)
            {
                Arrow arrow = state.level.arrows[i];
                colors[i] = arrow.color;
                GateSide side;
                int index;
                Engine.ExitGate(state.level.size, arrow, out side, out index);
                gateBase[i] = state.level.gates.Of(side)[index];
                List<Cell> path = Engine.PathCells(state.level.size, arrow);
                int mask = 0;
                for (int p = 0; p < path.Count; p++)
                {
                    int occupant = IndexAt(state.level.arrows, path[p].r, path[p].c);
                    if (occupant >= 0) mask |= 1 << occupant;
                }
                blockers[i] = mask;
            }

            int full = 0;
            for (int i = 0; i < count; i++)
            {
                if (state.remaining[i]) full |= 1 << i;
            }

            int nodes = 0;
            var dead = new HashSet<int>();
            List<int> found = Search(full, blockers, gateBase, colors, state.level.colors, count, dead, ref nodes, nodeCap);
            if (found == null) return null;
            return found.ToArray();
        }

        static List<int> Search(int remaining, int[] blockers, int[] gateBase, int[] colors, int colorCount, int arrowCount, HashSet<int> dead, ref int nodes, int nodeCap)
        {
            if (remaining == 0) return new List<int>();
            if (dead.Contains(remaining)) return null;
            nodes++;
            if (nodes > nodeCap) return null;

            int cleared = arrowCount - PopCount(remaining);
            for (int i = 0; i < arrowCount; i++)
            {
                if ((remaining & (1 << i)) == 0) continue;
                if ((blockers[i] & remaining) != 0) continue;
                if ((gateBase[i] + cleared) % colorCount != colors[i]) continue;
                List<int> rest = Search(remaining & ~(1 << i), blockers, gateBase, colors, colorCount, arrowCount, dead, ref nodes, nodeCap);
                if (rest != null)
                {
                    rest.Insert(0, i);
                    return rest;
                }
            }

            dead.Add(remaining);
            return null;
        }

        static int IndexAt(Arrow[] arrows, int r, int c)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                if (arrows[i].r == r && arrows[i].c == c) return i;
            }
            return -1;
        }

        static int PopCount(int value)
        {
            int count = 0;
            uint bits = (uint)value;
            while (bits != 0)
            {
                bits &= bits - 1;
                count++;
            }
            return count;
        }
    }
}
