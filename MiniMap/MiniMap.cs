using UnityEngine;
using System.Collections;

public class MiniMap : MonoBehaviour {

	private Camera miniCam;
	public float miniCamSizeMin,miniCamSizeMax;
	public float sensitivity;//小地图放大缩小的速度
	public GameObject player;
	public float relativeHeight;//相对于玩家的高度

	// Use this for initialization
	void Start () 
	{
		miniCam = GetComponent<Camera>();
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	void FixedUpdate()
	{
		transform.position = new Vector3(player.transform.position.x,player.transform.position.y + relativeHeight,player.transform.position.z);
		//transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,player.transform.localEulerAngles.y,transform.localEulerAngles.z);
	}

	public void ZoomIn()
	{
		if(miniCam.orthographicSize - sensitivity >= miniCamSizeMin)
		{
			miniCam.orthographicSize -= sensitivity;		
		}
	}

	public void ZoomOut()
	{
		if(miniCam.orthographicSize + sensitivity <= miniCamSizeMax)
		{
			miniCam.orthographicSize += sensitivity;			
		}
	}
}
