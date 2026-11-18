using System;
using System.Collections.Generic;

namespace HueGates.Logic
{
    public enum Dir
    {
        U,
        D,
        L,
        R
    }

    public enum Outcome
    {
        Cleared,
        Blocked,
        Denied
    }

    public enum GateSide
    {
        T,
        B,
        L,
        R
    }

    public struct Cell
    {
        public int r;
        public int c;

        public Cell(int r, int c)
        {
            this.r = r;
            this.c = c;
        }
    }

    public sealed class Arrow
    {
        public int r;
        public int c;
        public Dir dir;
        public int color;
    }

    public sealed class Gates
    {
        public int[] T;
        public int[] B;
        public int[] L;
        public int[] R;

        public int[] Of(GateSide side)
        {
            switch (side)
            {
                case GateSide.T: return T;
                case GateSide.B: return B;
                case GateSide.L: return L;
                default: return R;
            }
        }
    }

    public sealed class Level
    {
        public int id;
        public int size;
        public int colors;
        public string[] palette;
        public int lives;
        public Gates gates;
        public Arrow[] arrows;
        public int[] solution;
        public string note;
    }

    public sealed class LevelSet
    {
        public int version;
        public string game;
        public Level[] levels;
    }

    public sealed class Picture
    {
        public string name;
        public int order;
        public int size;
        public string[][] pixels;
        public int[][] pieces;
    }

    /// <summary>
    /// Board state from the mechanic spec. cleared is the only phase variable.
    /// </summary>
    public sealed class GameState
    {
        public Level level;
        public bool[] remaining;
        public int cleared;
        public int hearts;
        public List<int> history = new List<int>();

        public GameState(Level level)
        {
            this.level = level;
            remaining = new bool[level.arrows.Length];
            for (int i = 0; i < remaining.Length; i++) remaining[i] = true;
            hearts = level.lives;
        }
    }

    public static class GalleryRules
    {
        public const int PiecesPerPicture = 20;

        public static int PictureNumber(int levelId)
        {
            return (levelId - 1) / PiecesPerPicture + 1;
        }

        public static int PieceNumber(int levelId)
        {
            return (levelId - 1) % PiecesPerPicture + 1;
        }

        public static int LevelForPiece(int pictureNumber, int pieceNumber)
        {
            return (pictureNumber - 1) * PiecesPerPicture + pieceNumber;
        }

        public static bool IsPieceRevealed(int pictureNumber, int pieceNumber, int highestLevelCompleted)
        {
            return highestLevelCompleted >= LevelForPiece(pictureNumber, pieceNumber);
        }
    }

    public static class Stars
    {
        public static int FromHearts(int hearts)
        {
            if (hearts < 1) return 1;
            if (hearts > 3) return 3;
            return hearts;
        }
    }
}
