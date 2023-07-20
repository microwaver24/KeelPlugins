﻿using LockOnPlugin.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LockOnPlugin.Koikatu
{
    internal abstract class LockOnBase : MonoBehaviour
    {
        public static bool lockedOn;

        protected abstract float CameraMoveSpeed { get; set; }
        protected abstract Vector3 CameraTargetPos { get; set; }
        protected abstract Vector3 CameraAngle { get; set; }
        protected abstract float CameraFov { get; set; }
        protected abstract Vector3 CameraDir { get; set; }
        protected abstract bool CameraTargetTex { set; }
        protected abstract float CameraZoomSpeed { get; }
        protected abstract Transform CameraTransform { get; }
        protected virtual Vector3 CameraForward => CameraTransform.forward;
        protected virtual Vector3 CameraRight => CameraTransform.right;
        protected virtual Vector3 CameraUp => CameraTransform.up;
        protected virtual bool CameraEnabled => true;
        protected virtual Vector3 LockOnTargetPos => lockOnTarget.transform.position;
        protected virtual bool AllowTracking => true;
        protected virtual bool InputFieldSelected => EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null;

        protected KeyboardShortcutHotkey lockOnHotkey;
        protected KeyboardShortcutHotkey prevCharaHotkey;
        protected KeyboardShortcutHotkey nextCharaHotkey;

        protected ChaInfo currentCharaInfo;
        protected GameObject lockOnTarget;
        protected Vector3? lastTargetPos;
        protected float defaultCameraSpeed;
        protected float trackingSpeedMax = 0.3f;
        protected bool shouldResetLock;

        protected Vector3 targetOffsetSize;
        protected Vector3 targetOffsetSizeAdded;
        protected float offsetKeyHeld;
        protected bool reduceOffset;

        protected virtual void Start()
        {
            defaultCameraSpeed = CameraMoveSpeed;
            lockOnHotkey = new KeyboardShortcutHotkey(LockOnPluginCore.LockOnKey.Value, 0.4f);
            prevCharaHotkey = new KeyboardShortcutHotkey(LockOnPluginCore.PrevCharaKey.Value);
            nextCharaHotkey = new KeyboardShortcutHotkey(LockOnPluginCore.NextCharaKey.Value);
        }

        protected virtual void OnDestroy()
        {
            ResetModState();
        }

        protected virtual void Update()
        {
            if(!lockOnTarget && lockedOn)
            {
                Log.Debug("Reset LockOnPlugin");
                ResetModState();
            }

            lockOnHotkey.KeyHoldAction(LockOnRelease);
            lockOnHotkey.KeyUpAction(() => LockOn());
            prevCharaHotkey.KeyDownAction(() => CharaSwitch(false));
            nextCharaHotkey.KeyDownAction(() => CharaSwitch(true));

            if (lockOnTarget && CameraEnabled)
            {
                if(Input.GetMouseButton(0) && Input.GetMouseButton(1))
                {
                    float x = Input.GetAxis("Mouse X");
                    float y = Input.GetAxis("Mouse Y");
                    if(Mathf.Abs(x) > 0f || Mathf.Abs(y) > 0f)
                    {
                        targetOffsetSize += CameraRight * (x * defaultCameraSpeed) + CameraForward * (y * defaultCameraSpeed);
                        reduceOffset = false;
                    }
                }
                else if(Input.GetMouseButton(1))
                {
                    float x = Input.GetAxis("Mouse X");
                    if(Input.GetKey(LockOnPluginCore.CameraTiltKey.Value.MainKey))
                    {
                        Guitime.angle = 0.1f;
                        if(Mathf.Abs(x) > 0f)
                        {
                            //camera tilt adjustment
                            float newAngle = CameraAngle.z - x;
                            newAngle = Mathf.Repeat(newAngle, 360f);
                            CameraAngle = new Vector3(CameraAngle.x, CameraAngle.y, newAngle);
                        }
                    }
                    else if (Input.GetKey(LockOnPluginCore.CameraFovKey.Value.MainKey))
                    {
                        Guitime.fov = 0.1f;
                        if (Mathf.Abs(x) > 0f)
                        {
                            //fov adjustment
                            float newFov = CameraFov + x;
                            CameraFov = Mathf.Clamp(newFov, 1f, 160f);
                        }
                    }
                    else if(!InputFieldSelected)
                    {
                        if(Mathf.Abs(x) > 0f)
                        {
                            //handle zooming manually when camera.movespeed == 0
                            float newDir = CameraDir.z - x * CameraZoomSpeed;
                            newDir = Mathf.Clamp(newDir, float.MinValue, 0f);
                            CameraDir = new Vector3(0f, 0f, newDir);
                            reduceOffset = false;
                        }

                        float y = Input.GetAxis("Mouse Y");
                        if(Mathf.Abs(y) > 0f)
                        {
                            targetOffsetSize += Vector3.up * (y * defaultCameraSpeed);
                            reduceOffset = false;
                        }
                    }
                }

                bool lockMoveRight = Input.GetKey(LockOnPluginCore.LockMoveRightKey.Value.MainKey);
                bool lockMoveLeft = Input.GetKey(LockOnPluginCore.LockMoveLeftKey.Value.MainKey);
                bool lockMoveForward = Input.GetKey(LockOnPluginCore.LockMoveForwardKey.Value.MainKey);
                bool lockMoveBackward = Input.GetKey(LockOnPluginCore.LockMoveBackwardKey.Value.MainKey);
                bool lockMoveUp = Input.GetKey(LockOnPluginCore.LockMoveUpKey.Value.MainKey);
                bool lockMoveDown = Input.GetKey(LockOnPluginCore.LockMoveDownKey.Value.MainKey);

                if(!InputFieldSelected && (lockMoveRight || lockMoveLeft || lockMoveForward || lockMoveBackward || lockMoveUp || lockMoveDown))
                {
                    reduceOffset = false;
                    offsetKeyHeld += Time.deltaTime / 3f;
                    if (offsetKeyHeld > 1f)
                    {
                        offsetKeyHeld = 1f;
                    }
                    float speed = Time.deltaTime * Mathf.Lerp(0.2f, 2f, offsetKeyHeld);

                    if (lockMoveRight)
                    {
                        targetOffsetSize += CameraRight * speed;
                    }
                    else if (lockMoveLeft)
                    {
                        targetOffsetSize += CameraRight * -speed;
                    }

                    if (lockMoveForward)
                    {
                        targetOffsetSize += CameraForward * speed;
                    }
                    else if (lockMoveBackward)
                    {
                        targetOffsetSize += CameraForward * -speed;
                    }

                    if (lockMoveUp)
                    {
                        targetOffsetSize += CameraUp * speed;
                    }
                    else if (lockMoveDown)
                    {
                        targetOffsetSize += CameraUp * -speed;
                    }
                }
                else
                {
                    offsetKeyHeld -= Time.deltaTime * 2f;
                    if (offsetKeyHeld < 0f)
                    {
                        offsetKeyHeld = 0f;
                    }
                }

                if(reduceOffset)
                {
                    // add this as a setting
                    if(targetOffsetSize.magnitude > 0.00001f)
                    {
                        float trackingSpeed = CameraTargetManager.IsMovementPoint(lockOnTarget) ? trackingSpeedMax : LockOnPluginCore.TrackingSpeedNormal.Value;
                        targetOffsetSize = Vector3.MoveTowards(targetOffsetSize, new Vector3(), targetOffsetSize.magnitude / (1f / trackingSpeed));
                    }
                    else
                    {
                        targetOffsetSize = new Vector3();
                        reduceOffset = false;
                    }
                }

                if(AllowTracking)
                {
                    float trackingSpeed;
                    float leash;

                    if(CameraTargetManager.IsMovementPoint(lockOnTarget))
                    {
                        trackingSpeed = trackingSpeedMax;
                        leash = 0f;
                    }
                    else
                    {
                        trackingSpeed = LockOnPluginCore.TrackingSpeedNormal.Value;
                        leash = LockOnPluginCore.LockLeashLength.Value;
                    }

                    float distance = Vector3.Distance(CameraTargetPos, lastTargetPos.Value);
                    if(distance > leash + 0.00001f)
                        CameraTargetPos = Vector3.MoveTowards(CameraTargetPos, LockOnTargetPos + targetOffsetSize, (distance - leash) * trackingSpeed * Time.deltaTime * 60f);
                    CameraTargetPos += targetOffsetSize - targetOffsetSizeAdded;
                    targetOffsetSizeAdded = targetOffsetSize;
                    lastTargetPos = LockOnTargetPos + targetOffsetSize;
                }
            }
        }

        protected virtual void OnGUI()
        {
            if(LockOnPluginCore.ShowInfoMsg.Value && Guitime.info > 0f)
            {
                DebugGUI(Guitime.pos.x, Guitime.pos.y, 200f, 45f, Guitime.msg);
                Guitime.info -= Time.deltaTime;
            }

            if(Guitime.angle > 0f)
            {
                DebugGUI(0.5f, 0.5f, 100f, 50f, "Camera tilt\n" + CameraAngle.z.ToString("0.0"));
                Guitime.angle -= Time.deltaTime;
            }

            if(Guitime.fov > 0f)
            {
                DebugGUI(0.5f, 0.5f, 100f, 50f, "Field of view\n" + CameraFov.ToString("0.0"));
                Guitime.fov -= Time.deltaTime;
            }
        }

        private static void DebugGUI(float screenWidthMult, float screenHeightMult, float width, float height, string msg)
        {
            var guiStyle = GUI.skin.button;
            var textContent = new GUIContent(msg);
            var textSize = guiStyle.CalcSize(textContent);
            width = textSize.x;
            height = textSize.y;

            float xpos = Screen.width * screenWidthMult - width / 2f;
            float ypos = Screen.height * screenHeightMult - height / 2f;
            xpos = Mathf.Clamp(xpos, 0f, Screen.width - width);
            ypos = Mathf.Clamp(ypos, 0f, Screen.height - height);
            GUI.Button(new Rect(xpos, ypos, width, height), textContent);
        }

        protected GameObject NextLockOnTarget(List<GameObject> targets = null)
        {
            if (targets == null)
            {
                if (currentCharaInfo == null)
                {
                    Console.WriteLine($"NextLockOnTarget: currentCharaInfo is null");
                    return null;
                }

                targets = CameraTargetManager.GetTargetManager(currentCharaInfo)?.GetTargets();

                if (targets == null)
                {
                    Console.WriteLine($"NextLockOnTarget: targets is null");
                    return null;
                }
            }

            if (targets.Count <= 0)
            {
                Console.WriteLine($"NextLockOnTarget: targets is empty");
                return null;
            }

            if (lockOnTarget != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (lockOnTarget == targets[i])
                    {
                        int next = i + 1 > targets.Count - 1 ? 0 : i + 1;
                        return targets[next];
                    }
                }
            }

            return targets[0];
        }

        protected GameObject FirstLockOnTarget(List<GameObject> targets = null)
        {
            if (targets == null)
            {
                if (currentCharaInfo == null)
                {
                    Console.WriteLine($"FirstLockOnTarget: currentCharaInfo is null");
                    return null;
                }

                targets = CameraTargetManager.GetTargetManager(currentCharaInfo)?.GetTargets();

                if (targets == null)
                {
                    Console.WriteLine($"FirstLockOnTarget: targets is null");
                    return null;
                }
            }

            if (targets.Count <= 0)
            {
                Console.WriteLine($"FirstLockOnTarget: targets is empty");
                return null;
            }

            return targets[0];
        }

        protected virtual bool LockOn()
        {
            if (currentCharaInfo)
            {
                var targets = CameraTargetManager.GetTargetManager(currentCharaInfo).GetTargets();

                if (shouldResetLock)
                {
                    shouldResetLock = false;
                    return LockOn(FirstLockOnTarget(targets));
                }

                if (reduceOffset)
                {
                    CameraTargetPos += targetOffsetSize;
                    targetOffsetSize = new Vector3();
                }
                else if (targetOffsetSize.magnitude > 0f)
                {
                    reduceOffset = true;
                    Guitime.InfoMsg($"Reset offset. Still locked to \"{lockOnTarget.name}\".");
                    return true;
                }

                return LockOn(NextLockOnTarget(targets));
            }

            return false;
        }

        protected virtual bool LockOn(string targetName, bool lockOnAnyway = false, bool resetOffset = true)
        {
            foreach(var target in CameraTargetManager.GetTargetManager(currentCharaInfo).GetTargets())
            {
                if(target.name == targetName)
                {
                    if(LockOn(target, resetOffset))
                    {
                        return true;
                    }
                }
            }

            if(lockOnAnyway)
            {
                if(LockOn())
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool LockOn(GameObject target, bool resetOffset = true)
        {
            if(target)
            {
                if (resetOffset)
                {
                    reduceOffset = true;
                }
                lockedOn = true;
                lockOnTarget = target;
                if (lastTargetPos == null)
                {
                    lastTargetPos = LockOnTargetPos + targetOffsetSize;
                }
                CameraMoveSpeed = 0f;
                Guitime.InfoMsg("Locked to \"" + lockOnTarget.name + "\"");
                return true;
            }

            return false;
        }

        protected virtual void LockOnRelease()
        {
            if(lockOnTarget)
            {
                lockedOn = false;
                reduceOffset = true;
                lockOnTarget = null;
                lastTargetPos = null;
                CameraMoveSpeed = defaultCameraSpeed;
                Guitime.InfoMsg("Camera unlocked");
            }
        }

        protected virtual void CharaSwitch(bool scrollDown = true)
        {
            Log.Info("Character switching not implemented in this version");
        }

        protected virtual void ResetModState()
        {
            lockedOn = false;
            reduceOffset = true;
            lockOnTarget = null;
            lastTargetPos = null;
            CameraMoveSpeed = defaultCameraSpeed;
            Guitime.InfoMsg("Camera unlocked");
            currentCharaInfo = null;
        }

        protected static class Guitime
        {
            public static float angle;
            public static float fov;
            public static float info;
            public static string msg = "";
            public static Vector2 pos = new Vector2(0.5f, 0f);

            public static void InfoMsg(string newMsg, float time = 3f)
            {
                msg = newMsg;
                info = time;
            }
        }
    }
}
