using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RockCompany.Behaviors
{
    class Throwable : PhysicsProp
    {
        public Ray rockThrowRay;
        public RaycastHit rockHit;
        //public AnimationCurve rockFallCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 0.5f), new Keyframe(0.2f, 0.8f), new Keyframe(0.3f, 1f));
        //public static AnimationCurve temp = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 0.3f), new Keyframe(0.3f, 1f));
        public AnimationCurve rockFallCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 0.5f), new Keyframe(0.4f, 0.8f), new Keyframe(0.6f, 1f));
        public static AnimationCurve temp = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 0.3f), new Keyframe(0.6f, 1f));
        public AnimationCurve rockVerticalFallCurveNoBounce = temp;
        public AnimationCurve rockVerticalFallCurve = temp;

        private RaycastHit[] objectsHitByRock;
        private List<RaycastHit> objectsHitByRockList = new List<RaycastHit>();

        private int shovelMask = 11012424;

        // REMOVE WHEN DONE DEBUGGING
        internal ManualLogSource mls;
        private const string modGUID = "Jaydesonia.RockCompany";



        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            base.ItemActivate(used, buttonDown);
            if (base.IsOwner)
            {
                objectsHitByRock = Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position+(Vector3.up*-0.15f), playerHeldBy.gameplayCamera.transform.forward),1f,20f, shovelMask, QueryTriggerInteraction.Collide);
                objectsHitByRockList = objectsHitByRock.OrderBy((RaycastHit x) => x.distance).ToList();
                PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
                List<int> playerIDs = new List<int>();
                for (int i = 0; i < objectsHitByRockList.Count; i++)
                {
                    mls.LogInfo(String.Format("OBJECT HIT BY ROCK SPHERECAST: {0}, TYPE: {1}",objectsHitByRockList[i].collider.gameObject, objectsHitByRockList[i].collider.gameObject.GetType()));
                    IHittable component;
                    PlayerControllerB componentPlayer;
                    EnemyAI componentEnemy;
                    RaycastHit hitInfo;
                    if (objectsHitByRockList[i].transform.gameObject.layer == 8 || objectsHitByRockList[i].transform.gameObject.layer == 11)
                    {
                        
                    }
                    else if (objectsHitByRockList[i].transform.TryGetComponent<IHittable>(out component) && !(objectsHitByRockList[i].transform == playerHeldBy.transform) && (objectsHitByRockList[i].point == Vector3.zero || !Physics.Linecast(playerHeldBy.gameplayCamera.transform.position, objectsHitByRockList[i].point, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault)))
                    {
                        Vector3 forward = playerHeldBy.gameplayCamera.transform.forward;
                        Debug.DrawRay(objectsHitByRockList[i].point, Vector3.up * 0.25f, Color.green, 5f);
                        try
                        {
                            mls.LogInfo("ROCK HIT IHITTABLE: " + component);
                            mls.LogInfo("IHITTABLE TYPE: " + component.GetType());
                            // Set to 1 for test, 101 for real
                            int randomNumber = UnityEngine.Random.Range(0, 101);
                            if (component.GetType().Equals(typeof(Landmine)))
                            {
                                
                            }
                            else if (component.GetType().Equals(typeof(Turret)))
                            {
                                component.Hit(1, forward, playerHeldBy, playHitSFX: true);
                            }
                            else if (randomNumber == 0)
                            {
                                if(objectsHitByRockList[i].transform.TryGetComponent<PlayerControllerB>(out componentPlayer))
                                {
                                    componentPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Bludgeoning);
                                }
                                else if (objectsHitByRockList[i].transform.TryGetComponent<EnemyAI>(out componentEnemy))
                                {
                                    componentEnemy.KillEnemyServerRpc(false);
                                }
                                else
                                {
                                    // Backup code in case it is not player/not enemy but does have a hit component (this likely isn't neccesary, but im leaving it here in case)
                                    //component.Hit(10, forward, playerHeldBy, playHitSFX: true);
                                }
                            }
                            else if (objectsHitByRockList[i].transform.TryGetComponent<PlayerControllerB>(out componentPlayer))
                            {
                                mls.LogInfo("Attempting to hit player:");
                                if(!(playerIDs.Contains((int)componentPlayer.playerClientId)))
                                {
                                    mls.LogInfo("Adding player to list");
                                    playerIDs.Add((int)componentPlayer.playerClientId);
                                    componentPlayer.DamagePlayerFromOtherClientServerRpc(1, playerHeldBy.transform.forward, (int)playerHeldBy.playerClientId);
                                }
                                else
                                {
                                    mls.LogInfo("Player was on list");
                                }
                            }
                            else if (objectsHitByRockList[i].transform.TryGetComponent<EnemyAI>(out componentEnemy))
                            {
                                componentEnemy.HitEnemyServerRpc(1, (int)playerHeldBy.playerClientId, true);
                            }
                        }
                        catch (Exception arg)
                        {
                            Debug.Log($"Exception caught when hitting object with ROCK from player #{playerHeldBy.playerClientId}: {arg}");
                        }
                    }
                    
                }
                playerHeldBy.DiscardHeldObject(placeObject: true, null, GetRockThrowDestination());
                playerIDs.Clear();
            }
        }

        public Vector3 GetRockThrowDestination()
        {
            Vector3 position = base.transform.position;
            Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
            rockThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            // MIGHT NEED TO SET StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers back to collidersAndRoomMaskAndDefault
            Debug.Log(StartOfRound.Instance.collidersAndRoomMaskAndDefault);
            position = ((!Physics.Raycast(rockThrowRay, out rockHit, 20f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) ? rockThrowRay.GetPoint(16f) : rockThrowRay.GetPoint(rockHit.distance - 0.05f));
            Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
            rockThrowRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(rockThrowRay, out rockHit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                return rockHit.point + Vector3.up * 0.05f;
            }
            return rockThrowRay.GetPoint(30f);
        }

        public override void EquipItem()
        {
            SetControlTipForRock();
            EnableItemMeshes(enable: true);
            isPocketed = false;
        }

        private void SetControlTipForRock()
        {
            string[] allLines = (new string[1] { "Throw Rock: [LMB]" });
            if (base.IsOwner)
            {
                HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, itemProperties);
            }
        }

        public override void FallWithCurve()
        {
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, rockFallCurve.Evaluate(fallTime));
            if (magnitude > 5f)
            {
                base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), rockVerticalFallCurveNoBounce.Evaluate(fallTime));
            }
            else
            {
                base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), rockVerticalFallCurve.Evaluate(fallTime));
            }
            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
        }
    }
}
