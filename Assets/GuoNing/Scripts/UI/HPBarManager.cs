using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBarManager : MonoBehaviour
{

	public HPBar hpBarprefab;
    public Transform PieceTransform;

    public Canvas UICanvas;

    public GameObject hp;

    private Dictionary<int,HPBar> hpBars = new Dictionary<int, HPBar>();
	// Start is called before the first frame update
	void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
	    if (Input.GetKeyUp(KeyCode.Space))
	    {
			var hpBar = Instantiate(hpBarprefab, UICanvas.transform);
			hpBar.InitSegments(5, PieceTransform, new Vector3(0, 2, 0));
			hpBars.Add(0, hpBar);
		}

	    if (Input.GetKeyUp(KeyCode.Y))
	    {
		    PieceTransform.transform.position+=new Vector3(0,1,0);
			GetHPBar(0).SetHP(3);
		}
        

    }

	HPBar GetHPBar(int id)
	{
		if (hpBars.ContainsKey(id))
		{
			return hpBars[id];
		}
		return null;
	}
}
