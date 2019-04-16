using BepInEx;
using RoR2;
using RoR2.ConVar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NotEnoughModManagers
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a very simple plugin that adds Bandit to the game, and gives you a tier 3 item whenever you press F2.
    //Lets examine what each line of code is for:

    //This attribute specifies that we have a dependency on R2API, as we're using it to add Bandit to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency("com.bepis.r2api")]

    //This attribute is required, and lists metadata for your plugin.
    //The GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config). I like to use the java package notation, which is "com.[your name here].[your plugin name here]"
    //The name is the name of the plugin that's displayed on load, and the version number just specifies what version the plugin is.
    [BepInPlugin("is.361zn.is.notenoughmodmanagers", "NotEnoughModManagers", "0.1")]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class NotEnoughModManagers : BaseUnityPlugin
    {
        private bool isMainmenu;
        private bool isOpen;
        private Rect windowRect;

        private Matrix4x4 matrix;
        private const float width = 1920;
        private const float height = 1080;

        private List<Mod> mods;

        private Vector2 modScrollPosition;
        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920.0f, Screen.height / 1080.0f, 1.0f));

            isMainmenu = false;
            isOpen = false;
            On.RoR2.UI.MPButton.InputModuleIsAllowed += (orig, self, inputModule) =>
            {
                if (isOpen)
                {
                    return false;
                }
                return orig(self, inputModule);
            };
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += (orig, self, controller) =>
            {
                isMainmenu = self.name == "TitleMenu";
                orig(self, controller);
            };
            SceneManager.sceneUnloaded += delegate (Scene scene)
            {
                isMainmenu = false;
            };

            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnExit += (orig, self, controller) =>
            {
                isMainmenu = false;
                orig(self, controller);
            };
            // Fuck that EAWarning screen
            On.RoR2.RoR2Application.UnitySystemConsoleRedirector.Redirect += (orig) => {
            };
            On.RoR2.UI.MainMenu.MainMenuController.Start += (orig, self) =>
            {
                orig(self);
                if (self.desiredMenuScreen == self.EAwarningProfileMenu)
                {
                    self.desiredMenuScreen = self.titleMenuScreen;
                }
            };

            mods = new List<Mod>();
            StartCoroutine(GetThunderstorePackages());
        }

        public IEnumerator GetThunderstorePackages()
        {
            var www = UnityWebRequest.Get("https://thunderstore.io/api/v1/package");
            www.SetRequestHeader("User-Agent", "NotEnoughModManagers/0.1");
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            } else
            {
                var jsonNode = SimpleJSON.JSON.Parse(www.downloadHandler.text);
                foreach(var mod in jsonNode.AsArray.Children)
                {
                    print(mod["full_name"].Value);
                    mods.Add(new Mod()
                    {
                        name = mod["name"].Value,
                        owner = mod["owner"].Value,
                    });
                }
            }
        }

        private void OnGUI()
        {
            GUI.matrix = matrix;
            if (isMainmenu)
            {
                if (GUI.Button(new Rect(10, 10, 200, 50), "Not Enough Mod Manager"))
                {
                    isOpen = !isOpen;
                }
                if (isOpen)
                {
                    windowRect = GUILayout.Window(0, new Rect(width * 0.05f, height * 0.05f , width * 0.9f, height * 0.9f), WindowFunction, "");
                }
            }
        }

        private void WindowFunction(int windowId)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Not Enough Mod Managers");
            GUILayout.EndVertical();
            GUILayout.BeginVertical("box");
            modScrollPosition = GUILayout.BeginScrollView(modScrollPosition);
            var textStyle = new GUIStyle("label");
            foreach (var mod in mods)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(mod.name, textStyle);
                GUILayout.Label(mod.owner, textStyle);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        //The Update() method is run on every frame of the game.
        public void Update()
        {
        }
    }

    class Mod
    {
        public string name;
        public string owner;
    }
}