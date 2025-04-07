using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zorro.Settings;
using Landfall.Modding;
using Landfall.Haste;
using UnityEngine.Localization;
using System.Collections;
using Unity.Mathematics;

namespace FirstPerson;

// var original = typeof(PlayerMovement).getMethod(nameof(Start));
[LandfallPlugin]
public class FirstPersonPlugin{
    public static readonly Harmony harmony = new Harmony("com.uhhhhFirstPersonMod.Fishy");
    public static bool Enabled = true;
    public static float HubDelayTime = 4;

    static FirstPersonPlugin(){
        harmony.PatchAll();
        Debug.Log("Haste in First Person is loaded!");
    }
    public static IEnumerator ApplyPerspective(PlayerCharacter inst, float delay){
        Debug.Log("HFPM: ApplyPerspective Start Time: "+Time.time);
        yield return new WaitForSeconds(delay);
        inst.data.firstPerson = Enabled;
        Debug.Log("HFPM: ApplyPerspective End Time: "+Time.time);
    }

    public static IEnumerator ApplyCamSettings(CameraMovement inst, float height, float fov){
        if(inst.TryGetComponent<Camera>( out Camera camera)){
            // Debug.Log("HFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPM"+camera.fieldOfView);
            if(Enabled == false){
                camera.fieldOfView = 70;
            }else if(fov!=-1){
                camera.fieldOfView = fov;
            }
        }else{
            Debug.LogError("HFPM: Failed to get Camera Component!");
        }

        if(Enabled==true){
            inst.offset_FP = new Vector3(0f, height, 0f);
        }
        yield return null;
    }

}

[HarmonyPatch(typeof(Player))]
class PlayerPatch {
    [HarmonyPatch("Die")]
    [HarmonyPrefix]
    static void PlayerDiePre(Player __instance){
        __instance.character.data.firstPerson = false;
    }
}

[HarmonyPatch(typeof(PlayerCharacter))]
class FirstPersonPatch{

    

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    static void CharStartPost(PlayerCharacter __instance){
        Debug.Log("HFPM: OnPostfix starting coroutine");
        
        if(SceneManager.GetActiveScene().name == "FullHub"){
            Debug.Log("HFPM: Delaying Perspective due to hub by: "+ FirstPersonPlugin.HubDelayTime);
            __instance.StartCoroutine(FirstPersonPlugin.ApplyPerspective(__instance, FirstPersonPlugin.HubDelayTime));
        }else{
            __instance.StartCoroutine(FirstPersonPlugin.ApplyPerspective(__instance,0));
        }
        
    }

    [HarmonyPatch("Revive")]
    [HarmonyPostfix]
    static void RevivePlayerPost(PlayerCharacter __instance){
        if(SceneManager.GetActiveScene().name != "FullHub")
            __instance.data.firstPerson = FirstPersonPlugin.Enabled;
    }

    
    /*
    [HarmonyPatch("KillPlayer")]
    [HarmonyPrefix]
    static void KillPlayerPre(PlayerCharacter __instance){
        __instance.data.firstPerson = false;
    }
    

    [HarmonyPatch("TakeDamage")]
    [HarmonyPostfix]
    static void TakeDamage(PlayerCharacter __instance, float damage, Transform sourceTransform, string sourceName = null, EffectSource source = EffectSource.Environment){
        if(__instance.player.data.currentHealth <= 0f){
            __instance.data.firstPerson = false;
        }
    }
    */
}



[HarmonyPatch(typeof(CameraMovement))]
class CameraMovementPatch{
    public static float CamHeight=3.5f,CamFov = -1;
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    static void CameraStartPostFix(CameraMovement __instance){
        // __instance.StartCoroutine(FirstPersonPlugin.ApplyCamSettings(__instance, CamHeight, CamFov));
    }

    [HarmonyPatch("GetOffset")]
    [HarmonyPrefix]
    static bool CameraGetOffsetPostFix(CameraMovement __instance, ref Vector3 __result){
        if(FirstPersonPlugin.Enabled==false){
            // Debug.Log("HFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPMHFPM: Plugin disabled");
            return true;
        }
        __result = new Vector3(0,CamHeight,0);
        // __instance.currentOffset;
        __instance.StartCoroutine(FirstPersonPlugin.ApplyCamSettings(__instance, CamHeight, CamFov));
        return false;
    }


}// wait im stupid i dont need to do this nvm doing it anyways



[HasteSetting]
public class Enable : OffOnSetting, IExposedSetting {
    public override void ApplyValue()
    {
        FirstPersonPlugin.Enabled = base.Value==OffOnMode.ON;
        GameObject player = GameObject.Find("Player");
        if(player==null){
            Debug.LogError("HFPM: Failed to find player on settings change!");
            return;
        }
        // if(SceneManager.GetActiveScene().name == "FullHub")
        player.GetComponent<PlayerCharacter>().StartCoroutine(FirstPersonPlugin.ApplyPerspective(player.GetComponent<PlayerCharacter>(),FirstPersonPlugin.HubDelayTime)); 
        
    }

    public string GetCategory() => "H.F.P.M.";

    // public override OffOnMode GetDefaultValue() {
    //     return OffOnMode.ON;
    // }

    public LocalizedString GetDisplayName() => new UnlocalizedString("Enable First Person?");

    public override List<LocalizedString> GetLocalizedChoices() {
        return new List<LocalizedString>
        {
            new LocalizedString("Settings", "DisabledGraphicOption"),
            new LocalizedString("Settings", "EnabledGraphicOption")
        };
    }

    protected override OffOnMode GetDefaultValue()
    {
        return OffOnMode.ON;
    }
}

[HasteSetting]
public class Offset : FloatSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        GameObject camera = GameObject.Find("MainCamera");
        CameraMovementPatch.CamHeight=Mathf.Clamp(base.Value,0,7);
        if(camera==null){
            Debug.LogError("HFPM: Failed to find Camera on settings load!, storing value for later...");
            return;
        }
        camera.GetComponent<CameraMovement>().offset_FP = new Vector3(0, Mathf.Clamp(base.Value,0,7), .2f);
    }

    public string GetCategory() => "H.F.P.M.";

    public LocalizedString GetDisplayName() => new UnlocalizedString("Camera Height");

    protected override float GetDefaultValue()
    {
        return 3.5f;
    }

    protected override float2 GetMinMaxValue()
    {
        return new float2(0, 7);
    }
}
[HasteSetting]
public class Fov : FloatSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        // GameObject camera = GameObject.Find("MainCamera");
        CameraMovementPatch.CamFov=Mathf.Clamp(base.Value,0,360);
    //     if(camera==null){
    //         Debug.LogError("HFPM: Failed to find Camera on settings load!, storing value for later...");
    //         return;
    //     }
    //     // camera.GetComponent<CameraMovement>().offset_FP = new Vector3(0, Mathf.Clamp(base.Value,0,7), .2f);
    }

    public string GetCategory() => "H.F.P.M.";

    public LocalizedString GetDisplayName() => new UnlocalizedString("Camera Fov");

    protected override float GetDefaultValue()
    {
        return 70f;
    }

    protected override float2 GetMinMaxValue()
    {
        return new float2(0,360);
    }
}

