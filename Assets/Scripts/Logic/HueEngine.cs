using System;
using System.Collections.Generic;

namespace HueGates.Logic
{
    public static class Engine
    {
        public static void Step(Dir dir, out int dr, out int dc)
        {
            switch (dir)
            {
                case Dir.U: dr = -1; dc = 0; return;
                case Dir.D: dr = 1; dc = 0; return;
                case Dir.L: dr = 0; dc = -1; return;
                default: dr = 0; dc = 1; return;
            }
        }

        public static void ExitGate(int size, Arrow arrow, out GateSide side, out int index)
        {
            if (arrow.dir == Dir.U) { side = GateSide.T; index = arrow.c; return; }
            if (arrow.dir == Dir.D) { side = GateSide.B; index = arrow.c; return; }
            if (arrow.dir == Dir.L) { side = GateSide.L; index = arrow.r; return; }
            side = GateSide.R;
            index = arrow.r;
        }

        public static List<Cell> PathCells(int size, Arrow arrow)
        {
            int dr, dc;
            Step(arrow.dir, out dr, out dc);
            var cells = new List<Cell>();
            int r = arrow.r + dr;
            int c = arrow.c + dc;
            while (r >= 0 && r < size && c >= 0 && c < size)
            {
                cells.Add(new Cell(r, c));
                r += dr;
                c += dc;
            }
            return cells;
        }

        public static int GateColor(Level level, Arrow arrow, int cleared)
        {
            GateSide side;
            int index;
            ExitGate(level.size, arrow, out side, out index);
            return (level.gates.Of(side)[index] + cleared) % level.colors;
        }

        public static int ArrowAt(GameState state, int r, int c)
        {
            Arrow[] arrows = state.level.arrows;
            for (int i = 0; i < arrows.Length; i++)
            {
                if (!state.remaining[i]) continue;
                if (arrows[i].r == r && arrows[i].c == c) return i;
            }
            return -1;
        }

        public static int NearestBlocker(GameState state, int arrowIndex)
        {
            Arrow arrow = state.level.arrows[arrowIndex];
            List<Cell> path = PathCells(state.level.size, arrow);
            for (int p = 0; p < path.Count; p++)
            {
                int found = ArrowAt(state, path[p].r, path[p].c);
                if (found >= 0) return found;
            }
            return -1;
        }

        public static Outcome ResolveTap(GameState state, int arrowIndex)
        {
            Arrow arrow = state.level.arrows[arrowIndex];
            List<Cell> path = PathCells(state.level.size, arrow);
            for (int p = 0; p < path.Count; p++)
            {
                if (ArrowAt(state, path[p].r, path[p].c) >= 0) return Outcome.Blocked;
            }
            if (GateColor(state.level, arrow, state.cleared) != arrow.color) return Outcome.Denied;
            return Outcome.Cleared;
        }

        public static Outcome ApplyTap(GameState state, int arrowIndex)
        {
            if (arrowIndex < 0 || arrowIndex >= state.remaining.Length || !state.remaining[arrowIndex])
                throw new InvalidOperationException("Arrow is not on the board.");

            Outcome outcome = ResolveTap(state, arrowIndex);
            if (outcome == Outcome.Cleared)
            {
                state.remaining[arrowIndex] = false;
                state.cleared++;
                state.history.Add(arrowIndex);
            }
            else
            {
                state.hearts--;
            }
            return outcome;
        }

        public static bool Undo(GameState state)
        {
            if (state.history.Count == 0) return false;
            int last = state.history[state.history.Count - 1];
            state.history.RemoveAt(state.history.Count - 1);
            state.remaining[last] = true;
            state.cleared--;
            return true;
        }

        public static bool TryRevive(GameState state, ref bool reviveUsed)
        {
            if (reviveUsed || state.hearts > 0) return false;
            state.hearts += 1;
            reviveUsed = true;
            return true;
        }

        public static List<int> ValidMoves(GameState state)
        {
            var moves = new List<int>();
            for (int i = 0; i < state.level.arrows.Length; i++)
            {
                if (state.remaining[i] && ResolveTap(state, i) == Outcome.Cleared) moves.Add(i);
            }
            return moves;
        }

        public static bool IsWon(GameState state)
        {
            for (int i = 0; i < state.remaining.Length; i++)
            {
                if (state.remaining[i]) return false;
            }
            return true;
        }

        public static bool IsStuck(GameState state)
        {
            return !IsWon(state) && ValidMoves(state).Count == 0;
        }

        public static bool IsFailed(GameState state)
        {
            return state.hearts <= 0;
        }

        public static bool GateMatches(GameState state, int arrowIndex)
        {
            Arrow arrow = state.level.arrows[arrowIndex];
            return GateColor(state.level, arrow, state.cleared) == arrow.color;
        }
    }
}
