using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Joyman.TabWindow
{
    public class TabWindow : EditorWindow
    {
        private Label debugLabel;
        private string toolPath;

        private VisualElement buttonsContainer;
        private VisualElement viewsContainer;
        private StyleSheet activeTab;
        private StyleSheet inactiveTab;
        private List<PageBehaviour> pageBehaviours;
        private Dictionary<string, object> sharedData;

        [MenuItem("Tools/TabWindow")]
        public static void ShowExample()
        {
            TabWindow wnd = GetWindow<TabWindow>();
            wnd.titleContent = new GUIContent("Tech Artist Tool");

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
                toolPath = GetToolFolderPath();
            }

            pageBehaviours = new List<PageBehaviour>();
            sharedData = new Dictionary<string, object>();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(toolPath + "TabWindow.uxml");
            rootVisualElement.Add(visualTree.CloneTree());

            activeTab = AssetDatabase.LoadAssetAtPath<StyleSheet>(toolPath + "Core/StyleSheets/ActiveTab.uss");
            inactiveTab = AssetDatabase.LoadAssetAtPath<StyleSheet>(toolPath + "Core/StyleSheets/InactiveTab.uss");
            buttonsContainer = rootVisualElement.Q<VisualElement>("ButtonsContainer");
            viewsContainer = rootVisualElement.Q<VisualElement>("ViewsContainer");
            debugLabel = rootVisualElement.Q<Label>("DebugLabel");
            
            LoadPages();
            LoadWindowData();

            TabButtonPressedHandler("Settings");
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

        private string GetToolFolderPath([CallerFilePath] string callerPath = null)
        {
            callerPath = Path.GetDirectoryName(callerPath);
            callerPath = callerPath.Replace("\\", "/");
            callerPath = callerPath.Substring(callerPath.LastIndexOf("/Assets/", StringComparison.Ordinal) + 1);
            callerPath += "/";
            return callerPath;
        }

        private void Deinit()
        {
            if(pageBehaviours != null)
            {
                pageBehaviours.Clear();
                pageBehaviours = null;
            }

            if(sharedData != null)
            {
                sharedData.Clear();
                sharedData = null;
            }

            SaveWindowData();
        }

        private void LoadWindowData()
        {
            var textFieldsJson = EditorPrefs.GetString("TabWindow_TextFields");
            Dictionary<string, string> textFieldsLoadData = JsonConvert.DeserializeObject<Dictionary<string, string>>(textFieldsJson);

            var togglesJson = EditorPrefs.GetString("TabWindow_Toggles");
            Dictionary<string, bool> togglesLoadData = JsonConvert.DeserializeObject<Dictionary<string, bool>>(togglesJson);

            foreach (var item in pageBehaviours)
            {
                item.Load<string>(textFieldsLoadData);
                item.Load<bool>(togglesLoadData);
            }
        }

        private void SaveWindowData()
        {
            Save<string, TextField>("TabWindow_TextFields");
            Save<bool, Toggle>("TabWindow_Toggles");
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

        private void LoadPages()
        {
            pageBehaviours?.Clear();

            var uxmls = Directory.GetFiles(toolPath + "Pages/", "*.uxml", SearchOption.AllDirectories);
            foreach (var uxml in uxmls)
            {
                var pageWindowAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
                var pageVisualElement = pageWindowAsset.CloneTree();

                var behaviourPath = uxml.Replace("Window.uxml", "Behaviour.cs");
                var pageBehaviourAsset = AssetDatabase.LoadMainAssetAtPath(behaviourPath) as UnityEngine.Object;
                var behaviourType = Type.GetType(pageBehaviourAsset.name);

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
                
                Action tabButtonPressedHandler = ()=> TabButtonPressedHandler(pageWindowAsset.name);

                button.clicked -= tabButtonPressedHandler;
                button.clicked += tabButtonPressedHandler;
            }
        }

        private void TabButtonPressedHandler(string buttonName)
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
}