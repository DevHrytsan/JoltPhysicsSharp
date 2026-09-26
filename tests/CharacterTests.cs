// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using System.Numerics;
using NUnit.Framework;

namespace JoltPhysicsSharp.Tests;

[TestFixture(TestOf = typeof(CharacterVirtual))]
public class CharacterTests : BaseTest
{
    private static readonly ObjectLayer s_layer = new(0);

    [Test]
    public void CharacterVirtual_Position_RoundTrips()
    {
        using PhysicsSystem system = CreateSystem(out _);
        using CharacterVirtual character = CreateCharacterVirtual(system, new Vector3(1.5f, 2.0f, -3.25f));

        Assert.That(character.Position, Is.EqualTo(new Vector3(1.5f, 2.0f, -3.25f)));

        character.Position = new Vector3(100.5f, 70.5f, 8.0f);
        Assert.That(character.Position, Is.EqualTo(new Vector3(100.5f, 70.5f, 8.0f)));
    }

    [Test]
    public void CharacterVirtual_DoublePrecisionApis_ThrowInSinglePrecision()
    {
        using PhysicsSystem system = CreateSystem(out _);
        using CharacterVirtual character = CreateCharacterVirtual(system, Vector3.Zero);

        Assert.Throws<InvalidOperationException>(() => _ = character.RPosition);
        Assert.Throws<InvalidOperationException>(() => character.RPosition = default);
        Assert.Throws<InvalidOperationException>(() => _ = character.RGroundPosition);
        Assert.Throws<InvalidOperationException>(() => _ = character.RWorldTransform);
        Assert.Throws<InvalidOperationException>(() => _ = character.RCenterOfMassTransform);
    }

    [Test]
    public void CharacterVirtual_OnFloor_ReportsContact()
    {
        using PhysicsSystem system = CreateSystem(out BodyID floor);
        using CharacterVirtual character = CreateCharacterVirtual(system, new Vector3(0.5f, 0.5f, -0.25f));

        Vector3? solvePosition = null;
        Vector3 solveNormal = default;
        character.OnContactSolve += (CharacterVirtual _, in BodyID _, SubShapeID _, in RVector3 contactPosition, in Vector3 contactNormal,
            in Vector3 _, PhysicsMaterial? _, in Vector3 _, ref Vector3 _) =>
        {
            solvePosition = (Vector3)contactPosition;
            solveNormal = contactNormal;
        };

        StepOnFloor(character, system);

        Assert.That(character.GroundState, Is.EqualTo(GroundState.OnGround));
        Assert.That(character.GroundPosition.Y, Is.EqualTo(0.0f).Within(1e-3f));

        Assert.That(character.GetNumActiveContacts(), Is.GreaterThan(0));
        CharacterContact contact = character.GetActiveContact(0);
        Assert.That(contact.BodyB, Is.EqualTo(floor));
        Assert.That(contact.CharacterB, Is.Null);
        Assert.That(contact.MotionTypeB, Is.EqualTo(MotionType.Static));
        Assert.That(contact.ContactNormal.Y, Is.EqualTo(1.0f).Within(1e-3f));
        Assert.That(contact.Position.Y, Is.EqualTo(0.0f).Within(1e-3f));
        Assert.That(contact.RPosition, Is.EqualTo(new RVector3(contact.Position)));

        Assert.That(solvePosition, Is.Not.Null);
        Assert.That(solvePosition!.Value.Y, Is.EqualTo(0.0f).Within(1e-3f));
        Assert.That(solveNormal.Y, Is.EqualTo(1.0f).Within(1e-3f));
    }

    [Test]
    public void Character_Position_RoundTrips()
    {
        using PhysicsSystem system = CreateSystem(out _);
        using Character character = CreateCharacter(system, new Vector3(0.0f, 5.0f, 0.0f));

        character.SetPosition(new Vector3(30.125f, 12.5f, -7.75f));
        Assert.That(character.GetPosition(), Is.EqualTo(new Vector3(30.125f, 12.5f, -7.75f)));

        character.RemoveFromPhysicsSystem();
    }

    [Test]
    public void Character_DoublePrecisionApis_ThrowInSinglePrecision()
    {
        using PhysicsSystem system = CreateSystem(out _);
        using Character character = CreateCharacter(system, Vector3.Zero);

        Assert.Throws<InvalidOperationException>(() => character.GetRPosition());
        Assert.Throws<InvalidOperationException>(() => character.SetRPosition(default));
        Assert.Throws<InvalidOperationException>(() => character.GetRPositionAndRotation());
        Assert.Throws<InvalidOperationException>(() => character.SetRPositionAndRotation(default, Quaternion.Identity));
        Assert.Throws<InvalidOperationException>(() => character.GetRCenterOfMassPosition());
        Assert.Throws<InvalidOperationException>(() => character.GetRWorldTransform());

        character.RemoveFromPhysicsSystem();
    }

    private static PhysicsSystem CreateSystem(out BodyID floor)
    {
        ObjectLayerPairFilterTable objectLayerPairFilter = new(1);
        objectLayerPairFilter.EnableCollision(s_layer, s_layer);

        BroadPhaseLayerInterfaceTable broadPhaseLayerInterface = new(1, 1);
        broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(s_layer, new BroadPhaseLayer(0));

        PhysicsSystem system = new(new PhysicsSystemSettings
        {
            ObjectLayerPairFilter = objectLayerPairFilter,
            BroadPhaseLayerInterface = broadPhaseLayerInterface,
            ObjectVsBroadPhaseLayerFilter = new ObjectVsBroadPhaseLayerFilterTable(broadPhaseLayerInterface, 1, objectLayerPairFilter, 1),
        });

        using BodyCreationSettings floorSettings = new(new BoxShape(new Vector3(50.0f, 1.0f, 50.0f)), new Vector3(0.0f, -1.0f, 0.0f), Quaternion.Identity, MotionType.Static, s_layer);
        floor = system.BodyInterface.CreateAndAddBody(floorSettings, Activation.DontActivate);
        system.OptimizeBroadPhase();
        return system;
    }

    private static CharacterVirtual CreateCharacterVirtual(PhysicsSystem system, in Vector3 position)
    {
        CharacterVirtualSettings settings = new() { Shape = new CapsuleShape(0.6f, 0.3f) };
        return new CharacterVirtual(settings, position, Quaternion.Identity, 0, system);
    }

    private static Character CreateCharacter(PhysicsSystem system, in Vector3 position)
    {
        CharacterSettings settings = new() { Shape = new CapsuleShape(0.6f, 0.3f), Layer = s_layer };
        Character character = new(settings, position, Quaternion.Identity, 0, system);
        character.AddToPhysicsSystem();
        return character;
    }

    private static void StepOnFloor(CharacterVirtual character, PhysicsSystem system)
    {
        for (int i = 0; i < 30; i++)
        {
            character.LinearVelocity = new Vector3(0.5f, -5.0f, 0.0f);
            character.Update(1.0f / 60.0f, s_layer, system);
        }
    }
}
