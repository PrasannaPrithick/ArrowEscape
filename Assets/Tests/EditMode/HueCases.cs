using System;
using System.Collections.Generic;
using System.IO;
using HueGates.Logic;

namespace HueGates.Tests
{
    public static class HueCases
    {
        public static void RunAll(string json)
        {
            ReplayEveryLevel(json);
            SolverEveryLevel(json);
            LevelsDoNotStartStuck(json);
            Level1Vector(json);
            Level4FirstMoves(json);
            UndoRestoresBoard(json);
            WrongTapsDoNotChangeBoard(json);
            BlockedBeatsDenied();
            StuckThenUndo();
            FailThenRevive();
            GalleryMapping();
        }

        public static void ReplayEveryLevel(string json)
        {
            LevelSet set = LevelCatalog.Parse(json);
            if (set.levels.Length != 25) throw new Exception("Expected 25 levels, got " + set.levels.Length);
            for (int n = 0; n < set.levels.Length; n++)
            {
                Level level = set.levels[n];
                if (level.solution == null) throw new Exception("Level " + level.id + " has no solution");
                var state = new GameState(level);
                for (int s = 0; s < level.solution.Length; s++)
                {
                    int arrow = level.solution[s];
                    Outcome outcome = Engine.ApplyTap(state, arrow);
                    if (outcome != Outcome.Cleared)
                        throw new Exception("Level " + level.id + " solution step " + s + " was " + outcome);
                }
                if (!Engine.IsWon(state)) throw new Exception("Level " + level.id + " did not win");
                if (state.hearts != 3) throw new Exception("Level " + level.id + " lost hearts on the solution");
            }
        }

        public static void SolverEveryLevel(string json)
        {
            LevelSet set = LevelCatalog.Parse(json);
            for (int n = 0; n < set.levels.Length; n++)
            {
                Level level = set.levels[n];
                int[] solution = Solver.Solve(level);
                if (solution == null) throw new Exception("Solver found nothing for level " + level.id);
                var state = new GameState(level);
                for (int s = 0; s < solution.Length; s++)
                {
                    if (Engine.ApplyTap(state, solution[s]) != Outcome.Cleared)
                        throw new Exception("Solver move failed on level " + level.id);
                }
                if (!Engine.IsWon(state)) throw new Exception("Solver did not finish level " + level.id);
            }
        }

        public static void LevelsDoNotStartStuck(string json)
        {
            LevelSet set = LevelCatalog.Parse(json);
            for (int n = 0; n < set.levels.Length; n++)
            {
                var state = new GameState(set.levels[n]);
                if (Engine.IsStuck(state)) throw new Exception("Level " + set.levels[n].id + " starts stuck");
            }
        }

        public static void Level1Vector(string json)
        {
            Level level = LevelCatalog.Parse(json).levels[0];
            if (level.id != 1) throw new Exception("First level id");
            var replay = new GameState(level);
            for (int i = 0; i < 5; i++)
            {
                if (Engine.ApplyTap(replay, i) != Outcome.Cleared)
                    throw new Exception("Level 1 clear " + i);
            }
            if (!Engine.IsWon(replay)) throw new Exception("Level 1 should be won");

            var blocked = new GameState(level);
            if (Engine.ApplyTap(blocked, 2) != Outcome.Blocked)
                throw new Exception("Level 1 arrow 2 should be blocked");
            if (blocked.hearts != 2) throw new Exception("Level 1 hearts should drop to 2");
            if (blocked.cleared != 0) throw new Exception("Blocked tap changed cleared");
        }

