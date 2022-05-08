using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugWeaponTester : MonoBehaviour
{
    void Start()
    {
        for(int i = 0; i < tests; i++)
        {
            Test();
        }
    }

    // Start is called before the first frame update
    [SerializeField] int attackDice = 10;
    [SerializeField] int defenseDiceHigh = 6;
    [SerializeField] int defenseDiceLight = 14;
    [SerializeField] int armorlight = 0;
    [SerializeField] int armorHeavh = 3;
    [SerializeField] int healthlight = 10;
    [SerializeField] int healthHeavy = 14;   
    [SerializeField] int tests; 

    public void Test()
    {
        int weaponDice = Random.Range(1,10);
        int weaponDamage = Random.Range(1,10);
        int weaponPen = Random.Range(-5,5); 

        int minHitsTK = 0;
        int maxHitsTK  = 100;

        int lightKills = 0;
        int heavyKills = 0;
        int damageLight = 0;
        int damageHeavy = 0;
        int lightHits = 0;
        int heavyHits = 0;
        int currentHTK = 0;

        // run 100 attacks
        for(int i = 0; i < 500; i++)
        {
            int hits = 0;
            int lightdefense = 0;
            int heavydefense = 0;
            

            // roll attack/defend dice
            for(int j = 0; j < attackDice; j++)
            {
                if(Random.Range(1,7) > 4)
                {
                    hits++;
                }
            }
            for(int j = 0; j < defenseDiceHigh; j++)
            {
                if(Random.Range(1,7) > 4)
                {
                    heavydefense++;
                }
            }
            for(int j = 0; j < defenseDiceLight; j++)
            {
                if(Random.Range(1,7) > 4)
                {
                    lightdefense++;
                }
            }

            // roll damage;
            int damageRoll = Random.Range(1,1 + weaponDice) + weaponDamage;


            // hits light target
            if(hits > lightdefense)
            {
                lightHits++;
                damageLight += damageRoll;
                // if a kill: reset health
                if(damageLight >= healthlight)
                {
                    lightKills++;
                    damageLight = 0;
                }
            }

            // hits heavy target 
            if(hits > heavydefense)
            {
                heavyHits++;
                int soak = armorHeavh;
                // apply ap to armor, armor cannot be reduced to negative
                if(weaponPen != 0)
                {
                    soak -= weaponPen;
                }
                if(soak < 0)
                {
                    soak = 0;
                }

                // reduce incoming damage by soak
                damageRoll -= soak;

                // damage can never be reduced to 0
                if(damageRoll < 1)
                {
                    damageRoll = 1;
                }

                damageHeavy += damageRoll;
                // if a kill: reset health
                if(damageHeavy >= healthHeavy)
                {
                    heavyKills++;
                    damageHeavy = 0;
                }
            }
        }
        Debug.Log("Weapon stat: 1d" + weaponDice + " + " + weaponDamage + "(ap " + weaponPen +") " + "lightTTK: " + (500f/lightKills) + " HeavyTTK: " + (500f/heavyKills));       
    }
}
