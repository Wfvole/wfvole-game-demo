using System;
using TMPro;
using UnityEngine;
using XLua;



[LuaCallCSharp]
public class BuildingHudUICtrl : UIcontroller
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI introductionText;
    public TextMeshProUGUI produceText;
    public TextMeshProUGUI consumeText;
    public TextAsset luaScript;
    internal static LuaEnv luaEnv = new LuaEnv();//场景里只能有一个LuaEnv
    internal static float lastGCTime = 0;
    internal const float GCInterval = 1;//1 second
    private Action luaStart;
    private Action<BuildingData> luaReadBuildingData;

    private LuaTable scriptScopeTable;
    public override void Awake()
    {
        base.Awake();
        // 为每个脚本设置一个独立的脚本域防止脚本间全局变量、函数冲突
        scriptScopeTable = luaEnv.NewTable();
        // 设置其元表的 __index, 使其能够访问全局变量
        using (LuaTable meta = luaEnv.NewTable())
        {
            meta.Set("__index", luaEnv.Global);
            scriptScopeTable.SetMetaTable(meta);
        }
        // 将游戏物体脚本逻辑所需值注入到 Lua 脚本域中
        scriptScopeTable.Set("self", this);
        // 执行脚本
        luaEnv.DoString(luaScript.text, luaScript.name, scriptScopeTable);
        //对应Lua脚本中定义的函数转化为C#委托
        Action luaAwake = scriptScopeTable.Get<Action>("awake");
        scriptScopeTable.Get("start", out luaStart);
        scriptScopeTable.Get("luaReadBuildingData", out luaReadBuildingData);
        if (luaReadBuildingData == null)
        {
            scriptScopeTable.Get("ReadBuildingData", out luaReadBuildingData);
        }
        if (luaAwake != null)
        {
            luaAwake();
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (view["NameText"].TryGetComponent(out TextMeshProUGUI a))nameText = a;
        if (view["IntroductionText"].TryGetComponent(out TextMeshProUGUI b)) introductionText = b;
        if (view["ProduceText"].TryGetComponent(out TextMeshProUGUI c)) produceText = c;
        if (view["ConsumeText"].TryGetComponent(out TextMeshProUGUI d)) consumeText = d;
        add_button_listener("DButton",OnDButton);
        Eventmanager.Instance.AddListener("ReadBuildingData", ReadBuildingData);
        if (luaStart != null)
        {
            luaStart();
        }
    }
    void OnDButton() 
    {
        Debug.Log("尝试销毁建筑");
        Eventmanager.Instance.Emit("DestroyBuilding",true); 
    }
    /*/public void ReadBuildingData(BuildingData buildingdata)
    {
        nameText.text = buildingdata.name;
        introductionText.text = buildingdata.introduction;
        produceText.text = "产出"+"光"+buildingdata.lightRate.ToString("F1")+"/s  生命"+buildingdata.lifeRate.ToString("F1")+"/s"  ;
        consumeText.text = "消耗"+buildingdata.useResourceType.ToString()+buildingdata.consume.ToString("F1")+"/s" ;
    }/*/
    public void ReadBuildingData(string a, object b)
    {
        if(b is BuildingData buildingdata)
        {
            if (luaReadBuildingData != null)
            {
                luaReadBuildingData(buildingdata);
            }
        }
        
    }
}
