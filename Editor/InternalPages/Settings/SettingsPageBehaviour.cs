using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Joyman.TabWindow
{
    public class SettingsPageBehaviour : PageBehaviour
    {
        private Button button;
        private bool initedAfterLoaded;

        public override void Init(VisualElement pageElement, TabWindow rootWindow)
        {
            base.Init(pageElement, rootWindow);

            button = pageElement.Q<Button>("InitializePagesButton");

            button.clicked -= InitializePages;
            button.clicked += InitializePages;

            initedAfterLoaded = false;
        }

        public override void Load<T>(Dictionary<string, T> loadData)
        {
            base.Load(loadData);

            if(!initedAfterLoaded)
            {
                var windowName = pageElement.Q<TextField>("WindowNameInput");

                if(windowName.value == "")
                {
                    windowName.value = "TabWindow";
                }
                
                rootWindow.titleContent = new GUIContent(windowName.value);
                
                InitializePages();
            }
        }

        private void InitializePages()
        {
            var pagesPathInput = pageElement.Q<TextField>("PagesPathInput");
            var pagesPath = pagesPathInput.value;

            var loaded = rootWindow.LoadPages(pagesPath);

            if(loaded)
            {
                button.style.display = DisplayStyle.None;
            }

            initedAfterLoaded = true;
        }
    }
}