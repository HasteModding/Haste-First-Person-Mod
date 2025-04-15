using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zorro.Settings;
using Landfall.Modding;
using Landfall.Haste;
using UnityEngine.Localization;
using System.Collections;
using Unity.Mathematics;
using System.Reflection;
using System.Drawing;
using UnityEngine.UI;


namespace FirstPerson;



[LandfallPlugin]
public class FirstPersonPlugin{
    public static readonly Harmony harmony = new Harmony("com.uhhhhFirstPersonMod.Fishy");
    public static bool Enabled = true;
    public static bool useReFocus = true;
    public static bool BoostUi = true;

    public static GameObject prefabUi;
    public static ComputeShader shader;
    public static RawImage img;
    public static RenderTexture rt;
    public static RectTransform uiElRectTransform;
    public static Gradient gradient, rainbowGradient;
    public static float scrollSpeed=.5f;
    public static float internalTime = 0;
    static float cBoostAmount=0;
    public static float boostLerpSpeed=1.1f;
    public static float camHeight=3.5f;
    public static float FOV = 70;

    public static bool UseFovScaling = true;

    public static string AssemblyDirectory {
        get {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }

    static FirstPersonPlugin(){
        
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(AssemblyDirectory, "hfpmboostui"));
        if (assetBundle == null) {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }

        prefabUi = assetBundle.LoadAsset<GameObject>("Image");
        shader = assetBundle.LoadAsset<ComputeShader>("BoostUi");
        assetBundle.Unload(false);
        harmony.PatchAll();
        Debug.Log("Haste in First Person is loaded!");
    }

    public static void InsertUi(GameObject Canv){
        if(Canv==null){
            Debug.LogError("HFPM: null canvas on InsertUi");
            return;
        }
        if(uiElRectTransform==null){
            GameObject uiEle = UnityEngine.Object.Instantiate(prefabUi, new Vector3(0,0,0), quaternion.identity);
            
            uiEle.transform.parent = Canv.transform;

            uiElRectTransform = uiEle.GetComponent<RectTransform>();
            img = uiEle.GetComponent<RawImage>();
            rt = new RenderTexture(1920, 600, 0);
            rt.enableRandomWrite = true;
            img.texture = rt;
            uiElRectTransform.localScale= new Vector3(1,1,1);
        }
        // uiElRectTransform.SetSiblingIndex(1);
    }

