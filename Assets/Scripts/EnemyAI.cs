using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using AForge.Fuzzy;
public class EnemyAI : MonoBehaviour
{
    FuzzySet fsNear, fsMed, fsFar;
    LinguisticVariable lvDistance;
    FuzzySet fsSlow, fsMedium, fsFast;
    LinguisticVariable lvSpeed;

    Database db;
    InferenceSystem infSystem;

    Transform player;
    NavMeshAgent agent;
    public Transform[] wayPoints;
    public Transform rayOrigin;
    int currentWayPointIndex = 0;
    Animator fsm; 
    Vector3[] wayPointsPos = new Vector3[3];

    float distance, speed;
    int[] distance1 = { 10, 20, 30, 40 };
    int[] speed1 = { 15, 25, 45, 60 };

    int[] distance2 = { 10, 20, 30, 40 };
    int[] speed2 = { 15, 25, 45, 60 };

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        for (int i = 0; i < wayPoints.Length; i++)
            wayPointsPos[i] = wayPoints[i].position;

        fsm = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(wayPointsPos[currentWayPointIndex]);

        StartCoroutine("CheckPlayer");
    }

    private void Initialize()
    {
        SetDistanceFuzzy();
        SetSpeedFuzzy();
        AddToDatabase();

    }

    private void SetDistanceFuzzy()
    {
        int x, y, z, w;
        if (transform.name == "Enemy")
        {
            x = distance1[0];
            y = distance1[1];
            z = distance1[2];
            w = distance1[3];
        }
        else
        {
            x = distance2[0];
            y = distance2[1];
            z = distance2[2];
            w = distance2[3];
        }

        fsNear = new FuzzySet("Near", new TrapezoidalFunction(x, y, TrapezoidalFunction.EdgeType.Right));
        fsMed = new FuzzySet("Med", new TrapezoidalFunction(x, y, z, w));
        fsFar = new FuzzySet("Far", new TrapezoidalFunction(z, w, TrapezoidalFunction.EdgeType.Left));

        lvDistance = new LinguisticVariable("Distance", 0, 60);
        lvDistance.AddLabel(fsNear);
        lvDistance.AddLabel(fsMed);
        lvDistance.AddLabel(fsFar);
    }

    private void SetSpeedFuzzy()
    {
        int x, y, z, w;
        if (transform.name == "Enemy")
        {
            x = speed1[0];
            y = speed1[1];
            z = speed1[2];
            w = speed1[3];
        }
        else
        {
            x = speed2[0];
            y = speed2[1];
            z = speed2[2];
            w = speed2[3];
        }

        fsSlow = new FuzzySet("Slow", new TrapezoidalFunction(x, y, TrapezoidalFunction.EdgeType.Right));
        fsMedium = new FuzzySet("Medium", new TrapezoidalFunction(x, y, z, w));
        fsFast = new FuzzySet("Fast", new TrapezoidalFunction(z, w, TrapezoidalFunction.EdgeType.Left));
        lvSpeed = new LinguisticVariable("Speed", 0, 60);
        lvSpeed.AddLabel(fsSlow);
        lvSpeed.AddLabel(fsMedium);
        lvSpeed.AddLabel(fsFast);

    }

    private void AddToDatabase()
    {
        db = new Database();
        db.AddVariable(lvDistance);
        db.AddVariable(lvSpeed);

        infSystem = new InferenceSystem(db, new CentroidDefuzzifier(60));
        infSystem.NewRule("Rule 1", "IF Distance IS Near THEN Speed IS Slow");
        infSystem.NewRule("Rule 2", "IF Distance IS Med THEN Speed IS Medium");
        infSystem.NewRule("Rule 3", "IF Distance IS Far THEN Speed IS Fast");
    }


    private void Update()
    {
        Evaluate();
    }

    private void Evaluate()
    {
        if (player)
        {

            Vector3 dir = (player.position - transform.position).normalized;
            distance = Vector3.Distance(transform.position, player.position);
            infSystem.SetInput("Distance", distance);
            speed = infSystem.Evaluate("Speed");
            agent.speed = speed * 0.3f;
         
        }
    }

    IEnumerator CheckPlayer()
    {
        CheckVisibility();
        CheckDistance();
        CheckDistanceFromCurrentWaypoint();
        yield return new WaitForSeconds(0.1f);
        yield return CheckPlayer();
    }

    private void CheckDistanceFromCurrentWaypoint()
    {
        float distance = Vector3.Distance(wayPointsPos[currentWayPointIndex], rayOrigin.position);
        

        fsm.SetFloat("distanceFromCurrentWaypoint", distance);
    }

    private void CheckDistance()
    {
        float distance = Vector3.Distance(player.position, rayOrigin.position);
        fsm.SetFloat("distance", distance);
    }

    private void CheckVisibility()
    {
        float maxDistance = 20;
        Vector3 direction = (player.position - rayOrigin.position).normalized;
        Debug.DrawRay(rayOrigin.position, direction * maxDistance, Color.red);
        if (Physics.Raycast(rayOrigin.position, direction, out RaycastHit info, maxDistance))
        {
            if (info.transform.tag == "Player")
                fsm.SetBool("isVisible", true);

            else
                fsm.SetBool("isVisible", false);
        }
        else
            fsm.SetBool("isVisible", false);
    }


    public void SetLookRotation()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.1f);
    }
    public void Shoot()
    {
        float shootFreq = 5;
        GetComponent<ShootBehaviour>().Shoot(shootFreq);
    }

    public void Patrol()
    { }

    public void Chase()
    {
        agent.SetDestination(player.position);
    }


    public void SetNewWayPoint()
    {
        switch (currentWayPointIndex)
        {
            case 0:
                currentWayPointIndex = 1;
                break;
            case 1:
                currentWayPointIndex = 2;
                break;
            case 2:
                currentWayPointIndex = 0;
                break;
        }
        agent.SetDestination(wayPointsPos[currentWayPointIndex]);
    }
}
