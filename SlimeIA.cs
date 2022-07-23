using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SlimeIA : MonoBehaviour
{
    private GameManager _GameManager;
    private Animator anim;
    public ParticleSystem hitEffect;
    public int HP;
    private bool isDie;
    public enemyState state;
   
    //IA
    private NavMeshAgent agent;
    private Vector3 destination;
    private bool isPlayerVisible;
    private int idWaypoint;
    private bool isWalk;
    private bool isAlert;
    private bool isAttack;


    void Start()
    {
        _GameManager = FindObjectOfType(typeof(GameManager)) as GameManager;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        ChangeState(state);
    }
    void Update()
    {
        StateManager();

        if(agent.desiredVelocity.magnitude >= 0.1f)
        {
            isWalk = true;
        }
        else
        {
            isWalk = false;
        }
        anim.SetBool("isWalk", isWalk);
        anim.SetBool("isAlert", isAlert);
    }

    IEnumerator Died()
    {
        isDie = true;
        yield return new WaitForSeconds(3f);

        if(_GameManager.Perc(_GameManager.percDrop))
        {
            Instantiate(_GameManager.gemPrefab, transform.position, _GameManager.gemPrefab.transform.rotation);
        }

        Destroy(this.gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {

        if(_GameManager.gameState != GameState.GAMEPLAY){return;}

        if(other.gameObject.tag == "Player")
        {
            isPlayerVisible = true;
            if(state == enemyState.IDLE || state == enemyState.PATROL)
            {
              ChangeState(enemyState.ALERT);
            }
            else if (state == enemyState.FOLLOW)
            {
                StopCoroutine("FOLLOW");
                ChangeState(enemyState.FOLLOW);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            isPlayerVisible = false;
        }
    }

#region MEUS MÉTODOS
    void GetHit(int amount)
     {
        if(isDie == true) { return; }

        HP -= amount;
        if (HP > 0)
        {
            ChangeState(enemyState.FURY);
            anim.SetTrigger("GetHit");
            hitEffect.Emit(25);
        }
        else
        {
            ChangeState(enemyState.DIE);
            anim.SetTrigger("Die");
            StartCoroutine("Died");
        }
     }

    void StateManager()
    {

         if(_GameManager.gameState == GameState.DIE && (state == enemyState.FOLLOW || state == enemyState.FURY || state == enemyState.ALERT))
         {
              ChangeState(enemyState.IDLE);
         }
        
        
        switch(state)
        {
            case enemyState.ALERT:
                LookAt();
                break;
            case enemyState.FOLLOW:
                LookAt();
                destination = _GameManager.player.position;
                agent.destination = destination;

                if(agent.remainingDistance <= agent.stoppingDistance)
                {
                    Attack();
                }
                break;
            case enemyState.FURY:
                LookAt();
                destination = _GameManager.player.position;
                agent.destination = destination;

                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    Attack();
                }
                break;
            case enemyState.PATROL:
                break;
        }
    }

    void ChangeState(enemyState newState)
    {
        StopAllCoroutines();
        isAlert = false;
        state = newState;
        switch (newState)
        {
            case enemyState.IDLE:
                agent.stoppingDistance = 0;
                destination = transform.position;
                agent.destination = destination;
                StartCoroutine("IDLE");
                break;

            case enemyState.ALERT:
                agent.stoppingDistance = 0;
                destination = transform.position;
                agent.destination = destination;
                isAlert = true;
                StartCoroutine("ALERT");
                break;

            case enemyState.PATROL:
                agent.stoppingDistance = 0;
                idWaypoint = Random.Range(0, _GameManager.slimeWayPoints.Length);
                destination = _GameManager.slimeWayPoints[idWaypoint].position;
                agent.destination = destination;
                StartCoroutine("PATROL");
                break;

            case enemyState.FOLLOW:
                agent.stoppingDistance = _GameManager.slimeDistanceToAttack;
                StartCoroutine("FOLLOW");
                break;

            case enemyState.FURY:
                destination = transform.position;
                agent.stoppingDistance = _GameManager.slimeDistanceToAttack;
                agent.destination = destination;
            break;

            case enemyState.DIE:
                destination = transform.position;
                agent.destination = destination;
            break;
        }
      
    }

    IEnumerator IDLE()
    {
        yield return new WaitForSeconds(_GameManager.slimeIdleWaitTime);
        StayStill(50);
    }

    IEnumerator PATROL()
    {
        yield return new WaitUntil(() => agent.remainingDistance <= 0);
        StayStill(30);

    }

    IEnumerator ALERT()
    {
        yield return new WaitForSeconds(_GameManager.slimeAlertTime);

        if(isPlayerVisible == true)
        {
            ChangeState(enemyState.FOLLOW);
        }
        else
        {
            StayStill(10);
        }
    }
    IEnumerator FOLLOW()
    {
        yield return new WaitUntil(() => !isPlayerVisible);
        print("Perdi você!");
        yield return new WaitForSeconds(_GameManager.slimeAlertTime);
        StayStill(50);
    }
    IEnumerator ATTACK()
    {
        yield return new WaitForSeconds(_GameManager.slimeAttackDelay);
        isAttack = false;
    }
    void StayStill(int yes)
    {
        if(Rand() <= yes)
        {
            ChangeState(enemyState.IDLE);
        }
        else
        {
            ChangeState(enemyState.PATROL);
        }
    }


    int Rand()
    {
        int rand = Random.Range(0, 100);
        return rand;
    }

    void Attack()
    {
        if (isAttack == false && isPlayerVisible == true)
        {
            isAttack = true;
            anim.SetTrigger("Attack");
        }
    }

    void AttackIsDone()
    {
        StartCoroutine("ATTACK");
    }
    
    void LookAt()
    {
        
        Vector3 lookDirection = (_GameManager.player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, _GameManager.slimeLookAtSpeed * Time.deltaTime);
    }
#endregion
}
