using UnityEngine;
using System.Collections;

/* Grid is a "invisible button" that players can interact with 
 * Consider it as the "Controller" in MVC
 */
public class Grid : MonoBehaviour {
	public int x { get; private set; } // The position on board coordinate
	public int y { get; private set; }
	public Board board { get; set; }
	public Gem gem { get; set; }


	/* Function called when the gem board was created. */
	public void SetPositionInBoardCoordinate(int argX, int argY) {
		x = argX;
		y = argY;
	}

}