    public static void UpdateUi(){
        //Debug.Log("HFPMHFPMHFPM:"+img+" "+PlayerCharacter.localPlayer.data.GetBoost()+" "+cBoostAmount+" "+boostLerpSpeed+" "+Screen.width);
        if(!SystemInfo.supportsComputeShaders){
            return;
        }
        
        img.gameObject.SetActive(Enabled&&BoostUi);

        if(!img.enabled){
            cBoostAmount=0;
            return;
        }
        
        
        cBoostAmount = Mathf.Lerp(cBoostAmount, PlayerCharacter.localPlayer.data.GetBoost(), Time.deltaTime*boostLerpSpeed);
        internalTime += Time.deltaTime*Mathf.Clamp(cBoostAmount*2,.5f,2)*scrollSpeed;
        
        // Debug.Log(Time.time);
        // Time.timeScale = ti;
        float aspect = (float) Screen.width / (float) Screen.height;
        // float t = Mathf.InverseLerp( 3.84f, 1, aspect);
        if(aspect>2){
            float t = Mathf.InverseLerp( 3.84f, 2, aspect);
            uiElRectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(-371+150, -517+150, t));

        }else{
            float t = Mathf.InverseLerp( 2, 1, aspect);
            uiElRectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(-517+150, -960+150, t));
        }
        // Debug.Log(t+" "+aspect);

        // imgTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(-371, -960, t));

        if(rt==null)
            return;

        if(shader==null)
            return;

        UnityEngine.Color color = gradient.Evaluate(cBoostAmount);

		if (cBoostAmount > 1f)
		{
			UnityEngine.Color a = rainbowGradient.Evaluate(Time.time * 1f % 1f);
			float num = cBoostAmount - 1f;
			num = Mathf.Clamp01(num / 3f);
			num = Mathf.Pow(num, 0.2f);
            num *= 1.2f;
            num=Mathf.Clamp(num,0,1);
			color = UnityEngine.Color.Lerp(color, a * .75f, num);
		}


        float[] colorAsArray = new float[] {color.r, color.g, color.b, color.a};
        // Debug.Log(BoostAmount+" "+colorAsArray[0]+" "+colorAsArray[1]+" "+colorAsArray[2]+" "+colorAsArray[3]);
        

        shader.SetTexture(0, "Result", rt);
        shader.SetFloats("_boostColor", colorAsArray);
        
        shader.SetFloat("_boostSpeed", Mathf.Clamp(cBoostAmount*.75f,.1f,.75f));
        shader.SetFloat("_time", internalTime);
        
        shader.Dispatch(0, Mathf.CeilToInt(1920/8), Mathf.CeilToInt(600/8), 1);
    }

    public static Vector3 getOffset(){
        return new Vector3(0, camHeight, -0.2f);
    }
    public static void applyFOV(Camera cam){
        if(UseFovScaling)
            cam.fieldOfView = cam.fieldOfView+FOV-70;
        else
            cam.fieldOfView = FOV;
    }


    public static string[] PathsToDisable = new string[]{
        "Player/Visual/Courier_Retake/Courier/Armature/Hip/Spine_1/Spine_2/Spine_3/Neck/Head/Glasses",
        "Player/Visual/Courier_Retake/Courier/Armature/Hip/Spine_1/Camera (1)",
        "Player/Visual/Courier_Retake/Courier/Armature/Hip/Spine_1/Spine_2/Spine_3/Neck/Head/Eye_l",
        "Player/Visual/Courier_Retake/Courier/Armature/Hip/Spine_1/Spine_2/Spine_3/Neck/Head/Eye_r",
        "Player/Visual/Courier_Retake/Courier/Armature/Hip/Spine_1/Spine_2/Spine_3/BagRig (1)",
        "Player/Visual/Courier_Retake/Courier/Armature/Hip/Spine_1/Camera (1)",
        "Player/Visual/Courier_Retake/Courier/Meshes/Body",
        "Player/Visual/Courier_Retake/Courier/Meshes/Head",
        "Player/Visual/Courier_Retake/Courier/Meshes/TeetBottom",
        "Player/Visual/Courier_Retake/Courier/Meshes/TeethTop",
        "Player/Visual/Courier_Retake/Courier/Meshes/Tongue",
        "Player/Visual/Courier_Retake/Courier/Meshes/StraightArmRef"
    };
    public static void applyActives(){
        foreach (var path in PathsToDisable){
            var g = GameObject.Find(path);
            if(g==null){
                Debug.LogError("GameObject could not be found! "+path);
                return;
            }
            g.SetActive(!Enabled);
        }
    }
}

//-------------Patches

[HarmonyPatch(typeof(CameraMovement))]
public class HarmonyCameraMovementPatch{

    static float rotX=0, rotY=0, sens=1;
    
    // [HarmonyPatch("Start")]
    // [HarmonyPostfix]
    // static void CameraStartPost(CameraMovement __instance){
    //     rotX = PlayerCharacter.localPlayer.transform.localEulerAngles.x;
    //     rotY = PlayerCharacter.localPlayer.transform.localEulerAngles.y;

    // }
    
    [HarmonyPatch("GetOffset")]
    [HarmonyPrefix]
    static bool GetOffsetPre(CameraMovement __instance, ref Vector3 __result){
        if(!FirstPersonPlugin.Enabled){
            return true;
        }

        __result = FirstPersonPlugin.getOffset();
        FirstPersonPlugin.applyFOV(MainCamera.instance.cam);

        return false;
    }
    [HarmonyPatch("ApplyRotation")]
    [HarmonyPrefix]
    static bool ApplyRotation(CameraMovement __instance){
        if(!FirstPersonPlugin.Enabled||FirstPersonPlugin.useReFocus){
            return true;
        }
        
        // float mouseX = Input.GetAxisRaw("Mouse X")*sens;
        float mouseY = PlayerCharacter.localPlayer.input.lookInput.y;//Input.GetAxisRaw("Mouse Y")*sens;
        // rotY+=mouseX;
        rotX+=mouseY; 
        rotX=Mathf.Clamp(rotX, -90, 90);

        // __instance.transform.localRotation = Quaternion.Euler(rotX,rotY,0);
        // PlayerCharacter.localPlayer.data.lookRotationEulerAngles = __instance.transform.localEulerAngles;
        UnityEngine.Vector3 allowedRot = new(rotX,PlayerCharacter.localPlayer.data.lookRotationEulerAngles.y,PlayerCharacter.localPlayer.data.lookRotationEulerAngles.z);
        __instance.transform.localEulerAngles = allowedRot;
        return false;
    }

}

