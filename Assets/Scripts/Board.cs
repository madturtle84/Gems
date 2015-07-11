using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
/* Consider MVC: 	
 * Controller: 
	* MoveGem(); Select->Drag->destination
	* SelectMovingGem();
	* SelectDestination();
 * Model: CheckMatchingGems();
 * View: Gem.Explode(); Gem.Fall(); Gem.FollowCursor(); SwapGems();
*/
public class Board : MonoBehaviour {
	/* Public Variables */
	public GameObject gridPrefab;
	public GameObject gemPrefab;
	/* Public Properties */
	public enum BoardState { idle, processing, dragging };	
	public bool movingMode { get; set; }
	/* Private Variables */
	private BoardState currentState = BoardState.idle;
	private Vector2 boardOrigin = new Vector2(-4, -4);
	private int boardW = 8;
	private int boardH = 6;
	private int boardHTotal = 12;
	private Grid[,] grids;
	private Grid lastSelectedGrid;
	private Camera gameCamera;
	private Gem gemHolding;
	private List<GemGroup> groups = new List<GemGroup>();

	void Start() {
		grids = new Grid[boardW, boardHTotal];
		movingMode = false;
		gameCamera = Camera.main;
		InitializeBoard();
	}

	/** Basic Process:
	 *  1. Wait for player's input. *ProcessMouseInput*
	 *  2. Eliminate connecting gems based the rule of the game. *CheckMatchingGems*, *EliminateGemGroups*
	 *  3. Create new gems to fill the holes. *FillAllEmptyGrids*
	 *  4. Repeate step 2. if having connecting gems
	 *  5. Player regains control, back to step 1.
	**/
	void Update() {
		ProcessMouseInput();	
		/* Debug Message */
		if (Input.GetKeyDown(KeyCode.R)) {
			Application.LoadLevel(0);
		}
	}
	
	/* If there is a hole in the board, find the closest gem above and fall down to its position*/
	private int numFillingColumes = 0;// Number of columes that currently filling holes.
	private IEnumerator FillAllEmptyGrids() {
		currentState = BoardState.processing;
		yield return new WaitForSeconds(0.5f); // Wait for some time to see the eliminated result
		for (int x = 0; x < boardW; x++) {
			StartCoroutine(FillEmptyGridsAtColume(x));
		}
		while (numFillingColumes > 0) {
			yield return null;
		}
		/* Keep Eliminating until no matching gems can be found */
		CheckMatchingGems();
		if (groups.Count > 0) {
			StartCoroutine(EliminateGemGroups());
		}
		else {
			currentState = BoardState.idle;
		}
	}
	private IEnumerator FillEmptyGridsAtColume(int x) {
		numFillingColumes++;
		/* Iterate through the colume to fill holes */
		for (int y = 0; y < boardHTotal; y++) {
			Grid currentGrid = grids[x, y];
			if (currentGrid.gem) continue; // If not a hole, ignore this grid.

			/* Locate the closest gem above */
			int y2 = y;
			int topIdx = boardHTotal - 1;
			Grid gridAbove;// = grids[x,y2];
			do {
				if (y2 < topIdx) {
					y2++;
				}
				else {// If the top grid is empty, create a gem above the board
					CreateGemAtGrid(x, y2);
				}
				gridAbove = grids[x, y2];
			} while (gridAbove.gem == null);

			/* Move gem */
			GemFall(gridAbove, currentGrid);
			yield return new WaitForSeconds(0.1f);
		}
		numFillingColumes--;
	}
	void InitializeBoard() {
		for (int x = 0; x < boardW; x++) {
			for (int y = 0; y < boardHTotal; y++) {
				grids[x, y] = CreateGrid(x, y);
				CreateGemAtGrid(x, y);
				if (y >= boardH) grids[x, y].GetComponent<Collider2D>().enabled = false;
			}
		}
		RandomizeAllGroupsUntilNoGroup();
	}
	void InitializeBoardUsingLayout(char[,] layout) {
		for (int x = 0; x < boardW; x++) {
			for (int y = 0; y < boardH; y++) {
				grids[x, y].gem.ChangeGemType(layout[y, x]);
			}
		}
	}

	void ResetAllGems() {
		for (int x = 0; x < boardW; x++) {
			for (int y = 0; y < boardH; y++) {
				Grid currentGrid = grids[x, y];
				if(currentGrid.gem) DestroyGemAtGrid(currentGrid);
				CreateGemAtGrid(x, y);
			}
		}
		RandomizeAllGroupsUntilNoGroup();
	}