        public static void Level4FirstMoves(string json)
        {
            Level level = LevelCatalog.Parse(json).levels[3];
            if (level.id != 4) throw new Exception("levels[3] should be level 4");
            ExpectArrow(level, 0, 0, 4, Dir.D, 1, Outcome.Cleared);
            ExpectArrow(level, 1, 0, 3, Dir.R, 0, Outcome.Blocked);
            ExpectArrow(level, 2, 4, 1, Dir.D, 1, Outcome.Cleared);
            ExpectArrow(level, 3, 2, 1, Dir.L, 0, Outcome.Denied);
            ExpectArrow(level, 4, 2, 2, Dir.R, 0, Outcome.Cleared);
            ExpectArrow(level, 5, 3, 0, Dir.U, 1, Outcome.Denied);
            ExpectArrow(level, 6, 3, 3, Dir.L, 1, Outcome.Blocked);
            ExpectArrow(level, 7, 1, 2, Dir.U, 1, Outcome.Denied);
        }

        public static void UndoRestoresBoard(string json)
        {
            Level level = LevelCatalog.Parse(json).levels[0];
            var state = new GameState(level);
            int[] beforeGates = GateSnapshot(state);
            bool[] beforeRemaining = (bool[])state.remaining.Clone();
            Engine.ApplyTap(state, 0);
            int hearts = state.hearts;
            if (!Engine.Undo(state)) throw new Exception("Undo should succeed");
            if (state.cleared != 0) throw new Exception("Undo did not restore cleared");
            if (state.hearts != hearts) throw new Exception("Undo refunded or changed hearts");
            if (!Same(beforeRemaining, state.remaining)) throw new Exception("Undo did not restore arrows");
            if (!Same(beforeGates, GateSnapshot(state))) throw new Exception("Undo did not restore gates");
            if (Engine.Undo(state)) throw new Exception("Second undo should fail");
        }

        public static void WrongTapsDoNotChangeBoard(string json)
        {
            Level level = LevelCatalog.Parse(json).levels[3];
            AssertWrong(level, 1, Outcome.Blocked);
            AssertWrong(level, 3, Outcome.Denied);
        }

        public static void BlockedBeatsDenied()
        {
            var level = new Level();
            level.id = 0;
            level.size = 2;
            level.colors = 2;
            level.lives = 3;
            level.palette = new[] { "coral", "sky" };
            level.gates = new Gates();
            level.gates.T = new[] { 0, 0 };
            level.gates.B = new[] { 0, 0 };
            level.gates.L = new[] { 0, 0 };
            level.gates.R = new[] { 0, 0 };
            level.arrows = new[]
            {
                new Arrow { r = 0, c = 1, dir = Dir.L, color = 1 },
                new Arrow { r = 0, c = 0, dir = Dir.D, color = 0 }
            };
            var state = new GameState(level);
            if (Engine.ResolveTap(state, 0) != Outcome.Blocked)
                throw new Exception("A blocked arrow must not be reported as denied");
        }

        public static void StuckThenUndo()
        {
            var level = new Level();
            level.id = 0;
            level.size = 2;
            level.colors = 2;
            level.lives = 3;
            level.palette = new[] { "coral", "sky" };
            level.gates = new Gates();
            level.gates.T = new[] { 0, 0 };
            level.gates.B = new[] { 0, 0 };
            level.gates.L = new[] { 0, 0 };
            level.gates.R = new[] { 0, 0 };
            level.arrows = new[]
            {
                new Arrow { r = 0, c = 0, dir = Dir.R, color = 0 },
                new Arrow { r = 1, c = 1, dir = Dir.R, color = 0 }
            };
            var state = new GameState(level);
            if (Engine.IsStuck(state)) throw new Exception("Sample should start with a move");
            if (Engine.ApplyTap(state, 0) != Outcome.Cleared) throw new Exception("First arrow should clear");
            if (!Engine.IsStuck(state)) throw new Exception("Board should be stuck after the clear");
            if (!Engine.Undo(state)) throw new Exception("Undo failed");
            if (Engine.IsStuck(state)) throw new Exception("Undo should leave the board with a move");
        }

