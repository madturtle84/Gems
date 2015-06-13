using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GemGroup {
	public GemType gemType { get; private set; }
	public List<Gem> gems = new List<Gem>();
	public GemGroup() {

	}
	public GemGroup(List<Gem> initialGems) {
		AddGems(initialGems);
	}

	public void AddGem(Gem newGem) {
		/* If it's the first element, set the type of the group */
		if (gems.Count == 0) {
			this.gemType = newGem.gemType;
		}
		gems.Add(newGem);
		newGem.group = this;
	}
	public void AddGems(List<Gem> newGems) {
		for (int i = 0; i < newGems.Count; i++) {
			AddGem(newGems[i]);
		}
	}

	/* Note: The caller object get new gems, other group remain unchanged */
	public void MergeWith(GemGroup otherGroup) {
		AddGems(otherGroup.gems);
	}

	public void RandomizeGems() {
		for (int i = 0; i < gems.Count; i++) {
			gems[i].ChangeToRandomType();
			gems[i].group = null;
		}
	}
}