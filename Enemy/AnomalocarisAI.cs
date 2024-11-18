using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Anomalocaris.Enemy
{
    internal class AnomalocarisAI : EnemyAI
    {
        // No states needed since they are just thousand year old water shrimp

        private QuicksandTrigger water;
        private BoxCollider waterCollider;
        private readonly QuicksandTrigger[] sandAndWater = FindObjectsOfType<QuicksandTrigger>();
        private readonly List<QuicksandTrigger> waters = new List<QuicksandTrigger>();

        private float wanderTimer;
        private Vector3 wanderPos = Vector3.zero;

        private bool wallInFront;

        [SerializeField] private Transform RaycastPos;
        private const float Speed = 5;

        public override void Start()
        {
            base.Start();
            if (!IsServer) return;

            try
            {
                foreach (QuicksandTrigger maybeWater in sandAndWater)
                {
                    if (maybeWater.isWater && !maybeWater.gameObject.CompareTag("SpawnDenialPoint"))
                    {
                        waters.Add(maybeWater);
                    }
                }

                if (waters.Count == 0)
                {
                    RoundManager.Instance.DespawnEnemyOnServer(new NetworkObjectReference(gameObject.GetComponent<NetworkObject>()));
                    return;
                }

                if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Flooded)
                {
                    RoundManager.Instance.DespawnEnemyOnServer(new NetworkObjectReference(gameObject.GetComponent<NetworkObject>()));
                    return;
                }

                water = waters[UnityEngine.Random.Range(0, waters.Count)];
                waterCollider = water.gameObject.GetComponent<BoxCollider>();

                SetWanderPos();

                transform.position = new Vector3(water.transform.position.x, waterCollider.bounds.max.y, water.transform.position.z);
            } catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void SetWanderPos()
        {
            wanderPos.x = UnityEngine.Random.Range(waterCollider.bounds.min.x, waterCollider.bounds.max.x);
            wanderPos.y = UnityEngine.Random.Range(waterCollider.bounds.min.y, waterCollider.bounds.max.y);
            wanderPos.z = UnityEngine.Random.Range(waterCollider.bounds.min.z, waterCollider.bounds.max.z);

            wanderTimer = 0f;
        }

        private static bool Collision(Vector3 pos, Collider col)
        {
            return col.bounds.Contains(pos);
        }

        private void TurnTowardsLocation3d(Vector3 location)
        {
            transform.LookAt(location);
        }

        private bool CheckForWall()
        {
            return Physics.Raycast(RaycastPos.position, RaycastPos.forward, 4f, 1 << 8 /**Bitmasks are weird. This references layer 8 which is "Room"**/);
        }

        private void Rise(float riseSpeed)
        {
            transform.Translate(Vector3.up * (riseSpeed * Time.deltaTime));
        }

        private static float WaterTop(BoxCollider coll)
        {
            return coll.transform.localScale.y * coll.size.y / 2 + coll.transform.position.y;
        }

        public override void Update()
        {
            base.Update();

            if (!IsServer) return;

            if (isEnemyDead && transform.position.y < waterCollider.bounds.max.y)
                Rise(0.2f);

            if (isEnemyDead)
                return;

            wanderTimer += Time.deltaTime;

            base.DoAIInterval();
            if (WaterTop(waterCollider) < transform.position.y)
                transform.position = new Vector3(transform.position.x, WaterTop(waterCollider), transform.position.z);


            if (wanderTimer >= 5)
            {
                SetWanderPos();
            }

            Vector3 wanderLocation = Vector3.MoveTowards(transform.position, wanderPos, Speed * Time.deltaTime);

            TurnTowardsLocation3d(wanderPos);

            if (wallInFront || !Collision(wanderLocation, waterCollider))
            {
                SetWanderPos();
            }
            else
            {
                transform.position = wanderLocation;
            }
        }

        private void FixedUpdate()
        {
            if (!IsServer) return;
            wallInFront = CheckForWall();
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            enemyHP -= force;
            if (enemyHP <= 0)
            {
                KillEnemyOnOwnerClient();
            }
        }
    }
}
