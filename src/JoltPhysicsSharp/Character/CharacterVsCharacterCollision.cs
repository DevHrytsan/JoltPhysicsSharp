// Copyright (c) Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using System.Numerics;
using System.Runtime.InteropServices;
using static JoltPhysicsSharp.JoltApi;

namespace JoltPhysicsSharp;

public abstract class CharacterVsCharacterCollision : NativeObject
{
    protected CharacterVsCharacterCollision()
    {
    }

    protected CharacterVsCharacterCollision(nint handle)
        : base(handle)
    {
    }

    protected override void DisposeNative()
    {
        JPH_CharacterVsCharacterCollision_Destroy(Handle);
    }
}

public abstract class CharacterVsCharacterCollisionListener : CharacterVsCharacterCollision
{
    private static readonly JPH_CharacterVsCharacterCollision_Procs _procs;
    private readonly nint _listenerUserData;

    static unsafe CharacterVsCharacterCollisionListener()
    {
        _procs = new()
        {
            CollideCharacter = &OnCollideCharacterCallback,
            CastCharacter = &OnCastCharacterCallback,
        };
        JPH_CharacterVsCharacterCollision_SetProcs(in _procs);
    }

    public CharacterVsCharacterCollisionListener()
    {
        _listenerUserData = DelegateProxies.CreateUserData(this, true);
        Handle = JPH_CharacterVsCharacterCollision_Create(_listenerUserData);
    }

    protected override void DisposeNative()
    {
        DelegateProxies.GetUserData<CharacterVsCharacterCollisionListener>(_listenerUserData, out GCHandle gch);
        base.DisposeNative();
        gch.Free();
    }

    protected virtual void CollideCharacter(
        CharacterVirtual character,
        in Matrix4x4 centerOfMassTransform,
        in CollideShapeSettings collideShapeSettings,
        in Vector3 baseOffset/*, CollideShapeCollector &ioCollector*/)
    {
    }

    protected virtual void CollideCharacter(
        CharacterVirtual character,
        in RMatrix4x4 centerOfMassTransform,
        in CollideShapeSettings collideShapeSettings,
        in RVector3 baseOffset/*, CollideShapeCollector &ioCollector*/)
    {
    }

    protected virtual void CastCharacter(
        CharacterVirtual character,
        in Matrix4x4 centerOfMassTransform,
        in Vector3 direction,
        in ShapeCastSettings collideShapeSettings,
        in Vector3 baseOffset/*, CastShapeCollector  &ioCollector*/)
    {
    }

    protected virtual void CastCharacter(
        CharacterVirtual character,
        in RMatrix4x4 centerOfMassTransform,
        in Vector3 direction,
        in ShapeCastSettings collideShapeSettings,
        in RVector3 baseOffset/*, CastShapeCollector  &ioCollector*/)
    {
    }

    #region CharacterContactListener
    [UnmanagedCallersOnly]
    private static unsafe void OnCollideCharacterCallback(nint context,
        nint character,
        void* centerOfMassTransform, // JPH_RMat4
        JPH_CollideShapeSettings* collideShapeSettings,
        void* baseOffset) // JPH_RVec3
    {
        CharacterVsCharacterCollisionListener listener = DelegateProxies.GetUserData<CharacterVsCharacterCollisionListener>(context, out _);

        if (DoublePrecision)
        {
            listener.CollideCharacter(
                CharacterVirtual.GetObject(character)!,
                *(RMatrix4x4*)centerOfMassTransform,
                CollideShapeSettings.FromNative(*collideShapeSettings),
                *(RVector3*)baseOffset
                );
        }
        else
        {
            listener.CollideCharacter(
                CharacterVirtual.GetObject(character)!,
                ((Mat4*)centerOfMassTransform)->FromJolt(),
                CollideShapeSettings.FromNative(*collideShapeSettings),
                *(Vector3*)baseOffset
                );
        }
    }

    [UnmanagedCallersOnly]
    private static unsafe void OnCastCharacterCallback(nint context,
        nint character,
        void* centerOfMassTransform, // JPH_RMat4
        Vector3* direction,
        JPH_ShapeCastSettings* collideShapeSettings,
        void* baseOffset) // JPH_RVec3
    {
        CharacterVsCharacterCollisionListener listener = DelegateProxies.GetUserData<CharacterVsCharacterCollisionListener>(context, out _);

        if (DoublePrecision)
        {
            listener.CastCharacter(
                CharacterVirtual.GetObject(character)!,
                *(RMatrix4x4*)centerOfMassTransform,
                *direction,
                ShapeCastSettings.FromNative(*collideShapeSettings),
                *(RVector3*)baseOffset
                );
        }
        else
        {
            listener.CastCharacter(
                CharacterVirtual.GetObject(character)!,
                ((Mat4*)centerOfMassTransform)->FromJolt(),
                *direction,
                ShapeCastSettings.FromNative(*collideShapeSettings),
                *(Vector3*)baseOffset
                );
        }
    }
    #endregion
}

public sealed class CharacterVsCharacterCollisionSimple : CharacterVsCharacterCollision
{
    public CharacterVsCharacterCollisionSimple()
        : base(JPH_CharacterVsCharacterCollision_CreateSimple())
    {

    }

    public void Add(CharacterVirtual character)
    {
        JPH_CharacterVsCharacterCollisionSimple_AddCharacter(Handle, character.Handle);
    }

    public void Remove(CharacterVirtual character)
    {
        JPH_CharacterVsCharacterCollisionSimple_RemoveCharacter(Handle, character.Handle);
    }
}
