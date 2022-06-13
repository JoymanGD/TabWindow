using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Joyman.TabWindow
{
    public class SettingsPageBehaviour : PageBehaviour
    {
        private Button button;

        public override void Init(VisualElement pageElement, Joyman.TabWindow.TabWindow rootWindow)
        {
            base.Init(pageElement, rootWindow);

            button = pageElement.Q<Button>("InitializePagesButton");

            button.clicked -= InitializePages;
            button.clicked += InitializePages;
        }

        public void InitializePages()
        {
            var pagesPathInput = pageElement.Q<TextField>("PagesPathInput");
            var pagesPath = pagesPathInput.value;

            var loaded = rootWindow.LoadPages(pagesPath, PageType.External);

            if(loaded)
            {
                button.style.display = DisplayStyle.None;
            }
        }

        public void SetWindowname()
        {
            var windowName = pageElement.Q<TextField>("WindowNameInput");

            if(windowName.value == "")
            {
                windowName.value = "TabWindow";
            }
            
            rootWindow.titleContent = new GUIContent(windowName.value);
        }
    }
}