	void RandomizeAllGroupsUntilNoGroup() {
		CheckMatchingGems();
		while (groups.Count > 0) {
			for (int i = 0; i < groups.Count; i++) {
				groups[i].RandomizeGems();
			}
			CheckMatchingGems();
		}
	}

	/* "Controller" Functions */
	public void PickUpGemAt(Grid gd) {
		if (!gd) return;
		lastSelectedGrid = gd;		
		gemHolding = lastSelectedGrid.gem;
		gemHolding.PickUp();	
	}
	public void DropGemAt(Grid gd) {
		gemHolding.Drop(); // Stop following cursor.
		gemHolding.grid = gd;
		gd.gem = gemHolding;
		gemHolding.transform.position = BoardToScreenCoordinate(gd.x, gd.y); // Snap to grid position. TODO: Add animation
	}

	public void SwapGem(Grid grid0, Grid grid1) {
		Gem gem0 = grid0.gem;
		Gem gem1 = grid1.gem;
		/* Switch gem references */
		grid0.gem = gem1;
		grid1.gem = gem0;

		/* Update Position */  // TODO: Animation for swapping
		gem0.transform.position = BoardToScreenCoordinate(grid1.x, grid1.y);
		gem1.transform.position = BoardToScreenCoordinate(grid0.x, grid0.y);

	}

	void MoveGem(Grid startGrid, Grid targetGrid) {
		Gem gemToMove = startGrid.gem;
		/* Update Grid Data */
		startGrid.gem = null;
		targetGrid.gem = gemToMove;
		gemToMove.grid = targetGrid;
		/* Update Position */
		gemToMove.transform.position = BoardToScreenCoordinate(targetGrid.x, targetGrid.y);
		
	}

	void GemFall(Grid startGrid, Grid targetGrid) {
		Gem gemToMove = startGrid.gem;
		/* Update Grid Data */
		startGrid.gem = null;
		targetGrid.gem = gemToMove;
		gemToMove.grid = targetGrid;
		/* Update Position */
		gemToMove.FallDownTo(BoardToScreenCoordinate(targetGrid.x, targetGrid.y).y);		
	}

	/* Transfer the position in board coordinate (8*6) to the actual position displayed on screen */
	Vector2 BoardToScreenCoordinate(int gridX, int gridY) {
		return boardOrigin + new Vector2(gridX, gridY);
	}

	/* Create the Grid object at given position */
	Grid CreateGrid(int x, int y) {
		Vector2 screenPos = BoardToScreenCoordinate(x, y);
		GameObject go = (GameObject)Instantiate(gridPrefab, screenPos, Quaternion.identity);
		go.transform.parent = transform;
		Grid g = go.GetComponent<Grid>();
		g.SetPositionInBoardCoordinate(x, y);
		g.board = this;
		return g;
	}
	void CreateGemAtGrid(int gridX, int gridY) {
		GameObject pc = (GameObject)Instantiate(gemPrefab, BoardToScreenCoordinate(gridX, gridY), Quaternion.identity);
		pc.transform.parent = transform;
		Gem gemComponent = pc.GetComponent<Gem>();
		Grid thisGrid = grids[gridX, gridY];
		thisGrid.gem = gemComponent;
		gemComponent.grid = thisGrid;
	}

	void DestroyGemAtGrid(Grid g) {		
		if (g.gem == null) {
			print("gem is null at (" + g.x + ", " + g.y + ")");
			Debug.Break();			
		}
		Destroy(g.gem.gameObject);
		
		//g.gem.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		g.gem = null;
	}

