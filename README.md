TDCharacterController2D
===========

A top down character controller for interacting with Unity's 2D colliders, without the need for a non-kinematic Rigidbody. Built to replicate the default [CharacterController](http://docs.unity3d.com/ScriptReference/CharacterController.html)'s [Move()](http://docs.unity3d.com/ScriptReference/CharacterController.Move.html) function.

Usage
-----

Add TDCharacterController2D.cs to a GameObject. A [Rigidbody2D](http://docs.unity3d.com/ScriptReference/Rigidbody2D.html) and [CircleCollider2D](http://docs.unity3d.com/ScriptReference/CircleCollider2D.html) will added automatically, the Rigidbody2D will be set to [isKinematic](http://docs.unity3d.com/ScriptReference/Rigidbody2D-isKinematic.html) at run time, but feel free to set it in the Inspector yourself. Set collisionLayers in the Inspector. Move with

```
TDCharacterController2D.Move(Vector2 motion);
```

And it'll take care of the rest.

Team
-----

**Created by:** Sebastiao Almeida - [sebastiao.strd@gmail.com](mailto:sebastiao.strd@gmail.com)

**Small additions:**
* Adam Scoble - [adam.scoble@gmail.com](mailto:adam.scoble@gmail.com)

License
-----
You are free to use the TDCharacterController2D in both commercial and non-commercial projects. You cannot sell the TDCharacterController2D as a standalone asset, or bundle of assets. But if it is utilised as part of a larger package, that has its own functionality, then you may sell that.