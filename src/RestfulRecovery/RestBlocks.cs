using UnityEngine;

namespace RestfulRecovery
{
    internal readonly struct SeatProfile
    {
        // Where the seat surface sits within the placed model's rendered
        // bounds, as a fraction of its height. 0.5 works for chairs and
        // couches whose backrest roughly doubles the seat height; backless
        // stools sit near the top of their bounds.
        public readonly float SeatFraction;

        // Fallback seat-anchor height relative to the block base, used when
        // the placed model's bounds cannot be measured.
        public readonly float FallbackHeight;

        // How far the seat anchor is shifted along the direction the sitter
        // faces (couches seat slightly toward the cushion's front edge).
        public readonly float ForwardOffset;

        // Yaw relative to the block's shape rotation. Furniture families are
        // authored with different forward conventions (mirrored by their
        // Place=TowardsPlacer/TowardsPlacerInverted block property), so each
        // family carries its own correction.
        public readonly float YawOffsetDegrees;

        public SeatProfile(float seatFraction, float fallbackHeight, float forwardOffset, float yawOffsetDegrees)
        {
            SeatFraction = seatFraction;
            FallbackHeight = fallbackHeight;
            ForwardOffset = forwardOffset;
            YawOffsetDegrees = yawOffsetDegrees;
        }
    }

    internal static class RestBlocks
    {
        private const float ChairSeatHeight = 0.25f;
        private const float CouchSeatHeight = 0.5f;
        private const float UglyArmchairSeatHeight = 0.2f;
        private const float StoolTopHeight = 0.67f;
        private const float ChairFallback = -0.1f;
        private const float CouchFallback = -0.15f;
        private const float CouchForward = 0.15f;

        private readonly struct SeatFamily
        {
            public readonly string Prefix;
            public readonly SeatProfile Profile;

            public SeatFamily(string prefix, SeatProfile profile)
            {
                Prefix = prefix;
                Profile = profile;
            }
        }

        // Upright, usable seat families in V3.0. Broken, folded, and fallen
        // variants are filtered below; car seats, theater seats, and beds
        // are intentionally not listed.
        //
        // Yaw calibration: Place=TowardsPlacer families face away from the
        // sitter (need 180), Place=TowardsPlacerInverted families face with
        // the sitter (need 0). oldChair1 (180) and chairCamping (0) are
        // confirmed in game; officeChair01, schoolSeat, and the sectionals
        // have no Place property and are best guesses.
        private static readonly SeatFamily[] Families =
        {
            new SeatFamily("barStool", new SeatProfile(StoolTopHeight, ChairFallback, 0f, 0f)),
            new SeatFamily("chairCamping", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 0f)),
            new SeatFamily("chairFoldingMetalUnfolded", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 180f)),
            new SeatFamily("chairWood01", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 180f)),
            new SeatFamily("officeChair01", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 0f)),
            new SeatFamily("oldChair1", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 180f)),
            new SeatFamily("schoolSeat01", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 180f)),
            new SeatFamily("schoolSeat02", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 180f)),
            new SeatFamily("wheelchair", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 0f)),
            new SeatFamily("woodChair1", new SeatProfile(ChairSeatHeight, ChairFallback, 0f, 0f)),
            new SeatFamily("couchModern", new SeatProfile(CouchSeatHeight, CouchFallback, CouchForward, 0f)),
            // Must precede the generic couchUgly prefix: the single-seat
            // ugly armchair model needs a much lower seat point than the
            // multi-seat ugly couches.
            new SeatFamily("couchUglySingle", new SeatProfile(UglyArmchairSeatHeight, CouchFallback, CouchForward, 0f)),
            new SeatFamily("couchUgly", new SeatProfile(CouchSeatHeight, CouchFallback, CouchForward, 0f)),
            new SeatFamily("sectionalLeather", new SeatProfile(CouchSeatHeight, CouchFallback, CouchForward, 0f)),
            new SeatFamily("sectionalPlaid", new SeatProfile(CouchSeatHeight, CouchFallback, CouchForward, 0f))
        };

        private static readonly string[] ExcludedFragments =
        {
            "Broken",
            "Folded",
            "VariantHelper",
            "RandomHelper"
        };

        public static bool IsRestable(BlockValue blockValue)
        {
            return TryGetSeatProfile(blockValue, out _);
        }

        public static bool TryGetSeatProfile(BlockValue blockValue, out SeatProfile profile)
        {
            profile = default;
            var blockName = blockValue.Block?.GetBlockName();
            if (string.IsNullOrEmpty(blockName))
            {
                return false;
            }

            foreach (var fragment in ExcludedFragments)
            {
                if (blockName.Contains(fragment))
                {
                    return false;
                }
            }

            foreach (var family in Families)
            {
                if (blockName.StartsWith(family.Prefix))
                {
                    profile = family.Profile;
                    return true;
                }
            }

            return false;
        }
    }
}