	/* Note: Match-three Checking rule in P&D
	 * 1. Three or more horizontal or vertical gems with the same type makes a matching group.
	 * 2. Save number of combo for scoring.
	 * 3. Save number of gems erased of each matching for scoring.
	 * 4. Two adjacant group always merge into one group (therefore #combo count as one)
	 */	
	void CheckMatchingGems() {
		/* Make sure the groups is empty */
		groups.Clear();
		/* 1. Scan Vertically */
		for (int x = 0; x < boardW; x++) {
			for (int y = 0; y < boardH; y++) {						
				CompareGemWithPreviousAndUpdateBuffer(grids[x, y].gem);
			}
			SettleGemBuffer(); // Clear buffer at the end of a line.
		}
		/* 2. Scan Horizontally */
		for (int y = 0; y < boardH; y++) {
			for (int x = 0; x < boardW; x++) {
				CompareGemWithPreviousAndUpdateBuffer(grids[x, y].gem);
			}
			SettleGemBuffer();
		}
	}
	private List<Gem> gemBuffer = new List<Gem>();//A buffer that save the like gems in scanning direction.
	private List<Gem> gemsToAdd = new List<Gem>();
	private List<GemGroup> groupsToAdd = new List<GemGroup>();
	/* Evaluate Current Gem if it should add to buffer */
	/* Only two case. Either "Add" or "Reload"
	 * 1. Add: Add current gem to buffer. 
	 * 2. Reload: Clear the buffer and put current gem as initial element.
	 * (In two case, you always add current gem to buffer)
	 * 
	 * The tricky part is to determine "shared gem". We have to ensure no gem was added twice to the same group.
	 * We might need two buffer, one is to record the continuous gem on the scanning direction, and 
	 * And the other is to save the gems that we actually want to merge to the group.
	 * */
	void CompareGemWithPreviousAndUpdateBuffer(Gem currentGem){		
		if (gemBuffer.Count > 0 && gemBuffer[0].gemType != currentGem.gemType) {
			SettleGemBuffer();
		}
		/* In both cases, add current gem to the buffer. */
		gemBuffer.Add(currentGem);

		/* Check cases if sharing a gem with other groups*/
		if (currentGem.group == null) { // If current gem has no group
			gemsToAdd.Add(currentGem);
		}
		else {
			groupsToAdd.Add(currentGem.group);			
		}
	}
	void SettleGemBuffer(){
		/* If buffer currently have 3 gem, make them into a group */
		if (gemBuffer.Count >= 3) {
			/* Create a new group */
			GemGroup newGroup = new GemGroup(gemsToAdd);
			groups.Add(newGroup);
			/* If we have intersection gem, add them into new group */
			for (int i = 0; i < groupsToAdd.Count; i++) {
				newGroup.MergeWith(groupsToAdd[i]);
				groups.Remove(groupsToAdd[i]); // TODO: Remove is O(n), not good
			}
		}
		/* Reset buffer */
		gemBuffer.Clear();
		gemsToAdd.Clear();
		groupsToAdd.Clear();
	}

	IEnumerator EliminateGemGroups() {
		for (int gpIdx = 0; gpIdx < groups.Count; gpIdx++) {
			GemGroup currentGroup=groups[gpIdx];
			for (int j = 0; j < currentGroup.gems.Count; j++) {
				DestroyGemAtGrid(currentGroup.gems[j].grid);
			}
			yield return new WaitForSeconds(0.3f);
		}
		groups.Clear();
		StartCoroutine(FillAllEmptyGrids());
	}

	/// <summary>
	/// "Puzzle and Dragon"-Style Gem Swapper Control. 
	/// Player can drag a gem and constanly swap nearby gem until release.
	/// </summary>	
	private void ProcessMouseInput() {
		if (currentState == BoardState.processing) return;
		/* When clicking on a grid */
		if (Input.GetMouseButtonDown(0)) {
			PickUpGemAt(GetGridAtMousePosition());
		}
		/* If mouseover other grid while holding a gem */
		if (Input.GetMouseButton(0)) {
			if (gemHolding) {
				Grid currentGrid = GetGridAtMousePosition();
				if (currentGrid && currentGrid != lastSelectedGrid) {
					MoveGem(currentGrid, lastSelectedGrid);// Move the gem at new grid to prev grid
					lastSelectedGrid = currentGrid;
				}
			}
		}
		/* When release the gem */
		if (Input.GetMouseButtonUp(0)) {
			if (gemHolding) {
				Grid currentGrid = GetGridAtMousePosition();
				if (currentGrid) {
					DropGemAt(currentGrid);
				}
				else {
					DropGemAt(lastSelectedGrid);
				}
				/* Process Matching - Elimination */
				currentState = BoardState.processing;
				CheckMatchingGems();
				StartCoroutine(EliminateGemGroups());
			}
		}
	}

	Grid GetGridAtMousePosition() {
		Vector2 mousePos = gameCamera.ScreenToWorldPoint(Input.mousePosition);
		RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
		if (!hit) return null;
		else {
			return hit.transform.GetComponent<Grid>();
		}
	}
}
