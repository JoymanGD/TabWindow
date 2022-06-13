#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Linq;

namespace Joyman.TabWindow
{
    public class TabWindow : EditorWindow
    {
        private string toolPath = "Packages/com.joyman.tabwindow/Editor/";
        private VisualElement buttonsContainer;
        private VisualElement viewsContainer;
        private StyleSheet activeTab;
        private StyleSheet inactiveTab;
        private List<PageBehaviour> internalPageBehaviours;
        private List<PageBehaviour> externalPageBehaviours;
        private Dictionary<string, object> sharedData;
        private Label debugLabel;

        [MenuItem("Tools/TabWindow", false, 999)]
        public static void ShowExample()
        {
            TabWindow wnd = GetWindow<TabWindow>();

            wnd.minSize = new Vector2(640, 480);
            
            var position = wnd.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            wnd.position = position;
        }

        private void OnEnable()
        {
            Init();
        }

        private void OnDisable()
        {
            Deinit();
        }

        private void Init()
        {
            if(string.IsNullOrEmpty(toolPath))
            {
                toolPath = GetToolFolderPathByCaller();
            }

            internalPageBehaviours = new List<PageBehaviour>();
            externalPageBehaviours = new List<PageBehaviour>();
            sharedData = new Dictionary<string, object>();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(toolPath + "TabWindow.uxml");
            rootVisualElement.Add(visualTree.CloneTree());

            activeTab = AssetDatabase.LoadAssetAtPath<StyleSheet>(toolPath + "Core/StyleSheets/ActiveTab.uss");
            inactiveTab = AssetDatabase.LoadAssetAtPath<StyleSheet>(toolPath + "Core/StyleSheets/InactiveTab.uss");
            buttonsContainer = rootVisualElement.Q<VisualElement>("ButtonsContainer");
            viewsContainer = rootVisualElement.Q<VisualElement>("ViewsContainer");
            debugLabel = rootVisualElement.Q<Label>("DebugLabel");
            
            LoadInternalPages();
            LoadWindowData(PageType.Internal);

            LoadExternalPages();
            LoadWindowData(PageType.External);
            
            SwitchToTab("Settings");
        }

        public void AddSharedData(string key, object value)
        {
            if(sharedData.ContainsKey(key))
            {
                sharedData[key] = value;
            }
            else
            {
                sharedData.Add(key, value);
            }
        }

        public T GetSharedData<T>(string key) where T : class
        {
            T value = null;

            if(sharedData.ContainsKey(key))
            {
                try
                {
                    value = sharedData[key] as T;        
                }
                catch (System.Exception e)
                {
                    DebugMessage(e.Message, Color.red);
                    return null;
                }
            }

            return value;
        }

        private string GetToolFolderPathByCaller([CallerFilePath] string callerPath = null)
        {
            callerPath = Path.GetDirectoryName(callerPath);
            callerPath = callerPath.Replace("\\", "/");
            callerPath = callerPath.Substring(callerPath.LastIndexOf("/Assets/", StringComparison.Ordinal) + 1);
            callerPath += "/";
            return callerPath;
        }

        private void Deinit()
        {
            if(externalPageBehaviours != null)
            {
                externalPageBehaviours.Clear();
                externalPageBehaviours = null;
            }

            if(sharedData != null)
            {
                sharedData.Clear();
                sharedData = null;
            }

            SaveWindowData();
        }

        public void LoadWindowData(PageType pageType)
        {
            List<PageBehaviour> pageBehaviours = null;

            switch (pageType)
            {
                case PageType.Internal:
                    pageBehaviours = internalPageBehaviours;
                    break;
                case PageType.External:
                    pageBehaviours = externalPageBehaviours;
                    break;
            }

            var textFieldsJson = EditorPrefs.GetString("TabWindow_TextFields");
            Dictionary<string, string> textFieldsLoadData = JsonConvert.DeserializeObject<Dictionary<string, string>>(textFieldsJson);

            var togglesJson = EditorPrefs.GetString("TabWindow_Toggles");
            Dictionary<string, bool> togglesLoadData = JsonConvert.DeserializeObject<Dictionary<string, bool>>(togglesJson);

            var integersJson = EditorPrefs.GetString("TabWindow_Integers");
            Dictionary<string, int> integersLoadData = JsonConvert.DeserializeObject<Dictionary<string, int>>(integersJson);

            var vectors2Json = EditorPrefs.GetString("TabWindow_Vectors2");
            Dictionary<string, Vector2> vectors2LoadData = JsonConvert.DeserializeObject<Dictionary<string, Vector2>>(vectors2Json);

            var vectors3Json = EditorPrefs.GetString("TabWindow_Vectors3");
            Dictionary<string, Vector3> vectors3LoadData = JsonConvert.DeserializeObject<Dictionary<string, Vector3>>(vectors3Json);

            foreach (var item in pageBehaviours)
            {
                item.Load<string>(textFieldsLoadData);
                item.Load<bool>(togglesLoadData);
                item.Load<int>(integersLoadData);
                item.Load<Vector2>(vectors2LoadData);
                item.Load<Vector3>(vectors3LoadData);
            }
        }

