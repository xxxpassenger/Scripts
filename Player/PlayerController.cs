using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]

public class PlayerController : MonoBehaviour
{
	public GameObject destinationFlag;
    private NavMeshAgent playerAgent;
	private Animator wizardAC;
	public float maxHP = 100f,currentHP = 100f;
	private float distance=0.1f;
	private bool isDead;

    void Start()
    {
		playerAgent = GetComponent<NavMeshAgent>();
		wizardAC = GetComponent<Animator>();
    }


    void Update()
    {
		wizardAC.SetFloat("Dis",playerAgent.remainingDistance -playerAgent.stoppingDistance);	
		if(currentHP<=0.0f)
		{
			isDead = true;
		}
    }

	void FixedUpdate()
	{
		if (Input.GetMouseButtonDown(1))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit))
			{ 
				if (hit.collider.tag.Equals("Ground"))
				{
					playerAgent.SetDestination(hit.point);
					ShowDestinationFlag(hit.point);
				}
			}
		}
	}

	private void ShowDestinationFlag(Vector3 Destination)
	{
		GameObject DestinationFlag = GameObject.Instantiate(destinationFlag, new Vector3(Destination.x,Destination.y+0.1f,Destination.z), Quaternion.identity) as GameObject;
		Destroy(DestinationFlag,0.32f);
	}

	void Die()
	{
		playerAgent.Stop();
		playerAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		wizardAC.SetTrigger("Die");
		//Destroy(this.gameObject,2f);
	}
}