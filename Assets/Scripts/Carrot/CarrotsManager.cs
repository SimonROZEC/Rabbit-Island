using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CarrotsManager : MonoBehaviour
{
	[Header("Initialize")]
	public int InitialCarrotsRadius = 3;
	public int InitialCarrotsCount = 3;

	[Header("References")]
	[SerializeField] private Grid _grid;
	[SerializeField] private RabbitsManager _rabbitsManager;
	public GameObject CarrotPrefab;
	public GameObject DestroyVFXPrefab;

	[Header("Parameters")]
	public int MaxCarrotsPerRabbit = 4;

	private Dictionary<Vector2Int, CarrotController> _carrots;

	private void Start()
	{
		_carrots = new Dictionary<Vector2Int, CarrotController>();
	}

	/*
	 * Spawns a carrot on a cell position
	 * Returns the new carrot or null if there is already a carrot on the cell or the cell is invalid
	 */
	private CarrotController SpawnCarrot(Vector2Int cell)
	{
		if (!_grid.IsValidCell(cell)) return null;
		if (_carrots.ContainsKey(cell)) return null;

		// Check carrots count limit
		int maxAllowed = _rabbitsManager.RabbitCount * MaxCarrotsPerRabbit;
		if (_carrots.Count >= maxAllowed) return null;

		Vector3 position = _grid.GetRandomPosition(cell);
		GameObject obj = Instantiate(CarrotPrefab, position, Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.up));

		CarrotController carrot = obj.GetComponent<CarrotController>();
		_carrots.Add(cell, carrot);

		CarrotSpread spread = carrot.Spread;
		Grabbable grabbable = carrot.Grabbable;

		carrot.onRot += OnCarrotDestroy;
		carrot.FoodSource.onEaten += OnCarrotDestroy;
		spread.onSpread += SpreadCarrot;
		grabbable.onGrab += UprootCarrot;
		grabbable.onDrop += PlantCarrot;

		return carrot;
	}

	/*
	 * Spreads from another carrot
	 */
	private void SpreadCarrot(object source, EventArgs data)
	{
		if (source is CarrotSpread carrot)
		{
			Vector2Int sourcePosition = _grid.GetCell(carrot.transform.position);
			List<Vector2Int> adjacent = GetEmptyAdjacentCells(sourcePosition);

			if (adjacent.Count == 0) return;

			int index = Random.Range(0, adjacent.Count);
			SpawnCarrot(adjacent[index]);
		}
	}

	/*
	 * Removes a carrot from the grid
	 * The carrot is removed from the grid as soon as it's rotten, therefore, the rotten carrot cannot be eaten by rabbits
	 * However, the rotten carrots are still visually present on the ground during the rot animation (other new carrots can still appear
	 * on the same cell during this animation)
	 */
	private void OnCarrotDestroy(object source, EventArgs data)
	{
		if (source is CarrotController carrot)
		{
			RemoveCarrot(carrot);
			Instantiate(DestroyVFXPrefab, carrot.transform.position, Quaternion.identity);
		}

		if (source is FoodSource food)
		{
			RemoveCarrot(food.GetComponent<CarrotController>());
			Instantiate(DestroyVFXPrefab, food.transform.position, Quaternion.identity);
		}
	}

	/*
	 * Plants an existing carrot on the ground at a certain position
	 * 3 Cases:
	 *	- If the destination cell is empty, the carrot is planted there, returns the planted carrot.
	 *	- If the destination cell is invalid (out of bounds), the carrot is destroyed, returns null.
	 *	- If the destination cell already contains a carrot, the planted carrot merges into the other one, returns the already planted carrot.
	 *		The merge adds the food amount to the already planted carrot, the merged carrot is destroyed
	 */
	private void PlantCarrot(object source, Grabbable.DropData data)
	{
		if (source is Grabbable gb)
		{
			CarrotController carrot = gb.GetComponent<CarrotController>();
			if (!carrot) return;
			
			Instantiate(DestroyVFXPrefab, carrot.transform.position, Quaternion.identity);
			
			Vector2Int cell = data.Cell;

			if (!_grid.IsValidCell(cell))
			{
				Destroy(carrot.gameObject);
				return;
			}

			if (_carrots.ContainsKey(cell))
			{
				CarrotController placedCarrot = _carrots[cell];
				placedCarrot.Merge(carrot);
				Destroy(carrot.gameObject);
				return;
			}

			_carrots.Add(cell, carrot);
		}
	}

	/*
	 * Uproots an existing carrot of the grid
	 */
	private void UprootCarrot(object source, Grabbable.GrabData data)
	{
		if (source is Grabbable gb)
		{
			CarrotController carrot = gb.GetComponent<CarrotController>();
			if (!carrot) return;

			if (!_carrots.ContainsKey(data.Cell)) return;
			_carrots.Remove(data.Cell);
		}
	}

	/*
	 * Removes a carrot from the manager 
	 */
	private void RemoveCarrot(CarrotController carrot)
	{
		if (!carrot) return;
		Destroy(carrot.gameObject);

		Vector2Int cell = _grid.GetCell(carrot.transform.position);

		if (!_carrots.ContainsKey(cell)) return;
		_carrots.Remove(cell);
	}


	/**
	 * Removes all the carrots from the island
	 */
	public void Clear()
	{
		foreach (Vector2Int carrotCell in _carrots.Keys)
		{
			CarrotController carrot = _carrots[carrotCell];
			Destroy(carrot);
		}
		_carrots.Clear();
	}

	/**
	 * Initializes the island by instancing a certain amount of 
	 * carrots in the center of the island
	 */
	public void Init(int count, int radius)
	{
		List<Vector2Int> cells = _grid.GetCellsInRadius(radius);

		for (int i = 0; i < count && cells.Count > 0; i++)
		{
			int index = Random.Range(0, cells.Count);
			Vector2Int cell = cells[index];

			SpawnCarrot(cell);
			cells.Remove(cell);
		}
	}

	/**
	 * Returns the 4 adjacent cells of a cell that aren't already occupied
	 * by another carrot.
	 * 
	 * Used mainly for carrot spread
	 */
	private List<Vector2Int> GetEmptyAdjacentCells(Vector2Int cell)
	{
		List<Vector2Int> adjacent = new List<Vector2Int>();

		Vector2Int up = cell + Vector2Int.up;
		Vector2Int down = cell + Vector2Int.down;
		Vector2Int left = cell + Vector2Int.left;
		Vector2Int right = cell + Vector2Int.right;

		if (_grid.IsValidCell(up) && !_carrots.ContainsKey(up)) adjacent.Add(up);
		if (_grid.IsValidCell(down) && !_carrots.ContainsKey(down)) adjacent.Add(down);
		if (_grid.IsValidCell(left) && !_carrots.ContainsKey(left)) adjacent.Add(left);
		if (_grid.IsValidCell(right) && !_carrots.ContainsKey(right)) adjacent.Add(right);

		return adjacent;
	}

	/**
	 * Returns the carrot at the cell
	 * Returns null if there is no carrot at the cell
	 */
	public CarrotController GetCarrotAt(Vector2Int cell)
	{
		if (!_carrots.ContainsKey(cell)) return null;
		return _carrots[cell];
	}
}