        public static void FailThenRevive()
        {
            var level = new Level();
            level.id = 0;
            level.size = 2;
            level.colors = 2;
            level.lives = 3;
            level.palette = new[] { "coral", "sky" };
            level.gates = new Gates();
            level.gates.T = new[] { 1, 0 };
            level.gates.B = new[] { 0, 0 };
            level.gates.L = new[] { 0, 0 };
            level.gates.R = new[] { 0, 0 };
            level.arrows = new[]
            {
                new Arrow { r = 0, c = 0, dir = Dir.U, color = 0 }
            };
            var state = new GameState(level);
            for (int i = 0; i < 3; i++)
            {
                if (Engine.ApplyTap(state, 0) != Outcome.Denied)
                    throw new Exception("Expected denied tap");
            }
            if (!Engine.IsFailed(state)) throw new Exception("Three wrong taps should fail");
            if (state.cleared != 0 || !state.remaining[0]) throw new Exception("Wrong taps changed the board");
            bool used = false;
            if (!Engine.TryRevive(state, ref used)) throw new Exception("Revive should succeed");
            if (state.hearts != 1 || Engine.IsFailed(state)) throw new Exception("Revive should restore one heart");
            if (Engine.TryRevive(state, ref used)) throw new Exception("Revive is once per level");
            if (Engine.ResolveTap(state, 0) != Outcome.Denied) throw new Exception("Play should continue");
        }

        public static void GalleryMapping()
        {
            if (GalleryRules.PictureNumber(1) != 1 || GalleryRules.PieceNumber(1) != 1)
                throw new Exception("Level 1 reveal");
            if (GalleryRules.PictureNumber(20) != 1 || GalleryRules.PieceNumber(20) != 20)
                throw new Exception("Level 20 reveal");
            if (GalleryRules.PictureNumber(21) != 2 || GalleryRules.PieceNumber(21) != 1)
                throw new Exception("Level 21 reveal");
            if (GalleryRules.PictureNumber(25) != 2 || GalleryRules.PieceNumber(25) != 5)
                throw new Exception("Level 25 reveal");
            if (!GalleryRules.IsPieceRevealed(1, 20, 20) || GalleryRules.IsPieceRevealed(2, 1, 20))
                throw new Exception("Picture 1 completes at level 20");
            if (!GalleryRules.IsPieceRevealed(2, 5, 25) || GalleryRules.IsPieceRevealed(2, 6, 25))
                throw new Exception("Level 25 reveals five pieces of picture 2");
            if (Stars.FromHearts(3) != 3 || Stars.FromHearts(2) != 2 || Stars.FromHearts(1) != 1)
                throw new Exception("Stars follow hearts left");
        }

        static void ExpectArrow(Level level, int index, int r, int c, Dir dir, int color, Outcome outcome)
        {
            Arrow arrow = level.arrows[index];
            if (arrow.r != r || arrow.c != c || arrow.dir != dir || arrow.color != color)
                throw new Exception("Level 4 arrow " + index + " does not match the spec");
            var state = new GameState(level);
            if (Engine.ResolveTap(state, index) != outcome)
                throw new Exception("Level 4 arrow " + index + " expected " + outcome);
        }

        static void AssertWrong(Level level, int index, Outcome expected)
        {
            var state = new GameState(level);
            bool[] remaining = (bool[])state.remaining.Clone();
            Outcome outcome = Engine.ApplyTap(state, index);
            if (outcome != expected) throw new Exception("Expected " + expected + " got " + outcome);
            if (state.cleared != 0) throw new Exception("Wrong tap changed cleared");
            if (!Same(remaining, state.remaining)) throw new Exception("Wrong tap changed arrows");
            if (state.hearts != level.lives - 1) throw new Exception("Wrong tap should cost one heart");
        }

        static int[] GateSnapshot(GameState state)
        {
            var colors = new int[state.level.arrows.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Engine.GateColor(state.level, state.level.arrows[i], state.cleared);
            return colors;
        }

        static bool Same(bool[] a, bool[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        static bool Same(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
