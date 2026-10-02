using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "SiblingRuleTile", menuName = "Tiles/Sibling Rule Tile")]
public class SiblingRuleTile : RuleTile
{
    [Tooltip("tiles to use as siblings")]
    public List<TileBase> siblingTiles = new List<TileBase>();

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        bool sameGroup = other == this || (other != null && siblingTiles.Contains(other));

        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This:
                return sameGroup;
            case TilingRuleOutput.Neighbor.NotThis:
                return !sameGroup;
        }

        return base.RuleMatch(neighbor, other);
    }
}
