using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace HueGates.Tests
{
    public class HueEngineTests
    {
        string json;

        [SetUp]
        public void Load()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../data/levels_1_25.json"));
            json = File.ReadAllText(path);
        }

        [Test] public void ReplayEveryLevel() { HueCases.ReplayEveryLevel(json); }
        [Test] public void SolverEveryLevel() { HueCases.SolverEveryLevel(json); }
        [Test] public void LevelsDoNotStartStuck() { HueCases.LevelsDoNotStartStuck(json); }
        [Test] public void Level1Vector() { HueCases.Level1Vector(json); }
        [Test] public void Level4FirstMoves() { HueCases.Level4FirstMoves(json); }
        [Test] public void UndoRestoresBoard() { HueCases.UndoRestoresBoard(json); }
        [Test] public void WrongTapsDoNotChangeBoard() { HueCases.WrongTapsDoNotChangeBoard(json); }
        [Test] public void BlockedBeatsDenied() { HueCases.BlockedBeatsDenied(); }
        [Test] public void StuckThenUndo() { HueCases.StuckThenUndo(); }
        [Test] public void FailThenRevive() { HueCases.FailThenRevive(); }
        [Test] public void GalleryMapping() { HueCases.GalleryMapping(); }
    }
}