        private void SaveWindowData()
        {
            Save<string, TextField>("TabWindow_TextFields");
            Save<bool, Toggle>("TabWindow_Toggles");
            Save<int, IntegerField>("TabWindow_Integers");
            Save<Vector2, Vector2Field>("TabWindow_Vectors2");
            Save<Vector3, Vector3Field>("TabWindow_Vectors3");
        }

        private void Save<T1, T2>(string saveName) where T2 : BaseField<T1>
        {
            Dictionary<string, T1> saveData = new Dictionary<string, T1>();
            rootVisualElement.Query<T2>().ForEach(r=>
            {
                saveData.Add(r.name, r.value);
            });

            var json = JsonConvert.SerializeObject(saveData);

            EditorPrefs.SetString(saveName, json);
        }

        private void LoadInternalPages()
        {
            LoadPages(toolPath + "InternalPages/", PageType.Internal);
        }

        private void LoadExternalPages()
        {
            SettingsPageBehaviour settingsPageBehaviour = internalPageBehaviours.FirstOrDefault(b=> b.GetType() == typeof(SettingsPageBehaviour)) as SettingsPageBehaviour;
            settingsPageBehaviour?.InitializePages();
            settingsPageBehaviour?.SetWindowname();
        }

        public bool LoadPages(string pagesPath, PageType pageType)
        {
            List<PageBehaviour> pageBehaviours = null;

            switch (pageType)
            {
                case PageType.Internal:
                    pageBehaviours = internalPageBehaviours;
                    break;
                case PageType.External:
                    pageBehaviours = externalPageBehaviours;
                    break;
            }

            bool result = false;

            if(!string.IsNullOrEmpty(pagesPath))
            {
                if(Directory.Exists(pagesPath))
                {
                    var uxmls = Directory.GetFiles(pagesPath, "*.uxml", SearchOption.AllDirectories);
                    if(uxmls.Length > 0)
                    {
                        foreach (var uxml in uxmls)
                        {
                            var pageWindowAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
                            var pageVisualElement = pageWindowAsset.CloneTree();

                            var behaviourPath = uxml.Replace("Window.uxml", "Behaviour.cs");
                            var pageBehaviourAsset = AssetDatabase.LoadMainAssetAtPath(behaviourPath) as UnityEngine.Object;
                            var behaviourType = GetType("Joyman.TabWindow." + pageBehaviourAsset.name);

                            var pageBehaviour = Activator.CreateInstance(behaviourType) as PageBehaviour;
                            pageBehaviour.OnDebugMessage -= DebugMessage;
                            pageBehaviour.OnDebugMessage += DebugMessage;
                            pageBehaviours.Add(pageBehaviour);

                            var view = pageVisualElement.Q<VisualElement>(pageWindowAsset.name + "View");
                            viewsContainer.Add(view);

                            pageBehaviour.Init(view, this);

                            var buttonAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(toolPath + "Core/UXML/TabButton.uxml");
                            var buttonVE = buttonAsset.CloneTree();
                            var button = buttonVE.Q<Button>("TabButton");
                            button.text = pageWindowAsset.name.Replace("PageWindow", "");
                            button.name = pageWindowAsset.name + "Button";

                            buttonsContainer.Add(button);

                            Action tabButtonPressedHandler = ()=> SwitchToTab(pageWindowAsset.name);

                            button.clicked -= tabButtonPressedHandler;
                            button.clicked += tabButtonPressedHandler;
                        }

                        result = true;
                    }
                    else
                    {
                        DebugMessage("There are no pages on path!", Color.red);
                    }
                }
                else
                {
                    DebugMessage("Pages path doesn't exist!", Color.red);
                }
            }
            else
            {
                DebugMessage("Pages path is empty!", Color.red);
            }

            return result;
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public void SwitchToTab(string buttonName)
        {
            var views = viewsContainer.Children();
            
            foreach (VisualElement item in views)
            {
                if(item.name.Contains(buttonName))
                {
                    item.style.display = DisplayStyle.Flex;
                }
                else
                {
                    item.style.display = DisplayStyle.None;
                }
            }

            var buttons = buttonsContainer.Children();
            
            foreach (Button item in buttons)
            {
                if(item.name.Contains(buttonName))
                {
                    if(item.styleSheets.Contains(inactiveTab))
                    {
                        item.styleSheets.Remove(inactiveTab);
                    }

                    item.styleSheets.Add(activeTab);
                }
                else
                {
                    if(item.styleSheets.Contains(activeTab))
                    {
                        item.styleSheets.Remove(activeTab);
                    }

                    item.styleSheets.Add(inactiveTab);
                }
            }
        }

        protected void DebugMessage(string text, Color color)
        {
            debugLabel.text = text;
            debugLabel.style.color = color;
        }
    }

    public enum PageType
    {
        External = 0,
        Internal = 1
    }
}
#endif
