using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.Data;

namespace Game.Tests
{
    /// <summary>
    /// Guards against a real, found bug (M12): the impactFrames on the 3 real FMV clips
    /// (Clips_MeleeBasic/RangedBasic/SupportBasic, generated in M1/M2) were corrupted on
    /// disk -- values in the millions on clips a few hundred frames long at most (e.g.
    /// `impactFrames: 12000000` where the manifest's own source data says 18), serialized
    /// as a bare YAML scalar instead of the normal block list every other List&lt;int&gt;
    /// in this project uses. Root cause not fully confirmed (a JsonUtility array-parsing
    /// issue is suspected but unproven -- see PROJECT-README's Known gaps); hand-fixed to
    /// the manifest's real values as a direct fix. This test is the thing that would have
    /// caught it, and catches any regression -- including one from a future `Build Assets
    /// From Manifest` re-run, if the underlying parsing bug turns out to still be live.
    /// </summary>
    public class ClipMetadataTests
    {
        static readonly string[] ClipSetNames = { "MeleeBasic", "RangedBasic", "SupportBasic" };

        [Test]
        public void ImpactFrames_AreSaneForTheClipsLength()
        {
            foreach (var name in ClipSetNames)
            {
                var clipSet = Resources.Load<ClipSet>($"Battle/Clips/Clips_{name}");
                Assert.IsNotNull(clipSet, $"Clips_{name} missing from Resources/Battle/Clips.");

                var entry = clipSet.Get("basicAttack");
                Assert.IsNotNull(entry, $"Clips_{name} has no 'basicAttack' entry.");
                Assert.Greater(entry.frameRate, 0, $"Clips_{name}'s frameRate is {entry.frameRate}.");
                Assert.IsNotEmpty(entry.impactFrames, $"Clips_{name} has no impactFrames at all.");

                // The real clips (M1/M2) are all a few seconds long -- generous but not
                // meaningless upper bound. A frame index in the millions is the exact
                // shape of the bug this test exists to catch, not a plausible real value.
                const int SaneMaxFrame = 10_000;
                foreach (var frame in entry.impactFrames)
                {
                    Assert.GreaterOrEqual(frame, 0, $"Clips_{name} has a negative impact frame ({frame}).");
                    Assert.Less(frame, SaneMaxFrame,
                        $"Clips_{name}'s impactFrames contains {frame}, which is not a plausible frame index "
                        + "for a clip this short -- this is the exact corruption pattern found in M12 "
                        + "(a value in the millions where a small frame index was meant).");
                }

                Assert.AreEqual(entry.impactFrames.Distinct().Count(), entry.impactFrames.Count,
                    $"Clips_{name} lists the same impact frame more than once.");
            }
        }
    }
}
