using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Joyman.TabWindow
{
    public class PageBehaviour
    {
        public Action<string, Color> OnDebugMessage;

        protected VisualElement pageElement;
        protected TabWindow rootWindow;
        private string currentProcess = "";

        public virtual void Init(VisualElement pageElement, TabWindow rootWindow)
        {
            this.pageElement = pageElement;
            this.rootWindow = rootWindow;
        }

        public virtual void Load<T>(Dictionary<string, T> loadData)
        {
            if(loadData != null)
            {
                foreach (var item in loadData)
                {
                    var visualElement = pageElement.Q(item.Key) as BaseField<T>;

                    if(visualElement != null)
                    {
                        visualElement.value = item.Value;
                    }
                }
            }
        }

        protected void DebugMessage(string text, Color color)
        {
            OnDebugMessage?.Invoke(text, color);
        }

        protected virtual void ErrorHandler(Exception e)
        {
            DebugMessage(e.Message, Color.red);
        }

        protected virtual void SuccessHandler(string m)
        {
            DebugMessage(m, Color.green); 
        }

        protected virtual void ProcessStartedHandler(string processName)
        {
            if(currentProcess == "")
            {
                currentProcess = processName;
                OnDebugMessage?.Invoke("", Color.black);
                EditorUtility.DisplayProgressBar("Processing: " + processName, "Please wait until processing finished", 50f);
            }
        }

        protected virtual void ProcessEndedHandler(string processName)
        {
            if(currentProcess == processName)
            {
                currentProcess = "";
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
