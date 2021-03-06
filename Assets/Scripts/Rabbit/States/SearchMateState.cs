using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SearchMateState", menuName = "ScriptableObjects/Brain/SearchMateState")]
public class SearchMateState : MovementState
{
	public override void Begin(Brain brain)
	{
		base.Begin(brain);

		brain.TargetMate = null;
		brain.HasCheckedArea = false;

		onNewTarget += OnNewTarget;
	}

	public override void End(Brain brain)
	{
		onNewTarget -= OnNewTarget;
	}

	public override void Tick(Brain brain)
	{
		if (!brain.Movement.PositionReached()) return;

		brain.HasCheckedArea |= false; // No effect if the area was already checked
		base.Tick(brain);
	}

	public override Brain.Action TakeDecision(Brain brain)
	{
		if (brain.Hungry) // Find food is the priority
			return Brain.Action.SearchFood;
		else if (brain.TargetMate != null) // If another mate found while searching
			return Brain.Action.WaitMate;

		if (brain.Movement.PositionReached() && !brain.HasCheckedArea)
		{
			brain.HasCheckedArea = true;
			List<RabbitController> potentialPartners = brain.Eyes.GetRabbitsInSight();

			RabbitController closestValidMate = null;
			float minDistance = float.MaxValue;
			foreach (RabbitController rabbit in potentialPartners)
			{
				if (rabbit.Grabbable.Grabbed) continue;

				if (!rabbit.ReadyToMate || !rabbit.FreeToMate) continue;

				float distance = (brain.transform.position - rabbit.transform.position).sqrMagnitude;
				if (distance < minDistance && brain.Movement.CanReachPosition(rabbit.transform.position))
				{
					closestValidMate = rabbit;
					minDistance = distance;
				}
			}

			if (closestValidMate)
			{
				brain.TargetMate = closestValidMate;
				closestValidMate.Brain.TargetMate = brain.GetComponent<RabbitController>();
				return Brain.Action.JoinMate;
			}
		}
		return brain.CurrentAction;
	}

	#region [Callbacks]

	private void OnNewTarget(object sender, EventArgs data)
	{
		if (sender is Movement movement)
		{
			movement.GetComponent<Brain>().HasCheckedArea = false;
		}
	}

	#endregion
}
