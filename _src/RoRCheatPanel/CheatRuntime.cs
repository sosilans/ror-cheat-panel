using System;
using System.Collections.Generic;
using System.Reflection;
using KinematicCharacterController;
using RoR2;
using RoRCheatPanel.UI;
using UnityEngine;

namespace RoRCheatPanel;

internal static class CheatRuntime
{
	public static bool GodMode;

	public static bool InfiniteSkills;

	public static bool InfiniteEquipment;

	public static float TimeScale = 1f;

	public static bool FreezeStopwatch;

	public static bool Noclip;

	public static float NoclipSpeed = 22f;

	public static bool Disco;

	public static bool LowGravity;

	public static float BodyScale = 1f;

	public static float Fov = 60f;

	public static bool ThirdPerson;

	public static bool DrunkCamera;

	public static float ThirdPersonDistance = 8f;

	private static bool _camOverlayOn;

	private static Vector3 _savedCamPos;

	private static Quaternion _savedCamRot;

	private static readonly Dictionary<CharacterMotor, float> LowGravSaved = new Dictionary<CharacterMotor, float>();

	private const float MoonJumpMeters = 26f;

	private static bool _noclipEnabledOnMotor;

	private static CharacterMotor _noclipMotor;

	private static CursorLockMode _savedLock;

	private static bool _savedCursorVisible;

	private static bool _cursorOverridden;

	private static bool _stopwatchOverridden;

	private static readonly List<HealthComponent> GodApplied = new List<HealthComponent>();

	public static void ToggleGodMode()
	{
		GodMode = !GodMode;
		ApplyGodMode();
		UiHelpers.Toast(GodMode ? Loc.T("god_on") : Loc.T("god_off"));
	}

	public static void ToggleNoclip()
	{
		Noclip = !Noclip;
		if (!Noclip)
		{
			RestoreNoclip(_noclipMotor);
			NoclipBounds.Tick(noclipOn: false);
		}
		UiHelpers.Toast(Noclip ? Loc.T("noclip_on") : Loc.T("noclip_off"));
	}

	public static void Tick()
	{
		ApplyGodMode();
		ApplyInfinite();
		ApplyNoclip();
		PlayerAdmin.Tick();
		ApplyLowGravity();
		ApplyBodyScale();
		ClearCameraOverlay();
		ApplyFov();
		ApplyTimeScale();
		ApplyCursor();
		ApplyStopwatch();
	}

	public static void OnWindowClosed()
	{
		RestoreCursor();
		RestoreStopwatch();
	}

	private static void ApplyNoclip()
	{
		CharacterBody localBody = GameUtil.LocalBody;
		CharacterMotor val = (Object.op_Implicit((Object)(object)localBody) ? localBody.characterMotor : null);
		if (!Noclip)
		{
			RestoreNoclip(_noclipMotor);
			NoclipBounds.Tick(noclipOn: false);
			return;
		}
		NoclipBounds.Tick(noclipOn: true);
		if (Object.op_Implicit((Object)(object)val))
		{
			if (!_noclipEnabledOnMotor || (Object)(object)_noclipMotor != (Object)(object)val)
			{
				RestoreNoclip(_noclipMotor);
				EnableNoclip(val);
				_noclipMotor = val;
				_noclipEnabledOnMotor = true;
			}
			Fly(localBody, val);
		}
	}

