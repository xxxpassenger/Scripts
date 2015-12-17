/// <summary>
/// Class type.
/// This is a main script of hero use to control hero
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ClassType{None,Swordman,Archer,Mage}

[RequireComponent (typeof (PlayerStatus))]
[RequireComponent (typeof (PlayerSkill))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class HeroController : MonoBehaviour {

    private NavMeshAgent playerAgent;
    private Animator playerAC;
    public enum ControlAnimationState {Idle,Move,WaitAttack,Attack,Cast,ActiveSkill,TakeAtk,Death}; //Hero state
	
	public Texture2D heroImage;
	
	public ClassType classType; //Class hero
	public GameObject target;     //Target enemy
	public GameObject targetHP;  //Target show hp       敌人的血量显示
	public GameObject targetMoveTo; //Target move to (ex. npc , item)
	[SerializeField]
	public List<GameObject> modelMesh;      //Model to chage color if take attack
	public Color colorTakeDamage;		//Color take damage     承受伤害时的颜色
	
	
	public ControlAnimationState ctrlAnimState; //Control Animation State
	
	public bool autoAttack;     //是否自动攻击（否）
	
	//Get component other script
	
	private PlayerStatus playerStatus;
	private PlayerSkill playerSkill;
	
	private float delayAttack = 100;		//Delay Attack speed        延迟攻击、攻击间隔
	private Vector3 destinationPosition;		// The destination Point        鼠标点击时的目的地
	private float destinationDistance;			// The distance between this.transform and destinationPosition      目的地的距离
	private Vector3 movedir;        //移动时的朝向
	private float moveSpeed;						// The Speed the character will move        移动速度
	private Vector3 ctargetPos;					//Convert Target Position       不懂啊
	private Vector3 targetPos;					//Target Pos        目标位置
	private Quaternion targetRotation;			//Rotation]         目标旋转角度
	private bool checkCritical;					//Check Critical    检测是否暴击
	private bool onceAttack;					//Check Attack if disable AutoAttack        禁用自动攻击后按键一次攻击一次咯
	private float flinchValue;					//Check Enemy flinch        检测敌人退缩值
	private Color[] defaultColor;				//Default Material Color    默认颜色
	private bool getSkillTarget;                //Check Get Skill Target    检测是否获取到技能目标
	private bool alreadyLockSkill;				//Check lock freeskill      检测技能是否被锁定
	

	public bool useSkill;                    //Check use skill      检测使用技能

	public bool useFreeSkill;                    //Check use Free Target skill      检测使用非指向型技能

	public Vector3 freePosSkill;				//Position Skill        非指向型技能释放位置
        
	[HideInInspector]
	public float skillRange;                 //Skill Range Detect       技能范围探测
	[HideInInspector]
	public int castid;						//Cast skill id         投掷技能ID
	[HideInInspector]
	public GameObject DeadSpawnPoint;     //Spawn point when hero dead      角色死亡重生点
	[HideInInspector]
	public int typeAttack;						//Type Attack       攻击类型
	[HideInInspector]
	public int typeTakeAttack;			//Type TakeAttack       被攻击类型
	
	public bool dontMove;       //不许移动
	public bool dontClick;      //不许攻击
	
	private bool oneShotOpenDeadWindow;                 //一次性打开死亡窗口？
	
	public int layerActiveGround = 11;              //可移动地面层
	public int layerActiveItem = 10;                //可用装备层
	public int layerActiveEnemy = 9;                //可攻击敌人层
	public int layerActiveNpc = 13;                 //可交互NPC层
	
	
	//Editor Variable
	[HideInInspector]
	public int sizeMesh;                //网格大小？
	
	
	// Use this for initialization
	void Start () {

        playerAgent = GetComponent<NavMeshAgent>();
        playerAC = GetComponent<Animator>();

        layerActiveGround = 11;
		layerActiveItem = 10;
		layerActiveEnemy = 9;
		
		destinationPosition = this.transform.position;
		playerSkill = this.GetComponent<PlayerSkill>();
		playerStatus = this.GetComponent<PlayerStatus>();
		
		flinchValue = 100; //Declare flinch value (if zero it will flinch)      敌人被攻击会退缩
		delayAttack = 100; //Declare delay 100 sec
		
		defaultColor = new Color[modelMesh.Count];
		
		DeadSpawnPoint = GameObject.FindGameObjectWithTag("SpawnHero");
		
		 SetDefualtColor();	
	}
	
	// Update is called once per frame
	void Update () {
		
		TargetLock();
		HeroAnimationState();

        playerAC.SetFloat("Dis", playerAgent.remainingDistance - playerAgent.stoppingDistance);

        if (ctrlAnimState != ControlAnimationState.Death && ctrlAnimState != ControlAnimationState.Cast && ctrlAnimState != ControlAnimationState.ActiveSkill && dontMove == false)
		{
			ClickToMove();
			CancelSkill();
		}
        else if(dontMove == true)
        {
			ctrlAnimState = ControlAnimationState.Idle;
		}
		
	}
	
	void CancelSkill()          //取消技能
	{
		//Ray to enemy
		Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit h;

		if(Input.GetMouseButtonDown(1) && useSkill && ctrlAnimState != ControlAnimationState.Death)
		{
			playerSkill.oneShotResetTarget = false;
			useFreeSkill = false;
			useSkill = false;
			GameSetting.Instance.SetMouseCursor(0);
			castid = 0;
			skillRange = 0;
		}
		
		if(Physics.Raycast(r, out h ,100, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)){
			
			if(h.collider != null)
			{
				if(h.collider.tag == "Ground" && !useFreeSkill){
					
					if(Input.GetMouseButtonDown(0) && useSkill && ctrlAnimState != ControlAnimationState.Death)
					{
						playerSkill.oneShotResetTarget = false;
						useSkill = false;
						GameSetting.Instance.SetMouseCursor(0);
						castid = 0;
						skillRange = 0;
					}
				}
				
				if(h.collider.tag == "Enemy"){
					
					if(Input.GetMouseButtonDown(0) && useSkill && ctrlAnimState != ControlAnimationState.Death)
					{
						GameSetting.Instance.SetMouseCursor(0);
					}
				}	
			}
		}
		
	}

	
	//State Hero
	void HeroAnimationState() {
		
		if(ctrlAnimState == ControlAnimationState.Idle)
		{
			//animationManager.animationState = animationManager.Idle;
		}
		
		if(ctrlAnimState == ControlAnimationState.Move)
		{
			//animationManager.animationState = animationManager.Move;
		}
		
		if(ctrlAnimState == ControlAnimationState.WaitAttack)
		{
			//animationManager.animationState = animationManager.Idle;
			WaitAttack();

		}
		if(ctrlAnimState == ControlAnimationState.Attack)
		{
			if(target)
			{
				LookAtTarget(target.transform.position);
			
				if(checkCritical)
				{
					//animationManager.animationState = animationManager.CriticalAttack;
					delayAttack = 100;
					onceAttack = false;
				}else if(!checkCritical)
				{
					//animationManager.animationState = animationManager.Attack;
					delayAttack = 100;
					onceAttack = false;
				}
			}else
			{
				ctrlAnimState = ControlAnimationState.Idle;	
			}
			
			
		}
			
		
		if(ctrlAnimState == ControlAnimationState.TakeAtk)
		{
			//animationManager.animationState = animationManager.TakeAttack;

		}
		
		if(ctrlAnimState == ControlAnimationState.Cast)
		{
			playerSkill.CastSkill(playerSkill.FindSkillType(castid),playerSkill.FindSkillIndex(castid));
			
			//animationManager.animationState = animationManager.Cast;
		}
		
		if(ctrlAnimState == ControlAnimationState.ActiveSkill)
		{
			//animationManager.animationState = animationManager.ActiveSkill;
		}
		
		if(ctrlAnimState == ControlAnimationState.Death)
		{
			//animationManager.animationState = animationManager.Death;
		}
	}
	
	//Wait before attack
	void WaitAttack()       //等待攻击
	{		
		if(delayAttack > 0)
		{
			delayAttack -= Time.deltaTime * playerStatus.statusCal.atkSpd;	
		}else if(delayAttack <= 0)
		{
			checkCritical = CriticalCal(playerStatus.statusCal.criticalRate);
				
			if(checkCritical)
			{
				//typeAttack = Random.Range(0,animationManager.criticalAttack.Count);
				//animationManager.checkAttack = false;
			}else if(!checkCritical)
			{
				//typeAttack = Random.Range(0,animationManager.normalAttack.Count);
				//animationManager.checkAttack = false;
			}
			
			if(autoAttack)
			{
				ctrlAnimState = ControlAnimationState.Attack;
			}else
			{
				if(onceAttack)
				{
					ctrlAnimState = ControlAnimationState.Attack;
				}
			}
				
		}
	}
	
	void TargetLock()
	{
		//Ray to enemy
		Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit h;
		
		
		if(target == null)
		{
			if(Physics.Raycast(r, out h ,100, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)){
			
				if(h.collider != null)
				{
					if(h.collider.tag == "Enemy"){
						targetHP = h.collider.gameObject;
						
						if(!useSkill)
						GameSetting.Instance.SetMouseCursor(1);
									
					}else if(h.collider.tag == "Ground"){
						targetHP = null;
						
						if(!useSkill)
						GameSetting.Instance.SetMouseCursor(0);
				
					}else if(h.collider.tag == "Npc_Shop"){
						targetHP = null;
						
						if(!useSkill)
						GameSetting.Instance.SetMouseCursor(4);
				
					}else if(h.collider.tag == "Item"){
						targetHP = null;
						
						if(!useSkill)
						GameSetting.Instance.SetMouseCursor(5);
				
					}
				}
			}
			
		}else
		{
			
			if(Physics.Raycast(r, out h ,100, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)){
			
				if(h.collider != null)
				{
					if(h.collider.tag == "Ground"){
						if(!useSkill)
						GameSetting.Instance.SetMouseCursor(0);
					}
					
					if(h.collider.tag == "Enemy"){
						if(!useSkill)
						GameSetting.Instance.SetMouseCursor(1);
					}
				}
			}

			
			if(Input.GetMouseButtonDown(0))
			{
				if(Physics.Raycast(r, out h ,100, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)){
			
				if(h.collider != null)
				{
					if(h.collider.tag == "Enemy"){
						targetHP = h.collider.gameObject;
																
					}
						
					if(h.collider.tag == "Ground"){
						targetHP = null;	
									
					}
				}
			}
			}			
		}

		//Show enemy hp bar
		if(targetHP)
		{
			//EnemyStatus enemyStatus;
			//EnemyController enemyControl;
			//enemyStatus = targetHP.GetComponent<EnemyStatus>();
			//enemyControl = targetHP.GetComponent<EnemyController>();
			
			//EnemyHP.Instance.ShowHPbar(true);
			//EnemyHP.Instance.GetHPTarget(enemyControl.defaultHP,enemyStatus.status.hp,enemyStatus.enemyName);
			
		}else if(!targetHP)
		{
			//EnemyHP.Instance.ShowHPbar(false);
		}
	}
	
	//Movement Method
	void ClickToMove()
	{
		if(useFreeSkill && useSkill && getSkillTarget)
		{
			destinationDistance = Vector3.Distance(destinationPosition, this.transform.position); //Check Distance Player to Destination Point
				
			if(destinationDistance < skillRange){		// Reset speed to 0
					
				//Change to state Cast
					
				if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
				{
					ctrlAnimState = ControlAnimationState.Cast;     //在技能范围内直接甩技能妈的
					playerSkill.canCast = true;
					getSkillTarget = false;
					}
					
					LookAtTarget(freePosSkill);
					moveSpeed = 0;
				}
				else if(destinationDistance > skillRange ){			//Reset Speed to default
					//Change to state move
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					ctrlAnimState = ControlAnimationState.Move;
					moveSpeed = playerStatus.statusCal.movespd;
				}
		}else
		{
			if(target != null && !useSkill) //Click Enemy       点击到敌人身上
			{
				destinationDistance = Vector3.Distance(target.transform.position, this.transform.position); //Check Distance Player to Destination Point
				
				if(destinationDistance <= playerStatus.statusCal.atkRange){		// Reset speed to 0
					
					//Change to state Idle
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					ctrlAnimState = ControlAnimationState.WaitAttack;
					moveSpeed = 0;
					//敌人被选中高亮边框
					LookAtTarget(target.transform.position);
				}
				else if(destinationDistance > playerStatus.statusCal.atkRange ){			//Reset Speed to default
					//Change to state move
					LookAtTarget(target.transform.position);
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle || ctrlAnimState == ControlAnimationState.WaitAttack)
					ctrlAnimState = ControlAnimationState.Move;
					moveSpeed = playerStatus.statusCal.movespd;
				}
				
				
			}else
				
			if(target != null && useSkill) //Click Enemy
			{
				destinationDistance = Vector3.Distance(target.transform.position, this.transform.position); //Check Distance Player to Destination Point
				
				if(destinationDistance <= skillRange){		// Reset speed to 0
					
					//Change to state Cast
					
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					{
						ctrlAnimState = ControlAnimationState.Cast;
						playerSkill.canCast = true;
					}
					
					LookAtTarget(target.transform.position);
					moveSpeed = 0;
				}
				else if(destinationDistance > skillRange ){			//Reset Speed to default
					//Change to state move
					LookAtTarget(target.transform.position);
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle || ctrlAnimState == ControlAnimationState.WaitAttack)
					ctrlAnimState = ControlAnimationState.Move;
					moveSpeed = playerStatus.statusCal.movespd;
				}
				
				
			}else
				
			if(target == null && targetMoveTo != null)      //点击到NPC或者地上的物品时
			{	
				destinationDistance = Vector3.Distance(targetMoveTo.transform.position, this.transform.position); //Check Distance Player to Destination Point
				
				if(destinationDistance <= 2f){		// Reset speed to 0  鼠标点在目标周围2M范围内人物不会移动
					
					//Change to state Idle
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					ctrlAnimState = ControlAnimationState.Idle;
					moveSpeed = 0;
					
					//Talk to npc or keep item
					InteractObject();
					
				}
				else if(destinationDistance > 2f ){			//Reset Speed to default
					//Change to state move
					LookAtTarget(targetMoveTo.transform.position);
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					ctrlAnimState = ControlAnimationState.Move;
					moveSpeed = playerStatus.statusCal.movespd;
				}
			}else
			
			if(target == null && targetMoveTo == null) // Click Ground
			{
				destinationDistance = Vector3.Distance(destinationPosition, this.transform.position); //Check Distance Player to Destination Point
				
				if(destinationDistance < .5f){		// Reset speed to 0     鼠标点在角色周围0.5M范围内人物不会移动
					
					//Change to state Idle
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					ctrlAnimState = ControlAnimationState.Idle;
					moveSpeed = 0;
				}
				else if(destinationDistance > .5f ){			//Reset Speed to default
					//Change to state move
					if(ctrlAnimState == ControlAnimationState.Move || ctrlAnimState == ControlAnimationState.Idle)
					ctrlAnimState = ControlAnimationState.Move;
					moveSpeed = playerStatus.statusCal.movespd;
				}
			}
		}
		
		
		
		destinationDistance = Vector3.Distance(destinationPosition, this.transform.position);

        // Moves the Player if the Left Mouse Button was clicked        GUIUtility类貌似是旧UI的判断是否响应鼠标点击相关的类？
        if (Input.GetMouseButtonDown(0) && GUIUtility.hotControl==0 && dontClick == false) {
 			
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitdist;
		
			//if disable auto attack it can attack 1 time
			if(!autoAttack)
			{
				onceAttack = true;
			}
			
 
			if(Physics.Raycast(ray, out hitdist,100, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)) {
				
				if(hitdist.collider.tag != "Player")
				{
					Vector3 targetPoint = Vector3.zero;
					targetPoint.x = hitdist.point.x;
					targetPoint.y = transform.position.y;
					targetPoint.z = hitdist.point.z;
					destinationPosition = hitdist.point;
					targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
					
					if(alreadyLockSkill)
					{
						playerSkill.oneShotResetTarget = false;
						ResetOldCast();
						useFreeSkill = false;
						useSkill = false;
						getSkillTarget = false;
						alreadyLockSkill = false;
					}
					
					if(useFreeSkill && !alreadyLockSkill)
					{
						freePosSkill = destinationPosition;
						getSkillTarget = true;
						alreadyLockSkill = true;
					}
					
				}
			}
			
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit h;
			if(Physics.Raycast(r, out h ,100, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)){
				if(h.collider.tag == "Ground")
                {
                    playerAgent.SetDestination(h.point);

                    //Reset Lock Target
                    if (ctrlAnimState != ControlAnimationState.Attack)
					{
						target = null;
						targetMoveTo = null;
					}
					
					//Spawn Mouse Effect
					Instantiate(GameSetting.Instance.mousefxNormal,new Vector3(h.point.x,h.point.y+0.02f,h.point.z),h.collider.transform.rotation);
					
					
				}
                else if(h.collider.tag == "Enemy")
                {
					
					if(ctrlAnimState != ControlAnimationState.Attack)
					{
						target = h.collider.gameObject;
						targetMoveTo = null;
					}
					
					//Spawn Mouse Effect
					GameObject go = (GameObject)Instantiate(GameSetting.Instance.mousefxAttack,new Vector3(h.collider.transform.position.x,h.collider.transform.position.y+0.02f,h.collider.transform.position.z),Quaternion.identity);
					go.transform.parent = target.transform;
					
					
				}
                else if(h.collider.tag == "Npc" || h.collider.tag == "Item")
				{
					if(ctrlAnimState != ControlAnimationState.Attack)
					{
						target = null;
						targetMoveTo = h.collider.gameObject;
					}
					//Spawn Mouse Effect
					GameObject go = (GameObject)Instantiate(GameSetting.Instance.mousefxInteract,new Vector3(h.collider.transform.position.x,h.collider.transform.position.y+0.02f,h.collider.transform.position.z),Quaternion.identity);
					//go.transform.parent = targetMoveTo.transform;
				}
			}
	
		}
		
 
		// Moves the player if the mouse button is hold down
		else if (Input.GetMouseButton(0) && GUIUtility.hotControl==0 && dontClick == false) {
 
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitdist;
 
			if (Physics.Raycast(ray, out hitdist, 1 << layerActiveEnemy | 1 << layerActiveGround | 1 << layerActiveItem | 1 << layerActiveNpc)) {
				if(hitdist.collider.tag != "Player")
				{
				Vector3 targetPoint = Vector3.zero;//hitdist.point;
				targetPoint.x = hitdist.point.x;
				targetPoint.y = transform.position.y;
				targetPoint.z = hitdist.point.z;
				destinationPosition = hitdist.point;
				
				targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
				}
				
			}
		}
		
		//Reset State when release left-click
		if(Input.GetMouseButtonUp(0))
		{
			if(ctrlAnimState != ControlAnimationState.Attack)
			ctrlAnimState = ControlAnimationState.Idle;
			moveSpeed = 0;	
		}
		
		
		//Disable Auto Attack Command
		if(Input.GetMouseButton(0) && target && dontClick == false)
		{
			//if disable auto attack it can attack 1 time
			if(!autoAttack)
			{
				onceAttack = true;
			}
		}		
		
		if(ctrlAnimState == ControlAnimationState.Move){
			this.transform.rotation = Quaternion.Lerp(this.transform.rotation,targetRotation,Time.deltaTime *25);
			
            if(playerAgent.isOnNavMesh) //判断是否在地面上
            {
				movedir = Vector3.zero;
				movedir = transform.TransformDirection(Vector3.forward*moveSpeed);
			}
		}else
		{
			
			movedir = Vector3.Lerp(movedir,Vector3.zero,Time.deltaTime * 10);	
		
		}
		movedir.y -= 20 * Time.deltaTime;
	}	
	
	void InteractObject()
	{
		if(targetMoveTo.tag == "Npc")       //NPC交互，参考NPC相关的代码
		{
			//if(targetMoveTo.GetComponent<NpcSetup>().npcType == NpcSetup.NpcType.QuestNpc)
			//	targetMoveTo.GetComponent<NpcSetup>().SetupDialogQuest(targetMoveTo.GetComponent<NpcSetup>().questID);
				
			//targetMoveTo.GetComponent<NpcSetup>().CallDialogBox();

		}
        else if(targetMoveTo.tag == "Item")        //捡起物品
		{
			//guiMenu.PickupItem(targetMoveTo);		//参考捡起物品的代码	
		}
		ResetState();
		targetMoveTo = null;
		
	}
	
	//Look at target method
	void LookAtTarget(Vector3 _targetPos)       //朝向目标 ，主要用在释放技能的时候
	{
		targetPos.x = _targetPos.x;
		targetPos.y = this.transform.position.y;
		targetPos.z = _targetPos.z;
		this.transform.LookAt(targetPos);
	}
	
	//Critical Calculate
	bool CriticalCal(float criticalStat)        //判断是否暴击
	{
		float calCritical = criticalStat - Random.Range(0,101f);
		
		if(calCritical > 0)
		{
			return true; //Critical
		}else
		{
			return false; //Not Critical
		}
	}
	
	//ResetState Method
	public void ResetState()
	{
		moveSpeed = 0;
		movedir = Vector3.zero;	
		destinationDistance = 0;
		destinationPosition = this.transform.position;
		target= null;
		ctrlAnimState = ControlAnimationState.Idle;
		alreadyLockSkill = false;
		Invoke("resetCheckAttack",0.1f);

	}
	
	public void ResetBeforeCast()       //投掷重设
	{
		moveSpeed = 0;
		movedir = Vector3.zero;	
		destinationDistance = 0;
		destinationPosition = this.transform.position;
		target= null;
		alreadyLockSkill = false;
		Invoke("resetCheckAttack",0.1f);
	}
	
	void ResetMove()        //移动状态重设
	{
		moveSpeed = 0;
		movedir = Vector3.zero;	
		destinationDistance = 0;
		destinationPosition = this.transform.position;
	}
	
	public void ResetAttack()       //攻击状态重设
	{
		target = null;	
	}
	
	public void DeadReset()         //死亡重设
	{

        playerAgent.Stop();
        playerAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        playerAC.SetTrigger("Die");

        useSkill = false;
		GameSetting.Instance.SetMouseCursor(0);
		castid = 0;
		skillRange = 0;
		moveSpeed = 0;
		movedir = Vector3.zero;	
		destinationDistance = 0;
		destinationPosition = this.transform.position;
		target= null;
		
		if(!oneShotOpenDeadWindow)
		{
			Invoke("OpenDeadWindow",0.5f);
			oneShotOpenDeadWindow = true;
			
		}	
	}
	
	void OpenDeadWindow()       //显示死亡后的窗口
	{
		//if(!DeadWindow.enableWindow)
		//DeadWindow.enableWindow = true;
	}
	
	void resetCheckAttack()     //重设检测是否攻击
	{
		//animationManager.checkAttack = false;	
	}

    //承受伤害,重要功能！参数列表：伤害值，命中率，击退率？，攻击特效，攻击声音
    public void GetDamage(float targetAttack,float targetHit,float flinchRate,GameObject atkEffect,AudioClip atksfx)        
	{
		//Calculate Hit
		targetHit += Random.Range(-10,30);
		
		if(playerStatus.statusCal.spd - targetHit > 0) //Attack Miss
		{
			InitTextDamage(Color.white,"Miss");
			SoundManager.instance.PlayingSound("Attack_Miss");
			
		}
        else
		{
			int damage = Mathf.FloorToInt((targetAttack - playerStatus.statusCal.def) * Random.Range(0.8f,1.2f));
			
			if(damage <= 5)
			{
				damage = Random.Range(1,11); // if def < enemy attack
			}
			
			//Play SFX
			if(atksfx)
			AudioSource.PlayClipAtPoint(atksfx,transform.position);
			//Spawn Effect
			if(atkEffect)
			Instantiate(atkEffect,transform.position,Quaternion.identity);
			
			InitTextDamage(Color.red,damage.ToString());
			
			playerStatus.statusCal.hp -= damage;
			GetDamageColorReset();
			
			if(playerStatus.statusCal.hp <= 0)
			{	
				playerSkill.CastBreak();
				playerStatus.statusCal.hp = 0;
				ctrlAnimState = ControlAnimationState.Death;
			}
            else
			{
				flinchValue -= flinchRate;
				
				if(flinchValue <= 0)
				{
					if(ctrlAnimState == ControlAnimationState.Cast || ctrlAnimState == ControlAnimationState.ActiveSkill)
						playerSkill.CastBreak();
						
					ctrlAnimState = ControlAnimationState.TakeAtk;
					flinchValue = 100;
					playerSkill.oneShotResetTarget = false;
				}
				
			}
		}
		
		
	}
	
    //显示伤害值UI
	public void InitTextDamage(Color colorText,string damageGet){
		// Init text damage
		//GameObject loadPref = (GameObject)Resources.Load("TextDamage");
		//GameObject go = (GameObject)Instantiate(loadPref, transform.position  + (Vector3.up*1.0f), Quaternion.identity);
		//go.GetComponentInChildren<TextDamage>().SetDamage(damageGet, colorText);
	}
	
	void GetDamageColorReset()
	{
		int index = 0;
		while(index < modelMesh.Count){
			modelMesh[index].GetComponent<Renderer>().material.color = defaultColor[index];
			index++;
		}
		
		StartCoroutine(GetDamageColor(0.2f));
	}
	
	void SetDefualtColor()
	{
		int index = 0;
		while(index < modelMesh.Count){
			defaultColor[index] = modelMesh[index].GetComponent<Renderer>().material.color;
			index++;
		}
	}
	
	private IEnumerator GetDamageColor(float time){
		//if take damage material monster will change to setting color
		int index = 0;
		Color[] colorDef = new Color[modelMesh.Count];
		while(index < modelMesh.Count){
			colorDef[index] = modelMesh[index].GetComponent<Renderer>().material.color;
			modelMesh[index].GetComponent<Renderer>().material.color = colorTakeDamage;
			index++;
		}
		yield return new WaitForSeconds(time);
		index = 0;
		while(index < modelMesh.Count){
			modelMesh[index].GetComponent<Renderer>().material.color = colorDef[index];
			index++;
		}
		yield return 0;
		StopCoroutine("GetDamageColor");
	}
	
	//void OnControllerColliderHit(ControllerColliderHit hit)
	//{	
	//	if(hit.gameObject.tag == "Collider")
	//	{
	//		//Stop movement if collision with collider
	//		ResetMove();	
	//	}
	//}	
	
	public void GetCastID(int caseID){
		
		if(ctrlAnimState != ControlAnimationState.Cast && ctrlAnimState != ControlAnimationState.ActiveSkill && ctrlAnimState != ControlAnimationState.Death && 
			ctrlAnimState != ControlAnimationState.Attack)
		{
			castid = caseID;
			ctrlAnimState = ControlAnimationState.Cast;
		}
		
	}
	
	public void ResetOldCast()
	{
		useSkill = false;
		useFreeSkill = false;
		GameSetting.Instance.SetMouseCursor(0);
	}
	
	public void Reborn()
	{
		ctrlAnimState = ControlAnimationState.Idle;
		
		//Refil HP
		playerStatus.statusCal.hp = playerStatus.hpMax/2;
		playerStatus.statusCal.mp = playerStatus.mpMax/2;
		
		playerStatus.status.exp -= (playerStatus.status.exp / GameSetting.Instance.deadExpPenalty); 
		if(playerStatus.status.exp < 0)
		{
			playerStatus.status.exp = 0;	
		}

		playerStatus.StartRegen();
		
		transform.position = DeadSpawnPoint.transform.position;
		moveSpeed = 0;
		movedir = Vector3.zero;	
		destinationDistance = 0;
		destinationPosition = this.transform.position;
		target= null;
		alreadyLockSkill = false;
		Invoke("resetCheckAttack",0.1f);
		//animationManager.oneCheckDeadReset = false;
		oneShotOpenDeadWindow = false;
		//DeadWindow.enableWindow = false;

	}
}
