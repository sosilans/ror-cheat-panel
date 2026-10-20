using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel.Channel
{
    internal static class RemoteNoclip
    {
        private static readonly HashSet<CharacterBody> Flying = new HashSet<CharacterBody>();
        private static MethodInfo _allUsers;
        private static MethodInfo _isNoclip;
        private static FieldInfo _speed;

        public static void Install(Harmony harmony)
        {
            var runtime = Type.GetType("RoRCheatPanel.CheatRuntime, RoRCheatPanel");
            var admin = Type.GetType("RoRCheatPanel.PlayerAdmin, RoRCheatPanel");
            var util = Type.GetType("RoRCheatPanel.GameUtil, RoRCheatPanel");
            _speed = runtime?.GetField("NoclipSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _allUsers = util?.GetMethod("AllUsers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _isNoclip = admin?.GetMethod("IsNoclip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var apply = admin?.GetMethod("ApplyNoclip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (apply != null)
                harmony.Patch(apply, postfix: new HarmonyMethod(typeof(RemoteNoclip), nameof(FlyOthers)));

            PatchLocalOnly(harmony, admin, "EnableMotorNoclip", nameof(KeepLocalMotor));
            PatchLocalOnly(harmony, admin, "DisableMotorNoclip", nameof(KeepLocalMotor));
            PatchFly(harmony, admin);

            try
            {
                var zoneBody = AccessTools.Method(typeof(MapZone), "TeleportBody");
                if (zoneBody != null)
                    harmony.Patch(zoneBody, prefix: new HarmonyMethod(typeof(RemoteNoclip), nameof(SkipZone)));
            }
            catch (Exception)
            {
            }

            var teleport = AccessTools.Method(typeof(TeleportHelper), "TeleportBody", new[] { typeof(CharacterBody), typeof(Vector3), typeof(bool) });
            if (teleport != null)
                harmony.Patch(teleport, postfix: new HarmonyMethod(typeof(RemoteNoclip), nameof(ReplicateTeleport)));
        }

        private static void ReplicateTeleport(CharacterBody body, Vector3 targetFootPosition, bool forceOutOfVehicle)
        {
            if (!NetworkServer.active || body == null)
                return;
            try
            {
                if (Util.HasEffectiveAuthority(body.gameObject))
                    return;
                var argsType = typeof(TeleportHelper).GetNestedType("TeleportBodyArgs", BindingFlags.Public | BindingFlags.NonPublic);
                var rpc = typeof(CharacterBody).GetMethod("CallRpcTeleportWithLocalAuthority", BindingFlags.Instance | BindingFlags.Public);
                if (argsType == null || rpc == null)
                    return;
                var args = Activator.CreateInstance(argsType);
                argsType.GetProperty("body", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(args, body, null);
                argsType.GetField("targetPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(args, targetFootPosition);
                argsType.GetField("forceOutOfVehicle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(args, forceOutOfVehicle);
                rpc.Invoke(body, new[] { args });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] player teleport was not sent: " + ex.Message);
            }
        }

        private static void PatchLocalOnly(Harmony harmony, Type admin, string name, string prefix)
        {
            MethodInfo method = null;
            foreach (var candidate in admin != null ? admin.GetMethods(BindingFlags.NonPublic | BindingFlags.Static) : Array.Empty<MethodInfo>())
            {
                if (candidate.Name == name)
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing PlayerAdmin." + name);
                return;
            }

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(RemoteNoclip), prefix));
        }

        private static void PatchFly(Harmony harmony, Type admin)
        {
            MethodInfo method = null;
            foreach (var candidate in admin != null ? admin.GetMethods(BindingFlags.NonPublic | BindingFlags.Static) : Array.Empty<MethodInfo>())
            {
                if (candidate.Name == "FlyRemote")
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                Debug.LogWarning("[RoRCheatPanel.Channel] missing PlayerAdmin.FlyRemote");
                return;
            }

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(RemoteNoclip), nameof(SkipRemoteFly)),
                postfix: new HarmonyMethod(typeof(RemoteNoclip), nameof(SteerLocal)));
        }

        private static bool KeepLocalMotor(CharacterMotor motor)
        {
            if (motor == null)
                return true;
            var local = LocalMotor();
            if (local == null)
                return true;
            return motor == local;
        }

        private static bool SkipRemoteFly()
        {
            return false;
        }

        private static void SteerLocal(CharacterBody body, CharacterMotor motor)
        {
            if (body == null || motor == null || !IsLocalBody(body))
                return;

            var forward = body.transform.forward;
            var right = body.transform.right;
            var local = LocalUserManager.GetFirstLocalUser();
            var cam = local != null && local.cameraRigController != null ? local.cameraRigController.sceneCam : null;
            if (cam != null)
            {
                forward = cam.transform.forward;
                right = cam.transform.right;
            }

            var wish = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
                wish += forward;
            if (Input.GetKey(KeyCode.S))
                wish -= forward;
            if (Input.GetKey(KeyCode.A))
                wish -= right;
            if (Input.GetKey(KeyCode.D))
                wish += right;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E))
                wish += Vector3.up;
            if (Input.GetKey(KeyCode.Q))
                wish += Vector3.down;

            motor.velocity = Vector3.zero;
            if (wish.sqrMagnitude <= 0.0001f)
                return;

            var speed = Speed();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= 3f;
            var next = body.transform.position + wish.normalized * speed * Time.unscaledDeltaTime;
            var kcc = motor.Motor;
            if (kcc != null)
                kcc.SetPosition(next, true);
            else
                body.transform.position = next;
        }

        private static void FlyOthers()
        {
            if (!NetworkServer.active)
                return;

            Flying.Clear();
            foreach (var user in Users(_allUsers))
            {
                if (user == null || IsLocal(user) || !IsAdmin(user))
                    continue;
                var body = user.GetCurrentBody();
                if (body == null)
                    continue;
                Flying.Add(body);
                Drag(body);
            }
        }

        private static bool IsAdmin(NetworkUser user)
        {
            return _isNoclip != null && _isNoclip.Invoke(null, new object[] { user }) is bool admin && admin;
        }

        private static void Drag(CharacterBody body)
        {
            var input = body.inputBank;
            var wish = input != null ? input.moveVector : Vector3.zero;
            if (input != null)
            {
                if (input.jump.down)
                    wish += Vector3.up;
                if (input.rawMoveDown.down)
                    wish += Vector3.down;
            }

            if (wish.sqrMagnitude <= 0.0001f)
                return;

            var speed = Speed();
            if (input != null && input.sprint.down)
                speed *= 3f;
            var foot = body.footPosition + wish.normalized * speed * Time.unscaledDeltaTime;
            TeleportHelper.TeleportBody(body, foot, false);
        }

        private static float Speed()
        {
            var config = Type.GetType("RoRCheatPanel.CheatConfig, RoRCheatPanel");
            var entry = config?.GetField("NoclipSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            var value = entry?.GetType().GetProperty("Value")?.GetValue(entry);
            if (value is float configured)
                return configured;
            if (_speed != null && _speed.GetValue(null) is float speed)
                return speed;
            return 22f;
        }

        private static bool SkipZone(CharacterBody characterBody)
        {
            return characterBody == null || !Flying.Contains(characterBody);
        }

        private static CharacterMotor LocalMotor()
        {
            var body = LocalBody();
            return body != null ? body.characterMotor : null;
        }

        private static CharacterBody LocalBody()
        {
            var local = LocalUserManager.GetFirstLocalUser();
            if (local == null)
                return null;
            if (local.cachedBody != null)
                return local.cachedBody;
            var net = local.currentNetworkUser;
            return net != null ? net.GetCurrentBody() : null;
        }

        private static bool IsLocalBody(CharacterBody body)
        {
            if (body == null)
                return false;
            var local = LocalUserManager.GetFirstLocalUser();
            if (local == null)
                return false;
            return local.cachedBody == body || LocalBody() == body;
        }

        private static bool IsLocal(NetworkUser user)
        {
            var local = LocalUserManager.GetFirstLocalUser();
            var net = local != null ? local.currentNetworkUser : null;
            return net != null && user == net;
        }

        private static IEnumerable<NetworkUser> Users(MethodInfo method)
        {
            if (method == null || !(method.Invoke(null, null) is System.Collections.IEnumerable items))
                yield break;
            foreach (var item in items)
            {
                if (item is NetworkUser user && user != null)
                    yield return user;
            }
        }
    }
}