[HarmonyPatch(typeof(PlayerCharacter))]
public class HarmonyPlayerCharacterPatch{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    static void StartPost(PlayerCharacter __instance){
        if(!FirstPersonPlugin.Enabled){
            return;
        }
        FirstPersonPlugin.applyActives();
    }
}


[HarmonyPatch(typeof(VFX_BoostShoes))]
public class HarmonyVFXBoostShoes{
    // public static MeshRenderer shoeRender;
    [HarmonyPatch("Start")]
    [HarmonyPrefix]
    public static void StartPre(VFX_BoostShoes __instance){
        FirstPersonPlugin.InsertUi(GameplayUIManager.instance.gameObject);
        FirstPersonPlugin.gradient=__instance.gradient;
        FirstPersonPlugin.rainbowGradient=__instance.rainbowGradient;
        // shoeRender=__instance.GetComponent<MeshRenderer>();
    }

    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    public static bool UpdatePre(VFX_BoostShoes __instance){
        try{
            FirstPersonPlugin.UpdateUi();

        }catch(Exception e){
            Debug.Log(e.Message+" "+e.StackTrace);
        }
        if(!FirstPersonPlugin.Enabled){
            return true;
        }
       
        __instance.GetComponent<MeshRenderer>().material.SetColor("_Color2", new UnityEngine.Color(0,0,0,0));
        
        return false;
    }
}


//--------------settings

[HasteSetting]
public class GlobalEnableSetting : OffOnSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        FirstPersonPlugin.Enabled = base.Value==OffOnMode.ON;

        FirstPersonPlugin.applyActives();
    }

    public string GetCategory()
    {
        return "First Person";
    }

    public LocalizedString GetDisplayName()
    {
        return new UnlocalizedString("Enable First Person?");
    }

    public override List<LocalizedString> GetLocalizedChoices()
    {
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
class CamHeightOffsetSetting : FloatSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        FirstPersonPlugin.camHeight = base.Value;
    }

    public string GetCategory()
    {
        return "First Person";
    }

    public LocalizedString GetDisplayName()
    {
        return new UnlocalizedString("Camera Height Offset");
    }

    protected override float GetDefaultValue()
    {
        return 3.5f;
    }

    protected override float2 GetMinMaxValue()
    {
        return new float2(0,7);
    }
}

[HasteSetting]
class CamFovSetting : FloatSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        FirstPersonPlugin.FOV = base.Value;
    }

    public string GetCategory()
    {
        return "First Person";
    }

    public LocalizedString GetDisplayName()
    {
        return new UnlocalizedString("Camera FOV");
    }

    protected override float GetDefaultValue()
    {
        return 70;
    }

    protected override float2 GetMinMaxValue()
    {
        return new float2(0,360);
    }
}
[HasteSetting]
public class FovScalingSetting : OffOnSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        FirstPersonPlugin.UseFovScaling = base.Value==OffOnMode.ON;

    }

    public string GetCategory()
    {
        return "First Person";
    }

    public LocalizedString GetDisplayName()
    {
        return new UnlocalizedString("Scale FOV with speed?");
    }

    public override List<LocalizedString> GetLocalizedChoices()
    {
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
public class BoostUiEnable : OffOnSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        FirstPersonPlugin.BoostUi = base.Value==OffOnMode.ON;

        // FirstPersonPlugin.applyActives();
    }

    public string GetCategory()
    {
        return "First Person";
    }

    public LocalizedString GetDisplayName()
    {
        return new UnlocalizedString("Enable Boost Ui?");
    }

    public override List<LocalizedString> GetLocalizedChoices()
    {
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
public class DisableCameraRefocus : OffOnSetting, IExposedSetting
{
    public override void ApplyValue()
    {
        FirstPersonPlugin.useReFocus = base.Value==OffOnMode.ON;

        FirstPersonPlugin.applyActives();
    }

    public string GetCategory()
    {
        return "First Person";
    }

    public LocalizedString GetDisplayName()
    {
        return new UnlocalizedString("Use camera refocus?");
    }

    public override List<LocalizedString> GetLocalizedChoices()
    {
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