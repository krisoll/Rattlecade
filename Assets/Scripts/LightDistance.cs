using UnityEngine;
using System.Collections;

public class LightDistance : MonoBehaviour {
    public float minDistance;
    public float maxDistance;

	// Update is called once per frame
	void Update () {
        for (int i = 0; i < GameManager.gManager.players.Length; i++)
        {
            float d = Vector2.Distance(transform.position, GameManager.gManager.players[i].position);
            if (d < minDistance) GameManager.gManager.players[i].alpha = 1;
            else if (d < maxDistance)
                GameManager.gManager.players[i].alpha = Mathf.Max(GameManager.gManager.players[i].alpha,
                                            1 - ((d - minDistance) / (maxDistance - minDistance)));
        }
    }
}
