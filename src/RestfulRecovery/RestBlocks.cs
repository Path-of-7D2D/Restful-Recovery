using UnityEngine;

namespace RestfulRecovery
{
    internal readonly struct SeatProfile
    {
        // Offset from the block's center in the block's local (rotated) space.
        public readonly Vector3 LocalOffset;

        // Extra yaw applied on top of the block rotation so the player faces
        // the way someone sitting on this furniture family would.
        public readonly float YawOffsetDegrees;

        public SeatProfile(Vector3 localOffset, float yawOffsetDegrees)
        {
            LocalOffset = localOffset;
            YawOffsetDegrees = yawOffsetDegrees;
        }
    }

    internal static class RestBlocks
    {
        // Furniture models face opposite their shape rotation's forward, so
        // both profiles add 180 yaw to sit facing away from the backrest.
        // Y offsets approximate the seat cushion surface; the seated pose
        // keeps the hips near the entity origin.

        // Chairs: seat at the block center.
        private static readonly SeatProfile ChairProfile =
            new SeatProfile(new Vector3(0f, 0.42f, 0f), 180f);

        // Couches/sectionals: seat slightly toward the front edge of the
        // cushion (local -Z is the direction the sitter faces).
        private static readonly SeatProfile CouchProfile =
            new SeatProfile(new Vector3(0f, 0.38f, -0.15f), 180f);

        // Upright, usable seat families in V3.0. Broken, folded, and fallen
        // variants are filtered below; car seats, wheelchairs, theater seats,
        // and beds are intentionally not listed.
        private static readonly string[] ChairFamilies =
        {
            "barStool",
            "chairCamping",
            "chairFoldingMetalUnfolded",
            "chairWood01",
            "officeChair01",
            "oldChair1",
            "schoolSeat01",
            "schoolSeat02",
            "woodChair1"
        };

        private static readonly string[] CouchFamilies =
        {
            "couchModern",
            "couchUgly",
            "sectionalLeather",
            "sectionalPlaid"
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

            foreach (var family in CouchFamilies)
            {
                if (blockName.StartsWith(family))
                {
                    // sectionalLeatherChair / sectionalPlaidChair are single
                    // seats but share the couch cushion orientation.
                    profile = CouchProfile;
                    return true;
                }
            }

            foreach (var family in ChairFamilies)
            {
                if (blockName.StartsWith(family))
                {
                    profile = ChairProfile;
                    return true;
                }
            }

            return false;
        }
    }
}
