using UnityEngine;

namespace RestfulRecovery
{
    internal readonly struct SeatProfile
    {
        // Height of the seat anchor relative to the block base. The seated
        // pose renders the hips roughly 0.4m above the entity origin, so
        // these sit slightly below the base to land the hips on the cushion.
        public readonly float Height;

        // How far the seat anchor is shifted along the direction the sitter
        // faces (couches seat slightly toward the cushion's front edge).
        public readonly float ForwardOffset;

        // Yaw relative to the block's shape rotation. Furniture families are
        // authored with different forward conventions (mirrored by their
        // Place=TowardsPlacer/TowardsPlacerInverted block property), so each
        // family carries its own correction.
        public readonly float YawOffsetDegrees;

        public SeatProfile(float height, float forwardOffset, float yawOffsetDegrees)
        {
            Height = height;
            ForwardOffset = forwardOffset;
            YawOffsetDegrees = yawOffsetDegrees;
        }
    }

    internal static class RestBlocks
    {
        private const float ChairHeight = -0.1f;
        private const float CouchHeight = -0.15f;
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
        // variants are filtered below; car seats, wheelchairs, theater seats,
        // and beds are intentionally not listed.
        //
        // Yaw calibration: Place=TowardsPlacer families face away from the
        // sitter (need 180), Place=TowardsPlacerInverted families face with
        // the sitter (need 0). oldChair1 (180) and chairCamping (0) are
        // confirmed in game; officeChair01, schoolSeat, and the sectionals
        // have no Place property and are best guesses.
        private static readonly SeatFamily[] Families =
        {
            new SeatFamily("barStool", new SeatProfile(ChairHeight, 0f, 0f)),
            new SeatFamily("chairCamping", new SeatProfile(ChairHeight, 0f, 0f)),
            new SeatFamily("chairFoldingMetalUnfolded", new SeatProfile(ChairHeight, 0f, 180f)),
            new SeatFamily("chairWood01", new SeatProfile(ChairHeight, 0f, 180f)),
            new SeatFamily("officeChair01", new SeatProfile(ChairHeight, 0f, 0f)),
            new SeatFamily("oldChair1", new SeatProfile(ChairHeight, 0f, 180f)),
            new SeatFamily("schoolSeat01", new SeatProfile(ChairHeight, 0f, 180f)),
            new SeatFamily("schoolSeat02", new SeatProfile(ChairHeight, 0f, 180f)),
            new SeatFamily("woodChair1", new SeatProfile(ChairHeight, 0f, 0f)),
            new SeatFamily("couchModern", new SeatProfile(CouchHeight, CouchForward, 0f)),
            new SeatFamily("couchUgly", new SeatProfile(CouchHeight, CouchForward, 0f)),
            new SeatFamily("sectionalLeather", new SeatProfile(CouchHeight, CouchForward, 0f)),
            new SeatFamily("sectionalPlaid", new SeatProfile(CouchHeight, CouchForward, 0f))
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
