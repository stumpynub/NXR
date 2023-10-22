using Godot;
using NXR;
using NXRInteractable;
using System;

namespace NXRFirearm; 

[GlobalClass]
public partial class FirearmBulletZone : InteractableSnapZone
{
	
	[Export]
	private bool chamberFirearm = true;

	public FirearmBullet Bullet = null; 
    public override void _Ready()
    {
        base._Ready();

    }
    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void Snap(Interactable interactable)
    {
		if (Util.NodeIs((Node)GetParent(), typeof(FirearmBullet))) return; 
		
		Bullet = (FirearmBullet)interactable; 

        base.Snap(interactable);
    }

	public void Eject(Vector3 velocity, Vector3 torque) { 
		if (Bullet == null) return; 
		Unsnap(); 
		Bullet.ApplyCentralImpulse(velocity); 
		Bullet.ApplyTorqueImpulse(torque); 
	}
}
