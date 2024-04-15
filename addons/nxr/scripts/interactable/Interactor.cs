using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using NXR;

namespace NXRInteractable;

[GlobalClass]
public partial class Interactor : Area3D
{

	#region Exported: 
	/// <summary>
	///  Controller used for input and updating interactor transform 
	/// </summary>
	[Export] public Controller Controller { get; private set; }

	/// <summary>
	/// Amount of smoothing used when FollowControllerTransform = true 
	/// </summary>
	[Export] public float Smoothing { get; set; } = 20;

	/// <summary>
	/// Updates this interactors transform to match the Controller transform 
	/// </summary>
	[Export] public bool FollowControllerTransform { get; set ; } = true;
	#endregion


	#region Public: 
	/// <summary>
	/// the current interactable grabbed by this interactor 
	/// </summary>
	public Interactable GrabbedInteractable { get; set; }
	#endregion


	#region Private: 
	protected Interactable _distanceInteractable;
	protected bool _distanceGrabbing = false;
	private float _distanceGrabDelta = 0.0f;
	#endregion


	public override void _Ready()
	{
		Controller.ButtonPressed += Interact;
		Controller.ButtonReleased += InteractDrop;
	}


	public override void _PhysicsProcess(double delta)
	{

		if (FollowControllerTransform && Controller != null)
		{
			GlobalTransform = GlobalTransform.InterpolateWith(Controller.GlobalTransform, Smoothing * (float)delta);
		}

		DistanceGrab(delta);
	}

	public override void _Process(double delta)
	{

		if (IsInstanceValid(GrabbedInteractable) && GrabbedInteractable.HoldMode == HoldMode.Toggle && Controller.ButtonOneShot(GrabbedInteractable.GrabAction))
		{
			Drop();
		}

		if (!_distanceGrabbing)
		{
			DistanceDrop();
		}
	}


	/// <summary>
	/// Connected to Controller.ButtonPressed Signal; Checks for input
	/// and hovered interactables - Attempt to grab if found 
	/// </summary>
	/// <param name="buttonName">b</param>
	private void Interact(String buttonName)
	{
		if (IsInstanceValid(GrabbedInteractable)) { return; }

		foreach (Interactable hovered in HoveredInteractables())
		{

			if (buttonName == hovered.GrabAction)
			{

				Interactable interactable = hovered;
				float dist = GlobalPosition.DistanceTo(interactable.GlobalPosition);

				if (dist <= interactable.GrabBreakDistance && !_distanceGrabbing)
				{
					Grab(interactable);
					return;
				}

				if (interactable.DistanceGrabEnabled && dist < interactable.DistanceGrabReach)
				{
					_distanceInteractable = interactable;
					_distanceGrabbing = true;
					return;
				}
			}
		}
	}

	/// <summary>
	/// Alternate grab mode for grabbing interactables from afar
	/// </summary>
	/// <param name="delta"></param>
	private void DistanceGrab(double delta)
	{
		if (_distanceGrabbing && IsInstanceValid(_distanceInteractable))
		{

			Transform3D xform = _distanceInteractable.GlobalTransform;
			xform.Origin = xform.Origin.Slerp(GlobalTransform.Origin, _distanceGrabDelta);

			_distanceInteractable.GlobalTransform = xform;

			float dist = GlobalPosition.DistanceTo(_distanceInteractable.GlobalPosition);

			if (dist <= _distanceInteractable.GrabBreakDistance)
			{
				Grab(_distanceInteractable);
			}

			_distanceGrabDelta += (float)delta;

		}
	}


	/// <summary>
	/// Alternate drop mode for breaking 
	/// grabs after a certain distance 
	/// </summary>
	private void DistanceDrop()
	{
		if (!IsInstanceValid(GrabbedInteractable)) return;

		float dist = 0;

		if (this == GrabbedInteractable.PrimaryInteractor)
		{
			dist = GrabbedInteractable.PrimaryGrabPoint.GlobalPosition.DistanceTo(GlobalPosition);
		}
		else
		{
			dist = GrabbedInteractable.SecondaryGrabPoint.GlobalPosition.DistanceTo(GlobalPosition);
		}

		if (dist > GrabbedInteractable.GrabBreakDistance)
		{
			Drop();
		}
	}


	/// <summary>
	///  Connected to Controller.ButtonReleased; Checks for input 
	///  and grabbed interactable - Drop if found 
	/// </summary>
	/// <param name="buttonName">xr controller button</param>
	private void InteractDrop(String buttonName)
	{
		if (!IsInstanceValid(GrabbedInteractable)) return;

		if (buttonName == GrabbedInteractable.GrabAction && GrabbedInteractable.HoldMode == HoldMode.Hold)
		{
			Drop();
		}
	}

	/// <summary>
	/// Sorts all hovered interactable based on (distance / interactable.priority) 
	/// </summary>
	/// <returns>interactable</returns>
	private List<Interactable> HoveredInteractables()
	{
		Array<Node3D> bodies = GetOverlappingBodies();
		List<Interactable> interactables = new List<Interactable>();

		foreach (Node3D node in bodies)
		{
			if (Util.NodeIs(node, typeof(Interactable)))
			{
				interactables.Add((Interactable)node);
			}
		}

		// sort closest hovered interactable 
		interactables = interactables.OrderBy(x =>
			{
				float distanceToGrab = 1f;

				if (x.GetPrimaryInteractor() == null)
				{
					distanceToGrab = x.PrimaryGrabPoint.GlobalPosition.DistanceTo(GlobalPosition);
				}
				else
				{
					distanceToGrab = x.SecondaryGrabPoint.GlobalPosition.DistanceTo(GlobalPosition);
				}

				return distanceToGrab / x.Priority;
			}).ToList();

		return interactables;
	}


	/// <summary>
	/// Sets this interactors 'GrabbedInteractable' and calls the Grab method on given interactable
	/// </summary>
	/// <param name="interactable">interactable used for grab</param>
	public void Grab(Interactable interactable)
	{
		GrabbedInteractable = interactable;
		interactable.Grab(this);
		_distanceGrabbing = false;
	}

	/// <summary>
	/// Calls the 'Drop' method on GrabbedInteractable; Sets GrabbedInteractable to 'null'
	/// </summary>
	public void Drop()
	{
		GrabbedInteractable.Drop(this);
		GrabbedInteractable = null;
		_distanceGrabDelta = 0.0f;
	}
}
