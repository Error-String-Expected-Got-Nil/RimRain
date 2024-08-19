﻿using RimWorld;
using UnityEngine;
using Verse;

namespace RainRim.LizardTongueGrapple;

public class TongueGrappleProjectile : Projectile
{
    // Changes the position function from a simple lerp to the inverse parabola stretched so that its peak is
    // reached at x = 1.0. y = 0 at x = 0; y = 0.75 at x = 0.5; y = 1 at x = 1
    public override Vector3 ExactPosition => 
        origin.Yto0() 
        + (destination - origin).Yto0() * GenMath.InverseParabola(DistanceCoveredFraction * 0.5f)
        + Vector3.up * def.Altitude;
         
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        // TODO: Combat log entry?
            
        // Note: base.Impact() is what destroys this projectile. Skip if necessary?
        base.Impact(hitThing, blockedByShield);

        if (!(hitThing is Pawn target) || !(launcher is Pawn lizard)) return;
        if (!CanGrappleTarget(lizard, target)) return;

        var map = target.Map;
        var targetWasSelected = Find.Selector.IsSelected(target);
        var destinationPos = GetDestinationPosition(lizard.Position, target.Position, map);

        var pawnFlyer = PawnFlyer.MakeFlyer(RW_Common.RW_ThingDefOf.RW_LizardTongueGrabFlyer, target,
            destinationPos, null, null);

        if (pawnFlyer == null) return;

        GenSpawn.Spawn(pawnFlyer, destinationPos, map);
        if (targetWasSelected) Find.Selector.Select(target, false, false);
    }

    private static bool CanGrappleTarget(Pawn lizard, Pawn target)
    {
        return lizard.BodySize >= target.BodySize;
    }
        
    // Gets the space adjacent to the origin that's between the origin and the target, or the origin if that space
    // cannot be stood on
    private static IntVec3 GetDestinationPosition(IntVec3 origin, IntVec3 target, Map map)
    {
        var relativePosition = (target - origin).ToVector3();
        relativePosition.Normalize();
        var roundedRelativePosition = new IntVec3((int)Mathf.Round(relativePosition.x), 0, 
            (int)Mathf.Round(relativePosition.z));
        var candidatePosition = origin + roundedRelativePosition;
            
        return IsValidPositionTarget(map, candidatePosition) ? candidatePosition : origin;
    }

    private static bool IsValidPositionTarget(Map map, IntVec3 cell)
    {
        if (!cell.IsValid || !cell.InBounds(map) || cell.Impassable(map) || !cell.Walkable(map))
            return false;
        var edifice = cell.GetEdifice(map);
        return edifice is not Building_Door buildingDoor || buildingDoor.Open;
    }
}