	private static void EnableNoclip(CharacterMotor motor)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)motor))
		{
			CharacterFlightParameters flightParameters = motor.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			motor.flightParameters = flightParameters;
			CharacterGravityParameters gravityParameters = motor.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			motor.gravityParameters = gravityParameters;
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)motor).Motor))
			{
				((BaseCharacterController)motor).Motor.SetCapsuleCollisionsActivation(false);
				((BaseCharacterController)motor).Motor.SetMovementCollisionsSolvingActivation(false);
				((BaseCharacterController)motor).Motor.SetGroundSolvingActivation(false);
			}
		}
	}

	private static void RestoreNoclip(CharacterMotor motor)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (!_noclipEnabledOnMotor)
		{
			_noclipMotor = null;
			return;
		}
		if (Object.op_Implicit((Object)(object)motor))
		{
			CharacterFlightParameters flightParameters = motor.flightParameters;
			if (flightParameters.channeledFlightGranterCount > 0)
			{
				flightParameters.channeledFlightGranterCount--;
			}
			motor.flightParameters = flightParameters;
			CharacterGravityParameters gravityParameters = motor.gravityParameters;
			if (gravityParameters.channeledAntiGravityGranterCount > 0)
			{
				gravityParameters.channeledAntiGravityGranterCount--;
			}
			motor.gravityParameters = gravityParameters;
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)motor).Motor))
			{
				((BaseCharacterController)motor).Motor.SetCapsuleCollisionsActivation(true);
				((BaseCharacterController)motor).Motor.SetMovementCollisionsSolvingActivation(true);
				((BaseCharacterController)motor).Motor.SetGroundSolvingActivation(true);
			}
		}
		_noclipEnabledOnMotor = false;
		_noclipMotor = null;
	}

	private static void Fly(CharacterBody body, CharacterMotor motor)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		Camera val = ((GameUtil.LocalUser != null && Object.op_Implicit((Object)(object)GameUtil.LocalUser.cameraRigController)) ? GameUtil.LocalUser.cameraRigController.sceneCam : null);
		Vector3 val2 = (Object.op_Implicit((Object)(object)val) ? ((Component)val).transform.forward : (Object.op_Implicit((Object)(object)body.characterDirection) ? body.characterDirection.forward : ((Component)body).transform.forward));
		Vector3 val3 = (Object.op_Implicit((Object)(object)val) ? ((Component)val).transform.right : Vector3.Cross(Vector3.up, val2));
		Vector3 val4 = Vector3.zero;
		if (Input.GetKey((KeyCode)119))
		{
			val4 += val2;
		}
		if (Input.GetKey((KeyCode)115))
		{
			val4 -= val2;
		}
		if (Input.GetKey((KeyCode)97))
		{
			val4 -= val3;
		}
		if (Input.GetKey((KeyCode)100))
		{
			val4 += val3;
		}
		if (Input.GetKey((KeyCode)32) || Input.GetKey((KeyCode)101))
		{
			val4 += Vector3.up;
		}
		if (Input.GetKey((KeyCode)113))
		{
			val4 += Vector3.down;
		}
		motor.velocity = Vector3.zero;
		if (!(val4.sqrMagnitude < 0.0001f))
		{
			float num = NoclipSpeed;
			if (CheatConfig.NoclipSpeed != null)
			{
				num = CheatConfig.NoclipSpeed.Value;
			}
			if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303))
			{
				num *= 3f;
			}
			Vector3 val5 = val4.normalized * num * Time.unscaledDeltaTime;
			Vector3 val6 = ((Component)body).transform.position + val5;
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)motor).Motor))
			{
				((BaseCharacterController)motor).Motor.SetPosition(val6, true);
			}
			else
			{
				((Component)body).transform.position = val6;
			}
		}
	}

	private static void ApplyGodMode()
	{
		if (!GodMode)
		{
			ClearAppliedGodMode();
			return;
		}
		foreach (NetworkUser item in GameUtil.Targets())
		{
			CharacterBody val = (Object.op_Implicit((Object)(object)item) ? item.GetCurrentBody() : null);
			if (Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)val.healthComponent))
			{
				val.healthComponent.godMode = true;
				if (!GodApplied.Contains(val.healthComponent))
				{
					GodApplied.Add(val.healthComponent);
				}
			}
		}
	}

	private static void ClearAppliedGodMode()
	{
		if (GodApplied.Count == 0)
		{
			return;
		}
		for (int i = 0; i < GodApplied.Count; i++)
		{
			HealthComponent hc = GodApplied[i];
			if (Object.op_Implicit((Object)(object)hc))
			{
				hc.godMode = false;
			}
		}
		GodApplied.Clear();
	}

	private static void ApplyInfinite()
	{
		if ((!InfiniteSkills && !InfiniteEquipment) || !GameUtil.IsHost)
		{
			return;
		}
		foreach (NetworkUser item in GameUtil.Targets())
		{
			CharacterBody val = (Object.op_Implicit((Object)(object)item) ? item.GetCurrentBody() : null);
			if (InfiniteSkills && Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)val.skillLocator))
			{
				GenericSkill[] allSkills = val.skillLocator.AllSkills;
				foreach (GenericSkill val2 in allSkills)
				{
					if (Object.op_Implicit((Object)(object)val2))
					{
						val2.stock = val2.maxStock;
						val2.cooldownScale = 0f;
					}
				}
			}
			if (InfiniteEquipment && Object.op_Implicit((Object)(object)item.master) && Object.op_Implicit((Object)(object)item.master.inventory))
			{
				item.master.inventory.DeductActiveEquipmentCooldown(999f);
			}
		}
	}

	private static void ApplyTimeScale()
	{
		float num = Mathf.Clamp(TimeScale, 0.05f, 5f);
		if (Mathf.Abs(Time.timeScale - num) > 0.001f)
		{
			Time.timeScale = num;
			Time.fixedDeltaTime = 0.02f * num;
		}
	}

	public static void ResetTimeScale()
	{
		TimeScale = 1f;
		Time.timeScale = 1f;
		Time.fixedDeltaTime = 0.02f;
	}

	private static void ApplyCursor()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (!CheatWindow.Opened || !CheatConfig.UnlockCursor.Value)
		{
			RestoreCursor();
			return;
		}
		if (!_cursorOverridden)
		{
			_savedLock = Cursor.lockState;
			_savedCursorVisible = Cursor.visible;
			_cursorOverridden = true;
		}
		Cursor.lockState = (CursorLockMode)0;
		Cursor.visible = true;
	}

	private static void RestoreCursor()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if (_cursorOverridden)
		{
			Cursor.lockState = _savedLock;
			Cursor.visible = _savedCursorVisible;
			_cursorOverridden = false;
		}
	}

	private static void ApplyStopwatch()
	{
		if (!CheatConfig.PauseStopwatchOnOpen.Value || !CheatWindow.Opened || !GameUtil.IsHost || !Object.op_Implicit((Object)(object)Run.instance))
		{
			RestoreStopwatch();
			return;
		}
		Run.instance.NetworkisRunStopwatchForcePaused = true;
		_stopwatchOverridden = true;
	}

	private static void RestoreStopwatch()
	{
		if (!_stopwatchOverridden || !Object.op_Implicit((Object)(object)Run.instance))
		{
			_stopwatchOverridden = false;
			return;
		}
		if (!FreezeStopwatch)
		{
			Run.instance.NetworkisRunStopwatchForcePaused = false;
		}
		_stopwatchOverridden = false;
	}

	private static void ApplyLowGravity()
	{
		if (!LowGravity || Noclip)
		{
			RestoreLowGrav();
			return;
		}
		HashSet<CharacterMotor> hashSet = new HashSet<CharacterMotor>();
		foreach (NetworkUser item in GameUtil.Targets())
		{
			CharacterBody val = (Object.op_Implicit((Object)(object)item) ? item.GetCurrentBody() : null);
			CharacterMotor val2 = (Object.op_Implicit((Object)(object)val) ? val.characterMotor : null);
			if (Object.op_Implicit((Object)(object)val2))
			{
				hashSet.Add(val2);
				if (!LowGravSaved.ContainsKey(val2))
				{
					LowGravSaved[val2] = ((val2.gravityScale <= 0.01f) ? 1f : val2.gravityScale);
				}
				val2.gravityScale = MoonGravityScale(val);
			}
		}
		if (LowGravSaved.Count == hashSet.Count)
		{
			return;
		}
		List<CharacterMotor> list = new List<CharacterMotor>();
		foreach (KeyValuePair<CharacterMotor, float> item2 in LowGravSaved)
		{
			if (!Object.op_Implicit((Object)(object)item2.Key) || !hashSet.Contains(item2.Key))
			{
				list.Add(item2.Key);
			}
		}
		foreach (CharacterMotor item3 in list)
		{
			if (Object.op_Implicit((Object)(object)item3))
			{
				item3.gravityScale = LowGravSaved[item3];
			}
			LowGravSaved.Remove(item3);
		}
	}

	private static float MoonGravityScale(CharacterBody body)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		float num = ((Object.op_Implicit((Object)(object)body) && body.jumpPower > 1f) ? body.jumpPower : 15f);
		float num2 = Mathf.Abs(Physics.gravity.y);
		if (num2 < 1f)
		{
			num2 = 9.81f;
		}
		return Mathf.Clamp(num * num / 52f / num2, 0.12f, 0.45f);
	}

	private static void RestoreLowGrav()
	{
		if (LowGravSaved.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<CharacterMotor, float> item in LowGravSaved)
		{
			if (Object.op_Implicit((Object)(object)item.Key))
			{
				item.Key.gravityScale = item.Value;
			}
		}
		LowGravSaved.Clear();
	}

	private static void ApplyBodyScale()
	{
		float num = Mathf.Clamp(BodyScale, 0.2f, 8f);
		foreach (NetworkUser item in GameUtil.Targets())
		{
			CharacterBody val = (Object.op_Implicit((Object)(object)item) ? item.GetCurrentBody() : null);
			if (Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)val.modelLocator) && Object.op_Implicit((Object)(object)val.modelLocator.modelTransform))
			{
				val.modelLocator.modelTransform.localScale = DefaultModelScale(val) * num;
			}
		}
	}

	private static Vector3 DefaultModelScale(CharacterBody body)
	{
		GameObject prefab = BodyCatalog.GetBodyPrefab(body.bodyIndex);
		CharacterBody prefabBody = (Object.op_Implicit((Object)(object)prefab) ? prefab.GetComponent<CharacterBody>() : null);
		Transform model = (Object.op_Implicit((Object)(object)prefabBody) && Object.op_Implicit((Object)(object)prefabBody.modelLocator)) ? prefabBody.modelLocator.modelTransform : null;
		if (!Object.op_Implicit((Object)(object)model))
		{
			return Vector3.one;
		}
		Vector3 basis = model.localScale;
		return (basis.sqrMagnitude < 0.0001f) ? Vector3.one : basis;
	}

	private static void ApplyFov()
	{
		LocalUser localUser = GameUtil.LocalUser;
		if (localUser != null && Object.op_Implicit((Object)(object)localUser.cameraRigController))
		{
			float num = Mathf.Clamp(Fov, 30f, 140f);
			if (DrunkCamera)
			{
				num += Mathf.Sin(Time.unscaledTime * 2.6f) * 11f;
			}
			localUser.cameraRigController.baseFov = num;
		}
	}

	public static void LateTick()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		if (!ThirdPerson && !DrunkCamera)
		{
			return;
		}
		Camera val = SceneCam();
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		_savedCamPos = ((Component)val).transform.position;
		_savedCamRot = ((Component)val).transform.rotation;
		_camOverlayOn = true;
		CharacterBody localBody = GameUtil.LocalBody;
		if (ThirdPerson && Object.op_Implicit((Object)(object)localBody))
		{
			Vector3 val2 = (Object.op_Implicit((Object)(object)localBody.inputBank) ? localBody.inputBank.aimDirection : ((Component)localBody).transform.forward);
			if (val2.sqrMagnitude < 0.001f)
			{
				val2 = ((Component)localBody).transform.forward;
			}
			Vector3 val3 = localBody.corePosition + Vector3.up * 1.1f;
			float num = Mathf.Clamp(ThirdPersonDistance, 3f, 18f);
			Vector3 val4 = val3 - val2.normalized * num + Vector3.up * 0.35f;
			((Component)val).transform.position = val4;
			((Component)val).transform.rotation = Quaternion.LookRotation(val3 - val4, Vector3.up);
		}
		if (DrunkCamera)
		{
			float unscaledTime = Time.unscaledTime;
			((Component)val).transform.Rotate(Mathf.Sin(unscaledTime * 1.8f) * 0.55f, Mathf.Sin(unscaledTime * 1.15f) * 0.45f, Mathf.Sin(unscaledTime * 2.4f) * 7.5f, (Space)1);
		}
	}

	public static void RestoreCamera()
	{
		DrunkCamera = false;
		ThirdPerson = false;
		Fov = 60f;
		ClearCameraOverlay();
		LocalUser localUser = GameUtil.LocalUser;
		CameraRigController val = ((localUser != null) ? localUser.cameraRigController : null);
		if (Object.op_Implicit((Object)(object)val))
		{
			val.baseFov = 60f;
			FlattenCameraRig(val, 60f);
		}
		UiHelpers.Toast(Loc.T("fun_cam_reset_ok"));
	}

	private static void FlattenCameraRig(CameraRigController rig, float fov)
	{
		if (!Object.op_Implicit((Object)(object)rig))
		{
			return;
		}
		CameraState current = FlattenCameraState(rig.currentCameraState, fov);
		rig.currentCameraState = current;
		rig.SetCameraState(current);
		Camera sceneCam = rig.sceneCam;
		if (Object.op_Implicit((Object)(object)sceneCam))
		{
			FlattenWorldUp(((Component)sceneCam).transform);
		}
		Camera uiCam = rig.uiCam;
		if (Object.op_Implicit((Object)(object)uiCam))
		{
			FlattenWorldUp(((Component)uiCam).transform);
		}
		CharacterBody localBody = GameUtil.LocalBody;
		Vector3 aim = current.rotation * Vector3.forward;
		if (Object.op_Implicit((Object)(object)localBody) && Object.op_Implicit((Object)(object)localBody.inputBank) && localBody.inputBank.aimDirection.sqrMagnitude > 0.001f)
		{
			aim = localBody.inputBank.aimDirection;
		}
		if (aim.sqrMagnitude > 0.001f)
		{
			RecenterRig(rig, aim);
		}
	}

	private static CameraState FlattenCameraState(CameraState state, float fov)
	{
		Vector3 forward = state.rotation * Vector3.forward;
		if (forward.sqrMagnitude < 0.0001f)
		{
			forward = Vector3.forward;
		}
		state.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
		state.fov = fov;
		return state;
	}

	private static void FlattenWorldUp(Transform t)
	{
		if (!Object.op_Implicit((Object)(object)t))
		{
			return;
		}
		Vector3 forward = t.forward;
		if (forward.sqrMagnitude >= 0.0001f)
		{
			t.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
		}
	}

	private static void ClearCameraOverlay()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (_camOverlayOn && !DrunkCamera)
		{
			Camera val = SceneCam();
			if (Object.op_Implicit((Object)(object)val))
			{
				((Component)val).transform.position = _savedCamPos;
				((Component)val).transform.rotation = _savedCamRot;
			}
			_camOverlayOn = false;
		}
	}

	private static void RecenterRig(CameraRigController rig, Vector3 aim)
	{
		if (!Object.op_Implicit((Object)(object)rig) || aim.sqrMagnitude < 0.001f)
		{
			return;
		}
		object mode = rig.cameraMode;
		if (mode != null)
		{
			MethodInfo getData = mode.GetType().GetMethod("DebugGetInstanceData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object data = getData != null ? getData.Invoke(mode, new object[1] { rig }) : null;
			if (data != null)
			{
				MethodInfo setPitch = data.GetType().GetMethod("SetPitchYawFromLookVector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (setPitch != null && setPitch.GetParameters().Length == 1)
				{
					setPitch.Invoke(data, new object[1] { aim.normalized });
					return;
				}
			}
		}
		Type type = ((object)rig).GetType();
		MethodInfo methodInfo = type.GetMethod("SetPitchYawFromLookVector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? type.GetMethod("SetPitchYaw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (methodInfo != null && methodInfo.GetParameters().Length == 1)
		{
			methodInfo.Invoke(rig, new object[1] { aim });
			return;
		}
		Quaternion val = Quaternion.LookRotation(aim);
		Vector3 eulerAngles = val.eulerAngles;
		float value = ((eulerAngles.x > 180f) ? (eulerAngles.x - 360f) : eulerAngles.x);
		TrySetMember(rig, type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, "pitch", value);
		TrySetMember(rig, type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, "yaw", eulerAngles.y);
	}

	private static void TrySetMember(object target, Type type, BindingFlags flags, string name, float value)
	{
		FieldInfo fieldInfo = type.GetField(name, flags) ?? type.GetField("_" + name, flags);
		if (fieldInfo != null && fieldInfo.FieldType == typeof(float))
		{
			fieldInfo.SetValue(target, value);
			return;
		}
		PropertyInfo property = type.GetProperty(name, flags);
		if (property != null && property.CanWrite && property.PropertyType == typeof(float))
		{
			property.SetValue(target, value);
		}
	}

	private static Camera SceneCam()
	{
		LocalUser localUser = GameUtil.LocalUser;
		CameraRigController val = ((localUser != null) ? localUser.cameraRigController : null);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return null;
		}
		return val.sceneCam;
	}
}
